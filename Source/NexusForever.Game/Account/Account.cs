using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Account.Option;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Account.Costume;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Account.Option;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Account.Unlock;
using NexusForever.Game.RBAC;
using NexusForever.Game.Static;
using NexusForever.Game.Static.RBAC;
using NexusForever.Network.Session;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.GameTable;

namespace NexusForever.Game.Account
{
    public class Account : IAccount
    {
        public uint Id { get; private set; }
        public string Email { get; private set; }

        public IAccountRBACManager RbacManager { get; private set; }
        public IGenericUnlockManager GenericUnlockManager { get; private set; }
        public IAccountCurrencyManager CurrencyManager { get; private set; }
        public IAccountEntitlementManager EntitlementManager { get; private set; }
        public IAccountInventoryManager InventoryManager { get; private set; }
        public IAccountCostumeManager CostumeManager { get; private set; }
        public IRewardPropertyManager RewardPropertyManager { get; private set; }
        public IAccountRewardRotationGrantManager RewardRotationGrantManager { get; private set; }
        public IAccountKeybindingManager KeybindingManager { get; private set; } 

        public AccountTier AccountTier => RbacManager.HasPermission(Permission.Signature) ? AccountTier.Signature : AccountTier.Basic;

        public IGameSession Session { get; private set; }

        private readonly IPendingAccountItemGroupDelivery pendingAccountItemGroupDelivery;
        private readonly ILogger<AccountInventoryManager> accountInventoryLog;
        private readonly IRBACManager rbacManager;
        private readonly IAssetManager assetManager;
        private readonly ICharacterManager characterManager;
        private readonly IPrerequisiteManager prerequisiteManager;
        private readonly IItemManager itemManager;
        private readonly IDatabaseManager databaseManager;
        private readonly IGameTableManager gameTableManager;
        private readonly Role defaultRole = Role.Player;

        public Account()
        {
        }

        public Account(
            IPendingAccountItemGroupDelivery pendingAccountItemGroupDelivery,
            ILogger<AccountInventoryManager> accountInventoryLog = null,
            IRBACManager rbacManager = null,
            Role? defaultRole = null,
            IAssetManager assetManager = null,
            ICharacterManager characterManager = null,
            IPrerequisiteManager prerequisiteManager = null,
            IItemManager itemManager = null,
            IDatabaseManager databaseManager = null,
            IGameTableManager gameTableManager = null)
        {
            this.pendingAccountItemGroupDelivery = pendingAccountItemGroupDelivery;
            this.accountInventoryLog             = accountInventoryLog;
            this.rbacManager                     = rbacManager;
            this.defaultRole                     = defaultRole ?? Role.Player;
            this.assetManager                    = assetManager;
            this.characterManager                = characterManager;
            this.prerequisiteManager             = prerequisiteManager;
            this.itemManager                     = itemManager;
            this.databaseManager                 = databaseManager;
            this.gameTableManager                = gameTableManager ?? throw new ArgumentNullException(nameof(gameTableManager));
        }

        /// <inheritdoc/>
        public uint GetCREDDPendingOrderState()
        {
            try
            {
                AuthDatabase authDatabase = databaseManager?.GetDatabase<AuthDatabase>();
                return authDatabase != null && authDatabase.AccountHasCREDDOrder(Id) ? 1u : 0u;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentNullException)
            {
                return 0u;
            }
        }

        /// <summary>
        /// Initialise <see cref="IAccount"/> with supplied  database model and <see cref="IGameSession"/>.
        /// </summary>
        public void Initialise(AccountModel model, IGameSession session)
        {
            if (Session != null)
                throw new InvalidOperationException();
            if (gameTableManager == null)
                throw new InvalidOperationException($"{nameof(Account)} requires an {nameof(IGameTableManager)}.");

            Id      = model.Id;
            Email   = model.Email;
            Session = session;

            RbacManager           = new AccountRBACManager(this, model, rbacManager, defaultRole);
            GenericUnlockManager  = new GenericUnlockManager(this, model, gameTableManager);
            CurrencyManager       = new AccountCurrencyManager(this, model, gameTableManager);
            EntitlementManager    = new AccountEntitlementManager(this, model, assetManager, gameTableManager);
            InventoryManager      = new AccountInventoryManager(this, model, pendingAccountItemGroupDelivery, accountInventoryLog, characterManager, prerequisiteManager, itemManager, gameTableManager);
            CostumeManager        = new AccountCostumeManager(this, model, gameTableManager);
            RewardPropertyManager        = new RewardPropertyManager(this, assetManager, gameTableManager);
            RewardRotationGrantManager   = new AccountRewardRotationGrantManager(this, model);
            KeybindingManager              = new AccountKeybindingManager(model);
        }

        public void Save(AuthContext context)
        {
            RbacManager.Save(context);
            GenericUnlockManager.Save(context);
            CurrencyManager.Save(context);
            EntitlementManager.Save(context);
            InventoryManager.Save(context);
            CostumeManager.Save(context);
            ((AccountRewardRotationGrantManager)RewardRotationGrantManager).Save(context);
            KeybindingManager.Save(context);
        }
    }
}
