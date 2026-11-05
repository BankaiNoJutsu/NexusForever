using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public sealed record GuildCreationSaveReconciliation(string Id, ulong Character, ulong Guild, byte GuildType, string Name)
        : CharacterSaveReconciliation(Id)
    {
        private Action<CharacterContext> parent, enrolled;
        private Func<EntityEntry, bool> exactRow;

        public GuildCreationSaveReconciliation(string id, ulong character, ulong guild, byte type, string name,
            Action<CharacterContext> parent, Action<CharacterContext> enrolled, Func<EntityEntry, bool> row)
            : this(id, character, guild, type, name)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
            this.enrolled = enrolled ?? throw new ArgumentNullException(nameof(enrolled));
            exactRow = row ?? throw new ArgumentNullException(nameof(row));
        }
        internal override string Kind => "normal-guild-creation";
        internal override void Validate()
        {
            if (!Guid.TryParseExact(Id, "N", out var id) || id == Guid.Empty || Id != id.ToString("N")
                || Character == 0 || Guild == 0 || string.IsNullOrEmpty(Name) || parent == null || enrolled == null || exactRow == null)
                throw new InvalidOperationException("Paid guild creation requires its exact original Character and guild graph.");
        }
        internal override void ValidateParent(CharacterContext context)
        {
            if (context.AtomicCharacterId != Character) throw new InvalidOperationException("Paid guild creation changed its Character.");
            parent(context); context.GuildCreationOwner = this;
        }
        internal bool Authorizes(EntityEntry entry)
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Unchanged)) return false;
            bool allowed = entry.Entity switch
            {
                GuildModel row => row.Id == Guild && row.Type == GuildType && row.LeaderId == Character && row.Name == Name && row.DeleteTime == null,
                GuildDataModel row => row.Id == Guild,
                GuildRankModel row => row.Id == Guild && row.Index is 0 or 1 or 9,
                GuildMemberModel row => row.Id == Guild && row.CharacterId == Character && row.Rank == 0,
                _ => false
            };
            return allowed && exactRow(entry);
        }
        internal override void ValidateEnrolled(CharacterContext context)
        {
            enrolled(context);
            foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.Entity is
                GuildModel or GuildDataModel or GuildRankModel or GuildMemberModel or GuildAchievementModel))
                if (!Authorizes(entry)) throw new InvalidOperationException("Paid creation contains a foreign or unowned guild row.");
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
        internal GuildCreationSaveReconciliation GuildCreationOwner { get; set; }
    }
}
