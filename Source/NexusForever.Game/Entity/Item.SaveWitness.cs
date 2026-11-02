using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Entity
{
    public partial class Item
    {
        // A copied enrolled target is independent of later actor mutations.
        // The owning SaveUnit alone acknowledges Item revisions.
        internal class SaveWitness(Item owner, CharacterContext context, SaveUnit unit,
            ItemModel target, ItemModel model, string label = "Item")
        {
            private ItemModel original = model == null ? null : CopyModel((ItemModel)context.Entry(model).OriginalValues.ToObject());
            private EntityState? stagedState = model == null ? null : context.Entry(model).State;
            private readonly ItemModel deletion = target == null ? CopyModel(model) : null;
            private ItemModel expectedSql = model == null ? CopyModel(target)
                : context.Entry(model).State is EntityState.Added or EntityState.Detached ? null
                : CopyModel((ItemModel)context.Entry(model).OriginalValues.ToObject());

            internal void Validate(CharacterSaveValidationPhase phase)
            {
                if (phase != CharacterSaveValidationPhase.BeforeFlush)
                {
                    var actual = context.Item.AsNoTracking().SingleOrDefault(row => row.Id == owner.Guid);
                    if (!SameState(actual, target) || target != null && context.RealmBankItem.AsNoTracking().Any(row => row.Id == owner.Guid))
                        throw new InvalidOperationException($"{label} SQL differs from its captured storage witness.");
                    return;
                }
                lock (owner.persistenceGate)
                {
                    if (unit != null && !ReferenceEquals(owner.activeSave, unit))
                        throw new InvalidOperationException($"{label} lost its enrolled save owner.");
                    if (!SameState(context.Item.AsNoTracking().SingleOrDefault(row => row.Id == owner.Guid), expectedSql))
                        throw new InvalidOperationException($"{label} stored row changed after its enrollment or last flush.");
                    var entry = context.ChangeTracker.Entries<ItemModel>().SingleOrDefault(e => e.Entity.Id == owner.Guid);
                    if (target == null)
                    {
                        if (entry != null && (entry.State is not (EntityState.Deleted or EntityState.Detached)
                            || model != null && !ReferenceEquals(entry.Entity, model)))
                            throw new InvalidOperationException($"{label} deletion lost its tracked state.");
                        if (model != null)
                        {
                            var held = context.Entry(model);
                            if (held.State is not (EntityState.Deleted or EntityState.Detached)
                                || !SameState(model, deletion) || !SameState((ItemModel)held.OriginalValues.ToObject(), original))
                                throw new InvalidOperationException($"{label} deletion snapshot changed outside its enrolled owner.");
                        }
                        return;
                    }
                    if (entry == null)
                    {
                        if (model != null) throw new InvalidOperationException($"{label} lost its tracked snapshot.");
                        return; // a clean aggregate-observed Item can remain untracked
                    }
                    if (model != null && !ReferenceEquals(entry.Entity, model) || !SameState(entry.Entity, target)
                        || entry.State is EntityState.Deleted or EntityState.Detached
                        || model != null && (entry.State != stagedState || !SameState((ItemModel)entry.OriginalValues.ToObject(), original)))
                        throw new InvalidOperationException($"{label} snapshot changed outside its enrolled owner.");
                }
            }

            internal void ObserveFlush()
            {
                expectedSql = CopyModel(target);
                if (model == null) return;
                original = CopyModel((ItemModel)context.Entry(model).OriginalValues.ToObject());
                stagedState = context.Entry(model).State;
            }
        }
    }
}
