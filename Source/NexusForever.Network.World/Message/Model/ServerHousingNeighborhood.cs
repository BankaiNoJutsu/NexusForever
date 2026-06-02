using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Single housing neighborhood row (<c>0x0501</c> / shared <c>0x0506</c> list entry, 0x30 bytes).
    /// Reader: <c>ServerHousingNeighborhoodEntry_ReadPayload</c> @ <c>14009cbe0</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingNeighborhoodEntry)]
    public class ServerHousingNeighborhoodEntry : IWritable
    {
        public ulong NeighborhoodId { get; set; }
        public ushort RealmId0 { get; set; }
        public ushort RealmId1 { get; set; }

        /// <summary>Second uint64 in the native row; semantics blocked (not renamed without apply-site proof).</summary>
        public ulong NeighborhoodWireUInt64_AfterRealmIds { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Trailing uint32 field 0; apply-site semantics blocked.</summary>
        public uint NeighborhoodWireUInt32_0 { get; set; }

        /// <summary>Trailing uint32 field 1; apply-site semantics blocked.</summary>
        public uint NeighborhoodWireUInt32_1 { get; set; }

        /// <summary>Trailing uint32 field 2; apply-site semantics blocked.</summary>
        public uint NeighborhoodWireUInt32_2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            WriteRow(writer);
        }

        internal void WriteRow(GamePacketWriter writer)
        {
            writer.Write(NeighborhoodId);
            writer.Write(RealmId0, 14u);
            writer.Write(RealmId1, 14u);
            writer.Write(NeighborhoodWireUInt64_AfterRealmIds);
            writer.WriteStringWide(Name);
            writer.Write(NeighborhoodWireUInt32_0);
            writer.Write(NeighborhoodWireUInt32_1);
            writer.Write(NeighborhoodWireUInt32_2);
        }
    }

    /// <summary>
    /// Realm-scoped neighborhood list (<c>0x0506</c>).
    /// Consumer: <c>Housing_HandleNeighborhoodList</c> @ <c>1404ba4f0</c> -> Lua <c>HousingNeighborhoodRecieved</c>.
    /// </summary>
    /// <remarks>Client request opcode that triggers retail <c>0x0506</c> remains blocked; no production NF emitter.</remarks>
    [Message(GameMessageOpcode.ServerHousingNeighborhoodList)]
    public class ServerHousingNeighborhoodList : IWritable
    {
        public ushort RealmId { get; set; }
        public List<ServerHousingNeighborhoodEntry> Neighborhoods { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(RealmId, 14u);
            writer.Write(Neighborhoods.Count);
            Neighborhoods.ForEach(neighborhood => neighborhood.WriteRow(writer));
        }
    }

    [Message(GameMessageOpcode.ServerHousingCommunityPlacement)]
    public class ServerHousingCommunityPlacement : IWritable
    {
        public TargetResidence TargetResidence { get; } = new();
        public ulong PlacedResidenceId { get; set; }
        public uint PropertyIndex { get; set; }

        public void Write(GamePacketWriter writer)
        {
            TargetResidence.Write(writer);
            writer.Write(PlacedResidenceId);
            writer.Write(PropertyIndex);
        }
    }

    [Message(GameMessageOpcode.ServerHousingCommunityPrivacyLevelUpdate)]
    public class ServerHousingCommunityPrivacyLevelUpdate : IWritable
    {
        public const uint CommunityEntryType = 7u;
        public const uint PrivateFlag        = 0x10u;
        public const uint PublicLuaValue     = 0u;
        public const uint PrivateLuaValue    = 3u;

        public TargetResidence TargetResidence { get; } = new();
        public uint EntryType { get; set; } = CommunityEntryType;
        public uint Flags { get; set; }
        public uint PrivacyLevel { get; set; }

        public void Write(GamePacketWriter writer)
        {
            TargetResidence.Write(writer);
            writer.Write(EntryType);
            writer.Write(Flags);
            writer.Write(PrivacyLevel, 3u);
        }
    }
}
