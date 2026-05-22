using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingCommunityPrivacyLevelHandler : IMessageHandler<IWorldSession, ClientHousingCommunityPrivacyLevel>
    {
        #region Dependency Injection

        private readonly IGlobalResidenceManager globalResidenceManager;
        private readonly IRealmContext realmContext;

        public ClientHousingCommunityPrivacyLevelHandler(
            IGlobalResidenceManager globalResidenceManager,
            IRealmContext realmContext)
        {
            this.globalResidenceManager = globalResidenceManager;
            this.realmContext           = realmContext;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientHousingCommunityPrivacyLevel housingCommunityPrivacyLevel)
        {
            if (session.Player.Map is not IResidenceMapInstance)
                throw new InvalidPacketValueException();

            ICommunity community = session.Player.GuildManager.GetGuild<ICommunity>(GuildType.Community);
            if (community?.Residence == null)
                throw new InvalidPacketValueException();

            if (housingCommunityPrivacyLevel.TargetResidence.RealmId != realmContext.RealmId ||
                housingCommunityPrivacyLevel.TargetResidence.ResidenceId != community.Residence.Id)
                throw new InvalidPacketValueException();

            IGuildMember member = community.GetMember(session.Player.CharacterId);
            if (member == null || !member.Rank.HasPermission(GuildRankPermission.ChangeCommunityRemodelOptions))
                throw new InvalidPacketValueException();

            bool isPrivate = housingCommunityPrivacyLevel.PrivacyLevel == CommunityPrivacyLevel.Private;
            if (!isPrivate)
            {
                globalResidenceManager.DeregisterCommunityVists(community.Residence.Id);
                globalResidenceManager.RegisterCommunityVisits(community.Residence, community, session.Player.Name);
            }
            else
                globalResidenceManager.DeregisterCommunityVists(community.Residence.Id);

            community.SetCommunityPrivate(isPrivate);

            var message = new ServerHousingCommunityPrivacyLevelUpdate
            {
                Flags        = isPrivate ? ServerHousingCommunityPrivacyLevelUpdate.PrivateFlag : 0u,
                PrivacyLevel = isPrivate
                    ? ServerHousingCommunityPrivacyLevelUpdate.PrivateLuaValue
                    : ServerHousingCommunityPrivacyLevelUpdate.PublicLuaValue
            };
            message.TargetResidence.RealmId     = realmContext.RealmId;
            message.TargetResidence.ResidenceId = community.Residence.Id;

            session.EnqueueMessageEncrypted(message);
        }
    }
}
