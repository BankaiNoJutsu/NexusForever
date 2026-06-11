using System;
using System.Linq;
using NexusForever.Cryptography;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Server;
using NexusForever.Game.Static.Pregame;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;

namespace NexusForever.WorldServer.Network.Message.Handler.Character
{
    public class ClientSelectRealmHandler : IMessageHandler<IWorldSession, ClientSelectRealm>
    {
        #region Dependency Injection

        private readonly IServerManager serverManager;
        private readonly IDatabaseManager databaseManager;
        private readonly IRealmContext realmContext;

        public ClientSelectRealmHandler(
            IServerManager serverManager,
            IDatabaseManager databaseManager,
            IRealmContext realmContext = null)
        {
            this.serverManager   = serverManager;
            this.databaseManager = databaseManager;
            this.realmContext    = realmContext;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientSelectRealm selectRealm)
        {
            IServerInfo server = serverManager.Servers.SingleOrDefault(s => s.Model.Id == selectRealm.RealmId);
            if (server == null)
                throw new InvalidPacketValueException();

            // clicking back or selecting the current realm also triggers this packet, client crashes if we don't ignore it
            if (server.Model.Id == (realmContext?.RealmId ?? (ushort)0))
                return;

            if (!server.IsOnline)
            {
                session.EnqueueMessageEncrypted(new ServerRealmTransferResult
                {
                    Result = CharacterModifyResult.RealmTransferFailed_ServerDown
                });
                return;
            }

            byte[] sessionKey = RandomProvider.GetBytes(16u);
            session.Events.EnqueueEvent(new TaskEvent(databaseManager.GetDatabase<AuthDatabase>().UpdateAccountSessionKey(session.Account.Id, Convert.ToHexString(sessionKey)),
                () =>
            {
                session.EnqueueMessageEncrypted(new ServerNewRealm
                {
                    SessionKey  = sessionKey,
                    GatewayData = new ServerNewRealm.Gateway
                    {
                        Address = server.Address,
                        Port    = server.Model.Port
                    },
                    RealmName = server.Model.Name,
                    Type      = (RealmType)server.Model.Type
                });
            }));
        }
    }
}
