using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;
using LeagueLens.Domain.Entities;

namespace LeagueLens.Application.Tests.Sleeper.Mapping;

public class LeagueMapperTests
{
    [Fact]
    public void Map_CreatesLeagueWithGeneratedId()
    {
        var dto = new SleeperLeagueDto { LeagueId = "123", Name = "Dynasty League", Season = "2026" };

        var league = LeagueMapper.Map(dto);

        Assert.NotEqual(Guid.Empty, league.Id);
        Assert.Equal("123", league.SleeperLeagueId);
        Assert.Equal("Dynasty League", league.Name);
        Assert.Equal(2026, league.Season);
    }

    [Fact]
    public void MapOnto_UpdatesNameAndSeasonWithoutChangingId()
    {
        var existing = new League { Id = Guid.NewGuid(), SleeperLeagueId = "123", Name = "Old Name", Season = 2025 };
        var dto = new SleeperLeagueDto { LeagueId = "123", Name = "New Name", Season = "2026" };
        var originalId = existing.Id;

        LeagueMapper.MapOnto(dto, existing);

        Assert.Equal(originalId, existing.Id);
        Assert.Equal("New Name", existing.Name);
        Assert.Equal(2026, existing.Season);
    }
}
