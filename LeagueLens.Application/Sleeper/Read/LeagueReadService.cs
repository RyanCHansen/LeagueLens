using LeagueLens.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeagueLens.Application.Sleeper.Read;

/// <inheritdoc cref="ILeagueReadService"/>
public sealed class LeagueReadService(LeagueLensDbContext db) : ILeagueReadService
{
    public async Task<LeagueDetailResult?> GetLeagueDetailAsync(string sleeperLeagueId, CancellationToken ct)
    {
        var league = await db.Leagues.SingleOrDefaultAsync(l => l.SleeperLeagueId == sleeperLeagueId, ct);
        if (league is null)
            return null;

        var memberships = await db.LeagueMemberships
            .Where(m => m.LeagueId == league.Id)
            .ToListAsync(ct);

        var summaries = memberships
            .OrderBy(m => m.TeamName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(m => m.SleeperUserId, StringComparer.Ordinal)
            .Select(m => new LeagueMembershipSummary(m.Id, m.SleeperUserId, m.TeamName))
            .ToList();

        return new LeagueDetailResult(league.SleeperLeagueId, league.Name, league.Season, summaries);
    }

    public async Task<IReadOnlyList<TeamRosterResult>?> GetRostersAsync(string sleeperLeagueId, CancellationToken ct)
    {
        var league = await db.Leagues.SingleOrDefaultAsync(l => l.SleeperLeagueId == sleeperLeagueId, ct);
        if (league is null)
            return null;

        var memberships = await db.LeagueMemberships
            .Where(m => m.LeagueId == league.Id)
            .ToListAsync(ct);
        var membershipIds = memberships.Select(m => m.Id).ToList();

        // Current season only -- Roster is a replace-on-sync snapshot of current state, never a
        // history (see RosterSyncer), but nothing stops an older season's rows lingering if a
        // league's season value ever changed, so this filter is a correctness guard, not just belt-and-suspenders.
        var rosterRows = await db.Rosters
            .Where(r => membershipIds.Contains(r.LeagueMembershipId) && r.Season == league.Season)
            .ToListAsync(ct);

        var playerIds = rosterRows.Select(r => r.PlayerId).Distinct().ToList();
        var playersById = await db.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var rosterRowsByMembership = rosterRows.ToLookup(r => r.LeagueMembershipId);

        return memberships
            .OrderBy(m => m.TeamName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(m => m.SleeperUserId, StringComparer.Ordinal)
            .Select(m =>
            {
                var players = rosterRowsByMembership[m.Id]
                    // Defensive: Roster.PlayerId has a Restrict FK to Player (ADR-003), so this should
                    // never actually miss -- guards against a lookup gap rather than throwing on one.
                    .Where(r => playersById.ContainsKey(r.PlayerId))
                    .OrderBy(r => r.Slot)
                    .ThenBy(r => playersById[r.PlayerId].LastName, StringComparer.Ordinal)
                    .Select(r =>
                    {
                        var player = playersById[r.PlayerId];
                        return new RosterPlayerEntry(
                            player.SleeperPlayerId, player.FirstName, player.LastName,
                            player.Position, player.NflTeam, r.Slot);
                    })
                    .ToList();

                return new TeamRosterResult(m.Id, m.SleeperUserId, m.TeamName, players);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<MatchupResult>?> GetMatchupsAsync(string sleeperLeagueId, CancellationToken ct)
    {
        var league = await db.Leagues.SingleOrDefaultAsync(l => l.SleeperLeagueId == sleeperLeagueId, ct);
        if (league is null)
            return null;

        var matchups = await db.Matchups
            .Where(m => m.LeagueId == league.Id)
            .ToListAsync(ct);
        var matchupIds = matchups.Select(m => m.Id).ToList();

        var participants = await db.MatchupParticipants
            .Where(p => matchupIds.Contains(p.MatchupId))
            .ToListAsync(ct);

        var membershipIds = participants.Select(p => p.LeagueMembershipId).Distinct().ToList();
        var teamNameByMembership = await db.LeagueMemberships
            .Where(m => membershipIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.TeamName, ct);

        var participantsByMatchup = participants.ToLookup(p => p.MatchupId);

        var results = new List<MatchupResult>();
        foreach (var matchup in matchups.OrderBy(m => m.Week))
        {
            // Same stable-sort convention as WeekPreviewService.ToPreviewEntry -- TeamA/TeamB are
            // neutral slots, not a real distinction (ADR-007), so pick an order deterministically.
            var pair = participantsByMatchup[matchup.Id]
                .Select(p => new MatchupTeamResult(
                    p.LeagueMembershipId, teamNameByMembership.GetValueOrDefault(p.LeagueMembershipId), p.Score))
                .OrderBy(t => t.TeamName ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(t => t.LeagueMembershipId)
                .ToList();

            // A synced matchup always has exactly 2 participants (MatchupMapper skips anything
            // else); guard rather than assume in case of an ever-malformed row.
            if (pair.Count == 2)
                results.Add(new MatchupResult(matchup.Week, pair[0], pair[1]));
        }

        return results;
    }
}
