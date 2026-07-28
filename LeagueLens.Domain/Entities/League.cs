namespace LeagueLens.Domain.Entities;

public class League
{
    public Guid Id { get; set; }
    public required string SleeperLeagueId { get; set; }
    public required string Name { get; set; }
    public int Season { get; set; }
}
