using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Tutorial;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Quests.Tutorial;
using NexusForever.Script.Template;

namespace NexusForever.Game.Tests.Quests;

public class TutorialQuestChainRoutingTests
{
    [Fact]
    public void StarterTutorialCompletionRecoveryDoesNotAutoCompleteFinalDepartureQuests()
    {
        Assert.DoesNotContain(StarterTutorialDefinition.ExileDepartureQuestId, StarterTutorialDefinition.ReceiverlessQuestIds);
        Assert.DoesNotContain(StarterTutorialDefinition.DominionDepartureQuestId, StarterTutorialDefinition.ReceiverlessQuestIds);
    }

    [Theory]
    [MemberData(nameof(TutorialFollowUpQuestRoutes))]
    public void TutorialQuestCompletionGrantsExpectedFollowUpQuest(
        ushort questId,
        ushort nextQuestId,
        Func<IGlobalQuestManager, IQuestScript> createScript)
    {
        IQuestInfo nextQuestInfo = RecordingDispatchProxy<IQuestInfo>.Create(out _);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetQuestInfo), args =>
            (ushort)args[0] == nextQuestId ? nextQuestInfo : null);

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), _ => null);

        IQuest owner = CreateQuest(questId, questManager);
        IQuestScript script = createScript(globalQuestManager);
        ((IOwnedScript<IQuest>)script).OnLoad(owner);

        script.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation getQuestInfo = Assert.Single(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetQuestInfo)));
        Assert.Equal(nextQuestId, (ushort)getQuestInfo.Arguments[0]);

        RecordingDispatchProxy<IQuestManager>.Invocation questAdd = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAdd)),
            i => i.Arguments.Length == 1);
        Assert.Same(nextQuestInfo, questAdd.Arguments[0]);
    }

    public static IEnumerable<object[]> TutorialFollowUpQuestRoutes()
    {
        yield return [10518, 10525, (Func<IGlobalQuestManager, IQuestScript>)(g => new Q10518CombatQuestScript(NullLogger<Q10518CombatQuestScript>.Instance, g))];
        yield return [10524, 10526, (Func<IGlobalQuestManager, IQuestScript>)(g => new Q10524CombatQuestScript(NullLogger<Q10524CombatQuestScript>.Instance, g))];
        yield return [10525, 10540, (Func<IGlobalQuestManager, IQuestScript>)(g => new Q10525CryopodConversationsQuestScript(NullLogger<Q10525CryopodConversationsQuestScript>.Instance, g))];
        yield return [10526, 10541, (Func<IGlobalQuestManager, IQuestScript>)(g => new Q10526CryopodConversationsQuestScript(NullLogger<Q10526CryopodConversationsQuestScript>.Instance, g))];
        yield return [10540, 10519, (Func<IGlobalQuestManager, IQuestScript>)(g => new Q10540HoloTerminalQuestScript(NullLogger<Q10540HoloTerminalQuestScript>.Instance, g))];
        yield return [10541, 10522, (Func<IGlobalQuestManager, IQuestScript>)(g => new Q10541HoloTerminalQuestScript(NullLogger<Q10541HoloTerminalQuestScript>.Instance, g))];
        yield return [10519, 10520, (Func<IGlobalQuestManager, IQuestScript>)(g => new Q10519PreparingForDepartureQuestScript(NullLogger<Q10519PreparingForDepartureQuestScript>.Instance, g))];
        yield return [10522, 10523, (Func<IGlobalQuestManager, IQuestScript>)(g => new Q10522PreparingForDepartureQuestScript(NullLogger<Q10522PreparingForDepartureQuestScript>.Instance, g))];
        yield return [10520, 10528, (Func<IGlobalQuestManager, IQuestScript>)(g => new Q10520ShipInteriorQuestScript(NullLogger<Q10520ShipInteriorQuestScript>.Instance, g))];
        yield return [10523, 10530, (Func<IGlobalQuestManager, IQuestScript>)(g => new Q10523ShipInteriorQuestScript(NullLogger<Q10523ShipInteriorQuestScript>.Instance, g))];
    }

    private static IQuest CreateQuest(ushort questId, IQuestManager questManager)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), NexusForever.Game.Static.Reputation.Faction.Exile);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Id), questId);
        questProxy.SetProperty(nameof(IQuest.State), QuestState.Accepted);
        questProxy.SetProperty(nameof(IQuest.Player), player);
        questProxy.SetMethodHandler("GetEnumerator", _ => Enumerable.Empty<IQuestObjective>().GetEnumerator());

        return quest;
    }
}
