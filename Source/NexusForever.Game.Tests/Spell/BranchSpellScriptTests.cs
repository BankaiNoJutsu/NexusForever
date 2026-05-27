using System.Collections.Generic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Spells;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Spell;

public class BranchSpellScriptTests
{
    [Fact]
    public void EngineerPulseBlast_OnExecute_WithHostileTarget_CastsHiddenVolatility()
    {
        ISpellInfo sourceSpellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out _);
        ISpellInfo rootSpellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out _);
        ISpellParameters sourceParameters = CreateSpellParameters(sourceSpellInfo, rootSpellInfo, 77u);
        ISpellParameters hiddenParameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> hiddenParametersProxy);
        IUnitEntity caster = CreateUnit(10u, out RecordingDispatchProxy<IUnitEntity> casterProxy);
        IUnitEntity hostile = CreateUnit(20u, out _);
        ISpell spell = CreateSpell(caster, sourceParameters);

        casterProxy.SetMethodHandler(nameof(IUnitEntity.CanAttack), args => ReferenceEquals(args[0], hostile));

        var script = new EngineerPulseBlastSpellScript(CreateSpellParametersFactory(hiddenParameters));
        script.OnLoad(spell);
        script.OnExecute(spell, new List<ISpellTargetInfo>
        {
            CreateTarget(caster),
            CreateTarget(hostile)
        });

        RecordingDispatchProxy<IUnitEntity>.Invocation cast = Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Equal(42148u, (uint)cast.Arguments[0]);
        Assert.Same(hiddenParameters, cast.Arguments[1]);

        Assert.Contains(hiddenParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.ParentSpellInfo)), i => ReferenceEquals(i.Arguments[0], sourceSpellInfo));
        Assert.Contains(hiddenParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.RootSpellInfo)), i => ReferenceEquals(i.Arguments[0], rootSpellInfo));
        Assert.Contains(hiddenParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.PrimaryTargetId)), i => (uint)i.Arguments[0] == 10u);
        Assert.Contains(hiddenParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.UserInitiatedSpellCast)), i => !(bool)i.Arguments[0]);
        Assert.Contains(hiddenParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.IgnoreGlobalCooldown)), i => (bool)i.Arguments[0]);
        Assert.Contains(hiddenParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.ClientContextToken)), i => (uint)i.Arguments[0] == 77u);
        Assert.Contains(hiddenParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.ClientRequestSource)), i => (string)i.Arguments[0] == nameof(EngineerPulseBlastSpellScript));
    }

    [Fact]
    public void EngineerPulseBlast_OnExecute_WithoutHostileTarget_DoesNotCastHiddenVolatility()
    {
        IUnitEntity caster = CreateUnit(10u, out RecordingDispatchProxy<IUnitEntity> casterProxy);
        IUnitEntity friendly = CreateUnit(20u, out _);
        ISpell spell = CreateSpell(caster, CreateSpellParameters());

        casterProxy.SetMethodReturn(nameof(IUnitEntity.CanAttack), false);

        var script = new EngineerPulseBlastSpellScript(CreateSpellParametersFactory(RecordingDispatchProxy<ISpellParameters>.Create(out _)));
        script.OnLoad(spell);
        script.OnExecute(spell, new List<ISpellTargetInfo>
        {
            CreateTarget(caster),
            CreateTarget(friendly)
        });

        Assert.Empty(casterProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
    }

    [Fact]
    public void MarauderMineExplosion_OnFinish_DestroysMineForCompletedCast()
    {
        ICreatureEntity mine = CreateCreature(16718u, maxHealth: 75u, out RecordingDispatchProxy<ICreatureEntity> mineProxy);
        ISpell spell = CreateSpell(mine, CreateSpellParameters());
        var script = new MarauderMineExplosionSpellScript();

        script.OnLoad(spell);
        script.OnFinish(spell, cancelled: false);

        RecordingDispatchProxy<ICreatureEntity>.Invocation destroy = Assert.Single(mineProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(75u, destroy.Arguments[0]);
        Assert.Equal(DamageType.Physical, destroy.Arguments[1]);
        Assert.Null(destroy.Arguments[2]);
    }

    [Fact]
    public void MarauderMineExplosion_OnFinish_WhenCancelled_DoesNotDestroyMine()
    {
        ICreatureEntity mine = CreateCreature(16718u, maxHealth: 75u, out RecordingDispatchProxy<ICreatureEntity> mineProxy);
        ISpell spell = CreateSpell(mine, CreateSpellParameters());
        var script = new MarauderMineExplosionSpellScript();

        script.OnLoad(spell);
        script.OnFinish(spell, cancelled: true);

        Assert.Empty(mineProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    private static ISpell CreateSpell(IUnitEntity caster, ISpellParameters parameters)
    {
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Caster), caster);
        spellProxy.SetProperty(nameof(ISpell.Parameters), parameters);
        return spell;
    }

    private static IUnitEntity CreateUnit(uint guid, out RecordingDispatchProxy<IUnitEntity> proxy)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out proxy);
        proxy.SetProperty(nameof(IWorldEntity.Guid), guid);
        return unit;
    }

    private static ICreatureEntity CreateCreature(uint creatureId, uint maxHealth, out RecordingDispatchProxy<ICreatureEntity> proxy)
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        proxy.SetProperty(nameof(IWorldEntity.MaxHealth), maxHealth);
        return creature;
    }

    private static ISpellTargetInfo CreateTarget(IWorldEntity entity)
    {
        ISpellTargetInfo target = RecordingDispatchProxy<ISpellTargetInfo>.Create(out RecordingDispatchProxy<ISpellTargetInfo> proxy);
        proxy.SetProperty(nameof(ISpellTargetInfo.Entity), entity);
        return target;
    }

    private static ISpellParameters CreateSpellParameters(ISpellInfo spellInfo = null, ISpellInfo rootSpellInfo = null, uint clientContextToken = 0u)
    {
        ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> proxy);
        proxy.SetProperty(nameof(ISpellParameters.SpellInfo), spellInfo);
        proxy.SetProperty(nameof(ISpellParameters.RootSpellInfo), rootSpellInfo);
        proxy.SetProperty(nameof(ISpellParameters.ClientContextToken), clientContextToken);
        return parameters;
    }

    private static IFactory<ISpellParameters> CreateSpellParametersFactory(ISpellParameters spellParameters)
    {
        IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
        factoryProxy.SetMethodReturn(nameof(IFactory<ISpellParameters>.Resolve), spellParameters);
        return factory;
    }
}
