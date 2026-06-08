using System.Runtime.CompilerServices;
using System.Reflection;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
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

    [Fact]
    public void HandleMessage_SelectAfterLogoutFinished_ClearsOldPlayerAndEntersWorld()
    {
        CharacterSelectHandler handler = CreateHandler(
            out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy,
            out RecordingDispatchProxy<IMapManager> mapManagerProxy,
            CreateGameTable(new WorldEntry { Id = 10u }));

        IWorldSession session = CreateSession(
            123ul,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            LogoutState.Finished,
            [
                new CharacterModel
                {
                    Id      = 123ul,
                    WorldId = 10
                }
            ]);

        IPlayer newPlayer = RecordingDispatchProxy<IPlayer>.Create(out _);
        entityFactoryProxy.SetMethodReturn(nameof(IEntityFactory.CreateEntity), newPlayer);

        handler.HandleMessage(session, CreateCharacterSelect(123ul));

        Assert.Same(newPlayer, session.Player);
        Assert.Single(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateEntity)));

        RecordingDispatchProxy<IMapManager>.Invocation addToMap = Assert.Single(
            mapManagerProxy.GetInvocations(nameof(IMapManager.AddToMap)));
        Assert.Same(newPlayer, addToMap.Arguments[0]);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    private static CharacterSelectHandler CreateHandler(
        out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy,
        out RecordingDispatchProxy<IMapManager> mapManagerProxy,
        GameTable<WorldEntry> worldTable = null)
    {
        ICleanupManager cleanupManager = RecordingDispatchProxy<ICleanupManager>.Create(out RecordingDispatchProxy<ICleanupManager> cleanupProxy);
        cleanupProxy.SetMethodReturn(nameof(ICleanupManager.IsAccountLocked), false);

        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out entityFactoryProxy);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        if (worldTable != null)
            gameTableProxy.SetProperty(nameof(IGameTableManager.World), worldTable);

        IMapManager mapManager = RecordingDispatchProxy<IMapManager>.Create(out mapManagerProxy);

        return new CharacterSelectHandler(cleanupManager, entityFactory, gameTableManager, mapManager);
    }

    private static IWorldSession CreateSession(
        ulong playerCharacterId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        LogoutState logoutState = LogoutState.None,
        List<CharacterModel> characters = null)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        ILogoutManager logoutManager = RecordingDispatchProxy<ILogoutManager>.Create(out RecordingDispatchProxy<ILogoutManager> logoutManagerProxy);
        logoutManagerProxy.SetProperty(nameof(ILogoutManager.State), logoutState);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), playerCharacterId);
        playerProxy.SetProperty(nameof(IPlayer.LogoutManager), logoutManager);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), 42u);

        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetProperty(nameof(IWorldSession.Characters), characters ?? []);
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

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }
}
