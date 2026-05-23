using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Cinematic;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Static.Storefront;

namespace NexusForever.Game.Tests.Network;

public class PacketPlaceholderNamingTests
{
    [Theory]
    [InlineData((byte)11, true)]
    [InlineData((byte)19, true)]
    [InlineData((byte)7, false)]
    public void ClientPackedWorld_ReadExposesEnvelopeType(byte envelopeType, bool isKnownEnvelopeType)
    {
        byte[] payload = [1, 2, 3, 4];
        byte[] packetData = BuildPackedWorldPacket(envelopeType, payload);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var message = new ClientPackedWorld();
        message.Read(reader);

        Assert.Equal(envelopeType, message.EnvelopeType);
        Assert.Equal(isKnownEnvelopeType, message.IsKnownEnvelopeType);
        Assert.Equal(payload, message.Data);
    }

    [Fact]
    public void ClientStorefrontRequestCatalog_ReadExposesCatalogContext()
    {
        byte[] packetData = BuildStorefrontRequestCatalogPacket(catalogContext: 0x1234);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var message = new ClientStorefrontRequestCatalog();
        message.Read(reader);

        Assert.Equal((ushort)0x1234, message.CatalogContext);
    }

    [Fact]
    public void ClientStorefrontPurchaseCharacter_ReadExposesMappedPurchaseFields()
    {
        var target = new Identity
        {
            RealmId = 12,
            Id = 3456ul
        };

        byte[] packetData = BuildStorefrontPurchaseCharacterPacket(
            offerId: 77u,
            paymentCurrencySlot: 17,
            purchaseMoneyAmountBits: 0x10203040u,
            currencyId: 123,
            purchaseOptionId: 0x50607080u,
            target: target,
            purchaseExtensionId: 0x90A0B0C0u);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var message = new ClientStorefrontPurchaseCharacter();
        message.Read(reader);

        Assert.Equal(77u, message.OfferId);
        Assert.Equal((byte)17, message.PaymentCurrencySlot);
        Assert.Equal(0x10203040u, message.PurchaseMoneyAmountBits);
        Assert.Equal((ushort)123, message.CurrencyId);
        Assert.Equal(0x50607080u, message.PurchaseOptionId);
        Assert.Equal(0x90A0B0C0u, message.PurchaseExtensionId);
        Assert.Equal((ushort)12, message.Target.RealmId);
        Assert.Equal(3456ul, message.Target.Id);
    }

    [Fact]
    public void ClientStorefrontPurchaseAccount_ReadExposesMappedPurchaseFields()
    {
        var target = new Identity
        {
            RealmId = 2,
            Id = 111ul
        };

        var accountTarget = new Identity
        {
            RealmId = 3,
            Id = 222ul
        };

        byte[] packetData = BuildStorefrontPurchaseAccountPacket(
            offerId: 88u,
            paymentCurrencySlot: 9,
            purchaseMoneyAmountBits: 0x11223344u,
            currencyId: 321,
            purchaseOptionId: 0x55667788u,
            target: target,
            purchaseExtensionId: 0x99AABBCCu,
            accountTrailingField: 0xDDEEFF00u,
            accountTarget: accountTarget,
            recipientName: "Gift Recipient");

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var message = new ClientStorefrontPurchaseAccount();
        message.Read(reader);

        Assert.Equal(88u, message.OfferId);
        Assert.Equal((byte)9, message.PaymentCurrencySlot);
        Assert.Equal(0x11223344u, message.PurchaseMoneyAmountBits);
        Assert.Equal((ushort)321, message.CurrencyId);
        Assert.Equal(0x55667788u, message.PurchaseOptionId);
        Assert.Equal(0x99AABBCCu, message.PurchaseExtensionId);
        Assert.Equal(0xDDEEFF00u, message.AccountPurchaseExtensionId);
        Assert.Equal((ushort)2, message.Target.RealmId);
        Assert.Equal(111ul, message.Target.Id);
        Assert.Equal((ushort)3, message.AccountTarget.RealmId);
        Assert.Equal(222ul, message.AccountTarget.Id);
        Assert.Equal("Gift Recipient", message.RecipientName);
    }

