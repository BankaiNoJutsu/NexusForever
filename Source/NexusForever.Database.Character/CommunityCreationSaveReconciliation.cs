using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    // A community owns a residence graph as well as its guild graph. Ordinary
    // Guild creation receipts deliberately do not authorize these shared rows.
    public sealed record CommunityCreationSaveReconciliation(string Id, ulong Character, ulong Guild, ulong Residence, string Name)
        : CharacterSaveReconciliation(Id)
    {
        private Action<CharacterContext> parent, enrolled;
        private Func<EntityEntry, bool> exactRow;

        public CommunityCreationSaveReconciliation(string id, ulong character, ulong guild, ulong residence, string name,
            Action<CharacterContext> parent, Action<CharacterContext> enrolled, Func<EntityEntry, bool> row)
            : this(id, character, guild, residence, name)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
            this.enrolled = enrolled ?? throw new ArgumentNullException(nameof(enrolled));
            exactRow = row ?? throw new ArgumentNullException(nameof(row));
        }
        internal override string Kind => "community-creation-credits";
        internal override void Validate()
        {
            if (!Guid.TryParseExact(Id, "N", out var id) || id == Guid.Empty || Id != id.ToString("N")
                || Character == 0 || Guild == 0 || Residence == 0 || string.IsNullOrEmpty(Name)
                || parent == null || enrolled == null || exactRow == null)
                throw new InvalidOperationException("Paid Community creation requires its exact Character, guild and residence graphs.");
        }
        internal override void ValidateParent(CharacterContext context)
        {
            if (context.AtomicCharacterId != Character) throw new InvalidOperationException("Community creation changed its Character.");
            parent(context); context.CommunityCreationOwner = this;
        }
        internal bool Authorizes(EntityEntry entry)
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Unchanged)) return false;
            bool allowed = entry.Entity switch
            {
                GuildModel row => row.Id == Guild && row.Type == 7 && row.LeaderId == Character && row.Name == Name && row.DeleteTime == null,
                GuildRankModel row => row.Id == Guild && row.Index is 0 or 1 or 9,
                GuildMemberModel row => row.Id == Guild && row.CharacterId == Character && row.Rank == 0,
                ResidenceModel row => row.Id == Residence && row.OwnerId == null && row.GuildOwnerId == Guild && row.PropertyInfoId == 105 && row.Name == Name,
                ResidencePlotModel row => row.Id == Residence,
                _ => false
            };
            return allowed && exactRow(entry);
        }
        internal override void ValidateEnrolled(CharacterContext context)
        {
            enrolled(context);
            foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.Entity is
                GuildModel or GuildDataModel or GuildRankModel or GuildMemberModel or GuildAchievementModel
                or ResidenceModel or ResidencePlotModel or ResidenceDecor or ResidenceNeighborModel))
                if (!Authorizes(entry)) throw new InvalidOperationException("Community creation contains a foreign or unowned shared row.");
        }
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
        internal CommunityCreationSaveReconciliation CommunityCreationOwner { get; set; }
    }
}
