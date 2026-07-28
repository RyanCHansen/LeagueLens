namespace LeagueLens.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; set; }
    public Guid IdentityUserId { get; set; }
    public string? SleeperUserId { get; set; }
    public required string DisplayName { get; set; }
}
