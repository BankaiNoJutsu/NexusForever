using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Hazard;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model.Hazard;
using NexusForever.WorldServer.Command.Handler;
using NetworkHazard = NexusForever.Network.World.Message.Model.Hazard.Hazard;

namespace NexusForever.Game.Tests.Spell;

public class HazardAndVectorSlideSpellTests
{
    [Fact]
    public void HazardStateCollection_EnableModifyAndRemove_PreservesNativeActiveRowShape()
    {
        var hazards = new HazardStateCollection();
        var entry = new HazardEntry
        {
            Id                       = 91u,
            MeterMaxValue            = 105u,
            Flags                    = 0x21u,
            HazardTypeEnum           = (uint)HazardType.Timer,
            MeterThreshold00         = 25f,
            MeterThreshold01         = 50f,
            MeterThreshold02         = 75f,
            Spell4IdThresholdProc00  = 1001u,
            Spell4IdThresholdProc01  = 1002u,
            Spell4IdThresholdProc02  = 1003u
        };

        Assert.True(hazards.TryEnable(10u, entry, out bool firstEnable, out string enableReason));
        Assert.True(firstEnable);
        Assert.Null(enableReason);

        NetworkHazard enabled = Assert.Single(hazards.BuildHazards());
        Assert.Equal((ushort)91u, enabled.HazardId);
        Assert.Equal(HazardType.Timer, enabled.Type);
        Assert.Equal(105f, enabled.MeterValue);
        Assert.Equal(105f, enabled.MaxValue);
        Assert.True(enabled.StartsFull);
        Assert.True(enabled.Enabled);

        Assert.True(hazards.TryModifyMeter(91u, -30f, out string modifyReason));
        Assert.Null(modifyReason);
        NetworkHazard modified = Assert.Single(hazards.BuildHazards());
        Assert.Equal(75f, modified.MeterValue);
        Assert.Equal(1u, modified.CurrentThreshold);
        Assert.Equal(1001u, modified.ProcSpell4Id);

        Assert.True(hazards.RemoveEnable(10u, out uint hazardId, out bool lastEnable));
        Assert.Equal(91u, hazardId);
        Assert.True(lastEnable);
        Assert.Empty(hazards.BuildHazards());
    }

    [Fact]
    public void HazardStateCollection_SuspendSelectors_MapToIdAndTypeModifierArrays()
    {
        var hazards = new HazardStateCollection();
        HazardEntry temperatureA = CreateHazard(65u, HazardType.Temperature);
        HazardEntry temperatureB = CreateHazard(125u, HazardType.Temperature);
        HazardEntry breath = CreateHazard(214u, HazardType.Breath);

        Assert.True(hazards.TryEnable(1u, temperatureA, out _, out _));
        Assert.True(hazards.TryEnable(2u, temperatureB, out _, out _));
        Assert.True(hazards.TryEnable(3u, breath, out _, out _));
        Assert.True(hazards.TrySuspend(10u, temperatureA, 2u, out _));
        Assert.True(hazards.TrySuspend(11u, breath, 0u, out _));

        ServerHazardModifiers modifiers = hazards.BuildModifiers(260);
        Assert.True(modifiers.HazardTypeModifiers[(int)HazardType.Temperature].Suspended);
        Assert.True(modifiers.HazardIdModifiers[214].Suspended);
        Assert.Equal(1f, modifiers.HazardIdModifiers[214].Multiplier);

        Dictionary<ushort, NetworkHazard> active = hazards.BuildHazards().ToDictionary(hazard => hazard.HazardId);
        Assert.True(active[65].Suspended);
        Assert.True(active[125].Suspended);
        Assert.True(active[214].Suspended);

        Assert.True(hazards.RemoveSuspension(10u));
        active = hazards.BuildHazards().ToDictionary(hazard => hazard.HazardId);
        Assert.False(active[65].Suspended);
        Assert.False(active[125].Suspended);
        Assert.True(active[214].Suspended);
    }

