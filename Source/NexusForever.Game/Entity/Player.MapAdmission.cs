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

    private bool IsAdmissionAllowed() => !mapAdmission.Canceled
        && LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished)
        && Session is not NetworkSession { IsDisconnecting: true };

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
