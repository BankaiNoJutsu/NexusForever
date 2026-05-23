using NexusForever.Game.Static.Account;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Shared
{
    public class AccountInventoryItem : IWritable
    {
        public ulong Id { get; set; }
        public uint ItemId { get; set; }
        public AccountItemClaimState ClaimState { get; set; }
        public bool HasTargetPlayerIdentity { get; set; }
        public Identity TargetPlayerIdentity { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Id);
            writer.Write(ItemId);
            writer.Write(ClaimState, 5u);
            writer.Write(HasTargetPlayerIdentity);
            TargetPlayerIdentity.Write(writer);
        }
    }
}
