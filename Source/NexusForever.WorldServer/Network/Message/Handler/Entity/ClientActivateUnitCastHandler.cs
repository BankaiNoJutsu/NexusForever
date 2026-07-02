using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Quest;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.WorldServer.Network.Message.Handler.Spell;
using NLog;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientActivateUnitCastHandler : IMessageHandler<IWorldSession, ClientActivateUnitCast>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const uint TutorialHoverboardProjectorCreatureId = 73419u;
        private const uint TutorialHoverboardFinishCreatureId = 73735u;
        private const uint TutorialHousingProjectorCreatureId = 73741u;
        private const uint TutorialHoverboardMountSpellId = 85562u;
        private const uint TutorialCombatMineEasyCreatureId = 73463u;
        private const uint TutorialCombatMineMediumCreatureId = 73667u;
        private const uint TutorialCombatMineHardCreatureId = 73668u;
        private const ushort TutorialWorldId = 3460;
        private const ushort NorthernWildsWorldId = 426;
        private const uint NorthernWildsYetiHoldoutCreatureId = 12508u;
        private const uint NorthernWildsSkeechHoldoutCreatureId = 11139u;
        private const uint NorthernWildsDominionHoldoutCreatureId = 11141u;
        private const ushort Q3963MoreImportantThanRevengeQuestId = 3963;
        private const uint Q3963ShipControlsCreatureId = 27196u;

        #region Dependency Injection

        private readonly IPrerequisiteManager prerequisiteManager;
        private readonly IAssetManager assetManager;
        private readonly IGameTableManager gameTableManager;

        public ClientActivateUnitCastHandler(
            IPrerequisiteManager prerequisiteManager,
            IAssetManager assetManager,
            IGameTableManager gameTableManager = null)
        {
            this.prerequisiteManager = prerequisiteManager;
            this.assetManager        = assetManager;
            this.gameTableManager    = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientActivateUnitCast activateUnitCast)
        {
            HandleMessageInternal(session, activateUnitCast.ActivateUnitId, activateUnitCast.ContextToken, nameof(ClientActivateUnitCast));
        }

        internal void HandleMessageInternal(IWorldSession session, uint activateUnitId, uint contextToken, string clientRequestSource, Position position = null)
        {
            IWorldEntity entity = session.Player.GetVisible<IWorldEntity>(activateUnitId);
            if (entity == null)
            {
                IWorldEntity mapEntity = session.Player.Map?.GetEntity<IWorldEntity>(activateUnitId);
                if (mapEntity != null && IsTutorialSpecialActivationEntity(mapEntity) && session.Player.CanSeeEntity(mapEntity))
                {
                    entity = mapEntity;
                    log.Debug($"Tutorial activate-cast recovered stale visibility target: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                }
                else if (session.Player.Map?.Entry?.Id == TutorialWorldId)
                {
                    log.Debug($"Tutorial activate-cast ignored stale target: player={session.Player.Guid}, requestedEntity={activateUnitId}, mapEntity={mapEntity?.Guid ?? 0u}, creature={mapEntity?.CreatureId ?? 0u}.");
                    return;
                }
                else
                {
                    throw new InvalidPacketValueException();
                }
            }

            if (IsTutorialSpecialActivationEntity(entity))
                log.Debug($"Tutorial activate-cast attempt: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, busy={entity.IsBusy}.");

            if (ActivateUnitCombatHelper.TryHandleHostileActivation(session, entity))
                return;

            if (ActivationInteractionGuards.TryRejectBusyTarget(session, entity))
            {
                if (IsTutorialSpecialActivationEntity(entity))
                    log.Debug($"Tutorial activate-cast rejected busy: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                return;
            }

            bool skipPrimaryTargetRangeValidation = ShouldUseVisibleQuestConsoleActivateRange(session, entity);
            if (!skipPrimaryTargetRangeValidation
                && ActivationInteractionGuards.TryRejectOutOfRangeTarget(session, entity, gameTableManager: gameTableManager))
            {
                if (IsTutorialSpecialActivationEntity(entity))
                    log.Debug($"Tutorial activate-cast rejected range: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");
                return;
            }

            if (!TryResolveActivateSpell(entity, session.Player, out uint spell4Id))
            {
                if (IsTutorialSpecialActivationEntity(entity))
                    log.Debug($"Tutorial activate-cast resolve failed: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}.");

                if (TryCompleteTutorialHoverboardProjectorActivationWithoutActivateSpell(session, entity, 0u, CastResult.NoValidActivateSpell, clientRequestSource))
                    return;

                if (TryCompleteTutorialHousingProjectorActivationWithoutActivateSpell(session, entity, 0u, CastResult.NoValidActivateSpell))
                    return;

                if (TryCompleteTutorialMineActivationWithoutSpell(session, entity, 0u, CastResult.NoValidActivateSpell))
                    return;

                if (TryCompleteNorthernWildsSoldierHoldoutActivationWithoutSpell(session, entity, 0u, CastResult.NoValidActivateSpell))
                    return;

                if (TryCastActivePetActionFromActivateShortcut(session, entity, contextToken, clientRequestSource, position))
                    return;

                log.Warn($"Unhandled activate-cast request: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, entityId={entity.EntityId}, world={session.Player.Map?.Entry?.Id ?? 0u}, contextToken={contextToken}, source={clientRequestSource}, reason=no-valid-activate-spell.");
                SendSpellCastResult(session, contextToken, GetFallbackActivateSpellId(entity), CastResult.NoValidActivateSpell);
                entity.OnActivateFail(session.Player);
                return;
            }

            if (IsTutorialSpecialActivationEntity(entity))
                log.Debug($"Tutorial activate-cast resolved spell: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}.");
            bool isNorthernWildsShipControlsActivation = IsNorthernWildsShipControlsActivation(session, entity);
            if (isNorthernWildsShipControlsActivation)
                log.Debug($"Northern Wilds ship controls activate-cast resolved spell: player={session.Player.Guid}, requestedEntity={activateUnitId}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}, contextToken={contextToken}, source={clientRequestSource}.");

            uint clientSideInteractionId = isNorthernWildsShipControlsActivation
                ? GetClientSideInteractionId(spell4Id)
                : 0u;
            uint spell4BaseId = clientSideInteractionId != 0u ? GetSpell4BaseId(spell4Id) : 0u;
            bool deferClientSideInteractionActivation = clientSideInteractionId != 0u;
            uint clientSideInteractionDurationMs = GetClientSideInteractionDurationMs(clientSideInteractionId);
            uint effectiveCastTime = GetEffectiveActivateCastTime(entity, spell4Id);
            var spellParameters = new SpellParameters
            {
                PrimaryTargetId        = entity.Guid,
                UserInitiatedSpellCast = false,
                IgnoreGlobalCooldown   = true,
                CancelActiveTrade      = true,
                UseCreatureOverrides   = isNorthernWildsShipControlsActivation,
                SkipPrimaryTargetRangeValidation = skipPrimaryTargetRangeValidation,
                DeferActivateEffectObjectiveCredit = deferClientSideInteractionActivation,
                WaitForClientSideInteractionResponse = deferClientSideInteractionActivation && effectiveCastTime == 0u,
                ClientSideInteractionDurationMs = clientSideInteractionDurationMs,
                ClientContextToken     = contextToken,
                ClientRequestSource    = clientRequestSource
            };

            if (isNorthernWildsShipControlsActivation)
                log.Debug($"Northern Wilds ship controls activate-cast spell parameters: player={session.Player.Guid}, requestedEntity={activateUnitId}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}, effectiveCastTime={effectiveCastTime}, skipPrimaryTargetRangeValidation={skipPrimaryTargetRangeValidation}, clientSideInteractionId={clientSideInteractionId}, contextToken={contextToken}, source={clientRequestSource}.");
            if (effectiveCastTime != 0u && deferClientSideInteractionActivation)
                spellParameters.FinishCallback = (_, wasCancelled) =>
                {
                    if (!wasCancelled)
                        PendingClientSideInteractionActivationStore.Set(session, entity, clientSideInteractionId, spell4BaseId, invokeActivateCast: true);
                };

            ClientSpellEvidenceCaptureHelper.ApplyPendingCapture(session, spellParameters);
            CastResult castResult = session.Player.TryCastSpell(spell4Id, spellParameters);

            if (castResult != CastResult.Ok)
            {
                if (isNorthernWildsShipControlsActivation)
                    log.Warn($"Northern Wilds ship controls activate-cast failed: player={session.Player.Guid}, requestedEntity={activateUnitId}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}, castResult={castResult}, contextToken={contextToken}, source={clientRequestSource}.");
                if (IsTutorialSpecialActivationEntity(entity))
                    log.Debug($"Tutorial activate-cast failed: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}, castResult={castResult}.");

                if (TryCompleteTutorialHoverboardProjectorActivationWithoutActivateSpell(session, entity, spell4Id, castResult, clientRequestSource))
                    return;

                if (TryCompleteTutorialHousingProjectorActivationWithoutActivateSpell(session, entity, spell4Id, castResult))
                    return;

                if (TryCompleteTutorialMineActivationWithoutSpell(session, entity, spell4Id, castResult))
                    return;

                if (TryCompleteNorthernWildsSoldierHoldoutActivationWithoutSpell(session, entity, spell4Id, castResult))
                    return;

                entity.OnActivateFail(session.Player);
                return;
            }

            if (deferClientSideInteractionActivation)
            {
                if (effectiveCastTime == 0u)
                    PendingClientSideInteractionActivationStore.Set(session, entity, clientSideInteractionId, spell4BaseId, invokeActivateCast: true);
            }
            else
            {
                CompleteActivation(session, entity, invokeActivateCast: true);
            }

            if (IsTutorialSpecialActivationEntity(entity))
                log.Debug($"Tutorial activate-cast success: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}.");
            if (isNorthernWildsShipControlsActivation)
                log.Debug($"Northern Wilds ship controls activate-cast success: player={session.Player.Guid}, requestedEntity={activateUnitId}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}, clientSideInteractionId={clientSideInteractionId}, deferred={deferClientSideInteractionActivation}, contextToken={contextToken}, source={clientRequestSource}.");
        }

        private static bool TryCastActivePetActionFromActivateShortcut(IWorldSession session, IWorldEntity entity, uint contextToken, string clientRequestSource, Position position)
        {
            if (session?.Player == null
                || entity?.Guid != session.Player.Guid
                || clientRequestSource != nameof(ClientActivateUnitCastPosition)
                || session.Player.SpellManager == null
                || !session.Player.SpellManager.TryResolveSingleActivePetActionSpell(out uint actionSpell4Id))
                return false;

            var spellParameters = new SpellParameters
            {
                PrimaryTargetId        = entity.Guid,
                Position               = position,
                UserInitiatedSpellCast = true,
                ClientContextToken     = contextToken,
                ClientRequestSource    = clientRequestSource
            };

            ClientSpellEvidenceCaptureHelper.ApplyPendingCapture(session, spellParameters);
            CastResult castResult = session.Player.TryCastSpell(actionSpell4Id, spellParameters);
            if (castResult != CastResult.Ok)
            {
                SendSpellCastResult(session, contextToken, actionSpell4Id, castResult);
                return true;
            }

            log.Debug($"Active pet-action activate-cast succeeded: player={session.Player.Guid}, spell4Id={actionSpell4Id}, contextToken={contextToken}, source={clientRequestSource}.");
            return true;
        }

        private static bool IsNorthernWildsShipControlsActivation(IWorldSession session, IWorldEntity entity)
        {
            return entity?.CreatureId == Q3963ShipControlsCreatureId && IsNorthernWildsWorld(session, entity);
        }

        private static bool IsNorthernWildsSoldierHoldoutActivation(IWorldSession session, IWorldEntity entity)
        {
            return entity?.CreatureId is NorthernWildsYetiHoldoutCreatureId or NorthernWildsSkeechHoldoutCreatureId or NorthernWildsDominionHoldoutCreatureId
                && IsNorthernWildsWorld(session, entity);
        }

        private static bool ShouldUseVisibleQuestConsoleActivateRange(IWorldSession session, IWorldEntity entity)
        {
            if (!IsNorthernWildsShipControlsActivation(session, entity))
                return false;

            return session?.Player?.QuestManager?.GetQuestState(Q3963MoreImportantThanRevengeQuestId) == QuestState.Accepted;
        }

        private static bool IsNorthernWildsWorld(IWorldSession session, IWorldEntity entity = null)
        {
            uint? worldId = entity?.Map?.Entry?.Id;
            worldId ??= session?.Player?.Map?.Entry?.Id;
            return worldId == NorthernWildsWorldId;
        }

        private void CompleteActivation(IWorldSession session, IWorldEntity entity, bool invokeActivateCast)
        {
            if (invokeActivateCast)
                entity.OnActivateCast(session.Player);

            entity.OnActivateSuccess(session.Player);
            InteractionObjectiveUpdater.UpdateActivateSuccessObjectives(session.Player, entity, assetManager, includeActivateEntity: false, gameTableManager);
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

        private bool TryCompleteNorthernWildsSoldierHoldoutActivationWithoutSpell(IWorldSession session, IWorldEntity entity, uint spell4Id, CastResult castResult)
        {
            if (!IsNorthernWildsSoldierHoldoutActivation(session, entity))
                return false;

            log.Debug($"Northern Wilds Soldier holdout activate-cast bypassed blocked activate spell: player={session.Player.Guid}, entity={entity.Guid}, creature={entity.CreatureId}, spell4Id={spell4Id}, castResult={castResult}.");
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

        private bool TryCompleteTutorialHousingProjectorActivationWithoutActivateSpell(IWorldSession session, IWorldEntity entity, uint spell4Id, CastResult castResult)
        {
            if (!IsTutorialHousingProjectorEntity(entity) || session.Player.Map?.Entry?.Id != TutorialWorldId)
                return false;

            log.Debug($"Tutorial housing projector activate-cast bypassed blocked activate spell: player={session.Player.Guid}, entity={entity.Guid}, spell4Id={spell4Id}, castResult={castResult}.");
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
                if (prerequisiteId != 0u && !MeetsActivateSpellPrerequisite(entity, player, prerequisiteId))
                    continue;

                spell4Id = candidateSpellId;
                return true;
            }

            return false;
        }

        private bool MeetsActivateSpellPrerequisite(IWorldEntity entity, IPlayer player, uint prerequisiteId)
        {
            var parameters = new PrerequisiteParameters
            {
                Target = entity as IUnitEntity
            };

            return prerequisiteManager.Meets(player, prerequisiteId, parameters);
        }

        private uint GetEffectiveActivateCastTime(IWorldEntity entity, uint spell4Id)
        {
            uint creatureCastTime = entity.CreatureEntry?.ActivateSpellCastTime ?? 0u;
            if (creatureCastTime != 0u)
                return creatureCastTime;

            return gameTableManager?.Spell4?.GetEntry(spell4Id)?.CastTime ?? 0u;
        }

        private uint GetClientSideInteractionId(uint spell4Id)
        {
            Spell4Entry spell4Entry = gameTableManager?.Spell4?.GetEntry(spell4Id);
            if (spell4Entry == null)
                return 0u;

            return gameTableManager?.Spell4Base?.GetEntry(spell4Entry.Spell4BaseIdBaseSpell)?.ClientSideInteractionId ?? 0u;
        }

        private uint GetSpell4BaseId(uint spell4Id)
        {
            return gameTableManager?.Spell4?.GetEntry(spell4Id)?.Spell4BaseIdBaseSpell ?? 0u;
        }

        private uint GetClientSideInteractionDurationMs(uint clientSideInteractionId)
        {
            if (clientSideInteractionId == 0u)
                return 0u;

            uint duration = gameTableManager?.ClientSideInteraction?.GetEntry(clientSideInteractionId)?.Duration ?? 0u;
            return duration != 0u ? duration : 60000u;
        }

        private static uint GetFallbackActivateSpellId(IWorldEntity entity)
        {
            return entity.CreatureEntry?.Spell4IdActivate00
                ?? entity.CreatureEntry?.Spell4IdActivate01
                ?? entity.CreatureEntry?.Spell4IdActivate02
                ?? entity.CreatureEntry?.Spell4IdActivate03
                ?? 0u;
        }

        private static void SendSpellCastResult(IWorldSession session, uint contextToken, uint spell4Id, CastResult castResult)
        {
            session.EnqueueMessageEncrypted(new ServerSpellCastResult
            {
                ContextToken = contextToken,
                Spell4Id     = spell4Id,
                CastResult   = castResult
            });
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

        private static bool IsTutorialCombatMineEntity(IWorldEntity entity)
        {
            return entity.CreatureId is TutorialCombatMineEasyCreatureId or TutorialCombatMineMediumCreatureId or TutorialCombatMineHardCreatureId;
        }
    }
}