    [Fact]
    public void HazardStateCollection_AdvancesTableRateAndFreezesWhileSuspended()
    {
        DateTime nowUtc = new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var hazards = new HazardStateCollection(() => nowUtc);
        HazardEntry entry = CreateHazard(162u, HazardType.Temperature);
        entry.MeterChangeRate = 5f;

        Assert.True(hazards.TryEnable(1u, entry, out _, out _));
        nowUtc = nowUtc.AddSeconds(3);
        Assert.Equal(15f, Assert.Single(hazards.BuildHazards()).MeterValue);

        Assert.True(hazards.TrySuspend(2u, entry, 0u, out _));
        nowUtc = nowUtc.AddSeconds(10);
        Assert.Equal(15f, Assert.Single(hazards.BuildHazards()).MeterValue);

        Assert.True(hazards.RemoveSuspension(2u));
        nowUtc = nowUtc.AddSeconds(2);
        Assert.Equal(25f, Assert.Single(hazards.BuildHazards()).MeterValue);
    }

    [Fact]
    public void HazardModifyHandler_WithDehydrationRow_AppliesSignedDeltaToExactHazard()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodHandler(nameof(IPlayer.TryModifyHazard), args =>
        {
            args[2] = null;
            return true;
        });

        ISpell spell = CreateSpell(46361u, player);
        ISpellTargetEffectInfo info = CreateEffect(
            120001u,
            46361u,
            SpellEffectType.HazardModify,
            2u,
            1u,
            0u,
            BitConverter.SingleToUInt32Bits(-5f),
            163u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectHazardModify(spell, player, info);

        RecordingDispatchProxy<IPlayer>.Invocation invocation = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryModifyHazard)));
        Assert.Equal(163u, (uint)invocation.Arguments[0]);
        Assert.Equal(-5f, (float)invocation.Arguments[1]);
    }

    [Fact]
    public void HazardModifyHandler_WithCleanAirProportionalMode_RemainsBlocked()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy);
        ISpell spell = CreateSpell(44774u, player);
        ISpellTargetEffectInfo info = CreateEffect(
            120002u,
            44774u,
            SpellEffectType.HazardModify,
            4u,
            2u,
            BitConverter.SingleToUInt32Bits(0.07f),
            BitConverter.SingleToUInt32Bits(0.07f),
            119u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectHazardModify(spell, player, info);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TryModifyHazard)));
    }

    [Fact]
    public void HazardModifyHandler_WithUnobservedTail_RemainsBlocked()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy);
        ISpell spell = CreateSpell(46361u, player);
        ISpellTargetEffectInfo info = CreateEffect(
            120003u,
            46361u,
            SpellEffectType.HazardModify,
            2u,
            1u,
            0u,
            BitConverter.SingleToUInt32Bits(-5f),
            163u,
            1u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectHazardModify(spell, player, info);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TryModifyHazard)));
    }

    [Theory]
    [InlineData(0u, 214u)]
    [InlineData(2u, 65u)]
    public void HazardSuspendHandler_ForProvenSelectors_PreservesTargetMode(uint targetMode, uint hazardId)
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodHandler(nameof(IPlayer.TrySuspendHazard), args =>
        {
            args[3] = null;
            return true;
        });

        ISpell spell = CreateSpell(44842u, player);
        ISpellTargetEffectInfo info = CreateEffect(120003u, 44842u, SpellEffectType.HazardSuspend, hazardId, targetMode);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectHazardSuspend(spell, player, info);

        RecordingDispatchProxy<IPlayer>.Invocation invocation = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TrySuspendHazard)));
        Assert.Equal(120003u, (uint)invocation.Arguments[0]);
        Assert.Equal(hazardId, (uint)invocation.Arguments[1]);
        Assert.Equal(targetMode, (uint)invocation.Arguments[2]);
    }

    [Fact]
    public void VectorSlideHandler_ModeZeroPositiveMagnitude_PullsTowardPositionAnchor()
    {
        IUnitEntity caster = CreateUnit(1u, Vector3.Zero, Vector3.Zero, out _);
        IUnitEntity target = CreateUnit(2u, new Vector3(10f, 0f, 0f), Vector3.Zero, out RecordingDispatchProxy<IMovementManager> movementProxy);
        ISpell spell = CreateSpell(84356u, caster, new Position(Vector3.Zero));
        ISpellTargetEffectInfo info = CreateEffect(
            130001u,
            84356u,
            SpellEffectType.VectorSlide,
            0u,
            BitConverter.SingleToUInt32Bits(4f));

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectVectorSlide(spell, target, info);

        RecordingDispatchProxy<IMovementManager>.Invocation velocityCall = Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.SetVelocity)));
        AssertVectorEqual(new Vector3(-4f, 0f, 0f), (Vector3)velocityCall.Arguments[0]);
    }

    [Fact]
    public void VectorSlideHandler_ModeOneNegativeMagnitude_UsesInverseCasterFacing()
    {
        IUnitEntity caster = CreateUnit(1u, Vector3.Zero, Vector3.Zero, out _);
        IUnitEntity target = CreateUnit(2u, new Vector3(10f, 0f, 0f), Vector3.Zero, out RecordingDispatchProxy<IMovementManager> movementProxy);
        ISpell spell = CreateSpell(84573u, caster);
        ISpellTargetEffectInfo info = CreateEffect(
            130002u,
            84573u,
            SpellEffectType.VectorSlide,
            1u,
            BitConverter.SingleToUInt32Bits(-10.5f));

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectVectorSlide(spell, target, info);

        RecordingDispatchProxy<IMovementManager>.Invocation velocityCall = Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.SetVelocity)));
        AssertVectorEqual(new Vector3(0f, 0f, -10.5f), (Vector3)velocityCall.Arguments[0]);
    }

    [Fact]
    public void Interpreter_MapsHazardAndVectorSlidePayloadsWithoutLosingFloatBits()
    {
        SpellEffectInterpretation hazard = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry
        {
            EffectType = SpellEffectType.HazardModify,
            DataBits00 = 2u,
            DataBits01 = 1u,
            DataBits03 = BitConverter.SingleToUInt32Bits(-30f),
            DataBits04 = 163u
        });
        SpellEffectInterpretation slide = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry
        {
            EffectType = SpellEffectType.VectorSlide,
            DataBits00 = 1u,
            DataBits01 = BitConverter.SingleToUInt32Bits(8.1f),
            DataBits02 = 1u
        });

        Assert.Equal(-30f, hazard.HazardModify.Amount);
        Assert.Equal(163u, hazard.HazardModify.HazardId);
        Assert.Equal(8.1f, slide.VectorSlide.Magnitude);
        Assert.Equal(1u, slide.VectorSlide.Flags);
        Assert.True(hazard.HasKnownFamilySemantics);
        Assert.True(slide.HasKnownFamilySemantics);
    }

    [Fact]
    public void VectorSlideLifetime_ZeroDurationSchedulesTransientCleanup()
    {
        SpellEffectInterpretation slide = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry
        {
            EffectType = SpellEffectType.VectorSlide,
            DurationTime = 0u,
            DataBits00 = 1u,
            DataBits01 = BitConverter.SingleToUInt32Bits(8.1f)
        });

        Assert.True(global::NexusForever.Game.Spell.Spell.TryGetEffectLifetimeDelay(slide, out uint durationTime));
        Assert.Equal(0u, durationTime);
    }

    [Fact]
    public void Inspect4_DescribesHazardAndVectorSlideSemantics()
    {
        var category = new SpellCommandCategory(
            RecordingDispatchProxy<IGlobalSpellManager>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));

        SpellEffectInterpretation hazardEnable = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry
        {
            EffectType = SpellEffectType.HazardEnable,
            DataBits00 = 91u
        });
        SpellEffectInterpretation hazardModify = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry
        {
            EffectType = SpellEffectType.HazardModify,
            DataBits00 = 2u,
            DataBits01 = 1u,
            DataBits03 = BitConverter.SingleToUInt32Bits(-30f),
            DataBits04 = 163u
        });
        SpellEffectInterpretation hazardSuspend = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry
        {
            EffectType = SpellEffectType.HazardSuspend,
            DataBits00 = 214u,
            DataBits01 = 0u
        });
        SpellEffectInterpretation vectorSlide = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry
        {
            EffectType = SpellEffectType.VectorSlide,
            DataBits00 = 1u,
            DataBits01 = BitConverter.SingleToUInt32Bits(-10.5f),
            DataBits02 = 1u
        });

        Assert.Contains("hazard enable id 91", category.DescribeEffectSemantics(hazardEnable));
        Assert.Contains("amount -30", category.DescribeEffectSemantics(hazardModify));
        Assert.Contains("hazard suspend id 214", category.DescribeEffectSemantics(hazardSuspend));
        Assert.Contains("vector slide mode 1", category.DescribeEffectSemantics(vectorSlide));
        Assert.DoesNotContain("unknown", category.DescribeEffectSemantics(hazardEnable));
        Assert.DoesNotContain("unknown", category.DescribeEffectSemantics(hazardModify));
        Assert.DoesNotContain("unknown", category.DescribeEffectSemantics(hazardSuspend));
        Assert.DoesNotContain("unknown", category.DescribeEffectSemantics(vectorSlide));
    }

    private static HazardEntry CreateHazard(uint id, HazardType type)
    {
        return new HazardEntry
        {
            Id             = id,
            MeterMaxValue  = 100u,
            HazardTypeEnum = (uint)type
        };
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Rotation), Vector3.Zero);
        return player;
    }

    private static IUnitEntity CreateUnit(
        uint guid,
        Vector3 position,
        Vector3 rotation,
        out RecordingDispatchProxy<IMovementManager> movementProxy)
    {
        IMovementManager movement = RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), StateFlags.None);

        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> unitProxy);
        unitProxy.SetProperty(nameof(IUnitEntity.Guid), guid);
        unitProxy.SetProperty(nameof(IUnitEntity.Position), position);
        unitProxy.SetProperty(nameof(IUnitEntity.Rotation), rotation);
        unitProxy.SetProperty(nameof(IUnitEntity.MovementManager), movement);
        return unit;
    }

    private static ISpell CreateSpell(uint spell4Id, IUnitEntity caster, Position position = null)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry());

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = spell4Id });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);

        ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy);
        parametersProxy.SetProperty(nameof(ISpellParameters.SpellInfo), spellInfo);
        parametersProxy.SetProperty(nameof(ISpellParameters.Position), position);

        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Caster), caster);
        spellProxy.SetProperty(nameof(ISpell.CastingId), 7u);
        spellProxy.SetProperty(nameof(ISpell.Parameters), parameters);
        return spell;
    }

    private static ISpellTargetEffectInfo CreateEffect(
        uint effectId,
        uint spellId,
        SpellEffectType effectType,
        uint dataBits00,
        uint dataBits01 = 0u,
        uint dataBits02 = 0u,
        uint dataBits03 = 0u,
        uint dataBits04 = 0u,
        uint dataBits05 = 0u)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = effectId,
            SpellId    = spellId,
            EffectType = effectType,
            DataBits00 = dataBits00,
            DataBits01 = dataBits01,
            DataBits02 = dataBits02,
            DataBits03 = dataBits03,
            DataBits04 = dataBits04,
            DataBits05 = dataBits05
        });
        return info;
    }

    private static void AssertVectorEqual(Vector3 expected, Vector3 actual)
    {
        Assert.InRange(actual.X, expected.X - 0.0001f, expected.X + 0.0001f);
        Assert.InRange(actual.Y, expected.Y - 0.0001f, expected.Y + 0.0001f);
        Assert.InRange(actual.Z, expected.Z - 0.0001f, expected.Z + 0.0001f);
    }
}
