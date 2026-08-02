using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;
using LeagueLens.Domain.Enums;

namespace LeagueLens.Application.Tests.Sleeper.Mapping;

public class RosterMapperTests
{
    private static readonly List<string> StandardRosterPositions =
        ["QB", "RB", "RB", "WR", "WR", "TE", "FLEX", "DEF", "K", "BN", "BN", "BN", "BN"];

    [Fact]
    public void MapSlots_ZipsStartersAgainstNonBenchPositionsInOrder()
    {
        var roster = new SleeperRosterDto
        {
            RosterId = 1,
            OwnerId = "u1",
            Starters = ["p_qb", "p_rb1", "p_rb2", "p_wr1", "p_wr2", "p_te", "p_flex", "p_def", "p_k"],
            Players = ["p_qb", "p_rb1", "p_rb2", "p_wr1", "p_wr2", "p_te", "p_flex", "p_def", "p_k", "p_bench1", "p_bench2"],
            Reserve = ["p_ir1"],
            Taxi = ["p_taxi1"],
        };

        var slots = RosterMapper.MapSlots(roster, StandardRosterPositions).ToDictionary(s => s.SleeperPlayerId, s => s.Slot);

        Assert.Equal(RosterSlot.QB, slots["p_qb"]);
        Assert.Equal(RosterSlot.RB, slots["p_rb1"]);
        Assert.Equal(RosterSlot.RB, slots["p_rb2"]);
        Assert.Equal(RosterSlot.WR, slots["p_wr1"]);
        Assert.Equal(RosterSlot.Flex, slots["p_flex"]);
        Assert.Equal(RosterSlot.DefenseSpecialTeams, slots["p_def"]);
        Assert.Equal(RosterSlot.Kicker, slots["p_k"]);
        Assert.Equal(RosterSlot.InjuredReserve, slots["p_ir1"]);
        Assert.Equal(RosterSlot.Taxi, slots["p_taxi1"]);
        Assert.Equal(RosterSlot.Bench, slots["p_bench1"]);
        Assert.Equal(RosterSlot.Bench, slots["p_bench2"]);
        Assert.Equal(13, slots.Count);
    }

    [Fact]
    public void MapSlots_IgnoresEmptyStartingSlots()
    {
        var roster = new SleeperRosterDto
        {
            RosterId = 1,
            OwnerId = "u1",
            Starters = ["p_qb", "0", "p_rb2"],
            Players = ["p_qb", "p_rb2"],
        };
        var positions = new List<string> { "QB", "RB", "RB" };

        var slots = RosterMapper.MapSlots(roster, positions);

        Assert.Equal(2, slots.Count);
        Assert.DoesNotContain(slots, s => s.SleeperPlayerId == "0");
    }
}
