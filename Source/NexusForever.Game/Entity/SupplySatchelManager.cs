using System.Collections;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reward;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class SupplySatchelManager : ISupplySatchelManager
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
            loadedOrigin = CharacterLoadOrigin.Get(model);
            player = owner;
            this.gameTableManager = gameTableManager;
            maximumStackAmount = ResolveMaximumStackAmount(owner);

            foreach (CharacterTradeskillMaterialModel tradeskillMaterial in model.TradeskillMaterials)
                tradeskillMaterials.Add(tradeskillMaterial.MaterialId, new TradeskillMaterial(tradeskillMaterial, gameTableManager, CanMutateMaterial));
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
            var held = commodityReturn;
            foreach (ITradeskillMaterial material in tradeskillMaterials.Values.Where(material => held == null || material.MaterialId != held.Write.Id))
                if (material is TradeskillMaterial concrete) concrete.SaveMaterial(context, loadedOrigin?.Database); else material.Save(context);
            held?.SaveOrdinary(context);
        }
        internal Action<CharacterContext> CaptureNormalSave(object token, string database)
        {
            if (loadedOrigin != null && loadedOrigin.Database != database)
                throw new InvalidOperationException("Satchel capture changed its loaded database route.");
            // A retained return owns its material through ordinary reconciliation,
            // including rebases after settlement. Its existing typed witness and
            // retry receipt remain responsible for that one row.
            var held = commodityReturn;
            var saves = tradeskillMaterials.Values.Where(material => held == null || material.MaterialId != held.Write.Id).Select(material => material is TradeskillMaterial concrete
                ? concrete.CaptureSave(token, database) : (Action<CharacterContext>)material.Save).ToArray();
            return context => { foreach (var save in saves) save(context); held?.SaveOrdinary(context); };
        }

        public bool TryGetAutoStoreCapacity(IItemInfo info, out uint capacity, uint charges = 0)
        {
            capacity = 0;
            if (player?.IsLoading != false || info?.Entry == null || charges != 0u
                || info.Entry.MaxCharges != 0u || info.Entry.ExpirationTimeMinutes != 0u
                || !info.IsStackable() || info.IsEquippable()
                || !TryGetMaterialEntry(info, out TradeskillMaterialEntry entry)
                || entry.Id >= 512u)
                return false;

            // CollectItem currently counts/consumes physical bag items. Preserve that
            // contract while this new routing is limited to ordinary material grants.
            if (player.QuestManager?.GetActiveQuests()?.Any(quest => quest.Any(objective =>
                objective.ObjectiveInfo?.Type == QuestObjectiveType.CollectItem
                && objective.ObjectiveInfo.Entry?.Data == info.Id)) == true)
                return false;

            uint current = tradeskillMaterials.TryGetValue((ushort)entry.Id, out ITradeskillMaterial material)
                ? material.Amount : 0u;
            capacity = maximumStackAmount - Math.Min(current, maximumStackAmount);
            return true;
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
            RequireCommodityMutationAllowed();
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
                tradeskillMaterials.Add(materialId, new TradeskillMaterial(player.CharacterId, materialId, gameTableManager, CanMutateMaterial));
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
            RequireCommodityMutationAllowed();
            if (amount == 0)
                throw new ArgumentException("amount must be more than 0");

            if (!TryGetMaterialEntry(materialId, out _))
                return amount;

            uint amountAdded;
            if (tradeskillMaterials.ContainsKey(materialId))
                amountAdded = AddAmountToMaterial(materialId, amount);
            else
            {
                tradeskillMaterials.Add(materialId, new TradeskillMaterial(player.CharacterId, materialId, gameTableManager, CanMutateMaterial));
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
            RequireCommodityMutationAllowed();
            if (!TryGetMaterialEntry(item, out TradeskillMaterialEntry entry))
                return;

            ushort materialId = (ushort)entry.Id;
            if (tradeskillMaterials.ContainsKey(materialId))
            {
                if (amount > tradeskillMaterials[materialId].Amount)
                    return; // Swallow the issue for now.
            }
            else
                tradeskillMaterials.Add(materialId, new TradeskillMaterial(player.CharacterId, materialId, gameTableManager, CanMutateMaterial));

            RemoveAmount(materialId, amount);
        }

        public void RemoveAmount(ushort materialId, uint amount)
        {
            RequireCommodityMutationAllowed();
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
            => WithdrawToInventory(materialId, amount);

        public void MoveToInventoryFromClient(GameSession session, ushort materialId, uint amount)
        {
            if (player.Inventory is Inventory inventory && ReferenceEquals(player.SupplySatchelManager, this))
                inventory.TrySaveResourceConversion(session, () => WithdrawToInventory(materialId, amount));
        }

        private bool WithdrawToInventory(ushort materialId, uint amount)
        {
            if (amount == 0u || !tradeskillMaterials.TryGetValue(materialId, out ITradeskillMaterial material))
                return false;

            if (material.Entry == null || material.Entry.Item2IdStatRevolution == 0u || amount > material.Amount)
                return false;

            if (player.Inventory is Inventory inventory)
                return inventory.WithdrawSupplySatchelItem(this, material, amount);

            RemoveAmount(materialId, amount);
            player.Inventory.ItemCreate(InventoryLocation.Inventory, material.Entry.Item2IdStatRevolution, amount, ItemUpdateReason.ResourceConversion);
            return true;
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
            => TryGetMaterialEntry(item?.Info, out entry);

        private bool TryGetMaterialEntry(IItemInfo info, out TradeskillMaterialEntry entry)
        {
            entry = null;
            uint itemId = info?.Entry?.Id ?? 0u;
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
            var packet = new ServerSupplySatchelUpdate
            {
                MaterialId = materialId,
                StackCount = tradeskillMaterials[materialId].Amount
            };
            if (player.Inventory is Inventory inventory && inventory.BufferCommodityMessage(packet)) return;
            var session = player.Session;
            if (session is IPlayerSession bound &&
                (!ReferenceEquals(bound.Player, player) || !ReferenceEquals(player.SupplySatchelManager, this)))
                return;
            session.EnqueueMessageEncrypted(packet);
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
