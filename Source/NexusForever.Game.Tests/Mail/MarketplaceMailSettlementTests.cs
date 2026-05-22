using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Game;
using NexusForever.Game.Mail;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Mail;

[Collection(LegacyServiceProviderCollection.Name)]
public class MarketplaceMailSettlementTests
{
    [Fact]
    public void MailItem_ItemAuctionWon_UsesLocalizedTextAndAuctionContentType()
    {
        using ServiceProviderScope scope = UseGameTableProvider();

        MailItem mail = new(new MailParameters
        {
            RecipientCharacterId = 100ul,
            MessageType          = SenderType.ItemAuction,
            ContentType          = ContentType.AuctionWon,
            SubjectStringId      = MarketplaceMailTexts.FallbackMarketplaceLocalizedTextId,
            BodyStringId         = MarketplaceMailTexts.FallbackMarketplaceLocalizedTextId,
            DeliverySpeed        = DeliverySpeed.Instant
        });

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
        using ServiceProviderScope scope = UseGameTableProvider();

        MailItem mail = new(new MailParameters
        {
            RecipientCharacterId = 100ul,
            MessageType          = SenderType.CommodityAuction,
            ContentType          = ContentType.AuctionExpired,
            SubjectStringId      = MarketplaceMailTexts.FallbackMarketplaceLocalizedTextId,
            BodyStringId         = MarketplaceMailTexts.FallbackMarketplaceLocalizedTextId,
            DeliverySpeed        = DeliverySpeed.Instant
        });

        ServerMailAvailable.Mail packet = mail.Build();

        Assert.Equal(SenderType.CommodityAuction, packet.SenderType);
        Assert.Equal(ContentType.AuctionExpired, packet.ContentType);
    }

    [Fact]
    public void MarketplaceMailTexts_ResolvesAchievementTextToLocalizedId()
    {
        using ServiceProviderScope scope = UseGameTableProvider(
            new AchievementTextEntry
            {
                Id              = MarketplaceMailTexts.MarketplaceMailAchievementTextId,
                LocalizedTextId = 278287u
            });

        Assert.True(MarketplaceMailTexts.TryGetMarketplaceMailLocalizedTextId(out uint localizedTextId));
        Assert.Equal(278287u, localizedTextId);
    }

    private static ServiceProviderScope UseGameTableProvider(params AchievementTextEntry[] achievementTexts)
    {
        var configuration = new SharedConfiguration(new ConfigurationBuilder().Build());
        configuration.Initialise<TestConfiguration>();

        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.AchievementText), CreateGameTable(achievementTexts));

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton(new AssetManager());
        services.AddSingleton(gameTableManager);
        return new ServiceProviderScope(services.BuildServiceProvider());
    }

    private sealed class TestConfiguration
    {
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

    private sealed class ServiceProviderScope : IDisposable
    {
        private readonly IServiceProvider previous;

        public ServiceProviderScope(IServiceProvider provider)
        {
            previous                       = LegacyServiceProvider.Provider;
            LegacyServiceProvider.Provider = provider;
        }

        public void Dispose()
        {
            LegacyServiceProvider.Provider = previous;
        }
    }
}
