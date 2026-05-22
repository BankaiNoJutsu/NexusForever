namespace NexusForever.Game.Static.Storefront
{
    public enum StoreError
    {
        CatalogUnavailable       = 0,
        StoreDisabled            = 1,
        InvalidOffer             = 2,
        InvalidPrice             = 3,
        GenericFail              = 4,
        PurchasePending          = 5,
        PgWsCartFraudFailure     = 6,
        PgWsCartPaymentFailure   = 7,
        PgWsInvalidCCExpirationDate = 8,
        PgWsInvalidCreditCardNumber = 9,
        PgWsCreditCardExpired    = 10,
        PgWsCreditCardDeclined   = 11,
        PgWsCreditFloorExceeded  = 12,
        PgWsInventoryStatusFailure = 13,
        PgWsPaymentPostAuthFailure = 14,
        PgWsSubmitCartFailed     = 15,
        PurchaseVelocityLimit    = 16,
        MissingItemEntitlement   = 17,
        IneligibleGiftRecipient  = 18,
        CannotUseOffer           = 19,
        MissingEntitlement       = 20,
        CannotGiftOffer          = 21
    }
}
