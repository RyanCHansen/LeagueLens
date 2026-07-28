using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;

namespace LeagueLens.Domain.Tests;

public class PlayerTests
{
    [Fact]
    public void CanRepresentAFreeAgentWithNoNflTeam()
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            SleeperPlayerId = "sleeper-player-123",
            FirstName = "Free",
            LastName = "Agent",
            Position = PlayerPosition.WR,
            NflTeam = null
        };

        Assert.Null(player.NflTeam);
    }
}
