using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Shared.Game.Events;
using NetworkRank = NexusForever.Network.World.Message.Model.Guild.GuildRank;

namespace NexusForever.Game.Guild
{
    public abstract partial class GuildBase
    {
        private sealed class BankRankRecipient(IPlayer player, IGuildMember member)
        {
            internal readonly IPlayer Player = player;
            internal readonly IGuildMember Member = member;
            internal readonly IGameSession Session = player.Session;
            internal readonly EventQueue Events = player.Session.Events;
            internal readonly IAccount Account = player.Account;
            internal readonly ulong Epoch = player.Account.WorldSessionEpoch;
            internal readonly ulong Character = player.CharacterId;
            internal readonly ushort Realm = player.Identity.RealmId;
            internal readonly Queue<BankRankPublication> Pending = [];
            internal bool Scheduled, Sending;
        }
        private sealed record BankRankNotice(GuildResult Result, uint Rank, string Text);
        private sealed class BankRankPublication(GuildMutationPersistence.Rank[] ranks, BankRankNotice[] notices)
        {
            internal readonly GuildMutationPersistence.Rank[] Ranks = ranks;
            internal readonly BankRankNotice[] Notices = notices;
            internal int Cursor;
        }
        private sealed record BankRankRequest(long Sequence, BankRankNotice Notice, BankRankRecipient[] Recipients);
        private readonly List<BankRankRequest> bankRankRequests = [];
        private readonly List<BankRankRecipient> bankRankRecipients = [];
        private GuildMutationPersistence.Rank[] confirmedBankRanks = [];
        private long bankRankSequence;
        private ulong bankRankGuild;
        private ushort bankRankRealm;
        private GuildType bankRankType;

        private void InitialiseBankRankSettings(GuildMutationPersistence.Rank[] ranks)
        {
            confirmedBankRanks = ranks.ToArray(); bankRankGuild = Id; bankRankRealm = Identity.RealmId; bankRankType = Type;
        }

        [GuildOperationHandler(GuildOperation.BankTabPermissions)]
        private IGuildResultInfo GuildOperationBankPermissions(IGuildMember member, IPlayer player, ClientGuildOperation operation)
            => ApplyBankRankSettings(member, player, operation);
        [GuildOperationHandler(GuildOperation.WithdrawalLimitChange)]
        private IGuildResultInfo GuildOperationBankMoneyLimit(IGuildMember member, IPlayer player, ClientGuildOperation operation)
            => ApplyBankRankSettings(member, player, operation);
        [GuildOperationHandler(GuildOperation.RepairLimitChange)]
        private IGuildResultInfo GuildOperationBankRepairLimit(IGuildMember member, IPlayer player, ClientGuildOperation operation)
            => ApplyBankRankSettings(member, player, operation);

        private IGuildResultInfo ApplyBankRankSettings(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            if (Type is not (GuildType.Guild or GuildType.WarParty)) return new GuildResultInfo(GuildResult.NoBank);
            if (IsDisbandAccepted) return new GuildResultInfo(GuildResult.NotAGuild);
            if (!member.Rank.HasPermission(GuildRankPermission.EditLowerRankPermissions)) return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions);
            if (operation.Rank >= 10 || GetRank((byte)operation.Rank) is not { } rank) return new GuildResultInfo(GuildResult.InvalidRank);
            if (member.Rank.Index >= operation.Rank) return new GuildResultInfo(GuildResult.CanOnlyModifyLowerRanks);
            if (operation.Operation != GuildOperation.BankTabPermissions && operation.Data.UInt64Data > long.MaxValue)
                return new GuildResultInfo(GuildResult.UnableToProcess);
            RequireMutationEditable();
            PruneBankRankRecipients();
            BankRankRecipient requester = CaptureBankRankRecipient(player);
            if (requester == null) return new GuildResultInfo(GuildResult.UnableToProcess);
            var recipients = new List<BankRankRecipient> { requester };
            foreach (ulong character in onlineMembers)
                if (playerManager?.GetPlayer(character) is { } peer && CaptureBankRankRecipient(peer) is { } recipient && !recipients.Contains(recipient))
                    recipients.Add(recipient);
            // This is the native packed UInt64, including retained unknown bits.
            // A selector is not expanded into a guessed daily withdrawal count.
            if (operation.Operation == GuildOperation.BankTabPermissions) rank.BankPermissions = operation.Data.UInt64Data;
            else if (operation.Operation == GuildOperation.WithdrawalLimitChange) rank.BankMoneyWithdrawlLimits = operation.Data.UInt64Data;
            else rank.RepairLimit = operation.Data.UInt64Data;
            bankRankRequests.Add(new(++bankRankSequence, new(GuildResult.RankModified, rank.Index, ""), recipients.ToArray()));
            return new GuildResultInfo(GuildResult.Success);
        }

