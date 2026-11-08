using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
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
            IPlayer player = session.Player;
            if (player == null)
                return;

            log.LogTrace("Received flight-path purchase request from player {PlayerGuid}: Routes=[{RouteIds}].",
                player.Guid, string.Join(", ", flightPathPurchase.RouteIds));

            if (flightPathPurchase.RouteIds.Count == 0 || flightPathPurchase.RouteIds.Count > 30)
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

            if (gameTableManager.TaxiNode == null)
            {
                RejectFlightPathPurchase(session, "taxi node table is unavailable");
                return;
            }

            foreach (uint nodeId in routes.SelectMany(r => new[] { r.TaxiNodeIdSource, r.TaxiNodeIdDestination }).Distinct())
                if (!TaxiNodeAccess.IsAllowed(gameTableManager.TaxiNode.GetEntry(nodeId), player.Faction1))
                {
                    RejectFlightPathPurchase(session, "route contains a missing or inaccessible taxi node");
                    return;
                }

            if (!player.IsNearTaxiNode(routes[0].TaxiNodeIdSource, 15f))
            {
                RejectFlightPathPurchase(session, "no source kiosk within range");
                return;
            }

            TaxiNodeEntry destinationNode = gameTableManager.TaxiNode.GetEntry(routes[^1].TaxiNodeIdDestination);
            if (destinationNode == null)
            {
                RejectFlightPathPurchase(session, "destination node was not found");
                return;
            }

            if (gameTableManager.WorldLocation2 == null)
            {
                RejectFlightPathPurchase(session, "world location table is unavailable");
                return;
            }

            WorldLocation2Entry destinationLocation = gameTableManager.WorldLocation2.GetEntry(destinationNode.WorldLocation2Id);
            if (destinationLocation == null)
            {
                RejectFlightPathPurchase(session, $"destination world location {destinationNode.WorldLocation2Id} was not found");
                return;
            }

            WorldEntry world = destinationLocation.WorldId is > 0u and <= ushort.MaxValue
                ? gameTableManager.World?.GetEntry(destinationLocation.WorldId) : null;
            if (world == null
                || !float.IsFinite(destinationLocation.Position0) || !float.IsFinite(destinationLocation.Position1)
                || !float.IsFinite(destinationLocation.Position2))
            {
                RejectFlightPathPurchase(session, "destination world or position is invalid");
                return;
            }

            if (!player.CanTeleport())
            {
                player.SendGenericError(GenericError.InstanceTransferPending);
                return;
            }

            // Retail charges trunc(T * F * T) per hop from the destination node's ContentTier, never the raw Price column (TAXI_FARE_NATIVE_READ_20261108.md).
            ulong totalPrice = TaxiFare.Calculate(gameTableManager, routes);
            if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, totalPrice))
            {
                player.SendGenericError(GenericError.VendorNotEnoughCash);
                return;
            }
            if (!ReferenceEquals(session.Player, player))
                return;

            log.LogDebug("Requesting flight-path admission for player {PlayerGuid}: route chain [{RouteIds}], source node {SourceNode}, destination node {DestinationNode}, price {Price}.",
                player.Guid,
                string.Join(", ", flightPathPurchase.RouteIds),
                routes[0].TaxiNodeIdSource,
                routes[^1].TaxiNodeIdDestination,
                totalPrice);
            Vector3 rotation = new Quaternion(destinationLocation.Facing0, destinationLocation.Facing1,
                destinationLocation.Facing2, destinationLocation.Facing3).ToEuler();
            var position = new Vector3(destinationLocation.Position0, destinationLocation.Position1, destinationLocation.Position2);
            player.QueueTaxiFlight(world, position, rotation, destinationNode.Id, totalPrice);
        }

        private List<TaxiRouteEntry> ResolveRoutes(IReadOnlyList<uint> routeIds)
        {
            if (gameTableManager.TaxiRoute == null)
                return null;

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
