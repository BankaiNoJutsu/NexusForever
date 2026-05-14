using System;
using NexusForever.Network.Sts;
using NexusForever.Network.Sts.Model;

namespace NexusForever.StsServer.Network.Message.Handler
{
    public static class GameAccountHandler
    {
        [MessageHandler("/GameAccount/ListMyAccounts", SessionState.None)]
        public static void HandleListMyAccounts(StsSession session, ListMyAccountsMessage listMyAccounts)
        {
            if (session.Account == null || (!string.IsNullOrWhiteSpace(listMyAccounts.UserId) &&
                !string.Equals(listMyAccounts.UserId, session.Account.Email, StringComparison.OrdinalIgnoreCase)))
            {
                session.EnqueueMessageError(new ServerErrorMessage((int)ErrorCode.InvalidAccountNameOrPassword));
                return;
            }

            session.EnqueueMessageOk(new ListMyAccountsResponse
            {
                Alias         = session.Account.Email,
                Created       = session.Account.CreateTime.ToUniversalTime().ToString("O"),
                GameAccountId = session.Account.Id.ToString()
            });
        }
    }
}
