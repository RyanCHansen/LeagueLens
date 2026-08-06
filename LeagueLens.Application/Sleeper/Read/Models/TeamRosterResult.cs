using LeagueLens.Domain.Enums;

namespace LeagueLens.Application.Sleeper.Read.Models;

/// <summary>One team's current-season roster. Player identity is embedded per entry so no follow-up per-player call is needed.</summary>
public sealed record TeamRosterResult(
    Guid LeagueMembershipId,
    string SleeperUserId,
    string? TeamName,
    IReadOnlyList<RosterPlayerEntry> Players);

public sealed record RosterPlayerEntry(
    string SleeperPlayerId,
    string FirstName,
    string LastName,
    PlayerPosition Position,
    string? NflTeam,
    RosterSlot Slot);
