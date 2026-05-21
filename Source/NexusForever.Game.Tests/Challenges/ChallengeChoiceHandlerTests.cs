using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Challenges;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Challenges;

public class ChallengeChoiceHandlerTests
{
    [Fact]
    public void HandleMessage_SendsGenericFailureWithoutMutatingChallengeState()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var handler = new ClientChallengeChoiceHandler(NullLogger<ClientChallengeChoiceHandler>.Instance);
        ClientChallengeChoice choice = CreateChoice(1234, ChallengeChoice.AcceptShared, 0u);

        handler.HandleMessage(session, choice);

        ServerChallengeResult result = GetEncryptedMessages(sessionProxy)
            .OfType<ServerChallengeResult>()
            .Single();
        Assert.Equal(1234, result.ChallengeId);
        Assert.Equal(ChallengeResult.GenericFail, result.Result);
        Assert.Equal(0, result.Data);
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private static ClientChallengeChoice CreateChoice(ushort challengeId, ChallengeChoice choice, uint unused)
    {
        var message = (ClientChallengeChoice)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientChallengeChoice));
        typeof(ClientChallengeChoice)
            .GetProperty(nameof(ClientChallengeChoice.ChallengeId))!
            .SetValue(message, challengeId);
        typeof(ClientChallengeChoice)
            .GetProperty(nameof(ClientChallengeChoice.Choice))!
            .SetValue(message, choice);
        typeof(ClientChallengeChoice)
            .GetProperty(nameof(ClientChallengeChoice.Unused))!
            .SetValue(message, unused);
        return message;
    }
}