    [Fact]
    public void ClientAccountItemGiftPendingItemGroupToAccount_ReadExposesReservedZeroField()
    {
        var senderCharacter = new Identity
        {
            RealmId = 11,
            Id = 4567ul
        };

        byte[] packetData = BuildAccountItemGiftPendingItemGroupToAccountPacket(
            group: "gift-group",
            targetAccountId: 99ul,
            reservedZero: 0u,
            senderCharacter: senderCharacter);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var message = new ClientAccountItemGiftPendingItemGroupToAccount();
        message.Read(reader);

        Assert.Equal("gift-group", message.Group);
        Assert.Equal(99ul, message.TargetAccountId);
        Assert.Equal(0u, message.ReservedZero);
        Assert.Equal((ushort)11, message.SenderCharacter.RealmId);
        Assert.Equal(4567ul, message.SenderCharacter.Id);
    }

    [Fact]
    public void ServerAccountCurrencyGrant_WriteSerializesReasonAndReservedTail()
    {
        var message = new ServerAccountCurrencyGrant
        {
            AccountCurrency = new AccountCurrency
            {
                AccountCurrencyType = 3,
                Amount = 42ul
            },
            Reason = 99ul,
            Reserved = 0ul
        };

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal((byte)3, reader.ReadByte(5u));
        Assert.Equal(42ul, reader.ReadULong());
        Assert.Equal(99ul, reader.ReadULong());
        Assert.Equal(0ul, reader.ReadULong());
    }

    [Fact]
    public void ServerAccountItemCooldowns_WriteSerializesDecodedCooldownList()
    {
        var message = new ServerAccountItemCooldowns(
        [
            new ServerAccountItemCooldowns.Cooldown
            {
                AccountItemCooldownGroup = 0x10203040u,
                CooldownInSeconds        = 0x50607080u
            },
            new ServerAccountItemCooldowns.Cooldown
            {
                AccountItemCooldownGroup = 0x90A0B0C0u,
                CooldownInSeconds        = 0xD0E0F000u
            }
        ]);

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(0x90A0B0C0u, reader.ReadUInt());
        Assert.Equal(0xD0E0F000u, reader.ReadUInt());
    }

    [Fact]
    public void ServerDailyLoginUpdate_WriteSerializesDecodedPayload()
    {
        var message = new ServerDailyLoginUpdate
        {
            Value0 = 0x01020304u,
            Value1 = 0x05060708u,
            Value2 = 0x11121314u,
            Value3 = 0x15161718u,
            Value4 = 0x21222324u,
            FloatValue = 12.5f,
            UInt3Value = 5u
        };

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x05060708u, reader.ReadUInt());
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal(0x15161718u, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(12.5f, reader.ReadSingle());
        Assert.Equal(5u, reader.ReadUInt(3u));
    }

    [Fact]
    public void ServerAccountPrivilegeRestrictionUpdate_WriteSerializesDecodedPayload()
    {
        var message = new ServerAccountPrivilegeRestrictionUpdate
        {
            UInt3Value = 6u,
            FloatValue = 0.75f
        };

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(6u, reader.ReadUInt(3u));
        Assert.Equal(0.75f, reader.ReadSingle());
    }

    [Fact]
    public void ServerAccountItemCacheAdd_WriteSerializesDecodedAccountItemPayload()
    {
        var message = new ServerAccountItemCacheAdd
        {
            UnusedLeadingField = 0x11223344u,
            AccountItem = new AccountInventoryItem
            {
                Id         = 0x0102030405060708ul,
                ItemId     = 77u,
                ClaimState = AccountItemClaimState.CanClaim,
                HasTargetPlayerIdentity = true,
                TargetPlayerIdentity = new Identity
                {
                    RealmId = 12,
                    Id      = 0x1112131415161718ul
                }
            }
        };

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(77u, reader.ReadUInt());
        Assert.Equal((uint)AccountItemClaimState.CanClaim, reader.ReadUInt(5u));
        Assert.True(reader.ReadBit());
        Assert.Equal(12u, reader.ReadUInt(14u));
        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
    }

    [Fact]
    public void ServerAccountItemCacheListAppend_WriteSerializesDecodedAccountItemListPayload()
    {
        var message = new ServerAccountItemCacheListAppend
        {
            UnusedLeadingField = 0x55667788u
        };
        message.AccountItems.Add(new AccountInventoryItem
        {
            Id         = 0x0102030405060708ul,
            ItemId     = 123u,
            ClaimState = AccountItemClaimState.AccountMaxed,
            HasTargetPlayerIdentity = true,
            TargetPlayerIdentity = new Identity
            {
                RealmId = 9,
                Id      = 0x1112131415161718ul
            }
        });

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x55667788u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(123u, reader.ReadUInt());
        Assert.Equal((uint)AccountItemClaimState.AccountMaxed, reader.ReadUInt(5u));
        Assert.True(reader.ReadBit());
        Assert.Equal(9u, reader.ReadUInt(14u));
        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
    }

