using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Static;

namespace NexusForever.Game.Tests.Prerequisite;

// Prerequisite type 146 ItemTradeSkillLevel (agent-written native read, Decomp/Analysis/PREREQUISITE_146_NATIVE_READ_20261108.md; evaluator FUN_1404a0f90): the player is not involved. The item under
// evaluation gives a level v = (Item2.Flags & 1) ? PowerLevel : (RequiredLevel != 0 ? RequiredLevel : 1) (a Depreciated 0x4000 item takes Flags and RequiredLevel from Item2 74201). The tradeskill of the
// TradeskillTier row named by ObjectId has thresholds thr[Tier - 1] = ItemLevelMin for Tier 1 to 10 (missing tiers 0) and count = the highest tier present. The band index is the highest i with
// v >= thr[i - 1] (1 when below the first threshold or count is 0). The result compares that index with the row's Tier using the slot's comparison; Value is not read, and a missing item or row is false
// for every comparison.
public class ItemTradeSkillLevelPrerequisiteTests
{
    private const uint TradeskillId = 3u;
    private const uint SubstituteItemId = 74201u;

    [Theory]
    [InlineData(0u, 1u)]
    [InlineData(9u, 1u)]
    [InlineData(10u, 1u)]
    [InlineData(19u, 1u)]
    [InlineData(20u, 2u)]
    [InlineData(29u, 2u)]
    [InlineData(30u, 3u)]
    [InlineData(500u, 3u)]
    public void TheBandIndexUsesInclusiveLowerBounds(uint requiredLevel, uint expectedIndex)
    {
        var check = Check(Tiers(TradeskillId, (1, 10), (2, 20), (3, 30)));
        uint tierRowId = TierRowId(TradeskillId, expectedIndex);

        Assert.True(check.Meets(Player(), PrerequisiteComparison.Equal, 0u, tierRowId, Parameters(Item(flags: 0, requiredLevel: requiredLevel == 0u ? 1u : requiredLevel))));
    }

    [Fact]
    public void ARequiredLevelOfZeroCountsAsOne()
    {
        var check = Check(Tiers(TradeskillId, (1, 0), (2, 20)));

        Assert.True(check.Meets(Player(), PrerequisiteComparison.Equal, 0u, TierRowId(TradeskillId, 1u), Parameters(Item(flags: 0, requiredLevel: 0u))));
    }

    [Fact]
    public void FlagBitZeroSwitchesToThePowerLevel()
    {
        var check = Check(Tiers(TradeskillId, (1, 10), (2, 20), (3, 30)));
        IItem item = Item(flags: 1, requiredLevel: 5u, powerLevel: 35u);

        Assert.True(check.Meets(Player(), PrerequisiteComparison.Equal, 0u, TierRowId(TradeskillId, 3u), Parameters(item)));
    }

