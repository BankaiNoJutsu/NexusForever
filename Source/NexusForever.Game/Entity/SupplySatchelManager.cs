using System.Collections;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public class SupplySatchelManager : ISupplySatchelManager
    {
        private const uint DefaultMaximumStackAmount = 100u;

        private readonly IPlayer player;
        private readonly IGameTableManager gameTableManager;
        private readonly uint maximumStackAmount = DefaultMaximumStackAmount;
        private readonly Dictionary</* materialId */ushort, ITradeskillMaterial> tradeskillMaterials = new();

        public SupplySatchelManager(
            IPlayer owner,
            CharacterModel model,
            IGameTableManager gameTableManager)
        {
            player = owner;
            this.gameTableManager = gameTableManager;
            maximumStackAmount = ResolveMaximumStackAmount(owner);

            foreach (CharacterTradeskillMaterialModel tradeskillMaterial in model.TradeskillMaterials)
                tradeskillMaterials.Add(tradeskillMaterial.MaterialId, new TradeskillMaterial(tradeskillMaterial, gameTableManager));
        }

        private static uint ResolveMaximumStackAmount(IPlayer owner)
        {
            float? rewardValue = owner?.Account?.RewardPropertyManager?
                .GetRewardProperty(RewardPropertyType.TradeskillMatStackLimit)?
                .GetValue(0u);

            if (!rewardValue.HasValue || float.IsNaN(rewardValue.Value) || float.IsInfinity(rewardValue.Value) || rewardValue.Value <= 0f)
                return DefaultMaximumStackAmount;

            if (rewardValue.Value >= ushort.MaxValue)
                return ushort.MaxValue;

            uint stackAmount = (uint)rewardValue.Value;
            return stackAmount == 0u ? DefaultMaximumStackAmount : stackAmount;
        }

        public void Save(CharacterContext context)
        {
            foreach (ITradeskillMaterial material in tradeskillMaterials.Values)
                material.Save(context);
        }

        public ushort[] BuildNetworkPacket()
        {
            ushort[] tradeskillMaterialsCount = new ushort[512];

            foreach ((ushort materialId, ITradeskillMaterial material) in tradeskillMaterials)
            {
                if (materialId >= tradeskillMaterialsCount.Length)
                    continue;

                tradeskillMaterialsCount[materialId] = material.Amount;
            }

            return tradeskillMaterialsCount;
        }

        /// <summary>
        /// Add the provided amount to the <see cref="ITradeskillMaterial"/> that is associated with the provided <see cref="IItem"/>
        /// </summary>
        /// <returns>
        /// Returns the remainder that could not be added.
        /// </returns>
        public uint AddAmount(IItem item, uint amount)
        {
            if (amount == 0)
                throw new ArgumentException("amount must be more than 0.");

            if (!TryGetMaterialEntry(item, out TradeskillMaterialEntry entry))
                return amount;

            ushort materialId = (ushort)entry.Id;
            uint amountAdded = 0;
            if (tradeskillMaterials.ContainsKey(materialId))
                amountAdded = AddAmountToMaterial(materialId, amount);
            else
            {
                tradeskillMaterials.Add(materialId, new TradeskillMaterial(player.CharacterId, materialId, gameTableManager));
                amountAdded = AddAmountToMaterial(materialId, amount);
            }

            return amountAdded;
        }

        /// <summary>
        /// Add the provided amount to the <see cref="ITradeskillMaterial"/> for the given Material ID
        /// </summary>
        /// <returns>
        /// Returns the remainder that could not be added.
        /// </returns>
        public uint AddAmount(ushort materialId, uint amount)
        {
            if (amount == 0)
                throw new ArgumentException("amount must be more than 0");

            if (!TryGetMaterialEntry(materialId, out _))
                return amount;

            uint amountAdded;
            if (tradeskillMaterials.ContainsKey(materialId))
                amountAdded = AddAmountToMaterial(materialId, amount);
            else
            {
                tradeskillMaterials.Add(materialId, new TradeskillMaterial(player.CharacterId, materialId, gameTableManager));
                amountAdded = AddAmountToMaterial(materialId, amount);
            }

            return amountAdded;
        }

        private uint AddAmountToMaterial(ushort materialId, uint amount)
        {
            if (!tradeskillMaterials.TryGetValue(materialId, out ITradeskillMaterial material))
                throw new InvalidOperationException("Material not found in cache.");

            if (material.Amount >= maximumStackAmount)
                return amount;

            uint amountAllowed = maximumStackAmount - material.Amount;
            if (amountAllowed >= amount)
            {
                material.Amount += (ushort)amount;
                SendSupplySatchelStackUpdate(materialId);
                return 0;
            }

            material.Amount = (ushort)maximumStackAmount;
            SendSupplySatchelStackUpdate(materialId);
            return amount - amountAllowed;
        }

        /// <summary>
        /// Remove the provided amount to the <see cref="ITradeskillMaterial"/> that is associated with the provided <see cref="IItem"/>
        /// </summary>
        public void RemoveAmount(IItem item, uint amount)
        {
            if (!TryGetMaterialEntry(item, out TradeskillMaterialEntry entry))
                return;

            ushort materialId = (ushort)entry.Id;
            if (tradeskillMaterials.ContainsKey(materialId))
            {
                if (amount > tradeskillMaterials[materialId].Amount)
                    return; // Swallow the issue for now.
            }
            else
                tradeskillMaterials.Add(materialId, new TradeskillMaterial(player.CharacterId, materialId, gameTableManager));

            RemoveAmount(materialId, amount);
        }

        public void RemoveAmount(ushort materialId, uint amount)
        {
            if (!tradeskillMaterials.ContainsKey(materialId))
                throw new ArgumentOutOfRangeException(nameof(materialId));

            if (amount > tradeskillMaterials[materialId].Amount)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (tradeskillMaterials[materialId].Amount >= amount)
                tradeskillMaterials[materialId].Amount -= (ushort)amount;

            SendSupplySatchelStackUpdate(materialId);
        }

        /// <summary>
        /// Moves the given amount of the given material to the player's <see cref="IInventory"/>
        /// </summary>
        public void MoveToInventory(ushort materialId, uint amount)
        {
            if (amount == 0u || !tradeskillMaterials.TryGetValue(materialId, out ITradeskillMaterial material))
                return;

            if (material.Entry == null || amount > material.Amount)
                return;

            // Remove the amount first. The Inventory will replace what it couldn't create items for.
            RemoveAmount(materialId, amount);
            player.Inventory.ItemCreate(InventoryLocation.Inventory, material.Entry.Item2IdStatRevolution, amount, ItemUpdateReason.ResourceConversion);
        }

        /// <summary>
        /// Returns whether the <see cref="ITradeskillMaterial"/> associated with the given <see cref="IItem"/> is currently at its maximum amount
        /// </summary>
        public bool IsFull(IItem item)
        {
            if (!TryGetMaterialEntry(item, out TradeskillMaterialEntry entry))
                return true;

            return IsFull((ushort)entry.Id);
        }

        private bool IsFull(ushort materialId)
        {
            if (tradeskillMaterials.TryGetValue(materialId, out ITradeskillMaterial material))
                return material.Amount >= maximumStackAmount;
            else
                return false;
        }

        /// <summary>
        /// Returns whether there is enough of the given material
        /// </summary>
        public bool CanAfford(ushort materialId, uint amount)
        {
            if (tradeskillMaterials.TryGetValue(materialId, out ITradeskillMaterial material))
                return amount <= material.Amount;
            else
                return false;
        }

        private bool TryGetMaterialEntry(IItem item, out TradeskillMaterialEntry entry)
        {
            entry = null;
            uint itemId = item?.Info?.Entry?.Id ?? 0u;
            if (itemId == 0u)
                return false;

            entry = gameTableManager?.TradeskillMaterial?.Entries?
                .SingleOrDefault(i => i.Item2IdStatRevolution == itemId);
            return entry != null && entry.Id <= ushort.MaxValue;
        }

        private bool TryGetMaterialEntry(ushort materialId, out TradeskillMaterialEntry entry)
        {
            entry = gameTableManager?.TradeskillMaterial?.GetEntry(materialId);
            return entry != null && entry.Item2IdStatRevolution != 0u;
        }

        private void SendSupplySatchelStackUpdate(ushort materialId)
        {
            player.Session.EnqueueMessageEncrypted(new ServerSupplySatchelUpdate
            {
                MaterialId = materialId,
                StackCount = tradeskillMaterials[materialId].Amount
            });
        }

        public IEnumerator<ITradeskillMaterial> GetEnumerator()
        {
            return tradeskillMaterials.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
