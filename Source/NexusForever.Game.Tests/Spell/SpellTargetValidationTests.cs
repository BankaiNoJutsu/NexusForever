using System.Numerics;
using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;

namespace NexusForever.Game.Tests.Spell;

public class SpellTargetValidationTests
{
    [Theory]
    [InlineData(22u, 22u, 180f, false)]
    [InlineData(22u, 170u, 180f, true)]
    [InlineData(22u, 170u, 0f, false)]
    [InlineData(22u, 170u, 360f, false)]
    public void ShouldApplyPrimaryTargetAngle_SkipsSelfAnchoredTargets(uint casterGuid, uint targetGuid, float targetAngle, bool expected)
    {
        bool result = NexusForever.Game.Spell.Spell.ShouldApplyPrimaryTargetAngle(casterGuid, targetGuid, targetAngle);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x02u, true, CastResult.TargetUnknown)]
    [InlineData(0x02u, false, CastResult.TargetUnknown)]
    [InlineData(0x0Au, true, CastResult.TargetMustBeDead)]
    [InlineData(0x12u, true, CastResult.Ok)]
    public void CheckPrimaryTargetValidMask_DoesNotTreatObjectOnlyBitAsLivingUnitMask(uint targetBitmask, bool targetAlive, CastResult expected)
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _);
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), targetAlive);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfoWithValidTargetMask(targetBitmask)
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(expected, InvokePrivate<CastResult>(spell, "CheckPrimaryTargetValidMask", target));
    }

    [Fact]
    public void CheckPrimaryTargetValidMask_AllowsUnrestrictedMaskForLockboxTarget()
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _);
        IWorldEntity target = CreateWorldObject(2002u, EntityType.Lockbox);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfoWithValidTargetMask(0u)
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(CastResult.Ok, InvokePrivate<CastResult>(spell, "CheckPrimaryTargetValidMask", target));
    }

    [Fact]
    public void CheckPrimaryTargetValidMask_AllowsObjectOnlyBitForSimpleEntityTargets()
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _);
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Type), EntityType.Simple);
        targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfoWithValidTargetMask(0x02u)
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(CastResult.Ok, InvokePrivate<CastResult>(spell, "CheckPrimaryTargetValidMask", target));
    }

    [Fact]
    public void CheckPrimaryTargetValidMask_AllowsObjectMaskForSelfTargetedUnit()
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> casterProxy);
        casterProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfoWithValidTargetMask(0x02u)
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(CastResult.Ok, InvokePrivate<CastResult>(spell, "CheckPrimaryTargetValidMask", caster));
    }

    [Theory]
    [InlineData((uint)Faction.Exile, CastResult.Ok)]
    [InlineData((uint)Faction.MatchingTeam2, CastResult.TargetUnknown)]
    public void CheckPrimaryTargetValidMask_AllowsObjectMaskUnitTargetsOnlyWhenCastGroupMatches(uint faction2Id, CastResult expected)
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _);
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Faction2), (Faction)faction2Id);
        targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfoWithValidTargetMask(
                0x02u,
                new TargetGroupEntry
                {
                    Type        = 3u,
                    DataEntries = [166u, 167u, 391u, 0u, 0u, 0u, 0u]
                })
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(expected, InvokePrivate<CastResult>(spell, "CheckPrimaryTargetValidMask", target));
    }

    [Fact]
    public void CheckPrimaryTargetValidMask_AllowsObjectMaskUnitTargetsForMatchingActivateSpell()
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _);
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Spell4IdActivate00 = 126u });
        targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfoWithValidTargetMask(0x02u)
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(CastResult.Ok, InvokePrivate<CastResult>(spell, "CheckPrimaryTargetValidMask", target));
    }

    [Theory]
    [InlineData((uint)Faction.Exile, CastResult.Ok)]
    [InlineData((uint)Faction.MatchingTeam2, CastResult.TargetUnknown)]
    public void CheckPrimaryTargetCastGroup_UsesCasterForCreatureOverrideActivateSpell(uint casterFaction2Id, CastResult expected)
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> casterProxy);
        casterProxy.SetProperty(nameof(IWorldEntity.Faction2), (Faction)casterFaction2Id);

        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Faction2), (Faction)219u);
        targetProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Spell4IdActivate00 = 126u });

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            UseCreatureOverrides = true,
            SpellInfo = CreateSpellInfoWithValidTargetMask(
                0u,
                new TargetGroupEntry
                {
                    Type        = 3u,
                    DataEntries = [166u, 167u, 0u, 0u, 0u, 0u, 0u]
                })
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(expected, InvokePrivate<CastResult>(spell, "CheckPrimaryTargetCastGroup", target));
    }

    [Fact]
    public void Cast_WithCreatureOverrideActivateSpell_AllowsCasterFactionCastGroup()
    {
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        targetProxy.SetProperty(nameof(IWorldEntity.Faction2), (Faction)219u);
        targetProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry
        {
            Spell4IdActivate00 = 126u
        });

        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> casterProxy, CreateSearchMap(target));
        casterProxy.SetProperty(nameof(IWorldEntity.Faction2), Faction.Exile);
        casterProxy.SetMethodHandler(nameof(IUnitEntity.GetVisible), args => (uint)args[0] == target.Guid ? target : null);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            PrimaryTargetId      = target.Guid,
            UseCreatureOverrides = true,
            SpellInfo = CreateSpellInfoWithValidTargetMask(
                0u,
                new TargetGroupEntry
                {
                    Type        = 3u,
                    DataEntries = [166u, 167u, 0u, 0u, 0u, 0u, 0u]
                })
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(CastResult.Ok, spell.Cast());
    }

    [Fact]
    public void SendSpellStart_AttachesCreatureTelegraphToCaster_WhenPrimaryTargetExists()
    {
        IUnitEntity caster = CreateUnit(1001u, new Vector3(10f, 20f, 30f), out RecordingDispatchProxy<IUnitEntity> casterProxy);
        IPlayer target = CreatePlayer(2002u, new Vector3(10f, 20f, 20f));
        casterProxy.SetMethodHandler(nameof(IUnitEntity.GetVisible), args => (uint)args[0] == target.Guid ? target : null);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            PrimaryTargetId = target.Guid,
            SpellInfo       = CreateSpellInfoWithConeTelegraph()
        };

        var spell = CreateSpell(caster, parameters);
        InvokePrivate(spell, "InitialiseTelegraphs");
        InvokePrivate(spell, "SendSpellStart");

        RecordingDispatchProxy<IUnitEntity>.Invocation invocation = Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)));
        ServerSpellStart spellStart = Assert.IsType<ServerSpellStart>(invocation.Arguments[0]);

        ServerSpellStart.InitialPosition initialPosition = Assert.Single(spellStart.InitialPositionData);
        ServerSpellStart.TelegraphPosition telegraphPosition = Assert.Single(spellStart.TelegraphPositionData);

        Assert.Equal(caster.Guid, initialPosition.UnitId);
        Assert.Equal(caster.Guid, telegraphPosition.AttachedUnitId);
        Assert.Equal(caster.Position, telegraphPosition.Position.Vector);
        Assert.Equal(target.Guid, spellStart.PrimaryTargetId);
    }

    [Fact]
    public void CancelCast_WithCreatureTelegraph_FinishesImmediately()
    {
        IUnitEntity caster = CreateUnit(1001u, new Vector3(10f, 20f, 30f), out RecordingDispatchProxy<IUnitEntity> casterProxy, CreateEmptySearchMap());

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfoWithConeTelegraph(castTime: 5000u)
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(CastResult.Ok, spell.Cast());
        Assert.True(spell.BlocksCasting);
        Assert.Contains(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellStart);
        Assert.DoesNotContain(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellFinish);

        spell.CancelCast(CastResult.CasterCannotBeDead);

        Assert.True(spell.IsFinished);
        Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellFinish);
    }

    [Fact]
    public void Execute_KeepsCreatureTelegraphAnchored_WhenCasterMovesBeforeImpact()
    {
        Vector3 castPosition = new(10f, 20f, 30f);
        IUnitEntity caster = CreateUnit(1001u, castPosition, out RecordingDispatchProxy<IUnitEntity> casterProxy, CreateEmptySearchMap());

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfoWithCircleTelegraph()
        };

        var spell = CreateSpell(caster, parameters);
        InvokePrivate(spell, "InitialiseTelegraphs");

        casterProxy.SetProperty(nameof(IUnitEntity.Position), new Vector3(50f, 20f, 30f));
        InvokePrivate(spell, "Execute");

        ITelegraph telegraph = Assert.Single(GetTelegraphs(spell));
        Assert.Equal(castPosition, telegraph.Position);
    }

    [Fact]
    public void Cast_WithCasterCenteredTelegraph_UsesPlayerMovementPositionForImpact()
    {
        Vector3 gridPosition = Vector3.Zero;
        Vector3 movementPosition = new(20f, 0f, 0f);
        IUnitEntity target = CreateUnit(2002u, movementPosition, out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);

        IPlayer caster = CreatePlayer(1001u, gridPosition, out RecordingDispatchProxy<IPlayer> casterProxy, map: CreateSearchMap(target));
        casterProxy.SetProperty(nameof(IWorldEntity.MovementManager), CreateMovementPositionManager(movementPosition));

        int hitCount = 0;
        SpellEffectDelegate damageHandler = (_, selectedTarget, _) =>
        {
            Assert.Same(target, selectedTarget);
            hitCount++;
        };

        ISpellInfo spellInfo = CreateSpellInfoWithCircleTelegraph();
        spellInfo.Effects.Add(new Spell4EffectsEntry
        {
            Id          = 459u,
            SpellId     = spellInfo.Entry.Id,
            TargetFlags = (uint)SpellEffectTargetFlags.Telegraph,
            EffectType  = SpellEffectType.Damage,
            DamageType  = DamageType.Physical
        });

        var spell = CreateSpell(
            caster,
            new NexusForever.Game.Spell.SpellParameters
            {
                SpellInfo = spellInfo
            },
            CreateGlobalSpellManager(damageHandler));

        Assert.Equal(CastResult.Ok, spell.Cast());
        Assert.Equal(1, hitCount);
    }

    [Fact]
    public void IsEffectTargetStillValid_WhenTelegraphTargetMovedOutsideAnchor_ReturnsFalse()
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _, CreateEmptySearchMap());
        IUnitEntity target = CreateUnit(2002u, new Vector3(20f, 0f, 0f), out _);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfoWithCircleTelegraph()
        };

        var spell = CreateSpell(caster, parameters);
        InvokePrivate(spell, "InitialiseTelegraphs");

        var targetInfo = new NexusForever.Game.Spell.SpellTargetInfo(SpellEffectTargetFlags.Telegraph, target);

        Assert.False(spell.IsEffectTargetStillValid(SpellEffectTargetFlags.Telegraph, targetInfo));
    }

    [Fact]
    public void Execute_WithDelayedInitialImpact_BlocksNewCastingUntilImpact()
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _, CreateEmptySearchMap());

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfoWithDelayedInitialImpact()
        };

        var spell = CreateSpell(caster, parameters);

        Assert.False(spell.BlocksCasting);

        InvokePrivate(spell, "Execute");

        Assert.True(spell.BlocksCasting);
    }

    [Fact]
    public void Cast_WithInstantSpell_ExecutesImmediatelyAndStopsBlockingAfterCastReturns()
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> casterProxy, CreateEmptySearchMap());

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfo(127u, 102u)
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(CastResult.Ok, spell.Cast());
        Assert.False(spell.IsCasting);
        Assert.False(spell.BlocksCasting);
        Assert.Contains(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellGo);
    }

    [Fact]
    public void Cast_WithEmbeddedDamage_DoesNotEmitDuplicateStandaloneDamageCombatLog()
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> casterProxy, CreateEmptySearchMap());

        SpellEffectDelegate damageHandler = (spell, target, info) =>
        {
            info.AddDamage(new NexusForever.Game.Spell.SpellTargetInfo.SpellTargetEffectInfo.DamageDescription
            {
                DamageType          = DamageType.Physical,
                RawDamage           = 81u,
                RawScaledDamage     = 81u,
                ShieldAbsorbAmount  = 40u,
                AdjustedDamage      = 41u,
                CombatResult        = CombatResult.Critical
            });
            info.AddCombatLog(new CombatLogDamage
            {
                MitigatedDamage = 41u,
                RawDamage       = 81u,
                Shield          = 40u,
                DamageType      = DamageType.Physical,
                EffectType      = SpellEffectType.Damage,
                CastData        = new CombatLogCastData
                {
                    CasterId     = spell.Caster.Guid,
                    TargetId     = target.Guid,
                    SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                    CombatResult = CombatResult.Critical
                }
            });
        };

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfo(130u, 105u)
        };
        parameters.SpellInfo.Effects.Add(new Spell4EffectsEntry
        {
            Id          = 459u,
            SpellId     = 130u,
            TargetFlags = (uint)SpellEffectTargetFlags.Caster,
            EffectType  = SpellEffectType.Damage,
            DamageType  = DamageType.Physical
        });

        var spell = CreateSpell(caster, parameters, CreateGlobalSpellManager(damageHandler));

        Assert.Equal(CastResult.Ok, spell.Cast());

        IReadOnlyList<RecordingDispatchProxy<IUnitEntity>.Invocation> visibleMessages =
            casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible));
        Assert.DoesNotContain(visibleMessages, i =>
            i.Arguments[0] is ServerCombatLog { CombatLog: CombatLogDamage damage }
            && damage.GetType() == typeof(CombatLogDamage));

        ServerSpellGo spellGo = Assert.IsType<ServerSpellGo>(
            Assert.Single(visibleMessages, i => i.Arguments[0] is ServerSpellGo).Arguments[0]);
        TargetInfo.EffectInfo effectInfo = Assert.Single(Assert.Single(spellGo.TargetInfoData).EffectInfoData);
        Assert.Equal(1, effectInfo.InfoType);
        Assert.Equal(81u, effectInfo.DamageDescriptionData.RawDamage);
        Assert.Equal(40u, effectInfo.DamageDescriptionData.ShieldAbsorbAmount);
        Assert.Equal(41u, effectInfo.DamageDescriptionData.AdjustedDamage);
    }

    [Fact]
    public void Cast_WithCreatureOverrideCastTime_DelaysExecutionUntilOverrideElapsed()
    {
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        targetProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry
        {
            ActivateSpellCastTime = 2000u
        });

        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> casterProxy, CreateEmptySearchMap());
        casterProxy.SetMethodHandler(nameof(IUnitEntity.GetVisible), args => (uint)args[0] == target.Guid ? target : null);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            PrimaryTargetId      = target.Guid,
            UseCreatureOverrides = true,
            SpellInfo            = CreateSpellInfo(129u, 104u)
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(CastResult.Ok, spell.Cast());
        Assert.True(spell.IsCasting);
        Assert.True(spell.BlocksCasting);
        Assert.DoesNotContain(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellGo);

        ServerSpellStart spellStart = Assert.IsType<ServerSpellStart>(
            Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellStart).Arguments[0]);
        Assert.True(spellStart.UseCreatureOverrides);

        spell.Update(1.9d);

        Assert.True(spell.IsCasting);
        Assert.True(spell.BlocksCasting);
        Assert.DoesNotContain(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellGo);

        spell.Update(0.2d);

        Assert.True(spell.IsFinished);
        Assert.Contains(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellGo);
        Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellFinish);
    }

    [Fact]
    public void Cast_WithChannelMaxTime_DelaysFinishUntilChannelCompletes()
    {
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> casterProxy, CreateEmptySearchMap());

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateSpellInfo(128u, 103u, channelMaxTime: 2500u)
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(CastResult.Ok, spell.Cast());
        Assert.True(spell.BlocksCasting);
        Assert.False(spell.IsFinished);
        Assert.DoesNotContain(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellFinish);

        spell.Update(2.4d);

        Assert.True(spell.BlocksCasting);
        Assert.False(spell.IsFinished);
        Assert.DoesNotContain(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellFinish);

        spell.Update(0.2d);

        Assert.False(spell.BlocksCasting);
        Assert.True(spell.IsFinished);
        Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellFinish);
    }

    [Fact]
    public void Cast_WithElectrocuteChannelPulseTuple_DamagesEveryHalfSecond()
    {
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out _);
        IBaseMap map = CreateSearchMap(target);
        IPlayer caster = CreatePlayer(1001u, Vector3.Zero, out _, volatility: 48f, map);

        int damagePulses = 0;
        SpellEffectDelegate damageHandler = (_, _, info) =>
        {
            damagePulses++;
            Assert.Equal(92587u, info.Entry.Id);
            info.AddDamage(new NexusForever.Game.Spell.SpellTargetInfo.SpellTargetEffectInfo.DamageDescription
            {
                DamageType      = DamageType.Tech,
                RawDamage       = 1u,
                RawScaledDamage = 1u,
                AdjustedDamage  = 1u,
                CombatResult    = CombatResult.Hit
            });
        };

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateElectrocuteChannelSpellInfo()
        };

        var spell = CreateSpell(caster, parameters, CreateGlobalSpellManager(damageHandler));

        AssertChannelPulseCadence(spell, () => damagePulses);
    }

    [Fact]
    public void Cast_WithElectrocuteChannelPulseTuple_ConsumesVolatilityCostPerPulse()
    {
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out _);
        IBaseMap map = CreateSearchMap(target);
        IPlayer caster = CreatePlayer(1001u, Vector3.Zero, out RecordingDispatchProxy<IPlayer> casterProxy, volatility: 48f, map);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateElectrocuteChannelSpellInfo()
        };

        var spell = CreateSpell(caster, parameters, CreateGlobalSpellManager((_, _, _) => { }));

        Assert.Equal(CastResult.Ok, spell.Cast());
        Assert.Empty(casterProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital)));

        spell.Update(0.51d);
        Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital)));

        for (int i = 0; i < 5; i++)
            spell.Update(0.5d);

        IReadOnlyList<RecordingDispatchProxy<IPlayer>.Invocation> consumes =
            casterProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital));
        Assert.Equal(6, consumes.Count);
        Assert.All(consumes, consume =>
        {
            Assert.Equal(Vital.Resource1, consume.Arguments[0]);
            Assert.Equal(-8f, consume.Arguments[1]);
        });
        Assert.True(spell.IsFinished);
    }

    [Fact]
    public void Cast_WithElectrocuteChannelPulseTuple_RejectsInsufficientVolatility()
    {
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out _);
        IBaseMap map = CreateSearchMap(target);
        IPlayer caster = CreatePlayer(1001u, Vector3.Zero, out RecordingDispatchProxy<IPlayer> casterProxy, volatility: 7f, map);

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateElectrocuteChannelSpellInfo()
        };

        var spell = CreateSpell(caster, parameters);

        Assert.Equal(CastResult.CasterVitalCostResource1, spell.Cast());
        Assert.Empty(casterProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital)));
    }

    [Fact]
    public void Cast_WithActiveOverdrive_DoesNotRequireOrConsumeKineticChannelPulseCost()
    {
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out _);
        IBaseMap map = CreateSearchMap(target);
        IPlayer caster = CreatePlayer(1001u, Vector3.Zero, out RecordingDispatchProxy<IPlayer> casterProxy, volatility: 0f, map);
        casterProxy.SetMethodHandler(nameof(IUnitEntity.HasTrackedSpellState), args => (uint)args[0] == 42908u);

        int damagePulses = 0;
        SpellEffectDelegate damageHandler = (_, _, _) => damagePulses++;

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateElectrocuteChannelSpellInfo()
        };

        var spell = CreateSpell(caster, parameters, CreateGlobalSpellManager(damageHandler));

        AssertChannelPulseCadence(spell, () => damagePulses);
        Assert.Empty(casterProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital)));
    }

    [Fact]
    public void Cast_WithOverdriveScaleEffect_RestoresScaleAfterDuration()
    {
        IPlayer caster = CreatePlayer(1001u, Vector3.Zero, out RecordingDispatchProxy<IPlayer> casterProxy);
        IMovementManager movementManager = CreateScaleMovementManager(out Func<float> getScale);
        casterProxy.SetProperty(nameof(IWorldEntity.MovementManager), movementManager);

        var scaleStates = new Dictionary<uint, (float PreviousScale, uint RestoreTimeMs)>();
        casterProxy.SetMethodHandler(nameof(IUnitEntity.AddScale), args =>
        {
            scaleStates[(uint)args[0]] = ((float)args[3], (uint)args[4]);
            return null;
        });
        casterProxy.SetMethodHandler(nameof(IUnitEntity.RemoveScale), args =>
        {
            if (!scaleStates.Remove((uint)args[0], out (float PreviousScale, uint RestoreTimeMs) state))
                return false;

            if (state.RestoreTimeMs > 0u)
                movementManager.SetScaleKeys([0u, state.RestoreTimeMs], [movementManager.GetScale(), state.PreviousScale]);
            else
                movementManager.SetScale(state.PreviousScale);

            return true;
        });

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateOverdriveScaleSpellInfo()
        };

        var spell = CreateSpell(caster, parameters, CreateGlobalSpellManager(global::NexusForever.Game.Spell.SpellHandler.HandleEffectScale, SpellEffectType.Scale));

        Assert.Equal(CastResult.Ok, spell.Cast());
        Assert.Equal(1.3f, getScale(), precision: 3);
        Assert.False(spell.IsFinished);

        spell.Update(7.99d);

        Assert.Equal(1.3f, getScale(), precision: 3);
        Assert.False(spell.IsFinished);

        spell.Update(0.02d);

        Assert.Equal(1f, getScale(), precision: 3);
        Assert.True(spell.IsFinished);
    }

    [Fact]
    public void Cast_WithElectrocuteChannelPulseTuple_StopsBeforePulseWhenVolatilityRunsOut()
    {
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out _);
        IBaseMap map = CreateSearchMap(target);
        IPlayer caster = CreatePlayer(1001u, Vector3.Zero, out RecordingDispatchProxy<IPlayer> casterProxy, volatility: 8f, map);

        int damagePulses = 0;
        SpellEffectDelegate damageHandler = (_, _, _) => damagePulses++;

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateElectrocuteChannelSpellInfo()
        };

        var spell = CreateSpell(caster, parameters, CreateGlobalSpellManager(damageHandler));

        Assert.Equal(CastResult.Ok, spell.Cast());

        spell.Update(0.51d);

        Assert.Equal(1, damagePulses);
        Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital)));
        Assert.False(spell.IsFinished);

        spell.Update(0.5d);

        Assert.Equal(1, damagePulses);
        Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital)));
        Assert.True(spell.IsFinished);
    }

    [Fact]
    public void Cast_WithWhirlwindCurrentSpell_CostsKineticPerChannelPulse()
    {
        IPlayer caster = CreatePlayer(1001u, Vector3.Zero, out RecordingDispatchProxy<IPlayer> casterProxy, volatility: 1500f);

        int pulses = 0;
        SpellEffectDelegate proxyHandler = (_, _, info) =>
        {
            pulses++;
            Assert.Equal(99225u, info.Entry.Id);
            Assert.Equal(57586u, info.Entry.DataBits00);
        };

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateWhirlwindCurrentSpellInfo()
        };

        var spell = CreateSpell(caster, parameters, CreateGlobalSpellManager(proxyHandler, SpellEffectType.Proxy));

        Assert.Equal(CastResult.Ok, spell.Cast());
        Assert.Equal(1, pulses);
        Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital)));

        for (int i = 0; i < 5; i++)
            spell.Update(0.5d);

        IReadOnlyList<RecordingDispatchProxy<IPlayer>.Invocation> consumes =
            casterProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital));
        Assert.Equal(6, consumes.Count);
        Assert.All(consumes, consume =>
        {
            Assert.Equal(Vital.Resource1, consume.Arguments[0]);
            Assert.Equal(-250f, consume.Arguments[1]);
        });
        Assert.Equal(6, pulses);
        Assert.True(spell.IsFinished);
    }

    [Fact]
    public void Cast_WithRampageCurrentSpell_CostsKineticOnce()
    {
        IPlayer caster = CreatePlayer(1001u, Vector3.Zero, out RecordingDispatchProxy<IPlayer> casterProxy, volatility: 500f);

        int proxyCasts = 0;
        SpellEffectDelegate proxyHandler = (_, _, info) =>
        {
            proxyCasts++;
            Assert.Equal(177473u, info.Entry.Id);
            Assert.Equal(70146u, info.Entry.DataBits00);
        };

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateRampageCurrentSpellInfo()
        };

        var spell = CreateSpell(caster, parameters, CreateGlobalSpellManager(proxyHandler, SpellEffectType.Proxy));

        Assert.Equal(CastResult.Ok, spell.Cast());

        IReadOnlyList<RecordingDispatchProxy<IPlayer>.Invocation> consumes =
            casterProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital));
        RecordingDispatchProxy<IPlayer>.Invocation consume = Assert.Single(consumes);
        Assert.Equal(Vital.Resource1, consume.Arguments[0]);
        Assert.Equal(-250f, consume.Arguments[1]);
        Assert.Equal(0, proxyCasts);

        spell.Update(0.34d);

        Assert.Equal(1, proxyCasts);
        Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital)));
        Assert.True(spell.IsFinished);
    }

    [Theory]
    [InlineData(SpellEffectType.VitalModifier)]
    [InlineData(SpellEffectType.Transference)]
    [InlineData(SpellEffectType.Damage)]
    [InlineData(SpellEffectType.Heal)]
    [InlineData(SpellEffectType.DistanceDependentDamage)]
    [InlineData(SpellEffectType.ProxyLinearAE)]
    [InlineData(SpellEffectType.ProxyChannel)]
    [InlineData(SpellEffectType.Proxy)]
    [InlineData(SpellEffectType.ProxyRandomExclusive)]
    [InlineData(SpellEffectType.Absorption)]
    [InlineData(SpellEffectType.SapVital)]
    [InlineData(SpellEffectType.DistributedDamage)]
    [InlineData(SpellEffectType.ProxyChannelVariableTime)]
    [InlineData(SpellEffectType.HealShields)]
    [InlineData(SpellEffectType.DamageShields)]
    [InlineData(SpellEffectType.HealingAbsorption)]
    public void Cast_WithChannelPulseDirectVitalCombatEffect_RepeatsAtChannelPulseTime(SpellEffectType effectType)
    {
        IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out _);
        IBaseMap map = CreateSearchMap(target);
        IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _, map);

        int pulses = 0;
        uint effectId = 90000u + (uint)effectType;
        SpellEffectDelegate handler = (_, _, info) =>
        {
            pulses++;
            Assert.Equal(effectId, info.Entry.Id);
            Assert.Equal(effectType, info.Entry.EffectType);
        };

        var parameters = new NexusForever.Game.Spell.SpellParameters
        {
            SpellInfo = CreateChannelPulseSpellInfo(80000u + (uint)effectType, 70000u + (uint)effectType, effectId, effectType)
        };

        var spell = CreateSpell(caster, parameters, CreateGlobalSpellManager(handler, effectType));

        AssertChannelPulseCadence(spell, () => pulses);
    }

    [Theory]
    [InlineData(5u, PrerequisiteComparison.LessThan, 6u, true)]
    [InlineData(8u, PrerequisiteComparison.LessThan, 6u, false)]
    [InlineData(8u, PrerequisiteComparison.GreaterThan, 7u, true)]
    [InlineData(5u, PrerequisiteComparison.GreaterThan, 7u, false)]
    public void MeetsApplyPrerequisite_CreatureDifficulty_UsesCreatureDifficultyId(uint creatureDifficultyId, PrerequisiteComparison comparison, uint requiredDifficultyId, bool expected)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Creature2DifficultyId = creatureDifficultyId });

        var prerequisite = new PrerequisiteEntry
        {
            Flags = EvaluationMode.EvaluateAND,
            PrerequisiteTypeId = [PrerequisiteType.CreatureDifficulty, PrerequisiteType.None, PrerequisiteType.None],
            PrerequisiteComparisonId = [comparison, 0, 0],
            ObjectId = [requiredDifficultyId, 0u, 0u],
            Value = [0u, 0u, 0u]
        };

        bool result = NexusForever.Game.Spell.Spell.MeetsApplyPrerequisite(prerequisite, entity);

        Assert.Equal(expected, result);
    }

    private static NexusForever.Game.Spell.Spell CreateSpell(IUnitEntity caster, ISpellParameters parameters, IGlobalSpellManager globalSpellManager = null)
    {
        return new NexusForever.Game.Spell.Spell(
            caster,
            parameters,
            globalSpellManager: globalSpellManager ?? CreateGlobalSpellManager(),
            scriptManager: CreateScriptManager(),
            gameTableManager: CreateGameTableManager());
    }

    private static IGlobalSpellManager CreateGlobalSpellManager(SpellEffectDelegate effectHandler = null, params SpellEffectType[] handledEffectTypes)
    {
        IGlobalSpellManager globalSpellManager = RecordingDispatchProxy<IGlobalSpellManager>.Create(out RecordingDispatchProxy<IGlobalSpellManager> proxy);
        proxy.SetProperty(nameof(IGlobalSpellManager.NextCastingId), 1u);
        proxy.SetProperty(nameof(IGlobalSpellManager.NextEffectId), 1u);
        if (effectHandler != null)
        {
            HashSet<SpellEffectType> handledTypes = handledEffectTypes.Length == 0
                ? [SpellEffectType.Damage]
                : handledEffectTypes.ToHashSet();
            proxy.SetMethodHandler(nameof(IGlobalSpellManager.GetEffectHandler), args => handledTypes.Contains((SpellEffectType)args[0]) ? effectHandler : null);
        }

        return globalSpellManager;
    }

    private static IGameTableManager CreateGameTableManager()
    {
        return RecordingDispatchProxy<IGameTableManager>.Create(out _);
    }

    private static IScriptManager CreateScriptManager()
    {
        IScriptCollection scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out RecordingDispatchProxy<IScriptManager> scriptManagerProxy);
        scriptManagerProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);
        return scriptManager;
    }

    private static IUnitEntity CreateUnit(uint guid, Vector3 position, out RecordingDispatchProxy<IUnitEntity> proxy, IBaseMap map = null)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out proxy);
        proxy.SetProperty(nameof(IUnitEntity.Guid), guid);
        proxy.SetProperty(nameof(IUnitEntity.Position), position);
        proxy.SetProperty(nameof(IUnitEntity.Rotation), Vector3.Zero);
        proxy.SetProperty(nameof(IUnitEntity.HitRadius), 1f);
        proxy.SetProperty(nameof(IUnitEntity.Map), map);
        return unit;
    }

    private static IWorldEntity CreateWorldObject(uint guid, EntityType entityType)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> proxy);
        proxy.SetProperty(nameof(IWorldEntity.Guid), guid);
        proxy.SetProperty(nameof(IWorldEntity.Type), entityType);
        proxy.SetProperty(nameof(IWorldEntity.Position), Vector3.Zero);
        return entity;
    }

    private static IBaseMap CreateEmptySearchMap()
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> proxy);
        proxy.SetMethodHandler(nameof(IBaseMap.Search), _ => Array.Empty<IUnitEntity>());
        return map;
    }

    private static IBaseMap CreateSearchMap(params IUnitEntity[] targets)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> proxy);
        proxy.SetMethodHandler(nameof(IBaseMap.Search), _ => targets);
        return map;
    }

    private static IMovementManager CreateMovementPositionManager(Vector3 position)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> proxy);
        proxy.SetMethodReturn(nameof(IMovementManager.GetPosition), position);
        return movementManager;
    }

    private static IMovementManager CreateScaleMovementManager(out Func<float> getScale)
    {
        float currentScale = 1f;
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> proxy);
        proxy.SetMethodHandler(nameof(IMovementManager.GetScale), _ => currentScale);
        proxy.SetMethodHandler(nameof(IMovementManager.SetScale), args =>
        {
            currentScale = (float)args[0];
            return null;
        });
        proxy.SetMethodHandler(nameof(IMovementManager.SetScaleKeys), args =>
        {
            currentScale = ((List<float>)args[1])[^1];
            return null;
        });

        getScale = () => currentScale;
        return movementManager;
    }

    private static void AssertChannelPulseCadence(NexusForever.Game.Spell.Spell spell, Func<int> pulseCount)
    {
        Assert.Equal(CastResult.Ok, spell.Cast());
        Assert.Equal(0, pulseCount());
        Assert.True(spell.BlocksCasting);

        spell.Update(0.49d);

        Assert.Equal(0, pulseCount());

        spell.Update(0.02d);

        Assert.Equal(1, pulseCount());
        Assert.True(spell.BlocksCasting);

        for (int i = 0; i < 5; i++)
            spell.Update(0.5d);

        Assert.Equal(6, pulseCount());
        Assert.True(spell.IsFinished);
    }

    private static IPlayer CreatePlayer(uint guid, Vector3 position)
    {
        return CreatePlayer(guid, position, out _, null, null);
    }

    private static IPlayer CreatePlayer(uint guid, Vector3 position, out RecordingDispatchProxy<IPlayer> proxy, float? volatility = null, IBaseMap map = null)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out proxy);
        proxy.SetProperty(nameof(IPlayer.Guid), guid);
        proxy.SetProperty(nameof(IPlayer.Position), position);
        proxy.SetProperty(nameof(IPlayer.Rotation), Vector3.Zero);
        proxy.SetProperty(nameof(IPlayer.HitRadius), 1f);
        proxy.SetProperty(nameof(IPlayer.Map), map);
        proxy.SetProperty(nameof(IPlayer.IsLoading), true);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellCooldown), 0d);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetGlobalSpellCooldown), 0d);
        proxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        if (volatility.HasValue)
            ConfigureVolatility(proxy, volatility.Value);

        return player;
    }

    private static void ConfigureVolatility(RecordingDispatchProxy<IPlayer> proxy, float volatility)
    {
        float currentVolatility = volatility;
        proxy.SetMethodHandler(nameof(IUnitEntity.TryGetVitalValue), args =>
        {
            Vital vital = (Vital)args[0];
            if (vital is Vital.Resource1 or Vital.Volatility)
            {
                args[1] = currentVolatility;
                return true;
            }

            args[1] = 0f;
            return false;
        });
        proxy.SetMethodHandler(nameof(IUnitEntity.TryModifyVital), args =>
        {
            Vital vital = (Vital)args[0];
            if (vital is Vital.Resource1 or Vital.Volatility)
            {
                float oldValue = currentVolatility;
                currentVolatility = Math.Max(0f, currentVolatility + (float)args[1]);
                args[2] = currentVolatility - oldValue;
                return true;
            }

            args[2] = 0f;
            return false;
        });
    }

    private static ISpellInfo CreateSpellInfoWithConeTelegraph(uint castTime = 0u)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 99u });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 123u, Spell4BaseIdBaseSpell = 99u, CastTime = castTime });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>
        {
            new()
            {
                Id              = 456u,
                DamageShapeEnum = (uint)DamageShape.Cone,
                Param01         = 10f,
                Param02         = 60f
            }
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());

        return spellInfo;
    }

    private static ISpellInfo CreateSpellInfoWithCircleTelegraph()
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 100u });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 124u, Spell4BaseIdBaseSpell = 100u });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>
        {
            new()
            {
                Id              = 457u,
                DamageShapeEnum = (uint)DamageShape.Circle,
                Param00         = 5f
            }
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());

        return spellInfo;
    }

    private static ISpellInfo CreateSpellInfoWithValidTargetMask(uint targetBitmask, TargetGroupEntry castGroup = null)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 101u });
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.ValidTargets), new Spell4ValidTargetsEntry { TargetBitmask = targetBitmask });
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.CastGroup), castGroup);

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 126u, Spell4BaseIdBaseSpell = 101u });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>());
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());

        return spellInfo;
    }

    private static ISpellInfo CreateSpellInfo(uint spellId, uint baseId, uint castTime = 0u, uint channelMaxTime = 0u)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = baseId });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id                   = spellId,
            Spell4BaseIdBaseSpell = baseId,
            CastTime             = castTime,
            ChannelMaxTime       = channelMaxTime
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>());
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());

        return spellInfo;
    }

    private static ISpellInfo CreateElectrocuteChannelSpellInfo()
    {
        return CreateChannelPulseSpellInfo(
            41276u,
            25473u,
            92587u,
            SpellEffectType.Damage,
            (uint)Vital.Resource1,
            8u);
    }

    private static ISpellInfo CreateWhirlwindCurrentSpellInfo()
    {
        return CreateProxySpellInfo(
            33802u,
            19778u,
            99225u,
            57586u,
            channelMaxTime: 2500u,
            channelPulseTime: 500u);
    }

    private static ISpellInfo CreateRampageCurrentSpellInfo()
    {
        return CreateProxySpellInfo(
            58533u,
            37968u,
            177473u,
            70146u,
            delayTime: 333u);
    }

    private static ISpellInfo CreateOverdriveScaleSpellInfo()
    {
        ISpellInfo spellInfo = CreateSpellInfo(42908u, 25473u);
        spellInfo.Effects.Add(new Spell4EffectsEntry
        {
            Id           = 109482u,
            SpellId      = 42908u,
            TargetFlags  = (uint)SpellEffectTargetFlags.Caster,
            EffectType   = SpellEffectType.Scale,
            DurationTime = 8000u,
            DataBits00   = BitConverter.SingleToUInt32Bits(1.3f),
            DataBits01   = 500u,
            DataBits02   = 1000u
        });

        return spellInfo;
    }

    private static ISpellInfo CreateProxySpellInfo(uint spellId, uint baseSpellId, uint effectId, uint proxySpellId, uint channelMaxTime = 0u, uint channelPulseTime = 0u, uint delayTime = 0u)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = baseSpellId });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id                     = spellId,
            Spell4BaseIdBaseSpell = baseSpellId,
            ChannelMaxTime        = channelMaxTime,
            ChannelPulseTime      = channelPulseTime
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>());
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>
        {
            new()
            {
                Id          = effectId,
                SpellId     = spellId,
                TargetFlags = (uint)SpellEffectTargetFlags.Caster,
                EffectType  = SpellEffectType.Proxy,
                DelayTime   = delayTime,
                DataBits00  = proxySpellId
            }
        });

        return spellInfo;
    }

    private static ISpellInfo CreateChannelPulseSpellInfo(uint spellId, uint baseSpellId, uint effectId, SpellEffectType effectType, uint innateCostType = 0u, uint innateCost = 0u)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = baseSpellId });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id                     = spellId,
            Spell4BaseIdBaseSpell = baseSpellId,
            ChannelInitialDelay   = 500u,
            ChannelMaxTime        = 3000u,
            ChannelPulseTime      = 500u,
            InnateCostType0       = innateCostType,
            InnateCost0           = innateCost
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>
        {
            new()
            {
                Id              = 1u,
                DamageShapeEnum = (uint)DamageShape.Circle,
                Param00         = 5f
            }
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>
        {
            new()
            {
                Id          = effectId,
                SpellId     = spellId,
                TargetFlags = (uint)SpellEffectTargetFlags.Telegraph,
                EffectType  = effectType,
                DamageType  = DamageType.Tech
            }
        });

        return spellInfo;
    }

    private static ISpellInfo CreateSpellInfoWithDelayedInitialImpact()
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 101u });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 125u, Spell4BaseIdBaseSpell = 101u });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>());
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>
        {
            new()
            {
                Id          = 458u,
                TargetFlags = (uint)SpellEffectTargetFlags.Caster,
                EffectType  = SpellEffectType.Fluff,
                DelayTime   = 500u
            }
        });

        return spellInfo;
    }

    private static IReadOnlyList<ITelegraph> GetTelegraphs(NexusForever.Game.Spell.Spell spell)
    {
        FieldInfo field = typeof(NexusForever.Game.Spell.Spell).GetField("telegraphs", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<IReadOnlyList<ITelegraph>>(field.GetValue(spell));
    }

    private static void InvokePrivate(object instance, string methodName)
    {
        MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(instance, null);
    }

    private static T InvokePrivate<T>(object instance, string methodName, params object[] arguments)
    {
        MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<T>(method.Invoke(instance, arguments));
    }
}
