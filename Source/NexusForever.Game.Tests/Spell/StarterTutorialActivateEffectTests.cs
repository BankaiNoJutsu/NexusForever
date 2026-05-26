using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Shared;
using static NexusForever.Game.Static.Tutorial.StarterTutorialDefinition;

namespace NexusForever.Game.Tests.Spell;

[Collection(LegacyServiceProviderCollection.Name)]
public sealed class StarterTutorialActivateEffectTests : IDisposable
{
    private readonly IServiceProvider previousProvider;

    public StarterTutorialActivateEffectTests()
    {
        previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(CreateAssetManager())
            .BuildServiceProvider();
    }

    public void Dispose()
    {
        LegacyServiceProvider.Provider = previousProvider;
    }

    [Fact]
    public void HandleEffectActivateWorld_DepartureTerminal_RecordsTerminalAndCreditsObjectives()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IQuestManager> questProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        IWorldEntity terminal = CreateTerminal(ExileEverstarGroveDepartureTerminalCreatureId, checklistIndex: 4);
        ISpell spell = CreateSpell(player);
        ISpellTargetEffectInfo effectInfo = CreateActivateEffectInfo();

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectActivateWorld(spell, terminal, effectInfo);

        Assert.Contains(playerProxy.GetInvocations(nameof(IPlayer.RecordStarterTutorialDepartureTerminal)), i =>
            (uint)i.Arguments[0] == ExileEverstarGroveDepartureTerminalCreatureId);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.ActivateEntity, ExileEverstarGroveDepartureTerminalCreatureId, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.ActivateEntity2, ExileEverstarGroveDepartureTerminalCreatureId, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.ActivateTargetGroup, ExileEverstarGroveDepartureTerminalCreatureId, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.ActivateTargetGroupChecklist, ExileEverstarGroveDepartureTerminalCreatureId, 4u);
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IQuestManager> questProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        return player;
    }

    private static IWorldEntity CreateTerminal(uint creatureId, byte checklistIndex)
    {
        IWorldEntity terminal = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> terminalProxy);
        terminalProxy.SetProperty(nameof(IGridEntity.Guid), 99u);
        terminalProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        terminalProxy.SetProperty(nameof(IWorldEntity.QuestChecklistIdx), checklistIndex);
        return terminal;
    }

    private static ISpell CreateSpell(IPlayer player)
    {
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id = 34681u
        });

        ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy);
        parametersProxy.SetProperty(nameof(ISpellParameters.SpellInfo), spellInfo);

        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Caster), player);
        spellProxy.SetProperty(nameof(ISpell.CastingId), 7u);
        spellProxy.SetProperty(nameof(ISpell.Parameters), parameters);
        return spell;
    }

    private static ISpellTargetEffectInfo CreateActivateEffectInfo()
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            EffectType = SpellEffectType.Activate
        });
        return info;
    }

    private static AssetManager CreateAssetManager()
    {
        var assetManager = new AssetManager();
        typeof(AssetManager)
            .GetField("creatureAssociatedTargetGroups", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(assetManager, ImmutableDictionary<uint, ImmutableList<uint>>.Empty);
        return assetManager;
    }

    private static void AssertObjectiveUpdate(
        RecordingDispatchProxy<IQuestManager> questProxy,
        QuestObjectiveType type,
        uint data,
        uint progress)
    {
        Assert.Contains(questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)), i =>
            i.Arguments.Length == 3
            && i.Arguments[0] is QuestObjectiveType objectiveType
            && objectiveType == type
            && (uint)i.Arguments[1] == data
            && (uint)i.Arguments[2] == progress);
    }
}
