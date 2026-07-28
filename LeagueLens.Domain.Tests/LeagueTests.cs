using LeagueLens.Domain.Entities;

namespace LeagueLens.Domain.Tests;

public class LeagueTests
{
    [Fact]
    public void CanBeConstructedFromASleeperLeagueId()
    {
        var league = new League
        {
            Id = Guid.NewGuid(),
            SleeperLeagueId = "sleeper-league-123",
            Name = "The League",
            Season = 2026
        };

        Assert.Equal("sleeper-league-123", league.SleeperLeagueId);
        Assert.Equal(2026, league.Season);
    }
}
