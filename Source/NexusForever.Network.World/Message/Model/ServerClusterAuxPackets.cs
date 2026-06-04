using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    // Item / inventory cluster
    [Message(GameMessageOpcode.ServerItemContextActionAck)]
    public class ServerItemContextActionAck : ServerUnresolvedEmptyPayload
    {
    }

    [Message(GameMessageOpcode.ServerItemUnlockUInt32Flag)]
    public class ServerItemUnlockUInt32Flag : ServerUnresolvedUIntFlagPayload
    {
        public ServerItemUnlockUInt32Flag(uint value = 0u, bool flag = false) : base(value, flag) { }
    }

    [Message(GameMessageOpcode.ServerSupplySatchelAux)]
    public class ServerSupplySatchelAux : IWritable
    {
        /// <summary>
        /// Native registration binds <c>0x019A</c> to shared
        /// <c>MatchingQueueResultWaitTime_ReadPayload</c> (<c>14007fcf0</c>),
        /// which reads one 6-bit field plus one uint32. Supply-satchel
        /// semantics remain blocked.
        /// </summary>
        public uint UInt6Value { get; set; }

        public uint Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UInt6Value, 6u);
            writer.Write(Value);
        }
    }

    [Message(GameMessageOpcode.ServerCostumeItemAux)]
    public class ServerCostumeItemAux : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerCostumeItemAux_ReadPayload</c>
        /// (<c>1400874a0</c>) reads one 14-bit value, three uint32 fields,
        /// and two trailing flags. Costume/emote semantics remain blocked.
        /// </summary>
        public uint UInt14Value { get; set; }

        public uint Value0 { get; set; }

        public uint Value1 { get; set; }

        public uint Value2 { get; set; }

        public bool Flag0 { get; set; }

        public bool Flag1 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UInt14Value, 14u);
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Flag0);
            writer.Write(Flag1);
        }
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

    [Message(GameMessageOpcode.ServerItemModdableData)]
    public class ServerItemModdableData : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerItemModdableData_ReadPayload</c>
        /// (<c>1400a3ce0</c>) reads item guid, threshold data, random glyph
        /// data, and random circuit data. Producer timing remains blocked.
        /// </summary>
        public ulong ItemGuid { get; set; }

        public ulong ThresholdData { get; set; }

        public uint RandomGlyphData { get; set; }

        public ulong RandomCircuitData { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ItemGuid);
            writer.Write(ThresholdData);
            writer.Write(RandomGlyphData);
            writer.Write(RandomCircuitData);
        }
    }

    [Message(GameMessageOpcode.ServerItemMicrochips)]
    public class ServerItemMicrochips : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerItemMicrochips_ReadPayload</c>
        /// (<c>1400a3d50</c>) reads item guid, maker character id, random
        /// circuit data, an 18-bit power-core item id, a 3-bit count, and
        /// counted microchip item ids. This is a server-side item patch/update
        /// surface, not the client rune install request (<c>0x085B</c>);
        /// producer timing remains blocked.
        /// </summary>
        public ulong ItemGuid { get; set; }

        public ulong MakerCharacterId { get; set; }

        public ulong RandomCircuitData { get; set; }

        public uint PowerCoreItem2Id { get; set; }

        public List<uint> MicrochipItem2Ids { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            if (MicrochipItem2Ids.Count > 0x7)
                throw new InvalidOperationException("Item microchip count exceeds the 3-bit wire limit.");

            writer.Write(ItemGuid);
            writer.Write(MakerCharacterId);
            writer.Write(RandomCircuitData);
            writer.Write(PowerCoreItem2Id, 18u);
            writer.Write((byte)MicrochipItem2Ids.Count, 3u);
            writer.WriteRetailCompositeUInt32Array(MicrochipItem2Ids.ToArray());
        }
    }

    [Message(GameMessageOpcode.ServerItemGlyphs)]
    public class ServerItemGlyphs : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerItemGlyphs_ReadPayload</c>
        /// (<c>1400a3e40</c>) reads item guid, random glyph data, a 4-bit
        /// count, and counted glyph item ids. Producer timing remains blocked.
        /// </summary>
        public ulong ItemGuid { get; set; }

        public uint RandomGlyphData { get; set; }

        public List<uint> GlyphItem2Ids { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            if (GlyphItem2Ids.Count > 0xF)
                throw new InvalidOperationException("Item glyph count exceeds the 4-bit wire limit.");

            writer.Write(ItemGuid);
            writer.Write(RandomGlyphData);
            writer.Write((byte)GlyphItem2Ids.Count, 4u);
            writer.WriteRetailCompositeUInt32Array(GlyphItem2Ids.ToArray());
        }
    }

    // Path / scientist cluster
    [Message(GameMessageOpcode.ServerPathMissionAuxEmpty)]
    public class ServerPathMissionAuxEmpty : ServerUnresolvedEmptyPayload
    {
    }

    [Message(GameMessageOpcode.ServerPathScientistAuxUInt32)]
    public class ServerPathScientistAuxUInt32 : ServerUnresolvedUIntPayload
    {
        public ServerPathScientistAuxUInt32(uint value = 0u) : base(32u, value) { }
    }

    [Message(GameMessageOpcode.ServerInstanceResetAux)]
    public class ServerInstanceResetAux : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerInstanceResetAux_ReadPayload</c>
        /// (<c>14008e030</c>) reads one 4-bit field followed by one uint32.
        /// Instance-reset producer semantics remain blocked.
        /// </summary>
        public uint UInt4Value { get; set; }

        public uint Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UInt4Value, 4u);
            writer.Write(Value);
        }
    }

    // Entity select cluster
    [Message(GameMessageOpcode.ServerEntitySelectAuxUInt14)]
    public class ServerEntitySelectAuxUInt14 : ServerUnresolvedUIntPayload
    {
        public ServerEntitySelectAuxUInt14(uint value = 0u) : base(14u, value) { }
    }

    [Message(GameMessageOpcode.ServerEntitySelectAuxEmpty)]
    public class ServerEntitySelectAuxEmpty : ServerUnresolvedEmptyPayload
    {
    }

    // Reputation follow-up cluster (after ServerReputationUpdate 0x01A5)
    [Message(GameMessageOpcode.ServerReputationAuxUInt32)]
    public class ServerReputationAuxUInt32 : ServerUnresolvedUIntPayload
    {
        public ServerReputationAuxUInt32(uint value = 0u) : base(32u, value) { }
    }

    [Message(GameMessageOpcode.ServerReputationAuxUInt64UInt32)]
    public class ServerReputationAuxUInt64UInt32 : IWritable
    {
        /// <summary>
        /// Client reader <c>14008ef80</c> reads one uint64 field plus one
        /// uint32 field. Reputation/path-XP semantics remain blocked.
        /// </summary>
        public ulong Value0 { get; set; }

        public uint Value1 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
        }
    }

    [Message(GameMessageOpcode.ServerReputationAuxUInt64UInt32Alt)]
    public class ServerReputationAuxUInt64UInt32Alt : IWritable
    {
        /// <summary>
        /// Shares <c>ServerHousingResidenceKeyedUpdate_ReadPayload</c>
        /// (<c>14008de20</c>): one uint64 field plus one uint32 field.
        /// </summary>
        public ulong Value0 { get; set; }

        public uint Value1 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
        }
    }

    [Message(GameMessageOpcode.ServerReputationAuxUInt14UInt32)]
    public class ServerReputationAuxUInt14UInt32 : IWritable
    {
        /// <summary>
        /// Client reader <c>14008d430</c>, also used by
        /// <see cref="GameMessageOpcode.ServerReputationUpdate"/>, reads one
        /// 14-bit value followed by one uint32 field.
        /// </summary>
        public uint Value0 { get; set; }

        public uint Value1 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0, 14u);
            writer.Write(Value1);
        }
    }

    // Vehicle cluster
    [Message(GameMessageOpcode.ServerVehicleEmbarkAux)]
    public class ServerVehicleEmbarkAux : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerVehicleEmbarkAux_ReadPayload</c> (<c>14008ff20</c>)
        /// reads one flag, one 2-bit value, one uint64, and two uint32 fields.
        /// Vehicle/passenger semantics remain blocked.
        /// </summary>
        public bool Flag { get; set; }

        public uint UInt2Value { get; set; }

        public ulong Value0 { get; set; }

        public uint Value1 { get; set; }

        public uint Value2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Flag);
            writer.Write(UInt2Value, 2u);
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
        }
    }

    // Chat cluster
    [Message(GameMessageOpcode.ServerChatAuxBulk)]
    public class ServerChatAuxBulk : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerChatAuxRow_ReadPayload</c>
        /// (<c>140085ca0</c>) reads one 4-bit variant, two 16-bit header
        /// fields, then dispatches through <c>PTR_LAB_140c1ec90</c>.
        /// </summary>
        public ServerChatAuxRow Row { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            Row.Write(writer);
        }
    }

    [Message(GameMessageOpcode.ServerChatAuxPayload)]
    public class ServerChatAuxPayload : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerChatAuxPayload_ReadPayload</c>
        /// (<c>140086410</c>) reads a wide string, a 5-bit row count,
        /// chat rows, one flag, and a trailing 16-bit value.
        /// </summary>
        public string Text { get; set; } = "";

        public List<ServerChatAuxRow> Rows { get; } = [];

        public bool Flag { get; set; }

        public ushort Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.WriteStringWide(Text);
            writer.Write((uint)Rows.Count, 5u);
            foreach (ServerChatAuxRow row in Rows)
                row.Write(writer);

            writer.Write(Flag);
            writer.Write(Value);
        }
    }

    [Message(GameMessageOpcode.ServerChatAuxPayloadAlt)]
    public class ServerChatAuxPayloadAlt : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerChatAuxPayloadAlt_ReadPayload</c>
        /// (<c>140085fe0</c>) reads a wide string, a 5-bit row count,
        /// chat rows, and a trailing 16-bit value.
        /// </summary>
        public string Text { get; set; } = "";

        public List<ServerChatAuxRow> Rows { get; } = [];

        public ushort Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.WriteStringWide(Text);
            writer.Write((uint)Rows.Count, 5u);
            foreach (ServerChatAuxRow row in Rows)
                row.Write(writer);

            writer.Write(Value);
        }
    }

    public class ServerChatAuxRow : IWritable
    {
        public ushort Value0 { get; set; }

        public ushort Value1 { get; set; }

        public PayloadShape Payload { get; set; } = new BoolPayload();

        public void Write(GamePacketWriter writer)
        {
            PayloadShape payload = Payload ?? throw new InvalidOperationException("Chat aux row payload is required.");

            writer.Write(payload.Variant, 4u);
            writer.Write(Value0);
            writer.Write(Value1);
            payload.Write(writer);
        }

        public abstract class PayloadShape : IWritable
        {
            public abstract byte Variant { get; }

            public abstract void Write(GamePacketWriter writer);
        }

        public class BoolPayload : PayloadShape
        {
            private readonly byte variant;

            public BoolPayload(byte variant = 0)
            {
                if (variant is not (0 or 2 or 3))
                    throw new ArgumentOutOfRangeException(nameof(variant), variant, "Bool chat aux variants are 0, 2, and 3.");

                this.variant = variant;
            }

            public override byte Variant => variant;

            public bool Value { get; set; }

            public override void Write(GamePacketWriter writer)
            {
                writer.Write(Value);
            }
        }

        public class UInt32Payload : PayloadShape
        {
            private readonly byte variant;

            public UInt32Payload(byte variant = 1)
            {
                if (variant is not (1 or 7 or 11))
                    throw new ArgumentOutOfRangeException(nameof(variant), variant, "UInt32 chat aux variants are 1, 7, and 11.");

                this.variant = variant;
            }

            public override byte Variant => variant;

            public uint Value { get; set; }

            public override void Write(GamePacketWriter writer)
            {
                writer.Write(Value);
            }
        }

        public class UInt18Payload : PayloadShape
        {
            public override byte Variant => 4;

            public uint Value { get; set; }

            public override void Write(GamePacketWriter writer)
            {
                writer.Write(Value, 18u);
            }
        }

        public class UInt15Payload : PayloadShape
        {
            public override byte Variant => 5;

            public uint Value { get; set; }

            public override void Write(GamePacketWriter writer)
            {
                writer.Write(Value, 15u);
            }
        }

        public class UInt14Payload : PayloadShape
        {
            public override byte Variant => 6;

            public uint Value { get; set; }

            public override void Write(GamePacketWriter writer)
            {
                writer.Write(Value, 14u);
            }
        }

        public class ComplexPayload : PayloadShape
        {
            public override byte Variant => 8;

            public ulong Value0 { get; set; }

            public uint UInt18Value1 { get; set; }

            public ulong Value2 { get; set; }

            public ulong Value3 { get; set; }

            public uint Value4 { get; set; }

            public ulong Value5 { get; set; }

            public uint Value6 { get; set; }

            public uint Value7 { get; set; }

            public byte Value8 { get; set; }

            public uint UInt18Value9 { get; set; }

            public List<uint> Values10 { get; } = [];

            public List<uint> Values11 { get; } = [];

            public override void Write(GamePacketWriter writer)
            {
                writer.Write(Value0);
                writer.Write(UInt18Value1, 18u);
                writer.Write(Value2);
                writer.Write(Value3);
                writer.Write(Value4);
                writer.Write(Value5);
                writer.Write(Value6);
                writer.Write(Value7);
                writer.Write(Value8);
                writer.Write(UInt18Value9, 18u);
                writer.Write((uint)Values10.Count, 3u);
                writer.WriteRetailCompositeUInt32Array(Values10.ToArray());
                writer.Write((uint)Values11.Count, 4u);
                writer.WriteRetailCompositeUInt32Array(Values11.ToArray());
            }
        }

        public class UInt64Payload : PayloadShape
        {
            public override byte Variant => 9;

            public ulong Value { get; set; }

            public override void Write(GamePacketWriter writer)
            {
                writer.Write(Value);
            }
        }

        public class UInt14TwoUInt32Payload : PayloadShape
        {
            public override byte Variant => 10;

            public uint UInt14Value { get; set; }

            public uint Value1 { get; set; }

            public uint Value2 { get; set; }

            public override void Write(GamePacketWriter writer)
            {
                writer.Write(UInt14Value, 14u);
                writer.Write(Value1);
                writer.Write(Value2);
            }
        }
    }

    [Message(GameMessageOpcode.ServerChatAuxNotification)]
    public class ServerChatAuxNotification : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerChatAuxNotification_ReadPayload</c>
        /// (<c>1400a0890</c>) reads one 8-bit row count and counted rows.
        /// Chat/cinematic semantics remain blocked.
        /// </summary>
        public List<Row> Rows { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(checked((byte)Rows.Count));
            foreach (Row row in Rows)
                row.Write(writer);
        }

        public class Row : IWritable
        {
            public uint Value0 { get; set; }
            public uint UInt14Value { get; set; }
            public ulong Value2 { get; set; }
            public uint Value3 { get; set; }
            public uint Value4 { get; set; }
            public byte Value5 { get; set; }
            public List<SubRow> SubRows { get; } = [];

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Value0);
                writer.Write(UInt14Value, 14u);
                writer.Write(Value2);
                writer.Write(Value3);
                writer.Write(Value4);
                writer.Write(Value5);
                writer.Write(checked((byte)SubRows.Count));
                foreach (SubRow subRow in SubRows)
                    subRow.Write(writer);
            }
        }

        public class SubRow : IWritable
        {
            public uint UInt5Value { get; set; }
            public uint Value1 { get; set; }
            public uint Value2 { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(UInt5Value, 5u);
                writer.Write(Value1);
                writer.Write(Value2);
            }
        }
    }

    // Marketplace cluster
    [Message(GameMessageOpcode.ServerAuctionPostAux)]
    public class ServerAuctionPostAux : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerAuctionPostAux_ReadPayload</c>
        /// (<c>140090090</c>) reads one count, that many uint32 values, that
        /// many bytes, and one trailing uint32 field. Auction producer
        /// semantics remain blocked.
        /// </summary>
        public List<uint> Values { get; } = [];

        public byte[] Data { get; set; } = [];

        public uint Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            byte[] data = Data ?? [];
            if (data.Length != Values.Count)
                throw new InvalidOperationException("Auction post aux byte data length must match the uint32 value count.");

            writer.Write((uint)Values.Count);
            writer.WriteRetailCompositeUInt32Array(Values.ToArray());
            writer.WriteRetailCompositeByteSpan(data);
            writer.Write(Value);
        }
    }

    [Message(GameMessageOpcode.ServerAuctionsByFilterAux)]
    public class ServerAuctionsByFilterAux : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerAuctionsByFilterAux_ReadPayload</c>
        /// (<c>14008fe80</c>) reads one 14-bit value, three uint32 fields,
        /// and one flag. Auction filter semantics remain blocked.
        /// </summary>
        public uint UInt14Value { get; set; }

        public uint Value1 { get; set; }

        public uint Value2 { get; set; }

        public uint Value3 { get; set; }

        public bool Flag { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UInt14Value, 14u);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
            writer.Write(Flag);
        }
    }

    // Public event cluster
    [Message(GameMessageOpcode.ServerPublicEventAux)]
    public class ServerPublicEventAux : IWritable
    {
        public uint Value { get; set; }
        public List<uint> Values { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            // Client reader ServerPublicEventAux_ReadPayload (WildStar64.exe 14007b930):
            // uint32 value, 5-bit count, then count uint32 values. Semantic producer
            // meaning remains blocked per public-event surface.
            if (Values.Count > 0x1F)
                throw new InvalidOperationException("Public event aux value count exceeds the 5-bit wire limit.");

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
    public class ServerStoryCommunicatorAux : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerStoryCommunicatorAux_ReadPayload</c>
        /// (<c>140080c80</c>) reads five uint32 fields followed by one
        /// 16-bit field. Story/communicator semantics remain blocked.
        /// </summary>
        public uint Value0 { get; set; }

        public uint Value1 { get; set; }

        public uint Value2 { get; set; }

        public uint Value3 { get; set; }

        public uint Value4 { get; set; }

        public ushort Value5 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
            writer.Write(Value4);
            writer.Write(Value5);
        }
    }

    [Message(GameMessageOpcode.ServerRealmTransferDestinationsAux)]
    public class ServerRealmTransferDestinationsAux : IWritable
    {
        /// <summary>
        /// Client reader <c>ServerRealmTransferDestinationsAux_ReadPayload</c>
        /// (<c>14007d790</c>) reads one leading uint32 and then a byte-counted
        /// pointer-backed payload. Payload semantics remain blocked.
        /// </summary>
        public uint Value { get; set; }

        public byte[] Data { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value);
            writer.Write(Data?.Length ?? 0);
            writer.WriteBytes(Data ?? []);
        }
    }

    [Message(GameMessageOpcode.ServerRecruitmentAuxUInt32List)]
    public class ServerRecruitmentAuxUInt32List : IWritable
    {
        /// <summary>
        /// Native registration binds <c>0x077E</c> to
        /// <c>ServerFlightPathUpdate_ReadPayload</c> (<c>14008eaa0</c>):
        /// one uint32 count and a counted uint32 array. Recruitment/pet
        /// boundary semantics remain blocked.
        /// </summary>
        public List<uint> Values { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write((uint)Values.Count);
            writer.WriteRetailCompositeUInt32Array(Values.ToArray());
        }
    }

    [Message(GameMessageOpcode.ServerTimeOfDayAuxUInt32)]
    public class ServerTimeOfDayAuxUInt32 : ServerUnresolvedUIntPayload
    {
        public ServerTimeOfDayAuxUInt32(uint value = 0u) : base(32u, value) { }
    }

    // Single-byte aux
    [Message(GameMessageOpcode.ServerDatacubeAuxEmpty)]
    public class ServerDatacubeAuxEmpty : ServerUnresolvedEmptyPayload
    {
    }

    [Message(GameMessageOpcode.ServerDuelAuxEmpty)]
    public class ServerDuelAuxEmpty : ServerUnresolvedEmptyPayload
    {
    }

    [Message(GameMessageOpcode.ServerResurrectionAuxEmpty)]
    public class ServerResurrectionAuxEmpty : ServerUnresolvedEmptyPayload
    {
    }

    [Message(GameMessageOpcode.ServerAppearanceAuxEmpty)]
    public class ServerAppearanceAuxEmpty : ServerUnresolvedEmptyPayload
    {
    }

    [Message(GameMessageOpcode.ServerLootAuxEmpty)]
    public class ServerLootAuxEmpty : ServerUnresolvedEmptyPayload
    {
    }
}
