using NexusForever.Game.Account.Inventory;

namespace NexusForever.Game.Tests.Account.Inventory;

public class StorePurchaseVelocityLimiterTests
{
    [Fact]
    public void IsWithinVelocityLimit_AllowsPurchasesUntilWindowCap()
    {
        const uint accountId = 99001u;

        for (int i = 0; i < 10; i++)
            StorePurchaseVelocityLimiter.NotePurchase(accountId);

        Assert.False(StorePurchaseVelocityLimiter.IsWithinVelocityLimit(accountId));
    }
}
