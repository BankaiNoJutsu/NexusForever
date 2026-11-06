using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Achievement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using SourceFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildCreationFinanceTests
{
    // Called only by the owned provider harness. No global/session defaults are changed.
    internal async Task ProbeAcknowledgedAchievementDate(Action<object> observe)
    {
        using var f = await CreateFeedbackFixture();
        bool truncate = false;
        using (var context = f.Source.Context()) Assert.True(context.Database.IsMySql());
        ReplaceFeedbackDatabase(f.Actor, () => f.Source.Context(new FeedbackDateMode(() => truncate)));
        var parent = f.Achievements.GetAchievement(600); var child = f.Achievements.GetAchievement(601);
        var older = new AchievementInfo(new AchievementEntry { Id = 609, AchievementTypeId = (uint)AchievementType.KillCreatureEntry,
            ObjectId = 999, RequiredProgress = 1 });
        ((RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements).SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement),
            args => (ushort)args[0] switch { 600 => parent, 601 => child, 609 => older, _ => null });
        var manager = Assert.IsType<CharacterAchievementManager>(f.Actor.Player.AchievementManager);
        manager.GrantAchievement(609);
        var get = typeof(BaseAchievementManager<CharacterAchievementModel>).GetMethod("GetAchievement", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var row = (IAchievement)get.Invoke(manager, [(ushort)609])!;
        row.DateCompleted = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddMilliseconds(800);
        f.Actor.StartSave(); await f.Actor.Drain();
        DateTime storedDate;
        using (var before = f.Source.Context()) storedDate = before.CharacterAchievement.Single(value => value.AchievementId == 609).DateCompleted!.Value;
        Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 6), storedDate);
        truncate = true; EarnFeedback(f); await CommitFeedbackParent(f); await f.Actor.Drain();
        using var final = f.Source.Context();
        using var projection = f.Actor.Player.GetCharacterDatabase().CreateContext();
        DateTime recast = projection.Database.SqlQueryRaw<DateTime>("SELECT CAST({0} AS DATETIME) AS Value", row.DateCompleted.Value).AsEnumerable().Single();
        observe(new { StoredBefore = storedDate, ActorDate = row.DateCompleted, RecastInNewMode = recast,
            StoredAfter = final.CharacterAchievement.Single(value => value.AchievementId == 609).DateCompleted,
            ChildRows = final.CharacterAchievement.Count(value => value.AchievementId == 601),
            IntentRows = final.BankReconciliationReceipt.Count(value => value.Id.StartsWith("g")),
            DeliveryRows = final.BankReconciliationReceipt.Count(value => value.Id.StartsWith("h")) });
        Assert.Equal(storedDate, final.CharacterAchievement.Single(value => value.AchievementId == 609).DateCompleted);
        Assert.Equal(1u, final.CharacterAchievement.Single(value => value.AchievementId == 601).ProgressState);
        Assert.Single(final.BankReconciliationReceipt.Where(value => value.Id.StartsWith("h")));
    }
    private sealed class FeedbackDateMode(Func<bool> truncate) : DbConnectionInterceptor
    {
        private string Sql => truncate() ? "SET SESSION sql_mode = 'STRICT_TRANS_TABLES,NO_ENGINE_SUBSTITUTION,TIME_TRUNCATE_FRACTIONAL'"
            : "SET SESSION sql_mode = 'STRICT_TRANS_TABLES,NO_ENGINE_SUBSTITUTION'";
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        { using var command = connection.CreateCommand(); command.CommandText = Sql; command.ExecuteNonQuery(); }
        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        { using var command = connection.CreateCommand(); command.CommandText = Sql; await command.ExecuteNonQueryAsync(cancellationToken); }
    }
    private static void ReplaceFeedbackDatabase(Actor actor, Func<CharacterContext> contexts)
    {
        var auth = actor.Player.GetAuthDatabase();
        var database = new FeedbackDatabase(contexts);
        using (var context = contexts()) database.Initialise(new DatabaseConnectionString
        { Provider = context.Database.IsMySql() ? DatabaseProvider.MySql : DatabaseProvider.Sqlite, ConnectionString = context.Database.GetDbConnection().ConnectionString });
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var routes);
        routes.SetMethodHandler(nameof(IDatabaseManager.GetDatabase), (method, _) =>
            method.GetGenericArguments()[0] == typeof(NexusForever.Database.Auth.AuthDatabase) ? auth : database);
        typeof(Player).GetField("databaseManager", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, databases);
    }

    internal async Task ProbeFeedbackProcessProducer(string boundary, Action<string> checkpoint)
    {
        Assert.Contains(boundary, new[] { "before-parent", "after-parent", "recipient-before-flush", "recipient-before-commit", "recipient-after-commit", "after-publication", "control" });
        using var f = await CreateFeedbackFixture();
        ReplaceFeedbackDatabase(f.Actor, () => f.Source.Context(new FeedbackProcessSaveBlocker(boundary, checkpoint),
            new FeedbackProcessTransactionBlocker(boundary, checkpoint)));
        EarnFeedback(f);
        if (boundary == "before-parent") checkpoint(boundary);
        await CommitFeedbackParent(f);
        if (boundary == "after-parent") checkpoint(boundary);
        if (boundary.StartsWith("recipient-", StringComparison.Ordinal))
        {
            while (true) { f.Actor.Events.Update(.01); await Task.Delay(5); }
        }
        await f.Actor.Drain(); AssertFeedbackDelivered(f);
        if (boundary == "after-publication") checkpoint(boundary);
    }
    private static bool HasFeedbackMarker(DbContext context)
        => context is CharacterContext && context.ChangeTracker.Entries<BankReconciliationReceiptModel>().Any(row => row.Entity.Id.StartsWith("h"));
    private sealed class FeedbackProcessSaveBlocker(string boundary, Action<string> checkpoint) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (boundary == "recipient-before-flush" && HasFeedbackMarker(eventData.Context)) checkpoint(boundary);
            return ValueTask.FromResult(result);
        }
    }
    private sealed class FeedbackProcessTransactionBlocker(string boundary, Action<string> checkpoint) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (boundary == "recipient-before-commit" && HasFeedbackMarker(eventData.Context)) checkpoint(boundary);
            return ValueTask.FromResult(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (boundary == "recipient-after-commit" && HasFeedbackMarker(eventData.Context)) checkpoint(boundary);
            return Task.CompletedTask;
        }
    }

    internal sealed record FeedbackProcessObservation(int GuildRows, int Intents, int Deliveries, int CharacterRows, uint Progress, ulong Credits);
    internal async Task<FeedbackProcessObservation> ProbeFeedbackProcessRecovery(DbContextOptions<CharacterContext> options)
    {
        using var source = new SourceFixture(options, false);
        var tables = source.Source.Tables;
        ((RecordingDispatchProxy<IGameTableManager>)(object)tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000000 }));
        var account = (RecordingDispatchProxy<IAccount>)(object)source.Source.Player.Account;
        account.SetProperty(nameof(IAccount.Id), 12u);
        var database = new CharacterDatabase();
        using (var context = source.Context()) database.Initialise(new DatabaseConnectionString
        { Provider = context.Database.IsMySql() ? DatabaseProvider.MySql : DatabaseProvider.Sqlite, ConnectionString = context.Database.GetDbConnection().ConnectionString });
        CharacterModel model = Assert.Single(await database.GetCharacters(12));
        var events = new EventQueue(); var session = RecordingDispatchProxy<IWorldSession>.Create(out var connection);
        connection.SetProperty(nameof(IWorldSession.Events), events); connection.SetProperty(nameof(IWorldSession.CanProcessIncomingPackets), true);
        using var actor = new Actor(source, actorModel: model, actorSession: session, actorEvents: events, characterDatabase: database,
            inventoryFactory: (owner, _) => NexusForever.Game.Tests.Entity.BankDeferredCurrencyRecoveryTests.Inventory(owner,
                (IItemManager)(object)source.Source.ManagerProxy, tables, null));
        connection.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var currencies = new CurrencyManager(tables); currencies.Initialise(actor.Player, model); actor.Set(nameof(Player.CurrencyManager), currencies);
        var achievements = RecordingDispatchProxy<IGlobalAchievementManager>.Create(out var catalog);
        var child = new AchievementInfo(new AchievementEntry { Id = 601, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 600, RequiredProgress = 2, PrerequisiteIdServer = 1234 });
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => (ushort)args[0] == 601 ? child : null);
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            (AchievementType)args[0] == AchievementType.AchievementComplete ? new IAchievementInfo[] { child } : []);
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var denied);
        denied.SetMethodHandler(nameof(IPrerequisiteManager.Meets), _ => throw new InvalidOperationException("Restart recovery must not reevaluate accepted eligibility."));
        var manager = new CharacterAchievementManager(actor.Player, model, globalAchievementManager: achievements, gameTableManager: tables, prerequisiteManager: prerequisites);
        actor.Set(nameof(Player.AchievementManager), manager);
        await actor.Load(); manager.PumpGuildFeedbackRecovery(0); await actor.Drain();
        actor.StartSave(); await actor.Drain(); manager.PumpGuildFeedbackRecovery(31); await actor.Drain();
        using var stored = source.Context(); var row = stored.CharacterAchievement.SingleOrDefault(value => value.AchievementId == 601);
        return new(stored.GuildAchievement.Count(value => value.AchievementId == 600),
            stored.BankReconciliationReceipt.Count(value => value.Id.StartsWith("g")), stored.BankReconciliationReceipt.Count(value => value.Id.StartsWith("h")),
            row == null ? 0 : 1, row?.ProgressState ?? 0, stored.CharacterCurrency.Single(value => value.CurrencyId == 1).Amount);
    }
}
