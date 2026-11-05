using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Vendor
{
    public class ClientRepairVendorStatusRequestHandler : IMessageHandler<IWorldSession, ClientRepairVendorStatusRequest>
    {
        private readonly ILogger<ClientRepairVendorStatusRequestHandler> log;
        private readonly IBuybackManager buybackManager;

        public ClientRepairVendorStatusRequestHandler(
            ILogger<ClientRepairVendorStatusRequestHandler> log,
            IBuybackManager buybackManager)
        {
            this.log = log;
            this.buybackManager = buybackManager;
        }

        public void HandleMessage(IWorldSession session, ClientRepairVendorStatusRequest _)
        {
            if (session.Player.SelectedVendorInfo == null)
            {
                log.LogDebug("Rejecting sell-junk vendor request from player {PlayerGuid}: no selected vendor.",
                    session.Player.Guid);
                session.Player.SendGenericError(GenericError.VendorNoVendor);
                return;
            }

            VendorSellHelper.SellJunk(session.Player, buybackManager);
        }
    }

    public class ClientRepairItemVendorHandler : IMessageHandler<IWorldSession, ClientRepairItemVendor>
    {
        private const uint RepairFormulaId = 0x022Fu;

        private readonly ILogger<ClientRepairItemVendorHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientRepairItemVendorHandler(
            ILogger<ClientRepairItemVendorHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientRepairItemVendor repairItemVendor)
        {
            IPlayer player = session.Player;
            IInventory inventory = player?.Inventory;
            if (inventory == null || !InventoryMutationScope.TryAcquire(inventory, out var mutationLease))
                return;
            using var heldMutation = mutationLease;
            IVendorInfo vendor = player.SelectedVendorInfo;
            if (vendor == null)
            {
                player.SendGenericError(GenericError.VendorNoVendor);
                return;
            }

            IReadOnlyList<IItem> repairItems = GetRepairItems(player, repairItemVendor.ItemIdentity);
            if (repairItems.Count == 0)
                return;
            var quote = repairItems.Select(item => (Item: item, Durability: item.Durability)).ToArray();
            ICurrencyManager wallet = player.CurrencyManager;

            ulong repairCost = 0ul;
            foreach (IItem item in repairItems)
                repairCost = checked(repairCost + CalculateRepairCost(item));

            if (repairCost == 0ul)
                return;

            if (!wallet.CanAfford(CurrencyType.Credits, repairCost))
            {
                player.SendGenericError(GenericError.VendorNotEnoughCash);
                return;
            }

            if (!ReferenceEquals(session.Player, player) || !ReferenceEquals(player.Inventory, inventory)
                || !ReferenceEquals(player.CurrencyManager, wallet) || !ReferenceEquals(player.SelectedVendorInfo, vendor)
                || quote.Any(line => !line.Item.Durability.Equals(line.Durability)))
                return;

            if (repairItemVendor.ItemIdentity == 0ul && repairItemVendor.RepairCost != 0ul && repairItemVendor.RepairCost != repairCost)
            {
                log.LogDebug("Repair-all cost mismatch for player {PlayerGuid}: client {ClientCost}, server {ServerCost}.",
                    player.Guid, repairItemVendor.RepairCost, repairCost);
            }

            if (player is Player owner && inventory is Inventory concrete && wallet is CurrencyManager)
            {
                var originalSession = owner.Session;
                var events = originalSession.Events;
                var account = owner.Account;
                ulong epoch = account.WorldSessionEpoch;
                heldMutation.Dispose();
                concrete.SettleVendorRepair(owner, quote, repairCost, () =>
                    ReferenceEquals(session.Player, owner) && ReferenceEquals(owner.Session, originalSession)
                    && ReferenceEquals(originalSession.Events, events) && ReferenceEquals(owner.Account, account)
                    && account.WorldSessionEpoch == epoch && ReferenceEquals(owner.Inventory, concrete)
                    && ReferenceEquals(owner.CurrencyManager, wallet));
                return;
            }

            wallet.CurrencySubtractAmount(CurrencyType.Credits, repairCost);
            foreach (IItem item in repairItems)
                item.Durability = 1.0f;
        }

        private static IReadOnlyList<IItem> GetRepairItems(IPlayer player, ulong itemIdentity)
        {
            if (itemIdentity != 0ul)
            {
                IItem item = player.Inventory.GetItem(itemIdentity);
                return IsRepairable(item) ? [item] : [];
            }

            return player.Inventory
                .SelectMany(b => b)
                .Where(IsRepairable)
                .ToList();
        }

        private static bool IsRepairable(IItem item)
        {
            return item?.Info != null
                && item.Info.IsEquippable()
                && item.Durability < 1.0f;
        }

        private ulong CalculateRepairCost(IItem item)
        {
            GameFormulaEntry formula = gameTableManager.GameFormula?.GetEntry(RepairFormulaId);
            float repairMultiplier = formula?.Datafloat03 ?? 0f;
            if (repairMultiplier <= 0f)
                return 0ul;

            float missingDurability = Math.Clamp(1.0f - item.Durability, 0.0f, 1.0f);
            if (missingDurability <= 0f)
                return 0ul;

            ulong itemValue = GetRepairValue(item);
            if (itemValue == 0ul)
                return 0ul;

            double cost = repairMultiplier * missingDurability * itemValue;
            return (ulong)Math.Round(cost, MidpointRounding.AwayFromZero);
        }

        private static ulong GetRepairValue(IItem item)
        {
            for (byte i = 0; i < 2; i++)
            {
                if (item.Info.GetVendorBuyCurrency(i) == CurrencyType.Credits)
                    return item.Info.GetVendorBuyAmount(i);
            }

            for (byte i = 0; i < 2; i++)
            {
                if (item.Info.GetVendorSellCurrency(i) == CurrencyType.Credits)
                    return item.Info.GetVendorSellAmount(i);
            }

            return 0ul;
        }
    }
}
