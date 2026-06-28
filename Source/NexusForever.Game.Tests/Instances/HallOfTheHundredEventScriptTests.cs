using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance.WorldStory.HallOfTheHundred;
using NexusForever.Script.Instance.WorldStory.HallOfTheHundred.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Instances;

public class HallOfTheHundredEventScriptTests
{
    [Fact]
    public void EventScript_IsOwnedByVaultOfTheArchonPublicEvent()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(HallOfTheHundredEventScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 666u }, attribute.Id);
    }

    [Fact]
    public void OnLoad_SetsInitialGatherPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.GatherInFrontOfTheCrashedShip, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_GatherInFront_CreatesOpeningGatherTriggerAndActivatesObjectives()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.GatherInFrontOfTheCrashedShip);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.GatherInFrontOfTheCrashedShip);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.GatherInFrontOfTheCrashedShipAlt);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 48684u, 8396u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-952.698f, -710.584f, -156.552f));
    }

    [Fact]
    public void OnPublicEventPhase_SpeakWithDorianAndArtemis_CreatesOpeningConversationTriggerAndActivatesObjectives()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.SpeakWithDorianAndArtemis);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.SpeakWithDorian);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.SpeakWithArtemis);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 50547u, 8278u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-892.391f, -710.492f, -197.729f));
    }

    [Fact]
    public void OnPublicEventPhase_RejoinGroupAtMysteriousTree_CreatesOpeningConversationTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.RejoinGroupAtMysteriousTree);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.RejoinGroupAtMysteriousTree);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.FollowToMysteriousTree);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.FollowToMysteriousTree, 0);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 50547u, 8278u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-892.391f, -710.492f, -197.729f));
    }

    [Fact]
    public void OnPublicEventPhase_ListenToPlan_ActivatesAndCreditsScriptObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.ListenToPlan);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.LearnAboutBloodhearthTrees);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.ListenToPlan);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.LearnAboutBloodhearthTrees, 1);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.ListenToPlan, 1);
    }

    [Fact]
    public void OnPublicEventPhase_InvestigateLockedGate_CreatesLockedGateTriggerAndActivatesObjectives()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.InvestigateLockedGate);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.InvestigateLockedGate);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.NavigateVaregorPass);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 48658u, 7834u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(368.129f, -620.752f, -1459.28f));
    }

    [Fact]
    public void OnPublicEventPhase_DefeatVaregor_ActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatVaregor);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatVaregor);
    }

    [Fact]
    public void OnPublicEventPhase_ReviveDorianAndArtemis_ActivatesObjectives()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.ReviveDorianAndArtemis);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.ReviveDorian);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.ReviveArtemis);
    }

    [Fact]
    public void OnPublicEventPhase_MeetAtTheBridge_CreatesBridgeTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.MeetAtTheBridge);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.MeetAtTheBridge);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 50596u, 7836u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(370.969f, -654.254f, -1260.47f));
    }

    [Fact]
    public void OnPublicEventPhase_DestroyBridgeIce_ActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.DestroyBridgeIce);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.PlaceExplosivesOnBridgeIce);
    }

    [Fact]
    public void OnPublicEventPhase_FollowIntoKelHavikFortress_ActivatesAndCreditsScriptObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.FollowIntoKelHavikFortress);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.FollowIntoKelHavikFortress);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.FollowIntoKelHavikFortress, 0);
    }

    [Fact]
    public void OnPublicEventPhase_RegroupOutsideKelHavikFortress_CreatesKelHavikTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.RegroupOutsideKelHavikFortress);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.RegroupOutsideKelHavikFortress);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 50597u, 7857u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(618.068f, -654.879f, -987.026f));
    }

    [Fact]
    public void OnPublicEventPhase_StudyMysteriousTablets_ActivatesAndCreditsScriptObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.StudyMysteriousTablets);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.StudyMysteriousTablets);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.StudyMysteriousTablets, 1);
    }

    [Fact]
    public void OnPublicEventPhase_EnterWatchtower_CreatesWatchtowerTurnstileAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateTurnstileScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTurnstileTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.EnterWatchtower);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.EnterWatchtower);

        CreatedTurnstileTrigger trigger = Assert.Single(createdTriggers);
        AssertTurnstileInitialised(trigger, 48499u, 9.73506f, 7861u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(751.826f, -657.249f, -992.613f));
    }

    [Fact]
    public void OnPublicEventPhase_GatherAtFirstFloor_CreatesFirstFloorTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.GatherAtFirstFloor);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.GatherAtFirstFloor);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 48479u, 8527u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(1399.28f, -865.55f, -1175.43f));
    }

    [Fact]
    public void OnPublicEventPhase_ExploreUpperFloors_CreatesUpperFloorsTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.ExploreUpperFloors);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.ExploreUpperFloors);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatUnboundFlameElemental);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatIceboundOverlord);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 50579u, 8345u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(1402.81f, -745.789f, -1176.99f));
    }

    [Fact]
    public void OnPublicEventPhase_ExploreTopFloor_CreatesTopFloorTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.ExploreTopFloor);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.ExploreTopFloor);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatDarkwitchYotul);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 50367u, 8349u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(1407.9f, -626.027f, -1174.63f));
    }

    [Fact]
    public void OnPublicEventPhase_UseElevatorToBottomFloor_ActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.UseElevatorToBottomFloor);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.UseElevatorToBottomFloor);
    }

    [Fact]
    public void OnPublicEventPhase_FollowBackToCourtyard_ActivatesAndCreditsScriptObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.FollowBackToCourtyard);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.FollowBackToCourtyard);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.FollowBackToCourtyard, 0);
    }

    [Fact]
    public void OnPublicEventPhase_RegroupInKelHavikCourtyard_CreatesCourtyardTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.RegroupInKelHavikCourtyard);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.RegroupInKelHavikCourtyard);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 51027u, 8304u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(799.909f, -664.708f, -800.904f));
    }

    [Fact]
    public void OnPublicEventPhase_MeetAtKelHavikCourtyard_CreatesCourtyardStatueTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.MeetAtKelHavikCourtyard);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.MeetAtKelHavikCourtyard);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 50605u, 7894u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(751.019f, -647.451f, -852.278f));
    }

    [Fact]
    public void OnPublicEventPhase_FindCourtyardKeys_ActivatesKeyObjectives()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindCourtyardKeys);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.FindCourtyardKeys);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.CollectKeyFragments);
    }

    [Fact]
    public void OnPublicEventPhase_PlaceCourtyardKeys_ActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.PlaceCourtyardKeys);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.PlaceCourtyardKeys);
    }

    [Fact]
    public void OnPublicEventPhase_GatherAtVaultDoor_CreatesVaultDoorTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.GatherAtVaultDoor);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.GatherAtVaultDoor);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 48426u, 8314u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(1210.14f, -649.188f, -503.206f));
    }

    [Fact]
    public void OnPublicEventPhase_WaitVaultDoorOpening_ActivatesAndCreditsScriptObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.WaitVaultDoorOpening);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.WaitVaultDoorOpening);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.WaitVaultDoorOpening, 0);
    }

    [Fact]
    public void OnPublicEventPhase_FollowInsideHall_ActivatesAndCreditsScriptObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.FollowInsideHall);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.FollowInsideHall);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.FollowInsideHall, 1);
    }

    [Fact]
    public void OnPublicEventPhase_FollowToVaultEntrance_ActivatesAndCreditsScriptObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.FollowToVaultEntrance);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.FollowToVaultEntrance);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.FollowToVaultEntrance, 0);
    }

    [Fact]
    public void OnPublicEventPhase_MeetAtVaultEntrance_CreatesVaultEntranceTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.MeetAtVaultEntrance);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.MeetAtVaultEntrance);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 50645u, 8298u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(1361.12f, -626.637f, -414.843f));
    }

    [Fact]
    public void OnPublicEventPhase_FollowIntoVault_ActivatesAndCreditsScriptWithoutCountObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.FollowIntoVault);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.FollowIntoVault);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.FollowIntoVault, 1);
    }

    [Fact]
    public void OnPublicEventPhase_MeetAtAccessTerminal_CreatesAccessTerminalTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.MeetAtAccessTerminal);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.MeetAtAccessTerminal);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 51020u, 8295u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(1451.66f, -673.04f, -498.369f));
    }

    [Fact]
    public void OnPublicEventPhase_DeactivateVaultForceField_ActivatesAndCreditsScriptObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.DeactivateVaultForceField);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DeactivateVaultForceField);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.DeactivateVaultForceField, 1);
    }

    [Fact]
    public void OnPublicEventPhase_WatchHolocubeExplainVault_ActivatesAndCreditsScriptObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.WatchHolocubeExplainVault);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.WatchHolocubeExplainVault);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.WatchHolocubeExplainVault, 1);
    }

    [Fact]
    public void OnPublicEventPhase_SpeakAboutHolocube_ActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.SpeakAboutHolocube);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.SpeakAboutHolocube);
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
    }

    [Fact]
    public void OnPublicEventPhase_DestroyVaultConstructs_ActivatesObjectiveOnly()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.DestroyVaultConstructs);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DestroyVaultConstructs);
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
    }

    [Fact]
    public void OnPublicEventPhase_DefeatHarizog_ActivatesBossAndSummonedAddObjectives()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatHarizog);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.WitnessHarizogBreakFree);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatHarizog);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatHavikShiverhound);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatDarkwitchUhrga);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatHavikHonorguard);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.WitnessHarizogBreakFree, 0);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatOsunBlockingTheWay_ActivatesObjectiveOnly()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatOsunBlockingTheWay);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatOsunBlockingTheWay);
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
    }

    [Fact]
    public void OnPublicEventPhase_MeetAtVaultExit_CreatesVaultExitTriggerAndActivatesObjective()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.MeetAtVaultExit);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.MeetAtVaultExit);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 50702u, 8339u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(1372.81f, -628.495f, -429.714f));
    }

    [Fact]
    public void OpeningGatherTrigger_PlayerEnter_UpdatesOpeningGatherObjective()
    {
        var script = new OpeningGatherGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8396u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void OpeningGatherTrigger_PlayerReenters_UpdatesOpeningGatherObjectiveOnce()
    {
        var script = new OpeningGatherGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8396u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void OpeningGatherTrigger_NonPlayerEnter_DoesNotUpdateOpeningGatherObjective()
    {
        var script = new OpeningGatherGridTriggerEntityScript();
        IGridEntity nonPlayer = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(nonPlayer);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void OpeningConversationTrigger_IsBoundToOpeningConversationObject()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(OpeningConversationGridTriggerEntityScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 8278u }, attribute.Id);
    }

    [Fact]
    public void OpeningConversationTrigger_PlayerEnter_UpdatesOpeningConversationObjectives()
    {
        var script = new OpeningConversationGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8278u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void OpeningConversationTrigger_PlayerReenters_UpdatesOpeningConversationObjectivesOnce()
    {
        var script = new OpeningConversationGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8278u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void OpeningConversationTrigger_NonPlayerEnter_DoesNotUpdateOpeningConversationObjectives()
    {
        var script = new OpeningConversationGridTriggerEntityScript();
        IGridEntity nonPlayer = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(nonPlayer);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void LockedGateInvestigationTrigger_IsBoundToLockedGateObject()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(LockedGateInvestigationGridTriggerEntityScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 7834u }, attribute.Id);
    }

    [Fact]
    public void LockedGateInvestigationTrigger_PlayerEnter_UpdatesLockedGateObjectives()
    {
        var script = new LockedGateInvestigationGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        var updates = publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Equal(2, updates.Count);
        Assert.Contains(updates, update =>
            (PublicEventObjective)update.Arguments[0] == PublicEventObjective.InvestigateLockedGate
            && (int)update.Arguments[1] == 1);
        Assert.Contains(updates, update =>
            (PublicEventObjectiveType)update.Arguments[0] == PublicEventObjectiveType.ParticipantsInTriggerVolume
            && (uint)update.Arguments[1] == 7879u
            && (int)update.Arguments[2] == 1);
    }

    [Fact]
    public void LockedGateInvestigationTrigger_PlayerReenters_UpdatesLockedGateObjectiveOnce()
    {
        var script = new LockedGateInvestigationGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        var updates = publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Equal(2, updates.Count);
        Assert.Contains(updates, update =>
            (PublicEventObjective)update.Arguments[0] == PublicEventObjective.InvestigateLockedGate
            && (int)update.Arguments[1] == 1);
        Assert.Contains(updates, update =>
            (PublicEventObjectiveType)update.Arguments[0] == PublicEventObjectiveType.ParticipantsInTriggerVolume
            && (uint)update.Arguments[1] == 7879u
            && (int)update.Arguments[2] == 1);
    }

    [Fact]
    public void LockedGateInvestigationTrigger_NonPlayerEnter_DoesNotUpdateLockedGateObjective()
    {
        var script = new LockedGateInvestigationGridTriggerEntityScript();
        IGridEntity nonPlayer = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(nonPlayer);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void BridgeMeetTrigger_IsBoundToBridgeMeetObject()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(BridgeMeetGridTriggerEntityScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 7836u }, attribute.Id);
    }

    [Fact]
    public void BridgeMeetTrigger_PlayerEnter_UpdatesBridgeMeetObjective()
    {
        var script = new BridgeMeetGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(7836u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void BridgeMeetTrigger_PlayerReenters_UpdatesBridgeMeetObjectiveOnce()
    {
        var script = new BridgeMeetGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(7836u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void BridgeMeetTrigger_NonPlayerEnter_DoesNotUpdateBridgeMeetObjective()
    {
        var script = new BridgeMeetGridTriggerEntityScript();
        IGridEntity nonPlayer = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(nonPlayer);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void VaultEntranceTrigger_IsBoundToVaultEntranceObject()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(VaultEntranceGridTriggerEntityScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 8298u }, attribute.Id);
    }

    [Fact]
    public void VaultEntranceTrigger_PlayerEnter_UpdatesVaultEntranceObjective()
    {
        var script = new VaultEntranceGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8298u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void VaultEntranceTrigger_PlayerReenters_UpdatesVaultEntranceObjectiveOnce()
    {
        var script = new VaultEntranceGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8298u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void VaultEntranceTrigger_NonPlayerEnter_DoesNotUpdateVaultEntranceObjective()
    {
        var script = new VaultEntranceGridTriggerEntityScript();
        IGridEntity nonPlayer = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(nonPlayer);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void VaultDoorTrigger_IsBoundToVaultDoorObject()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(VaultDoorGridTriggerEntityScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 8314u }, attribute.Id);
    }

    [Fact]
    public void VaultDoorTrigger_PlayerEnter_UpdatesVaultDoorObjective()
    {
        var script = new VaultDoorGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8314u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void VaultDoorTrigger_PlayerReenters_UpdatesVaultDoorObjectiveOnce()
    {
        var script = new VaultDoorGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8314u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void VaultDoorTrigger_NonPlayerEnter_DoesNotUpdateVaultDoorObjective()
    {
        var script = new VaultDoorGridTriggerEntityScript();
        IGridEntity nonPlayer = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(nonPlayer);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void VaultExitTrigger_IsBoundToVaultExitObject()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(VaultExitGridTriggerEntityScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 8339u }, attribute.Id);
    }

    [Fact]
    public void VaultExitTrigger_PlayerEnter_UpdatesVaultExitObjective()
    {
        var script = new VaultExitGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8339u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void VaultExitTrigger_PlayerReenters_UpdatesVaultExitObjectiveOnce()
    {
        var script = new VaultExitGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8339u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void VaultExitTrigger_NonPlayerEnter_DoesNotUpdateVaultExitObjective()
    {
        var script = new VaultExitGridTriggerEntityScript();
        IGridEntity nonPlayer = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(nonPlayer);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void AccessTerminalTrigger_IsBoundToAccessTerminalObject()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(AccessTerminalGridTriggerEntityScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 8295u }, attribute.Id);
    }

    [Fact]
    public void AccessTerminalTrigger_PlayerEnter_UpdatesAccessTerminalObjective()
    {
        var script = new AccessTerminalGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8295u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void AccessTerminalTrigger_PlayerReenters_UpdatesAccessTerminalObjectiveOnce()
    {
        var script = new AccessTerminalGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(8295u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void AccessTerminalTrigger_NonPlayerEnter_DoesNotUpdateAccessTerminalObjective()
    {
        var script = new AccessTerminalGridTriggerEntityScript();
        IGridEntity nonPlayer = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(nonPlayer);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void CourtyardStatueTrigger_IsBoundToCourtyardStatueObject()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(CourtyardStatueGridTriggerEntityScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 7894u }, attribute.Id);
    }

    [Fact]
    public void CourtyardStatueTrigger_PlayerEnter_UpdatesCourtyardStatueObjective()
    {
        var script = new CourtyardStatueGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(7894u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void CourtyardStatueTrigger_PlayerReenters_UpdatesCourtyardStatueObjectiveOnce()
    {
        var script = new CourtyardStatueGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(7894u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void CourtyardStatueTrigger_NonPlayerEnter_DoesNotUpdateCourtyardStatueObjective()
    {
        var script = new CourtyardStatueGridTriggerEntityScript();
        IGridEntity nonPlayer = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(nonPlayer);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Theory]
    [MemberData(nameof(HallDirectObjectiveCreditCases))]
    public void DirectObjectiveScripts_OnActivateSuccess_UpdatesMappedObjectiveOnce(
        Type scriptType,
        PublicEventObjective expectedObjective,
        int expectedCount)
    {
        var script = (HallDirectObjectiveEntityScriptBase)Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity worldEntity,
            out _);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 2);
        Assert.Equal(expectedObjective, update.Arguments[0]);
        Assert.Equal(expectedCount, update.Arguments[1]);
    }

    [Theory]
    [InlineData(typeof(DorianWalkerReviveEntityScript), 67423u)]
    [InlineData(typeof(ArtemisZinReviveEntityScript), 67425u)]
    [InlineData(typeof(DorianWalkerHolocubeTalkEntityScript), 67423u)]
    [InlineData(typeof(ArtemisZinHolocubeTalkEntityScript), 67425u)]
    [InlineData(typeof(WatchtowerReturnElevatorEntityScript), 72199u)]
    [InlineData(typeof(ForcefieldPowerLinkEntityScript), 72903u)]
    [InlineData(typeof(KelHavikStaffKeyEntityScript), 71617u)]
    [InlineData(typeof(KelHavikHammerKeyEntityScript), 71618u)]
    [InlineData(typeof(KelHavikPolearmKeyEntityScript), 71619u)]
    [InlineData(typeof(VaultSecretRoomKeyFragmentEntityScript), 72958u)]
    [InlineData(typeof(KelHavikDoorLockEntityScript), 72367u)]
    [InlineData(typeof(BridgeWeakPointEntityScript), 72108u)]
    public void HallObjectiveEntityScripts_AreBoundToMappedCreatures(Type scriptType, uint expectedCreatureId)
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { expectedCreatureId }, attribute.CreatureId);
    }

    [Theory]
    [MemberData(nameof(HallTargetGroupObjectiveCreditCases))]
    public void TargetGroupScripts_OnActivateSuccess_UpdatesMappedTargetGroupOnceAndRemovesEntity(
        Type scriptType,
        PublicEventObjectiveType expectedObjectiveType,
        uint expectedTargetGroupId)
    {
        var script = (HallTargetGroupObjectiveEntityScriptBase)Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity worldEntity,
            out RecordingDispatchProxy<IWorldEntity> worldEntityProxy);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(expectedObjectiveType, update.Arguments[0]);
        Assert.Equal(expectedTargetGroupId, update.Arguments[1]);
        Assert.Equal(expectedObjectiveType == PublicEventObjectiveType.ActivateTargetGroupChecklist ? 0 : 1, update.Arguments[2]);
        Assert.Single(worldEntityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [MemberData(nameof(HallTalkObjectiveCreditCases))]
    public void TalkObjectiveScripts_OnActivateSuccess_UpdatesMappedTalkTargetGroupOncePerPlayer(
        Type scriptType,
        uint expectedTargetGroupId)
    {
        var script = (HallTalkObjectiveEntityScriptBase)Activator.CreateInstance(scriptType);
        IPlayer firstPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> firstPlayerProxy);
        IPlayer secondPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> secondPlayerProxy);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity worldEntity,
            out _);
        firstPlayerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        secondPlayerProxy.SetProperty(nameof(IPlayer.CharacterId), 43ul);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(firstPlayer);
        script.OnActivateSuccess(firstPlayer);
        script.OnActivateSuccess(secondPlayer);

        List<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates = publicEventManagerProxy
            .GetInvocations(nameof(IPublicEventManager.UpdateObjective))
            .Where(i => i.Arguments.Length == 4)
            .ToList();
        Assert.Equal(2, updates.Count);
        Assert.Same(firstPlayer, updates[0].Arguments[0]);
        Assert.Same(secondPlayer, updates[1].Arguments[0]);
        foreach (RecordingDispatchProxy<IPublicEventManager>.Invocation update in updates)
        {
            Assert.Equal(PublicEventObjectiveType.TalkTo, update.Arguments[1]);
            Assert.Equal(expectedTargetGroupId, update.Arguments[2]);
            Assert.Equal(1, update.Arguments[3]);
        }
    }

    [Fact]
    public void ForcefieldPowerLink_OnActivateSuccess_UpdatesConstructObjectiveOnceAndRemovesLink()
    {
        var script = new ForcefieldPowerLinkEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity worldEntity,
            out RecordingDispatchProxy<IWorldEntity> worldEntityProxy);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 2);
        Assert.Equal(PublicEventObjective.DestroyVaultConstructs, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
        Assert.Single(worldEntityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void BridgeWeakPoint_OnActivateSuccess_UpdatesBridgeWeakPointChecklistOnce()
    {
        var script = new BridgeWeakPointEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity worldEntity,
            out _);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(14163u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_GatherSucceeded_RemovesOpeningGatherTriggerAndStartsOpeningConversationPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            48684u,
            701u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 701u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.GatherInFrontOfTheCrashedShip, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.SpeakWithDorianAndArtemis, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_BothGatherObjectivesSucceeded_StartsOpeningConversationPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.GatherInFrontOfTheCrashedShip, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.GatherInFrontOfTheCrashedShipAlt, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.SpeakWithDorianAndArtemis, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SpeakSucceeded_RemovesOpeningConversationTrigger()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            50547u,
            702u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 702u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakWithDorian, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.RejoinGroupAtMysteriousTree, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_BothSpeakObjectivesSucceeded_StartsRejoinPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakWithDorian, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakWithArtemis, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.RejoinGroupAtMysteriousTree, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_RejoinSucceeded_RemovesOpeningConversationTriggerAndStartsListenToPlanPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            50547u,
            703u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 703u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RejoinGroupAtMysteriousTree, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ListenToPlan, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_RejoinSucceeded_StartsListenToPlanPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RejoinGroupAtMysteriousTree, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RejoinGroupAtMysteriousTree, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ListenToPlan, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ListenToPlanSucceeded_StartsLockedGateInvestigationPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ListenToPlan, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.InvestigateLockedGate, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ListenToPlanSucceeded_StartsLockedGateInvestigationPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ListenToPlan, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ListenToPlan, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.InvestigateLockedGate, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_LockedGateSucceeded_RemovesLockedGateTriggerAndStartsVaregorPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            48658u,
            704u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 704u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.InvestigateLockedGate, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DefeatVaregor, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_LockedGateSucceeded_StartsVaregorPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.InvestigateLockedGate, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.InvestigateLockedGate, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DefeatVaregor, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_VaregorPassNavigationSucceeded_DoesNotAdvanceRoute()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.NavigateVaregorPass, PublicEventStatus.Succeeded));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_VaregorSucceeded_StartsRevivePhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatVaregor, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ReviveDorianAndArtemis, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_VaregorSucceeded_StartsRevivePhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatVaregor, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatVaregor, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ReviveDorianAndArtemis, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_OnlyDorianRevived_DoesNotStartBridgePhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ReviveDorian, PublicEventStatus.Succeeded));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_BothCompanionsRevived_StartsBridgePhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ReviveDorian, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ReviveArtemis, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.MeetAtTheBridge, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_BothCompanionsRevived_StartsBridgePhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ReviveDorian, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ReviveArtemis, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ReviveDorian, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.MeetAtTheBridge, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_BridgeMeetSucceeded_RemovesBridgeTriggerAndStartsDestroyBridgeIcePhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            50596u,
            705u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 705u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtTheBridge, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DestroyBridgeIce, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_BridgeMeetSucceeded_StartsDestroyBridgeIcePhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtTheBridge, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtTheBridge, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DestroyBridgeIce, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_BridgeIceSucceeded_StartsFollowIntoKelHavikFortressPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.PlaceExplosivesOnBridgeIce, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FollowIntoKelHavikFortress, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_BridgeIceSucceeded_StartsFollowIntoKelHavikFortressPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.PlaceExplosivesOnBridgeIce, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.PlaceExplosivesOnBridgeIce, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FollowIntoKelHavikFortress, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FollowIntoKelHavikSucceeded_StartsRegroupPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FollowIntoKelHavikFortress, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.RegroupOutsideKelHavikFortress, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FollowIntoKelHavikSucceeded_StartsRegroupPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FollowIntoKelHavikFortress, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FollowIntoKelHavikFortress, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.RegroupOutsideKelHavikFortress, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_RegroupSucceeded_RemovesKelHavikTriggerAndStartsTabletStudyPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            50597u,
            706u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 706u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RegroupOutsideKelHavikFortress, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.StudyMysteriousTablets, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_RegroupSucceeded_StartsTabletStudyPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RegroupOutsideKelHavikFortress, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RegroupOutsideKelHavikFortress, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.StudyMysteriousTablets, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_StudyMysteriousTabletsSucceeded_StartsWatchtowerPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.StudyMysteriousTablets, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.EnterWatchtower, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_StudyMysteriousTabletsSucceeded_StartsWatchtowerPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.StudyMysteriousTablets, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.StudyMysteriousTablets, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.EnterWatchtower, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_EnterWatchtowerSucceeded_StartsFirstFloorPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.EnterWatchtower, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.GatherAtFirstFloor, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FirstFloorSucceeded_RemovesTriggerAndStartsUpperFloorsPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            48479u,
            707u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 707u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.GatherAtFirstFloor, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ExploreUpperFloors, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FirstFloorSucceeded_StartsUpperFloorsPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.GatherAtFirstFloor, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.GatherAtFirstFloor, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ExploreUpperFloors, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_UpperFloorsSucceeded_RemovesTriggerAndStartsTopFloorPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            50579u,
            708u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 708u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ExploreUpperFloors, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ExploreTopFloor, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_TopFloorSucceeded_RemovesTriggerAndStartsElevatorPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            50367u,
            709u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 709u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ExploreTopFloor, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.UseElevatorToBottomFloor, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ElevatorSucceeded_StartsFollowBackPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.UseElevatorToBottomFloor, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FollowBackToCourtyard, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FollowBackSucceeded_StartsCourtyardRegroupPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FollowBackToCourtyard, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.RegroupInKelHavikCourtyard, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_CourtyardRegroupSucceeded_RemovesTriggerAndStartsCourtyardStatuePhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            51027u,
            710u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 710u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RegroupInKelHavikCourtyard, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.MeetAtKelHavikCourtyard, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_CourtyardRegroupSucceeded_StartsCourtyardStatuePhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RegroupInKelHavikCourtyard, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.RegroupInKelHavikCourtyard, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.MeetAtKelHavikCourtyard, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_CourtyardStatueSucceeded_RemovesTriggerAndStartsFindKeysPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            50605u,
            712u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 712u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtKelHavikCourtyard, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FindCourtyardKeys, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_CourtyardStatueSucceeded_StartsFindKeysPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtKelHavikCourtyard, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtKelHavikCourtyard, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FindCourtyardKeys, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FindKeysSucceeded_WaitsForKeyFragments()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FindCourtyardKeys, PublicEventStatus.Succeeded));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KeyFragmentsSucceeded_WaitsForCourtyardKeys()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.CollectKeyFragments, PublicEventStatus.Succeeded));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KeyObjectivesSucceeded_StartsPlaceKeysPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FindCourtyardKeys, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.CollectKeyFragments, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.PlaceCourtyardKeys, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KeyObjectivesSucceeded_StartsPlaceKeysPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FindCourtyardKeys, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.CollectKeyFragments, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FindCourtyardKeys, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.CollectKeyFragments, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.PlaceCourtyardKeys, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_PlaceKeysSucceeded_StartsGatherAtVaultDoorPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.PlaceCourtyardKeys, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.GatherAtVaultDoor, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_PlaceKeysSucceeded_StartsGatherAtVaultDoorPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.PlaceCourtyardKeys, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.PlaceCourtyardKeys, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.GatherAtVaultDoor, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_GatherAtVaultDoorSucceeded_RemovesTriggerAndStartsWaitVaultDoorOpeningPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            48426u,
            714u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 714u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.GatherAtVaultDoor, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.WaitVaultDoorOpening, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_GatherAtVaultDoorSucceeded_StartsWaitVaultDoorOpeningPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.GatherAtVaultDoor, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.GatherAtVaultDoor, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.WaitVaultDoorOpening, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_WaitVaultDoorOpeningSucceeded_StartsFollowInsideHallPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.WaitVaultDoorOpening, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FollowInsideHall, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_WaitVaultDoorOpeningSucceeded_StartsFollowInsideHallPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.WaitVaultDoorOpening, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.WaitVaultDoorOpening, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FollowInsideHall, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FollowInsideHallSucceeded_StartsFollowToVaultEntrancePhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FollowInsideHall, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FollowToVaultEntrance, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FollowToVaultEntranceSucceeded_StartsMeetAtVaultEntrancePhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FollowToVaultEntrance, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.MeetAtVaultEntrance, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_MeetAtVaultEntranceSucceeded_RemovesTriggerAndStartsFollowIntoVaultPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            50645u,
            713u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 713u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtVaultEntrance, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FollowIntoVault, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_MeetAtVaultEntranceSucceeded_StartsFollowIntoVaultPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtVaultEntrance, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtVaultEntrance, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FollowIntoVault, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FollowIntoVaultSucceeded_StartsMeetAtAccessTerminalPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FollowIntoVault, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.MeetAtAccessTerminal, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FollowIntoVaultSucceeded_StartsMeetAtAccessTerminalPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FollowIntoVault, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FollowIntoVault, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.MeetAtAccessTerminal, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_MeetAtAccessTerminalSucceeded_RemovesTriggerAndStartsForceFieldPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            51020u,
            711u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 711u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtAccessTerminal, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DeactivateVaultForceField, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_MeetAtAccessTerminalSucceeded_StartsForceFieldPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtAccessTerminal, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtAccessTerminal, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DeactivateVaultForceField, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DeactivateVaultForceFieldSucceeded_StartsWatchHolocubePhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DeactivateVaultForceField, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.WatchHolocubeExplainVault, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DeactivateVaultForceFieldSucceeded_StartsWatchHolocubePhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DeactivateVaultForceField, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DeactivateVaultForceField, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.WatchHolocubeExplainVault, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_WatchHolocubeSucceeded_StartsSpeakAboutHolocubePhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.WatchHolocubeExplainVault, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.SpeakAboutHolocube, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_WatchHolocubeSucceeded_StartsSpeakAboutHolocubePhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.WatchHolocubeExplainVault, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.WatchHolocubeExplainVault, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.SpeakAboutHolocube, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SpeakAboutHolocubeSucceeded_StartsDestroyVaultConstructsPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakAboutHolocube, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DestroyVaultConstructs, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SpeakAboutHolocubeSucceeded_StartsDestroyVaultConstructsPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakAboutHolocube, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakAboutHolocube, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DestroyVaultConstructs, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DestroyVaultConstructsSucceeded_StartsHarizogPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DestroyVaultConstructs, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DefeatHarizog, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DestroyVaultConstructsSucceeded_StartsHarizogPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DestroyVaultConstructs, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DestroyVaultConstructs, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DefeatHarizog, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_HarizogSucceeded_StartsOsunBlockingWayPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatHarizog, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DefeatOsunBlockingTheWay, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_HarizogSucceeded_StartsOsunBlockingWayPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatHarizog, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatHarizog, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DefeatOsunBlockingTheWay, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_OsunBlockingWaySucceeded_StartsVaultExitPhase()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatOsunBlockingTheWay, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.MeetAtVaultExit, setPhase.Arguments[0]);
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_OsunBlockingWaySucceeded_StartsVaultExitPhaseOnce()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatOsunBlockingTheWay, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatOsunBlockingTheWay, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.MeetAtVaultExit, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_VaultExitSucceeded_RemovesTriggerAndFinishesEvent()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            50702u,
            715u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 715u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtVaultExit, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_IncompleteFinalObjective_DoesNotFinish()
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.MeetAtVaultExit, PublicEventStatus.Active));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Theory]
    [InlineData(PublicEventObjective.DefeatUnboundFlameElemental)]
    [InlineData(PublicEventObjective.DefeatIceboundOverlord)]
    [InlineData(PublicEventObjective.DefeatDarkwitchYotul)]
    public void OnPublicEventObjectiveStatus_OptionalBossSucceeded_DoesNotAdvanceRoute(PublicEventObjective objective)
    {
        HallOfTheHundredEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotRemoveOpeningGatherTrigger()
    {
        HallOfTheHundredEventScript script = CreateScript(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            48684u,
            701u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 701u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.GatherInFrontOfTheCrashedShip, PublicEventStatus.Active));

        Assert.Empty(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    private static HallOfTheHundredEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        return CreateScript(out eventProxy, out _, out _, preserveLoadInvocations);
    }

    private static HallOfTheHundredEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers,
        bool preserveLoadInvocations = false)
    {
        var script = new HallOfTheHundredEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out eventProxy, out mapProxy, out createdTriggers);
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static HallOfTheHundredEventScript CreateTurnstileScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTurnstileTrigger> createdTriggers)
    {
        var script = new HallOfTheHundredEventScript();
        IPublicEvent publicEvent = CreateTurnstilePublicEvent(out eventProxy, out mapProxy, out createdTriggers);
        script.OnLoad(publicEvent);
        eventProxy.Invocations.Clear();
        return script;
    }

    private static IPublicEvent CreatePublicEvent(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3009u });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedTrigger> triggers = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedTrigger trigger = CreateCreatedTrigger();
            triggers.Add(trigger);
            return trigger.Instance;
        });

        createdTriggers = triggers;
        return publicEvent;
    }

    private static IPublicEvent CreateTurnstilePublicEvent(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTurnstileTrigger> createdTriggers)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3009u });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedTurnstileTrigger> triggers = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedTurnstileTrigger trigger = CreateTurnstileTrigger();
            triggers.Add(trigger);
            return trigger.Instance;
        });

        createdTriggers = triggers;
        return publicEvent;
    }

    private static CreatedTrigger CreateCreatedTrigger()
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        return new CreatedTrigger(trigger, triggerProxy);
    }

    private static CreatedTurnstileTrigger CreateTurnstileTrigger()
    {
        ITurnstileGridTriggerEntity trigger = RecordingDispatchProxy<ITurnstileGridTriggerEntity>.Create(
            out RecordingDispatchProxy<ITurnstileGridTriggerEntity> triggerProxy);
        return new CreatedTurnstileTrigger(trigger, triggerProxy);
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateTrigger(out IGridTriggerEntity trigger)
    {
        trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(out RecordingDispatchProxy<IGridTriggerEntity> triggerProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        triggerProxy.SetProperty(nameof(IGridEntity.Map), map);

        return publicEventManagerProxy;
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(
        out IWorldEntity worldEntity,
        out RecordingDispatchProxy<IWorldEntity> worldEntityProxy)
    {
        worldEntity = RecordingDispatchProxy<IWorldEntity>.Create(out worldEntityProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        worldEntityProxy.SetProperty(nameof(IGridEntity.Map), map);

        return publicEventManagerProxy;
    }

    private static IWorldLocationVolumeGridTriggerEntity CreateWorldLocationTrigger(
        uint worldLocationId,
        uint guid,
        out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy)
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(out triggerProxy);
        triggerProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        triggerProxy.SetProperty(nameof(IWorldLocationVolumeGridTriggerEntity.Entry), new WorldLocation2Entry
        {
            Id = worldLocationId
        });
        return trigger;
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

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, PublicEventObjective objective)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
    }

    private static void AssertObjectiveUpdated(
        RecordingDispatchProxy<IPublicEvent> eventProxy,
        PublicEventObjective objective,
        int count)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)),
            update => (PublicEventObjective)update.Arguments[0] == objective
                && (int)update.Arguments[1] == count);
    }

    private static void AssertTriggerInitialised(CreatedTrigger trigger, uint worldLocationId, uint objectId)
    {
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Invocation initialise = Assert.Single(
            trigger.Proxy.GetInvocations(nameof(IWorldLocationVolumeGridTriggerEntity.Initialise)));
        Assert.Equal(worldLocationId, initialise.Arguments[0]);
        Assert.Equal(objectId, initialise.Arguments[1]);
    }

    private static void AssertTurnstileInitialised(CreatedTurnstileTrigger trigger, uint triggerId, float range, uint objectId)
    {
        RecordingDispatchProxy<ITurnstileGridTriggerEntity>.Invocation initialise = Assert.Single(
            trigger.Proxy.GetInvocations(nameof(ITurnstileGridTriggerEntity.Initialise)));
        Assert.Equal(triggerId, initialise.Arguments[0]);
        Assert.Equal(range, initialise.Arguments[1]);
        Assert.Equal(objectId, initialise.Arguments[2]);
    }

    private static void AssertTriggerAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity trigger,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(trigger, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(3009u, position.Info.Entry.Id);
    }

    private sealed record CreatedTrigger(
        IWorldLocationVolumeGridTriggerEntity Instance,
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> Proxy);

    private sealed record CreatedTurnstileTrigger(
        ITurnstileGridTriggerEntity Instance,
        RecordingDispatchProxy<ITurnstileGridTriggerEntity> Proxy);

    public static IEnumerable<object[]> HallDirectObjectiveCreditCases()
    {
        yield return [typeof(DorianWalkerReviveEntityScript), PublicEventObjective.ReviveDorian, 1];
        yield return [typeof(ArtemisZinReviveEntityScript), PublicEventObjective.ReviveArtemis, 1];
        yield return [typeof(WatchtowerReturnElevatorEntityScript), PublicEventObjective.UseElevatorToBottomFloor, 0];
        yield return [typeof(ForcefieldPowerLinkEntityScript), PublicEventObjective.DestroyVaultConstructs, 1];
    }

    public static IEnumerable<object[]> HallTargetGroupObjectiveCreditCases()
    {
        yield return [typeof(KelHavikStaffKeyEntityScript), PublicEventObjectiveType.ActivateTargetGroup, 14104u];
        yield return [typeof(KelHavikHammerKeyEntityScript), PublicEventObjectiveType.ActivateTargetGroup, 14104u];
        yield return [typeof(KelHavikPolearmKeyEntityScript), PublicEventObjectiveType.ActivateTargetGroup, 14104u];
        yield return [typeof(VaultSecretRoomKeyFragmentEntityScript), PublicEventObjectiveType.ActivateTargetGroup, 14276u];
        yield return [typeof(KelHavikDoorLockEntityScript), PublicEventObjectiveType.ActivateTargetGroupChecklist, 14212u];
        yield return [typeof(UnlitIncenseEntityScript), PublicEventObjectiveType.ActivateTargetGroupChecklist, 12275u];
        yield return [typeof(SacredBasReliefEntityScript), PublicEventObjectiveType.ActivateTargetGroupChecklist, 12302u];
    }

    public static IEnumerable<object[]> HallTalkObjectiveCreditCases()
    {
        yield return [typeof(DorianWalkerHolocubeTalkEntityScript), 14194u];
        yield return [typeof(ArtemisZinHolocubeTalkEntityScript), 14194u];
    }
}
