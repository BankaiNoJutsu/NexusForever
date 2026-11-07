using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

// Native evaluator (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md): type 19 (FUN_14049f0f0, whose Ghidra "void" signature is a tail call into the shared compare helper)
// looks up the TradeskillTier row with id ObjectId, finds the player's matching tradeskill and compares the player's tier in it with the row's Tier using the comparison; Value is
// unused and a player without that tradeskill compares as tier 0. 267 of 268 retail slots have ObjectId in TradeskillTier (comparisons >= 181, = 66, < 21; Value 0 in 255).
public class PrerequisiteCheckTradeskillTierTests
{
    private static readonly TradeskillTierEntry[] Tiers =
    [
        new() { Id = 10, TradeSkillId = (uint)TradeskillType.Armorer, Tier = 1, RequiredXp = 0 },
        new() { Id = 11, TradeSkillId = (uint)TradeskillType.Armorer, Tier = 2, RequiredXp = 100 },
        new() { Id = 12, TradeSkillId = (uint)TradeskillType.Armorer, Tier = 3, RequiredXp = 500 }
    ];

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 12u, false)] // player tier 2, row tier 3
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 11u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 10u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 12u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 11u, false)]
    [InlineData(PrerequisiteComparison.Equal, 11u, true)]
    [InlineData(PrerequisiteComparison.Equal, 12u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 12u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 10u, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 11u, true)]
    public void ComparesThePlayersTierInTheRowsTradeskillWithTheRowsTier(PrerequisiteComparison comparison, uint rowId, bool expected)
    {
        IPlayer player = Player(hasTradeskill: true, xp: 150u);

        Assert.Equal(expected, Check().Meets(player, comparison, 0u, rowId, new PrerequisiteParameters()));
    }

    [Fact]
    public void ValueIsIgnored()
    {
        IPlayer player = Player(hasTradeskill: true, xp: 150u);

        Assert.True(Check().Meets(player, PrerequisiteComparison.Equal, 99u, 11u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.LessThan, 10u, true)]            // tier 0 < 1
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 10u, false)]
    [InlineData(PrerequisiteComparison.Equal, 10u, false)]
    public void APlayerWithoutTheTradeskillCountsAsTierZero(PrerequisiteComparison comparison, uint rowId, bool expected)
    {
        IPlayer player = Player(hasTradeskill: false, xp: 0u);

        Assert.Equal(expected, Check().Meets(player, comparison, 0u, rowId, new PrerequisiteParameters()));
    }

    [Fact]
    public void AnUnknownRowOrMissingTableDoesNotMeetThePrerequisite()
    {
        IPlayer player = Player(hasTradeskill: true, xp: 150u);

        Assert.False(Check().Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 0u, 999u, new PrerequisiteParameters()));
        Assert.False(new PrerequisiteCheckTradeskillTier(RecordingDispatchProxy<IGameTableManager>.Create(out _))
            .Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 0u, 11u, new PrerequisiteParameters()));
    }

    private static PrerequisiteCheckTradeskillTier Check()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.TradeskillTier), PlayerBindPointTests.Table(Tiers));
        return new PrerequisiteCheckTradeskillTier(tables);
    }

    private static IPlayer Player(bool hasTradeskill, uint xp)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasTradeskill), hasTradeskill);
        playerProxy.SetMethodReturn(nameof(IPlayer.GetTradeskillXp), xp);
        return player;
    }
}
