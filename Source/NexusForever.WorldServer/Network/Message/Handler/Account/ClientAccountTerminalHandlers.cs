using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Entity;
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

            IPlayer player = session.Player;
            IAccount account = session.Account;
            AccountOperationResult result = TryRedeem(session, player, account, redeem.CreddAmount, out CREDDRedeemResultType redeemResult);
            log.LogDebug("Processed CREDD redeem from player {PlayerGuid}: amount {CreddAmount}, result {Result}, redeemResult {RedeemResult}.",
                player?.Guid, redeem.CreddAmount, result, redeemResult);

            if (!ReferenceEquals(session.Player, player) || !ReferenceEquals(session.Account, account))
                return;
            session.EnqueueMessageEncrypted(new ServerCREDDRedeemResult((uint)redeemResult));
            ClientAccountItemOperationResultHelper.Send(session, AccountOperation.CREDDRedeem, result);
        }

        private static AccountOperationResult TryRedeem(IWorldSession session, IPlayer player, IAccount account,
            ulong creddAmount, out CREDDRedeemResultType redeemResult)
        {
            redeemResult = CREDDRedeemResultType.GenericFail;
            if (player == null)
                return AccountOperationResult.NoCharacter;
            if (account == null || !OwnsRedemption() || creddAmount > ulong.MaxValue / CreddToCreditsRatio)
                return AccountOperationResult.GenericFail;

            IAccountCurrencyManager wallet = account.CurrencyManager;
            ICurrencyManager credits = player.CurrencyManager;
            IAccountEntitlementManager entitlements = account.EntitlementManager;
            if (wallet == null || credits == null || entitlements == null)
                return AccountOperationResult.GenericFail;

            if (!wallet.CanAfford(AccountCurrencyType.Credd, creddAmount))
            {
                redeemResult = CREDDRedeemResultType.NoCREDD;
                return AccountOperationResult.NoCREDD;
            }

            ulong creditAmount = creddAmount * CreddToCreditsRatio;
            if (!credits.CanCreditFullAmount(CurrencyType.Credits, creditAmount)
                || !entitlements.CanIncreaseEntitlement(EntitlementType.CREDDUsage, 1)
                || !OwnsRedemption())
                return AccountOperationResult.GenericFail;

            wallet.CurrencySubtractAmount(AccountCurrencyType.Credd, creddAmount);
            credits.CurrencyAddAmount(CurrencyType.Credits, creditAmount);
            entitlements.UpdateEntitlement(EntitlementType.CREDDUsage, 1);

            redeemResult = CREDDRedeemResultType.Ok;
            return AccountOperationResult.Ok;

            bool OwnsRedemption() => ReferenceEquals(session.Account, account) && ReferenceEquals(account.Session, session)
                && ReferenceEquals(session.Player, player) && ReferenceEquals(player.Session, session)
                && ReferenceEquals(player.Account, account);
        }
    }
}
