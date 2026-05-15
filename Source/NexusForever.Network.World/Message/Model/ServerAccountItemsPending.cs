using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Game.Static.Account;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerAccountItemsPending)]
    public class ServerAccountItemsPending : IWritable
    {
        public class PendingAccountItemGroup : IWritable
        {
            public ulong Id { get; set; }
            public uint AccountItemId { get; set; }
            public ulong Unknown2 { get; set; }
            public string Group { get; set; } = string.Empty;
            public uint Unknown4 { get; set; }
            public Identity SenderIdentity { get; set; } = new();
            public ulong Unknown7 { get; set; }
            public AccountItemClaimState ClaimState { get; set; }
            public ulong Unknown9 { get; set; }
            public Identity TargetIdentity { get; set; } = new();

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Id);
                writer.Write(AccountItemId);
                writer.Write(Unknown2);
                writer.WriteStringWide(Group);
                writer.Write(Unknown4);
                SenderIdentity.Write(writer);
                writer.Write(Unknown7);
                writer.Write(ClaimState, 5u);
                writer.Write(Unknown9);
                TargetIdentity.Write(writer);
            }
        }

        public List<PendingAccountItemGroup> PendingGroups { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PendingGroups.Count);
            PendingGroups.ForEach(w => w.Write(writer));
        }
    }
}
