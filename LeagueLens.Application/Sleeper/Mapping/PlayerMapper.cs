using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;

namespace LeagueLens.Application.Sleeper.Mapping;

public static class PlayerMapper
{
    // PlayerPosition only covers offensive skill positions (ADR-003/roadmap scope for Phase 2).
    // Sleeper's catalog also includes IDP positions (DL/LB/DB), punters, etc. — those fail to
    // parse here and the caller skips them rather than crashing the whole catalog sync.
    public static bool TryMapPosition(string? sleeperPosition, out PlayerPosition position) =>
        Enum.TryParse(sleeperPosition, ignoreCase: true, out position) && Enum.IsDefined(position);

    public static bool TryMap(string sleeperPlayerId, SleeperPlayerDto dto, out Player player)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) && string.IsNullOrWhiteSpace(dto.LastName))
        {
            player = null!;
            return false;
        }

        if (!TryMapPosition(dto.Position, out var position))
        {
            player = null!;
            return false;
        }

        player = new Player
        {
            Id = Guid.NewGuid(),
            SleeperPlayerId = sleeperPlayerId,
            FirstName = dto.FirstName ?? string.Empty,
            LastName = dto.LastName ?? string.Empty,
            Position = position,
            NflTeam = dto.Team,
        };
        return true;
    }

    public static void MapOnto(SleeperPlayerDto dto, Player existing)
    {
        if (!string.IsNullOrWhiteSpace(dto.FirstName)) existing.FirstName = dto.FirstName;
        if (!string.IsNullOrWhiteSpace(dto.LastName)) existing.LastName = dto.LastName;
        existing.NflTeam = dto.Team;
        if (TryMapPosition(dto.Position, out var position)) existing.Position = position;
    }
}
