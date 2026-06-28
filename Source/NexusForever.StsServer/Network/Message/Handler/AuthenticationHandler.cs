using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using NexusForever.Cryptography;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Network.Sts;
using NexusForever.Network.Sts.Model;
using NexusForever.Shared.Game.Events;

namespace NexusForever.StsServer.Network.Message.Handler
{
    public static class AuthenticationHandler
    {
        private static IDatabaseManager databaseManager;

        public static void Initialise(
            IDatabaseManager databaseManager)
        {
            AuthenticationHandler.databaseManager = databaseManager;
        }

        [MessageHandler("/Auth/LoginStart", SessionState.Connected)]
        public static void HandleLoginStart(StsSession session, ClientLoginStartMessage loginStart)
        {
            session.Account     = null;
            session.KeyExchange = null;

            session.Events.EnqueueEvent(new TaskGenericEvent<AccountModel>(GetAuthDatabase().GetAccountByEmailAsync(loginStart.LoginName),
                account =>
            {
                if (account == null)
                {
                    session.EnqueueMessageError(new ServerErrorMessage((int)ErrorCode.InvalidAccountNameOrPassword));
                    return;
                }

                session.Account = account;

                byte[] s = Convert.FromHexString(account.S);
                byte[] v = Convert.FromHexString(account.V);
                session.KeyExchange = new Srp6Provider(loginStart.LoginName, s, v);

                byte[] B = session.KeyExchange.GenerateServerCredentials();
                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(s.Length);
                    writer.Write(s, 0, s.Length);
                    writer.Write(B.Length);
                    writer.Write(B, 0, B.Length);

                    session.EnqueueMessageOk(new ServerLoginStartMessage
                    {
                        KeyData = Convert.ToBase64String(stream.ToArray())
                    });
                }

                session.State = SessionState.LoginStart;
            }));
        }

        [MessageHandler("/Auth/KeyData", SessionState.LoginStart)]
        public static void HandleKeyData(StsSession session, ClientKeyDataMessage keyData)
        {
            if (session.KeyExchange == null)
            {
                RejectKeyData(session);
                return;
            }

            byte[] key;
            try
            {
                session.KeyExchange.CalculateSecret(keyData.A);
                key = session.KeyExchange.CalculateSessionKey();
            }
            catch (CryptographicException)
            {
                RejectKeyData(session);
                return;
            }

            bool validEvidence;
            try
            {
                validEvidence = session.KeyExchange.VerifyClientEvidenceMessage(keyData.M1);
            }
            catch (CryptographicException)
            {
                RejectKeyData(session);
                return;
            }

            if (!validEvidence)
            {
                RejectKeyData(session);
                return;
            }

            byte[] M2 = session.KeyExchange.CalculateServerEvidenceMessage();

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(M2.Length);
                writer.Write(M2, 0, M2.Length);

                session.EnqueueMessageOk(new ServerKeyDataMessage
                {
                    KeyData = Convert.ToBase64String(stream.ToArray())
                });
            }

