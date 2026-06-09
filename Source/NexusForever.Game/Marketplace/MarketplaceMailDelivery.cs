using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Mail;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Marketplace
{
    /// <summary>
    /// Delivers marketplace items and credits through the mail system when inventory delivery is not possible.
    /// </summary>
    internal static class MarketplaceMailDelivery
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

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

        public static bool TrySendItemAuctionReturnMail(
            ulong recipientCharacterId,
            IItem item,
            Action<CharacterContext> additionalSaveAction)
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
                additionalSaveAction,
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

        public static bool TrySendItemAuctionWonMail(
            ulong recipientCharacterId,
            IItem item,
            Action<CharacterContext> additionalSaveAction)
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
                additionalSaveAction,
                item);
        }

        public static bool TrySendCommodityAuctionFillMail(ulong recipientCharacterId, uint item2Id, uint quantity)
        {
            return TrySendCommodityAuctionFillMail(recipientCharacterId, item2Id, quantity, null);
        }

        public static bool TrySendCommodityAuctionFillMail(
            ulong recipientCharacterId,
            uint item2Id,
            uint quantity,
            Action<CharacterContext> additionalSaveAction)
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
                additionalSaveAction,
                item);
        }

        public static bool TrySendSystemItemMail(
            ulong recipientCharacterId,
            uint item2Id,
            uint quantity,
            string subject,
            string body)
        {
            if (quantity == 0u)
                return false;

            IItemInfo info = ItemManager.Instance.GetItemInfo(item2Id);
            if (info == null)
                return false;

            var item = new Item(recipientCharacterId, info, quantity);
            return TrySendMail(
                recipientCharacterId,
                SenderType.GM,
                ContentType.PlayerMessage,
                subject,
                body,
                0ul,
                item);
        }

        public static bool TrySendMarketplaceCreditMail(
            ulong recipientCharacterId,
            ulong credits,
            string subject,
            string body)
        {
            if (credits == 0ul)
                return false;

            return TrySendMail(
                recipientCharacterId,
                SenderType.ItemAuction,
                ContentType.AuctionWon,
                subject,
                body,
                credits);
        }

        public static void SaveMarketplaceCreditMail(
            CharacterContext context,
            ulong recipientCharacterId,
            ulong credits,
            string subject,
            string body)
        {
            if (credits == 0ul)
                return;

            MailItem mail = CreateMail(
                recipientCharacterId,
                SenderType.ItemAuction,
                ContentType.AuctionWon,
                subject,
                body,
                credits);
            mail.Save(context);
        }

        public static bool TrySendCommodityAuctionReturnMail(ulong recipientCharacterId, uint item2Id, uint quantity)
        {
            return TrySendCommodityAuctionReturnMail(recipientCharacterId, item2Id, quantity, null);
        }

        public static bool TrySendCommodityAuctionReturnMail(
            ulong recipientCharacterId,
            uint item2Id,
            uint quantity,
            Action<CharacterContext> additionalSaveAction)
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
                additionalSaveAction,
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
            return TrySendMail(
                recipientCharacterId,
                senderType,
                contentType,
                subject,
                body,
                credits,
                null,
                items);
        }

        private static bool TrySendMail(
            ulong recipientCharacterId,
            SenderType senderType,
            ContentType contentType,
            string subject,
            string body,
            ulong credits,
            Action<CharacterContext> additionalSaveAction,
            params IItem[] items)
        {
            CharacterDatabase database = TryGetCharacterDatabase();
            if (database == null)
                return false;

            List<(IItem Item, ulong? CharacterId)> itemSnapshots = [];
            try
            {
                MailItem mail = CreateMail(
                    recipientCharacterId,
                    senderType,
                    contentType,
                    subject,
                    body,
                    credits);

                uint index = 0;
                foreach (IItem item in items)
                {
                    if (item == null)
                        continue;

                    itemSnapshots.Add((item, item.CharacterId));
                    item.CharacterId = recipientCharacterId;
                    mail.AttachmentAdd(new MailAttachment(mail.Id, index++, item));
                }

                PersistMail(database, recipientCharacterId, mail, items, additionalSaveAction);
            }
            catch (Exception ex)
            {
                foreach ((IItem item, ulong? characterId) in itemSnapshots)
                    item.CharacterId = characterId;

                log.Error(ex, "MarketplaceMailDelivery failed to persist marketplace mail for recipient {0}.", recipientCharacterId);
                return false;
            }

            return true;
        }

        private static MailItem CreateMail(
            ulong recipientCharacterId,
            SenderType senderType,
            ContentType contentType,
            string subject,
            string body,
            ulong credits)
        {
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

            return new MailItem(parameters);
        }

        private static void PersistMail(CharacterDatabase database, ulong recipientCharacterId, MailItem mail, IEnumerable<IItem> items)
        {
            PersistMail(database, recipientCharacterId, mail, items, null);
        }

        private static void PersistMail(
            CharacterDatabase database,
            ulong recipientCharacterId,
            MailItem mail,
            IEnumerable<IItem> items,
            Action<CharacterContext> additionalSaveAction)
        {
            database.SaveBlocking(context =>
            {
                additionalSaveAction?.Invoke(context);

                mail.Save(context);

                foreach (IItem item in items)
                {
                    if (item is Item itemEntity)
                        itemEntity.Save(context);
                }
            });

            IPlayer recipient = PlayerManager.Instance.GetPlayer(recipientCharacterId);
            recipient?.MailManager.EnqueueMail(mail);
        }

        private static CharacterDatabase TryGetCharacterDatabase()
        {
            IDatabaseManager manager = LegacyServiceProvider.Provider?.GetService<IDatabaseManager>()
                ?? LegacyServiceProvider.Provider?.GetService<DatabaseManager>();

            return manager?.GetDatabase<CharacterDatabase>();
        }
    }
}
