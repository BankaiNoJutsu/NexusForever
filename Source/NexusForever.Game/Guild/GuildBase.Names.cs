using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Abstract.Guild;

namespace NexusForever.Game.Guild
{
    public partial class GuildBase
    {
        internal IGlobalGuildManager NameRegistry => globalGuildManager;
        internal bool NameUnitActive => namePersistence?.HasActiveUnit == true;
        internal void SavePaidName(CharacterContext context, string target) => namePersistence.Save(context, target);
        internal void AdoptPaidName(string target)
        {
            Name = target;
            saveMask &= ~GuildBaseSaveMask.Name;
        }
        protected void RequireNameSaveDatabase(CharacterContext context) => namePersistence?.RequireDatabase(context);

        private void ObserveCreatedName(CharacterContext context)
        {
            var entry = context.ChangeTracker.Entries<GuildModel>().Single(row => row.Entity.Id == Id);
            ulong guildId = Id;
            byte guildType = (byte)Type;
            string target = entry.Entity.Name;
            string database = CharacterDatabaseIdentity.Get(context);
            var guard = new CharacterColumnSaveIntent(entry,
                [nameof(GuildModel.Id), nameof(GuildModel.Name), nameof(GuildModel.Type), nameof(GuildModel.DeleteTime)], true, "Created guild name");
            guard.Capture();
            bool MatchesSql()
            {
                GuildModel row = context.Guild.AsNoTracking().SingleOrDefault(row => row.Id == guildId);
                return row != null && row.Type == guildType && row.DeleteTime == null && StringComparer.Ordinal.Equals(row.Name, target);
            }
            bool flushed = false;
            GuildMutationPersistence.Snapshot createdMutations = null;
            EventHandler<SavedChangesEventArgs> onFlushed = (_, _) =>
            {
                if (context.HasSavepointRollback) return;
                if (!MatchesSql()) throw new InvalidOperationException("Created guild name did not flush its captured value.");
                createdMutations = GuildMutationPersistence.Read(context, guildId, guildType);
                flushed = true; guard.Capture(); guard.Flushed = true;
            };
            context.SavedChanges += onFlushed;
            context.OnSaveCompleted(committed =>
            {
                context.SavedChanges -= onFlushed;
                if (!committed || !flushed || Id != guildId || (byte)Type != guildType) return;
                namePersistence ??= new GuildNamePersistence(this, target, database, savedName => globalGuildManager?.GuildNameSaved(this, savedName));
                if (mutationPersistence == null)
                    InitialiseMutations(createdMutations, database);
                globalGuildManager?.GuildNameSaved(this, target);
            });
            context.OnSaveValidation(phase =>
            {
                if (context.HasSavepointRollback) return;
                CharacterDatabaseIdentity.Require(context, database);
                if (Id != guildId || (byte)Type != guildType || !context.ChangeTracker.Entries<GuildModel>().Any(row => ReferenceEquals(row.Entity, entry.Entity)))
                    throw new InvalidOperationException("Created guild name lost its original tracked owner.");
                context.ChangeTracker.DetectChanges(); guard.Validate();
                if (phase != CharacterSaveValidationPhase.BeforeFlush && !MatchesSql())
                    throw new InvalidOperationException("Created guild name SQL differs from its captured value.");
            });
        }
    }
}
