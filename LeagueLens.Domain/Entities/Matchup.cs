namespace LeagueLens.Domain.Entities;

public class Matchup
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public int Week { get; set; }
}
