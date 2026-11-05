using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Crafting;
using NexusForever.GameTable;

namespace NexusForever.WorldServer.Service
{
    // Registered after World startup: migrations, realm and game tables are ready.
    public sealed class CraftingAccountOutboxHostedService(IDatabaseManager databases, IRealmContext realm,
        IGameTableManager tables, ILogger<CraftingAccountOutboxHostedService> log) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var characters = databases.GetDatabase<CharacterDatabase>()
                ?? throw new InvalidOperationException("Crafting outbox requires the Character database.");
            var auth = databases.GetDatabase<AuthDatabase>()
                ?? throw new InvalidOperationException("Crafting outbox requires the Auth database.");
            var worker = new CraftingAccountOutbox(characters.CreateContext, auth.CreateContext, realm.RealmId, tables);
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await worker.DrainAsync(cancellationToken: stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception error) { log.LogError(error, "Crafting account outbox pass failed in realm {Realm}.", realm.RealmId); }
                try { if (!await timer.WaitForNextTickAsync(stoppingToken)) return; }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            }
        }
    }
}
