using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;
using LeagueLens.Domain.Entities;

namespace LeagueLens.Application.Tests.Sleeper.Mapping;

public class LeagueMembershipMapperTests
{
    [Fact]
    public void Map_CreatesMembershipLinkedToLeague()
    {
        var leagueId = Guid.NewGuid();
        var dto = new SleeperUserDto
        {
            UserId = "u1",
            DisplayName = "Alice",
            Metadata = new SleeperUserMetadataDto { TeamName = "Alice's Aces" },
        };

        var membership = LeagueMembershipMapper.Map(dto, leagueId);

        Assert.NotEqual(Guid.Empty, membership.Id);
        Assert.Equal(leagueId, membership.LeagueId);
        Assert.Equal("u1", membership.SleeperUserId);
        Assert.Equal("Alice's Aces", membership.TeamName);
        Assert.Null(membership.UserProfileId);
    }

    [Fact]
    public void MapOnto_UpdatesTeamNameOnly()
    {
        var existing = new LeagueMembership { Id = Guid.NewGuid(), LeagueId = Guid.NewGuid(), SleeperUserId = "u1", TeamName = "Old Name" };
        var dto = new SleeperUserDto { UserId = "u1", Metadata = new SleeperUserMetadataDto { TeamName = "New Name" } };
        var originalId = existing.Id;

        LeagueMembershipMapper.MapOnto(dto, existing);

        Assert.Equal(originalId, existing.Id);
        Assert.Equal("New Name", existing.TeamName);
    }
}
