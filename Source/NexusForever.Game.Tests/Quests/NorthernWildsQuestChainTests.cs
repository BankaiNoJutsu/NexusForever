using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Quests.NorthernWilds;
using NexusForever.Script.Template;

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
        var script = new Q3486EmpoweredTowerQuestScript(cinematicFactory);

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
    public void Q3673_OnAchieved_QueuesCinematicWithoutGrantingFollowUp()
    {
        IQuest owner = CreateQuest(3673, new Dictionary<ushort, QuestState?>(), out List<ushort> grantedQuests, out _);
        ICinematicFactory cinematicFactory = CreateCinematicFactory(out ICinematicBase cinematic, out _);
        var script = new Q3673ContactWithThaydQuestScript(cinematicFactory, CreateGlobalQuestManager());

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
        var script = new Q3673ContactWithThaydQuestScript(cinematicFactory, CreateGlobalQuestManager());

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Achieved);

        Assert.Equal([3670], grantedQuests);

        RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy = GetCinematicManagerProxy(owner);
        Assert.Empty(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
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
            _ => throw new ArgumentOutOfRangeException(nameof(questId), questId, null)
        };
    }

    private static IQuest CreateQuest(
        ushort questId,
        IReadOnlyDictionary<ushort, QuestState?> questStates,
        out List<ushort> grantedQuests,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy)
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
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);

        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Id), questId);
        questProxy.SetProperty(nameof(IQuest.State), QuestState.Accepted);
        questProxy.SetProperty(nameof(IQuest.Player), player);

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
}