    [Fact]
    public void ADepreciatedItemTakesItsFlagsAndRequiredLevelFromTheSubstituteItem()
    {
        // substitute item 74201 has flag bit 0 set, so the item's own PowerLevel decides
        var check = Check(Tiers(TradeskillId, (1, 10), (2, 20), (3, 30)),
            new Item2Entry { Id = SubstituteItemId, Flags = (ItemFlags)0x101, RequiredLevel = 1u });
        IItem item = Item(flags: 0x4000, requiredLevel: 5u, powerLevel: 22u);

        Assert.True(check.Meets(Player(), PrerequisiteComparison.Equal, 0u, TierRowId(TradeskillId, 2u), Parameters(item)));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 2u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 3u, false)]
    [InlineData(PrerequisiteComparison.Equal, 2u, true)]
    [InlineData(PrerequisiteComparison.Equal, 3u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 3u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 3u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 1u, true)]
    public void TheIndexIsComparedWithTheRowTier(PrerequisiteComparison comparison, uint rowTier, bool expected)
    {
        var check = Check(Tiers(TradeskillId, (1, 10), (2, 20), (3, 30)));
        IItem item = Item(flags: 0, requiredLevel: 25u); // band index 2

        Assert.Equal(expected, check.Meets(Player(), comparison, 0u, TierRowId(TradeskillId, rowTier), Parameters(item)));
    }

    [Fact]
    public void ATradeskillWithAllZeroThresholdsAlwaysReachesItsHighestTier()
    {
        var check = Check(Tiers(TradeskillId, (1, 0), (2, 0), (3, 0)));

        Assert.True(check.Meets(Player(), PrerequisiteComparison.Equal, 0u, TierRowId(TradeskillId, 3u), Parameters(Item(flags: 0, requiredLevel: 1u))));
        Assert.True(check.Meets(Player(), PrerequisiteComparison.GreaterThanOrEqual, 0u, TierRowId(TradeskillId, 3u), Parameters(Item(flags: 0, requiredLevel: 1u))));
    }

    [Fact]
    public void AGapInTheTiersLeavesZeroThresholdsSoLowItemsLandOnTheTopOfTheGap()
    {
        // retail fishing: tiers 1 (0) and 7 (45); tiers 2 to 6 are missing and count as threshold 0
        var check = Check(Tiers(TradeskillId, (1, 0), (7, 45)));

        Assert.False(check.Meets(Player(), PrerequisiteComparison.Equal, 0u, TierRowId(TradeskillId, 7u), Parameters(Item(flags: 0, requiredLevel: 10u))));
        Assert.True(check.Meets(Player(), PrerequisiteComparison.LessThan, 0u, TierRowId(TradeskillId, 7u), Parameters(Item(flags: 0, requiredLevel: 10u))));
        Assert.True(check.Meets(Player(), PrerequisiteComparison.Equal, 0u, TierRowId(TradeskillId, 7u), Parameters(Item(flags: 0, requiredLevel: 45u))));
    }

    [Fact]
    public void TiersAboveTenDoNotCountTowardsTheThresholds()
    {
        var check = Check(Tiers(TradeskillId, (1, 10), (11, 5)));

        Assert.True(check.Meets(Player(), PrerequisiteComparison.Equal, 0u, TierRowId(TradeskillId, 1u), Parameters(Item(flags: 0, requiredLevel: 50u))));
    }

    [Fact]
    public void ValueIsNotRead()
    {
        var check = Check(Tiers(TradeskillId, (1, 10), (2, 20)));
        IItem item = Item(flags: 0, requiredLevel: 25u);

        Assert.True(check.Meets(Player(), PrerequisiteComparison.Equal, 10u, TierRowId(TradeskillId, 2u), Parameters(item)));
        Assert.True(check.Meets(Player(), PrerequisiteComparison.Equal, 0u, TierRowId(TradeskillId, 2u), Parameters(item)));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal)]
    [InlineData(PrerequisiteComparison.NotEqual)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual)]
    public void AMissingItemIsFalseForEveryComparison(PrerequisiteComparison comparison)
    {
        var check = Check(Tiers(TradeskillId, (1, 10)));

        Assert.False(check.Meets(Player(), comparison, 0u, TierRowId(TradeskillId, 1u), new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal)]
    [InlineData(PrerequisiteComparison.NotEqual)]
    public void AMissingTierRowIsFalseForEveryComparison(PrerequisiteComparison comparison)
    {
        var check = Check(Tiers(TradeskillId, (1, 10)));

        Assert.False(check.Meets(Player(), comparison, 0u, 999999u, Parameters(Item(flags: 0, requiredLevel: 5u))));
    }

    private static uint TierRowId(uint tradeskill, uint tier) => tradeskill * 100u + tier;

    private static TradeskillTierEntry[] Tiers(uint tradeskill, params (uint Tier, uint ItemLevelMin)[] tiers)
        => tiers.Select(t => new TradeskillTierEntry { Id = TierRowId(tradeskill, t.Tier), TradeSkillId = tradeskill, Tier = t.Tier, ItemLevelMin = t.ItemLevelMin }).ToArray();

    private static PrerequisiteCheckItemTradeSkillLevel Check(TradeskillTierEntry[] tiers, params Item2Entry[] items)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.TradeskillTier), PlayerBindPointTests.Table(tiers));
        proxy.SetProperty(nameof(IGameTableManager.Item), PlayerBindPointTests.Table(items));
        return new PrerequisiteCheckItemTradeSkillLevel(tables);
    }

    private static IItem Item(uint flags, uint requiredLevel, uint powerLevel = 0u)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 1u, Flags = (ItemFlags)flags, RequiredLevel = requiredLevel, PowerLevel = powerLevel });
        itemProxy.SetProperty(nameof(IItem.Info), info);
        return item;
    }

    private static PrerequisiteParameters Parameters(IItem item) => new() { Item = item };

    private static IPlayer Player() => RecordingDispatchProxy<IPlayer>.Create(out _);
}
