using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Matching.Queue;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Matching;

public class MatchingCharacterStatusTests
{
    [Fact]
    public void SendMatchingStatus_IncludesActiveRoleCheckMatchType()
    {
        Identity identity = new()
        {
            RealmId = 1,
            Id      = 100ul
        };
        MatchingCharacter character = CreateCharacter(identity,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out RecordingDispatchProxy<IMatchingManager> matchingManagerProxy);
        matchingManagerProxy.SetMethodReturn(nameof(IMatchingManager.GetReadyMatchType), MatchType.Dungeon);

        character.SendMatchingStatus();

        ServerMatchingQueueStatus status = Assert.Single(GetEncryptedMessages<ServerMatchingQueueStatus>(sessionProxy));
        Assert.Equal(MatchType.Dungeon, status.ReadyMatchType);
        Assert.Equal(MatchType.None, status.JoinedMatchType);
    }

    [Fact]
    public void RemoveMatchingQueueProposal_WhenQueueIsAbsent_DoesNotThrowOrSendPackets()
    {
        Identity identity = new()
        {
            RealmId = 1,
            Id      = 200ul
        };
        MatchingCharacter character = CreateCharacter(identity,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _);

        Exception exception = Record.Exception(() => character.RemoveMatchingQueueProposal(MatchType.Arena));

        Assert.Null(exception);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void RemoveMatchingQueueProposal_WhenQueueExists_SendsLeftQueueAndStatus()
    {
        Identity identity = new()
        {
            RealmId = 1,
            Id      = 300ul
        };
        MatchingCharacter character = CreateCharacter(identity,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _);
        IMatchingQueueProposal proposal = RecordingDispatchProxy<IMatchingQueueProposal>.Create(out RecordingDispatchProxy<IMatchingQueueProposal> proposalProxy);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.MatchType), MatchType.Dungeon);
        IMatchingQueueGroup group = RecordingDispatchProxy<IMatchingQueueGroup>.Create(out _);
        character.AddMatchingQueueProposal(proposal, group);

        character.RemoveMatchingQueueProposal(MatchType.Dungeon);

        object[] messages = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .ToArray();
        Assert.Collection(messages,
            message => Assert.IsType<ServerMatchingQueueResultAnnounce>(message),
            message => Assert.IsType<ServerMatchingLeftQueue>(message),
            message => Assert.IsType<ServerMatchingQueueStatus>(message));
    }

    private static MatchingCharacter CreateCharacter(
        Identity identity,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<IMatchingManager> matchingManagerProxy)
    {
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        IMatchCharacterStore matchCharacterStore = RecordingDispatchProxy<IMatchCharacterStore>.Create(out RecordingDispatchProxy<IMatchCharacterStore> matchCharacterStoreProxy);
        IMatchingManager matchingManager = RecordingDispatchProxy<IMatchingManager>.Create(out matchingManagerProxy);

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Identity), identity);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        IMatchCharacter matchCharacter = RecordingDispatchProxy<IMatchCharacter>.Create(out _);
        matchCharacterStoreProxy.SetMethodReturn(nameof(IMatchCharacterStore.GetMatchCharacter), matchCharacter);

        var character = new MatchingCharacter(
            NullLogger<MatchingCharacter>.Instance,
            playerManager,
            matchCharacterStore,
            matchingManager);
        character.Initialise(identity);
        return character;
    }

    private static IReadOnlyList<T> GetEncryptedMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }
}
