using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Configuration.Model;

namespace NexusForever.Database.Auth
{
    public class AuthContext : DbContext
    {
        public DbSet<AccountModel> Account { get; set; }
        public DbSet<AccountCostumeUnlockModel> AccountCostumeUnlock { get; set; }
        public DbSet<AccountCurrencyModel> AccountCurrency { get; set; }
        public DbSet<AccountEntitlementModel> AccountEntitlement { get; set; }
        public DbSet<AccountExternalReferenceModel> AccountExternalReference { get; set; }
        public DbSet<AccountGenericUnlockModel> AccountGenericUnlock { get; set; }
        public DbSet<AccountInventoryModel> AccountInventory { get; set; }
        public DbSet<AccountPendingItemModel> AccountPendingItem { get; set; }
        public DbSet<AccountCREDDOrderModel> AccountCREDDOrder { get; set; }
        public DbSet<AccountCREDDHistoryModel> AccountCREDDHistory { get; set; }
        public DbSet<AccountDailyLoginModel> AccountDailyLogin { get; set; }
        public DbSet<AccountRewardRotationGrantModel> AccountRewardRotationGrant { get; set; }
        public DbSet<AccountStorePurchaseHistoryModel> AccountStorePurchaseHistory { get; set; }
        public DbSet<AccountFortuneSessionModel> AccountFortuneSession { get; set; }
        public DbSet<AccountItemCooldownModel> AccountItemCooldown { get; set; }
        public DbSet<AccountKeybindingModel> AccountKeybinding { get; set; }
        public DbSet<AccountPermissionModel> AccountPermission { get; set; }
        public DbSet<AccountRoleModel> AccountRole { get; set; }
        public DbSet<AccountSuspensionModel> AccountSuspension { get; set; }
        public DbSet<PermissionModel> Permission { get; set; }
        public DbSet<RoleModel> Role { get; set; }
        public DbSet<RolePermissionModel> RolePermission { get; set; }
        public DbSet<ServerModel> Server { get; set; }
        public DbSet<ServerMessageModel> ServerMessage { get; set; }

        private readonly IConnectionString config;

        public AuthContext(IConnectionString config)
        {
            this.config = config;
        }

        public AuthContext(DbContextOptions<AuthContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseConfiguration(config);

            optionsBuilder.EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountModel>(entity =>
            {
                entity.ToTable("account");

                entity.HasIndex(e => e.Email)
                    .HasDatabaseName("email");

                entity.HasIndex(e => e.GameToken)
                    .HasDatabaseName("gameToken");

                entity.HasIndex(e => e.SessionKey)
                    .HasDatabaseName("sessionKey");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.CreateTime)
                    .HasColumnName("createTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasColumnName("email")
                    .HasColumnType("varchar(128)")
                    .HasDefaultValue("");

                entity.Property(e => e.GameToken)
                    .IsRequired()
                    .HasColumnName("gameToken")
                    .HasColumnType("varchar(32)")
                    .HasDefaultValue("");

                entity.Property(e => e.S)
                    .IsRequired()
                    .HasColumnName("s")
                    .HasColumnType("varchar(32)")
                    .HasDefaultValue("");

                entity.Property(e => e.SessionKey)
                    .IsRequired()
                    .HasColumnName("sessionKey")
                    .HasColumnType("varchar(32)")
                    .HasDefaultValue("");

                entity.Property(e => e.V)
                    .IsRequired()
                    .HasColumnName("v")
                    .HasColumnType("varchar(512)")
                    .HasDefaultValue("");
            });

            modelBuilder.Entity<AccountCostumeUnlockModel>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.ItemId })
                    .HasName("PRIMARY");

