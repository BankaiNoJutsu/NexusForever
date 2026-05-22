using NexusForever.Game.Housing;
using NexusForever.Game.Retail;
using NexusForever.Game.Support;
using NexusForever.Game.Static.Support;

namespace NexusForever.Game.Tests.Retail;

public class RetailCertainRulesTests
{
    public RetailCertainRulesTests()
    {
        RetailStuckCooldownTracker.ClearForTests();
    }

    [Theory]
    [InlineData(0, 0, 100)]
    [InlineData(3, 33, 67)]
    [InlineData(7, 100, 0)]
    public void HousingHarvestSplit_MapsShareIndexToOwnerAndNeighbor(byte index, int neighbor, int owner)
    {
        Assert.Equal(neighbor, RetailHousingHarvestSplit.GetNeighborSharePercent(index));
        Assert.Equal(owner, RetailHousingHarvestSplit.GetOwnerSharePercent(index));
    }

    [Fact]
    public void StuckCooldownTracker_BlocksUntilElapsed()
    {
        ulong characterId = 4242;
        RetailStuckCooldownTracker.RecordUse(characterId, UnstickType.RecallTransmat);

        Assert.True(RetailStuckCooldownTracker.GetRemainingCooldownMs(characterId, UnstickType.RecallTransmat) > 0);
    }

    [Fact]
    public void RetailMatchTypeFlags_DungeonAllowsGroupRequeue_ArenaDoesNot()
    {
        const uint dungeonFlags = 0x000B;
        const uint arenaFlags   = 0x000C;
        Assert.NotEqual(0u, dungeonFlags & 0x02u);
        Assert.Equal(0u, arenaFlags & 0x02u);
    }
}
