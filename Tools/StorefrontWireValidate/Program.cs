using Microsoft.EntityFrameworkCore;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Storefront;
using NexusForever.Game.Storefront;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;

const string DefaultConnectionString =
    "Server=127.0.0.1;Port=3306;User ID=bankai;Password=bankai;Database=nexus_forever_world";

string connectionString = DefaultConnectionString;
int bisectPacket = 0;
for (int i = 0; i < args.Length; i++)
{
    if (args[i].StartsWith("--connection=", StringComparison.OrdinalIgnoreCase))
    {
        connectionString = args[i]["--connection=".Length..];
        continue;
    }

    if (args[i].StartsWith("--bisect-packet=", StringComparison.OrdinalIgnoreCase)
        && int.TryParse(args[i]["--bisect-packet=".Length..], out int packet))
    {
        bisectPacket = packet;
    }
}

Console.WriteLine($"Using connection: {connectionString}");

var options = new DbContextOptionsBuilder<WorldContext>()
    .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)))
    .Options;

using var context = new WorldContext(options);
List<StoreOfferGroupModel> groupModels = context.StoreOfferGroup
    .Include(e => e.StoreOfferGroupCategory)
    .Include(e => e.StoreOfferItem)
        .ThenInclude(e => e.StoreOfferItemData)
    .Include(e => e.StoreOfferItem)
        .ThenInclude(e => e.StoreOfferItemPrice)
    .AsNoTracking()
    .OrderBy(e => e.Id)
    .Where(e => e.Visible == 1)
    .ToList();

List<ServerStoreOffers.OfferGroup> offerGroups = groupModels
    .Select(BuildOfferGroup)
    .Where(group => group.Offers.Count != 0)
    .ToList();

Console.WriteLine($"Loaded {offerGroups.Count} visible offer groups ({offerGroups.Sum(g => g.Offers.Count)} offers).");

const uint displayInfoOverrideBitLimit = (1u << 14) - 1u;
List<string> wireLimitViolations = offerGroups
    .Where(group => group.DisplayInfoOverride > displayInfoOverrideBitLimit)
    .Select(group => $"groupId={group.Id} displayInfoOverride={group.DisplayInfoOverride} exceeds 14-bit wire limit")
    .ToList();

if (wireLimitViolations.Count != 0)
{
    foreach (string violation in wireLimitViolations)
        Console.Error.WriteLine($"FAIL {violation}");

    return 1;
}

if (args.Any(a => a.Equals("--scan-db", StringComparison.OrdinalIgnoreCase)))
{
    return ScanDatabaseViolations(groupModels) ? 1 : 0;
}

if (bisectPacket > 0)
{
    return BisectPacket(offerGroups, bisectPacket) ? 1 : 0;
}

var batch = new ServerStoreOffers();
int packetIndex = 0;
bool anyFailure = false;

foreach (ServerStoreOffers.OfferGroup offerGroup in offerGroups)
{
    batch.OfferGroups.Add(offerGroup);

    if (batch.OfferGroups.Count != 20)
        continue;

    packetIndex++;
    anyFailure |= !ValidateBatch(packetIndex, batch);
    batch = new ServerStoreOffers();
}

if (batch.OfferGroups.Count != 0)
{
    packetIndex++;
    anyFailure |= !ValidateBatch(packetIndex, batch);
}

return anyFailure ? 1 : 0;

static bool ScanDatabaseViolations(List<StoreOfferGroupModel> groupModels)
{
    const uint accountItemIdBitLimit = (1u << 15) - 1u;
    bool anyFailure = false;

    foreach (StoreOfferGroupModel group in groupModels)
    {
        foreach (StoreOfferItemModel offer in group.StoreOfferItem.Where(offer => offer.Visible == 1))
        {
            foreach (StoreOfferItemDataModel item in offer.StoreOfferItemData)
            {
                if (item.Type > 2)
                {
                    Console.Error.WriteLine(
                        $"FAIL offerId={offer.Id} groupId={group.Id} itemType={item.Type} exceeds supported range.");
                    anyFailure = true;
                }

                if (item.Type == 0 && item.ItemId > accountItemIdBitLimit)
                {
                    Console.Error.WriteLine(
                        $"FAIL offerId={offer.Id} groupId={group.Id} accountItemId={item.ItemId} exceeds 15-bit wire limit.");
                    anyFailure = true;
                }
            }

            foreach (StoreOfferItemPriceModel price in offer.StoreOfferItemPrice)
            {
                if (price.DiscountType > 2)
                {
                    Console.Error.WriteLine(
                        $"FAIL offerId={offer.Id} groupId={group.Id} discountType={price.DiscountType} exceeds 2-bit wire limit.");
                    anyFailure = true;
                }
            }
        }

        if (group.DisplayInfoOverride > (1u << 14) - 1u)
        {
            Console.Error.WriteLine(
                $"FAIL groupId={group.Id} displayInfoOverride={group.DisplayInfoOverride} exceeds 14-bit wire limit.");
            anyFailure = true;
        }
    }

    if (!anyFailure)
        Console.WriteLine("OK   no database wire-limit violations found.");

    return anyFailure;
}

