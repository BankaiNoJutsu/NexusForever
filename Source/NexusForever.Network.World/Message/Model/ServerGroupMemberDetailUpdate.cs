using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Compact group-member detail refresh (0x0468, 0x28 bytes).
    /// Prefix of the layout copied by FUN_140607490 @ 140607490.
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupMemberDetailUpdate)]
    public class ServerGroupMemberDetailUpdate : IWritable
    {
        public ulong GroupId { get; set; }

        public Identity TargetPlayer { get; set; } = new();

        public byte Level { get; set; }

        public byte EffectiveLevel { get; set; }

        /// <inheritdoc cref="GroupCharacter.StatBlockPrefix17"/>
        public uint StatBlockPrefix17 { get; set; }

        public ushort GroupMemberId { get; set; }

        public float Health { get; set; }

        public float HealthMax { get; set; }

        public NexusForever.Game.Static.PlayerPath.Path Path { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GroupId);
            TargetPlayer.Write(writer);
            writer.Write(Level, 7u);
            writer.Write(EffectiveLevel, 7u);
            writer.Write(StatBlockPrefix17, 17u);
            writer.Write(GroupMemberId);
            writer.WritePackedFloat(Health);
            writer.WritePackedFloat(HealthMax);
            writer.Write(Path, 3u);
            writer.WriteBytes(new byte[12]);
        }
    }
}
