using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Mail;

namespace NexusForever.Game.Tests.Mail;

public class MarketplaceMailSettlementTests
{
    [Fact]
    public void MailItem_ItemAuctionWon_UsesLocalizedTextAndAuctionContentType()
    {
        var assetManager = new AssetManager();

        MailItem mail = new(new MailParameters
        {
            RecipientCharacterId = 100ul,
            MessageType          = SenderType.ItemAuction,
            ContentType          = ContentType.AuctionWon,
            SubjectStringId      = MarketplaceMailTexts.FallbackMarketplaceLocalizedTextId,
            BodyStringId         = MarketplaceMailTexts.FallbackMarketplaceLocalizedTextId,
            DeliverySpeed        = DeliverySpeed.Instant
        }, assetManager);

        ServerMailAvailable.Mail packet = mail.Build();

        Assert.Equal(SenderType.ItemAuction, packet.SenderType);
        Assert.Equal(ContentType.AuctionWon, packet.ContentType);
        Assert.Equal(MarketplaceMailTexts.FallbackMarketplaceLocalizedTextId, packet.SubjectStringId);
        Assert.Equal(MarketplaceMailTexts.FallbackMarketplaceLocalizedTextId, packet.BodyStringId);
        Assert.Equal(DeliverySpeed.Instant, mail.DeliverySpeed);
    }

    [Fact]
    public void MailItem_CommodityAuctionReturn_UsesAuctionExpiredContentType()
    {
        var assetManager = new AssetManager();

        MailItem mail = new(new MailParameters
        {
            RecipientCharacterId = 100ul,
            MessageType          = SenderType.CommodityAuction,
            ContentType          = ContentType.AuctionExpired,
            SubjectStringId      = MarketplaceMailTexts.FallbackMarketplaceLocalizedTextId,
            BodyStringId         = MarketplaceMailTexts.FallbackMarketplaceLocalizedTextId,
            DeliverySpeed        = DeliverySpeed.Instant
        }, assetManager);

        ServerMailAvailable.Mail packet = mail.Build();

        Assert.Equal(SenderType.CommodityAuction, packet.SenderType);
        Assert.Equal(ContentType.AuctionExpired, packet.ContentType);
    }

    [Fact]
    public void MailItem_PersistedMarketplaceMail_PreservesStoredContentType()
    {
        MailItem mail = new(new CharacterMailModel
        {
            Id               = 101ul,
            RecipientId      = 100ul,
            SenderType       = (byte)SenderType.CommodityAuction,
            ContentType      = (byte)ContentType.AuctionExpired,
            Subject          = "Returned commodity",
            Message          = "",
            CurrencyType     = (byte)CurrencyType.Credits,
            DeliveryTime     = (byte)DeliverySpeed.Instant,
            CreateTime       = DateTime.UtcNow
        });

        ServerMailAvailable.Mail packet = mail.Build();

        Assert.Equal(SenderType.CommodityAuction, packet.SenderType);
        Assert.Equal(ContentType.AuctionExpired, packet.ContentType);
    }

    [Fact]
    public void MailItem_PersistedLegacyMarketplaceMail_FallsBackToSenderType()
    {
        MailItem mail = new(new CharacterMailModel
        {
            Id               = 101ul,
            RecipientId      = 100ul,
            SenderType       = (byte)SenderType.ItemAuction,
            ContentType      = 0,
            Subject          = "Legacy auction",
            Message          = "",
            CurrencyType     = (byte)CurrencyType.Credits,
            DeliveryTime     = (byte)DeliverySpeed.Instant,
            CreateTime       = DateTime.UtcNow
        });

        ServerMailAvailable.Mail packet = mail.Build();

        Assert.Equal(SenderType.ItemAuction, packet.SenderType);
        Assert.Equal(ContentType.AuctionWon, packet.ContentType);
    }

    [Fact]
    public void MailItem_Build_ReportsRemainingExpiryDays()
    {
        MailItem mail = new(new CharacterMailModel
        {
            Id               = 101ul,
            RecipientId      = 100ul,
            SenderType       = (byte)SenderType.Creature,
            CreatureId       = 42u,
            Subject          = "Old mail",
            Message          = "Almost expired",
            CurrencyType     = (byte)CurrencyType.Credits,
            DeliveryTime     = (byte)DeliverySpeed.Instant,
            CreateTime       = DateTime.UtcNow.AddDays(-29.5d)
        });

        ServerMailAvailable.Mail packet = mail.Build();

        Assert.InRange(packet.ExpiryTimeInDays, 0.45f, 0.5f);
    }

