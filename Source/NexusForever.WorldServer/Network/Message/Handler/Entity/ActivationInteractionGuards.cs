using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    internal static class ActivationInteractionGuards
    {
        private const float DefaultInteractionMaxRange = 5f;

        public static bool TryRejectBusyTarget(IWorldSession session, IWorldEntity entity)
        {
            if (!entity.IsBusy)
                return false;

            session.Player.SendGenericError(GenericError.TargetBusy);
            entity.OnActivateFail(session.Player);
            return true;
        }

        public static bool TryRejectOutOfRangeTarget(
            IWorldSession session,
            IWorldEntity entity,
            GenericError? error = null,
            IGameTableManager gameTableManager = null)
        {
            Creature2Entry creatureEntry = entity.CreatureEntry ?? gameTableManager?.Creature2?.GetEntry(entity.CreatureId);

            float minRange = creatureEntry?.ActivateSpellMinRange ?? 0f;
            float maxRange = creatureEntry?.ActivateSpellMaxRange > 0f
                ? creatureEntry.ActivateSpellMaxRange
                : DefaultInteractionMaxRange;
            float interactionPadding = ResolveInteractionPadding(session.Player, entity, creatureEntry);

            float distanceSquared = Vector3.DistanceSquared(session.Player.Position, entity.Position);
            float minDistance = minRange + interactionPadding;
            float maxDistance = maxRange + interactionPadding;
            if ((minRange > 0f && distanceSquared < minDistance * minDistance) || distanceSquared > maxDistance * maxDistance)
            {
                if (error.HasValue)
                    session.Player.SendGenericError(error.Value);

                entity.OnActivateFail(session.Player);
                return true;
            }

            return false;
        }

        private static float ResolveInteractionPadding(IPlayer player, IWorldEntity entity, Creature2Entry creatureEntry)
        {
            return ResolvePlayerInteractionPadding(player) + ResolveTargetInteractionPadding(entity, creatureEntry);
        }

        private static float ResolvePlayerInteractionPadding(IPlayer player)
        {
            return player?.HitRadius > 0f
                ? player.HitRadius * 0.5f
                : 0f;
        }

        private static float ResolveTargetInteractionPadding(IWorldEntity entity, Creature2Entry creatureEntry)
        {
            if (entity is IUnitEntity unit && unit.HitRadius > 0f)
                return unit.HitRadius * 0.5f;

            Creature2DisplayInfoEntry displayEntry = entity.CreatureDisplayEntry;
            if (displayEntry?.HitRadius > 0f)
            {
                float scale = creatureEntry?.ModelScale > 0f
                    ? creatureEntry.ModelScale
                    : 1f;
                return displayEntry.HitRadius * scale * 0.5f;
            }

            return 0f;
        }
    }
}
