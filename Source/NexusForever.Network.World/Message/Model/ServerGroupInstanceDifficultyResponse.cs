using NexusForever.Game.Static.Setting;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Group instance difficulty sync broadcast (0x0414, 0x18 bytes).
    /// Correlated to <see cref="ClientGroupSetInstanceDifficulty"/> (0x0412); field layout is conservative until Ghidra reader proof.
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupInstanceDifficultyResponse)]
    public class ServerGroupInstanceDifficultyResponse : IWritable
    {
        public ulong GroupId { get; set; }

        /// <summary>Character guid of the member who changed difficulty.</summary>
        public uint CharacterGuid { get; set; }

        public WorldDifficulty Difficulty { get; set; }

        /// <summary>Reserved dword; may carry prime level once mapped.</summary>
        public uint Unknown0 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GroupId);
            writer.Write(CharacterGuid);
            writer.Write((uint)Difficulty);
            writer.Write(Unknown0);
            writer.Write(0u);
        }
    }
}