    [Fact]
    public void ServerAccountItemCacheRemove_WriteSerializesDecodedUInt32AndAccountInventoryItemId()
    {
        var message = new ServerAccountItemCacheRemove
        {
            UnusedLeadingField = 0x10203040u,
            AccountInventoryItemId = 0x0102030405060708ul
        };

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
    }

    [Fact]
    public void ServerAccountPendingItemAdd_WriteSerializesDecodedPendingAccountItemGroupPayload()
    {
        var message = new ServerAccountPendingItemAdd
        {
            PendingGroup = new ServerAccountItemsPending.PendingAccountItemGroup
            {
                Id            = 0x0102030405060708ul,
                AccountItemId = 321u,
                Unknown2           = 0x1112131415161718ul,
                Group              = "gift-group",
                SenderAccountId    = 0x55667788u,
                SenderIdentity = new Identity
                {
                    RealmId = 14,
                    Id      = 0x2122232425262728ul
                },
                TargetAccountId           = 0x3132333435363738ul,
                ClaimState                = AccountItemClaimState.AccountMaxedWithPending,
                HasTargetPlayerIdentity   = 0x4142434445464748ul,
                TargetIdentity = new Identity
                {
                    RealmId = 15,
                    Id      = 0x5152535455565758ul
                }
            }
        };

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(321u, reader.ReadUInt());
        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
        Assert.Equal("gift-group", reader.ReadWideString());
        Assert.Equal(0x55667788u, reader.ReadUInt());
        Assert.Equal(14u, reader.ReadUInt(14u));
        Assert.Equal(0x2122232425262728ul, reader.ReadULong());
        Assert.Equal(0x3132333435363738ul, reader.ReadULong());
        Assert.Equal((uint)AccountItemClaimState.AccountMaxedWithPending, reader.ReadUInt(5u));
        Assert.Equal(0x4142434445464748ul, reader.ReadULong());
        Assert.Equal(15u, reader.ReadUInt(14u));
        Assert.Equal(0x5152535455565758ul, reader.ReadULong());
    }

    [Fact]
    public void ServerCREDDOperationHistory_WriteSerializesDecodedRows()
    {
        var message = new ServerCREDDOperationHistory();
        message.Rows.Add(new ServerUnresolvedAccountIdentityRowListPayload.Row
        {
            Operation     = 0x10203040u,
            IsInitiator   = true,
            LogAgeMinutes = 0x50607080u,
            Identity0 = new Identity
            {
                RealmId = 17,
                Id      = 0x0102030405060708ul
            },
            Identity1 = new Identity
            {
                RealmId = 18,
                Id      = 0x1112131415161718ul
            },
            FriendCharacterId = 0x2122232425262728ul,
            MoneyAmount       = 0x3132333435363738ul
        });

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(17u, reader.ReadUInt(14u));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(18u, reader.ReadUInt(14u));
        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
        Assert.Equal(0x2122232425262728ul, reader.ReadULong());
        Assert.Equal(0x3132333435363738ul, reader.ReadULong());
    }

