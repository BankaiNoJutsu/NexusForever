namespace NexusForever.Game.Static.Account
{
    /// <summary>
    /// Six-bit result codes dispatched through the client <c>CREDDRedeemResult</c> event (opcode 0x097B).
    /// </summary>
    public enum CREDDRedeemResultType : uint
    {
        Ok           = 0,
        GenericFail  = 1,
        NoCREDD      = 2,
        NeedTransaction = 3,
        Cooldown     = 4,
    }
}
