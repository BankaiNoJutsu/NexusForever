using System.Collections;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NLog;

namespace NexusForever.Game.Combat
{
    public class ThreatManager : IThreatManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<uint, HostileLink> hostiles = new();
        private readonly IUnitEntity owner;

        // Numeric IDs remain the protocol lookup keys. The retained actors,
        // managers and reciprocal entry identify the relation being retired.
        private sealed class HostileLink
        {
            internal ThreatManager Manager { get; }
            internal IUnitEntity Target { get; }
            internal IThreatManager PeerManager { get; }
            internal uint OwnerId { get; }
            internal uint TargetId { get; }
            internal IBaseMap OwnerMap { get; }
            internal IBaseMap TargetMap { get; }
            internal IHostileEntity Hostile { get; }
            internal HostileLink Peer { get; set; }
            internal IHostileEntity ExternalPeer { get; set; }

            internal HostileLink(ThreatManager manager, IUnitEntity target)
            {
                Manager = manager; Target = target; PeerManager = target.ThreatManager;
                OwnerId = manager.owner.Guid; TargetId = target.Guid;
                OwnerMap = manager.owner.Map; TargetMap = target.Map;
                Hostile = new HostileEntity(TargetId);
            }

            internal bool CurrentOwner => ReferenceEquals(Manager.owner.ThreatManager, Manager)
                && Manager.owner.Guid == OwnerId && ReferenceEquals(Manager.owner.Map, OwnerMap);

            internal bool CurrentActors => CurrentOwner && PeerManager != null
                && ReferenceEquals(Target.ThreatManager, PeerManager) && Target.Guid == TargetId
                && ReferenceEquals(Target.Map, TargetMap);
        }

