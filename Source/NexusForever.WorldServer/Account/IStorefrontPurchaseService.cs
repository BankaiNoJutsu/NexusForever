using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Storefront;
using NexusForever.GameTable;
using NexusForever.WorldServer.Network;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.WorldServer.Account
{
    public delegate bool StorefrontDeliveryValidator(IOfferItem offerItem, IReadOnlyList<uint> accountItemIds, out StoreError error, out string reason);

    public interface IStorefrontPurchaseService
    {
        void TryPurchase(
            IWorldSession session,
            ILogger log,
            uint offerId,
            byte paymentCurrencySlot,
            ushort currencyId,
            Action<IOfferItem, IReadOnlyList<uint>> deliverItems,
            Action<IWorldSession> sendSuccess,
            string purchaseScope,
            StorefrontDeliveryValidator deliveryValidator = null,
            bool requirePlayer = true,
            Action rollbackDelivery = null);

        bool IsCurrentOrEmptyTarget(IWorldSession session, NetworkIdentity identity);

        NetworkIdentity GetCurrentPlayerIdentity(IWorldSession session);

        void SendFailure(IWorldSession session, StoreError error);

        void SendCharacterPurchaseSuccess(IWorldSession session);

        void SendAccountPurchaseSuccess(IWorldSession session);

        bool ValidateDirectAccountGrantClaim(
            IWorldSession session,
            IGameTableManager gameTableManager,
            IOfferItem offerItem,
            out StoreError error,
            out string reason);

        bool TryBuildDirectAccountGrantPlan(
            IWorldSession session,
            IGameTableManager gameTableManager,
            IOfferItem offerItem,
            bool requireDirectAccountGrant,
            out DirectAccountGrantPlan plan,
            out StoreError error,
            out string reason);

        void ApplyDirectAccountGrantPlan(IWorldSession session, DirectAccountGrantPlan plan);

        void RollbackDirectAccountGrantPlan(IWorldSession session, DirectAccountGrantPlan plan);

        void PersistAccount(IWorldSession session, ILogger log);
    }

    public sealed class DirectAccountGrantPlan
    {
        public Dictionary<AccountCurrencyType, ulong> CurrencyGrants { get; } = [];
        public Dictionary<EntitlementType, int> EntitlementGrants { get; } = [];
    }
}
