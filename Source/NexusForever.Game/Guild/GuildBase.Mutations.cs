using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Guild;

namespace NexusForever.Game.Guild
{
    public abstract partial class GuildBase
    {
        private readonly object mutationGate = new();
        internal virtual object MutationGate => mutationGate;
        private GuildMutationPersistence mutationPersistence;
        protected bool HasMutationPersistence => mutationPersistence != null;
        protected bool OwnsMutationSave(CharacterContext context) => mutationPersistence?.Owns(context) == true;
        internal void RequireMutationEditable()
        {
            if (pendingDisband is { Publishing: true } or { Published: true })
                throw new InvalidOperationException("Guild cleanup has sealed its accepted mutation graph.");
        }
        private GuildMutationPersistence.Snapshot CaptureMutations()
        {
            foreach (var entry in ranks)
                if (entry.Value.GuildId != Id || entry.Value.Index != entry.Key)
                    throw new InvalidOperationException("Guild rank lost its original identity.");
            foreach (var entry in members.Where(value => !value.Value.PendingDelete))
                if (!ReferenceEquals(entry.Value.Guild, this) || !ranks.TryGetValue(entry.Value.Rank.Index, out IGuildRank rank)
                    || !ReferenceEquals(rank, entry.Value.Rank) || rank.PendingDelete)
                    throw new InvalidOperationException("Guild member lost its original guild/rank owner.");
            return new(IsDisbandAccepted ? null : LeaderId, (uint)Flags,
            ranks.Values.Where(row => !row.PendingDelete).OrderBy(row => row.Index)
                .Select(row => new GuildMutationPersistence.Rank(row.Index, row.Name, (uint)row.Permissions, row.BankPermissions, row.BankMoneyWithdrawlLimits, row.RepairLimit)).ToArray(),
            members.Values.Where(row => !row.PendingDelete).OrderBy(row => row.CharacterId)
                .Select(row => new GuildMutationPersistence.Member(row.CharacterId, row.Rank.Index, row.Note, row.CommunityPlotReservation)).ToArray(),
            this is Guild guild ? new(guild.MessageOfTheDay, guild.AdditionalInfo,
                (ushort)guild.Standard.BackgroundIcon.GuildStandardPartEntry.Id,
                (ushort)guild.Standard.ForegroundIcon.GuildStandardPartEntry.Id,
                (ushort)guild.Standard.ScanLines.GuildStandardPartEntry.Id) : null);
        }
        internal bool OwnsMutationNameWrite(CharacterContext context)
        {
            if (namePersistence?.Owns(context) == true) return true;
            if (pendingDisband == null) return false;
            lock (pendingDisband.Contexts) return pendingDisband.Contexts.Contains(context);
        }
        internal bool MatchesMutationTombstone(GuildModel row) => pendingDisband != null
            && row.DeleteTime == pendingDisband.DeletedAt && row.Name == null && row.LeaderId == null
            && row.OriginalName == pendingDisband.Name && row.OriginalLeaderId == pendingDisband.LeaderId;
        private void InitialiseMutations(GuildMutationPersistence.Snapshot baseline, string database)
        {
            mutationPersistence = new(this, MutationGate, baseline, database, CaptureMutations, AcknowledgeMutations);
            foreach (GuildRank rank in ranks.Values.OfType<GuildRank>()) rank.BindSaveOwner(this);
        }
        private void AcknowledgeMutations(GuildMutationPersistence.Snapshot before, GuildMutationPersistence.Snapshot target)
        {
            // Accepted disband already captured every retained member for its cleanup.
            if (!IsDisbandAccepted) CaptureMutationPublications(before, target);
            if (LeaderId == target.Leader) saveMask &= ~GuildBaseSaveMask.LeaderId;
            if ((uint)Flags == target.Flags) saveMask &= ~GuildBaseSaveMask.Flags;
            if (this is Guild guild && target.Data != null) guild.AcknowledgeMutationData(target.Data);
            foreach (var row in ranks.ToArray())
            {
                GuildMutationPersistence.Rank stored = target.Ranks.SingleOrDefault(value => value.Index == row.Key);
                if (stored == null)
                {
                    if (row.Value.PendingDelete) ranks.Remove(row.Key);
                }
                else if (row.Value is GuildRank rank) rank.AcknowledgeMutation(stored);
            }
            foreach (var row in members.ToArray())
            {
                GuildMutationPersistence.Member stored = target.Members.SingleOrDefault(value => value.Character == row.Key);
                if (stored == null)
                {
                    if (row.Value.PendingDelete) members.Remove(row.Key);
                }
                else if (row.Value is GuildMember member) member.AcknowledgeMutation(stored);
            }
        }
        internal bool SaveRankMutations(GuildRank rank, CharacterContext context)
        {
            if (mutationPersistence == null) return false;
            lock (MutationGate)
            {
                if (!ranks.TryGetValue(rank.Index, out IGuildRank current) || !ReferenceEquals(current, rank))
                {
                    var error = new InvalidOperationException("Guild rank lost its original collection owner.");
                    context.RejectSave(error); throw error;
                }
                Save(context); return true;
            }
        }
        internal bool SaveMemberMutations(GuildMember member, CharacterContext context)
        {
            if (mutationPersistence == null) return false;
            lock (MutationGate)
            {
                if (!members.TryGetValue(member.CharacterId, out IGuildMember current) || !ReferenceEquals(current, member))
                {
                    var error = new InvalidOperationException("Guild member lost its original collection owner.");
                    context.RejectSave(error); throw error;
                }
                Save(context); return true;
            }
        }
    }
}
