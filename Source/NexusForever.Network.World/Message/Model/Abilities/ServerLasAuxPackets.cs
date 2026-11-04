using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Abilities
{
    // Client reader ServerUInt18_ReadPayload @ 140080d30 (FUN_14006c290 registration 0x00B0).
    // Shared 18-bit Spell4Id stub also used by unlock mount/vanity pet and 0x0815.
    [Message(GameMessageOpcode.ServerLasOpeningUInt18)]
    public class ServerLasOpeningUInt18 : IWritable
    {
        public uint Spell4Id { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Spell4Id, 18u);
        }
    }

    // Client reader FUN_14008dc30 @ 14008dc30 (FUN_14006c290 registration 0x016B, object size 0x20).
    // Wire: 4-bit field, 32-bit field, 4-bit count, count uint32 values, count uint32 values.
    [Message(GameMessageOpcode.ServerActionSetDualUInt32Lists)]
    public class ServerActionSetDualUInt32Lists : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public List<uint> ValuesA { get; } = [];
        public List<uint> ValuesB { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            if (ValuesA.Count != ValuesB.Count)
                throw new InvalidOperationException($"{nameof(ServerActionSetDualUInt32Lists)} requires matching list lengths.");

            writer.Write(Value0, 4u);
            writer.Write(Value1);
            writer.Write(ValuesA.Count, 4u);
            foreach (uint value in ValuesA)
                writer.Write(value);
            foreach (uint value in ValuesB)
                writer.Write(value);
        }
    }

    // Client reader LAB_14008ed80 (FUN_14006c290 registration 0x016D): one 16-bit field.
    [Message(GameMessageOpcode.ServerActionSetUInt16)]
    public class ServerActionSetUInt16 : IWritable
    {
        public ushort Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value, 16u);
        }
    }

    // Client reader ServerUInt32_ReadPayload (FUN_14006c290 registration 0x016E).
    // krakal/abilities-part2 labels this as ServerAbilityTierPointsBonus, but producer semantics remain unverified here.
    [Message(GameMessageOpcode.ServerActionSetUInt32)]
    public class ServerActionSetUInt32 : IWritable
    {
        public uint Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value);
        }
    }

    // 0x019C replaces the unlocked inlaid AMP list and raises CharacterUnlockedInlaidEldanAugmentation.
    // The client reader at 0x14008e1f0 uses a 7-bit count followed by ushort IDs.
    [Message(GameMessageOpcode.ServerActionSetUInt16List)]
    public class ServerActionSetUInt16List : IWritable
    {
        // The apply handler at 0x1403d1c00 clamps the visible count but copies all incoming rows.
        public const int MaxUnlockedInlaidAugmentations = 54;

        public List<ushort> UnlockedInlaidAugmentationIds { get; } = [];
        public List<ushort> Values => UnlockedInlaidAugmentationIds;

        public void Write(GamePacketWriter writer)
        {
            if (UnlockedInlaidAugmentationIds.Count > MaxUnlockedInlaidAugmentations)
                throw new InvalidOperationException("Unlocked inlaid AMP list exceeds the client capacity.");

            writer.Write(UnlockedInlaidAugmentationIds.Count, 7u);
            foreach (ushort id in UnlockedInlaidAugmentationIds)
                writer.Write(id);
        }
    }

    // Client reader FUN_1400819b0 @ 1400819b0 (FUN_14006c290 registration 0x01A4, object size 0x14).
    // Also registered for 0x01AC. Five consecutive 32-bit fields.
    [Message(GameMessageOpcode.ServerActionSetFiveUInt32)]
    public class ServerActionSetFiveUInt32 : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }
        public uint Value4 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
            writer.Write(Value4);
        }
    }
}
