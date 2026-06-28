using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Adventure.TheSiegeOfTempestRefuge;
using NexusForever.Script.Template.Filter;
using SiegeDominionEventScript = NexusForever.Script.Instance.Adventure.TheSiegeOfTempestRefuge.TheSiegeOfTempestRefugeDominionEventScript;
using SiegeExileEventScript = NexusForever.Script.Instance.Adventure.TheSiegeOfTempestRefuge.TheSiegeOfTempestRefugeExileEventScript;
using SiegeMapScript = NexusForever.Script.Instance.Adventure.TheSiegeOfTempestRefuge.TheSiegeOfTempestRefugeMapScript;
using SiegeObjective = NexusForever.Script.Instance.Adventure.TheSiegeOfTempestRefuge.PublicEventObjective;
using SiegePhase = NexusForever.Script.Instance.Adventure.TheSiegeOfTempestRefuge.PublicEventPhase;

namespace NexusForever.Game.Tests.Instances;

public class TheSiegeOfTempestRefugeAdventureScriptTests
{
    [Fact]
    public void SiegeMapScript_OnLoad_CreatesFactionPublicEvents()
    {
        SiegeMapScript script = CreateMapScript(
            out RecordingDispatchProxy<IPublicEventManager> managerProxy,
            out _,
            out _,
            out IContentMapInstance contentMap);

        script.OnLoad(contentMap);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> invocations =
            managerProxy.GetInvocations(nameof(IPublicEventManager.CreateEvent));
        Assert.Equal([173u, 174u], invocations.Select(i => i.Arguments[0]));
    }

    [Theory]
    [InlineData(Faction.Exile, 173u)]
    [InlineData(Faction.Dominion, 174u)]
    public void SiegeMapScript_OnAddToMap_JoinsPlayerToFactionEvent(Faction faction, uint expectedPublicEventId)
    {
        SiegeMapScript script = CreateMapScript(
            out _,
            out RecordingDispatchProxy<IPublicEvent> exileEventProxy,
            out RecordingDispatchProxy<IPublicEvent> dominionEventProxy,
            out IContentMapInstance contentMap);
        script.OnLoad(contentMap);

        IPlayer player = CreatePlayer(faction);
        script.OnAddToMap(player);

        RecordingDispatchProxy<IPublicEvent> joinedProxy = expectedPublicEventId == 173u
            ? exileEventProxy
            : dominionEventProxy;
        RecordingDispatchProxy<IPublicEvent> otherProxy = expectedPublicEventId == 173u
            ? dominionEventProxy
            : exileEventProxy;

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation =
            Assert.Single(joinedProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[1]);
        Assert.Empty(otherProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
    }

    [Fact]
    public void SiegeMapScript_OnPublicEventFinish_FinishesMatchForEitherFactionEvent()
    {
        SiegeMapScript script = CreateMapScript(
            out _,
            out _,
            out _,
            out IContentMapInstance contentMap,
            out RecordingDispatchProxy<IMatch> matchProxy);
        script.OnLoad(contentMap);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out _);
        script.OnPublicEventFinish(publicEvent, null);
        Assert.Empty(matchProxy.GetInvocations(nameof(IMatch.MatchFinish)));

        publicEvent = GetCreatedPublicEvent(contentMap, 174u);
        script.OnPublicEventFinish(publicEvent, null);

        Assert.Single(matchProxy.GetInvocations(nameof(IMatch.MatchFinish)));
    }

    [Fact]
    public void SiegeMapScript_OnMatchFinish_FinishesBothFactionEvents()
    {
        SiegeMapScript script = CreateMapScript(
            out _,
            out RecordingDispatchProxy<IPublicEvent> exileEventProxy,
            out RecordingDispatchProxy<IPublicEvent> dominionEventProxy,
            out IContentMapInstance contentMap);
        script.OnLoad(contentMap);

        script.OnMatchFinish();

        Assert.Single(exileEventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Single(dominionEventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Theory]
    [InlineData(typeof(SiegeMapScript), 1233u)]
    [InlineData(typeof(SiegeExileEventScript), 173u)]
    [InlineData(typeof(SiegeDominionEventScript), 174u)]
    public void SiegeScripts_AreOwnedByMappedClientRows(Type scriptType, uint ownerId)
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { ownerId }, attribute.Id);
    }

