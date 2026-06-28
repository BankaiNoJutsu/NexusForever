using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Quests.CrimsonIsle;
using NexusForever.Script.Template;

namespace NexusForever.Game.Tests.Quests;

public class CrimsonIsleQuestChainTests
{
    [Fact]
    public void Q5593_OnCompleted_GrantsPoweringDownAndStasisInterrupted()
    {
        IQuest owner = CreateQuest(5593, new Dictionary<ushort, QuestState?>(), out List<ushort> grantedQuests);
        var script = new Q5593QuestScript(
            NullLogger<Q5593QuestScript>.Instance,
            CreateGlobalQuestManager());

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);

        Assert.Equal([5573, 8855], grantedQuests);
    }

    [Theory]
    [InlineData(5595, 5575)]
    [InlineData(5596, 5597)]
    [InlineData(5597, 5604)]
    public void FollowUpQuest_OnCompleted_GrantsExpectedNextQuest(ushort questId, ushort expectedNextQuestId)
    {
        IQuest owner = CreateQuest(questId, new Dictionary<ushort, QuestState?>(), out List<ushort> grantedQuests);
        IQuestScript script = CreateFollowUpScript(questId, CreateGlobalQuestManager());

        ((IOwnedScript<IQuest>)script).OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);

        Assert.Equal([expectedNextQuestId], grantedQuests);
    }

    [Theory]
    [InlineData(5573)]
    [InlineData(5575)]
    public void BranchCompletion_GrantsOrdnanceRecovery(ushort questId)
    {
        IQuest owner = CreateQuest(questId, new Dictionary<ushort, QuestState?>(), out List<ushort> grantedQuests);
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager();

        switch (questId)
        {
            case 5573:
                var cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
                var poweringDown = new Q5573PoweringDownQuestScript(
                    cinematicFactory,
                    globalQuestManager,
                    NullLogger<Q5573PoweringDownQuestScript>.Instance);
                poweringDown.OnLoad(owner);
                poweringDown.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);
                break;
            case 5575:
                var seizingPower = new Q5575QuestScript(
                    NullLogger<Q5575QuestScript>.Instance,
                    globalQuestManager);
                seizingPower.OnLoad(owner);
                seizingPower.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);
                break;
        }

        Assert.Equal([5596], grantedQuests);
    }

    [Fact]
    public void Q5604_OnCompleted_GrantsRadioSilenceAndHeavyArmor()
    {
        IQuest owner = CreateQuest(5604, new Dictionary<ushort, QuestState?>(), out List<ushort> grantedQuests);
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager();
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        var script = new Q5604TacticalDemolitionsQuestScript(
            cinematicFactory,
            globalQuestManager,
            NullLogger<Q5604TacticalDemolitionsQuestScript>.Instance);

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);

        Assert.Equal([5580, 5583], grantedQuests);
    }

    [Fact]
    public void Q5580_OnCompleted_WhenHeavyArmorMissing_DoesNotGrantLastResistance()
    {
        IQuest owner = CreateQuest(5580, new Dictionary<ushort, QuestState?>
        {
            [5580] = QuestState.Completed,
        }, out List<ushort> grantedQuests);
        var script = new Q5580QuestScript(
            NullLogger<Q5580QuestScript>.Instance,
            CreateGlobalQuestManager());

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);

        Assert.Empty(grantedQuests);
    }

    [Fact]
    public void Q5583_OnCompleted_WhenRadioSilenceCompleted_GrantsLastResistance()
    {
        IQuest owner = CreateQuest(5583, new Dictionary<ushort, QuestState?>
        {
            [5580] = QuestState.Completed,
            [5583] = QuestState.Completed,
        }, out List<ushort> grantedQuests);
        var script = new Q5583QuestScript(
            NullLogger<Q5583QuestScript>.Instance,
            CreateGlobalQuestManager());

        script.OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);

        Assert.Equal([5594], grantedQuests);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void Q5594Warbot_OnKilled_GuardsAchievementAndCreditsObjective(bool alreadyCompleted, int expectedAchievementGrants)
    {
        IPlayer player = CreatePlayerWithQuestAndAchievementManagers(
            alreadyCompleted,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        var script = new Q5594WarbotEntityScript();

        script.OnKilled(player);

        Assert.Equal(expectedAchievementGrants, achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)).Count);
        if (expectedAchievementGrants == 1)
            Assert.Equal((ushort)1730, achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement))[0].Arguments[0]);

        RecordingDispatchProxy<IQuestManager>.Invocation objectiveUpdate = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(8249u, objectiveUpdate.Arguments[0]);
        Assert.Equal(1u, objectiveUpdate.Arguments[1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(QuestState.Completed)]
    public void Q5594Warbot_OnKilled_WhenLastResistanceNotAccepted_DoesNotGrantAchievementOrCredit(QuestState? questState)
    {
        IPlayer player = CreatePlayerWithQuestAndAchievementManagers(
            alreadyCompleted: false,
            lastResistanceQuestState: questState,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        var script = new Q5594WarbotEntityScript();

        script.OnKilled(player);

        Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.HasCompletedAchievement)));
        Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    private static IQuest CreateQuest(
        ushort questId,
        IReadOnlyDictionary<ushort, QuestState?> questStates,
        out List<ushort> grantedQuests)
    {
        grantedQuests = [];
        List<ushort> capturedGrantedQuests = grantedQuests;

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
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

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Id), questId);
        questProxy.SetProperty(nameof(IQuest.Player), player);

        return quest;
    }

    private static IQuestScript CreateFollowUpScript(ushort questId, IGlobalQuestManager globalQuestManager)
    {
        return questId switch
        {
            5595 => new Q5595QuestScript(
                NullLogger<Q5595QuestScript>.Instance,
                globalQuestManager),
            5596 => new Q5596QuestScript(
                NullLogger<Q5596QuestScript>.Instance,
                globalQuestManager),
            5597 => new Q5597QuestScript(
                NullLogger<Q5597QuestScript>.Instance,
                globalQuestManager),
            _ => throw new ArgumentOutOfRangeException(nameof(questId), questId, null)
        };
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

    private static IPlayer CreatePlayerWithQuestAndAchievementManagers(
        bool alreadyCompleted,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy)
    {
        return CreatePlayerWithQuestAndAchievementManagers(
            alreadyCompleted,
            QuestState.Accepted,
            out achievementManagerProxy,
            out questManagerProxy);
    }

    private static IPlayer CreatePlayerWithQuestAndAchievementManagers(
        bool alreadyCompleted,
        QuestState? lastResistanceQuestState,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy)
    {
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementManagerProxy);
        achievementManagerProxy.SetMethodHandler(nameof(ICharacterAchievementManager.HasCompletedAchievement), args =>
        {
            Assert.Equal((ushort)1730, args[0]);
            return alreadyCompleted;
        });

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
        {
            Assert.Equal((ushort)5594, args[0]);
            return lastResistanceQuestState;
        });

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        return player;
    }
}
