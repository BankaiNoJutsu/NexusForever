using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script.Main.Creature;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Map;

public class SpeedBoostEntityScriptTests
{
    [Fact]
    public void OnLoad_ConfiguresTouchRange()
    {
        ICreatureEntity speedBoost = CreateSpeedBoost(out RecordingDispatchProxy<ICreatureEntity> speedBoostProxy);
        var script = CreateScript();

        script.OnLoad(speedBoost);

        RecordingDispatchProxy<ICreatureEntity>.Invocation rangeCheck =
            Assert.Single(speedBoostProxy.GetInvocations(nameof(IGridEntity.SetInRangeCheck)));
        Assert.Equal(4f, rangeCheck.Arguments[0]);
    }

    [Fact]
    public void OnAddToMap_ReappliesTouchRange()
    {
        ICreatureEntity speedBoost = CreateSpeedBoost(out RecordingDispatchProxy<ICreatureEntity> speedBoostProxy);
        var script = CreateScript();
        script.OnLoad(speedBoost);

        script.OnAddToMap(RecordingDispatchProxy<IBaseMap>.Create(out _));

        IReadOnlyList<RecordingDispatchProxy<ICreatureEntity>.Invocation> rangeChecks =
            speedBoostProxy.GetInvocations(nameof(IGridEntity.SetInRangeCheck));
        Assert.Equal(2, rangeChecks.Count);
        Assert.All(rangeChecks, invocation => Assert.Equal(4f, invocation.Arguments[0]));
    }

    [Fact]
    public void OnEnterRange_WithPlayer_CastsSpeedBoostOnPlayer()
    {
        SpellParameters spellParameters = new();
        ICreatureEntity speedBoost = CreateSpeedBoost(out _);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = CreateScript(spellParameters);
        script.OnLoad(speedBoost);

        script.OnEnterRange(player);

        RecordingDispatchProxy<IPlayer>.Invocation cast =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Equal(72259u, cast.Arguments[0]);
        Assert.Same(spellParameters, cast.Arguments[1]);
        Assert.Equal(player.Guid, spellParameters.PrimaryTargetId);
        Assert.False(spellParameters.UserInitiatedSpellCast);
        Assert.True(spellParameters.IgnoreGlobalCooldown);
        Assert.True(spellParameters.CancelActiveTrade);
        Assert.False(spellParameters.UseCreatureOverrides);
        Assert.Equal(nameof(SpeedBoostEntityScript), spellParameters.ClientRequestSource);
    }

    [Fact]
    public void Update_WithNearbyPlayer_CastsSpeedBoostOnPlayer()
    {
        SpellParameters spellParameters = new();
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetMethodReturn(nameof(IBaseMap.Search), new IGridEntity[] { player });
        ICreatureEntity speedBoost = CreateSpeedBoost(out _, map);
        var script = CreateScript(spellParameters);
        script.OnLoad(speedBoost);

        script.Update(0.05d);
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));

        script.Update(0.05d);

        RecordingDispatchProxy<IPlayer>.Invocation cast =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Equal(72259u, cast.Arguments[0]);
        Assert.Same(spellParameters, cast.Arguments[1]);
        Assert.Equal(player.Guid, spellParameters.PrimaryTargetId);
    }

    [Fact]
    public void OnEnterRange_WithMountedPlayer_CastsSpeedBoostOnPilot()
    {
        SpellParameters spellParameters = new();
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy, platformGuid: 700u);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == player.Guid ? player : null);
        ICreatureEntity speedBoost = CreateSpeedBoost(out _, map);
        IVehicleEntity vehicle = CreateVehicle(player, out _);
        var script = CreateScript(spellParameters);
        script.OnLoad(speedBoost);

        script.OnEnterRange(vehicle);

        RecordingDispatchProxy<IPlayer>.Invocation cast =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Equal(72259u, cast.Arguments[0]);
        Assert.Same(spellParameters, cast.Arguments[1]);
        Assert.Equal(player.Guid, spellParameters.PrimaryTargetId);
    }

    [Fact]
    public void OnEnterRange_WithRecentSuccessfulCast_DoesNotCastAgain()
    {
        ICreatureEntity speedBoost = CreateSpeedBoost(out _);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = CreateScript();
        script.OnLoad(speedBoost);

        script.OnEnterRange(player);
        script.OnEnterRange(player);

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
    }

    [Fact]
    public void OnEnterRange_WhenCastFails_DoesNotStartCooldown()
    {
        ICreatureEntity speedBoost = CreateSpeedBoost(out _);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy, castResult: CastResult.CasterUnknown);
        var script = CreateScript();
        script.OnLoad(speedBoost);

        script.OnEnterRange(player);
        script.OnEnterRange(player);

        Assert.Equal(2, playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)).Count);
    }

    [Fact]
    public void OnEnterRange_WithDeadPlayer_DoesNotCast()
    {
        ICreatureEntity speedBoost = CreateSpeedBoost(out _);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy, isAlive: false);
        var script = CreateScript();
        script.OnLoad(speedBoost);

        script.OnEnterRange(player);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
    }

    private static SpeedBoostEntityScript CreateScript(SpellParameters spellParameters = null)
    {
        IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
        factoryProxy.SetMethodReturnFactory(nameof(IFactory<ISpellParameters>.Resolve), () => spellParameters ?? new SpellParameters());

        return new SpeedBoostEntityScript(
            NullLogger<SpeedBoostEntityScript>.Instance,
            factory);
    }

    private static ICreatureEntity CreateSpeedBoost(
        out RecordingDispatchProxy<ICreatureEntity> speedBoostProxy,
        IBaseMap map = null,
        CastResult castResult = CastResult.Ok)
    {
        ICreatureEntity speedBoost = RecordingDispatchProxy<ICreatureEntity>.Create(out speedBoostProxy);
        speedBoostProxy.SetProperty(nameof(ICreatureEntity.Guid), 500u);
        speedBoostProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 62476u);
        speedBoostProxy.SetProperty(nameof(ICreatureEntity.Map), map);
        return speedBoost;
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IPlayer> playerProxy,
        bool isAlive = true,
        uint? platformGuid = null,
        CastResult castResult = CastResult.Ok)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 100u);
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), isAlive);
        playerProxy.SetProperty(nameof(IPlayer.PlatformGuid), platformGuid);
        playerProxy.SetMethodReturn(nameof(IPlayer.TryCastSpell), castResult);
        return player;
    }

    private static IVehicleEntity CreateVehicle(IPlayer pilot, out RecordingDispatchProxy<IVehicleEntity> vehicleProxy)
    {
        IVehicleEntity vehicle = RecordingDispatchProxy<IVehicleEntity>.Create(out vehicleProxy);
        vehicleProxy.SetProperty(nameof(IVehicleEntity.Guid), 700u);
        vehicleProxy.SetProperty(nameof(IVehicleEntity.ControllerGuid), pilot.Guid);
        vehicleProxy.SetMethodHandler(nameof(IVehicleEntity.GetPassenger), args =>
            args.Length == 2 && args[0] is VehicleSeatType seatType && (byte)args[1] == 0
                ? new TestVehiclePassenger(seatType, 0, pilot.Guid)
                : null);

        return vehicle;
    }

    private sealed class TestVehiclePassenger : IVehiclePassenger
    {
        public VehicleSeatType SeatType { get; }
        public byte SeatPosition { get; }
        public uint Guid { get; }

        public TestVehiclePassenger(VehicleSeatType seatType, byte seatPosition, uint guid)
        {
            SeatType     = seatType;
            SeatPosition = seatPosition;
            Guid         = guid;
        }
    }
}
