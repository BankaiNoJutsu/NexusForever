using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerMatchingQueueJoin_ReadPayload</c> (<c>1400995f0</c>).
    /// Reads one map block via <c>MatchingQueueJoinMapData_ReadPayload</c> (<c>1400988f0</c>),
    /// one queue block via <c>MatchingQueueJoinQueueData_ReadPayload</c> (<c>1400989d0</c>),
    /// then one trailing 32-bit queued-roles field.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingQueueJoin)]
    public class ServerMatchingQueueJoin : IWritable
    {
        /// <summary>
        /// Native reader: <c>MatchingQueueJoinMapData_ReadPayload</c> (<c>1400988f0</c>).
        /// Reads a 5-bit match type, counted uint32 map-id list, one 14-bit matching-game-type
        /// field, and one 32-bit queue-flags field.
        /// </summary>
        public class Map : IWritable
        {
            public Game.Static.Matching.MatchType MatchType { get; set; }
            public List<uint> MatchingGameMapIds { get; set; } = [];
            public ushort MatchingGameType { get; set; }
            public MatchingQueueFlags QueueFlags { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(MatchType, 5u);

                writer.Write(MatchingGameMapIds.Count);
                foreach (var mapId in MatchingGameMapIds)
                    writer.Write(mapId);

                writer.Write(MatchingGameType, 14u);
                writer.Write(QueueFlags, 32u);
            }
        }

        /// <summary>
        /// Native reader: <c>MatchingQueueJoinQueueData_ReadPayload</c> (<c>1400989d0</c>).
        /// Reads a 5-bit match type, one party flag bit, queue time, and average wait time.
        /// </summary>
        public class Queue : IWritable
        {
            public Game.Static.Matching.MatchType MatchType { get; set; }
            public bool IsParty { get; set; }
            public uint QueueTime { get; set; }
            public uint AverageWaitTime { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(MatchType, 5u);
                writer.Write(IsParty);
                writer.Write(QueueTime);
                writer.Write(AverageWaitTime);
            }
        }

        public Map MapData { get; set; }
        public Queue QueueData { get; set; }
        public Role QueuedRoles { get; set; }

        public void Write(GamePacketWriter writer)
        {
            MapData.Write(writer);
            QueueData.Write(writer);

            writer.Write(QueuedRoles, 32u);
        }
    }
}
