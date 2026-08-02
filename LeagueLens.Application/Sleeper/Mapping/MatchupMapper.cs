using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Domain.Entities;

namespace LeagueLens.Application.Sleeper.Mapping;

public static class MatchupMapper
{
    // Sleeper pairs two roster entries per matchup via a shared matchup_id and never labels
    // either side home/away — order within the pair is arbitrary and doesn't affect standings.
    // A matchup_id with only one entry is a bye week; anything other than exactly two entries,
    // or a roster with no linked membership, is skipped and counted rather than guessed at.
    public static IReadOnlyList<Matchup> Map(
        IReadOnlyList<SleeperMatchupDto> weekMatchups,
        int week,
        Guid leagueId,
        IReadOnlyDictionary<int, Guid> membershipIdByRosterId,
        out int skipped)
    {
        skipped = 0;
        var result = new List<Matchup>();

        foreach (var group in weekMatchups.Where(m => m.MatchupId.HasValue).GroupBy(m => m.MatchupId!.Value))
        {
            var entries = group.ToList();
            if (entries.Count != 2)
            {
                skipped += entries.Count;
                continue;
            }

            if (!membershipIdByRosterId.TryGetValue(entries[0].RosterId, out var homeId) ||
                !membershipIdByRosterId.TryGetValue(entries[1].RosterId, out var awayId))
            {
                skipped += entries.Count;
                continue;
            }

            result.Add(new Matchup
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                Week = week,
                HomeLeagueMembershipId = homeId,
                AwayLeagueMembershipId = awayId,
                HomeScore = (decimal)entries[0].Points,
                AwayScore = (decimal)entries[1].Points,
            });
        }

        return result;
    }
}
