using NexusForever.Game.Static.Pet;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pet
{
    /// <summary>
    /// Native <c>Pet_SetStance_SendClientPetSetStance</c> (<c>14050a270</c>) sends
    /// this as pet unit id plus a mapped 5-bit stance value.
    /// </summary>
    [Message(GameMessageOpcode.ClientPetSetStance)]
    public class ClientPetSetStance : IReadable
    {
        public uint PetUnitId { get; private set; } //The id number for the pet whose stance is being set. Setting this to 0 will set the stance for all of the player's pets.
        public PetStance Stance { get; private set; }

        public void Read(GamePacketReader reader)
        {
            PetUnitId = reader.ReadUInt();
            Stance = reader.ReadEnum<PetStance>(5u);
        }
    }
}
