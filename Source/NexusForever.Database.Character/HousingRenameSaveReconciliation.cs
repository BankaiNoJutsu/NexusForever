using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public sealed class HousingRenameConflictException(Exception cause)
        : InvalidOperationException("Housing rename lost its captured community or residence.", cause);

    public sealed record HousingRenameSaveReconciliation(string Id, ulong Character, ulong Guild, ulong Residence, byte GuildType, string Name)
        : CharacterSaveReconciliation(Id)
    {
        private Action<CharacterContext> validateParent;
        private Action<CharacterContext> validateEnrolled;
        private Func<EntityEntry, bool> exactRow;

        public HousingRenameSaveReconciliation(string id, ulong character, ulong guild, ulong residence, byte type, string name,
            Action<CharacterContext> parent, Action<CharacterContext> enrolled, Func<EntityEntry, bool> row)
            : this(id, character, guild, residence, type, name)
        {
            validateParent = parent ?? throw new ArgumentNullException(nameof(parent));
            validateEnrolled = enrolled ?? throw new ArgumentNullException(nameof(enrolled));
            exactRow = row ?? throw new ArgumentNullException(nameof(row));
        }
        internal override string Kind => "normal-housing-rename";
        internal override void Validate()
        {
            if (!Guid.TryParseExact(Id, "N", out var id) || id == Guid.Empty || Id != id.ToString("N")
                || Character == 0 || Guild == 0 || Residence == 0 || string.IsNullOrEmpty(Name)
                || validateParent == null || validateEnrolled == null || exactRow == null)
                throw new InvalidOperationException("Housing rename requires its exact retained Character/community/residence graph.");
        }
        internal override void ValidateParent(CharacterContext context)
        {
            try
            {
                if (context.AtomicCharacterId != Character) throw new InvalidOperationException("Housing rename changed its Character.");
                ValidateSqlParents(context);
                validateParent(context); context.HousingRenameOwner = this;
                context.OnSaveValidation(() => ValidateSqlParents(context));
            }
            catch (InvalidOperationException error) { throw new HousingRenameConflictException(error); }
        }
        internal bool Authorizes(EntityEntry entry)
        {
            // These are the only shared rows a paid community rename may add
            // to a Character save. The Game owner must also prove its exact
            // frozen row; a callback cannot confer general foreign ownership.
            bool allowed = entry.Entity switch
            {
                GuildModel row => row.Id == Guild && row.Type == GuildType && row.LeaderId == Character && row.DeleteTime == null
                    && entry.State == EntityState.Modified && entry.Properties.Where(p => p.IsModified).All(p => p.Metadata.Name == nameof(GuildModel.Name)),
                ResidenceModel row => row.Id == Residence && row.OwnerId == null && row.GuildOwnerId == Guild,
                ResidencePlotModel row => row.Id == Residence,
                ResidenceDecor row => row.Id == Residence,
                ResidenceNeighborModel row => row.ResidenceId == Residence,
                _ => false
            };
            return allowed && exactRow(entry);
        }
        private void ValidateSqlParents(CharacterContext context)
        {
            var guild = context.Guild.AsNoTracking().SingleOrDefault(row => row.Id == Guild);
            var residence = context.Residence.AsNoTracking().SingleOrDefault(row => row.Id == Residence);
            if (guild == null || guild.Type != GuildType || guild.LeaderId != Character || guild.DeleteTime != null
                || residence == null || residence.OwnerId != null || residence.GuildOwnerId != Guild)
                throw new HousingRenameConflictException(new InvalidOperationException("Housing rename requires its SQL leader and community-owned root."));
        }
        internal override void ValidateEnrolled(CharacterContext context)
        {
            validateEnrolled(context); ValidateSqlParents(context);
        }

        // Keep the existing physical ordinary-reconciliation receipt store.
        // Its rows contain only an ID, tagged full-graph payload and timestamp;
        // Kind separates housing from the older bank payloads without DDL or
        // changing any existing receipt. Foreign payloads cannot prove success.
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
        internal HousingRenameSaveReconciliation HousingRenameOwner { get; set; }
    }
}
