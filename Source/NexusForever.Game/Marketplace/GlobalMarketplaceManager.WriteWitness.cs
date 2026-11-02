using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        // A successful row count is not proof of the stored listing. Triggers
        // can rewrite an update or recreate a deleted row before commit.
        private static void ObserveListingWrite<TModel, TImage>(CharacterContext context, TModel model,
            bool deleted, Func<TModel, TImage> capture, Func<TImage> read)
            where TModel : class where TImage : class
        {
            context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
            context.ChangeTracker.DetectChanges();
            var entry = context.Entry(model);
            var tracked = capture(model);
            var target = deleted ? null : tracked;
            TImage original = null, expectedSql = null;
            EntityState state = EntityState.Detached;
            string[] columns = [];
            string database = GameItem.GetRewardDatabaseKey(context);
            CaptureTracking();
            expectedSql = state == EntityState.Added ? null : original;
            if (deleted ? state != EntityState.Deleted : state is not (EntityState.Added or EntityState.Modified or EntityState.Unchanged))
                throw new InvalidOperationException("Marketplace listing has no staged write intent.");

            EventHandler<SavedChangesEventArgs> flushed = (_, _) =>
            {
                // An implicit transaction has already passed its pre-commit
                // witness. A later writer cannot reclassify that committed save.
                if (context.Database.CurrentTransaction != null) VerifySql(target);
                CaptureTracking(); expectedSql = target;
            };
            context.SavedChanges += flushed;
            context.OnSaveSettled(_ => context.SavedChanges -= flushed);
            context.OnSaveValidation(phase =>
            {
                if (GameItem.GetRewardDatabaseKey(context) != database || context.HasSavepointRollback)
                    throw new InvalidOperationException("Marketplace listing lost its save route or transaction.");
                if (!Equals(capture(model), tracked))
                    throw new InvalidOperationException("Marketplace listing changed its captured tracked scalars.");
                if (phase == CharacterSaveValidationPhase.BeforeFlush)
                {
                    context.ChangeTracker.DetectChanges();
                    if (context.Database.AutoTransactionBehavior != AutoTransactionBehavior.Always
                        || entry.State != state || !Equals(capture((TModel)entry.OriginalValues.ToObject()), original)
                        || !entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).Order().SequenceEqual(columns))
                        throw new InvalidOperationException("Marketplace listing lost its staged state or write mask.");
                    VerifySql(expectedSql);
                }
                else
                {
                    if (context.Database.CurrentTransaction == null)
                        throw new InvalidOperationException("Marketplace listing postimage requires an active transaction.");
                    VerifySql(target);
                }
            });

            void CaptureTracking()
            {
                state = entry.State;
                original = capture((TModel)entry.OriginalValues.ToObject());
                columns = entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).Order().ToArray();
            }
            void VerifySql(TImage expected)
            {
                if (!Equals(read(), expected))
                    throw new InvalidOperationException("Marketplace listing SQL differs from its complete captured row.");
            }
        }
    }
}
