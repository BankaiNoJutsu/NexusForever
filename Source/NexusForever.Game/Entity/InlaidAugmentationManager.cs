using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    public class InlaidAugmentationManager : IInlaidAugmentationManager
    {
        private readonly object gate = new();
        private readonly ulong characterId;
        private readonly SortedSet<ushort> unlocked = [];
        private readonly HashSet<ushort> persisted = [];

        /// <summary>
        /// Create a new <see cref="IInlaidAugmentationManager"/> from an existing database model.
        /// </summary>
        public InlaidAugmentationManager(CharacterModel characterModel)
        {
            characterId = characterModel.Id;
            foreach (CharacterInlaidAugmentationModel model in characterModel.InlaidAugmentation)
            {
                unlocked.Add(model.AugmentationId);
                persisted.Add(model.AugmentationId);
            }
        }

        public IReadOnlyList<ushort> Ids
        {
            get
            {
                lock (gate)
                    return unlocked.ToArray();
            }
        }

        public bool IsUnlocked(ushort augmentationId)
        {
            lock (gate)
                return unlocked.Contains(augmentationId);
        }

        public bool Unlock(ushort augmentationId)
        {
            if (augmentationId == 0)
                return false;

            lock (gate)
            {
                if (unlocked.Count >= IInlaidAugmentationManager.MaxIds && !unlocked.Contains(augmentationId))
                    return false;

                return unlocked.Add(augmentationId);
            }
        }

        public void Save(CharacterContext context)
        {
            ushort[] pending;
            lock (gate)
                pending = unlocked.Where(id => !persisted.Contains(id)).ToArray();

            foreach (ushort id in pending)
            {
                context.CharacterInlaidAugmentation.Add(new CharacterInlaidAugmentationModel
                {
                    Id             = characterId,
                    AugmentationId = id
                });
            }

            if (pending.Length == 0)
                return;

            // A row counts as persisted only once its save actually committed; a failed save leaves it pending for the next one.
            context.OnSaveSettled(success =>
            {
                if (!success)
                    return;

                lock (gate)
                    foreach (ushort id in pending)
                        persisted.Add(id);
            });
        }
    }
}
