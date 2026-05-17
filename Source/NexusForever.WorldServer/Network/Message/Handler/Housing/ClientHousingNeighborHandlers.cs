using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingNeighborInviteHandler : IMessageHandler<IWorldSession, ClientHousingNeighborInvite>
    {
        private readonly ILogger<ClientHousingNeighborInviteHandler> log;

        public ClientHousingNeighborInviteHandler(ILogger<ClientHousingNeighborInviteHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientHousingNeighborInvite housingNeighborInvite)
        {
            log.LogDebug("Ignoring unsupported housing neighbor invite from player {PlayerGuid}: target residence {ResidenceId}/{RealmId}, target name length {TargetNameLength}.",
                session.Player?.Guid,
                housingNeighborInvite.TargetResidence.ResidenceId,
                housingNeighborInvite.TargetResidence.RealmId,
                housingNeighborInvite.TargetName?.Length ?? 0);
        }
    }

    public class ClientHousingNeighborInviteResponseHandler : IMessageHandler<IWorldSession, ClientHousingNeighborInviteResponse>
    {
        private readonly ILogger<ClientHousingNeighborInviteResponseHandler> log;

        public ClientHousingNeighborInviteResponseHandler(ILogger<ClientHousingNeighborInviteResponseHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientHousingNeighborInviteResponse housingNeighborInviteResponse)
        {
            log.LogDebug("Ignoring unsupported housing neighbor invite response from player {PlayerGuid}: accepted {Accepted}.",
                session.Player?.Guid, housingNeighborInviteResponse.Accepted);
        }
    }

    public class ClientHousingNeighborEvictHandler : IMessageHandler<IWorldSession, ClientHousingNeighborEvict>
    {
        private readonly ILogger<ClientHousingNeighborEvictHandler> log;

        public ClientHousingNeighborEvictHandler(ILogger<ClientHousingNeighborEvictHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientHousingNeighborEvict housingNeighborEvict)
        {
            log.LogDebug("Ignoring unsupported housing neighbor evict request from player {PlayerGuid}: target residence {ResidenceId}/{RealmId}, target name length {TargetNameLength}.",
                session.Player?.Guid,
                housingNeighborEvict.TargetResidence.ResidenceId,
                housingNeighborEvict.TargetResidence.RealmId,
                housingNeighborEvict.TargetName?.Length ?? 0);
        }
    }

    public class ClientHousingNeighborSetPermissionHandler : IMessageHandler<IWorldSession, ClientHousingNeighborSetPermission>
    {
        private readonly ILogger<ClientHousingNeighborSetPermissionHandler> log;

        public ClientHousingNeighborSetPermissionHandler(ILogger<ClientHousingNeighborSetPermissionHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientHousingNeighborSetPermission housingNeighborSetPermission)
        {
            if (housingNeighborSetPermission.Permission > 2u)
                throw new InvalidPacketValueException($"Invalid housing neighbor permission received: {housingNeighborSetPermission.Permission}");

            log.LogDebug("Ignoring unsupported housing neighbor permission request from player {PlayerGuid}: target residence {ResidenceId}/{RealmId}, target name length {TargetNameLength}, permission {Permission}.",
                session.Player?.Guid,
                housingNeighborSetPermission.TargetResidence.ResidenceId,
                housingNeighborSetPermission.TargetResidence.RealmId,
                housingNeighborSetPermission.TargetName?.Length ?? 0,
                housingNeighborSetPermission.Permission);
        }
    }

    public class ClientHousingInteriorWallpaperUpdateHandler : IMessageHandler<IWorldSession, ClientHousingInteriorWallpaperUpdate>
    {
        private readonly ILogger<ClientHousingInteriorWallpaperUpdateHandler> log;

        public ClientHousingInteriorWallpaperUpdateHandler(ILogger<ClientHousingInteriorWallpaperUpdateHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientHousingInteriorWallpaperUpdate interiorWallpaperUpdate)
        {
            uint changedSlots = 0u;
            foreach (uint slotFlag in interiorWallpaperUpdate.SlotFlags)
            {
                if (slotFlag != 0u)
                    changedSlots++;
            }

            log.LogDebug("Ignoring unsupported housing interior wallpaper update from player {PlayerGuid}: changed slots {ChangedSlots}, decor records {DecorCount}.",
                session.Player?.Guid, changedSlots, interiorWallpaperUpdate.DecorUpdates.Count);
        }
    }
}
