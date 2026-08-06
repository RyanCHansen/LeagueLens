using LeagueLens.Domain.Enums;

namespace LeagueLens.Application.Sleeper.Read.Models;

public sealed record PlayerDetailResult(
    string SleeperPlayerId,
    string FirstName,
    string LastName,
    PlayerPosition Position,
    string? NflTeam);
