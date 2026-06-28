using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NLog;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientSpellClientSideInteractionHandler : IMessageHandler<IWorldSession, ClientSpellClientSideInteraction>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private const byte CancelAction = 0;
        private const byte CompleteAction = 1;
        private const byte StartAction = 3;

        #region Dependency Injection

        private readonly IAssetManager assetManager;
        private readonly IGameTableManager gameTableManager;

        public ClientSpellClientSideInteractionHandler(
            IAssetManager assetManager,
            IGameTableManager gameTableManager = null)
        {
            this.assetManager     = assetManager;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientSpellClientSideInteraction clientSideInteraction)
        {
            uint requestedSpell4BaseId = NormaliseSpell4BaseId(clientSideInteraction.Spell4BaseIdPlusOne);
            if (clientSideInteraction.Action == StartAction)
            {
                log.Debug($"Client-side interaction spell callback started: player={session.Player?.Guid ?? 0u}, spellCastId={clientSideInteraction.SpellCastId}, action={clientSideInteraction.Action}, spell4BaseIdPlusOne={clientSideInteraction.Spell4BaseIdPlusOne}, spell4BaseId={requestedSpell4BaseId}, selectedTarget={session.Player?.TargetGuid ?? 0u}, world={session.Player?.Map?.Entry?.Id ?? 0u}.");
                return;
            }

            if (clientSideInteraction.Action is not CompleteAction and not CancelAction)
            {
                log.Debug($"Ignoring unknown client-side interaction spell callback action: player={session.Player?.Guid ?? 0u}, spellCastId={clientSideInteraction.SpellCastId}, action={clientSideInteraction.Action}, spell4BaseIdPlusOne={clientSideInteraction.Spell4BaseIdPlusOne}, spell4BaseId={requestedSpell4BaseId}, selectedTarget={session.Player?.TargetGuid ?? 0u}, world={session.Player?.Map?.Entry?.Id ?? 0u}.");
                return;
            }

            if (!PendingClientSideInteractionActivationStore.TryConsume(session, requestedSpell4BaseId, out PendingClientSideInteractionActivation pendingActivation))
            {
                log.Debug($"Ignoring client-side interaction without pending activation: player={session.Player?.Guid ?? 0u}, spellCastId={clientSideInteraction.SpellCastId}, action={clientSideInteraction.Action}, spell4BaseIdPlusOne={clientSideInteraction.Spell4BaseIdPlusOne}, spell4BaseId={requestedSpell4BaseId}, selectedTarget={session.Player?.TargetGuid ?? 0u}, world={session.Player?.Map?.Entry?.Id ?? 0u}.");
                return;
            }

            IWorldEntity entity = ResolvePendingActivationTarget(session, pendingActivation.EntityGuid);
            if (entity == null)
            {
                log.Warn($"Client-side interaction spell callback could not resolve pending activation target: player={session.Player?.Guid ?? 0u}, spellCastId={clientSideInteraction.SpellCastId}, action={clientSideInteraction.Action}, pendingEntity={pendingActivation.EntityGuid}, pendingCreature={pendingActivation.CreatureId}, clientSideInteractionId={pendingActivation.ClientSideInteractionId}, spell4BaseId={pendingActivation.Spell4BaseId}, selectedTarget={session.Player?.TargetGuid ?? 0u}, world={session.Player?.Map?.Entry?.Id ?? 0u}.");
                return;
            }

            bool wasCancelled = clientSideInteraction.Action == CancelAction;
            bool spellCompleted = session.Player?.TryCompleteClientSideInteractionSpell(clientSideInteraction.SpellCastId, wasCancelled) ?? false;
            if (!spellCompleted)
                log.Debug($"Client-side interaction spell callback did not find an active waiting spell to finish: player={session.Player?.Guid ?? 0u}, spellCastId={clientSideInteraction.SpellCastId}, action={clientSideInteraction.Action}, wasCancelled={wasCancelled}, spell4BaseId={requestedSpell4BaseId}, pendingEntity={pendingActivation.EntityGuid}, world={session.Player?.Map?.Entry?.Id ?? 0u}.");

            if (wasCancelled)
            {
                log.Debug($"Cancelling client-side interaction spell activation: player={session.Player?.Guid ?? 0u}, spellCastId={clientSideInteraction.SpellCastId}, pendingEntity={pendingActivation.EntityGuid}, resolvedEntity={entity.Guid}, creature={entity.CreatureId}, clientSideInteractionId={pendingActivation.ClientSideInteractionId}, spell4BaseId={pendingActivation.Spell4BaseId}, selectedTarget={session.Player?.TargetGuid ?? 0u}, world={session.Player?.Map?.Entry?.Id ?? 0u}.");
                entity.OnActivateFail(session.Player);
                return;
            }

            log.Debug($"Completing client-side interaction spell activation: player={session.Player?.Guid ?? 0u}, spellCastId={clientSideInteraction.SpellCastId}, pendingEntity={pendingActivation.EntityGuid}, resolvedEntity={entity.Guid}, creature={entity.CreatureId}, clientSideInteractionId={pendingActivation.ClientSideInteractionId}, spell4BaseId={pendingActivation.Spell4BaseId}, selectedTarget={session.Player?.TargetGuid ?? 0u}, world={session.Player?.Map?.Entry?.Id ?? 0u}.");
            ActivateCastCompletion.Complete(session, entity, assetManager, gameTableManager, pendingActivation.InvokeActivateCast);
        }

        private static IWorldEntity ResolvePendingActivationTarget(IWorldSession session, uint entityGuid)
        {
            IPlayer player = session?.Player;
            if (player == null || entityGuid == 0u)
                return null;

            IWorldEntity entity = player.GetVisible<IWorldEntity>(entityGuid);
            if (entity != null)
                return entity;

            entity = player.Map?.GetEntity<IWorldEntity>(entityGuid);
            return entity != null && player.CanSeeEntity(entity) ? entity : null;
        }

        private static uint NormaliseSpell4BaseId(uint spell4BaseIdPlusOne)
        {
            return spell4BaseIdPlusOne == 0u ? 0u : spell4BaseIdPlusOne - 1u;
        }
    }
}
