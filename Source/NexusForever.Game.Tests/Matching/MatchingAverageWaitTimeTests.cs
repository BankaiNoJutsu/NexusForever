using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Matching.Queue;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Matching;

public class MatchingAverageWaitTimeTests
{
    [Fact]
    public void SendAverageWaitTimeUpdate_WhenQueued_SendsOpcode0628()
    {
        Identity identity = new() { RealmId = 1, Id = 401ul };
        MatchingCharacter character = CreateCharacter(identity, out RecordingDispatchProxy<IGameSession> sessionProxy, out _);
        IMatchingQueueProposal proposal = RecordingDispatchProxy<IMatchingQueueProposal>.Create(out RecordingDispatchProxy<IMatchingQueueProposal> proposalProxy);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.MatchType), MatchType.Dungeon);
        IMatchingQueueGroup group = RecordingDispatchProxy<IMatchingQueueGroup>.Create(out _);
        character.AddMatchingQueueProposal(proposal, group);

        character.SendAverageWaitTimeUpdate(MatchType.Dungeon, 120_000u);

        ServerMatchingAverageWaitTimeUpdate update = Assert.Single(GetEncryptedMessages<ServerMatchingAverageWaitTimeUpdate>(sessionProxy));
        Assert.Equal(MatchType.Dungeon, update.Type);
        Assert.Equal(120_000u, update.AverageWaitTime);
    }

    [Fact]
    public void SendAverageWaitTimeUpdate_WhenNotQueued_DoesNotSend()
    {
        Identity identity = new() { RealmId = 1, Id = 402ul };
        MatchingCharacter character = CreateCharacter(identity, out RecordingDispatchProxy<IGameSession> sessionProxy, out _);

        character.SendAverageWaitTimeUpdate(MatchType.Dungeon, 120_000u);

        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    private static MatchingCharacter CreateCharacter(
        Identity identity,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<IMatchingManager> matchingManagerProxy)
    {
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        IMatchManager matchManager = RecordingDispatchProxy<IMatchManager>.Create(out RecordingDispatchProxy<IMatchManager> matchManagerProxy);
        IMatchingManager matchingManager = RecordingDispatchProxy<IMatchingManager>.Create(out matchingManagerProxy);

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Identity), identity);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        IMatchCharacter matchCharacter = RecordingDispatchProxy<IMatchCharacter>.Create(out _);
        matchManagerProxy.SetMethodReturn(nameof(IMatchManager.GetMatchCharacter), matchCharacter);

        var character = new MatchingCharacter(
            NullLogger<MatchingCharacter>.Instance,
            playerManager,
            matchManager,
            matchingManager);
        character.Initialise(identity);
        return character;
    }

    private static List<T> GetEncryptedMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1 && i.Arguments[0] is T)
            .Select(i => (T)i.Arguments[0])
            .ToList();
    }
}
