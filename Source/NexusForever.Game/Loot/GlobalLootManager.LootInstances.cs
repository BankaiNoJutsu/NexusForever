using System;
using System.Collections.Generic;
using System.Linq;

namespace NexusForever.Game.Loot
{
    public partial class GlobalLootManager
    {
        private void RemoveExpiredLootInstances()
        {
            foreach (LootInstance lootInstance in lootInstances.Where(i => i.HasExpired).ToList())
                RemoveExpiredLootInstance(lootInstance, sendRemove: true);
        }

        public void RemoveLootForOwner(uint ownerUnitId, bool sendRemove = true)
        {
            if (!lootInstancesByOwnerUnit.TryGetValue(ownerUnitId, out List<LootInstance> ownerInstances))
                return;

            foreach (LootInstance lootInstance in ownerInstances.ToList())
            {
                if (sendRemove)
                    lootInstance.SendLootRemoveToAllLooters();

                RemoveLootInstance(lootInstance);
            }
        }

        private void RemoveExpiredLootInstance(LootInstance lootInstance, bool sendRemove)
        {
            if (!lootInstance.HasExpired)
                return;

            if (sendRemove)
                lootInstance.SendLootRemoveToAllLooters();

            RemoveLootInstance(lootInstance);
        }

        private void AddLootInstance(LootInstance lootInstance)
        {
            lootInstances.Add(lootInstance);

            if (!lootInstancesByOwnerUnit.TryGetValue(lootInstance.OwnerUnitId, out List<LootInstance> ownerInstances))
            {
                ownerInstances = [];
                lootInstancesByOwnerUnit.Add(lootInstance.OwnerUnitId, ownerInstances);
            }
            ownerInstances.Add(lootInstance);

            foreach (ulong looterId in lootInstance.LooterCharacterIds)
            {
                if (!lootInstancesByLooter.TryGetValue(looterId, out List<LootInstance> looterInstances))
                {
                    looterInstances = [];
                    lootInstancesByLooter.Add(looterId, looterInstances);
                }
                looterInstances.Add(lootInstance);
            }
        }

        private void RemoveLootInstance(LootInstance lootInstance)
        {
            if (!lootInstances.Remove(lootInstance))
                return;

            if (lootInstancesByOwnerUnit.TryGetValue(lootInstance.OwnerUnitId, out List<LootInstance> ownerInstances))
            {
                ownerInstances.Remove(lootInstance);
                if (ownerInstances.Count == 0)
                    lootInstancesByOwnerUnit.Remove(lootInstance.OwnerUnitId);
            }

            foreach (ulong looterId in lootInstance.LooterCharacterIds)
            {
                if (!lootInstancesByLooter.TryGetValue(looterId, out List<LootInstance> looterInstances))
                    continue;

                looterInstances.Remove(lootInstance);
                if (looterInstances.Count == 0)
                    lootInstancesByLooter.Remove(looterId);
            }
        }

        private IReadOnlyList<LootInstance> GetLootInstancesForOwner(uint ownerUnitId)
        {
            return lootInstancesByOwnerUnit.TryGetValue(ownerUnitId, out List<LootInstance> ownerInstances)
                ? ownerInstances
                : Array.Empty<LootInstance>();
        }

        private IReadOnlyList<LootInstance> GetLootInstancesForLooter(ulong characterId)
        {
            return lootInstancesByLooter.TryGetValue(characterId, out List<LootInstance> looterInstances)
                ? looterInstances
                : Array.Empty<LootInstance>();
        }
    }
}
