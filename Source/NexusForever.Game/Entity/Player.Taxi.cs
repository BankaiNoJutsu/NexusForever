using System.Numerics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Model;
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
        {
            ushort[] pending;
            lock (taxiStateLock)
                pending = unsavedTaxiNodes.ToArray();
            if (pending.Length == 0)
                return;
            foreach (ushort id in pending)
                context.Add(new CharacterTaxiNodeModel { Id = CharacterId, TaxiNodeId = id });
            context.SavedChanges += (_, _) =>
            {
                lock (taxiStateLock)
                    unsavedTaxiNodes.ExceptWith(pending);
            };
        }

        private void SendTaxiNodes()
        {
            var packet = new ServerFlightPathUpdate();
            lock (taxiStateLock)
                packet.FlightPathIds.AddRange(taxiNodes.OrderBy(id => id).Select(id => (uint)id));
            Session.EnqueueMessageEncrypted(packet);
        }
    }
}
