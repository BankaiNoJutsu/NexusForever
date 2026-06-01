using Microsoft.EntityFrameworkCore;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Storefront;
using NexusForever.Game.Storefront;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Storefront;

public class StorefrontCatalogDatabaseWireTests
{
    [Fact]
    public void LiveWorldDatabase_AllOfferBatches_PassRetailWireReader()
    {
        string connectionString = Environment.GetEnvironmentVariable("NEXUS_FOREVER_WORLD_DB");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        List<StoreOfferGroupModel> groupModels;
        try
        {
            var options = new DbContextOptionsBuilder<WorldContext>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                .Options;

            using var context = new WorldContext(options);
            groupModels = context.StoreOfferGroup
                .Include(e => e.StoreOfferGroupCategory)
                .Include(e => e.StoreOfferItem)
                    .ThenInclude(e => e.StoreOfferItemData)
                .Include(e => e.StoreOfferItem)
                    .ThenInclude(e => e.StoreOfferItemPrice)
                .AsNoTracking()
                .OrderBy(e => e.Id)
                .Where(e => e.Visible == 1)
                .ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"World database unavailable; set NEXUS_FOREVER_WORLD_DB or start MySQL. {ex.Message}",
                ex);
        }

        List<ServerStoreOffers.OfferGroup> offerGroups = groupModels
            .Select(BuildOfferGroup)
            .Where(group => group.Offers.Count != 0)
            .ToList();

        var batch = new ServerStoreOffers();
        int packetIndex = 0;
        var failures = new List<string>();

        foreach (ServerStoreOffers.OfferGroup offerGroup in offerGroups)
        {
            batch.OfferGroups.Add(offerGroup);

            if (batch.OfferGroups.Count != 20)
                continue;

            packetIndex++;
            RecordBatchValidation(packetIndex, batch, failures);
            batch = new ServerStoreOffers();
        }

        if (batch.OfferGroups.Count != 0)
        {
            packetIndex++;
            RecordBatchValidation(packetIndex, batch, failures);
        }

        Assert.Empty(failures);
    }

    private static void RecordBatchValidation(int packetIndex, ServerStoreOffers batch, List<string> failures)
    {
        byte[] body = SerializeBody(batch);

        using var stream = new MemoryStream(body);
        using var reader = new GamePacketReader(stream);

        if (!RetailStoreOffersWireReader.TryRead(reader, out string failure, out uint groupId, out uint offerId))
        {
            failures.Add($"packet={packetIndex} groupId={groupId} offerId={offerId} failure={failure} " +
                $"bodyBytes={body.Length} groupIds=[{string.Join(",", batch.OfferGroups.Select(g => g.Id))}]");
            return;
        }

        if (reader.BytesRemaining != 0)
        {
            failures.Add($"packet={packetIndex} trailingBytes={reader.BytesRemaining} bodyBytes={body.Length} " +
                $"groupIds=[{string.Join(",", batch.OfferGroups.Select(g => g.Id))}]");
        }
    }

    private static ServerStoreOffers.OfferGroup BuildOfferGroup(StoreOfferGroupModel model)
    {
        var group = new ServerStoreOffers.OfferGroup
        {
            Id                  = model.Id,
            DisplayFlags        = (DisplayFlag)model.DisplayFlags,
            Name                = model.Name,
            Description         = model.Description,
            DisplayInfoOverride = model.DisplayInfoOverride,
            Categories          = model.StoreOfferGroupCategory
                .Where(category => category.Visible == 1)
                .OrderBy(category => category.Index)
                .ThenBy(category => category.CategoryId)
                .Select(category => new ServerStoreOffers.OfferGroup.Category
                {
                    Id    = category.CategoryId,
                    Index = category.Index
                })
                .ToList()
        };

        foreach (StoreOfferItemModel offerModel in model.StoreOfferItem
            .Where(offer => offer.Visible == 1)
            .OrderBy(offer => offer.Id))
        {
            group.Offers.Add(BuildOffer(offerModel));
        }

        return group;
    }

    private static ServerStoreOffers.OfferGroup.Offer BuildOffer(StoreOfferItemModel model)
    {
        float pricePremium     = 0f;
        float priceAlternative = 0f;

        foreach (StoreOfferItemPriceModel price in model.StoreOfferItemPrice.OrderBy(price => price.CurrencyId))
        {
            float headline = GetCurrencyValue(price);
            if (price.CurrencyId == (byte)AccountCurrencyType.Protobuck)
                pricePremium = headline;
            if (price.CurrencyId == (byte)AccountCurrencyType.Omnibit)
                priceAlternative = headline;
        }

        var offer = new ServerStoreOffers.OfferGroup.Offer
        {
            Id                            = model.Id,
            Name                          = model.Name,
            Description                   = model.Description,
            DisplayFlags                  = (DisplayFlag)model.DisplayFlags,
            PricePremium                  = pricePremium,
            PriceAlternative              = priceAlternative,
            RetailCatalogWireScalar       = model.RetailCatalogWireScalar,
            RetailCatalogWireByte = model.RetailCatalogWireByte
        };

        foreach (StoreOfferItemPriceModel price in model.StoreOfferItemPrice.OrderBy(price => price.CurrencyId))
        {
            offer.CurrencyData.Add(new OfferItemPrice(price).Build());
        }

        foreach (StoreOfferItemDataModel itemData in model.StoreOfferItemData
            .OrderBy(itemData => itemData.Type)
            .ThenBy(itemData => itemData.ItemId)
            .ThenBy(itemData => itemData.Amount))
        {
            offer.ItemData.Add(new ServerStoreOffers.OfferGroup.Offer.OfferItemData
            {
                Type              = itemData.Type,
                AccountItemId     = (ushort)itemData.ItemId,
                Amount            = itemData.Amount,
                Type1AccountItemId = itemData.ItemId,
                Type1Amount        = itemData.Amount,
                Type2AccountItemId = itemData.ItemId
            });
        }

        return offer;
    }

    private static float GetCurrencyValue(StoreOfferItemPriceModel price)
    {
        if (price.DiscountValue == 0f)
            return (float)Math.Ceiling(price.Price);

        return (float)Math.Ceiling(price.Price / ((100f - price.DiscountValue) / 100f));
    }

    private static byte[] SerializeBody(ServerStoreOffers packet)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            packet.Write(writer);
            writer.FlushBits();
        }

        return stream.ToArray();
    }
}
