using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerHousingNeighborhoodEntry)]
    public class ServerHousingNeighborhoodEntry : IWritable
    {
        public ulong NeighborhoodId { get; set; }
        public ushort RealmId0 { get; set; }
        public ushort RealmId1 { get; set; }
        public ulong Value0 { get; set; }
        public string Name { get; set; } = string.Empty;
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            WriteRow(writer);
        }

        internal void WriteRow(GamePacketWriter writer)
        {
            writer.Write(NeighborhoodId);
            writer.Write(RealmId0, 14u);
            writer.Write(RealmId1, 14u);
            writer.Write(Value0);
            writer.WriteStringWide(Name);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
        }
    }

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
