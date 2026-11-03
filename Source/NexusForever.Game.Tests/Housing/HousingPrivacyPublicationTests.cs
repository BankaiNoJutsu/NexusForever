using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network;
using NexusForever.Network.Internal;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;

namespace NexusForever.Game.Tests.Housing;

public sealed class HousingPrivacyPublicationTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public HousingPrivacyPublicationTests() { }
    internal HousingPrivacyPublicationTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrivateReplyCannotPrecedeItsCommittedPrivacyFlag(bool target)
    {
        using var f = await Create(!target); bool premature = false; int replies = 0;
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerHousingCommunityPrivacyLevelUpdate)
            { replies++; using var probe = f.Context(); premature |= SqlPrivate(probe) != target; }
            return null;
        });
        f.Handler.HandleMessage(f.Session, Request(target)); await Flush(f);
        Assert.False(premature, "Privacy reply preceded its committed guild flag."); Assert.Equal(1, replies);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LocalGuildFlagBroadcastCannotPrecedeItsCommittedPrivacyFlag(bool target)
    {
        using var f = await Create(!target); bool premature = false; int broadcasts = 0;
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerGuildFlagsUpdate)
            { broadcasts++; using var probe = f.Context(); premature |= SqlPrivate(probe) != target; }
            return null;
        });
        f.Handler.HandleMessage(f.Session, Request(target)); await Flush(f);
        Assert.False(premature, "Local guild flags preceded the committed privacy flag."); Assert.Equal(1, broadcasts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VisitIndexRetainsItsPriorPrivacyUntilTheFlagCommits(bool target)
    {
        using var f = await Create(!target); f.Handler.HandleMessage(f.Session, Request(target));
        Assert.Equal(target, f.Residences.GetRandomVisitableCommunities().Any());
        await Flush(f); Assert.Equal(!target, f.Residences.GetRandomVisitableCommunities().Any());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LivePrivacyRetainsItsPriorValueUntilTheFlagCommits(bool target)
    {
        using var f = await Create(!target); f.Handler.HandleMessage(f.Session, Request(target));
        Assert.Equal(!target, f.Community.HasFlags(GuildFlag.CommunityPrivate));
        await Flush(f); Assert.Equal(target, f.Community.HasFlags(GuildFlag.CommunityPrivate));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedPrivacyWriteCannotPublishItsIndexOrMessages(bool target)
    {
        using var f = await Create(!target);
        using (var setup = f.Context())
            await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite()
                ? "CREATE TRIGGER reject_privacy BEFORE UPDATE ON guild BEGIN SELECT RAISE(ABORT, 'owned privacy write failure'); END"
                : "CREATE TRIGGER reject_privacy BEFORE UPDATE ON guild FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned privacy write failure'");
        f.Handler.HandleMessage(f.Session, Request(target)); await Flush(f);
        using (var failed = f.Context())
        {
            Assert.Equal(!target, SqlPrivate(failed)); Assert.Equal(target, f.Residences.GetRandomVisitableCommunities().Any());
            Assert.Empty(PrivacyMessages(f)); await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_privacy");
        }
        await Flush(f); using var verify = f.Context(); Assert.Equal(target, SqlPrivate(verify));
        Assert.Equal(!target, f.Residences.GetRandomVisitableCommunities().Any()); Assert.Equal(2, PrivacyMessages(f).Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicPrivacyFlowPersistsItsFlagAndPreservesWireValues(bool target)
    {
        using var f = await Create(!target); f.Handler.HandleMessage(f.Session, Request(target)); await Flush(f);
        using var verify = f.Context(); Assert.Equal(target, SqlPrivate(verify)); Assert.Equal(target, f.Community.HasFlags(GuildFlag.CommunityPrivate));
        Assert.Equal(!target, f.Residences.GetRandomVisitableCommunities().Any());
        var reply = Assert.Single(PrivacyMessages(f).OfType<ServerHousingCommunityPrivacyLevelUpdate>());
        Assert.Equal((ushort)1, reply.TargetResidence.RealmId); Assert.Equal(100ul, reply.TargetResidence.ResidenceId);
        Assert.Equal(target ? ServerHousingCommunityPrivacyLevelUpdate.PrivateFlag : 0u, reply.Flags);
        Assert.Equal(target ? ServerHousingCommunityPrivacyLevelUpdate.PrivateLuaValue : ServerHousingCommunityPrivacyLevelUpdate.PublicLuaValue, reply.PrivacyLevel);
        Assert.Equal(ServerHousingCommunityPrivacyLevelUpdate.CommunityEntryType, reply.EntryType);
        Assert.Single(PrivacyMessages(f).OfType<ServerGuildFlagsUpdate>());
    }

    [Theory]
    [InlineData("permission")]
    [InlineData("realm")]
    public async Task InvalidPrivacyRequestCannotChangeTheFlagIndexOrMessages(string invalid)
    {
        using var f = await Create(true);
        if (invalid == "permission") f.Community.GetMember(42).Rank.Permissions = GuildRankPermission.None;
        Assert.Throws<InvalidPacketValueException>(() => f.Handler.HandleMessage(f.Session, Request(false, invalid == "realm" ? (ushort)2 : (ushort)1)));
        using var verify = f.Context(); Assert.True(SqlPrivate(verify)); Assert.True(f.Community.HasFlags(GuildFlag.CommunityPrivate));
        Assert.Empty(f.Residences.GetRandomVisitableCommunities()); Assert.Empty(PrivacyMessages(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SamePrivacyStillAcknowledgesItsConfirmedState(bool target)
    {
        using var f = await Create(target); f.Handler.HandleMessage(f.Session, Request(target)); await Flush(f); await Flush(f);
        using var verify = f.Context(); Assert.Equal(target, SqlPrivate(verify)); Assert.False(f.Community.NeedsSave);
        Assert.Equal(!target, f.Residences.GetRandomVisitableCommunities().Any());
        Assert.Single(PrivacyMessages(f).OfType<ServerHousingCommunityPrivacyLevelUpdate>());
        Assert.Single(PrivacyMessages(f).OfType<ServerGuildFlagsUpdate>());
    }

    [Fact]
    public async Task RapidOppositeRequestsCommitAndPublishInTheirAcceptedOrder()
    {
        using var f = await Create(true); var replies = new List<bool>();
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerHousingCommunityPrivacyLevelUpdate reply)
            {
                bool privacy = reply.Flags == ServerHousingCommunityPrivacyLevelUpdate.PrivateFlag;
                using var verify = f.Context(); Assert.Equal(privacy, SqlPrivate(verify));
                Assert.Equal(!privacy, f.Residences.GetRandomVisitableCommunities().Any()); replies.Add(privacy);
            }
            return null;
        });
        f.Handler.HandleMessage(f.Session, Request(false)); f.Handler.HandleMessage(f.Session, Request(true)); await Flush(f); await Flush(f);
        Assert.Equal(new[] { false, true }, replies); using var final = f.Context(); Assert.True(SqlPrivate(final));
    }

    [Theory]
    [InlineData("guild")]
    [InlineData("reply")]
    public async Task FailedPublicationRetriesItsOwnStageWithFreshMetadata(string stage)
    {
        using var f = await Create(true); int attempts = 0;
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            bool selected = stage == "guild" && args[0] is ServerGuildFlagsUpdate || stage == "reply" && args[0] is ServerHousingCommunityPrivacyLevelUpdate;
            if (!selected) return null;
            if (++attempts == 1)
            {
                if (args[0] is ServerGuildFlagsUpdate flags) flags.Flags = GuildFlag.CommunityPrivate;
                if (args[0] is ServerHousingCommunityPrivacyLevelUpdate reply) { reply.Flags = 99; reply.TargetResidence.ResidenceId = 999; }
                throw new IOException("owned privacy enqueue failure");
            }
            if (args[0] is ServerGuildFlagsUpdate fresh) Assert.Equal(GuildFlag.None, fresh.Flags);
            if (args[0] is ServerHousingCommunityPrivacyLevelUpdate retry) { Assert.Equal(0u, retry.Flags); Assert.Equal(100ul, retry.TargetResidence.ResidenceId); }
            return null;
        });
        Assert.Null(Record.Exception(() => f.Handler.HandleMessage(f.Session, Request(false))));
        await Flush(f); await Flush(f); Assert.Equal(2, attempts); Assert.False(f.Community.NeedsSave);
        if (stage == "reply") Assert.Single(PrivacyMessages(f).OfType<ServerGuildFlagsUpdate>());
        using var verify = f.Context(); Assert.False(SqlPrivate(verify)); Assert.Single(f.Residences.GetRandomVisitableCommunities());
    }

    [Fact]
    public async Task ReentrantWorldUpdateKeepsTheFirstReceiptOwnedWhileAcceptingTheNextRequest()
    {
        using var f = await Create(true); int replies = 0;
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerHousingCommunityPrivacyLevelUpdate && ++replies == 1)
            {
                f.Guilds.Update(0);
                Assert.Throws<InvalidOperationException>(() => f.Community.Flags ^= GuildFlag.CommunityPrivate);
                f.Handler.HandleMessage(f.Session, Request(true));
            }
            return null;
        });
        f.Handler.HandleMessage(f.Session, Request(false)); await Flush(f); await Flush(f);
        Assert.Equal(2, replies); using var verify = f.Context(); Assert.True(SqlPrivate(verify)); Assert.Empty(f.Residences.GetRandomVisitableCommunities());
    }

    [Fact]
    public async Task ReplacedSessionPlayerCannotReceiveTheAlreadyCommittedChange()
    {
        using var f = await Create(true); f.Handler.HandleMessage(f.Session, Request(false));
        f.Guilds.Update(61); await WaitForSave(f); f.Messages.SetProperty(nameof(IWorldSession.Player), null);
        await Flush(f); using var verify = f.Context(); Assert.False(SqlPrivate(verify));
        Assert.False(f.Community.HasFlags(GuildFlag.CommunityPrivate)); Assert.Single(f.Residences.GetRandomVisitableCommunities());
        Assert.Empty(PrivacyMessages(f));
    }

    [Fact]
    public async Task CommittedChangeCannotRepublishItsCommunityAfterDisband()
    {
        using var f = await Create(true); f.Handler.HandleMessage(f.Session, Request(false));
        f.Guilds.Update(61); await WaitForSave(f); f.Community.DisbandGuild(); await Flush(f);
        using var verify = f.Context(); Assert.NotNull(verify.Guild.Single().DeleteTime);
        Assert.Empty(f.Residences.GetRandomVisitableCommunities()); Assert.Empty(PrivacyMessages(f));
    }

    [Fact]
    public async Task LaterUnrelatedFlagsRemainDirtyAfterApplyingTheConfirmedPrivacyBit()
    {
        using var f = await Create(true); f.Handler.HandleMessage(f.Session, Request(false));
        f.Guilds.Update(61); await WaitForSave(f); f.Community.SetFlag(GuildFlag.Recruiting);
        f.Guilds.Update(0); Assert.False(f.Community.HasFlags(GuildFlag.CommunityPrivate)); Assert.True(f.Community.HasFlags(GuildFlag.Recruiting));
        Assert.True(f.Community.NeedsSave); await Flush(f);
        using var verify = f.Context(); Assert.Equal((uint)GuildFlag.Recruiting, verify.Guild.Single().Flags); Assert.False(f.Community.NeedsSave);
    }

    [Fact]
    public async Task RegisteredDirectPrivacyProducerUsesTheSameConfirmedPublicationPath()
    {
        using var f = await Create(true); f.Community.SetCommunityPrivate(false);
        Assert.True(f.Community.HasFlags(GuildFlag.CommunityPrivate)); Assert.Empty(f.Residences.GetRandomVisitableCommunities());
        await Flush(f); Assert.Single(f.Residences.GetRandomVisitableCommunities());
        Assert.Single(PrivacyMessages(f).OfType<ServerGuildFlagsUpdate>()); Assert.Empty(PrivacyMessages(f).OfType<ServerHousingCommunityPrivacyLevelUpdate>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplacedCommunityRootCannotReceiveTheOriginalPrivateReceiptOrVisitEntry(bool target)
    {
        using var f = await Create(!target); f.Handler.HandleMessage(f.Session, Request(target));
        f.Guilds.Update(61); await WaitForSave(f); f.Community.Residence = null; await Flush(f);
        using var verify = f.Context(); Assert.Equal(target, SqlPrivate(verify)); Assert.Equal(target, f.Community.HasFlags(GuildFlag.CommunityPrivate));
        Assert.Empty(f.Residences.GetRandomVisitableCommunities()); Assert.Empty(PrivacyMessages(f));
    }

    private static bool SqlPrivate(CharacterContext context) => (context.Guild.Single(row => row.Id == 77).Flags & (uint)GuildFlag.CommunityPrivate) != 0;
    private static object[] PrivacyMessages(Fixture f) => f.Messages.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
        .Select(row => row.Arguments[0]).Where(row => row is ServerGuildFlagsUpdate or ServerHousingCommunityPrivacyLevelUpdate).ToArray();
    private static ClientHousingCommunityPrivacyLevel Request(bool isPrivate, ushort realm = 1)
    {
        using var bytes = new MemoryStream();
        using (var writer = new GamePacketWriter(bytes))
        { new TargetResidence { RealmId = realm, ResidenceId = 100 }.Write(writer); writer.Write(isPrivate); writer.FlushBits(); }
        using var input = new MemoryStream(bytes.ToArray()); using var reader = new GamePacketReader(input);
        var request = new ClientHousingCommunityPrivacyLevel(); request.Read(reader); return request;
    }
    private static async Task Flush(Fixture f)
    {
        for (int tick = 0; tick < 3; tick++)
        {
            f.Guilds.Update(tick == 0 ? 61 : 0);
            await WaitForSave(f);
        }
    }
    private static async Task WaitForSave(Fixture f)
    {
        var type = typeof(GlobalGuildManager);
        object gate = type.GetField("cacheLock", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Guilds);
        var pending = (HashSet<ulong>)type.GetField("guildSavesInFlight", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Guilds);
        bool idle = false;
        for (int attempt = 0; attempt < 500; attempt++)
        {
            f.Events.Update(.01);
            lock (gate) idle = pending.Count == 0;
            if (idle) break; await Task.Delay(10);
        }
        Assert.True(idle, "Owned privacy save did not settle.");
    }
    private async Task<Fixture> Create(bool initiallyPrivate)
    {
        string file = Path.Combine(Path.GetTempPath(), $"nf-housing-privacy-{Guid.NewGuid():N}.sqlite");
        var provider = options ?? new DbContextOptionsBuilder<CharacterContext>().UseSqlite($"Data Source={file};Pooling=False;Default Timeout=10").Options;
        var database = new CharacterDatabase();
        using (var seed = new CharacterContext(provider))
        {
            string configuredConnection = seed.Database.GetDbConnection().ConnectionString;
            await seed.Database.EnsureCreatedAsync(); seed.Character.Add(new() { Id = 42, Name = "Leader" });
            seed.Guild.Add(new() { Id = 77, Type = (byte)GuildType.Community, LeaderId = 42, Name = "Community",
                Flags = initiallyPrivate ? (uint)GuildFlag.CommunityPrivate : 0,
                GuildRank = [new GuildRankModel { Id = 77, Index = 0, Name = "Leader", Permission = (uint)(GuildRankPermission.Leader | GuildRankPermission.ChangeCommunityRemodelOptions) }],
                GuildMember = [new GuildMemberModel { Id = 77, CharacterId = 42, Rank = 0 }] });
            seed.Residence.Add(new() { Id = 100, GuildOwnerId = 77, Name = "Community", PropertyInfoId = (byte)PropertyInfoId.Community });
            await seed.SaveChangesAsync();
            database.Initialise(new DatabaseConnectionString { Provider = seed.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                ConnectionString = configuredConnection });
        }
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db); db.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var rp); rp.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var broker); broker.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var text); text.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var cp);
        ICharacter character = RecordingDispatchProxy<ICharacter>.Create(out var characterProxy); characterProxy.SetProperty(nameof(ICharacter.Name), "Leader");
        cp.SetMethodReturn(nameof(ICharacterManager.GetCharacter), character);
        var events = new EventQueue(); IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var messages); messages.SetProperty(nameof(IWorldSession.Events), events);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var actor); actor.SetProperty(nameof(IPlayer.CharacterId), 42ul); actor.SetProperty(nameof(IPlayer.Name), "Leader");
        actor.SetProperty(nameof(IPlayer.Session), session); actor.SetProperty(nameof(IPlayer.Map), RecordingDispatchProxy<IResidenceMapInstance>.Create(out _));
        messages.SetProperty(nameof(IWorldSession.Player), player);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var online); online.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);
        GlobalGuildManager guilds = null;
        var forward = RecordingDispatchProxy<IGlobalGuildManager>.Create(out var gp); gp.SetMethodHandler(nameof(IGlobalGuildManager.GetGuild), args => guilds.GetGuild((ulong)args[0]));
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        var residences = new GlobalResidenceManager(characterManager: characters, globalGuildManager: forward, realmContext: realm, databaseManager: databases, gameTableManager: tables);
        var factory = RecordingDispatchProxy<IGuildFactory>.Create(out var create);
        create.SetMethodHandler(nameof(IGuildFactory.CreateGuild), args =>
        {
            var community = new Community(realm, publisher, residences, filter, RecordingDispatchProxy<IMapLockManager>.Create(out _), characters, guilds, players);
            community.Initialise((GuildModel)args[0]); return community;
        });
        guilds = new GlobalGuildManager(factory, databases); guilds.Initialise(); residences.Initialise();
        var community = Assert.IsType<Community>(guilds.GetGuild(77));
        var membership = RecordingDispatchProxy<IGuildManager>.Create(out var mp); mp.SetMethodReturn(nameof(IGuildManager.GetGuild), community); actor.SetProperty(nameof(IPlayer.GuildManager), membership);
        community.OnPlayerLogin(player); messages.Invocations.Clear();
        return new(provider, file, guilds, residences, community, session, messages, events, new(residences, realm));
    }
    private sealed record Fixture(DbContextOptions<CharacterContext> Options, string File, GlobalGuildManager Guilds, GlobalResidenceManager Residences,
        Community Community, IWorldSession Session, RecordingDispatchProxy<IWorldSession> Messages, EventQueue Events, ClientHousingCommunityPrivacyLevelHandler Handler) : IDisposable
    {
        internal CharacterContext Context() => new(Options);
        public void Dispose() { if (System.IO.File.Exists(File)) System.IO.File.Delete(File); }
    }
}
