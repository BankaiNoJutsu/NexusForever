using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Inventory;

[Collection(LegacyServiceProviderCollection.Name)]
public class DailyLoginRewardManagerTests
{
    [Fact]
    public void SendDailyLoginUpdate_EmitsLastClaimedLoginDayInsteadOfLastRewardItemKey()
    {
        using LegacyServiceProviderScope _ = UseDailyLoginRewards(CreateDailyRewards(10u));

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Session), session);

        var manager = new DailyLoginRewardManager(account, new AccountModel
        {
            AccountDailyLogin = new AccountDailyLoginModel
            {
                LoginDaysTotal      = 10u,
                RewardsAvailable    = 3u,
                LastRewardItemKey   = 987654u,
                PremiumKeyStatus    = 2u,
                SecondsUntilNextKey = 1234u,
                LastDayIncrementUtc = DateTime.UtcNow
            }
        });

        manager.SendDailyLoginUpdate();

        RecordingDispatchProxy<IGameSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var packet = Assert.IsType<ServerDailyLoginUpdate>(invocation.Arguments[0]);
        Assert.Equal(10u, packet.Value0);
        Assert.Equal(3u, packet.Value1);
        Assert.Equal(7u, packet.Value2);
        Assert.Equal(1234u, packet.Value4);
        Assert.Equal(2u, packet.UInt3Value);
    }

    [Fact]
    public void TryClaimReward_ClaimsNextUnclaimedLoginDay()
    {
        using LegacyServiceProviderScope _ = UseDailyLoginRewards(
            new DailyLoginRewardEntry { Id = 1u, LoginDay = 1u, RewardObjectValue = 101u },
            new DailyLoginRewardEntry { Id = 2u, LoginDay = 2u, RewardObjectValue = 102u },
            new DailyLoginRewardEntry { Id = 3u, LoginDay = 3u, RewardObjectValue = 103u });

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IAccountInventoryManager inventory = RecordingDispatchProxy<IAccountInventoryManager>.Create(out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IAccountInventoryManager.CanAddItem), true);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventory);

        var manager = new DailyLoginRewardManager(account, new AccountModel
        {
            AccountDailyLogin = new AccountDailyLoginModel
            {
                LoginDaysTotal      = 3u,
                RewardsAvailable    = 3u,
                LastDayIncrementUtc = DateTime.UtcNow
            }
        });

        AccountOperationResult result = manager.TryClaimReward();

        Assert.Equal(AccountOperationResult.Ok, result);
        RecordingDispatchProxy<IAccountInventoryManager>.Invocation addInvocation =
            Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.AddItem)));
        Assert.Equal(101u, addInvocation.Arguments[0]);

        RecordingDispatchProxy<IGameSession>.Invocation packetInvocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var packet = Assert.IsType<ServerDailyLoginUpdate>(packetInvocation.Arguments[0]);
        Assert.Equal(1u, packet.Value2);
    }

    [Fact]
    public void TryClaimReward_UsesPersistedClaimedDayWhenAvailableCountIsStale()
    {
        using LegacyServiceProviderScope _ = UseDailyLoginRewards(
            new DailyLoginRewardEntry { Id = 1u, LoginDay = 1u, RewardObjectValue = 101u },
            new DailyLoginRewardEntry { Id = 2u, LoginDay = 2u, RewardObjectValue = 102u },
            new DailyLoginRewardEntry { Id = 3u, LoginDay = 3u, RewardObjectValue = 103u },
            new DailyLoginRewardEntry { Id = 4u, LoginDay = 4u, RewardObjectValue = 104u });

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IAccountInventoryManager inventory = RecordingDispatchProxy<IAccountInventoryManager>.Create(out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IAccountInventoryManager.CanAddItem), true);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventory);

        var manager = new DailyLoginRewardManager(account, new AccountModel
        {
            AccountDailyLogin = new AccountDailyLoginModel
            {
                LoginDaysTotal        = 4u,
                RewardsAvailable      = 4u,
                LastClaimedLoginDay   = 3u,
                LastDayIncrementUtc   = DateTime.UtcNow
            }
        });

        AccountOperationResult result = manager.TryClaimReward();

        Assert.Equal(AccountOperationResult.Ok, result);
        RecordingDispatchProxy<IAccountInventoryManager>.Invocation addInvocation =
            Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.AddItem)));
        Assert.Equal(104u, addInvocation.Arguments[0]);

        RecordingDispatchProxy<IGameSession>.Invocation packetInvocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var packet = Assert.IsType<ServerDailyLoginUpdate>(packetInvocation.Arguments[0]);
        Assert.Equal(0u, packet.Value1);
        Assert.Equal(4u, packet.Value2);
    }

    [Fact]
    public void SendDailyLoginUpdate_DoesNotMakeFinalClaimedRewardAvailableAfterScheduleEnd()
    {
        using LegacyServiceProviderScope _ = UseDailyLoginRewards(
            new DailyLoginRewardEntry { Id = 1u, LoginDay = 1u, RewardObjectValue = 101u },
            new DailyLoginRewardEntry { Id = 2u, LoginDay = 2u, RewardObjectValue = 102u });

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Session), session);

        var manager = new DailyLoginRewardManager(account, new AccountModel
        {
            AccountDailyLogin = new AccountDailyLoginModel
            {
                LoginDaysTotal        = 2u,
                RewardsAvailable      = 0u,
                LastClaimedLoginDay   = 2u,
                LastDayIncrementUtc   = DateTime.UtcNow.AddDays(-1d)
            }
        });

        manager.SendDailyLoginUpdate();

        RecordingDispatchProxy<IGameSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var packet = Assert.IsType<ServerDailyLoginUpdate>(invocation.Arguments[0]);
        Assert.Equal(3u, packet.Value0);
        Assert.Equal(0u, packet.Value1);
        Assert.Equal(2u, packet.Value2);
    }

    [Fact]
    public void TryClaimReward_DoesNotRepeatFinalConfiguredRewardAfterAdditionalLoginDay()
    {
        using LegacyServiceProviderScope _ = UseDailyLoginRewards(
            new DailyLoginRewardEntry { Id = 1u, LoginDay = 1u, RewardObjectValue = 101u },
            new DailyLoginRewardEntry { Id = 2u, LoginDay = 2u, RewardObjectValue = 102u });

        IAccountInventoryManager inventory = RecordingDispatchProxy<IAccountInventoryManager>.Create(out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventory);

        var manager = new DailyLoginRewardManager(account, new AccountModel
        {
            AccountDailyLogin = new AccountDailyLoginModel
            {
                LoginDaysTotal        = 2u,
                RewardsAvailable      = 0u,
                LastClaimedLoginDay   = 2u,
                LastDayIncrementUtc   = DateTime.UtcNow.AddDays(-1d)
            }
        });

        AccountOperationResult result = manager.TryClaimReward();

        Assert.Equal(AccountOperationResult.AlreadyClaimed, result);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.AddItem)));
    }

    private static GameTableManager CreateGameTableManager(params DailyLoginRewardEntry[] dailyLoginRewards)
    {
        var manager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        SetAutoProperty(manager, nameof(GameTableManager.DailyLoginReward), CreateGameTable(dailyLoginRewards));
        return manager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static DailyLoginRewardEntry[] CreateDailyRewards(uint count)
    {
        return Enumerable.Range(1, (int)count)
            .Select(i => new DailyLoginRewardEntry
            {
                Id                = (uint)i,
                LoginDay          = (uint)i,
                RewardObjectValue = 100u + (uint)i
            })
            .ToArray();
    }

    private static LegacyServiceProviderScope UseDailyLoginRewards(params DailyLoginRewardEntry[] dailyLoginRewards)
    {
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(CreateGameTableManager(dailyLoginRewards))
            .BuildServiceProvider();
        return new LegacyServiceProviderScope(provider);
    }
}
