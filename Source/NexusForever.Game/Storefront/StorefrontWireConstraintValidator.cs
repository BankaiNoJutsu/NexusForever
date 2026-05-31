using NexusForever.Game.Static.Storefront;
using NexusForever.Network.World.Message.Model;
using NLog;

namespace NexusForever.Game.Storefront
{
    /// <summary>
    /// Startup checks for storefront catalog rows that exceed retail bit-field limits.
    /// </summary>
    internal static class StorefrontWireConstraintValidator
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private const uint DisplayInfoOverrideBitLimit = (1u << 14) - 1u;
        private const uint AccountItemIdBitLimit       = (1u << 15) - 1u;

        public static void Validate(
            IReadOnlyList<ServerStoreCategories.StoreCategory> categories,
            IReadOnlyList<ServerStoreOffers.OfferGroup> offerGroups)
        {
            foreach (ServerStoreOffers.OfferGroup group in offerGroups)
            {
                if (group.DisplayInfoOverride > DisplayInfoOverrideBitLimit)
                {
                    log.Error($"StorefrontCatalogDiagnostics wire constraint groupId={group.Id} " +
                        $"displayInfoOverride={group.DisplayInfoOverride} exceeds 14-bit limit.");
                }

                foreach (ServerStoreOffers.OfferGroup.Offer offer in group.Offers)
                {
                    foreach (ServerStoreOffers.OfferGroup.Offer.OfferCurrencyData currency in offer.CurrencyData)
                    {
                        if ((uint)currency.DiscountType > 2u)
                        {
                            log.Error($"StorefrontCatalogDiagnostics wire constraint groupId={group.Id} offerId={offer.Id} " +
                                $"discountType={(uint)currency.DiscountType} exceeds 2-bit limit.");
                        }
                    }

                    foreach (ServerStoreOffers.OfferGroup.Offer.OfferItemData item in offer.ItemData)
                    {
                        if (item.Type > 2u)
                        {
                            log.Error($"StorefrontCatalogDiagnostics wire constraint groupId={group.Id} offerId={offer.Id} " +
                                $"itemType={item.Type} exceeds supported range.");
                            continue;
                        }

                        if (item.Type == 0u && item.AccountItemId > AccountItemIdBitLimit)
                        {
                            log.Error($"StorefrontCatalogDiagnostics wire constraint groupId={group.Id} offerId={offer.Id} " +
                                $"accountItemId={item.AccountItemId} exceeds 15-bit limit.");
                        }
                    }
                }
            }

            _ = categories;
        }
    }
}
