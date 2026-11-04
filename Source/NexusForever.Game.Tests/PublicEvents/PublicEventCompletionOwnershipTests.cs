using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
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
using NexusForever.Network.World.Message.Model;
using NexusForever.Script;
using NexusForever.WorldServer.Network;
using EventTeam = NexusForever.Game.Static.PublicEvent.PublicEventTeam;
using RuntimeEvent = NexusForever.Game.PublicEvent.PublicEvent;
using RuntimeTeam = NexusForever.Game.PublicEvent.PublicEventTeam;
using SaveActor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using SaveFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using Xunit.Abstractions;

namespace NexusForever.Game.Tests.PublicEvents;

public sealed class PublicEventCompletionOwnershipTests(ITestOutputHelper output)
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
        var publicEvent = CreateEvent(players, map, objectiveId, objectId);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicEventFinalityAndQuestCreditAreSeparateFromCharacterRollback(bool useWorldSaveQueue)
    {
        const uint objectiveId = 201u;
        using var f = new SaveFixture(null, new QuestObjectiveEntry
        {
            Id = 90010, Type = (uint)QuestObjectiveType.CompletePublicEventObjective, Data = objectiveId, Count = 2
        });
        f.Seed();
        using var actor = new SaveActor(f);
        if (useWorldSaveQueue)
            await actor.Load();
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        actor.Set(nameof(IPlayer.Map), map);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodReturn(nameof(IPlayerManager.GetPlayer), actor.Player);
        var publicEvent = CreateEvent(players, map, objectiveId, 501);
        publicEvent.JoinEvent(actor.Player, EventTeam.PublicTeam);
        using (var setup = f.Context())
            await setup.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER reject_public_event_quest BEFORE UPDATE ON character_quest_objective
                BEGIN SELECT RAISE(ABORT, 'owned public event quest write failure'); END
                """);

        publicEvent.UpdateObjective(objectiveId, 1);
        Assert.True(publicEvent.IsFinalised);
        Assert.Equal(QuestState.Achieved, actor.Quest.State);
        output.WriteLine("Quest objective updates before Character save: {0}",
            f.Source.SessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
                .Count(call => call.Arguments[0] is ServerQuestObjectiveUpdate));

        Task Save() => CharacterAtomicSave.Execute(() => f.Context(), actor.Player.CharacterId, actor.Player.Save);
        if (useWorldSaveQueue)
        {
            actor.StartSave();
            await actor.Drain();
            output.WriteLine("World save disconnect requests: {0}. Quest reload required: {1}.",
                f.Source.SessionProxy.GetInvocations(nameof(IWorldSession.ForceDisconnect)).Count,
                actor.Quests.RequiresAuthoritativeReload);
        }
        else
            await Assert.ThrowsAsync<DbUpdateException>(Save);
        using (var failed = f.Context())
        {
            Assert.Equal((byte)QuestState.Accepted, failed.CharacterQuest.Single().State);
            Assert.Equal(1u, failed.CharacterQuestObjective.Single().Progress);
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_public_event_quest");
        }

        if (!useWorldSaveQueue)
        {
            await Save();
            using var committed = f.Context();
            Assert.Equal((byte)QuestState.Achieved, committed.CharacterQuest.Single().State);
            Assert.Equal(2u, committed.CharacterQuestObjective.Single().Progress);
        }
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
    }

    private static RuntimeEvent CreateEvent(IPlayerManager players, IBaseMap map, uint objectiveId, uint objectId)
    {
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
        return publicEvent;
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
