using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Achievement;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network;
using NexusForever.Network.Internal;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Achievement;
using GameGuild = NexusForever.Game.Guild.Guild;
using PacketRow = NexusForever.Network.World.Message.Model.Achievement.Achievement;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildCreationFinanceTests
{
    public static TheoryData<string> CompletionDates => new()
    {
        null,
        "2026-01-02T03:04:05Z",
        "2026-07-02T03:04:05Z",
        "2026-03-29T00:59:59Z",
        "2026-03-29T01:00:00Z",
        "2026-10-25T00:59:59Z",
        "2026-10-25T01:00:00Z",
        "2026-01-02T03:04:05.8001234Z"
    };

    [Theory]
    [MemberData(nameof(CompletionDates))]
    public async Task GuildProviderLoadedPacketsRetainStoredCompletion(string timestamp)
        => await ProbeGuildPacketTimestamp(timestamp, _ => { });

    internal async Task ProbeGuildPacketTimestamp(string timestamp, Action<object> observe)
    {
        IPlayer recipient = null;
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args =>
            args[0] is ulong id && id == recipient?.CharacterId ? recipient : null);
        using var f = await CreateFeedbackFixture(players: players);
        recipient = f.Actor.Player;
        var original = Assert.IsType<GameGuild>(recipient.GuildManager.Guild);
        var row = Row<GuildAchievementModel>(original.AchievementManager, 600, create: true);
        row.DateCompleted = ParseCompletion(timestamp);
        row.ProgressCount = timestamp == null ? 0x10203u : 1u;
        row.CreditedChecklistMask = 0x1020408;
        Assert.DoesNotContain(f.Packets<ServerAchievementUpdate>(), p => p.Achievements.Any(a => a.AchievementId == 600));
        await CommitFeedbackParent(f);
        await DrainGuildPackets(original);
        using var sql = f.Source.Context();
        var stored = Assert.Single(sql.GuildAchievement.AsNoTracking());
        AssertStoredKind(sql.Database.IsMySql(), stored.DateCompleted);
        CheckPacket(Assert.Single(f.Packets<ServerAchievementUpdate>(), p => p.Achievements.Any(a => a.AchievementId == 600)), stored);

        GameGuild LoadGuild()
        {
            var model = Assert.Single(f.Actor.Player.GetCharacterDatabase().GetGuilds());
            var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
            realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
            var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var people);
            var character = RecordingDispatchProxy<ICharacter>.Create(out var person);
            person.SetProperty(nameof(ICharacter.Name), "Leader");
            people.SetMethodReturn(nameof(ICharacterManager.GetCharacter), character);
            var guild = new GameGuild(realm, RecordingDispatchProxy<IInternalMessagePublisher>.Create(out _),
                RecordingDispatchProxy<ITextFilterManager>.Create(out _), globalAchievementManager: f.Achievements,
                gameTableManager: f.Tables, playerManager: players, characterManager: characters);
            guild.Initialise(model);
            return guild;
        }
        var loaded = LoadGuild();
        loaded.AchievementManager.SendInitialPackets(recipient);
        CheckPacket(f.Packets<ServerAchievementInit>().Last(), stored);
        var loadedRow = Row<GuildAchievementModel>(loaded.AchievementManager, 600);
        CheckLegacy(loadedRow.Build(), stored);

        // A committed update must also preserve the provider-loaded date, with a
        // real online-member registration and recipient lookup through the Guild.
        loaded.OnPlayerLogin(recipient);
        loadedRow.ProgressCount++;
        loadedRow.CreditedChecklistMask ^= 0x40000000;
        int packetsBefore = f.Packets<ServerAchievementUpdate>().Count();
        using (var transaction = f.Source.Context())
        {
            loaded.Save(transaction);
            await DrainGuildPackets(loaded);
            Assert.Equal(packetsBefore, f.Packets<ServerAchievementUpdate>().Count());
            await transaction.SaveChangesAsync();
        }
        await DrainGuildPackets(loaded);
        Assert.Equal(packetsBefore + 1, f.Packets<ServerAchievementUpdate>().Count());
        sql.ChangeTracker.Clear();
        stored = Assert.Single(sql.GuildAchievement.AsNoTracking());
        CheckPacket(f.Packets<ServerAchievementUpdate>().Last(), stored);
        Assert.Equal(ParseCompletion(timestamp).HasValue, stored.DateCompleted.HasValue);
        var fresh = LoadGuild();
        fresh.AchievementManager.SendInitialPackets(recipient);
        CheckPacket(f.Packets<ServerAchievementInit>().Last(), stored);
        CheckLegacy(Row<GuildAchievementModel>(fresh.AchievementManager, 600).Build(), stored);
        observe(new { Provider = sql.Database.ProviderName, LocalZone = TimeZoneInfo.Local.Id, Timestamp = timestamp,
            Stored = stored.DateCompleted, Kind = stored.DateCompleted?.Kind.ToString(), stored.ProgressState,
            stored.CreditedChecklistMask, Wire = ExpectedFileTime(stored.DateCompleted), ProviderLoadedUpdate = true });
    }

    [Theory]
    [MemberData(nameof(CompletionDates))]
    public async Task CharacterCommittedPacketsRetainStoredCompletion(string timestamp)
        => await ProbeCharacterPacketTimestamp(timestamp, _ => { });

    internal async Task ProbeCharacterPacketTimestamp(string timestamp, Action<object> observe)
    {
        using var f = await CreateFeedbackFixture();
        var manager = Assert.IsType<CharacterAchievementManager>(f.Actor.Player.AchievementManager);
        var row = Row<CharacterAchievementModel>(manager, 601, create: true);
        row.DateCompleted = ParseCompletion(timestamp);
        row.ProgressCount = timestamp == null ? 1u : 2u;
        row.CreditedChecklistMask = 0x4020108;
        Assert.DoesNotContain(f.Packets<ServerAchievementUpdate>(), p => p.Achievements.Any(a => a.AchievementId == 601));
        f.Actor.StartSave();
        await f.Actor.Drain();
        using var sql = f.Source.Context();
        var stored = Assert.Single(sql.CharacterAchievement.AsNoTracking());
        AssertStoredKind(sql.Database.IsMySql(), stored.DateCompleted);
        CheckPacket(Assert.Single(f.Packets<ServerAchievementUpdate>(), p => p.Achievements.Any(a => a.AchievementId == 601)), stored);
        manager.SendInitialPackets(f.Actor.Player);
        CheckPacket(f.Packets<ServerAchievementInit>().Last(p => p.Achievements.Any(a => a.AchievementId == 601)), stored);
        using var fresh = await FreshFeedbackActor(f);
        var connection = (RecordingDispatchProxy<NexusForever.WorldServer.Network.IWorldSession>)(object)fresh.Player.Session;
        fresh.Player.AchievementManager.SendInitialPackets(fresh.Player);
        var initial = connection.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<ServerAchievementInit>().Last();
        CheckPacket(initial, stored);
        CheckLegacy(Row<CharacterAchievementModel>(fresh.Player.AchievementManager, 601).Build(), stored);
        observe(new { Provider = sql.Database.ProviderName, LocalZone = TimeZoneInfo.Local.Id, Timestamp = timestamp,
            Stored = stored.DateCompleted, Kind = stored.DateCompleted?.Kind.ToString(), stored.ProgressState,
            stored.CreditedChecklistMask, Wire = ExpectedFileTime(stored.DateCompleted) });
    }

    private static IAchievement Row<T>(object manager, ushort id, bool create = false) where T : class, IAchievementModel, new()
    {
        if (create) ((BaseAchievementManager<T>)manager).GrantAchievement(id);
        var get = typeof(BaseAchievementManager<T>).GetMethod("GetAchievement", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return Assert.IsAssignableFrom<IAchievement>(get.Invoke(manager, [id]));
    }
    private static Task DrainGuildPackets(GameGuild guild) => (Task)typeof(GameGuild).BaseType!
        .GetMethod("PublishMutationsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(guild, null)!;
    private static DateTime? ParseCompletion(string value) => value == null ? null
        : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    private static ulong ExpectedFileTime(DateTime? stored) => stored.HasValue
        ? (ulong)DateTime.SpecifyKind(stored.Value, DateTimeKind.Utc).ToFileTimeUtc() : 0;
    private static void AssertStoredKind(bool mysql, DateTime? date)
    {
        if (mysql && date.HasValue) Assert.Equal(DateTimeKind.Unspecified, date.Value.Kind);
    }
    private static void CheckLegacy(PacketRow row, IAchievementModel stored)
        => CheckPacket(new ServerAchievementInit { Achievements = [row] }, stored);
    private static void CheckPacket(IWritable packet, IAchievementModel stored)
    {
        using var bytes = new MemoryStream();
        using var writer = new GamePacketWriter(bytes);
        packet.Write(writer);
        writer.FlushBits();
        using var reader = new GamePacketReader(new MemoryStream(bytes.ToArray()));
        if (packet is ServerAchievementUpdate) Assert.False(reader.ReadBit());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(stored.AchievementId, reader.ReadUShort(15));
        Assert.Equal(stored.ProgressState, reader.ReadUInt());
        Assert.Equal(stored.CreditedChecklistMask, reader.ReadUInt());
        Assert.Equal(ExpectedFileTime(stored.DateCompleted), reader.ReadULong());
        Assert.Equal(0u, reader.BytesRemaining);
    }
}
