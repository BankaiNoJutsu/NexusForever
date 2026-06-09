using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Account.Inventory;
using NexusForever.Network;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.CREDDExchange;

namespace NexusForever.WorldServer.Network.Message.Handler.Account
{
    public class ClientDailyLoginClaimRewardHandler : IMessageHandler<IWorldSession, ClientDailyLoginClaimReward>
    {
        private readonly ILogger<ClientDailyLoginClaimRewardHandler> log;

        public ClientDailyLoginClaimRewardHandler(ILogger<ClientDailyLoginClaimRewardHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientDailyLoginClaimReward _)
        {
            AccountOperationResult result;
            if (!ClientAccountInventoryHandlerGuard.TryGetInventoryManager(session, AccountOperation.RequestDailyLoginRewards, out IAccountInventoryManager inventoryManager, out result))
            {
                log.LogDebug("Processed daily-login claim from player {PlayerGuid}: result {Result}.", session.Player?.Guid, result);
                return;
            }

            result = inventoryManager.ClaimDailyLoginReward();
            log.LogDebug("Processed daily-login claim from player {PlayerGuid}: result {Result}.", session.Player?.Guid, result);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.RequestDailyLoginRewards, result);
        }
    }

    public class ClientAccountRedeemCouponHandler : IMessageHandler<IWorldSession, ClientAccountRedeemCoupon>
    {
        private readonly ILogger<ClientAccountRedeemCouponHandler> log;

        public ClientAccountRedeemCouponHandler(ILogger<ClientAccountRedeemCouponHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAccountRedeemCoupon redeemCoupon)
        {
            AccountOperationResult result = CouponRedemptionService.TryRedeem(session.Account, redeemCoupon.CouponCode, out uint accountItemId);
            log.LogDebug("Processed coupon redemption from player {PlayerGuid}: code {CouponCode}, accountItem {AccountItemId}, result {Result}.",
                session.Player?.Guid, redeemCoupon.CouponCode, accountItemId, result);
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.RedeemCoupon, result);
        }
    }

    public class ClientCREDDRedeemHandler : IMessageHandler<IWorldSession, ClientCREDDRedeem>
    {
        private const ulong CreddToCreditsRatio = 1000ul;

        private readonly ILogger<ClientCREDDRedeemHandler> log;

        public ClientCREDDRedeemHandler(ILogger<ClientCREDDRedeemHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCREDDRedeem redeem)
        {
            if (redeem.CreddAmount == 0ul)
                throw new InvalidPacketValueException();

            AccountOperationResult result = TryRedeem(session, redeem.CreddAmount, out CREDDRedeemResultType redeemResult);
            log.LogDebug("Processed CREDD redeem from player {PlayerGuid}: amount {CreddAmount}, result {Result}, redeemResult {RedeemResult}.",
                session.Player?.Guid, redeem.CreddAmount, result, redeemResult);

            session.EnqueueMessageEncrypted(new ServerCREDDRedeemResult((uint)redeemResult));
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.CREDDRedeem, result);
        }

        private static AccountOperationResult TryRedeem(IWorldSession session, ulong creddAmount, out CREDDRedeemResultType redeemResult)
        {
            redeemResult = CREDDRedeemResultType.GenericFail;
            if (session.Player == null)
                return AccountOperationResult.NoCharacter;

            if (!session.Account.CurrencyManager.CanAfford(AccountCurrencyType.Credd, creddAmount))
            {
                redeemResult = CREDDRedeemResultType.NoCREDD;
                return AccountOperationResult.NoCREDD;
            }

            ulong creditAmount = creddAmount * CreddToCreditsRatio;
            if (creditAmount == 0ul)
                return AccountOperationResult.GenericFail;

            session.Account.CurrencyManager.CurrencySubtractAmount(AccountCurrencyType.Credd, creddAmount);
            session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, creditAmount);
            session.Account.EntitlementManager.UpdateEntitlement(EntitlementType.CREDDUsage, 1);

            redeemResult = CREDDRedeemResultType.Ok;
            return AccountOperationResult.Ok;
        }
    }
}
