using LeagueLens.Domain.Enums;

namespace LeagueLens.Domain.Entities;

public class Player
{
    public Guid Id { get; set; }
    public required string SleeperPlayerId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public PlayerPosition Position { get; set; }
    public string? NflTeam { get; set; }
}
