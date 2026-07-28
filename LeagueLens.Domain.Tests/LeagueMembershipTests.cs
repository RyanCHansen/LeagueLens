using LeagueLens.Domain.Entities;

namespace LeagueLens.Domain.Tests;

public class LeagueMembershipTests
{
    [Fact]
    public void CanRepresentASleeperManagerWithNoLeagueLensAccount()
    {
        var membership = new LeagueMembership
        {
            Id = Guid.NewGuid(),
            LeagueId = Guid.NewGuid(),
            SleeperUserId = "sleeper-user-123",
            UserProfileId = null
        };

        Assert.Null(membership.UserProfileId);
        Assert.NotNull(membership.SleeperUserId);
    }

    [Fact]
    public void CanLinkToAClaimingUserProfile()
    {
        var userProfileId = Guid.NewGuid();

        var membership = new LeagueMembership
        {
            Id = Guid.NewGuid(),
            LeagueId = Guid.NewGuid(),
            SleeperUserId = "sleeper-user-123",
            UserProfileId = userProfileId
        };

        Assert.Equal(userProfileId, membership.UserProfileId);
    }
}
