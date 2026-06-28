using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden;

namespace NexusForever.Game.Tests.Instances;

public class SanctuaryOfTheSwordmaidenEventScriptTests
{
    [Fact]
    public void OnLoad_SetsEnterPhase()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.Enter, invocation.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventPhase.RandomPath, PublicEventObjective.CollectTorineSpiritRelics)]
    [InlineData(PublicEventPhase.GoToLifeWeaverTerracePath1, PublicEventObjective.EnterLifeweaverTerrace)]
    [InlineData(PublicEventPhase.SpiritmotherSelene, PublicEventObjective.EscortSpiritmotherSelene)]
    [InlineData(PublicEventPhase.SpiritmotherSeleneTheCorrupted, PublicEventObjective.DefeatSpiritmotherSeleneTheCorrupted)]
    public void OnPublicEventPhase_SingleObjectivePhases_ActivateMappedObjective(PublicEventPhase phase, PublicEventObjective objective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(objective, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_ActivatesOpeningObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatDeadringerShallaos);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EliminateZealousTorine);
    }

    [Fact]
    public void OnPublicEventPhase_Temple_ActivatesBranchObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TempleOfTheLifeSpeaker);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EnterTheTempleOfTheLifeSpeaker);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatHammerfistMoldjaw);
    }

    [Fact]
    public void OnPublicEventPhase_MoldwoodCorruption_ActivatesBranchObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.MoldwoodCorruption);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ReachTheMoldwoodCorruption);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatCorruptedEdgesmithTorian);
    }

    [Fact]
    public void OnPublicEventPhase_MoldwoodCorruption_ActivatesBranchOptionalObjective()
    {
        var script = CreateScriptWithOptionalObjectives([PublicEventObjective.FreeTheSpiritsOfTheCorruptedTorineSisters]);
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.MoldwoodCorruption);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.FreeTheSpiritsOfTheCorruptedTorineSisters);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_ActivatesLifeweaverTechClusterOptionalObjective()
    {
        var script = CreateScriptWithOptionalObjectives([PublicEventObjective.SabotageLifeweaverTechClusters]);
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.SabotageLifeweaverTechClusters);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_ActivatesDareiaOptionalObjective()
    {
        var script = CreateScriptWithOptionalObjectives([PublicEventObjective.KillCorruptedDeathbringerDareia]);
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillCorruptedDeathbringerDareia);
    }

    [Fact]
    public void OnPublicEventPhase_Rayna_ActivatesBossAndChallengeObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithFlameCrazedDemon(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.RaynaDarkspeaker);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatRaynaDarkspeaker);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DestroyTheFlameCrazedDemon);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DodgeTorineTotemOfFlame);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DestroyTorineTotemsOfFlame);
    }

    [Fact]
    public void OnPublicEventPhase_Rayna_SpawnsReviewedFlameCrazedDemonPlacement()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithFlameCrazedDemon(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedFlameCrazedDemon createdFlameCrazedDemon);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.RaynaDarkspeaker);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DestroyTheFlameCrazedDemon);

        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            createdFlameCrazedDemon.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300051u, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(29254u, model.Creature);
        Assert.Equal((ushort)1271u, model.World);
        Assert.Equal((ushort)1556u, model.Area);
        Assert.Equal(4877.322f, model.X);
        Assert.Equal(-797.6906f, model.Y);
        Assert.Equal(-3318.368f, model.Z);
        Assert.Equal(24811u, model.DisplayInfo);
        Assert.Equal((ushort)978u, model.Faction1);
        Assert.Equal((ushort)978u, model.Faction2);
        Assert.Equal(166u, model.EntityEvent.EventId);
        Assert.Equal(4u, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal("FlameCrazedDemonEntityScript", entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 40f);
        AssertGridEntityAddedToMap(mapProxy, createdFlameCrazedDemon.Instance, new Vector3(4877.322f, -797.6906f, -3318.368f));
    }

    [Fact]
    public void OnPublicEventPhase_MoldwoodOverlord_ActivatesBranchObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.MoldwoodOverlordSkash);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatMoldwoodOverlordSkash);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatSkashOrHeWillCorruptThePrisoner);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DestroyTheElderMoldwoodRavager);
    }

    [Fact]
    public void OnPublicEventPhase_MoldwoodOverlord_ActivatesBranchOptionalObjectives()
    {
        var script = CreateScriptWithOptionalObjectives(
            [
                PublicEventObjective.DestroyTheMoldwoodCorruptors,
                PublicEventObjective.DestroyMoldwoodSkurgeAndCrawlers,
                PublicEventObjective.KillDistractedMoldwoodMaulers,
                PublicEventObjective.UseTheSoulSporeOnMoldwoodGorgers
            ]);
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.MoldwoodOverlordSkash);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DestroyTheMoldwoodCorruptors);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DestroyMoldwoodSkurgeAndCrawlers);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillDistractedMoldwoodMaulers);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.UseTheSoulSporeOnMoldwoodGorgers);
    }

    [Fact]
    public void OnPublicEventPhase_Path2_ActivatesTerraceAndBranchObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.GoToLifeWeaverTerracePath2);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EnterLifeweaverTerrace);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.KillCorruptedLifecallerKhalee);
    }

    [Fact]
    public void OnPublicEventPhase_Ondu_ActivatesBossAndGuardianObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.OnduLifeWeaver);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatOnduLifeweaver);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.KillTheLifeweaverGuardian);
    }

    [Fact]
    public void OnPublicEventPhase_Ondu_ActivatesBranchOptionalObjectives()
    {
        var script = CreateScriptWithOptionalObjectives(
            [
                PublicEventObjective.DestroyDeathstingSwarms,
                PublicEventObjective.KillCorruptedVeteranSwordmaidens,
                PublicEventObjective.KillTheCorruptedTerrorantulas,
                PublicEventObjective.DefeatCorruptedLifeweaverPell
            ]);
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.OnduLifeWeaver);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DestroyDeathstingSwarms);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillCorruptedVeteranSwordmaidens);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillTheCorruptedTerrorantulas);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatCorruptedLifeweaverPell);
    }

    [Fact]
    public void OnPublicEventPhase_Relics_ActivatesPlacementObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.PlaceSpiritRelics);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.PlaceTheSpiritRelics);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.PlaceSpiritRelicsButDoNotfall);
    }

    [Fact]
    public void OnPublicEventPhase_RandomPath_BroadcastsMappedSeleneMessage()
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((CommunicatorMessage.SpiritMotherSelene14, message));
        IPublicEvent publicEvent = CreatePublicEvent([player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.RandomPath);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Theory]
    [InlineData(false, PublicEventPhase.MoldwoodCorruption)]
    [InlineData(true, PublicEventPhase.TempleOfTheLifeSpeaker)]
    public void OnPublicEventPhase_RandomPath_UsesWipBranchRouteChoice(bool useTempleRoute, PublicEventPhase expectedPhase)
    {
        var script = CreateScriptWithRouteChoice(useTempleRoute);
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.RandomPath);

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == expectedPhase);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_RelicComplete_AfterMoldwoodRoute_DoesNotOverridePath()
    {
        var script = CreateScriptWithRouteChoice(false);
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);
        script.OnPublicEventPhase((uint)PublicEventPhase.RandomPath);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.CollectTorineSpiritRelics, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.MoldwoodCorruption);
        Assert.DoesNotContain(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.TempleOfTheLifeSpeaker);
    }

    [Theory]
    [InlineData(PublicEventObjective.DefeatDeadringerShallaos, PublicEventPhase.RandomPath)]
    [InlineData(PublicEventObjective.CollectTorineSpiritRelics, PublicEventPhase.TempleOfTheLifeSpeaker)]
    [InlineData(PublicEventObjective.EnterTheTempleOfTheLifeSpeaker, PublicEventPhase.RaynaDarkspeaker)]
    [InlineData(PublicEventObjective.ReachTheMoldwoodCorruption, PublicEventPhase.MoldwoodOverlordSkash)]
    [InlineData(PublicEventObjective.DefeatRaynaDarkspeaker, PublicEventPhase.GoToLifeWeaverTerracePath1)]
    [InlineData(PublicEventObjective.DefeatMoldwoodOverlordSkash, PublicEventPhase.GoToLifeWeaverTerracePath2)]
    [InlineData(PublicEventObjective.EnterLifeweaverTerrace, PublicEventPhase.OnduLifeWeaver)]
    [InlineData(PublicEventObjective.DefeatOnduLifeweaver, PublicEventPhase.PlaceSpiritRelics)]
    [InlineData(PublicEventObjective.PlaceTheSpiritRelics, PublicEventPhase.SpiritmotherSelene)]
    [InlineData(PublicEventObjective.EscortSpiritmotherSelene, PublicEventPhase.SpiritmotherSeleneTheCorrupted)]
    public void OnPublicEventObjectiveStatus_Success_AdvancesConservativeChain(PublicEventObjective objective, PublicEventPhase nextPhase)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == nextPhase);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatSpiritmotherSeleneTheCorrupted, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatDeadringerShallaos, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    private static IPublicEvent CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent([], out eventProxy);
    }

    private static IPublicEvent CreatePublicEvent(IReadOnlyList<IPlayer> players, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithFlameCrazedDemon(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedFlameCrazedDemon createdFlameCrazedDemon)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 1271u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        CreatedFlameCrazedDemon flameCrazedDemon = CreateCreatedFlameCrazedDemon();
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => flameCrazedDemon.Instance);

        createdFlameCrazedDemon = flameCrazedDemon;
        return publicEvent;
    }

    private static CreatedFlameCrazedDemon CreateCreatedFlameCrazedDemon()
    {
        INonPlayerEntity flameCrazedDemon = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> flameCrazedDemonProxy);
        return new CreatedFlameCrazedDemon(flameCrazedDemon, flameCrazedDemonProxy);
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

    private static SanctuaryOfTheSwordmaidenEventScript CreateScript(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript([], messages);
    }

    private static SanctuaryOfTheSwordmaidenEventScript CreateScriptWithOptionalObjectives(
        PublicEventObjective[] optionalObjectives,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(optionalObjectives, true, messages);
    }

    private static SanctuaryOfTheSwordmaidenEventScript CreateScriptWithRouteChoice(
        bool useTempleRoute,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript([], useTempleRoute, messages);
    }

    private static SanctuaryOfTheSwordmaidenEventScript CreateScript(
        IReadOnlyCollection<PublicEventObjective> optionalObjectives,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(optionalObjectives, true, messages);
    }

    private static SanctuaryOfTheSwordmaidenEventScript CreateScript(
        IReadOnlyCollection<PublicEventObjective> optionalObjectives,
        bool useTempleRoute,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return new TestSanctuaryOfTheSwordmaidenEventScript(globalQuestManager, optionalObjectives, useTempleRoute);
    }

    private static IPlayer CreatePlayer(out IGameSession session)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        session = RecordingDispatchProxy<IGameSession>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, PublicEventObjective objective)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
    }

    private static void AssertGridEntityAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity entity,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(entity, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(1271u, position.Info.Entry.Id);
    }

    private sealed record CreatedFlameCrazedDemon(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed class TestSanctuaryOfTheSwordmaidenEventScript : SanctuaryOfTheSwordmaidenEventScript
    {
        private readonly IReadOnlySet<PublicEventObjective> optionalObjectives;

        public TestSanctuaryOfTheSwordmaidenEventScript(
            IGlobalQuestManager globalQuestManager,
            IEnumerable<PublicEventObjective> optionalObjectives,
            bool useTempleRoute)
            : base(globalQuestManager)
        {
            this.optionalObjectives = optionalObjectives.ToHashSet();
            UseTempleRoute = useTempleRoute;
        }

        private bool UseTempleRoute { get; }

        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return optionalObjectives.Contains(objective);
        }

        protected override bool ShouldUseWipTempleRoute()
        {
            return UseTempleRoute;
        }
    }
}
