using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;
using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;

namespace LeagueLens.Application.Tests.Sleeper.Mapping;

public class PlayerMapperTests
{
    [Theory]
    [InlineData("QB", PlayerPosition.QB)]
    [InlineData("rb", PlayerPosition.RB)]
    [InlineData("DEF", PlayerPosition.DEF)]
    public void TryMap_MapsSupportedPositions(string sleeperPosition, PlayerPosition expected)
    {
        var dto = new SleeperPlayerDto { FirstName = "John", LastName = "Doe", Position = sleeperPosition, Team = "KC" };

        var mapped = PlayerMapper.TryMap("1001", dto, out var player);

        Assert.True(mapped);
        Assert.Equal(expected, player.Position);
        Assert.Equal("1001", player.SleeperPlayerId);
        Assert.NotEqual(Guid.Empty, player.Id);
    }

    [Theory]
    [InlineData("LB")]
    [InlineData("DL")]
    [InlineData("DB")]
    [InlineData(null)]
    public void TryMap_RejectsUnsupportedOrMissingPosition(string? sleeperPosition)
    {
        var dto = new SleeperPlayerDto { FirstName = "John", LastName = "Doe", Position = sleeperPosition };

        var mapped = PlayerMapper.TryMap("1001", dto, out _);

        Assert.False(mapped);
    }

    [Fact]
    public void TryMap_RejectsEntryWithNoName()
    {
        var dto = new SleeperPlayerDto { Position = "QB" };

        var mapped = PlayerMapper.TryMap("1001", dto, out _);

        Assert.False(mapped);
    }

    [Fact]
    public void MapOnto_UpdatesTeamAndPositionWithoutChangingId()
    {
        var existing = new Player
        {
            Id = Guid.NewGuid(),
            SleeperPlayerId = "1001",
            FirstName = "John",
            LastName = "Doe",
            Position = PlayerPosition.RB,
            NflTeam = "KC",
        };
        var dto = new SleeperPlayerDto { FirstName = "John", LastName = "Doe", Position = "WR", Team = "SF" };
        var originalId = existing.Id;

        PlayerMapper.MapOnto(dto, existing);

        Assert.Equal(originalId, existing.Id);
        Assert.Equal(PlayerPosition.WR, existing.Position);
        Assert.Equal("SF", existing.NflTeam);
    }
}
