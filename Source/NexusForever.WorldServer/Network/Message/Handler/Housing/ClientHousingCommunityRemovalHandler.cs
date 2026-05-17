using System;
using System.Linq;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingCommunityRemovalHandler : IMessageHandler<IWorldSession, ClientHousingCommunityRemoval>
    {
        #region Dependency Injection

        private readonly IGlobalResidenceManager globalResidenceManager;
        private readonly IMapLockManager mapLockManager;
        private readonly IRealmContext realmContext;

        public ClientHousingCommunityRemovalHandler(
            IGlobalResidenceManager globalResidenceManager,
            IMapLockManager mapLockManager,
            IRealmContext realmContext)
        {
            this.globalResidenceManager = globalResidenceManager;
            this.mapLockManager         = mapLockManager;
            this.realmContext           = realmContext;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientHousingCommunityRemoval housingCommunityRemoval)
        {
            if (session.Player.Map is not IResidenceMapInstance)
                throw new InvalidPacketValueException();

            ICommunity community = session.Player.GuildManager.GetGuild<ICommunity>(GuildType.Community);
            if (community?.Residence == null)
                throw new InvalidPacketValueException();

            IResidenceEntrance entrance = globalResidenceManager.GetResidenceEntrance(PropertyInfoId.Residence);
            if (entrance == null)
                throw new InvalidOperationException();

            if (housingCommunityRemoval.TargetResidence.RealmId != realmContext.RealmId)
                throw new InvalidPacketValueException();

            IResidenceChild child = community.Residence
                .GetChildren()
                .SingleOrDefault(c => c.Residence.Id == housingCommunityRemoval.TargetResidence.ResidenceId);
            if (child == null)
                throw new InvalidPacketValueException();

            bool isOwner = child.Residence.OwnerId == session.Player.CharacterId;
            if (!isOwner)
            {
                IGuildMember member = community.GetMember(session.Player.CharacterId);
                if (member == null || !member.Rank.HasPermission(GuildRankPermission.RemoveCommunityPlotReservation))
                    throw new InvalidPacketValueException();
            }

            if (child.Residence.Map != null)
                child.Residence.Map.RemoveChild(child.Residence);
            else
                child.Residence.Parent.RemoveChild(child.Residence);

            if (child.Residence.OwnerId.HasValue)
            {
                IGuildMember ownerMember = community.GetMember(child.Residence.OwnerId.Value);
                if (ownerMember != null)
                    ownerMember.CommunityPlotReservation = -1;
            }

            child.Residence.PropertyInfoId = PropertyInfoId.Residence;

            if (!isOwner)
                return;

            // shouldn't need to check for existing instance
            // individual residence instances are unloaded when transfered to a community
            // if for some reason the instance is still unloading the residence will be initalised again after
            IMapLock mapLock = mapLockManager.GetResidenceLock(child.Residence);
            session.Player.Rotation = entrance.Rotation.ToEuler();
            session.Player.TeleportTo(entrance.Entry, entrance.Position, mapLock);
        }
    }
}
