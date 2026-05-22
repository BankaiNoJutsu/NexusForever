using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.ICComm;
using NexusForever.Game.Abstract.Pvp;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Game.Achievement;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Character;
using NexusForever.Game.Chat;
using NexusForever.Game.Cinematic;
using NexusForever.Game.Combat;
using NexusForever.Game.Customisation;
using NexusForever.Game.Entity;
using NexusForever.Game.Fortune;
using NexusForever.Game.Group;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.ICComm;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Loot;
using NexusForever.Game.Map;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Matching;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Pvp;
using NexusForever.Game.PublicEvent;
using NexusForever.Game.Quest;
using NexusForever.Game.RBAC;
using NexusForever.Game.Reputation;
using NexusForever.Game.Server;
using NexusForever.Game.Spell;
using NexusForever.Game.Storefront;
using NexusForever.Game.Story;
using NexusForever.Game.Trade;
using NexusForever.Shared;

namespace NexusForever.Game
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGame(this IServiceCollection sc)
        {
            sc.AddSingletonLegacy<IAssetManager, AssetManager>();
            sc.AddSingletonLegacy<ICleanupManager, CleanupManager>();
            sc.AddSingletonLegacy<IDisableManager, DisableManager>();
            sc.AddSingletonLegacy<IItemManager, ItemManager>();
            sc.AddSingletonLegacy<IRealmContext, RealmContext>();
            sc.AddSingletonLegacy<IShutdownManager, ShutdownManager>();
            sc.AddSingletonLegacy<IDamageCalculator, DamageCalculator>();
            sc.AddSingletonLegacy<IDuelManager, DuelManager>();
            sc.AddSingletonLegacy<IICCommManager, ICCommManager>();
            sc.AddSingletonLegacy<ITradeManager, TradeManager>();
            sc.AddSingletonLegacy<IGroupStateManager, GroupStateManager>();
            sc.AddSingletonLegacy<IGlobalLootManager, GlobalLootManager>();
            sc.AddSingletonLegacy<IAccountPendingItemRepository, AccountPendingItemRepository>();
            sc.AddSingletonLegacy<IPendingAccountItemGroupDelivery, RetailPendingAccountItemGroupDelivery>();

            sc.AddGameAchievement();
            sc.AddGameCharacter();
            sc.AddGameCinematic();
            sc.AddGameCombat();
            sc.AddGameCustomisation();
            sc.AddGameEntity();
            sc.AddGameFortune();
            sc.AddGameEvent();
            sc.AddGameGuild();
            sc.AddGameHousing();
            sc.AddGameMap();
            sc.AddGameMarketplace();
            sc.AddGameMatching();
            sc.AddGamePrerequisite();
            sc.AddGameQuest();
            sc.AddGameRbac();
            sc.AddGameReputation();
            sc.AddGameServer();
            sc.AddGameChat();
            sc.AddGameSpell();
            sc.AddGameStory();
            sc.AddGameStore();
        }
    }
}
