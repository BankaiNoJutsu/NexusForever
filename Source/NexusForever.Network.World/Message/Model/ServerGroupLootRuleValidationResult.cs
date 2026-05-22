using NexusForever.Game.Static.Group;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Loot-rule change validation result (0x0431, 0x18 bytes).
    /// Follows <see cref="ServerGroupLootRulesChange"/> (0x042F).
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupLootRuleValidationResult)]
    public class ServerGroupLootRuleValidationResult : IWritable
    {
        public ulong GroupId { get; set; }

        public uint Unknown0 { get; set; }

        public GroupActionResult Result { get; set; }

        public uint Unknown1 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GroupId);
            writer.Write(Unknown0);
            writer.Write(Result, 32u);
            writer.Write(Unknown1);
            writer.Write(0u);
        }
    }
}
