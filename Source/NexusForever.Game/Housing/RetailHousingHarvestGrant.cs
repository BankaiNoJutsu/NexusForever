using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Housing
{
    /// <summary>
    /// Grants housing plug harvest yields with owner/neighbor split (build 16042).
    /// </summary>
    public static class RetailHousingHarvestGrant
    {
        const int HarvestCooldownSeconds = 300;

        static readonly Dictionary<(ulong ResidenceId, uint PlotInfoId), DateTimeOffset> lastHarvestUtc = new();

        public static bool TryHarvestPlug(
            IPlayer harvester,
            IResidence residence,
            IPlot plot,
            HousingPlugItemEntry plugEntry,
            IGameTableManager gameTableManager)
        {
            if (harvester == null || residence == null || plot == null || plugEntry == null || gameTableManager == null)
                return false;

            if (!CanHarvest(harvester, residence))
                return false;

            if (!RetailHousingHarvestResolver.TryResolveYield(
                    plugEntry,
                    gameTableManager,
                    GetContributionPointTotal(plot),
                    out RetailHousingHarvestResolver.HarvestYield yield))
                return false;

            var key = (residence.Id, plot.PlotInfoEntry.Id);
            lock (lastHarvestUtc)
            {
                if (lastHarvestUtc.TryGetValue(key, out DateTimeOffset last)
                    && DateTimeOffset.UtcNow - last < TimeSpan.FromSeconds(HarvestCooldownSeconds))
                    return false;

                lastHarvestUtc[key] = DateTimeOffset.UtcNow;
            }

            bool garden = plugEntry.HousingPlugTypeEnum == 2u;
            ulong ownerCharacterId = residence.OwnerId ?? 0ul;
            if (ownerCharacterId == 0ul)
                return false;

            (uint ownerAmount, uint harvesterAmount) = RetailHousingHarvestSplit.SplitYield(
                residence,
                ownerCharacterId,
                harvester.CharacterId,
                yield.Quantity,
                garden);

            var ownerGrants     = new List<(uint, uint)>();
            var harvesterGrants = new List<(uint, uint)>();

            if (ownerAmount > 0)
            {
                if (TryGrantToCharacter(ownerCharacterId, yield.Item2Id, ownerAmount))
                    ownerGrants.Add((yield.Item2Id, ownerAmount));
            }

            if (harvesterAmount > 0)
            {
                harvester.Inventory.ItemCreate(
                    InventoryLocation.Inventory,
                    yield.Item2Id,
                    harvesterAmount,
                    ItemUpdateReason.HousingContribution);
                harvesterGrants.Add((yield.Item2Id, harvesterAmount));
            }

            IPlayer ownerPlayer = PlayerManager.Instance.GetPlayer(ownerCharacterId);
            if (ownerGrants.Count > 0)
                RetailHousingHarvestNotify.NotifyGrants(ownerPlayer ?? harvester, ownerGrants);

            if (harvesterGrants.Count > 0 && harvester.CharacterId != ownerCharacterId)
                RetailHousingHarvestNotify.NotifyGrants(harvester, harvesterGrants);

            return ownerGrants.Count > 0 || harvesterGrants.Count > 0;
        }

        static uint GetContributionPointTotal(IPlot plot)
        {
            // Until plot contribution totals are persisted, treat built plugs as tier-0 eligible.
            return plot.BuildState >= 4 ? uint.MaxValue : 0u;
        }

        static bool CanHarvest(IPlayer harvester, IResidence residence)
        {
            if (harvester.CharacterId == residence.OwnerId)
                return true;

            return residence.GetNeighbors().Any(n => n.CharacterId == harvester.CharacterId && n.PermissionLevel > 0);
        }

        static bool TryGrantToCharacter(ulong characterId, uint item2Id, uint quantity)
        {
            IPlayer owner = PlayerManager.Instance.GetPlayer(characterId);
            if (owner != null)
            {
                owner.Inventory.ItemCreate(InventoryLocation.Inventory, item2Id, quantity, ItemUpdateReason.HousingContribution);
                return true;
            }

            return MarketplaceMailDelivery.TrySendSystemItemMail(
                characterId,
                item2Id,
                quantity,
                "Housing Harvest",
                "Resources from your housing plot were mailed while you were away.");
        }
    }
}
