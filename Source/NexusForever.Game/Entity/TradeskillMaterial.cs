using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    public partial class TradeskillMaterial : ITradeskillMaterial
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="ITradeskillMaterial"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum TradeskillMaterialSaveMask
        {
            None   = 0x0000,
            Create = 0x0001,
            Amount = 0x0002
        }

        public TradeskillMaterialEntry Entry { get; }
        public ulong Owner { get; }
        public ushort MaterialId { get; }

        public ushort Amount
        {
            get { lock (materialSaveGate) return amount; }
            set
            {
                lock (materialSaveGate)
                {
                    if (commodityReturnOwner != null || publicationMutationAllowed?.Invoke() == false) throw new InvalidOperationException("Material is reserved for a commodity return.");
                    amount = value; ++amountRevision;
                    saveMask |= TradeskillMaterialSaveMask.Amount;
                }
            }
        }
        private ushort amount;
        private readonly Func<bool> publicationMutationAllowed;

        private TradeskillMaterialSaveMask saveMask;

        public TradeskillMaterial(
            CharacterTradeskillMaterialModel model,
            IGameTableManager gameTableManager)
        {
            Owner      = model.Id;
            MaterialId = model.MaterialId;
            Amount     = model.Amount;

            Entry      = gameTableManager?.TradeskillMaterial?.GetEntry(MaterialId);

            saveMask   = TradeskillMaterialSaveMask.None;
            commodityStoredAmount = model.Amount;
        }

        public TradeskillMaterial(
            ulong characterId,
            ushort materialId,
            IGameTableManager gameTableManager)
        {
            Owner      = characterId;
            MaterialId = materialId;
            Amount     = 0;

            Entry      = gameTableManager?.TradeskillMaterial?.GetEntry(MaterialId);

            saveMask   = TradeskillMaterialSaveMask.Create;
        }

        internal TradeskillMaterial(CharacterTradeskillMaterialModel model, IGameTableManager gameTableManager,
            Func<bool> publicationMutationAllowed) : this(model, gameTableManager)
            => this.publicationMutationAllowed = publicationMutationAllowed;

        internal TradeskillMaterial(ulong characterId, ushort materialId, IGameTableManager gameTableManager,
            Func<bool> publicationMutationAllowed) : this(characterId, materialId, gameTableManager)
            => this.publicationMutationAllowed = publicationMutationAllowed;

        public void Save(CharacterContext context) => SaveMaterial(context, null);
    }
}
