using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;

namespace NexusForever.Game.Tests.Pet;

public class PetMiniBarManagerTests
{
    [Fact]
    public void ActualDismissRowUsesWireSlotZeroAndDismissesOnlyBoundPetOnce()
    {
        var f = new PetMiniBarTestFixture();
        var first = f.Pet(); var second = f.Pet(101, 42684);
        Assert.True(f.Manager.TryBind(first)); Assert.True(f.Manager.TryBind(second));
        Assert.False(f.Manager.TryDismiss(ShortcutSet.PetMiniBar0, 2)); // command object is not slot
        Assert.True(f.Manager.TryDismiss(ShortcutSet.PetMiniBar0, 0));
        Assert.False(f.Manager.TryDismiss(ShortcutSet.PetMiniBar0, 0));
        Assert.Equal(new uint[] { 100 }, f.Removals);
        Assert.Equal(0, f.Bars[^1].ActionBarShortcutSetId);
        Assert.Equal(0u, f.Bars[^1].AssociatedUnitId);
        Assert.Same(second, Assert.Single(f.Factory.GetSummonCreatures(42684)));
        f.Manager.Unbind(first);
        Assert.Equal(3, f.Bars.Length); // no second clear on callback
    }

    [Fact]
    public void StableAllocationDuplicateCapacityAndLatestBindingContract()
    {
        var f = new PetMiniBarTestFixture();
        var pets = Enumerable.Range(0, 6).Select(i => f.Pet((uint)(100 + i))).ToArray();
        foreach (var pet in pets.Take(5)) Assert.True(f.Manager.TryBind(pet));
        Assert.True(f.Manager.TryBind(pets[0])); Assert.False(f.Manager.TryBind(pets[5]));
        Assert.Equal(new[] { 2, 3, 4, 5, 6 }, f.Bars.Select(bar => (int)bar.ShortcutSet));
        f.Manager.Unbind(pets[0]);
        Assert.True(f.Manager.TryBind(pets[5]));
        f.Manager.Unbind(pets[0]); // stale callback cannot clear replacement
        Assert.Equal(7, f.Bars.Length);
        Assert.Equal(105u, f.Bars[^1].AssociatedUnitId);
        // There is no wire nonce. A valid click now denotes the latest binding.
        Assert.True(f.Manager.TryDismiss(ShortcutSet.PetMiniBar0, 0));
        Assert.Equal(new uint[] { 105 }, f.Removals);
    }

    [Theory]
    [InlineData(0u, 0u)] [InlineData(1u, 0u)] [InlineData(7u, 0u)]
    [InlineData(9u, 0u)] [InlineData(uint.MaxValue, 0u)]
    [InlineData(2u, 1u)] [InlineData(2u, 12u)] [InlineData(2u, uint.MaxValue)]
    public void InvalidSetsOrSlotsNeverDismiss(uint set, uint slot)
    {
        var f = new PetMiniBarTestFixture(); Assert.True(f.Manager.TryBind(f.Pet()));
        Assert.False(f.Manager.TryDismiss((ShortcutSet)set, slot)); Assert.Empty(f.Removals);
        Assert.Single(f.Bars);
    }

    [Theory]
    [InlineData("missing")] [InlineData("type")] [InlineData("object")]
    [InlineData("extraType")] [InlineData("extraObject")]
    public void MissingOrNonDismissOnlyRowsNeverPublish(string fault)
    {
        var f = new PetMiniBarTestFixture(); var row = PetMiniBarTestFixture.DismissRow();
        if (fault == "type") row.ShortcutType00 = 12;
        if (fault == "object") row.ObjectId00 = 0;
        if (fault == "extraType") row.ShortcutType11 = 11;
        if (fault == "extraObject") row.ObjectId01 = 2;
        f.Tables.SetProperty(nameof(IGameTableManager.ActionBarShortcutSet), fault == "missing" ? null : PlayerBindPointTests.Table(row));
        Assert.False(f.Manager.TryBind(f.Pet())); Assert.Empty(f.Packets);
    }

    [Theory]
    [InlineData("owner")] [InlineData("untracked")] [InlineData("vanished")]
    [InlineData("reusedGuid")] [InlineData("map")] [InlineData("generation")]
    [InlineData("canceled")] [InlineData("loading")] [InlineData("replacementOwner")]
    public void InvalidatedOwnershipNeverDismisses(string fault)
    {
        var f = new PetMiniBarTestFixture(); var pet = f.Pet(); Assert.True(f.Manager.TryBind(pet));
        var proxy = (RecordingDispatchProxy<IUnitEntity>)(object)pet;
        switch (fault)
        {
            case "owner": proxy.SetProperty(nameof(IWorldEntity.SummonerGuid), 77u); break;
            case "untracked": f.Factory.UntrackSummon(pet); break;
            case "vanished": f.Entities.Remove(pet.Guid); break;
            case "reusedGuid": f.Pet(pet.Guid); break;
            case "map": proxy.SetProperty(nameof(IGridEntity.Map), RecordingDispatchProxy<IBaseMap>.Create(out _)); break;
            case "generation": f.Generation++; break;
            case "canceled": f.AdmissionCurrent = false; break;
            case "loading": f.Player.SetProperty(nameof(IPlayer.IsLoading), true); break;
            case "replacementOwner": f.Entities[42] = RecordingDispatchProxy<IPlayer>.Create(out _); break;
        }
        Assert.False(f.Manager.TryDismiss(ShortcutSet.PetMiniBar0, 0)); Assert.Empty(f.Removals);
        Assert.Single(f.Bars);
    }

