using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Quests.CrimsonIsle;

namespace NexusForever.Game.Tests.Quests;

public class CrimsonIsleBranchInteractionTests
{
    private const ushort LastResistanceQuest = 5594;
    private const ushort OlyssiaWorld = 22;
    private const float TeleportX = -5750.767f;
    private const float TeleportY = -971.7648f;
    private const float TeleportZ = -623.33295f;
    private const float RotationX = -1.1721236f;

    [Fact]
    public void Q5573PoweringDown_OnPowerRegulatorsComplete_QueuesCinematicAndCreditsObjective()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<IQ5573PoweringDownCinematic>.Create(out _);
        var script = new Q5573PoweringDownQuestScript(
            CreateCinematicFactory(cinematic),
            CreateGlobalQuestManager(),
            NullLogger<Q5573PoweringDownQuestScript>.Instance);
        IQuest quest = CreateQuest(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuest> questProxy,
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(CreateObjective(8229u, complete: true));

        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);

        RecordingDispatchProxy<IQuest>.Invocation objectiveUpdate = Assert.Single(
            questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
        Assert.Equal(12870u, objectiveUpdate.Arguments[0]);
        Assert.Equal(1u, objectiveUpdate.Arguments[1]);
    }

    [Fact]
    public void Q5573PoweringDown_OnPowerRegulatorsComplete_WhenAlreadyAchieved_DoesNotQueueOrCredit()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<IQ5573PoweringDownCinematic>.Create(out _);
        var script = new Q5573PoweringDownQuestScript(
            CreateCinematicFactory(cinematic),
            CreateGlobalQuestManager(),
            NullLogger<Q5573PoweringDownQuestScript>.Instance);
        IQuest quest = CreateQuest(
            QuestState.Achieved,
            out RecordingDispatchProxy<IQuest> questProxy,
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(CreateObjective(8229u, complete: true));

        Assert.Empty(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Empty(questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
    }

    [Fact]
    public void Q5604TacticalDemolitions_OnExileCannonsComplete_QueuesCinematicAndCreditsObjective()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<IQ5604TacticalDemolitionsCinematic>.Create(out _);
        var script = new Q5604TacticalDemolitionsQuestScript(
            CreateCinematicFactory(cinematic),
            CreateGlobalQuestManager(),
            NullLogger<Q5604TacticalDemolitionsQuestScript>.Instance);
        IQuest quest = CreateQuest(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuest> questProxy,
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(CreateObjective(8268u, complete: true));

        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);

        RecordingDispatchProxy<IQuest>.Invocation objectiveUpdate = Assert.Single(
            questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
        Assert.Equal(15918u, objectiveUpdate.Arguments[0]);
        Assert.Equal(1u, objectiveUpdate.Arguments[1]);
    }

    [Fact]
    public void Q5604TacticalDemolitions_OnExileCannonsComplete_WhenAlreadyAchieved_DoesNotQueueOrCredit()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<IQ5604TacticalDemolitionsCinematic>.Create(out _);
        var script = new Q5604TacticalDemolitionsQuestScript(
            CreateCinematicFactory(cinematic),
            CreateGlobalQuestManager(),
            NullLogger<Q5604TacticalDemolitionsQuestScript>.Instance);
        IQuest quest = CreateQuest(
            QuestState.Achieved,
            out RecordingDispatchProxy<IQuest> questProxy,
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(CreateObjective(8268u, complete: true));

        Assert.Empty(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Empty(questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
    }

    [Fact]
    public void Q5584TrappedAssistant_OnActivateSuccess_SetsStateAndKillsAssistant()
    {
        ICreatureEntity owner = CreateCreature(maxHealth: 125u, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer activator = RecordingDispatchProxy<IPlayer>.Create(out _);
        var script = new Q5584TrappedAssistantEntityScript();

        script.OnLoad(owner);
        script.OnActivateSuccess(activator);

        RecordingDispatchProxy<ICreatureEntity>.Invocation standState = Assert.Single(ownerProxy.GetInvocations("set_StandState"));
        Assert.Equal(StandState.State2, standState.Arguments[0]);

        RecordingDispatchProxy<ICreatureEntity>.Invocation damage = Assert.Single(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(125u, damage.Arguments[0]);
        Assert.Equal(DamageType.Physical, damage.Arguments[1]);
        Assert.Null(damage.Arguments[2]);
    }

    [Fact]
    public void Q5594ShipControls_OnActivateSuccess_WhenQuestAccepted_AchievesAndTeleports()
    {
        IPlayer player = CreatePlayer(
            QuestState.Accepted,
            canTeleport: true,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = new Q5594ShipControlsEntityScript();

        script.OnLoad(CreateCreature(maxHealth: 100u, out _));
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation achieved = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAchieve)));
        Assert.Equal(LastResistanceQuest, achieved.Arguments[0]);
        AssertTeleportedToBloodfireVillage(playerProxy);
    }

    [Fact]
    public void Q5594ShipControls_OnActivateSuccess_WhenQuestMissing_StillTeleportsWithoutQuestCredit()
    {
        IPlayer player = CreatePlayer(
            questState: null,
            canTeleport: true,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = new Q5594ShipControlsEntityScript();

        script.OnLoad(CreateCreature(maxHealth: 100u, out _));
        script.OnActivateSuccess(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAchieve)));
        AssertTeleportedToBloodfireVillage(playerProxy);
    }

    [Fact]
    public void Q5594ShipControls_OnActivateSuccess_WhenTeleportBlocked_AchievesWithoutRelocation()
    {
        IPlayer player = CreatePlayer(
            QuestState.Accepted,
            canTeleport: false,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = new Q5594ShipControlsEntityScript();

        script.OnLoad(CreateCreature(maxHealth: 100u, out _));
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation achieved = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAchieve)));
        Assert.Equal(LastResistanceQuest, achieved.Arguments[0]);
        Assert.Empty(playerProxy.GetInvocations("set_Rotation"));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    private static ICreatureEntity CreateCreature(
        uint maxHealth,
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy)
    {
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(ICreatureEntity.MaxHealth), maxHealth);
        return owner;
    }

    private static IPlayer CreatePlayer(
        QuestState? questState,
        bool canTeleport,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
            (ushort)args[0] == LastResistanceQuest ? questState : null);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), canTeleport);
        return player;
    }

    private static void AssertTeleportedToBloodfireVillage(RecordingDispatchProxy<IPlayer> playerProxy)
    {
        RecordingDispatchProxy<IPlayer>.Invocation rotation = Assert.Single(playerProxy.GetInvocations("set_Rotation"));
        Vector3 vector = Assert.IsType<Vector3>(rotation.Arguments[0]);
        Assert.Equal(RotationX, vector.X, precision: 6);
        Assert.Equal(0f, vector.Y);
        Assert.Equal(0f, vector.Z);

        RecordingDispatchProxy<IPlayer>.Invocation teleport = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Equal(OlyssiaWorld, teleport.Arguments[0]);
        Assert.Equal(TeleportX, (float)teleport.Arguments[1], precision: 3);
        Assert.Equal(TeleportY, (float)teleport.Arguments[2], precision: 3);
        Assert.Equal(TeleportZ, (float)teleport.Arguments[3], precision: 3);
        Assert.Null(teleport.Arguments[4]);
        Assert.Equal(TeleportReason.Relocate, teleport.Arguments[5]);
    }

    private static IQuest CreateQuest(
        QuestState state,
        out RecordingDispatchProxy<IQuest> questProxy,
        out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy)
    {
        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out cinematicManagerProxy);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);

        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out questProxy);
        questProxy.SetProperty(nameof(IQuest.State), state);
        questProxy.SetProperty(nameof(IQuest.Player), player);
        return quest;
    }

    private static IQuestObjective CreateObjective(uint objectiveId, bool complete)
    {
        IQuestObjectiveInfo objectiveInfo = RecordingDispatchProxy<IQuestObjectiveInfo>.Create(out RecordingDispatchProxy<IQuestObjectiveInfo> objectiveInfoProxy);
        objectiveInfoProxy.SetProperty(nameof(IQuestObjectiveInfo.Id), objectiveId);

        IQuestObjective objective = RecordingDispatchProxy<IQuestObjective>.Create(out RecordingDispatchProxy<IQuestObjective> objectiveProxy);
        objectiveProxy.SetProperty(nameof(IQuestObjective.ObjectiveInfo), objectiveInfo);
        objectiveProxy.SetMethodReturn(nameof(IQuestObjective.IsComplete), complete);
        return objective;
    }

    private static ICinematicFactory CreateCinematicFactory(ICinematicBase cinematic)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);
        return cinematicFactory;
    }

    private static IGlobalQuestManager CreateGlobalQuestManager()
    {
        return RecordingDispatchProxy<IGlobalQuestManager>.Create(out _);
    }
}
