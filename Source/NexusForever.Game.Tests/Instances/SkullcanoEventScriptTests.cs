using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Dungeon.Skullcano;
using NexusForever.Script.Instance.Dungeon.Skullcano.Script;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Instances;

public class SkullcanoEventScriptTests
{
    [Fact]
    public void OnLoad_SetsEnterPhase()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.Enter, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_ActivatesOpeningBossObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithOpeningBosses(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatThunderfoot);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatStewShamanTugga);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_ActivatesBranchOptionalLoppObjective()
    {
        var script = CreateScriptWithOptionalObjectives([PublicEventObjective.FreeCapturedLopp]);
        IPublicEvent publicEvent = CreatePublicEventWithOpeningBosses(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.FreeCapturedLopp);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_SpawnsReviewedOpeningBossPlacements()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithOpeningBosses(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedBoss thunderfoot,
            out CreatedBoss tugga);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatThunderfoot);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatStewShamanTugga);

        AssertOpeningBossModel(
            thunderfoot,
            1100300049u,
            24475u,
            4793,
            new Vector3(115.26f, -923.71f, -491.56f),
            21318u,
            242,
            "ThunderfootNormalEntityScript");
        AssertOpeningBossModel(
            tugga,
            1100300050u,
            24493u,
            1220,
            new Vector3(508.5147f, -978.8553f, -367.8973f),
            27916u,
            868,
            "StewShamanTuggaNormalEntityScript");
        AssertGridEntityAddedToMap(mapProxy, 0, thunderfoot.Instance, new Vector3(115.26f, -923.71f, -491.56f));
        AssertGridEntityAddedToMap(mapProxy, 1, tugga.Instance, new Vector3(508.5147f, -978.8553f, -367.8973f));
    }

    [Fact]
    public void OnPublicEventPhase_Chasm_ActivatesBranchObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(5u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.RandomPathChasm);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.CrossTheLavaFilledChasm &&
            (uint)i.Arguments[1] == 5u);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.FindAWayAcrossTheLava);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GatherPrimalFireEssences);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DontGetStruckByLaveka);
    }

    [Theory]
    [InlineData(false, PublicEventPhase.RandomPathChasm)]
    [InlineData(true, PublicEventPhase.RandomPathFindChief)]
    public void OnPublicEventPhase_RandomPath_UsesWipBranchRouteChoice(bool useFindChiefPath, PublicEventPhase expectedPhase)
    {
        var script = CreateScriptWithFindChiefPath(useFindChiefPath);
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.RandomPath);

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == expectedPhase);
    }

    [Fact]
    public void OnPublicEventPhase_FindChief_ActivatesCavePathObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(4u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.RandomPathFindChief);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.FindChiefKaskalak &&
            (uint)i.Arguments[1] == 4u);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.MineGoldInfusedLavaCores);
    }

    [Fact]
    public void OnPublicEventPhase_FindChief_BroadcastsBranchFactionPair()
    {
        IPlayer exilePlayer = CreatePlayer(Faction.Exile, out IGameSession exileSession);
        IPlayer dominionPlayer = CreatePlayer(Faction.Dominion, out IGameSession dominionSession);
        ICommunicatorMessage dorianMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> dorianProxy);
        ICommunicatorMessage artemisMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> artemisProxy);
        var script = CreateScript(
            (CommunicatorMessage.DorianWalker2, dorianMessage),
            (CommunicatorMessage.ArtemisZin2, artemisMessage));
        IPublicEvent publicEvent = CreatePublicEvent(2u, [exilePlayer, dominionPlayer], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.RandomPathFindChief);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation dorianSend = Assert.Single(dorianProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(exileSession, dorianSend.Arguments[0]);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation artemisSend = Assert.Single(artemisProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(dominionSession, artemisSend.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Cave_ActivatesBranchObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.RandomPathCave);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.EscortChiefKaskalak);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.SurviveMoltenCavernHeat);
    }

    [Fact]
    public void OnPublicEventPhase_Chasm_BroadcastsBranchFactionPair()
    {
        IPlayer exilePlayer = CreatePlayer(Faction.Exile, out IGameSession exileSession);
        IPlayer dominionPlayer = CreatePlayer(Faction.Dominion, out IGameSession dominionSession);
        ICommunicatorMessage dorianMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> dorianProxy);
        ICommunicatorMessage artemisMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> artemisProxy);
        var script = CreateScript(
            (CommunicatorMessage.DorianWalker3, dorianMessage),
            (CommunicatorMessage.ArtemisZin3, artemisMessage));
        IPublicEvent publicEvent = CreatePublicEvent(2u, [exilePlayer, dominionPlayer], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.RandomPathChasm);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation dorianSend = Assert.Single(dorianProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(exileSession, dorianSend.Arguments[0]);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation artemisSend = Assert.Single(artemisProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(dominionSession, artemisSend.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Bosun_ActivatesBossAndFallbackMarauderObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Bosun);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatBosunOctog);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.RidSkullcanoOfMarauders);
        Assert.DoesNotContain(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.FreeTheRedmoonPrisoners);
    }

    [Fact]
    public void OnPublicEventPhase_Bosun_ActivatesBranchOptionalPrisonerObjectives()
    {
        var script = CreateScriptWithOptionalObjectives(
            [
                PublicEventObjective.FreeTheRedmoonPrisoners,
                PublicEventObjective.RidSkullcanoOfMarauders
            ]);
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Bosun);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.FreeTheRedmoonPrisoners);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.RidSkullcanoOfMarauders);
    }

    [Fact]
    public void OnPublicEventPhase_Redmoon_ActivatesMappedObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Redmoon);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.DefeatMordechaiRedmoon, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Bosun_BroadcastsBranchFactionPair()
    {
        IPlayer exilePlayer = CreatePlayer(Faction.Exile, out IGameSession exileSession);
        IPlayer dominionPlayer = CreatePlayer(Faction.Dominion, out IGameSession dominionSession);
        ICommunicatorMessage dorianMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> dorianProxy);
        ICommunicatorMessage artemisMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> artemisProxy);
        var script = CreateScript(
            (CommunicatorMessage.DorianWalker4, dorianMessage),
            (CommunicatorMessage.ArtemisZin4, artemisMessage));
        IPublicEvent publicEvent = CreatePublicEvent(2u, [exilePlayer, dominionPlayer], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Bosun);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation dorianSend = Assert.Single(dorianProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(exileSession, dorianSend.Arguments[0]);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation artemisSend = Assert.Single(artemisProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(dominionSession, artemisSend.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Bosun_BroadcastsBranchOptionalPrisonerFactionPair()
    {
        IPlayer exilePlayer = CreatePlayer(Faction.Exile, out IGameSession exileSession);
        IPlayer dominionPlayer = CreatePlayer(Faction.Dominion, out IGameSession dominionSession);
        ICommunicatorMessage dorianMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> dorianProxy);
        ICommunicatorMessage artemisMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> artemisProxy);
        var script = CreateScriptWithOptionalObjectives(
            [
                PublicEventObjective.FreeTheRedmoonPrisoners
            ],
            (CommunicatorMessage.DorianWalker5, dorianMessage),
            (CommunicatorMessage.ArtemisZin5, artemisMessage));
        IPublicEvent publicEvent = CreatePublicEvent(2u, [exilePlayer, dominionPlayer], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Bosun);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation dorianSend = Assert.Single(dorianProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(exileSession, dorianSend.Arguments[0]);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation artemisSend = Assert.Single(artemisProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(dominionSession, artemisSend.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Platform_UsesCurrentPlayerCount()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Platform);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GatherOnThePlatform, activation.Arguments[0]);
        Assert.Equal(3u, activation.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventPhase_GetToRedmoon_ActivatesFinalApproachObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.GetToRedmoon);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.KillGruharAndTakeStash);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ReachTheEldanTerraformer);
    }

    [Fact]
    public void OnPublicEventPhase_GetToRedmoon_ActivatesBranchOptionalMissileObjective()
    {
        IPlayer exilePlayer = CreatePlayer(Faction.Exile, out IGameSession exileSession);
        IPlayer dominionPlayer = CreatePlayer(Faction.Dominion, out IGameSession dominionSession);
        ICommunicatorMessage dorianMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> dorianProxy);
        ICommunicatorMessage artemisMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> artemisProxy);
        var script = CreateScriptWithOptionalObjectives(
            [
                PublicEventObjective.HackMissileConsoles
            ],
            (CommunicatorMessage.DorianWalker9, dorianMessage),
            (CommunicatorMessage.ArtemisZin9, artemisMessage));
        IPublicEvent publicEvent = CreatePublicEvent(2u, [exilePlayer, dominionPlayer], out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.GetToRedmoon);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.HackMissileConsoles);
        RecordingDispatchProxy<ICommunicatorMessage>.Invocation dorianSend = Assert.Single(dorianProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(exileSession, dorianSend.Arguments[0]);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation artemisSend = Assert.Single(artemisProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(dominionSession, artemisSend.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_GetToRedmoon_ActivatesBranchOptionalGoldTreasureObjective()
    {
        var script = CreateScriptWithOptionalObjectives([PublicEventObjective.GatherShinyGoldObjects]);
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.GetToRedmoon);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillGruharAndTakeStash);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.ReachTheEldanTerraformer);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.GatherShinyGoldObjects);
    }

    [Fact]
    public void GoldCoveredTreasureScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(GoldCoveredTreasureEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 24675u }, attribute.CreatureId);
    }

    [Fact]
    public void GoldCoveredTreasure_OnActivateSuccess_UpdatesActiveChecklistTargetGroupObjectiveOnce()
    {
        var script = new GoldCoveredTreasureEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity treasure);

        script.OnLoad(treasure);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(2610u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);
    }

    [Fact]
    public void ChiefKaskalakScript_IsBoundToMappedTalkTargetGroupRows()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(ChiefKaskalakEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 33452u, 24788u }, attribute.CreatureId);
    }

    [Fact]
    public void ChiefKaskalak_OnActivateSuccess_UpdatesActiveTalkTargetGroupObjectiveOnce()
    {
        var script = new ChiefKaskalakEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity chief);

        script.OnLoad(chief);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Same(player, update.Arguments[0]);
        Assert.Equal(PublicEventObjectiveType.TalkTo, update.Arguments[1]);
        Assert.Equal(3944u, update.Arguments[2]);
        Assert.Equal(1, update.Arguments[3]);
    }

    [Theory]
    [InlineData(PublicEventObjective.DefeatThunderfoot, PublicEventPhase.RandomPath)]
    [InlineData(PublicEventObjective.SpeakToChiefKaskalak, PublicEventPhase.RandomPathCave)]
    [InlineData(PublicEventObjective.EscortChiefKaskalak, PublicEventPhase.Bosun)]
    [InlineData(PublicEventObjective.CrossTheLavaFilledChasm, PublicEventPhase.Bosun)]
    [InlineData(PublicEventObjective.DefeatBosunOctog, PublicEventPhase.Platform)]
    [InlineData(PublicEventObjective.GatherOnThePlatform, PublicEventPhase.GetToRedmoon)]
    [InlineData(PublicEventObjective.ReachTheEldanTerraformer, PublicEventPhase.Redmoon)]
    public void OnPublicEventObjectiveStatus_Success_AdvancesMainChain(PublicEventObjective objective, PublicEventPhase nextPhase)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == nextPhase);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FindChief_ActivatesSpeakObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FindChiefKaskalak, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.SpeakToChiefKaskalak, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KillGruhar_ActivatesTerraformerObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.KillGruharAndTakeStash, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.RedmoonTerraformer, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatMordechaiRedmoon, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatThunderfoot, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, [], out eventProxy);
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, IReadOnlyList<IPlayer> players, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithOpeningBosses(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedBoss thunderfoot,
        out CreatedBoss tugga)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 1263u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        CreatedBoss[] bosses =
        [
            CreateCreatedBoss(),
            CreateCreatedBoss()
        ];
        int createIndex = 0;
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => bosses[createIndex++].Instance);

        thunderfoot = bosses[0];
        tugga = bosses[1];
        return publicEvent;
    }

    private static CreatedBoss CreateCreatedBoss()
    {
        INonPlayerEntity boss = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> bossProxy);
        return new CreatedBoss(boss, bossProxy);
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
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        return publicEventManagerProxy;
    }

    private static SkullcanoEventScript CreateScript(
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript([], messages);
    }

    private static SkullcanoEventScript CreateScriptWithOptionalObjectives(
        PublicEventObjective[] optionalObjectives,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(optionalObjectives, false, messages);
    }

    private static SkullcanoEventScript CreateScriptWithFindChiefPath(
        bool useFindChiefPath,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript([], useFindChiefPath, messages);
    }

    private static SkullcanoEventScript CreateScript(
        IReadOnlyCollection<PublicEventObjective> optionalObjectives,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(optionalObjectives, false, messages);
    }

    private static SkullcanoEventScript CreateScript(
        IReadOnlyCollection<PublicEventObjective> optionalObjectives,
        bool useFindChiefPath,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return new TestSkullcanoEventScript(globalQuestManager, optionalObjectives, useFindChiefPath);
    }

    private static IPlayer CreatePlayer(Faction faction, out IGameSession session)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        session = RecordingDispatchProxy<IGameSession>.Create(out _);
        playerProxy.SetProperty(nameof(IWorldEntity.Faction1), faction);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, PublicEventObjective objective)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
    }

    private static void AssertOpeningBossModel(
        CreatedBoss boss,
        uint entityId,
        uint creatureId,
        ushort areaId,
        Vector3 position,
        uint displayInfo,
        ushort factionId,
        string scriptName)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            boss.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(creatureId, model.Creature);
        Assert.Equal((ushort)1263u, model.World);
        Assert.Equal(areaId, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(displayInfo, model.DisplayInfo);
        Assert.Equal(factionId, model.Faction1);
        Assert.Equal(factionId, model.Faction2);
        Assert.Equal(148u, model.EntityEvent.EventId);
        Assert.Equal(0u, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(scriptName, entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 35f);
    }

    private static void AssertGridEntityAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        int invocationIndex,
        IGridEntity entity,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = mapProxy.GetInvocations(nameof(IMap.EnqueueAdd))[invocationIndex];
        Assert.Same(entity, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(1263u, position.Info.Entry.Id);
    }

    private sealed record CreatedBoss(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed class TestSkullcanoEventScript : SkullcanoEventScript
    {
        private readonly IReadOnlySet<PublicEventObjective> optionalObjectives;

        public TestSkullcanoEventScript(
            IGlobalQuestManager globalQuestManager,
            IEnumerable<PublicEventObjective> optionalObjectives,
            bool useFindChiefPath)
            : base(globalQuestManager)
        {
            this.optionalObjectives = optionalObjectives.ToHashSet();
            UseFindChiefPath = useFindChiefPath;
        }

        private bool UseFindChiefPath { get; }

        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return optionalObjectives.Contains(objective);
        }

        protected override bool ShouldUseWipFindChiefPath()
        {
            return UseFindChiefPath;
        }
    }
}
