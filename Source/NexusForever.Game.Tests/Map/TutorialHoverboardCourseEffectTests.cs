using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Tutorial;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Map;

public class TutorialHoverboardCourseEffectTests
{
    [Fact]
    public void Booster_OnMountedTutorialPlayer_AppliesExtraGasSprintVisualAndVelocity()
    {
        IBaseMap map = CreateTutorialMap();
        ISimpleEntity owner = CreateSimpleEntity(73461u, map, out _);
        IPlayer player = CreateHoverboardPlayer(
            map,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);
        var script = new TutorialHoverboardBoosterEntityScript(
            NullLogger<TutorialHoverboardBoosterEntityScript>.Instance,
            CreateSpellParametersFactory());
        script.OnLoad(owner);

        script.OnEnterRange(player);

        RecordingDispatchProxy<IMovementManager>.Invocation velocity = Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.SetVelocity)));
        var appliedVelocity = (Vector3)velocity.Arguments[0];
        Assert.Equal(57f, appliedVelocity.Length(), precision: 3);

        uint[] spellIds = playerProxy
            .GetInvocations(nameof(IPlayer.CastSpell))
            .Where(i => i.Arguments.Length == 2 && i.Arguments[0] is uint)
            .Select(i => (uint)i.Arguments[0])
            .ToArray();
        Assert.Equal([85424u, 82298u], spellIds);
        Assert.All(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)), invocation =>
        {
            ISpellParameters spellParameters = (ISpellParameters)invocation.Arguments[1];
            Assert.Equal(100u, spellParameters.PrimaryTargetId);
            Assert.True(spellParameters.IgnoreGlobalCooldown);
        });
    }

    [Fact]
    public void HoverboardRing_OnMountedTutorialPlayer_CastsMappedRaceObjectiveFx()
    {
        IBaseMap map = CreateTutorialMap();
        ISimpleEntity owner = CreateSimpleEntity(73416u, map, out _);
        IPlayer player = CreateHoverboardPlayer(
            map,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out _);
        var script = new TutorialHoverboardRingEntityScript(
            NullLogger<TutorialHoverboardRingEntityScript>.Instance,
            CreateSpellParametersFactory());
        script.OnLoad(owner);

        script.OnEnterRange(player);

        uint[] spellIds = playerProxy
            .GetInvocations(nameof(IPlayer.CastSpell))
            .Where(i => i.Arguments.Length == 2 && i.Arguments[0] is uint)
            .Select(i => (uint)i.Arguments[0])
            .ToArray();
        Assert.Equal([82460u], spellIds);
        Assert.All(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)), invocation =>
            Assert.True(((ISpellParameters)invocation.Arguments[1]).IgnoreGlobalCooldown));
    }

    [Fact]
    public void HoverboardRing_OnMountedVehicle_CastsMappedRaceObjectiveFxOnPilot()
    {
        IBaseMap map = CreateTutorialMap(out RecordingDispatchProxy<IBaseMap> mapProxy);
        ISimpleEntity owner = CreateSimpleEntity(73416u, map, out _);
        IPlayer player = CreateHoverboardPlayer(
            map,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out _,
            platformGuid: 700u);
        IVehicleEntity vehicle = CreateHoverboardVehicle(
            map,
            player,
            mapProxy,
            out _,
            out _);
        var script = new TutorialHoverboardRingEntityScript(
            NullLogger<TutorialHoverboardRingEntityScript>.Instance,
            CreateSpellParametersFactory());
        script.OnLoad(owner);

        script.OnEnterRange(vehicle);

        uint[] spellIds = playerProxy
            .GetInvocations(nameof(IPlayer.CastSpell))
            .Where(i => i.Arguments.Length == 2 && i.Arguments[0] is uint)
            .Select(i => (uint)i.Arguments[0])
            .ToArray();
        Assert.Equal([82460u], spellIds);
        Assert.All(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)), invocation =>
            Assert.True(((ISpellParameters)invocation.Arguments[1]).IgnoreGlobalCooldown));
    }

    [Fact]
    public void Booster_OnMountedVehicle_AppliesEffectsToPilotAndVelocityToVehicle()
    {
        IBaseMap map = CreateTutorialMap(out RecordingDispatchProxy<IBaseMap> mapProxy);
        ISimpleEntity owner = CreateSimpleEntity(73461u, map, out _);
        IPlayer player = CreateHoverboardPlayer(
            map,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out _,
            platformGuid: 700u);
        IVehicleEntity vehicle = CreateHoverboardVehicle(
            map,
            player,
            mapProxy,
            out _,
            out RecordingDispatchProxy<IMovementManager> vehicleMovementProxy);
        var script = new TutorialHoverboardBoosterEntityScript(
            NullLogger<TutorialHoverboardBoosterEntityScript>.Instance,
            CreateSpellParametersFactory());
        script.OnLoad(owner);

        script.OnEnterRange(vehicle);

        RecordingDispatchProxy<IMovementManager>.Invocation velocity = Assert.Single(vehicleMovementProxy.GetInvocations(nameof(IMovementManager.SetVelocity)));
        var appliedVelocity = (Vector3)velocity.Arguments[0];
        Assert.Equal(57f, appliedVelocity.Length(), precision: 3);

        uint[] spellIds = playerProxy
            .GetInvocations(nameof(IPlayer.CastSpell))
            .Where(i => i.Arguments.Length == 2 && i.Arguments[0] is uint)
            .Select(i => (uint)i.Arguments[0])
            .ToArray();
        Assert.Equal([85424u, 82298u], spellIds);
        Assert.All(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)), invocation =>
            Assert.True(((ISpellParameters)invocation.Arguments[1]).IgnoreGlobalCooldown));
    }

    [Fact]
    public void HoverboardRing_OnNonTutorialPlayer_DoesNotCastFx()
    {
        IBaseMap map = CreateTutorialMap();
        ISimpleEntity owner = CreateSimpleEntity(73416u, map, out _);
        IPlayer player = CreateHoverboardPlayer(
            map,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out _,
            hasTutorialQuest: false);
        var script = new TutorialHoverboardRingEntityScript(
            NullLogger<TutorialHoverboardRingEntityScript>.Instance,
            CreateSpellParametersFactory());
        script.OnLoad(owner);

        script.OnEnterRange(player);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
    }

    [Fact]
    public void ObjectiveRing_BeforeHoverboardEquip_DoesNotCastHoverboardFx()
    {
        IBaseMap map = CreateTutorialMap();
        ISimpleEntity owner = CreateSimpleEntity(70939u, map, out _);
        IPlayer player = CreateHoverboardPlayer(
            map,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out _,
            hasHoverboardSpellState: false);
        var script = new TutorialHoverboardRingEntityScript(
            NullLogger<TutorialHoverboardRingEntityScript>.Instance,
            CreateSpellParametersFactory());
        script.OnLoad(owner);

        script.OnEnterRange(player);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
    }

    private static IBaseMap CreateTutorialMap()
    {
        return CreateTutorialMap(out _);
    }

    private static IBaseMap CreateTutorialMap(out RecordingDispatchProxy<IBaseMap> mapProxy)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 3460u });
        return map;
    }

    private static ISimpleEntity CreateSimpleEntity(uint creatureId, IBaseMap map, out RecordingDispatchProxy<ISimpleEntity> entityProxy)
    {
        ISimpleEntity entity = RecordingDispatchProxy<ISimpleEntity>.Create(out entityProxy);
        entityProxy.SetProperty(nameof(ISimpleEntity.Guid), 500u + creatureId);
        entityProxy.SetProperty(nameof(ISimpleEntity.CreatureId), creatureId);
        entityProxy.SetProperty(nameof(ISimpleEntity.Position), Vector3.Zero);
        entityProxy.SetProperty(nameof(ISimpleEntity.Map), map);
        return entity;
    }

    private static IPlayer CreateHoverboardPlayer(
        IBaseMap map,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IMovementManager> movementProxy,
        bool hasTutorialQuest = true,
        uint? platformGuid = null,
        bool hasHoverboardSpellState = true)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
            hasTutorialQuest && (ushort)args[0] == 10527 ? (QuestState?)QuestState.Accepted : null);

        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetVelocity), Vector3.UnitZ);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetMove), Vector3.Zero);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), StateFlags.Move);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 100u);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Rotation), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.PlatformGuid), platformGuid);
        playerProxy.SetProperty(nameof(IPlayer.MovementManager), movementManager);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetMethodHandler(nameof(IPlayer.HasTrackedSpellState), args => hasHoverboardSpellState && (uint)args[0] == 85562u);

        return player;
    }

    private static IVehicleEntity CreateHoverboardVehicle(
        IBaseMap map,
        IPlayer pilot,
        RecordingDispatchProxy<IBaseMap> mapProxy,
        out RecordingDispatchProxy<IVehicleEntity> vehicleProxy,
        out RecordingDispatchProxy<IMovementManager> movementProxy)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetVelocity), Vector3.UnitZ);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetMove), Vector3.Zero);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), StateFlags.Move);

        IVehicleEntity vehicle = RecordingDispatchProxy<IVehicleEntity>.Create(out vehicleProxy);
        vehicleProxy.SetProperty(nameof(IVehicleEntity.Guid), 700u);
        vehicleProxy.SetProperty(nameof(IVehicleEntity.Map), map);
        vehicleProxy.SetProperty(nameof(IVehicleEntity.Position), Vector3.Zero);
        vehicleProxy.SetProperty(nameof(IVehicleEntity.ControllerGuid), pilot.Guid);
        vehicleProxy.SetProperty(nameof(IVehicleEntity.MovementManager), movementManager);
        vehicleProxy.SetMethodHandler(nameof(IVehicleEntity.GetPassenger), args =>
            args.Length == 2 && args[0] is VehicleSeatType seatType && (byte)args[1] == 0
                ? new TestVehiclePassenger(seatType, 0, pilot.Guid)
                : null);

        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args =>
            (uint)args[0] == pilot.Guid ? pilot : null);

        return vehicle;
    }

    private static IFactory<ISpellParameters> CreateSpellParametersFactory()
    {
        IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
        factoryProxy.SetMethodHandler(nameof(IFactory<ISpellParameters>.Resolve), args =>
            RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy));

        return factory;
    }

    private sealed class TestVehiclePassenger : IVehiclePassenger
    {
        public VehicleSeatType SeatType { get; }
        public byte SeatPosition { get; }
        public uint Guid { get; }

        public TestVehiclePassenger(VehicleSeatType seatType, byte seatPosition, uint guid)
        {
            SeatType = seatType;
            SeatPosition = seatPosition;
            Guid = guid;
        }
    }
}
