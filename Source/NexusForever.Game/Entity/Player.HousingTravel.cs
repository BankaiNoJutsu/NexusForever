using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private object housingTeleportOwner;
        private IMapPosition housingTeleportPosition;
        internal bool OwnsHousingTeleport(object owner)
        {
            lock (mapAdmission.SyncRoot) return ReferenceEquals(housingTeleportOwner, owner) && pendingTeleport != null
                && ReferenceEquals(pendingTeleport.MapPosition, housingTeleportPosition);
        }
        internal void TeleportHousing(object owner, IMapPosition position)
        {
            lock (mapAdmission.SyncRoot)
            {
                if (!IsAdmissionAllowed()) return;
                if (ReferenceEquals(housingTeleportOwner, owner) && pendingTeleport != null
                    && ReferenceEquals(pendingTeleport.MapPosition, housingTeleportPosition))
                {
                    // An earlier AddToMap throw left this exact accepted intent
                    // pending. Re-submit it without creating a second teleport.
                    if (mapManager == null) throw new InvalidOperationException("Player requires an IMapManager to retry housing travel.");
                    mapManager.AddToMap(this, housingTeleportPosition);
                    return;
                }
                housingTeleportOwner = owner; housingTeleportPosition = position;
                TeleportToCore(position, TeleportReason.Relocate);
            }
        }
        internal void FinishHousingTeleport(object owner)
        {
            lock (mapAdmission.SyncRoot)
                if (ReferenceEquals(housingTeleportOwner, owner)) { housingTeleportOwner = null; housingTeleportPosition = null; }
        }
    }
}
