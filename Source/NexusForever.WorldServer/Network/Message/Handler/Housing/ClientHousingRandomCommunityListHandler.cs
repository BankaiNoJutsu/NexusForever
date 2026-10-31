using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Housing;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingRandomCommunityListHandler : IMessageHandler<IWorldSession, ClientHousingRandomCommunityList>
    {
        #region Dependency Injection

        private readonly IGlobalResidenceManager globalResidenceManager;
        private readonly IRealmContext realmContext;

        public ClientHousingRandomCommunityListHandler(
            IGlobalResidenceManager globalResidenceManager,
            IRealmContext realmContext)
        {
            this.globalResidenceManager = globalResidenceManager;
            this.realmContext           = realmContext;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientHousingRandomCommunityList _)
        {
            session.EnqueueMessageEncrypted(HousingPacketBuilder.BuildRandomCommunityList(globalResidenceManager, realmContext.RealmId));
        }
    }
}
