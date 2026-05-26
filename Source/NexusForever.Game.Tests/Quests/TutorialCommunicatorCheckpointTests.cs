using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Cinematic;
using NexusForever.Game.Cinematic.Cinematics;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Script.Main.Quests.Tutorial;
using NexusForever.Script.Main.Tutorial;
using NexusForever.Script.Template;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Quests;

public class TutorialCommunicatorCheckpointTests
{
    [Fact]
    public void ExileCombat_OnRetailCheckpoints_SendsObjectiveCommunicatorsInOrder()
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(out List<uint> sentMessages);
        IQuest owner = CreateQuest(10518, []);
        var script = new Q10518CombatQuestScript(
            NullLogger<Q10518CombatQuestScript>.Instance,
            globalQuestManager);
        script.OnLoad(owner);

        script.OnObjectiveUpdate(CreateObjective(21286));
        script.OnObjectiveUpdate(CreateObjective(21287));
        script.OnObjectiveUpdate(CreateObjective(21340));
        script.OnObjectiveUpdate(CreateObjective(21318));
        script.OnObjectiveUpdate(CreateObjective(21288));
        script.OnObjectiveUpdate(CreateObjective(21288));

        Assert.Equal([7971u, 8026u, 8027u, 7972u, 8035u, 7973u], sentMessages);
    }

    [Fact]
    public void ExileHoverboard_OnRetailCheckpoints_SendsTerrainAndCombatProjectorCommunicators()
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(out List<uint> sentMessages);
        IQuest owner = CreateQuest(10527, []);
        var script = new StarterTutorialQuestScript(globalQuestManager, CreateSpellParametersFactory());
        script.OnLoad(owner);

        ((IQuestScript)script).OnObjectiveUpdate(CreateObjective(21324));
        ((IQuestScript)script).OnObjectiveUpdate(CreateObjective(21323));

        Assert.Equal([7981u, 8025u], sentMessages);
    }

    [Theory]
    [InlineData(10527, 21324u)]
    [InlineData(10532, 21354u)]
    public void HoverboardQuest_OnProjectorObjectiveComplete_CastsTrailVisual(ushort questId, uint objectiveId)
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(out _);
        IQuest owner = CreateQuest(questId, [], out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = new StarterTutorialQuestScript(globalQuestManager, CreateSpellParametersFactory());
        script.OnLoad(owner);

        ((IQuestScript)script).OnObjectiveUpdate(CreateObjective(objectiveId));

        RecordingDispatchProxy<IPlayer>.Invocation cast = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.CastSpell)),
            i => i.Arguments.Length == 2 && i.Arguments[0] is uint);
        Assert.Equal(82298u, (uint)cast.Arguments[0]);
        ISpellParameters spellParameters = (ISpellParameters)cast.Arguments[1];
        Assert.Equal(42u, spellParameters.PrimaryTargetId);
        Assert.True(spellParameters.IgnoreGlobalCooldown);
    }

    [Theory]
    [InlineData(10513, 21271u)]
    [InlineData(10513, 21279u)]
    [InlineData(10513, 21272u)]
    [InlineData(10521, 21300u)]
    [InlineData(10521, 21301u)]
    [InlineData(10521, 21303u)]
    public void MovementQuest_OnStepCircleObjectiveComplete_DoesNotCastScanSpell(ushort questId, uint objectiveId)
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(out _);
        IQuest owner = CreateQuest(questId, [], out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = new StarterTutorialQuestScript(globalQuestManager, CreateSpellParametersFactory());
        script.OnLoad(owner);

        ((IQuestScript)script).OnObjectiveUpdate(CreateObjective(objectiveId));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
    }

    [Fact]
    public void MovementQuest_OnHiddenOptionalObjectiveComplete_DoesNotCastScanSpell()
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(out _);
        IQuest owner = CreateQuest(10513, [], out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = new StarterTutorialQuestScript(globalQuestManager, CreateSpellParametersFactory());
        script.OnLoad(owner);

        ((IQuestScript)script).OnObjectiveUpdate(CreateObjective(21280u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
    }

    [Fact]
    public void ExileMissionSimulation_OnLoadAndCombatCheckpoints_SendsRetailCommunicators()
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(out List<uint> sentMessages);
        IQuest owner = CreateQuest(10520, [CreateObjective(21294, complete: false, progress: 0)]);
        var script = new Q10520ShipInteriorQuestScript(
            NullLogger<Q10520ShipInteriorQuestScript>.Instance,
            globalQuestManager);

        script.OnLoad(owner);
        script.OnObjectiveUpdate(CreateObjective(21294));
        script.OnObjectiveUpdate(CreateObjective(21296));

        Assert.Equal([7985u, 7987u, 7992u], sentMessages);
    }

    [Fact]
    public void ExileDeparture_OnRhodaObjectiveComplete_SendsDestinationCommunicator()
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(out List<uint> sentMessages);
        IQuest owner = CreateQuest(10528, []);
        var script = new Q10528EscapePodQuestScript(
            NullLogger<Q10528EscapePodQuestScript>.Instance,
            globalQuestManager);
        script.OnLoad(owner);

        script.OnObjectiveUpdate(CreateObjective(21326));

        Assert.Equal([8063u], sentMessages);
    }

    [Theory]
    [InlineData(Faction.Exile, 749303u, 749304u)]
    [InlineData(Faction.Dominion, 749288u, 749289u)]
    public void CombatProjectorCinematic_UsesFactionSpecificRetailText(Faction faction, uint firstTextId, uint secondTextId)
    {
        IPlayer player = CreatePlayer(faction);
        var cinematic = new NoviceTutorialCombatProjector();

        typeof(CinematicBase)
            .GetProperty("Player", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(cinematic, player);
        typeof(NoviceTutorialCombatProjector)
            .GetMethod("SetupTexts", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(cinematic, null);

        Assert.Equal(firstTextId, cinematic.Texts[1500]);
        Assert.Equal(secondTextId, cinematic.Texts[6500]);
    }

    private static IGlobalQuestManager CreateGlobalQuestManager(out List<uint> sentMessages)
    {
        sentMessages = [];
        List<uint> capturedMessages = sentMessages;

        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage), args =>
        {
            uint communicatorMessageId = (uint)args[0];
            ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> communicatorMessageProxy);
            communicatorMessageProxy.SetMethodHandler(nameof(ICommunicatorMessage.Send), _ =>
            {
                capturedMessages.Add(communicatorMessageId);
                return null;
            });
            return communicatorMessage;
        });

        return globalQuestManager;
    }

    private static IFactory<ISpellParameters> CreateSpellParametersFactory()
    {
        IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
        factoryProxy.SetMethodHandler(nameof(IFactory<ISpellParameters>.Resolve), args =>
            RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy));

        return factory;
    }

    private static IQuest CreateQuest(ushort questId, IReadOnlyCollection<IQuestObjective> objectives)
    {
        return CreateQuest(questId, objectives, out _);
    }

    private static IQuest CreateQuest(ushort questId, IReadOnlyCollection<IQuestObjective> objectives, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Id), questId);
        questProxy.SetProperty(nameof(IQuest.State), QuestState.Accepted);
        questProxy.SetProperty(nameof(IQuest.Player), player);
        questProxy.SetMethodHandler("GetEnumerator", _ => objectives.GetEnumerator());

        return quest;
    }

    private static IQuestObjective CreateObjective(uint objectiveId, bool complete = true, uint progress = 1)
    {
        IQuestObjectiveInfo objectiveInfo = RecordingDispatchProxy<IQuestObjectiveInfo>.Create(out RecordingDispatchProxy<IQuestObjectiveInfo> objectiveInfoProxy);
        objectiveInfoProxy.SetProperty(nameof(IQuestObjectiveInfo.Id), objectiveId);

        IQuestObjective objective = RecordingDispatchProxy<IQuestObjective>.Create(out RecordingDispatchProxy<IQuestObjective> objectiveProxy);
        objectiveProxy.SetProperty(nameof(IQuestObjective.ObjectiveInfo), objectiveInfo);
        objectiveProxy.SetProperty(nameof(IQuestObjective.Progress), progress);
        objectiveProxy.SetMethodReturn(nameof(IQuestObjective.IsComplete), complete);

        return objective;
    }

    private static IPlayer CreatePlayer(Faction faction)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), faction);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        return player;
    }
}