static bool BisectPacket(List<ServerStoreOffers.OfferGroup> offerGroups, int packetIndex)
{
    List<ServerStoreOffers.OfferGroup> packetGroups = GetPacketGroups(offerGroups, packetIndex);
    if (packetGroups.Count == 0)
    {
        Console.Error.WriteLine($"No groups for packet {packetIndex}.");
        return true;
    }

    Console.WriteLine($"Bisect packet={packetIndex} groups={packetGroups.Count} " +
        $"ids=[{string.Join(",", packetGroups.Select(g => g.Id))}]");

    var cumulative = new ServerStoreOffers();
    bool anyFailure = false;

    for (int i = 0; i < packetGroups.Count; i++)
    {
        cumulative.OfferGroups.Add(packetGroups[i]);
        byte[] body = SerializeBody(cumulative);

        using var stream = new MemoryStream(body);
        using var reader = new GamePacketReader(stream);

        if (!RetailStoreOffersWireReader.TryRead(reader, out string failure, out uint groupId, out uint offerId))
        {
            Console.Error.WriteLine(
                $"FAIL after groupIndex={i} groupId={packetGroups[i].Id} offerId={offerId} reason={failure} bodyBytes={body.Length}");
            anyFailure = true;
            break;
        }

        if (reader.BytesRemaining != 0)
        {
            Console.Error.WriteLine(
                $"FAIL after groupIndex={i} groupId={packetGroups[i].Id} trailingBytes={reader.BytesRemaining} bodyBytes={body.Length}");
            anyFailure = true;
            break;
        }

        Console.WriteLine(
            $"OK   through groupIndex={i} groupId={packetGroups[i].Id} offers={packetGroups[i].Offers.Count} bodyBytes={body.Length}");
    }

    return anyFailure;
}

static List<ServerStoreOffers.OfferGroup> GetPacketGroups(List<ServerStoreOffers.OfferGroup> offerGroups, int packetIndex)
{
    int index = 0;
    var batch = new List<ServerStoreOffers.OfferGroup>();

    foreach (ServerStoreOffers.OfferGroup offerGroup in offerGroups)
    {
        batch.Add(offerGroup);

        if (batch.Count != 20)
            continue;

        index++;
        if (index == packetIndex)
            return batch;

        batch = new List<ServerStoreOffers.OfferGroup>();
    }

    index++;
    return index == packetIndex ? batch : new List<ServerStoreOffers.OfferGroup>();
}

static bool ValidateBatch(int packetIndex, ServerStoreOffers batch)
{
    byte[] body = SerializeBody(batch);

    using var stream = new MemoryStream(body);
    using var reader = new GamePacketReader(stream);

    if (!RetailStoreOffersWireReader.TryRead(reader, out string failure, out uint groupId, out uint offerId))
    {
        Console.Error.WriteLine(
            $"FAIL packet={packetIndex} groupId={groupId} offerId={offerId} reason={failure} bodyBytes={body.Length}");
        Console.Error.WriteLine($"  groupIds=[{string.Join(",", batch.OfferGroups.Select(g => g.Id))}]");
        return false;
    }

    if (reader.BytesRemaining != 0)
    {
        Console.Error.WriteLine(
            $"FAIL packet={packetIndex} trailingBytes={reader.BytesRemaining} bodyBytes={body.Length}");
        Console.Error.WriteLine($"  groupIds=[{string.Join(",", batch.OfferGroups.Select(g => g.Id))}]");
        return false;
    }

    Console.WriteLine($"OK   packet={packetIndex} groups={batch.OfferGroups.Count} bodyBytes={body.Length}");
    return true;
}

static ServerStoreOffers.OfferGroup BuildOfferGroup(StoreOfferGroupModel model)
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

static ServerStoreOffers.OfferGroup.Offer BuildOffer(StoreOfferItemModel model)
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
        RetailCatalogWireScalar       = RetailStoreOfferWireConstants.CatalogWireScalarBits,
        RetailCatalogWireByte         = model.RetailCatalogWireByte
    };

    foreach (StoreOfferItemPriceModel price in model.StoreOfferItemPrice.OrderBy(price => price.CurrencyId))
    {
        offer.CurrencyData.Add(new ServerStoreOffers.OfferGroup.Offer.OfferCurrencyData
        {
            CurrencyId            = price.CurrencyId,
            Price                 = price.Price,
            DiscountType          = (DiscountType)price.DiscountType,
            DiscountValue         = price.DiscountValue,
            DiscountTimeRemaining = price.DiscountTimeRemaining,
            TimeSinceExpiry       = price.Expiry != 0 ? -price.Expiry : -1995405795L
        });
    }

    foreach (StoreOfferItemDataModel itemData in model.StoreOfferItemData
        .OrderBy(itemData => itemData.Type)
        .ThenBy(itemData => itemData.ItemId)
        .ThenBy(itemData => itemData.Amount))
    {
        offer.ItemData.Add(new ServerStoreOffers.OfferGroup.Offer.OfferItemData
        {
            Type               = itemData.Type,
            AccountItemId      = (ushort)itemData.ItemId,
            Amount             = itemData.Amount,
            Type1AccountItemId = itemData.ItemId,
            Type1Amount        = itemData.Amount,
            Type2AccountItemId = itemData.ItemId
        });
    }

    return offer;
}

static float GetCurrencyValue(StoreOfferItemPriceModel price)
{
    if (price.DiscountValue == 0f)
        return (float)Math.Ceiling(price.Price);

    return (float)Math.Ceiling(price.Price / ((100f - price.DiscountValue) / 100f));
}

static byte[] SerializeBody(ServerStoreOffers packet)
{
    using var stream = new MemoryStream();
    using (var writer = new GamePacketWriter(stream))
    {
        packet.Write(writer);
        writer.FlushBits();
    }

    return stream.ToArray();
}
