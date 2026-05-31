using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Dungeon.ProtogamesAcademy.Script;
using NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script;
using NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script;
using NexusForever.Script.Instance.Dungeon.Skullcano.Script;
using NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script;
using NexusForever.Script.Instance.Raid.Datascape.Script;
using NexusForever.Script.Instance.Raid.GeneticArchives.Script;
using NexusForever.Script.Instance.Raid.RedMoonTerror.Script;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using DatascapeObjective = NexusForever.Script.Instance.Raid.Datascape.PublicEventObjective;
using GeneticObjective = NexusForever.Script.Instance.Raid.GeneticArchives.PublicEventObjective;
using ProtogamesObjective = NexusForever.Script.Instance.Dungeon.ProtogamesAcademy.PublicEventObjective;
using RedMoonObjective = NexusForever.Script.Instance.Raid.RedMoonTerror.PublicEventObjective;
using RuinsObjective = NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.PublicEventObjective;
using SanctuaryObjective = NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.PublicEventObjective;
using SkullcanoObjective = NexusForever.Script.Instance.Dungeon.Skullcano.PublicEventObjective;
using StormtalonObjective = NexusForever.Script.Instance.Dungeon.StormtalonsLair.PublicEventObjective;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Instances;

public class PublicEventObjectiveCreditEntityScriptTests
{
    public static IEnumerable<object[]> ObjectiveCreditScripts()
    {
        yield return ScriptCase((f, g) => new InvulnotronEntityScript(f, g), ProtogamesObjective.DefeatInvulnotron);
        yield return ScriptCase((f, g) => new GromkaEntityScript(f, g), ProtogamesObjective.DefeatGromka);
        yield return ScriptCase((f, g) => new IrukiBoldbeardEntityScript(f, g), ProtogamesObjective.DefeatIrukiBoldbeard);
        yield return ScriptCase((f, g) => new SeekNSlaughterEntityScript(f, g), ProtogamesObjective.DefeatSeekNSlaughter);
        yield return ScriptCase((f, g) => new IceboxMk2EntityScript(f, g), ProtogamesObjective.DefeatIceboxMk2);
        yield return ScriptCase((f, g) => new GrondTheCorpsemakerEntityScript(f, g), RuinsObjective.DefeatGrondTheCorpsemaker);
        yield return ScriptCase((f, g) => new SlavemasterDrokkEntityScript(f, g), RuinsObjective.DefeatSlavemasterDrokk);
        yield return ScriptCase((f, g) => new ForgemasterTrogunEntityScript(f, g), RuinsObjective.DefeatForgemasterTrogun);
        yield return ScriptCase((f, g) => new FlameCrazedDemonEntityScript(f, g), SanctuaryObjective.DestroyTheFlameCrazedDemon);
        yield return ScriptCase((f, g) => new ThunderfootNormalEntityScript(f, g), SkullcanoObjective.DefeatThunderfoot);
        yield return ScriptCase((f, g) => new StewShamanTuggaNormalEntityScript(f, g), SkullcanoObjective.DefeatStewShamanTugga);
        yield return ScriptCase((f, g) => new BladeWindTheInvokerVeteranEntityScript(f, g), StormtalonObjective.DefeatBladeWindTheInvoker);
        yield return ScriptCase((f, g) => new AethrosVeteranEntityScript(f, g), StormtalonObjective.EliminateAethros);
        yield return ScriptCase((f, g) => new OptimizedMemoryProbeED1EntityScript(f, g), DatascapeObjective.DefeatOptimizedMemoryProbeED1);
        yield return ScriptCase((f, g) => new OptimizedMemoryProbeP2ZEntityScript(f, g), DatascapeObjective.DefeatOptimizedMemoryProbeP2Z);
        yield return ScriptCase((f, g) => new OptimizedMemoryProbeTX67EntityScript(f, g), DatascapeObjective.DefeatOptimizedMemoryProbeTX67);
        yield return ScriptCase((f, g) => new NullSystemDaemonEntityScript(f, g), DatascapeObjective.DefeatTheSystemDaemons);
        yield return ScriptCase((f, g) => new BinarySystemDaemonEntityScript(f, g), DatascapeObjective.DefeatTheSystemDaemons);
        yield return ScriptCase((f, g) => new DatascapeAvatusEntityScript(f, g), DatascapeObjective.DefeatAvatus);
        yield return ScriptCase((f, g) => new FrostBoulderAvalancheFirstEntityScript(f, g), DatascapeObjective.DefeatTheFirstFrostBoulderAvalanche);
        yield return ScriptCase((f, g) => new FrostBoulderAvalancheSecondEntityScript(f, g), DatascapeObjective.DefeatTheSecondFrostBoulderAvalanche);
        yield return ScriptCase((f, g) => new FrostbringerWarlockEntityScript(f, g), DatascapeObjective.DefeatTheFrostbringerWarlock);
        yield return ScriptCase((f, g) => new BioEnhancedBroodmotherEntityScript(f, g), DatascapeObjective.DefeatTheBioEnhancedBroodmother);
        yield return ScriptCase((f, g) => new GloomclawEntityScript(f, g), DatascapeObjective.DefeatGloomclaw);
        yield return ScriptCase((f, g) => new HyperAcceleratedSkeledroidEntityScript(f, g), DatascapeObjective.DefeatTheHyperAcceleratedSkeledroid);
        yield return ScriptCase((f, g) => new AugmentedHeraldOfAvatusEntityScript(f, g), DatascapeObjective.DefeatTheAugmentedHeraldOfAvatus);
        yield return ScriptCase((f, g) => new WarmongerAgrathaEntityScript(f, g), DatascapeObjective.DefeatWarmongerAgratha);
        yield return ScriptCase((f, g) => new WarmongerChunaEntityScript(f, g), DatascapeObjective.DefeatWarmongerChuna);
        yield return ScriptCase((f, g) => new WarmongerTalariiEntityScript(f, g), DatascapeObjective.DefeatWarmongerTalarii);
        yield return ScriptCase((f, g) => new GrandWarmongerTargreshEntityScript(f, g), DatascapeObjective.DefeatGrandWarmongerTargresh);
        yield return ScriptCase((f, g) => new ExperimentX89EntityScript(f, g), GeneticObjective.DefeatExperimentX89);
        yield return ScriptCase((f, g) => new KuralakTheDefilerEntityScript(f, g), GeneticObjective.DefeatKuralakTheDefiler);
        yield return ScriptCase((f, g) => new LavekaTheDarkHeartedEntityScript(f, g), RedMoonObjective.DefeatLavekaTheDarkHearted);
    }

