using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class SpellManager
    {
        private readonly Dictionary<uint, IReadOnlyCollection<ISpellPropertyInstance>> augmentationProperties = [];
        private readonly Dictionary<uint, HashSet<SpellCooldownModifier>> augmentationCooldowns = [];
        private double augmentationRetryTime;

        private void RefreshAugmentations()
        {
            if (!initialised)
                return;

            HashSet<uint> desired = ActiveActionSet < ActionSet.MaxActionSets
                ? actionSets[ActiveActionSet].Amps.Select(a => a.Entry.Spell4IdAugment)
                    .Where(id => AugmentationSpellRules.IsSupported(id, GetGameTableManager())).ToHashSet()
                : [];

            foreach (uint id in augmentationProperties.Keys.Where(id => !desired.Contains(id)).ToArray())
            {
                foreach (var instance in augmentationProperties[id])
                    player.RemoveSpellPropertyInstance(instance);
                augmentationProperties.Remove(id);
            }

            foreach (uint id in augmentationCooldowns.Keys.Where(id => !desired.Contains(id)).ToArray())
            {
                foreach (var instance in GetSpellCooldownModifiers().Where(m => m.Spell4Id == id))
                    if (augmentationCooldowns[id].Contains(instance))
                        RemoveSpellCooldownModifier(instance.EffectId);
                augmentationCooldowns.Remove(id);
            }

            if (!player.InWorld || !player.IsAlive)
                return;

            foreach (uint id in desired)
            {
                // A modifier from another source may have replaced our instance. Never
                // overwrite or claim that source, even when it uses the same spell.
                var existing = player.GetSpellPropertyInstances(id);
                var existingCooldowns = GetSpellCooldownModifiers().Where(m => m.Spell4Id == id).ToArray();
                if (augmentationCooldowns.TryGetValue(id, out var ownedCooldowns))
                    ownedCooldowns.IntersectWith(existingCooldowns);
                if (existing.Count != 0 || existingCooldowns.Length != 0)
                    continue;

                CastResult result = player.TryCastSpell(id, new SpellParameters
                {
                    PrimaryTargetId = player.Guid,
                    UserInitiatedSpellCast = false,
                    IgnoreGlobalCooldown = true,
                    ClientRequestSource = nameof(SpellManager) + ".Augmentation"
                });
                if (result == CastResult.Ok)
                {
                    augmentationProperties[id] = player.CaptureSpellPropertyInstances(id);
                    // Records are immutable but can be replaced with value-equal
                    // instances. Only the actual objects installed by this cast
                    // belong to this AMP, never a replacement from another owner.
                    augmentationCooldowns[id] = new HashSet<SpellCooldownModifier>(
                        GetSpellCooldownModifiers().Where(m => m.Spell4Id == id), ReferenceEqualityComparer.Instance);
                }
            }
        }

        private void UpdateAugmentations(double lastTick)
        {
            if (!initialised)
                return;
            augmentationRetryTime -= lastTick;
            if (augmentationRetryTime > 0d)
                return;
            augmentationRetryTime = 1d;
            RefreshAugmentations();
        }
    }
}
