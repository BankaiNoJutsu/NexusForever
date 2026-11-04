using System.Numerics;
using System.Threading;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;

namespace NexusForever.Game.Entity;

public partial class Player : IPlayerMapAdmission
{
    private PlayerMapAdmissionState mapAdmissionState;
    private PlayerMapAdmissionState mapAdmission =>
        System.Threading.LazyInitializer.EnsureInitialized(ref mapAdmissionState);
    object IPlayerMapAdmission.MapAdmissionSyncRoot => mapAdmission.SyncRoot;
    ulong IPlayerMapAdmission.MapAdmissionGeneration
    {
        get { lock (mapAdmission.SyncRoot) return mapAdmission.Generation; }
    }

    ulong IPlayerMapAdmission.BeginMapAdmission()
    {
        lock (mapAdmission.SyncRoot)
        {
            if (!IsAdmissionAllowed())
                return mapAdmission.Generation;
            IsLoading = true;
            return mapAdmission.Queue();
        }
    }

    bool IPlayerMapAdmission.IsMapAdmissionCurrent(ulong generation)
    {
        lock (mapAdmission.SyncRoot)
            return IsAdmissionAllowed() && mapAdmission.IsCurrent(generation);
    }

    void IPlayerMapAdmission.CompleteMapAdmission(ulong generation, bool successful)
    {
        lock (mapAdmission.SyncRoot)
        {
            if (mapAdmission.FinishBootstrap(generation, successful && IsAdmissionAllowed()))
                CompleteEnteredWorld(generation);
        }
    }

    internal void FailMapBootstrap(ulong generation, IGameSession originalSession)
    {
        lock (mapAdmission.SyncRoot)
        {
            if (!mapAdmission.IsCurrent(generation))
                return;

            CancelMapAdmission();
            IsLoading = false;
            if (ReferenceEquals(Session, originalSession)
                && (originalSession is not IPlayerSession bound || ReferenceEquals(bound.Player, this)))
                originalSession?.ForceDisconnect();
        }
    }

    private bool IsAdmissionAllowed() => !mapAdmission.Canceled
        && LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished)
        && Session is not NetworkSession { IsDisconnecting: true };

    private (ushort World, ushort Zone, Vector3 Position, Vector3 Rotation)? CaptureMapLocationForSave(long? capturedTaxiFareVersion)
    {
        var admission = mapAdmission;
        // A Character transaction must not wait on map callbacks that can schedule persistence.
        if (!Monitor.TryEnter(admission.SyncRoot))
            return null;
        try
        {
            if (admission.HasUnconfirmedLocation || (capturedTaxiFareVersion.HasValue && capturedTaxiFareVersion.Value != TaxiFareVersion)
                || Map?.Entry is not { } world || Zone is not { } zone)
                return null;

            return ((ushort)world.Id, (ushort)zone.Id, Position, Rotation);
        }
        finally { Monitor.Exit(admission.SyncRoot); }
    }

    public override void OnEnqueueAddToMap()
    {
        lock (mapAdmission.SyncRoot)
        {
            if (IsAdmissionAllowed() && !mapAdmission.Queued)
            {
                IsLoading = true;
                mapAdmission.Queue();
            }
        }
    }

    private void CancelMapAdmission()
    {
        ClearPetMiniBars(sendPackets: false);
        mapAdmission.Cancel();
        pendingTeleport = null;
    }
}
