using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.PublicEvent;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.PublicEvent;
using NexusForever.Script;
using NexusForever.WorldServer.Network;
using EventTeam = NexusForever.Game.Static.PublicEvent.PublicEventTeam;
using RuntimeEvent = NexusForever.Game.PublicEvent.PublicEvent;
using RuntimeTeam = NexusForever.Game.PublicEvent.PublicEventTeam;

namespace NexusForever.Game.Tests.PublicEvents;

public sealed class PublicEventCompletionOwnershipTests
{
    [Theory]
    [InlineData("current")]
    [InlineData("replacement")]
    [InlineData("unregistered")]
    [InlineData("moved")]
    [InlineData("rebound")]
    public void CompletionUsesTheOriginalActiveMembership(string scenario)
    {
        const uint objectiveId = 201u;
        const uint objectId = 501u;
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        var original = new Actor(map, 42ul, 100u);
        var replacement = new Actor(map, 42ul, 200u);
        var teammate = new Actor(map, 43ul, 300u);
        IPlayer registered = original.Player;
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var playerManager);
        playerManager.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] == 43ul ? teammate.Player : registered);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publicEvent = new RuntimeEvent(
            NullLogger<RuntimeEvent>.Instance,
            RecordingDispatchProxy<IScriptManager>.Create(out _),
            new PublicEventTestSupport.DelegateFactory<IPublicEventTeam>(() => new RuntimeTeam(
                NullLogger<RuntimeTeam>.Instance,
                new PublicEventTestSupport.DelegateFactory<IPublicEventObjective>(() => new PublicEventObjective()),
                new PublicEventTestSupport.DelegateFactory<IPublicEventTeamMember>(() =>
                    new PublicEventTeamMember(NullLogger<PublicEventTeamMember>.Instance, players, realm)),
                new PublicEventTestSupport.ThrowingFactory<IPublicEventVote>(),
                new PublicEventStats())),
            RecordingDispatchProxy<IPublicEventEntityFactory>.Create(out _));
        publicEvent.Initialise(RecordingDispatchProxy<IPublicEventManager>.Create(out _),
            new PublicEventTestSupport.TestPublicEventTemplate(new PublicEventObjectiveEntry
            {
                Id = objectiveId,
                Count = 1,
                ObjectId = objectId,
                PublicEventTeamId = EventTeam.PublicTeam,
                PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
                PublicEventObjectiveTypeEnum = PublicEventObjectiveType.Script,
                PublicEventObjectiveFlags = PublicEventObjectiveFlag.InitialObjective
            }), map);
        publicEvent.JoinEvent(original.Player, EventTeam.PublicTeam);
        publicEvent.JoinEvent(teammate.Player, EventTeam.PublicTeam);

        switch (scenario)
        {
            case "replacement": registered = replacement.Player; break;
            case "unregistered": registered = null; break;
            case "moved": original.Proxy.SetProperty(nameof(IPlayer.Map), RecordingDispatchProxy<IBaseMap>.Create(out _)); break;
            case "rebound": original.Session.SetProperty(nameof(IWorldSession.Player), replacement.Player); break;
        }

        IPlayer contributor = scenario == "replacement" ? replacement.Player : original.Player;
        publicEvent.UpdateObjective(contributor, PublicEventObjectiveType.Script, objectId, 1);
        if (scenario != "current")
            Assert.False(publicEvent.IsFinalised);
        publicEvent.UpdateObjective(objectiveId, 1);

        Assert.True(publicEvent.IsFinalised);
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
        AssertCredit(teammate, objectiveId);
        var scoreboard = Assert.Single(teammate.Packets.OfType<ServerPublicEventEnd>());
        var enrolledRow = Assert.Single(scoreboard.ParticipantStats, row => row.Player.Id == 42ul);
        Assert.Equal(100u, enrolledRow.UnitId);
        AssertNoCredit(replacement);
        if (scenario == "current")
            AssertCredit(original, objectiveId);
        else
            AssertNoCredit(original);
    }

    private static void AssertCredit(Actor actor, uint objectiveId)
    {
        var achievement = Assert.Single(actor.Achievements.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        Assert.Same(actor.Player, achievement.Arguments[0]);
        Assert.Equal(AchievementType.PublicEventObjectiveComplete, achievement.Arguments[1]);
        Assert.Equal(objectiveId, achievement.Arguments[2]);
        var quest = Assert.Single(actor.Quests.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)),
            call => Equals(call.Arguments[0], QuestObjectiveType.CompletePublicEventObjective));
        Assert.Equal(objectiveId, quest.Arguments[1]);
        Assert.Single(actor.Packets.OfType<ServerPublicEventObjectiveStatusUpdate>());
        Assert.Single(actor.Packets.OfType<ServerPublicEventEnd>());
    }

    private static void AssertNoCredit(Actor actor)
    {
        Assert.Empty(actor.Achievements.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        Assert.Empty(actor.Quests.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(actor.Packets.OfType<ServerPublicEventObjectiveStatusUpdate>());
        Assert.Empty(actor.Packets.OfType<ServerPublicEventEnd>());
    }

    private sealed class Actor
    {
        public IPlayer Player { get; }
        public RecordingDispatchProxy<IPlayer> Proxy { get; }
        public RecordingDispatchProxy<IWorldSession> Session { get; }
        public RecordingDispatchProxy<ICharacterAchievementManager> Achievements { get; }
        public RecordingDispatchProxy<IQuestManager> Quests { get; }
        public IEnumerable<object> Packets => Session.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]);

        public Actor(IBaseMap map, ulong characterId, uint guid)
        {
            var player = RecordingDispatchProxy<IPlayer>.Create(out var actor);
            Player = player;
            Proxy = actor;
            var session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
            Session = network;
            network.SetProperty(nameof(IWorldSession.Player), player);
            actor.SetProperty(nameof(IPlayer.CharacterId), characterId);
            actor.SetProperty(nameof(IPlayer.Guid), guid);
            actor.SetProperty(nameof(IPlayer.Session), session);
            actor.SetProperty(nameof(IPlayer.Map), map);
            actor.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var achievements));
            Achievements = achievements;
            actor.SetProperty(nameof(IPlayer.QuestManager), RecordingDispatchProxy<IQuestManager>.Create(out var quests));
            Quests = quests;
        }
    }
}
