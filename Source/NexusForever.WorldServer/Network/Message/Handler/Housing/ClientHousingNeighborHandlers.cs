using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingNeighborInviteHandler : IMessageHandler<IWorldSession, ClientHousingNeighborInvite>
    {
        private readonly ILogger<ClientHousingNeighborInviteHandler> log;
        private readonly ICharacterManager characterManager;
        private readonly IPlayerManager playerManager;
        private readonly IGlobalResidenceManager globalResidenceManager;

        public ClientHousingNeighborInviteHandler(
            ILogger<ClientHousingNeighborInviteHandler> log,
            ICharacterManager characterManager,
            IPlayerManager playerManager,
            IGlobalResidenceManager globalResidenceManager)
        {
            this.log                    = log;
            this.characterManager       = characterManager;
            this.playerManager          = playerManager;
            this.globalResidenceManager = globalResidenceManager;
        }

        public void HandleMessage(IWorldSession session, ClientHousingNeighborInvite housingNeighborInvite)
        {
            IResidence inviterResidence = session.Player.ResidenceManager.GetOrCreateResidence();
            if (inviterResidence == null)
            {
                HousingNeighborResultSender.Send(session, housingNeighborInvite.TargetResidence, housingNeighborInvite.TargetName, HousingResult.Neighbor_PrivilegeRestricted);
                return;
            }

            if (!TryResolveInviteTarget(housingNeighborInvite.TargetResidence, housingNeighborInvite.TargetName, out ResolvedNeighborTarget target, out HousingResult failureResult))
            {
                HousingNeighborResultSender.Send(session, housingNeighborInvite.TargetResidence, housingNeighborInvite.TargetName, failureResult);
                return;
            }

            if (target.CharacterId == session.Player.CharacterId || inviterResidence.HasNeighbor(target.CharacterId))
            {
                HousingNeighborResultSender.Send(session, target.TargetResidence, target.Name, HousingResult.Neighbor_AlreadyNeighbors);
                return;
            }

            if (!target.Player.ResidenceManager.TryQueueNeighborInvite(new ResidenceNeighborInviteInfo
            {
                InviterCharacterId = session.Player.CharacterId,
                InviterResidenceId = inviterResidence.Id,
                InviterName        = session.Player.Name
            }))
            {
                HousingNeighborResultSender.Send(session, target.TargetResidence, target.Name, HousingResult.Neighbor_InvitePending);
                return;
            }

            log.LogDebug("Queued housing neighbor invite from player {PlayerGuid} to character {TargetCharacterId} residence {TargetResidenceId}/{TargetRealmId}.",
                session.Player?.Guid,
                target.CharacterId,
                target.TargetResidence.ResidenceId,
                target.TargetResidence.RealmId);

            HousingNeighborResultSender.Send(session, target.TargetResidence, target.Name, HousingResult.Neighbor_Success);
            HousingNeighborInviteSender.Send(target.Player.Session,
                HousingNeighborTargetHelper.CreateTargetResidence(session.Player.Identity.RealmId, inviterResidence.Id),
                session.Player.Name);
        }

        private bool TryResolveInviteTarget(TargetResidence targetResidence, string targetName, out ResolvedNeighborTarget target, out HousingResult result)
        {
            target = null;

            if (!string.IsNullOrWhiteSpace(targetName))
            {
                ulong? targetCharacterId = characterManager.GetCharacterIdByName(targetName.Trim());
                if (!targetCharacterId.HasValue || targetCharacterId.Value == 0ul)
                {
                    result = HousingResult.Neighbor_PlayerDoesntExist;
                    return false;
                }

                IPlayer targetPlayer = playerManager.GetPlayer(targetCharacterId.Value);
                if (targetPlayer == null)
                {
                    result = HousingResult.Neighbor_PlayerNotOnline;
                    return false;
                }

                IResidence targetPlayerResidence = globalResidenceManager.GetResidenceByOwner(targetCharacterId.Value);
                if (targetPlayerResidence == null)
                {
                    result = HousingResult.Neighbor_PlayerNotAHomeowner;
                    return false;
                }

                target = new ResolvedNeighborTarget(targetPlayer, targetCharacterId.Value, targetPlayer.Name,
                    HousingNeighborTargetHelper.CreateTargetResidence(targetPlayer.Identity.RealmId, targetPlayerResidence.Id));
                result = HousingResult.Success;
                return true;
            }

            if (targetResidence.ResidenceId == 0ul)
            {
                result = HousingResult.Neighbor_PlayerNotFound;
                return false;
            }

            IResidence resolvedResidence = globalResidenceManager.GetResidence(targetResidence.ResidenceId);
            if (resolvedResidence?.OwnerId == null)
            {
                result = HousingResult.Neighbor_PlayerNotFound;
                return false;
            }

            IPlayer resolvedPlayer = playerManager.GetPlayer(resolvedResidence.OwnerId.Value);
            if (resolvedPlayer == null)
            {
                result = HousingResult.Neighbor_PlayerNotOnline;
                return false;
            }

            target = new ResolvedNeighborTarget(resolvedPlayer, resolvedResidence.OwnerId.Value, resolvedPlayer.Name,
                HousingNeighborTargetHelper.CreateTargetResidence(resolvedPlayer.Identity.RealmId, resolvedResidence.Id));
            result = HousingResult.Success;
            return true;
        }
    }

    public class ClientHousingNeighborInviteResponseHandler : IMessageHandler<IWorldSession, ClientHousingNeighborInviteResponse>
    {
        private readonly ILogger<ClientHousingNeighborInviteResponseHandler> log;
        private readonly IPlayerManager playerManager;
        private readonly IGlobalResidenceManager globalResidenceManager;

        public ClientHousingNeighborInviteResponseHandler(
            ILogger<ClientHousingNeighborInviteResponseHandler> log,
            IPlayerManager playerManager,
            IGlobalResidenceManager globalResidenceManager)
        {
            this.log                    = log;
            this.playerManager          = playerManager;
            this.globalResidenceManager = globalResidenceManager;
        }

        public void HandleMessage(IWorldSession session, ClientHousingNeighborInviteResponse housingNeighborInviteResponse)
        {
            if (!session.Player.ResidenceManager.TryTakePendingNeighborInvite(out ResidenceNeighborInviteInfo pendingInvite, out bool expired))
            {
                HousingNeighborResultSender.Send(
                    session,
                    new TargetResidence(),
                    string.Empty,
                    expired ? HousingResult.Neighbor_RequestTimedOut : HousingResult.Neighbor_NoPendingInvite);
                return;
            }

            IResidence inviterResidence = globalResidenceManager.GetResidence(pendingInvite.InviterResidenceId);
            if (inviterResidence == null)
            {
                HousingNeighborResultSender.Send(session, new TargetResidence(), string.Empty, HousingResult.Neighbor_NoPendingInvite);
                return;
            }

            TargetResidence inviterTargetResidence = HousingNeighborTargetHelper.CreateTargetResidence(session.Player.Identity.RealmId, pendingInvite.InviterResidenceId);
            IResidence inviteeResidence = session.Player.ResidenceManager.GetOrCreateResidence();
            HousingResult inviteeResult = HousingResult.Neighbor_Success;
            bool neighborAdded = false;
            if (housingNeighborInviteResponse.Accepted)
            {
                neighborAdded = inviterResidence.AddNeighbor(session.Player.CharacterId);
                if (!neighborAdded)
                    inviteeResult = HousingResult.Neighbor_AlreadyNeighbors;
            }

            HousingNeighborResultSender.Send(session, inviterTargetResidence, pendingInvite.InviterName, inviteeResult);

            IPlayer inviterPlayer = playerManager.GetPlayer(pendingInvite.InviterCharacterId);
            if (inviterPlayer == null)
                return;

            ulong inviteeResidenceId = inviteeResidence?.Id ?? 0ul;
            HousingResult inviterResult = housingNeighborInviteResponse.Accepted
                ? neighborAdded ? HousingResult.Neighbor_RequestAccepted : HousingResult.Neighbor_AlreadyNeighbors
                : HousingResult.Neighbor_RequestDeclined;

            HousingNeighborInviteResultSender.Send(
                inviterPlayer.Session,
                session.Player.CharacterId,
                session.Player.Identity.RealmId,
                inviteeResidenceId,
                0u,
                inviterResult);

            if (housingNeighborInviteResponse.Accepted && neighborAdded)
            {
                HousingNeighborUpdateSender.Send(
                    inviterPlayer.Session,
                    session.Player.CharacterId,
                    session.Player.Identity.RealmId,
                    inviteeResidenceId,
                    0u,
                    HousingNeighborUpdateType.Added);
            }

            log.LogDebug("Processed housing neighbor invite response from player {PlayerGuid}: inviter {InviterCharacterId}, accepted {Accepted}, sender result {SenderResult}.",
                session.Player?.Guid,
                pendingInvite.InviterCharacterId,
                housingNeighborInviteResponse.Accepted,
                inviteeResult);
        }
    }

    public class ClientHousingNeighborEvictHandler : IMessageHandler<IWorldSession, ClientHousingNeighborEvict>
    {
        private readonly ILogger<ClientHousingNeighborEvictHandler> log;
        private readonly ICharacterManager characterManager;
        private readonly IGlobalResidenceManager globalResidenceManager;

        public ClientHousingNeighborEvictHandler(
            ILogger<ClientHousingNeighborEvictHandler> log,
            ICharacterManager characterManager,
            IGlobalResidenceManager globalResidenceManager)
        {
            this.log                    = log;
            this.characterManager       = characterManager;
            this.globalResidenceManager = globalResidenceManager;
        }

        public void HandleMessage(IWorldSession session, ClientHousingNeighborEvict housingNeighborEvict)
        {
            if (!TryResolveNeighborIdentity(housingNeighborEvict.TargetResidence, housingNeighborEvict.TargetName, out ulong targetCharacterId, out HousingResult failureResult))
            {
                HousingNeighborResultSender.Send(session, housingNeighborEvict.TargetResidence, housingNeighborEvict.TargetName, failureResult);
                return;
            }

            IResidence ownerResidence = session.Player.ResidenceManager.Residence;
            if (ownerResidence == null || !ownerResidence.RemoveNeighbor(targetCharacterId))
            {
                HousingNeighborResultSender.Send(session, housingNeighborEvict.TargetResidence, housingNeighborEvict.TargetName, HousingResult.Neighbor_InvalidNeighbor);
                return;
            }

            log.LogDebug("Removed housing neighbor {TargetCharacterId} from player {PlayerGuid} residence {ResidenceId}.",
                targetCharacterId,
                session.Player?.Guid,
                ownerResidence.Id);

            HousingNeighborResultSender.Send(session, housingNeighborEvict.TargetResidence, housingNeighborEvict.TargetName, HousingResult.Neighbor_Success);

            ulong targetResidenceId = ResolveNeighborResidenceId(housingNeighborEvict.TargetResidence, targetCharacterId);
            if (targetResidenceId != 0ul)
            {
                HousingNeighborUpdateSender.Send(
                    session,
                    targetCharacterId,
                    session.Player.Identity.RealmId,
                    targetResidenceId,
                    0u,
                    HousingNeighborUpdateType.Removed);
            }
            else
            {
                session.Player.ResidenceManager.SendHousingNeighbors();
            }
        }

        private ulong ResolveNeighborResidenceId(TargetResidence targetResidence, ulong targetCharacterId)
        {
            if (targetResidence.ResidenceId != 0ul)
                return targetResidence.ResidenceId;

            return globalResidenceManager.GetResidenceByOwner(targetCharacterId)?.Id ?? 0ul;
        }

        private bool TryResolveNeighborIdentity(TargetResidence targetResidence, string targetName, out ulong targetCharacterId, out HousingResult result)
        {
            targetCharacterId = 0ul;

            if (!string.IsNullOrWhiteSpace(targetName))
            {
                ulong? resolvedCharacterId = characterManager.GetCharacterIdByName(targetName.Trim());
                if (!resolvedCharacterId.HasValue || resolvedCharacterId.Value == 0ul)
                {
                    result = HousingResult.Neighbor_PlayerDoesntExist;
                    return false;
                }

                targetCharacterId = resolvedCharacterId.Value;
                result = HousingResult.Success;
                return true;
            }

            if (targetResidence.ResidenceId == 0ul)
            {
                result = HousingResult.Neighbor_PlayerNotFound;
                return false;
            }

            IResidence resolvedResidence = globalResidenceManager.GetResidence(targetResidence.ResidenceId);
            if (resolvedResidence?.OwnerId == null)
            {
                result = HousingResult.Neighbor_PlayerNotFound;
                return false;
            }

            targetCharacterId = resolvedResidence.OwnerId.Value;
            result = HousingResult.Success;
            return true;
        }
    }

    public class ClientHousingNeighborSetPermissionHandler : IMessageHandler<IWorldSession, ClientHousingNeighborSetPermission>
    {
        private readonly ILogger<ClientHousingNeighborSetPermissionHandler> log;
        private readonly ICharacterManager characterManager;
        private readonly IGlobalResidenceManager globalResidenceManager;

        public ClientHousingNeighborSetPermissionHandler(
            ILogger<ClientHousingNeighborSetPermissionHandler> log,
            ICharacterManager characterManager,
            IGlobalResidenceManager globalResidenceManager)
        {
            this.log                    = log;
            this.characterManager       = characterManager;
            this.globalResidenceManager = globalResidenceManager;
        }

        public void HandleMessage(IWorldSession session, ClientHousingNeighborSetPermission housingNeighborSetPermission)
        {
            if (housingNeighborSetPermission.Permission > 2u)
                throw new InvalidPacketValueException($"Invalid housing neighbor permission received: {housingNeighborSetPermission.Permission}");

            if (!TryResolveNeighborIdentity(housingNeighborSetPermission.TargetResidence, housingNeighborSetPermission.TargetName, out ulong targetCharacterId, out HousingResult failureResult))
            {
                HousingNeighborResultSender.Send(session, housingNeighborSetPermission.TargetResidence, housingNeighborSetPermission.TargetName, failureResult);
                return;
            }

            IResidence ownerResidence = session.Player.ResidenceManager.Residence;
            if (ownerResidence == null || !ownerResidence.TrySetNeighborPermission(targetCharacterId, (byte)housingNeighborSetPermission.Permission))
            {
                HousingNeighborResultSender.Send(session, housingNeighborSetPermission.TargetResidence, housingNeighborSetPermission.TargetName, HousingResult.Neighbor_InvalidNeighbor);
                return;
            }

            log.LogDebug("Updated housing neighbor permission from player {PlayerGuid}: neighbor {TargetCharacterId}, permission {Permission}.",
                session.Player?.Guid,
                targetCharacterId,
                housingNeighborSetPermission.Permission);

            HousingNeighborResultSender.Send(session, housingNeighborSetPermission.TargetResidence, housingNeighborSetPermission.TargetName, HousingResult.Neighbor_Success);

            ulong targetResidenceId = ResolveNeighborResidenceId(housingNeighborSetPermission.TargetResidence, targetCharacterId);
            if (targetResidenceId != 0ul)
            {
                HousingNeighborUpdateSender.Send(
                    session,
                    targetCharacterId,
                    session.Player.Identity.RealmId,
                    targetResidenceId,
                    housingNeighborSetPermission.Permission,
                    HousingNeighborUpdateType.PermissionChanged);
            }
            else
            {
                session.Player.ResidenceManager.SendHousingNeighbors();
            }
        }

        private ulong ResolveNeighborResidenceId(TargetResidence targetResidence, ulong targetCharacterId)
        {
            if (targetResidence.ResidenceId != 0ul)
                return targetResidence.ResidenceId;

            return globalResidenceManager.GetResidenceByOwner(targetCharacterId)?.Id ?? 0ul;
        }

        private bool TryResolveNeighborIdentity(TargetResidence targetResidence, string targetName, out ulong targetCharacterId, out HousingResult result)
        {
            targetCharacterId = 0ul;

            if (!string.IsNullOrWhiteSpace(targetName))
            {
                ulong? resolvedCharacterId = characterManager.GetCharacterIdByName(targetName.Trim());
                if (!resolvedCharacterId.HasValue || resolvedCharacterId.Value == 0ul)
                {
                    result = HousingResult.Neighbor_PlayerDoesntExist;
                    return false;
                }

                targetCharacterId = resolvedCharacterId.Value;
                result = HousingResult.Success;
                return true;
            }

            if (targetResidence.ResidenceId == 0ul)
            {
                result = HousingResult.Neighbor_PlayerNotFound;
                return false;
            }

            IResidence resolvedResidence = globalResidenceManager.GetResidence(targetResidence.ResidenceId);
            if (resolvedResidence?.OwnerId == null)
            {
                result = HousingResult.Neighbor_PlayerNotFound;
                return false;
            }

            targetCharacterId = resolvedResidence.OwnerId.Value;
            result = HousingResult.Success;
            return true;
        }
    }

    public class ClientHousingInteriorWallpaperUpdateHandler : IMessageHandler<IWorldSession, ClientHousingInteriorWallpaperUpdate>
    {
        public void HandleMessage(IWorldSession session, ClientHousingInteriorWallpaperUpdate interiorWallpaperUpdate)
        {
            if (session.Player.Map is not IResidenceMapInstance residenceMap)
                throw new InvalidPacketValueException();

            residenceMap.InteriorWallpaperUpdate(session.Player, interiorWallpaperUpdate);
        }
    }

    internal static class HousingNeighborResultSender
    {
        public static void Send(IGameSession session, TargetResidence targetResidence, string playerName, HousingResult result)
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

    internal static class HousingNeighborInviteSender
    {
        public static void Send(IGameSession session, TargetResidence targetResidence, string playerName)
        {
            var message = new ServerHousingNeighborInvitePrompt
            {
                PlayerName = playerName ?? string.Empty
            };
            message.TargetResidence.RealmId     = targetResidence.RealmId;
            message.TargetResidence.ResidenceId = targetResidence.ResidenceId;
            session.EnqueueMessageEncrypted(message);
        }
    }

    internal static class HousingNeighborInviteResultSender
    {
        public static void Send(IGameSession session, ulong characterId, ushort realmId, ulong residenceId, uint permission, HousingResult result)
        {
            var message = new ServerHousingNeighborInviteResult
            {
                Result = result
            };

            message.Neighbor.CharacterId = characterId;
            message.Neighbor.Permission = permission;
            message.Neighbor.TargetResidence.RealmId = realmId;
            message.Neighbor.TargetResidence.ResidenceId = residenceId;
            session.EnqueueMessageEncrypted(message);
        }
    }

    internal static class HousingNeighborUpdateSender
    {
        public static void Send(IGameSession session, ulong characterId, ushort realmId, ulong residenceId, uint permission, HousingNeighborUpdateType updateType)
        {
            var message = new ServerHousingNeighborUpdate
            {
                UpdateType = updateType
            };

            message.Neighbor.CharacterId = characterId;
            message.Neighbor.Permission = permission;
            message.Neighbor.TargetResidence.RealmId = realmId;
            message.Neighbor.TargetResidence.ResidenceId = residenceId;
            session.EnqueueMessageEncrypted(message);
        }
    }

    internal sealed record ResolvedNeighborTarget(IPlayer Player, ulong CharacterId, string Name, TargetResidence TargetResidence);

    internal static class HousingNeighborTargetHelper
    {
        public static TargetResidence CreateTargetResidence(ushort realmId, ulong residenceId)
        {
            return new TargetResidence
            {
                RealmId     = realmId,
                ResidenceId = residenceId
            };
        }
    }
}
