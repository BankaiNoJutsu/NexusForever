using System.Collections.Immutable;
using System.Reflection;
using NexusForever.Database.World.Model;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Storefront;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Storefront;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Storefront;

public class StorefrontCatalogTests
{
    [Fact]
    public void OfferGroup_BuildSkipsInvisibleOffers()
    {
        var group = new OfferGroup(new StoreOfferGroupModel
        {
            Id          = 42u,
            Name        = "Test Group",
            Description = "Test Group",
            Visible     = 1,
            StoreOfferItem =
            [
                new StoreOfferItemModel
                {
                    Id          = 100u,
                    Name        = "Visible Offer",
                    Description = "Visible Offer",
                    Visible     = 1
                },
                new StoreOfferItemModel
                {
                    Id          = 200u,
                    Name        = "Hidden Offer",
                    Description = "Hidden Offer",
                    Visible     = 0
                }
            ]
        });

        Assert.True(group.HasOffers);
        Assert.Collection(group.Build().Offers,
            offer => Assert.Equal(100u, offer.Id));
    }

    [Fact]
    public void OfferGroup_HasNoOffersWhenAllOffersAreInvisible()
    {
        var group = new OfferGroup(new StoreOfferGroupModel
        {
            Id          = 43u,
            Name        = "Hidden Group",
            Description = "Hidden Group",
            Visible     = 1,
            StoreOfferItem =
            [
                new StoreOfferItemModel
                {
                    Id          = 300u,
                    Name        = "Hidden Offer",
                    Description = "Hidden Offer",
                    Visible     = 0
                }
            ]
        });

        Assert.False(group.HasOffers);
        Assert.Empty(group.Build().Offers);
    }

    [Fact]
    public void SendStoreOffers_SendsStoreOffersInLiveSizedBatches()
    {
        var manager = new GlobalStorefrontManager();
        SetPrivateField(manager, "serverStoreOfferGroupCache", Enumerable.Range(1, 41)
            .Select(id => new ServerStoreOffers.OfferGroup { Id = (uint)id })
            .ToImmutableList());

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);

        typeof(GlobalStorefrontManager)
            .GetMethod("SendStoreOffers", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(manager, [session]);

        int[] packetGroupCounts = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<ServerStoreOffers>()
            .Select(packet => packet.OfferGroups.Count)
            .ToArray();

        Assert.Equal([20, 20, 1], packetGroupCounts);
    }

    [Fact]
    public void HandleCatalogRequest_DoesNotSendCatalogUpdatedNotification()
    {
        var manager = new GlobalStorefrontManager();
        SetPrivateField(manager, "serverStoreCategoryCache", ImmutableList<ServerStoreCategories.StoreCategory>.Empty);
        SetPrivateField(manager, "serverStoreOfferGroupCache", ImmutableList.Create(new ServerStoreOffers.OfferGroup { Id = 1u }));

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);

        manager.HandleCatalogRequest(session, 1u);
        manager.HandleCatalogRequest(session, 1u);

