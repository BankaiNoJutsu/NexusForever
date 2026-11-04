using System.Numerics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private readonly object taxiStateLock = new();
        private readonly HashSet<ushort> taxiNodes = [];
        private readonly HashSet<ushort> unsavedTaxiNodes = [];
        private readonly UpdateTimer taxiDiscoveryTimer = new(TimeSpan.FromSeconds(2));

        private void LoadTaxiNodes(IEnumerable<CharacterTaxiNodeModel> models)
        {
            lock (taxiStateLock)
                foreach (var model in models)
                    if (TaxiNodeAccess.IsAllowed(gameTableManager?.TaxiNode?.GetEntry(model.TaxiNodeId), Faction1))
                        taxiNodes.Add(model.TaxiNodeId);
        }

        public void DiscoverTaxiNode(uint taxiNodeId)
        {
            if (!TaxiNodeAccess.IsAllowed(gameTableManager?.TaxiNode?.GetEntry(taxiNodeId), Faction1))
                return;
            lock (taxiStateLock)
            {
                if (!taxiNodes.Add((ushort)taxiNodeId))
                    return;
                unsavedTaxiNodes.Add((ushort)taxiNodeId);
            }
            // 0x0186 already has the upstream 14-bit taxi-unlock wire shape.
            Session.EnqueueMessageEncrypted(new ServerEntitySelectAuxUInt14 { Value = taxiNodeId });
        }

        public bool IsNearTaxiNode(uint taxiNodeId, float range)
        {
            if (taxiNodeId == 0 || taxiNodeId > TaxiNodeAccess.MaxNodeId || !float.IsFinite(range) || range < 0f)
                return false;
            return visibleEntities.Values.OfType<IWorldEntity>().Any(e =>
                e.Map == Map && e.CreatureEntry?.TaxiNodeId == taxiNodeId
                && Vector3.DistanceSquared(e.Position, Position) <= range * range);
        }

        public void QueueTaxiFlight(WorldEntry world, Vector3 position, Vector3 rotation, uint destinationNode, ulong fare)
        {
            lock (mapAdmission.SyncRoot)
            {
                if (!IsAdmissionAllowed())
                    return;

                var destination = new MapPosition { Info = new MapInfo { Entry = world }, Position = position };
                TeleportToCore(destination, TeleportReason.Relocate, new PendingTaxiFlight(Session, destinationNode, fare, rotation));
            }
        }

        // Called under the generation guard. Check funds before leaving the source and settle at grid readiness.
        internal bool TryApplyTaxiAdmission(IMapPosition position, bool checkFare, bool settleFare = false)
        {
            if (pendingTeleport?.TaxiFlight is not { } flight)
                return true;
            if (flight.Admitted || !ReferenceEquals(pendingTeleport.MapPosition, position))
                return false;
            if (!ReferenceEquals(Session, flight.Session) || (Session is IPlayerSession bound && !ReferenceEquals(bound.Player, this)))
            {
                CancelMapAdmission();
                return false;
            }
            if (checkFare && flight.Fare != 0 && !CurrencyManager.CanAfford(CurrencyType.Credits, flight.Fare))
            {
                OnTeleportToFailed(GenericError.VendorNotEnoughCash);
                return false;
            }
            if (!settleFare)
            {
                // The destination can override the route yaw when it adjusts the arrival position.
                if (checkFare) Rotation = flight.Rotation;
                return true;
            }

            flight.Admitted = true;
            if (flight.Fare != 0)
                CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, flight.Fare);
            DiscoverTaxiNode(flight.DestinationNode);
            return true;
        }

        private void DiscoverNearbyTaxiNodes()
        {
            foreach (IWorldEntity kiosk in visibleEntities.Values.OfType<IWorldEntity>())
                if (kiosk.Map == Map && kiosk.CreatureEntry?.TaxiNodeId is > 0
                    && Vector3.DistanceSquared(kiosk.Position, Position) <= 30f * 30f)
                    DiscoverTaxiNode(kiosk.CreatureEntry.TaxiNodeId);
        }

        private void UpdateTaxiDiscovery(double lastTick)
        {
            taxiDiscoveryTimer.Update(lastTick);
            if (!taxiDiscoveryTimer.HasElapsed)
                return;
            DiscoverNearbyTaxiNodes();
            taxiDiscoveryTimer.Reset();
        }

        private void SaveTaxiNodes(CharacterContext context)
            => SaveTaxiPersistence(context);

        private void SendTaxiNodes()
        {
            var packet = new ServerFlightPathUpdate();
            lock (taxiStateLock)
                packet.FlightPathIds.AddRange(taxiNodes.OrderBy(id => id).Select(id => (uint)id));
            Session.EnqueueMessageEncrypted(packet);
        }
    }
}
