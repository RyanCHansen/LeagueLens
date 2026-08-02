using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Domain.Entities;

namespace LeagueLens.Application.Sleeper.Mapping;

public static class LeagueMapper
{
    public static League Map(SleeperLeagueDto dto) => new()
    {
        Id = Guid.NewGuid(),
        SleeperLeagueId = dto.LeagueId,
        Name = dto.Name,
        Season = int.Parse(dto.Season),
    };

    public static void MapOnto(SleeperLeagueDto dto, League existing)
    {
        existing.Name = dto.Name;
        existing.Season = int.Parse(dto.Season);
    }
}
