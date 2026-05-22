using NexusForever.Game;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Housing;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    internal static class HousingVisitHelper
    {
        public static bool CanProcessVisit(IWorldSession session)
        {
            if (session.Player.Map is not IResidenceMapInstance)
                throw new InvalidPacketValueException();

            return session.Player.CanTeleport();
        }

        public static void VisitResidence(
            IWorldSession session,
            IResidence residence,
            ushort realmId,
            ulong residenceId,
            string playerName,
            IGlobalResidenceManager globalResidenceManager,
            IMapLockManager mapLockManager)
        {
            if (residence == null)
            {
                SendHousingVisitResult(session, realmId, residenceId, playerName, HousingResult.Visit_Failed);
                return;
            }

            switch (residence.PrivacyLevel)
            {
                case ResidencePrivacyLevel.Private:
                    SendHousingVisitResult(session, realmId, residenceId, playerName, HousingResult.Visit_Private);
                    return;
                case ResidencePrivacyLevel.NeighborsOnly:
                    if (residence.HasNeighbor(session.Player.CharacterId) || residence.CanModifyResidence(session.Player))
                        break;

                    SendHousingVisitResult(session, realmId, residenceId, playerName, HousingResult.InvalidPermissions);
                    return;
                case ResidencePrivacyLevel.RoommatesOnly:
                    if (!residence.CanModifyResidence(session.Player))
                    {
                        SendHousingVisitResult(session, realmId, residenceId, playerName, HousingResult.InvalidPermissions);
                        return;
                    }
                    break;
            }

            IMapLock mapLock = mapLockManager.GetResidenceLock(residence.Parent ?? residence);
            IResidenceEntrance entrance = globalResidenceManager.GetResidenceEntrance(residence.PropertyInfoId);

            session.Player.Rotation = entrance.Rotation.ToEuler();
            session.Player.TeleportTo(new MapPosition
            {
                Info = new MapInfo
                {
                    Entry   = entrance.Entry,
                    MapLock = mapLock
                },
                Position = entrance.Position
            });
        }

        public static void SendHousingVisitResult(
            IWorldSession session,
            ushort realmId,
            ulong residenceId,
            string playerName,
            HousingResult result)
        {
            session.EnqueueMessageEncrypted(new ServerHousingResult
            {
                RealmId     = realmId,
                ResidenceId = residenceId,
                PlayerName  = playerName ?? string.Empty,
                Result      = result
            });
        }
    }
}
