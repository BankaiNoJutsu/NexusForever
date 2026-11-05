using System;
using System.Threading.Tasks;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Crafting;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Shared.Game.Events;

namespace NexusForever.WorldServer.Network.Message.Handler.Authentication
{
    public class ClientHelloRealmHandler : IMessageHandler<IWorldSession, ClientHelloRealm>
    {
        #region Depenency Injection

        private readonly IDatabaseManager databaseManager;
        private readonly IRealmContext realmContext;
        private readonly IGameTableManager gameTableManager;

        public ClientHelloRealmHandler(
            IDatabaseManager databaseManager,
            IRealmContext realmContext, IGameTableManager gameTableManager = null)
        {
            this.databaseManager = databaseManager;
            this.realmContext = realmContext;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientHelloRealm helloRealm)
        {
            if (session.Account != null)
                throw new InvalidPacketValueException("Realm session is already initialized.");
            // prevent packets from being processed until asynchronous account select task is complete
            session.CanProcessIncomingPackets = false;

            string sessionKey = Convert.ToHexString(helloRealm.SessionKey);
            session.Events.EnqueueEvent(new TaskGenericEvent<AccountModel>(AdmitAsync(helloRealm.AccountId, helloRealm.AccountString, sessionKey),
                account =>
            {
                if (session is NetworkSession { IsDisconnecting: true }) return;
                if (account == null)
                    throw new InvalidPacketValueException($"Realm account admission rejected (account={helloRealm.AccountId}).");

                session.Initialise(account);
                session.SetEncryptionKey(helloRealm.SessionKey);
                session.CanProcessIncomingPackets = true;
            },
            () =>
            {
                if (session is NetworkSession { IsDisconnecting: true }) return;
                session.CanProcessIncomingPackets = true;
                session.ForceDisconnect();
            }));
        }

        private async Task<AccountModel> AdmitAsync(uint accountId, string accountName, string sessionKey)
        {
            var auth = databaseManager.GetDatabase<AuthDatabase>();
            var account = await auth.AdmitWorldSessionAsync(accountId, accountName, sessionKey);
            if (account != null)
            {
                await VendorAccountPurchase.RecoverAtAdmissionAsync(account, auth, databaseManager.GetDatabase<CharacterDatabase>(), realmContext.RealmId);
                await CraftingAccountRecovery.RecoverAtAdmissionAsync(account, auth, databaseManager.GetDatabase<CharacterDatabase>(), realmContext.RealmId, gameTableManager);
            }
            return account;
        }
    }
}
