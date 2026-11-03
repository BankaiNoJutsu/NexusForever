using System.Collections;
using System.Reflection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Achievement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Achievement;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Achievement;

public sealed class CharacterAchievementPublicationTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public CharacterAchievementPublicationTests() { }
    internal CharacterAchievementPublicationTests(DbContextOptions<CharacterContext> options)=>this.options=options;
    private const BindingFlags PrivateStatic=BindingFlags.NonPublic|BindingFlags.Static;

    [Fact]
    public async Task ProgressWaitsForTheOriginalPlayerSaveAndWorldUpdate()
    {
        using var f=await Create();f.Progress(1);Assert.Empty(f.Updates);
        await f.Save();Assert.Empty(f.Updates);
        using(var sql=f.Source.Context())Assert.Equal(1u,sql.CharacterAchievement.Single().ProgressState);
        await f.Pump();Assert.Equal(1u,Assert.Single(Assert.Single(f.Updates).Achievements).ProgressState);
    }

    [Fact]
    public async Task RealmFirstAnnouncementWaitsForSqlAndTheWorldUpdate()
    {
        using var f=await Create(true);f.Manager.GrantAchievement(500);Assert.Empty(f.Announcements);Assert.Empty(f.Updates);
        await f.Save();Assert.Empty(f.Announcements);await f.Pump();
        Assert.Equal((ushort)500,Assert.Single(f.Announcements).AchievementId);Assert.Single(f.Updates);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RejectedSqlRetainsProgressWithoutPublishing(bool completed)
    {
        using var f=await Create(completed);using(var setup=f.Source.Context())await Reject(setup);
        if(completed)f.Manager.GrantAchievement(500);else f.Progress(1);
        await Assert.ThrowsAsync<DbUpdateException>(f.Save);await f.Pump();
        using(var failed=f.Source.Context()){Assert.Empty(failed.CharacterAchievement);await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_achievement_publication");}
        Assert.Empty(f.Updates);Assert.Empty(f.Announcements);
        await f.Save();await f.Pump();Assert.Single(f.Updates);Assert.Equal(completed?1:0,f.Announcements.Count());
    }

    [Fact]
    public async Task CallerRollbackCannotPublishAnUncommittedAchievement()
    {
        using var f=await Create();f.Progress(1);
        await using(var context=f.Source.Context())await using(var transaction=await context.Database.BeginTransactionAsync())
        {f.Actor.Player.Save(context);await context.SaveChangesAsync();await transaction.RollbackAsync();}
        await f.Pump();Assert.Empty(f.Updates);
        await f.Save();await f.Pump();Assert.Single(f.Updates);
    }

    [Fact]
    public async Task InitialPacketsDoNotExposeUncommittedProgress()
    {
        using var f=await Create();f.Progress(1);f.Manager.SendInitialPackets(f.Actor.Player);
        Assert.Empty(Assert.Single(f.Packets.OfType<ServerAchievementInit>()).Achievements);
        await f.Save();await f.Pump();f.Manager.SendInitialPackets(f.Actor.Player);
        Assert.Single(f.Packets.OfType<ServerAchievementInit>().Last().Achievements);
    }

    [Fact]
    public async Task ReplacedSessionsCannotReceiveAnOldCommittedPublication()
    {
        using var f=await Create();f.Progress(1);await f.Save();
        var replacement=RecordingDispatchProxy<NexusForever.WorldServer.Network.IWorldSession>.Create(out var proxy);
        proxy.SetProperty("Player",f.Actor.Player);f.Actor.Set(nameof(Player.Session),replacement);
        await f.Pump();Assert.Empty(f.Updates);Assert.Empty(proxy.GetInvocations("EnqueueMessageEncrypted"));
    }

    [Fact]
    public async Task UnchangedConfirmedSavesDoNotRepeatTheProgressUpdate()
    {
        using var f=await Create();f.Progress(1);await f.Save();await f.Pump();await f.Save();await f.Pump();Assert.Single(f.Updates);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LostCommitAcknowledgementsPublishOnlyAfterRecovery(bool completed)
    {
        using var f=await Create(completed);if(completed)f.Manager.GrantAchievement(500);else f.Progress(1);
        await Assert.ThrowsAsync<IOException>(()=>CharacterAtomicSave.Execute(()=>f.Source.Context(new LostAcknowledgement()),42,f.Actor.Player.Save));
        await f.Pump();Assert.Empty(f.Updates);Assert.Empty(f.Announcements);
        using(var sql=f.Source.Context())Assert.Single(sql.CharacterAchievement);
        await f.Save();await f.Pump();Assert.Single(f.Updates);Assert.Equal(completed?1:0,f.Announcements.Count());
    }

    [Fact]
    public async Task ConfirmedUpdatesKeepTheirFrozenOrderDespiteLaterLiveProgress()
    {
        using var f=await Create(requiredProgress:10);f.Progress(1);await f.Save();f.Progress(2);await f.Save();f.Progress(3);
        await f.Pump();Assert.Equal(new uint[]{1,2},f.Updates.SelectMany(row=>row.Achievements).Select(row=>row.ProgressState));
        f.Manager.SendInitialPackets(f.Actor.Player);Assert.Equal(2u,Assert.Single(f.Packets.OfType<ServerAchievementInit>().Last().Achievements).ProgressState);
        await f.Save();await f.Pump();Assert.Equal(new uint[]{1,2,3},f.Updates.SelectMany(row=>row.Achievements).Select(row=>row.ProgressState));
    }

    [Fact]
    public async Task FailedPrivatePublicationRetriesAFreshPacketWithoutUsingLaterLiveValues()
    {
        using var f=await Create(requiredProgress:10);bool failed=false;var attempts=new List<ServerAchievementUpdate>();
        f.Messages.SetMethodHandler("EnqueueMessageEncrypted",args=>
        {
            if(args[0] is ServerAchievementUpdate packet){attempts.Add(packet);if(!failed){failed=true;packet.Achievements[0].ProgressState=999;throw new IOException("owned update publication failure");}}
            return null;
        });
        f.Progress(1);await f.Save();f.Progress(2);await f.Pump();
        Assert.Equal(2,attempts.Count);Assert.NotSame(attempts[0],attempts[1]);Assert.Equal(1u,Assert.Single(attempts[1].Achievements).ProgressState);
    }

    [Fact]
    public async Task RealmFirstRecipientRetriesDoNotRepeatAcknowledgedRecipients()
    {
        using var f=await Create(true);var observer=RecordingDispatchProxy<IPlayer>.Create(out var player);
        player.SetProperty(nameof(IPlayer.Identity),new Identity{Id=43,RealmId=1});player.SetProperty(nameof(IPlayer.CharacterId),43ul);
        var connection=RecordingDispatchProxy<IWorldSession>.Create(out var messages);player.SetProperty(nameof(IPlayer.Session),connection);messages.SetProperty("Player",observer);
        f.Recipients.Add(observer);bool failed=false;var attempts=new List<ServerRealmFirstAchievement>();
        messages.SetMethodHandler("EnqueueMessageEncrypted",args=>
        {if(args[0] is ServerRealmFirstAchievement packet){attempts.Add(packet);if(!failed){failed=true;packet.Name="poisoned";throw new IOException("owned recipient failure");}}return null;});
        f.Manager.GrantAchievement(500);await f.Save();await f.Pump();
        Assert.Single(f.Updates);Assert.Single(f.Announcements);Assert.Equal(2,attempts.Count);Assert.NotSame(attempts[0],attempts[1]);Assert.NotEqual("poisoned",attempts[1].Name);
    }

    [Fact]
    public async Task ReplacedManagersCannotPublishTheirOriginalPendingUpdates()
    {
        using var f=await Create();f.Progress(1);await f.Save();
        f.Actor.Set(nameof(Player.AchievementManager),RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
        await f.Pump();Assert.Empty(f.Updates);
    }

    [Fact]
    public async Task SavepointRollbackCannotQualifyAnAchievementPublication()
    {
        using var f=await Create();f.Progress(1);
        await using(var context=f.Source.Context())await using(var transaction=await context.Database.BeginTransactionAsync())
        {
            await transaction.CreateSavepointAsync("before_achievement");f.Actor.Player.Save(context);await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("before_achievement");await transaction.CommitAsync();
        }
        await f.Pump();Assert.Empty(f.Updates);using(var sql=f.Source.Context())Assert.Empty(sql.CharacterAchievement);
        await f.Save();await f.Pump();Assert.Single(f.Updates);
    }

    [Fact]
    public async Task NormalSavePublicationUsesThePreAuthAchievementSnapshot()
    {
        using var f=await Create(requiredProgress:10);var currency=new CurrencyManager(f.Source.Source.Tables);
        currency.Initialise(f.Actor.Player,new CharacterModel{Id=42});f.Actor.Set(nameof(Player.CurrencyManager),currency);
        f.Progress(1);f.Actor.OnAuth=()=>f.Progress(2);await f.Actor.Player.SaveDirect();await f.Actor.Drain();
        using(var sql=f.Source.Context())Assert.Equal(1u,Assert.Single(sql.CharacterAchievement).ProgressState);
        Assert.Equal(1u,Assert.Single(Assert.Single(f.Updates).Achievements).ProgressState);
        f.Actor.OnAuth=null;await f.Actor.Player.SaveDirect();await f.Actor.Drain();
        Assert.Equal(new uint[]{1,2},f.Updates.SelectMany(row=>row.Achievements).Select(row=>row.ProgressState));
    }

    [Fact]
    public async Task ReentrantEventUpdatesCannotReplayTheCommittedHead()
    {
        using var f=await Create();bool entered=false;
        f.Messages.SetMethodHandler("EnqueueMessageEncrypted",args=>
        {if(args[0] is ServerAchievementUpdate&&!entered){entered=true;f.Actor.Events.Update(.01);}return null;});
        f.Progress(1);await f.Save();await f.Pump();Assert.True(entered);Assert.Single(f.Updates);Assert.Empty(f.Actor.Errors);
    }

    [Fact]
    public async Task SqlCompletionDoesNotWaitForAnEarlierOutwardSend()
    {
        using var f=await Create(requiredProgress:10);f.Progress(1);await f.Save();f.Progress(2);
        await using var context=f.Source.Context();await using var transaction=await context.Database.BeginTransactionAsync();
        using var confirmed=new ManualResetEventSlim();context.OnSaveCompleted(success=>{if(success)confirmed.Set();});
        f.Actor.Player.Save(context);await context.SaveChangesAsync();Task commit=null;bool observed=false,settled=false;
        f.Messages.SetMethodHandler("EnqueueMessageEncrypted",args=>
        {
            if(args[0] is ServerAchievementUpdate&&commit==null)
            {
                commit=Task.Run(async()=>await transaction.CommitAsync());
                observed=confirmed.Wait(TimeSpan.FromSeconds(10));settled=commit.Wait(TimeSpan.FromSeconds(2));
            }
            return null;
        });
        await f.Pump();Assert.NotNull(commit);await commit.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(observed,"The second SQL commit did not reach its terminal observer.");
        Assert.True(settled,"A completed SQL save waited for an unrelated outward publication callback.");
        using var sql=f.Source.Context();Assert.Equal(2u,sql.CharacterAchievement.Single().ProgressState);
    }

    [Fact]
    public async Task WithdrawnRecipientsCannotReceiveARetainedRealmFirstRetry()
    {
        using var f=await Create(true);var observer=RecordingDispatchProxy<IPlayer>.Create(out var player);
        player.SetProperty(nameof(IPlayer.Identity),new Identity{Id=43,RealmId=1});player.SetProperty(nameof(IPlayer.CharacterId),43ul);
        var connection=RecordingDispatchProxy<IWorldSession>.Create(out var messages);player.SetProperty(nameof(IPlayer.Session),connection);messages.SetProperty("Player",observer);
        f.Recipients.Add(observer);int attempts=0;
        messages.SetMethodHandler("EnqueueMessageEncrypted",args=>
        {if(args[0] is ServerRealmFirstAchievement&&++attempts==1)throw new IOException("owned obsolete recipient failure");return null;});
        f.Manager.GrantAchievement(500);await f.Save();f.Actor.Events.Update(.01);Assert.Equal(1,attempts);
        f.Recipients.Remove(observer);await f.Pump();Assert.Equal(1,attempts);Assert.Single(f.Announcements);
    }

    [Fact]
    public async Task WithdrawnSourceActorsCannotPublishCommittedUpdates()
    {
        using var f=await Create();f.Progress(1);await f.Save();f.Recipients.Clear();await f.Pump();Assert.Empty(f.Updates);
    }

    private async Task<Frame> Create(bool realmFirst=false,uint requiredProgress=2)
    {
        var source=new Fixture(options);source.Seed();
        typeof(PlayerExperiencePersistenceTests).GetMethod("Seed",PrivateStatic)!.Invoke(null,[source,true]);
        var session=RecordingDispatchProxy<IWorldSession>.Create(out var messages);var events=new EventQueue();messages.SetProperty("Events",events);
        ((RecordingDispatchProxy<IAccount>)(object)source.Source.Player.Account).SetProperty(nameof(IAccount.Session),session);
        var actor=new Actor(source,actorSession:session,actorEvents:events);messages.SetProperty("Player",actor.Player);await actor.Load();
        typeof(PlayerExperiencePersistenceTests).GetMethod("Configure",PrivateStatic)!.Invoke(null,[source,actor,true]);
        Assert.Same(actor.Player,((IPlayerSession)session).Player);Assert.Same(session,actor.Player.Session);Assert.Same(events,actor.Events);
        var registry=new Registry(new AchievementInfo(new AchievementEntry{Id=500,AchievementTypeId=(uint)(realmFirst?AchievementType.RealmFirst:AchievementType.PathLevel),RequiredProgress=requiredProgress}));
        var recipients=new List<IPlayer>{actor.Player};
        var players=RecordingDispatchProxy<IPlayerManager>.Create(out var enumeration);
        enumeration.SetMethodHandler(nameof(IEnumerable.GetEnumerator),_=>((IEnumerable<IPlayer>)recipients.ToArray()).GetEnumerator());
        enumeration.SetMethodHandler(nameof(IPlayerManager.GetPlayer),args=>args[0] is Identity id
            ?recipients.SingleOrDefault(player=>player.Identity?.Id==id.Id&&player.Identity.RealmId==id.RealmId):null);
        using var context=source.Context();var loaded=context.Character.AsNoTracking().Single();
        var manager=new CharacterAchievementManager(actor.Player,loaded,globalAchievementManager:registry,playerManager:players);
        actor.Set(nameof(Player.AchievementManager),manager);return new(source,actor,manager,messages,recipients);
    }
    private static Task Reject(CharacterContext context)=>context.Database.ExecuteSqlRawAsync(context.Database.IsSqlite()
        ? "CREATE TRIGGER reject_achievement_publication BEFORE INSERT ON character_achievement BEGIN SELECT RAISE(ABORT, 'owned achievement SQL failure'); END"
        : "CREATE TRIGGER reject_achievement_publication BEFORE INSERT ON character_achievement FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned achievement SQL failure'");
    private sealed record Frame(Fixture Source,Actor Actor,CharacterAchievementManager Manager,RecordingDispatchProxy<IWorldSession> Messages,List<IPlayer> Recipients):IDisposable
    {
        internal IEnumerable<object> Packets=>Messages.GetInvocations("EnqueueMessageEncrypted").Select(row=>row.Arguments[0]);
        internal IEnumerable<ServerAchievementUpdate> Updates=>Packets.OfType<ServerAchievementUpdate>();
        internal IEnumerable<ServerRealmFirstAchievement> Announcements=>Packets.OfType<ServerRealmFirstAchievement>();
        internal void Progress(uint value)=>Manager.SetAchievementProgress(Actor.Player,AchievementType.PathLevel,0,0,value);
        internal Task Save()=>CharacterAtomicSave.Execute(()=>Source.Context(),42,Actor.Player.Save);
        internal Task Pump(){Actor.Events.Update(.01);Actor.Events.Update(.01);return Task.CompletedTask;}
        public void Dispose(){Actor.Dispose();Source.Dispose();}
    }
    private sealed class LostAcknowledgement:DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction,TransactionEndEventData data,CancellationToken cancellationToken=default)
            =>Task.FromException(new IOException("owned acknowledgement loss"));
    }
    private sealed class Registry(IAchievementInfo info):IGlobalAchievementManager
    {
        private readonly GlobalAchievementManager core=new();
        public void Initialise(){}public IAchievementInfo GetAchievement(ushort id)=>id==info.Id?info:null;
        public IEnumerable<IAchievementInfo> GetCharacterAchievements(AchievementType type)=>type==(AchievementType)info.Entry.AchievementTypeId?[info]:[];
        public IEnumerable<IAchievementInfo> GetGuildAchievements(AchievementType type)=>[];
        public bool TryClaimRealmFirstAchievement(IAchievementInfo value,bool guild)=>core.TryClaimRealmFirstAchievement(value,guild);
    }
}
