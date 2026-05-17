using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

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
            log.LogDebug("Rejecting housing neighbor invite from player {PlayerGuid}: neighbor backing store is not available, target residence {ResidenceId}/{RealmId}, target name length {TargetNameLength}.",
                session.Player?.Guid,
                housingNeighborInvite.TargetResidence.ResidenceId,
                housingNeighborInvite.TargetResidence.RealmId,
                housingNeighborInvite.TargetName?.Length ?? 0);

            HousingNeighborResultSender.Send(session, housingNeighborInvite.TargetResidence, housingNeighborInvite.TargetName, HousingResult.Neighbor_PrivilegeRestricted);
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
            log.LogDebug("Rejecting housing neighbor invite response from player {PlayerGuid}: no pending invite, accepted {Accepted}.",
                session.Player?.Guid, housingNeighborInviteResponse.Accepted);

            HousingNeighborResultSender.Send(session, new TargetResidence(), string.Empty, HousingResult.Neighbor_NoPendingInvite);
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
            log.LogDebug("Rejecting housing neighbor evict request from player {PlayerGuid}: neighbor backing store is not available, target residence {ResidenceId}/{RealmId}, target name length {TargetNameLength}.",
                session.Player?.Guid,
                housingNeighborEvict.TargetResidence.ResidenceId,
                housingNeighborEvict.TargetResidence.RealmId,
                housingNeighborEvict.TargetName?.Length ?? 0);

            HousingNeighborResultSender.Send(session, housingNeighborEvict.TargetResidence, housingNeighborEvict.TargetName, HousingResult.Neighbor_InvalidNeighbor);
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

            log.LogDebug("Rejecting housing neighbor permission request from player {PlayerGuid}: neighbor backing store is not available, target residence {ResidenceId}/{RealmId}, target name length {TargetNameLength}, permission {Permission}.",
                session.Player?.Guid,
                housingNeighborSetPermission.TargetResidence.ResidenceId,
                housingNeighborSetPermission.TargetResidence.RealmId,
                housingNeighborSetPermission.TargetName?.Length ?? 0,
                housingNeighborSetPermission.Permission);

            HousingNeighborResultSender.Send(session, housingNeighborSetPermission.TargetResidence, housingNeighborSetPermission.TargetName, HousingResult.Neighbor_InvalidNeighbor);
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

            log.LogDebug("Rejected housing interior wallpaper update from player {PlayerGuid}: interior slot wallpaper persistence is not mapped, changed slots {ChangedSlots}, decor records {DecorCount}.",
                session.Player?.Guid, changedSlots, interiorWallpaperUpdate.DecorUpdates.Count);
        }
    }

    internal static class HousingNeighborResultSender
    {
        public static void Send(IWorldSession session, TargetResidence targetResidence, string playerName, HousingResult result)
        {
            session.EnqueueMessageEncrypted(new ServerHousingResult
            {
                RealmId     = targetResidence.RealmId,
                ResidenceId = targetResidence.ResidenceId,
                PlayerName  = playerName ?? string.Empty,
                Result      = result
            });
        }
    }
}
