using System;
using NexusForever.Game;
using NexusForever.Game.Abstract.Guild;
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
    public class ClientHousingVisitHandler : IMessageHandler<IWorldSession, ClientHousingVisit>
    {
        #region Dependency Injection

        private readonly IGlobalResidenceManager globalResidenceManager;
        private readonly IGlobalGuildManager globalGuildManager;
        private readonly IMapLockManager mapLockManager;

        public ClientHousingVisitHandler(
            IGlobalResidenceManager globalResidenceManager,
            IGlobalGuildManager globalGuildManager,
            IMapLockManager mapLockManager)
        {
            this.globalResidenceManager = globalResidenceManager;
            this.globalGuildManager     = globalGuildManager;
            this.mapLockManager         = mapLockManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientHousingVisit housingVisit)
        {
            if (!HousingVisitHelper.CanProcessVisit(session))
                return;

            IResidence residence;
            if (!string.IsNullOrEmpty(housingVisit.TargetResidenceName))
                residence = globalResidenceManager.GetResidenceByOwner(housingVisit.TargetResidenceName);
            else if (!string.IsNullOrEmpty(housingVisit.TargetCommunityName))
                residence = globalResidenceManager.GetCommunityByOwner(housingVisit.TargetCommunityName);
            else if (housingVisit.TargetResidence.ResidenceId != 0ul)
                residence = globalResidenceManager.GetResidence(housingVisit.TargetResidence.ResidenceId);
            else if (housingVisit.TargetCommunity.NeighbourhoodId != 0ul)
            {
                ulong residenceId = globalGuildManager.GetGuild<ICommunity>(housingVisit.TargetCommunity.NeighbourhoodId)?.Residence?.Id ?? 0ul;
                residence = globalResidenceManager.GetResidence(residenceId);
            }
            else
            {
                HousingVisitHelper.SendHousingVisitResult(
                    session,
                    housingVisit.TargetResidence.RealmId,
                    housingVisit.TargetResidence.ResidenceId,
                    housingVisit.TargetResidenceName ?? housingVisit.TargetCommunityName,
                    HousingResult.Visit_Failed);
                return;
            }

            HousingVisitHelper.VisitResidence(
                session,
                residence,
                housingVisit.TargetResidence.RealmId,
                housingVisit.TargetResidence.ResidenceId,
                housingVisit.TargetResidenceName ?? housingVisit.TargetCommunityName,
                globalResidenceManager,
                mapLockManager);
        }
    }
}
