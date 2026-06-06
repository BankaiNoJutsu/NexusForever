using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Spell;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network.Message.Handler.Spell;
using NLog;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientActivateUnitCastHandler : IMessageHandler<IWorldSession, ClientActivateUnitCast>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const uint TutorialHoverboardProjectorCreatureId = 73419u;
        private const uint TutorialHoverboardFinishCreatureId = 73735u;
        private const uint TutorialHoverboardMountSpellId = 85562u;
        private const uint TutorialCombatMineEasyCreatureId = 73463u;
        private const uint TutorialCombatMineMediumCreatureId = 73667u;
        private const uint TutorialCombatMineHardCreatureId = 73668u;
        private const ushort TutorialWorldId = 3460;

        #region Dependency Injection

        private readonly IPrerequisiteManager prerequisiteManager;
        private readonly IAssetManager assetManager;

        public ClientActivateUnitCastHandler(
            IPrerequisiteManager prerequisiteManager,
            IAssetManager assetManager)
        {
            this.prerequisiteManager = prerequisiteManager;
            this.assetManager        = assetManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientActivateUnitCast activateUnitCast)
        {
            HandleMessageInternal(session, activateUnitCast.ActivateUnitId, activateUnitCast.ContextToken, nameof(ClientActivateUnitCast));
        }

        internal void HandleMessageInternal(IWorldSession session, uint activateUnitId, uint contextToken, string clientRequestSource)
        {
            IWorldEntity entity = session.Player.GetVisible<IWorldEntity>(activateUnitId);
            if (entity == null)
            {
                IWorldEntity mapEntity = session.Player.Map?.GetEntity<IWorldEntity>(activateUnitId);
                if (mapEntity != null && IsTutorialHoverboardActivationEntity(mapEntity) && session.Player.CanSeeEntity(mapEntity))
                {
                    entity = mapEntity;
                    log.Debug($"Tutorial hoverboard activate-cast recovered stale visibility target: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                }
                else if (session.Player.Map?.Entry?.Id == TutorialWorldId)
                {
                    log.Debug($"Tutorial hoverboard activate-cast ignored stale target: player={session.Player.Guid}, requestedEntity={activateUnitId}, mapEntity={mapEntity?.Guid ?? 0u}, creature={mapEntity?.CreatureId ?? 0u}.");
                    return;
                }
                else
                {
                    throw new InvalidPacketValueException();
                }
            }

            if (IsTutorialHoverboardActivationEntity(entity))
                log.Debug($"Tutorial hoverboard activate-cast attempt: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, busy={entity.IsBusy}.");

            if (ActivateUnitCombatHelper.TryHandleHostileActivation(session, entity))
                return;

            if (ActivationInteractionGuards.TryRejectBusyTarget(session, entity))
            {
                if (IsTutorialHoverboardActivationEntity(entity))
                    log.Debug($"Tutorial hoverboard activate-cast rejected busy: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                return;
            }

            if (ActivationInteractionGuards.TryRejectOutOfRangeTarget(session, entity))
            {
                if (IsTutorialHoverboardActivationEntity(entity))
                    log.Debug($"Tutorial hoverboard activate-cast rejected range: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                return;
            }

            if (!TryResolveActivateSpell(entity, session.Player, out uint spell4Id))
            {
                if (IsTutorialHoverboardActivationEntity(entity))
                    log.Debug($"Tutorial hoverboard activate-cast resolve failed: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");

                if (TryCompleteTutorialHoverboardProjectorActivationWithoutActivateSpell(session, entity, 0u, CastResult.NoValidActivateSpell, clientRequestSource))
                    return;

                if (TryCompleteTutorialMineActivationWithoutSpell(session, entity, 0u, CastResult.NoValidActivateSpell))
                    return;

                log.Warn($"Unhandled activate-cast request: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, entityId={entity.EntityId}, world={session.Player.Map?.Entry?.Id ?? 0u}, contextToken={contextToken}, source={clientRequestSource}, reason=no-valid-activate-spell.");
                SendSpellCastResult(session, GetFallbackActivateSpellId(entity), CastResult.NoValidActivateSpell);
                entity.OnActivateFail(session.Player);
                return;
            }

            if (IsTutorialHoverboardActivationEntity(entity))
                log.Debug($"Tutorial hoverboard activate-cast resolved spell: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}.");

            var spellParameters = new SpellParameters
            {
                PrimaryTargetId        = entity.Guid,
                UserInitiatedSpellCast = false,
                IgnoreGlobalCooldown   = true,
                CancelActiveTrade      = true,
                ClientContextToken     = contextToken,
                ClientRequestSource    = clientRequestSource
            };

            ClientSpellEvidenceCaptureHelper.ApplyPendingCapture(session, spellParameters);
            CastResult castResult = session.Player.TryCastSpell(spell4Id, spellParameters);

            if (castResult != CastResult.Ok)
            {
                if (IsTutorialHoverboardActivationEntity(entity))
                    log.Debug($"Tutorial hoverboard activate-cast failed: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}, castResult={castResult}.");

                if (TryCompleteTutorialHoverboardProjectorActivationWithoutActivateSpell(session, entity, spell4Id, castResult, clientRequestSource))
                    return;

                if (TryCompleteTutorialMineActivationWithoutSpell(session, entity, spell4Id, castResult))
                    return;

                entity.OnActivateFail(session.Player);
                return;
            }

            CompleteActivation(session, entity, invokeActivateCast: true);

            if (IsTutorialHoverboardActivationEntity(entity))
                log.Debug($"Tutorial hoverboard activate-cast success: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}.");
        }

        private void CompleteActivation(IWorldSession session, IWorldEntity entity, bool invokeActivateCast)
        {
            if (invokeActivateCast)
                entity.OnActivateCast(session.Player);

            entity.OnActivateSuccess(session.Player);
            InteractionObjectiveUpdater.UpdateActivateSuccessObjectives(session.Player, entity, assetManager, includeActivateEntity: false);
            ActivationAchievementUpdater.Update(session.Player, entity);
        }

        private bool TryCompleteTutorialMineActivationWithoutSpell(IWorldSession session, IWorldEntity entity, uint spell4Id, CastResult castResult)
        {
            if (!IsTutorialCombatMineEntity(entity))
                return false;

            log.Debug($"Tutorial combat mine activate-cast bypassed blocked activate spell: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}, castResult={castResult}.");
            CompleteActivation(session, entity, invokeActivateCast: false);
            return true;
        }

        private bool TryCompleteTutorialHoverboardProjectorActivationWithoutActivateSpell(IWorldSession session, IWorldEntity entity, uint spell4Id, CastResult castResult, string clientRequestSource)
        {
            if (entity.CreatureId != TutorialHoverboardProjectorCreatureId)
                return false;

            CastResult mountCastResult = TryCastTutorialHoverboardMount(session, clientRequestSource);
            log.Debug($"Tutorial hoverboard projector activate-cast bypassed blocked activate spell: player={session.Player.Guid}, entity={entity.Guid}, spell4Id={spell4Id}, castResult={castResult}, mountCastResult={mountCastResult}.");
            if (mountCastResult != CastResult.Ok)
            {
                entity.OnActivateFail(session.Player);
                return true;
            }

            CompleteActivation(session, entity, invokeActivateCast: false);
            return true;
        }

        private static CastResult TryCastTutorialHoverboardMount(IWorldSession session, string clientRequestSource)
        {
            var mountSpellParameters = new SpellParameters
            {
                PrimaryTargetId        = session.Player.Guid,
                UserInitiatedSpellCast = false,
                IgnoreGlobalCooldown   = true,
                CancelActiveTrade      = true,
                ClientRequestSource    = clientRequestSource
            };

            return session.Player.TryCastSpell(TutorialHoverboardMountSpellId, mountSpellParameters);
        }

        private bool TryResolveActivateSpell(IWorldEntity entity, IPlayer player, out uint spell4Id)
        {
            spell4Id = 0u;

            uint[] spellIds =
            [
                entity.CreatureEntry?.Spell4IdActivate00 ?? 0u,
                entity.CreatureEntry?.Spell4IdActivate01 ?? 0u,
                entity.CreatureEntry?.Spell4IdActivate02 ?? 0u,
                entity.CreatureEntry?.Spell4IdActivate03 ?? 0u
            ];

            uint[] prerequisiteIds =
            [
                entity.CreatureEntry?.PrerequisiteIdActivateSpell00 ?? 0u,
                entity.CreatureEntry?.PrerequisiteIdActivateSpell01 ?? 0u,
                entity.CreatureEntry?.PrerequisiteIdActivateSpell02 ?? 0u,
                entity.CreatureEntry?.PrerequisiteIdActivateSpell03 ?? 0u
            ];

            for (int index = 0; index < spellIds.Length; index++)
            {
                uint candidateSpellId = spellIds[index];
                if (candidateSpellId == 0u)
                    continue;

                uint prerequisiteId = prerequisiteIds[index];
                if (prerequisiteId != 0u && !prerequisiteManager.Meets(player, prerequisiteId))
                    continue;

                spell4Id = candidateSpellId;
                return true;
            }

            return false;
        }

        private static uint GetFallbackActivateSpellId(IWorldEntity entity)
        {
            return entity.CreatureEntry?.Spell4IdActivate00
                ?? entity.CreatureEntry?.Spell4IdActivate01
                ?? entity.CreatureEntry?.Spell4IdActivate02
                ?? entity.CreatureEntry?.Spell4IdActivate03
                ?? 0u;
        }

        private static void SendSpellCastResult(IWorldSession session, uint spell4Id, CastResult castResult)
        {
            session.EnqueueMessageEncrypted(new ServerSpellCastResult
            {
                Spell4Id   = spell4Id,
                CastResult = castResult
            });
        }

        private static bool IsTutorialHoverboardActivationEntity(IWorldEntity entity)
        {
            return entity.CreatureId is TutorialHoverboardProjectorCreatureId or TutorialHoverboardFinishCreatureId;
        }

        private static bool IsTutorialCombatMineEntity(IWorldEntity entity)
        {
            return entity.CreatureId is TutorialCombatMineEasyCreatureId or TutorialCombatMineMediumCreatureId or TutorialCombatMineHardCreatureId;
        }
    }
}
