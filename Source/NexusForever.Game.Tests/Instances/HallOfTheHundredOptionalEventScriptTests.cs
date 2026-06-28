using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.WorldStory.HallOfTheHundred;
using NexusForever.Script.Instance.WorldStory.HallOfTheHundred.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Instances;

public class HallOfTheHundredOptionalEventScriptTests
{
    [Fact]
    public void HallMapScript_OnLoad_CreatesMainAndOptionalPublicEvents()
    {
        HallOfTheHundredMapScript script = CreateMapScript(
            out RecordingDispatchProxy<IPublicEventManager> managerProxy,
            out _,
            out _,
            out IContentMapInstance contentMap);

        script.OnLoad(contentMap);

        Assert.Equal(
            [666u, 668u, 669u, 677u, 678u, 693u, 696u, 874u, 875u],
            managerProxy.GetInvocations(nameof(IPublicEventManager.CreateEvent))
                .Select(i => (uint)i.Arguments[0])
                .ToArray());
    }

    [Fact]
    public void HallMapScript_OnAddToMap_JoinsPlayersToMainAndOptionalPublicEvents()
    {
        HallOfTheHundredMapScript script = CreateMapScript(
            out RecordingDispatchProxy<IPublicEventManager> managerProxy,
            out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
            out RecordingDispatchProxy<IPublicEvent> protectEventOneProxy,
            out RecordingDispatchProxy<IPublicEvent> protectEventTwoProxy,
            out RecordingDispatchProxy<IPublicEvent> graveyardEventProxy,
            out RecordingDispatchProxy<IPublicEvent> warhoundEventProxy,
            out RecordingDispatchProxy<IPublicEvent> coldAndHungryEventProxy,
            out RecordingDispatchProxy<IPublicEvent> coldSoupEventProxy,
            out RecordingDispatchProxy<IPublicEvent> medalEventProxy,
            out RecordingDispatchProxy<IPublicEvent> escapeEventProxy,
            out IContentMapInstance contentMap);
        script.OnLoad(contentMap);
        managerProxy.Invocations.Clear();

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        script.OnAddToMap(player);

        AssertJoin(mainEventProxy, player);
        AssertJoin(protectEventOneProxy, player);
        AssertJoin(protectEventTwoProxy, player);
        AssertJoin(graveyardEventProxy, player);
        AssertJoin(warhoundEventProxy, player);
        AssertJoin(coldAndHungryEventProxy, player);
        AssertJoin(coldSoupEventProxy, player);
        AssertJoin(medalEventProxy, player);
        AssertJoin(escapeEventProxy, player);
    }

