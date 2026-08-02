using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Domain.Enums;

namespace LeagueLens.Application.Sleeper.Mapping;

public static class RosterMapper
{
    private static readonly Dictionary<string, RosterSlot> StartingSlotBySleeperCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["QB"] = RosterSlot.QB,
        ["RB"] = RosterSlot.RB,
        ["WR"] = RosterSlot.WR,
        ["TE"] = RosterSlot.TE,
        ["FLEX"] = RosterSlot.Flex,
        ["SUPER_FLEX"] = RosterSlot.SuperFlex,
        ["DEF"] = RosterSlot.DefenseSpecialTeams,
        ["K"] = RosterSlot.Kicker,
    };

    // Sleeper's roster.starters is positional, aligned 1:1 with the league's roster_positions
    // once bench slots are filtered out. IDP starting slots (DL/LB/DB) have no RosterSlot
    // counterpart yet and are silently excluded, same as PlayerMapper's position handling.
    public static IReadOnlyList<(string SleeperPlayerId, RosterSlot Slot)> MapSlots(
        SleeperRosterDto roster, IReadOnlyList<string> leagueRosterPositions)
    {
        var result = new List<(string, RosterSlot)>();
        var starters = roster.Starters ?? [];
        var startingPositions = leagueRosterPositions
            .Where(p => !string.Equals(p, "BN", StringComparison.OrdinalIgnoreCase))
            .ToList();

        for (var i = 0; i < starters.Count && i < startingPositions.Count; i++)
        {
            var sleeperPlayerId = starters[i];
            if (string.IsNullOrEmpty(sleeperPlayerId) || sleeperPlayerId == "0")
                continue; // empty starting slot

            if (StartingSlotBySleeperCode.TryGetValue(startingPositions[i], out var slot))
                result.Add((sleeperPlayerId, slot));
        }

        foreach (var sleeperPlayerId in roster.Reserve ?? [])
            result.Add((sleeperPlayerId, RosterSlot.InjuredReserve));

        foreach (var sleeperPlayerId in roster.Taxi ?? [])
            result.Add((sleeperPlayerId, RosterSlot.Taxi));

        var accountedFor = result.Select(r => r.Item1).ToHashSet();
        foreach (var sleeperPlayerId in (roster.Players ?? []).Where(p => !accountedFor.Contains(p)))
            result.Add((sleeperPlayerId, RosterSlot.Bench));

        return result;
    }
}
