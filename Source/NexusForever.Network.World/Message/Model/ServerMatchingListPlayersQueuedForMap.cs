using NexusForever.Game.Static.Reputation;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerMatchingListPlayersQueuedForMap_ReadPayload</c> (<c>140099770</c>).
    /// Reads one uint32 map id and a counted queued-player array via
    /// <c>MatchingQueuedPlayerInfo_ReadPayload</c> (<c>140098500</c>).
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingListPlayersQueuedForMap)]
    public class ServerMatchingListPlayersQueuedForMap : IWritable
    {
        /// <summary>
        /// Native reader: <c>MatchingPrimeLevelInfo_ReadPayload</c> (<c>1400ad150</c>).
        /// Reads a 15-bit world id and one 16-bit achieved prime-level value.
        /// </summary>
        public class PrimeLevelInfo : IWritable
        {
            public ushort WorldId { get; set; }
            public ushort PrimeLevelAchieved { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(WorldId, 15u);
                writer.Write(PrimeLevelAchieved);
            }
        }

        /// <summary>
        /// Native row size is <c>0x60</c> bytes. Reader <c>140098500</c> confirms
        /// 14-bit faction/race/class fields, a 2-bit gender slot, one 32-bit
        /// level-like field, a 3-bit path, four trailing unknown scalar/flag
        /// slots, five <see cref="GroupMemberStatSlot"/> rows, and a counted
        /// <see cref="PrimeLevelInfo"/> array.
        /// </summary>
        public class QueuedPlayerInfo : IWritable
        {
            public Identity Identity { get; set; }
            public string Name { get; set; }
            public Faction Faction { get; set; }
            public uint Race {  get; set; }
            public uint Class { get; set; }
            public uint Gender { get; set; }
            public uint Level { get; set; }
            public uint Path { get; set; }
            public uint Unknown30 { get; set; }
            public bool Unknown34 { get; set; }
            public uint Unknown38 { get; set; }
            public uint Unknown3C { get; set; }
            public GroupMemberStatSlot[] StatSlots = new GroupMemberStatSlot[5];
            public List<PrimeLevelInfo> PrimeLevels { get; set; } = new List<PrimeLevelInfo>();

            public void Write(GamePacketWriter writer)
            {
                Identity.Write(writer);
                writer.WriteStringWide(Name);
                writer.Write(Faction, 14u);
                writer.Write(Race, 14u);
                writer.Write(Class, 14u);
                writer.Write(Gender, 2u);
                writer.Write(Level);
                writer.Write(Path, 3u);
                writer.Write(Unknown30);
                writer.Write(Unknown34);
                writer.Write(Unknown38);
                writer.Write(Unknown3C);
                foreach (GroupMemberStatSlot stat in StatSlots)
                {
                    stat.Write(writer);
                }
                writer.Write(PrimeLevels.Count);
                PrimeLevels.ForEach(i => i.Write(writer));
            }

        }

        public uint MapId { get; set; }
        public List<QueuedPlayerInfo> QueuedPlayers { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(MapId);
            writer.Write(QueuedPlayers.Count);
            foreach (var player in QueuedPlayers)
            {
                player.Write(writer);
            }
        }
    }
}
