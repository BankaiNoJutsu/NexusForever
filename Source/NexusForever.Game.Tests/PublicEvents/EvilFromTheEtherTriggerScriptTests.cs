using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network.Session;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Expedition.EvilFromTheEther;
using NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Event;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.PublicEvents;

public class EvilFromTheEtherTriggerScriptTests
{
    [Fact]
    public void CaptainWeirTeleportTrigger_UpdatesEscapeObjective()
    {
        var script = new CaptainWeirTeleportGridTriggerEntityScript();
        IPlayer player = CreatePlayer(1ul, out var playerProxy);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPlayer>.Invocation teleportInvocation = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportToLocal)));
        Assert.Equal(new Vector3(-398.65857f, -842.03436f, 119.298386f), teleportInvocation.Arguments[0]);
        Assert.Equal(true, teleportInvocation.Arguments[1]);

        RecordingDispatchProxy<IPublicEventManager>.Invocation updateInvocation = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.EscapeToTheTeleporter, updateInvocation.Arguments[0]);
        Assert.Equal(1, updateInvocation.Arguments[1]);
    }

    [Fact]
    public void CaptainWeirTeleportTrigger_ReenteringSamePlayer_CreditsEscapeObjectiveOnce()
    {
        var script = new CaptainWeirTeleportGridTriggerEntityScript();
        IPlayer player = CreatePlayer(1ul, out var playerProxy);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        Assert.Equal(2, playerProxy.GetInvocations(nameof(IPlayer.TeleportToLocal)).Count);

        RecordingDispatchProxy<IPublicEventManager>.Invocation updateInvocation = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.EscapeToTheTeleporter, updateInvocation.Arguments[0]);
        Assert.Equal(1, updateInvocation.Arguments[1]);
    }

    [Fact]
    public void CaptainWeirTeleportTrigger_DifferentPlayers_CreditEscapeObjectiveSeparately()
    {
        var script = new CaptainWeirTeleportGridTriggerEntityScript();
        IPlayer firstPlayer = CreatePlayer(1ul);
        IPlayer secondPlayer = CreatePlayer(2ul);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(firstPlayer);
        script.OnEnterRange(secondPlayer);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updateInvocations = publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Equal(2, updateInvocations.Count);
        Assert.All(updateInvocations, i =>
        {
            Assert.Equal(PublicEventObjective.EscapeToTheTeleporter, i.Arguments[0]);
            Assert.Equal(1, i.Arguments[1]);
        });
    }

    [Fact]
    public void UpperDeckTeleportTrigger_UpdatesUpperDeckObjective()
    {
        var script = new UpperDeckTeleportGridTriggerEntityScript();
        IPlayer player = CreatePlayer(1ul, out var playerProxy);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPlayer>.Invocation teleportInvocation = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportToLocal)));
        Assert.Equal(new Vector3(-53.353714f, -841.44684f, 164.51099f), teleportInvocation.Arguments[0]);
        Assert.Equal(false, teleportInvocation.Arguments[1]);

        RecordingDispatchProxy<IPublicEventManager>.Invocation updateInvocation = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.TeleportToUpperDeck, updateInvocation.Arguments[0]);
        Assert.Equal(1, updateInvocation.Arguments[1]);
    }

    [Fact]
    public void UpperDeckTeleportTrigger_ReenteringSamePlayer_CreditsUpperDeckObjectiveOnce()
    {
        var script = new UpperDeckTeleportGridTriggerEntityScript();
        IPlayer player = CreatePlayer(1ul, out var playerProxy);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        Assert.Equal(2, playerProxy.GetInvocations(nameof(IPlayer.TeleportToLocal)).Count);

        RecordingDispatchProxy<IPublicEventManager>.Invocation updateInvocation = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.TeleportToUpperDeck, updateInvocation.Arguments[0]);
        Assert.Equal(1, updateInvocation.Arguments[1]);
    }

    [Fact]
    public void UpperDeckTeleportTrigger_DifferentPlayers_CreditUpperDeckObjectiveSeparately()
    {
        var script = new UpperDeckTeleportGridTriggerEntityScript();
        IPlayer firstPlayer = CreatePlayer(1ul);
        IPlayer secondPlayer = CreatePlayer(2ul);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(firstPlayer);
        script.OnEnterRange(secondPlayer);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updateInvocations = publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Equal(2, updateInvocations.Count);
        Assert.All(updateInvocations, i =>
        {
            Assert.Equal(PublicEventObjective.TeleportToUpperDeck, i.Arguments[0]);
            Assert.Equal(1, i.Arguments[1]);
        });
    }

    [Fact]
    public void CrewQuatersDoor_OnEnterRange_UpdatesEnterCrewQuartersObjective()
    {
        var script = new CrewQuatersDoorEntityScript();
        IDoorEntity door = CreateDoorWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out _);
        IPlayer player = CreatePlayer(1ul);

        script.OnLoad(door);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.EnterCrewQuarters, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void CrewQuatersDoor_OnEnterRange_WhenSamePlayerReenters_CreditsOnce()
    {
        var script = new CrewQuatersDoorEntityScript();
        IDoorEntity door = CreateDoorWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out _);
        IPlayer player = CreatePlayer(1ul);

        script.OnLoad(door);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.EnterCrewQuarters, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void CrewQuatersDoor_OnEnterRange_WhenDifferentPlayersEnter_CreditsOnce()
    {
        var script = new CrewQuatersDoorEntityScript();
        IDoorEntity door = CreateDoorWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out _);

        script.OnLoad(door);
        script.OnEnterRange(CreatePlayer(1ul));
        script.OnEnterRange(CreatePlayer(2ul));

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.EnterCrewQuarters, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void CrewQuatersDoor_OnEnterRange_WithNonPlayer_DoesNotCredit()
    {
        var script = new CrewQuatersDoorEntityScript();
        IDoorEntity door = CreateDoorWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out _);
        IGridEntity nonPlayer = RecordingDispatchProxy<IGridEntity>.Create(out _);

        script.OnLoad(door);
        script.OnEnterRange(nonPlayer);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void SecurityChiefKondovich_OnDeath_UpdatesSecurityChiefObjective()
    {
        var script = new SecurityChiefKondovichEntityScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _),
            RecordingDispatchProxy<IScriptEventFactory>.Create(out _),
            RecordingDispatchProxy<IScriptEventManager>.Create(out _));
        ICreatureEntity creature = CreateCreatureWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(creature);
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.KillSecurityChiefKondovich, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void SecurityChiefKondovich_OnDeath_WhenRepeated_CreditsSecurityChiefObjectiveOnce()
    {
        var script = new SecurityChiefKondovichEntityScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _),
            RecordingDispatchProxy<IScriptEventFactory>.Create(out _),
            RecordingDispatchProxy<IScriptEventManager>.Create(out _));
        ICreatureEntity creature = CreateCreatureWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(creature);
        script.OnDeath();
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.KillSecurityChiefKondovich, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    public static IEnumerable<object[]> CrewLogMessages()
    {
        yield return [(byte)0, CommunicatorMessage.CrewLog1];
        yield return [(byte)1, CommunicatorMessage.CrewLog2];
        yield return [(byte)2, CommunicatorMessage.CrewLog3];
        yield return [(byte)3, CommunicatorMessage.CrewLog4];
        yield return [(byte)4, CommunicatorMessage.CrewLog5];
        yield return [(byte)5, CommunicatorMessage.CrewLog6];
        yield return [(byte)6, CommunicatorMessage.CrewLog7];
    }

    [Theory]
    [MemberData(nameof(CrewLogMessages))]
    public void CrewLog_OnActivateSuccess_SendsMappedMessageAndUpdatesDownloadObjective(
        byte checklistIndex,
        CommunicatorMessage expectedMessage)
    {
        ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(
            out RecordingDispatchProxy<ICommunicatorMessage> communicatorMessageProxy);
        CrewLogEntityScript script = CreateCrewLogScript(
            communicatorMessage,
            out RecordingDispatchProxy<IGlobalQuestManager> questManagerProxy);
        IPlayer player = CreatePlayerWithSession(1ul, out IGameSession session);
        IWorldEntity crewLog = CreateCrewLogEntity(checklistIndex, [player], out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(crewLog);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation lookup = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
        Assert.Equal(expectedMessage, lookup.Arguments[0]);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(
            communicatorMessageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.DownloadCrewLogs, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void CrewLog_OnActivateSuccess_BroadcastsMappedMessageToAllCurrentPlayers()
    {
        ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(
            out RecordingDispatchProxy<ICommunicatorMessage> communicatorMessageProxy);
        CrewLogEntityScript script = CreateCrewLogScript(
            communicatorMessage,
            out RecordingDispatchProxy<IGlobalQuestManager> questManagerProxy);
        IPlayer firstPlayer = CreatePlayerWithSession(1ul, out IGameSession firstSession);
        IPlayer secondPlayer = CreatePlayerWithSession(2ul, out IGameSession secondSession);
        IWorldEntity crewLog = CreateCrewLogEntity(6, [firstPlayer, secondPlayer], out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(crewLog);
        script.OnActivateSuccess(firstPlayer);

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation lookup = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
        Assert.Equal(CommunicatorMessage.CrewLog7, lookup.Arguments[0]);

        var sends = communicatorMessageProxy.GetInvocations(nameof(ICommunicatorMessage.Send));
        Assert.Collection(sends,
            send => Assert.Same(firstSession, send.Arguments[0]),
            send => Assert.Same(secondSession, send.Arguments[0]));

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.DownloadCrewLogs, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void CrewLog_OnActivateSuccess_WhenRepeated_SendsMessageAgainAndCreditsOnce()
    {
        ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(
            out RecordingDispatchProxy<ICommunicatorMessage> communicatorMessageProxy);
        CrewLogEntityScript script = CreateCrewLogScript(communicatorMessage, out _);
        IPlayer player = CreatePlayerWithSession(1ul, out _);
        IWorldEntity crewLog = CreateCrewLogEntity(0, [player], out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(crewLog);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        Assert.Equal(2, communicatorMessageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)).Count);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.DownloadCrewLogs, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void CrewLog_OnActivateSuccess_WithUnknownChecklistIndex_DoesNotSendOrCredit()
    {
        CrewLogEntityScript script = CreateCrewLogScript(
            RecordingDispatchProxy<ICommunicatorMessage>.Create(out _),
            out RecordingDispatchProxy<IGlobalQuestManager> questManagerProxy);
        IPlayer player = CreatePlayerWithSession(1ul, out _);
        IWorldEntity crewLog = CreateCrewLogEntity(7, [player], out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(crewLog);
        script.OnActivateSuccess(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void EthericDriveSchematics_OnActivateSuccess_UpdatesPickupObjective()
    {
        var script = new EthericDriveSchematicsEntityScript();
        IWorldEntity schematics = CreateNonPlayerWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<INonPlayerEntity> schematicsProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(schematics);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 2);
        Assert.Equal(PublicEventObjective.PickUpDriveSchematics, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);

        Assert.Single(schematicsProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void EthericDriveSchematics_OnActivateSuccess_AfterCollected_DoesNotCreditAgain()
    {
        var script = new EthericDriveSchematicsEntityScript();
        IWorldEntity schematics = CreateNonPlayerWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<INonPlayerEntity> schematicsProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(schematics);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 2);
        Assert.Equal(PublicEventObjective.PickUpDriveSchematics, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);

        Assert.Single(schematicsProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void DriveDiagnosticsScript_IsBoundToMappedCreatureRow()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(DriveDiagnosticsEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 75222u }, attribute.CreatureId);
    }

    [Fact]
    public void DriveDiagnostics_OnActivateSuccess_UpdatesDriveDiagnosticsTargetGroupAndRemoves()
    {
        var script = new DriveDiagnosticsEntityScript();
        IWorldEntity diagnostics = CreateWorldEntityWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<IWorldEntity> diagnosticsProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(diagnostics);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(14430u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);

        Assert.Single(diagnosticsProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void DriveDiagnostics_OnActivateSuccess_AfterCollected_DoesNotCreditAgain()
    {
        var script = new DriveDiagnosticsEntityScript();
        IWorldEntity diagnostics = CreateWorldEntityWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<IWorldEntity> diagnosticsProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(diagnostics);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(14430u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);

        Assert.Single(diagnosticsProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void MedbayDoorControl_UsesCreatureFilterForMappedCreatureRow()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(MedbayDoorControlEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 71283u }, attribute.CreatureId);
    }

    [Fact]
    public void MedbayDoorControl_OnActivateSuccess_UpdatesOpenMedbayTargetGroupOnce()
    {
        var script = new MedbayDoorControlEntityScript();
        IWorldEntity control = CreateWorldEntityWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<IWorldEntity> controlProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(control);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(14064u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);

        Assert.Empty(controlProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void SparePartsCrate_OnActivateSuccess_UpdatesScavengeTargetGroupAndRemoves()
    {
        var script = new SparePartsCrateEntityScript();
        ICollectableUnitEntity crate = CreateCollectableUnitWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<ICollectableUnitEntity> crateProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(crate);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(14066u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);

        Assert.Single(crateProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void SparePartsCrate_OnActivateSuccess_WhenRepeated_CreditsAndRemovesOnce()
    {
        var script = new SparePartsCrateEntityScript();
        ICollectableUnitEntity crate = CreateCollectableUnitWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<ICollectableUnitEntity> crateProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(crate);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(14066u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);

        Assert.Single(crateProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void RepairedMedbayDoorControl_OnActivateSuccess_UpdatesRepairTargetGroupOnce()
    {
        var script = new RepairedMedbayDoorControlEntityScript();
        IWorldEntity control = CreateWorldEntityWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<IWorldEntity> controlProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(control);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(14069u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);

        Assert.Empty(controlProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void MedbayGeneratorControls_OnActivateSuccess_UpdatesGeneratorTargetGroupOnce()
    {
        var script = new MedbayGeneratorControlsEntityScript();
        IWorldEntity control = CreateWorldEntityWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<IWorldEntity> controlProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(control);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(14076u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);

        Assert.Empty(controlProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [InlineData(typeof(MainEngineeringGeneratorAlphaControlsEntityScript), 71374u)]
    [InlineData(typeof(MainEngineeringGeneratorBetaControlsEntityScript), 71375u)]
    public void MainEngineeringGeneratorControls_AreBoundToMappedControlCreatures(
        Type scriptType,
        uint expectedCreatureId)
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { expectedCreatureId }, attribute.CreatureId);
    }

    [Theory]
    [MemberData(nameof(MainEngineeringGeneratorControlScripts))]
    public void MainEngineeringGeneratorControls_OnActivateSuccess_UpdatesSpecificAndAggregateTargetGroupsOnce(
        MedbayObjectiveEntityScriptBase script,
        uint expectedTargetGroup)
    {
        IWorldEntity control = CreateWorldEntityWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<IWorldEntity> controlProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        ((IOwnedScript<IWorldEntity>)script).OnLoad(control);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective))
                .Where(i => i.Arguments.Length == 3)
                .ToList();
        Assert.Equal(2, updates.Count);
        Assert.Contains(updates, i =>
            (PublicEventObjectiveType)i.Arguments[0] == PublicEventObjectiveType.ActivateTargetGroup &&
            (uint)i.Arguments[1] == expectedTargetGroup &&
            (int)i.Arguments[2] == 1);
        Assert.Contains(updates, i =>
            (PublicEventObjectiveType)i.Arguments[0] == PublicEventObjectiveType.ActivateTargetGroup &&
            (uint)i.Arguments[1] == 14077u &&
            (int)i.Arguments[2] == 1);

        Assert.Empty(controlProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    public static IEnumerable<object[]> MainEngineeringGeneratorControlScripts()
    {
        yield return [new MainEngineeringGeneratorAlphaControlsEntityScript(), 14083u];
        yield return [new MainEngineeringGeneratorBetaControlsEntityScript(), 14084u];
    }

    [Fact]
    public void TeleporterControls_OnActivateSuccess_UpdatesRestoreTeleporterTargetGroupOnce()
    {
        var script = new TeleporterControlsEntityScript();
        IWorldEntity control = CreateWorldEntityWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<IWorldEntity> controlProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(control);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(14107u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);

        Assert.Empty(controlProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void EthericDriveControls_OnActivateSuccess_UpdatesSelfDestructTargetGroupOnce()
    {
        var script = new EthericDriveControlsEntityScript();
        IUnitEntity control = CreateUnitWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<IUnitEntity> controlProxy,
            out RecordingDispatchProxy<IMovementManager> movementManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(control);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IMovementManager>.Invocation mode = Assert.Single(
            movementManagerProxy.GetInvocations(nameof(IMovementManager.SetMode)));
        Assert.Equal(ModeType.Slide, mode.Arguments[0]);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(14050u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);

        Assert.Empty(controlProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void KatjaZarkhov_OnDeath_UpdatesDefeatObjectiveAndSummonsDriveSchematicsOnce()
    {
        ICreatureInfo creatureInfo = RecordingDispatchProxy<ICreatureInfo>.Create(out _);
        INonPlayerEntity summonedSchematics = RecordingDispatchProxy<INonPlayerEntity>.Create(out _);
        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(out var summonFactoryProxy);
        summonFactoryProxy.SetMethodReturn(nameof(IEntitySummonFactory.Summon), summonedSchematics);
        ICreatureInfoManager creatureInfoManager = RecordingDispatchProxy<ICreatureInfoManager>.Create(out var creatureInfoManagerProxy);
        creatureInfoManagerProxy.SetMethodReturn(nameof(ICreatureInfoManager.GetCreatureInfo), creatureInfo);
        var script = new KatjaZarkhovEntityScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _),
            RecordingDispatchProxy<IScriptEventFactory>.Create(out _),
            RecordingDispatchProxy<IScriptEventManager>.Create(out _),
            creatureInfoManager);
        ICreatureEntity creature = CreateCreatureWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(IWorldEntity.SummonFactory), summonFactory);

        script.OnLoad(creature);
        script.OnDeath();
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 2);
        Assert.Equal(PublicEventObjective.DefeatKatjaZarkhov, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);

        RecordingDispatchProxy<ICreatureInfoManager>.Invocation lookup = Assert.Single(
            creatureInfoManagerProxy.GetInvocations(nameof(ICreatureInfoManager.GetCreatureInfo)));
        Assert.Equal(71821u, Convert.ToUInt32(lookup.Arguments[0]));

        RecordingDispatchProxy<IEntitySummonFactory>.Invocation summon = Assert.Single(
            summonFactoryProxy.GetInvocations(nameof(IEntitySummonFactory.Summon)));
        Assert.Same(creatureInfo, summon.Arguments[0]);
    }

    public static IEnumerable<object[]> EthericOrganismObjectiveScripts()
    {
        yield return EthericOrganismScriptCase(
            (spellParametersFactory, gameTableManager) => new FeastingEthericOrganismEntityScript(spellParametersFactory, gameTableManager),
            PublicEventObjective.DefeatEthericOrganisms,
            [71621u, 71633u],
            71628u);
        yield return EthericOrganismScriptCase(
            (spellParametersFactory, gameTableManager) => new TeleporterEthericOrganismEntityScript(spellParametersFactory, gameTableManager),
            PublicEventObjective.DefeatEthericOrganisms2,
            [71628u, 71634u],
            71621u);
        yield return EthericOrganismScriptCase(
            (spellParametersFactory, gameTableManager) => new EthericEnergyRodEntityScript(spellParametersFactory, gameTableManager),
            PublicEventObjective.KillEtherChargedRavenous,
            [71014u],
            71621u);
    }

    [Theory]
    [MemberData(nameof(EthericOrganismObjectiveScripts))]
    public void EthericOrganismScripts_AreBoundToMappedNormalAndVeteranCreatureRows(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        PublicEventObjective expectedObjective,
        uint[] expectedCreatureIds,
        uint unrelatedCreatureId)
    {
        _ = expectedObjective;

        PublicEventObjectiveCreditEntityScript script = createScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(script.GetType());

        var match = new ScriptFilterMatch();
        foreach (uint expectedCreatureId in expectedCreatureIds)
        {
            IScriptFilterSearch matchingSearch = new ScriptFilterSearch()
                .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
                .FilterByCreatureId(expectedCreatureId);
            Assert.True(match.Match(matchingSearch, parameters));
        }

        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(unrelatedCreatureId);
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Theory]
    [MemberData(nameof(EthericOrganismObjectiveScripts))]
    public void EthericOrganism_OnDeath_UpdatesMappedObjectiveOnce(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        PublicEventObjective expectedObjective,
        uint[] expectedCreatureIds,
        uint unrelatedCreatureId)
    {
        _ = expectedCreatureIds;
        _ = unrelatedCreatureId;

        PublicEventObjectiveCreditEntityScript script = createScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ICreatureEntity creature = CreateCreatureWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(creature);
        script.OnDeath();
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 2);
        Assert.Equal((uint)expectedObjective, Convert.ToUInt32(update.Arguments[0]));
        Assert.Equal(1, update.Arguments[1]);
    }

    public static IEnumerable<object[]> EthericPortalObjectiveScripts()
    {
        yield return EthericPortalScriptCase(
            (eventFactory, eventManager, creatureInfoManager) => new EthericPortalSmallEntityScript(eventFactory, eventManager, creatureInfoManager),
            PublicEventObjective.DefeatTetheredOrganisms);
        yield return EthericPortalScriptCase(
            (eventFactory, eventManager, creatureInfoManager) => new EthericPortalLargeEntityScript(eventFactory, eventManager, creatureInfoManager),
            PublicEventObjective.DefeatTetheredOrganisms2);
    }

    [Theory]
    [MemberData(nameof(EthericPortalObjectiveScripts))]
    public void EthericPortal_OnDeath_UpdatesMappedPortalObjectiveAndRemovesFromMap(
        Func<IScriptEventFactory, IScriptEventManager, ICreatureInfoManager, EthericPortalEntityScript> createScript,
        PublicEventObjective expectedObjective)
    {
        EthericPortalEntityScript script = createScript(
            RecordingDispatchProxy<IScriptEventFactory>.Create(out _),
            RecordingDispatchProxy<IScriptEventManager>.Create(out _),
            RecordingDispatchProxy<ICreatureInfoManager>.Create(out _));
        INonPlayerEntity portal = CreateNonPlayerWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<INonPlayerEntity> portalProxy);

        script.OnLoad(portal);
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 2);
        Assert.Equal(expectedObjective, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);

        Assert.Single(portalProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [MemberData(nameof(EthericPortalObjectiveScripts))]
    public void EthericPortal_OnDeath_WhenRepeated_UpdatesMappedObjectiveAndRemovesOnce(
        Func<IScriptEventFactory, IScriptEventManager, ICreatureInfoManager, EthericPortalEntityScript> createScript,
        PublicEventObjective expectedObjective)
    {
        EthericPortalEntityScript script = createScript(
            RecordingDispatchProxy<IScriptEventFactory>.Create(out _),
            RecordingDispatchProxy<IScriptEventManager>.Create(out _),
            RecordingDispatchProxy<ICreatureInfoManager>.Create(out _));
        INonPlayerEntity portal = CreateNonPlayerWithPublicEventManager(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
            out RecordingDispatchProxy<INonPlayerEntity> portalProxy);

        script.OnLoad(portal);
        script.OnDeath();
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 2);
        Assert.Equal(expectedObjective, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);

        Assert.Single(portalProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateTrigger(out IGridTriggerEntity trigger)
    {
        trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(out var triggerProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        triggerProxy.SetProperty(nameof(IGridEntity.Map), map);

        return publicEventManagerProxy;
    }

    private static IPlayer CreatePlayer(ulong characterId)
    {
        return CreatePlayer(characterId, out _);
    }

    private static IPlayer CreatePlayer(ulong characterId, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        return player;
    }

    private static IPlayer CreatePlayerWithSession(ulong characterId, out IGameSession session)
    {
        IPlayer player = CreatePlayer(characterId, out RecordingDispatchProxy<IPlayer> playerProxy);
        session = RecordingDispatchProxy<IGameSession>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static IDoorEntity CreateDoorWithPublicEventManager(
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
        out RecordingDispatchProxy<IDoorEntity> doorProxy)
    {
        IDoorEntity door = RecordingDispatchProxy<IDoorEntity>.Create(out doorProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        doorProxy.SetProperty(nameof(IGridEntity.Map), map);

        return door;
    }

    private static CrewLogEntityScript CreateCrewLogScript(
        ICommunicatorMessage communicatorMessage,
        out RecordingDispatchProxy<IGlobalQuestManager> questManagerProxy)
    {
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodReturn(nameof(IGlobalQuestManager.GetCommunicatorMessage), communicatorMessage);
        return new CrewLogEntityScript(globalQuestManager);
    }

    private static IWorldEntity CreateCrewLogEntity(
        byte checklistIndex,
        IEnumerable<IPlayer> players,
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        IWorldEntity crewLog = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> crewLogProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IMapInstance map = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);
        crewLogProxy.SetProperty(nameof(IGridEntity.Map), map);
        crewLogProxy.SetProperty(nameof(IWorldEntity.QuestChecklistIdx), checklistIndex);

        return crewLog;
    }

    private static ICreatureEntity CreateCreatureWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        return CreateCreatureWithPublicEventManager(out publicEventManagerProxy, out _);
    }

    private static ICreatureEntity CreateCreatureWithPublicEventManager(
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
        out RecordingDispatchProxy<ICreatureEntity> creatureProxy)
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out creatureProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        creatureProxy.SetProperty(nameof(IGridEntity.Map), map);

        return creature;
    }

    private static INonPlayerEntity CreateNonPlayerWithPublicEventManager(
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
        out RecordingDispatchProxy<INonPlayerEntity> entityProxy)
    {
        INonPlayerEntity entity = RecordingDispatchProxy<INonPlayerEntity>.Create(out entityProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);

        return entity;
    }

    private static IWorldEntity CreateWorldEntityWithPublicEventManager(
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
        out RecordingDispatchProxy<IWorldEntity> entityProxy)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);

        return entity;
    }

    private static IUnitEntity CreateUnitWithPublicEventManager(
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
        out RecordingDispatchProxy<IUnitEntity> entityProxy,
        out RecordingDispatchProxy<IMovementManager> movementManagerProxy)
    {
        IUnitEntity entity = RecordingDispatchProxy<IUnitEntity>.Create(out entityProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out movementManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        entityProxy.SetProperty(nameof(IWorldEntity.MovementManager), movementManager);

        return entity;
    }

    private static ICollectableUnitEntity CreateCollectableUnitWithPublicEventManager(
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
        out RecordingDispatchProxy<ICollectableUnitEntity> entityProxy)
    {
        ICollectableUnitEntity entity = RecordingDispatchProxy<ICollectableUnitEntity>.Create(out entityProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);

        return entity;
    }

    private static object[] EthericPortalScriptCase(
        Func<IScriptEventFactory, IScriptEventManager, ICreatureInfoManager, EthericPortalEntityScript> createScript,
        PublicEventObjective expectedObjective)
    {
        return [createScript, expectedObjective];
    }

    private static object[] EthericOrganismScriptCase(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        PublicEventObjective expectedObjective,
        uint[] expectedCreatureIds,
        uint unrelatedCreatureId)
    {
        return [createScript, expectedObjective, expectedCreatureIds, unrelatedCreatureId];
    }
}
