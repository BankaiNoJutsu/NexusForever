using System.Runtime.CompilerServices;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Tests.Storefront;

public class PlayerStorefrontBootstrapTests
{
    [Fact]
    public void CanSendDeferredInWorldStorefrontCatalog_WhenLoading_IsFalse()
    {
        Player player = CreatePlayer();

        player.IsLoading = true;

        Assert.False(player.CanSendDeferredInWorldStorefrontCatalog());
    }

    [Fact]
    public void CanSendDeferredInWorldStorefrontCatalog_AfterLoading_IsTrue()
    {
        Player player = CreatePlayer();

        player.IsLoading = false;

        Assert.True(player.CanSendDeferredInWorldStorefrontCatalog());
    }

    private static Player CreatePlayer()
    {
        return (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
    }
}
