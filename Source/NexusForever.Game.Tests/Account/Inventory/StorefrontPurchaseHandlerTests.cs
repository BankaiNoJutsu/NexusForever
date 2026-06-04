using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Storefront;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Account;
using NexusForever.WorldServer.Network.Message.Handler.Character;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Tests.Account.Inventory;

public class StorefrontPurchaseHandlerTests
{
    [Fact]
    public void CharacterPurchase_WithMismatchedTarget_ReturnsStoreError()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientStorefrontPurchaseCharacterHandler(
            NullLogger<ClientStorefrontPurchaseCharacterHandler>.Instance,
            null);

        ClientStorefrontPurchaseCharacter purchase = ReadCharacterPurchase(
            offerId: 77u,
            currencyId: AccountCurrencyType.NCoin,
            target: new NetworkIdentity
            {
                RealmId = 1,
                Id      = 999ul
            });

        handler.HandleMessage(session, purchase);

        ServerStoreError error = Assert.Single(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Equal(StoreError.GenericFail, error.Error);
    }

    [Fact]
    public void CharacterPurchase_WithUnknownOffer_ReturnsInvalidOfferStoreError()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IGlobalStorefrontManager storefrontManager = RecordingDispatchProxy<IGlobalStorefrontManager>.Create(out _);
        var handler = new ClientStorefrontPurchaseCharacterHandler(
            NullLogger<ClientStorefrontPurchaseCharacterHandler>.Instance,
            storefrontManager);

        ClientStorefrontPurchaseCharacter purchase = ReadCharacterPurchase(
            offerId: 1234u,
            currencyId: AccountCurrencyType.NCoin,
            target: new NetworkIdentity());

        handler.HandleMessage(session, purchase);

        ServerStoreError error = Assert.Single(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Equal(StoreError.InvalidOffer, error.Error);
    }

