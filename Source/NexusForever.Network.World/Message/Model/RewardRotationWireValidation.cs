using System;

namespace NexusForever.Network.World.Message.Model
{
    internal static class RewardRotationWireValidation
    {
        private const uint MaxScheduleContentId = 0x3FFFu;
        private const uint MaxEntryStateTypeId = 0x7u;
        private const uint MaxRewardRotationIndex = 0x6u;
        private const int MaxContentContextContentIds = 5;
        private const int MaxContentContextPayloadBytes = 0x28;

        public static void ValidateScheduleRow(ServerRewardRotationScheduleArray.ScheduleRow row, string packetName)
        {
            if (row == null)
                throw new InvalidOperationException($"{packetName} row must not be null.");

            if (row.ContentId > MaxScheduleContentId)
                throw new InvalidOperationException($"{packetName} content id {row.ContentId} exceeds the 14-bit wire limit.");

            if (!float.IsFinite(row.Duration))
                throw new InvalidOperationException($"{packetName} duration {row.Duration} must be finite.");

            if (row.RewardType is < 1 or > 3)
                throw new InvalidOperationException($"Unsupported {packetName} reward type {row.RewardType}.");
        }

        public static void ValidateEntryStateTypeId(uint typeId, string packetName)
        {
            if (typeId > MaxEntryStateTypeId)
                throw new InvalidOperationException($"{packetName} type id {typeId} exceeds the 3-bit wire limit.");
        }

        public static void ValidateEntryStateRewardTypeLane(byte rewardTypeLane, string packetName)
        {
            if (rewardTypeLane is < 1 or > 3)
                throw new InvalidOperationException($"{packetName} state/reward-type lane {rewardTypeLane} must be 1-3.");
        }

        public static void ValidateContentContext(ServerRewardRotationContentContext packet, string packetName)
        {
            if (packet == null)
                throw new InvalidOperationException($"{packetName} must not be null.");

            if (packet.RewardRotationIndex > MaxRewardRotationIndex)
                throw new InvalidOperationException($"{packetName} reward rotation index {packet.RewardRotationIndex} exceeds the supported 0-{MaxRewardRotationIndex} range.");

            if (packet.ContentIds == null)
                throw new InvalidOperationException($"{packetName} content ids must not be null.");

            if (packet.ContentIds.Count > MaxContentContextContentIds)
                throw new InvalidOperationException($"{packetName} carries {packet.ContentIds.Count} content ids; the 0x28-byte wire budget allows at most {MaxContentContextContentIds}.");

            int payloadBytes = CalculateContentContextPayloadBytes(packet.ContentIds.Count);
            if (payloadBytes > MaxContentContextPayloadBytes)
                throw new InvalidOperationException($"{packetName} serializes to {payloadBytes} bytes, exceeding the registered 0x28-byte payload size.");
        }

        private static int CalculateContentContextPayloadBytes(int contentIdCount)
        {
            int bitCount = 14 + (32 * 4) + (contentIdCount * 32) + 1;
            return (bitCount + 7) / 8;
        }
    }
}
