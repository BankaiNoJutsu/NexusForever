using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Game.Spell;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.GameTable;
using NLog;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientActivateUnitHandler : IMessageHandler<IWorldSession, ClientActivateUnit>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const uint TutorialHoverboardProjectorCreatureId = 73419u;
        private const uint TutorialHoverboardFinishCreatureId = 73735u;
        private const uint TutorialHousingProjectorCreatureId = 73741u;
        private const uint TutorialHoverboardMountSpellId = 85562u;
        private const ushort TutorialWorldId = 3460;

        private readonly ITradeManager tradeManager;
        private readonly IAssetManager assetManager;
        private readonly IGameTableManager gameTableManager;

        public ClientActivateUnitHandler(
            ITradeManager tradeManager,
            IAssetManager assetManager,
            IGameTableManager gameTableManager = null)
        {
            this.tradeManager = tradeManager;
            this.assetManager = assetManager;
            this.gameTableManager = gameTableManager;
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

            if (IsTutorialSpecialActivationEntity(entity))
                log.Debug($"Tutorial activate attempt: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, busy={entity.IsBusy}.");

            if (ActivateUnitCombatHelper.TryHandleHostileActivation(session, entity))
            {
                tradeManager.Cancel(session.Player);
                return;
            }

            if (ActivationInteractionGuards.TryRejectBusyTarget(session, entity))
            {
                if (IsTutorialSpecialActivationEntity(entity))
                    log.Debug($"Tutorial activate rejected busy: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                return;
            }

            if (ActivationInteractionGuards.TryRejectOutOfRangeTarget(session, entity, gameTableManager: gameTableManager))
            {
                if (IsTutorialSpecialActivationEntity(entity))
                    log.Debug($"Tutorial activate rejected range: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                return;
            }

            if (!TryCastTutorialHoverboardMount(session, entity, nameof(ClientActivateUnit)))
            {
                entity.OnActivateFail(session.Player);
                return;
            }

            entity.OnActivate(session.Player);
            tradeManager.Cancel(session.Player);
            entity.OnActivateSuccess(session.Player);
            InteractionObjectiveUpdater.UpdateActivateSuccessObjectives(session.Player, entity, assetManager, includeActivateEntity: true, gameTableManager);
            ActivationAchievementUpdater.Update(session.Player, entity);

            if (IsTutorialSpecialActivationEntity(entity))
                log.Debug($"Tutorial activate success: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
        }

        private static bool IsTutorialHoverboardActivationEntity(IWorldEntity entity)
        {
            return entity.CreatureId is TutorialHoverboardProjectorCreatureId or TutorialHoverboardFinishCreatureId;
        }

        private static bool IsTutorialHousingProjectorEntity(IWorldEntity entity)
        {
            return entity.CreatureId == TutorialHousingProjectorCreatureId;
        }

        private static bool IsTutorialSpecialActivationEntity(IWorldEntity entity)
        {
            return IsTutorialHoverboardActivationEntity(entity) || IsTutorialHousingProjectorEntity(entity);
        }

        private static bool TryCastTutorialHoverboardMount(IWorldSession session, IWorldEntity entity, string clientRequestSource)
        {
            if (entity.CreatureId != TutorialHoverboardProjectorCreatureId)
                return true;

            var mountSpellParameters = new SpellParameters
            {
                PrimaryTargetId        = session.Player.Guid,
                UserInitiatedSpellCast = false,
                IgnoreGlobalCooldown   = true,
                CancelActiveTrade      = true,
                ClientRequestSource    = clientRequestSource
            };

            var castResult = session.Player.TryCastSpell(TutorialHoverboardMountSpellId, mountSpellParameters);
            log.Debug($"Tutorial hoverboard direct activate mount cast: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, castResult={castResult}.");
            return castResult == CastResult.Ok;
        }

        private static bool TryRecoverTutorialActivationTarget(IWorldSession session, uint targetId, out IWorldEntity entity, string opcodeName)
        {
            entity = session.Player.Map?.GetEntity<IWorldEntity>(targetId);
            if (entity == null)
            {
                log.Debug($"Tutorial activate {opcodeName} lookup failed: player={session.Player.Guid}, requestedEntity={targetId}, reason=not-on-map.");
                return false;
            }

            bool canSee = session.Player.CanSeeEntity(entity);
            if (IsTutorialSpecialActivationEntity(entity) && canSee)
            {
                log.Debug($"Tutorial activate {opcodeName} recovered stale visibility target: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                return true;
            }

            log.Debug($"Tutorial activate {opcodeName} ignored stale target: player={session.Player.Guid}, requestedEntity={targetId}, mapEntity={entity.Guid}, creature={entity.CreatureId}, canSee={canSee}.");
            entity = null;
            return false;
        }
    }
}
