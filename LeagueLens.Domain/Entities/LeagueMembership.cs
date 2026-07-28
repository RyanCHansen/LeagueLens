namespace LeagueLens.Domain.Entities;

public class LeagueMembership
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public Guid? UserProfileId { get; set; }
    public required string SleeperUserId { get; set; }
    public string? TeamName { get; set; }
}
