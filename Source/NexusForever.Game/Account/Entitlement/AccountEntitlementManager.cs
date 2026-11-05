using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Entitlement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reward;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using Microsoft.EntityFrameworkCore;
using NexusForever.Network.Message;

namespace NexusForever.Game.Account.Entitlement
{
    public class AccountEntitlementManager : EntitlementManager<IAccountEntitlement>, IAccountEntitlementManager
    {
        private readonly IAccount account;
        private readonly IAssetManager assetManager;
        private readonly IGameTableManager gameTableManager;
        private CreddUsageReservation creddUsage;

        /// <summary>
        /// Create a new <see cref="IAccountEntitlementManager"/> from existing database model.
        /// </summary>
        public AccountEntitlementManager(IAccount account, AccountModel model, IAssetManager assetManager, IGameTableManager gameTableManager)
            : base(gameTableManager)
        {
            this.account       = account;
            this.assetManager = assetManager;
            this.gameTableManager = gameTableManager;

            foreach (AccountEntitlementModel entitlementModel in model.AccountEntitlement)
            {
                EntitlementEntry entry = gameTableManager.Entitlement?.GetEntry(entitlementModel.EntitlementId);
                if (entry == null)
                    throw new DatabaseDataException($"Account {model.Id} has invalid entitlement {entitlementModel.EntitlementId} stored!");

                var entitlement = new AccountEntitlement(account, entitlementModel, entry);
                entitlements.Add(entitlement.Type, entitlement);
            }
        }

        public void Save(AuthContext context)
        {
            foreach (IAccountEntitlement entitlement in entitlements.Values)
                entitlement.Save(context);
        }

        protected override bool CanUpdateEntitlement(EntitlementEntry entry, int value)
        {
            // only handle account entitlements in AccountEntitlementManager
            EntitlementFlags entitlementFlags = (EntitlementFlags)entry.Flags;
            if (entitlementFlags.HasFlag(EntitlementFlags.Character)
                || entry.Id == (uint)EntitlementType.CREDDUsage && creddUsage != null)
                return false;

            return base.CanUpdateEntitlement(entry, value);
        }

        protected override void UpdateEntitlement(EntitlementEntry entry, int value)
        {
            base.UpdateEntitlement(entry, value);
            UpdateRewardProperties(entry, value);
        }

        private void UpdateRewardProperties(EntitlementEntry entry, int value)
        {
            // some reward property premium modifier entries use an existing entitlement values rather than static values
            // make sure we update these when the entitlement changes
            foreach (RewardPropertyPremiumModifierEntry modifierEntry in (assetManager?.GetRewardPropertiesForTier(account.AccountTier) ?? Enumerable.Empty<RewardPropertyPremiumModifierEntry>())
                .Where(e => e.EntitlementIdModifierCount == entry.Id))
            {
                account.RewardPropertyManager.UpdateRewardProperty((RewardPropertyType)modifierEntry.RewardPropertyId, value, modifierEntry.RewardPropertyData);
            }
        }

        protected override IAccountEntitlement CreateEntitlement(EntitlementEntry type)
        {
            return new AccountEntitlement(account, type, 0);
        }

        protected override void SendEntitlement(IAccountEntitlement entitlement)
        {
            account.Session.EnqueueMessageEncrypted(entitlement.Build());
        }

        internal CreddUsageReservation CaptureCreddUsage()
        {
            var entry = gameTableManager.Entitlement?.GetEntry((uint)EntitlementType.CREDDUsage);
            var original = GetEntitlement(EntitlementType.CREDDUsage) as AccountEntitlement;
            if (entry == null || !CanIncreaseEntitlement(EntitlementType.CREDDUsage, 1)
                || original?.CanAdoptCreddUsage() == false || account.WorldSessionEpoch == 0)
                throw new InvalidOperationException("CREDD redemption requires an available usage entitlement.");
            return creddUsage = new(this, entry, original, account.WorldSessionEpoch);
        }

        internal sealed class CreddUsageReservation(AccountEntitlementManager manager, EntitlementEntry entry,
            AccountEntitlement original, ulong epoch) : IDisposable
        {
            internal uint Limit => entry.MaxCount;
            private AccountEntitlementModel stored;
            private AccountEntitlement adopted;
            private bool observed;

            internal async Task ReadAsync(Func<AuthContext> contexts)
            {
                uint accountId = manager.account.Id;
                await using var context = contexts();
                var owner = await context.Account.AsNoTracking()
                    .Where(row => row.Id == accountId && context.AccountWorldSession.Any(session => session.AccountId == row.Id && session.Epoch == epoch))
                    .Select(row => new { Usage = row.AccountEntitlement
                        .Where(value => value.EntitlementId == (byte)EntitlementType.CREDDUsage).ToArray() })
                    .SingleOrDefaultAsync();
                if (owner == null)
                    throw new InvalidOperationException("CREDD usage observation lost its admitted account.");
                stored = owner.Usage.SingleOrDefault();
                observed = true;
            }

            internal void Adopt()
            {
                if (!observed || !IsCurrent() || original?.CanAdoptCreddUsage() == false
                    || !ReferenceEquals(manager.GetEntitlement(EntitlementType.CREDDUsage), original))
                    throw new InvalidOperationException("CREDD usage lost its retained entitlement owner.");
                if (stored != null)
                {
                    adopted = new AccountEntitlement(manager.account, stored, entry);
                    manager.entitlements[EntitlementType.CREDDUsage] = adopted;
                }
                else if (original != null)
                    throw new InvalidOperationException("CREDD usage observation lost its stored row.");
            }

            internal void Publish(Action<IWritable> send)
            {
                if (!IsCurrent() || adopted == null)
                    throw new InvalidOperationException("CREDD usage lost its publication owner.");
                manager.UpdateRewardProperties(entry, 1);
                send(((IAccountEntitlement)adopted).Build());
            }

            private bool IsCurrent() => ReferenceEquals(manager.creddUsage, this)
                && ReferenceEquals(manager.account.EntitlementManager, manager) && manager.account.WorldSessionEpoch == epoch;

            public void Dispose()
            {
                if (ReferenceEquals(manager.creddUsage, this))
                    manager.creddUsage = null;
            }
        }
    }
}
