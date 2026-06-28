using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Script.Instance;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using BayOfBetrayalMapScript = NexusForever.Script.Instance.Adventure.BayOfBetrayal.BayOfBetrayalMapScript;
using CrimelordsOfWhitevaleMapScript = NexusForever.Script.Instance.Adventure.CrimelordsOfWhitevale.CrimelordsOfWhitevaleMapScript;
using DaggerstonePassMapScript = NexusForever.Script.Instance.Battleground.DaggerstonePass.DaggerstonePassMapScript;
using DatascapeMapScript = NexusForever.Script.Instance.Raid.Datascape.DatascapeMapScript;
using DeepSpaceExplorationMapScript = NexusForever.Script.Instance.Expedition.DeepSpaceExploration.DeepSpaceExplorationMapScript;
using EvilFromTheEtherMapScript = NexusForever.Script.Instance.Expedition.EvilFromTheEther.EvilFromTheEtherMapScript;
using DungeonUltimateProtogamesMapScript = NexusForever.Script.Instance.Dungeon.UltimateProtogames.UltimateProtogamesMapScript;
using FragmentZeroMapScript = NexusForever.Script.Instance.Expedition.FragmentZero.FragmentZeroMapScript;
using GeneticArchivesMapScript = NexusForever.Script.Instance.Raid.GeneticArchives.GeneticArchivesMapScript;
using HallOfTheHundredMapScript = NexusForever.Script.Instance.WorldStory.HallOfTheHundred.HallOfTheHundredMapScript;
using HallsOfTheBloodswornMapScript = NexusForever.Script.Instance.Battleground.HallsOfTheBloodsworn.HallsOfTheBloodswornMapScript;
using InfestationMapScript = NexusForever.Script.Instance.Expedition.Infestation.InfestationMapScript;
using InitializationCoreY83MapScript = NexusForever.Script.Instance.Raid.InitializationCoreY83.InitializationCoreY83MapScript;
using JourneyIntoOMNICore1MapScript = NexusForever.Script.Instance.WorldStory.JourneyIntoOMNICore1.JourneyIntoOMNICore1MapScript;
using ProtostarsSuperMallInTheSkyMapScript = NexusForever.Script.Instance.EventInstances.ProtostarsSuperMallInTheSky.ProtostarsSuperMallInTheSkyMapScript;
using UltimateProtogamesDownsizerMapScript = NexusForever.Script.Instance.Dungeon.UltimateProtogames.Downsizer.UltimateProtogamesMapScript;
using RageLogicMapScript = NexusForever.Script.Instance.Expedition.RageLogic.RageLogicMapScript;
using RedMoonTerror40ManMapScript = NexusForever.Script.Instance.Raid.RedMoonTerror.FortyMan.RedMoonTerror40ManMapScript;
using RedMoonTerrorMapScript = NexusForever.Script.Instance.Raid.RedMoonTerror.RedMoonTerrorMapScript;
using RiotInTheVoidMapScript = NexusForever.Script.Instance.Adventure.RiotInTheVoid.RiotInTheVoidMapScript;
using SanctuaryOfTheSwordmaidenMapScript = NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.SanctuaryOfTheSwordmaidenMapScript;
using ShadesEveMapScript = NexusForever.Script.Instance.EventInstances.ShadesEve.ShadesEveMapScript;
using SkullcanoMapScript = NexusForever.Script.Instance.Dungeon.Skullcano.SkullcanoMapScript;
using SpaceMadnessMapScript = NexusForever.Script.Instance.Expedition.SpaceMadness.SpaceMadnessMapScript;
using StormtalonsLairMapScript = NexusForever.Script.Instance.Dungeon.StormtalonsLair.StormtalonsLairMapScript;
using TheCryoPlexMapScript = NexusForever.Script.Instance.Arena.TheCryoPlex.TheCryoPlexMapScript;
using TheHycrestInsurrectionMapScript = NexusForever.Script.Instance.Adventure.TheHycrestInsurrection.TheHycrestInsurrectionMapScript;
using TheMalgraveTrailMapScript = NexusForever.Script.Instance.Adventure.TheMalgraveTrail.TheMalgraveTrailMapScript;
using WalatikiTempleMapScript = NexusForever.Script.Instance.Battleground.WalatikiTemple.WalatikiTempleMapScript;
using WarOfTheWildsMapScript = NexusForever.Script.Instance.Adventure.WarOfTheWilds.WarOfTheWildsMapScript;

namespace NexusForever.Game.Tests.Instances;

