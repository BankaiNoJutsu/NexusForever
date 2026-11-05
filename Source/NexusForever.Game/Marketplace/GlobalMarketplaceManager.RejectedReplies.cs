using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        private Action<GenericError, AuctionInfo> RetainRejectedAuctionReply(Player owner,
            Action<GenericError, AuctionInfo> reply, Func<bool> publicationAllowed)
            => RetainRejectedReply(owner, reply, view =>
            {
                var snapshot = CloneAuction(view);
                ulong deadline = checked((ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + snapshot.ExpirationTime);
                return () => CloneAuction(snapshot, deadline);
            }, publicationAllowed);

        private Action<GenericError, CommodityOrder> RetainRejectedCommodityReply(Player owner,
            Action<GenericError, CommodityOrder> reply, Func<bool> publicationAllowed)
            => RetainRejectedReply(owner, reply, view =>
            {
                var snapshot = CloneOrder(view);
                return () => CloneOrder(snapshot);
            }, publicationAllowed);

        // These public request replies share actor lifetime and retry timing.
        // The two wire payloads retain their own snapshot/expiry semantics.
        private Action<GenericError, T> RetainRejectedReply<T>(Player owner, Action<GenericError, T> reply,
            Func<T, Func<T>> captureView, Func<bool> publicationAllowed)
        {
            var session = owner.Session;
            var queue = session.Events;
            var account = owner.Account;
            ulong character = owner.CharacterId;
            uint accountId = account.Id;
            bool CurrentActor() => ReferenceEquals(owner.Session, session) && ReferenceEquals(session.Events, queue)
                && (session is not IPlayerSession bound || ReferenceEquals(bound.Player, owner))
                && owner.CharacterId == character && ReferenceEquals(owner.Account, account) && account.Id == accountId
                && ReferenceEquals(account.Session, session) && ReferenceEquals(playerManager?.GetPlayer(character), owner)
                && session is not NetworkSession { IsDisconnecting: true }
                && owner.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished)
                && publicationAllowed?.Invoke() != false;

            return (error, view) =>
            {
                // Successful replies keep each coordinator's listing authority
                // and notification ordering.
                if (error == GenericError.Ok) { reply(error, view); return; }
                Func<T> freshView = captureView(view);
                void Publish()
                {
                    if (!CurrentActor()) return;
                    try { reply(error, freshView()); }
                    catch (Exception failure)
                    {
                        log.Warn(failure, "Rejected marketplace reply failed; retrying on its original actor.");
                        var delay = Task.Delay(TimeSpan.FromSeconds(1));
                        queue.EnqueueThreadSafe(new PredicateEvent(() => !CurrentActor() || delay.IsCompleted, Publish));
                    }
                }
                Publish();
            };
        }
    }
}
