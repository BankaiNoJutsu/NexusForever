using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Spell;

[Collection(LegacyServiceProviderCollection.Name)]
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
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _);
            IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> targetProxy);
            targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), targetAlive);

            var parameters = new NexusForever.Game.Spell.SpellParameters
            {
                SpellInfo = CreateSpellInfoWithValidTargetMask(targetBitmask)
            };

            var spell = new NexusForever.Game.Spell.Spell(caster, parameters);

            Assert.Equal(expected, InvokePrivate<CastResult>(spell, "CheckPrimaryTargetValidMask", target));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CheckPrimaryTargetValidMask_AllowsObjectOnlyBitForSimpleEntityTargets()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _);
            IUnitEntity target = CreateUnit(2002u, Vector3.Zero, out RecordingDispatchProxy<IUnitEntity> targetProxy);
            targetProxy.SetProperty(nameof(IWorldEntity.Type), EntityType.Simple);
            targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);

            var parameters = new NexusForever.Game.Spell.SpellParameters
            {
                SpellInfo = CreateSpellInfoWithValidTargetMask(0x02u)
            };

            var spell = new NexusForever.Game.Spell.Spell(caster, parameters);

            Assert.Equal(CastResult.Ok, InvokePrivate<CastResult>(spell, "CheckPrimaryTargetValidMask", target));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void SendSpellStart_AttachesCreatureTelegraphToCaster_WhenPrimaryTargetExists()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            IUnitEntity caster = CreateUnit(1001u, new Vector3(10f, 20f, 30f), out RecordingDispatchProxy<IUnitEntity> casterProxy);
            IPlayer target = CreatePlayer(2002u, new Vector3(10f, 20f, 20f));
            casterProxy.SetMethodHandler(nameof(IUnitEntity.GetVisible), args => (uint)args[0] == target.Guid ? target : null);

            var parameters = new NexusForever.Game.Spell.SpellParameters
            {
                PrimaryTargetId = target.Guid,
                SpellInfo       = CreateSpellInfoWithConeTelegraph()
            };

            var spell = new NexusForever.Game.Spell.Spell(caster, parameters);
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
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void Execute_KeepsCreatureTelegraphAnchored_WhenCasterMovesBeforeImpact()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            Vector3 castPosition = new(10f, 20f, 30f);
            IUnitEntity caster = CreateUnit(1001u, castPosition, out RecordingDispatchProxy<IUnitEntity> casterProxy, CreateEmptySearchMap());

            var parameters = new NexusForever.Game.Spell.SpellParameters
            {
                SpellInfo = CreateSpellInfoWithCircleTelegraph()
            };

            var spell = new NexusForever.Game.Spell.Spell(caster, parameters);
            InvokePrivate(spell, "InitialiseTelegraphs");

            casterProxy.SetProperty(nameof(IUnitEntity.Position), new Vector3(50f, 20f, 30f));
            InvokePrivate(spell, "Execute");

            ITelegraph telegraph = Assert.Single(GetTelegraphs(spell));
            Assert.Equal(castPosition, telegraph.Position);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void IsEffectTargetStillValid_WhenTelegraphTargetMovedOutsideAnchor_ReturnsFalse()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _, CreateEmptySearchMap());
            IUnitEntity target = CreateUnit(2002u, new Vector3(20f, 0f, 0f), out _);

            var parameters = new NexusForever.Game.Spell.SpellParameters
            {
                SpellInfo = CreateSpellInfoWithCircleTelegraph()
            };

            var spell = new NexusForever.Game.Spell.Spell(caster, parameters);
            InvokePrivate(spell, "InitialiseTelegraphs");

            var targetInfo = new NexusForever.Game.Spell.SpellTargetInfo(SpellEffectTargetFlags.Telegraph, target);

            Assert.False(spell.IsEffectTargetStillValid(SpellEffectTargetFlags.Telegraph, targetInfo));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void Execute_WithDelayedInitialImpact_BlocksNewCastingUntilImpact()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            IUnitEntity caster = CreateUnit(1001u, Vector3.Zero, out _, CreateEmptySearchMap());

            var parameters = new NexusForever.Game.Spell.SpellParameters
            {
                SpellInfo = CreateSpellInfoWithDelayedInitialImpact()
            };

            var spell = new NexusForever.Game.Spell.Spell(caster, parameters);

            Assert.False(spell.BlocksCasting);

            InvokePrivate(spell, "Execute");

            Assert.True(spell.BlocksCasting);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
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

    private static IServiceProvider BuildProvider()
    {
        IScriptCollection scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out RecordingDispatchProxy<IScriptManager> scriptManagerProxy);
        scriptManagerProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);

        return new ServiceCollection()
            .AddSingleton<NexusForever.Game.Spell.GlobalSpellManager>()
            .AddSingleton(scriptManager)
            .BuildServiceProvider();
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

    private static IBaseMap CreateEmptySearchMap()
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> proxy);
        proxy.SetMethodHandler(nameof(IBaseMap.Search), _ => Array.Empty<IUnitEntity>());
        return map;
    }

    private static IPlayer CreatePlayer(uint guid, Vector3 position)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> proxy);
        proxy.SetProperty(nameof(IPlayer.Guid), guid);
        proxy.SetProperty(nameof(IPlayer.Position), position);
        proxy.SetProperty(nameof(IPlayer.Rotation), Vector3.Zero);
        proxy.SetProperty(nameof(IPlayer.HitRadius), 1f);
        return player;
    }

    private static ISpellInfo CreateSpellInfoWithConeTelegraph()
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 99u });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 123u, Spell4BaseIdBaseSpell = 99u });
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

    private static ISpellInfo CreateSpellInfoWithValidTargetMask(uint targetBitmask)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 101u });
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.ValidTargets), new Spell4ValidTargetsEntry { TargetBitmask = targetBitmask });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 126u, Spell4BaseIdBaseSpell = 101u });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());

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
