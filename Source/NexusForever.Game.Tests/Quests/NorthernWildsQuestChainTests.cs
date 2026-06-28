using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Story;
using NexusForever.Game.Cinematic.Cinematics;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Cinematic;
using NexusForever.Script.Main.Quests.NorthernWilds;
using NexusForever.Script.Template;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Tests.Quests;

public class NorthernWildsQuestChainTests
{
    [Theory]
    [InlineData(3479, 3667)]
    [InlineData(3480, 3667)]
    [InlineData(3667, 3486)]
    [InlineData(3487, 3963)]
    [InlineData(3886, 3673)]
    [InlineData(3671, 3668)]
    [InlineData(4666, 4667)]
    [InlineData(4667, 4696)]
    [InlineData(4696, 4694)]
    public void FollowUpQuest_OnCompleted_GrantsExpectedNextQuest(ushort questId, ushort expectedNextQuestId)
    {
        IQuest owner = CreateQuest(questId, new Dictionary<ushort, QuestState?>(), out List<ushort> grantedQuests, out _);
        IQuestScript script = CreateFollowUpScript(questId, CreateGlobalQuestManager());

        ((IOwnedScript<IQuest>)script).OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);

        Assert.Equal([expectedNextQuestId], grantedQuests);
    }

    [Fact]
    public void Q3486_OnCompleted_QueuesCinematicAndMentionsTableBackedFollowUps()
    {
        IQuest owner = CreateQuest(3486, new Dictionary<ushort, QuestState?>(), out List<ushort> grantedQuests, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out ICinematicBase cinematic, out _);
        IStoryBuilder storyBuilder = RecordingDispatchProxy<IStoryBuilder>.Create(out _);
        var script = new Q3486EmpoweredTowerQuestScript(cinematicFactory, storyBuilder);

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);

        Assert.Empty(grantedQuests);
        Assert.Equal(
            [(ushort)3671, (ushort)3797],
            questManagerProxy.GetInvocations(nameof(IQuestManager.QuestMention))
                .Select(i => (ushort)i.Arguments[0])
                .ToList());

        RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy = GetCinematicManagerProxy(owner);
        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);
    }

    [Fact]
    public void Q3486_OnAcceptedInTowerZone_ShowsStoryPanelAndCreditsArrival()
    {
        IQuest owner = CreateQuest(
            3486,
            new Dictionary<ushort, QuestState?>(),
            out _,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            new WorldZoneEntry { Id = 729u });
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out _, out _);
        IStoryBuilder storyBuilder = RecordingDispatchProxy<IStoryBuilder>.Create(out RecordingDispatchProxy<IStoryBuilder> storyBuilderProxy);
        var script = new Q3486EmpoweredTowerQuestScript(cinematicFactory, storyBuilder);

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Accepted, QuestState.Mentioned);

        RecordingDispatchProxy<IStoryBuilder>.Invocation storyPanel = Assert.Single(
            storyBuilderProxy.GetInvocations(nameof(IStoryBuilder.SendServerStoryPanelShow)));
        Assert.Same(owner.Player, storyPanel.Arguments[0]);
        Assert.Equal(1575u, storyPanel.Arguments[1]);

        RecordingDispatchProxy<IQuestManager>.Invocation objectiveUpdate = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(4987u, objectiveUpdate.Arguments[0]);
        Assert.Equal(1u, objectiveUpdate.Arguments[1]);
    }

    [Fact]
    public void Q3486_OnAcceptedOutsideTowerZone_DoesNotCreditArrival()
    {
        IQuest owner = CreateQuest(
            3486,
            new Dictionary<ushort, QuestState?>(),
            out _,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            new WorldZoneEntry { Id = 602u });
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out _, out _);
        IStoryBuilder storyBuilder = RecordingDispatchProxy<IStoryBuilder>.Create(out RecordingDispatchProxy<IStoryBuilder> storyBuilderProxy);
        var script = new Q3486EmpoweredTowerQuestScript(cinematicFactory, storyBuilder);

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Accepted, QuestState.Mentioned);

        Assert.Empty(storyBuilderProxy.GetInvocations(nameof(IStoryBuilder.SendServerStoryPanelShow)));
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void Q3673_OnAchieved_QueuesCinematicWithoutGrantingFollowUp()
    {
        IQuest owner = CreateQuest(3673, new Dictionary<ushort, QuestState?>(), out List<ushort> grantedQuests, out _);
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out ICinematicBase cinematic, out _);
        var script = new Q3673ContactWithThaydQuestScript(
            cinematicFactory,
            NullLogger<Q3673ContactWithThaydQuestScript>.Instance,
            CreateGlobalQuestManager());

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Achieved, QuestState.Accepted);

        Assert.Empty(grantedQuests);

        RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy = GetCinematicManagerProxy(owner);
        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);
    }

    [Fact]
    public void Q3673_OnCompleted_GrantsNorthernWildsTransition()
    {
        IQuest owner = CreateQuest(3673, new Dictionary<ushort, QuestState?>(), out List<ushort> grantedQuests, out _);
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out _, out _);
        var script = new Q3673ContactWithThaydQuestScript(
            cinematicFactory,
            NullLogger<Q3673ContactWithThaydQuestScript>.Instance,
            CreateGlobalQuestManager());

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Achieved);

        Assert.Equal([3670], grantedQuests);

        RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy = GetCinematicManagerProxy(owner);
        Assert.Empty(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
    }

    [Fact]
    public void Q3963_OnAchieved_QueuesDepartureCinematicAndDelaysAlgorocTransport()
    {
        IGameSession session = CreateSessionWithEvents();
        IBaseMap map = CreateMap(426u);
        IQuest owner = CreateQuest(
            3963,
            new Dictionary<ushort, QuestState?>(),
            out _,
            out _,
            map: map,
            session: session);
        RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)owner.Player;
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
        playerProxy.SetMethodHandler(nameof(IPlayer.TeleportTo), args =>
        {
            Assert.Equal((ushort)51, args[0]);
            Assert.Equal(3832.38f, args[1]);
            Assert.Equal(-1001.45f, args[2]);
            Assert.Equal(-4497.14f, args[3]);
            Assert.Null(args[4]);
            Assert.Equal(TeleportReason.Relocate, args[5]);
            return null;
        });
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out ICinematicBase cinematic, out _);

        var script = new Q3963MoreImportantThanRevengeQuestScript(
            NullLogger<Q3963MoreImportantThanRevengeQuestScript>.Instance,
            cinematicFactory,
            CreateGameTableManager(CreateWorldLocationTable(CreateAlgorocDepartureWorldLocation())));

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Achieved, QuestState.Accepted);

        RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy = GetCinematicManagerProxy(owner);
        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));

        IEvent pendingEvent = GetQueuedEvent(session.Events);
        pendingEvent.Execute();

        RecordingDispatchProxy<IPlayer>.Invocation rotationInvocation = Assert.Single(
            playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Vector3 rotation = Assert.IsType<Vector3>(rotationInvocation.Arguments[0]);
        Assert.InRange(rotation.X, -27f, -25f);
        Assert.Equal(0f, rotation.Y, precision: 5);
        Assert.Equal(0f, rotation.Z, precision: 5);
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void Q3963_OnAchieved_WhenDepartureLocationMissing_DoesNotTeleport()
    {
        IQuest owner = CreateQuest(3963, new Dictionary<ushort, QuestState?>(), out _, out _);
        RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)owner.Player;
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);

        var script = new Q3963MoreImportantThanRevengeQuestScript(
            NullLogger<Q3963MoreImportantThanRevengeQuestScript>.Instance,
            CreateCinematicFactory(out _, out _),
            CreateGameTableManager(CreateWorldLocationTable()));

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Achieved, QuestState.Accepted);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void Q3963_OnAchieved_WhenTeleportUnavailable_DoesNotTeleport()
    {
        IQuest owner = CreateQuest(3963, new Dictionary<ushort, QuestState?>(), out _, out _);
        RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)owner.Player;
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), false);

        var script = new Q3963MoreImportantThanRevengeQuestScript(
            NullLogger<Q3963MoreImportantThanRevengeQuestScript>.Instance,
            CreateCinematicFactory(out _, out _),
            CreateGameTableManager(CreateWorldLocationTable(CreateAlgorocDepartureWorldLocation())));

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Achieved, QuestState.Accepted);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void Q3963_OnAchieved_WhenPlayerLeavesNorthernWildsBeforeDelay_DoesNotTeleport()
    {
        IGameSession session = CreateSessionWithEvents();
        IBaseMap map = CreateMap(51u);
        IQuest owner = CreateQuest(
            3963,
            new Dictionary<ushort, QuestState?>(),
            out _,
            out _,
            map: map,
            session: session);
        RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)owner.Player;
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);

        var script = new Q3963MoreImportantThanRevengeQuestScript(
            NullLogger<Q3963MoreImportantThanRevengeQuestScript>.Instance,
            CreateCinematicFactory(out _, out _),
            CreateGameTableManager(CreateWorldLocationTable(CreateAlgorocDepartureWorldLocation())));

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Achieved, QuestState.Accepted);
        GetQueuedEvent(session.Events).Execute();

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void Q3963DepartureCinematic_StartPlayback_UsesTableBackedDepartureCameraSpline()
    {
        List<object> messages = [];
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        sessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args.Length == 1)
                messages.Add(args[0]);
            return null;
        });

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IGridEntity.Position), new Vector3(4518.05f, -694.852f, -5164.07f));

        new Q3963MoreImportantThanRevengeDepartureCinematic().StartPlayback(player);

        ServerCinematicCameraSpline cameraSpline = Assert.Single(messages.OfType<ServerCinematicCameraSpline>());
        Assert.Equal(3860u, cameraSpline.SplineId);
        Assert.True(cameraSpline.UseRotation);
        Assert.DoesNotContain(messages.OfType<ServerCinematicCameraSpline>(), s => s.SplineId is 4050u or 4086u);
        Assert.Contains(messages.OfType<ServerCinematicNotify>(), n => n.Delay == 26500u);
        Assert.Contains(messages.OfType<ServerCinematicCamera>(), c => c.Delay == 25000u && c.Flags == CameraAddFlags.WhiteOut);

        ServerCinematicActorAdd[] passengerActors = messages
            .OfType<ServerCinematicActorAdd>()
            .Where(a => a.Creature2Id == 12537u)
            .ToArray();
        Assert.Equal(2, passengerActors.Length);
        Assert.All(passengerActors, a =>
        {
            Assert.Equal(EntityCreateFlag.Immediate | EntityCreateFlag.HasInteractionPrereq, a.CreateFlags);
            Assert.Equal(0, a.TextureLevelOfDetailBias);
            Assert.Equal(ModeType.Walk, a.MovementMode);
        });

        ServerCinematicActorAdd[] shipActors = messages
            .OfType<ServerCinematicActorAdd>()
            .Where(a => a.Creature2Id is 8531u or 8532u)
            .OrderBy(a => a.Creature2Id)
            .ToArray();
        Assert.Equal(2, shipActors.Length);
        Assert.All(shipActors, a =>
        {
            Assert.Equal(EntityCreateFlag.Immediate | EntityCreateFlag.HasInteractionPrereq, a.CreateFlags);
            Assert.Equal(0, a.TextureLevelOfDetailBias);
            Assert.Equal(ModeType.Free, a.MovementMode);
        });
        Assert.Equal(new Vector3(4515.42f, -674.002f, -5183.32f), shipActors[0].Position.Vector);
        Assert.Equal(new Vector3(4558.71f, -670.928f, -5196.63f), shipActors[1].Position.Vector);

        ServerCinematicActorSpline[] actorSplines = messages
            .OfType<ServerCinematicActorSpline>()
            .OrderBy(s => s.SplineId)
            .ToArray();
        Assert.DoesNotContain(actorSplines, s => s.SplineId is 4050u or 4086u);
        Assert.Collection(
            actorSplines,
            spline =>
            {
                Assert.Equal(3159u, spline.SplineId);
                Assert.Equal(0u, spline.Delay);
                Assert.True(spline.UseRotation);
                Assert.Equal(22f, spline.SplineSpeed);
                Assert.Equal(8u, spline.SplineMode);
            },
            spline =>
            {
                Assert.Equal(3160u, spline.SplineId);
                Assert.Equal(0u, spline.Delay);
                Assert.True(spline.UseRotation);
                Assert.Equal(22f, spline.SplineSpeed);
                Assert.Equal(8u, spline.SplineMode);
            },
            spline =>
            {
                Assert.Equal(3481u, spline.SplineId);
                Assert.Equal(6500u, spline.Delay);
                Assert.True(spline.UseRotation);
                Assert.Equal(36f, spline.SplineSpeed);
                Assert.Equal(8u, spline.SplineMode);
            },
            spline =>
            {
                Assert.Equal(3482u, spline.SplineId);
                Assert.Equal(6500u, spline.Delay);
                Assert.True(spline.UseRotation);
                Assert.Equal(30f, spline.SplineSpeed);
                Assert.Equal(8u, spline.SplineMode);
            });

        uint[] passengerUnitIds = passengerActors.Select(a => a.UnitId).ToArray();
        ServerCinematicActorVisibility[] passengerHides = messages
            .OfType<ServerCinematicActorVisibility>()
            .Where(v => passengerUnitIds.Contains(v.UnitId))
            .ToArray();
        Assert.Equal(2, passengerHides.Length);
        Assert.All(passengerHides, v =>
        {
            Assert.Equal(6500u, v.Delay);
            Assert.True(v.Hide);
        });

        ServerCinematicActorVisibility playerHide = Assert.Single(
            messages.OfType<ServerCinematicActorVisibility>(),
            v => v.UnitId == 0u && v.Hide);
        Assert.True(playerHide.AffectOnlyPlayers);
    }

    [Fact]
    public void Q3673_OnLoad_WhenAllFlareObjectivesComplete_CreditsHiddenCompletionObjective()
    {
        IReadOnlyList<IQuestObjective> objectives = CreateQ3673CompletionGateObjectives(
            checklistComplete: true,
            csiOneComplete: true,
            csiTwoComplete: true,
            csiThreeComplete: true);
        IQuest owner = CreateQuest(3673, new Dictionary<ushort, QuestState?>(), out _, out _, objectives: objectives);
        RecordingDispatchProxy<IQuest> ownerProxy = (RecordingDispatchProxy<IQuest>)(object)owner;
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out _, out _);
        var script = new Q3673ContactWithThaydQuestScript(
            cinematicFactory,
            NullLogger<Q3673ContactWithThaydQuestScript>.Instance,
            CreateGlobalQuestManager());

        script.OnLoad(owner);

        RecordingDispatchProxy<IQuest>.Invocation update = Assert.Single(
            ownerProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
        Assert.Equal(13391u, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void Q3673_OnSignalFlareChecklistCompleteBeforeCsiObjectives_DoesNotCreditHiddenCompletionObjective()
    {
        IReadOnlyList<IQuestObjective> objectives = CreateQ3673CompletionGateObjectives(
            checklistComplete: true,
            csiOneComplete: false,
            csiTwoComplete: false,
            csiThreeComplete: false);
        IQuest owner = CreateQuest(3673, new Dictionary<ushort, QuestState?>(), out _, out _, objectives: objectives);
        RecordingDispatchProxy<IQuest> ownerProxy = (RecordingDispatchProxy<IQuest>)(object)owner;
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out _, out _);
        var script = new Q3673ContactWithThaydQuestScript(
            cinematicFactory,
            NullLogger<Q3673ContactWithThaydQuestScript>.Instance,
            CreateGlobalQuestManager());

        script.OnLoad(owner);
        script.OnObjectiveUpdate(objectives[0]);

        Assert.Empty(ownerProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
    }

    [Fact]
    public void Q3673_OnSignalFlareObjectivesComplete_WhenCallbacksRepeat_CreditsHiddenCompletionObjectiveOnce()
    {
        IReadOnlyList<IQuestObjective> objectives = CreateQ3673CompletionGateObjectives(
            checklistComplete: true,
            csiOneComplete: true,
            csiTwoComplete: true,
            csiThreeComplete: true);
        IQuest owner = CreateQuest(3673, new Dictionary<ushort, QuestState?>(), out _, out _, objectives: objectives);
        RecordingDispatchProxy<IQuest> ownerProxy = (RecordingDispatchProxy<IQuest>)(object)owner;
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out _, out _);
        var script = new Q3673ContactWithThaydQuestScript(
            cinematicFactory,
            NullLogger<Q3673ContactWithThaydQuestScript>.Instance,
            CreateGlobalQuestManager());

        script.OnLoad(owner);
        foreach (IQuestObjective objective in objectives)
            script.OnObjectiveUpdate(objective);
        script.OnObjectiveUpdate(objectives[0]);

        RecordingDispatchProxy<IQuest>.Invocation update = Assert.Single(
            ownerProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
        Assert.Equal(13391u, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void Q3673_OnLastSignalFlareCsiCompleteAfterChecklist_CreditsHiddenCompletionObjective()
    {
        bool csiThreeComplete = false;
        IQuestObjective checklistObjective = CreateQuestObjective(4748u, QuestObjectiveType.ActivateTargetGroupChecklist, isComplete: true);
        IQuestObjective csiOneObjective = CreateQuestObjective(4888u, QuestObjectiveType.SucceedCSI, isComplete: true);
        IQuestObjective csiTwoObjective = CreateQuestObjective(4889u, QuestObjectiveType.SucceedCSI, isComplete: true);
        IQuestObjective csiThreeObjective = CreateQuestObjective(4890u, QuestObjectiveType.SucceedCSI, () => csiThreeComplete);
        IReadOnlyList<IQuestObjective> objectives =
        [
            checklistObjective,
            csiOneObjective,
            csiTwoObjective,
            csiThreeObjective
        ];
        IQuest owner = CreateQuest(3673, new Dictionary<ushort, QuestState?>(), out _, out _, objectives: objectives);
        RecordingDispatchProxy<IQuest> ownerProxy = (RecordingDispatchProxy<IQuest>)(object)owner;
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out _, out _);
        var script = new Q3673ContactWithThaydQuestScript(
            cinematicFactory,
            NullLogger<Q3673ContactWithThaydQuestScript>.Instance,
            CreateGlobalQuestManager());

        script.OnLoad(owner);
        script.OnObjectiveUpdate(checklistObjective);

        Assert.Empty(ownerProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));

        csiThreeComplete = true;
        script.OnObjectiveUpdate(csiThreeObjective);

        RecordingDispatchProxy<IQuest>.Invocation update = Assert.Single(
            ownerProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
        Assert.Equal(13391u, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void Q3673_OnSignalFlareChecklistIncomplete_DoesNotCreditHiddenCompletionObjective()
    {
        IReadOnlyList<IQuestObjective> objectives = CreateQ3673CompletionGateObjectives(
            checklistComplete: false,
            csiOneComplete: true,
            csiTwoComplete: true,
            csiThreeComplete: true);
        IQuest owner = CreateQuest(3673, new Dictionary<ushort, QuestState?>(), out _, out _, objectives: objectives);
        RecordingDispatchProxy<IQuest> ownerProxy = (RecordingDispatchProxy<IQuest>)(object)owner;
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out _, out _);
        var script = new Q3673ContactWithThaydQuestScript(
            cinematicFactory,
            NullLogger<Q3673ContactWithThaydQuestScript>.Instance,
            CreateGlobalQuestManager());

        script.OnLoad(owner);
        script.OnObjectiveUpdate(objectives[0]);

        Assert.Empty(ownerProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
    }

    private static IQuestScript CreateFollowUpScript(ushort questId, IGlobalQuestManager globalQuestManager)
    {
        return questId switch
        {
            3479 => new Q3479FromTheWreckageQuestScript(
                NullLogger<Q3479FromTheWreckageQuestScript>.Instance,
                globalQuestManager),
            3480 => new Q3480QuestScript(
                NullLogger<Q3480QuestScript>.Instance,
                globalQuestManager),
            3667 => new Q3667TheTowerQuestScript(
                NullLogger<Q3667TheTowerQuestScript>.Instance,
                globalQuestManager),
            3487 => new Q3487ShellshockQuestScript(
                NullLogger<Q3487ShellshockQuestScript>.Instance,
                globalQuestManager),
            3886 => new Q3886FieryDistractionQuestScript(
                NullLogger<Q3886FieryDistractionQuestScript>.Instance,
                globalQuestManager),
            3671 => new Q3671QuestScript(
                NullLogger<Q3671QuestScript>.Instance,
                globalQuestManager),
            4666 => new Q4666TakeTheFightToThemQuestScript(
                NullLogger<Q4666TakeTheFightToThemQuestScript>.Instance,
                globalQuestManager),
            4667 => new Q4667BurnItDownQuestScript(
                NullLogger<Q4667BurnItDownQuestScript>.Instance,
                globalQuestManager),
            4696 => new Q4696LeavingTheTempleOfOsiricQuestScript(
                NullLogger<Q4696LeavingTheTempleOfOsiricQuestScript>.Instance,
                globalQuestManager),
            _ => throw new ArgumentOutOfRangeException(nameof(questId), questId, null)
        };
    }

    private static IQuest CreateQuest(
        ushort questId,
        IReadOnlyDictionary<ushort, QuestState?> questStates,
        out List<ushort> grantedQuests,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        WorldZoneEntry zone = null,
        IReadOnlyList<IQuestObjective> objectives = null,
        IBaseMap map = null,
        IGameSession session = null,
        Vector3? playerPosition = null)
    {
        grantedQuests = [];
        List<ushort> capturedGrantedQuests = grantedQuests;

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
        {
            ushort requestedQuestId = (ushort)args[0];
            return questStates.TryGetValue(requestedQuestId, out QuestState? state) ? state : null;
        });
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.QuestAdd), args =>
        {
            IQuestInfo questInfo = Assert.IsAssignableFrom<IQuestInfo>(args[0]);
            capturedGrantedQuests.Add((ushort)questInfo.Entry.Id);
            return null;
        });

        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out _);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        if (zone != null)
            playerProxy.SetProperty(nameof(IPlayer.Zone), zone);
        if (map != null)
            playerProxy.SetProperty(nameof(IPlayer.Map), map);
        if (session != null)
            playerProxy.SetProperty(nameof(IPlayer.Session), session);
        if (playerPosition != null)
            playerProxy.SetProperty(nameof(IGridEntity.Position), playerPosition.Value);

        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Id), questId);
        questProxy.SetProperty(nameof(IQuest.State), QuestState.Accepted);
        questProxy.SetProperty(nameof(IQuest.Player), player);
        if (objectives != null)
            questProxy.SetMethodHandler(
                nameof(IEnumerable<IQuestObjective>.GetEnumerator),
                _ => ((IEnumerable<IQuestObjective>)objectives).GetEnumerator());

        return quest;
    }

    private static IGlobalQuestManager CreateGlobalQuestManager()
    {
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetQuestInfo), args => CreateQuestInfo((ushort)args[0]));
        return globalQuestManager;
    }

    private static IQuestInfo CreateQuestInfo(ushort questId)
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out RecordingDispatchProxy<IQuestInfo> questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = questId });
        return questInfo;
    }

    private static IQuestObjective CreateQuestObjective(uint objectiveId, QuestObjectiveType type, bool isComplete)
    {
        return CreateQuestObjective(objectiveId, type, () => isComplete);
    }

    private static IQuestObjective CreateQuestObjective(uint objectiveId, QuestObjectiveType type, Func<bool> isComplete)
    {
        IQuestObjective objective = RecordingDispatchProxy<IQuestObjective>.Create(out RecordingDispatchProxy<IQuestObjective> objectiveProxy);
        objectiveProxy.SetProperty(nameof(IQuestObjective.ObjectiveInfo), new QuestObjectiveInfo(new QuestObjectiveEntry
        {
            Id    = objectiveId,
            Type  = (uint)type,
            Count = 1u
        }));
        objectiveProxy.SetMethodHandler(nameof(IQuestObjective.IsComplete), _ => isComplete());
        return objective;
    }

    private static IReadOnlyList<IQuestObjective> CreateQ3673CompletionGateObjectives(
        bool checklistComplete,
        bool csiOneComplete,
        bool csiTwoComplete,
        bool csiThreeComplete)
    {
        return
        [
            CreateQuestObjective(4748u, QuestObjectiveType.ActivateTargetGroupChecklist, checklistComplete),
            CreateQuestObjective(4888u, QuestObjectiveType.SucceedCSI, csiOneComplete),
            CreateQuestObjective(4889u, QuestObjectiveType.SucceedCSI, csiTwoComplete),
            CreateQuestObjective(4890u, QuestObjectiveType.SucceedCSI, csiThreeComplete)
        ];
    }

    private static ICinematicFactory CreateCinematicFactory(
        out ICinematicBase cinematic,
        out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy)
    {
        cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out cinematicFactoryProxy);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);
        return cinematicFactory;
    }

    private static RecordingDispatchProxy<ICinematicManager> GetCinematicManagerProxy(IQuest quest)
    {
        return (RecordingDispatchProxy<ICinematicManager>)(object)quest.Player.CinematicManager;
    }

    private static IGameSession CreateSessionWithEvents()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        sessionProxy.SetProperty(nameof(INetworkSession.Events), new EventQueue());
        return session;
    }

    private static IBaseMap CreateMap(uint worldId)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = worldId });
        return map;
    }

    private static IEvent GetQueuedEvent(EventQueue queue)
    {
        FieldInfo field = typeof(EventQueue).GetField("events", BindingFlags.Instance | BindingFlags.NonPublic);
        var pendingEvents = Assert.IsAssignableFrom<IEnumerable>(field?.GetValue(queue));
        object pending = Assert.Single(pendingEvents.Cast<object>());

        PropertyInfo property = pending.GetType().GetProperty("Event", BindingFlags.Instance | BindingFlags.Public);
        return Assert.IsAssignableFrom<IEvent>(property?.GetValue(pending));
    }

    private static IGameTableManager CreateGameTableManager(GameTable<WorldLocation2Entry> worldLocationTable)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), worldLocationTable);
        return gameTableManager;
    }

    private static WorldLocation2Entry CreateAlgorocDepartureWorldLocation()
    {
        return new WorldLocation2Entry
        {
            Id        = 9801u,
            WorldId   = 51u,
            Position0 = 3832.38f,
            Position1 = -1001.45f,
            Position2 = -4497.14f,
            Facing0   = -0.00000000000100379f,
            Facing1   = -0.2271f,
            Facing2   = 0.00000000000430461f,
            Facing3   = 0.973871f
        };
    }

    private static GameTable<WorldLocation2Entry> CreateWorldLocationTable(params WorldLocation2Entry[] entries)
    {
        var table = (GameTable<WorldLocation2Entry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<WorldLocation2Entry>));

        typeof(GameTable<WorldLocation2Entry>)
            .GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, entries);

        uint maxId = entries.Length == 0 ? 0u : entries.Max(e => e.Id) + 1u;
        typeof(GameTable<WorldLocation2Entry>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = maxId });

        int[] lookup = Enumerable.Repeat(-1, (int)maxId).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[entries[i].Id] = i;

        typeof(GameTable<WorldLocation2Entry>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        return table;
    }
}
