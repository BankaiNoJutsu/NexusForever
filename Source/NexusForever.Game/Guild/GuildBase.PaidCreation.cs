using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.Game.Guild
{
    public abstract partial class GuildBase
    {
        private GuildCreationSettlement paidCreation;
        internal void RequireCreationAchievementEditable(Achievement.GuildAchievementManager manager)
        {
            if (paidCreation?.AllowsAchievementMutation(manager) == true) return;
            RequireMutationEditable();
        }
        internal bool TryRetainCreationGuildEffect(IPlayer source, Abstract.Achievement.IGuildAchievementManager manager, Action effect)
        {
            lock (MutationGate)
            {
                if (paidCreation == null) return false;
                paidCreation.RetainGuildEffect(source, manager, effect); return true;
            }
        }
        internal void PreparePaidCreation(IPlayer player, GuildCreationSettlement command)
        {
            lock (MutationGate)
            {
                if (!PendingCreate || paidCreation != null || MemberCount != 0 || LeaderId.HasValue)
                    throw new InvalidOperationException("Paid creation requires its untouched original guild.");
                LeaderId = player.CharacterId; AddMember(player, 0); paidCreation = command;
            }
        }
        internal void ReleasePaidCreation(GuildCreationSettlement command)
        {
            lock (MutationGate)
            {
                if (!ReferenceEquals(paidCreation, command) || PendingCreate)
                    throw new InvalidOperationException("Paid creation has no confirmed original guild graph.");
                paidCreation = null;
            }
        }
        internal IEnumerable<Func<IWritable>> CreationPackets(IPlayer player)
        {
            yield return () => new ServerGuildResult { Result = Static.Guild.GuildResult.YouCreated,
                GuildIdentity = Identity.ToNetworkIdentity(), TargetName = Name };
            yield return () => new ServerGuildJoin { GuildData = Build(), Self = GetMember(player.CharacterId).Build(),
                WithdrawlInfo = new(), DisplayThisGuildNameplate = player.GuildManager is GuildManager manager
                    && manager.PublishedAffiliation.Id == Id };
            yield return () => new ServerGuildRoster { GuildIdentity = Identity.ToNetworkIdentity(),
                GuildMembers = [GetMember(player.CharacterId).Build()], Done = true };
            yield return () => new ServerGuildMemberChange { GuildIdentity = Identity.ToNetworkIdentity(),
                GuildMember = GetMember(player.CharacterId).Build(), MemberCount = 1, OnlineMemberCount = 1 };
        }
    }
}
