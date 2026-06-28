using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Adventure.BayOfBetrayal;
using NexusForever.Script.Template.Filter;
using BayEventScript = NexusForever.Script.Instance.Adventure.BayOfBetrayal.BayOfBetrayalEventScript;
using BayIntroScript = NexusForever.Script.Instance.Adventure.BayOfBetrayal.BayOfBetrayalIntroEventScript;
using BayObjective = NexusForever.Script.Instance.Adventure.BayOfBetrayal.PublicEventObjective;
using BayPhase = NexusForever.Script.Instance.Adventure.BayOfBetrayal.PublicEventPhase;

namespace NexusForever.Game.Tests.Instances;

public class BayOfBetrayalAdventureScriptTests
{
    [Fact]
    public void BayMapScript_OnLoad_CreatesMainAndIntroPublicEvents()
    {
        BayOfBetrayalMapScript script = CreateMapScript(
            out RecordingDispatchProxy<IPublicEventManager> managerProxy,
            out _,
            out _,
            out IContentMapInstance contentMap);

        script.OnLoad(contentMap);

        Assert.Equal(
            [673u, 672u],
            managerProxy.GetInvocations(nameof(IPublicEventManager.CreateEvent))
                .Select(i => (uint)i.Arguments[0])
                .ToArray());
    }

    [Fact]
    public void BayMapScript_OnAddToMap_JoinsPlayersToMainAndIntroPublicEvents()
    {
        BayOfBetrayalMapScript script = CreateMapScript(
            out _,
            out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
            out RecordingDispatchProxy<IPublicEvent> introEventProxy,
            out IContentMapInstance contentMap);
        script.OnLoad(contentMap);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        script.OnAddToMap(player);

        AssertJoin(mainEventProxy, player);
        AssertJoin(introEventProxy, player);
    }

    [Theory]
    [InlineData(typeof(BayOfBetrayalMapScript), 3176u)]
    [InlineData(typeof(BayIntroScript), 672u)]
    [InlineData(typeof(BayEventScript), 673u)]
    public void BayScripts_AreOwnedByMappedClientRows(Type scriptType, uint ownerId)
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { ownerId }, attribute.Id);
    }

    [Fact]
    public void Intro_OnLoad_SetsTalkPhase()
    {
        _ = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(BayPhase.TalkToHeraldAnkumar, invocation.Arguments[0]);
    }

    [Fact]
    public void Intro_TalkPhase_ActivatesTalkObjective()
    {
        BayIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)BayPhase.TalkToHeraldAnkumar);

        AssertObjectiveActivated(eventProxy, BayObjective.TalkToHeraldAnkumar);
    }

    [Fact]
    public void Intro_TalkSuccess_StartsSpeechPhase()
    {
        BayIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(BayObjective.TalkToHeraldAnkumar, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(BayPhase.ListenToHeraldAnkumarSpeech, invocation.Arguments[0]);
    }

    [Fact]
    public void Intro_SpeechPhase_ActivatesAndCreditsSpeechObjective()
    {
        BayIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)BayPhase.ListenToHeraldAnkumarSpeech);

        AssertObjectiveActivated(eventProxy, BayObjective.ListenToHeraldAnkumarSpeech);
        AssertObjectiveUpdated(eventProxy, BayObjective.ListenToHeraldAnkumarSpeech, 1);
    }

    [Fact]
    public void Intro_SpeechSuccess_StartsProtectPhase()
    {
        BayIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(BayObjective.ListenToHeraldAnkumarSpeech, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(BayPhase.ProtectHeraldAnkumar, invocation.Arguments[0]);
    }

    [Fact]
    public void Intro_ProtectPhase_ActivatesProtectObjective()
    {
        BayIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)BayPhase.ProtectHeraldAnkumar);

        AssertObjectiveActivated(eventProxy, BayObjective.ProtectHeraldAnkumar);
    }

    [Fact]
    public void Intro_ProtectSuccess_FinishesIntroPublicEvent()
    {
        BayIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(BayObjective.ProtectHeraldAnkumar, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[0]);
    }

    [Fact]
    public void Main_OnLoad_SetsSurviveTrialsPhase()
    {
        _ = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(BayPhase.SurviveTheTrials, invocation.Arguments[0]);
    }

    [Fact]
    public void Main_SurviveTrialsPhase_ActivatesParentObjective()
    {
        BayEventScript script = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)BayPhase.SurviveTheTrials);

        AssertObjectiveActivated(eventProxy, BayObjective.SurviveTheTrials);
    }

    [Fact]
    public void Main_SurviveTrialsSuccess_FinishesPublicEvent()
    {
        BayEventScript script = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(BayObjective.SurviveTheTrials, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[0]);
    }

    private static BayOfBetrayalMapScript CreateMapScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
        out RecordingDispatchProxy<IPublicEvent> introEventProxy,
        out IContentMapInstance contentMap)
    {
        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out managerProxy);
        IPublicEvent mainEvent = RecordingDispatchProxy<IPublicEvent>.Create(out mainEventProxy);
        IPublicEvent introEvent = RecordingDispatchProxy<IPublicEvent>.Create(out introEventProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.CreateEvent), args =>
        {
            return (uint)args[0] switch
            {
                673u => mainEvent,
                672u => introEvent,
                _    => null
            };
        });

        contentMap = RecordingDispatchProxy<IContentMapInstance>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IContentMapInstance.PublicEventManager), manager);

        return new BayOfBetrayalMapScript();
    }

    private static BayIntroScript CreateIntroScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        var script = new BayIntroScript();
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static BayEventScript CreateMainScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        var script = new BayEventScript();
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static void AssertJoin(RecordingDispatchProxy<IPublicEvent> eventProxy, IPlayer player)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[1]);
    }

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, BayObjective objective)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(objective, invocation.Arguments[0]);
    }

    private static void AssertObjectiveUpdated(RecordingDispatchProxy<IPublicEvent> eventProxy, BayObjective objective, int count)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(objective, invocation.Arguments[0]);
        Assert.Equal(count, invocation.Arguments[1]);
    }

    private static IPublicEventObjective CreateObjective(BayObjective objective, PublicEventStatus status)
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
