using System.Runtime.CompilerServices;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Character;

namespace NexusForever.Game.Tests.Pregame;

public class CharacterSelectHandlerTests
{
    [Fact]
    public void HandleMessage_DuplicateSelectForLoadingPlayer_IsIgnored()
    {
        CharacterSelectHandler handler = CreateHandler(
            out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy,
            out RecordingDispatchProxy<IMapManager> mapManagerProxy);
        IWorldSession session = CreateSession(123ul, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        handler.HandleMessage(session, CreateCharacterSelect(123ul));

        Assert.Empty(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateEntity)));
        Assert.Empty(mapManagerProxy.GetInvocations(nameof(IMapManager.AddToMap)));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void HandleMessage_SelectDifferentCharacterWhilePlayerExists_SendsInWorldFailure()
    {
        CharacterSelectHandler handler = CreateHandler(
            out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy,
            out RecordingDispatchProxy<IMapManager> mapManagerProxy);
        IWorldSession session = CreateSession(123ul, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        handler.HandleMessage(session, CreateCharacterSelect(456ul));

        Assert.Empty(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateEntity)));
        Assert.Empty(mapManagerProxy.GetInvocations(nameof(IMapManager.AddToMap)));

        RecordingDispatchProxy<IWorldSession>.Invocation send = Assert.Single(
            sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        var failure = Assert.IsType<ServerCharacterSelectFail>(send.Arguments[0]);
        Assert.Equal(CharacterSelectResult.FailedCharacterInWorld, failure.Result);
    }

    private static CharacterSelectHandler CreateHandler(
        out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy,
        out RecordingDispatchProxy<IMapManager> mapManagerProxy)
    {
        ICleanupManager cleanupManager = RecordingDispatchProxy<ICleanupManager>.Create(out RecordingDispatchProxy<ICleanupManager> cleanupProxy);
        cleanupProxy.SetMethodReturn(nameof(ICleanupManager.IsAccountLocked), false);

        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out entityFactoryProxy);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        IMapManager mapManager = RecordingDispatchProxy<IMapManager>.Create(out mapManagerProxy);

        return new CharacterSelectHandler(cleanupManager, entityFactory, gameTableManager, mapManager);
    }

    private static IWorldSession CreateSession(ulong playerCharacterId, out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), playerCharacterId);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), 42u);

        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetProperty(nameof(IWorldSession.Characters), new List<CharacterModel>());
        sessionProxy.SetProperty(nameof(IWorldSession.Events), new EventQueue());
        sessionProxy.SetProperty(nameof(IWorldSession.Heartbeat), new SocketHeartbeat());
        return session;
    }

    private static ClientCharacterSelect CreateCharacterSelect(ulong characterId)
    {
        var select = (ClientCharacterSelect)RuntimeHelpers.GetUninitializedObject(typeof(ClientCharacterSelect));
        typeof(ClientCharacterSelect)
            .GetProperty(nameof(ClientCharacterSelect.CharacterId))!
            .SetValue(select, characterId);
        return select;
    }
}