                entity.ToTable("account_costume_unlock");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.ItemId)
                    .HasColumnName("itemId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.Timestamp)
                    .HasColumnName("timestamp")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountCostumeUnlock)
                    .HasForeignKey(d => d.Id)
                    .HasConstraintName("FK__account_costume_item_id__account_id");
            });

            modelBuilder.Entity<AccountCurrencyModel>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.CurrencyId })
                    .HasName("PRIMARY");

                entity.ToTable("account_currency");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.CurrencyId)
                    .HasColumnName("currencyId")
                    .HasColumnType("tinyint(4) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0);

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountCurrency)
                    .HasForeignKey(d => d.Id)
                    .HasConstraintName("FK__account_currency_id__account_id");
            });

            modelBuilder.Entity<AccountEntitlementModel>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.EntitlementId })
                    .HasName("PRIMARY");

                entity.ToTable("account_entitlement");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.EntitlementId)
                    .HasColumnName("entitlementId")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountEntitlement)
                    .HasForeignKey(d => d.Id)
                    .HasConstraintName("FK__account_entitlement_id__account_id");
            });

            modelBuilder.Entity<AccountExternalReferenceModel>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.Type })
                    .HasName("PRIMARY");

                entity.ToTable("account_external_reference");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned");

                entity.Property(e => e.Type)
                    .HasColumnName("type")
                    .HasColumnType("varchar(64)");

                entity.Property(e => e.Value)
                    .HasColumnName("value")
                    .HasColumnType("varchar(512)");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountExternalReference)
                    .HasForeignKey(d => d.Id)
                    .HasConstraintName("FK__account_external_reference_id__account_id");
            });

            modelBuilder.Entity<AccountGenericUnlockModel>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.Entry })
                    .HasName("PRIMARY");

                entity.ToTable("account_generic_unlock");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.Entry)
                    .HasColumnName("entry")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.Timestamp)
                    .HasColumnName("timestamp")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountGenericUnlock)
                    .HasForeignKey(d => d.Id)
                    .HasConstraintName("FK__account_generic_unlock_id__account_id");
            });

            modelBuilder.Entity<AccountInventoryModel>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.InventoryId })
                    .HasName("PRIMARY");

                entity.ToTable("account_inventory");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.InventoryId)
                    .HasColumnName("inventoryId")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.AccountItemId)
                    .HasColumnName("accountItemId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.ClaimState)
                    .HasColumnName("claimState")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.HasTargetPlayerIdentity)
                    .HasColumnName("unknown1")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(false);

                entity.Property(e => e.TargetRealmId)
                    .HasColumnName("targetRealmId")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.TargetCharacterId)
                    .HasColumnName("targetCharacterId")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.CreateTime)
                    .HasColumnName("createTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountInventory)
                    .HasForeignKey(d => d.Id)
                    .HasConstraintName("FK__account_inventory_id__account_id");
            });

            modelBuilder.Entity<AccountPendingItemModel>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.PendingItemId })
                    .HasName("PRIMARY");

                entity.ToTable("account_pending_item");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.PendingItemId)
                    .HasColumnName("pendingItemId")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.GroupName)
                    .IsRequired()
                    .HasColumnName("groupName")
                    .HasColumnType("varchar(128)")
                    .HasDefaultValue("");

                entity.Property(e => e.AccountItemId)
                    .HasColumnName("accountItemId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.SenderAccountId)
                    .HasColumnName("senderAccountId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.SenderRealmId)
                    .HasColumnName("senderRealmId")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.SenderCharacterId)
                    .HasColumnName("senderCharacterId")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.TargetRealmId)
                    .HasColumnName("targetRealmId")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.TargetCharacterId)
                    .HasColumnName("targetCharacterId")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.ClaimState)
                    .HasColumnName("claimState")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.HasTargetPlayerIdentity)
                    .HasColumnName("unknown1")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(false);

                entity.Property(e => e.CreateTime)
                    .HasColumnName("createTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountPendingItem)
                    .HasForeignKey(d => d.Id)
                    .HasConstraintName("FK__account_pending_item_id__account_id");
            });

            modelBuilder.Entity<AccountCREDDOrderModel>(entity =>
            {
                entity.HasKey(e => e.OrderId)
                    .HasName("PRIMARY");

                entity.ToTable("account_credd_order");

                entity.Property(e => e.OrderId)
                    .HasColumnName("orderId")
                    .HasColumnType("bigint(20) unsigned")
                    .ValueGeneratedNever();

                entity.Property(e => e.AccountId)
                    .HasColumnName("accountId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.CharacterId)
                    .HasColumnName("characterId")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0ul);

                entity.Property(e => e.RealmId)
                    .HasColumnName("realmId")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.CreditAmount)
                    .HasColumnName("creditAmount")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0ul);

                entity.Property(e => e.IsBuyOrder)
                    .HasColumnName("isBuyOrder")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<AccountCREDDHistoryModel>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("PRIMARY");

                entity.ToTable("account_credd_history");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("bigint(20) unsigned")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.AccountId)
                    .HasColumnName("accountId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.CreatedUtc)
                    .HasColumnName("createdUtc")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                entity.Property(e => e.Operation)
                    .HasColumnName("operation")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.IsInitiator)
                    .HasColumnName("isInitiator")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(false);

                entity.Property(e => e.IdentityRealmId)
                    .HasColumnName("identityRealmId")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.IdentityCharacterId)
                    .HasColumnName("identityCharacterId")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0ul);

                entity.Property(e => e.CounterpartyRealmId)
                    .HasColumnName("counterpartyRealmId")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.CounterpartyCharacterId)
                    .HasColumnName("counterpartyCharacterId")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0ul);

                entity.Property(e => e.CreditAmount)
                    .HasColumnName("creditAmount")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0ul);
            });

            modelBuilder.Entity<AccountFortuneSessionModel>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("PRIMARY");

                entity.ToTable("account_fortune_session");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .ValueGeneratedNever();

                entity.Property(e => e.Card0AccountItemId)
                    .HasColumnName("card0AccountItemId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.Card1AccountItemId)
                    .HasColumnName("card1AccountItemId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.Card2AccountItemId)
                    .HasColumnName("card2AccountItemId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.Card0Rarity)
                    .HasColumnName("card0Rarity")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue((byte)0);

                entity.Property(e => e.Card1Rarity)
                    .HasColumnName("card1Rarity")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue((byte)0);

                entity.Property(e => e.Card2Rarity)
                    .HasColumnName("card2Rarity")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue((byte)0);

                entity.Property(e => e.Card0Flipped)
                    .HasColumnName("card0Flipped")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(false);

                entity.Property(e => e.Card1Flipped)
                    .HasColumnName("card1Flipped")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(false);

                entity.Property(e => e.Card2Flipped)
                    .HasColumnName("card2Flipped")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(false);

                entity.Property(e => e.Card0Granted)
                    .HasColumnName("card0Granted")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(false);

                entity.Property(e => e.Card1Granted)
                    .HasColumnName("card1Granted")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(false);

                entity.Property(e => e.Card2Granted)
                    .HasColumnName("card2Granted")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(false);

                entity.HasOne(d => d.Account)
                    .WithOne(p => p.AccountFortuneSession)
                    .HasForeignKey<AccountFortuneSessionModel>(d => d.Id)
                    .HasConstraintName("FK__account_fortune_session_id__account_id");
            });

            modelBuilder.Entity<AccountDailyLoginModel>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("PRIMARY");

                entity.ToTable("account_daily_login");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .ValueGeneratedNever();

                entity.Property(e => e.LoginDaysTotal)
                    .HasColumnName("loginDaysTotal")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.RewardsAvailable)
                    .HasColumnName("rewardsAvailable")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.LastClaimedLoginDay)
                    .HasColumnName("lastClaimedLoginDay")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.LastRewardItemKey)
                    .HasColumnName("lastRewardItemKey")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.PremiumKeyStatus)
                    .HasColumnName("premiumKeyStatus")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.SecondsUntilNextKey)
                    .HasColumnName("secondsUntilNextKey")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.LastClaimUtc)
                    .HasColumnName("lastClaimUtc")
                    .HasColumnType("datetime");

                entity.Property(e => e.LastDayIncrementUtc)
                    .HasColumnName("lastDayIncrementUtc")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.Account)
                    .WithOne(p => p.AccountDailyLogin)
                    .HasForeignKey<AccountDailyLoginModel>(d => d.Id)
                    .HasConstraintName("FK__account_daily_login_id__account_id");
            });

            modelBuilder.Entity<AccountRewardRotationGrantModel>(entity =>
            {
                entity.HasKey(e => new { e.AccountId, e.RewardRotationIndex, e.ContentId, e.RewardKeyId, e.RewardType })
                    .HasName("PRIMARY");

                entity.ToTable("account_reward_rotation_grant");

                entity.Property(e => e.AccountId)
                    .HasColumnName("accountId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.RewardRotationIndex)
                    .HasColumnName("rewardRotationIndex")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.ContentId)
                    .HasColumnName("contentId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.RewardKeyId)
                    .HasColumnName("rewardKeyId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.RewardType)
                    .HasColumnName("rewardType")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue((byte)0);

                entity.Property(e => e.GrantFlags)
                    .HasColumnName("grantFlags")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.GrantedUtc)
                    .HasColumnName("grantedUtc")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountRewardRotationGrant)
                    .HasForeignKey(d => d.AccountId)
                    .HasConstraintName("FK__account_reward_rotation_grant_accountId__account_id");
            });

            modelBuilder.Entity<AccountStorePurchaseHistoryModel>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("PRIMARY");

                entity.ToTable("account_store_purchase_history");

                entity.HasIndex(e => new { e.AccountId, e.PurchasedUtc })
                    .HasDatabaseName("accountId_purchasedUtc");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("bigint(20) unsigned")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.AccountId)
                    .HasColumnName("accountId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.OfferId)
                    .HasColumnName("offerId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                entity.Property(e => e.CurrencyId)
                    .HasColumnName("currencyId")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.Price)
                    .HasColumnName("price")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0ul);

                entity.Property(e => e.PurchasedUtc)
                    .HasColumnName("purchasedUtc")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountStorePurchaseHistory)
                    .HasForeignKey(d => d.AccountId)
                    .HasConstraintName("FK__account_store_purchase_history_accountId__account_id");
            });

            modelBuilder.Entity<AccountItemCooldownModel>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.CooldownGroupId })
                    .HasName("PRIMARY");

                entity.ToTable("account_item_cooldown");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.CooldownGroupId)
                    .HasColumnName("cooldownGroupId")
                    .HasColumnType("int(10) unsigned")
                    .ValueGeneratedNever();

                entity.Property(e => e.Timestamp)
                    .HasColumnName("timestamp")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                entity.Property(e => e.Duration)
                    .HasColumnName("duration")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountItemCooldown)
                    .HasForeignKey(d => d.Id)
                    .HasConstraintName("FK__account_item_cooldown_id__account_id");
            });

            modelBuilder.Entity<AccountKeybindingModel>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.InputActionId })
                    .HasName("PRIMARY");

                entity.ToTable("account_keybinding");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.InputActionId)
                    .HasColumnName("inputActionId")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.Code00)
                    .HasColumnName("code00")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.Code01)
                    .HasColumnName("code01")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.Code02)
                    .HasColumnName("code02")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.DeviceEnum00)
                    .HasColumnName("deviceEnum00")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.DeviceEnum01)
                    .HasColumnName("deviceEnum01")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.DeviceEnum02)
                    .HasColumnName("deviceEnum02")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.EventTypeEnum00)
                    .HasColumnName("eventTypeEnum00")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.EventTypeEnum01)
                    .HasColumnName("eventTypeEnum01")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.EventTypeEnum02)
                    .HasColumnName("eventTypeEnum02")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.MetaKeys00)
                    .HasColumnName("metaKeys00")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.MetaKeys01)
                    .HasColumnName("metaKeys01")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.MetaKeys02)
                    .HasColumnName("metaKeys02")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountKeybinding)
                    .HasForeignKey(d => d.Id)
                    .HasConstraintName("FK__account_keybinding_id__account_id");
            });

            modelBuilder.Entity<AccountPermissionModel>(entity =>
            {
                entity.ToTable("account_permission");

                entity.HasKey(i => new { i.Id, i.PermissionId });

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.PermissionId)
                    .HasColumnName("permissionId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.HasOne(e => e.Account)
                    .WithMany(f => f.AccountPermission)
                    .HasForeignKey(e => e.Id)
                    .HasConstraintName("FK__account_permission_id__account_id");

                entity.HasOne(e => e.Permission)
                    .WithMany(f => f.AccountPermission)
                    .HasForeignKey(e => e.PermissionId)
                    .HasConstraintName("FK__account_permission_permission_id__permission_id");
            });

            modelBuilder.Entity<AccountRoleModel>(entity =>
            {
                entity.ToTable("account_role");

                entity.HasKey(i => new { i.Id, i.RoleId });

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.RoleId)
                    .HasColumnName("roleId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.HasOne(e => e.Account)
                    .WithMany(f => f.AccountRole)
                    .HasForeignKey(e => e.Id)
                    .HasConstraintName("FK__account_role_id__account_id");

                entity.HasOne(e => e.Role)
                    .WithMany(f => f.AccountRole)
                    .HasForeignKey(e => e.RoleId)
                    .HasConstraintName("FK__account_role_role_id__role_id");
            });

            modelBuilder.Entity<AccountSuspensionModel>(entity =>
            {
                entity.ToTable("account_suspension");

                entity.HasKey(e => new { e.Id, e.BanId })
                    .HasName("PRIMARY");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.BanId)
                    .HasColumnName("banId")
                    .HasColumnType("int(10) unsigned")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.StartTime)
                    .HasColumnName("startTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                entity.Property(e => e.EndTime)
                    .HasColumnName("endTime")
                    .HasColumnType("datetime")
                    .HasDefaultValue(null);

                entity.HasOne(e => e.Account)
                    .WithMany(f => f.AccountSuspension)
                    .HasForeignKey(e => e.Id)
                    .HasConstraintName("FK__account_suspension_account_id__account_id");
            });

            modelBuilder.Entity<PermissionModel>(entity =>
            {
                entity.ToTable("permission");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("varchar(64)")
                    .HasDefaultValue("");

                entity.HasData(
                    new PermissionModel
                    {
                        Id   = 1,
                        Name = "Category: Account"
                    },
                    new PermissionModel
                    {
                        Id   = 2,
                        Name = "Command: AccountCreate"
                    },
                    new PermissionModel
                    {
                        Id   = 3,
                        Name = "Command: AccountDelete"
                    },
                    new PermissionModel
                    {
                        Id   = 15,
                        Name = "Category: Achievement"
                    },
                    new PermissionModel
                    {
                        Id   = 17,
                        Name = "Command: AchievementUpdate"
                    },
                    new PermissionModel
                    {
                        Id   = 16,
                        Name = "Command: AchievementGrant"
                    },
                    new PermissionModel
                    {
                        Id   = 18,
                        Name = "Category: Broadcast"
                    },
                    new PermissionModel
                    {
                        Id   = 19,
                        Name = "Command: BroadcastMessage"
                    },
                    new PermissionModel
                    {
                        Id   = 20,
                        Name = "Category: Character"
                    },
                    new PermissionModel
                    {
                        Id   = 21,
                        Name = "Command: CharacterXP"
                    },
                    new PermissionModel
                    {
                        Id   = 22,
                        Name = "Command: CharacterLevel"
                    },
                    new PermissionModel
                    {
                        Id   = 5,
                        Name = "Command: CharacterSave"
                    },
                    new PermissionModel
                    {
                        Id   = 23,
                        Name = "Category: Currency"
                    },
                    new PermissionModel
                    {
                        Id   = 24,
                        Name = "Category: CurrencyAccount"
                    },
                    new PermissionModel
                    {
                        Id   = 25,
                        Name = "Command: CurrencyAccountAdd"
                    },
                    new PermissionModel
                    {
                        Id   = 26,
                        Name = "Command: CurrencyAccountList"
                    },
                    new PermissionModel
                    {
                        Id   = 27,
                        Name = "Category: CurrencyCharacter"
                    },
                    new PermissionModel
                    {
                        Id   = 28,
                        Name = "Command: CurrencyCharacterAdd"
                    },
                    new PermissionModel
                    {
                        Id   = 29,
                        Name = "Command: CurrencyCharacterList"
                    },
                    new PermissionModel
                    {
                        Id   = 30,
                        Name = "Category: Disable"
                    },
                    new PermissionModel
                    {
                        Id   = 31,
                        Name = "Command: DisableInfo"
                    },
                    new PermissionModel
                    {
                        Id   = 32,
                        Name = "Command: DisableReload"
                    },
                    new PermissionModel
                    {
                        Id   = 33,
                        Name = "Category: Door"
                    },
                    new PermissionModel
                    {
                        Id   = 34,
                        Name = "Command: DoorOpen"
                    },
                    new PermissionModel
                    {
                        Id   = 35,
                        Name = "Command: DoorClose"
                    },
                    new PermissionModel
                    {
                        Id   = 36,
                        Name = "Category: Entitlement"
                    },
                    new PermissionModel
                    {
                        Id   = 40,
                        Name = "Category: EntitlementAccount"
                    },
                    new PermissionModel
                    {
                        Id   = 41,
                        Name = "Command: EntitlementAdd"
                    },
                    new PermissionModel
                    {
                        Id   = 42,
                        Name = "Command: EntitlementAccountList"
                    },
                    new PermissionModel
                    {
                        Id   = 37,
                        Name = "Category: EntitlementCharacter"
                    },
                    new PermissionModel
                    {
                        Id   = 39,
                        Name = "Command: EntitlementCharacterList"
                    },
                    new PermissionModel
                    {
                        Id   = 43,
                        Name = "Category: Entity"
                    },
                    new PermissionModel
                    {
                        Id   = 44,
                        Name = "Command: EntityInfo"
                    },
                    new PermissionModel
                    {
                        Id   = 45,
                        Name = "Command: EntityProperties"
                    },
                    new PermissionModel
                    {
                        Id   = 60,
                        Name = "Category: EntityModify"
                    },
                    new PermissionModel
                    {
                        Id   = 61,
                        Name = "Command: EntityModifyDisplayInfo"
                    },
                    new PermissionModel
                    {
                        Id   = 46,
                        Name = "Category: Generic"
                    },
                    new PermissionModel
                    {
                        Id   = 47,
                        Name = "Command: GenericUnlock"
                    },
                    new PermissionModel
                    {
                        Id   = 48,
                        Name = "Command: GenericUnlockAll"
                    },
                    new PermissionModel
                    {
                        Id   = 49,
                        Name = "Command: GenericList"
                    },
                    new PermissionModel
                    {
                        Id   = 4,
                        Name = "Category: Help"
                    },
                    new PermissionModel
                    {
                        Id   = 54,
                        Name = "Category: House"
                    },
                    new PermissionModel
                    {
                        Id   = 58,
                        Name = "Command: HouseTeleport"
                    },
                    new PermissionModel
                    {
                        Id   = 55,
                        Name = "Category: HouseDecor"
                    },
                    new PermissionModel
                    {
                        Id   = 56,
                        Name = "Command: HouseDecorAdd"
                    },
                    new PermissionModel
                    {
                        Id   = 57,
                        Name = "Command: HouseDecorLookup"
                    },
                    new PermissionModel
                    {
                        Id   = 59,
                        Name = "Category: Item"
                    },
                    new PermissionModel
                    {
                        Id   = 6,
                        Name = "Command: ItemAdd"
                    },
                    new PermissionModel
                    {
                        Id   = 62,
                        Name = "Category: Movement"
                    },
                    new PermissionModel
                    {
                        Id   = 63,
                        Name = "Category: MovementSpline"
                    },
                    new PermissionModel
                    {
                        Id   = 64,
                        Name = "Command: MovementSplineAdd"
                    },
                    new PermissionModel
                    {
                        Id   = 65,
                        Name = "Command: MovementSplineClear"
                    },
                    new PermissionModel
                    {
                        Id   = 66,
                        Name = "Command: MovementSplineLaunch"
                    },
                    new PermissionModel
                    {
                        Id   = 67,
                        Name = "Category: MovementGenerator"
                    },
                    new PermissionModel
                    {
                        Id   = 68,
                        Name = "Command: MovementGeneratorDirect"
                    },
                    new PermissionModel
                    {
                        Id   = 69,
                        Name = "Command: MovementGeneratorRandom"
                    },
                    new PermissionModel
                    {
                        Id   = 70,
                        Name = "Category: Path"
                    },
                    new PermissionModel
                    {
                        Id   = 71,
                        Name = "Command: PathUnlock"
                    },
                    new PermissionModel
                    {
                        Id   = 72,
                        Name = "Command: PathActivate"
                    },
                    new PermissionModel
                    {
                        Id   = 73,
                        Name = "Command: PathXP"
                    },
                    new PermissionModel
                    {
                        Id   = 74,
                        Name = "Category: Pet"
                    },
                    new PermissionModel
                    {
                        Id   = 75,
                        Name = "Command: PetUnlockFlair"
                    },
                    new PermissionModel
                    {
                        Id   = 76,
                        Name = "Category: Quest"
                    },
                    new PermissionModel
                    {
                        Id   = 77,
                        Name = "Command: QuestAdd"
                    },
                    new PermissionModel
                    {
                        Id   = 78,
                        Name = "Command: QuestAchieve"
                    },
                    new PermissionModel
                    {
                        Id   = 79,
                        Name = "Command: QuestAchieveObjective"
                    },
                    new PermissionModel
                    {
                        Id   = 80,
                        Name = "Command: QuestObjective"
                    },
                    new PermissionModel
                    {
                        Id   = 81,
                        Name = "Command: QuestKill"
                    },
                    new PermissionModel
                    {
                        Id   = 7,
                        Name = "Category: RBAC"
                    },
                    new PermissionModel
                    {
                        Id   = 8,
                        Name = "Category: RBACAccount"
                    },
                    new PermissionModel
                    {
                        Id   = 9,
                        Name = "Category: RBACAccountPermission"
                    },
                    new PermissionModel
                    {
                        Id   = 10,
                        Name = "Command: RBACAccountPermissionGrant"
                    },
                    new PermissionModel
                    {
                        Id   = 11,
                        Name = "Command: RBACAccountPermissionRevoke"
                    },
                    new PermissionModel
                    {
                        Id   = 12,
                        Name = "Category: RBACAccountRole"
                    },
                    new PermissionModel
                    {
                        Id   = 13,
                        Name = "Command: RBACAccountRoleGrant"
                    },
                    new PermissionModel
                    {
                        Id   = 14,
                        Name = "Command: RBACAccountRoleRevoke"
                    },
                    new PermissionModel
                    {
                        Id   = 83,
                        Name = "Category: Spell"
                    },
                    new PermissionModel
                    {
                        Id   = 84,
                        Name = "Command: SpellAdd"
                    },
                    new PermissionModel
                    {
                        Id   = 85,
                        Name = "Command: SpellCast"
                    },
                    new PermissionModel
                    {
                        Id   = 86,
                        Name = "Command: SpellResetCooldown"
                    },
                    new PermissionModel
                    {
                        Id   = 50,
                        Name = "Category: Teleport"
                    },
                    new PermissionModel
                    {
                        Id   = 51,
                        Name = "Command: TeleportCoordinates"
                    },
                    new PermissionModel
                    {
                        Id   = 52,
                        Name = "Command: TeleportLocation"
                    },
                    new PermissionModel
                    {
                        Id   = 53,
                        Name = "Command: TeleportName"
                    },
                    new PermissionModel
                    {
                        Id   = 87,
                        Name = "Category: Title"
                    },
                    new PermissionModel
                    {
                        Id   = 88,
                        Name = "Command: TitleAdd"
                    },
                    new PermissionModel
                    {
                        Id   = 89,
                        Name = "Command: TitleRevoke"
                    },
                    new PermissionModel
                    {
                        Id   = 90,
                        Name = "Command: TitleAll"
                    },
                    new PermissionModel
                    {
                        Id   = 91,
                        Name = "Command: TitleNone"
                    },
                    new PermissionModel
                    {
                        Id   = 92,
                        Name = "Command: ItemLookup"
                    },
                    new PermissionModel
                    {
                        Id   = 116,
                        Name = "Command: ItemInfo"
                    },
                    new PermissionModel
                    {
                        Id   = 82,
                        Name = "Category: Realm"
                    },
                    new PermissionModel
                    {
                        Id   = 94,
                        Name = "Command: RealmMOTD"
                    },
                    new PermissionModel
                    {
                        Id   = 95,
                        Name = "Category: Story"
                    },
                    new PermissionModel
                    {
                        Id   = 96,
                        Name = "Command: StoryPanel"
                    },
                    new PermissionModel
                    {
                        Id   = 97,
                        Name = "Command: StoryCommunicator"
                    },
                    new PermissionModel
                    {
                        Id   = 98,
                        Name = "Category: Reputation"
                    },
                    new PermissionModel
                    {
                        Id   = 99,
                        Name = "Command: ReputationUpdate"
                    },
                    new PermissionModel
                    {
                        Id   = 100,
                        Name = "Category: Guild"
                    },
                    new PermissionModel
                    {
                        Id   = 101,
                        Name = "Command: GuildRegister"
                    },
                    new PermissionModel
                    {
                        Id   = 102,
                        Name = "Command: GuildJoin"
                    },
                    new PermissionModel
                    {
                        Id   = 103,
                        Name = "Category: Map"
                    },
                    new PermissionModel
                    {
                        Id   = 104,
                        Name = "Command: MapUnload"
                    },
                    new PermissionModel
                    {
                        Id   = 105,
                        Name = "Command: MapPlayerRemove"
                    },
                    new PermissionModel
                    {
                        Id   = 106,
                        Name = "Command: MapPlayerRemoveCancel"
                    },
                    new PermissionModel
                    {
                        Id   = 107,
                        Name = "Category: RealmShutdown"
                    },
                    new PermissionModel
                    {
                        Id   = 108,
                        Name = "Command: RealmShutdownStart"
                    },
                    new PermissionModel
                    {
                        Id   = 109,
                        Name = "Command: RealmShutdownCancel"
                    },
                    new PermissionModel
                    {
                        Id   = 110,
                        Name = "Command: QuestList"
                    },
                    new PermissionModel()
                    {
                        Id   = 111,
                        Name = "Command: RealmMaxPlayers"
                    },
                    new PermissionModel()
                    {
                        Id   = 112,
                        Name = "Category: Script"
                    },
                    new PermissionModel()
                    {
                        Id   = 113,
                        Name = "Command: ScriptReload"
                    },
                    new PermissionModel()
                    {
                        Id   = 114,
                        Name = "Command: ScriptInfo"
                    },
                    new PermissionModel()
                    {
                        Id   = 115,
                        Name = "Command: ScriptAdd"
                    },
                    new PermissionModel()
                    {
                        Id   = 117,
                        Name = "Category: Ban"
                    },
                    new PermissionModel()
                    {
                        Id   = 118,
                        Name = "Category: BanAccount"
                    },
                    new PermissionModel()
                    {
                        Id   = 119,
                        Name = "Command: BanAccountPlayer"
                    },
                    new PermissionModel()
                    {
                        Id   = 120,
                        Name = "Command: BanAccountCharacter"
                    },
                    new PermissionModel()
                    {
                        Id   = 121,
                        Name = "Category: EntityThreat"
                    },
                    new PermissionModel()
                    {
                        Id   = 122,
                        Name = "Command: EntityThreatAdjust"
                    },
                    new PermissionModel
                    {
                        Id   = 123,
                        Name = "Command: EntityThreatList"
                    },
                    new PermissionModel
                    {
                        Id   = 124,
                        Name = "Command: EntityThreatClear"
                    },
                    new PermissionModel
                    {
                        Id   = 125,
                        Name = "Command: EntityThreatRemove"
                    },
                    new PermissionModel
                    {
                        Id   = 10000,
                        Name = "Other: InstantLogout"
                    },
                    new PermissionModel
                    {
                        Id   = 10001,
                        Name = "Other: Signature"
                    },
                    new PermissionModel
                    {
                        Id   = 10002,
                        Name = "Other: BypassInstanceLimits"
                    },
                    new PermissionModel
                    {
                        Id   = 10003,
                        Name = "Other: GMFlag"
                    },
                    new PermissionModel
                    {
                        Id   = 10004,
                        Name = "Other: EntitlementGrantOther"
                    });
            });

            modelBuilder.Entity<RoleModel>(entity =>
            {
                entity.ToTable("role");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("varchar(64)")
                    .HasDefaultValue("");

                entity.Property(e => e.Flags)
                    .HasColumnName("flags")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.HasData(
                    new RoleModel
                    {
                        Id    = 1,
                        Name  = "Player",
                        Flags = 1
                    },
                    new RoleModel
                    {
                        Id    = 2,
                        Name  = "GameMaster",
                        Flags = 1
                    },
                    new RoleModel
                    {
                        Id    = 3,
                        Name  = "Administrator",
                        Flags = 2
                    },
                    new RoleModel
                    {
                        Id    = 4,
                        Name  = "Console",
                        Flags = 2
                    },
                    new RoleModel
                    {
                        Id    = 5,
                        Name  = "WebSocket",
                        Flags = 2
                    });
            });

            modelBuilder.Entity<RolePermissionModel>(entity =>
            {
                entity.ToTable("role_permission");

                entity.HasKey(i => new { i.Id, i.PermissionId });

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.Property(e => e.PermissionId)
                    .HasColumnName("permissionId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0);

                entity.HasOne(e => e.Role)
                    .WithMany(f => f.RolePermission)
                    .HasForeignKey(e => e.Id)
                    .HasConstraintName("FK__role_permission_id__role_id");

                entity.HasOne(e => e.Permission)
                    .WithMany(f => f.RolePermission)
                    .HasForeignKey(e => e.PermissionId)
                    .HasConstraintName("FK__role_permission_permission_id__permission_id");
            });

            modelBuilder.Entity<ServerModel>(entity =>
            {
                entity.ToTable("server");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("tinyint(3) unsigned")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Host)
                    .IsRequired()
                    .HasColumnName("host")
                    .HasColumnType("varchar(64)")
                    .HasDefaultValue("127.0.0.1");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar(64)")
                    .HasDefaultValue("NexusForever");

                entity.Property(e => e.Port)
                    .HasColumnName("port")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValue(24000);

                entity.Property(e => e.Type)
                    .HasColumnName("type")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue(0);

                entity.HasData(new ServerModel
                {
                    Id   = 1,
                    Host = "127.0.0.1",
                    Name = "NexusForever",
                    Port = 24000,
                    Type = 0
                });
            });

            modelBuilder.Entity<ServerMessageModel>(entity =>
            {
                entity.HasKey(e => new { e.Index, e.Language })
                    .HasName("PRIMARY");

                entity.ToTable("server_message");

                entity.Property(e => e.Index)
                    .HasColumnName("index")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue(0)
                    .ValueGeneratedNever();

                entity.Property(e => e.Language)
                    .HasColumnName("language")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue(0)
                    .ValueGeneratedNever();

                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasColumnName("message")
                    .HasColumnType("varchar(256)")
                    .HasDefaultValue("");

                entity.HasData(
                    new ServerMessageModel
                    {
                        Index    = 0,
                        Language = 0,
                        Message  = "Welcome to this NexusForever server!\nVisit: https://github.com/NexusForever/NexusForever"
                    },
                    new ServerMessageModel
                    {
                        Index    = 0,
                        Language = 1,
                        Message  = "Willkommen auf diesem NexusForever server!\nBesuch: https://github.com/NexusForever/NexusForever"
                    });
            });

            NexusForever.Database.EntityFramework.DatabaseModelBuilderExtensions.UseProviderCompatibility(modelBuilder, Database.ProviderName);
        }
    }
}
