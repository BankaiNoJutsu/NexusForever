using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Tutorial;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Quests.Tutorial;
using NexusForever.Script.Template;
using static NexusForever.Game.Static.Tutorial.StarterTutorialDefinition;

namespace NexusForever.Game.Tests.Quests;

public class StarterTutorialDepartureRoutingTests
{
    private const float Tolerance = 0.0001f;

    [Theory]
    [MemberData(nameof(ExileTerminalRoutes))]
    public void ExileDepartureRoutingResolvesTerminalDestination(
        uint? terminalCreatureId,
        ushort expectedWorldId,
        Vector3 expectedPosition,
        ushort expectedWelcomeQuestId)
    {
        StarterTutorialDepartureDestination destination = StarterTutorialDepartureRouting.ResolveExileDestination(terminalCreatureId);

        Assert.Equal(expectedWorldId, destination.WorldId);
        Assert.Equal(expectedPosition.X, destination.Position.X, Tolerance);
        Assert.Equal(expectedPosition.Y, destination.Position.Y, Tolerance);
        Assert.Equal(expectedPosition.Z, destination.Position.Z, Tolerance);
        Assert.Equal(expectedWelcomeQuestId, destination.WelcomeQuestId);
    }

    [Theory]
    [MemberData(nameof(DominionTerminalRoutes))]
    public void DominionDepartureRoutingResolvesTerminalDestination(
        uint? terminalCreatureId,
        ushort expectedWorldId,
        Vector3 expectedPosition,
        ushort expectedWelcomeQuestId)
    {
        StarterTutorialDepartureDestination destination = StarterTutorialDepartureRouting.ResolveDominionDestination(terminalCreatureId);

        Assert.Equal(expectedWorldId, destination.WorldId);
        Assert.Equal(expectedPosition.X, destination.Position.X, Tolerance);
        Assert.Equal(expectedPosition.Y, destination.Position.Y, Tolerance);
        Assert.Equal(expectedPosition.Z, destination.Position.Z, Tolerance);
        Assert.Equal(expectedWelcomeQuestId, destination.WelcomeQuestId);
    }

