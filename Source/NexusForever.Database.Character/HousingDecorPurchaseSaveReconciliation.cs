using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public sealed class HousingDecorPurchaseConflictException(Exception cause)
        : InvalidOperationException("Housing decor purchase lost its captured SQL parent or permission.", cause);

    public sealed record HousingDecorPurchaseSaveReconciliation(string Id, ulong Character, ulong Residence,
        ulong? Owner, ulong? Guild, byte GuildType, ulong Decor)
        : CharacterSaveReconciliation(Id)
    {
        private Action<CharacterContext> parent;
        private Action<CharacterContext> enrolled;
        private Func<EntityEntry, bool> exactRow;
        public HousingDecorPurchaseSaveReconciliation(string id, ulong character, ulong residence, ulong? owner,
            ulong? guild, byte guildType, ulong decor, Action<CharacterContext> parent,
            Action<CharacterContext> enrolled, Func<EntityEntry, bool> row)
            : this(id, character, residence, owner, guild, guildType, decor)
        { this.parent = parent; this.enrolled = enrolled; exactRow = row; }
        internal override string Kind => "normal-housing-decor-purchase";
        internal override void Validate()
        {
            if (!Guid.TryParseExact(Id, "N", out var id) || id == Guid.Empty || Id != id.ToString("N")
                || Character == 0 || Residence == 0 || Decor == 0 || Owner.HasValue && Owner != Character
                || !Owner.HasValue && (!Guild.HasValue || Guild == 0)
                || parent == null || enrolled == null || exactRow == null)
                throw new InvalidOperationException("Housing decor requires its exact Character, residence and new decor.");
        }
        internal override void ValidateParent(CharacterContext context)
        {
            try
            {
                if (context.AtomicCharacterId != Character) throw new InvalidOperationException("Housing decor changed its Character.");
                ValidateSqlParents(context); parent(context); context.HousingDecorPurchaseOwner = this;
                context.OnSaveValidation(() => ValidateSqlParents(context));
            }
            catch (InvalidOperationException error) { throw new HousingDecorPurchaseConflictException(error); }
        }
        private void ValidateSqlParents(CharacterContext context)
        {
            var root = context.Residence.AsNoTracking().SingleOrDefault(row => row.Id == Residence);
            if (root == null || root.OwnerId != Owner || root.GuildOwnerId != Guild)
                throw new InvalidOperationException("Housing decor SQL differs from its captured root.");
            if (Owner.HasValue) return;
            var guild = context.Guild.AsNoTracking().SingleOrDefault(row => row.Id == Guild);
            var member = context.GuildMember.AsNoTracking().SingleOrDefault(row => row.Id == Guild && row.CharacterId == Character);
            var rank = member == null ? null : context.GuildRank.AsNoTracking().SingleOrDefault(row => row.Id == Guild && row.Index == member.Rank);
            const uint decorateCommunity = 0x40000000;
            if (guild == null || guild.Type != GuildType || guild.DeleteTime != null || rank == null
                || (rank.Permission & decorateCommunity) == 0)
                throw new InvalidOperationException("Housing decor lost its SQL community decoration permission.");
        }
        internal bool Authorizes(EntityEntry entry)
        {
            bool allowed = entry.Entity switch
            {
                ResidenceDecor row => row.Id == Residence && (row.DecorId == Decor && entry.State == EntityState.Added || entry.State == EntityState.Unchanged),
                ResidenceModel row => row.Id == Residence && entry.State == EntityState.Unchanged,
                ResidencePlotModel row => row.Id == Residence && entry.State == EntityState.Unchanged,
                ResidenceNeighborModel row => row.ResidenceId == Residence && entry.State == EntityState.Unchanged,
                _ => false
            };
            return allowed && exactRow(entry);
        }
        internal override void ValidateEnrolled(CharacterContext context) { enrolled(context); ValidateSqlParents(context); }
        internal override string ReadPayload(CharacterContext context, string id)
            => context.BankReconciliationReceipt.AsNoTracking().Where(row => row.Id == id).Select(row => row.Payload).SingleOrDefault();
        internal override object AddReceipt(CharacterContext context, string payload, DateTime created)
        {
            var row = new BankReconciliationReceiptModel { Id = Id, Payload = payload, CreatedUtc = created };
            context.BankReconciliationReceipt.Add(row); return row;
        }
        internal override CharacterReconciliationRolledBackException RolledBack(CharacterContext context, string database, Exception cause, bool completed)
            => new(context, database, Id, cause, completed);
    }
    public partial class CharacterContext
    {
        internal HousingDecorPurchaseSaveReconciliation HousingDecorPurchaseOwner { get; set; }
    }
}
