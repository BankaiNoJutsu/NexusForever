using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Network.World.Message.Model
{
    public enum HousingNeighborUpdateType : uint
    {
        Added             = 0u,
        PermissionChanged = 1u,
        Removed           = 2u,
        Refresh           = 3u
    }

    [Message(GameMessageOpcode.ServerHousingNeighborInvitePrompt)]
    public class ServerHousingNeighborInvitePrompt : IWritable
    {
        public TargetResidence TargetResidence { get; } = new();
        public string PlayerName { get; set; } = string.Empty;

        public void Write(GamePacketWriter writer)
        {
            TargetResidence.Write(writer);
            writer.WriteStringWide(PlayerName);
        }
    }

    [Message(GameMessageOpcode.ServerHousingNeighborInviteResult)]
    public class ServerHousingNeighborInviteResult : IWritable
    {
        public ServerHousingNeighbors.Neighbor Neighbor { get; } = new();
        public HousingResult Result { get; set; }

        public void Write(GamePacketWriter writer)
        {
            Neighbor.Write(writer);
            writer.Write((uint)Result, 7u);
        }
    }

    [Message(GameMessageOpcode.ServerHousingNeighborUpdate)]
    public class ServerHousingNeighborUpdate : IWritable
    {
        public ServerHousingNeighbors.Neighbor Neighbor { get; } = new();
        public HousingNeighborUpdateType UpdateType { get; set; }

        public void Write(GamePacketWriter writer)
        {
            Neighbor.Write(writer);
            writer.Write((uint)UpdateType, 3u);
        }
    }

    [Message(GameMessageOpcode.ServerHousingCommunityPlotReservation)]
    public class ServerHousingCommunityPlotReservation : IWritable
    {
        public TargetResidence TargetResidence { get; } = new();
        public uint PlotIndex { get; set; } = uint.MaxValue;

        public void Write(GamePacketWriter writer)
        {
            TargetResidence.Write(writer);
            writer.Write(PlotIndex);
        }
    }
}