    [Fact]
    public void EventScript_IsOwnedByTheGraveyardEchoedPublicEvent()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(TheGraveyardEchoedEventScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 696u }, attribute.Id);
    }

    [Fact]
    public void EventScript_IsOwnedByTheWarhoundsOfVaregorPublicEvent()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(TheWarhoundsOfVaregorEventScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 693u }, attribute.Id);
    }

    [Fact]
    public void EventScript_IsOwnedByColdAndHungryPublicEvent()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(ColdAndHungryEventScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 677u }, attribute.Id);
    }

    [Fact]
    public void EventScript_IsOwnedByColdSoupForTheSoulrotPublicEvent()
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            typeof(ColdSoupForTheSoulrotEventScript).GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { 678u }, attribute.Id);
    }

    [Fact]
    public void OnLoad_SetsInitialInvestigatePhase()
    {
        TheGraveyardEchoedEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.InvestigatePellGraveyard, invocation.Arguments[0]);
    }

    [Fact]
    public void Warhounds_OnLoad_SetsInitialExplorePhase()
    {
        TheWarhoundsOfVaregorEventScript script = CreateWarhoundScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ExploreWarhoundKennel, invocation.Arguments[0]);
    }

    [Fact]
    public void ColdAndHungry_OnLoad_SetsInitialExploreYetiCavePhase()
    {
        ColdAndHungryEventScript script = CreateColdAndHungryScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ExploreYetiCave, invocation.Arguments[0]);
    }

    [Fact]
    public void ColdSoup_OnLoad_SetsInitialInvestigateSoulrotCavePhase()
    {
        ColdSoupForTheSoulrotEventScript script = CreateColdSoupScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.InvestigateSoulrotCave, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Investigate_CreatesGraveyardTriggerAndActivatesObjective()
    {
        TheGraveyardEchoedEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.InvestigatePellGraveyard);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.InvestigatePellGraveyard);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 48675u, 7895u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-714.623f, -684.77f, -828.793f));
    }

    [Fact]
    public void OnPublicEventPhase_Investigate_ActivatesMainPellGraveyardParentObjectiveWhenPresent()
    {
        TheGraveyardEchoedEventScript script = CreateScript(
            out _,
            out RecordingDispatchProxy<IPublicEvent> mainEventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.InvestigatePellGraveyard);

        AssertObjectiveActivated(mainEventProxy, PublicEventObjective.InvestigatePellGraveyardParent);
    }

    [Fact]
    public void Warhounds_OnPublicEventPhase_Explore_CreatesKennelTriggerAndActivatesObjective()
    {
        TheWarhoundsOfVaregorEventScript script = CreateWarhoundScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.ExploreWarhoundKennel);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.ExploreWarhoundKennel);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 48660u, 8426u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-295.518f, -679.848f, -1181.76f));
    }

    [Fact]
    public void Warhounds_OnPublicEventPhase_Explore_ActivatesMainWarhoundParentObjectiveWhenPresent()
    {
        TheWarhoundsOfVaregorEventScript script = CreateWarhoundScript(
            out _,
            out RecordingDispatchProxy<IPublicEvent> mainEventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.ExploreWarhoundKennel);

        AssertObjectiveActivated(mainEventProxy, PublicEventObjective.ExploreWarhoundKennelParent);
    }

    [Fact]
    public void ColdAndHungry_OnPublicEventPhase_ExploreYetiCave_CreatesLeaveTriggerAndActivatesMappedObjectives()
    {
        ColdAndHungryEventScript script = CreateColdAndHungryScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.ExploreYetiCave);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.InvestigateFrozenCarcasses);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.LeaveYetiCave);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 48428u, 7855u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-261.565f, -666.126f, -869.018f));
    }

    [Fact]
    public void ColdAndHungry_OnPublicEventPhase_ExploreYetiCave_ActivatesMainYetiParentObjectiveWhenPresent()
    {
        ColdAndHungryEventScript script = CreateColdAndHungryScript(
            out _,
            out RecordingDispatchProxy<IPublicEvent> mainEventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.ExploreYetiCave);

        AssertObjectiveActivated(mainEventProxy, PublicEventObjective.ExploreYetiCaveParent);
    }

    [Fact]
    public void ColdSoup_OnPublicEventPhase_InvestigateSoulrotCave_CreatesCanisterTriggerAndActivatesObjective()
    {
        ColdSoupForTheSoulrotEventScript script = CreateColdSoupScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.InvestigateSoulrotCave);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.InvestigateLeakingSoulrotCanister);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 48534u, 7864u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(27.9102f, -652.776f, -311.237f));
    }

    [Fact]
    public void ColdSoup_OnPublicEventPhase_InvestigateSoulrotCave_ActivatesMainSoulrotParentObjectiveWhenPresent()
    {
        ColdSoupForTheSoulrotEventScript script = CreateColdSoupScript(
            out _,
            out RecordingDispatchProxy<IPublicEvent> mainEventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.InvestigateSoulrotCave);

        AssertObjectiveActivated(mainEventProxy, PublicEventObjective.InvestigateSoulrotCaveParent);
    }

    [Fact]
    public void Warhounds_OnPublicEventPhase_DefeatWatchhound_ActivatesObjective()
    {
        TheWarhoundsOfVaregorEventScript script = CreateWarhoundScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatVaregorWatchhound);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatVaregorWatchhound);
    }

    [Fact]
    public void ColdAndHungry_OnPublicEventPhase_EscapeYeti_ActivatesAndCreditsScriptObjective()
    {
        ColdAndHungryEventScript script = CreateColdAndHungryScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.EscapeYeti);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.EscapeYeti);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.EscapeYeti, 1);
    }

    [Fact]
    public void ColdAndHungry_OnPublicEventPhase_DefeatYeti_ActivatesObjective()
    {
        ColdAndHungryEventScript script = CreateColdAndHungryScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatYeti);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatYeti);
    }

    [Fact]
    public void ColdSoup_OnPublicEventPhase_ReviveSoulrottedBody_ActivatesMappedObjectives()
    {
        ColdSoupForTheSoulrotEventScript script = CreateColdSoupScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.ReviveSoulrottedBody);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.ReviveSoulrottedBody);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.ReviveSoulrottedBodyScriptGate);
    }

    [Theory]
    [InlineData(PublicEventPhase.CollectSoulrot, PublicEventObjective.CollectSoulrot)]
    [InlineData(PublicEventPhase.DisposeSoulrotWaste, PublicEventObjective.DisposeSoulrotWaste)]
    [InlineData(PublicEventPhase.KillSoulless, PublicEventObjective.KillTheSoulless)]
    public void ColdSoup_OnPublicEventPhase_ActivatesMappedObjective(PublicEventPhase phase, PublicEventObjective objective)
    {
        ColdSoupForTheSoulrotEventScript script = CreateColdSoupScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)phase);

        AssertObjectiveActivated(eventProxy, objective);
    }

    [Fact]
    public void Warhounds_OnPublicEventPhase_ExploreFurther_CreatesDepthTriggerAndActivatesMappedObjectives()
    {
        TheWarhoundsOfVaregorEventScript script = CreateWarhoundScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);

        script.OnPublicEventPhase((uint)PublicEventPhase.ExploreFurtherIntoWarhoundKennel);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.ExploreFurtherIntoWarhoundKennel);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatWarhoundPack);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 48661u, 7881u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-429.241f, -679.465f, -1269.96f));
    }

    [Theory]
    [InlineData(PublicEventPhase.LightIncenseInPellGraveyard, PublicEventObjective.LightIncenseInPellGraveyard)]
    [InlineData(PublicEventPhase.DeliverRestlessSpiritsToAltar, PublicEventObjective.DeliverRestlessSpiritsToAltar)]
    [InlineData(PublicEventPhase.DefeatPrimalWraith, PublicEventObjective.DefeatPrimalWraith)]
    public void OnPublicEventPhase_ActivatesMappedObjective(PublicEventPhase phase, PublicEventObjective objective)
    {
        TheGraveyardEchoedEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)phase);

        AssertObjectiveActivated(eventProxy, objective);
    }

    [Fact]
    public void OnPublicEventPhase_FightOffPrimalEchoes_ActivatesAndCreditsScriptObjective()
    {
        TheGraveyardEchoedEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.FightOffPrimalEchoes);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.FightOffPrimalEchoes);
        AssertObjectiveUpdated(eventProxy, PublicEventObjective.FightOffPrimalEchoes, 1);
    }

    [Theory]
    [InlineData(PublicEventObjective.InvestigatePellGraveyard, PublicEventPhase.LightIncenseInPellGraveyard)]
    [InlineData(PublicEventObjective.LightIncenseInPellGraveyard, PublicEventPhase.DeliverRestlessSpiritsToAltar)]
    [InlineData(PublicEventObjective.DeliverRestlessSpiritsToAltar, PublicEventPhase.FightOffPrimalEchoes)]
    [InlineData(PublicEventObjective.FightOffPrimalEchoes, PublicEventPhase.DefeatPrimalWraith)]
    public void OnPublicEventObjectiveStatus_Succeeded_StartsNextPhase(
        PublicEventObjective objective,
        PublicEventPhase expectedPhase)
    {
        TheGraveyardEchoedEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(expectedPhase, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_InvestigateSucceeded_RemovesGraveyardTrigger()
    {
        TheGraveyardEchoedEventScript script = CreateScript(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            48675u,
            701u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 701u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.InvestigatePellGraveyard, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void Warhounds_OnPublicEventObjectiveStatus_ExploreSucceeded_RemovesKennelTrigger()
    {
        TheWarhoundsOfVaregorEventScript script = CreateWarhoundScript(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            48660u,
            702u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 702u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ExploreWarhoundKennel, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void ColdAndHungry_OnPublicEventObjectiveStatus_LeaveYetiCaveSucceeded_RemovesLeaveTrigger()
    {
        ColdAndHungryEventScript script = CreateColdAndHungryScript(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            48428u,
            704u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 704u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.LeaveYetiCave, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void ColdSoup_OnPublicEventObjectiveStatus_CanisterSucceeded_RemovesCanisterTrigger()
    {
        ColdSoupForTheSoulrotEventScript script = CreateColdSoupScript(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            48534u,
            705u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 705u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.InvestigateLeakingSoulrotCanister, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void Warhounds_OnPublicEventObjectiveStatus_ExploreSucceeded_StartsDefeatWatchhoundPhaseOnce()
    {
        TheWarhoundsOfVaregorEventScript script = CreateWarhoundScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ExploreWarhoundKennel, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ExploreWarhoundKennel, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DefeatVaregorWatchhound, setPhase.Arguments[0]);
    }

    [Fact]
    public void ColdAndHungry_OnPublicEventObjectiveStatus_LeaveYetiCaveSucceeded_StartsEscapePhaseOnce()
    {
        ColdAndHungryEventScript script = CreateColdAndHungryScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.LeaveYetiCave, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.LeaveYetiCave, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.EscapeYeti, setPhase.Arguments[0]);
    }

    [Fact]
    public void ColdAndHungry_OnPublicEventObjectiveStatus_EscapeYetiSucceeded_StartsDefeatYetiPhaseOnce()
    {
        ColdAndHungryEventScript script = CreateColdAndHungryScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.EscapeYeti, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.EscapeYeti, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DefeatYeti, setPhase.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventObjective.InvestigateLeakingSoulrotCanister, PublicEventPhase.ReviveSoulrottedBody)]
    [InlineData(PublicEventObjective.ReviveSoulrottedBody, PublicEventPhase.CollectSoulrot)]
    [InlineData(PublicEventObjective.ReviveSoulrottedBodyScriptGate, PublicEventPhase.CollectSoulrot)]
    [InlineData(PublicEventObjective.CollectSoulrot, PublicEventPhase.DisposeSoulrotWaste)]
    [InlineData(PublicEventObjective.DisposeSoulrotWaste, PublicEventPhase.KillSoulless)]
    public void ColdSoup_OnPublicEventObjectiveStatus_Succeeded_StartsNextPhaseOnce(
        PublicEventObjective objective,
        PublicEventPhase expectedPhase)
    {
        ColdSoupForTheSoulrotEventScript script = CreateColdSoupScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(expectedPhase, setPhase.Arguments[0]);
    }

    [Fact]
    public void Warhounds_OnPublicEventObjectiveStatus_DefeatWatchhoundSucceeded_StartsExploreFurtherPhaseOnce()
    {
        TheWarhoundsOfVaregorEventScript script = CreateWarhoundScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatVaregorWatchhound, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatVaregorWatchhound, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ExploreFurtherIntoWarhoundKennel, setPhase.Arguments[0]);
    }

    [Fact]
    public void Warhounds_OnPublicEventObjectiveStatus_ExploreFurtherSucceeded_RemovesDepthTrigger()
    {
        TheWarhoundsOfVaregorEventScript script = CreateWarhoundScript(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            48661u,
            703u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 703u ? trigger : null);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ExploreFurtherIntoWarhoundKennel, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Succeeded_StartsNextPhaseOnce()
    {
        TheGraveyardEchoedEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.LightIncenseInPellGraveyard, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.LightIncenseInPellGraveyard, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DeliverRestlessSpiritsToAltar, setPhase.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_PrimalWraithSucceeded_FinishesEvent()
    {
        TheGraveyardEchoedEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatPrimalWraith, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void ColdAndHungry_OnPublicEventObjectiveStatus_DefeatYetiSucceeded_FinishesEvent()
    {
        ColdAndHungryEventScript script = CreateColdAndHungryScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatYeti, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void ColdSoup_OnPublicEventObjectiveStatus_KillSoullessSucceeded_FinishesEvent()
    {
        ColdSoupForTheSoulrotEventScript script = CreateColdSoupScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.KillTheSoulless, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void PellGraveyardTrigger_OnEnterRange_UpdatesMappedTriggerAndParentObjectivesOnce()
    {
        var script = new PellGraveyardGridTriggerEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Equal(2, updates.Count);

        RecordingDispatchProxy<IPublicEventManager>.Invocation triggerUpdate = Assert.Single(
            updates,
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, triggerUpdate.Arguments[0]);
        Assert.Equal(7895u, triggerUpdate.Arguments[1]);
        Assert.Equal(1, triggerUpdate.Arguments[2]);

        RecordingDispatchProxy<IPublicEventManager>.Invocation parentUpdate = Assert.Single(
            updates,
            i => i.Arguments.Length == 2);
        Assert.Equal(PublicEventObjective.InvestigatePellGraveyardParent, parentUpdate.Arguments[0]);
        Assert.Equal(1, parentUpdate.Arguments[1]);
    }

    [Fact]
    public void PellGraveyardTrigger_OnEnterRange_IgnoresNonPlayers()
    {
        var script = new PellGraveyardGridTriggerEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(RecordingDispatchProxy<IGridEntity>.Create(out _));

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void WarhoundKennelTrigger_OnEnterRange_UpdatesMappedTriggerAndParentObjectivesOnce()
    {
        var script = new WarhoundKennelGridTriggerEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Equal(2, updates.Count);

        RecordingDispatchProxy<IPublicEventManager>.Invocation triggerUpdate = Assert.Single(
            updates,
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, triggerUpdate.Arguments[0]);
        Assert.Equal(8426u, triggerUpdate.Arguments[1]);
        Assert.Equal(1, triggerUpdate.Arguments[2]);

        RecordingDispatchProxy<IPublicEventManager>.Invocation parentUpdate = Assert.Single(
            updates,
            i => i.Arguments.Length == 2);
        Assert.Equal(PublicEventObjective.ExploreWarhoundKennelParent, parentUpdate.Arguments[0]);
        Assert.Equal(1, parentUpdate.Arguments[1]);
    }

    [Fact]
    public void WarhoundKennelTrigger_OnEnterRange_IgnoresNonPlayers()
    {
        var script = new WarhoundKennelGridTriggerEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(RecordingDispatchProxy<IGridEntity>.Create(out _));

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void WarhoundKennelDepthTrigger_OnEnterRange_UpdatesMappedTriggerObjectiveOnce()
    {
        var script = new WarhoundKennelDepthGridTriggerEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, update.Arguments[0]);
        Assert.Equal(7881u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void WarhoundKennelDepthTrigger_OnEnterRange_IgnoresNonPlayers()
    {
        var script = new WarhoundKennelDepthGridTriggerEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(RecordingDispatchProxy<IGridEntity>.Create(out _));

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void YetiCaveExitTrigger_OnEnterRange_UpdatesMappedTriggerAndParentObjectivesOnce()
    {
        var script = new YetiCaveExitGridTriggerEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Equal(2, updates.Count);

        RecordingDispatchProxy<IPublicEventManager>.Invocation triggerUpdate = Assert.Single(
            updates,
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, triggerUpdate.Arguments[0]);
        Assert.Equal(7855u, triggerUpdate.Arguments[1]);
        Assert.Equal(1, triggerUpdate.Arguments[2]);

        RecordingDispatchProxy<IPublicEventManager>.Invocation parentUpdate = Assert.Single(
            updates,
            i => i.Arguments.Length == 2);
        Assert.Equal(PublicEventObjective.ExploreYetiCaveParent, parentUpdate.Arguments[0]);
        Assert.Equal(1, parentUpdate.Arguments[1]);
    }

    [Fact]
    public void YetiCaveExitTrigger_OnEnterRange_IgnoresNonPlayers()
    {
        var script = new YetiCaveExitGridTriggerEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(RecordingDispatchProxy<IGridEntity>.Create(out _));

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void SoulrotCaveCanisterTrigger_OnEnterRange_UpdatesMappedTriggerAndParentObjectivesOnce()
    {
        var script = new SoulrotCaveCanisterGridTriggerEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Equal(2, updates.Count);

        RecordingDispatchProxy<IPublicEventManager>.Invocation triggerUpdate = Assert.Single(
            updates,
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, triggerUpdate.Arguments[0]);
        Assert.Equal(7864u, triggerUpdate.Arguments[1]);
        Assert.Equal(1, triggerUpdate.Arguments[2]);

        RecordingDispatchProxy<IPublicEventManager>.Invocation parentUpdate = Assert.Single(
            updates,
            i => i.Arguments.Length == 2);
        Assert.Equal(PublicEventObjective.InvestigateSoulrotCaveParent, parentUpdate.Arguments[0]);
        Assert.Equal(1, parentUpdate.Arguments[1]);
    }

    [Fact]
    public void SoulrotCaveCanisterTrigger_OnEnterRange_IgnoresNonPlayers()
    {
        var script = new SoulrotCaveCanisterGridTriggerEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(RecordingDispatchProxy<IGridEntity>.Create(out _));

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void FrozenLever_UsesCreatureFilterForMappedTargetGroupMember()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(FrozenLeverEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 67940u }, attribute.CreatureId);
    }

    [Fact]
    public void ColdAndHungryYeti_UsesCreatureFilterForMappedObjectiveCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(ColdAndHungryYetiEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 67657u }, attribute.CreatureId);
    }

    [Fact]
    public void FrozenCarcass_UsesCreatureFilterForMappedTargetGroupMember()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(FrozenCarcassEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 67659u }, attribute.CreatureId);
    }

    [Fact]
    public void FrozenLever_OnDeath_UpdatesMappedClusterObjectiveOnce()
    {
        var script = new FrozenLeverEntityScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ICreatureEntity creature = CreateCreatureWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(creature);
        script.OnDeath();
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 2);
        Assert.Equal((uint)PublicEventObjective.DefeatWarhoundPack, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void SoulrottedBody_OnActivateSuccess_UpdatesMappedTargetGroupAndScriptGateOnceAndRemovesEntity()
    {
        var script = new SoulrottedBodyEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity worldEntity,
            out RecordingDispatchProxy<IWorldEntity> worldEntityProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Equal(2, updates.Count);

        RecordingDispatchProxy<IPublicEventManager>.Invocation targetGroupUpdate = Assert.Single(
            updates,
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, targetGroupUpdate.Arguments[0]);
        Assert.Equal(12240u, targetGroupUpdate.Arguments[1]);
        Assert.Equal(1, targetGroupUpdate.Arguments[2]);

        RecordingDispatchProxy<IPublicEventManager>.Invocation directUpdate = Assert.Single(
            updates,
            i => i.Arguments.Length == 2);
        Assert.Equal(PublicEventObjective.ReviveSoulrottedBodyScriptGate, directUpdate.Arguments[0]);
        Assert.Equal(1, directUpdate.Arguments[1]);
        Assert.Single(worldEntityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void SoulrotSample_OnActivateSuccess_UpdatesMappedScriptObjectiveOnceAndRemovesEntity()
    {
        var script = new SoulrotSampleEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity worldEntity,
            out RecordingDispatchProxy<IWorldEntity> worldEntityProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.CollectSoulrot, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
        Assert.Single(worldEntityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void SoulrotWasteDisposal_OnActivateSuccess_UpdatesMappedTargetGroupOnceAndRemovesEntity()
    {
        var script = new SoulrotWasteDisposalEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity worldEntity,
            out RecordingDispatchProxy<IWorldEntity> worldEntityProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(14232u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
        Assert.Single(worldEntityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [InlineData(typeof(UnlitIncenseEntityScript), 12275u)]
    [InlineData(typeof(SacredBasReliefEntityScript), 12302u)]
    [InlineData(typeof(FrozenCarcassEntityScript), 12213u)]
    public void ChecklistTargetGroupScripts_OnActivateSuccess_UpdatesMappedChecklistTargetGroupOnceAndRemovesEntity(
        Type scriptType,
        uint targetGroupId)
    {
        HallTargetGroupObjectiveEntityScriptBase script = (HallTargetGroupObjectiveEntityScriptBase)Activator.CreateInstance(scriptType);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity worldEntity,
            out RecordingDispatchProxy<IWorldEntity> worldEntityProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(targetGroupId, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);
        Assert.Single(worldEntityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    private static HallOfTheHundredMapScript CreateMapScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
        out RecordingDispatchProxy<IPublicEvent> graveyardEventProxy,
        out IContentMapInstance contentMap)
    {
        return CreateMapScript(
            out managerProxy,
            out mainEventProxy,
            out _,
            out _,
            out graveyardEventProxy,
            out _,
            out _,
            out _,
            out _,
            out _,
            out contentMap);
    }

    private static HallOfTheHundredMapScript CreateMapScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
        out RecordingDispatchProxy<IPublicEvent> graveyardEventProxy,
        out RecordingDispatchProxy<IPublicEvent> warhoundEventProxy,
        out IContentMapInstance contentMap)
    {
        return CreateMapScript(
            out managerProxy,
            out mainEventProxy,
            out _,
            out _,
            out graveyardEventProxy,
            out warhoundEventProxy,
            out _,
            out _,
            out _,
            out _,
            out contentMap);
    }

    private static HallOfTheHundredMapScript CreateMapScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
        out RecordingDispatchProxy<IPublicEvent> protectEventOneProxy,
        out RecordingDispatchProxy<IPublicEvent> protectEventTwoProxy,
        out RecordingDispatchProxy<IPublicEvent> graveyardEventProxy,
        out RecordingDispatchProxy<IPublicEvent> warhoundEventProxy,
        out RecordingDispatchProxy<IPublicEvent> coldAndHungryEventProxy,
        out RecordingDispatchProxy<IPublicEvent> coldSoupEventProxy,
        out RecordingDispatchProxy<IPublicEvent> medalEventProxy,
        out RecordingDispatchProxy<IPublicEvent> escapeEventProxy,
        out IContentMapInstance contentMap)
    {
        IPublicEvent mainEvent = RecordingDispatchProxy<IPublicEvent>.Create(out mainEventProxy);
        IPublicEvent protectEventOne = RecordingDispatchProxy<IPublicEvent>.Create(out protectEventOneProxy);
        IPublicEvent protectEventTwo = RecordingDispatchProxy<IPublicEvent>.Create(out protectEventTwoProxy);
        IPublicEvent graveyardEvent = RecordingDispatchProxy<IPublicEvent>.Create(out graveyardEventProxy);
        IPublicEvent warhoundEvent = RecordingDispatchProxy<IPublicEvent>.Create(out warhoundEventProxy);
        IPublicEvent coldAndHungryEvent = RecordingDispatchProxy<IPublicEvent>.Create(out coldAndHungryEventProxy);
        IPublicEvent coldSoupEvent = RecordingDispatchProxy<IPublicEvent>.Create(out coldSoupEventProxy);
        IPublicEvent medalEvent = RecordingDispatchProxy<IPublicEvent>.Create(out medalEventProxy);
        IPublicEvent escapeEvent = RecordingDispatchProxy<IPublicEvent>.Create(out escapeEventProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out managerProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.CreateEvent), args =>
        {
            uint id = (uint)args[0];
            return id switch
            {
                666u => mainEvent,
                668u => protectEventOne,
                669u => protectEventTwo,
                696u => graveyardEvent,
                693u => warhoundEvent,
                677u => coldAndHungryEvent,
                678u => coldSoupEvent,
                874u => medalEvent,
                875u => escapeEvent,
                _ => null
            };
        });

        contentMap = CreateContentMap(publicEventManager);
        return new HallOfTheHundredMapScript();
    }

    private static IContentMapInstance CreateContentMap(IPublicEventManager publicEventManager)
    {
        IContentMapInstance contentMap = RecordingDispatchProxy<IContentMapInstance>.Create(out RecordingDispatchProxy<IContentMapInstance> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        return contentMap;
    }

    private static TheGraveyardEchoedEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        return CreateScript(out eventProxy, out _, out _, preserveLoadInvocations);
    }

    private static TheGraveyardEchoedEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
        bool preserveLoadInvocations = false)
    {
        var script = new TheGraveyardEchoedEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out eventProxy, out RecordingDispatchProxy<IMapInstance> mapProxy, out _);

        IPublicEvent mainEvent = RecordingDispatchProxy<IPublicEvent>.Create(out mainEventProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> managerProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.GetEvent), args => (uint)args[0] == 666u ? mainEvent : null);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static TheGraveyardEchoedEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers,
        bool preserveLoadInvocations = false)
    {
        var script = new TheGraveyardEchoedEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out eventProxy, out mapProxy, out createdTriggers);
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static ColdAndHungryEventScript CreateColdAndHungryScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        return CreateColdAndHungryScript(out eventProxy, out _, out _, preserveLoadInvocations);
    }

    private static ColdAndHungryEventScript CreateColdAndHungryScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
        bool preserveLoadInvocations = false)
    {
        var script = new ColdAndHungryEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out eventProxy, out RecordingDispatchProxy<IMapInstance> mapProxy, out _);

        IPublicEvent mainEvent = RecordingDispatchProxy<IPublicEvent>.Create(out mainEventProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> managerProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.GetEvent), args => (uint)args[0] == 666u ? mainEvent : null);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static ColdAndHungryEventScript CreateColdAndHungryScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers,
        bool preserveLoadInvocations = false)
    {
        var script = new ColdAndHungryEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out eventProxy, out mapProxy, out createdTriggers);
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static ColdSoupForTheSoulrotEventScript CreateColdSoupScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        return CreateColdSoupScript(out eventProxy, out _, out _, preserveLoadInvocations);
    }

    private static ColdSoupForTheSoulrotEventScript CreateColdSoupScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
        bool preserveLoadInvocations = false)
    {
        var script = new ColdSoupForTheSoulrotEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out eventProxy, out RecordingDispatchProxy<IMapInstance> mapProxy, out _);

        IPublicEvent mainEvent = RecordingDispatchProxy<IPublicEvent>.Create(out mainEventProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> managerProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.GetEvent), args => (uint)args[0] == 666u ? mainEvent : null);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static ColdSoupForTheSoulrotEventScript CreateColdSoupScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers,
        bool preserveLoadInvocations = false)
    {
        var script = new ColdSoupForTheSoulrotEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out eventProxy, out mapProxy, out createdTriggers);
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static TheWarhoundsOfVaregorEventScript CreateWarhoundScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        return CreateWarhoundScript(out eventProxy, out _, out _, preserveLoadInvocations);
    }

    private static TheWarhoundsOfVaregorEventScript CreateWarhoundScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
        bool preserveLoadInvocations = false)
    {
        var script = new TheWarhoundsOfVaregorEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out eventProxy, out RecordingDispatchProxy<IMapInstance> mapProxy, out _);

        IPublicEvent mainEvent = RecordingDispatchProxy<IPublicEvent>.Create(out mainEventProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> managerProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.GetEvent), args => (uint)args[0] == 666u ? mainEvent : null);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static TheWarhoundsOfVaregorEventScript CreateWarhoundScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers,
        bool preserveLoadInvocations = false)
    {
        var script = new TheWarhoundsOfVaregorEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out eventProxy, out mapProxy, out createdTriggers);
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
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

    private static CreatedTrigger CreateCreatedTrigger()
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        return new CreatedTrigger(trigger, triggerProxy);
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

    private static ICreatureEntity CreateCreatureWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        creatureProxy.SetProperty(nameof(IGridEntity.Map), map);

        return creature;
    }

    private static void AssertJoin(RecordingDispatchProxy<IPublicEvent> eventProxy, IPlayer player)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation join = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
        Assert.Same(player, join.Arguments[0]);
        Assert.Equal(PublicEventTeam.PublicTeam, join.Arguments[1]);
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
        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(objective, update.Arguments[0]);
        Assert.Equal(count, update.Arguments[1]);
    }

    private static void AssertTriggerInitialised(CreatedTrigger trigger, uint worldLocationId, uint objectId)
    {
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Invocation initialise = Assert.Single(
            trigger.Proxy.GetInvocations(nameof(IWorldLocationVolumeGridTriggerEntity.Initialise)));
        Assert.Equal(worldLocationId, initialise.Arguments[0]);
        Assert.Equal(objectId, initialise.Arguments[1]);
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
}
