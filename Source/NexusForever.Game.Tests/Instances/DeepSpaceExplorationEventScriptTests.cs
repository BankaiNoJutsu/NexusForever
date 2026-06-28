using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.GameTable;
using NexusForever.Script.Instance.Expedition.DeepSpaceExploration;
using NexusForever.Script.Instance.Expedition.DeepSpaceExploration.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Instances;

public class DeepSpaceExplorationEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialTalkToCaptainTyraniaPhase()
    {
        var script = new DeepSpaceExplorationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.TalkToCaptainTyrania, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_TalkToCaptainTyrania_ActivatesMappedObjective()
    {
        var script = new DeepSpaceExplorationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToCaptainTyrania);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.TalkTo48900InTheGalacticObserversStarhelmCommandDeck, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_TalkToCrewMembers_ActivatesMappedObjective()
    {
        var script = new DeepSpaceExplorationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToCrewMembers);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.TalkToCrewMembers, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_DisableSpecimenContainmentCells_ActivatesMappedObjective()
    {
        var script = new DeepSpaceExplorationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DisableSpecimenContainmentCells);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.DisableSpecimenContainmentCells, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_KillSteelfinForces_ActivatesMappedObjective()
    {
        var script = new DeepSpaceExplorationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.KillSteelfinForces);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.KillSteelfinForces, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_CaptainTyraniaSuccess_SetsCrewMembersPhase()
    {
        var script = new DeepSpaceExplorationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.TalkTo48900InTheGalacticObserversStarhelmCommandDeck,
            PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation[] phaseChanges =
            eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)).ToArray();
        Assert.Equal(2, phaseChanges.Length);
        Assert.Equal(PublicEventPhase.TalkToCaptainTyrania, phaseChanges[0].Arguments[0]);
        Assert.Equal(PublicEventPhase.TalkToCrewMembers, phaseChanges[1].Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_CaptainTyraniaActive_DoesNotAdvance()
    {
        var script = new DeepSpaceExplorationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.TalkTo48900InTheGalacticObserversStarhelmCommandDeck,
            PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_TalkToCrewMembersSuccess_SetsContainmentCellPhase()
    {
        var script = new DeepSpaceExplorationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.TalkToCrewMembers,
            PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation[] phaseChanges =
            eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)).ToArray();
        Assert.Equal(2, phaseChanges.Length);
        Assert.Equal(PublicEventPhase.TalkToCaptainTyrania, phaseChanges[0].Arguments[0]);
        Assert.Equal(PublicEventPhase.DisableSpecimenContainmentCells, phaseChanges[1].Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DisableSpecimenContainmentCellsSuccess_SetsKillSteelfinForcesPhase()
    {
        var script = new DeepSpaceExplorationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.DisableSpecimenContainmentCells,
            PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation[] phaseChanges =
            eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)).ToArray();
        Assert.Equal(2, phaseChanges.Length);
        Assert.Equal(PublicEventPhase.TalkToCaptainTyrania, phaseChanges[0].Arguments[0]);
        Assert.Equal(PublicEventPhase.KillSteelfinForces, phaseChanges[1].Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KillSteelfinForcesSuccess_DoesNotInventFollowUp()
    {
        var script = new DeepSpaceExplorationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.KillSteelfinForces,
            PublicEventStatus.Succeeded));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void CaptainTyraniaEntityScript_UsesMappedCreatureFilter()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(CaptainTyraniaEntityScript)
                .GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 48900u }, attribute.CreatureId);
    }

    [Fact]
    public void GalacticObserverCrewMemberEntityScript_UsesMappedCrewCreatureFilters()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(GalacticObserverCrewMemberEntityScript)
                .GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 49474u, 49475u, 49476u, 49477u, 49479u, 49480u }, attribute.CreatureId);
    }

    [Fact]
    public void SpecimenContainmentCellTerminalEntityScript_UsesMappedCreatureFilters()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(SpecimenContainmentCellTerminalEntityScript)
                .GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 48991u, 49800u, 49801u, 49802u }, attribute.CreatureId);
    }

    [Fact]
    public void SteelSerpentMainframeCortexEntityScript_UsesMappedCreatureFilter()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(SteelSerpentMainframeCortexEntityScript)
                .GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 48996u }, attribute.CreatureId);
    }

    [Fact]
    public void SteelfinForcesEntityScript_UsesMappedCreatureFilters()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(SteelfinForcesEntityScript)
                .GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 49408u, 48740u, 48702u, 49406u, 48762u }, attribute.CreatureId);
    }

    [Fact]
    public void CaptainTyrania_OnActivateSuccess_UpdatesMappedTalkTargetGroup()
    {
        var script = new CaptainTyraniaEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateWorldEntityWithPublicEventManager(out IWorldEntity captain);

        script.OnLoad(captain);
        script.OnActivateSuccess(player);

        AssertPlayerObjectiveUpdate(
            publicEventManagerProxy,
            player,
            PublicEventObjectiveType.TalkTo,
            6873u);
    }

    [Fact]
    public void CaptainTyrania_OnActivateSuccess_WhenRepeated_CreditsOnce()
    {
        var script = new CaptainTyraniaEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateWorldEntityWithPublicEventManager(out IWorldEntity captain);

        script.OnLoad(captain);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        AssertPlayerObjectiveUpdate(
            publicEventManagerProxy,
            player,
            PublicEventObjectiveType.TalkTo,
            6873u);
    }

    [Fact]
    public void GalacticObserverCrewMember_OnActivateSuccess_UpdatesMappedTalkChecklistTargetGroup()
    {
        var script = new GalacticObserverCrewMemberEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateWorldEntityWithPublicEventManager(out IWorldEntity crewMember);

        script.OnLoad(crewMember);
        script.OnActivateSuccess(player);

        AssertPlayerObjectiveUpdate(
            publicEventManagerProxy,
            player,
            PublicEventObjectiveType.TalkToChecklist,
            6966u);
    }

    [Fact]
    public void SpecimenContainmentCellTerminal_OnActivateSuccess_UpdatesMappedTargetGroupChecklist()
    {
        var script = new SpecimenContainmentCellTerminalEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateWorldEntityWithPublicEventManager(out IWorldEntity terminal);

        script.OnLoad(terminal);
        script.OnActivateSuccess(player);

        AssertObjectiveUpdate(
            publicEventManagerProxy,
            PublicEventObjectiveType.ActivateTargetGroupChecklist,
            6902u);
    }

    [Fact]
    public void SpecimenContainmentCellTerminal_OnActivateSuccess_WhenRepeated_CreditsOnce()
    {
        var script = new SpecimenContainmentCellTerminalEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateWorldEntityWithPublicEventManager(out IWorldEntity terminal);

        script.OnLoad(terminal);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        AssertObjectiveUpdate(
            publicEventManagerProxy,
            PublicEventObjectiveType.ActivateTargetGroupChecklist,
            6902u);
    }

    [Fact]
    public void SteelSerpentMainframeCortex_OnActivateSuccess_UpdatesMappedTargetGroup()
    {
        var script = new SteelSerpentMainframeCortexEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateWorldEntityWithPublicEventManager(out IWorldEntity cortex);

        script.OnLoad(cortex);
        script.OnActivateSuccess(player);

        AssertObjectiveUpdate(
            publicEventManagerProxy,
            PublicEventObjectiveType.ActivateTargetGroup,
            6905u);
    }

    [Fact]
    public void SteelSerpentMainframeCortex_OnActivateSuccess_WhenRepeated_CreditsOnce()
    {
        var script = new SteelSerpentMainframeCortexEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateWorldEntityWithPublicEventManager(out IWorldEntity cortex);

        script.OnLoad(cortex);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        AssertObjectiveUpdate(
            publicEventManagerProxy,
            PublicEventObjectiveType.ActivateTargetGroup,
            6905u);
    }

    [Fact]
    public void SteelfinForces_OnDeath_UpdatesMappedKillObjective()
    {
        var script = new SteelfinForcesEntityScript(
            CreateSpellParametersFactory(),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateCreatureWithPublicEventManager(out ICreatureEntity steelfin);

        script.OnLoad(steelfin);
        script.OnDeath();

        AssertDirectObjectiveUpdate(
            publicEventManagerProxy,
            PublicEventObjective.KillSteelfinForces);
    }

    [Fact]
    public void SteelfinForces_OnDeath_WhenRepeated_CreditsOnce()
    {
        var script = new SteelfinForcesEntityScript(
            CreateSpellParametersFactory(),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateCreatureWithPublicEventManager(out ICreatureEntity steelfin);

        script.OnLoad(steelfin);
        script.OnDeath();
        script.OnDeath();

        AssertDirectObjectiveUpdate(
            publicEventManagerProxy,
            PublicEventObjective.KillSteelfinForces);
    }

    [Fact]
    public void GalacticObserverCrewMember_OnActivateSuccess_WhenRepeated_CreditsOnce()
    {
        var script = new GalacticObserverCrewMemberEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateWorldEntityWithPublicEventManager(out IWorldEntity crewMember);

        script.OnLoad(crewMember);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        AssertPlayerObjectiveUpdate(
            publicEventManagerProxy,
            player,
            PublicEventObjectiveType.TalkToChecklist,
            6966u);
    }

    private static IPublicEvent CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
    }

    private static IPublicEventObjective CreateObjective(PublicEventObjective objective, PublicEventStatus status)
    {
        IPublicEventObjective eventObjective = RecordingDispatchProxy<IPublicEventObjective>.Create(out RecordingDispatchProxy<IPublicEventObjective> objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry
        {
            Id = (uint)objective
        });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
        return eventObjective;
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(out IWorldEntity entity)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> entityProxy);
        entityProxy.SetProperty(nameof(IWorldEntity.Map), map);

        return publicEventManagerProxy;
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateCreatureWithPublicEventManager(out ICreatureEntity entity)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        entity = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> entityProxy);
        entityProxy.SetProperty(nameof(IWorldEntity.Map), map);

        return publicEventManagerProxy;
    }

    private static IFactory<ISpellParameters> CreateSpellParametersFactory()
    {
        IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
        factoryProxy.SetMethodReturn(nameof(IFactory<ISpellParameters>.Resolve), RecordingDispatchProxy<ISpellParameters>.Create(out _));
        return factory;
    }

    private static void AssertPlayerObjectiveUpdate(
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
        IPlayer player,
        PublicEventObjectiveType objectiveType,
        uint objectId)
    {
        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Same(player, update.Arguments[0]);
        Assert.Equal(objectiveType, update.Arguments[1]);
        Assert.Equal(objectId, update.Arguments[2]);
        Assert.Equal(objectiveType == PublicEventObjectiveType.TalkToChecklist ? 0 : 1, update.Arguments[3]);
    }

    private static void AssertObjectiveUpdate(
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
        PublicEventObjectiveType objectiveType,
        uint objectId)
    {
        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(objectiveType, update.Arguments[0]);
        Assert.Equal(objectId, update.Arguments[1]);
        Assert.Equal(objectiveType == PublicEventObjectiveType.ActivateTargetGroupChecklist ? 0 : 1, update.Arguments[2]);
    }

    private static void AssertDirectObjectiveUpdate(
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
        PublicEventObjective objective)
    {
        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal((uint)objective, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }
}
