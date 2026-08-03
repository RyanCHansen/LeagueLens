using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Domain.Entities;

namespace LeagueLens.Application.Sleeper.Mapping;

public static class MatchupMapper
{
    // Sleeper pairs two roster entries per matchup via a shared matchup_id and never labels
    // either side -- there's no home/away in the source data, so we don't invent one here
    // either (see ADR-007). A matchup_id with only one entry is a bye week; anything other
    // than exactly two entries, or a roster with no linked membership, is skipped and counted
    // rather than guessed at.
    public static MatchupMappingResult Map(
        IReadOnlyList<SleeperMatchupDto> weekMatchups,
        int week,
        Guid leagueId,
        IReadOnlyDictionary<int, Guid> membershipIdByRosterId,
        out int skipped)
    {
        skipped = 0;
        var matchups = new List<Matchup>();
        var participants = new List<MatchupParticipant>();

        foreach (var group in weekMatchups.Where(m => m.MatchupId.HasValue).GroupBy(m => m.MatchupId!.Value))
        {
            var entries = group.ToList();
            if (entries.Count != 2)
            {
                skipped += entries.Count;
                continue;
            }

            if (!membershipIdByRosterId.TryGetValue(entries[0].RosterId, out var firstMembershipId) ||
                !membershipIdByRosterId.TryGetValue(entries[1].RosterId, out var secondMembershipId))
            {
                skipped += entries.Count;
                continue;
            }

            var matchupId = Guid.NewGuid();
            matchups.Add(new Matchup { Id = matchupId, LeagueId = leagueId, Week = week });
            participants.Add(new MatchupParticipant { Id = Guid.NewGuid(), MatchupId = matchupId, LeagueMembershipId = firstMembershipId, Score = (decimal)entries[0].Points });
            participants.Add(new MatchupParticipant { Id = Guid.NewGuid(), MatchupId = matchupId, LeagueMembershipId = secondMembershipId, Score = (decimal)entries[1].Points });
        }

        return new MatchupMappingResult(matchups, participants);
    }
}