    [Theory]
    [InlineData(10528, ExileEverstarGroveDepartureTerminalCreatureId, 990, 9113)]
    [InlineData(10528, ExileNorthernWildsDepartureTerminalCreatureId, 426, 9112)]
    [InlineData(10528, DominionCrimsonIsleDepartureTerminalCreatureId, 426, 9112)]
    [InlineData(10528, null, 426, 9112)]
    [InlineData(10530, DominionCrimsonIsleDepartureTerminalCreatureId, 870, 9127)]
    [InlineData(10530, DominionLevianBayDepartureTerminalCreatureId, 1387, 9126)]
    [InlineData(10530, ExileEverstarGroveDepartureTerminalCreatureId, 870, 9127)]
    [InlineData(10530, null, 870, 9127)]
    public void DepartureQuestCompletionTeleportsAndGrantsWelcomeQuest(
        ushort questId,
        uint? terminalCreatureId,
        ushort expectedWorldId,
        ushort expectedWelcomeQuestId)
    {
        IQuestInfo welcomeQuestInfo = RecordingDispatchProxy<IQuestInfo>.Create(out _);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetQuestInfo), args =>
            (ushort)args[0] == expectedWelcomeQuestId ? welcomeQuestInfo : null);

        IQuest owner = CreateQuest(
            questId,
            questId == 10528 ? Faction.Exile : Faction.Dominion,
            terminalCreatureId,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        IQuestScript script = questId switch
        {
            10528 => new Q10528EscapePodQuestScript(NullLogger<Q10528EscapePodQuestScript>.Instance, globalQuestManager),
            10530 => new Q10530EscapePodQuestScript(NullLogger<Q10530EscapePodQuestScript>.Instance, globalQuestManager),
            _     => throw new ArgumentOutOfRangeException(nameof(questId))
        };

        ((IOwnedScript<IQuest>)script).OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);

        RecordingDispatchProxy<IPlayer>.Invocation teleport = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)),
            i => i.Arguments.Length == 6 && i.Arguments[0] is ushort);

        Assert.Equal(expectedWorldId, (ushort)teleport.Arguments[0]);
        Assert.Equal(TeleportReason.Relocate, teleport.Arguments[5]);

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation getQuestInfo = Assert.Single(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetQuestInfo)));
        Assert.Equal(expectedWelcomeQuestId, (ushort)getQuestInfo.Arguments[0]);

        RecordingDispatchProxy<IQuestManager>.Invocation questAdd = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAdd)),
            i => i.Arguments.Length == 1);
        Assert.Same(welcomeQuestInfo, questAdd.Arguments[0]);
    }

    [Theory]
    [InlineData(10528, ExileEverstarGroveDepartureTerminalCreatureId, 9113)]
    [InlineData(10530, DominionLevianBayDepartureTerminalCreatureId, 9126)]
    public void DepartureQuestCompletionDoesNotDuplicateExistingWelcomeQuest(
        ushort questId,
        uint terminalCreatureId,
        ushort expectedWelcomeQuestId)
    {
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        IQuest owner = CreateQuest(
            questId,
            questId == 10528 ? Faction.Exile : Faction.Dominion,
            terminalCreatureId,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            existingWelcomeQuestState: QuestState.Accepted);
        IQuestScript script = CreateDepartureScript(questId, globalQuestManager);

        ((IOwnedScript<IQuest>)script).OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);

        Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)),
            i => i.Arguments.Length == 6 && i.Arguments[0] is ushort);

        RecordingDispatchProxy<IQuestManager>.Invocation getQuestState = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.GetQuestState)));
        Assert.Equal(expectedWelcomeQuestId, (ushort)getQuestState.Arguments[0]);
        Assert.Empty(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetQuestInfo)));
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAdd)));
    }

    [Theory]
    [InlineData(10528, ExileEverstarGroveDepartureTerminalCreatureId)]
    [InlineData(10530, DominionCrimsonIsleDepartureTerminalCreatureId)]
    public void DepartureQuestCompletionDoesNotGrantWelcomeQuestWhenTeleportIsDenied(
        ushort questId,
        uint terminalCreatureId)
    {
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        IQuest owner = CreateQuest(
            questId,
            questId == 10528 ? Faction.Exile : Faction.Dominion,
            terminalCreatureId,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            canTeleport: false);
        IQuestScript script = CreateDepartureScript(questId, globalQuestManager);

        ((IOwnedScript<IQuest>)script).OnLoad(owner);
        script.OnQuestStateChange(QuestState.Completed, QuestState.Accepted);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Empty(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetQuestInfo)));
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAdd)));
    }

    public static IEnumerable<object[]> ExileTerminalRoutes()
    {
        Vector3 northernWilds = new(4086f, -683f, -5217f);

        yield return [ExileEverstarGroveDepartureTerminalCreatureId, 990, new Vector3(-771.823f, -904.2852f, -2269.56f), 9113];
        yield return [ExileNorthernWildsDepartureTerminalCreatureId, 426, northernWilds, 9112];
        yield return [DominionCrimsonIsleDepartureTerminalCreatureId, 426, northernWilds, 9112];
        yield return [null, 426, northernWilds, 9112];
    }

    public static IEnumerable<object[]> DominionTerminalRoutes()
    {
        Vector3 crimsonIsle = new(-8261.3984f, -995.471f, -242.3648f);

        yield return [DominionCrimsonIsleDepartureTerminalCreatureId, 870, crimsonIsle, 9127];
        yield return [DominionLevianBayDepartureTerminalCreatureId, 1387, new Vector3(-3835.341f, -980.2174f, -6050.524f), 9126];
        yield return [ExileEverstarGroveDepartureTerminalCreatureId, 870, crimsonIsle, 9127];
        yield return [null, 870, crimsonIsle, 9127];
    }

    private static IQuest CreateQuest(
        ushort questId,
        Faction faction,
        uint? terminalCreatureId,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        bool canTeleport = true,
        QuestState? existingWelcomeQuestState = null)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), _ => existingWelcomeQuestState);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), faction);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.StarterTutorialDepartureTerminalCreatureId), terminalCreatureId);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), canTeleport);

        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Id), questId);
        questProxy.SetProperty(nameof(IQuest.State), QuestState.Accepted);
        questProxy.SetProperty(nameof(IQuest.Player), player);
        questProxy.SetMethodHandler("GetEnumerator", _ => Enumerable.Empty<IQuestObjective>().GetEnumerator());

        return quest;
    }

    private static IQuestScript CreateDepartureScript(ushort questId, IGlobalQuestManager globalQuestManager)
    {
        return questId switch
        {
            10528 => new Q10528EscapePodQuestScript(NullLogger<Q10528EscapePodQuestScript>.Instance, globalQuestManager),
            10530 => new Q10530EscapePodQuestScript(NullLogger<Q10530EscapePodQuestScript>.Instance, globalQuestManager),
            _     => throw new ArgumentOutOfRangeException(nameof(questId))
        };
    }
}
