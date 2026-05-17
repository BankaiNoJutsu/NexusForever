using System;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientConvertResourceHandler : IMessageHandler<IWorldSession, ClientConvertResource>
    {
        private readonly ILogger<ClientConvertResourceHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientConvertResourceHandler(
            ILogger<ClientConvertResourceHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientConvertResource convertResource)
        {
            ResourceConversionEntry conversion = gameTableManager.ResourceConversion.GetEntry(convertResource.ConversionId);
            if (conversion == null)
            {
                log.LogDebug("Ignoring resource conversion request from player {PlayerGuid}: unknown conversion {ConversionId}, selector {Selector}, resource field {ResourceField}.",
                    session.Player?.Guid, convertResource.ConversionId, convertResource.Selector, convertResource.ResourceField);
                return;
            }

            if (!TryApplyConversion(session, conversion))
                return;

            log.LogDebug("Applied resource conversion for player {PlayerGuid}: conversion {ConversionId}, type {ConversionType}, source {SourceId} x{SourceCount}, target {TargetId} x{TargetCount}.",
                session.Player?.Guid,
                conversion.Id,
                conversion.ResourceConversionTypeEnum,
                conversion.SourceId,
                conversion.SourceCount,
                conversion.TargetId,
                conversion.TargetCount);
        }

        private bool TryApplyConversion(IWorldSession session, ResourceConversionEntry conversion)
        {
            return conversion.ResourceConversionTypeEnum switch
            {
                0u => TryConvertItemToItem(session, conversion),
                1u or 2u => TryConvertItemToReputation(session, conversion),
                3u => TryConvertCurrencyToReputation(session, conversion),
                4u => TryConvertCurrencyToCurrency(session, conversion),
                _ => RejectConversion(session, conversion, $"unknown conversion type {conversion.ResourceConversionTypeEnum}")
            };
        }

        private bool TryConvertItemToItem(IWorldSession session, ResourceConversionEntry conversion)
        {
            if (gameTableManager.Item.GetEntry(conversion.TargetId) == null)
                return RejectConversion(session, conversion, $"target item {conversion.TargetId} was not found");

            if (!session.Player.Inventory.HasItemCount(conversion.SourceId, conversion.SourceCount))
                return RejectConversion(session, conversion, $"missing source item {conversion.SourceId} x{conversion.SourceCount}");

            session.Player.Inventory.ItemDelete(conversion.SourceId, conversion.SourceCount, ItemUpdateReason.ResourceConversion);
            session.Player.Inventory.ItemCreate(InventoryLocation.Inventory, conversion.TargetId, conversion.TargetCount, ItemUpdateReason.ResourceConversion);
            return true;
        }

        private bool TryConvertItemToReputation(IWorldSession session, ResourceConversionEntry conversion)
        {
            if (!Enum.IsDefined(typeof(Faction), conversion.TargetId))
                return RejectConversion(session, conversion, $"target faction {conversion.TargetId} was not found");

            if (!session.Player.Inventory.HasItemCount(conversion.SourceId, conversion.SourceCount))
                return RejectConversion(session, conversion, $"missing source item {conversion.SourceId} x{conversion.SourceCount}");

            session.Player.Inventory.ItemDelete(conversion.SourceId, conversion.SourceCount, ItemUpdateReason.ResourceConversion);
            session.Player.ReputationManager.UpdateReputation((Faction)conversion.TargetId, conversion.TargetCount);
            return true;
        }

        private bool TryConvertCurrencyToReputation(IWorldSession session, ResourceConversionEntry conversion)
        {
            if (!TryGetCurrency(conversion.SourceId, out CurrencyType sourceCurrency))
                return RejectConversion(session, conversion, $"source currency {conversion.SourceId} was not found");
            if (!Enum.IsDefined(typeof(Faction), conversion.TargetId))
                return RejectConversion(session, conversion, $"target faction {conversion.TargetId} was not found");

            if (!session.Player.CurrencyManager.CanAfford(sourceCurrency, conversion.SourceCount))
                return RejectConversion(session, conversion, $"missing source currency {conversion.SourceId} x{conversion.SourceCount}");

            session.Player.CurrencyManager.CurrencySubtractAmount(sourceCurrency, conversion.SourceCount);
            session.Player.ReputationManager.UpdateReputation((Faction)conversion.TargetId, conversion.TargetCount);
            return true;
        }

        private bool TryConvertCurrencyToCurrency(IWorldSession session, ResourceConversionEntry conversion)
        {
            if (!TryGetCurrency(conversion.SourceId, out CurrencyType sourceCurrency))
                return RejectConversion(session, conversion, $"source currency {conversion.SourceId} was not found");
            if (!TryGetCurrency(conversion.TargetId, out CurrencyType targetCurrency))
                return RejectConversion(session, conversion, $"target currency {conversion.TargetId} was not found");

            CurrencyType surchargeCurrency = CurrencyType.None;
            if (conversion.SurchargeId != 0u && !TryGetCurrency(conversion.SurchargeId, out surchargeCurrency))
                return RejectConversion(session, conversion, $"surcharge currency {conversion.SurchargeId} was not found");

            if (!session.Player.CurrencyManager.CanAfford(sourceCurrency, conversion.SourceCount))
                return RejectConversion(session, conversion, $"missing source currency {conversion.SourceId} x{conversion.SourceCount}");
            if (surchargeCurrency != CurrencyType.None && !session.Player.CurrencyManager.CanAfford(surchargeCurrency, conversion.SurchargeCount))
                return RejectConversion(session, conversion, $"missing surcharge currency {conversion.SurchargeId} x{conversion.SurchargeCount}");

            session.Player.CurrencyManager.CurrencySubtractAmount(sourceCurrency, conversion.SourceCount);
            if (surchargeCurrency != CurrencyType.None)
                session.Player.CurrencyManager.CurrencySubtractAmount(surchargeCurrency, conversion.SurchargeCount);

            session.Player.CurrencyManager.CurrencyAddAmount(targetCurrency, conversion.TargetCount);
            return true;
        }

        private bool TryGetCurrency(uint currencyId, out CurrencyType currencyType)
        {
            currencyType = (CurrencyType)currencyId;
            return Enum.IsDefined(typeof(CurrencyType), currencyType)
                && gameTableManager.CurrencyType.GetEntry(currencyId) != null;
        }

        private bool RejectConversion(IWorldSession session, ResourceConversionEntry conversion, string reason)
        {
            log.LogDebug("Rejected resource conversion request from player {PlayerGuid}: conversion {ConversionId}, reason {Reason}.",
                session.Player?.Guid, conversion.Id, reason);
            return false;
        }
    }

    public class ClientDashCastHandler : IMessageHandler<IWorldSession, ClientDashCast>
    {
        private readonly ILogger<ClientDashCastHandler> log;

        public ClientDashCastHandler(ILogger<ClientDashCastHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientDashCast dashCast)
        {
            log.LogDebug("Ignoring client dash-cast state request from player {PlayerGuid}: direction/state {DirectionOrState}.",
                session.Player?.Guid, dashCast.DirectionOrState);
        }
    }

    public class ClientMovementFallLandHandler : IMessageHandler<IWorldSession, ClientMovementFallLand>
    {
        private readonly ILogger<ClientMovementFallLandHandler> log;

        public ClientMovementFallLandHandler(ILogger<ClientMovementFallLandHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientMovementFallLand fallLand)
        {
            log.LogDebug("Ignoring client movement fall-land state request from player {PlayerGuid}: state {LandingState}, position {Position}.",
                session.Player?.Guid, fallLand.LandingState, fallLand.Position);
        }
    }

    public class ClientMovementFallDamageHandler : IMessageHandler<IWorldSession, ClientMovementFallDamage>
    {
        private readonly ILogger<ClientMovementFallDamageHandler> log;

        public ClientMovementFallDamageHandler(ILogger<ClientMovementFallDamageHandler> log)
        {
            this.log = log;
        }

        /// <summary>
        /// Client sends the accumulated fall-damage state float just before the fall-land opcode.
        /// The server uses this as a hint but is not authoritative over damage application.
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientMovementFallDamage fallDamage)
        {
            log.LogDebug("ClientMovementFallDamage: player={Player}, fallStateValue={Value}",
                session.Player?.Guid, fallDamage.FallStateValue);
        }
    }
}
