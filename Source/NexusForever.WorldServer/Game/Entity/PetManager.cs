using NexusForever.Shared.GameTable.Model;
using NexusForever.WorldServer.Game.Entity.Static;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NexusForever.WorldServer.Game.Entity
{
    public class PetManager : IEnumerable<WorldEntity>
    {
        private Player owner;

        private List<uint> combatPetGuids = new List<uint>();
        private uint? vanityPetGuid;

        public PetManager(Player player)
        {
            owner = player;

            // TODO: Implement resummoning combat pets on new session.
        }

        public IEnumerable<uint> GetCombatPetGuids()
        {
            return combatPetGuids;
        }

        public IEnumerable<Pet> GetCombatPets()
        {
            foreach (uint guid in combatPetGuids)
            {
                Pet pet = owner.GetVisible<Pet>(guid);
                if (pet != null)
                    yield return pet;
            }
        }

        public VanityPet GetVanityPet()
        {
            if (vanityPetGuid == null)
                return null;

            VanityPet pet = owner.GetVisible<VanityPet>(vanityPetGuid.Value);
            return pet;
        }

        public void SummonPet(SummonedPetType petType, uint creature, uint castingId, Spell4Entry spellInfo, Spell4EffectsEntry effectsEntry)
        {
            switch (petType)
            {
                case SummonedPetType.CombatPet:
                    var pet = new Pet(owner, creature, castingId, spellInfo, effectsEntry);
                    owner.Map.EnqueueAdd(pet, pet.GetSpawnPosition(owner));
                    break;
                case SummonedPetType.VanityPet:
                    // enqueue removal of existing vanity pet if summoned
                    if (vanityPetGuid != null)
                    {
                        VanityPet oldVanityPet = owner.GetVisible<VanityPet>(vanityPetGuid.Value);
                        oldVanityPet?.RemoveFromMap();
                        vanityPetGuid = null;
                    }

                    var vanityPet = new VanityPet(owner, creature);
                    owner.Map.EnqueueAdd(vanityPet, owner.Position);
                    break;
                default:
                    break;
            }
        }

        public void AddPetGuid(SummonedPetType petType, uint guid)
        {
            switch (petType)
            {
                case SummonedPetType.CombatPet:
                    combatPetGuids.Add(guid);
                    break;
                case SummonedPetType.VanityPet:
                    if (vanityPetGuid != null)
                        throw new InvalidOperationException();

                    vanityPetGuid = guid;
                    break;
                default:
                    break;
            }
        }

        public void RemovePetGuid(SummonedPetType petType, WorldEntity entity)
        {
            switch (petType)
            {
                case SummonedPetType.CombatPet:
                    combatPetGuids.Remove(entity.Guid);
                    break;
                case SummonedPetType.VanityPet:
                    if (vanityPetGuid == null)
                        throw new InvalidOperationException();

                    vanityPetGuid = null;
                    break;
                default:
                    break;
            }
        }

        public void OnTeleport(PendingTeleport pendingTeleport)
        {
            // store vanity pet summoned before teleport so it can be summoned again after being added to the new map
            uint? vanityPetId = null;
            if (vanityPetGuid != null)
            {
                VanityPet pet = owner.GetVisible<VanityPet>(vanityPetGuid.Value);
                vanityPetId = pet?.Creature.Id;

                if (vanityPetId != null)
                    pendingTeleport.AddVanityPet(vanityPetId);
            }

            foreach (uint guid in combatPetGuids)
            {
                Pet pet = owner.GetVisible<Pet>(guid);
                if (pet == null)
                    throw new InvalidOperationException();

                pendingTeleport.AddPet(pet);
                pet.RemoveFromMap();
            }
            combatPetGuids.Clear();
        }

        public void OnAddToMap(PendingTeleport pendingTeleport)
        {
            if (pendingTeleport == null)
                return;

            // resummon vanity pet if it existed before teleport
            if (pendingTeleport?.VanityPetId != null)
            {
                var vanityPet = new VanityPet(owner, pendingTeleport.VanityPetId.Value);
                owner.Map.EnqueueAdd(vanityPet, owner.Position);
            }

            if (pendingTeleport != null)
                foreach (Pet pet in pendingTeleport.Pets)
                {
                    pet.SetOwnerGuid(owner.Guid);
                    owner.Map.EnqueueAdd(pet, pet.GetSpawnPosition(owner));
                }
        }

        public void OnRemoveFromMap()
        {
            // enqueue removal of existing vanity pet if summoned
            if (vanityPetGuid != null)
            {
                VanityPet pet = owner.GetVisible<VanityPet>(vanityPetGuid.Value);
                pet?.RemoveFromMap();
                vanityPetGuid = null;
            }
        }

        public void DismissPets()
        {
            List<Pet> pets = GetCombatPets().ToList();
            if (pets.Count == 0)
                return;

            pets.Reverse();
            foreach (Pet pet in pets)
            {
                pet.RemoveFromMap();

                owner.EnqueueToVisible(new NexusForever.WorldServer.Network.Message.Model.ServerSpellFinish
                {
                    ServerUniqueId = pet.CastingId,
                }, true);
            }
        }

        public IEnumerator<WorldEntity> GetEnumerator()
        {
            List<WorldEntity> worldEntities = new List<WorldEntity>();

            foreach (Pet pet in GetCombatPets())
                worldEntities.Add(pet as WorldEntity);

            worldEntities.Add(GetVanityPet() as WorldEntity);

            return worldEntities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