    [Theory]
    [MemberData(nameof(EventCases))]
    public void Main_OnLoad_SetsDefendAssaultPhase(Func<TheSiegeOfTempestRefugeEventScript> factory)
    {
        _ = CreateMainScript(factory, out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation =
            Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(SiegePhase.DefendAgainstAssault, invocation.Arguments[0]);
    }

    [Theory]
    [MemberData(nameof(PhaseActivationCases))]
    public void Main_Phase_ActivatesMappedObjective(
        Func<TheSiegeOfTempestRefugeEventScript> factory,
        SiegePhase phase,
        SiegeObjective objective)
    {
        TheSiegeOfTempestRefugeEventScript script = CreateMainScript(factory, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(objective, invocation.Arguments[0]);
    }

    [Theory]
    [MemberData(nameof(PhaseAdvanceCases))]
    public void Main_ObjectiveSuccess_AdvancesPhase(
        Func<TheSiegeOfTempestRefugeEventScript> factory,
        SiegeObjective objective,
        SiegePhase nextPhase)
    {
        TheSiegeOfTempestRefugeEventScript script = CreateMainScript(factory, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation =
            Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(nextPhase, invocation.Arguments[0]);
    }

    [Theory]
    [MemberData(nameof(FinalObjectiveCases))]
    public void Main_FinalObjective_FinishesPublicEvent(
        Func<TheSiegeOfTempestRefugeEventScript> factory,
        SiegeObjective objective)
    {
        TheSiegeOfTempestRefugeEventScript script = CreateMainScript(factory, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation =
            Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[0]);
    }

    [Theory]
    [MemberData(nameof(EventCases))]
    public void Main_IncompleteObjective_DoesNotAdvance(Func<TheSiegeOfTempestRefugeEventScript> factory)
    {
        TheSiegeOfTempestRefugeEventScript script = CreateMainScript(factory, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(SiegeObjective.DefendAgainstTheDominionAssault, PublicEventStatus.Active));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    public static IEnumerable<object[]> EventCases()
    {
        yield return [() => new SiegeExileEventScript()];
        yield return [() => new SiegeDominionEventScript()];
    }

    public static IEnumerable<object[]> PhaseActivationCases()
    {
        yield return [() => new SiegeExileEventScript(), SiegePhase.DefendAgainstAssault, SiegeObjective.DefendAgainstTheDominionAssault];
        yield return [() => new SiegeExileEventScript(), SiegePhase.DefendGenerator, SiegeObjective.DefendTheGeneratorAgainstDominionAttacks];
        yield return [() => new SiegeExileEventScript(), SiegePhase.FallBackToGenerator, SiegeObjective.FallBackToTheGeneratorDominionAssault];
        yield return [() => new SiegeExileEventScript(), SiegePhase.HoldOutAgainstAssault, SiegeObjective.HoldOutAgainstTheDominionAssault];
        yield return [() => new SiegeDominionEventScript(), SiegePhase.DefendAgainstAssault, SiegeObjective.DefendAgainstTheExileAssault];
        yield return [() => new SiegeDominionEventScript(), SiegePhase.DefendGenerator, SiegeObjective.DefendTheGeneratorAgainstExileAttackers];
        yield return [() => new SiegeDominionEventScript(), SiegePhase.FallBackToGenerator, SiegeObjective.FallBackToTheGeneratorExileAssault];
        yield return [() => new SiegeDominionEventScript(), SiegePhase.HoldOutAgainstAssault, SiegeObjective.HoldOutAgainstTheExileAssault];
    }

    public static IEnumerable<object[]> PhaseAdvanceCases()
    {
        yield return [() => new SiegeExileEventScript(), SiegeObjective.DefendAgainstTheDominionAssault, SiegePhase.DefendGenerator];
        yield return [() => new SiegeExileEventScript(), SiegeObjective.DefendTheGeneratorAgainstDominionAttacks, SiegePhase.FallBackToGenerator];
        yield return [() => new SiegeExileEventScript(), SiegeObjective.FallBackToTheGeneratorDominionAssault, SiegePhase.HoldOutAgainstAssault];
        yield return [() => new SiegeDominionEventScript(), SiegeObjective.DefendAgainstTheExileAssault, SiegePhase.DefendGenerator];
        yield return [() => new SiegeDominionEventScript(), SiegeObjective.DefendTheGeneratorAgainstExileAttackers, SiegePhase.FallBackToGenerator];
        yield return [() => new SiegeDominionEventScript(), SiegeObjective.FallBackToTheGeneratorExileAssault, SiegePhase.HoldOutAgainstAssault];
    }

    public static IEnumerable<object[]> FinalObjectiveCases()
    {
        yield return [() => new SiegeExileEventScript(), SiegeObjective.HoldOutAgainstTheDominionAssault];
        yield return [() => new SiegeDominionEventScript(), SiegeObjective.HoldOutAgainstTheExileAssault];
    }

    private static SiegeMapScript CreateMapScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out RecordingDispatchProxy<IPublicEvent> exileEventProxy,
        out RecordingDispatchProxy<IPublicEvent> dominionEventProxy,
        out IContentMapInstance contentMap)
    {
        return CreateMapScript(
            out managerProxy,
            out exileEventProxy,
            out dominionEventProxy,
            out contentMap,
            out _);
    }

    private static SiegeMapScript CreateMapScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out RecordingDispatchProxy<IPublicEvent> exileEventProxy,
        out RecordingDispatchProxy<IPublicEvent> dominionEventProxy,
        out IContentMapInstance contentMap,
        out RecordingDispatchProxy<IMatch> matchProxy)
    {
        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out managerProxy);
        IPublicEvent exilePublicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out exileEventProxy);
        IPublicEvent dominionPublicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out dominionEventProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.CreateEvent), args =>
        {
            return (uint)args[0] switch
            {
                173u => exilePublicEvent,
                174u => dominionPublicEvent,
                _    => null
            };
        });

        IMatch match = RecordingDispatchProxy<IMatch>.Create(out matchProxy);
        contentMap = RecordingDispatchProxy<IContentMapInstance>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IContentMapInstance.PublicEventManager), manager);
        mapProxy.SetProperty(nameof(IContentMapInstance.Match), match);

        return new SiegeMapScript();
    }

    private static IPublicEvent GetCreatedPublicEvent(IContentMapInstance contentMap, uint publicEventId)
    {
        return contentMap.PublicEventManager.CreateEvent(publicEventId);
    }

    private static IPlayer CreatePlayer(Faction faction)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), faction);
        return player;
    }

    private static TheSiegeOfTempestRefugeEventScript CreateMainScript(
        Func<TheSiegeOfTempestRefugeEventScript> factory,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        TheSiegeOfTempestRefugeEventScript script = factory();
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static IPublicEventObjective CreateObjective(SiegeObjective objective, PublicEventStatus status)
    {
        IPublicEventObjective eventObjective = RecordingDispatchProxy<IPublicEventObjective>.Create(out var objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry
        {
            Id = Convert.ToUInt32(objective)
        });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
        return eventObjective;
    }
}