    [Theory]
    [MemberData(nameof(ObjectiveCreditScripts))]
    public void OnDeath_UpdatesMappedPublicEventObjective(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        uint expectedObjectiveId)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> managerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(IWorldEntity.CreatureId), 1u);
        creatureProxy.SetProperty(nameof(IGridEntity.Map), map);

        PublicEventObjectiveCreditEntityScript script = createScript(CreateSpellParametersFactory(), RecordingDispatchProxy<IGameTableManager>.Create(out _));
        script.OnLoad(creature);
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            managerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 2);
        Assert.Equal(expectedObjectiveId, (uint)update.Arguments[0]);
        Assert.Equal(1, (int)update.Arguments[1]);
    }

    [Theory]
    [MemberData(nameof(ObjectiveCreditScripts))]
    public void ObjectiveCreditScripts_RequireScriptNameFilter(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        uint expectedObjectiveId)
    {
        _ = expectedObjectiveId;

        PublicEventObjectiveCreditEntityScript script = createScript(CreateSpellParametersFactory(), RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ScriptFilterScriptNameAttribute attribute = script.GetType().GetCustomAttribute<ScriptFilterScriptNameAttribute>();

        Assert.NotNull(attribute);
        Assert.False(string.IsNullOrWhiteSpace(attribute.ScriptName));
    }

    [Theory]
    [MemberData(nameof(ObjectiveCreditScripts))]
    public void ObjectiveCreditScripts_DoNotInheritCombatAI(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        uint expectedObjectiveId)
    {
        _ = expectedObjectiveId;

        PublicEventObjectiveCreditEntityScript script = createScript(CreateSpellParametersFactory(), RecordingDispatchProxy<IGameTableManager>.Create(out _));

        Assert.False(typeof(CombatAI).IsAssignableFrom(script.GetType()));
    }

    [Theory]
    [MemberData(nameof(ObjectiveCreditScripts))]
    public void ObjectiveCreditScripts_OnlyMatchExplicitScriptNameSearch(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        uint expectedObjectiveId)
    {
        _ = expectedObjectiveId;

        PublicEventObjectiveCreditEntityScript script = createScript(CreateSpellParametersFactory(), RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(script.GetType());

        IScriptFilterSearch unnamedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(1u);
        Assert.False(new ScriptFilterMatch().Match(unnamedSearch, parameters));

        IScriptFilterSearch namedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByScriptNames([parameters.ScriptName]);
        Assert.True(new ScriptFilterMatch().Match(namedSearch, parameters));
    }

    [Theory]
    [InlineData(typeof(ExperimentX89EntityScript), "ExperimentX-89EntityScript")]
    [InlineData(typeof(OptimizedMemoryProbeTX67EntityScript), "OptimizedMemoryProbeTX-67EntityScript")]
    [InlineData(typeof(LavekaTheDarkHeartedEntityScript), "LavekaTheDarkHeartedEntityScript")]
    [InlineData(typeof(SeekNSlaughterEntityScript), "SeekNSlaughterEntityScript")]
    [InlineData(typeof(IceboxMk2EntityScript), "IceboxMk2EntityScript")]
    public void ExplicitSqlScriptNames_UseScriptFilter(Type scriptType, string expectedScriptName)
    {
        ScriptFilterScriptNameAttribute attribute = scriptType.GetCustomAttribute<ScriptFilterScriptNameAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(expectedScriptName, attribute.ScriptName);
    }

    private static object[] ScriptCase<TObjective>(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        TObjective objective)
        where TObjective : Enum
    {
        return [createScript, Convert.ToUInt32(objective)];
    }

    private static IFactory<ISpellParameters> CreateSpellParametersFactory()
    {
        IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
        factoryProxy.SetMethodReturn(nameof(IFactory<ISpellParameters>.Resolve), RecordingDispatchProxy<ISpellParameters>.Create(out _));
        return factory;
    }
}
