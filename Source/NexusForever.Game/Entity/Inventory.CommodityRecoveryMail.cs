using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Mail;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        // A fixed delivery for accepted later grants when inventory custody
        // requires reload. It is enrolled in the same complete ordinary receipt.
        internal sealed class CommodityRecoveryMail(string database, MailHeaderState header, GlobalMarketplaceManager.BidItem[] items)
        {
            internal void Stage(CharacterContext context)
            {
                CharacterDatabaseIdentity.Require(context, database);
                if (context.AtomicCharacterId != header.RecipientId || context.Database.CurrentTransaction == null
                    || items.Length is 0 or > 10 || items.Any(item => item.Owner != header.RecipientId
                        || item.Location != (ushort)InventoryLocation.None || item.Bag != 0 || item.Id == 0 || item.Quantity == 0)
                    || !Matches(context, false))
                    throw new InvalidOperationException("Deferred commodity delivery lost its captured creation preimages.");
                context.CharacterMail.Add(header.Model());
                for (uint index = 0; index < items.Length; index++)
                {
                    context.Item.Add(items[index].Model());
                    context.CharacterMailAttachment.Add(new() { Id = header.Id, Index = index, ItemGuid = items[index].Id });
                }
                context.OnSaveValidation(phase =>
                {
                    if (!Matches(context, phase != CharacterSaveValidationPhase.BeforeFlush))
                        throw new InvalidOperationException("Deferred commodity delivery differs from its fixed custody graph.");
                });
            }

            private bool Matches(CharacterContext context, bool committed)
            {
                if (MailHeaderState.From(context.CharacterMail.AsNoTracking().SingleOrDefault(row => row.Id == header.Id)) != (committed ? header : null)) return false;
                var links = context.CharacterMailAttachment.AsNoTracking().Where(row => row.Id == header.Id).ToArray();
                if (links.Length != (committed ? items.Length : 0)) return false;
                for (uint index = 0; index < items.Length; index++)
                {
                    var item = items[index];
                    if (GlobalMarketplaceManager.BidItem.Capture(context.Item.AsNoTracking().SingleOrDefault(row => row.Id == item.Id)) != (committed ? item : null)
                        || context.RealmBankItem.Any(row => row.Id == item.Id) || context.MarketplaceAuction.Any(row => row.ItemId == item.Id)
                        || context.CharacterMailAttachment.Any(row => row.ItemGuid == item.Id && (!committed || row.Id != header.Id || row.Index != index))
                        || committed && !links.Any(link => link.Index == index && link.ItemGuid == item.Id)) return false;
                }
                return true;
            }
        }
    }
}