public class InstanceMapBindingTests
{
    [Theory]
    [InlineData(typeof(StormtalonsLairMapScript), 145u)]
    [InlineData(typeof(SkullcanoMapScript), 148u)]
    [InlineData(typeof(SanctuaryOfTheSwordmaidenMapScript), 166u)]
    [InlineData(typeof(HallOfTheHundredMapScript), 666u)]
    [InlineData(typeof(DungeonUltimateProtogamesMapScript), 594u)]
    [InlineData(typeof(UltimateProtogamesDownsizerMapScript), 642u)]
    [InlineData(typeof(ProtostarsSuperMallInTheSkyMapScript), 679u)]
    [InlineData(typeof(DeepSpaceExplorationMapScript), 447u)]
    [InlineData(typeof(RageLogicMapScript), 214u)]
    [InlineData(typeof(BayOfBetrayalMapScript), 673u)]
    [InlineData(typeof(CrimelordsOfWhitevaleMapScript), 146u)]
    [InlineData(typeof(RiotInTheVoidMapScript), 179u)]
    [InlineData(typeof(TheHycrestInsurrectionMapScript), 419u)]
    [InlineData(typeof(TheMalgraveTrailMapScript), 53u)]
    [InlineData(typeof(WarOfTheWildsMapScript), 158u)]
    [InlineData(typeof(ShadesEveMapScript), 597u)]
    [InlineData(typeof(FragmentZeroMapScript), 680u)]
    [InlineData(typeof(InfestationMapScript), 95u)]
    [InlineData(typeof(EvilFromTheEtherMapScript), 781u)]
    [InlineData(typeof(SpaceMadnessMapScript), 390u)]
    [InlineData(typeof(JourneyIntoOMNICore1MapScript), 605u)]
    [InlineData(typeof(DatascapeMapScript), 157u)]
    [InlineData(typeof(GeneticArchivesMapScript), 159u)]
    [InlineData(typeof(InitializationCoreY83MapScript), 595u)]
    [InlineData(typeof(RedMoonTerrorMapScript), 705u)]
    [InlineData(typeof(RedMoonTerror40ManMapScript), 650u)]
    public void BranchMapBindings_ExposeMappedPublicEventId(Type scriptType, uint publicEventId)
    {
        object script = CreateMapScript(scriptType);
        Assert.IsType(scriptType, script);

        var mapScript = Assert.IsAssignableFrom<EventBaseContentMapScript>(script);

        Assert.Equal(publicEventId, mapScript.PublicEventId);
    }

    [Fact]
    public void BranchPvpMapBindings_ExposeMappedPublicEventIds()
    {
        IMatchingDataManager matchingDataManager = RecordingDispatchProxy<IMatchingDataManager>.Create(out _);
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out _);

        AssertPvpBinding(new TheCryoPlexMapScript(matchingDataManager, playerManager), 581u, 582u);
        AssertPvpBinding(new DaggerstonePassMapScript(), 438u, 466u);
        AssertPvpBinding(new HallsOfTheBloodswornMapScript(), 876u, 877u);
        AssertPvpBinding(new WalatikiTempleMapScript(), 217u, 366u);
    }

    [Fact]
    public void SanctuaryMapScript_OnLoadCreatesJoinsAndFinishesSpiritualRevivalPublicEvent()
    {
        SanctuaryOfTheSwordmaidenMapScript script = CreateSanctuaryMapScript(
            out RecordingDispatchProxy<IPublicEventManager> managerProxy,
            out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
            out RecordingDispatchProxy<IPublicEvent> spiritualRevivalEventProxy,
            out IContentMapInstance contentMap);

        script.OnLoad(contentMap);

        Assert.Equal(
            [166u, 202u],
            managerProxy.GetInvocations(nameof(IPublicEventManager.CreateEvent))
                .Select(i => (uint)i.Arguments[0])
                .ToArray());

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        script.OnAddToMap(player);

        AssertJoin(mainEventProxy, player);
        AssertJoin(spiritualRevivalEventProxy, player);

        script.OnMatchFinish();

        AssertFinish(mainEventProxy);
        AssertFinish(spiritualRevivalEventProxy);
    }

    private static void AssertPvpBinding(EventBasePvpContentMapScript mapScript, uint publicEventId, uint publicSubEventId)
    {
        Assert.Equal(publicEventId, mapScript.PublicEventId);
        Assert.Equal(publicSubEventId, mapScript.PublicSubEventId);
    }

    private static object CreateMapScript(Type scriptType)
    {
        if (scriptType == typeof(JourneyIntoOMNICore1MapScript))
            return new JourneyIntoOMNICore1MapScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _));
        if (scriptType == typeof(DeepSpaceExplorationMapScript))
            return new DeepSpaceExplorationMapScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _));
        if (scriptType == typeof(ProtostarsSuperMallInTheSkyMapScript))
            return new ProtostarsSuperMallInTheSkyMapScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _));
        if (scriptType == typeof(FragmentZeroMapScript))
            return new FragmentZeroMapScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _));
        if (scriptType == typeof(EvilFromTheEtherMapScript))
            return new EvilFromTheEtherMapScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _));
        if (scriptType == typeof(InfestationMapScript))
            return new InfestationMapScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _));
        if (scriptType == typeof(ShadesEveMapScript))
            return new ShadesEveMapScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _));
        if (scriptType == typeof(SpaceMadnessMapScript))
            return new SpaceMadnessMapScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _));

        return Activator.CreateInstance(scriptType);
    }

    private static SanctuaryOfTheSwordmaidenMapScript CreateSanctuaryMapScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
        out RecordingDispatchProxy<IPublicEvent> spiritualRevivalEventProxy,
        out IContentMapInstance contentMap)
    {
        IPublicEvent mainEvent = RecordingDispatchProxy<IPublicEvent>.Create(out mainEventProxy);
        IPublicEvent spiritualRevivalEvent = RecordingDispatchProxy<IPublicEvent>.Create(out spiritualRevivalEventProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out managerProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.CreateEvent), args =>
        {
            uint id = (uint)args[0];
            return id switch
            {
                166u => mainEvent,
                202u => spiritualRevivalEvent,
                _    => null
            };
        });

        contentMap = RecordingDispatchProxy<IContentMapInstance>.Create(out RecordingDispatchProxy<IContentMapInstance> mapProxy);
        mapProxy.SetProperty(nameof(IContentMapInstance.PublicEventManager), publicEventManager);

        return new SanctuaryOfTheSwordmaidenMapScript();
    }

    private static void AssertJoin(RecordingDispatchProxy<IPublicEvent> eventProxy, IPlayer player)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation join = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
        Assert.Same(player, join.Arguments[0]);
        Assert.Equal(PublicEventTeam.PublicTeam, join.Arguments[1]);
    }

    private static void AssertFinish(RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }
}
