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
        private readonly Dictionary<uint, IReadOnlyCollection<ISpellProcInstance>> augmentationProcs = [];
        private readonly HashSet<uint> augmentationLifetimes = [];
        private double augmentationRetryTime;
        private bool refreshingAugmentations;

        private HashSet<uint> GetDesiredAugmentations()
            => ActiveActionSet < ActionSet.MaxActionSets
                ? actionSets[ActiveActionSet].Amps.Select(a => a.Entry.Spell4IdAugment)
                    .Where(id => AugmentationSpellRules.IsSupported(id, GetGameTableManager())).ToHashSet()
                : [];

        private void RetireAugmentations(HashSet<uint> desired)
        {
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

            foreach (uint id in augmentationProcs.Keys.Where(id => !desired.Contains(id)).ToArray())
            {
                foreach (var instance in augmentationProcs[id])
                    player.RemoveProc(instance);
                augmentationProcs.Remove(id);
            }

            // A periodic AMP is a running spell; retiring the AMP ends it.
            foreach (uint id in augmentationLifetimes.Where(id => !desired.Contains(id)).ToArray())
            {
                player.EndSpellLifetimes(spell4Id => spell4Id == id);
                augmentationLifetimes.Remove(id);
            }
        }

        private void RefreshAugmentations()
        {
            if (!initialised || refreshingAugmentations)
                return;

            refreshingAugmentations = true;
            try
            {
                HashSet<uint> desired = GetDesiredAugmentations();
                RetireAugmentations(desired);
                if (!player.InWorld || !player.IsAlive)
                    return;

                foreach (uint id in desired)
                {
                    // A cast callback may have changed the active nodes/spec.
                    if (!player.InWorld || !player.IsAlive || !GetDesiredAugmentations().Contains(id))
                        continue;
                    // A modifier from another source may have replaced our instance. Never
                    // overwrite or claim that source, even when it uses the same spell.
                    var existing = player.GetSpellPropertyInstances(id);
                    var existingCooldowns = GetSpellCooldownModifiers().Where(m => m.Spell4Id == id).ToArray();
                    var existingProcs = player.CaptureSpellProcInstances(id) ?? [];
                    if (augmentationCooldowns.TryGetValue(id, out var ownedCooldowns))
                        ownedCooldowns.IntersectWith(existingCooldowns);
                    if (existing.Count != 0 || existingCooldowns.Length != 0 || existingProcs.Count != 0)
                        continue;
                    // A periodic AMP that is still running (it ends with the caster and is cast again here once alive).
                    if (player.HasSpellLifetime(id))
                        continue;

                    var parameters = new AugmentationSpellParameters
                    {
                        PrimaryTargetId = player.Guid,
                        UserInitiatedSpellCast = false,
                        IgnoreGlobalCooldown = true,
                        ClientRequestSource = nameof(SpellManager) + ".Augmentation"
                    };
                    CastResult result = player.TryCastSpell(id, parameters);
                    if (result == CastResult.Ok)
                    {
                        augmentationProperties[id] = player.CaptureSpellPropertyInstances(id);
                        // Only the actual objects installed by this cast belong to
                        // this AMP, never a later replacement from another source.
                        augmentationCooldowns[id] = new HashSet<SpellCooldownModifier>(
                            GetSpellCooldownModifiers().Where(m => m.Spell4Id == id), ReferenceEqualityComparer.Instance);
                        augmentationProcs[id] = parameters.ProcInstances.ToArray();
                        if (AugmentationSpellRules.IsPeriodic(id, GetGameTableManager()))
                            augmentationLifetimes.Add(id);
                    }
                }
            }
            finally
            {
                try { RetireAugmentations(GetDesiredAugmentations()); }
                finally { refreshingAugmentations = false; }
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
