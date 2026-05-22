using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
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
    public void HandleMessage_DelegatesChoiceToChallengeManager()
    {
        IChallengeManager challengeManager = RecordingDispatchProxy<IChallengeManager>.Create(out RecordingDispatchProxy<IChallengeManager> challengeProxy);
        IPlayer player = CreatePlayer(challengeManager);
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        var handler = new ClientChallengeChoiceHandler(NullLogger<ClientChallengeChoiceHandler>.Instance);
        ClientChallengeChoice choice = CreateChoice(1234, ChallengeChoice.AcceptShared, 0u);

        handler.HandleMessage(session, choice);

        RecordingDispatchProxy<IChallengeManager>.Invocation invocation = Assert.Single(
            challengeProxy.GetInvocations(nameof(IChallengeManager.HandleChoice)));
        Assert.Equal((ushort)1234, invocation.Arguments[0]);
        Assert.Equal(ChallengeChoice.AcceptShared, invocation.Arguments[1]);
    }

    private static IPlayer CreatePlayer(IChallengeManager challengeManager)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), challengeManager);
        return player;
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
