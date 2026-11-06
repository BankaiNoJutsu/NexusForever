using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Achievement;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network;
using NexusForever.Network.Internal;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Achievement;
using NexusForever.WorldServer.Network;
using GameGuild = NexusForever.Game.Guild.Guild;
using PacketAchievement = NexusForever.Network.World.Message.Model.Achievement.Achievement;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildCreationFinanceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("2026-01-02T03:04:05Z")]
    [InlineData("2026-07-02T03:04:05Z")]
    [InlineData("2026-03-29T01:30:00Z")]
    [InlineData("2026-10-25T01:30:00Z")]
    [InlineData("2026-01-02T03:04:05.8000000Z")]
    public async Task GuildAchievementPacketsRetainStoredCompletion(string timestamp)
        => await ProbeGuildPacketTimestamps(timestamp, _ => { });

    [Theory]
    [InlineData(null)]
    [InlineData("2026-01-02T03:04:05Z")]
    [InlineData("2026-07-02T03:04:05Z")]
    [InlineData("2026-03-29T01:30:00Z")]
    [InlineData("2026-10-25T01:30:00Z")]
    [InlineData("2026-01-02T03:04:05.8000000Z")]
    public async Task CharacterAchievementPacketsRetainStoredCompletion(string timestamp)
        => await ProbeCharacterPacketTimestamps(timestamp, _ => { });

    internal async Task ProbeGuildPacketTimestamps(string timestamp, Action<object> observe)
    {
        using var f = await CreateFeedbackFixture();
        ConfigureTimestampAchievements(f, timestamp);
        var guild = Assert.IsType<GameGuild>(f.Actor.Player.GuildManager.Guild);

        // Normal Guild broadcasts resolve online members through IPlayerManager.
        // The finance fixture intentionally omits that collaborator; supply it
        // for this packet observation without changing production or the fixture.
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args =>
            (ulong)args[0] == f.Actor.Player.CharacterId ? f.Actor.Player : null);
        typeof(GuildBase).GetField("playerManager", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(guild, players);
        Assert.NotNull(guild.GetMember(f.Actor.Player.CharacterId));

        EarnFeedback(f);
        var manager = Assert.IsType<GuildAchievementManager>(guild.AchievementManager);
        var live = TimestampAchievement<GuildAchievementModel>(manager, 600);
        DateTime? original = ParseTimestamp(timestamp);
        Assert.Equal(original.HasValue, live.DateCompleted.HasValue);
        if (original.HasValue) live.DateCompleted = original;
        Assert.DoesNotContain(f.Packets<ServerAchievementUpdate>().SelectMany(packet => packet.Achievements), row => row.AchievementId == 600);
        await CommitFeedbackParent(f);
        await f.Flush();
        using var sql = f.Source.Context();
        var stored = Assert.Single(sql.GuildAchievement.AsNoTracking());
        Assert.Equal(original.HasValue, stored.DateCompleted.HasValue);
        if (sql.Database.IsMySql() && stored.DateCompleted.HasValue)
            Assert.Equal(DateTimeKind.Unspecified, stored.DateCompleted.Value.Kind);
        ulong expected = StoredFileTime(stored.DateCompleted);
        var updatePacket = Assert.Single(f.Packets<ServerAchievementUpdate>(), packet => packet.Achievements.Any(row => row.AchievementId == 600));
        var update = Assert.Single(updatePacket.Achievements);

        var model = Assert.Single(f.Actor.Player.GetCharacterDatabase().GetGuilds());
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var reloaded = new GameGuild(realm, RecordingDispatchProxy<IInternalMessagePublisher>.Create(out _),
            RecordingDispatchProxy<ITextFilterManager>.Create(out _), globalAchievementManager: f.Achievements, gameTableManager: f.Tables);
        reloaded.Initialise(model);
        reloaded.AchievementManager.SendInitialPackets(f.Actor.Player);
        var initialPacket = f.Packets<ServerAchievementInit>().Last();
        var initial = Assert.Single(initialPacket.Achievements);
        var legacy = TimestampAchievement<GuildAchievementModel>(reloaded.AchievementManager, 600).Build();
        AssertTimestampRows(600, stored.ProgressState, stored.CreditedChecklistMask, expected, update, initial, legacy);
        AssertTimestampWire(updatePacket, 600, stored.ProgressState, stored.CreditedChecklistMask, expected);
        AssertTimestampWire(initialPacket, 600, stored.ProgressState, stored.CreditedChecklistMask, expected);
        AssertTimestampWire(new ServerAchievementInit { Achievements = [legacy] }, 600, stored.ProgressState, stored.CreditedChecklistMask, expected);
        observe(new { Owner = "Guild", Provider = sql.Database.ProviderName, LocalZone = TimeZoneInfo.Local.Id,
            Original = original, Stored = stored.DateCompleted, Kind = stored.DateCompleted?.Kind.ToString(), Expected = expected,
            Update = update.DateCompleted, Reload = initial.DateCompleted, Legacy = legacy.DateCompleted });
    }

    internal async Task ProbeCharacterPacketTimestamps(string timestamp, Action<object> observe)
    {
        using var f = await CreateFeedbackFixture();
        ConfigureTimestampAchievements(f, timestamp);
        var manager = Assert.IsType<CharacterAchievementManager>(f.Actor.Player.AchievementManager);
        manager.CheckAchievements(f.Actor.Player, AchievementType.AchievementComplete, 600);
        var live = TimestampAchievement<CharacterAchievementModel>(manager, 601);
        DateTime? original = ParseTimestamp(timestamp);
        Assert.Equal(original.HasValue, live.DateCompleted.HasValue);
        if (original.HasValue) live.DateCompleted = original;
        Assert.DoesNotContain(f.Packets<ServerAchievementUpdate>().SelectMany(packet => packet.Achievements), row => row.AchievementId == 601);
        f.Actor.StartSave();
        await f.Actor.Drain();
        using var sql = f.Source.Context();
        var stored = Assert.Single(sql.CharacterAchievement.AsNoTracking());
        Assert.Equal(original.HasValue, stored.DateCompleted.HasValue);
        if (sql.Database.IsMySql() && stored.DateCompleted.HasValue)
            Assert.Equal(DateTimeKind.Unspecified, stored.DateCompleted.Value.Kind);
        ulong expected = StoredFileTime(stored.DateCompleted);
        var updatePacket = Assert.Single(f.Packets<ServerAchievementUpdate>(), packet => packet.Achievements.Any(row => row.AchievementId == 601));
        var update = Assert.Single(updatePacket.Achievements);
        using var fresh = await FreshFeedbackActor(f);
        fresh.Player.AchievementManager.SendInitialPackets(fresh.Player);
        var messages = (RecordingDispatchProxy<IWorldSession>)(object)fresh.Player.Session;
        var initialPacket = messages.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<ServerAchievementInit>().Last();
        var initial = Assert.Single(initialPacket.Achievements);
        var legacy = TimestampAchievement<CharacterAchievementModel>(fresh.Player.AchievementManager, 601).Build();
        AssertTimestampRows(601, stored.ProgressState, stored.CreditedChecklistMask, expected, update, initial, legacy);
        AssertTimestampWire(updatePacket, 601, stored.ProgressState, stored.CreditedChecklistMask, expected);
        AssertTimestampWire(initialPacket, 601, stored.ProgressState, stored.CreditedChecklistMask, expected);
        AssertTimestampWire(new ServerAchievementInit { Achievements = [legacy] }, 601, stored.ProgressState, stored.CreditedChecklistMask, expected);
        observe(new { Owner = "Character", Provider = sql.Database.ProviderName, LocalZone = TimeZoneInfo.Local.Id,
            Original = original, Stored = stored.DateCompleted, Kind = stored.DateCompleted?.Kind.ToString(), Expected = expected,
            Update = update.DateCompleted, Reload = initial.DateCompleted, Legacy = legacy.DateCompleted });
    }

    private static void ConfigureTimestampAchievements(Fixture f, string timestamp)
        => ConfigureFeedback(f,
            new AchievementEntry { Id = 600, Flags = (uint)AchievementFlags.Guild,
                AchievementTypeId = (uint)AchievementType.KillCreatureEntry, ObjectId = 500, RequiredProgress = timestamp == null ? 2u : 1u },
            new AchievementEntry { Id = 601, AchievementTypeId = (uint)AchievementType.AchievementComplete,
                ObjectId = 600, RequiredProgress = timestamp == null ? 2u : 1u }, []);

    private static DateTime? ParseTimestamp(string value) => value == null ? null
        : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    private static ulong StoredFileTime(DateTime? value) => value.HasValue
        ? (ulong)DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).ToFileTimeUtc() : 0;
    private static IAchievement TimestampAchievement<T>(object manager, ushort id) where T : class, IAchievementModel, new()
        => (IAchievement)typeof(BaseAchievementManager<T>).GetMethod("GetAchievement", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(manager, [id])!;
    private static void AssertTimestampRows(ushort id, uint progress, uint credited, ulong completed, params PacketAchievement[] rows)
    {
        foreach (var row in rows)
        {
            Assert.Equal(id, row.AchievementId); Assert.Equal(progress, row.ProgressState);
            Assert.Equal(credited, row.CreditedChecklistMask); Assert.Equal(completed, row.DateCompleted);
        }
    }
    private static void AssertTimestampWire(IWritable packet, ushort id, uint progress, uint credited, ulong completed)
    {
        using var bytes = new MemoryStream();
        using (var writer = new GamePacketWriter(bytes)) { packet.Write(writer); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(bytes.ToArray()));
        if (packet is ServerAchievementUpdate) Assert.False(reader.ReadBit());
        Assert.Equal(1, reader.ReadInt());
        Assert.Equal(id, reader.ReadUShort(15));
        Assert.Equal(progress, reader.ReadUInt()); Assert.Equal(credited, reader.ReadUInt());
        Assert.Equal(completed, reader.ReadULong());
    }
}
