using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity;

public partial class Player
{
    internal void TeleportToMatch(IMatchTeamMember member, IMapEntrance entrance)
    {
        lock (mapAdmission.SyncRoot)
        {
            if (!IsAdmissionAllowed() || Map?.Entry is not { } sourceWorld)
                return;

            WorldEntry destinationWorld = gameTableManager.World.GetEntry(entrance.MapId)
                ?? throw new ArgumentException($"{entrance.MapId} is not a valid world id!");
            var source = new MapPosition
            {
                Info = new MapInfo { Entry = sourceWorld, MapLock = (Map as IMapInstance)?.MapLock },
                Position = Position
            };
            var destination = new MapPosition
            {
                Info = new MapInfo { Entry = destinationWorld },
                Position = entrance.Position
            };
            TeleportToCore(destination, TeleportReason.Relocate, arrivalRotation: entrance.Rotation,
                matchTransfer: new PendingMatchTransfer(member, source, Rotation));
        }
    }

    internal void RemoveFromMapForTransfer(IMapPosition destination)
    {
        if (pendingTeleport?.MatchTransfer is { } transfer
            && ReferenceEquals(pendingTeleport.MapPosition, destination))
        {
            if (Map is BaseMap source)
            {
                source.EnqueueAdmissionRemoval(this, mapAdmission.Generation);
                return;
            }
            transfer.SourceRemovalStarted = true;
        }
        RemoveFromMap();
    }

    internal void BeginMatchTransferSourceRemoval()
    {
        if (pendingTeleport?.MatchTransfer is { } transfer)
            transfer.SourceRemovalStarted = true;
    }

    internal void CancelMatchTransfer(IMatchTeamMember member)
    {
        lock (mapAdmission.SyncRoot)
        {
            if (!IsAdmissionAllowed() || !mapAdmission.Queued
                || pendingTeleport?.MatchTransfer is not { } transfer
                || !ReferenceEquals(transfer.Member, member))
                return;

            uint? vanityPetId = pendingTeleport.VanityPetId;
            pendingTeleport = null;
            mapAdmission.AbortQueue();
            Rotation = transfer.SourceRotation;
            if (!transfer.SourceRemovalStarted && Map != null)
            {
                IsLoading = false;
                SetControl(this);
                return;
            }

            // The source has started removing the player. Replace the destination with the source.
            TeleportToCore(transfer.Source, TeleportReason.Relocate, carriedVanityPetId: vanityPetId);
            if (pendingTeleport == null)
                OnTeleportToFailedCore(GenericError.InstanceInvalidDestination);
        }
    }
}
