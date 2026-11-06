using System.Numerics;
using System.Threading;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Map;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private readonly object taxiStateLock = new();
        private readonly HashSet<ushort> taxiNodes = [];
        private readonly HashSet<ushort> unsavedTaxiNodes = [];
        private readonly UpdateTimer taxiDiscoveryTimer = new(TimeSpan.FromSeconds(2));
        private long taxiFareVersion;
        private long TaxiFareVersion => Volatile.Read(ref taxiFareVersion);

        private void LoadTaxiNodes(IEnumerable<CharacterTaxiNodeModel> models)
        {
            lock (taxiStateLock)
                foreach (var model in models)
                    if (TaxiNodeAccess.IsAllowed(gameTableManager?.TaxiNode?.GetEntry(model.TaxiNodeId), Faction1))
                        taxiNodes.Add(model.TaxiNodeId);
        }

        public void DiscoverTaxiNode(uint taxiNodeId)
        {
            if (TryLearnTaxiNode(taxiNodeId)) SendTaxiNodeUnlock(Session, taxiNodeId);
        }

        private bool TryLearnTaxiNode(uint taxiNodeId)
        {
            if (!TaxiNodeAccess.IsAllowed(gameTableManager?.TaxiNode?.GetEntry(taxiNodeId), Faction1))
                return false;
            lock (taxiStateLock)
            {
                if (!taxiNodes.Add((ushort)taxiNodeId))
                    return false;
                unsavedTaxiNodes.Add((ushort)taxiNodeId);
            }
            return true;
        }

        private static void SendTaxiNodeUnlock(IGameSession session, uint taxiNodeId)
        {
            // 0x0186 already has the upstream 14-bit taxi-unlock wire shape.
            session.EnqueueMessageEncrypted(new ServerEntitySelectAuxUInt14 { Value = taxiNodeId });
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

        internal ulong? GetRapidTransportAdmissionGeneration(ushort nodeId)
        {
            lock (mapAdmission.SyncRoot)
            {
                if (IsLoading)
                    return null;

                var world = ResolveRapidTransportWorld(nodeId, out _);
                return world != null && disableManager?.IsDisabled(DisableType.World, world.Id) != true
                    ? mapAdmission.Generation
                    : null;
            }
        }

        internal void QueueRapidTransport(ushort nodeId, ulong? castAdmissionGeneration = null)
        {
            lock (mapAdmission.SyncRoot)
            {
                if (castAdmissionGeneration is ulong generation && !mapAdmission.IsCurrent(generation))
                    return;

                var world = ResolveRapidTransportWorld(nodeId, out var location);
                if (world == null) return;

                var destination = new MapPosition
                {
                    Info = new MapInfo { Entry = world },
                    Position = new Vector3(location.Position0, location.Position1, location.Position2)
                };
                var facing = new Quaternion(location.Facing0, location.Facing1, location.Facing2, location.Facing3);
                TeleportToCore(destination, TeleportReason.Relocate, arrivalRotation: facing.ToEuler());
            }
        }

        private WorldEntry ResolveRapidTransportWorld(ushort nodeId, out WorldLocation2Entry location)
        {
            location = null;
            if (!IsAdmissionAllowed() || !CanTeleport() || mapManager == null
                || (Session is IPlayerSession bound && !ReferenceEquals(bound.Player, this))) return null;

            var node = gameTableManager?.TaxiNode?.GetEntry(nodeId);
            if (node == null) return null;
            location = gameTableManager.WorldLocation2?.GetEntry(node.WorldLocation2Id);
            if (location == null || location.WorldId is 0 or > ushort.MaxValue
                || !float.IsFinite(location.Position0) || !float.IsFinite(location.Position1) || !float.IsFinite(location.Position2)
                || !float.IsFinite(location.Facing0) || !float.IsFinite(location.Facing1)
                || !float.IsFinite(location.Facing2) || !float.IsFinite(location.Facing3)) return null;

            return gameTableManager.World?.GetEntry(location.WorldId);
        }

        // Called under the generation guard. Check funds before source removal and again at grid readiness.
        internal bool TryApplyTaxiAdmission(IMapPosition position, bool checkFare, bool setRotation = true)
        {
            if (pendingTeleport?.TaxiFlight is not { } flight)
                return true;
            if (flight.Admitted || !ReferenceEquals(pendingTeleport.MapPosition, position))
                return false;
            if (!OwnsTaxiFlight(flight))
            {
                CancelMapAdmission();
                return false;
            }
            if (checkFare && flight.Fare != 0 && !CurrencyManager.CanAfford(CurrencyType.Credits, flight.Fare))
            {
                OnTeleportToFailed(GenericError.VendorNotEnoughCash);
                return false;
            }
            // The destination can override the route yaw when it adjusts the arrival position.
            if (checkFare && setRotation) Rotation = flight.Rotation;
            return true;
        }

        internal bool TrySettleTaxiFare(IMapPosition position, ulong generation)
        {
            if (pendingTeleport?.TaxiFlight is not { } flight)
                return true;
            if (flight.Admitted || !ReferenceEquals(pendingTeleport.MapPosition, position)
                || mapAdmission.BootstrapGeneration != generation || Map == null || !OwnsTaxiFlight(flight))
                return false;

            var session = flight.Session;
            var events = session.Events;
            var wallet = CurrencyManager as CurrencyManager;
            if (flight.Fare != 0)
            {
                if (wallet != null)
                {
                    if (!wallet.TryDebitTaxiFare(flight.Fare)) return false;
                }
                else
                {
                    if (!CurrencyManager.CanAfford(CurrencyType.Credits, flight.Fare)) return false;
                    CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, flight.Fare);
                }
            }
            flight.Admitted = true;
            Interlocked.Increment(ref taxiFareVersion);
            MarkMapLocationDirty();
            bool learned = false;
            try { learned = TryLearnTaxiNode(flight.DestinationNode); }
            catch (Exception exception) { log.Warn(exception, "Failed to record taxi node discovery after fare settlement."); }
            if (flight.Fare != 0 && wallet != null)
                PublishSettledTaxiNotice(session, events, () =>
                {
                    if (ReferenceEquals(CurrencyManager, wallet)) wallet.PublishTaxiBalance(session);
                });
            if (learned)
                PublishSettledTaxiNotice(session, events, () => SendTaxiNodeUnlock(session, flight.DestinationNode));
            return true;
        }

        private void PublishSettledTaxiNotice(IGameSession session, EventQueue events, Action publish)
        {
            void Attempt()
            {
                lock (mapAdmission.SyncRoot)
                {
                    if (!IsAdmissionAllowed() || !ReferenceEquals(Session, session) || !ReferenceEquals(session.Events, events)
                        || (session is IPlayerSession bound && !ReferenceEquals(bound.Player, this))) return;
                    try { publish(); }
                    catch (Exception exception)
                    {
                        log.Warn(exception, "Failed to enqueue a settled taxi notice.");
                        events?.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), Attempt));
                    }
                }
            }
            Attempt();
        }

        private bool OwnsTaxiFlight(PendingTaxiFlight flight) => ReferenceEquals(Session, flight.Session)
            && (Session is not IPlayerSession bound || ReferenceEquals(bound.Player, this));

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
