using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    // Item / inventory cluster
    [Message(GameMessageOpcode.ServerItemContextActionAck)]
    public class ServerItemContextActionAck : ServerUnresolvedRawPayload
    {
        public ServerItemContextActionAck(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.ServerItemUnlockUInt32Flag)]
    public class ServerItemUnlockUInt32Flag : ServerUnresolvedUIntFlagPayload
    {
        public ServerItemUnlockUInt32Flag(uint value = 0u, bool flag = false) : base(value, flag) { }
    }

    [Message(GameMessageOpcode.ServerSupplySatchelAux)]
    public class ServerSupplySatchelAux : ServerUnresolvedRawPayload
    {
        public ServerSupplySatchelAux(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.ServerCostumeItemAux)]
    public class ServerCostumeItemAux : ServerUnresolvedRawPayload
    {
        public ServerCostumeItemAux(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.ServerItemSwapAux)]
    public class ServerItemSwapAux : IWritable
    {
        public ItemDragDrop DragDrop { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            // Client reader ServerItemSwapAux_ReadPayload (WildStar64.exe 1400a48d0)
            // reads one ItemDragDrop row (two uint64 fields). ServerItemSwap @ 0x0568
            // reuses the same pair twice inside ServerItemSwap_ReadPayload @ 1400a4840.
            DragDrop.Write(writer);
        }
    }

    // Options / keybind cluster (ClientOptions 0x012B)
    [Message(GameMessageOpcode.ServerOptionAuxPayload)]
    public class ServerOptionAuxPayload : ServerUnresolvedRawPayload
    {
        public ServerOptionAuxPayload(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.ServerOptionAuxPayloadLarge)]
    public class ServerOptionAuxPayloadLarge : ServerUnresolvedRawPayload
    {
        public ServerOptionAuxPayloadLarge(byte[] payload = null) : base(0x28u, payload) { }
    }

    [Message(GameMessageOpcode.ServerOptionAuxPayloadMedium)]
    public class ServerOptionAuxPayloadMedium : ServerUnresolvedRawPayload
    {
        public ServerOptionAuxPayloadMedium(byte[] payload = null) : base(0x18u, payload) { }
    }

    // Path / scientist cluster
    [Message(GameMessageOpcode.ServerPathMissionAuxByte)]
    public class ServerPathMissionAuxByte : ServerUnresolvedRawPayload
    {
        public ServerPathMissionAuxByte(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.ServerPathScientistAuxUInt32)]
    public class ServerPathScientistAuxUInt32 : ServerUnresolvedRawPayload
    {
        public ServerPathScientistAuxUInt32(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.ServerInstanceResetAux)]
    public class ServerInstanceResetAux : ServerUnresolvedRawPayload
    {
        public ServerInstanceResetAux(byte[] payload = null) : base(0x8u, payload) { }
    }

    // Entity select cluster
    [Message(GameMessageOpcode.ServerEntitySelectAuxUInt32)]
    public class ServerEntitySelectAuxUInt32 : ServerUnresolvedRawPayload
    {
        public ServerEntitySelectAuxUInt32(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.ServerEntitySelectAuxByte)]
    public class ServerEntitySelectAuxByte : ServerUnresolvedRawPayload
    {
        public ServerEntitySelectAuxByte(byte[] payload = null) : base(0x1u, payload) { }
    }

    // Reputation follow-up cluster (after ServerReputationUpdate 0x01A5)
    [Message(GameMessageOpcode.ServerReputationAuxUInt32)]
    public class ServerReputationAuxUInt32 : ServerUnresolvedRawPayload
    {
        public ServerReputationAuxUInt32(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.ServerReputationAuxRow)]
    public class ServerReputationAuxRow : ServerUnresolvedRawPayload
    {
        public ServerReputationAuxRow(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.ServerReputationAuxRowAlt)]
    public class ServerReputationAuxRowAlt : ServerUnresolvedRawPayload
    {
        public ServerReputationAuxRowAlt(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.ServerReputationAuxUInt64)]
    public class ServerReputationAuxUInt64 : ServerUnresolvedRawPayload
    {
        public ServerReputationAuxUInt64(byte[] payload = null) : base(0x8u, payload) { }
    }

    // Vehicle cluster
    [Message(GameMessageOpcode.ServerVehicleEmbarkAux)]
    public class ServerVehicleEmbarkAux : ServerUnresolvedRawPayload
    {
        public ServerVehicleEmbarkAux(byte[] payload = null) : base(0x10u, payload) { }
    }

    // Chat cluster
    [Message(GameMessageOpcode.ServerChatAuxBulk)]
    public class ServerChatAuxBulk : ServerUnresolvedRawPayload
    {
        public ServerChatAuxBulk(byte[] payload = null) : base(0x68u, payload) { }
    }

    [Message(GameMessageOpcode.ServerChatAuxPayload)]
    public class ServerChatAuxPayload : ServerUnresolvedRawPayload
    {
        public ServerChatAuxPayload(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.ServerChatAuxPayloadAlt)]
    public class ServerChatAuxPayloadAlt : ServerUnresolvedRawPayload
    {
        public ServerChatAuxPayloadAlt(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.ServerChatAuxNotification)]
    public class ServerChatAuxNotification : ServerUnresolvedRawPayload
    {
        public ServerChatAuxNotification(byte[] payload = null) : base(0x10u, payload) { }
    }

    // Marketplace cluster
    [Message(GameMessageOpcode.ServerAuctionPostAux)]
    public class ServerAuctionPostAux : ServerUnresolvedRawPayload
    {
        public ServerAuctionPostAux(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.ServerAuctionsByFilterAux)]
    public class ServerAuctionsByFilterAux : ServerUnresolvedRawPayload
    {
        public ServerAuctionsByFilterAux(byte[] payload = null) : base(0x14u, payload) { }
    }

    // Public event cluster
    [Message(GameMessageOpcode.ServerPublicEventAuxRaw)]
    public class ServerPublicEventAuxRaw : IWritable
    {
        public uint Value { get; set; }
        public List<uint> Values { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            // Client reader ServerPublicEventAux_ReadPayload (WildStar64.exe 14007b930):
            // uint32 value, 5-bit count, then count uint32 values. Semantic producer
            // meaning remains blocked per public-event surface.
            writer.Write(Value);
            writer.Write(Values.Count, 5u);
            foreach (uint value in Values)
                writer.Write(value);
        }
    }

    [Message(GameMessageOpcode.ServerPublicEventVoteAux)]
    public class ServerPublicEventVoteAux : IWritable
    {
        public ushort Value { get; set; }
        public bool Flag { get; set; }

        public void Write(GamePacketWriter writer)
        {
            // Client reader ServerPublicEventVoteAux_ReadPayload (WildStar64.exe
            // 14007c0c0): 15-bit value plus one flag. Vote lifecycle semantics remain
            // blocked until the producer/consumer sequence is mapped.
            writer.Write(Value, 15u);
            writer.Write(Flag);
        }
    }

    // Story / realm / recruitment / misc
    [Message(GameMessageOpcode.ServerStoryCommunicatorAux)]
    public class ServerStoryCommunicatorAux : ServerUnresolvedRawPayload
    {
        public ServerStoryCommunicatorAux(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.ServerRealmTransferDestinationsAux)]
    public class ServerRealmTransferDestinationsAux : ServerUnresolvedRawPayload
    {
        public ServerRealmTransferDestinationsAux(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.ServerRecruitmentAuxFourUInt32)]
    public class ServerRecruitmentAuxFourUInt32 : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
        }
    }

    [Message(GameMessageOpcode.ServerTimeOfDayAuxUInt32)]
    public class ServerTimeOfDayAuxUInt32 : ServerUnresolvedRawPayload
    {
        public ServerTimeOfDayAuxUInt32(byte[] payload = null) : base(0x4u, payload) { }
    }

    // Single-byte aux
    [Message(GameMessageOpcode.ServerDatacubeAuxByte)]
    public class ServerDatacubeAuxByte : ServerUnresolvedRawPayload
    {
        public ServerDatacubeAuxByte(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.ServerDuelAuxByte)]
    public class ServerDuelAuxByte : ServerUnresolvedRawPayload
    {
        public ServerDuelAuxByte(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.ServerResurrectionAuxByte)]
    public class ServerResurrectionAuxByte : ServerUnresolvedRawPayload
    {
        public ServerResurrectionAuxByte(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.ServerAppearanceAuxByte)]
    public class ServerAppearanceAuxByte : ServerUnresolvedRawPayload
    {
        public ServerAppearanceAuxByte(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.ServerLootAuxByte)]
    public class ServerLootAuxByte : ServerUnresolvedRawPayload
    {
        public ServerLootAuxByte(byte[] payload = null) : base(0x1u, payload) { }
    }
}
