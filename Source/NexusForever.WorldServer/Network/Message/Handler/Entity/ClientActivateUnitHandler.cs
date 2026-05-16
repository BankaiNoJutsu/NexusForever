using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NLog;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientActivateUnitHandler : IMessageHandler<IWorldSession, ClientActivateUnit>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const uint TutorialHoverboardProjectorCreatureId = 73419u;
        private const uint TutorialHoverboardFinishCreatureId = 73735u;
        private const ushort TutorialWorldId = 3460;

        private readonly ITradeManager tradeManager;
        private readonly IAssetManager assetManager;

        public ClientActivateUnitHandler(
            ITradeManager tradeManager,
            IAssetManager assetManager)
        {
            this.tradeManager = tradeManager;
            this.assetManager = assetManager;
        }

        public void HandleMessage(IWorldSession session, ClientActivateUnit activateUnit)
        {
            IWorldEntity entity = session.Player.GetVisible<IWorldEntity>(activateUnit.ActivateUnitId);
            if (entity == null)
            {
                if (session.Player.Map?.Entry?.Id == TutorialWorldId)
                {
                    if (!TryRecoverTutorialActivationTarget(session, activateUnit.ActivateUnitId, out entity, nameof(ClientActivateUnit)))
                        return;
                }
                else
                    throw new InvalidPacketValueException();
            }

            if (IsTutorialHoverboardActivationEntity(entity))
                log.Debug($"Tutorial hoverboard activate attempt: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, busy={entity.IsBusy}.");

            if (ActivationInteractionGuards.TryRejectBusyTarget(session, entity))
            {
                if (IsTutorialHoverboardActivationEntity(entity))
                    log.Debug($"Tutorial hoverboard activate rejected busy: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                return;
            }

            if (ActivationInteractionGuards.TryRejectOutOfRangeTarget(session, entity))
            {
                if (IsTutorialHoverboardActivationEntity(entity))
                    log.Debug($"Tutorial hoverboard activate rejected range: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                return;
            }

            entity.OnActivate(session.Player);
            tradeManager.Cancel(session.Player);
            entity.OnActivateSuccess(session.Player);
            InteractionObjectiveUpdater.UpdateActivateSuccessObjectives(session.Player, entity, assetManager, includeActivateEntity: true);

            if (IsTutorialHoverboardActivationEntity(entity))
                log.Debug($"Tutorial hoverboard activate success: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
        }

        private static bool IsTutorialHoverboardActivationEntity(IWorldEntity entity)
        {
            return entity.CreatureId is TutorialHoverboardProjectorCreatureId or TutorialHoverboardFinishCreatureId;
        }

        private static bool TryRecoverTutorialActivationTarget(IWorldSession session, uint targetId, out IWorldEntity entity, string opcodeName)
        {
            entity = session.Player.Map?.GetEntity<IWorldEntity>(targetId);
            if (entity == null)
            {
                log.Debug($"Tutorial hoverboard {opcodeName} lookup failed: player={session.Player.Guid}, requestedEntity={targetId}, reason=not-on-map.");
                return false;
            }

            bool canSee = session.Player.CanSeeEntity(entity);
            if (IsTutorialHoverboardActivationEntity(entity) && canSee)
            {
                log.Debug($"Tutorial hoverboard {opcodeName} recovered stale visibility target: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                return true;
            }

            log.Debug($"Tutorial hoverboard {opcodeName} ignored stale target: player={session.Player.Guid}, requestedEntity={targetId}, mapEntity={entity.Guid}, creature={entity.CreatureId}, canSee={canSee}.");
            entity = null;
            return false;
        }
    }
}
