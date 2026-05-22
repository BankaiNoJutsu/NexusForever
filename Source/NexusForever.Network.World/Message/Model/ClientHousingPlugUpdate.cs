using NexusForever.Game.Static.Housing;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientHousingPlugUpdate)]
    public class ClientHousingPlugUpdate : IReadable
    {
        public const int ContributionRecordCount = 5;

        public enum PlugUpdateOperation
        {
            PlaceOrRotate = 1,
            Remove        = 2,
            Repair        = 4
        }

        public class ContributionRecord : IReadable
        {
            public uint ContributionPointRequirement { get; private set; }
            public uint Reserved0 { get; private set; }
            public uint Reserved1 { get; private set; }
            public uint Reserved2 { get; private set; }
            public uint Reserved3 { get; private set; }

            public bool HasPayload => ContributionPointRequirement != 0u
                || Reserved0 != 0u
                || Reserved1 != 0u
                || Reserved2 != 0u
                || Reserved3 != 0u;

            public void Read(GamePacketReader reader)
            {
                ContributionPointRequirement = reader.ReadUInt();
                Reserved0                    = reader.ReadUInt();
                Reserved1                    = reader.ReadUInt();
                Reserved2                    = reader.ReadUInt();
                Reserved3                    = reader.ReadUInt();
            }
        }

        public Identity Identity { get; } = new();
        public uint HousingPlotInfoId { get; set; }
        public uint HousingPlugItemId { get; set; }
        public HousingPlugFacing PlugFacing { get; set; }
        public uint Reserved { get; set; }
        public PlugUpdateOperation Operation { get; set; }
        public List<ContributionRecord> Contributions { get; } = new();

        public void Read(GamePacketReader reader)
        {
            Identity.Read(reader);

            HousingPlotInfoId = reader.ReadUInt();
            HousingPlugItemId = reader.ReadUInt();
            PlugFacing        = reader.ReadEnum<HousingPlugFacing>(32u);
            Reserved          = reader.ReadUInt();
            Operation         = reader.ReadEnum<PlugUpdateOperation>(3u);

            Contributions.Clear();
            for (int i = 0; i < ContributionRecordCount; i++)
            {
                var contribution = new ContributionRecord();
                contribution.Read(reader);
                Contributions.Add(contribution);
            }
        }
    }
}
