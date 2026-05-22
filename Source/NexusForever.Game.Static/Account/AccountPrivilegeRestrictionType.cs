namespace NexusForever.Game.Static.Account
{
    /// <summary>
    /// Privilege restriction indices consumed by the client for opcode 0x0971 (0..3).
    /// </summary>
    public enum AccountPrivilegeRestrictionType : uint
    {
        StorePurchaseVelocity = 0,
        StoreGiftVelocity     = 1,
        Reserved2             = 2,
        Reserved3             = 3,
    }
}
