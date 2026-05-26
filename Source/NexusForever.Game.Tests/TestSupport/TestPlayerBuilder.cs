using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.TestSupport;

internal sealed class TestPlayerBuilder
{
    private readonly IPlayer player;
    private readonly RecordingDispatchProxy<IPlayer> playerProxy;

    private TestPlayerBuilder()
    {
        player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
    }

    public RecordingDispatchProxy<IPlayer> PlayerProxy => playerProxy;

    public static TestPlayerBuilder Create()
    {
        return new TestPlayerBuilder();
    }

    public TestPlayerBuilder WithAccount(IAccount account)
    {
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        return this;
    }

    public TestPlayerBuilder WithAchievementManager(ICharacterAchievementManager achievementManager)
    {
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        return this;
    }

    public TestPlayerBuilder WithCharacterId(ulong characterId)
    {
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        return this;
    }

    public TestPlayerBuilder WithGuid(uint guid)
    {
        playerProxy.SetProperty(nameof(IPlayer.Guid), guid);
        return this;
    }

    public TestPlayerBuilder WithIdentity(Identity identity)
    {
        playerProxy.SetProperty(nameof(IPlayer.Identity), identity);
        return this;
    }

    public TestPlayerBuilder WithInventory(IInventory inventory)
    {
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        return this;
    }

    public TestPlayerBuilder WithCurrencyManager(ICurrencyManager currencyManager)
    {
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        return this;
    }

    public TestPlayerBuilder WithSession(IGameSession session)
    {
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return this;
    }

    public IPlayer Build()
    {
        return player;
    }
}