    [Fact]
    public void CharacterPurchase_WithPaymentCurrencySlot_UsesSlotPrice()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);
        IGlobalStorefrontManager storefrontManager = CreateStorefrontManager(
            offerId: 1626u,
            accountItemId: 400u,
            priceCurrency: AccountCurrencyType.Protobuck,
            price: 80f);
        var handler = new ClientStorefrontPurchaseCharacterHandler(
            NullLogger<ClientStorefrontPurchaseCharacterHandler>.Instance,
            storefrontManager);

        ClientStorefrontPurchaseCharacter purchase = ReadCharacterPurchase(
            offerId: 1626u,
            currencyId: AccountCurrencyType.NCoin,
            target: new NetworkIdentity(),
            paymentCurrencySlot: (byte)AccountCurrencyType.Protobuck);

        handler.HandleMessage(session, purchase);

        Assert.Empty(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Single(GetMessages<ServerStorePurchaseOfferResult>(sessionProxy));
        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation subtract = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(AccountCurrencyType.Protobuck, subtract.Arguments[0]);
        Assert.Equal(80ul, subtract.Arguments[1]);
        RecordingDispatchProxy<IAccountInventoryManager>.Invocation addItem = Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.AddItem)));
        Assert.Equal(400u, addItem.Arguments[0]);
        var targetIdentity = Assert.IsType<NetworkIdentity>(addItem.Arguments[1]);
        Assert.Equal(100ul, targetIdentity.Id);
        Assert.Equal(1, targetIdentity.RealmId);
        Assert.True((bool)addItem.Arguments[3]);
    }

    [Fact]
    public void CharacterPurchaseSuccess_Emits098C()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        StorefrontPurchaseHelper.SendCharacterPurchaseSuccess(session);

        ServerStorePurchaseOfferResult result = Assert.Single(GetMessages<ServerStorePurchaseOfferResult>(sessionProxy));
        Assert.True(result.IsSuccess);
        Assert.Equal(PurchaseResultDisplayType.Default, result.DisplayType);
    }

    [Fact]
    public void AccountPurchaseSuccess_Emits098DAnd098CCompatibilityResult()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        StorefrontPurchaseHelper.SendAccountPurchaseSuccess(session);

        ServerStorePurchaseOfferResultVariant variantResult = Assert.Single(GetMessages<ServerStorePurchaseOfferResultVariant>(sessionProxy));
        Assert.True(variantResult.IsSuccess);
        Assert.Equal(PurchaseResultDisplayType.Default, variantResult.DisplayType);

        ServerStorePurchaseOfferResult result = Assert.Single(GetMessages<ServerStorePurchaseOfferResult>(sessionProxy));
        Assert.True(result.IsSuccess);
        Assert.Equal(PurchaseResultDisplayType.Default, result.DisplayType);
    }

    [Fact]
    public void AccountGiftPurchase_WithUnknownRecipient_ReturnsIneligibleGiftRecipientStoreError()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IGlobalStorefrontManager storefrontManager = RecordingDispatchProxy<IGlobalStorefrontManager>.Create(out _);
        ICharacterManager characterManager = RecordingDispatchProxy<ICharacterManager>.Create(out _);
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out _);
        var handler = new ClientStorefrontPurchaseAccountHandler(
            NullLogger<ClientStorefrontPurchaseAccountHandler>.Instance,
            storefrontManager,
            characterManager,
            playerManager,
            CreateGameTableManager(),
            new InMemoryAccountPendingItemRepository());

        ClientStorefrontPurchaseAccount purchase = ReadAccountPurchase(
            offerId: 1234u,
            currencyId: AccountCurrencyType.NCoin,
            target: new NetworkIdentity(),
            accountTarget: new NetworkIdentity(),
            recipientName: "Missing Recipient");

        handler.HandleMessage(session, purchase);

        ServerStoreError error = Assert.Single(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Equal(StoreError.IneligibleGiftRecipient, error.Error);
    }

    [Fact]
    public void AccountPurchase_WithPaymentCurrencySlot_UsesSlotPrice()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);
        IGlobalStorefrontManager storefrontManager = CreateStorefrontManager(
            offerId: 1626u,
            accountItemId: 400u,
            priceCurrency: AccountCurrencyType.Protobuck,
            price: 80f);
        var handler = new ClientStorefrontPurchaseAccountHandler(
            NullLogger<ClientStorefrontPurchaseAccountHandler>.Instance,
            storefrontManager,
            RecordingDispatchProxy<ICharacterManager>.Create(out _),
            RecordingDispatchProxy<IPlayerManager>.Create(out _),
            CreateGameTableManager(),
            new InMemoryAccountPendingItemRepository());

        ClientStorefrontPurchaseAccount purchase = ReadAccountPurchase(
            offerId: 1626u,
            currencyId: AccountCurrencyType.NCoin,
            target: new NetworkIdentity(),
            accountTarget: new NetworkIdentity(),
            recipientName: string.Empty,
            paymentCurrencySlot: (byte)AccountCurrencyType.Protobuck);

        handler.HandleMessage(session, purchase);

        Assert.Empty(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Single(GetMessages<ServerStorePurchaseOfferResultVariant>(sessionProxy));
        Assert.Single(GetMessages<ServerStorePurchaseOfferResult>(sessionProxy));
        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation subtract = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(AccountCurrencyType.Protobuck, subtract.Arguments[0]);
        Assert.Equal(80ul, subtract.Arguments[1]);
        RecordingDispatchProxy<IAccountInventoryManager>.Invocation addItem = Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.AddItem)));
        Assert.Equal(400u, addItem.Arguments[0]);
    }

    [Fact]
    public void AccountPurchase_WithoutPlayer_DirectAccountEntitlementUnlock_AddsClaimableAccountItem()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy,
            out RecordingDispatchProxy<IAccountEntitlementManager> entitlementProxy,
            hasPlayer: false);
        IGlobalStorefrontManager storefrontManager = CreateStorefrontManager(
            offerId: 1545u,
            accountItemId: 133u,
            priceCurrency: AccountCurrencyType.Omnibit,
            price: 115f,
            accountItemEntry: CreateCharacterSlotAccountItemEntry());
        var handler = new ClientStorefrontPurchaseAccountHandler(
            NullLogger<ClientStorefrontPurchaseAccountHandler>.Instance,
            storefrontManager,
            RecordingDispatchProxy<ICharacterManager>.Create(out _),
            RecordingDispatchProxy<IPlayerManager>.Create(out _),
            CreateGameTableManager(CreateCharacterSlotEntitlementEntry()),
            new InMemoryAccountPendingItemRepository());

        ClientStorefrontPurchaseAccount purchase = ReadAccountPurchase(
            offerId: 1545u,
            currencyId: AccountCurrencyType.Omnibit,
            target: new NetworkIdentity(),
            accountTarget: new NetworkIdentity(),
            recipientName: string.Empty);

        handler.HandleMessage(session, purchase);

        Assert.Empty(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Single(GetMessages<ServerStorePurchaseOfferResultVariant>(sessionProxy));
        Assert.Single(GetMessages<ServerStorePurchaseOfferResult>(sessionProxy));
        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation subtract = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(AccountCurrencyType.Omnibit, subtract.Arguments[0]);
        Assert.Equal(115ul, subtract.Arguments[1]);
        Assert.Empty(entitlementProxy.GetInvocations(nameof(IAccountEntitlementManager.UpdateEntitlement)));
        RecordingDispatchProxy<IAccountInventoryManager>.Invocation addItem = Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.AddItem)));
        Assert.Equal(133u, addItem.Arguments[0]);
    }

    [Fact]
    public void AccountPurchase_DirectAccountEntitlementAtMax_ReturnsCannotUseOfferAndDoesNotCharge()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy,
            out RecordingDispatchProxy<IAccountEntitlementManager> entitlementProxy,
            hasPlayer: false,
            existingEntitlement: CreateAccountEntitlement(EntitlementType.BaseCharacterSlots, 12u, 12u));
        IGlobalStorefrontManager storefrontManager = CreateStorefrontManager(
            offerId: 1545u,
            accountItemId: 133u,
            priceCurrency: AccountCurrencyType.Omnibit,
            price: 115f,
            accountItemEntry: CreateCharacterSlotAccountItemEntry());
        var handler = new ClientStorefrontPurchaseAccountHandler(
            NullLogger<ClientStorefrontPurchaseAccountHandler>.Instance,
            storefrontManager,
            RecordingDispatchProxy<ICharacterManager>.Create(out _),
            RecordingDispatchProxy<IPlayerManager>.Create(out _),
            CreateGameTableManager(CreateCharacterSlotEntitlementEntry()),
            new InMemoryAccountPendingItemRepository());

        ClientStorefrontPurchaseAccount purchase = ReadAccountPurchase(
            offerId: 1545u,
            currencyId: AccountCurrencyType.Omnibit,
            target: new NetworkIdentity(),
            accountTarget: new NetworkIdentity(),
            recipientName: string.Empty);

        handler.HandleMessage(session, purchase);

        ServerStoreError error = Assert.Single(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Equal(StoreError.CannotUseOffer, error.Error);
        Assert.Empty(GetMessages<ServerStorePurchaseOfferResultVariant>(sessionProxy));
        Assert.Empty(GetMessages<ServerStorePurchaseOfferResult>(sessionProxy));
        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(entitlementProxy.GetInvocations(nameof(IAccountEntitlementManager.UpdateEntitlement)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.AddItem)));
    }

    [Fact]
    public void AccountPurchase_DirectAccountEntitlementGrantAmountExceedsSupportedRange_ReturnsCannotUseOfferAndDoesNotCharge()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy,
            out RecordingDispatchProxy<IAccountEntitlementManager> entitlementProxy,
            hasPlayer: false);
        IGlobalStorefrontManager storefrontManager = CreateStorefrontManager(
            offerId: 1546u,
            accountItemId: 133u,
            priceCurrency: AccountCurrencyType.Omnibit,
            price: 115f,
            accountItemEntry: new AccountItemEntry
            {
                Id               = 133u,
                EntitlementId    = (uint)EntitlementType.BaseCharacterSlots,
                EntitlementCount = 2_000_000_000u
            },
            amount: 3u);
        var handler = new ClientStorefrontPurchaseAccountHandler(
            NullLogger<ClientStorefrontPurchaseAccountHandler>.Instance,
            storefrontManager,
            RecordingDispatchProxy<ICharacterManager>.Create(out _),
            RecordingDispatchProxy<IPlayerManager>.Create(out _),
            CreateGameTableManager(new EntitlementEntry
            {
                Id       = (uint)EntitlementType.BaseCharacterSlots,
                MaxCount = uint.MaxValue,
                Flags    = (uint)EntitlementFlags.None
            }),
            new InMemoryAccountPendingItemRepository());

        ClientStorefrontPurchaseAccount purchase = ReadAccountPurchase(
            offerId: 1546u,
            currencyId: AccountCurrencyType.Omnibit,
            target: new NetworkIdentity(),
            accountTarget: new NetworkIdentity(),
            recipientName: string.Empty);

        handler.HandleMessage(session, purchase);

        ServerStoreError error = Assert.Single(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Equal(StoreError.CannotUseOffer, error.Error);
        Assert.Empty(GetMessages<ServerStorePurchaseOfferResultVariant>(sessionProxy));
        Assert.Empty(GetMessages<ServerStorePurchaseOfferResult>(sessionProxy));
        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(entitlementProxy.GetInvocations(nameof(IAccountEntitlementManager.UpdateEntitlement)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.AddItem)));
    }

    [Fact]
    public void AccountPurchase_DirectAccountCurrencyGrant_AddsClaimableAccountItem()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);
        IGlobalStorefrontManager storefrontManager = CreateStorefrontManager(
            offerId: 1700u,
            accountItemId: 901u,
            priceCurrency: AccountCurrencyType.Protobuck,
            price: 1f,
            accountItemEntry: new AccountItemEntry
            {
                Id                    = 901u,
                AccountCurrencyEnum   = (uint)AccountCurrencyType.Omnibit,
                AccountCurrencyAmount = 610ul
            });
        var handler = new ClientStorefrontPurchaseAccountHandler(
            NullLogger<ClientStorefrontPurchaseAccountHandler>.Instance,
            storefrontManager,
            RecordingDispatchProxy<ICharacterManager>.Create(out _),
            RecordingDispatchProxy<IPlayerManager>.Create(out _),
            CreateGameTableManager(),
            new InMemoryAccountPendingItemRepository());

        ClientStorefrontPurchaseAccount purchase = ReadAccountPurchase(
            offerId: 1700u,
            currencyId: AccountCurrencyType.Protobuck,
            target: new NetworkIdentity(),
            accountTarget: new NetworkIdentity(),
            recipientName: string.Empty);

        handler.HandleMessage(session, purchase);

        Assert.Empty(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Single(GetMessages<ServerStorePurchaseOfferResultVariant>(sessionProxy));
        Assert.Single(GetMessages<ServerStorePurchaseOfferResult>(sessionProxy));
        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation subtract = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(AccountCurrencyType.Protobuck, subtract.Arguments[0]);
        Assert.Equal(1ul, subtract.Arguments[1]);
        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        RecordingDispatchProxy<IAccountInventoryManager>.Invocation addItem = Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.AddItem)));
        Assert.Equal(901u, addItem.Arguments[0]);
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return CreateSession(out sessionProxy, out _, out _);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy)
    {
        return CreateSession(out sessionProxy, out currencyProxy, out inventoryProxy, out _, hasPlayer: true);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy,
        out RecordingDispatchProxy<IAccountEntitlementManager> entitlementProxy,
        bool hasPlayer,
        IAccountEntitlement existingEntitlement = null)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out currencyProxy);
        IAccountInventoryManager inventoryManager = RecordingDispatchProxy<IAccountInventoryManager>.Create(out inventoryProxy);
        IAccountEntitlementManager entitlementManager = RecordingDispatchProxy<IAccountEntitlementManager>.Create(out entitlementProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), 5001u);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventoryManager);
        accountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlementManager);
        entitlementProxy.SetMethodHandler(nameof(IAccountEntitlementManager.GetEntitlement), args =>
            existingEntitlement != null && (EntitlementType)args[0] == existingEntitlement.Type
                ? existingEntitlement
                : null);
        currencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.CanAfford), true);
        inventoryProxy.SetMethodReturn(nameof(IAccountInventoryManager.CanAddItem), true);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        if (!hasPlayer)
        {
            sessionProxy.SetProperty(nameof(IWorldSession.Player), null);
            return session;
        }

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 321u);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity
        {
            RealmId = 1,
            Id      = 100ul
        });

        return session;
    }

    private static IGlobalStorefrontManager CreateStorefrontManager(
        uint offerId,
        uint accountItemId,
        AccountCurrencyType priceCurrency,
        float price,
        AccountItemEntry accountItemEntry = null,
        uint amount = 1u)
    {
        IGlobalStorefrontManager storefrontManager = RecordingDispatchProxy<IGlobalStorefrontManager>.Create(out RecordingDispatchProxy<IGlobalStorefrontManager> storefrontProxy);
        IOfferItem offerItem = RecordingDispatchProxy<IOfferItem>.Create(out RecordingDispatchProxy<IOfferItem> offerProxy);
        IOfferItemPrice itemPrice = RecordingDispatchProxy<IOfferItemPrice>.Create(out RecordingDispatchProxy<IOfferItemPrice> priceProxy);
        IOfferItemData itemData = RecordingDispatchProxy<IOfferItemData>.Create(out RecordingDispatchProxy<IOfferItemData> itemDataProxy);

        itemDataProxy.SetProperty(nameof(IOfferItemData.ItemId), (ushort)accountItemId);
        itemDataProxy.SetProperty(nameof(IOfferItemData.Amount), amount);
        if (accountItemEntry != null)
            itemDataProxy.SetProperty(nameof(IOfferItemData.Entry), accountItemEntry);
        offerProxy.SetProperty(nameof(IOfferItem.Items), new List<IOfferItemData> { itemData });
        priceProxy.SetProperty(nameof(IOfferItemPrice.Price), price);
        offerProxy.SetMethodHandler(nameof(IOfferItem.GetPriceDataForCurrency), args =>
            (AccountCurrencyType)args[0] == priceCurrency ? itemPrice : null);
        storefrontProxy.SetMethodHandler(nameof(IGlobalStorefrontManager.GetStoreOfferItem), args =>
            (uint)args[0] == offerId ? offerItem : null);

        return storefrontManager;
    }

    private static AccountItemEntry CreateCharacterSlotAccountItemEntry()
    {
        return new AccountItemEntry
        {
            Id                      = 133u,
            EntitlementId           = (uint)EntitlementType.BaseCharacterSlots,
            EntitlementCount        = 1u,
            EntitlementScopeEnum    = 2u,
            PrerequisiteId          = 39005u
        };
    }

    private static EntitlementEntry CreateCharacterSlotEntitlementEntry()
    {
        return new EntitlementEntry
        {
            Id       = (uint)EntitlementType.BaseCharacterSlots,
            MaxCount = 12u,
            Flags    = (uint)EntitlementFlags.None
        };
    }

    private static IAccountEntitlement CreateAccountEntitlement(EntitlementType type, uint amount, uint maxCount)
    {
        IAccountEntitlement entitlement = RecordingDispatchProxy<IAccountEntitlement>.Create(out RecordingDispatchProxy<IAccountEntitlement> entitlementProxy);
        entitlementProxy.SetProperty(nameof(IAccountEntitlement.Type), type);
        entitlementProxy.SetProperty(nameof(IAccountEntitlement.Amount), amount);
        entitlementProxy.SetProperty(nameof(IAccountEntitlement.Entry), new EntitlementEntry
        {
            Id       = (uint)type,
            MaxCount = maxCount
        });
        return entitlement;
    }

    private static IGameTableManager CreateGameTableManager(params EntitlementEntry[] entitlementEntries)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        gameTableProxy.SetProperty(nameof(IGameTableManager.Entitlement), CreateGameTable(entitlementEntries));
        return gameTableManager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        int maxId = (int)entries.Select(GetEntryId).DefaultIfEmpty(0u).Max();
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetField(table, "header", new GameTableHeader
        {
            MaxId = (ulong)(maxId + 1)
        });

        var lookup = new int[maxId + 1];
        Array.Fill(lookup, -1);
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        SetField(table, "lookup", lookup);
        return table;
    }

    private static uint GetEntryId<T>(T entry) where T : class, new()
    {
        return (uint)typeof(T)
            .GetField("Id", BindingFlags.Instance | BindingFlags.Public)!
            .GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        SetField(instance, $"<{propertyName}>k__BackingField", value);
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static ClientStorefrontPurchaseCharacter ReadCharacterPurchase(
        uint offerId,
        AccountCurrencyType currencyId,
        NetworkIdentity target,
        byte paymentCurrencySlot = 0,
        uint purchaseMoneyAmountBits = 0)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(offerId);
            writer.Write(paymentCurrencySlot, 5u);
            writer.Write(purchaseMoneyAmountBits);
            writer.Write((uint)currencyId, 14u);
            writer.Write(0u);
            target.Write(writer);
            writer.Write(0u);
            writer.FlushBits();
        }

        byte[] packetData = stream.ToArray();
        using var packetStream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(packetStream);
        var purchase = new ClientStorefrontPurchaseCharacter();
        purchase.Read(reader);
        return purchase;
    }

    private static ClientStorefrontPurchaseAccount ReadAccountPurchase(
        uint offerId,
        AccountCurrencyType currencyId,
        NetworkIdentity target,
        NetworkIdentity accountTarget,
        string recipientName,
        byte paymentCurrencySlot = 0,
        uint purchaseMoneyAmountBits = 0)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(offerId);
            writer.Write(paymentCurrencySlot, 5u);
            writer.Write(purchaseMoneyAmountBits);
            writer.Write((uint)currencyId, 14u);
            writer.Write(0u);
            target.Write(writer);
            writer.Write(0u);
            writer.Write(0u);
            accountTarget.Write(writer);
            writer.WriteStringWide(recipientName);
            writer.FlushBits();
        }

        byte[] packetData = stream.ToArray();
        using var packetStream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(packetStream);
        var purchase = new ClientStorefrontPurchaseAccount();
        purchase.Read(reader);
        return purchase;
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IWorldSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }
}
