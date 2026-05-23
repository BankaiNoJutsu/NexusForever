using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    public class ClientRaidInfoRequestHandler : IMessageHandler<IWorldSession, ClientRaidInfoRequest>
    {
        private readonly ILogger<ClientRaidInfoRequestHandler> log;
        private readonly IMapLockManager mapLockManager;

        /// <summary>
        /// Default lockout duration in days used when no reset schedule is configured.
        /// Retail WildStar had per-instance reset timers; this is a placeholder.
        /// </summary>
        private const float DefaultLockoutDays = 7f;

        public ClientRaidInfoRequestHandler(
            ILogger<ClientRaidInfoRequestHandler> log,
            IMapLockManager mapLockManager)
        {
            this.log            = log;
            this.mapLockManager = mapLockManager;
        }

        public void HandleMessage(IWorldSession session, ClientRaidInfoRequest _)
        {
            log.LogDebug("ClientRaidInfoRequest: player={Player}", session.Player?.Guid);

            var response = new ServerRaidInfoResponse
            {
                Raids = []
            };

            // Populate response with the player's active solo instance locks.
            // These are created when a player enters an instanced map and serve
            // as raid lockout records until a database-backed persistence layer
            // with weekly reset schedules is implemented (see F-010).
            IMapLockCollection lockCollection = mapLockManager.TryGetSoloLockCollection(session.Player.Identity);
            if (lockCollection != null)
            {
                var now = System.DateTime.UtcNow;
                var expireDate = now.AddDays(DefaultLockoutDays);
                var expireTimestamp = (ulong)new System.DateTimeOffset(expireDate).ToUnixTimeSeconds();
                var daysUntilExpire = DefaultLockoutDays;

                foreach (IMapLock mapLock in lockCollection)
                {
                    // Only report instance locks (skip residence locks which have WorldId == 0).
                    if (mapLock.WorldId == 0)
                        continue;

                    response.Raids.Add(new ServerRaidInfoResponse.RaidInfo
                    {
                        // Retail used database auto-increment IDs; derive a stable local id from the instance Guid.
                        SavedInstanceId = System.BitConverter.ToUInt64(mapLock.InstanceId.ToByteArray(), 0),
                        WorldId         = (ushort)mapLock.WorldId,
                        DateExpireUTC   = expireTimestamp,
                        DaysUntilExpire = daysUntilExpire,
                        PrimeLevel      = 0u
                    });
                }
            }

            session.EnqueueMessageEncrypted(response);

            // Keep the raid-info UI compatibility emission; non-zero queue semantics are not mapped yet.
            session.EnqueueMessageEncrypted(new ServerRaidQueueStatus
            {
                Unknown0 = 0u,
                Unknown1 = 0u,
                Unknown2 = 0u,
                Unknown3 = 0u,
                Unknown4 = 0u
            });
        }
    }
}
