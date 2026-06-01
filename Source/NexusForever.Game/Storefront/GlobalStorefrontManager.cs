using System.Collections.Concurrent;
using System.Collections.Immutable;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Static.Storefront;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Storefront
{
    /// <summary>
    /// GlobalStorefrontManager provides global caching of all the store items that are sent to each player. It was made global so that reloading store items while the server is 
    /// running would be handled in a global context.
    /// </summary>
    public sealed class GlobalStorefrontManager : Singleton<GlobalStorefrontManager>, IGlobalStorefrontManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentDictionary<string, byte> catalogDeliveredSessionIds = new();
        private readonly ConcurrentDictionary<uint, byte> accountsCatalogRequestedBeforeWorldLogin = new();

        private ImmutableDictionary<uint, ICategory> storeCategories;
        private ImmutableList<ServerStoreCategories.StoreCategory> serverStoreCategoryCache;

        private ImmutableDictionary<uint, IOfferGroup> offerGroups;
        private ImmutableList<ServerStoreOffers.OfferGroup> serverStoreOfferGroupCache;

        private ImmutableDictionary</*offerId*/uint, /*offerGroupId*/uint> offerGroupLookup;

        public void Initialise()
        {
            InitialiseStoreCategories();
            InitialiseStoreOfferGroups();

            BuildNetworkPackets();

            ValidateCachedStoreCategoriesPacket();
            StorefrontWireConstraintValidator.Validate(serverStoreCategoryCache, serverStoreOfferGroupCache);

            log.Info($"Initialised {storeCategories.Count} categories with {offerGroups.Count} offers groups.");
        }

        private void InitialiseStoreCategories()
        {
            ImmutableList<StoreCategoryModel> storeCategoryModels = DatabaseManager.Instance
                .GetDatabase<WorldDatabase>()
                .GetStoreCategories();

            storeCategories = BuildStoreCategories(storeCategoryModels);
        }

        private static ImmutableDictionary<uint, ICategory> BuildStoreCategories(IEnumerable<StoreCategoryModel> storeCategoryModels)
        {
            List<StoreCategoryModel> orderedModels = storeCategoryModels
                .OrderBy(category => category.Id)
                .ToList();

            var builder = ImmutableDictionary.CreateBuilder<uint, ICategory>();
            foreach (StoreCategoryModel category in orderedModels.Where(category => Convert.ToBoolean(category.Visible)))
                // StorefrontLib.GetCategoryTree roots the visible catalog under the retail hidden
                // root category id from GameFormula (26 locally), so preserve original parent ids.
                builder.Add(category.Id, new Category(category));

            return builder.ToImmutable();
        }

        private void InitialiseStoreOfferGroups()
        {
            IEnumerable<StoreOfferGroupModel> offerGroupModels = DatabaseManager.Instance.GetDatabase<WorldDatabase>().GetStoreOfferGroups()
                .OrderBy(i => i.Id)
                .Where(x => Convert.ToBoolean(x.Visible));

            var offerGroupBuilder = ImmutableDictionary.CreateBuilder<uint, IOfferGroup>();
            var offerBuilder      = ImmutableDictionary.CreateBuilder<uint, uint>();
            foreach (StoreOfferGroupModel offerGroup in offerGroupModels)
            {
                var group = new OfferGroup(offerGroup);
                if (!group.HasOffers)
                    continue;

                offerGroupBuilder.Add(offerGroup.Id, group);

                foreach (StoreOfferItemModel offerItem in offerGroup.StoreOfferItem.Where(i => Convert.ToBoolean(i.Visible)))
                    offerBuilder.Add(offerItem.Id, offerGroup.Id); // Cache the offer item's group ID, to lookup the entry.
            }
                
            offerGroups      = offerGroupBuilder.ToImmutable();
            offerGroupLookup = offerBuilder.ToImmutable();
        }

        private void BuildNetworkPackets()
        {
            serverStoreCategoryCache = BuildStoreCategoryPacketCache(storeCategories.Values);
            Dictionary<uint, int> categoryOrderLookup = serverStoreCategoryCache
                .Select((category, index) => (CategoryId: category.CategoryId, Index: index))
                .ToDictionary(category => category.CategoryId, category => category.Index);

            serverStoreOfferGroupCache = offerGroups.Values
                .Select(offerGroup => new
                {
                    OfferGroup = offerGroup,
                    SortKey    = GetOfferGroupSortKey(offerGroup, categoryOrderLookup)
                })
                .OrderBy(entry => entry.SortKey.CategoryOrder)
                .ThenBy(entry => entry.SortKey.CategoryIndex)
                .ThenBy(entry => entry.OfferGroup.Id)
                .Select(entry => entry.OfferGroup.Build())
                .ToImmutableList();

            ValidateCachedStoreOfferBatches();
        }

        private static (int CategoryOrder, uint CategoryIndex) GetOfferGroupSortKey(
            IOfferGroup offerGroup,
            IReadOnlyDictionary<uint, int> categoryOrderLookup)
        {
            int selectedCategoryOrder = int.MaxValue;
            uint selectedCategoryIndex = uint.MaxValue;

            foreach (IOfferGroupCategory category in offerGroup.Categories)
            {
                int categoryOrder = categoryOrderLookup.TryGetValue(category.Id, out int order)
                    ? order
                    : int.MaxValue;
                if (categoryOrder > selectedCategoryOrder)
                    continue;
                if (categoryOrder == selectedCategoryOrder && category.Index >= selectedCategoryIndex)
                    continue;

                selectedCategoryOrder = categoryOrder;
                selectedCategoryIndex = category.Index;
            }

            return (selectedCategoryOrder, selectedCategoryIndex);
        }

        private void ValidateCachedStoreCategoriesPacket()
        {
            List<ServerStoreCategories.CurrencyPackage> currencyPackages = VirtualCurrencyPackageCatalog.BuildCatalogRows().ToList();
            var message = new ServerStoreCategories
            {
                StoreCategories  = serverStoreCategoryCache.ToList(),
                RealCurrency     = RealCurrency.Usd,
                CurrencyPackages = currencyPackages
            };

            byte[] body = GetBodyBytes(message);
            using var stream = new MemoryStream(body);
            using var reader = new GamePacketReader(stream);
            if (!RetailStoreCategoriesWireReader.TryRead(reader, out string failure))
            {
                log.Error($"StorefrontCatalogDiagnostics retail categories wire validation failed " +
                    $"failure={failure} bodyBytes={body.Length}.");
            }
        }

        private void ValidateCachedStoreOfferBatches()
        {
            var batch = new ServerStoreOffers();
            int packetIndex = 0;

            foreach (ServerStoreOffers.OfferGroup offerGroup in serverStoreOfferGroupCache)
            {
                batch.OfferGroups.Add(offerGroup);

                if (batch.OfferGroups.Count != 20)
                    continue;

                packetIndex++;
                if (!TryValidateRetailStoreOffersBody(batch, out string failure, out uint groupId, out uint offerId))
                {
                    log.Error($"StorefrontCatalogDiagnostics retail wire validation failed at startup packet={packetIndex} " +
                        $"groupId={groupId} offerId={offerId} failure={failure} bodyBytes={GetBodyByteCount(batch)}.");
                }

                batch = new ServerStoreOffers();
            }

            if (batch.OfferGroups.Count != 0)
            {
                packetIndex++;
                if (!TryValidateRetailStoreOffersBody(batch, out string failure, out uint groupId, out uint offerId))
                {
                    log.Error($"StorefrontCatalogDiagnostics retail wire validation failed at startup packet={packetIndex} " +
                        $"groupId={groupId} offerId={offerId} failure={failure} bodyBytes={GetBodyByteCount(batch)}.");
                }
            }
        }

        private static ImmutableList<ServerStoreCategories.StoreCategory> BuildStoreCategoryPacketCache(IEnumerable<ICategory> categories)
        {
            List<ICategory> categoryList = categories.ToList();
            Dictionary<uint, List<ICategory>> childLookup = categoryList
                .GroupBy(category => category.ParentCategoryId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(category => category.Index)
                        .ThenBy(category => category.Id)
                        .ToList());

            var remainingCategoryIds = categoryList
                .Select(category => category.Id)
                .ToHashSet();

            var builder = ImmutableList.CreateBuilder<ServerStoreCategories.StoreCategory>();
            AddChildren(0u);

            foreach (ICategory category in categoryList
                .Where(category => remainingCategoryIds.Contains(category.Id))
                .OrderBy(category => category.ParentCategoryId)
                .ThenBy(category => category.Index)
                .ThenBy(category => category.Id))
            {
                AddCategorySubtree(category);
            }

            return builder.ToImmutable();

            void AddChildren(uint parentCategoryId)
            {
                if (!childLookup.TryGetValue(parentCategoryId, out List<ICategory> children))
                    return;

                foreach (ICategory child in children)
                    AddCategorySubtree(child);
            }

            void AddCategorySubtree(ICategory category)
            {
                if (!remainingCategoryIds.Remove(category.Id))
                    return;

                builder.Add(category.Build());
                AddChildren(category.Id);
            }
        }

        /// <summary>
        /// Return the <see cref="IOfferItem"/> that matches the supplied offer ID
        /// </summary>
        public IOfferItem GetStoreOfferItem(uint offerId)
        {
            if (!offerGroupLookup.TryGetValue(offerId, out uint offerGroupId))
                return null;

            return offerGroups.TryGetValue(offerGroupId, out IOfferGroup offerGroup) ? offerGroup.GetOfferItem(offerId) : null;
        }

        /// <summary>
        /// This method is used to send the current Store Catalog to the <see cref="IGameSession"/>
        /// </summary>
        public void HandleCatalogRequest(IGameSession session, uint accountId)
        {
            // Retail answers 0x082D with 0x0988/0x098B/0x0987 only. Prepending 0x0989 here caused the
            // client to re-request 0x082D in a loop whenever we had already sent a dirty notification.
            log.Info($"StorefrontCatalogDiagnostics catalog start refresh=False categories={serverStoreCategoryCache.Count} " +
                $"offerGroups={serverStoreOfferGroupCache.Count} offers={serverStoreOfferGroupCache.Sum(group => group.Offers.Count)} " +
                $"itemRows={serverStoreOfferGroupCache.Sum(group => group.Offers.Sum(offer => offer.ItemData.Count))}.");

            SendCatalogToSession(session, accountId, isRefresh: false);

            log.Info("StorefrontCatalogDiagnostics catalog end.");
        }

        public void MarkAccountCatalogRequestedBeforeWorldLogin(uint accountId)
        {
            if (accountId != 0)
                accountsCatalogRequestedBeforeWorldLogin[accountId] = 0;
        }

        public void SendBootstrapCatalogPacketsIfNeeded(IGameSession session, uint accountId)
        {
            string deliveryKey = GetCatalogDeliveryKey(session, accountId);

            bool catalogAlreadyDelivered = WasCatalogDelivered(session, accountId);
            bool requestedBeforeWorldLogin = accountId != 0u &&
                accountsCatalogRequestedBeforeWorldLogin.TryRemove(accountId, out _);
            string reason = catalogAlreadyDelivered
                ? "after prior catalog delivery so the in-world UI receives StoreCatalogReady"
                : requestedBeforeWorldLogin
                    ? "after a pre-world catalog request did not complete"
                    : string.Empty;
            log.Info(string.IsNullOrEmpty(reason)
                ? $"StorefrontCatalogDiagnostics sending in-world bootstrap catalog for {deliveryKey}."
                : $"StorefrontCatalogDiagnostics sending in-world bootstrap catalog for {deliveryKey} {reason}.");
            SendCatalogToSession(session, accountId, isRefresh: false);
        }

        public void ClearCatalogDeliveryState(string sessionId, uint accountId = 0)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                catalogDeliveredSessionIds.TryRemove(sessionId, out _);
                catalogDeliveredSessionIds.TryRemove($"session:{sessionId}", out _);
            }

            if (accountId != 0)
            {
                catalogDeliveredSessionIds.TryRemove(GetAccountCatalogDeliveryKey(accountId), out _);
                accountsCatalogRequestedBeforeWorldLogin.TryRemove(accountId, out _);
            }
        }

        public void SendCatalogPackets(IGameSession session, uint accountId = 0)
        {
            SendCatalogToSession(session, accountId, isRefresh: false);
        }

        private void SendCatalogToSession(IGameSession session, uint accountId, bool isRefresh)
        {
            if (isRefresh)
                session.EnqueueMessageEncrypted(new ServerStoreCatalogUpdated());

            log.Info($"StorefrontCatalogDiagnostics catalog packets only refresh={isRefresh} categories={serverStoreCategoryCache.Count} " +
                $"offerGroups={serverStoreOfferGroupCache.Count} offers={serverStoreOfferGroupCache.Sum(group => group.Offers.Count)}.");

            SendStoreCategories(session);
            SendStoreOffers(session);

            // WildStar64 treats 0x0989 as a dirty notification, so the initial catalog response
            // must end with ServerStoreFinalise and reserve ServerStoreCatalogUpdated for real refreshes.
            SendStoreFinalise(session);
            MarkCatalogDelivered(session, accountId);
        }

        private bool WasCatalogDelivered(IGameSession session, uint accountId)
        {
            return catalogDeliveredSessionIds.ContainsKey(GetCatalogDeliveryKey(session, accountId));
        }

        private void MarkCatalogDelivered(IGameSession session, uint accountId)
        {
            catalogDeliveredSessionIds[GetCatalogDeliveryKey(session, accountId)] = 0;

            if (accountId != 0)
                accountsCatalogRequestedBeforeWorldLogin.TryRemove(accountId, out _);
        }

        private static string GetCatalogDeliveryKey(IGameSession session, uint accountId)
        {
            if (accountId != 0)
                return GetAccountCatalogDeliveryKey(accountId);

            if (session is INetworkSession networkSession)
                return $"session:{networkSession.Id}";

            return $"session:{session.GetHashCode()}";
        }

        private static string GetAccountCatalogDeliveryKey(uint accountId) => $"account:{accountId}";

        private void SendStoreCategories(IGameSession session)
        {
            List<ServerStoreCategories.CurrencyPackage> currencyPackages = VirtualCurrencyPackageCatalog.BuildCatalogRows().ToList();
            string categoryParents = string.Join(",", serverStoreCategoryCache.Select(category => $"{category.CategoryId}->{category.ParentCategoryId}"));

            var message = new ServerStoreCategories
            {
                StoreCategories  = serverStoreCategoryCache.ToList(),
                RealCurrency     = RealCurrency.Usd,
                CurrencyPackages = currencyPackages
            };

            log.Info($"StorefrontCatalogDiagnostics sending ServerStoreCategories categories={serverStoreCategoryCache.Count} " +
                $"categoryIds=[{string.Join(",", serverStoreCategoryCache.Select(category => category.CategoryId))}] " +
                $"categoryParents=[{categoryParents}] " +
                $"currencyPackages={currencyPackages.Count} packageIds=[{string.Join(",", currencyPackages.Select(package => package.Id))}] " +
                $"bodyBytes={GetBodyByteCount(message)}.");

            session.EnqueueMessageEncrypted(message);
        }

        private void SendStoreOffers(IGameSession session)
        {
            var storeOffers = new ServerStoreOffers();
            int packetIndex = 0;
            foreach (ServerStoreOffers.OfferGroup offerGroup in serverStoreOfferGroupCache)
            {
                storeOffers.OfferGroups.Add(offerGroup);

                // Ensure we only send 20 Offer Groups per packet. This is the same as Live.
                if (storeOffers.OfferGroups.Count == 20)
                {
                    LogStoreOffersPacket(++packetIndex, storeOffers);
                    session.EnqueueMessageEncrypted(storeOffers);
                    storeOffers = new ServerStoreOffers();
                }
            }

            if (storeOffers.OfferGroups.Count != 0)
            {
                LogStoreOffersPacket(++packetIndex, storeOffers);
                session.EnqueueMessageEncrypted(storeOffers);
            }
        }

        private void SendStoreFinalise(IGameSession session)
        {
            log.Info("StorefrontCatalogDiagnostics sending ServerStoreFinalise.");
            session.EnqueueMessageEncrypted(new ServerStoreFinalise());
        }

        private static void LogStoreOffersPacket(int packetIndex, ServerStoreOffers storeOffers)
        {
            int bodyBytes = GetBodyByteCount(storeOffers);
            if (!TryValidateRetailStoreOffersBody(storeOffers, out string failure, out uint groupId, out uint offerId))
            {
                log.Error($"StorefrontCatalogDiagnostics retail wire validation failed before send packet={packetIndex} " +
                    $"groupId={groupId} offerId={offerId} failure={failure} bodyBytes={bodyBytes}.");
            }

            log.Info($"StorefrontCatalogDiagnostics sending ServerStoreOffers packet={packetIndex} " +
                $"groups={storeOffers.OfferGroups.Count} groupIds=[{string.Join(",", storeOffers.OfferGroups.Select(group => group.Id))}] " +
                $"offers={storeOffers.OfferGroups.Sum(group => group.Offers.Count)} " +
                $"itemRows={storeOffers.OfferGroups.Sum(group => group.Offers.Sum(offer => offer.ItemData.Count))} " +
                $"currencyRows={storeOffers.OfferGroups.Sum(group => group.Offers.Sum(offer => offer.CurrencyData.Count))} " +
                $"categoryLinks={storeOffers.OfferGroups.Sum(group => group.Categories.Count)} " +
                $"bodyBytes={bodyBytes}.");
        }

        private static bool TryValidateRetailStoreOffersBody(
            ServerStoreOffers storeOffers,
            out string failure,
            out uint groupId,
            out uint offerId)
        {
            byte[] body = GetBodyBytes(storeOffers);
            using var stream = new MemoryStream(body);
            using var reader = new GamePacketReader(stream);
            return RetailStoreOffersWireReader.TryRead(reader, out failure, out groupId, out offerId);
        }

        private static byte[] GetBodyBytes(IWritable message)
        {
            using var stream = new MemoryStream();
            using var writer = new GamePacketWriter(stream);

            message.Write(writer);
            writer.FlushBits();

            return stream.ToArray();
        }

        private static int GetBodyByteCount(IWritable message)
        {
            return GetBodyBytes(message).Length;
        }
    }
}
