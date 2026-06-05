using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pet
{
    /// <summary>
    /// Native reader <c>ServerUInt32_ReadPayload</c> @ <c>14007ab50</c> reads one pet unit id.
    /// Native apply path <c>Pet_ApplyDespawnedPayload</c> @ <c>1403c09b0</c> removes the cached
    /// pet row and dispatches <c>PetDespawned</c> when the row was present.
    /// </summary>
    [Message(GameMessageOpcode.ServerPetDespawned)]
    public class ServerPetDespawned : IWritable
    {
        public uint PetUnitId { get; set; } // TBC if this follows other messages, 0 means all engineer pets

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PetUnitId);
        }
    }
}
