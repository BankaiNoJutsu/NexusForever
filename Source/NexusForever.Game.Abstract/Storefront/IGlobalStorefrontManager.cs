using NexusForever.Network.Session;

namespace NexusForever.Game.Abstract.Storefront
{
    public interface IGlobalStorefrontManager
    {
        void Initialise();

        /// <summary>
        /// Return the <see cref="IOfferItem"/> that matches the supplied offer ID
        /// </summary>
        IOfferItem GetStoreOfferItem(uint offerId);

        /// <summary>
        /// This method is used to send the current Store Catalog to the <see cref="IGameSession"/>
        /// </summary>
        void HandleCatalogRequest(IGameSession session, uint accountId);

        /// <summary>
        /// Sends only the catalog packets (<c>0x0988</c>, <c>0x098B</c>, <c>0x0987</c>) without account inventory refresh.
        /// </summary>
        void SendCatalogPackets(IGameSession session, uint accountId = 0);

        /// <summary>
        /// Records that character select issued <c>0x082D</c> before a world player existed.
        /// </summary>
        void MarkAccountCatalogRequestedBeforeWorldLogin(uint accountId);

        /// <summary>
        /// Sends the initial catalog after entering the world when this account has not received one yet.
        /// </summary>
        void SendBootstrapCatalogPacketsIfNeeded(IGameSession session, uint accountId);

        /// <summary>
        /// Clears catalog delivery state when the client disconnects or returns to character select.
        /// </summary>
        void ClearCatalogDeliveryState(string sessionId, uint accountId = 0);
    }
}