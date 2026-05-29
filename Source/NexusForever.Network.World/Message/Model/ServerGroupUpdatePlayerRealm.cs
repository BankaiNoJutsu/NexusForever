using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode <c>0x0467</c>; mapped from <c>ServerGroupUpdatePlayerRealm_ReadPayload</c> (<c>1400841d0</c>).
    /// The native reader reuses the shared group realm/world/map/phase tail helper also seen in <see cref="Shared.GroupCharacter"/>.
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupUpdatePlayerRealm)]
    public class ServerGroupUpdatePlayerRealm : IWritable
    {
        public ulong GroupId { get; set; }
        public Identity TargetPlayerIdentity { get; set; }
        public uint RealmId { get; set; }
        public uint ZoneId { get; set; }
        public uint MapId { get; set; }
        public uint PhaseId { get; set; }
        public bool IsSyncdToGroup { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GroupId);
            TargetPlayerIdentity.Write(writer);
            writer.Write(RealmId, 14);
            writer.Write(ZoneId, 15);
            writer.Write(MapId);
            writer.Write(PhaseId);
            writer.Write(IsSyncdToGroup);
        }
    }
}
