namespace LeagueLens.Domain.Entities;

public class MatchupParticipant
{
    public Guid Id { get; set; }
    public Guid MatchupId { get; set; }
    public Guid LeagueMembershipId { get; set; }
    public decimal Score { get; set; }
}
