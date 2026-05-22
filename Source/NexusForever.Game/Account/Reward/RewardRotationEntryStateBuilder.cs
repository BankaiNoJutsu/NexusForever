using System.Linq;
using NexusForever.Database.Auth.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Builds <see cref="ServerRewardRotationEntryStateArray"/> rows.
    /// Client <c>RewardRotation_ApplyEntryStateToLoadedContent</c> (14063a0e0) treats the wire
    /// <c>State</c> byte as the reward-type lane (1=item, 2=essence, 3=modifier) and
    /// <c>Value</c> as a grant-flag bitmask (1 for item/modifier, 0x80000000 for essence).
    /// </summary>
    public static class RewardRotationEntryStateBuilder
    {
        public const uint GrantFlagItemOrModifier = 1u;
        public const uint GrantFlagEssence = 0x80000000u;

        public static ServerRewardRotationEntryStateArray BuildEmpty()
        {
            return new ServerRewardRotationEntryStateArray();
        }

        public static ServerRewardRotationEntryStateArray.EntryStateRow CreateRow(
            uint contentTypeIndex,
            uint contentId,
            uint rewardKeyId,
            byte rewardTypeLane,
            uint grantFlags)
        {
            return new ServerRewardRotationEntryStateArray.EntryStateRow
            {
                TypeId = contentTypeIndex,
                ContentId = contentId,
                RewardTypeId = rewardKeyId,
                State = rewardTypeLane,
                Value = grantFlags
            };
        }

        public static uint GetGrantFlagsForRewardType(byte rewardType)
        {
            return rewardType switch
            {
                RewardRotationScheduleBuilder.RewardTypeItem => GrantFlagItemOrModifier,
                RewardRotationScheduleBuilder.RewardTypeEssence => GrantFlagEssence,
                RewardRotationScheduleBuilder.RewardTypeModifier => GrantFlagItemOrModifier,
                _ => 0u
            };
        }

        public static ServerRewardRotationEntryStateArray BuildFromPersistedGrants(
            IEnumerable<AccountRewardRotationGrantModel> grants,
            uint rewardRotationIndex)
        {
            ServerRewardRotationEntryStateArray entryState = new();
            if (grants == null)
                return entryState;

            foreach (AccountRewardRotationGrantModel grant in grants)
            {
                if (grant == null || grant.RewardRotationIndex != rewardRotationIndex)
                    continue;

                entryState.Entries.Add(CreateRow(
                    rewardRotationIndex,
                    grant.ContentId,
                    grant.RewardKeyId,
                    grant.RewardType,
                    grant.GrantFlags));
            }

            return entryState;
        }

        public static ServerRewardRotationEntryStateArray BuildGrantedRows(
            uint rewardRotationIndex,
            ServerRewardRotationScheduleArray schedule,
            RewardRotationContentContextSources sources)
        {
            ServerRewardRotationEntryStateArray entryState = new();
            if (schedule?.Entries == null || schedule.Entries.Count == 0)
                return entryState;

            foreach (ServerRewardRotationScheduleArray.ScheduleRow scheduleRow in schedule.Entries)
            {
                if (scheduleRow == null || scheduleRow.ContentId == 0u)
                    continue;

                RewardRotationContentEntry contentEntry = sources.RewardRotationContent.Entries?
                    .FirstOrDefault(entry => entry != null && entry.Id == scheduleRow.ContentId);
                if (contentEntry == null || contentEntry.ContentTypeEnum != rewardRotationIndex)
                    continue;

                entryState.Entries.Add(CreateRow(
                    rewardRotationIndex,
                    scheduleRow.ContentId,
                    scheduleRow.RewardKeyId,
                    scheduleRow.RewardType,
                    GetGrantFlagsForRewardType(scheduleRow.RewardType)));
            }

            return entryState;
        }
    }
}
