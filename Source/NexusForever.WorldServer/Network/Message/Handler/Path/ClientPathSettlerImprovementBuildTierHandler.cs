using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathSettlerImprovementBuildTierHandler : IMessageHandler<IWorldSession, ClientPathSettlerImprovementBuildTier>
    {
        private const uint MaxUInt14 = (1u << 14) - 1u;
        private const uint MaxUInt15 = (1u << 15) - 1u;

        private readonly ILogger<ClientPathSettlerImprovementBuildTierHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientPathSettlerImprovementBuildTierHandler(
            ILogger<ClientPathSettlerImprovementBuildTierHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientPathSettlerImprovementBuildTier buildTier)
        {
            log.LogDebug("ClientPathSettlerImprovementBuildTier: player={Player} improvementGroupId={ImprovementGroupId} buildTier={BuildTier}",
                session.Player?.Guid, buildTier.PathSettlerImprovementGroupId, buildTier.BuildTier);

            IPlayer player = session.Player;
            if (player == null)
                return;

            PathSettlerImprovementGroupEntry improvementGroup = gameTableManager.PathSettlerImprovementGroup?.GetEntry(buildTier.PathSettlerImprovementGroupId);
            if (improvementGroup == null)
                return;

            if (!TrySendSettlerBuildAcknowledgement(player, improvementGroup, buildTier.BuildTier, out uint bundleCount))
                return;

            player.PathManager.ApplySettlerImprovementGroupStatus(
                buildTier.PathSettlerImprovementGroupId,
                (int)buildTier.BuildTier,
                bundleCount);
            player.PathManager.CompleteMissionBySettlerImprovementGroupId(buildTier.PathSettlerImprovementGroupId);
        }

        private bool TrySendSettlerBuildAcknowledgement(
            IPlayer player,
            PathSettlerImprovementGroupEntry improvementGroup,
            uint buildTier,
            out uint bundleCount)
        {
            bundleCount = 0u;

            // WildStar64.exe 14007aa70 maps the result payload shape, but not the server
            // failure enum/resource semantics. Emit only table-backed success acknowledgements.
            if (!CanWriteUInt14(improvementGroup.Id) || !CanWriteUInt14(improvementGroup.PathSettlerHubId))
            {
                log.LogDebug("Skipping settler build acknowledgement for out-of-range improvementGroupId={ImprovementGroupId} hubId={HubId}",
                    improvementGroup.Id, improvementGroup.PathSettlerHubId);
                return false;
            }

            uint improvementId = GetImprovementIdForTier(improvementGroup, buildTier);
            if (improvementId == 0u || improvementId > MaxUInt15)
            {
                log.LogDebug("Skipping settler build acknowledgement for improvementGroupId={ImprovementGroupId} buildTier={BuildTier} improvementId={ImprovementId}",
                    improvementGroup.Id, buildTier, improvementId);
                return false;
            }

            PathSettlerImprovementEntry improvement = gameTableManager.PathSettlerImprovement?.GetEntry(improvementId);
            if (improvement == null)
            {
                log.LogDebug("Skipping settler build acknowledgement for improvementGroupId={ImprovementGroupId} buildTier={BuildTier}: missing improvementId={ImprovementId}",
                    improvementGroup.Id, buildTier, improvementId);
                return false;
            }

            PathSettlerHubEntry hub = gameTableManager.PathSettlerHub?.GetEntry(improvementGroup.PathSettlerHubId);
            if (hub == null)
            {
                log.LogDebug("Skipping settler build acknowledgement for improvementGroupId={ImprovementGroupId}: missing hubId={HubId}",
                    improvementGroup.Id, improvementGroup.PathSettlerHubId);
                return false;
            }

            if (!TryConsumeSettlerResources(player, hub, improvement, improvementGroup.Id, buildTier))
                return false;

            bundleCount = improvementGroup.DurationPerBundleMs == 0u ? 0u : 1u;
            player.EnqueueToVisible(new ServerPathSettlerBuildStatus
            {
                PathSettlerHubId = (ushort)improvementGroup.PathSettlerHubId,
                Status = new SettlerImprovementGroupStatus
                {
                    PathSettlerImprovementGroupId = (ushort)improvementGroup.Id,
                    Tier = (int)buildTier,
                    RemainingTimeMs = improvementGroup.DurationPerBundleMs,
                    BundleCount = bundleCount
                }
            }, true);

            player.Session.EnqueueMessageEncrypted(new ServerPathSettlerBuildResult
            {
                Result = 1u,
                PathSettlerImprovementId = improvementId,
                PathSettlerImprovementGroupId = improvementGroup.Id
            });
            return true;
        }

        private bool TryConsumeSettlerResources(
            IPlayer player,
            PathSettlerHubEntry hub,
            PathSettlerImprovementEntry improvement,
            uint improvementGroupId,
            uint buildTier)
        {
            SettlerResourceCost[] costs =
            [
                new SettlerResourceCost(hub.Item2IdResource00, improvement.CountResource00),
                new SettlerResourceCost(hub.Item2IdResource01, improvement.CountResource01),
                new SettlerResourceCost(hub.Item2IdResource02, improvement.CountResource02)
            ];

            foreach (SettlerResourceCost cost in costs)
            {
                if (cost.Count == 0u)
                    continue;

                if (cost.ItemId == 0u)
                {
                    log.LogDebug("Skipping settler build acknowledgement for improvementGroupId={ImprovementGroupId} buildTier={BuildTier}: missing resource item for count={Count}",
                        improvementGroupId, buildTier, cost.Count);
                    return false;
                }

                if (!player.Inventory.HasItemCount(cost.ItemId, cost.Count))
                {
                    log.LogDebug("Skipping settler build acknowledgement for improvementGroupId={ImprovementGroupId} buildTier={BuildTier}: missing itemId={ItemId} count={Count}",
                        improvementGroupId, buildTier, cost.ItemId, cost.Count);
                    return false;
                }
            }

            foreach (SettlerResourceCost cost in costs)
            {
                if (cost.Count == 0u)
                    continue;

                player.Inventory.ItemDelete(cost.ItemId, cost.Count, ItemUpdateReason.SettlerImprovementConsumeResource);
            }
            return true;
        }

        private static bool CanWriteUInt14(uint value)
        {
            return value <= MaxUInt14;
        }

        private static uint GetImprovementIdForTier(PathSettlerImprovementGroupEntry improvementGroup, uint buildTier)
        {
            return buildTier switch
            {
                0u => improvementGroup.PathSettlerImprovementIdTier00,
                1u => improvementGroup.PathSettlerImprovementIdTier01,
                2u => improvementGroup.PathSettlerImprovementIdTier02,
                3u => improvementGroup.PathSettlerImprovementIdTier03,
                _  => 0u
            };
        }

        private readonly record struct SettlerResourceCost(uint ItemId, uint Count);
    }
}
