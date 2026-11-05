using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NLog;

namespace NexusForever.Game.Entity
{
    internal sealed class AppearanceChangeSettlement : IHousingFeeOperation
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly Player player;
        private readonly AppearanceManager manager;
        private readonly AppearanceChangeState target;
        private readonly IAccount account;
        private readonly IGameSession session;
        private readonly EventQueue events;
        private readonly object creditWallet, accountWallet;
        private readonly Identity identity;
        private readonly ushort realm;
        private readonly ulong character, epoch, creditFee;
        private readonly uint accountId;
        private readonly bool useTokens;
        private readonly ulong fee;
        private readonly uint guid;
        private readonly object map;
        private readonly AppearanceTokenPayment tokens;
        private Player.HousingNameActor actor;
        private AppearanceChangeState before;
        private Task attempt;
        private List<Action> notices;
        private int cursor;
        private bool ended, advancing, reported;
        internal bool HasActor => actor != null;
        internal bool CanSave => actor?.CanSave == true && Owns();

        internal AppearanceChangeSettlement(Player player, AppearanceManager manager, AppearanceChangeState target, bool useTokens, ulong fee)
        {
            this.player = player; this.manager = manager; this.target = target;
            this.useTokens = useTokens; this.fee = fee;
            account = player.Account; accountId = account.Id; session = player.Session; events = session.Events;
            creditWallet = player.CurrencyManager; accountWallet = account.CurrencyManager;
            identity = player.Identity; realm = identity.RealmId;
            character = player.CharacterId; epoch = account.WorldSessionEpoch; guid = player.Guid; map = player.Map;
            creditFee = useTokens ? 0 : fee;
            if (character == 0 || account.Id == 0 || epoch == 0 || !ReferenceEquals(account.Session, session))
                throw new InvalidOperationException("Appearance requires its original admitted Character/account.");
            if (useTokens && fee != 0) tokens = new(player, target, fee, Owns);
        }
        private bool Owns() => !ended && manager.OwnsChange(this) && player.CharacterId == character
            && ReferenceEquals(player.Identity, identity) && identity.RealmId == realm
            && ReferenceEquals(player.CurrencyManager, creditWallet) && ReferenceEquals(account.CurrencyManager, accountWallet)
            && ReferenceEquals(player.AppearanceManager, manager) && ReferenceEquals(player.Account, account)
            && account.Id == accountId && account.WorldSessionEpoch == epoch && ReferenceEquals(player.Session, session)
            && ReferenceEquals(account.Session, session) && ReferenceEquals(session.Events, events);
        private bool CurrentRecipient() => Owns() && session is IPlayerSession current && ReferenceEquals(current.Player, player)
            && session is not NetworkSession { IsDisconnecting: true };

