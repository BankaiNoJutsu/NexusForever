using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Entity
{
    public partial class TradeskillMaterial
    {
        // Existing writer retained inside the typed commodity witness/receipt.
        // Ordinary material saves use the terminal-intent owner instead.
        internal void SaveCommodity(CharacterContext context, SupplySatchelManager.CommodityReturnReservation owner)
        {
            if (!owner.OwnsMaterial(this) || materialSave != null || uncertainAmounts != null)
                throw new InvalidOperationException("Commodity material cannot reuse another save owner.");
            if (saveMask == TradeskillMaterialSaveMask.None)
                return;

            if ((saveMask & TradeskillMaterialSaveMask.Create) != 0)
            {
                var model = new CharacterTradeskillMaterialModel
                {
                    Id         = Owner,
                    MaterialId = MaterialId,
                    Amount     = amount
                };

                context.Add(model);
            }
            else
            {
                var model = new CharacterTradeskillMaterialModel
                {
                    Id         = Owner,
                    MaterialId = MaterialId
                };

                var tracked = context.CharacterTradeskillMaterial.Local.SingleOrDefault(row => row.Id == Owner && row.MaterialId == MaterialId);
                EntityEntry<CharacterTradeskillMaterialModel> entity = tracked == null ? context.Attach(model) : context.Entry(tracked);
                model = entity.Entity;
                if ((saveMask & TradeskillMaterialSaveMask.Amount) != 0)
                {
                    model.Amount = amount;
                    entity.Property(p => p.Amount).IsModified = true;
                }
            }

            ushort captured = amount;
            context.OnSaveSettled(committed => { if (committed) commodityStoredAmount = captured; });
            saveMask = TradeskillMaterialSaveMask.None;
        }
    }
}
