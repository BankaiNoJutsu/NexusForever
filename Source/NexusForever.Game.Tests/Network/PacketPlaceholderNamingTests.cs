using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Cinematic;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Network.World.Message.Model.Pet;
using NexusForever.Network.World.Message.Model.PublicEvent;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Model.Story;
using NexusForever.Network.World.Message.Model.Story.Message;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Static.Pet;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Game.Static.Story;
using NexusForever.Game.Static.Storefront;
using CharacterClass = NexusForever.Game.Static.Entity.Class;
using CharacterRace = NexusForever.Game.Static.Entity.Race;
using CharacterSex = NexusForever.Game.Static.Entity.Sex;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;
using ReputationFaction = NexusForever.Game.Static.Reputation.Faction;

namespace NexusForever.Game.Tests.Network;

public class PacketPlaceholderNamingTests
{
    [Fact]
    public void OpcodePlaceholderInventory_MatchesFreshEvidenceQueue()
    {
        string[] expectedClientPlaceholders =
        [
            nameof(GameMessageOpcode.Client0x00C8),
            nameof(GameMessageOpcode.Client0x00ED),
            nameof(GameMessageOpcode.Client0x011B),
            nameof(GameMessageOpcode.Client0x011D),
            nameof(GameMessageOpcode.Client0x012D),
            nameof(GameMessageOpcode.Client0x0550),
            nameof(GameMessageOpcode.Client0x062A),
            nameof(GameMessageOpcode.Client0x0634),
            nameof(GameMessageOpcode.Client0x063E),
            nameof(GameMessageOpcode.Client0x0701),
            nameof(GameMessageOpcode.Client0x07E3),
            nameof(GameMessageOpcode.Client0x0928)
        ];
        string[] clientPlaceholders = Enum.GetNames<GameMessageOpcode>()
            .Where(n => n.StartsWith("Client0x", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedClientPlaceholders, clientPlaceholders);

        string[] serverPlaceholders = Enum.GetNames<GameMessageOpcode>()
            .Where(n => n.StartsWith("Server0x", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        string[] expectedServerPlaceholders = [nameof(GameMessageOpcode.Server0x0015)];
        Assert.Equal(expectedServerPlaceholders, serverPlaceholders);
    }

    [Fact]
    public void Client0x00C8_RemainsDiagnosticUntilOpcodeSpecificSenderIsProven()
    {
        Assert.Equal((ushort)0x00C8, (ushort)GameMessageOpcode.Client0x00C8);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientMatchingQueueLeave, (ushort)GameMessageOpcode.Client0x00C8);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientMatchingQueueLeaveAsGroup, (ushort)GameMessageOpcode.Client0x00C8);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientChallengeChoice, (ushort)GameMessageOpcode.Client0x00C8);

        Assert.NotNull(typeof(Client0x00C8).GetProperty(nameof(Client0x00C8.MatchType)));
        Assert.Null(typeof(Client0x00C8).GetProperty("ChallengeId"));
        Assert.Null(typeof(Client0x00C8).GetProperty("Choice"));
        Assert.Null(typeof(Client0x00C8).GetProperty("QueueId"));
    }

