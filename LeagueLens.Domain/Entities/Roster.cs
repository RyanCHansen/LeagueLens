using LeagueLens.Domain.Enums;

namespace LeagueLens.Domain.Entities;

public class Roster
{
    public Guid Id { get; set; }
    public Guid LeagueMembershipId { get; set; }
    public Guid PlayerId { get; set; }
    public RosterSlot Slot { get; set; }
    public int Season { get; set; }
}
