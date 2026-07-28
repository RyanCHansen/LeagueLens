namespace LeagueLens.Domain.Entities;

public class Matchup
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public int Week { get; set; }
    public Guid HomeLeagueMembershipId { get; set; }
    public Guid AwayLeagueMembershipId { get; set; }
    public decimal HomeScore { get; set; }
    public decimal AwayScore { get; set; }
}