    [Theory]
    [InlineData(0u)] [InlineData(999u)]
    public void UnsupportedPetKindsCannotBind(uint creature)
    {
        var f = new PetMiniBarTestFixture(); Assert.False(f.Manager.TryBind(f.Pet(creature: creature)));
        Assert.Empty(f.Packets);
    }

    [Fact]
    public void OwnerTransferClearsBarsAndCancellationDoesNotSendLatePackets()
    {
        var f = new PetMiniBarTestFixture(); var pet = f.Pet(); Assert.True(f.Manager.TryBind(pet));
        f.Generation++; f.Manager.Clear(true);
        Assert.Equal(2, f.Bars.Length); Assert.Equal(0, f.Bars[^1].ActionBarShortcutSetId);
        f.Player.SetProperty(nameof(IPlayerMapAdmission.MapAdmissionGeneration), f.Generation);
        Assert.True(f.Manager.TryBind(pet));
        f.AdmissionCurrent = false; f.Manager.Clear(false); f.Manager.Unbind(pet); f.Manager.Clear(true);
        Assert.Equal(3, f.Bars.Length); Assert.False(f.Manager.TryBind(pet));
        Assert.False(f.Manager.TryDismiss(ShortcutSet.PetMiniBar0, 0));
    }

    [Fact]
    public void ReentrantCancellationAfterClearCannotUnsummon()
    {
        var f = new PetMiniBarTestFixture(); Assert.True(f.Manager.TryBind(f.Pet()));
        f.PacketHook = _ => { f.AdmissionCurrent = false; f.Manager.Clear(false); };
        Assert.True(f.Manager.TryDismiss(ShortcutSet.PetMiniBar0, 0));
        Assert.Empty(f.Removals);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ClearCannotHideReentrantReplacementBindings(bool advanceGeneration)
    {
        var f = new PetMiniBarTestFixture(); var first = f.Pet(); var second = f.Pet(101);
        Assert.True(f.Manager.TryBind(first)); Assert.True(f.Manager.TryBind(second)); f.Packets.Clear();
        f.PacketHook = _ =>
        {
            f.PacketHook = null;
            if (advanceGeneration) f.Generation++;
            Assert.True(f.Manager.TryBind(first)); Assert.True(f.Manager.TryBind(second));
        };
        f.Manager.Clear(true);
        Assert.Equal(3, f.Bars.Length);
        Assert.Equal(0, f.Bars[0].ActionBarShortcutSetId);
        Assert.All(f.Bars.Skip(1), bar => Assert.Equal(499, bar.ActionBarShortcutSetId));
        Assert.Equal(ShortcutSet.PetMiniBar1, f.Bars[^1].ShortcutSet);
        Assert.True(f.Manager.TryDismiss(ShortcutSet.PetMiniBar1, 0));
        Assert.Equal(new uint[] { 101 }, f.Removals);
    }

    [Fact]
    public void ClearStopsOldContinuationAfterGenerationChangesWithoutRebinding()
    {
        var f = new PetMiniBarTestFixture();
        Assert.True(f.Manager.TryBind(f.Pet())); Assert.True(f.Manager.TryBind(f.Pet(101))); f.Packets.Clear();
        f.PacketHook = _ => f.Generation++;
        f.Manager.Clear(true);
        Assert.Single(f.Bars);
        Assert.False(f.Manager.TryDismiss(ShortcutSet.PetMiniBar1, 0));
    }

    [Fact]
    public void RealPlayerAdmissionCancellationDropsMiniBindingsWithoutOutboundCleanup()
    {
        var f = new PetMiniBarTestFixture(); var pet = f.Pet();
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        typeof(GridEntity).GetProperty(nameof(IGridEntity.Guid))!.SetValue(player, 42u);
        typeof(GridEntity).GetProperty(nameof(IGridEntity.Map))!.SetValue(player, f.Owner.Map);
        typeof(Player).GetProperty(nameof(IPlayer.Session))!.SetValue(player, f.Owner.Session);
        typeof(Player).GetField("gameTableManager", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(player,
            (IGameTableManager)f.Tables);
        var factory = new EntitySummonFactory(RecordingDispatchProxy<IEntityFactory>.Create(out _));
        factory.Initialise(player); factory.TrackSummon(pet);
        typeof(WorldEntity).GetField("summonFactory", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(player, factory);
        f.Entities[42] = player;
        Assert.True(player.TryBindPetMiniBar(pet));
        typeof(Player).GetMethod("CancelMapAdmission", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(player, null);
        player.UnbindPetMiniBar(pet);
        Assert.False(player.TryDismissPetMiniBar(ShortcutSet.PetMiniBar0, 0));
        Assert.False(player.TryBindPetMiniBar(pet));
        Assert.Single(f.Bars); Assert.Empty(f.Removals);
    }
}
