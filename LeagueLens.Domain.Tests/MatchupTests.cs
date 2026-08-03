using LeagueLens.Domain.Entities;

namespace LeagueLens.Domain.Tests;

public class MatchupTests
{
    [Fact]
    public void ParticipantsReferenceDifferentLeagueMembershipsOfTheSameMatchup()
    {
        var matchupId = Guid.NewGuid();
        var first = new MatchupParticipant
        {
            Id = Guid.NewGuid(),
            MatchupId = matchupId,
            LeagueMembershipId = Guid.NewGuid(),
            Score = 105.5m,
        };
        var second = new MatchupParticipant
        {
            Id = Guid.NewGuid(),
            MatchupId = matchupId,
            LeagueMembershipId = Guid.NewGuid(),
            Score = 98.2m,
        };

        Assert.Equal(first.MatchupId, second.MatchupId);
        Assert.NotEqual(first.LeagueMembershipId, second.LeagueMembershipId);
    }
}
