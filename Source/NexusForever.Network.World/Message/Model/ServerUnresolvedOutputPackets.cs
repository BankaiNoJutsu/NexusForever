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

    [Message(GameMessageOpcode.Server0x00B7)]
    public class Server0x00B7 : ServerUnresolvedRawPayload
    {
        public Server0x00B7(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x00CB)]
    public class Server0x00CB : ServerUnresolvedEmptyPayload
    {
    }

    [Message(GameMessageOpcode.Server0x00CC)]
    public class Server0x00CC : ServerUnresolvedUIntPayload
    {
        public Server0x00CC(uint value = 0u) : base(15u, value) { }
    }

    [Message(GameMessageOpcode.Server0x00CD)]
    public class Server0x00CD : ServerUnresolvedUIntPayload
    {
        public Server0x00CD(uint value = 0u) : base(15u, value) { }
    }

    [Message(GameMessageOpcode.Server0x00CE)]
    public class Server0x00CE : ServerUnresolvedWideStringPayload
    {
        public Server0x00CE(string text = "") : base(text) { }
    }

    [Message(GameMessageOpcode.Server0x00D1)]
    public class Server0x00D1 : ServerUnresolvedEmptyPayload
    {
    }

    [Message(GameMessageOpcode.Server0x00DF)]
    public class Server0x00DF : ServerUnresolvedRawPayload
    {
        public Server0x00DF(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x00EE)]
    public class Server0x00EE : ServerUnresolvedRawPayload
    {
        public Server0x00EE(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0101)]
    public class Server0x0101 : ServerUnresolvedRawPayload
    {
        public Server0x0101(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x010D)]
    public class Server0x010D : ServerUnresolvedEmptyPayload
    {
    }

    [Message(GameMessageOpcode.Server0x0110)]
    public class Server0x0110 : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1, 18u);
            writer.Write(Value2);
            writer.Write(Value3, 8u);
        }
    }

    [Message(GameMessageOpcode.Server0x0139)]
    public class Server0x0139 : ServerUnresolvedRawPayload
    {
        public Server0x0139(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0143)]
    public class Server0x0143 : ServerUnresolvedRawPayload
    {
        public Server0x0143(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x014D)]
    public class Server0x014D : ServerUnresolvedRawPayload
    {
        public Server0x014D(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0157)]
    public class Server0x0157 : ServerUnresolvedRawPayload
    {
        public Server0x0157(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0160)]
    public class Server0x0160 : ServerUnresolvedRawPayload
    {
        public Server0x0160(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0181)]
    public class Server0x0181 : ServerUnresolvedRawPayload
    {
        public Server0x0181(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0183)]
    public class Server0x0183 : ServerUnresolvedUIntFlagPayload
    {
        public Server0x0183(uint value = 0u, bool flag = false) : base(value, flag) { }
    }

    [Message(GameMessageOpcode.Server0x0186)]
    public class Server0x0186 : ServerUnresolvedRawPayload
    {
        public Server0x0186(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0187)]
    public class Server0x0187 : ServerUnresolvedRawPayload
    {
        public Server0x0187(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x019A)]
    public class Server0x019A : ServerUnresolvedRawPayload
    {
        public Server0x019A(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01A6)]
    public class Server0x01A6 : ServerUnresolvedRawPayload
    {
        public Server0x01A6(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01A7)]
    public class Server0x01A7 : ServerUnresolvedRawPayload
    {
        public Server0x01A7(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01A8)]
    public class Server0x01A8 : ServerUnresolvedRawPayload
    {
        public Server0x01A8(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01A9)]
    public class Server0x01A9 : ServerUnresolvedRawPayload
    {
        public Server0x01A9(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01B2)]
    public class Server0x01B2 : ServerUnresolvedRawPayload
    {
        public Server0x01B2(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01B8)]
    public class Server0x01B8 : ServerUnresolvedRawPayload
    {
        public Server0x01B8(byte[] payload = null) : base(0x68u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01C1)]
    public class Server0x01C1 : ServerUnresolvedRawPayload
    {
        public Server0x01C1(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01C4)]
    public class Server0x01C4 : ServerUnresolvedRawPayload
    {
        public Server0x01C4(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01EF)]
    public class Server0x01EF : ServerUnresolvedRawPayload
    {
        public Server0x01EF(byte[] payload = null) : base(0x10u, payload) { }
    }

    public class Server0x025FRow : IWritable
    {
        public ushort Value0 { get; set; }
        public uint Value1 { get; set; }
        public ServerUnresolvedUInt32Triple Value2 { get; set; } = new();
        public uint Value5 { get; set; }
        public ServerUnresolvedUInt32Triple Value6 { get; set; } = new();
        public uint Value9 { get; set; }
        public uint Value10 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0, 16u);
            writer.Write(Value1);
            Value2.Write(writer);
            writer.Write(Value5);
            Value6.Write(writer);
            writer.Write(Value9);
            writer.Write(Value10);
        }
    }

    [Message(GameMessageOpcode.Server0x0260)]
    public class Server0x0260 : IWritable
    {
        public List<Server0x025FRow> Rows { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write((uint)Rows.Count);
            Rows.ForEach(row => row.Write(writer));
        }
    }

    [Message(GameMessageOpcode.Server0x025F)]
    public class Server0x025F : Server0x025FRow
    {
    }

    public class Server0x0263Row : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }
        public uint Value4 { get; set; }
        public uint Value5 { get; set; }
        public ServerUnresolvedUInt32Triple Value6 { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2, 17u);
            writer.Write(Value3, 17u);
            writer.Write(Value4, 17u);
            writer.Write(Value5);
            Value6.Write(writer);
        }
    }

    [Message(GameMessageOpcode.Server0x0261)]
    public class Server0x0261 : IWritable
    {
        public List<Server0x0263Row> Rows { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write((uint)Rows.Count);
            Rows.ForEach(row => row.Write(writer));
        }
    }

    [Message(GameMessageOpcode.Server0x0263)]
    public class Server0x0263 : Server0x0263Row
    {
    }

    [Message(GameMessageOpcode.Server0x0264)]
    public class Server0x0264 : IWritable
    {
        public uint Value0 { get; set; }
        public ushort Value1 { get; set; }
        public uint Value2 { get; set; }
        public List<uint> Values { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1, 16u);
            writer.Write(Value2);
            writer.Write((uint)Values.Count);
            Values.ForEach(value => writer.Write(value));
        }
    }

    [Message(GameMessageOpcode.Server0x0347)]
    public class Server0x0347 : ServerUnresolvedRawPayload
    {
        public Server0x0347(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0348)]
    public class Server0x0348 : ServerUnresolvedRawPayload
    {
        public Server0x0348(byte[] payload = null) : base(0x30u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x034C)]
    public class Server0x034C : ServerUnresolvedRawPayload
    {
        public Server0x034C(byte[] payload = null) : base(0x1Cu, payload) { }
    }

    [Message(GameMessageOpcode.Server0x034D)]
    public class Server0x034D : ServerUnresolvedRawPayload
    {
        public Server0x034D(byte[] payload = null) : base(0x40u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x034E)]
    public class Server0x034E : ServerUnresolvedRawPayload
    {
        public Server0x034E(byte[] payload = null) : base(0x28u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0351)]
    public class Server0x0351 : ServerUnresolvedRawPayload
    {
        public Server0x0351(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x037F)]
    public class Server0x037F : ServerUnresolvedRawPayload
    {
        public Server0x037F(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x03EF)]
    public class Server0x03EF : ServerUnresolvedRawPayload
    {
        public Server0x03EF(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.ServerGroupKickResult)]
    public class ServerGroupKickResult : ServerUnresolvedRawPayload
    {
        public ServerGroupKickResult(byte[] payload = null) : base(0xCu, payload) { }
    }

    [Message(GameMessageOpcode.ServerGroupLootRuleValidationResult)]
    public class ServerGroupLootRuleValidationResult : ServerUnresolvedRawPayload
    {
        public ServerGroupLootRuleValidationResult(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.ServerGroupRosterUpdate)]
    public class ServerGroupRosterUpdate : ServerUnresolvedRawPayload
    {
        public ServerGroupRosterUpdate(byte[] payload = null) : base(0x60u, payload) { }
    }

    [Message(GameMessageOpcode.ServerGroupMemberRoleChange)]
    public class ServerGroupMemberRoleChange : ServerUnresolvedRawPayload
    {
        public ServerGroupMemberRoleChange(byte[] payload = null) : base(0x28u, payload) { }
    }

    [Message(GameMessageOpcode.ServerGroupReadyCheckStatusUpdate)]
    public class ServerGroupReadyCheckStatusUpdate : ServerUnresolvedRawPayload
    {
        public ServerGroupReadyCheckStatusUpdate(byte[] payload = null) : base(0x38u, payload) { }
    }

    [Message(GameMessageOpcode.ServerGroupRequestJoinWindow)]
    public class ServerGroupRequestJoinWindow : ServerUnresolvedRawPayload
    {
        public ServerGroupRequestJoinWindow(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.ServerQuestShareResult)]
    public class ServerQuestShareResult : ServerUnresolvedRawPayload
    {
        public ServerQuestShareResult(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.ServerGroupMemberDetailUpdate)]
    public class ServerGroupMemberDetailUpdate : ServerUnresolvedRawPayload
    {
        public ServerGroupMemberDetailUpdate(byte[] payload = null) : base(0x28u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0567)]
    public class Server0x0567 : ServerUnresolvedRawPayload
    {
        public Server0x0567(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x056B)]
    public class Server0x056B : ServerUnresolvedRawPayload
    {
        public Server0x056B(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x056C)]
    public class Server0x056C : ServerUnresolvedRawPayload
    {
        public Server0x056C(byte[] payload = null) : base(0x28u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x056D)]
    public class Server0x056D : ServerUnresolvedRawPayload
    {
        public Server0x056D(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x05A1)]
    public class Server0x05A1 : ServerUnresolvedRawPayload
    {
        public Server0x05A1(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x06DF)]
    public class Server0x06DF : ServerUnresolvedRawPayload
    {
        public Server0x06DF(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x06F7)]
    public class Server0x06F7 : ServerUnresolvedRawPayload
    {
        public Server0x06F7(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.ServerRaidQueueStatus)]
    public class ServerRaidQueueStatus : ServerUnresolvedRawPayload
    {
        public ServerRaidQueueStatus(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x074A)]
    public class Server0x074A : ServerUnresolvedRawPayload
    {
        public Server0x074A(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x077E)]
    public class Server0x077E : ServerUnresolvedRawPayload
    {
        public Server0x077E(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x07D5)]
    public class Server0x07D5 : ServerUnresolvedRawPayload
    {
        public Server0x07D5(byte[] payload = null) : base(0x14u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0846)]
    public class Server0x0846 : ServerUnresolvedRawPayload
    {
        public Server0x0846(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x084B)]
    public class Server0x084B : ServerUnresolvedRawPayload
    {
        public Server0x084B(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0855)]
    public class Server0x0855 : ServerUnresolvedRawPayload
    {
        public Server0x0855(byte[] payload = null) : base(0xCu, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0889)]
    public class Server0x0889 : IWritable
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

    [Message(GameMessageOpcode.Server0x08CC)]
    public class Server0x08CC : IWritable
    {
        public uint Value { get; set; }
        public string Text { get; set; } = "";

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value);
            writer.WriteStringWide(Text);
        }
    }

    [Message(GameMessageOpcode.Server0x08F4)]
    public class Server0x08F4 : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1, 5u);
            writer.Write(Value2);
        }
    }

    [Message(GameMessageOpcode.Server0x0939)]
    public class Server0x0939 : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public string Text { get; set; } = "";

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1, 14u);
            writer.Write(Value2, 18u);
            writer.WriteStringWide(Text);
        }
    }

    [Message(GameMessageOpcode.Server0x093D)]
    public class Server0x093D : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1, 5u);
            writer.Write(Value2);
            writer.Write(Value3);
        }
    }

    [Message(GameMessageOpcode.Server0x093E)]
    public class Server0x093E : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public ulong Value2 { get; set; }

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
        public uint Unknown0 { get; set; }
        public AccountInventoryItem AccountItem { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Unknown0);
            AccountItem.Write(writer);
        }
    }

    [Message(GameMessageOpcode.ServerAccountItemCacheListAppend)]
    public class ServerAccountItemCacheListAppend : IWritable
    {
        public uint Unknown0 { get; set; }
        public List<AccountInventoryItem> AccountItems { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Unknown0);
            writer.Write(AccountItems.Count);
            AccountItems.ForEach(i => i.Write(writer));
        }
    }

    [Message(GameMessageOpcode.ServerAccountItemCacheRemove)]
    public class ServerAccountItemCacheRemove : IWritable
    {
        public uint Unknown0 { get; set; }
        public ulong AccountInventoryItemId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Unknown0);
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
    public class ServerCREDDExchangeOrderCacheRows : ServerUnresolvedULongUInt14UInt7ListPayload
    {
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
