using LeagueLens.Domain.Entities;
using LeagueLens.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// Computes standings, power rankings, and trends for a league at query time from persisted
/// <see cref="Matchup"/> rows — no snapshot is stored (see ADR-006).
/// </summary>
public sealed class LeagueIntelService(
    LeagueLensDbContext db,
    IPowerRankingCalculator powerRankingCalculator,
    IOptions<LeagueIntelOptions> options)
{
    /// <summary>Current-season standings for the league, through the latest played week.</summary>
    public async Task<IReadOnlyList<StandingsEntry>> GetStandingsAsync(Guid leagueId, CancellationToken ct)
    {
        var (memberships, matchups) = await LoadLeagueDataAsync(leagueId, ct);
        return RankStandings(BuildTeamStats(memberships, matchups));
    }

    /// <summary>Current power rankings for the league, through the latest played week.</summary>
    public async Task<IReadOnlyList<PowerRankingEntry>> GetPowerRankingsAsync(Guid leagueId, CancellationToken ct)
    {
        var (memberships, matchups) = await LoadLeagueDataAsync(leagueId, ct);
        return RankPowerRankings(BuildTeamStats(memberships, matchups));
    }

    /// <summary>
    /// Standing-movement and scoring-trend indicators for each team, comparing the current
    /// state against <see cref="LeagueIntelOptions.TrendWindowWeeks"/> weeks ago.
    /// </summary>
    public async Task<IReadOnlyList<TrendEntry>> GetTrendsAsync(Guid leagueId, CancellationToken ct)
    {
        var (memberships, matchups) = await LoadLeagueDataAsync(leagueId, ct);
        var window = options.Value.TrendWindowWeeks;
        var latestWeek = matchups.Count == 0 ? 0 : matchups.Max(m => m.Week);

        var currentRanks = RankStandings(BuildTeamStats(memberships, matchups))
            .ToDictionary(s => s.LeagueMembershipId, s => s.Rank);

        Dictionary<Guid, int>? priorRanks = null;
        if (latestWeek >= window + 1)
        {
            var priorMatchups = matchups.Where(m => m.Week <= latestWeek - window).ToList();
            priorRanks = RankStandings(BuildTeamStats(memberships, priorMatchups))
                .ToDictionary(s => s.LeagueMembershipId, s => s.Rank);
        }

        Dictionary<Guid, decimal>? recentAvg = null;
        Dictionary<Guid, decimal>? priorAvg = null;
        if (latestWeek >= window * 2)
        {
            var recentMatchups = matchups.Where(m => m.Week > latestWeek - window).ToList();
            var priorWindowMatchups = matchups
                .Where(m => m.Week > latestWeek - (2 * window) && m.Week <= latestWeek - window)
                .ToList();

            recentAvg = AveragePointsFor(memberships, recentMatchups, window);
            priorAvg = AveragePointsFor(memberships, priorWindowMatchups, window);
        }

        return memberships
            .Select(m => new TrendEntry(
                m.Id,
                m.TeamName,
                priorRanks is null ? null : priorRanks[m.Id] - currentRanks[m.Id],
                recentAvg is null ? null : recentAvg[m.Id] - priorAvg![m.Id]))
            .OrderBy(t => t.TeamName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(t => t.LeagueMembershipId)
            .ToList();
    }

    private async Task<(List<LeagueMembership> Memberships, List<Matchup> Matchups)> LoadLeagueDataAsync(Guid leagueId, CancellationToken ct)
    {
        var memberships = await db.LeagueMemberships.Where(m => m.LeagueId == leagueId).ToListAsync(ct);
        var matchups = await db.Matchups.Where(m => m.LeagueId == leagueId).ToListAsync(ct);
        return (memberships, matchups);
    }

    private static List<TeamStats> BuildTeamStats(IReadOnlyList<LeagueMembership> memberships, IReadOnlyList<Matchup> matchups)
    {
        var accumulators = memberships.ToDictionary(m => m.Id, m => new TeamStatsAccumulator(m.Id, m.TeamName));

        foreach (var matchup in matchups)
        {
            Accumulate(accumulators, matchup.HomeLeagueMembershipId, matchup.HomeScore, matchup.AwayScore);
            Accumulate(accumulators, matchup.AwayLeagueMembershipId, matchup.AwayScore, matchup.HomeScore);
        }

        return accumulators.Values.Select(a => a.ToStats()).ToList();
    }

    private static void Accumulate(Dictionary<Guid, TeamStatsAccumulator> accumulators, Guid membershipId, decimal scored, decimal allowed)
    {
        // Defensive: a matchup should never reference a membership outside the league it belongs
        // to, but silently skipping keeps this a pure read path instead of throwing on bad data.
        if (!accumulators.TryGetValue(membershipId, out var accumulator))
            return;

        accumulator.PointsFor += scored;
        accumulator.PointsAgainst += allowed;
        if (scored > allowed) accumulator.Wins++;
        else if (scored < allowed) accumulator.Losses++;
        else accumulator.Ties++;
    }

    private static IReadOnlyList<StandingsEntry> RankStandings(IReadOnlyList<TeamStats> stats) =>
        stats
            .OrderByDescending(s => s.WinPct)
            .ThenByDescending(s => s.PointsFor)
            .ThenBy(s => s.TeamName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(s => s.MembershipId)
            .Select((s, i) => new StandingsEntry(
                s.MembershipId, s.TeamName, s.Wins, s.Losses, s.Ties, s.PointsFor, s.PointsAgainst, i + 1))
            .ToList();

    private IReadOnlyList<PowerRankingEntry> RankPowerRankings(IReadOnlyList<TeamStats> stats)
    {
        var maxPointsFor = stats.Count == 0 ? 0m : stats.Max(s => s.PointsFor);

        return stats
            .Select(s => (
                s.MembershipId,
                s.TeamName,
                Score: powerRankingCalculator.Calculate(s.Wins, s.Ties, s.GamesPlayed, s.PointsFor, maxPointsFor)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.TeamName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(x => x.MembershipId)
            .Select((x, i) => new PowerRankingEntry(x.MembershipId, x.TeamName, x.Score, i + 1))
            .ToList();
    }

    private static Dictionary<Guid, decimal> AveragePointsFor(IReadOnlyList<LeagueMembership> memberships, IReadOnlyList<Matchup> matchups, int weeks)
    {
        var totals = memberships.ToDictionary(m => m.Id, _ => 0m);

        foreach (var matchup in matchups)
        {
            if (totals.ContainsKey(matchup.HomeLeagueMembershipId)) totals[matchup.HomeLeagueMembershipId] += matchup.HomeScore;
            if (totals.ContainsKey(matchup.AwayLeagueMembershipId)) totals[matchup.AwayLeagueMembershipId] += matchup.AwayScore;
        }

        return totals.ToDictionary(kv => kv.Key, kv => weeks == 0 ? 0m : kv.Value / weeks);
    }

    private sealed class TeamStatsAccumulator(Guid membershipId, string? teamName)
    {
        public decimal PointsFor;
        public decimal PointsAgainst;
        public int Wins;
        public int Losses;
        public int Ties;

        public TeamStats ToStats() => new(membershipId, teamName, Wins, Losses, Ties, PointsFor, PointsAgainst);
    }

    private sealed record TeamStats(Guid MembershipId, string? TeamName, int Wins, int Losses, int Ties, decimal PointsFor, decimal PointsAgainst)
    {
        public int GamesPlayed => Wins + Losses + Ties;
        public decimal WinPct => GamesPlayed == 0 ? 0m : (Wins + 0.5m * Ties) / GamesPlayed;
    }
}
