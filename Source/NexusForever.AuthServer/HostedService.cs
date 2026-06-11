using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusForever.AuthServer.Network;
using NexusForever.Database;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Server;
using NexusForever.Network.Auth.Message;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;
using NexusForever.Shared.Diagnostics;

namespace NexusForever.AuthServer
{
    public class HostedService : IHostedService
    {
        #region Dependency Injection

        private readonly ILogger log;

        private readonly INetworkManager<IAuthSession> networkManager;
        private readonly IMessageManager messageManager;
        private readonly IWorldManager worldManager;
        private readonly IServerManager serverManager;
        private readonly IDatabaseManager databaseManager;
        private readonly ISharedConfiguration sharedConfiguration;

        public HostedService(
            ILogger<IHostedService> log,
            INetworkManager<IAuthSession> networkManager,
            IMessageManager messageManager,
            IWorldManager worldManager,
            IServerManager serverManager,
            IDatabaseManager databaseManager,
            ISharedConfiguration sharedConfiguration)
        {
            this.log            = log;

            this.networkManager = networkManager;
            this.messageManager = messageManager;
            this.worldManager   = worldManager;
            this.serverManager  = serverManager;
            this.databaseManager = databaseManager;
            this.sharedConfiguration = sharedConfiguration;
        }

        #endregion

        /// <summary>
        /// Start <see cref="AuthServer"/> and any related resources.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            log.LogInformation("Starting...");

            sharedConfiguration.Initialise<AuthServerConfiguration>();

            databaseManager.Initialise(sharedConfiguration.Get<DatabaseConfig>());

            serverManager.Initialise();

            // initialise world after all assets have loaded but before any network or command handlers might be invoked
            worldManager.Initialise(lastTick =>
            {
                NexusForeverDiagnostics.MeasureTickSubsystem("network", () => networkManager.Update(lastTick));
            });

            // initialise network and command managers last to make sure the rest of the server is ready for invoked handlers
            messageManager.RegisterNetworkManagerMessagesAndHandlers();
            messageManager.RegisterNetworkManagerAuthMessages();
            messageManager.RegisterNetworkManagerAuthHandlers();

            networkManager.Initialise();
            networkManager.Start();

            log.LogInformation("Started!");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Shutdown <see cref="AuthServer"/> and any related resources.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            log.LogInformation("Stopping...");

            // stop network manager listening for incoming connections
            // it is still possible for incoming packets to be parsed though won't be handled once the world thread is stopped
            networkManager.Shutdown();

            // stop server manager pinging other servers
            serverManager.Shutdown();

            // stop world manager processing the world thread
            // at this point no incoming packets will be handled
            worldManager.Shutdown();

            log.LogInformation("Stopped!");
            return Task.CompletedTask;
        }
    }
}
