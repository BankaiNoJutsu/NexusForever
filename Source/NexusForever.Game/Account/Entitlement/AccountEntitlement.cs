using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Entitlement
{
    public sealed class AccountEntitlement : Game.Entitlement.Entitlement, IAccountEntitlement
    {
        private readonly IAccount account;
        private SaveRegistration pendingSave;
        private uint committedAmount;
        private ulong committedRevision;

        internal (uint Amount, ulong Revision) GetCommittedSnapshot()
        {
            lock (saveStateLock) return (committedAmount, committedRevision);
        }

        /// <summary>
        /// Create a new <see cref="IAccountEntitlement"/> from an existing database model.
        /// </summary>
        public AccountEntitlement(IAccount account, AccountEntitlementModel model, EntitlementEntry entry)
            : base(entry, model.Amount)
        {
            this.account = account;
            committedAmount = model.Amount;
        }

        /// <summary>
        /// Create a new <see cref="IAccountEntitlement"/> from supplied <see cref="EntitlementEntry"/> and value.
        /// </summary>
        public AccountEntitlement(IAccount account, EntitlementEntry entry, uint value)
            : base(entry, value)
        {
            this.account = account;
            saveMask |= SaveMask.Create;
        }

        public void Save(AuthContext context)
        {
            lock (saveStateLock)
            {
                if (pendingSave != null)
                {
                    if (!ReferenceEquals(pendingSave.Context, context))
                    {
                        context.RejectSaveUnit();
                        throw new InvalidOperationException("This account entitlement already belongs to another pending save unit.");
                    }
                    if (pendingSave.Revision == saveRevision) return;
                    if (pendingSave.Staged)
                    {
                        context.RejectSaveUnit();
                        throw new InvalidOperationException("Account entitlement changed after SQL staging; retry with a fresh save unit.");
                    }
                    pendingSave.Amount = amount;
                    pendingSave.Revision = saveRevision;
                    return;
                }
                if (saveMask == SaveMask.None) return;
                var registration = new SaveRegistration(context, amount, saveRevision);
                pendingSave = registration;
                try
                {
                    context.EnlistSaveWork(unit =>
                    {
                        lock (saveStateLock)
                        {
                            if (registration.Model != null)
                                unit.Entry(registration.Model).State = EntityState.Detached;
                            unit.ReservePendingItemIds(account.Id, 0);
                            bool mysql = unit.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true;
                            AccountEntitlementModel model = (mysql
                                ? unit.AccountEntitlement.FromSqlInterpolated($"SELECT * FROM account_entitlement WHERE id = {account.Id} AND entitlementId = {(byte)Type} FOR UPDATE")
                                : unit.AccountEntitlement.Where(row => row.Id == account.Id && row.EntitlementId == (byte)Type)).AsNoTracking().SingleOrDefault();
                            if (model == null)
                            {
                                model = new AccountEntitlementModel { Id = account.Id, EntitlementId = (byte)Type, Amount = registration.Amount };
                                unit.AccountEntitlement.Add(model);
                            }
                            else
                            {
                                unit.Attach(model);
                                model.Amount = registration.Amount;
                            }
                            registration.Model = model;
                            registration.Staged = true;
                        }
                    }, committed =>
                    {
                        bool notify = false;
                        lock (saveStateLock)
                        {
                            if (!ReferenceEquals(pendingSave, registration)) return;
                            pendingSave = null;
                            if (registration.Model != null)
                            {
                                try { context.Entry(registration.Model).State = EntityState.Detached; }
                                catch (ObjectDisposedException) { }
                            }
                            if (committed)
                            {
                                committedAmount = registration.Amount;
                                committedRevision = registration.Revision;
                                notify = Type == EntitlementType.LoyaltyChallengeBonus;
                                saveMask &= ~SaveMask.Create;
                                if (saveRevision == registration.Revision)
                                    saveMask &= ~SaveMask.Amount;
                            }
                        }
                        if (notify)
                        {
                            // Notification failure must not turn an acknowledged Auth commit
                            // into a failed redemption, nor prevent other terminal callbacks.
                            try { account.RewardPropertyManager?.RefreshCommittedEntitlement(this, registration.Amount, registration.Revision); }
                            catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Committed account entitlement reward-property refresh failed."); }
                        }
                    });
                }
                catch
                {
                    pendingSave = null;
                    context.RejectSaveUnit();
                    throw;
                }
            }
        }

        private sealed class SaveRegistration(AuthContext context, uint amount, ulong revision)
        {
            public AuthContext Context { get; } = context;
            public uint Amount { get; set; } = amount;
            public ulong Revision { get; set; } = revision;
            public bool Staged { get; set; }
            public AccountEntitlementModel Model { get; set; }
        }

        ServerAccountEntitlement INetworkBuildable<ServerAccountEntitlement>.Build()
        {
            return new ServerAccountEntitlement
            {
                Entitlement = Type,
                Count       = Amount
            };
        }
    }
}
