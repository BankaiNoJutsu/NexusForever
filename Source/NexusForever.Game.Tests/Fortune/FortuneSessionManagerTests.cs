using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Static.Fortune;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Fortune;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Fortune;

namespace NexusForever.Game.Tests.Fortune;

public class FortuneSessionManagerTests
{
    [Fact]
    public void SendStatus_WithNoSessionSendsEmptyRewardsAndResetCards()
    {
        var manager = new FortuneSessionManager();
        IWorldSession session = CreateSession(42u, out var sessionProxy);

        manager.SendStatus(session);

        object[] messages = GetEncryptedMessages(sessionProxy).ToArray();
        Assert.IsType<ServerFortuneRewards>(messages[0]);

        ServerFortuneCards cards = Assert.IsType<ServerFortuneCards>(messages[1]);
        Assert.Equal(FortuneOperation.Reset, cards.Operation);
        Assert.All(cards.Rarity, rarity => Assert.Equal(RewardRarity.Normal, rarity));
        Assert.All(cards.AccountItemId, itemId => Assert.Equal(0u, itemId));
        Assert.All(cards.CardFlipped, Assert.False);
    }

    [Fact]
    public void FlipCard_AfterStartUpdatesSelectedCard()
    {
        var manager = new FortuneSessionManager();
        IWorldSession session = CreateSession(42u, out var sessionProxy);

        manager.Start(session);
        manager.FlipCard(session, CreateFlipCard(1u));

        ServerFortuneCards startCards = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneCards>().Single();
        Assert.Equal(FortuneOperation.Update, startCards.Operation);
        Assert.All(startCards.CardFlipped, Assert.False);

        ServerFortuneCardUpdate update = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneCardUpdate>().Single();
        Assert.Equal(FortuneOperation.Update, update.Operation);
        Assert.Equal([false, true, false], update.CardFlipped);
    }

    [Fact]
    public void SendStatus_AfterFlipSendsCurrentCardState()
    {
        var manager = new FortuneSessionManager();
        IWorldSession session = CreateSession(42u, out var sessionProxy);

        manager.Start(session);
        manager.FlipCard(session, CreateFlipCard(2u));
        manager.SendStatus(session);

        ServerFortuneCards statusCards = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneCards>().Last();
        Assert.Equal(FortuneOperation.Update, statusCards.Operation);
        Assert.Equal([false, false, true], statusCards.CardFlipped);
    }

    [Fact]
    public void SendStatus_UsesAccountScopedSessionState()
    {
        var manager = new FortuneSessionManager();
        IWorldSession firstSession = CreateSession(42u, out var firstSessionProxy);
        IWorldSession secondSession = CreateSession(77u, out var secondSessionProxy);

        manager.Start(firstSession);
        manager.FlipCard(firstSession, CreateFlipCard(0u));
        manager.SendStatus(secondSession);

        ServerFortuneCardUpdate firstUpdate = GetEncryptedMessages(firstSessionProxy).OfType<ServerFortuneCardUpdate>().Single();
        Assert.Equal([true, false, false], firstUpdate.CardFlipped);
        ServerFortuneCards secondCards = GetEncryptedMessages(secondSessionProxy).OfType<ServerFortuneCards>().Single();
        Assert.Equal(FortuneOperation.Reset, secondCards.Operation);
        Assert.All(secondCards.CardFlipped, Assert.False);
    }

    [Fact]
    public void Start_ReplacesExistingSessionForAccount()
    {
        var manager = new FortuneSessionManager();
        IWorldSession session = CreateSession(42u, out var sessionProxy);

        manager.Start(session);
        manager.FlipCard(session, CreateFlipCard(0u));
        manager.Start(session);

        ServerFortuneCards restartedCards = GetEncryptedMessages(sessionProxy)
            .OfType<ServerFortuneCards>()
            .Last();
        Assert.Equal(FortuneOperation.Update, restartedCards.Operation);
        Assert.All(restartedCards.CardFlipped, Assert.False);
    }

    [Fact]
    public void FlipCard_WithoutStartedSessionSendsResetClick()
    {
        var manager = new FortuneSessionManager();
        IWorldSession session = CreateSession(42u, out var sessionProxy);

        manager.FlipCard(session, CreateFlipCard(0u));

        ServerFortuneReset reset = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneReset>().Single();
        Assert.Equal(3u, reset.Unknown);
    }

    [Fact]
    public void FlipCard_WithInvalidIndexSendsResetClick()
    {
        var manager = new FortuneSessionManager();
        IWorldSession session = CreateSession(42u, out var sessionProxy);

        manager.Start(session);
        manager.FlipCard(session, CreateFlipCard(3u));

        ServerFortuneReset reset = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneReset>().Single();
        Assert.Equal(3u, reset.Unknown);
    }

    private static IWorldSession CreateSession(uint accountId, out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), accountId);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        return session;
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private static ClientFortuneFlipCard CreateFlipCard(uint selectedCardIndex)
    {
        var flipCard = (ClientFortuneFlipCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientFortuneFlipCard));
        typeof(ClientFortuneFlipCard)
            .GetProperty(nameof(ClientFortuneFlipCard.SelectedCardIndex))!
            .SetValue(flipCard, selectedCardIndex);
        return flipCard;
    }
}
