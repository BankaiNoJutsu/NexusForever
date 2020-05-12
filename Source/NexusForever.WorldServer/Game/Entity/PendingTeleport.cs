using System.Collections.Generic;
using System.Numerics;
using NexusForever.WorldServer.Game.Map;

namespace NexusForever.WorldServer.Game.Entity
{
    public class PendingTeleport
    {
        public MapInfo Info { get; }
        public Vector3 Vector { get; }
        public uint? VanityPetId { get; private set; }
        public List<Pet> Pets { get; } = new List<Pet>();

        public PendingTeleport(MapInfo info, Vector3 vector)
        {
            Info        = info;
            Vector      = vector;
        }

        public void AddPet(Pet pet)
        {
            Pets.Add(pet);
        }

        public void AddVanityPet(uint? petGuid)
        {
            VanityPetId = petGuid;
        }
    }
}