    [Fact]
    public void Client0x00ED_RemainsDiagnosticUntilOpcodeSpecificOwnerIsProven()
    {
        Assert.Equal((ushort)0x00ED, (ushort)GameMessageOpcode.Client0x00ED);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientDuelAccept, (ushort)GameMessageOpcode.Client0x00ED);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientDuelDecline, (ushort)GameMessageOpcode.Client0x00ED);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientDuelForfeit, (ushort)GameMessageOpcode.Client0x00ED);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientDuelInitiate, (ushort)GameMessageOpcode.Client0x00ED);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientMailSend, (ushort)GameMessageOpcode.Client0x00ED);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientPathScientistDismissScanbot, (ushort)GameMessageOpcode.Client0x00ED);

        AssertNeutralProperties<Client0x00ED>(
            nameof(Client0x00ED.Value0),
            nameof(Client0x00ED.Value1),
            nameof(Client0x00ED.Value2),
            nameof(Client0x00ED.Value3),
            nameof(Client0x00ED.Value4),
            nameof(Client0x00ED.Value5));
        Assert.Null(typeof(Client0x00ED).GetProperty("OpponentUnitId"));
        Assert.Null(typeof(Client0x00ED).GetProperty("MailId"));
        Assert.Null(typeof(Client0x00ED).GetProperty("Recipient"));
        Assert.Null(typeof(Client0x00ED).GetProperty("PathMissionId"));
        Assert.Null(typeof(Client0x00ED).GetProperty("ScanbotProfileId"));
    }

    [Fact]
    public void Client0x011BAnd011D_RemainDiagnosticUntilOpcodeSpecificOwnersAreProven()
    {
        Assert.Equal((ushort)0x011B, (ushort)GameMessageOpcode.Client0x011B);
        Assert.Equal((ushort)0x011D, (ushort)GameMessageOpcode.Client0x011D);
        Assert.NotEqual((ushort)GameMessageOpcode.ServerLootBindOnPickup, (ushort)GameMessageOpcode.Client0x011B);
        Assert.NotEqual((ushort)GameMessageOpcode.ServerLootBindOnPickup, (ushort)GameMessageOpcode.Client0x011D);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientMailDelete, (ushort)GameMessageOpcode.Client0x011D);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientMailOpen, (ushort)GameMessageOpcode.Client0x011D);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientMailTakeAttachment, (ushort)GameMessageOpcode.Client0x011D);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientMailTakeCash, (ushort)GameMessageOpcode.Client0x011D);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientLootVacuum, (ushort)GameMessageOpcode.Client0x011B);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientTradeskillResetTalents, (ushort)GameMessageOpcode.Client0x011D);

        Assert.Null(typeof(Client0x011B).GetProperty("Value"));
        Assert.Null(typeof(Client0x011B).GetProperty("LootUnitId"));
        Assert.Null(typeof(Client0x011B).GetProperty("MailId"));
        Assert.Null(typeof(Client0x011B).GetProperty("ItemGuid"));
        Assert.NotNull(typeof(Client0x011D).GetProperty(nameof(Client0x011D.Value)));
        Assert.Null(typeof(Client0x011D).GetProperty("LootUnitId"));
        Assert.Null(typeof(Client0x011D).GetProperty("MailId"));
        Assert.Null(typeof(Client0x011D).GetProperty("ItemGuid"));
        Assert.Null(typeof(Client0x011D).GetProperty("TradeskillId"));
    }

    [Fact]
    public void Client0x012D_RemainsDiagnosticUntilOpcodeSpecificSenderIsProven()
    {
        Assert.Equal((ushort)0x012D, (ushort)GameMessageOpcode.Client0x012D);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientSuggest, (ushort)GameMessageOpcode.Client0x012D);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientAccountItemClaimPendingItemGroup, (ushort)GameMessageOpcode.Client0x012D);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientAccountItemReturnPendingItemGroup, (ushort)GameMessageOpcode.Client0x012D);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientRealmTransfer, (ushort)GameMessageOpcode.Client0x012D);
        Assert.NotEqual((ushort)GameMessageOpcode.Client0x063E, (ushort)GameMessageOpcode.Client0x012D);

        Assert.NotNull(typeof(Client0x012D).GetProperty(nameof(Client0x012D.Text)));
        Assert.Null(typeof(Client0x012D).GetProperty("Suggestion"));
        Assert.Null(typeof(Client0x012D).GetProperty("PendingItemGroup"));
        Assert.Null(typeof(Client0x012D).GetProperty("PendingItemGroupId"));
        Assert.Null(typeof(Client0x012D).GetProperty("RealmId"));
        Assert.Null(typeof(Client0x012D).GetProperty("SelectedCharacterId"));
        Assert.Null(typeof(Client0x012D).GetProperty("PetName"));
    }

    [Fact]
    public void Client0x0550_RemainsDiagnosticUntilOpcodeSpecificSenderIsProven()
    {
        Assert.Equal((ushort)0x0550, (ushort)GameMessageOpcode.Client0x0550);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientICCommChannelJoin, (ushort)GameMessageOpcode.Client0x0550);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientICCommMessage, (ushort)GameMessageOpcode.Client0x0550);
        Assert.NotEqual((ushort)GameMessageOpcode.ServerSpellList, (ushort)GameMessageOpcode.Client0x0550);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientMatchingMatchInitiateLookingForReplacements, (ushort)GameMessageOpcode.Client0x0550);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientMovementControlAck, (ushort)GameMessageOpcode.Client0x0550);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientTradeskillResetTalents, (ushort)GameMessageOpcode.Client0x0550);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientAbilityBookActivateSpell, (ushort)GameMessageOpcode.Client0x0550);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientCombatLogDisableOthers, (ushort)GameMessageOpcode.Client0x0550);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientCombatLogDisables, (ushort)GameMessageOpcode.Client0x0550);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientP2PTradingInitiateTrade, (ushort)GameMessageOpcode.Client0x0550);

        Assert.NotNull(typeof(Client0x0550).GetProperty(nameof(Client0x0550.Value)));
        Assert.Null(typeof(Client0x0550).GetProperty("ChannelId"));
        Assert.Null(typeof(Client0x0550).GetProperty("MessageId"));
        Assert.Null(typeof(Client0x0550).GetProperty("Text"));
        Assert.Null(typeof(Client0x0550).GetProperty("Spell4Id"));
        Assert.Null(typeof(Client0x0550).GetProperty("Roles"));
        Assert.Null(typeof(Client0x0550).GetProperty("Ticket"));
        Assert.Null(typeof(Client0x0550).GetProperty("TradeskillId"));
        Assert.Null(typeof(Client0x0550).GetProperty("TargetUnitId"));
    }

    [Fact]
    public void Client0x063E_RemainsDiagnosticUntilOpcodeSpecificSenderIsProven()
    {
        Assert.Equal((ushort)0x063E, (ushort)GameMessageOpcode.Client0x063E);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientSuggest, (ushort)GameMessageOpcode.Client0x063E);
        Assert.NotEqual((ushort)GameMessageOpcode.Client0x012D, (ushort)GameMessageOpcode.Client0x063E);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientAccountItemClaimPendingItemGroup, (ushort)GameMessageOpcode.Client0x063E);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientAccountItemReturnPendingItemGroup, (ushort)GameMessageOpcode.Client0x063E);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientSupportTicket, (ushort)GameMessageOpcode.Client0x063E);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientRequestCommodityInfo, (ushort)GameMessageOpcode.Client0x063E);
        Assert.NotEqual((ushort)GameMessageOpcode.ClientAuctionsByFilterRequest, (ushort)GameMessageOpcode.Client0x063E);

        Assert.NotNull(typeof(Client0x063E).GetProperty(nameof(Client0x063E.Text)));
        Assert.Null(typeof(Client0x063E).GetProperty("Suggestion"));
        Assert.Null(typeof(Client0x063E).GetProperty("TicketText"));
        Assert.Null(typeof(Client0x063E).GetProperty("FilterText"));
        Assert.Null(typeof(Client0x063E).GetProperty("MarketplaceStatus"));
        Assert.Null(typeof(Client0x063E).GetProperty("AuthDeniedReason"));
        Assert.Null(typeof(Client0x063E).GetProperty("PendingItemGroupId"));
    }

    [Fact]
    public void ClientAddonModuleList_UsesStructuralEvidenceBackedOpcodeName()
    {
        Assert.Equal((ushort)0x07B6, (ushort)GameMessageOpcode.ClientAddonModuleList);

        var attribute = Assert.IsType<MessageAttribute>(Attribute.GetCustomAttribute(typeof(ClientAddonModuleList), typeof(MessageAttribute)));
        Assert.Equal(GameMessageOpcode.ClientAddonModuleList, attribute.Opcode);
    }

    [Theory]
    [InlineData(0x003D, nameof(GameMessageOpcode.ClientAccountRealmData), nameof(ClientAccountRealmData))]
    [InlineData(0x0760, nameof(GameMessageOpcode.ClientRealmListRealmRow), nameof(ClientRealmListRealmRow))]
    [InlineData(0x0762, nameof(GameMessageOpcode.ClientRealmListMessageRow), nameof(ClientRealmListMessageRow))]
    public void ClientRealmListRows_UseServerRealmListStructuralNames(ushort opcode, string expectedOpcodeName, string expectedTypeName)
    {
        Assert.Equal(opcode, (ushort)Enum.Parse<GameMessageOpcode>(expectedOpcodeName));
        Assert.Equal(expectedOpcodeName, Enum.GetName(Enum.Parse<GameMessageOpcode>(expectedOpcodeName)));

        Type packetType = expectedTypeName switch
        {
            nameof(ClientAccountRealmData) => typeof(ClientAccountRealmData),
            nameof(ClientRealmListRealmRow) => typeof(ClientRealmListRealmRow),
            nameof(ClientRealmListMessageRow) => typeof(ClientRealmListMessageRow),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedTypeName))
        };

        var attribute = Assert.IsType<MessageAttribute>(Attribute.GetCustomAttribute(packetType, typeof(MessageAttribute)));
        Assert.Equal(Enum.Parse<GameMessageOpcode>(expectedOpcodeName), attribute.Opcode);
    }

    [Theory]
    [InlineData(0x00C8, nameof(GameMessageOpcode.Client0x00C8), nameof(Client0x00C8))]
    [InlineData(0x00ED, nameof(GameMessageOpcode.Client0x00ED), nameof(Client0x00ED))]
    [InlineData(0x011B, nameof(GameMessageOpcode.Client0x011B), nameof(Client0x011B))]
    [InlineData(0x011D, nameof(GameMessageOpcode.Client0x011D), nameof(Client0x011D))]
    [InlineData(0x012D, nameof(GameMessageOpcode.Client0x012D), nameof(Client0x012D))]
    [InlineData(0x0550, nameof(GameMessageOpcode.Client0x0550), nameof(Client0x0550))]
    [InlineData(0x062A, nameof(GameMessageOpcode.Client0x062A), nameof(Client0x062A))]
    [InlineData(0x0634, nameof(GameMessageOpcode.Client0x0634), nameof(Client0x0634))]
    [InlineData(0x063E, nameof(GameMessageOpcode.Client0x063E), nameof(Client0x063E))]
    [InlineData(0x0701, nameof(GameMessageOpcode.Client0x0701), nameof(Client0x0701))]
    [InlineData(0x07E3, nameof(GameMessageOpcode.Client0x07E3), nameof(Client0x07E3))]
    [InlineData(0x0928, nameof(GameMessageOpcode.Client0x0928), nameof(Client0x0928))]
    public void NumericDiagnosticClientPackets_RemainNumericUntilSemanticOwnerIsProven(ushort opcode, string expectedOpcodeName, string expectedTypeName)
    {
        Assert.Equal(opcode, (ushort)Enum.Parse<GameMessageOpcode>(expectedOpcodeName));
        Assert.Equal(expectedOpcodeName, Enum.GetName(Enum.Parse<GameMessageOpcode>(expectedOpcodeName)));

        Type packetType = expectedTypeName switch
        {
            nameof(Client0x00C8) => typeof(Client0x00C8),
            nameof(Client0x00ED) => typeof(Client0x00ED),
            nameof(Client0x011B) => typeof(Client0x011B),
            nameof(Client0x011D) => typeof(Client0x011D),
            nameof(Client0x012D) => typeof(Client0x012D),
            nameof(Client0x0550) => typeof(Client0x0550),
            nameof(Client0x062A) => typeof(Client0x062A),
            nameof(Client0x0634) => typeof(Client0x0634),
            nameof(Client0x063E) => typeof(Client0x063E),
            nameof(Client0x0701) => typeof(Client0x0701),
            nameof(Client0x07E3) => typeof(Client0x07E3),
            nameof(Client0x0928) => typeof(Client0x0928),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedTypeName))
        };

        Assert.Equal(expectedTypeName, packetType.Name);

        var attribute = Assert.IsType<MessageAttribute>(Attribute.GetCustomAttribute(packetType, typeof(MessageAttribute)));
        Assert.Equal(Enum.Parse<GameMessageOpcode>(expectedOpcodeName), attribute.Opcode);
    }

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
            RestrictionType = 6u,
            DurationDays    = 0.75f
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
        Assert.Equal(0ul, reader.ReadULong());
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

        using (var stream = new MemoryStream(WritePacket(new ServerStoreError(StoreError.CatalogUnavailable))))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal((uint)StoreError.CatalogUnavailable, reader.ReadUInt(5u));
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
            Id           = 0x10203040u,
            Name         = "currency",
            Count        = 0x50607080u,
            Price        = 7.25f,
            CurrencyType = AccountCurrencyType.NCoin
        };

        using var resultStream = new MemoryStream(WritePacket(currencyPackage));
        using var resultReader = new GamePacketReader(resultStream);

        Assert.Equal(0x10203040u, resultReader.ReadUInt());
        Assert.Equal("currency", resultReader.ReadWideString());
        Assert.Equal(0x50607080u, resultReader.ReadUInt());
        Assert.Equal(7.25f, resultReader.ReadSingle());
        Assert.Equal((uint)AccountCurrencyType.NCoin, resultReader.ReadUInt());
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
            Key = 0x0102030405060708ul
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0u, reader.ReadUInt());
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
    public void ServerRealmTransferDestinationsAux_WriteSerializesMappedEnvelope()
    {
        byte[] payload = [0xAA, 0xBB, 0xCC, 0xDD];
        byte[] packetData = WritePacket(new ServerRealmTransferDestinationsAux
        {
            Value = 0x01020304u,
            Data  = payload
        });

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal((uint)payload.Length, reader.ReadUInt());
        Assert.Equal(payload, reader.ReadBytes((uint)payload.Length));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void Server0x0015_WriteSerializesSharedUInt5UInt32Shape()
    {
        using var stream = new MemoryStream(WritePacket(new Server0x0015
        {
            Value0 = 0x1Au,
            Value1 = 0x11223344u
        }));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x1Au, reader.ReadUInt(5u));
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void Server0x0015_RemainsNeutralUntilOpcodeSpecificConsumerIsProven()
    {
        Assert.Equal((ushort)0x0015, (ushort)GameMessageOpcode.Server0x0015);
        Assert.NotEqual((ushort)GameMessageOpcode.ServerMatchingAverageWaitTimeUpdate, (ushort)GameMessageOpcode.Server0x0015);

        Assert.NotNull(typeof(Server0x0015).GetProperty(nameof(Server0x0015.Value0)));
        Assert.NotNull(typeof(Server0x0015).GetProperty(nameof(Server0x0015.Value1)));
        Assert.Null(typeof(Server0x0015).GetProperty(nameof(ServerMatchingAverageWaitTimeUpdate.Type)));
        Assert.Null(typeof(Server0x0015).GetProperty(nameof(ServerMatchingAverageWaitTimeUpdate.AverageWaitTime)));
    }

    [Fact]
    public void ServerRaidQueueStatus_UsesRaidInfoRowNamesWhileStandaloneProducerRemainsBlocked()
    {
        Assert.Equal((ushort)0x0718, (ushort)GameMessageOpcode.ServerRaidQueueStatus);
        Assert.NotEqual((ushort)GameMessageOpcode.ServerRaidInfoResponse, (ushort)GameMessageOpcode.ServerRaidQueueStatus);

        Assert.NotNull(typeof(ServerRaidQueueStatus).GetProperty(nameof(ServerRaidQueueStatus.SavedInstanceId)));
        Assert.NotNull(typeof(ServerRaidQueueStatus).GetProperty(nameof(ServerRaidQueueStatus.WorldId)));
        Assert.NotNull(typeof(ServerRaidQueueStatus).GetProperty(nameof(ServerRaidQueueStatus.DateExpireUTC)));
        Assert.NotNull(typeof(ServerRaidQueueStatus).GetProperty(nameof(ServerRaidQueueStatus.DaysUntilExpire)));
        Assert.NotNull(typeof(ServerRaidQueueStatus).GetProperty(nameof(ServerRaidQueueStatus.PrimeLevel)));

        Assert.Null(typeof(ServerRaidQueueStatus).GetProperty("Unknown0"));
        Assert.Null(typeof(ServerRaidQueueStatus).GetProperty("Unknown1"));
        Assert.Null(typeof(ServerRaidQueueStatus).GetProperty("Unknown2"));
        Assert.Null(typeof(ServerRaidQueueStatus).GetProperty("Unknown3"));
        Assert.Null(typeof(ServerRaidQueueStatus).GetProperty("Unknown4"));
        Assert.Null(typeof(ServerRaidQueueStatus).GetProperty("QueuePosition"));
        Assert.Null(typeof(ServerRaidQueueStatus).GetProperty("QueueStatus"));
        Assert.Null(typeof(ServerRaidQueueStatus).GetProperty("MatchType"));
    }

    [Fact]
    public void ServerSpellCastResult_LeadingFieldUsesContextTokenAfterEchoSemanticsProof()
    {
        Assert.Equal((ushort)0x07FC, (ushort)GameMessageOpcode.ServerSpellCastResult);

        Assert.NotNull(typeof(ServerSpellCastResult).GetProperty(nameof(ServerSpellCastResult.ContextToken)));
        Assert.NotNull(typeof(ServerSpellCastResult).GetProperty(nameof(ServerSpellCastResult.Spell4Id)));
        Assert.NotNull(typeof(ServerSpellCastResult).GetProperty(nameof(ServerSpellCastResult.CastResult)));
        Assert.Null(typeof(ServerSpellCastResult).GetProperty("Unknown0"));
        Assert.Null(typeof(ServerSpellCastResult).GetProperty("ClientContextToken"));
        Assert.Null(typeof(ServerSpellCastResult).GetProperty("CastingId"));
    }

    [Fact]
    public void ServerEntityStatAuxFieldsRemainNeutralUntilApplyHandlersAreProven()
    {
        Assert.Equal((ushort)0x0889, (ushort)GameMessageOpcode.ServerEntityStatUInt32Triplet);
        AssertNeutralProperties<ServerEntityStatUInt32Triplet>(
            nameof(ServerEntityStatUInt32Triplet.Value0),
            nameof(ServerEntityStatUInt32Triplet.Value1),
            nameof(ServerEntityStatUInt32Triplet.Value2));

        Assert.Equal((ushort)0x08CC, (ushort)GameMessageOpcode.ServerEntityStatUInt32WideString);
        AssertNeutralProperties<ServerEntityStatUInt32WideString>(
            nameof(ServerEntityStatUInt32WideString.Value),
            nameof(ServerEntityStatUInt32WideString.Text));

        Assert.Equal((ushort)0x08F4, (ushort)GameMessageOpcode.ServerEntityStatUInt32UInt5UInt32);
        AssertNeutralProperties<ServerEntityStatUInt32UInt5UInt32>(
            nameof(ServerEntityStatUInt32UInt5UInt32.Value0),
            nameof(ServerEntityStatUInt32UInt5UInt32.Value1),
            nameof(ServerEntityStatUInt32UInt5UInt32.Value2));

        Assert.Equal((ushort)0x0939, (ushort)GameMessageOpcode.ServerEntityStatUInt32UInt14UInt18WideString);
        AssertNeutralProperties<ServerEntityStatUInt32UInt14UInt18WideString>(
            nameof(ServerEntityStatUInt32UInt14UInt18WideString.Value0),
            nameof(ServerEntityStatUInt32UInt14UInt18WideString.Value1),
            nameof(ServerEntityStatUInt32UInt14UInt18WideString.Value2),
            nameof(ServerEntityStatUInt32UInt14UInt18WideString.Text));

        Assert.Equal((ushort)0x093D, (ushort)GameMessageOpcode.ServerEntityStatUInt32UInt5Pair);
        AssertNeutralProperties<ServerEntityStatUInt32UInt5Pair>(
            nameof(ServerEntityStatUInt32UInt5Pair.Value0),
            nameof(ServerEntityStatUInt32UInt5Pair.Value1),
            nameof(ServerEntityStatUInt32UInt5Pair.Value2),
            nameof(ServerEntityStatUInt32UInt5Pair.Value3));

        Assert.Equal((ushort)0x093E, (ushort)GameMessageOpcode.ServerEntityStatTwoUInt32UInt64);
        AssertNeutralProperties<ServerEntityStatTwoUInt32UInt64>(
            nameof(ServerEntityStatTwoUInt32UInt64.Value0),
            nameof(ServerEntityStatTwoUInt32UInt64.Value1),
            nameof(ServerEntityStatTwoUInt32UInt64.Value2));
    }

    [Fact]
    public void ServerMapTrackedUnitUpdate_UsesTrackingSlotIdUntilProducerSelectionIsProven()
    {
        Assert.Equal((ushort)0x0849, (ushort)GameMessageOpcode.ServerMapTrackedUnitUpdate);
        Assert.Equal((ushort)0x0848, (ushort)GameMessageOpcode.ServerMapTrackedUnitDisable);
        Assert.NotNull(typeof(ServerMapTrackedUnitUpdate).GetProperty(nameof(ServerMapTrackedUnitUpdate.TrackedUnitId)));
        Assert.NotNull(typeof(ServerMapTrackedUnitUpdate).GetProperty(nameof(ServerMapTrackedUnitUpdate.TrackingSlotId)));
        Assert.Null(typeof(ServerMapTrackedUnitUpdate).GetProperty("PublicEventObjectiveId"));
        Assert.NotNull(typeof(ServerMapTrackedUnitDisable).GetProperty(nameof(ServerMapTrackedUnitDisable.TrackedUnitId)));
    }

    [Fact]
    public void ServerHousingNeighborhoodFieldsRemainWireNamedUntilProducerBackingIsProven()
    {
        Assert.Equal((ushort)0x0501, (ushort)GameMessageOpcode.ServerHousingNeighborhoodEntry);
        Assert.Equal((ushort)0x0506, (ushort)GameMessageOpcode.ServerHousingNeighborhoodList);
        Assert.NotNull(typeof(ServerHousingNeighborhoodEntry).GetProperty(nameof(ServerHousingNeighborhoodEntry.NeighborhoodId)));
        Assert.NotNull(typeof(ServerHousingNeighborhoodEntry).GetProperty(nameof(ServerHousingNeighborhoodEntry.RealmId0)));
        Assert.NotNull(typeof(ServerHousingNeighborhoodEntry).GetProperty(nameof(ServerHousingNeighborhoodEntry.RealmId1)));
        Assert.NotNull(typeof(ServerHousingNeighborhoodEntry).GetProperty(nameof(ServerHousingNeighborhoodEntry.NeighborhoodWireUInt64_AfterRealmIds)));
        Assert.NotNull(typeof(ServerHousingNeighborhoodEntry).GetProperty(nameof(ServerHousingNeighborhoodEntry.NeighborhoodWireUInt32_0)));
        Assert.NotNull(typeof(ServerHousingNeighborhoodEntry).GetProperty(nameof(ServerHousingNeighborhoodEntry.NeighborhoodWireUInt32_1)));
        Assert.NotNull(typeof(ServerHousingNeighborhoodEntry).GetProperty(nameof(ServerHousingNeighborhoodEntry.NeighborhoodWireUInt32_2)));
        Assert.Null(typeof(ServerHousingNeighborhoodEntry).GetProperty("BaseCost"));
        Assert.Null(typeof(ServerHousingNeighborhoodEntry).GetProperty("MaxPopulation"));
        Assert.Null(typeof(ServerHousingNeighborhoodEntry).GetProperty("HousingMapInfoIdPrimary"));
    }

    [Fact]
    public void ClusterAuxPackets_WriteRepresentativeReaderBackedShapes()
    {
        Assert.Empty(WritePacket(new ServerItemContextActionAck()));
        Assert.Empty(WritePacket(new ServerDatacubeAuxEmpty()));
        Assert.Empty(WritePacket(new ServerDuelAuxEmpty()));
        Assert.Empty(WritePacket(new ServerResurrectionAuxEmpty()));
        Assert.Empty(WritePacket(new ServerAppearanceAuxEmpty()));
        Assert.Empty(WritePacket(new ServerLootAuxEmpty()));
        Assert.Empty(WritePacket(new ServerPathMissionAuxEmpty()));
        Assert.Empty(WritePacket(new ServerEntitySelectAuxEmpty()));

        using (var pathScientistStream = new MemoryStream(WritePacket(new ServerPathScientistAuxUInt32(0x01020304u))))
        using (var pathScientistReader = new GamePacketReader(pathScientistStream))
        {
            Assert.Equal(0x01020304u, pathScientistReader.ReadUInt());
            Assert.Equal(pathScientistStream.Length, pathScientistStream.Position);
        }

        using (var entitySelectStream = new MemoryStream(WritePacket(new ServerEntitySelectAuxUInt14(0x1234u))))
        using (var entitySelectReader = new GamePacketReader(entitySelectStream))
        {
            Assert.Equal(0x1234u, entitySelectReader.ReadUInt(14u));
            Assert.Equal(entitySelectStream.Length, entitySelectStream.Position);
        }

        using (var instanceResetStream = new MemoryStream(WritePacket(new ServerInstanceResetAux
        {
            UInt4Value = 0xAu,
            Value      = 0x01020304u
        })))
        using (var instanceResetReader = new GamePacketReader(instanceResetStream))
        {
            Assert.Equal(0xAu, instanceResetReader.ReadUInt(4u));
            Assert.Equal(0x01020304u, instanceResetReader.ReadUInt());
            Assert.Equal(instanceResetStream.Length, instanceResetStream.Position);
        }

        using (var reputationStream = new MemoryStream(WritePacket(new ServerReputationAuxUInt32(0x05060708u))))
        using (var reputationReader = new GamePacketReader(reputationStream))
        {
            Assert.Equal(0x05060708u, reputationReader.ReadUInt());
            Assert.Equal(reputationStream.Length, reputationStream.Position);
        }

        using (var reputationUInt64UInt32Stream = new MemoryStream(WritePacket(new ServerReputationAuxUInt64UInt32
        {
            Value0 = 0x0102030405060708ul,
            Value1 = 0x090A0B0Cu
        })))
        using (var reputationUInt64UInt32Reader = new GamePacketReader(reputationUInt64UInt32Stream))
        {
            Assert.Equal(0x0102030405060708ul, reputationUInt64UInt32Reader.ReadULong());
            Assert.Equal(0x090A0B0Cu, reputationUInt64UInt32Reader.ReadUInt());
            Assert.Equal(reputationUInt64UInt32Stream.Length, reputationUInt64UInt32Stream.Position);
        }

        using (var reputationUInt64UInt32AltStream = new MemoryStream(WritePacket(new ServerReputationAuxUInt64UInt32Alt
        {
            Value0 = 0x1112131415161718ul,
            Value1 = 0x191A1B1Cu
        })))
        using (var reputationUInt64UInt32AltReader = new GamePacketReader(reputationUInt64UInt32AltStream))
        {
            Assert.Equal(0x1112131415161718ul, reputationUInt64UInt32AltReader.ReadULong());
            Assert.Equal(0x191A1B1Cu, reputationUInt64UInt32AltReader.ReadUInt());
            Assert.Equal(reputationUInt64UInt32AltStream.Length, reputationUInt64UInt32AltStream.Position);
        }

        using (var reputationUInt14UInt32Stream = new MemoryStream(WritePacket(new ServerReputationAuxUInt14UInt32
        {
            Value0 = 0x1234u,
            Value1 = 0x1D1E1F20u
        })))
        using (var reputationUInt14UInt32Reader = new GamePacketReader(reputationUInt14UInt32Stream))
        {
            Assert.Equal(0x1234u, reputationUInt14UInt32Reader.ReadUInt(14u));
            Assert.Equal(0x1D1E1F20u, reputationUInt14UInt32Reader.ReadUInt());
            Assert.Equal(reputationUInt14UInt32Stream.Length, reputationUInt14UInt32Stream.Position);
        }

        using (var timeOfDayStream = new MemoryStream(WritePacket(new ServerTimeOfDayAuxUInt32(0x090A0B0Cu))))
        using (var timeOfDayReader = new GamePacketReader(timeOfDayStream))
        {
            Assert.Equal(0x090A0B0Cu, timeOfDayReader.ReadUInt());
            Assert.Equal(timeOfDayStream.Length, timeOfDayStream.Position);
        }

        using (var supplySatchelStream = new MemoryStream(WritePacket(new ServerSupplySatchelAux
        {
            UInt6Value = 0x2Au,
            Value      = 0x01020304u
        })))
        using (var supplySatchelReader = new GamePacketReader(supplySatchelStream))
        {
            Assert.Equal(0x2Au, supplySatchelReader.ReadUInt(6u));
            Assert.Equal(0x01020304u, supplySatchelReader.ReadUInt());
            Assert.Equal(supplySatchelStream.Length, supplySatchelStream.Position);
        }

        using (var costumeStream = new MemoryStream(WritePacket(new ServerCostumeItemAux
        {
            UInt14Value = 0x1234u,
            Value0      = 0x01020304u,
            Value1      = 0x05060708u,
            Value2      = 0x090A0B0Cu,
            Flag0       = true,
            Flag1       = false
        })))
        using (var costumeReader = new GamePacketReader(costumeStream))
        {
            Assert.Equal(0x1234u, costumeReader.ReadUInt(14u));
            Assert.Equal(0x01020304u, costumeReader.ReadUInt());
            Assert.Equal(0x05060708u, costumeReader.ReadUInt());
            Assert.Equal(0x090A0B0Cu, costumeReader.ReadUInt());
            Assert.True(costumeReader.ReadBit());
            Assert.False(costumeReader.ReadBit());
            Assert.Equal(costumeStream.Length, costumeStream.Position);
        }

        using (var itemSwapStream = new MemoryStream(WritePacket(new ServerItemSwapAux
        {
            DragDrop = new ItemDragDrop
            {
                Guid     = 0x0102030405060708ul,
                DragDrop = 0x090A0B0C0D0E0F10ul
            }
        })))
        using (var itemSwapReader = new GamePacketReader(itemSwapStream))
        {
            Assert.Equal(0x0102030405060708ul, itemSwapReader.ReadULong());
            Assert.Equal(0x090A0B0C0D0E0F10ul, itemSwapReader.ReadULong());
            Assert.Equal(itemSwapStream.Length, itemSwapStream.Position);
        }

        using (var itemModdableDataStream = new MemoryStream(WritePacket(new ServerItemModdableData
        {
            ItemGuid          = 0x0102030405060708ul,
            ThresholdData     = 0x1112131415161718ul,
            RandomGlyphData   = 0x21222324u,
            RandomCircuitData = 0x3132333435363738ul
        })))
        using (var itemModdableDataReader = new GamePacketReader(itemModdableDataStream))
        {
            Assert.Equal(0x0102030405060708ul, itemModdableDataReader.ReadULong());
            Assert.Equal(0x1112131415161718ul, itemModdableDataReader.ReadULong());
            Assert.Equal(0x21222324u, itemModdableDataReader.ReadUInt());
            Assert.Equal(0x3132333435363738ul, itemModdableDataReader.ReadULong());
            Assert.Equal(itemModdableDataStream.Length, itemModdableDataStream.Position);
        }

        using (var itemMicrochipsStream = new MemoryStream(WritePacket(new ServerItemMicrochips
        {
            ItemGuid          = 0x4142434445464748ul,
            MakerCharacterId  = 0x5152535455565758ul,
            RandomCircuitData = 0x6162636465666768ul,
            PowerCoreItem2Id  = 0x23456u,
            MicrochipItem2Ids = { 0x01020304u, 0x05060708u }
        })))
        using (var itemMicrochipsReader = new GamePacketReader(itemMicrochipsStream))
        {
            Assert.Equal(0x4142434445464748ul, itemMicrochipsReader.ReadULong());
            Assert.Equal(0x5152535455565758ul, itemMicrochipsReader.ReadULong());
            Assert.Equal(0x6162636465666768ul, itemMicrochipsReader.ReadULong());
            Assert.Equal(0x23456u, itemMicrochipsReader.ReadUInt(18u));
            Assert.Equal((byte)2, itemMicrochipsReader.ReadByte(3u));
            Assert.Equal(new uint[] { 0x01020304u, 0x05060708u }, itemMicrochipsReader.ReadRetailCompositeUInt32Array(2));
            Assert.Equal(itemMicrochipsStream.Length, itemMicrochipsStream.Position);
        }

        using (var itemGlyphsStream = new MemoryStream(WritePacket(new ServerItemGlyphs
        {
            ItemGuid        = 0x7172737475767778ul,
            RandomGlyphData = 0x81828384u,
            GlyphItem2Ids   = { 0x11121314u, 0x21222324u, 0x31323334u }
        })))
        using (var itemGlyphsReader = new GamePacketReader(itemGlyphsStream))
        {
            Assert.Equal(0x7172737475767778ul, itemGlyphsReader.ReadULong());
            Assert.Equal(0x81828384u, itemGlyphsReader.ReadUInt());
            Assert.Equal((byte)3, itemGlyphsReader.ReadByte(4u));
            Assert.Equal(new uint[] { 0x11121314u, 0x21222324u, 0x31323334u }, itemGlyphsReader.ReadRetailCompositeUInt32Array(3));
            Assert.Equal(itemGlyphsStream.Length, itemGlyphsStream.Position);
        }

        using (var vehicleEmbarkStream = new MemoryStream(WritePacket(new ServerVehicleEmbarkAux
        {
            Flag       = true,
            UInt2Value = 0x2u,
            Value0     = 0x0102030405060708ul,
            Value1     = 0x090A0B0Cu,
            Value2     = 0x0D0E0F10u
        })))
        using (var vehicleEmbarkReader = new GamePacketReader(vehicleEmbarkStream))
        {
            Assert.True(vehicleEmbarkReader.ReadBit());
            Assert.Equal(0x2u, vehicleEmbarkReader.ReadUInt(2u));
            Assert.Equal(0x0102030405060708ul, vehicleEmbarkReader.ReadULong());
            Assert.Equal(0x090A0B0Cu, vehicleEmbarkReader.ReadUInt());
            Assert.Equal(0x0D0E0F10u, vehicleEmbarkReader.ReadUInt());
            Assert.Equal(vehicleEmbarkStream.Length, vehicleEmbarkStream.Position);
        }

        using (var chatBulkStream = new MemoryStream(WritePacket(new ServerChatAuxBulk
        {
            Row = new ServerChatAuxRow
            {
                Value0  = 0x0102,
                Value1  = 0x0304,
                Payload = new ServerChatAuxRow.BoolPayload(2) { Value = true }
            }
        })))
        using (var chatBulkReader = new GamePacketReader(chatBulkStream))
        {
            AssertChatAuxRowHeader(chatBulkReader, 2u, 0x0102, 0x0304);
            Assert.True(chatBulkReader.ReadBit());
            Assert.Equal(chatBulkStream.Length, chatBulkStream.Position);
        }

        var chatPayload = new ServerChatAuxPayload
        {
            Text  = "chat-payload",
            Flag  = true,
            Value = 0x1234
        };
        chatPayload.Rows.Add(new ServerChatAuxRow
        {
            Value0  = 0x0506,
            Value1  = 0x0708,
            Payload = new ServerChatAuxRow.UInt32Payload(7) { Value = 0x11121314u }
        });

        using (var chatPayloadStream = new MemoryStream(WritePacket(chatPayload)))
        using (var chatPayloadReader = new GamePacketReader(chatPayloadStream))
        {
            Assert.Equal("chat-payload", chatPayloadReader.ReadWideString());
            Assert.Equal(1u, chatPayloadReader.ReadUInt(5u));
            AssertChatAuxRowHeader(chatPayloadReader, 7u, 0x0506, 0x0708);
            Assert.Equal(0x11121314u, chatPayloadReader.ReadUInt());
            Assert.True(chatPayloadReader.ReadBit());
            Assert.Equal(0x1234u, chatPayloadReader.ReadUInt(16u));
            Assert.Equal(chatPayloadStream.Length, chatPayloadStream.Position);
        }

        var chatPayloadAlt = new ServerChatAuxPayloadAlt
        {
            Text  = "chat-payload-alt",
            Value = 0x5678
        };
        chatPayloadAlt.Rows.Add(new ServerChatAuxRow
        {
            Value0  = 0x090A,
            Value1  = 0x0B0C,
            Payload = new ServerChatAuxRow.UInt14TwoUInt32Payload
            {
                UInt14Value = 0x1234u,
                Value1      = 0x21222324u,
                Value2      = 0x25262728u
            }
        });

        using (var chatPayloadAltStream = new MemoryStream(WritePacket(chatPayloadAlt)))
        using (var chatPayloadAltReader = new GamePacketReader(chatPayloadAltStream))
        {
            Assert.Equal("chat-payload-alt", chatPayloadAltReader.ReadWideString());
            Assert.Equal(1u, chatPayloadAltReader.ReadUInt(5u));
            AssertChatAuxRowHeader(chatPayloadAltReader, 10u, 0x090A, 0x0B0C);
            Assert.Equal(0x1234u, chatPayloadAltReader.ReadUInt(14u));
            Assert.Equal(0x21222324u, chatPayloadAltReader.ReadUInt());
            Assert.Equal(0x25262728u, chatPayloadAltReader.ReadUInt());
            Assert.Equal(0x5678u, chatPayloadAltReader.ReadUInt(16u));
            Assert.Equal(chatPayloadAltStream.Length, chatPayloadAltStream.Position);
        }

        var chatNotification = new ServerChatAuxNotification();
        var chatNotificationRow = new ServerChatAuxNotification.Row
        {
            Value0      = 0x01020304u,
            UInt14Value = 0x1234u,
            Value2      = 0x1112131415161718ul,
            Value3      = 0x21222324u,
            Value4      = 0x25262728u,
            Value5      = 0x2A
        };
        chatNotificationRow.SubRows.Add(new ServerChatAuxNotification.SubRow
        {
            UInt5Value = 0x1Bu,
            Value1     = 0x31323334u,
            Value2     = 0x35363738u
        });
        chatNotification.Rows.Add(chatNotificationRow);

        using (var chatNotificationStream = new MemoryStream(WritePacket(chatNotification)))
        using (var chatNotificationReader = new GamePacketReader(chatNotificationStream))
        {
            Assert.Equal((byte)1, chatNotificationReader.ReadByte());
            Assert.Equal(0x01020304u, chatNotificationReader.ReadUInt());
            Assert.Equal(0x1234u, chatNotificationReader.ReadUInt(14u));
            Assert.Equal(0x1112131415161718ul, chatNotificationReader.ReadULong());
            Assert.Equal(0x21222324u, chatNotificationReader.ReadUInt());
            Assert.Equal(0x25262728u, chatNotificationReader.ReadUInt());
            Assert.Equal((byte)0x2A, chatNotificationReader.ReadByte());
            Assert.Equal((byte)1, chatNotificationReader.ReadByte());
            Assert.Equal(0x1Bu, chatNotificationReader.ReadUInt(5u));
            Assert.Equal(0x31323334u, chatNotificationReader.ReadUInt());
            Assert.Equal(0x35363738u, chatNotificationReader.ReadUInt());
            Assert.Equal(chatNotificationStream.Length, chatNotificationStream.Position);
        }

        using (var storyCommunicatorStream = new MemoryStream(WritePacket(new ServerStoryCommunicatorAux
        {
            Value0 = 0x01020304u,
            Value1 = 0x05060708u,
            Value2 = 0x090A0B0Cu,
            Value3 = 0x0D0E0F10u,
            Value4 = 0x11121314u,
            Value5 = 0x1516
        })))
        using (var storyCommunicatorReader = new GamePacketReader(storyCommunicatorStream))
        {
            Assert.Equal(0x01020304u, storyCommunicatorReader.ReadUInt());
            Assert.Equal(0x05060708u, storyCommunicatorReader.ReadUInt());
            Assert.Equal(0x090A0B0Cu, storyCommunicatorReader.ReadUInt());
            Assert.Equal(0x0D0E0F10u, storyCommunicatorReader.ReadUInt());
            Assert.Equal(0x11121314u, storyCommunicatorReader.ReadUInt());
            Assert.Equal(0x1516u, storyCommunicatorReader.ReadUInt(16u));
            Assert.Equal(storyCommunicatorStream.Length, storyCommunicatorStream.Position);
        }

        using var stream = new MemoryStream(WritePacket(new ServerRecruitmentAuxUInt32List
        {
            Values = { 0x01020304u, 0x05060708u, 0x090A0B0Cu, 0x0D0E0F10u }
        }));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(new uint[] { 0x01020304u, 0x05060708u, 0x090A0B0Cu, 0x0D0E0F10u }, reader.ReadRetailCompositeUInt32Array(4));
        Assert.Equal(stream.Length, stream.Position);

        using var auctionPostStream = new MemoryStream(WritePacket(new ServerAuctionPostAux
        {
            Values = { 0x01020304u, 0x05060708u },
            Data   = [0xAA, 0xBB],
            Value  = 0x0C0D0E0Fu
        }));
        using var auctionPostReader = new GamePacketReader(auctionPostStream);

        Assert.Equal(2u, auctionPostReader.ReadUInt());
        Assert.Equal(new uint[] { 0x01020304u, 0x05060708u }, auctionPostReader.ReadRetailCompositeUInt32Array(2));
        Assert.Equal(new byte[] { 0xAA, 0xBB }, auctionPostReader.ReadRetailCompositeByteSpan(2));
        Assert.Equal(0x0C0D0E0Fu, auctionPostReader.ReadUInt());
        Assert.Equal(auctionPostStream.Length, auctionPostStream.Position);

        using var auctionsByFilterStream = new MemoryStream(WritePacket(new ServerAuctionsByFilterAux
        {
            UInt14Value = 0x1234u,
            Value1      = 0x11121314u,
            Value2      = 0x21222324u,
            Value3      = 0x31323334u,
            Flag        = true
        }));
        using var auctionsByFilterReader = new GamePacketReader(auctionsByFilterStream);

        Assert.Equal(0x1234u, auctionsByFilterReader.ReadUInt(14u));
        Assert.Equal(0x11121314u, auctionsByFilterReader.ReadUInt());
        Assert.Equal(0x21222324u, auctionsByFilterReader.ReadUInt());
        Assert.Equal(0x31323334u, auctionsByFilterReader.ReadUInt());
        Assert.True(auctionsByFilterReader.ReadBit());
        Assert.Equal(auctionsByFilterStream.Length, auctionsByFilterStream.Position);

        using var publicEventStream = new MemoryStream(WritePacket(new ServerPublicEventAux
        {
            Value = 0x11223344u,
            Values = [0x01020304u, 0x05060708u]
        }));
        using var publicEventReader = new GamePacketReader(publicEventStream);

        Assert.Equal(0x11223344u, publicEventReader.ReadUInt());
        Assert.Equal(2u, publicEventReader.ReadUInt(5u));
        Assert.Equal(0x01020304u, publicEventReader.ReadUInt());
        Assert.Equal(0x05060708u, publicEventReader.ReadUInt());
        Assert.Equal(publicEventStream.Length, publicEventStream.Position);

        using var voteStream = new MemoryStream(WritePacket(new ServerPublicEventVoteAux
        {
            Value = 0x1234,
            Flag = true
        }));
        using var voteReader = new GamePacketReader(voteStream);

        Assert.Equal(0x1234u, voteReader.ReadUInt(15u));
        Assert.True(voteReader.ReadBit());
        Assert.Equal(voteStream.Length, voteStream.Position);
    }

    [Fact]
    public void ServerChatAuxRows_WriteSerializesMappedVariantTable()
    {
        var packet = new ServerChatAuxPayloadAlt
        {
            Text  = "variant-table",
            Value = 0xBEEF
        };
        packet.Rows.Add(new ServerChatAuxRow
        {
            Value0  = 0x0101,
            Value1  = 0x0202,
            Payload = new ServerChatAuxRow.BoolPayload(0) { Value = true }
        });
        packet.Rows.Add(new ServerChatAuxRow
        {
            Value0  = 0x0303,
            Value1  = 0x0404,
            Payload = new ServerChatAuxRow.BoolPayload(3) { Value = false }
        });
        packet.Rows.Add(new ServerChatAuxRow
        {
            Value0  = 0x0505,
            Value1  = 0x0606,
            Payload = new ServerChatAuxRow.UInt32Payload(1) { Value = 0x01020304u }
        });
        packet.Rows.Add(new ServerChatAuxRow
        {
            Value0  = 0x0707,
            Value1  = 0x0808,
            Payload = new ServerChatAuxRow.UInt18Payload { Value = 0x23456u }
        });
        packet.Rows.Add(new ServerChatAuxRow
        {
            Value0  = 0x0909,
            Value1  = 0x0A0A,
            Payload = new ServerChatAuxRow.UInt15Payload { Value = 0x2345u }
        });
        packet.Rows.Add(new ServerChatAuxRow
        {
            Value0  = 0x0B0B,
            Value1  = 0x0C0C,
            Payload = new ServerChatAuxRow.UInt14Payload { Value = 0x1234u }
        });
        packet.Rows.Add(new ServerChatAuxRow
        {
            Value0  = 0x0D0D,
            Value1  = 0x0E0E,
            Payload = new ServerChatAuxRow.UInt64Payload { Value = 0x0102030405060708ul }
        });
        packet.Rows.Add(new ServerChatAuxRow
        {
            Value0  = 0x0F0F,
            Value1  = 0x1010,
            Payload = new ServerChatAuxRow.UInt32Payload(11) { Value = 0x11121314u }
        });

        var complex = new ServerChatAuxRow.ComplexPayload
        {
            Value0       = 0x2122232425262728ul,
            UInt18Value1 = 0x12345u,
            Value2       = 0x3132333435363738ul,
            Value3       = 0x4142434445464748ul,
            Value4       = 0x51525354u,
            Value5       = 0x6162636465666768ul,
            Value6       = 0x71727374u,
            Value7       = 0x81828384u,
            Value8       = 0x8A,
            UInt18Value9 = 0x23456u
        };
        complex.Values10.Add(0x91929394u);
        complex.Values10.Add(0xA1A2A3A4u);
        complex.Values11.Add(0xB1B2B3B4u);

        packet.Rows.Add(new ServerChatAuxRow
        {
            Value0  = 0x1111,
            Value1  = 0x1212,
            Payload = complex
        });

        using var stream = new MemoryStream(WritePacket(packet));
        using var reader = new GamePacketReader(stream);

        Assert.Equal("variant-table", reader.ReadWideString());
        Assert.Equal(9u, reader.ReadUInt(5u));

        AssertChatAuxRowHeader(reader, 0u, 0x0101, 0x0202);
        Assert.True(reader.ReadBit());
        AssertChatAuxRowHeader(reader, 3u, 0x0303, 0x0404);
        Assert.False(reader.ReadBit());
        AssertChatAuxRowHeader(reader, 1u, 0x0505, 0x0606);
        Assert.Equal(0x01020304u, reader.ReadUInt());
        AssertChatAuxRowHeader(reader, 4u, 0x0707, 0x0808);
        Assert.Equal(0x23456u, reader.ReadUInt(18u));
        AssertChatAuxRowHeader(reader, 5u, 0x0909, 0x0A0A);
        Assert.Equal(0x2345u, reader.ReadUInt(15u));
        AssertChatAuxRowHeader(reader, 6u, 0x0B0B, 0x0C0C);
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        AssertChatAuxRowHeader(reader, 9u, 0x0D0D, 0x0E0E);
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        AssertChatAuxRowHeader(reader, 11u, 0x0F0F, 0x1010);
        Assert.Equal(0x11121314u, reader.ReadUInt());

        AssertChatAuxRowHeader(reader, 8u, 0x1111, 0x1212);
        Assert.Equal(0x2122232425262728ul, reader.ReadULong());
        Assert.Equal(0x12345u, reader.ReadUInt(18u));
        Assert.Equal(0x3132333435363738ul, reader.ReadULong());
        Assert.Equal(0x4142434445464748ul, reader.ReadULong());
        Assert.Equal(0x51525354u, reader.ReadUInt());
        Assert.Equal(0x6162636465666768ul, reader.ReadULong());
        Assert.Equal(0x71727374u, reader.ReadUInt());
        Assert.Equal(0x81828384u, reader.ReadUInt());
        Assert.Equal((byte)0x8A, reader.ReadByte());
        Assert.Equal(0x23456u, reader.ReadUInt(18u));
        Assert.Equal(2u, reader.ReadUInt(3u));
        Assert.Equal(new uint[] { 0x91929394u, 0xA1A2A3A4u }, reader.ReadRetailCompositeUInt32Array(2));
        Assert.Equal(1u, reader.ReadUInt(4u));
        Assert.Equal(new uint[] { 0xB1B2B3B4u }, reader.ReadRetailCompositeUInt32Array(1));

        Assert.Equal(0xBEEFu, reader.ReadUInt(16u));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ClientPublicEventVote_ReadExposesMappedFields()
    {
        byte[] packetData;
        using (var stream = new MemoryStream())
        {
            using (var writer = new GamePacketWriter(stream))
            {
                writer.Write(0x1234u, 14u);
                writer.Write(0x2345u, 14u);
                writer.Write((uint)PublicEventTeam.BlueTeam, 14u);
                writer.Write(3u);
                writer.FlushBits();
            }

            packetData = stream.ToArray();
        }

        using var readStream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(readStream);

        var message = new ClientPublicEventVote();
        message.Read(reader);

        Assert.Equal(0x1234u, message.EventId);
        Assert.Equal(0x2345u, message.VoteId);
        Assert.Equal((uint)PublicEventTeam.BlueTeam, message.TeamId);
        Assert.Equal(3u, message.Choice);
        Assert.Equal(readStream.Length, readStream.Position);
    }

    [Fact]
    public void ServerPublicEventVotePackets_WriteMappedFields()
    {
        using (var stream = new MemoryStream(WritePacket(new ServerPublicEventVoteInitiate
        {
            EventId = 0x1234u,
            VoteId = 0x2345u,
            TeamId = (uint)PublicEventTeam.RedTeam
        })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x1234u, reader.ReadUInt(14u));
            Assert.Equal(0x2345u, reader.ReadUInt(14u));
            Assert.Equal((uint)PublicEventTeam.RedTeam, reader.ReadUInt(14u));
            Assert.Equal(stream.Length, stream.Position);
        }

        using (var stream = new MemoryStream(WritePacket(new ServerPublicEventDetailedVoteInitiate
        {
            EventId = 0x1234u,
            VoteId = 0x2345u,
            TeamId = PublicEventTeam.BlueTeam,
            Tallies =
            [
                new ServerPublicEventDetailedVoteInitiate.Tally
                {
                    Choice = 2u,
                    Count = 7u
                }
            ],
            CanPlayerVote = true,
            ElapsedTimeMs = 1500u
        })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x1234u, reader.ReadUInt(14u));
            Assert.Equal(0x2345u, reader.ReadUInt(14u));
            Assert.Equal((uint)PublicEventTeam.BlueTeam, reader.ReadUInt(14u));
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(2u, reader.ReadUInt());
            Assert.Equal(7u, reader.ReadUInt());
            Assert.True(reader.ReadBit());
            Assert.Equal(1500u, reader.ReadUInt());
            Assert.Equal(stream.Length, stream.Position);
        }

        using (var stream = new MemoryStream(WritePacket(new ServerPublicEventVoteTally
        {
            EventId = 0x1234u,
            VoteId = 0x2345u,
            Choice = 2u
        })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x1234u, reader.ReadUInt(14u));
            Assert.Equal(0x2345u, reader.ReadUInt(14u));
            Assert.Equal(2u, reader.ReadUInt());
            Assert.Equal(stream.Length, stream.Position);
        }

        using (var stream = new MemoryStream(WritePacket(new ServerPublicEventVoteEnd
        {
            EventId = 0x1234u,
            VoteId = 0x2345u,
            Winner = 3u
        })))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x1234u, reader.ReadUInt(14u));
            Assert.Equal(0x2345u, reader.ReadUInt(14u));
            Assert.Equal(3u, reader.ReadUInt());
            Assert.Equal(stream.Length, stream.Position);
        }
    }

    [Fact]
    public void ClientPublicEventRequestScoreboard_ReadExposesMappedFields()
    {
        byte[] packetData;
        using (var stream = new MemoryStream())
        {
            using (var writer = new GamePacketWriter(stream))
            {
                writer.Write(0x1234u, 14u);
                writer.Write(true);
                writer.FlushBits();
            }

            packetData = stream.ToArray();
        }

        using var readStream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(readStream);

        var message = new ClientPublicEventRequestScoreboard();
        message.Read(reader);

        Assert.Equal(0x1234u, message.PublicEventId);
        Assert.True(message.Subscribe);
        Assert.Equal(readStream.Length, readStream.Position);
    }

    [Fact]
    public void ServerPublicEventStatsPackets_WriteMappedReaderBackedShapes()
    {
        var personalStat = new ServerPublicEventPersonalStatUpdate
        {
            PublicEventId = 0x1234u,
            StatType      = PublicEventStat.Kills,
            Value         = 42
        };

        using (var stream = new MemoryStream(WritePacket(personalStat)))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x1234u, reader.ReadUInt(14u));
            Assert.Equal((uint)PublicEventStat.Kills, reader.ReadUInt());
            Assert.Equal(42, reader.ReadInt());
            Assert.Equal(stream.Length, stream.Position);
        }

        var statsUpdate = new ServerPublicEventStatsUpdate
        {
            PublicEventId = 0x1234u,
            TeamStats =
            [
                new PublicEventTeamStats
                {
                    TeamId = PublicEventTeam.RedTeam,
                    Stats = BuildPublicEventStats(0x00000005u, 10u, 20u)
                }
            ],
            ParticipantStats =
            [
                new PublicEventParticipantStats
                {
                    TeamId = PublicEventTeam.BlueTeam,
                    UnitId = 0x01020304u,
                    Player = new Identity
                    {
                        RealmId = 7,
                        Id      = 0x1112131415161718ul
                    },
                    Class = CharacterClass.Esper,
                    Path  = PlayerPath.Scientist,
                    Stats = BuildPublicEventStats(0x00000008u, 30u)
                }
            ]
        };

        using var updateStream = new MemoryStream(WritePacket(statsUpdate));
        using var updateReader = new GamePacketReader(updateStream);

        Assert.Equal(0x1234u, updateReader.ReadUInt(14u));
        Assert.Equal(1u, updateReader.ReadUInt());
        Assert.Equal((uint)PublicEventTeam.RedTeam, updateReader.ReadUInt(14u));
        AssertPublicEventStats(updateReader, 0x00000005u, 10u, 20u);
        Assert.Equal(1u, updateReader.ReadUInt());
        Assert.Equal((uint)PublicEventTeam.BlueTeam, updateReader.ReadUInt(14u));
        Assert.Equal(0x01020304u, updateReader.ReadUInt());
        Assert.Equal(7u, updateReader.ReadUInt(14u));
        Assert.Equal(0x1112131415161718ul, updateReader.ReadULong());
        Assert.Equal((uint)CharacterClass.Esper, updateReader.ReadUInt());
        Assert.Equal((uint)PlayerPath.Scientist, updateReader.ReadUInt());
        AssertPublicEventStats(updateReader, 0x00000008u, 30u);
        Assert.Equal(updateStream.Length, updateStream.Position);
    }

    [Fact]
    public void ServerPublicEventEnd_WriteSerializesMappedScoreboardAndRewardFields()
    {
        var message = new ServerPublicEventEnd
        {
            PublicEventId = 0x1234u,
            Reason = PublicEventRemoveReason.Success,
            ElapsedTimeMs = 15000u,
            PersonalStats = BuildPublicEventStats(0x00000002u, 99u),
            TeamStats =
            [
                new PublicEventTeamStats
                {
                    TeamId = PublicEventTeam.RedTeam,
                    Stats = BuildPublicEventStats(0x00000001u, 11u)
                }
            ],
            ParticipantStats =
            [
                new PublicEventParticipantStats
                {
                    TeamId = PublicEventTeam.RedTeam,
                    UnitId = 0x01020304u,
                    Player = new Identity
                    {
                        RealmId = 8,
                        Id      = 0x2122232425262728ul
                    },
                    Class = CharacterClass.Warrior,
                    Path  = PlayerPath.Soldier,
                    Stats = BuildPublicEventStats(0x00000004u, 22u)
                }
            ],
            ObjectiveStatus =
            [
                new ServerPublicEventEnd.PublicEventObjectiveStatus
                {
                    ObjectiveId = 0x2345u,
                    Status = PublicEventStatus.Succeeded
                }
            ],
            RewardTier = PublicEventRewardTier.Gold,
            RewardType = PublicEventRewardType.Completion,
            RewardThreshold = [100u, 200u, 300u]
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal((uint)PublicEventRemoveReason.Success, reader.ReadUInt());
        Assert.Equal(15000u, reader.ReadUInt());
        AssertPublicEventStats(reader, 0x00000002u, 99u);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((uint)PublicEventTeam.RedTeam, reader.ReadUInt(14u));
        AssertPublicEventStats(reader, 0x00000001u, 11u);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((uint)PublicEventTeam.RedTeam, reader.ReadUInt(14u));
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(8u, reader.ReadUInt(14u));
        Assert.Equal(0x2122232425262728ul, reader.ReadULong());
        Assert.Equal((uint)CharacterClass.Warrior, reader.ReadUInt());
        Assert.Equal((uint)PlayerPath.Soldier, reader.ReadUInt());
        AssertPublicEventStats(reader, 0x00000004u, 22u);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x2345u, reader.ReadUInt(15u));
        Assert.Equal((uint)PublicEventStatus.Succeeded, reader.ReadUInt());
        Assert.Equal((uint)PublicEventRewardTier.Gold, reader.ReadUInt());
        Assert.Equal((uint)PublicEventRewardType.Completion, reader.ReadUInt());
        Assert.Equal(100u, reader.ReadUInt());
        Assert.Equal(200u, reader.ReadUInt());
        Assert.Equal(300u, reader.ReadUInt());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerPublicEventObjectiveNotificationMode_WriteSerializesMappedFields()
    {
        var message = new ServerPublicEventObjectiveNotificationMode
        {
            ObjectiveId = 0x5234u,
            NotificationMode = PublicEventObjectiveNotificationMode.Critical
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x5234u, reader.ReadUInt(15u));
        Assert.Equal((uint)PublicEventObjectiveNotificationMode.Critical, reader.ReadUInt());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerPublicEventObjectiveStatusUpdate_WriteSerializesMappedStatusRow()
    {
        var message = new ServerPublicEventObjectiveStatusUpdate
        {
            ObjectiveId = 0x2345u,
            ObjectiveStatus = new PublicEventObjectiveStatus
            {
                Status = PublicEventStatus.Active,
                ObjectiveData = 0x01020304u,
                DynamicMax = 50u,
                Count = 12.5f,
                UnkState = 0xA0B0C0D0u,
                DataType = PublicEventObjectiveDataType.CapturePoint,
                CapturingTeam = PublicEventTeam.Dominion
            }
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x2345u, reader.ReadUInt(15u));
        AssertPublicEventObjectiveStatus(reader, PublicEventObjectiveDataType.CapturePoint);
        Assert.Equal((uint)PublicEventTeam.Dominion, reader.ReadUInt());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerPublicEventObjectiveUpdate_WriteSerializesMappedObjectivePayload()
    {
        var message = new ServerPublicEventObjectiveUpdate
        {
            Objective = new PublicEventObjective
            {
                ObjectiveId = 0x3456u,
                ObjectiveStatus = new PublicEventObjectiveStatus
                {
                    Status = PublicEventStatus.Active,
                    ObjectiveData = 0x01020304u,
                    DynamicMax = 50u,
                    Count = 12.5f,
                    UnkState = 0xA0B0C0D0u,
                    DataType = PublicEventObjectiveDataType.VirtualItemDepot,
                    VirtualItems =
                    [
                        new PublicEventObjectiveStatus.VirtualItem
                        {
                            ItemId = 0x1234u,
                            Count = 7u
                        }
                    ]
                },
                Busy = true,
                ElapsedTimeMs = 25000u,
                NotificationMode = (uint)PublicEventObjectiveNotificationMode.Warn,
                Locations = [0x01020304u, 0x05060708u],
                MapRegions =
                [
                    new MapRegion
                    {
                        WorldSocketId = 0x2345u,
                        WorldLocation2Id = 0x12345u
                    }
                ]
            }
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x3456u, reader.ReadUInt(15u));
        AssertPublicEventObjectiveStatus(reader, PublicEventObjectiveDataType.VirtualItemDepot);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(7u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.Equal(25000u, reader.ReadUInt());
        Assert.Equal((uint)PublicEventObjectiveNotificationMode.Warn, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x05060708u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x2345u, reader.ReadUInt(15u));
        Assert.Equal(0x12345u, reader.ReadUInt(17u));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerPublicEventObjectiveStart_WriteSerializesObjectiveId()
    {
        var message = new ServerPublicEventObjectiveStart
        {
            ObjectiveId = 0x4567u
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x4567u, reader.ReadUInt(15u));
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
    public void ServerCommunicatorMessage_WriteSerializesMappedCommunicatorIdAndConditionFlag()
    {
        var message = new ServerCommunicatorMessage
        {
            CommunicatorMessagesId = 0x1234,
            CheckConditions = true
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x1234u, reader.ReadUInt(15u));
        Assert.True(reader.ReadBit());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerStoryTextCommunicator_WriteSerializesStoryAndCommunicatorFields()
    {
        var message = new ServerStoryTextCommunicator
        {
            StoryMessage = new StoryMessage
            {
                MsgId = 0x01020304u,
                RandomTextLineId = 0x05060708u
            },
            Creature2Id = 0x23456u,
            DurationMs = 15000u,
            PortraitPlacement = CommunicatorPortraitPlacement.Right,
            Overlay = CommunicatorOverlay.HeavyStatic,
            Background = CommunicatorBackground.TheEntity
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        AssertStoryMessageHeader(reader, 0x01020304u, 0x05060708u, 0u);
        Assert.Equal(0x23456u, reader.ReadUInt(18u));
        Assert.Equal(15000u, reader.ReadUInt());
        Assert.Equal((uint)CommunicatorPortraitPlacement.Right, reader.ReadUInt(2u));
        Assert.Equal((uint)CommunicatorOverlay.HeavyStatic, reader.ReadUInt(2u));
        Assert.Equal((uint)CommunicatorBackground.TheEntity, reader.ReadUInt(3u));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerStoryPanelCustomShow_WriteSerializesStoryPanelFields()
    {
        var message = new ServerStoryPanelCustomShow
        {
            StoryMessage = new StoryMessage
            {
                MsgId = 0x01020304u,
                RandomTextLineId = 0x05060708u
            },
            SoundContextEventId = 0x11121314u,
            StoryPanelType = StoryPanelType.FullScreen,
            DurationMS = 7500u,
            StoryPanelStyle = StoryPanelStyle.Eldan
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        AssertStoryMessageHeader(reader, 0x01020304u, 0x05060708u, 0u);
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal((uint)StoryPanelType.FullScreen, reader.ReadUInt());
        Assert.Equal(7500u, reader.ReadUInt());
        Assert.Equal((uint)StoryPanelStyle.Eldan, reader.ReadUInt());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void StoryMessage_WriteSerializesMappedActorRowsWithoutAddingRuntimeSequencing()
    {
        var message = new StoryMessage
        {
            MsgId = 0x01020304u,
            RandomTextLineId = 0x05060708u,
            Actors =
            [
                new CreatureActor
                {
                    Creature2Id = 0x23456u,
                    TokenReplacementValue = 11u,
                    TokenName = "creature"
                },
                new CustomTextActor
                {
                    Text = "custom text",
                    TokenReplacementValue = 12u,
                    TokenName = "custom"
                },
                new LocalisedTextActor
                {
                    LocalisedTextId = 0x1ABCDEu,
                    TokenReplacementValue = 13u,
                    TokenName = "localized"
                },
                new PlayerActor
                {
                    UnitId = 0x11121314u,
                    Name = "Player Name",
                    Level = 50u,
                    Gender = CharacterSex.Female,
                    Race = CharacterRace.Mechari,
                    Class = CharacterClass.Engineer,
                    Faction = ReputationFaction.Dominion,
                    Path = PlayerPath.Scientist,
                    TitleId = 0x1234,
                    TokenReplacementValue = 14u,
                    TokenName = "player"
                },
                new CreatureUnitActor
                {
                    UnitId = 0x21222324u,
                    Creature2Id = 0x12345u,
                    TokenReplacementValue = 15u,
                    TokenName = "unit"
                },
                new PlayerSelfActor
                {
                    PlayerUnitId = 0x31323334u,
                    TokenReplacementValue = 16u,
                    TokenName = "self"
                }
            ]
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        AssertStoryMessageHeader(reader, 0x01020304u, 0x05060708u, 6u);

        Assert.Equal((uint)StoryTextSourceType.Creature, reader.ReadUInt(3u));
        Assert.Equal(0x23456u, reader.ReadUInt(18u));
        AssertStoryActorToken(reader, 11u, "creature");

        Assert.Equal((uint)StoryTextSourceType.CustomText, reader.ReadUInt(3u));
        Assert.Equal("custom text", reader.ReadWideString());
        AssertStoryActorToken(reader, 12u, "custom");

        Assert.Equal((uint)StoryTextSourceType.LocalizedText, reader.ReadUInt(3u));
        Assert.Equal(0x1ABCDEu, reader.ReadUInt(21u));
        AssertStoryActorToken(reader, 13u, "localized");

        Assert.Equal((uint)StoryTextSourceType.Player, reader.ReadUInt(3u));
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal("Player Name", reader.ReadWideString());
        Assert.Equal(50u, reader.ReadUInt());
        Assert.Equal((uint)CharacterSex.Female, reader.ReadUInt(2u));
        Assert.Equal((uint)CharacterRace.Mechari, reader.ReadUInt(5u));
        Assert.Equal((uint)CharacterClass.Engineer, reader.ReadUInt(5u));
        Assert.Equal((uint)ReputationFaction.Dominion, reader.ReadUInt(14u));
        Assert.Equal((uint)PlayerPath.Scientist, reader.ReadUInt(3u));
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        AssertStoryActorToken(reader, 14u, "player");

        Assert.Equal((uint)StoryTextSourceType.CreatureUnit, reader.ReadUInt(3u));
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(0x12345u, reader.ReadUInt(18u));
        AssertStoryActorToken(reader, 15u, "unit");

        Assert.Equal((uint)StoryTextSourceType.PlayerSelf, reader.ReadUInt(3u));
        Assert.Equal(0x31323334u, reader.ReadUInt());
        AssertStoryActorToken(reader, 16u, "self");

        Assert.Equal(stream.Length, stream.Position);
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
    public void ClientPetSetStance_ReadConsumesFiveBitStance()
    {
        byte[] packetData;
        using (var stream = new MemoryStream())
        {
            using (var writer = new GamePacketWriter(stream))
            {
                writer.Write(0x01020304u);
                writer.Write(PetStance.Aggressive, 5u);
                writer.Write(0x5u, 3u);
                writer.FlushBits();
            }

            packetData = stream.ToArray();
        }

        using var readStream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(readStream);

        var message = new ClientPetSetStance();
        message.Read(reader);

        Assert.Equal(0x01020304u, message.PetUnitId);
        Assert.Equal(PetStance.Aggressive, message.Stance);
        Assert.Equal(0x5u, reader.ReadUInt(3u));
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

    [Fact]
    public void ServerStoreOffer_WriteSerializesTrailingBytePlaceholderBeforeCounts()
    {
        const long retailCatalogWireScalar = RetailStoreOfferWireConstants.CatalogWireScalarBits;
        const byte retailCatalogWireTrailingByte = 0x42;

        var offer = new ServerStoreOffers.OfferGroup.Offer
        {
            Id = 77u,
            Name = "Visible Offer",
            Description = "Visible Offer Description",
            PricePremium = 12.5f,
            PriceAlternative = 34.5f,
            DisplayFlags = 0,
            RetailCatalogWireScalar = retailCatalogWireScalar,
            RetailCatalogWireByte = retailCatalogWireTrailingByte
        };

        byte[] packet = WritePacket(offer);

        using var stream = new MemoryStream(packet);
        using var reader = new GamePacketReader(stream);

        Assert.Equal(77u, reader.ReadUInt());
        Assert.Equal("Visible Offer", reader.ReadWideString());
        Assert.Equal("Visible Offer Description", reader.ReadWideString());
        byte[] priceBytes = reader.ReadRetailCompositeByteSpan(8);
        Assert.Equal(12.5f, BitConverter.ToSingle(priceBytes, 0));
        Assert.Equal(34.5f, BitConverter.ToSingle(priceBytes, 4));
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(retailCatalogWireScalar, reader.ReadLong());
        Assert.Equal(retailCatalogWireTrailingByte, reader.ReadByte());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
    }

    [Fact]
    public void ServerPathSettlerBuildStatus_WriteSerializesMappedStatusRow()
    {
        var message = new ServerPathSettlerBuildStatus
        {
            PathSettlerHubId = 46,
            Status = new SettlerImprovementGroupStatus
            {
                PathSettlerImprovementGroupId = 11,
                Tier = 2,
                RemainingTimeMs = 1234u,
                BundleCount = 3u
            }
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(46u, reader.ReadUInt(14u));
        Assert.Equal(11u, reader.ReadUInt(14u));
        Assert.Equal(2, reader.ReadInt());
        Assert.Equal(1234u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
    }

    [Fact]
    public void ServerPathSettlerBuildStatusList_WriteSerializesCountedRows()
    {
        var message = new ServerPathSettlerBuildStatusList
        {
            PathSettlerHubId = 46,
            ImprovementGroupStatuses =
            [
                new SettlerImprovementGroupStatus
                {
                    PathSettlerImprovementGroupId = 11,
                    Tier = 0,
                    RemainingTimeMs = 100u,
                    BundleCount = 1u
                },
                new SettlerImprovementGroupStatus
                {
                    PathSettlerImprovementGroupId = 12,
                    Tier = -1,
                    RemainingTimeMs = 0u,
                    BundleCount = 0u
                }
            ]
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(46u, reader.ReadUInt(14u));
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(11u, reader.ReadUInt(14u));
        Assert.Equal(0, reader.ReadInt());
        Assert.Equal(100u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(12u, reader.ReadUInt(14u));
        Assert.Equal(-1, reader.ReadInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
    }

    [Fact]
    public void ServerPathSettlerBuildResult_WriteSerializesMappedFields()
    {
        var message = new ServerPathSettlerBuildResult
        {
            Result = 1u,
            PathSettlerImprovementId = 9002u,
            PathSettlerImprovementGroupId = 11u
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(9002u, reader.ReadUInt(15u));
        Assert.Equal(11u, reader.ReadUInt(14u));
    }

    [Fact]
    public void ServerPathSoldierHoldoutStatus_WriteSerializesMappedSafeShape()
    {
        var message = new ServerPathSoldierHoldoutStatus
        {
            PathSoldierEventId = 12u,
            UnitId = 0x01020304u,
            IsBoss = true,
            Mode = PlayerPathSoldierEventMode.Active,
            DelayTime = 5000,
            WaveIndex = 3,
            MaxDefendHealth = 123.5f,
            MaxAuxiliaryHealth = 45.25f,
            StartTimeOffset = 99
        };
        message.Units.Add(new TowerDefenseUnit
        {
            UnitId = 0x11121314u,
            Type = TowerDefenseUnitType.Defend
        });
        message.Units.Add(new TowerDefenseUnit
        {
            UnitId = 0x21222324u,
            Type = TowerDefenseUnitType.Escaping
        });

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(12u, reader.ReadUInt(14u));
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal((uint)TowerDefenseUnitType.Defend, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal((uint)TowerDefenseUnitType.Escaping, reader.ReadUInt());
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.Equal((uint)PlayerPathSoldierEventMode.Active, reader.ReadUInt());
        Assert.Equal(5000, reader.ReadInt());
        Assert.Equal(3, reader.ReadInt());
        Assert.Equal(123.5f, reader.ReadSingle());
        Assert.Equal(45.25f, reader.ReadSingle());
        Assert.Equal(99, reader.ReadInt());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerPathSoldierHoldOutNextWave_WriteSerializesEventWaveAndBossFlag()
    {
        var message = new ServerPathSoldierHoldOutNextWave
        {
            PathSoldierEventId = 12,
            WaveIndex = 4u,
            IsBoss = true
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(12u, reader.ReadUInt(14u));
        Assert.Equal(4u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerPathSoldierHoldoutEnd_WriteSerializesEventAndResult()
    {
        var message = new ServerPathSoldierHoldoutEnd
        {
            PathSoldierEventId = 12,
            Reason = PlayerPathSoldierResult.Success
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(12u, reader.ReadUInt(14u));
        Assert.Equal((uint)PlayerPathSoldierResult.Success, reader.ReadUInt());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void ServerPathSoldierHoldoutDeath_WriteSerializesEventIdOnly()
    {
        var message = new ServerPathSoldierHoldoutDeath
        {
            PathSoldierEventId = 12
        };

        using var stream = new MemoryStream(WritePacket(message));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(12u, reader.ReadUInt(14u));
        Assert.Equal(stream.Length, stream.Position);
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

    private static PublicEventStats BuildPublicEventStats(uint mask, params uint[] values)
    {
        var stats = new PublicEventStats();
        for (uint bit = 0u; bit < 32u; bit++)
        {
            if ((mask & (1u << (int)bit)) != 0u)
                stats.Mask.SetBit(bit, true);
        }

        stats.Values.AddRange(values);
        return stats;
    }

    private static void AssertPublicEventStats(GamePacketReader reader, uint mask, params uint[] values)
    {
        Assert.Equal(mask, reader.ReadUInt());
        Assert.Equal((uint)values.Length, reader.ReadUInt(5u));

        foreach (uint value in values)
            Assert.Equal(value, reader.ReadUInt());
    }

    private static void AssertPublicEventObjectiveStatus(GamePacketReader reader, PublicEventObjectiveDataType dataType)
    {
        Assert.Equal((uint)PublicEventStatus.Active, reader.ReadUInt());
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(50u, reader.ReadUInt());
        Assert.Equal(12.5f, reader.ReadSingle());
        Assert.Equal(0xA0B0C0D0u, reader.ReadUInt());
        Assert.Equal((uint)dataType, reader.ReadUInt(3u));
    }

    private static void AssertStoryMessageHeader(GamePacketReader reader, uint msgId, uint randomTextLineId, uint actorCount)
    {
        Assert.Equal(msgId, reader.ReadUInt());
        Assert.Equal(randomTextLineId, reader.ReadUInt());
        Assert.Equal(actorCount, reader.ReadUInt(8u));
    }

    private static void AssertStoryActorToken(GamePacketReader reader, uint tokenReplacementValue, string tokenName)
    {
        Assert.Equal(tokenReplacementValue, reader.ReadUInt());
        Assert.Equal(tokenName, reader.ReadString());
    }

    private static void AssertChatAuxRowHeader(GamePacketReader reader, uint variant, ushort value0, ushort value1)
    {
        Assert.Equal(variant, reader.ReadUInt(4u));
        Assert.Equal(value0, reader.ReadUShort());
        Assert.Equal(value1, reader.ReadUShort());
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

    private static void AssertNeutralProperties<TPacket>(params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
            Assert.NotNull(typeof(TPacket).GetProperty(propertyName));
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
