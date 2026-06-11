using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Account.Reward;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network.Message.Handler.Fortune;

namespace NexusForever.WorldServer.Network.Message.Handler.Reward
{
    public class ClientRewardUpdateRequestHandler : IMessageHandler<IWorldSession, ClientRewardUpdateRequest>
    {
        private readonly ILogger<ClientRewardUpdateRequestHandler> log;
        private readonly IGlobalStorefrontManager globalStorefrontManager;
        private readonly IRewardRotationRefreshProvider refreshProvider;
        private readonly IFortuneSessionManager fortuneSessionManager;
        private readonly IGameTableManager gameTableManager;

        public ClientRewardUpdateRequestHandler(
            ILogger<ClientRewardUpdateRequestHandler> log,
            IGlobalStorefrontManager globalStorefrontManager,
            IRewardRotationRefreshProvider refreshProvider = null,
            IFortuneSessionManager fortuneSessionManager = null,
            IGameTableManager gameTableManager = null)
        {
            this.log = log;
            this.globalStorefrontManager = globalStorefrontManager;
            this.refreshProvider = refreshProvider;
            this.fortuneSessionManager = fortuneSessionManager;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientRewardUpdateRequest rewardUpdateRequest)
        {
            log.LogDebug("Received reward rotation refresh request for player {PlayerGuid}: reward rotation index {RewardRotationIndex}.",
                session.Player?.Guid, rewardUpdateRequest.RewardRotationIndex);

            // Store open issues rotation index 0 before 082D; catalog must lead rotation placeholders.
            if (rewardUpdateRequest.RewardRotationIndex == 0u && session.Player != null)
            {
                log.LogInformation("StorefrontCatalogDiagnostics sending catalog before reward rotation index 0 for player {PlayerGuid} account={AccountId}.",
                    session.Player.Guid, session.Account?.Id ?? 0u);
                globalStorefrontManager.HandleCatalogRequest(session, session.Account.Id);
                fortuneSessionManager?.SendStatus(session);
            }

            session.Account.RewardPropertyManager.SendInitialPackets();

            IRewardRotationRefreshProvider provider = refreshProvider
                ?? new AccountRewardRotationRefreshProvider(
                    session.Account,
                    gameTableManager,
                    playerLevel: ResolveRewardRotationPlayerLevel(session.Player),
                    worldDifficultyFlags: RewardRotationScheduleBuilder.KnownWorldDifficultyFlags);
            RewardRotationRefresh refresh = RewardRotationRefreshBuilder.Build(rewardUpdateRequest.RewardRotationIndex, provider);
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

            foreach (IWritable contentContextPacket in refresh.GetContentContextPackets())
                session.EnqueueMessageEncrypted(contentContextPacket);

            EnqueueScheduleArrayIfNotEmpty(session, refresh);

            if (TryProcessClaimRequest(session, rewardUpdateRequest, refresh, out ServerRewardRotationEntryStateArray.EntryStateRow claimedRow))
            {
                ServerRewardRotationEntryStateUpsert upsert = RewardRotationGrantClaimService.BuildUpsert(claimedRow);
                if (upsert != null)
                    session.EnqueueMessageEncrypted(upsert);

                IAccountRewardRotationGrantManager grantManager = session.Account.RewardRotationGrantManager;
                if (grantManager != null)
                {
                    ServerRewardRotationEntryStateArray entryState = grantManager.BuildEntryState(rewardUpdateRequest.RewardRotationIndex);
                    EnqueueEntryStateArrayIfNotEmpty(session, refresh, entryState);
                }
                else
                {
                    EnqueueEntryStateArrayIfNotEmpty(session, refresh, refresh.EntryStateArray);
                }
            }
            else
            {
                EnqueueEntryStateArrayIfNotEmpty(session, refresh, refresh.EntryStateArray);
            }
            RewardRotationRuntimeEvidenceCollector.RecordRequestIfArmed(
                session.Player,
                rewardUpdateRequest.RewardRotationIndex,
                refresh);

            log.LogDebug("Sent reward rotation response for player {PlayerGuid}: reward rotation index {RewardRotationIndex}, content-context ids {ContentContextIdCount}, schedule entries {ScheduleEntryCount}, entry-state entries {EntryStateCount}, placeholder {IsPlaceholder}, source {ResponseSource}.",
                session.Player?.Guid, refresh.RewardRotationIndex, refresh.ContentContextIdCount, refresh.ScheduleEntryCount, refresh.EntryStateCount, refresh.IsPlaceholder, refresh.ResponseSource);

            if (refresh.IsPlaceholder)
            {
                log.LogInformation("Reward rotation response for player {PlayerGuid} is an empty placeholder: reward rotation index {RewardRotationIndex}, content-context ids {ContentContextIdCount}, schedule entries {ScheduleEntryCount}, entry-state entries {EntryStateCount}, source {ResponseSource}. Live schedule and entry-state semantics remain evidence-blocked.",
                    session.Player?.Guid, refresh.RewardRotationIndex, refresh.ContentContextIdCount, refresh.ScheduleEntryCount, refresh.EntryStateCount, refresh.ResponseSource);
            }
            else
            {
                log.LogInformation("Reward rotation response for player {PlayerGuid} carried observable rows from source {ResponseSource}: reward rotation index {RewardRotationIndex}, content-context ids {ContentContextIdCount}, schedule entries {ScheduleEntryCount}, entry-state entries {EntryStateCount}. Review reward evidence captures before implementing live semantics.",
                    session.Player?.Guid, refresh.ResponseSource, refresh.RewardRotationIndex, refresh.ContentContextIdCount, refresh.ScheduleEntryCount, refresh.EntryStateCount);
            }
        }

