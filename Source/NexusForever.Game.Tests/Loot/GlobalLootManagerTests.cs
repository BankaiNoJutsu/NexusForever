using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Chat;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Network.World.Message.Model.Story;
using NexusForever.Shared;
using NetworkLootItem = NexusForever.Network.World.Message.Model.Loot.LootItem;

namespace NexusForever.Game.Tests.Loot;

[Collection(LegacyServiceProviderCollection.Name)]
public class GlobalLootManagerTests
{
    [Fact]
    public void RemoveClientLootUnitIdOverlay_NormalisesObservedCollectId()
    {
        uint normalised = GlobalLootManager.RemoveClientLootUnitIdOverlay(0xC0000019u);

        Assert.Equal(0x80000019u, normalised);
    }

    [Fact]
    public void IsMappedFlatLootGroup_DataMappingComment_ReturnsTrue()
    {
        LootGroup group = CreateLootGroup("DataMapping creature_loot: Grimstone Digger");

        Assert.True(GlobalLootManager.IsMappedFlatLootGroup(group));
    }

    [Fact]
    public void IsMappedFlatLootGroup_LaughingWsQuestLootComment_ReturnsFalse()
    {
        LootGroup group = CreateLootGroup("LaughingWS quest loot: Item for QuestObjective 13011");

        Assert.False(GlobalLootManager.IsMappedFlatLootGroup(group));
    }

    [Fact]
    public void GiveGeneratedLoot_AccountCurrencyWithGrantedNotify_SendsSingleGrantedExplosionNotifyFromParentSource()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);
        IPlayer player = CreatePlayer(out var currencyProxy, out var achievementProxy, out var sessionProxy);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(manager)
            .BuildServiceProvider();

        try
        {
            manager.GiveGeneratedLoot(
                player,
                [new GeneratedLootItem(LootItemType.AccountCurrency, (uint)AccountCurrencyType.Omnibit, 75u)],
                ownerUnitId: 99u,
                sendGrantedNotify: true,
                parentUnitId: 123u);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        Assert.Equal(AccountCurrencyType.Omnibit, currencyCall.Arguments[0]);
        Assert.Equal(75ul, currencyCall.Arguments[1]);

        Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));

        var sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionCalls);
        var notify = Assert.IsType<ServerLootNotify>(sessionCall.Arguments[0]);
        Assert.Equal(99u, notify.OwnerUnitId);
        Assert.Equal(123u, notify.ParentUnitId);
        Assert.True(notify.Explosion);

        var lootItem = Assert.Single(notify.LootItems);
        Assert.Equal(0u, lootItem.LootUnitId);
        Assert.Equal(LootItemType.AccountCurrency, lootItem.Type);
        Assert.Equal((uint)AccountCurrencyType.Omnibit, lootItem.ItemId);
        Assert.Equal(75u, lootItem.Amount);
        Assert.True(lootItem.CanLoot);
        Assert.True(lootItem.Granted);
        Assert.True(lootItem.Explosion);
    }

    [Fact]
    public void GiveGeneratedLoot_MultipleItemsWithGrantedNotify_SendsSingleExplosionNotifyWithActualRows()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);
        IPlayer player = CreatePlayer(
            out var accountCurrencyProxy,
            out var characterCurrencyProxy,
            out var achievementProxy,
            out var sessionProxy);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(manager)
            .BuildServiceProvider();

        try
        {
            manager.GiveGeneratedLoot(
                player,
                [
                    new GeneratedLootItem(LootItemType.AccountCurrency, (uint)AccountCurrencyType.Omnibit, 75u),
                    new GeneratedLootItem(LootItemType.Cash, (uint)CurrencyType.Credits, 17u)
                ],
                ownerUnitId: 99u,
                sendGrantedNotify: true,
                parentUnitId: 123u);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation accountCurrencyCall = Assert.Single(accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        Assert.Equal(AccountCurrencyType.Omnibit, accountCurrencyCall.Arguments[0]);
        Assert.Equal(75ul, accountCurrencyCall.Arguments[1]);

        RecordingDispatchProxy<ICurrencyManager>.Invocation characterCurrencyCall = Assert.Single(characterCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, characterCurrencyCall.Arguments[0]);
        Assert.Equal(17ul, characterCurrencyCall.Arguments[1]);
        Assert.True((bool)characterCurrencyCall.Arguments[2]);

        Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));

        var sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionCalls);
        var notify = Assert.IsType<ServerLootNotify>(sessionCall.Arguments[0]);
        Assert.Equal(99u, notify.OwnerUnitId);
        Assert.Equal(123u, notify.ParentUnitId);
        Assert.True(notify.Explosion);
        Assert.Equal(2, notify.LootItems.Count);

        NetworkLootItem accountLootItem = Assert.Single(notify.LootItems, item => item.Type == LootItemType.AccountCurrency);
        Assert.Equal(0u, accountLootItem.LootUnitId);
        Assert.Equal((uint)AccountCurrencyType.Omnibit, accountLootItem.ItemId);
        Assert.Equal(75u, accountLootItem.Amount);
        Assert.True(accountLootItem.CanLoot);
        Assert.True(accountLootItem.Granted);
        Assert.True(accountLootItem.Explosion);

        NetworkLootItem cashLootItem = Assert.Single(notify.LootItems, item => item.Type == LootItemType.Cash);
        Assert.Equal(0u, cashLootItem.LootUnitId);
        Assert.Equal((uint)CurrencyType.Credits, cashLootItem.ItemId);
        Assert.Equal(17u, cashLootItem.Amount);
        Assert.True(cashLootItem.CanLoot);
        Assert.True(cashLootItem.Granted);
        Assert.True(cashLootItem.Explosion);
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return CreatePlayer(
            out currencyProxy,
            out _,
            out achievementProxy,
            out sessionProxy);
    }

    private static LootGroup CreateLootGroup(string comment)
    {
        return new LootGroup(new LootGroupModel
        {
            Comment = comment,
            Item = []
        }, loadChildren: false);
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out RecordingDispatchProxy<ICurrencyManager> characterCurrencyProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out currencyProxy);
        ICurrencyManager characterCurrencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out characterCurrencyProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);

        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), characterCurrencyManager);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty("Guid", 4242u);

        return player;
    }
}
