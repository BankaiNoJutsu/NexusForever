using System.Numerics;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
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

    private long mapLocationVersion;
    private readonly record struct SavedMapLocation(ulong Character, ushort World, ushort Zone,
        Vector3 Position, Vector3 Rotation, long Version, ulong Admission);

    private void MarkMapLocationDirty()
    {
        lock (mapAdmission.SyncRoot)
        {
            mapLocationVersion++;
            saveMask |= PlayerSaveMask.Location;
        }
    }

    private SavedMapLocation? CaptureMapLocationForSave(long? capturedTaxiFareVersion)
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

            return new(CharacterId, (ushort)world.Id, (ushort)zone.Id, Position, Rotation,
                mapLocationVersion, admission.Generation);
        }
        finally { Monitor.Exit(admission.SyncRoot); }
    }

    private void TrackMapLocationSave(CharacterContext context, SavedMapLocation location)
    {
        bool flushed = false;
        // SavedChanges precedes CharacterContext's implicit terminal settlement.
        // Read SQL through this transaction so a suppressed or altered write cannot
        // acknowledge the live location merely because some other fields flushed.
        bool MatchesSql() => !context.HasSavepointRollback && context.Character.AsNoTracking().Any(row =>
                row.Id == location.Character && row.WorldId == location.World && row.WorldZoneId == location.Zone
                && row.LocationX == location.Position.X && row.LocationY == location.Position.Y && row.LocationZ == location.Position.Z
                && row.RotationX == location.Rotation.X && row.RotationY == location.Rotation.Y && row.RotationZ == location.Rotation.Z);
        EventHandler<SavedChangesEventArgs> onFlushed = (_, _) => flushed = MatchesSql();
        context.SavedChanges += onFlushed;
        context.OnSaveValidation(phase =>
        {
            if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit && flushed)
                flushed = MatchesSql();
        });
        context.OnSaveSettled(committed =>
        {
            context.SavedChanges -= onFlushed;
            // Settlement must not wait for a map callback that is itself waiting
            // for persistence. A later save can safely acknowledge this location.
            if (!committed || !flushed || !Monitor.TryEnter(mapAdmission.SyncRoot)) return;
            try
            {
                if (CharacterId == location.Character && mapLocationVersion == location.Version
                    && mapAdmission.Generation == location.Admission && !mapAdmission.HasUnconfirmedLocation
                    && Map?.Entry?.Id == location.World && Zone?.Id == location.Zone
                    && Position == location.Position && Rotation == location.Rotation)
                    saveMask &= ~PlayerSaveMask.Location;
            }
            finally { Monitor.Exit(mapAdmission.SyncRoot); }
        });
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
        matchReturnAfterEntry = null;
    }
}
