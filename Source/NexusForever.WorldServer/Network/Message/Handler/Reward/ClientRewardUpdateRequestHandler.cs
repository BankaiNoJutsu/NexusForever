using Microsoft.Extensions.Logging;
using NexusForever.Game.Account.Reward;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Reward
{
    public class ClientRewardUpdateRequestHandler : IMessageHandler<IWorldSession, ClientRewardUpdateRequest>
    {
        private readonly ILogger<ClientRewardUpdateRequestHandler> log;

        public ClientRewardUpdateRequestHandler(
            ILogger<ClientRewardUpdateRequestHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientRewardUpdateRequest rewardUpdateRequest)
        {
            log.LogDebug("Received reward rotation refresh request for player {PlayerGuid}: reward rotation index {RewardRotationIndex}.",
                session.Player?.Guid, rewardUpdateRequest.RewardRotationIndex);

            session.Account.RewardPropertyManager.SendInitialPackets();

            RewardRotationRefresh refresh = RewardRotationRefreshBuilder.Build(rewardUpdateRequest.RewardRotationIndex);
            if (refresh == null)
            {
                RewardRotationRuntimeEvidenceCollector.RecordRequestIfArmed(
                    session.Player,
                    rewardUpdateRequest.RewardRotationIndex,
                    null,
                    $"Unsupported reward rotation request captured; no response packets were generated because the evidence-backed range is 0-{RewardRotationRefreshBuilder.MaxSupportedRewardRotationIndex}.");
                log.LogWarning("Ignoring unsupported reward rotation index {RewardRotationIndex} for player {PlayerGuid}; supported range is 0-{MaxRewardRotationIndex}.",
                    rewardUpdateRequest.RewardRotationIndex, session.Player?.Guid, RewardRotationRefreshBuilder.MaxSupportedRewardRotationIndex);
                return;
            }

            session.EnqueueMessageEncrypted(refresh.ScheduleArray);
            session.EnqueueMessageEncrypted(refresh.EntryStateArray);
            RewardRotationRuntimeEvidenceCollector.RecordRequestIfArmed(
                session.Player,
                rewardUpdateRequest.RewardRotationIndex,
                refresh);

            log.LogDebug("Sent reward rotation response for player {PlayerGuid}: reward rotation index {RewardRotationIndex}, schedule entries {ScheduleEntryCount}, entry-state entries {EntryStateCount}, placeholder {IsPlaceholder}, source {ResponseSource}.",
                session.Player?.Guid, refresh.RewardRotationIndex, refresh.ScheduleEntryCount, refresh.EntryStateCount, refresh.IsPlaceholder, refresh.ResponseSource);

            if (refresh.IsPlaceholder)
            {
                log.LogInformation("Reward rotation response for player {PlayerGuid} is an empty placeholder: reward rotation index {RewardRotationIndex}, schedule entries {ScheduleEntryCount}, entry-state entries {EntryStateCount}, source {ResponseSource}. Live schedule and entry-state semantics remain evidence-blocked.",
                    session.Player?.Guid, refresh.RewardRotationIndex, refresh.ScheduleEntryCount, refresh.EntryStateCount, refresh.ResponseSource);
            }
            else
            {
                log.LogInformation("Reward rotation response for player {PlayerGuid} carried observable rows from source {ResponseSource}: reward rotation index {RewardRotationIndex}, schedule entries {ScheduleEntryCount}, entry-state entries {EntryStateCount}. Review reward evidence captures before implementing live semantics.",
                    session.Player?.Guid, refresh.ResponseSource, refresh.RewardRotationIndex, refresh.ScheduleEntryCount, refresh.EntryStateCount);
            }
        }
    }
}
