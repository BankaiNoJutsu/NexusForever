using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;

namespace NexusForever.Game.Map;

internal static class PlayerMapAdmissionGuard
{
    public static bool IsCurrent(IGridEntity entity, ulong? generation)
    {
        if (entity is not IPlayer player)
            return true;
        if (player.LogoutManager?.State is LogoutState.Logout or LogoutState.Finished
            || player.Session is NetworkSession { IsDisconnecting: true })
            return false;
        return player is not IPlayerMapAdmission admission
            || generation.HasValue && admission.IsMapAdmissionCurrent(generation.Value);
    }

    public static ulong? Capture(IGridEntity entity) =>
        (entity as IPlayerMapAdmission)?.MapAdmissionGeneration;

    public static bool Run(IGridEntity entity, ulong? generation, Action action)
    {
        if (entity is IPlayerMapAdmission admission)
        {
            lock (admission.MapAdmissionSyncRoot)
            {
                if (!IsCurrent(entity, generation))
                    return false;
                action();
                return true;
            }
        }
        if (!IsCurrent(entity, generation))
            return false;
        action();
        return true;
    }

    public static bool IsCurrent(IGridAction action) => action switch
    {
        GridActionAdd add => IsCurrent(add.Entity, add.AdmissionGeneration)
            && add.Entity is not IPlayer { Map: not null },
        GridActionPending pending => IsCurrent(pending.Entity, pending.AdmissionGeneration),
        _ => true
    };
}
