using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pregame;
using NetworkMessage = NexusForever.Network.Message.Model.Shared.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.Client0x003D)]
    public class Client0x003D : RealmInfo.AccountRealmData, IReadable
    {
        /// <summary>
        /// Opcode 0x003D. Native registration in <c>ClientWorldOpcodeRegister_MovementSpline</c>
        /// @ <c>1400a8190</c> binds a <c>0x18</c>-byte client slot with writer
        /// <c>Client0x003D_WritePayload</c> @ <c>1400aba70</c>. The writer serialises one
        /// 14-bit field, one uint32, one wide string, and one trailing uint64. That exact
        /// row shape matches <see cref="RealmInfo.AccountRealmData"/> inside
        /// <see cref="Client0x0760"/> realm rows, but standalone packet semantics remain
        /// unresolved.
        /// </summary>
    }

    [Message(GameMessageOpcode.Client0x00C8)]
    public class Client0x00C8 : IReadable
    {
        /// <summary>
        /// Opcode 0x00C8. Native client registration binds this unresolved packet to shared local
        /// writer slot <c>LAB_14008a150</c> and reader slot <c>LAB_14008a140</c> with registered
        /// size <c>4</c>, the same structural slot reused by matching queue-leave opcodes
        /// <c>0x05B5</c> and <c>0x05B6</c>. The reader advances a 5-bit field, so the proven
        /// wire surface matches the queue-leave <c>MatchType</c> payload even though the owner
        /// remains unresolved.
        /// </summary>
        public Game.Static.Matching.MatchType MatchType { get; private set; }

        public void Read(GamePacketReader reader)
        {
            MatchType = reader.ReadEnum<Game.Static.Matching.MatchType>(5u);
        }
    }

    [Message(GameMessageOpcode.Client0x00ED)]
    public class Client0x00ED : IReadable
    {
        /// <summary>
        /// Opcode 0x00ED. Native client registration binds this unresolved packet to
        /// <c>ClientUnresolvedDiagnosticPacket00ED_WritePayload</c> (<c>1400a6200</c>) with
        /// registered size <c>0x20</c>. The writer emits one uint64, one uint32, one uint64,
        /// and three trailing bits. Exact <c>0xED</c> immediate scans only surfaced mail-side
        /// <c>GameFormula_GetEntryById(0xed)</c> lookups in helpers around
        /// <c>ClientMailSend</c> (<c>0x0168</c>), not a packet owner for opcode <c>0x00ED</c>,
        /// so the packet remains numerically named.
        /// </summary>
        public ulong Value0 { get; private set; }

        public uint Value1 { get; private set; }

        public ulong Value2 { get; private set; }

        public bool Value3 { get; private set; }

        public bool Value4 { get; private set; }

        public bool Value5 { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value0 = reader.ReadULong();
            Value1 = reader.ReadUInt();
            Value2 = reader.ReadULong();
            Value3 = reader.ReadBit();
            Value4 = reader.ReadBit();
            Value5 = reader.ReadBit();
        }
    }

    [Message(GameMessageOpcode.Client0x011B)]
    public class Client0x011B : IReadable
    {
        /// <summary>
        /// Opcode 0x011B. Native client registration binds this packet to shared zero-payload
        /// <c>ClientCraftingAbandon_WritePayload</c> (<c>140001ba0</c>), the same no-op writer
        /// reused by named empty requests such as <c>ClientPathScientistDismissScanbot</c>
        /// (<c>0x00F0</c>), <c>ClientPathScientistDismissScanbotPathAction</c> (<c>0x015F</c>),
        /// and <c>ClientLootVacuum</c> (<c>0x01AD</c>). The request owner remains unresolved.
        /// </summary>

        public void Read(GamePacketReader reader)
        {
        }
    }

    [Message(GameMessageOpcode.Client0x011D)]
    public class Client0x011D : IReadable
    {
        /// <summary>
        /// Opcode 0x011D. Native client registration binds this packet to shared
        /// <c>ClientTradeskillResetTalents_WritePayload</c> (<c>14007d010</c>), so the proven
        /// wire surface remains one raw uint32 field.
        /// </summary>
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [Message(GameMessageOpcode.Client0x012D)]
    public class Client0x012D : IReadable
    {
        /// <summary>
        /// Opcode 0x012D. Native client registration binds this packet to shared
        /// <c>ClientSuggest_WritePayload</c> (<c>14007ae80</c>), the same one-wide-string
        /// serializer reused by <c>ClientSuggest</c> (<c>0x0833</c>),
        /// <c>ClientAccountItemClaimPendingItemGroup</c> (<c>0x0233</c>), and
        /// <c>ClientAccountItemReturnPendingItemGroup</c> (<c>0x07C6</c>), and
        /// <c>Client0x063E</c>. The owner remains unresolved, so the model stays structurally
        /// named.
        /// </summary>
        public string Text { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Text = reader.ReadWideString();
        }
    }

    [Message(GameMessageOpcode.Client0x0550)]
    public class Client0x0550 : IReadable
    {
        /// <summary>
        /// Opcode 0x0550. Native client registration binds this packet to shared
        /// <c>ClientTradeskillResetTalents_WritePayload</c> (<c>14007d010</c>), so the proven
        /// wire surface remains one raw uint32 field.
        /// </summary>
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [Message(GameMessageOpcode.Client0x062A)]
    public class Client0x062A : IReadable
    {
        /// <summary>
        /// Opcode 0x062A. Native client registration in <c>ClientWorldOpcodeRegister_MovementSpline</c>
        /// (<c>1400a8190</c>) binds this packet to shared
        /// <c>ClientTradeskillResetTalents_WritePayload</c> (<c>14007d010</c>), so the proven
        /// wire surface remains one raw uint32 field.
        /// </summary>
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [Message(GameMessageOpcode.Client0x0634)]
    public class Client0x0634 : IReadable
    {
        /// <summary>
        /// Opcode 0x0634. Native client registration in <c>ClientWorldOpcodeRegister_MovementSpline</c>
        /// (<c>1400a8190</c>) binds this packet to shared
        /// <c>ClientTradeskillResetTalents_WritePayload</c> (<c>14007d010</c>), so the proven
        /// wire surface remains one raw uint32 field.
        /// </summary>
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [Message(GameMessageOpcode.Client0x063E)]
    public class Client0x063E : IReadable
    {
        /// <summary>
        /// Opcode 0x063E. Native client registration in <c>ClientWorldOpcodeRegister_MovementSpline</c>
        /// (<c>1400a8190</c>) binds this packet to shared
        /// <c>ClientSuggest_WritePayload</c> (<c>14007ae80</c>), so the proven wire surface remains
        /// one wide-string field.
        /// </summary>
        public string Text { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Text = reader.ReadWideString();
        }
    }

    [Message(GameMessageOpcode.Client0x0701)]
    public class Client0x0701 : IReadable
    {
        /// <summary>
        /// Opcode 0x0701. Native writer <c>ClientUInt2UInt32_WritePayload</c> @ <c>1400a69d0</c>
        /// serialises one 2-bit field followed by one uint32 inside the registered 8-byte slot.
        /// Packet semantics remain unresolved.
        /// </summary>
        public uint LeadingBits { get; private set; }

        public uint TrailingValue { get; private set; }

        public void Read(GamePacketReader reader)
        {
            LeadingBits   = reader.ReadUInt(2u);
            TrailingValue = reader.ReadUInt(32u);
        }
    }

    [Message(GameMessageOpcode.Client0x0760)]
    public class Client0x0760 : RealmInfo, IReadable
    {
        /// <summary>
        /// Opcode 0x0760. Native registration in <c>ClientWorldOpcodeRegister_MovementSpline</c>
        /// @ <c>1400a8190</c> binds a <c>0x58</c>-byte client slot with writer
        /// <c>Client0x0760_WritePayload</c> @ <c>1400abd30</c>. Wire layout matches one
        /// <see cref="RealmInfo"/> row with the same field order used by
        /// <see cref="ServerRealmList"/> realm entries. The source event that sends or consumes
        /// this client-side realm row remains unresolved, so the model stays numerically named.
        /// </summary>
        public RealmInfo Realm => this;
    }

    [Message(GameMessageOpcode.Client0x0762)]
    public class Client0x0762 : NetworkMessage, IReadable
    {
        /// <summary>
        /// Opcode 0x0762. Native registration in <c>ClientWorldOpcodeRegister_MovementSpline</c>
        /// @ <c>1400a8190</c> binds a <c>0x10</c>-byte client slot with writer
        /// <c>Client0x0762_WritePayload</c> @ <c>1400ac410</c>. Wire layout matches one
        /// <see cref="NetworkMessage"/> row with the same field order used by
        /// <see cref="ServerRealmList"/> messages. The source event that sends or consumes
        /// this client-side message row remains unresolved, so the model stays numerically named.
        /// </summary>
        public NetworkMessage MessageRow => this;
    }

    [Message(GameMessageOpcode.ClientAddonModuleList)]
    public class ClientAddonModuleList : IReadable
    {
        public class Row
        {
            public uint ModuleNibble { get; private set; }

            public bool ModuleFlag { get; private set; }

            public byte ModuleByte { get; private set; }

            public string ModuleName { get; private set; }

            internal static Row Read(GamePacketReader reader)
            {
                return new Row
                {
                    ModuleNibble = reader.ReadUInt(4u),
                    ModuleFlag   = reader.ReadBit(),
                    ModuleByte   = reader.ReadByte(),
                    ModuleName   = reader.ReadWideString()
                };
            }
        }

        /// <summary>
        /// Opcode 0x07B6. Native client registration binds this unresolved packet to
        /// <c>ClientUnresolvedDiagnosticPacket07B6_WritePayload</c> (<c>140080220</c>) with
        /// registered size <c>0x20</c>. The writer emits four uint32 fields, one uint32 row
        /// count, and that many rows via <c>14007ff70</c> with the shape { 4-bit value, bit,
        /// byte, wide string }. Sender and Lua_GetAddons walk are mapped (1403f42e0 / 140043370),
        /// so this model keeps structural addon-module naming while gameplay semantics stay blocked.
        /// </summary>
        public uint HeaderValue0 { get; private set; }

        public uint HeaderValue1 { get; private set; }

        public uint HeaderValue2 { get; private set; }

        public uint HeaderValue3 { get; private set; }

        public uint ModuleCount { get; private set; }

        public List<Row> Modules { get; } = new();

        public void Read(GamePacketReader reader)
        {
            HeaderValue0 = reader.ReadUInt();
            HeaderValue1 = reader.ReadUInt();
            HeaderValue2 = reader.ReadUInt();
            HeaderValue3 = reader.ReadUInt();
            ModuleCount  = reader.ReadUInt();

            Modules.Clear();
            for (uint i = 0; i < ModuleCount; i++)
                Modules.Add(Row.Read(reader));
        }
    }

    [Message(GameMessageOpcode.Client0x07E3)]
    public class Client0x07E3 : IReadable
    {
        /// <summary>
        /// Opcode 0x07E3. Native client registration in <c>ClientWorldOpcodeRegister_MovementSpline</c>
        /// (<c>1400a8190</c>) binds this packet to shared
        /// <c>ClientTradeskillResetTalents_WritePayload</c> (<c>14007d010</c>), so the proven
        /// wire surface remains one raw uint32 field.
        /// </summary>
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [Message(GameMessageOpcode.Client0x0928)]
    public class Client0x0928 : IReadable
    {
        /// <summary>
        /// Opcode 0x0928. Native writer <c>ClientUInt32UInt5_WritePayload</c> @ <c>1400898b0</c>
        /// serialises one uint32 followed by one 5-bit field inside the registered 8-byte slot.
        /// The same helper is used by <c>ClientPetSetStance</c> (<c>0x068E</c>), but the
        /// sender/consumer for this opcode remains unresolved.
        /// </summary>
        public uint LeadingValue { get; private set; }

        public uint TrailingBits { get; private set; }

        public void Read(GamePacketReader reader)
        {
            LeadingValue = reader.ReadUInt(32u);
            TrailingBits = reader.ReadUInt(5u);
        }
    }
}
