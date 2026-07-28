using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;

namespace LeagueLens.Domain.Tests;

public class RosterTests
{
    [Fact]
    public void OneRowRepresentsOnePlayerInOneSlot()
    {
        var leagueMembershipId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        var rosterSpot = new Roster
        {
            Id = Guid.NewGuid(),
            LeagueMembershipId = leagueMembershipId,
            PlayerId = playerId,
            Slot = RosterSlot.Bench,
            Season = 2026
        };

        Assert.Equal(leagueMembershipId, rosterSpot.LeagueMembershipId);
        Assert.Equal(playerId, rosterSpot.PlayerId);
        Assert.Equal(RosterSlot.Bench, rosterSpot.Slot);
    }
}
