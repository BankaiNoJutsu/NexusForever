using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Cinematic;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Static.Housing;

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
    public void ClientStorefrontPurchaseCharacter_ReadExposesSelector()
    {
        var target = new Identity
        {
            RealmId = 12,
            Id = 3456ul
        };

        byte[] packetData = BuildStorefrontPurchaseCharacterPacket(
            offerId: 77u,
            selector: 17,
            purchaseField0: 0x10203040u,
            currencyId: 123,
            purchaseField1: 0x50607080u,
            target: target,
            purchaseField2: 0x90A0B0C0u);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var message = new ClientStorefrontPurchaseCharacter();
        message.Read(reader);

        Assert.Equal(77u, message.OfferId);
        Assert.Equal((byte)17, message.Selector);
        Assert.Equal((ushort)123, message.CurrencyId);
        Assert.Equal((ushort)12, message.Target.RealmId);
        Assert.Equal(3456ul, message.Target.Id);
    }

    [Fact]
    public void ClientStorefrontPurchaseAccount_ReadExposesSelector()
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
            selector: 9,
            purchaseField0: 0x11223344u,
            currencyId: 321,
            purchaseField1: 0x55667788u,
            target: target,
            purchaseField2: 0x99AABBCCu,
            accountField: 0xDDEEFF00u,
            accountTarget: accountTarget,
            recipientName: "Gift Recipient");

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var message = new ClientStorefrontPurchaseAccount();
        message.Read(reader);

        Assert.Equal(88u, message.OfferId);
        Assert.Equal((byte)9, message.Selector);
        Assert.Equal((ushort)321, message.CurrencyId);
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
    public void ClientHousingPlugUpdate_ReadExposesReservedField()
    {
        byte[] contributionData = Enumerable.Range(0, 100).Select(static value => (byte)value).ToArray();
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
        Assert.Equal(contributionData, message.ContributionData);
    }

    [Fact]
    public void ServerStoreOfferItemData_WriteSerializesTypeSpecificAccountItemFields()
    {
        var type1 = new ServerStoreOffers.OfferGroup.Offer.OfferItemData
        {
            Type               = 1u,
            Type1AccountItemId = 123u,
            Type1Amount        = 4u
        };

        byte[] type1Packet = WritePacket(type1);

        using (var stream = new MemoryStream(type1Packet))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(123u, reader.ReadUInt());
            Assert.Equal(4u, reader.ReadUInt());
        }

        var type2 = new ServerStoreOffers.OfferGroup.Offer.OfferItemData
        {
            Type               = 2u,
            Type2AccountItemId = 987u
        };

        byte[] type2Packet = WritePacket(type2);

        using var type2Stream = new MemoryStream(type2Packet);
        using var type2Reader = new GamePacketReader(type2Stream);

        Assert.Equal(2u, type2Reader.ReadUInt());
        Assert.Equal(987u, type2Reader.ReadUInt());
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
        byte selector,
        uint purchaseField0,
        ushort currencyId,
        uint purchaseField1,
        Identity target,
        uint purchaseField2)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(offerId);
            writer.Write(selector, 5u);
            writer.Write(purchaseField0);
            writer.Write(currencyId, 14u);
            writer.Write(purchaseField1);
            target.Write(writer);
            writer.Write(purchaseField2);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static byte[] BuildStorefrontPurchaseAccountPacket(
        uint offerId,
        byte selector,
        uint purchaseField0,
        ushort currencyId,
        uint purchaseField1,
        Identity target,
        uint purchaseField2,
        uint accountField,
        Identity accountTarget,
        string recipientName)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(offerId);
            writer.Write(selector, 5u);
            writer.Write(purchaseField0);
            writer.Write(currencyId, 14u);
            writer.Write(purchaseField1);
            target.Write(writer);
            writer.Write(purchaseField2);
            writer.Write(accountField);
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
        byte[] contributionData)
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
            writer.WriteBytes(contributionData);
            writer.FlushBits();
        }

        return stream.ToArray();
    }
}