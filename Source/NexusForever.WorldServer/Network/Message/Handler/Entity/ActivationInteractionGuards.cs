using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    internal static class ActivationInteractionGuards
    {
        private const float DefaultInteractionMaxRange = 5f;

        public static bool TryRejectBusyTarget(IWorldSession session, IWorldEntity entity)
        {
            if (entity is not IUnitEntity unitEntity || !unitEntity.IsBusy)
                return false;

            session.Player.SendGenericError(GenericError.TargetBusy);
            entity.OnActivateFail(session.Player);
            return true;
        }

        public static bool TryRejectOutOfRangeTarget(IWorldSession session, IWorldEntity entity, GenericError? error = null)
        {
            var creatureEntry = GameTableManager.Instance.Creature2.GetEntry(entity.CreatureId);

            float minRange = creatureEntry?.ActivateSpellMinRange ?? 0f;
            float maxRange = creatureEntry?.ActivateSpellMaxRange > 0f
                ? creatureEntry.ActivateSpellMaxRange
                : DefaultInteractionMaxRange;

            float distanceSquared = Vector3.DistanceSquared(session.Player.Position, entity.Position);
            if ((minRange > 0f && distanceSquared < minRange * minRange) || distanceSquared > maxRange * maxRange)
            {
                if (error.HasValue)
                    session.Player.SendGenericError(error.Value);

                entity.OnActivateFail(session.Player);
                return true;
            }

            return false;
        }
    }
}