using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.Pet;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.IO.Map;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Map;

public class MapRemovalLifecycleTests
{
    [Fact]
    public void RealGridDuplicateRemovalRecyclesGuidAndRunsCallbacksExactlyOnce()
    {
        var map = new RemovalMap(); var pet = map.Pet(100, out var proxy);
        map.EnqueueRemove(pet); map.EnqueueRemove(pet); map.Tick();
        Assert.Single(proxy.GetInvocations(nameof(IGridEntity.OnRemoveFromMap)));
        Assert.Null(map.GetEntity<IGridEntity>(100)); Assert.Equal(0u, map.GridCount);
        Assert.Single(map.Events.GetInvocations(nameof(IPublicEventManager.OnRemoveFromMap)));
        Assert.Equal(100u, map.NextGuid());
        Assert.NotEqual(100u, map.NextGuid());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OldMapAndReusedGuidActionsCannotRemoveCurrentEntity(bool transferred)
    {
        var oldMap = new RemovalMap(); var pet = oldMap.Pet(100, out var proxy);
        oldMap.EnqueueRemove(pet);
        if (transferred)
        {
            var newMap = new RemovalMap();
            proxy.SetProperty(nameof(IGridEntity.Map), newMap);
            newMap.Adopt(pet);
            oldMap.Tick();
            Assert.Same(newMap, pet.Map);
            Assert.Same(pet, newMap.GetEntity<IGridEntity>(100));
            Assert.Equal(1u, newMap.GridCount);
        }
        else
        {
            var replacement = oldMap.Pet(100, out _);
            oldMap.Tick(); Assert.Same(replacement, oldMap.GetEntity<IGridEntity>(100));
        }
        Assert.Empty(proxy.GetInvocations(nameof(IGridEntity.OnRemoveFromMap)));
        Assert.Empty(oldMap.Events.GetInvocations(nameof(IPublicEventManager.OnRemoveFromMap)));
    }

    [Fact]
    public void MiniDismissThenActualSummonEffectCleanupBeforeTickRemovesOnce()
    {
        var map = new RemovalMap(); var owner = RecordingDispatchProxy<IPlayer>.Create(out var player);
        player.SetProperty(nameof(IPlayer.Guid), 42u); player.SetProperty(nameof(IPlayer.Map), map);
        player.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        map.Register(owner);
        var factory = new EntitySummonFactory(RecordingDispatchProxy<IEntityFactory>.Create(out _));
        factory.Initialise(owner); player.SetProperty(nameof(IPlayer.SummonFactory), factory);
        var pet = map.Pet(100, out var petProxy); factory.TrackSummon(pet);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.ActionBarShortcutSet), PlayerBindPointTests.Table(PetMiniBarTestFixture.DismissRow()));
        var bars = new PetMiniBarManager(owner, tables); Assert.True(bars.TryBind(pet));
        Assert.True(bars.TryDismiss(ShortcutSet.PetMiniBar0, 0));

        var entry = new Spell4EffectsEntry { Id = 97661, SpellId = 42814, EffectType = SpellEffectType.SummonPet, DataBits00 = 42683, DataBits01 = 3 };
        var info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.CreatedEntities), new IGridEntity[] { pet });
        // Exercise the actual lifetime cleanup used by cancel/expiry, without a fake second enqueue.
        var spell = (global::NexusForever.Game.Spell.Spell)RuntimeHelpers.GetUninitializedObject(typeof(global::NexusForever.Game.Spell.Spell));
        var method = typeof(global::NexusForever.Game.Spell.Spell).GetMethod("BuildLifetimeRemovalAction",
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            [typeof(SpellEffectInterpretation), typeof(IUnitEntity), typeof(ISpellTargetEffectInfo)], null)!;
        var cleanup = (Action)method.Invoke(spell, [SpellEffectInterpreter.Interpret(entry), pet, info])!;
        cleanup();
        Assert.Equal(2, map.QueueCount); map.Tick();
        Assert.Single(petProxy.GetInvocations(nameof(IGridEntity.OnRemoveFromMap)));
        Assert.Equal(0u, map.GridCount);
        Assert.Single(map.Events.GetInvocations(nameof(IPublicEventManager.OnRemoveFromMap)));
    }

    private sealed class RemovalMap : BaseMap
    {
        private readonly MapGrid grid;
        public RecordingDispatchProxy<IPublicEventManager> Events { get; }
        public int QueueCount => pendingActions.Count;
        public uint GridCount => (uint)typeof(MapGrid).GetField("entityCount", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(grid)!;
        public RemovalMap() : this(RecordingDispatchProxy<IPublicEventManager>.Create(out var events), events) { }
        private RemovalMap(IPublicEventManager events, RecordingDispatchProxy<IPublicEventManager> proxy)
            : base(RecordingDispatchProxy<IEntityFactory>.Create(out _), events)
        {
            Events = proxy;
            typeof(BaseMap).GetProperty(nameof(Entry))!.SetValue(this, new WorldEntry { Id = 426 });
            var coord = MapGrid.GetGridCoord(Vector3.Zero);
            grid = new MapGrid(coord.gridX, coord.gridZ, false);
            var grids = (IMapGrid[])typeof(BaseMap).GetField("grids", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
            grids[coord.gridZ * MapDefines.WorldGridCount + coord.gridX] = grid;
        }
        public void Register(IGridEntity entity) => entities[entity.Guid] = entity;
        public void Adopt(IGridEntity entity) { Register(entity); grid.AddEntity(entity, entity.Position); }
        public IUnitEntity Pet(uint guid, out RecordingDispatchProxy<IUnitEntity> proxy)
        {
            var pet = RecordingDispatchProxy<IUnitEntity>.Create(out proxy);
            var state = proxy;
            state.SetProperty(nameof(IGridEntity.Guid), guid); state.SetProperty(nameof(IGridEntity.Map), this);
            state.SetProperty(nameof(IGridEntity.Position), Vector3.Zero); state.SetProperty(nameof(IGridEntity.InWorld), true);
            state.SetProperty(nameof(IWorldEntity.CreatureId), 42683u); state.SetProperty(nameof(IWorldEntity.SummonerGuid), 42u);
            state.SetMethodHandler(nameof(IGridEntity.RemoveFromMap), _ => { EnqueueRemove(pet); return null; });
            state.SetMethodHandler(nameof(IGridEntity.OnRemoveFromMap), _ => {
                state.SetProperty(nameof(IGridEntity.Map), null); state.SetProperty(nameof(IGridEntity.Guid), 0u);
                state.SetProperty(nameof(IGridEntity.InWorld), false); return null;
            });
            Register(pet); grid.AddEntity(pet, Vector3.Zero); return pet;
        }
        public void Tick() => typeof(BaseMap).GetMethod("ProcessGridActions", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(this, null);
        public uint NextGuid()
        {
            var counter = (QueuedCounter)typeof(BaseMap).GetField("entityCounter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
            return counter.Dequeue();
        }
    }
}