            // enqueue new key to be set after next packet flush
            session.InitialiseEncryption(key);
        }

        [MessageHandler("/Auth/LoginFinish", SessionState.None)]
        public static void HandleLoginFinish(StsSession session, ClientLoginFinishMessage loginFinish)
        {
            if (session.Account == null)
            {
                session.EnqueueMessageError(new ServerErrorMessage((int)ErrorCode.InvalidAccountNameOrPassword));
                return;
            }

            var response = new ServerLoginFinishMessage
            {
                AuthType   = 0,
                LocationId = "",
                UserId     = session.Account.Email,
                UserCenter = GetUserCenter(session.Account),
                UserName   = session.Account.Email,
                AccessMask = 1L,
                Aliases    = { session.Account.Email }
            };
            AddRoleIds(response.RoleIds, session.Account);

            session.EnqueueMessageOk(response);
            session.State = SessionState.Connected;
        }

        [MessageHandler("/Auth/GetUserInfo", SessionState.None)]
        public static void HandleGetUserInfo(StsSession session, AuthGetUserInfoMessage getUserInfo)
        {
            EnqueueAuthUserInfo(session, getUserInfo.UserId);
        }

        [MessageHandler("/Auth/GetMyUserInfo", SessionState.None)]
        public static void HandleGetMyUserInfo(StsSession session, AuthGetMyUserInfoMessage getMyUserInfo)
        {
            EnqueueAuthUserInfo(session, getMyUserInfo.UserId);
        }

        [MessageHandler("/Auth/PageVerifiedIps", SessionState.None)]
        public static void HandlePageVerifiedIps(StsSession session, AuthPageVerifiedIpsMessage pageVerifiedIps)
        {
            session.EnqueueMessageOk(new AuthPageVerifiedIpsResponse());
        }

        [MessageHandler("/Auth/UnregisterVerifiedIp", SessionState.None)]
        public static void HandleUnregisterVerifiedIp(StsSession session, AuthUnregisterVerifiedIpMessage unregisterVerifiedIp)
        {
            session.EnqueueMessageOk(new EmptyStsResponse());
        }

        [MessageHandler("/Auth/RequestGameToken", SessionState.None)]
        public static void HandleRequestGameToken(StsSession session, RequestGameTokenMessage requestGameToken)
        {
            if (session.Account == null || (!string.IsNullOrWhiteSpace(requestGameToken.UserId) &&
                !string.Equals(requestGameToken.UserId, session.Account.Email, StringComparison.OrdinalIgnoreCase)))
            {
                session.EnqueueMessageError(new ServerErrorMessage((int)ErrorCode.InvalidAccountNameOrPassword));
                return;
            }

            Guid guid = RandomProvider.GetGuid();

            session.Account.GameToken = Convert.ToHexString(guid.ToByteArray());
            session.Events.EnqueueEvent(new TaskEvent(GetAuthDatabase().UpdateAccountGameToken(session.Account.Id, session.Account.GameToken),
                () =>
            {
                session.EnqueueMessageOk(new RequestGameTokenResponse
                {
                    Token = guid.ToString()
                });
            }));
        }

        [MessageHandler("/Auth/ConsumeGameToken", SessionState.None)]
        public static void HandleConsumeGameToken(StsSession session, ConsumeGameTokenMessage consumeGameToken)
        {
            string normalisedToken = NormaliseGameToken(consumeGameToken.Token);
            if (string.IsNullOrWhiteSpace(normalisedToken))
            {
                session.EnqueueMessageError(new ServerErrorMessage((int)ErrorCode.InvalidAccountNameOrPassword));
                return;
            }

            if (session.Account != null && GameTokenMatches(session.Account.GameToken, normalisedToken))
            {
                EnqueueConsumeGameTokenResponse(session, session.Account);
                return;
            }

            session.Events.EnqueueEvent(new TaskGenericEvent<AccountModel>(GetAuthDatabase().GetAccountByGameTokenAsync(normalisedToken),
                account =>
            {
                if (account == null)
                {
                    session.EnqueueMessageError(new ServerErrorMessage((int)ErrorCode.InvalidAccountNameOrPassword));
                    return;
                }

                session.Account = account;
                EnqueueConsumeGameTokenResponse(session, account);
            }));
        }

        private static void EnqueueConsumeGameTokenResponse(StsSession session, AccountModel account)
        {
            var response = new ConsumeGameTokenResponse
            {
                GameAccountId = account.Id.ToString(),
                LoginName     = account.Email,
                UserId        = account.Email,
                UserName      = account.Email,
                UserCenter    = GetUserCenter(account)
            };
            AddRoleIds(response.RoleIds, account);

            session.EnqueueMessageOk(response);
        }

        private static void EnqueueAuthUserInfo(StsSession session, string requestedIdentity)
        {
            if (session.Account != null)
            {
                EnqueueAuthUserInfo(session, session.Account, requestedIdentity);
                return;
            }

            if (string.IsNullOrWhiteSpace(requestedIdentity))
            {
                session.EnqueueMessageError(new ServerErrorMessage((int)ErrorCode.InvalidAccountNameOrPassword));
                return;
            }

            session.Events.EnqueueEvent(new TaskGenericEvent<AccountModel>(
                GetAuthDatabase().GetAccountByEmailAsync(requestedIdentity),
                account =>
            {
                if (account == null)
                {
                    session.EnqueueMessageError(new ServerErrorMessage((int)ErrorCode.InvalidAccountNameOrPassword));
                    return;
                }

                EnqueueAuthUserInfo(session, account, requestedIdentity);
            }));
        }

        private static void EnqueueAuthUserInfo(StsSession session, AccountModel account, string requestedIdentity)
        {
            if (!string.IsNullOrWhiteSpace(requestedIdentity) &&
                !string.Equals(requestedIdentity, account.Email, StringComparison.OrdinalIgnoreCase))
            {
                session.EnqueueMessageError(new ServerErrorMessage((int)ErrorCode.InvalidAccountNameOrPassword));
                return;
            }

            session.EnqueueMessageOk(new AuthUserInfoResponse
            {
                UserId     = account.Email,
                UserCenter = GetUserCenter(account),
                UserName   = account.Email,
                LoginName  = account.Email,
                UserStatus = 0,
                Created    = GetCreatedUtc(account)
            });
        }

        private static bool GameTokenMatches(string storedToken, string token)
        {
            if (string.IsNullOrWhiteSpace(storedToken) || string.IsNullOrWhiteSpace(token))
                return false;

            if (string.Equals(storedToken, token, StringComparison.OrdinalIgnoreCase))
                return true;

            string normalisedToken = NormaliseGameToken(token);
            return string.Equals(storedToken, normalisedToken, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormaliseGameToken(string token)
        {
            if (Guid.TryParse(token, out Guid guid))
                return Convert.ToHexString(guid.ToByteArray());

            string compactToken = token.Replace("-", "");
            if (compactToken.Length == 32 && IsHex(compactToken))
                return compactToken.ToUpperInvariant();

            return token;
        }

        private static bool IsHex(string value)
        {
            foreach (char c in value)
            {
                bool hex =
                    c >= '0' && c <= '9' ||
                    c >= 'a' && c <= 'f' ||
                    c >= 'A' && c <= 'F';
                if (!hex)
                    return false;
            }

            return true;
        }

        private static void AddRoleIds(ICollection<uint> roleIds, AccountModel account)
        {
            foreach (AccountRoleModel accountRole in account.AccountRole)
                roleIds.Add(accountRole.RoleId);
        }

        private static uint GetUserCenter(AccountModel account)
        {
            return account.Id == 0u ? 1u : account.Id;
        }

        private static string GetCreatedUtc(AccountModel account)
        {
            DateTime createTime = account.CreateTime == default ? DateTime.UtcNow : account.CreateTime;
            return createTime.ToUniversalTime().ToString("O");
        }

        private static AuthDatabase GetAuthDatabase()
        {
            AuthDatabase authDatabase = databaseManager?.GetDatabase<AuthDatabase>();
            return authDatabase ?? throw new InvalidOperationException("AuthDatabase is not available.");
        }

        private static void RejectKeyData(StsSession session)
        {
            session.Account     = null;
            session.KeyExchange = null;
            session.State       = SessionState.Connected;

            session.EnqueueMessageError(new ServerErrorMessage((int)ErrorCode.InvalidAccountNameOrPassword));
        }
    }
}
