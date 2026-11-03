using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Guild
{
    public partial class Community
    {
        private readonly HashSet<CharacterContext> renameSaveContexts = [];

        public override void RenameGuild(string name)
        {
            lock (residenceLifecycleLock)
            {
                base.RenameGuild(name);
                if (Residence != null)
                {
                    QueueResidenceChange(Residence, name);
                    Residence.Name = name;
                }
            }
        }

        private Func<bool> ObserveResidenceRenames(CharacterContext context, KeyValuePair<ulong, ResidenceChange>[] changes)
        {
            var renames = changes.Where(change => change.Value.Name != null).ToArray();
            if (renames.Length == 0) return () => true;
            var guards = renames.Select(change =>
            {
                var entry = context.ChangeTracker.Entries<ResidenceModel>().Single(row => row.Entity.Id == change.Key);
                var guard = new CharacterColumnSaveIntent(entry, [nameof(ResidenceModel.Id), nameof(ResidenceModel.Name)], true, "Community name");
                guard.Capture();
                return guard;
            }).ToArray();
            string database = CharacterDatabaseIdentity.Get(context);
            ResidenceModel[] stored = renames.Select(change => context.Residence.AsNoTracking().SingleOrDefault(row => row.Id == change.Key)).ToArray();
            bool[] sqlExists = stored.Select(row => row != null).ToArray();
            string[] sqlNames = stored.Select(row => row?.Name).ToArray();
            for (int index = 0; index < stored.Length; index++)
                if (!sqlExists[index] && guards[index].Entry.State != EntityState.Added)
                    throw new InvalidOperationException("Community rename lost its existing residence row.");
            bool MatchesSql(bool target)
            {
                for (int index = 0; index < renames.Length; index++)
                {
                    ulong id = renames[index].Key;
                    ResidenceModel row = context.Residence.AsNoTracking().SingleOrDefault(row => row.Id == id);
                    bool expected = target || sqlExists[index];
                    if ((row != null) != expected || expected && row.Name != (target ? renames[index].Value.Name : sqlNames[index]))
                        return false;
                }
                return true;
            }
            bool flushed = false;
            EventHandler<SavedChangesEventArgs> onFlushed = (_, _) =>
            {
                if (context.HasSavepointRollback) return;
                CharacterDatabaseIdentity.Require(context, database);
                if (!MatchesSql(true)) throw new InvalidOperationException("Community residence name did not flush its captured value.");
                flushed = true;
                sqlNames = renames.Select(change => change.Value.Name).ToArray();
                Array.Fill(sqlExists, true);
                foreach (var guard in guards) { guard.Capture(); guard.Flushed = true; }
            };
            renameSaveContexts.Add(context);
            context.SavedChanges += onFlushed;
            context.OnSaveCompleted(_ =>
            {
                lock (residenceLifecycleLock)
                {
                    context.SavedChanges -= onFlushed;
                    renameSaveContexts.Remove(context);
                }
            });
            context.OnSaveValidation(phase =>
            {
                if (context.HasSavepointRollback) return;
                CharacterDatabaseIdentity.Require(context, database);
                context.ChangeTracker.DetectChanges();
                foreach (var guard in guards)
                {
                    if (!context.ChangeTracker.Entries<ResidenceModel>().Any(row => ReferenceEquals(row.Entity, guard.Entry.Entity)))
                        throw new InvalidOperationException("Community name lost its tracked residence.");
                    guard.Validate();
                }
                if (!MatchesSql(phase != CharacterSaveValidationPhase.BeforeFlush))
                    throw new InvalidOperationException("Community residence name SQL differs from its captured save unit.");
            });
            return () => flushed;
        }
    }
}
