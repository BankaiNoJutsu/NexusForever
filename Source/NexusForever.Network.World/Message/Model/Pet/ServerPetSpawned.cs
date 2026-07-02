using NexusForever.Network.Message;
using NexusForever.Game.Static.Pet;

namespace NexusForever.Network.World.Message.Model.Pet
{
    /// <summary>
    /// Native reader <c>LAB_14008ce70</c> reads pet unit id, 18-bit spell id, 5-bit valid
    /// stances, and 5-bit current stance. Native apply path
    /// <c>Pet_ApplySpawnedPayload</c> @ <c>1403c08d0</c> caches the pet row and dispatches
    /// <c>PetSpawned</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerPetSpawned)]
    public class ServerPetSpawned : IWritable
    {
        public uint PetUnitId { get; set; } // Cached pet row key. Engineer bot command surfaces use 0 for all active bots.
        public uint SummoningSpell4Id { get; set; }
        public uint ValidStances { get; set; }
        public PetStance Stance { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PetUnitId);
            writer.Write(SummoningSpell4Id, 18u);
            writer.Write(ValidStances, 5u);
            writer.Write(PetStanceEncoding.ToWireMask(Stance), 5u);
        }
    }
}
