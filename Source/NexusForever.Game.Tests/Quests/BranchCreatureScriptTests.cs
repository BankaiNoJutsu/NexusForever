using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script.Main.Creature;
using NexusForever.Script.Main.Housing;
using NexusForever.Script.Main.Quests.NorthernWilds;
using NexusForever.Script.Template;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Quests;

public class BranchCreatureScriptTests
{
    [Fact]
    public void HousingDoor_OnLoad_InitialisesClosedState()
    {
        ICreatureEntity owner = CreateCreature(65852u, health: 100u, maxHealth: 100u, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        var script = new HousingDoorEntityScript();

        script.OnLoad(owner);

        RecordingDispatchProxy<ICreatureEntity>.Invocation state = Assert.Single(ownerProxy.GetInvocations("set_" + nameof(IWorldEntity.StandState)));
        Assert.Equal(StandState.State0, state.Arguments[0]);
    }

    [Fact]
    public void HousingDoor_OnActivateSuccess_TogglesOpenAndEmitsDoorStateFromActivator()
    {
        ICreatureEntity owner = CreateCreature(70052u, health: 100u, maxHealth: 100u, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        ownerProxy.SetProperty(nameof(IWorldEntity.Guid), 900u);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = new HousingDoorEntityScript();

        script.OnLoad(owner);
        script.OnActivateSuccess(player);

        List<RecordingDispatchProxy<ICreatureEntity>.Invocation> states = ownerProxy.GetInvocations("set_" + nameof(IWorldEntity.StandState)).ToList();
        Assert.Equal(2, states.Count);
        Assert.Equal(StandState.State1, states[1].Arguments[0]);

        RecordingDispatchProxy<IPlayer>.Invocation emote = Assert.Single(playerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)));
        ServerEmote message = Assert.IsType<ServerEmote>(emote.Arguments[0]);
        Assert.Equal(900u, message.Guid);
        Assert.Equal(StandState.State1, message.StandState);
        Assert.True((bool)emote.Arguments[1]);
    }

