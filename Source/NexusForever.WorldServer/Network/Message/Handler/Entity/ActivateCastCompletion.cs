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
            bool invokeActivateCast,
            PendingClientSideInteractionActivation pendingActivation = null)
        {
            IPlayer CompletionPlayer() => pendingActivation?.Player ?? session.Player;
            bool Current() => pendingActivation == null || pendingActivation.ResolveTarget(session) != null;
            if (!Current())
                return;
            if (invokeActivateCast)
                entity.OnActivateCast(CompletionPlayer());

            if (!Current())
                return;
            entity.OnActivateSuccess(CompletionPlayer());
            if (!Current())
                return;
            InteractionObjectiveUpdater.UpdateActivateSuccessObjectives(
                CompletionPlayer(),
                entity,
                assetManager,
                includeActivateEntity: ShouldIncludeActivateEntityForActivateCast(session, entity),
                gameTableManager);
            if (Current())
                ActivationAchievementUpdater.Update(CompletionPlayer(), entity);
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
