using NexusForever.Game.Static.Housing;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientHousingPlugUpdate)]
    public class ClientHousingPlugUpdate : IReadable
    {
        public enum PlugUpdateOperation
        {
            PlaceOrRotate = 1,
            Remove        = 2,
            Repair        = 4
        }

        public Identity Identity { get; } = new();
        public uint HousingPlotInfoId { get; set; }
        public uint HousingPlugItemId { get; set; }
        public HousingPlugFacing PlugFacing { get; set; }
        public uint Reserved { get; set; }
        public PlugUpdateOperation Operation { get; set; }
        public byte[] ContributionData { get; set; }

        public void Read(GamePacketReader reader)
        {
            Identity.Read(reader);

            HousingPlotInfoId = reader.ReadUInt();
            HousingPlugItemId = reader.ReadUInt();
            PlugFacing        = reader.ReadEnum<HousingPlugFacing>(32u);
            Reserved          = reader.ReadUInt();
            Operation         = reader.ReadEnum<PlugUpdateOperation>(3u);

            // HousingContribution related, client function that sends this looks up values from HousingContributionInfo.tbl.
            ContributionData = reader.ReadBytes(5 * 20);
        }
    }
}
