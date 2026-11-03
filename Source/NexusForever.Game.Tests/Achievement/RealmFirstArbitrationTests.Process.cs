using System.Collections;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Achievement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Achievement;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Achievement;

public sealed partial class RealmFirstArbitrationTests
{
    // Called by the owned-provider console in separate processes. The parent
    // creates the schema/characters; workers must never reseed shared data.
    internal async Task<object> RunSharedCharacter(ulong id, string exchange, bool loseAcknowledgement, bool barrier)
    {
        using var source = new Fixture(options, seedCharacter: false);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var messages);
        var events = new EventQueue(); messages.SetProperty("Events", events);
        using var actor = new Actor(source, actorModel: new CharacterModel { Id = id }, actorSession: session, actorEvents: events,
            inventoryFactory: (player, _) => BankDeferredCurrencyRecoveryTests.Inventory(player,
                (IItemManager)(object)source.Source.ManagerProxy, source.Source.Tables, null));
        messages.SetProperty("Player", actor.Player); actor.Set(nameof(Player.Name), "Owner" + id); await actor.Load();
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodHandler(nameof(IEnumerable.GetEnumerator), _ => ((IEnumerable<IPlayer>)new[] { actor.Player }).GetEnumerator());
        registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => args[0] is Identity identity && identity.Id == id && identity.RealmId == 1 ? actor.Player : null);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var table);
        table.SetProperty(nameof(IGameTableManager.Achievement), PlayerBindPointTests.Table(new AchievementEntry
            { Id = AchievementId, AchievementTypeId = (uint)AchievementType.RealmFirst, RequiredProgress = 1 }));
        var databases = (IDatabaseManager)typeof(Player).GetField("databaseManager", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actor.Player)!;
        var global = new GlobalAchievementManager(databases, tables); global.Initialise();
        using (var read = source.Context())
            actor.Set(nameof(Player.AchievementManager), new CharacterAchievementManager(actor.Player,
                read.Character.AsNoTracking().Include(row => row.Achievement).Single(row => row.Id == id), globalAchievementManager: global, playerManager: players));
        actor.Player.AchievementManager.GrantAchievement(AchievementId);
        var interceptors = new List<IInterceptor>();
        if (barrier) interceptors.Add(new ClaimCommandBarrier(exchange, id));
        if (loseAcknowledgement) interceptors.Add(new LostAcknowledgement());
        string status = "committed"; int? mysqlError = null;
        using (var context = source.Context(interceptors.ToArray()))
        {
            actor.Player.Save(context);
            try { await context.SaveChangesAsync(); }
            catch (IOException error) when (loseAcknowledgement && error.Message == "owned realm-first acknowledgement loss") { status = "unknown"; }
            catch (DbUpdateException error) when (error.InnerException is MySqlException { Number: 1062 } duplicate)
                { status = "conflict"; mysqlError = duplicate.Number; }
        }
        void Pump() { actor.Events.Update(.01); actor.Events.Update(.01); }
        ServerRealmFirstAchievement[] Notices() => messages.GetInvocations("EnqueueMessageEncrypted")
            .Select(call => call.Arguments[0]).OfType<ServerRealmFirstAchievement>().ToArray();
        Pump();
        File.WriteAllText(Path.Combine(exchange, $"first-{id}.json"), JsonSerializer.Serialize(new
            { Id = id, Status = status, MySqlError = mysqlError, Announcements = Notices().Length, Pid = Environment.ProcessId }));
        if (status != "committed")
        {
            await WaitForFile(Path.Combine(exchange, $"retry-{id}"));
            await CharacterAtomicSave.Execute(() => source.Context(), id, actor.Player.Save); Pump();
        }
        Assert.Empty(actor.Errors);
        return new { Id = id, FirstStatus = status, MySqlError = mysqlError, Pid = Environment.ProcessId,
            Announcements = Notices().Select(row => new { row.AchievementId, row.Name, row.IsGuildAchievement }).ToArray() };
    }

    private static async Task WaitForFile(string path)
    {
        var watch = Stopwatch.StartNew();
        while (!File.Exists(path))
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(60)) throw new TimeoutException("Owned realm-first process barrier did not settle.");
            await Task.Delay(5);
        }
    }
    private sealed class ClaimCommandBarrier(string exchange, ulong id) : DbCommandInterceptor
    {
        private int entered;
        private async Task Before(DbCommand command)
        {
            if (!command.CommandText.Contains("realm_first_achievement", StringComparison.OrdinalIgnoreCase)
                || !command.CommandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase)
                || Interlocked.Exchange(ref entered, 1) != 0) return;
            File.WriteAllText(Path.Combine(exchange, $"sql-{id}.json"), JsonSerializer.Serialize(new { Pid = Environment.ProcessId, Id = id, Stage = "before-claim-SQL" }));
            await WaitForFile(Path.Combine(exchange, $"go-{id}"));
        }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data,
            InterceptionResult<DbDataReader> result, CancellationToken token = default) { await Before(command); return result; }
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData data,
            InterceptionResult<int> result, CancellationToken token = default) { await Before(command); return result; }
    }
}
