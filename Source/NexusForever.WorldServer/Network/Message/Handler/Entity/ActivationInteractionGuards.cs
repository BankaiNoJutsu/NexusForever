using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    internal static class ActivationInteractionGuards
    {
        public static bool TryRejectBusyTarget(IWorldSession session, IWorldEntity entity)
        {
            if (entity is not IUnitEntity unitEntity || !unitEntity.IsBusy)
                return false;

            session.Player.SendGenericError(GenericError.TargetBusy);
            entity.OnActivateFail(session.Player);
            return true;
        }
    }
}