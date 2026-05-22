using System.Linq;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.GameTable;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Records authoritative reward-rotation grant rows after a claim is validated against the
    /// current schedule snapshot. Item/currency delivery remains blocked until catalog linkage is verified.
    /// </summary>
    public static class RewardRotationGrantClaimService
    {
        public static bool TryRecordClaim(
            IAccountRewardRotationGrantManager grantManager,
            uint rewardRotationIndex,
            uint contentId,
            uint rewardKeyId,
            byte rewardType)
        {
            if (grantManager == null || contentId == 0u || rewardKeyId == 0u)
                return false;

            if (!RewardRotationRefreshBuilder.IsSupported(rewardRotationIndex))
                return false;

            if (rewardType is not (RewardRotationScheduleBuilder.RewardTypeItem
                or RewardRotationScheduleBuilder.RewardTypeEssence
                or RewardRotationScheduleBuilder.RewardTypeModifier))
                return false;

            grantManager.RecordGrant(
                rewardRotationIndex,
                contentId,
                rewardKeyId,
                rewardType,
                RewardRotationEntryStateBuilder.GetGrantFlagsForRewardType(rewardType));

            return true;
        }

        public static bool TryRecordClaimFromScheduleRow(
            IAccountRewardRotationGrantManager grantManager,
            uint rewardRotationIndex,
            ServerRewardRotationScheduleArray schedule,
            uint contentId,
            byte rewardType)
        {
            if (schedule?.Entries == null)
                return false;

            ServerRewardRotationScheduleArray.ScheduleRow scheduleRow = schedule.Entries
                .FirstOrDefault(row => row != null && row.ContentId == contentId && row.RewardType == rewardType);
            if (scheduleRow == null)
                return false;

            return TryRecordClaim(
                grantManager,
                rewardRotationIndex,
                scheduleRow.ContentId,
                scheduleRow.RewardKeyId,
                scheduleRow.RewardType);
        }

        public static bool TryRecordClaimFromScheduleRow(
            IAccountRewardRotationGrantManager grantManager,
            uint rewardRotationIndex,
            ServerRewardRotationScheduleArray schedule,
            uint contentId,
            byte rewardType,
            out ServerRewardRotationEntryStateArray.EntryStateRow entryStateRow)
        {
            entryStateRow = null;
            if (!TryRecordClaimFromScheduleRow(grantManager, rewardRotationIndex, schedule, contentId, rewardType))
                return false;

            entryStateRow = RewardRotationEntryStateBuilder.CreateRow(
                rewardRotationIndex,
                contentId,
                schedule.Entries.First(row => row != null && row.ContentId == contentId && row.RewardType == rewardType).RewardKeyId,
                rewardType,
                RewardRotationEntryStateBuilder.GetGrantFlagsForRewardType(rewardType));

            return true;
        }

        public static bool TryRecordClaimFromScheduleRow(
            IAccountRewardRotationGrantManager grantManager,
            uint rewardRotationIndex,
            IGameTableManager gameTables,
            uint contentId,
            byte rewardType,
            out ServerRewardRotationEntryStateArray.EntryStateRow entryStateRow)
        {
            entryStateRow = null;
            if (gameTables == null)
                return false;

            RewardRotationContentContextSources sources = RewardRotationContentContextSources.From(gameTables);
            List<uint> contentIds = RewardRotationContentContextBuilder.CollectContentIds(sources, rewardRotationIndex);
            ServerRewardRotationScheduleArray schedule = RewardRotationScheduleBuilder.Build(
                sources,
                rewardRotationIndex,
                contentIds,
                gameTables);

            return TryRecordClaimFromScheduleRow(
                grantManager,
                rewardRotationIndex,
                schedule,
                contentId,
                rewardType,
                out entryStateRow);
        }

        public static ServerRewardRotationEntryStateUpsert BuildUpsert(
            ServerRewardRotationEntryStateArray.EntryStateRow entryStateRow)
        {
            if (entryStateRow == null)
                return null;

            return new ServerRewardRotationEntryStateUpsert
            {
                TypeId = entryStateRow.TypeId,
                ContentId = entryStateRow.ContentId,
                RewardTypeId = entryStateRow.RewardTypeId,
                State = entryStateRow.State,
                Value = entryStateRow.Value
            };
        }
    }
}