    [Fact]
    public void BrambleTrap_OnActivateFail_CastsPenaltySpell()
    {
        ISpellParameters spellParameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> spellParametersProxy);
        var script = new BrambleTrapEntityScript(CreateSpellParametersFactory(spellParameters));
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy);

        script.OnActivateFail(player);

        RecordingDispatchProxy<IPlayer>.Invocation cast = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
        Assert.Equal(46051u, (uint)cast.Arguments[0]);
        Assert.Same(spellParameters, cast.Arguments[1]);

        Assert.Contains(spellParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.UserInitiatedSpellCast)), i => !(bool)i.Arguments[0]);
        Assert.Contains(spellParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.ClientRequestSource)), i => (string)i.Arguments[0] == nameof(BrambleTrapEntityScript));
    }

    [Fact]
    public void BrambleTrap_OnActivateSuccess_RemovesPenaltyStateAndDestroysTrap()
    {
        ICreatureEntity owner = CreateCreature(27768u, health: 40u, maxHealth: 75u, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = new BrambleTrapEntityScript(CreateSpellParametersFactory(RecordingDispatchProxy<ISpellParameters>.Create(out _)));

        script.OnLoad(owner);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPlayer>.Invocation remove = Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.RemoveTrackedSpellStates)));
        var predicate = Assert.IsType<Func<uint, bool>>(remove.Arguments[0]);
        Assert.True(predicate(46051u));
        Assert.False(predicate(123u));
        Assert.Equal(uint.MaxValue, remove.Arguments[1]);

        RecordingDispatchProxy<ICreatureEntity>.Invocation destroy = Assert.Single(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(75u, destroy.Arguments[0]);
        Assert.Equal(DamageType.Physical, destroy.Arguments[1]);
        Assert.Null(destroy.Arguments[2]);
    }

    [Fact]
    public void MarauderMine_OnActivateSuccess_CastsDisarmExplosionAtActivator()
    {
        ISpellParameters spellParameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> spellParametersProxy);
        ICreatureEntity owner = CreateCreature(16718u, health: 40u, maxHealth: 75u, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer player = CreatePlayer(out _);
        var script = new MarauderMineEntityScript(CreateSpellParametersFactory(spellParameters));

        script.OnLoad(owner);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<ICreatureEntity>.Invocation cast = Assert.Single(ownerProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Equal(26443u, (uint)cast.Arguments[0]);
        Assert.Same(spellParameters, cast.Arguments[1]);

        Assert.Contains(spellParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.PrimaryTargetId)), i => (uint)i.Arguments[0] == 42u);
        Assert.Contains(spellParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.UserInitiatedSpellCast)), i => !(bool)i.Arguments[0]);
        Assert.Contains(spellParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.ClientRequestSource)), i => (string)i.Arguments[0] == nameof(MarauderMineEntityScript));
    }

    [Fact]
    public void DominionGate_OnActivateSuccess_OpensGateAndClosesAfterDelay()
    {
        IDoorEntity door = RecordingDispatchProxy<IDoorEntity>.Create(out RecordingDispatchProxy<IDoorEntity> doorProxy);
        doorProxy.SetProperty(nameof(IDoorEntity.IsOpen), false);
        doorProxy.SetMethodHandler(nameof(IDoorEntity.OpenDoor), _ =>
        {
            doorProxy.SetProperty(nameof(IDoorEntity.IsOpen), true);
            return null;
        });
        doorProxy.SetMethodHandler(nameof(IDoorEntity.CloseDoor), _ =>
        {
            doorProxy.SetProperty(nameof(IDoorEntity.IsOpen), false);
            return null;
        });

        ISimpleEntity owner = CreateSimpleEntity(12653u, out RecordingDispatchProxy<ISimpleEntity> ownerProxy);
        ownerProxy.SetMethodHandler(nameof(IGridEntity.GetVisibleCreature), _ => new[] { door });

        var script = new DominionGateEntityScript();
        script.OnLoad(owner);
        script.OnActivateSuccess(CreatePlayer(out _));

        Assert.Single(doorProxy.GetInvocations(nameof(IDoorEntity.OpenDoor)));
        Assert.Empty(doorProxy.GetInvocations(nameof(IDoorEntity.CloseDoor)));

        script.Update(9.9d);
        Assert.Empty(doorProxy.GetInvocations(nameof(IDoorEntity.CloseDoor)));

        script.Update(0.2d);
        Assert.Single(doorProxy.GetInvocations(nameof(IDoorEntity.CloseDoor)));
    }

    [Fact]
    public void DominionGate_OnActivateSuccess_WhenGateAlreadyOpen_DoesNotReschedule()
    {
        IDoorEntity door = RecordingDispatchProxy<IDoorEntity>.Create(out RecordingDispatchProxy<IDoorEntity> doorProxy);
        doorProxy.SetProperty(nameof(IDoorEntity.IsOpen), true);

        ISimpleEntity owner = CreateSimpleEntity(12653u, out RecordingDispatchProxy<ISimpleEntity> ownerProxy);
        ownerProxy.SetMethodHandler(nameof(IGridEntity.GetVisibleCreature), _ => new[] { door });

        var script = new DominionGateEntityScript();
        script.OnLoad(owner);
        script.OnActivateSuccess(CreatePlayer(out _));
        script.Update(10.1d);

        Assert.Empty(doorProxy.GetInvocations(nameof(IDoorEntity.OpenDoor)));
        Assert.Empty(doorProxy.GetInvocations(nameof(IDoorEntity.CloseDoor)));
    }

    [Fact]
    public void DominionGate_UsesSimpleEntityOwnerForBarrierControlPanel()
    {
        Assert.True(typeof(IOwnedScript<ISimpleEntity>).IsAssignableFrom(typeof(DominionGateEntityScript)));
    }

    private static ICreatureEntity CreateCreature(
        uint creatureId,
        uint health,
        uint maxHealth,
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy)
    {
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        ownerProxy.SetProperty(nameof(IWorldEntity.Health), health);
        ownerProxy.SetProperty(nameof(IWorldEntity.MaxHealth), maxHealth);
        return owner;
    }

    private static ISimpleEntity CreateSimpleEntity(
        uint creatureId,
        out RecordingDispatchProxy<ISimpleEntity> ownerProxy)
    {
        ISimpleEntity owner = RecordingDispatchProxy<ISimpleEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        return owner;
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.Guid), 42u);
        return player;
    }

    private static IFactory<ISpellParameters> CreateSpellParametersFactory(ISpellParameters spellParameters)
    {
        IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
        factoryProxy.SetMethodReturn(nameof(IFactory<ISpellParameters>.Resolve), spellParameters);
        return factory;
    }
}
