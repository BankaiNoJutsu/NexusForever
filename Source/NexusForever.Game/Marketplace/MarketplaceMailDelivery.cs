using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Mail;
using NexusForever.GameTable;
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

        public static bool IsAvailable(CharacterDatabase database)
        {
            return database != null;
        }

        public static bool TrySendItemAuctionReturnMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            IItem item,
            IPlayerManager playerManager = null)
        {
            if (item == null)
                return false;

            return TrySendMail(
                database,
                gameTableManager,
                assetManager,
                recipientCharacterId,
                SenderType.ItemAuction,
                ContentType.AuctionExpired,
                ItemAuctionReturnSubject,
                ItemAuctionReturnBody,
                0ul,
                playerManager,
                item);
        }

        public static bool TrySendItemAuctionReturnMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            IItem item,
            Action<CharacterContext> additionalSaveAction,
            IPlayerManager playerManager = null)
        {
            if (item == null)
                return false;

            return TrySendMail(
                database,
                gameTableManager,
                assetManager,
                recipientCharacterId,
                SenderType.ItemAuction,
                ContentType.AuctionExpired,
                ItemAuctionReturnSubject,
                ItemAuctionReturnBody,
                0ul,
                additionalSaveAction,
                playerManager,
                item);
        }

        public static bool TrySendItemAuctionWonMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            IItem item,
            IPlayerManager playerManager = null)
        {
            if (item == null)
                return false;

            return TrySendMail(
                database,
                gameTableManager,
                assetManager,
                recipientCharacterId,
                SenderType.ItemAuction,
                ContentType.AuctionWon,
                ItemAuctionWonSubject,
                ItemAuctionWonBody,
                0ul,
                playerManager,
                item);
        }

        public static bool TrySendItemAuctionWonMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            IItem item,
            Action<CharacterContext> additionalSaveAction,
            IPlayerManager playerManager = null)
        {
            if (item == null)
                return false;

            return TrySendMail(
                database,
                gameTableManager,
                assetManager,
                recipientCharacterId,
                SenderType.ItemAuction,
                ContentType.AuctionWon,
                ItemAuctionWonSubject,
                ItemAuctionWonBody,
                0ul,
                additionalSaveAction,
                playerManager,
                item);
        }

        public static bool TrySendCommodityAuctionFillMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            uint item2Id,
            uint quantity,
            IPlayerManager playerManager = null,
            IItemManager itemManager = null)
        {
            return TrySendCommodityAuctionFillMail(database, gameTableManager, assetManager, recipientCharacterId, item2Id, quantity, null, playerManager, itemManager);
        }

        public static bool TrySendCommodityAuctionFillMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            uint item2Id,
            uint quantity,
            Action<CharacterContext> additionalSaveAction,
            IPlayerManager playerManager = null,
            IItemManager itemManager = null)
        {
            if (quantity == 0u)
                return false;

            IItemInfo info = itemManager?.GetItemInfo(item2Id);
            if (info == null)
                return false;

            var item = new Item(recipientCharacterId, info, quantity, itemManager: itemManager, gameTableManager: gameTableManager);
            return TrySendMail(
                database,
                gameTableManager,
                assetManager,
                recipientCharacterId,
                SenderType.CommodityAuction,
                ContentType.AuctionWon,
                CommodityFillSubject,
                CommodityFillBody,
                0ul,
                additionalSaveAction,
                playerManager,
                item);
        }

        public static bool TrySendSystemItemMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            uint item2Id,
            uint quantity,
            string subject,
            string body,
            IPlayerManager playerManager = null,
            IItemManager itemManager = null)
        {
            if (quantity == 0u)
                return false;

            IItemInfo info = itemManager?.GetItemInfo(item2Id);
            if (info == null)
                return false;

            var item = new Item(recipientCharacterId, info, quantity, itemManager: itemManager, gameTableManager: gameTableManager);
            return TrySendMail(
                database,
                gameTableManager,
                assetManager,
                recipientCharacterId,
                SenderType.GM,
                ContentType.PlayerMessage,
                subject,
                body,
                0ul,
                playerManager,
                item);
        }

        public static bool TrySendMarketplaceCreditMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            ulong credits,
            string subject,
            string body,
            IPlayerManager playerManager = null)
        {
            if (credits == 0ul)
                return false;

            return TrySendMail(
                database,
                gameTableManager,
                assetManager,
                recipientCharacterId,
                SenderType.ItemAuction,
                ContentType.AuctionWon,
                subject,
                body,
                credits,
                playerManager);
        }

        public static void SaveMarketplaceCreditMail(
            CharacterContext context,
            ulong recipientCharacterId,
            ulong credits,
            string subject,
            string body,
            IGameTableManager gameTableManager = null,
            IAssetManager assetManager = null)
        {
            if (credits == 0ul)
                return;

            MailItem mail = CreateMail(
                recipientCharacterId,
                SenderType.ItemAuction,
                ContentType.AuctionWon,
                subject,
                body,
                credits,
                gameTableManager,
                assetManager);
            mail.Save(context);
        }

        public static bool TrySendCommodityAuctionReturnMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            uint item2Id,
            uint quantity,
            IPlayerManager playerManager = null,
            IItemManager itemManager = null)
        {
            return TrySendCommodityAuctionReturnMail(database, gameTableManager, assetManager, recipientCharacterId, item2Id, quantity, null, playerManager, itemManager);
        }

        public static bool TrySendCommodityAuctionReturnMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            uint item2Id,
            uint quantity,
            Action<CharacterContext> additionalSaveAction,
            IPlayerManager playerManager = null,
            IItemManager itemManager = null)
        {
            if (quantity == 0u)
                return false;

            IItemInfo info = itemManager?.GetItemInfo(item2Id);
            if (info == null)
                return false;

            var item = new Item(recipientCharacterId, info, quantity, itemManager: itemManager, gameTableManager: gameTableManager);
            return TrySendMail(
                database,
                gameTableManager,
                assetManager,
                recipientCharacterId,
                SenderType.CommodityAuction,
                ContentType.AuctionExpired,
                CommodityReturnSubject,
                CommodityReturnBody,
                0ul,
                additionalSaveAction,
                playerManager,
                item);
        }

        private static bool TrySendMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            SenderType senderType,
            ContentType contentType,
            string subject,
            string body,
            ulong credits,
            IPlayerManager playerManager,
            params IItem[] items)
        {
            return TrySendMail(
                database,
                gameTableManager,
                assetManager,
                recipientCharacterId,
                senderType,
                contentType,
                subject,
                body,
                credits,
                null,
                playerManager,
                items);
        }

        private static bool TrySendMail(
            CharacterDatabase database,
            IGameTableManager gameTableManager,
            IAssetManager assetManager,
            ulong recipientCharacterId,
            SenderType senderType,
            ContentType contentType,
            string subject,
            string body,
            ulong credits,
            Action<CharacterContext> additionalSaveAction,
            IPlayerManager playerManager,
            params IItem[] items)
        {
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
                    credits,
                    gameTableManager,
                    assetManager);

                uint index = 0;
                foreach (IItem item in items)
                {
                    if (item == null)
                        continue;

                    itemSnapshots.Add((item, item.CharacterId));
                    item.CharacterId = recipientCharacterId;
                    mail.AttachmentAdd(new MailAttachment(mail.Id, index++, item));
                }

                PersistMail(database, recipientCharacterId, mail, items, additionalSaveAction, playerManager);
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
            ulong credits,
            IGameTableManager gameTableManager,
            IAssetManager assetManager)
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
                && MarketplaceMailTexts.TryGetMarketplaceMailLocalizedTextId(out uint localizedTextId, gameTableManager))
            {
                parameters.SubjectStringId = localizedTextId;
                parameters.BodyStringId    = localizedTextId;
            }
            else
            {
                parameters.Subject = subject;
                parameters.Body    = body;
            }

            return new MailItem(parameters, assetManager);
        }

        private static void PersistMail(CharacterDatabase database, ulong recipientCharacterId, MailItem mail, IEnumerable<IItem> items)
        {
            PersistMail(database, recipientCharacterId, mail, items, null, null);
        }

        private static void PersistMail(
            CharacterDatabase database,
            ulong recipientCharacterId,
            MailItem mail,
            IEnumerable<IItem> items,
            Action<CharacterContext> additionalSaveAction,
            IPlayerManager playerManager)
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

            IPlayer recipient = playerManager?.GetPlayer(recipientCharacterId);
            recipient?.MailManager.EnqueueMail(mail);
        }
    }
}
