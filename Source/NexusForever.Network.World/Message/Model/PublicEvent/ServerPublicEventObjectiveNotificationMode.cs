using NexusForever.Game.Static.PublicEvent;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    [Message(GameMessageOpcode.ServerPublicEventObjectiveNotificationMode)]
    public class ServerPublicEventObjectiveNotificationMode : IWritable
    {
        public uint ObjectiveId { get; set; }
        public PublicEventObjectiveNotificationMode NotificationMode { get; set; }

        public void Write(GamePacketWriter writer)
        {
            // WildStar64.exe 14007a3a0 reads opcode 0x0133 as objectiveId u15 + notification mode u32.
            writer.Write(ObjectiveId, 15);
            writer.Write(NotificationMode, 32u);
        }
    }
}
