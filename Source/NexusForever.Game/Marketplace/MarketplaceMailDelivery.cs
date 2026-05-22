using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Mail;
using NexusForever.Shared;

namespace NexusForever.Game.Marketplace
{
    /// <summary>
    /// Delivers marketplace items and credits through the mail system when inventory delivery is not possible.
    /// </summary>
    internal static class MarketplaceMailDelivery
    {
        private const string ItemAuctionReturnSubject = "Auction House";
        private const string ItemAuctionReturnBody    = "Your listing has expired. The attached item has been returned.";
        private const string ItemAuctionWonSubject    = "Auction House";
        private const string ItemAuctionWonBody       = "You won an auction. The attached item is enclosed.";
        private const string CommodityFillSubject     = "Commodity Exchange";
        private const string CommodityFillBody        = "Your commodity order has been filled. Attached are the purchased items.";
        private const string CommodityReturnSubject   = "Commodity Exchange";
        private const string CommodityReturnBody      = "Your commodity order was cancelled. The listed items have been returned.";

        public static bool IsAvailable => TryGetCharacterDatabase() != null;

        public static bool TrySendItemAuctionReturnMail(ulong recipientCharacterId, IItem item)
        {
            if (item == null)
                return false;

            return TrySendMail(
                recipientCharacterId,
                SenderType.ItemAuction,
                ContentType.AuctionExpired,
                ItemAuctionReturnSubject,
                ItemAuctionReturnBody,
                0ul,
                item);
        }

        public static bool TrySendItemAuctionWonMail(ulong recipientCharacterId, IItem item)
        {
            if (item == null)
                return false;

            return TrySendMail(
                recipientCharacterId,
                SenderType.ItemAuction,
                ContentType.AuctionWon,
                ItemAuctionWonSubject,
                ItemAuctionWonBody,
                0ul,
                item);
        }

        public static bool TrySendCommodityAuctionFillMail(ulong recipientCharacterId, uint item2Id, uint quantity)
        {
            if (quantity == 0u)
                return false;

            IItemInfo info = ItemManager.Instance.GetItemInfo(item2Id);
            if (info == null)
                return false;

            var item = new Item(recipientCharacterId, info, quantity);
            return TrySendMail(
                recipientCharacterId,
                SenderType.CommodityAuction,
                ContentType.AuctionWon,
                CommodityFillSubject,
                CommodityFillBody,
                0ul,
                item);
        }

        public static bool TrySendCommodityAuctionReturnMail(ulong recipientCharacterId, uint item2Id, uint quantity)
        {
            if (quantity == 0u)
                return false;

            IItemInfo info = ItemManager.Instance.GetItemInfo(item2Id);
            if (info == null)
                return false;

            var item = new Item(recipientCharacterId, info, quantity);
            return TrySendMail(
                recipientCharacterId,
                SenderType.CommodityAuction,
                ContentType.AuctionExpired,
                CommodityReturnSubject,
                CommodityReturnBody,
                0ul,
                item);
        }

        private static bool TrySendMail(
            ulong recipientCharacterId,
            SenderType senderType,
            ContentType contentType,
            string subject,
            string body,
            ulong credits,
            params IItem[] items)
        {
            CharacterDatabase database = TryGetCharacterDatabase();
            if (database == null)
                return false;

            var parameters = new MailParameters
            {
                RecipientCharacterId = recipientCharacterId,
                MessageType          = senderType,
                ContentType          = contentType,
                MoneyToGive          = credits,
                DeliverySpeed        = DeliverySpeed.Instant
            };

            if ((senderType == SenderType.ItemAuction || senderType == SenderType.CommodityAuction)
                && MarketplaceMailTexts.TryGetMarketplaceMailLocalizedTextId(out uint localizedTextId))
            {
                parameters.SubjectStringId = localizedTextId;
                parameters.BodyStringId    = localizedTextId;
            }
            else
            {
                parameters.Subject = subject;
                parameters.Body    = body;
            }

            var mail = new MailItem(parameters);

            uint index = 0;
            foreach (IItem item in items)
            {
                if (item == null)
                    continue;

                item.CharacterId = recipientCharacterId;
                mail.AttachmentAdd(new MailAttachment(mail.Id, index++, item));
            }

            PersistMail(database, recipientCharacterId, mail, items);

            return true;
        }

        private static void PersistMail(CharacterDatabase database, ulong recipientCharacterId, MailItem mail, IEnumerable<IItem> items)
        {
            database.Save(context =>
            {
                mail.Save(context);

                foreach (IItem item in items)
                {
                    if (item is Item itemEntity)
                        itemEntity.Save(context);
                }
            }).GetAwaiter().GetResult();

            IPlayer recipient = PlayerManager.Instance.GetPlayer(recipientCharacterId);
            recipient?.MailManager.EnqueueMail(mail);
        }

        private static CharacterDatabase TryGetCharacterDatabase()
        {
            if (LegacyServiceProvider.Provider?.GetService<DatabaseManager>() is not DatabaseManager manager)
                return null;

            return manager.GetDatabase<CharacterDatabase>();
        }
    }
}