        internal void Advance()
        {
            if (ended || advancing) return;
            advancing = true;
            try { AdvanceCore(); }
            catch (Exception error)
            {
                if (!reported) { log.Error(error, "Appearance retains its original unsettled operation."); reported = true; }
            }
            finally
            {
                advancing = false;
                if (!ended)
                {
                    Task delay = Task.Delay(25);
                    events.EnqueueEvent(new PredicateEvent(() => delay.IsCompleted, Advance));
                }
            }
        }
        private void AdvanceCore()
        {
            if (actor == null)
            {
                if (!CurrentRecipient() || player.Guid != guid || !ReferenceEquals(player.Map, map)
                    || !(useTokens ? account.CurrencyManager.CanAfford(NexusForever.Game.Static.Account.AccountCurrencyType.ServiceToken, fee)
                        : player.CurrencyManager.CanAfford(CurrencyType.Credits, fee)))
                { tokens?.CancelBeforeAttempt(); manager.FinishPaidChange(this); ended = true; return; }
                if (!player.HousingNameCanBegin) return;
                before = manager.ConfirmedAppearance;
                actor = player.BeginHousingName(this, CurrencyType.Credits, creditFee);
                if (actor == null) return;
            }
            if (attempt != null)
            {
                if (!attempt.IsCompleted) return;
                var completed = attempt; attempt = null; completed.GetAwaiter().GetResult();
            }
            if (tokens != null && !actor.Primary && !tokens.ObserveCommitted()) return;
            if (notices == null) actor.RefreshPublicationReadiness();
            if (!actor.ReadyToPublish) { attempt = actor.Attempt(); return; }
            if (notices == null)
            {
                manager.ConfirmPaidChange(this, target, actor.CanPublish && CurrentRecipient());
                notices = new();
                foreach (Func<IWritable> packet in actor.BeginPublication().Concat(tokens?.CapturePublication() ?? []))
                    notices.Add(() => { if (actor.CanPublish && Owns()) session.EnqueueMessageEncrypted(packet()); });
                foreach (IPlayer recipient in player.CaptureAppearanceRecipients())
                {
                    var captured = recipient.Session; var recipientMap = recipient.Map; var recipientAccount = recipient.Account; var recipientEvents = captured.Events;
                    var recipientIdentity = recipient.Identity; var recipientRealm = recipientIdentity?.RealmId;
                    ulong recipientCharacter = recipient.CharacterId, recipientEpoch = recipientAccount.WorldSessionEpoch;
                    uint recipientGuid = recipient.Guid;
                    notices.Add(() =>
                    {
                        if (actor.CanPublish && CurrentRecipient() && player.Guid == guid && ReferenceEquals(player.Map, map)
                            && ReferenceEquals(captured.Events, recipientEvents) && ReferenceEquals(recipient.Identity, recipientIdentity)
                            && recipient.Identity?.RealmId == recipientRealm && ReferenceEquals(recipientMap, map) && recipient.Guid == recipientGuid && recipient.CharacterId == recipientCharacter
                            && ReferenceEquals(recipient.Account, recipientAccount) && recipientAccount.WorldSessionEpoch == recipientEpoch
                            && ReferenceEquals(recipient.Session, captured) && ReferenceEquals(recipientAccount.Session, captured)
                            && ReferenceEquals(recipient.Map, recipientMap)
                            && captured is IPlayerSession current && ReferenceEquals(current.Player, recipient)
                            && captured is not NetworkSession { IsDisconnecting: true })
                            captured.EnqueueMessageEncrypted(new ServerEntityBoneUpdate { UnitId = guid, Bones = target.Bones.Select(row => row.Value).ToList() });
                    });
                }
            }
            while (cursor < notices.Count) { notices[cursor](); cursor++; }
            actor.End(); manager.FinishPaidChange(this); ended = true;
        }
        void IHousingFeeOperation.PrepareEffects() { }
        void IHousingFeeOperation.BeforePrimarySave() => tokens?.Prepare();
        void IHousingFeeOperation.AfterPrimaryRollback() => tokens?.AfterRollback();
        void IHousingFeeOperation.ApplyCommittedEffects()
        {
            if (!Owns()) throw new InvalidOperationException("Appearance lost its original committed actor.");
            // Complete followup confirmation precedes live adoption.
        }
        CharacterSaveReconciliation IHousingFeeOperation.CreateReceipt(bool primary)
        {
            void Parent(CharacterContext context)
            {
                if (actor?.Owns() != true || !Owns()) throw new InvalidOperationException("Appearance changed its complete Character owner.");
                CharacterDatabaseIdentity.Require(context, actor.Database);
                if (AppearanceChangeState.Read(context, character).Hash != (primary ? before.Hash : target.Hash))
                    throw new InvalidOperationException("Appearance SQL differs from its retained original graph.");
            }
            void Enrolled(CharacterContext context)
            {
                if (primary) target.ValidateEnrolled(context, character);
            }
            return new AppearanceChangeSaveReconciliation(Guid.NewGuid().ToString("N"), character, accountId, epoch,
                target.Race, target.Sex, target.Hash, tokens?.Fee ?? 0, tokens?.AuthRoute, primary, Parent, Enrolled,
                tokens == null ? null : tokens.AuthorizeAsync, tokens == null ? null : tokens.RollbackAsync);
        }
        void IHousingFeeOperation.Stage(CharacterContext context) => StageAppearance(context);
        internal void StageAppearance(CharacterContext context)
        {
            if (!CanSave) throw new InvalidOperationException("Appearance has no original coupled enrollment.");
            target.Stage(context, character);
        }
    }

    public partial class Player
    {
        internal void AdoptConfirmedAppearanceHeader(byte confirmedRace, byte confirmedSex, bool publish)
        { race = (Race)confirmedRace; sex = (Sex)confirmedSex; if (publish) SetVisualEmit(true); }

        internal IPlayer[] CaptureAppearanceRecipients() => (!IsLoading ? new IPlayer[] { this } : [])
            .Concat(visibleEntities.Values.OfType<IPlayer>().Where(recipient => IsLoading || recipient.Guid != Guid)).ToArray();
    }
}
