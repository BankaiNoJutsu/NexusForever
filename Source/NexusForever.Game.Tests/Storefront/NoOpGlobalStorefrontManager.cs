using NexusForever.Game.Abstract.Storefront;
using NexusForever.Network.Message;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Storefront;

internal sealed class NoOpGlobalStorefrontManager : IGlobalStorefrontManager
{
    public NoOpGlobalStorefrontManager()
    {
    }

    public void Initialise()
    {
    }

    public IOfferItem GetStoreOfferItem(uint offerId) => null;

    public void HandleCatalogRequest(IGameSession session, uint accountId)
    {
    }

    public void SendCatalogPackets(IGameSession session, uint accountId = 0)
    {
    }

    public void MarkAccountCatalogRequestedBeforeWorldLogin(uint accountId)
    {
    }

    public void SendBootstrapCatalogPacketsIfNeeded(IGameSession session, uint accountId)
    {
    }

    public void ClearCatalogDeliveryState(string sessionId, uint accountId = 0)
    {
    }
}
