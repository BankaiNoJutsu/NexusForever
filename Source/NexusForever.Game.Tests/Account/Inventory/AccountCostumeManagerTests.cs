using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Costume;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Account.Inventory;

[Collection(LegacyServiceProviderCollection.Name)]
public class AccountCostumeManagerTests
{
    private const uint ItemId = 12345u;

    [Fact]
    public void UnlockItem_WithNonEquippableItemSendsInvalidWithoutUnlockingOrSoulbinding()
    {
        AccountCostumeManager manager = CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IItem item = CreateItem(isEquippable: false, out RecordingDispatchProxy<IItem> itemProxy);

        manager.UnlockItem(player: null, item);

        RecordingDispatchProxy<IGameSession>.Invocation call = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        ServerCostumeItemUnlock result = Assert.IsType<ServerCostumeItemUnlock>(call.Arguments[0]);
        Assert.Equal(CostumeUnlockResult.InvalidItem, result.Result);
        Assert.Equal(0u, result.ItemId);
        Assert.False(manager.HasItemUnlock(ItemId));
        Assert.Empty(itemProxy.GetInvocations(nameof(IItem.MakeSoulbound)));
    }

    [Fact]
    public void UnlockItem_WithMissingFormulaTableUsesDefaultLimitAndUnlocks()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            AccountCostumeManager manager = CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy);
            IItem item = CreateItem(isEquippable: true, out RecordingDispatchProxy<IItem> itemProxy);

            manager.UnlockItem(player: null, item);

            RecordingDispatchProxy<IGameSession>.Invocation call = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
            ServerCostumeItemUnlock result = Assert.IsType<ServerCostumeItemUnlock>(call.Arguments[0]);
            Assert.Equal(CostumeUnlockResult.UnlockSuccess, result.Result);
            Assert.Equal(ItemId, result.ItemId);
            Assert.True(manager.HasItemUnlock(ItemId));
            Assert.Single(itemProxy.GetInvocations(nameof(IItem.MakeSoulbound)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ForgetItem_WithMissingItemTableSendsInvalidWithoutForgetting()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            AccountCostumeManager manager = CreateManager(
                out RecordingDispatchProxy<IGameSession> sessionProxy,
                ItemId);

            manager.ForgetItem(ItemId);

            RecordingDispatchProxy<IGameSession>.Invocation call = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
            ServerCostumeItemUnlock result = Assert.IsType<ServerCostumeItemUnlock>(call.Arguments[0]);
            Assert.Equal(CostumeUnlockResult.InvalidItem, result.Result);
            Assert.Equal(0u, result.ItemId);
            Assert.True(manager.HasItemUnlock(ItemId));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static AccountCostumeManager CreateManager(
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        params uint[] unlockedItemIds)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IAccountEntitlementManager entitlementManager = RecordingDispatchProxy<IAccountEntitlementManager>.Create(out _);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        accountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlementManager);

        return new AccountCostumeManager(account, new AccountModel
        {
            Id = 1u,
            AccountCostumeUnlock = unlockedItemIds
                .Select(itemId => new AccountCostumeUnlockModel
                {
                    ItemId = itemId
                })
                .ToList()
        });
    }

    private static IItem CreateItem(bool isEquippable, out RecordingDispatchProxy<IItem> itemProxy)
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsEquippable), isEquippable);

        IItem item = RecordingDispatchProxy<IItem>.Create(out itemProxy);
        itemProxy.SetProperty(nameof(IItem.Id), ItemId);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        return item;
    }

    private static IServiceProvider BuildProvider()
    {
        return new ServiceCollection()
            .AddSingleton(new GameTableManager(Options.Create(new GameTableConfig
            {
                GameTablePath = string.Empty
            })))
            .BuildServiceProvider();
    }
}
