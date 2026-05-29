using NexusForever.Game.Static.Matching;
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
        /// Native row size is <c>0x60</c> bytes. Reader <c>140098500</c> confirms
        /// 14-bit faction/race/class fields, a 2-bit gender slot, one 32-bit level field,
        /// 3-bit path, correlated prime-level / party / role tail fields, five
        /// <see cref="GroupMemberStatSlot"/> rows, and a counted <see cref="PrimeLevelInfo"/> array.
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

            /// <summary>
            /// Queue prime level at struct <c>+0x30</c>; correlates with
            /// <see cref="ClientMatchingQueue.PrimeLevel"/> on <c>ClientMatchingQueue_WritePayload</c>
            /// (<c>140098a70</c>).
            /// </summary>
            public uint PrimeLevel { get; set; }

            /// <summary>Party-queue flag at struct <c>+0x34</c>; correlates with the party bit on
            /// <c>MatchingQueueJoinQueueData_ReadPayload</c> (<c>1400989d0</c>).</summary>
            public bool IsParty { get; set; }

            /// <summary>Selected role mask at struct <c>+0x38</c>; correlates with
            /// <see cref="ClientMatchingQueue.Roles"/> and <see cref="ServerMatchingQueueJoin.QueuedRoles"/>.</summary>
            public Role Roles { get; set; }

            /// <summary>Trailing uint32 at struct <c>+0x3C</c>; wire type confirmed, semantics blocked.</summary>
            public uint TrailingUInt32_0x3C { get; set; }

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
                writer.Write(PrimeLevel);
                writer.Write(IsParty);
                writer.Write(Roles, 32u);
                writer.Write(TrailingUInt32_0x3C);
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
