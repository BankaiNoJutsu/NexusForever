using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity.Player
{
    public class ClientFlightPathPurchaseHandler : IMessageHandler<IWorldSession, ClientFlightPathPurchase>
    {
        #region Dependency Injection

        private readonly ILogger<ClientFlightPathPurchaseHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientFlightPathPurchaseHandler(
            ILogger<ClientFlightPathPurchaseHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log = log;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientFlightPathPurchase flightPathPurchase)
        {
            log.LogTrace("Received flight-path purchase request from player {PlayerGuid}: Routes=[{RouteIds}].",
                session.Player?.Guid, string.Join(", ", flightPathPurchase.RouteIds));

            if (flightPathPurchase.RouteIds.Count == 0)
            {
                RejectFlightPathPurchase(session, "empty route list");
                return;
            }

            List<TaxiRouteEntry> routes = ResolveRoutes(flightPathPurchase.RouteIds);
            if (routes == null)
            {
                RejectFlightPathPurchase(session, "one or more route ids were not found");
                return;
            }

            if (!IsContiguousRoutePath(routes))
            {
                RejectFlightPathPurchase(session, "route ids do not form a contiguous path");
                return;
            }

            ulong totalPrice = 0ul;
            foreach (TaxiRouteEntry route in routes)
                totalPrice += route.Price;

            if (!session.Player.CurrencyManager.CanAfford(CurrencyType.Credits, totalPrice))
            {
                log.LogTrace("Flight-path purchase rejected for player {PlayerGuid}: insufficient credits for route chain [{RouteIds}] with price {Price}.",
                    session.Player?.Guid, string.Join(", ", flightPathPurchase.RouteIds), totalPrice);
                session.Player.SendGenericError(GenericError.VendorNotEnoughCash);
                return;
            }

            log.LogDebug("Flight-path purchase validated for player {PlayerGuid}: route chain [{RouteIds}], source node {SourceNode}, destination node {DestinationNode}, price {Price}. Taxi embark is not implemented yet.",
                session.Player?.Guid,
                string.Join(", ", flightPathPurchase.RouteIds),
                routes[0].TaxiNodeIdSource,
                routes[^1].TaxiNodeIdDestination,
                totalPrice);
            session.Player.SendGenericError(GenericError.EmbarkNoSplineForTaxi);
        }

        private List<TaxiRouteEntry> ResolveRoutes(IReadOnlyList<uint> routeIds)
        {
            List<TaxiRouteEntry> routes = [];
            foreach (uint routeId in routeIds)
            {
                TaxiRouteEntry route = gameTableManager.TaxiRoute.GetEntry(routeId);
                if (route == null)
                    return null;

                routes.Add(route);
            }

            return routes;
        }

        private static bool IsContiguousRoutePath(IReadOnlyList<TaxiRouteEntry> routes)
        {
            for (int i = 1; i < routes.Count; i++)
                if (routes[i - 1].TaxiNodeIdDestination != routes[i].TaxiNodeIdSource)
                    return false;

            return true;
        }

        private void RejectFlightPathPurchase(IWorldSession session, string reason)
        {
            log.LogTrace("Flight-path purchase rejected for player {PlayerGuid}: {Reason}.",
                session.Player?.Guid, reason);
            session.Player.SendGenericError(GenericError.EmbarkNoSplineForTaxi);
        }
    }
}
