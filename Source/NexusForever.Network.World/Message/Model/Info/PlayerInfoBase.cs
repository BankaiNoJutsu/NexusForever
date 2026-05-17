using NexusForever.Game.Static.Reputation;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Network.World.Message.Model.Info
{
    public class PlayerInfoBase : IWritable
    {
        public PlayerInfoResult ResultCode { get; set; }
        public Identity Identity { get; set; }
        public string Name { get; set; }
        public Faction Faction { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ResultCode, 3u);
            Identity.Write(writer);
            writer.WriteStringFixed(Name);
            writer.Write(Faction, 14u);
        }
    }
}