    [Fact]
    public void MarketplaceMailTexts_ResolvesAchievementTextToLocalizedId()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            new AchievementTextEntry
            {
                Id              = MarketplaceMailTexts.MarketplaceMailAchievementTextId,
                LocalizedTextId = 278287u
            });

        Assert.True(MarketplaceMailTexts.TryGetMarketplaceMailLocalizedTextId(out uint localizedTextId, gameTableManager));
        Assert.Equal(278287u, localizedTextId);
    }

    [Fact]
    public void MarketplaceMailDelivery_IsAvailable_UsesExplicitCharacterDatabase()
    {
        Assert.True(MarketplaceMailDelivery.IsAvailable(new CharacterDatabase()));
        Assert.False(MarketplaceMailDelivery.IsAvailable(null));
    }

    [Fact]
    public void MarketplaceMailDelivery_AdditionalSaveFailure_RestoresAttachedItemOwner()
    {
        CreateMarketplaceMailDependencies(
            out CharacterDatabase characterDatabase,
            out GameTableManager gameTableManager,
            out AssetManager assetManager);

        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        itemProxy.SetProperty(nameof(IItem.Id), 123u);
        itemProxy.SetProperty(nameof(IItem.Guid), 456ul);
        itemProxy.SetProperty(nameof(IItem.CharacterId), 100ul);

        bool additionalSaveInvoked = false;
        bool result = MarketplaceMailDelivery.TrySendItemAuctionWonMail(
            characterDatabase,
            gameTableManager,
            assetManager,
            200ul,
            item,
            _ =>
            {
                additionalSaveInvoked = true;
                throw new InvalidOperationException("Simulated marketplace settlement save failure.");
            });

        Assert.False(result);
        Assert.True(additionalSaveInvoked);
        Assert.Equal(100ul, item.CharacterId);
    }

    [Fact]
    public void MarketplaceMailDelivery_AuctionReturnAdditionalSaveFailure_RestoresAttachedItemOwner()
    {
        CreateMarketplaceMailDependencies(
            out CharacterDatabase characterDatabase,
            out GameTableManager gameTableManager,
            out AssetManager assetManager);

        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        itemProxy.SetProperty(nameof(IItem.Id), 123u);
        itemProxy.SetProperty(nameof(IItem.Guid), 456ul);
        itemProxy.SetProperty(nameof(IItem.CharacterId), 100ul);

        bool additionalSaveInvoked = false;
        bool result = MarketplaceMailDelivery.TrySendItemAuctionReturnMail(
            characterDatabase,
            gameTableManager,
            assetManager,
            200ul,
            item,
            _ =>
            {
                additionalSaveInvoked = true;
                throw new InvalidOperationException("Simulated marketplace settlement save failure.");
            });

        Assert.False(result);
        Assert.True(additionalSaveInvoked);
        Assert.Equal(100ul, item.CharacterId);
    }

    [Fact]
    public void MarketplaceMailDelivery_CommodityReturnAdditionalSaveFailure_ReturnsFalse()
    {
        CreateMarketplaceMailDependencies(
            out CharacterDatabase characterDatabase,
            out GameTableManager gameTableManager,
            out AssetManager assetManager);

        IItemInfo itemInfo = CreateItemInfo(123u);
        ItemManager itemManager = CreatePrimedItemManager(itemInfo);

        bool additionalSaveInvoked = false;
        bool result = MarketplaceMailDelivery.TrySendCommodityAuctionReturnMail(
            characterDatabase,
            gameTableManager,
            assetManager,
            200ul,
            itemInfo.Id,
            2u,
            _ =>
            {
                additionalSaveInvoked = true;
                throw new InvalidOperationException("Simulated marketplace settlement save failure.");
            },
            itemManager: itemManager);

        Assert.False(result);
        Assert.True(additionalSaveInvoked);
    }

    [Fact]
    public void MarketplaceMailDelivery_CommodityFillAdditionalSaveFailure_ReturnsFalse()
    {
        CreateMarketplaceMailDependencies(
            out CharacterDatabase characterDatabase,
            out GameTableManager gameTableManager,
            out AssetManager assetManager);

        IItemInfo itemInfo = CreateItemInfo(123u);
        ItemManager itemManager = CreatePrimedItemManager(itemInfo);

        bool additionalSaveInvoked = false;
        bool result = MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(
            characterDatabase,
            gameTableManager,
            assetManager,
            200ul,
            itemInfo.Id,
            2u,
            _ =>
            {
                additionalSaveInvoked = true;
                throw new InvalidOperationException("Simulated marketplace settlement save failure.");
            },
            itemManager: itemManager);

        Assert.False(result);
        Assert.True(additionalSaveInvoked);
    }

    private static void CreateMarketplaceMailDependencies(
        out CharacterDatabase characterDatabase,
        out GameTableManager gameTableManager,
        out AssetManager assetManager,
        params AchievementTextEntry[] achievementTexts)
    {
        gameTableManager = CreateGameTableManager(achievementTexts);
        assetManager = new AssetManager();

        characterDatabase = new CharacterDatabase();
        characterDatabase.Initialise(new DatabaseConnectionString
        {
            Provider         = DatabaseProvider.MySql,
            ConnectionString = "server=127.0.0.1;user id=nexus;password=nexus;database=nexus_forever_character;"
        });
    }

    private static GameTableManager CreateGameTableManager(params AchievementTextEntry[] achievementTexts)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.AchievementText), CreateGameTable(achievementTexts));
        return gameTableManager;
    }

    private static IItemInfo CreateItemInfo(uint itemId)
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), itemId);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id            = itemId,
            MaxStackCount = 200u
        });

        return itemInfo;
    }

    private static ItemManager CreatePrimedItemManager(IItemInfo itemInfo)
    {
        var itemManager = new ItemManager();
        SetPrivateField(itemManager, "item", ImmutableDictionary<uint, IItemInfo>.Empty.Add(itemInfo.Id, itemInfo));
        SetPrivateField(itemManager, "nextItemId", 1ul);
        return itemManager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

}
