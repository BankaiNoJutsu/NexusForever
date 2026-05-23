using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Game.Static.Account;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerAccountItemsPending)]
    public class ServerAccountItemsPending : IWritable
    {
        /// <summary>
        /// One pending group row from <c>PendingAccountItemGroup_ReadPayload</c> @ <c>1400a9b20</c>
        /// (0x60-byte client struct; cached by <c>AccountPendingItemGroupCache_InsertFromPayload</c> @ <c>140005bf0</c>).
        /// </summary>
        public class PendingAccountItemGroup : IWritable
        {
            public ulong Id { get; set; }
            public uint AccountItemId { get; set; }
            /// <summary>
            /// Trailing u64 at wire <c>+0x10</c> after <see cref="AccountItemId"/>. Stored in client cache
            /// slot <c>[2]</c> by <c>AccountPendingItemGroupCache_InsertFromPayload</c> @ <c>140005bf0</c>.
            /// Grouped lookup uses <see cref="Id"/> only (<c>AccountPendingItemGroupCache_LookupByPendingItemId</c>
            /// @ <c>140007810</c>). <c>AccountItemUi_GiftSelectedPendingItemGroup</c> @ <c>140519260</c> reads the
            /// group wide string at cache <c>+0x38</c> and gift targets from UI fields, not this slot.
            /// NF has no DB column; emit <c>0</c> unless retail sends non-zero.
            /// </summary>
            public ulong Unknown2 { get; set; }
            public string Group { get; set; } = string.Empty;
            public uint SenderAccountId { get; set; }
            public Identity SenderIdentity { get; set; } = new();
            /// <summary>Account gift target; zero when not an account-targeted pending group.</summary>
            public ulong TargetAccountId { get; set; }
            public AccountItemClaimState ClaimState { get; set; }
            /// <summary>Wire u64; retail uses low bit for target-player identity presence (see account inventory items).</summary>
            public ulong HasTargetPlayerIdentity { get; set; }
            public Identity TargetIdentity { get; set; } = new();

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Id);
                writer.Write(AccountItemId);
                writer.Write(Unknown2);
                writer.WriteStringWide(Group);
                writer.Write(SenderAccountId);
                SenderIdentity.Write(writer);
                writer.Write(TargetAccountId);
                writer.Write(ClaimState, 5u);
                writer.Write(HasTargetPlayerIdentity);
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
