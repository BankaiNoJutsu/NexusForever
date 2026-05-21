using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Shared;

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
    public void GiveGeneratedLoot_AccountCurrencyWithGrantedNotify_SendsDirectGrantWithoutExplosionNotify()
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
                [new GeneratedLootItem(LootItemType.AccountCurrency, (uint)AccountCurrencyType.Omnibit, 4u)],
                ownerUnitId: 99u,
                sendGrantedNotify: true);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        Assert.Equal(AccountCurrencyType.Omnibit, currencyCall.Arguments[0]);
        Assert.Equal(4ul, currencyCall.Arguments[1]);

        Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));

        RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var grant = Assert.IsType<ServerLootGrant>(sessionCall.Arguments[0]);
        Assert.Equal(99u, grant.OwnerUnitId);
        Assert.Equal(4242u, grant.LooterUnitId);
        Assert.Equal(LootItemType.AccountCurrency, grant.LootItem.Type);
        Assert.Equal((uint)AccountCurrencyType.Omnibit, grant.LootItem.ItemId);
        Assert.Equal(4u, grant.LootItem.Amount);
        Assert.False(grant.LootItem.Granted);
        Assert.False(grant.LootItem.Explosion);
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out currencyProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);

        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty("Guid", 4242u);

        return player;
    }
}
