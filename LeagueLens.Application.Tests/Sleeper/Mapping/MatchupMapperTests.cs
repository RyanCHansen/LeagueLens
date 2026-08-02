using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;

namespace LeagueLens.Application.Tests.Sleeper.Mapping;

public class MatchupMapperTests
{
    [Fact]
    public void Map_PairsEntriesSharingAMatchupId()
    {
        var homeMembershipId = Guid.NewGuid();
        var awayMembershipId = Guid.NewGuid();
        var membershipIdByRosterId = new Dictionary<int, Guid> { [1] = homeMembershipId, [2] = awayMembershipId };
        var weekMatchups = new List<SleeperMatchupDto>
        {
            new() { RosterId = 1, MatchupId = 100, Points = 110.5 },
            new() { RosterId = 2, MatchupId = 100, Points = 98.25 },
        };

        var matchups = MatchupMapper.Map(weekMatchups, week: 3, Guid.NewGuid(), membershipIdByRosterId, out var skipped);

        Assert.Equal(0, skipped);
        var matchup = Assert.Single(matchups);
        Assert.Equal(3, matchup.Week);
        Assert.Equal(homeMembershipId, matchup.HomeLeagueMembershipId);
        Assert.Equal(awayMembershipId, matchup.AwayLeagueMembershipId);
        Assert.Equal(110.5m, matchup.HomeScore);
        Assert.Equal(98.25m, matchup.AwayScore);
    }

    [Fact]
    public void Map_SkipsByeWeekEntryWithNoOpponent()
    {
        var membershipIdByRosterId = new Dictionary<int, Guid> { [1] = Guid.NewGuid() };
        var weekMatchups = new List<SleeperMatchupDto> { new() { RosterId = 1, MatchupId = 100, Points = 110.5 } };

        var matchups = MatchupMapper.Map(weekMatchups, week: 3, Guid.NewGuid(), membershipIdByRosterId, out var skipped);

        Assert.Empty(matchups);
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

        var matchups = MatchupMapper.Map(weekMatchups, week: 3, Guid.NewGuid(), membershipIdByRosterId, out var skipped);

        Assert.Empty(matchups);
        Assert.Equal(2, skipped);
    }
}
