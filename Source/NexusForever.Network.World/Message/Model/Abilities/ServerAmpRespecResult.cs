using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Network.World.Message.Model.Abilities
{
    // Originally called Eldan Augmentations. See eldanAugmentation tbl for information
    [Message(GameMessageOpcode.ServerAmpRespecResult)]
    public class ServerAmpRespecResult : IWritable
    {
        public class AmpResult
        {
            public ushort SpecIndex { get; set; }
            public LimitedActionSetResult Result { get; set; }
        }

        public List<AmpResult> Results { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Results.Count, 7u);
            Results.ForEach(result => writer.Write(result.SpecIndex, 3u));
            Results.ForEach(result => writer.Write(result.Result, 6u));
        }
    }
}
