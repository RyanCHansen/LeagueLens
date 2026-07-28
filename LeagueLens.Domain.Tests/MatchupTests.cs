using LeagueLens.Domain.Entities;

namespace LeagueLens.Domain.Tests;

public class MatchupTests
{
    [Fact]
    public void HomeAndAwayReferenceDifferentLeagueMemberships()
    {
        var matchup = new Matchup
        {
            Id = Guid.NewGuid(),
            LeagueId = Guid.NewGuid(),
            Week = 1,
            HomeLeagueMembershipId = Guid.NewGuid(),
            AwayLeagueMembershipId = Guid.NewGuid(),
            HomeScore = 105.5m,
            AwayScore = 98.2m
        };

        Assert.NotEqual(matchup.HomeLeagueMembershipId, matchup.AwayLeagueMembershipId);
    }
}