        private bool DeferBankRankAnnouncement(BankRankNotice notice = null)
        {
            lock (MutationGate)
            {
                if (!HasPendingBankRankSettings) return false;
                PruneBankRankRecipients();
                var recipients = bankRankRecipients.Where(OwnsBankRankRecipient).ToList();
                foreach (ulong character in onlineMembers)
                    if (playerManager?.GetPlayer(character) is { } player && CaptureBankRankRecipient(player) is { } recipient && !recipients.Contains(recipient))
                        recipients.Add(recipient);
                // A later ordinary rank operation must retain its own confirmed
                // readback instead of being lost behind an older bank packet.
                bankRankRequests.Add(new(++bankRankSequence, notice, recipients.ToArray()));
                return true;
            }
        }

        private BankRankRecipient CaptureBankRankRecipient(IPlayer player)
        {
            if (player.Account == null || player.Identity == null || player.Session?.Events == null
                || GetMember(player.CharacterId) is not { PendingDelete: false } member) return null;
            var recipient = bankRankRecipients.FirstOrDefault(value => ReferenceEquals(value.Player, player)
                && ReferenceEquals(value.Member, member) && ReferenceEquals(value.Session, player.Session)
                && ReferenceEquals(value.Account, player.Account) && value.Epoch == player.Account.WorldSessionEpoch)
                ?? new BankRankRecipient(player, member);
            if (!OwnsBankRankRecipient(recipient)) return null;
            if (!bankRankRecipients.Contains(recipient)) bankRankRecipients.Add(recipient);
            return recipient;
        }
        private bool OwnsBankRankRecipient(BankRankRecipient recipient)
            => Id == bankRankGuild && Type == bankRankType && Identity.RealmId == bankRankRealm && !IsDisbandAccepted
                && (globalGuildManager == null || ReferenceEquals(globalGuildManager.GetGuild(new Abstract.Identity { Id = bankRankGuild, RealmId = bankRankRealm }), this))
                && recipient.Player.CharacterId == recipient.Character && recipient.Player.Identity?.Id == recipient.Character
                && recipient.Player.Identity.RealmId == recipient.Realm && recipient.Realm == bankRankRealm
                && ReferenceEquals(GetMember(recipient.Character), recipient.Member) && !recipient.Member.PendingDelete
                && (playerManager == null || ReferenceEquals(playerManager.GetPlayer(recipient.Character), recipient.Player))
                && ReferenceEquals(recipient.Player.Session, recipient.Session) && ReferenceEquals(recipient.Session.Events, recipient.Events)
                && recipient.Session is IPlayerSession current && ReferenceEquals(current.Player, recipient.Player)
                && recipient.Session is not NetworkSession { IsDisconnecting: true }
                && ReferenceEquals(recipient.Player.Account, recipient.Account) && recipient.Account.WorldSessionEpoch == recipient.Epoch
                && ReferenceEquals(recipient.Account.Session, recipient.Session);
        private void PruneBankRankRecipients()
        {
            foreach (var recipient in bankRankRecipients.Where(value => !OwnsBankRankRecipient(value)).ToArray())
            {
                recipient.Pending.Clear(); bankRankRecipients.Remove(recipient);
            }
        }
        private bool HasPendingBankRankSettings
        {
            get { lock (MutationGate) return bankRankRequests.Count != 0 || bankRankRecipients.Any(value => value.Pending.Count != 0 && OwnsBankRankRecipient(value)); }
        }
        private bool TryGetConfirmedBankRanks(out List<NetworkRank> ranks)
        {
            lock (MutationGate)
            {
                ranks = HasPendingBankRankSettings ? BuildBankRanks(confirmedBankRanks) : null;
                return ranks != null;
            }
        }
        private static List<NetworkRank> BuildBankRanks(GuildMutationPersistence.Rank[] ranks)
        {
            var packets = new List<NetworkRank>();
            for (byte i = 0; i < 10; i++)
            {
                var rank = ranks.SingleOrDefault(value => value.Index == i);
                packets.Add(rank == null ? new() : new() { RankName = rank.Name, PermissionMask = (GuildRankPermission)rank.Permission,
                    BankWithdrawalPermissions = rank.Bank, MoneyWithdrawalLimit = rank.Money, RepairLimit = rank.Repair });
            }
            return packets;
        }
        private void AcknowledgeBankRankSettings(GuildMutationPersistence.Rank[] ranks, long sequence)
        {
            confirmedBankRanks = ranks.ToArray();
            var accepted = bankRankRequests.Where(value => value.Sequence <= sequence).ToArray();
            bankRankRequests.RemoveAll(value => value.Sequence <= sequence);
            foreach (var recipient in accepted.SelectMany(value => value.Recipients).Distinct())
            {
                if (!OwnsBankRankRecipient(recipient)) continue;
                var notices = accepted.Where(value => value.Recipients.Contains(recipient) && value.Notice != null).Select(value => value.Notice)
                    .GroupBy(value => (value.Result, value.Rank)).Select(group => group.Last()).ToArray();
                recipient.Pending.Enqueue(new(ranks.ToArray(), notices));
                ScheduleBankRankPublication(recipient, Task.CompletedTask);
            }
        }
        private void ScheduleBankRankPublication(BankRankRecipient recipient, Task delay)
        {
            lock (MutationGate)
            {
                if (!OwnsBankRankRecipient(recipient)) { recipient.Pending.Clear(); return; }
                if (recipient.Scheduled || recipient.Sending || recipient.Pending.Count == 0) return;
                recipient.Scheduled = true;
            }
            recipient.Events.EnqueueThreadSafe(new PredicateEvent(
                () => !OwnsBankRankRecipient(recipient) || delay.IsCompleted && mutationPersistence?.HasActiveUnit != true,
                () => PublishBankRankSettings(recipient)));
        }
        private void PublishBankRankSettings(BankRankRecipient recipient)
        {
            lock (MutationGate)
            {
                recipient.Scheduled = false;
                if (recipient.Sending) return;
                if (!OwnsBankRankRecipient(recipient)) { recipient.Pending.Clear(); return; }
                recipient.Sending = true;
            }
            bool retry = false;
            try
            {
                while (OwnsBankRankRecipient(recipient))
                {
                    BankRankPublication publication;
                    lock (MutationGate) if (!recipient.Pending.TryPeek(out publication)) return;
                    while (publication.Cursor <= publication.Notices.Length)
                    {
                        if (!OwnsBankRankRecipient(recipient)) return;
                        IWritable packet = publication.Cursor == 0
                            ? new ServerGuildRankChange { GuildIdentity = new() { Id = bankRankGuild, RealmId = bankRankRealm }, Ranks = BuildBankRanks(publication.Ranks) }
                            : new ServerGuildResult { GuildIdentity = new() { Id = bankRankGuild, RealmId = bankRankRealm },
                                Result = publication.Notices[publication.Cursor - 1].Result, ReferenceId = publication.Notices[publication.Cursor - 1].Rank,
                                TargetName = publication.Notices[publication.Cursor - 1].Text };
                        recipient.Session.EnqueueMessageEncrypted(packet); publication.Cursor++;
                    }
                    lock (MutationGate)
                        if (recipient.Pending.TryPeek(out var current) && ReferenceEquals(current, publication)) recipient.Pending.Dequeue();
                }
            }
            catch (Exception error) { retry = true; log.Warn(error, "Committed bank rank settings retain their original recipient and packet cursor."); }
            finally
            {
                lock (MutationGate) recipient.Sending = false;
                ScheduleBankRankPublication(recipient, retry ? Task.Delay(TimeSpan.FromSeconds(1)) : Task.CompletedTask);
            }
        }
    }
}
