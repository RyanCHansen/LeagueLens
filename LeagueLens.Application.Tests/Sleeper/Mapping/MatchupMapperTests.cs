using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;

namespace LeagueLens.Application.Tests.Sleeper.Mapping;

public class MatchupMapperTests
{
    [Fact]
    public void Map_PairsEntriesSharingAMatchupId()
    {
        var firstMembershipId = Guid.NewGuid();
        var secondMembershipId = Guid.NewGuid();
        var membershipIdByRosterId = new Dictionary<int, Guid> { [1] = firstMembershipId, [2] = secondMembershipId };
        var weekMatchups = new List<SleeperMatchupDto>
        {
            new() { RosterId = 1, MatchupId = 100, Points = 110.5 },
            new() { RosterId = 2, MatchupId = 100, Points = 98.25 },
        };

        var result = MatchupMapper.Map(weekMatchups, week: 3, Guid.NewGuid(), membershipIdByRosterId, out var skipped);

        Assert.Equal(0, skipped);
        var matchup = Assert.Single(result.Matchups);
        Assert.Equal(3, matchup.Week);
        Assert.Equal(2, result.Participants.Count);
        Assert.All(result.Participants, p => Assert.Equal(matchup.Id, p.MatchupId));

        var first = result.Participants.Single(p => p.LeagueMembershipId == firstMembershipId);
        Assert.Equal(110.5m, first.Score);
        var second = result.Participants.Single(p => p.LeagueMembershipId == secondMembershipId);
        Assert.Equal(98.25m, second.Score);
    }

    [Fact]
    public void Map_SkipsByeWeekEntryWithNoOpponent()
    {
        var membershipIdByRosterId = new Dictionary<int, Guid> { [1] = Guid.NewGuid() };
        var weekMatchups = new List<SleeperMatchupDto> { new() { RosterId = 1, MatchupId = 100, Points = 110.5 } };

        var result = MatchupMapper.Map(weekMatchups, week: 3, Guid.NewGuid(), membershipIdByRosterId, out var skipped);

        Assert.Empty(result.Matchups);
        Assert.Empty(result.Participants);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void Map_SkipsPairWhenARosterHasNoLinkedMembership()
    {
        var membershipIdByRosterId = new Dictionary<int, Guid> { [1] = Guid.NewGuid() };
        var weekMatchups = new List<SleeperMatchupDto>
        {
            new() { RosterId = 1, MatchupId = 100, Points = 110.5 },
            new() { RosterId = 2, MatchupId = 100, Points = 98.25 }, // roster 2 has no owner linked
        };

        var result = MatchupMapper.Map(weekMatchups, week: 3, Guid.NewGuid(), membershipIdByRosterId, out var skipped);

        Assert.Empty(result.Matchups);
        Assert.Empty(result.Participants);
        Assert.Equal(2, skipped);
    }
}
