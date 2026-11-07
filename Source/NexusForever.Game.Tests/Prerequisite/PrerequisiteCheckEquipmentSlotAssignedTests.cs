using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md, "Type 56"): the evaluator case 0x38 reads Value as a slot index; for an index below 28 the result is whether the
// pointer at unit+0x2d8 + index*0x10 is non-null (otherwise 0), and the shared tail returns that for Equal, its negation for NotEqual and false for any other comparison; no unit gives false.
// Identity of the array as the unit's equipped-item slots (EquippedItem) is inferred from the retail Values (0 to 24, led by 8, 0, 9, 20, 2, 15), not read from the code.
public class PrerequisiteCheckEquipmentSlotAssignedTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 8u, true)]            // System slot holds an item
    [InlineData(PrerequisiteComparison.NotEqual, 8u, false)]
    [InlineData(PrerequisiteComparison.Equal, 9u, false)]           // Augment slot is empty
    [InlineData(PrerequisiteComparison.NotEqual, 9u, true)]
    [InlineData(PrerequisiteComparison.Equal, 20u, true)]           // Bag3
    [InlineData(PrerequisiteComparison.Equal, 27u, false)]          // last valid index, empty
    [InlineData(PrerequisiteComparison.NotEqual, 27u, true)]
    [InlineData(PrerequisiteComparison.Equal, 28u, false)]          // index out of the 28-slot range counts as unassigned
    [InlineData(PrerequisiteComparison.NotEqual, 28u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0xffffffffu, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 0xffffffffu, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 8u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 8u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 9u, false)]
    public void ComparesWhetherTheEquipmentSlotAtValueIsAssigned(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(8u, 20u);

        Assert.Equal(expected, new PrerequisiteCheckEquipmentSlotAssigned().Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void OnlyTheEquippedLocationCounts()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        IItem item = RecordingDispatchProxy<IItem>.Create(out _);
        inventoryProxy.SetMethodHandler(nameof(IInventory.GetItem), (_, args) =>
            args.Length == 2 && args[0] is InventoryLocation.Inventory && (uint)args[1] == 8u ? item : null);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        Assert.False(new PrerequisiteCheckEquipmentSlotAssigned().Meets(player, PrerequisiteComparison.Equal, 8u, 0u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(params uint[] assignedSlots)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        IItem item = RecordingDispatchProxy<IItem>.Create(out _);
        inventoryProxy.SetMethodHandler(nameof(IInventory.GetItem), (_, args) =>
            args.Length == 2 && args[0] is InventoryLocation.Equipped && assignedSlots.Contains((uint)args[1]) ? item : null);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        return player;
    }
}