        private void EnqueueScheduleArrayIfNotEmpty(IWorldSession session, RewardRotationRefresh refresh)
        {
            if (refresh.ScheduleEntryCount == 0)
            {
                if (ShouldSendEmptyRotationArrays(refresh))
                {
                    log.LogInformation("StorefrontCatalogDiagnostics reward rotation response for player {PlayerGuid}: sending empty ServerRewardRotationScheduleArray for reward rotation index {RewardRotationIndex}, source {ResponseSource}, because {Reason}.",
                        session.Player?.Guid, refresh.RewardRotationIndex, refresh.ResponseSource, DescribeEmptyArrayReason(refresh));
                    session.EnqueueMessageEncrypted(refresh.ScheduleArray);
                    return;
                }

                log.LogInformation("StorefrontCatalogDiagnostics reward rotation response for player {PlayerGuid}: skipping empty ServerRewardRotationScheduleArray for reward rotation index {RewardRotationIndex}, source {ResponseSource}.",
                    session.Player?.Guid, refresh.RewardRotationIndex, refresh.ResponseSource);
                return;
            }

            session.EnqueueMessageEncrypted(refresh.ScheduleArray);
        }

        private void EnqueueEntryStateArrayIfNotEmpty(
            IWorldSession session,
            RewardRotationRefresh refresh,
            ServerRewardRotationEntryStateArray entryStateArray)
        {
            if (entryStateArray == null || entryStateArray.Entries.Count == 0)
            {
                if (ShouldSendEmptyRotationArrays(refresh))
                {
                    log.LogInformation("StorefrontCatalogDiagnostics reward rotation response for player {PlayerGuid}: sending empty ServerRewardRotationEntryStateArray for reward rotation index {RewardRotationIndex}, source {ResponseSource}, because {Reason}.",
                        session.Player?.Guid, refresh.RewardRotationIndex, refresh.ResponseSource, DescribeEmptyArrayReason(refresh));
                    session.EnqueueMessageEncrypted(entryStateArray ?? new ServerRewardRotationEntryStateArray());
                    return;
                }

                log.LogInformation("StorefrontCatalogDiagnostics reward rotation response for player {PlayerGuid}: skipping empty ServerRewardRotationEntryStateArray for reward rotation index {RewardRotationIndex}, source {ResponseSource}.",
                    session.Player?.Guid, refresh.RewardRotationIndex, refresh.ResponseSource);
                return;
            }

            session.EnqueueMessageEncrypted(entryStateArray);
        }

        private static bool ShouldSendEmptyRotationArrays(RewardRotationRefresh refresh)
        {
            return refresh.IsPlaceholder && refresh.RewardRotationIndex == 0u;
        }

        private static string DescribeEmptyArrayReason(RewardRotationRefresh refresh)
        {
            return "reward rotation index 0 is a storefront-visible placeholder";
        }

        private static uint ResolveRewardRotationPlayerLevel(IPlayer player)
        {
            return System.Math.Max(
                player?.Level ?? RewardRotationScheduleBuilder.DefaultPlayerLevel,
                RewardRotationScheduleBuilder.DefaultPlayerLevel);
        }

        private static bool TryProcessClaimRequest(
            IWorldSession session,
            ClientRewardUpdateRequest rewardUpdateRequest,
            RewardRotationRefresh refresh,
            out ServerRewardRotationEntryStateArray.EntryStateRow entryStateRow)
        {
            entryStateRow = null;
            if (!rewardUpdateRequest.HasClaimRequest)
                return false;

            IAccountRewardRotationGrantManager grantManager = session.Account?.RewardRotationGrantManager;
            if (grantManager == null)
                return false;

            return RewardRotationGrantClaimService.TryRecordClaimFromScheduleRow(
                grantManager,
                rewardUpdateRequest.RewardRotationIndex,
                refresh.ScheduleArray,
                rewardUpdateRequest.ClaimContentId,
                rewardUpdateRequest.ClaimRewardType,
                out entryStateRow);
        }
    }
}
