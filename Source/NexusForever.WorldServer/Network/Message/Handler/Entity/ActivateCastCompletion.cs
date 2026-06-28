using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    internal static class ActivateCastCompletion
    {
        private const ushort NorthernWildsWorldId = 426;
        private const uint Q3963ShipControlsCreatureId = 27196u;

        public static void Complete(
            IWorldSession session,
            IWorldEntity entity,
            IAssetManager assetManager,
            IGameTableManager gameTableManager,
            bool invokeActivateCast)
        {
            if (invokeActivateCast)
                entity.OnActivateCast(session.Player);

            entity.OnActivateSuccess(session.Player);
            InteractionObjectiveUpdater.UpdateActivateSuccessObjectives(
                session.Player,
                entity,
                assetManager,
                includeActivateEntity: ShouldIncludeActivateEntityForActivateCast(session, entity),
                gameTableManager);
            ActivationAchievementUpdater.Update(session.Player, entity);
        }

        private static bool ShouldIncludeActivateEntityForActivateCast(IWorldSession session, IWorldEntity entity)
        {
            if (entity?.CreatureId != Q3963ShipControlsCreatureId)
                return false;

            uint? worldId = entity.Map?.Entry?.Id;
            worldId ??= session?.Player?.Map?.Entry?.Id;

            return worldId == NorthernWildsWorldId;
        }
    }
}
