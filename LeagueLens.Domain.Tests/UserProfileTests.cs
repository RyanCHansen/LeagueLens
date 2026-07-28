using LeagueLens.Domain.Entities;

namespace LeagueLens.Domain.Tests;

public class UserProfileTests
{
    [Fact]
    public void DoesNotInheritFromAnyIdentityFrameworkType()
    {
        Assert.Equal(typeof(object), typeof(UserProfile).BaseType);
    }

    [Fact]
    public void CanRepresentAUserWhoHasNotLinkedASleeperAccountYet()
    {
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            IdentityUserId = Guid.NewGuid(),
            DisplayName = "Ryan",
            SleeperUserId = null
        };

        Assert.Null(profile.SleeperUserId);
    }
}
