using System;

namespace NexusForever.Network.World.Message.Model
{
    internal static class RewardRotationWireValidation
    {
        private const uint MaxScheduleContentId = 0x3FFFu;
        private const uint MaxEntryStateTypeId = 0x7u;

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
    }
}
