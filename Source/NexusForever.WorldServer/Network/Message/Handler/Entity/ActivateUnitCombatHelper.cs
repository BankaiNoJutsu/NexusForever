using NexusForever.Game.Abstract.Entity;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    internal static class ActivateUnitCombatHelper
    {
        private const uint ActivationThreat = 1u;

        public static bool TryHandleHostileActivation(IWorldSession session, IWorldEntity entity)
        {
            if (entity is not IUnitEntity target)
                return false;

            if (!session.Player.CanAttack(target))
                return false;

            session.Player.SetTarget(target, ActivationThreat);
            target.ThreatManager.UpdateThreat(session.Player, (int)ActivationThreat);
            return true;
        }
    }
}