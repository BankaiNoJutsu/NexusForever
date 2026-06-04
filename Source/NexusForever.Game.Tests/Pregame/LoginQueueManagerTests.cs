using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Character;

namespace NexusForever.Game.Tests.Pregame;

public class LoginQueueManagerTests
{
    [Fact]
    public void OnNewSession_WhenRealmIsFull_MarksSessionQueuedAndOnlySendsQueueStatus()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        ICharacterListManager characterListManager = RecordingDispatchProxy<ICharacterListManager>.Create(
            out RecordingDispatchProxy<ICharacterListManager> characterListProxy);
        var manager = new LoginQueueManager(
            NullLogger<LoginQueueManager>.Instance,
            Options.Create(new RealmConfig
            {
                MaxPlayers = 0
            }),
            RecordingDispatchProxy<INetworkManager<IWorldSession>>.Create(out _),
            characterListManager);

        bool admitted = manager.OnNewSession(session);

        Assert.False(admitted);
        Assert.True(session.IsQueued);
        Assert.Empty(characterListProxy.GetInvocations(nameof(ICharacterListManager.SendCharacterListPackets)));

        RecordingDispatchProxy<IWorldSession>.Invocation send = Assert.Single(
            sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        var status = Assert.IsType<ServerQueueStatus>(send.Arguments[0]);
        Assert.Equal(1u, status.QueuePosition);
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IAccountRBACManager rbacManager = RecordingDispatchProxy<IAccountRBACManager>.Create(out RecordingDispatchProxy<IAccountRBACManager> rbacProxy);
        rbacProxy.SetMethodReturn(nameof(IAccountRBACManager.HasPermission), false);

        accountProxy.SetProperty(nameof(IAccount.Id), 42u);
        accountProxy.SetProperty(nameof(IAccount.RbacManager), rbacManager);

        sessionProxy.SetProperty(nameof(IWorldSession.Id), "queue-test");
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Characters), new List<CharacterModel>());
        sessionProxy.SetProperty(nameof(IWorldSession.Events), new EventQueue());
        sessionProxy.SetProperty(nameof(IWorldSession.Heartbeat), new SocketHeartbeat());
        return session;
    }
}