    [Fact]
    public void ServerCREDDExchangeOrderCacheRows_WriteSerializesDecodedULongUInt14UInt7Rows()
    {
        var message = new ServerCREDDExchangeOrderCacheRows();
        message.Rows.Add(new ServerCREDDExchangeOrderCacheRows.Row
        {
            OrderId      = 0x0102030405060708ul,
            CreditAmount = 0x1234u,
            SideFlag     = 0x55u
        });

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x55u, reader.ReadUInt(7u));
    }

    [Fact]
    public void ServerAccountStoreSmallShapes_WriteDecodedFields()
    {
        Assert.Empty(WritePacket(new ServerAccountPendingItemsClear()));
        Assert.Empty(WritePacket(new ServerStoreCatalogUpdated()));

        using (var stream = new MemoryStream(WritePacket(new ServerCREDDRedeemResult(0x2Au))))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x2Au, reader.ReadUInt(6u));
        }

        using (var stream = new MemoryStream(WritePacket(new ServerAccountPendingItemGroupDelete { Value = "pending", Flag = true })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal("pending", reader.ReadWideString());
            Assert.True(reader.ReadBit());
        }

        using (var stream = new MemoryStream(WritePacket(new ServerAccountPendingItemDelete { Value = 0x0102030405060708ul, Flag = true })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x0102030405060708ul, reader.ReadULong());
            Assert.True(reader.ReadBit());
        }

        using (var stream = new MemoryStream(WritePacket(new ServerStorePurchaseOfferResult { IsSuccess = true, DisplayType = PurchaseResultDisplayType.NothingToClaim })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.True(reader.ReadBit());
            Assert.Equal((uint)PurchaseResultDisplayType.NothingToClaim, reader.ReadUInt(5u));
        }

        using (var stream = new MemoryStream(WritePacket(new ServerWalletUpdate(0xA0B0C0D0u))))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0xA0B0C0D0u, reader.ReadUInt());
        }

        using (var stream = new MemoryStream(WritePacket(new ServerAccountUInt64Payload(0x0102030405060708ul))))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        }

        using (var stream = new MemoryStream(WritePacket(new ServerStoreError(StoreError.IneligibleGiftRecipient))))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal((uint)StoreError.IneligibleGiftRecipient, reader.ReadUInt(5u));
        }

        using (var stream = new MemoryStream(WritePacket(new ServerStorePurchaseOfferResultVariant { IsSuccess = true, DisplayType = PurchaseResultDisplayType.VIPSubscription })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.True(reader.ReadBit());
            Assert.Equal((uint)PurchaseResultDisplayType.VIPSubscription, reader.ReadUInt(5u));
        }

        using (var stream = new MemoryStream(WritePacket(new ServerStoreCompleteOrderVirtualCurrencyPackageResult { Flag = true, Value = 0x1Cu })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.True(reader.ReadBit());
            Assert.Equal(0x1Cu, reader.ReadUInt(5u));
        }
    }

    [Fact]
    public void ServerStoreAuxiliaryShapes_WriteDecodedFields()
    {
        var orderRows = new ServerStorePurchaseHistoryReady();
        orderRows.Rows.Add(new ServerUnresolvedStoreRowListPayload.Row
        {
            Value0      = 0x0102030405060708ul,
            Value1      = 0x1112131415161718ul,
            UInt5Value  = 0x1Bu,
            UInt3Value  = 0x5u,
            FloatValue  = 12.5f,
            StringValue = "order-row",
            Flag        = true,
            Value7      = 0x2122232425262728ul,
            Value8      = 0xA0B0C0D0u
        });

        using (var stream = new MemoryStream(WritePacket(orderRows)))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(0x0102030405060708ul, reader.ReadULong());
            Assert.Equal(0x1112131415161718ul, reader.ReadULong());
            Assert.Equal(0x1Bu, reader.ReadUInt(5u));
            Assert.Equal(0x5u, reader.ReadUInt(3u));
            Assert.Equal(12.5f, reader.ReadSingle());
            Assert.Equal("order-row", reader.ReadWideString());
            Assert.True(reader.ReadBit());
            Assert.Equal(0x2122232425262728ul, reader.ReadULong());
            Assert.Equal(0xA0B0C0D0u, reader.ReadUInt());
        }

        var currencyPackage = new ServerStoreCurrencyPackageRow
        {
            Value0      = 0x10203040u,
            StringValue = "currency",
            Value2      = 0x50607080u,
            FloatValue  = 7.25f,
            Value4      = 0x90A0B0C0u
        };

        using var resultStream = new MemoryStream(WritePacket(currencyPackage));
        using var resultReader = new GamePacketReader(resultStream);

        Assert.Equal(0x10203040u, resultReader.ReadUInt());
        Assert.Equal("currency", resultReader.ReadWideString());
        Assert.Equal(0x50607080u, resultReader.ReadUInt());
        Assert.Equal(7.25f, resultReader.ReadSingle());
        Assert.Equal(0x90A0B0C0u, resultReader.ReadUInt());
    }

    [Fact]
    public void ServerStorePurchaseVirtualCurrencyPackageResult_WriteSerializesDecodedShape()
    {
        var message = new ServerStorePurchaseVirtualCurrencyPackageResult
        {
            Flag       = true,
            UInt5Value = 17u,
            String0    = "s0",
            String1    = "s1",
            String2    = "s2",
            String3    = "s3",
            String4    = "s4",
            String5    = "s5",
            String6    = "s6",
            String7    = "s7",
            Float0     = 1.25f,
            Float1     = 2.5f,
            Float2     = 3.75f,
            String8    = "s8"
        };

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.True(reader.ReadBit());
        Assert.Equal(17u, reader.ReadUInt(5u));
        Assert.Equal("s0", reader.ReadWideString());
        Assert.Equal("s1", reader.ReadWideString());
        Assert.Equal("s2", reader.ReadWideString());
        Assert.Equal("s3", reader.ReadWideString());
        Assert.Equal("s4", reader.ReadWideString());
        Assert.Equal("s5", reader.ReadWideString());
        Assert.Equal("s6", reader.ReadWideString());
        Assert.Equal("s7", reader.ReadWideString());
        Assert.Equal(1.25f, reader.ReadSingle());
        Assert.Equal(2.5f, reader.ReadSingle());
        Assert.Equal(3.75f, reader.ReadSingle());
        Assert.Equal("s8", reader.ReadWideString());
    }

    [Fact]
    public void ServerHousingResidenceKeyedUpdate_WriteSerializesMappedFields()
    {
        var message = new ServerHousingResidenceKeyedUpdate
        {
            Key        = 0x0102030405060708ul,
            Unknown0   = 0xA0B0C0D0u
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0xA0B0C0D0u, reader.ReadUInt());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void HousingClusterPlaceholderPackets_WriteReaderBackedShapes()
    {
        Assert.Empty(WritePacket(new ServerHousingResidenceEmpty()));
        Assert.Empty(WritePacket(new ServerHousingResidenceEmptyFollowUp()));
        Assert.Empty(WritePacket(new ServerHousingBasicsEmpty()));

        using (var stream = new MemoryStream(WritePacket(new ServerHousingResidenceUInt15 { Value = 0x1234u })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x1234u, reader.ReadUInt(15u));
            Assert.Equal(stream.Length, stream.Position);
        }

        using (var stream = new MemoryStream(WritePacket(new ServerHousingResidenceUInt15Alt { Value = 0x2345u })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x2345u, reader.ReadUInt(15u));
            Assert.Equal(stream.Length, stream.Position);
        }

        using (var stream = new MemoryStream(WritePacket(new ServerHousingResidenceWideString { Text = "Housing" })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal("Housing", reader.ReadWideString());
            Assert.Equal(stream.Length, stream.Position);
        }

        var basicsFollowup = new ServerHousingBasicsFollowup
        {
            Value0 = 0x01020304u,
            Value1 = 0x23456u,
            Value2 = 0xA0B0C0D0u,
            Value3 = 0x7Fu
        };

        using (var stream = new MemoryStream(WritePacket(basicsFollowup)))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x01020304u, reader.ReadUInt());
            Assert.Equal(0x23456u, reader.ReadUInt(18u));
            Assert.Equal(0xA0B0C0D0u, reader.ReadUInt());
            Assert.Equal(0x7Fu, reader.ReadUInt(8u));
            Assert.Equal(stream.Length, stream.Position);
        }
    }

    [Fact]
    public void ClusterAuxPackets_WriteRepresentativeReaderBackedShapes()
    {
        Assert.Equal(new byte[] { 0xAB }, WritePacket(new ServerItemContextActionAck([0xAB])));
        Assert.Equal(8, WritePacket(new ServerSupplySatchelAux()).Length);
        Assert.Equal(0x20, WritePacket(new ServerChatAuxPayload()).Length);

        using var stream = new MemoryStream(WritePacket(new ServerRecruitmentAuxFourUInt32
        {
            Value0 = 0x01020304u,
            Value1 = 0x05060708u,
            Value2 = 0x090A0B0Cu,
            Value3 = 0x0D0E0F10u
        }));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x05060708u, reader.ReadUInt());
        Assert.Equal(0x090A0B0Cu, reader.ReadUInt());
        Assert.Equal(0x0D0E0F10u, reader.ReadUInt());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerSpellUInt32TripletList_WriteSerializesCountedRows()
    {
        var message = new ServerSpellUInt32TripletList();
        message.Rows.Add(new ServerSpellUInt32TripletListRow
        {
            Value0 = 1u,
            Value1 = 2u,
            Value2 = 3u
        });
        message.Rows.Add(new ServerSpellUInt32TripletListRow
        {
            Value0 = 4u,
            Value1 = 5u,
            Value2 = 6u
        });

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(5u, reader.ReadUInt());
        Assert.Equal(6u, reader.ReadUInt());
    }

    [Fact]
    public void ServerSpellFourUInt32_WriteSerializesMappedFields()
    {
        var message = new ServerSpellFourUInt32
        {
            Value0 = 0x10203040u,
            Value1 = 0x50607080u,
            Value2 = 0x90A0B0C0u,
            Value3 = 0xD0E0F001u
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(0x90A0B0C0u, reader.ReadUInt());
        Assert.Equal(0xD0E0F001u, reader.ReadUInt());
    }

    [Fact]
    public void ServerMatchingManagerFlag_WriteSerializesFlag()
    {
        var message = new ServerMatchingManagerFlag
        {
            Flag = true
        };

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.True(reader.ReadBit());
    }

    [Fact]
    public void ServerCinematicDelayFlag_WriteSerializesDelayAndFlag()
    {
        var message = new ServerCinematicDelayFlag
        {
            Delay = 1500u,
            Flag = true
        };

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(1500u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
    }

    [Fact]
    public void ClientCinematicCameraSubjectPosVel_ReadInitializesPosition()
    {
        byte[] packetData = BuildCinematicCameraSubjectPosVelPacket();

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var message = new ClientCinematicCameraSubjectPosVel();
        message.Read(reader);

        Assert.Equal(1.25f, message.Position.Vector.X);
        Assert.Equal(-2.5f, message.Position.Vector.Y);
        Assert.Equal(3.75f, message.Position.Vector.Z);
        Assert.Equal(4.5f, message.Velocity.X);
        Assert.Equal(-5.5f, message.Velocity.Y);
        Assert.Equal(6.5f, message.Velocity.Z);
    }

    [Fact]
    public void ServerEntityDestroy_WriteSerializesGuidAndFlag()
    {
        var message = new ServerEntityDestroy
        {
            Guid = 0x10203040u,
            Flag = true
        };

        byte[] packetData = WritePacket(message);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
    }

    [Fact]
    public void CinematicFlags_UsesTransitionDurationSet_MatchesTransitionDurationBit()
    {
        Assert.Equal(0x0002, (ushort)CinematicFlags.UsesTransitionDurationSet);
    }

    [Fact]
    public void ClientHousingPlugUpdate_ReadExposesReservedAndContributionRecords()
    {
        uint[] contributionData = Enumerable.Range(0, ClientHousingPlugUpdate.ContributionRecordCount * 5)
            .Select(static value => (uint)value + 1u)
            .ToArray();
        byte[] packetData = BuildHousingPlugUpdatePacket(
            realmId: 7,
            identityId: 1234ul,
            housingPlotInfoId: 55u,
            housingPlugItemId: 77u,
            plugFacing: HousingPlugFacing.West,
            reserved: 9u,
            operation: ClientHousingPlugUpdate.PlugUpdateOperation.Remove,
            contributionData: contributionData);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var message = new ClientHousingPlugUpdate();
        message.Read(reader);

        Assert.Equal((ushort)7, message.Identity.RealmId);
        Assert.Equal(1234ul, message.Identity.Id);
        Assert.Equal(55u, message.HousingPlotInfoId);
        Assert.Equal(77u, message.HousingPlugItemId);
        Assert.Equal(HousingPlugFacing.West, message.PlugFacing);
        Assert.Equal(9u, message.Reserved);
        Assert.Equal(ClientHousingPlugUpdate.PlugUpdateOperation.Remove, message.Operation);
        Assert.Equal(ClientHousingPlugUpdate.ContributionRecordCount, message.Contributions.Count);
        Assert.Equal(1u, message.Contributions[0].ContributionPointRequirement);
        Assert.Equal(2u, message.Contributions[0].Reserved0);
        Assert.Equal(3u, message.Contributions[0].Reserved1);
        Assert.Equal(4u, message.Contributions[0].Reserved2);
        Assert.Equal(5u, message.Contributions[0].Reserved3);
        Assert.Equal(21u, message.Contributions[4].ContributionPointRequirement);
        Assert.Equal(25u, message.Contributions[4].Reserved3);
    }

    [Fact]
    public void ServerStoreOfferItemData_WriteSerializesTypeSpecificAccountItemFields()
    {
        var type1 = new ServerStoreOffers.OfferGroup.Offer.OfferItemData
        {
            Type               = 1u,
            Type1AccountItemId = 123u,
            Type1Amount        = 4u,
            Amount             = 99u
        };

        byte[] type1Packet = WritePacket(type1);

        using (var stream = new MemoryStream(type1Packet))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(123u, reader.ReadUInt());
            Assert.Equal(4u, reader.ReadUInt());
            Assert.Equal(99u, reader.ReadUInt());
        }

        var type2 = new ServerStoreOffers.OfferGroup.Offer.OfferItemData
        {
            Type               = 2u,
            Type2AccountItemId = 987u,
            Amount             = 55u
        };

        byte[] type2Packet = WritePacket(type2);

        using var type2Stream = new MemoryStream(type2Packet);
        using var type2Reader = new GamePacketReader(type2Stream);

        Assert.Equal(2u, type2Reader.ReadUInt());
        Assert.Equal(987u, type2Reader.ReadUInt());
        Assert.Equal(55u, type2Reader.ReadUInt());
    }

    private static byte[] BuildPackedWorldPacket(byte envelopeType, byte[] payload)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(envelopeType, 5u);
            writer.FlushBits();
            writer.Write((uint)(payload.Length + 4));
            writer.WriteBytes(payload);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static byte[] BuildStorefrontRequestCatalogPacket(ushort catalogContext)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(catalogContext, 14u);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static byte[] BuildStorefrontPurchaseCharacterPacket(
        uint offerId,
        byte paymentCurrencySlot,
        uint purchaseMoneyAmountBits,
        ushort currencyId,
        uint purchaseOptionId,
        Identity target,
        uint purchaseExtensionId)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(offerId);
            writer.Write(paymentCurrencySlot, 5u);
            writer.Write(purchaseMoneyAmountBits);
            writer.Write(currencyId, 14u);
            writer.Write(purchaseOptionId);
            target.Write(writer);
            writer.Write(purchaseExtensionId);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static byte[] BuildStorefrontPurchaseAccountPacket(
        uint offerId,
        byte paymentCurrencySlot,
        uint purchaseMoneyAmountBits,
        ushort currencyId,
        uint purchaseOptionId,
        Identity target,
        uint purchaseExtensionId,
        uint accountTrailingField,
        Identity accountTarget,
        string recipientName)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(offerId);
            writer.Write(paymentCurrencySlot, 5u);
            writer.Write(purchaseMoneyAmountBits);
            writer.Write(currencyId, 14u);
            writer.Write(purchaseOptionId);
            target.Write(writer);
            writer.Write(purchaseExtensionId);
            writer.Write(accountTrailingField);
            accountTarget.Write(writer);
            writer.WriteStringWide(recipientName);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static byte[] BuildAccountItemGiftPendingItemGroupToAccountPacket(
        string group,
        ulong targetAccountId,
        uint reservedZero,
        Identity senderCharacter)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.WriteStringWide(group);
            writer.Write(targetAccountId);
            writer.Write(reservedZero);
            senderCharacter.Write(writer);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static byte[] BuildCinematicCameraSubjectPosVelPacket()
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(1.25f);
            writer.Write(-2.5f);
            writer.Write(3.75f);
            writer.Write(4.5f);
            writer.Write(-5.5f);
            writer.Write(6.5f);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static byte[] WritePacket(IWritable message)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            message.Write(writer);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static byte[] BuildHousingPlugUpdatePacket(
        ushort realmId,
        ulong identityId,
        uint housingPlotInfoId,
        uint housingPlugItemId,
        HousingPlugFacing plugFacing,
        uint reserved,
        ClientHousingPlugUpdate.PlugUpdateOperation operation,
        uint[] contributionData)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            var identity = new Identity
            {
                RealmId = realmId,
                Id = identityId
            };

            identity.Write(writer);
            writer.Write(housingPlotInfoId);
            writer.Write(housingPlugItemId);
            writer.Write(plugFacing, 32u);
            writer.Write(reserved);
            writer.Write(operation, 3u);
            foreach (uint value in contributionData)
                writer.Write(value);
            writer.FlushBits();
        }

        return stream.ToArray();
    }
}
