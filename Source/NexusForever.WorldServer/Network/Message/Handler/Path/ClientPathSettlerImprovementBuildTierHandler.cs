using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;

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

            PathSettlerImprovementGroupEntry improvementGroup = gameTableManager.PathSettlerImprovementGroup.GetEntry(buildTier.PathSettlerImprovementGroupId);
            if (improvementGroup != null)
                SendSettlerBuildAcknowledgement(player, improvementGroup, buildTier.BuildTier);

            player.PathManager.CompleteMissionBySettlerImprovementGroupId(buildTier.PathSettlerImprovementGroupId);
        }

        private void SendSettlerBuildAcknowledgement(IPlayer player, PathSettlerImprovementGroupEntry improvementGroup, uint buildTier)
        {
            // WildStar64.exe 14007aa70 maps the result payload shape, but not the server
            // failure enum/resource semantics. Emit only table-backed success acknowledgements.
            if (!CanWriteUInt14(improvementGroup.Id) || !CanWriteUInt14(improvementGroup.PathSettlerHubId))
            {
                log.LogDebug("Skipping settler build acknowledgement for out-of-range improvementGroupId={ImprovementGroupId} hubId={HubId}",
                    improvementGroup.Id, improvementGroup.PathSettlerHubId);
                return;
            }

            uint improvementId = GetImprovementIdForTier(improvementGroup, buildTier);
            if (improvementId == 0u || improvementId > MaxUInt15)
            {
                log.LogDebug("Skipping settler build acknowledgement for improvementGroupId={ImprovementGroupId} buildTier={BuildTier} improvementId={ImprovementId}",
                    improvementGroup.Id, buildTier, improvementId);
                return;
            }

            player.EnqueueToVisible(new ServerPathSettlerBuildStatus
            {
                PathSettlerHubId = (ushort)improvementGroup.PathSettlerHubId,
                Status = new SettlerImprovementGroupStatus
                {
                    PathSettlerImprovementGroupId = (ushort)improvementGroup.Id,
                    Tier = (int)buildTier,
                    RemainingTimeMs = improvementGroup.DurationPerBundleMs,
                    BundleCount = improvementGroup.DurationPerBundleMs == 0u ? 0u : 1u
                }
            }, true);

            player.Session.EnqueueMessageEncrypted(new ServerPathSettlerBuildResult
            {
                Result = 1u,
                PathSettlerImprovementId = improvementId,
                PathSettlerImprovementGroupId = improvementGroup.Id
            });
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
    }
}
