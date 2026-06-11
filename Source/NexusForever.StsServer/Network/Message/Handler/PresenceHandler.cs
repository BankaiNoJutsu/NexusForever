using System;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Network.Sts;
using NexusForever.Network.Sts.Model;
using NexusForever.Shared.Game.Events;

namespace NexusForever.StsServer.Network.Message.Handler
{
    public static class PresenceHandler
    {
        private static IDatabaseManager databaseManager;

        public static void Initialise(
            IDatabaseManager databaseManager)
        {
            PresenceHandler.databaseManager = databaseManager;
        }

        [MessageHandler("/Presence/Login", SessionState.None)]
        public static void HandlePresenceLogin(StsSession session, PresenceLoginMessage message)
        {
            EnqueuePresenceUserInfo(session, GetRequestedIdentity(message));
        }

        [MessageHandler("/Presence/GetUserInfo", SessionState.None)]
        public static void HandlePresenceGetUserInfo(StsSession session, PresenceGetUserInfoMessage message)
        {
            EnqueuePresenceUserInfo(session, GetRequestedIdentity(message));
        }

        [MessageHandler("/Presence/Logout", SessionState.None)]
        public static void HandlePresenceLogout(StsSession session, PresenceLogoutMessage message)
        {
            // The retail client keeps the STS socket alive and can start SRP again after logging out.
            session.State       = SessionState.Connected;
            session.Account     = null;
            session.KeyExchange = null;

            session.EnqueueMessageOk(new EmptyStsResponse());
        }

        [MessageHandler("/Presence/SetAppData", SessionState.None)]
        public static void HandlePresenceSetAppData(StsSession session, PresenceSetAppDataMessage message)
        {
            session.EnqueueMessageOk(new EmptyStsResponse());
        }

        [MessageHandler("/Presence/Reversed", SessionState.None)]
        public static void HandlePresenceReversed(StsSession session, PresenceReversedMessage message)
        {
            session.EnqueueMessageOk(new EmptyStsResponse());
        }

        [MessageHandler("/Presence/SendUserInfo", SessionState.None)]
        public static void HandlePresenceSendUserInfo(StsSession session, PresenceSendUserInfoMessage message)
        {
            session.EnqueueMessageOk(new EmptyStsResponse());
        }

        [MessageHandler("/Presence/XferRequest", SessionState.None)]
        public static void HandlePresenceXferRequest(StsSession session, PresenceXferRequestMessage message)
        {
            session.EnqueueMessageOk(new EmptyStsResponse());
        }

        [MessageHandler("/Presence/XferPresences", SessionState.None)]
        public static void HandlePresenceXferPresences(StsSession session, PresenceXferPresencesMessage message)
        {
            session.EnqueueMessageOk(new EmptyStsResponse());
        }

        private static string GetRequestedIdentity(PresenceMessage message)
        {
            return FirstNonEmpty(message.UserId, message.LoginName, message.UserName, message.Alias);
        }

        private static void EnqueuePresenceUserInfo(StsSession session, string requestedIdentity)
        {
            if (session.Account != null)
            {
                EnqueuePresenceUserInfo(session, session.Account, requestedIdentity);
                return;
            }

            if (string.IsNullOrWhiteSpace(requestedIdentity))
            {
                session.EnqueueMessageOk(CreatePresenceUserInfoResponse(null, requestedIdentity));
                return;
            }

            session.Events.EnqueueEvent(new TaskGenericEvent<AccountModel>(
                GetAuthDatabase().GetAccountByEmailAsync(requestedIdentity),
                account =>
            {
                EnqueuePresenceUserInfo(session, account, requestedIdentity);
            }));
        }

        private static void EnqueuePresenceUserInfo(StsSession session, AccountModel account, string requestedIdentity)
        {
            if (account != null && !string.IsNullOrWhiteSpace(requestedIdentity) &&
                !string.Equals(requestedIdentity, account.Email, StringComparison.OrdinalIgnoreCase))
            {
                session.EnqueueMessageError(new ServerErrorMessage((int)ErrorCode.InvalidAccountNameOrPassword));
                return;
            }

            session.EnqueueMessageOk(CreatePresenceUserInfoResponse(account, requestedIdentity));
        }

        private static PresenceUserInfoResponse CreatePresenceUserInfoResponse(AccountModel account, string requestedIdentity)
        {
            string identity = FirstNonEmpty(account?.Email, requestedIdentity);
            string created = account?.CreateTime.ToUniversalTime().ToString("O") ?? DateTime.UtcNow.ToString("O");
            uint userCenter = account == null || account.Id == 0u ? 1u : account.Id;

            var response = new PresenceUserInfoResponse
            {
                LocationId = "",
                UserId     = identity,
                UserCenter = userCenter,
                UserName   = identity,
                LoginName  = identity,
                AccessMask = 1L,
                UserStatus = 0,
                Status     = "",
                Created    = created
            };

            if (!string.IsNullOrWhiteSpace(identity))
                response.Aliases.Add(identity);

            return response;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return "";
        }

        private static AuthDatabase GetAuthDatabase()
        {
            AuthDatabase authDatabase = databaseManager?.GetDatabase<AuthDatabase>();
            return authDatabase ?? throw new InvalidOperationException("AuthDatabase is not available.");
        }
    }
}
