using System;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.GameTable;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientEntityInteractionHandler : IMessageHandler<IWorldSession, ClientEntityInteract>
    {
        private const ushort TutorialWorldId = 3460;
        private const uint TutorialHoverboardProjectorCreatureId = 73419u;
        private const uint TutorialHoverboardFinishCreatureId = 73735u;

        #region Dependency Injection

        private readonly ILogger<ClientEntityInteractionHandler> log;

        private readonly IAssetManager assetManager;
        private readonly RealmBankManager realmBankManager;
        private readonly IGameTableManager gameTableManager;

        public ClientEntityInteractionHandler(
            ILogger<ClientEntityInteractionHandler> log,
            IAssetManager assetManager,
            RealmBankManager realmBankManager,
            IGameTableManager gameTableManager = null)
        {
            this.log              = log;
            this.assetManager     = assetManager;
            this.realmBankManager = realmBankManager;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientEntityInteract entityInteraction)
        {
            IWorldEntity entity = session.Player.GetVisible<IWorldEntity>(entityInteraction.Guid);
            if (entity == null && session.Player.Map?.Entry?.Id == TutorialWorldId)
            {
                if (!TryRecoverTutorialInteractionTarget(session, entityInteraction.Guid, out entity))
                    return;
            }

            if (entity != null && ActivationInteractionGuards.TryRejectBusyTarget(session, entity))
                return;

            if (entity != null && ActivationInteractionGuards.TryRejectOutOfRangeTarget(
                    session,
                    entity,
                    entityInteraction.Event == 49 ? GenericError.VendorTooFar : null,
                    gameTableManager))
                return;

            if (TryHandleInteractionEvent(session, entityInteraction, entity))
                UpdateInteractionObjectives(session, entity);
        }

        private bool TryHandleInteractionEvent(IWorldSession session, ClientEntityInteract entityInteraction, IWorldEntity entity)
        {
            switch (entityInteraction.Event)
            {
                case 37: // Quest NPC
                {
                    if (entity == null)
                        throw new InvalidPacketValueException();

                    session.EnqueueMessageEncrypted(new ServerDialogStart
                    {
                        DialogUnitId = entityInteraction.Guid
                    });
                    return true;
                }
                case 49: // Handle Vendor
                    if (entity == null)
                        throw new InvalidPacketValueException();

                    HandleVendor(session, entity);
                    return true;
                case 68: // "MailboxActivate"
                    if (session.Player.Map.GetEntity<IMailboxEntity>(entityInteraction.Guid) == null)
                        throw new InvalidPacketValueException();

                    return true;
                case 67: // "ShowRealmBank"
                    if (entity == null)
                        throw new InvalidPacketValueException();

                    realmBankManager.OpenRealmBank(session.Player);
                    return true;
                case 8: // "HousingGuildNeighborhoodBrokerOpen"
                case 40:
                case 41: // "ResourceConversionOpen"
                case 42: // "ToggleAbilitiesWindow"
                case 43: // "InvokeTradeskillTrainerWindow"
                case 45: // "InvokeShuttlePrompt"
                case 46:
                case 47:
                case 48: // "InvokeTaxiWindow"
                case 65: // "MannequinWindowOpen"
                case 66: // "ShowBank"
                case 69: // "ShowDye"
                case 70: // "GuildRegistrarOpen"
                case 71: // "WarPartyRegistrarOpen"
                case 72: // "GuildBankerOpen"
                case 73: // "WarPartyBankerOpen"
                case 75: // "ToggleMarketplaceWindow"
                case 76: // "ToggleAuctionWindow"
                case 79: // "TradeskillEngravingStationOpen"
                case 80: // "HousingMannequinOpen"
                case 81: // "CityDirectionsList"
                case 82: // "ToggleCREDDExchangeWindow"
                case 84: // "CommunityRegistrarOpen"
                case 85: // "ContractBoardOpen"
                case 86: // "BarberOpen"
                case 87: // "MasterCraftsmanOpen"
                    if (entity == null)
                        throw new InvalidPacketValueException();

                    return true;
                default:
                    log.LogWarning("Unhandled entity interaction event {InteractionEvent} from player {PlayerGuid}: requestedEntity={RequestedEntity}, resolvedEntity={ResolvedEntity}, creature={CreatureId}, entityId={EntityId}, world={WorldId}.",
                        entityInteraction.Event,
                        session.Player?.Guid,
                        entityInteraction.Guid,
                        entity?.Guid ?? 0u,
                        entity?.CreatureId ?? 0u,
                        entity?.EntityId ?? 0u,
                        session.Player?.Map?.Entry?.Id ?? 0u);
                    return false;
            }
        }

        private void UpdateInteractionObjectives(IWorldSession session, IWorldEntity entity)
        {
            InteractionObjectiveUpdater.UpdateDirectInteractionObjectives(session.Player, entity, assetManager, gameTableManager);
        }

        private void HandleVendor(IWorldSession session, IWorldEntity worldEntity)
        {
            if (worldEntity is not INonPlayerEntity vendorEntity)
                throw new InvalidOperationException();

            if (vendorEntity.VendorInfo == null)
                throw new InvalidOperationException();

            session.Player.SelectedVendorInfo = vendorEntity.VendorInfo;

            ServerVendorItemsUpdated vendorItemsUpdated = vendorEntity.VendorInfo.Build();
            vendorItemsUpdated.Guid = vendorEntity.Guid;
            session.EnqueueMessageEncrypted(vendorItemsUpdated);
        }

        private bool TryRecoverTutorialInteractionTarget(IWorldSession session, uint targetId, out IWorldEntity entity)
        {
            entity = session.Player.Map?.GetEntity<IWorldEntity>(targetId);
            if (entity == null)
            {
                log.LogDebug("Tutorial entity interaction lookup failed: player={PlayerGuid}, requestedEntity={RequestedEntity}, reason=not-on-map.",
                    session.Player.Guid,
                    targetId);
                return false;
            }

            bool canSee = session.Player.CanSeeEntity(entity);
            if (IsTutorialHoverboardActivationEntity(entity) && canSee)
            {
                log.LogDebug("Tutorial entity interaction recovered stale visibility target: player={PlayerGuid}, entity={EntityGuid}, creature={CreatureId}.",
                    session.Player.Guid,
                    entity.Guid,
                    entity.CreatureId);
                return true;
            }

            log.LogDebug("Tutorial entity interaction ignored stale target: player={PlayerGuid}, requestedEntity={RequestedEntity}, mapEntity={MapEntity}, creature={CreatureId}, canSee={CanSee}.",
                session.Player.Guid,
                targetId,
                entity.Guid,
                entity.CreatureId,
                canSee);
            entity = null;
            return false;
        }

        private static bool IsTutorialHoverboardActivationEntity(IWorldEntity entity)
        {
            return entity.CreatureId is TutorialHoverboardProjectorCreatureId or TutorialHoverboardFinishCreatureId;
        }
    }
}
