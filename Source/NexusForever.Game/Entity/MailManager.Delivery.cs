using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Shared.Game;
using NexusForever.Shared.Game.Events;
using NLog;

namespace NexusForever.Game.Entity
{
    public partial class MailManager
    {
        private static readonly ILogger deliveryLog = LogManager.GetCurrentClassLogger();
        private readonly UpdateTimer deliveryTimer = new(TimeSpan.FromSeconds(30));
        private readonly HashSet<ulong> knownMail = [];
        private Func<CharacterContext> deliveryContexts;
        private IGameSession deliverySession;
        private IAccount deliveryAccount;
        private IInventory deliveryInventory;
        private uint deliveryAccountId;
        private ulong deliveryCharacter;
        private ushort deliveryRealm;
        private bool deliveryLoading;

        private bool DeliveryActor() => deliverySession != null && ReferenceEquals(player.MailManager, this) && ReferenceEquals(player.Session, deliverySession)
            && ReferenceEquals(player.Account, deliveryAccount) && ReferenceEquals(deliveryAccount?.Session, deliverySession)
            && deliveryAccount?.Id == deliveryAccountId && ReferenceEquals(player.Inventory, deliveryInventory)
            && player.CharacterId == deliveryCharacter && player.Identity?.RealmId == deliveryRealm
            && deliverySession is not NetworkSession { IsDisconnecting: true }
            && player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

        private void InitializeDelivery(Func<CharacterContext> contexts)
        {
            deliveryContexts = contexts;
            deliverySession = player.Session; deliveryAccount = player.Account;
            deliveryInventory = player.Inventory; deliveryAccountId = deliveryAccount?.Id ?? 0;
            deliveryCharacter = player.CharacterId; deliveryRealm = player.Identity?.RealmId ?? realmId;
        }

        private void NotifyCommittedMail(MailItem mail, uint account)
        {
            var recipient = mail.RecipientId == player.CharacterId ? player : playerManager?.GetPlayer(mail.RecipientId);
            if (recipient?.MailManager is not MailManager manager || manager.deliveryAccountId != account
                || manager.deliveryCharacter != mail.RecipientId || manager.deliveryRealm != deliveryRealm
                || !manager.DeliveryActor())
            {
                mail.CreationOwner?.Delivered();
                return;
            }
            var outgoing = mail.OutgoingDelivery;
            manager.deliverySession.Events.EnqueueThreadSafe(new PredicateEvent(
                () => !manager.DeliveryActor() || outgoing?.IsValid == false || outgoing?.IsReady != false, () =>
            {
                if (manager.DeliveryActor() && manager.deliveryAccountId == account && outgoing?.IsValid != false
                    && outgoing?.IsReady != false) manager.EnqueueMail(mail);
            }));
        }

        private void UpdateDelivery(double lastTick)
        {
            if (deliveryContexts == null || deliveryLoading || !DeliveryActor()) return;
            deliveryTimer.Update(lastTick);
            if (!deliveryTimer.HasElapsed) return;
            deliveryTimer.Reset(); deliveryLoading = true;
            ulong[] known = knownMail.ToArray();
            var load = Task.Run(async () =>
            {
                await using var driver = deliveryContexts();
                string database = Item.GetRewardDatabaseKey(driver);
                return await driver.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    // A retry owns a fresh transaction and tracking state; only
                    // its committed result can reach the original actor below.
                    await using var context = deliveryContexts();
                    if (Item.GetRewardDatabaseKey(context) != database)
                        throw new InvalidOperationException("Mail inbox changed its execution database.");
                    await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    var owner = await context.Character.AsNoTracking().SingleOrDefaultAsync(c => c.Id == deliveryCharacter);
                    if (owner == null || owner.AccountId != deliveryAccountId || owner.DeleteTime != null)
                        throw new InvalidOperationException("Mail inbox lost its Character/account owner.");
                    var rows = await context.CharacterMail.AsNoTracking().Where(m => m.RecipientId == deliveryCharacter && !known.Contains(m.Id))
                        .OrderBy(m => m.CreateTime).Take(128).Include(m => m.Attachment).ThenInclude(a => a.Item).ToArrayAsync();
                    await transaction.CommitAsync();
                    return (database, rows);
                });
            });
            deliverySession.Events.EnqueueEvent(new TaskEvent(load, () =>
            {
                deliveryLoading = false;
                if (!DeliveryActor()) return;
                foreach (CharacterMailModel row in load.Result.rows)
                {
                    if (knownMail.Contains(row.Id)) continue;
                    // A still-owned local send may have later dirty Item intent.
                    // Its original objects may cross only after actor adoption.
                    if (!ResolveOutgoing(load.Result.database, row, out MailItem original)) continue;
                    if (original == null && !BankMailDelivery.Resolve(load.Result.database, row, out original)) continue;
                    var mail = original ?? new MailItem(row, realmId, itemManager, gameTableManager, sharedConfiguration);
                    if (IsExpired(mail))
                    {
                        if (!TryAcceptMail(mail)) continue;
                        knownMail.Add(mail.Id); ExpireMail(mail);
                    }
                    else EnqueueMail(mail);
                }
            }, () =>
            {
                deliveryLoading = false;
                deliveryLog.Warn(load.Exception, "Committed mail inbox refresh failed; retrying on a later actor tick.");
            }));
        }
    }
}