        Type[] packetTypes = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0].GetType())
            .ToArray();

        Assert.Equal(
        [
            typeof(ServerStoreCategories),
            typeof(ServerStoreOffers),
            typeof(ServerStoreFinalise),
            typeof(ServerStoreCategories),
            typeof(ServerStoreOffers),
            typeof(ServerStoreFinalise)
        ],
            packetTypes);
    }

    [Fact]
    public void SendBootstrapCatalogPacketsIfNeeded_NotifiesDirtyWhenCatalogAlreadySentAtPregame()
    {
        var manager = new GlobalStorefrontManager();
        SetPrivateField(manager, "serverStoreCategoryCache", ImmutableList<ServerStoreCategories.StoreCategory>.Empty);
        SetPrivateField(manager, "serverStoreOfferGroupCache", ImmutableList<ServerStoreOffers.OfferGroup>.Empty);

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);

        manager.SendCatalogPackets(session, 1u);
        int sentAfterFirstPass = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;

        manager.SendBootstrapCatalogPacketsIfNeeded(session, 1u);

        Type[] packetTypes = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Skip(sentAfterFirstPass)
            .Select(invocation => invocation.Arguments[0].GetType())
            .ToArray();

        Assert.Equal([typeof(ServerStoreCatalogUpdated)], packetTypes);
    }

    [Fact]
    public void SendBootstrapCatalogPacketsIfNeeded_SendsOncePerAccountDespiteSessionIdChange()
    {
        var manager = new GlobalStorefrontManager();
        SetPrivateField(manager, "serverStoreCategoryCache", ImmutableList<ServerStoreCategories.StoreCategory>.Empty);
        SetPrivateField(manager, "serverStoreOfferGroupCache", ImmutableList<ServerStoreOffers.OfferGroup>.Empty);

        IGameSession worldSession = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> worldProxy);

        manager.SendBootstrapCatalogPacketsIfNeeded(worldSession, 1u);

        Type[] worldPacketTypes = worldProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0].GetType())
            .ToArray();

        Assert.Equal(
        [
            typeof(ServerStoreCategories),
            typeof(ServerStoreFinalise)
        ],
            worldPacketTypes);
    }

    [Fact]
    public void SendBootstrapCatalogPacketsIfNeeded_SendsInitialCatalogWhenCharacterSelectRequestedButNotDelivered()
    {
        var manager = new GlobalStorefrontManager();
        SetPrivateField(manager, "serverStoreCategoryCache", ImmutableList<ServerStoreCategories.StoreCategory>.Empty);
        SetPrivateField(manager, "serverStoreOfferGroupCache", ImmutableList<ServerStoreOffers.OfferGroup>.Empty);

        IGameSession worldSession = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> worldProxy);

        manager.MarkAccountCatalogRequestedBeforeWorldLogin(1u);
        manager.SendBootstrapCatalogPacketsIfNeeded(worldSession, 1u);

        Type[] worldPacketTypes = worldProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0].GetType())
            .ToArray();

        Assert.Equal(
        [
            typeof(ServerStoreCategories),
            typeof(ServerStoreFinalise)
        ],
            worldPacketTypes);
    }

    [Fact]
    public void BuildStoreCategories_RebasesVisibleChildrenOfHiddenStructuralParents()
    {
        MethodInfo method = typeof(GlobalStorefrontManager)
            .GetMethod("BuildStoreCategories", BindingFlags.Static | BindingFlags.NonPublic)!;

        StoreCategoryModel[] storeCategoryModels =
        [
            new()
            {
                Id          = 26u,
                ParentId    = 0u,
                Name        = "TOP LEVEL",
                Description = "DO NOT DELETE",
                Index       = 0u,
                Visible     = 0
            },
            new()
            {
                Id          = 27u,
                ParentId    = 26u,
                Name        = "Featured",
                Description = "Featured",
                Index       = 1u,
                Visible     = 1
            },
            new()
            {
                Id          = 28u,
                ParentId    = 27u,
                Name        = "Subcategory",
                Description = "Subcategory",
                Index       = 2u,
                Visible     = 1
            },
            new()
            {
                Id          = 242u,
                ParentId    = 26u,
                Name        = "Hidden Event Category",
                Description = "Hidden Event Category",
                Index       = 3u,
                Visible     = 0
            }
        ];

        ImmutableDictionary<uint, ICategory> categories =
            (ImmutableDictionary<uint, ICategory>)method.Invoke(null, [storeCategoryModels])!;

        Assert.Equal(2, categories.Count);
        Assert.False(categories.ContainsKey(26u));
        Assert.True(categories.ContainsKey(27u));
        Assert.True(categories.ContainsKey(28u));
        Assert.False(categories.ContainsKey(242u));
        Assert.Equal(0u, categories[27u].ParentCategoryId);
        Assert.Equal(27u, categories[28u].ParentCategoryId);
    }

    [Fact]
    public void BuildNetworkPackets_OrdersStoreCategoriesByTreeIndex()
    {
        var manager = new GlobalStorefrontManager();
        SetPrivateField(manager, "storeCategories", ImmutableDictionary<uint, ICategory>.Empty
            .Add(27u, new Category(new StoreCategoryModel
            {
                Id          = 27u,
                ParentId    = 0u,
                Name        = "Late Root",
                Description = "Late Root",
                Index       = 7u,
                Visible     = 1
            }))
            .Add(28u, new Category(new StoreCategoryModel
            {
                Id          = 28u,
                ParentId    = 27u,
                Name        = "Child",
                Description = "Child",
                Index       = 1u,
                Visible     = 1
            }))
            .Add(76u, new Category(new StoreCategoryModel
            {
                Id          = 76u,
                ParentId    = 0u,
                Name        = "Featured",
                Description = "Featured",
                Index       = 1u,
                Visible     = 1
            })));
        SetPrivateField(manager, "offerGroups", ImmutableDictionary<uint, IOfferGroup>.Empty);

        typeof(GlobalStorefrontManager)
            .GetMethod("BuildNetworkPackets", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(manager, null);

        var categoryCache = (ImmutableList<ServerStoreCategories.StoreCategory>)typeof(GlobalStorefrontManager)
            .GetField("serverStoreCategoryCache", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager)!;

        Assert.Equal([76u, 27u, 28u], categoryCache.Select(category => category.CategoryId));
    }

    [Fact]
    public void OfferGroup_BuildOrdersCategoriesByIndex()
    {
        var group = new OfferGroup(new StoreOfferGroupModel
        {
            Id                  = 1545u,
            Name                = "Group",
            Description         = "Group",
            Visible             = 1,
            DisplayInfoOverride = 0,
            StoreOfferGroupCategory =
            [
                new StoreOfferGroupCategoryModel { Id = 1545u, CategoryId = 35u, Index = 19, Visible = 1 },
                new StoreOfferGroupCategoryModel { Id = 1545u, CategoryId = 36u, Index = 4, Visible = 1 }
            ]
        });

        Assert.Equal([36u, 35u], group.Build().Categories.Select(category => category.Id));
    }

    [Fact]
    public void ServerStoreCategories_WritesCurrencyPackageTypeAsUInt32()
    {
        var packet = new ServerStoreCategories
        {
            StoreCategories =
            [
                new ServerStoreCategories.StoreCategory
                {
                    Name             = "Featured",
                    Description      = "Featured",
                    CategoryId       = 27u,
                    ParentCategoryId = 0u,
                    Index            = 1u,
                    Visible          = true
                }
            ],
            RealCurrency = RealCurrency.Usd,
            CurrencyPackages =
            [
                new ServerStoreCategories.CurrencyPackage
                {
                    Id           = 123u,
                    Name         = "Package",
                    Count        = 400u,
                    Price        = 12.5f,
                    CurrencyType = AccountCurrencyType.NCoin
                }
            ]
        };

        byte[] payload = WritePacket(packet.Write);

        using var stream = new MemoryStream(payload);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal("Featured", reader.ReadWideString());
        Assert.Equal("Featured", reader.ReadWideString());
        Assert.Equal(27u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.Equal((uint)RealCurrency.Usd, reader.ReadUInt(3));

        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(123u, reader.ReadUInt());
        Assert.Equal("Package", reader.ReadWideString());
        Assert.Equal(400u, reader.ReadUInt());
        Assert.Equal(12.5f, reader.ReadSingle());
        Assert.Equal((uint)AccountCurrencyType.NCoin, reader.ReadUInt());
        Assert.Equal(0u, reader.BytesRemaining);
    }

    [Fact]
    public void RetailCompositeUInt32Array_MatchesSequentialWrites_ForTwoAndFourElements()
    {
        foreach (uint[] values in new[] { new uint[] { 76u, 27u }, new uint[] { 76u, 27u, 28u, 29u } })
        {
            byte[] composite = WritePacket(writer => writer.WriteRetailCompositeUInt32Array(values));
            byte[] sequential = WritePacket(writer =>
            {
                foreach (uint value in values)
                    writer.Write(value);
            });

            Assert.Equal(sequential, composite);
        }
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(20)]
    public void RetailCompositeUInt32Array_MatchesSequentialWrites_ForTypicalCategoryCounts(int elementCount)
    {
        uint[] values = Enumerable.Range(1, elementCount).Select(i => (uint)(70 + i)).ToArray();

        byte[] composite = WritePacket(writer => writer.WriteRetailCompositeUInt32Array(values));
        byte[] sequential = WritePacket(writer =>
        {
            foreach (uint value in values)
                writer.Write(value);
        });

        Assert.Equal(sequential, composite);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void RetailCompositeUInt32Array_MatchesSequentialWrites_ForOddElementCounts(int elementCount)
    {
        uint[] values = Enumerable.Range(1, elementCount).Select(i => (uint)(70 + i)).ToArray();

        byte[] composite = WritePacket(writer => writer.WriteRetailCompositeUInt32Array(values));
        byte[] sequential = WritePacket(writer =>
        {
            foreach (uint value in values)
                writer.Write(value);
        });

        Assert.Equal(sequential, composite);
    }

    [Fact]
    public void OfferPrices_AfterWideStrings_CompositeBlockMatchesSequentialFloats()
    {
        var offer = new ServerStoreOffers.OfferGroup.Offer
        {
            Id               = 1678u,
            Name             = "Battlesworn Costume Set",
            Description      = "You'll swear you're ready for battle with the Battlesworn costume set.",
            PricePremium     = 790f,
            PriceAlternative = 395f,
            RetailCatalogWireScalar       = RetailStoreOfferWireConstants.CatalogWireScalarBits,
            RetailCatalogWireByte = 0,
            CurrencyData =
            [
                new ServerStoreOffers.OfferGroup.Offer.OfferCurrencyData
                {
                    CurrencyId            = (byte)AccountCurrencyType.Protobuck,
                    Price                 = 790f,
                    DiscountType          = DiscountType.None,
                    DiscountTimeRemaining = 1L,
                    TimeSinceExpiry       = -1995405795L
                },
                new ServerStoreOffers.OfferGroup.Offer.OfferCurrencyData
                {
                    CurrencyId            = (byte)AccountCurrencyType.Omnibit,
                    Price                 = 395f,
                    DiscountType          = DiscountType.None,
                    DiscountTimeRemaining = 1L,
                    TimeSinceExpiry       = -1995405795L
                }
            ],
            ItemData =
            [
                new ServerStoreOffers.OfferGroup.Offer.OfferItemData
                {
                    Type          = 0u,
                    AccountItemId = 363,
                    Amount        = 1u
                }
            ]
        };

        byte[] composite = WritePacket(offer.Write);

        byte[] sequential = WritePacket(writer =>
        {
            writer.Write(offer.Id);
            writer.WriteStringWide(offer.Name);
            writer.WriteStringWide(offer.Description);
            writer.Write(offer.PricePremium);
            writer.Write(offer.PriceAlternative);
            writer.Write(offer.DisplayFlags, 32u);
            writer.Write(offer.RetailCatalogWireScalar);
            writer.Write(offer.RetailCatalogWireByte);
            writer.Write(offer.CurrencyData.Count);
            offer.CurrencyData.ForEach(e => e.Write(writer));
            writer.Write(offer.ItemData.Count);
            offer.ItemData.ForEach(e => e.Write(writer));
        });

        Assert.Equal(sequential, composite);
    }

    [Fact]
    public void OfferPrices_TwoFloatWrites_MatchRetailCompositeEightByteBlock()
    {
        byte[] composite = WritePacket(writer =>
        {
            Span<byte> priceBytes = stackalloc byte[8];
            BitConverter.TryWriteBytes(priceBytes, BitConverter.SingleToUInt32Bits(12.5f));
            BitConverter.TryWriteBytes(priceBytes[4..], BitConverter.SingleToUInt32Bits(34.5f));
            writer.WriteRetailCompositeByteSpan(priceBytes);
        });

        byte[] sequential = WritePacket(writer =>
        {
            writer.Write(12.5f);
            writer.Write(34.5f);
        });

        Assert.Equal(sequential, composite);
    }

    [Fact]
    public void RetailCompositeByteSpan_Client337160ReaderConsumesSameBitsAsInternalReader()
    {
        uint[] values = [76u, 27u, 28u];

        byte[] payload = WritePacket(writer => writer.WriteRetailCompositeUInt32Array(values));

        using (var stream = new MemoryStream(payload))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(values, reader.ReadRetailCompositeUInt32Array(values.Length));
            Assert.Equal(0u, reader.BytesRemaining);
        }

        using (var stream = new MemoryStream(payload))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(values, ReadRetailCompositeUInt32ArrayClient337160(reader, values.Length));
            Assert.Equal(0u, reader.BytesRemaining);
        }
    }

    private static uint[] ReadRetailCompositeUInt32ArrayClient337160(GamePacketReader reader, int elementCount)
    {
        byte[] bytes = ReadRetailCompositeByteSpanClient337160(reader, elementCount * sizeof(uint));
        var values = new uint[elementCount];
        for (int i = 0; i < elementCount; i++)
            values[i] = BitConverter.ToUInt32(bytes, i * sizeof(uint));

        return values;
    }

    private static byte[] ReadRetailCompositeByteSpanClient337160(GamePacketReader reader, int byteCount)
    {
        byte[] bytes = new byte[byteCount];
        ulong param = (ulong)byteCount;
        int offset = 0;

        if ((param & 1) != 0)
        {
            bytes[offset++] = reader.ReadByte(8u);
            param--;
        }

        if ((param & 2) != 0)
        {
            ushort value = reader.ReadUShort(16u);
            bytes[offset++] = (byte)value;
            bytes[offset++] = (byte)(value >> 8);
            param -= 2;
        }

        if ((param & 4) != 0)
        {
            uint value = reader.ReadUInt(32u);
            bytes[offset++] = (byte)value;
            bytes[offset++] = (byte)(value >> 8);
            bytes[offset++] = (byte)(value >> 16);
            bytes[offset++] = (byte)(value >> 24);
            param -= 4;
        }

        while (param != 0)
        {
            ulong value = reader.ReadULong(64u);
            for (int i = 0; i < 8 && offset < byteCount; i++)
                bytes[offset++] = (byte)(value >> (8 * i));

            param -= 8;
        }

        return bytes;
    }

    [Fact]
    public void ServerStoreOfferGroup_AfterOffers_CompositeCategoryArraysMatchSequentialWrites()
    {
        var group = new ServerStoreOffers.OfferGroup
        {
            Id                  = 1533u,
            Name                = "Group",
            Description         = "Group",
            DisplayInfoOverride = 0,
            Categories          =
            [
                new ServerStoreOffers.OfferGroup.Category { Id = 76u, Index = 1u },
                new ServerStoreOffers.OfferGroup.Category { Id = 27u, Index = 2u }
            ]
        };

        for (uint offerId = 1500u; offerId < 1504u; offerId++)
        {
            group.Offers.Add(new ServerStoreOffers.OfferGroup.Offer
            {
                Id                            = offerId,
                Name                          = "Offer",
                Description                   = "Desc",
                PricePremium                  = 10f,
                PriceAlternative              = 5f,
                DisplayFlags                  = 0,
                RetailCatalogWireScalar       = RetailStoreOfferWireConstants.CatalogWireScalarBits,
                RetailCatalogWireByte = 0
            });
        }

        byte[] composite = WritePacket(group.Write);

        byte[] sequential = WritePacket(writer =>
        {
            writer.Write(group.Id);
            writer.Write(group.DisplayFlags, 32u);
            writer.WriteStringWide(group.Name);
            writer.WriteStringWide(group.Description);
            writer.Write(group.DisplayInfoOverride, 14u);
            writer.Write(group.Offers.Count);
            group.Offers.ForEach(offer => offer.Write(writer));
            writer.Write(group.Categories.Count);
            foreach (ServerStoreOffers.OfferGroup.Category category in group.Categories)
                writer.Write(category.Id);
            foreach (ServerStoreOffers.OfferGroup.Category category in group.Categories)
                writer.Write(category.Index);
        });

        Assert.Equal(sequential, composite);
    }

    [Fact]
    public void ServerStoreOfferGroup_FourCategoryLinks_UseRetailCompositeUInt32Arrays()
    {
        var group = new ServerStoreOffers.OfferGroup
        {
            Id                  = 1680u,
            Name                = "Group",
            Description         = "Group",
            DisplayInfoOverride = 0,
            Categories          =
            [
                new ServerStoreOffers.OfferGroup.Category { Id = 76u, Index = 1u },
                new ServerStoreOffers.OfferGroup.Category { Id = 27u, Index = 2u },
                new ServerStoreOffers.OfferGroup.Category { Id = 28u, Index = 3u },
                new ServerStoreOffers.OfferGroup.Category { Id = 29u, Index = 4u }
            ]
        };

        byte[] payload = WritePacket(group.Write);

        using var stream = new MemoryStream(payload);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(1680u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal("Group", reader.ReadWideString());
        Assert.Equal("Group", reader.ReadWideString());
        Assert.Equal(0u, reader.ReadUInt(14u));
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal([76u, 27u, 28u, 29u], reader.ReadRetailCompositeUInt32Array(4));
        Assert.Equal([1u, 2u, 3u, 4u], reader.ReadRetailCompositeUInt32Array(4));
        Assert.Equal(0u, reader.BytesRemaining);
    }

    [Fact]
    public void ServerStoreOffer_TrailingField_WritesEightBitsBeforeCurrencyCount()
    {
        const byte trailing = 0x42;

        var offer = new ServerStoreOffers.OfferGroup.Offer
        {
            Id                            = 77u,
            Name                          = "Offer",
            Description                   = "Offer",
            PricePremium                  = 12.5f,
            PriceAlternative              = 34.5f,
            DisplayFlags                  = 0,
            RetailCatalogWireScalar       = RetailStoreOfferWireConstants.CatalogWireScalarBits,
            RetailCatalogWireByte = trailing
        };

        byte[] payload = WritePacket(offer.Write);

        using var stream = new MemoryStream(payload);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(77u, reader.ReadUInt());
        Assert.Equal("Offer", reader.ReadWideString());
        Assert.Equal("Offer", reader.ReadWideString());
        byte[] priceBytes = reader.ReadRetailCompositeByteSpan(8);
        Assert.Equal(12.5f, BitConverter.ToSingle(priceBytes, 0));
        Assert.Equal(34.5f, BitConverter.ToSingle(priceBytes, 4));
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(RetailStoreOfferWireConstants.CatalogWireScalarBits, reader.ReadLong());
        Assert.Equal(trailing, reader.ReadByte());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
    }

    [Fact]
    public void ServerStoreCategories_ThreeCurrencyPackages_MatchNested098FRowSize()
    {
        var packet = new ServerStoreCategories
        {
            RealCurrency     = RealCurrency.Usd,
            CurrencyPackages =
            [
                new ServerStoreCategories.CurrencyPackage
                {
                    Id           = 1u,
                    Name         = "NCoin Pack (1,000)",
                    Count        = 1_000u,
                    Price        = 4.99f,
                    CurrencyType = AccountCurrencyType.NCoin
                },
                new ServerStoreCategories.CurrencyPackage
                {
                    Id           = 2u,
                    Name         = "NCoin Pack (2,500)",
                    Count        = 2_500u,
                    Price        = 9.99f,
                    CurrencyType = AccountCurrencyType.NCoin
                },
                new ServerStoreCategories.CurrencyPackage
                {
                    Id           = 3u,
                    Name         = "Omnibit Pack (500)",
                    Count        = 500u,
                    Price        = 2.99f,
                    CurrencyType = AccountCurrencyType.Omnibit
                }
            ]
        };

        byte[] categoriesPayload = WritePacket(packet.Write);
        byte[] rowPayload = WritePacket(new ServerStoreCurrencyPackageRow
        {
            Id           = 1u,
            Name         = "NCoin Pack (1,000)",
            Count        = 1_000u,
            Price        = 4.99f,
            CurrencyType = AccountCurrencyType.NCoin
        }.Write);

        using var stream = new MemoryStream(categoriesPayload);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal((uint)RealCurrency.Usd, reader.ReadUInt(3));
        Assert.Equal(3u, reader.ReadUInt());

        for (int i = 0; i < 3; i++)
        {
            long rowStart = stream.Position;
            Assert.Equal(packet.CurrencyPackages[i].Id, reader.ReadUInt());
            Assert.Equal(packet.CurrencyPackages[i].Name, reader.ReadWideString());
            Assert.Equal(packet.CurrencyPackages[i].Count, reader.ReadUInt());
            Assert.Equal(packet.CurrencyPackages[i].Price, reader.ReadSingle());
            Assert.Equal((uint)packet.CurrencyPackages[i].CurrencyType, reader.ReadUInt());
            Assert.Equal(rowPayload.Length, stream.Position - rowStart);
        }

        Assert.Equal(0u, reader.BytesRemaining);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    [Fact]
    public void SeedLikeOffer_WithDualCurrencyRows_PassesRetailWireReader()
    {
        var offer = new ServerStoreOffers.OfferGroup.Offer
        {
            Id                       = 1534u,
            Name                     = "Fortune Coin",
            Description              = "One (1) Fortune Coin for Madam Fay's Fortunes.",
            PricePremium             = 40f,
            PriceAlternative         = 80f,
            DisplayFlags             = 0,
            RetailCatalogWireScalar  = RetailStoreOfferWireConstants.CatalogWireScalarBits,
            RetailCatalogWireByte = 0,
            CurrencyData =
            [
                new ServerStoreOffers.OfferGroup.Offer.OfferCurrencyData
                {
                    CurrencyId            = 6,
                    Price                   = 40f,
                    DiscountType          = DiscountType.None,
                    DiscountValue           = 0f,
                    DiscountTimeRemaining   = -235145984L,
                    TimeSinceExpiry         = -1995405795L
                },
                new ServerStoreOffers.OfferGroup.Offer.OfferCurrencyData
                {
                    CurrencyId            = 11,
                    Price                   = 80f,
                    DiscountType          = DiscountType.None,
                    DiscountValue           = 0f,
                    DiscountTimeRemaining   = -235145984L,
                    TimeSinceExpiry         = -1995405795L
                }
            ],
            ItemData =
            [
                new ServerStoreOffers.OfferGroup.Offer.OfferItemData
                {
                    Type          = 0u,
                    AccountItemId = 590,
                    Amount        = 1u
                }
            ]
        };

        var packet = new ServerStoreOffers
        {
            OfferGroups =
            [
                new ServerStoreOffers.OfferGroup
                {
                    Id                  = 1533u,
                    Name                = "Fortune Coins",
                    Description         = "Fortune Coins",
                    DisplayInfoOverride = 0,
                    Categories          = [new ServerStoreOffers.OfferGroup.Category { Id = 49u, Index = 1u }],
                    Offers              = [offer]
                }
            ]
        };

        byte[] body = WritePacket(packet.Write);

        using var stream = new MemoryStream(body);
        using var reader = new GamePacketReader(stream);

        Assert.True(RetailStoreOffersWireReader.TryRead(reader, out string failure, out uint groupId, out uint offerId), failure);
        Assert.Equal(1533u, groupId);
        Assert.Equal(1534u, offerId);
        Assert.Equal(0u, reader.BytesRemaining);
    }

    [Fact]
    public void ServerStoreOffers_RetailWireReader_ConsumesSerializedOfferGroup()
    {
        var packet = new ServerStoreOffers
        {
            OfferGroups =
            [
                new ServerStoreOffers.OfferGroup
                {
                    Id                  = 1553u,
                    Name                = "Deluxe Upgrade Bundle",
                    Description         = "Bundle",
                    DisplayInfoOverride = 197,
                    Categories          =
                    [
                        new ServerStoreOffers.OfferGroup.Category { Id = 76u, Index = 1u },
                        new ServerStoreOffers.OfferGroup.Category { Id = 27u, Index = 2u }
                    ],
                    Offers =
                    [
                        new ServerStoreOffers.OfferGroup.Offer
                        {
                            Id                       = 1553u,
                            Name                     = "Deluxe Upgrade Bundle",
                            Description              = "Bundle",
                            RetailCatalogWireScalar  = RetailStoreOfferWireConstants.CatalogWireScalarBits,
                            CurrencyData =
                            [
                                new ServerStoreOffers.OfferGroup.Offer.OfferCurrencyData
                                {
                                    CurrencyId            = 6,
                                    Price                   = 10f,
                                    DiscountType          = DiscountType.None,
                                    DiscountTimeRemaining   = -235145984L,
                                    TimeSinceExpiry         = -1995405795L
                                }
                            ],
                            ItemData =
                            [
                                new ServerStoreOffers.OfferGroup.Offer.OfferItemData
                                {
                                    Type          = 0u,
                                    AccountItemId = 590,
                                    Amount        = 1u
                                },
                                new ServerStoreOffers.OfferGroup.Offer.OfferItemData
                                {
                                    Type          = 0u,
                                    AccountItemId = 591,
                                    Amount        = 1u
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        byte[] body = WritePacket(packet.Write);

        using var stream = new MemoryStream(body);
        using var reader = new GamePacketReader(stream);

        Assert.True(RetailStoreOffersWireReader.TryRead(reader, out string failure, out uint groupId, out uint offerId), failure);
        Assert.Equal(1553u, groupId);
        Assert.Equal(0u, reader.BytesRemaining);
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            write(writer);
            writer.FlushBits();
        }

        return stream.ToArray();
    }
}