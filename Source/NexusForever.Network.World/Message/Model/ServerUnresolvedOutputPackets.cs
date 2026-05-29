using NexusForever.Game.Static.Storefront;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    public abstract class ServerUnresolvedRawPayload : IWritable
    {
        public byte[] Payload { get; }

        protected ServerUnresolvedRawPayload(uint expectedLength, byte[] payload = null)
        {
            Payload = payload ?? new byte[checked((int)expectedLength)];
            if (Payload.Length != expectedLength)
                throw new ArgumentException($"Payload must be exactly {expectedLength} bytes.", nameof(payload));
        }

        public void Write(GamePacketWriter writer)
        {
            writer.WriteBytes(Payload);
        }
    }

    public abstract class ServerUnresolvedEmptyPayload : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
        }
    }

    public abstract class ServerUnresolvedUIntPayload : IWritable
    {
        private readonly uint bits;

        public uint Value { get; set; }

        protected ServerUnresolvedUIntPayload(uint bits, uint value = 0u)
        {
            this.bits = bits;
            Value     = value;
        }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value, bits);
        }
    }

    public abstract class ServerUnresolvedWideStringPayload : IWritable
    {
        public string Text { get; set; }

        protected ServerUnresolvedWideStringPayload(string text = "")
        {
            Text = text;
        }

        public void Write(GamePacketWriter writer)
        {
            writer.WriteStringWide(Text);
        }
    }

    public abstract class ServerUnresolvedUIntFlagPayload : IWritable
    {
        public uint Value { get; set; }
        public bool Flag { get; set; }

        protected ServerUnresolvedUIntFlagPayload(uint value = 0u, bool flag = false)
        {
            Value = value;
            Flag  = flag;
        }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value);
            writer.Write(Flag);
        }
    }

    [Message(GameMessageOpcode.Server0x0015)]
    public class Server0x0015 : IWritable
    {
        /// <summary>
        /// Opcode 0x0015. Native reader <c>ServerUInt5UInt32_ReadPayload</c> (<c>140081f00</c>)
        /// reads one 5-bit field followed by one uint32 field. Matching opcode <c>0x0628</c>
        /// reuses the same reader but has stronger runtime semantics, so this placeholder keeps
        /// both fields neutral.
        /// </summary>
        public uint Value0 { get; set; }

        public uint Value1 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0, 5u);
            writer.Write(Value1);
        }
    }

    public class ServerUnresolvedUInt32Triple : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
        }
    }

    [Message(GameMessageOpcode.ServerAccountItemCooldowns)]
    public class ServerAccountItemCooldowns : IWritable
    {
        public class Cooldown
        {
            public uint AccountItemCooldownGroup { get; set; }
            public uint CooldownInSeconds { get; set; }
        }

        public List<Cooldown> Cooldowns { get; } = new();

        public ServerAccountItemCooldowns(IEnumerable<Cooldown> cooldowns = null)
        {
            if (cooldowns != null)
                Cooldowns.AddRange(cooldowns);
        }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Cooldowns.Count);
            foreach (Cooldown cooldown in Cooldowns)
            {
                writer.Write(cooldown.AccountItemCooldownGroup);
                writer.Write(cooldown.CooldownInSeconds);
            }
        }
    }

    [Message(GameMessageOpcode.ServerAccountItemCacheAdd)]
    public class ServerAccountItemCacheAdd : IWritable
    {
        /// <summary>
        /// Leading uint32 from <c>ServerAccountItemCacheAdd_ReadPayload</c> (<c>1400a0300</c>).
        /// Not consumed by <c>AccountItemAddToCache_HandleServer096A</c> (<c>140004e30</c>); emit zero until a producer is mapped.
        /// </summary>
        public uint UnusedLeadingField { get; set; }
        public AccountInventoryItem AccountItem { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UnusedLeadingField);
            AccountItem.Write(writer);
        }
    }

    [Message(GameMessageOpcode.ServerAccountItemCacheListAppend)]
    public class ServerAccountItemCacheListAppend : IWritable
    {
        /// <summary>
        /// Leading uint32 from <c>ServerAccountItemCacheListAppend_ReadPayload</c> (<c>1400a0350</c>).
        /// Not consumed by <c>AccountItemListAppend_HandleServer096B</c> (<c>140004f60</c>); emit zero until a producer is mapped.
        /// </summary>
        public uint UnusedLeadingField { get; set; }
        public List<AccountInventoryItem> AccountItems { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UnusedLeadingField);
            writer.Write(AccountItems.Count);
            AccountItems.ForEach(i => i.Write(writer));
        }
    }

    [Message(GameMessageOpcode.ServerAccountItemCacheRemove)]
    public class ServerAccountItemCacheRemove : IWritable
    {
        /// <summary>
        /// Leading uint32 from <c>ServerAccountItemCacheRemove_ReadPayload</c> (<c>140098280</c>).
        /// Not consumed by <c>AccountItemRemoveFromCache_HandleServer096C</c> (<c>140005040</c>); emit zero until a producer is mapped.
        /// </summary>
        public uint UnusedLeadingField { get; set; }
        public ulong AccountInventoryItemId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UnusedLeadingField);
            writer.Write(AccountInventoryItemId);
        }
    }

    [Message(GameMessageOpcode.ServerDailyLoginUpdate)]
    public class ServerDailyLoginUpdate : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }
        public uint Value4 { get; set; }
        public float FloatValue { get; set; }
        public uint UInt3Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
            writer.Write(Value4);
            writer.Write(FloatValue);
            writer.Write(UInt3Value, 3u);
        }
    }

    [Message(GameMessageOpcode.ServerAccountPrivilegeRestrictionUpdate)]
    public class ServerAccountPrivilegeRestrictionUpdate : IWritable
    {
        public uint UInt3Value { get; set; }
        public float FloatValue { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UInt3Value, 3u);
            writer.Write(FloatValue);
        }
    }

    [Message(GameMessageOpcode.ServerAccountPendingItemAdd)]
    public class ServerAccountPendingItemAdd : IWritable
    {
        public ServerAccountItemsPending.PendingAccountItemGroup PendingGroup { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            PendingGroup.Write(writer);
        }
    }

    [Message(GameMessageOpcode.ServerAccountPendingItemsClear)]
    public class ServerAccountPendingItemsClear : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
        }
    }

    public abstract class ServerUnresolvedAccountIdentityRowListPayload : IWritable
    {
        public class Row : IWritable
        {
            public uint Operation { get; set; }
            public bool IsInitiator { get; set; }
            public uint LogAgeMinutes { get; set; }
            public Identity Identity0 { get; set; } = new();
            public Identity Identity1 { get; set; } = new();
            public ulong FriendCharacterId { get; set; }
            public ulong MoneyAmount { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Operation);
                writer.Write(IsInitiator);
                writer.Write(LogAgeMinutes);
                Identity0.Write(writer);
                Identity1.Write(writer);
                writer.Write(FriendCharacterId);
                writer.Write(MoneyAmount);
            }
        }

        public List<Row> Rows { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Rows.Count);
            Rows.ForEach(r => r.Write(writer));
        }
    }

    [Message(GameMessageOpcode.ServerCREDDOperationHistory)]
    public class ServerCREDDOperationHistory : ServerUnresolvedAccountIdentityRowListPayload
    {
    }

    [Message(GameMessageOpcode.ServerCREDDExchangeOrderCacheRows)]
    public class ServerCREDDExchangeOrderCacheRows : IWritable
    {
        /// <summary>
        /// One compact row from <c>ServerCREDDExchangeOrderCacheRows_ReadPayload</c> (<c>1400a0210</c>).
        /// </summary>
        public class Row : IWritable
        {
            /// <summary>uint64 field; matches cancel-order id and <c>CREDDExchangeInfo_AppendOrderCacheRows</c> cache keys.</summary>
            public ulong OrderId { get; set; }

            /// <summary>14-bit field; runtime maps to order credit amount (truncated to 14 bits).</summary>
            public uint CreditAmount { get; set; }

            /// <summary>7-bit field; runtime uses 0 for sell orders and 1 for buy orders. Native row semantics remain unlabeled.</summary>
            public uint SideFlag { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(OrderId);
                writer.Write(CreditAmount, 14u);
                writer.Write(SideFlag, 7u);
            }
        }

        public List<Row> Rows { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Rows.Count);
            Rows.ForEach(r => r.Write(writer));
        }
    }

    public abstract class ServerUnresolvedULongUInt14UInt7ListPayload : IWritable
    {
        public class Row : IWritable
        {
            public ulong Value0 { get; set; }
            public uint UInt14Value { get; set; }
            public uint UInt7Value { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Value0);
                writer.Write(UInt14Value, 14u);
                writer.Write(UInt7Value, 7u);
            }
        }

        public List<Row> Rows { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Rows.Count);
            Rows.ForEach(r => r.Write(writer));
        }
    }

    [Message(GameMessageOpcode.ServerCREDDRedeemResult)]
    public class ServerCREDDRedeemResult : ServerUnresolvedUIntPayload
    {
        public ServerCREDDRedeemResult(uint value = 0u) : base(6u, value) { }
    }

    [Message(GameMessageOpcode.ServerAccountPendingItemGroupDelete)]
    public class ServerAccountPendingItemGroupDelete : IWritable
    {
        public string Value { get; set; } = string.Empty;
        public bool Flag { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.WriteStringWide(Value);
            writer.Write(Flag);
        }
    }

    [Message(GameMessageOpcode.ServerAccountPendingItemDelete)]
    public class ServerAccountPendingItemDelete : IWritable
    {
        public ulong Value { get; set; }
        public bool Flag { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value);
            writer.Write(Flag);
        }
    }

    [Message(GameMessageOpcode.ServerWalletUpdate)]
    public class ServerWalletUpdate : ServerUnresolvedUIntPayload
    {
        public ServerWalletUpdate(uint value = 0u) : base(32u, value) { }
    }

    [Message(GameMessageOpcode.ServerStoreCatalogUpdated)]
    public class ServerStoreCatalogUpdated : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
        }
    }

    [Message(GameMessageOpcode.ServerStoreError)]
    public class ServerStoreError : IWritable
    {
        public StoreError Error { get; set; }

        public ServerStoreError(StoreError error = StoreError.GenericFail)
        {
            Error = error;
        }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Error, 5u);
        }
    }

    public abstract class ServerUnresolvedStoreRowListPayload : IWritable
    {
        public class Row : IWritable
        {
            public ulong Value0 { get; set; }
            public ulong Value1 { get; set; }
            public uint UInt5Value { get; set; }
            public uint UInt3Value { get; set; }
            public float FloatValue { get; set; }
            public string StringValue { get; set; } = string.Empty;
            public bool Flag { get; set; }
            public ulong Value7 { get; set; }
            public uint Value8 { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Value0);
                writer.Write(Value1);
                writer.Write(UInt5Value, 5u);
                writer.Write(UInt3Value, 3u);
                writer.Write(FloatValue);
                writer.WriteStringWide(StringValue);
                writer.Write(Flag);
                writer.Write(Value7);
                writer.Write(Value8);
            }
        }

        public List<Row> Rows { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Rows.Count);
            Rows.ForEach(r => r.Write(writer));
        }
    }

    [Message(GameMessageOpcode.ServerStorePurchaseHistoryReady)]
    public class ServerStorePurchaseHistoryReady : ServerUnresolvedStoreRowListPayload
    {
    }

    [Message(GameMessageOpcode.ServerStoreCompleteOrderVirtualCurrencyPackageResult)]
    public class ServerStoreCompleteOrderVirtualCurrencyPackageResult : IWritable
    {
        public bool Flag { get; set; }
        public uint Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Flag);
            writer.Write(Value, 5u);
        }
    }

    [Message(GameMessageOpcode.ServerStorePurchaseVirtualCurrencyPackageResult)]
    public class ServerStorePurchaseVirtualCurrencyPackageResult : IWritable
    {
        public bool Flag { get; set; }
        public uint UInt5Value { get; set; }
        public string String0 { get; set; } = string.Empty;
        public string String1 { get; set; } = string.Empty;
        public string String2 { get; set; } = string.Empty;
        public string String3 { get; set; } = string.Empty;
        public string String4 { get; set; } = string.Empty;
        public string String5 { get; set; } = string.Empty;
        public string String6 { get; set; } = string.Empty;
        public string String7 { get; set; } = string.Empty;
        public float Float0 { get; set; }
        public float Float1 { get; set; }
        public float Float2 { get; set; }
        public string String8 { get; set; } = string.Empty;

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Flag);
            writer.Write(UInt5Value, 5u);
            writer.WriteStringWide(String0);
            writer.WriteStringWide(String1);
            writer.WriteStringWide(String2);
            writer.WriteStringWide(String3);
            writer.WriteStringWide(String4);
            writer.WriteStringWide(String5);
            writer.WriteStringWide(String6);
            writer.WriteStringWide(String7);
            writer.Write(Float0);
            writer.Write(Float1);
            writer.Write(Float2);
            writer.WriteStringWide(String8);
        }
    }
}
