using LeagueLens.Application.Sleeper.Client;
using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeagueLens.Application.LeagueIntel;

/// <inheritdoc cref="IWeekPreviewService"/>
public sealed class WeekPreviewService(LeagueLensDbContext db, ISleeperApiClient client) : IWeekPreviewService
{
    public async Task<WeekPreviewResult?> GetWeekPreviewAsync(string sleeperLeagueId, CancellationToken ct)
    {
        var league = await db.Leagues.SingleOrDefaultAsync(l => l.SleeperLeagueId == sleeperLeagueId, ct);
        if (league is null)
            return null;

        var memberships = await db.LeagueMemberships.Where(m => m.LeagueId == league.Id).ToListAsync(ct);
        var membershipIdBySleeperUserId = memberships.ToDictionary(m => m.SleeperUserId, m => m.Id);
        var teamNames = memberships.ToDictionary(m => m.Id, m => m.TeamName);

        var state = await client.GetNflStateAsync(ct);
        var rosters = await client.GetLeagueRostersAsync(sleeperLeagueId, ct);
        var weekMatchups = await client.GetMatchupsAsync(sleeperLeagueId, state.Week, ct);

        var membershipIdByRosterId = rosters
            .Where(r => r.OwnerId is not null && membershipIdBySleeperUserId.ContainsKey(r.OwnerId))
            .ToDictionary(r => r.RosterId, r => membershipIdBySleeperUserId[r.OwnerId!]);

        // Same pairing shape as MatchupMapper (group by shared matchup_id, skip byes/unlinked
        // rosters) but not persistence-oriented, so it isn't literally reused -- there's no
        // Matchup/MatchupParticipant row to build, just a read-time pairing.
        var entries = weekMatchups
            .Where(m => m.MatchupId.HasValue)
            .GroupBy(m => m.MatchupId!.Value)
            .Select(g => g.ToList())
            .Where(pair => pair.Count == 2
                && membershipIdByRosterId.ContainsKey(pair[0].RosterId)
                && membershipIdByRosterId.ContainsKey(pair[1].RosterId))
            .Select(pair => ToPreviewEntry(pair, membershipIdByRosterId, teamNames))
            .OrderBy(e => e.TeamA.TeamName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(e => e.TeamA.LeagueMembershipId)
            .ToList();

        return new WeekPreviewResult(state.Week, entries);
    }

    private static MatchupPreviewEntry ToPreviewEntry(
        IReadOnlyList<SleeperMatchupDto> pair,
        IReadOnlyDictionary<int, Guid> membershipIdByRosterId,
        IReadOnlyDictionary<Guid, string?> teamNames)
    {
        var results = new[]
        {
            ToTeamPreviewInfo(membershipIdByRosterId[pair[0].RosterId], teamNames),
            ToTeamPreviewInfo(membershipIdByRosterId[pair[1].RosterId], teamNames),
        };
        Array.Sort(results, (a, b) =>
        {
            var byName = string.CompareOrdinal(a.TeamName ?? string.Empty, b.TeamName ?? string.Empty);
            return byName != 0 ? byName : a.LeagueMembershipId.CompareTo(b.LeagueMembershipId);
        });

        return new MatchupPreviewEntry(results[0], results[1]);
    }

    private static TeamPreviewInfo ToTeamPreviewInfo(Guid membershipId, IReadOnlyDictionary<Guid, string?> teamNames) =>
        new(membershipId, teamNames.GetValueOrDefault(membershipId));
}
