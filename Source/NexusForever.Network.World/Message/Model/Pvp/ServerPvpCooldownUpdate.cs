using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pvp
{
    /// <summary>
    /// Time for the PvP flag disable cooldown to expire in milliseconds.
    /// Native opcode <c>0x013E</c> uses shared <c>ServerUInt32_ReadPayload</c>
    /// (<c>14007ab50</c>); the opcode-specific client apply owner remains unmapped.
    /// </summary>
    [Message(GameMessageOpcode.ServerPvpCooldownUpdate)]
    public class ServerPvpCooldownUpdate : IWritable
    {
        public uint CooldownRemaining { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(CooldownRemaining);
        }
    }
}
