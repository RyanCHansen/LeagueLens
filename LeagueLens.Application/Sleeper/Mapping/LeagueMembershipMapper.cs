using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Domain.Entities;

namespace LeagueLens.Application.Sleeper.Mapping;

public static class LeagueMembershipMapper
{
    public static LeagueMembership Map(SleeperUserDto dto, Guid leagueId) => new()
    {
        Id = Guid.NewGuid(),
        LeagueId = leagueId,
        SleeperUserId = dto.UserId,
        TeamName = dto.Metadata?.TeamName,
    };

    public static void MapOnto(SleeperUserDto dto, LeagueMembership existing)
    {
        existing.TeamName = dto.Metadata?.TeamName;
    }
}