        /// <summary>
        /// Initialise <see cref="IThreatManager"/> for a <see cref="IUnitEntity"/>.
        /// </summary>
        public ThreatManager(IUnitEntity owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// Returns if any <see cref="IHostileEntity"/> exists in the threat list.
        /// </summary>
        public bool IsThreatened => hostiles.Count > 0;

        /// <summary>
        /// Returns <see cref="IHostileEntity"/> for supplied target if it exists in the threat list.
        /// </summary>
        public IHostileEntity GetHostile(uint target)
            => hostiles.TryGetValue(target, out HostileLink link) ? link.Hostile : null;

        /// <summary>
        /// Return the <see cref="IHostileEntity"/> with the highest threat in the threat list.
        /// </summary>
        public IHostileEntity GetTopHostile()
        {
            IHostileEntity topHostile = null;
            foreach (HostileLink link in hostiles.Values)
                if (topHostile == null || link.Hostile.Threat > topHostile.Threat)
                    topHostile = link.Hostile;
            return topHostile;
        }

        private bool Owns(HostileLink link)
            => hostiles.TryGetValue(link.TargetId, out HostileLink current) && ReferenceEquals(current, link);

        private HostileLink GetOrPrepareLink(IUnitEntity target)
        {
            if (target == null || target.Guid == owner.Guid || !ReferenceEquals(owner.ThreatManager, this))
                return null;
            hostiles.TryGetValue(target.Guid, out HostileLink current);
            if (current?.CurrentActors == true && ReferenceEquals(current.Target, target))
                return current;

            var prepared = new HostileLink(this, target);
            if (current != null) RemoveHostile(current);
            // A cleanup callback may have installed another relation or moved
            // either actor. That new work is not owned by this older request.
            return prepared.CurrentActors && !hostiles.ContainsKey(prepared.TargetId) ? prepared : null;
        }

        /// <summary>
        /// Add threat for the provided <see cref="IUnitEntity"/>.
        /// </summary>
        public void UpdateThreat(IUnitEntity target, int threat)
        {
            HostileLink link = GetOrPrepareLink(target);
            if (link == null) return;
            if (Owns(link)) UpdateThreat(link, threat);
            else CreateHostile(link, threat);
        }

        public void SetThreat(IUnitEntity target, uint threat)
        {
            HostileLink link = GetOrPrepareLink(target);
            if (link == null) return;
            uint currentThreat = Owns(link) ? link.Hostile.Threat : 0u;
            if (currentThreat == threat) return;
            if (threat == 0u) { RemoveHostile(link); return; }

            int delta = threat > currentThreat
                ? (int)Math.Min(int.MaxValue, threat - currentThreat)
                : -(int)Math.Min(int.MaxValue, currentThreat - threat);
            if (Owns(link)) UpdateThreat(link, delta);
            else CreateHostile(link, delta);
        }

        private void BindReciprocal(HostileLink link)
        {
            if (!Owns(link) || !link.CurrentActors) return;
            if (link.PeerManager is ThreatManager peerManager)
            {
                if (!ReferenceEquals(peerManager.owner, link.Target)
                    || !peerManager.hostiles.TryGetValue(link.OwnerId, out HostileLink peer)
                    || !peer.CurrentActors || !ReferenceEquals(peer.Target, owner)
                    || !ReferenceEquals(peer.PeerManager, this) || peer.OwnerId != link.TargetId)
                    return;
                link.Peer = peer;
                peer.Peer = link;
            }
            else link.ExternalPeer = link.PeerManager.GetHostile(link.OwnerId);
        }

        private void CreateHostile(HostileLink link, int threat)
        {
            link.Hostile.UpdateThreat(threat);
            hostiles.Add(link.TargetId, link);
            if (!TryNotify(() => BindReciprocal(link), "reciprocal bind"))
            {
                RemoveHostile(link);
                return;
            }
            if (!Owns(link)) return;
            if (!link.CurrentActors) { RemoveHostile(link); return; }
            if (!TryNotify(() => owner.OnThreatAddTarget(link.Hostile), "add"))
            {
                RemoveHostile(link);
                return;
            }
            if (!Owns(link)) return;
            if (!link.CurrentActors) { RemoveHostile(link); return; }

            try
            {
                if (link.Peer == null && link.ExternalPeer == null)
                    link.PeerManager.UpdateThreat(owner, 1);
                BindReciprocal(link);
            }
            catch (Exception error)
            {
                log.Warn(error, "Reciprocal threat admission failed; retiring the original relation.");
                RemoveHostile(link);
                return;
            }
            if (!Owns(link)) return;
            if (!link.CurrentActors || link.Peer == null && link.ExternalPeer == null)
                RemoveHostile(link);
        }

        private void UpdateThreat(HostileLink link, int threat)
        {
            link.Hostile.UpdateThreat(threat);
            if (link.Hostile.Threat == 0u) RemoveHostile(link);
            else
            {
                TryNotify(() => owner.OnThreatChange(link.Hostile), "change");
                if (Owns(link) && !link.CurrentActors) RemoveHostile(link);
            }
        }

        /// <summary>
        /// Clear the threat list and alert all entities that this <see cref="IUnitEntity"/> has forgotten them.
        /// </summary>
        public void ClearThreatList()
        {
            foreach (HostileLink link in hostiles.Values.ToArray())
                RemoveHostile(link);
        }

        /// <summary>
        /// Remove the target with the given unit id from this threat list, if they exist. This will alert the entity that they have been forgotten by this <see cref="IUnitEntity"/>.
        /// </summary>
        public void RemoveHostile(uint unitId)
        {
            if (hostiles.TryGetValue(unitId, out HostileLink link))
                RemoveHostile(link);
        }

        private void RemoveHostile(HostileLink link)
        {
            if (!Owns(link)) return;
            hostiles.Remove(link.TargetId);

            // Retire both original entries before invoking callbacks. A callback
            // may create a replacement with the same IDs; never remove that entry.
            HostileLink peer = link.Peer;
            link.Peer = null;
            if (peer != null)
            {
                if (ReferenceEquals(peer.Peer, link)) peer.Peer = null;
                peer.Manager.RemoveHostile(peer);
            }
            else if (link.ExternalPeer != null)
            {
                TryNotify(() =>
                {
                    if (ReferenceEquals(link.PeerManager.GetHostile(link.OwnerId), link.ExternalPeer))
                        link.PeerManager.RemoveHostile(link.OwnerId);
                }, "reciprocal remove");
            }

            if (!link.CurrentOwner) return;
            if (!hostiles.ContainsKey(link.TargetId))
                TryNotify(() => owner.EnqueueToVisible(new ServerEntityThreatUpdate
                {
                    UnitId = link.OwnerId, TargetId = link.TargetId, ThreatLevel = 0
                }), "remove publication");
            if (link.CurrentOwner)
                TryNotify(() => owner.OnThreatRemoveTarget(link.Hostile), "remove");
        }

        private static bool TryNotify(Action notify, string operation)
        {
            try { notify(); return true; }
            catch (Exception error)
            {
                log.Warn(error, "Threat {0} notification failed; preserving relation cleanup.", operation);
                return false;
            }
        }

        /// <summary>
        /// Broadcast threat list.
        /// </summary>
        /// <remarks>
        /// Threat list will be sent to all <see cref="IPlayer"/>s targeting owner <see cref="IUnitEntity"/>.
        /// </remarks>
        public void BroadcastThreatList()
        {
            ServerEntityThreatListUpdate message = BuildServerThreatListUpdate();
            foreach (uint guid in owner.TargetingGuids)
            {
                IUnitEntity unit = owner.GetVisible<IUnitEntity>(guid);
                if (unit is not IPlayer player)
                    continue;

                player.Session.EnqueueMessageEncrypted(message);
            }
        }

        /// <summary>
        /// Send threat list to supplied <see cref="IGameSession"/>.
        /// </summary>
        public void SendThreatList(IGameSession session)
        {
            session.EnqueueMessageEncrypted(BuildServerThreatListUpdate());
        }

        private ServerEntityThreatListUpdate BuildServerThreatListUpdate()
        {
            var threatUpdate = new ServerEntityThreatListUpdate
            {
                SrcUnitId = owner.Guid,
            };

            // The client threat HUD promotes the owner's current target to the first
            // visible slot from this five-entry packet, so keep that target in-band
            // whenever it already exists on the threat list.
            List<IHostileEntity> hostileEntities = [];
            uint? prioritisedHostileId = null;
            if (owner.TargetGuid.HasValue && hostiles.TryGetValue(owner.TargetGuid.Value, out HostileLink currentTarget))
            {
                prioritisedHostileId = currentTarget.TargetId;
                hostileEntities.Add(currentTarget.Hostile);
            }

            foreach (IHostileEntity hostile in hostiles.Values.Select(link => link.Hostile))
            {
                if (prioritisedHostileId.HasValue && hostile.HatedUnitId == prioritisedHostileId.Value)
                    continue;

                InsertThreatSorted(hostileEntities, hostile, prioritisedHostileId.HasValue ? 1 : 0);
            }

            for (int i = 0; i < hostileEntities.Count; i++)
            {
                threatUpdate.ThreatUnitIds[i] = hostileEntities[i].HatedUnitId;
                threatUpdate.ThreatLevels[i] = hostileEntities[i].Threat;
            }

            return threatUpdate;
        }

        private static void InsertThreatSorted(List<IHostileEntity> hostileEntities, IHostileEntity hostile, int minimumIndex)
        {
            int insertIndex = hostileEntities.Count;
            while (insertIndex > minimumIndex && hostile.Threat > hostileEntities[insertIndex - 1].Threat)
                insertIndex--;

            if (insertIndex >= 5)
                return;

            hostileEntities.Insert(insertIndex, hostile);
            if (hostileEntities.Count > 5)
                hostileEntities.RemoveAt(5);
        }

        public IEnumerator<IHostileEntity> GetEnumerator()
        {
            return hostiles.Values.Select(link => link.Hostile).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
