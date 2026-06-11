using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusForever.Database;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Chat.Format;
using NexusForever.Game.Abstract.Customisation;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.ICComm;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Pvp;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Game.Achievement;
using NexusForever.Game.Customisation;
using NexusForever.Game.Entity;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.Loot;
using NexusForever.Game.Map;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Quest;
using NexusForever.Game.RBAC;
using NexusForever.Game.Reputation;
using NexusForever.Game.Abstract.Server;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Text.Filter;
using NexusForever.GameTable.Text.Search;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message;
using NexusForever.Script;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;
using NexusForever.Shared.Diagnostics;
using NexusForever.WorldServer.Command;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Service
{
    public class HostedService : IHostedService
    {
        #region Dependency Injection

        private readonly ILogger log;

        private readonly IScriptManager scriptManager;
        private readonly ILoginQueueManager loginQueueManager;
        private readonly INetworkManager<IWorldSession> networkManager;
        private readonly IMessageManager messageManager;
        private readonly IMatchingManager matchingManager;
        private readonly IMatchManager matchManager;
        private readonly IDuelManager duelManager;
        private readonly IICCommManager icCommManager;
        private readonly ITradeManager tradeManager;
        private readonly IPublicEventTemplateManager publicEventManager;
        private readonly ICreatureInfoManager creatureInfoManager;
        private readonly IChatFormatManager chatFormatManager;
        private readonly IWorldManager worldManager;
        private readonly IBuybackManager buybackManager;
        private readonly ICommandManager commandManager;
        private readonly IEntityCommandManager entityCommandManager;
        private readonly IMapIOManager mapIOManager;
        private readonly IEntityCacheManager entityCacheManager;
        private readonly ICustomisationManager customisationManager;
        private readonly IServerManager serverManager;
        private readonly IGlobalStorefrontManager globalStorefrontManager;
        private readonly IMapManager mapManager;
        private readonly ISearchManager searchManager;
        private readonly ITextFilterManager textFilterManager;
        private readonly IShutdownManager shutdownManager;
        private readonly IRBACManager rbacManager;
        private readonly IGlobalCinematicManager globalCinematicManager;
        private readonly IFactionManager factionManager;
        private readonly IDisableManager disableManager;
        private readonly IGlobalLootManager globalLootManager;
        private readonly IEntityManager entityManager;
        private readonly IGlobalMarketplaceManager globalMarketplaceManager;
        private readonly IGlobalAchievementManager globalAchievementManager;
        private readonly IGlobalGuildManager globalGuildManager;
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly IGlobalSpellManager globalSpellManager;
        private readonly ICharacterManager characterManager;
        private readonly IGlobalResidenceManager globalResidenceManager;
        private readonly IAssetManager assetManager;
        private readonly IItemManager itemManager;
        private readonly IDatabaseManager databaseManager;
        private readonly IRealmContext realmContext;
        private readonly ISharedConfiguration sharedConfiguration;
        private readonly IGameTableManager gameTableManager;

        public HostedService(
            ILogger<IHostedService> log,
            IScriptManager scriptManager,
            ILoginQueueManager loginQueueManager,
            INetworkManager<IWorldSession> networkManager,
            IMessageManager messageManager,
            IMatchingManager matchingManager,
            IMatchManager matchManager,
            IDuelManager duelManager,
            IICCommManager icCommManager,
            ITradeManager tradeManager,
            IPublicEventTemplateManager publicEventManager,
            ICreatureInfoManager creatureInfoManager,
            IChatFormatManager chatFormatManager,
            IWorldManager worldManager,
            IBuybackManager buybackManager,
            ICommandManager commandManager,
            IEntityCommandManager entityCommandManager,
            IMapIOManager mapIOManager,
            IEntityCacheManager entityCacheManager,
            ICustomisationManager customisationManager,
            IServerManager serverManager,
            IGlobalStorefrontManager globalStorefrontManager,
            IMapManager mapManager,
            ISearchManager searchManager,
            ITextFilterManager textFilterManager,
            IShutdownManager shutdownManager,
            IRBACManager rbacManager,
            IGlobalCinematicManager globalCinematicManager,
            IFactionManager factionManager,
            IDisableManager disableManager,
            IGlobalLootManager globalLootManager,
            IEntityManager entityManager,
            IGlobalMarketplaceManager globalMarketplaceManager,
            IGlobalAchievementManager globalAchievementManager,
            IGlobalGuildManager globalGuildManager,
            IGlobalQuestManager globalQuestManager,
            IGlobalSpellManager globalSpellManager,
            ICharacterManager characterManager,
            IGlobalResidenceManager globalResidenceManager,
            IAssetManager assetManager,
            IItemManager itemManager,
            IDatabaseManager databaseManager,
            IRealmContext realmContext,
            ISharedConfiguration sharedConfiguration,
            IGameTableManager gameTableManager)
        {
            this.log               = log;

            this.scriptManager      = scriptManager;
            this.loginQueueManager  = loginQueueManager;
            this.networkManager     = networkManager;
            this.messageManager     = messageManager;
            this.matchingManager    = matchingManager;
            this.matchManager       = matchManager;
            this.duelManager        = duelManager;
            this.icCommManager      = icCommManager;
            this.tradeManager       = tradeManager;
            this.publicEventManager = publicEventManager;
            this.creatureInfoManager = creatureInfoManager;
            this.chatFormatManager  = chatFormatManager;
            this.worldManager       = worldManager;
            this.buybackManager     = buybackManager;
            this.commandManager     = commandManager;
            this.entityCommandManager = entityCommandManager;
            this.mapIOManager       = mapIOManager;
            this.entityCacheManager = entityCacheManager;
            this.customisationManager = customisationManager;
            this.serverManager      = serverManager;
            this.globalStorefrontManager = globalStorefrontManager;
            this.mapManager         = mapManager;
            this.searchManager      = searchManager;
            this.textFilterManager  = textFilterManager;
            this.shutdownManager    = shutdownManager;
            this.rbacManager        = rbacManager;
            this.globalCinematicManager = globalCinematicManager;
            this.factionManager     = factionManager;
            this.disableManager     = disableManager;
            this.globalLootManager  = globalLootManager;
            this.entityManager      = entityManager;
            this.globalMarketplaceManager = globalMarketplaceManager;
            this.globalAchievementManager = globalAchievementManager;
            this.globalGuildManager = globalGuildManager;
            this.globalQuestManager = globalQuestManager;
            this.globalSpellManager = globalSpellManager;
            this.characterManager   = characterManager;
            this.globalResidenceManager = globalResidenceManager;
            this.assetManager       = assetManager;
            this.itemManager        = itemManager;
            this.databaseManager    = databaseManager;
            this.realmContext       = realmContext;
            this.sharedConfiguration = sharedConfiguration;
            this.gameTableManager   = gameTableManager;
        }

        #endregion

        /// <summary>
        /// Start <see cref="WorldServer"/> and any related resources.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            log.LogInformation("Starting...");

            sharedConfiguration.Initialise<WorldServerConfiguration>();

            databaseManager.Initialise(sharedConfiguration.Get<DatabaseConfig>());
            databaseManager.Migrate();

            realmContext.Initialise();

            // RBACManager must be initialised before CommandManager
            rbacManager.Initialise();

            disableManager.Initialise();

            scriptManager.Initialise();

            await gameTableManager.Initialise();
            publicEventManager.Initialise();
            mapIOManager.Initialise();
            searchManager.Initialise();
            entityManager.Initialise();
            creatureInfoManager.Initialise();
            entityCommandManager.Initialise();
            entityCacheManager.Initialise();
            factionManager.Initialise();

            globalCinematicManager.Initialise();
            chatFormatManager.Initialise();
            globalAchievementManager.Initialise(); // must be initialised before guilds
            globalGuildManager.Initialise(); // must be initialised before residences
            characterManager.Initialise(); // must be initialised before residences
            globalResidenceManager.Initialise();

            assetManager.Initialise();
            itemManager.Initialise();
            globalMarketplaceManager.Initialise();
            globalSpellManager.Initialise();
            globalQuestManager.Initialise();
            globalLootManager.Initialise();

            globalStorefrontManager.Initialise();
            serverManager.Initialise(realmContext.RealmId);

            textFilterManager.Initialise();

            customisationManager.Initialise();

            shutdownManager.Initialise(WorldServer.Shutdown);

            matchingManager.Initialise();

            messageManager.RegisterNetworkManagerMessagesAndHandlers();
            messageManager.RegisterNetworkManagerWorldMessages();
            messageManager.RegisterNetworkManagerWorldHandlers();

            // initialise world after all assets have loaded but before any network or command handlers might be invoked
            worldManager.Initialise(lastTick =>
            {
                // NetworkManager must be first and MapManager must come before everything else
                NexusForeverDiagnostics.MeasureTickSubsystem("network", () => networkManager.Update(lastTick));
                NexusForeverDiagnostics.MeasureTickSubsystem("map", () => mapManager.Update(lastTick));

                NexusForeverDiagnostics.MeasureTickSubsystem("buyback", () => buybackManager.Update(lastTick));
                NexusForeverDiagnostics.MeasureTickSubsystem("quest", () => globalQuestManager.Update(lastTick));
                NexusForeverDiagnostics.MeasureTickSubsystem("loot", () => globalLootManager.Update(lastTick));
                NexusForeverDiagnostics.MeasureTickSubsystem("guild", () => globalGuildManager.Update(lastTick));
                NexusForeverDiagnostics.MeasureTickSubsystem("residence", () => globalResidenceManager.Update(lastTick)); // must be after guild update
                NexusForeverDiagnostics.MeasureTickSubsystem("marketplace", () => globalMarketplaceManager.Update(lastTick));

                NexusForeverDiagnostics.MeasureTickSubsystem("login-queue", () => loginQueueManager.Update(lastTick));
                NexusForeverDiagnostics.MeasureTickSubsystem("matching", () => matchingManager.Update(lastTick));
                NexusForeverDiagnostics.MeasureTickSubsystem("match", () => matchManager.Update(lastTick));
                NexusForeverDiagnostics.MeasureTickSubsystem("duel", () => duelManager.Update(lastTick));
                NexusForeverDiagnostics.MeasureTickSubsystem("iccomm", () => icCommManager.Update(lastTick));
                NexusForeverDiagnostics.MeasureTickSubsystem("trade", () => tradeManager.Update(lastTick));

                NexusForeverDiagnostics.MeasureTickSubsystem("script", () => scriptManager.Update(lastTick));

                NexusForeverDiagnostics.MeasureTickSubsystem("shutdown", () => shutdownManager.Update(lastTick));

                // process commands after everything else in the tick has processed
                NexusForeverDiagnostics.MeasureTickSubsystem("command", () => commandManager.Update(lastTick));
            });

            // initialise network and command managers last to make sure the rest of the server is ready for invoked handlers
            networkManager.Initialise();
            networkManager.Start();

            commandManager.Initialise();

            log.LogInformation("Started!");
        }

        /// <summary>
        /// Stop <see cref="WorldServer"/> and any related resources.
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            log.LogInformation("Stopping...");

            // stop network manager listening for incoming connections
            // it is still possible for incoming packets to be parsed though won't be handled once the world thread is stopped
            networkManager.Shutdown();

            // stop command manager listening for commands
            commandManager.Shutdown();

            // stop server manager pinging other servers
            serverManager.Shutdown();

            // stop world manager processing the world thread
            // at this point no incoming packets will be handled
            worldManager.Shutdown();

            // save residences, guilds and players to the database
            globalResidenceManager.Shutdown();
            globalGuildManager.Shutdown();

            foreach (IWorldSession worldSession in networkManager)
            {
                if (worldSession.Player != null)
                    await worldSession.Player.SaveDirect();
            }

            log.LogInformation("Stopped!");
        }
    }
}
