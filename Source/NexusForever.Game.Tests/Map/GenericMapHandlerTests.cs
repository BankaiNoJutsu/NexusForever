using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Map;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Map;

namespace NexusForever.Game.Tests.Map;

public class GenericMapHandlerTests
{
    [Fact]
    public void NodeRequest_WithMissingGenericMapNodeTableDoesNotEmit()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out _);
        ClientGenericMapNodeRequestHandler handler = CreateRequestHandler(CreateGameTableManager());

        handler.HandleMessage(session, CreateNodeRequest(42u));

        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void NodeRequest_WithKnownNodeSendsEnabledNodeState()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out _);
        ClientGenericMapNodeRequestHandler handler = CreateRequestHandler(CreateGameTableManager(
            genericMapNodes: [new GenericMapNodeEntry { Id = 42u }]));

        handler.HandleMessage(session, CreateNodeRequest(42u));

        RecordingDispatchProxy<IWorldSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        ServerGenericMapNode response = Assert.IsType<ServerGenericMapNode>(invocation.Arguments[0]);
        Assert.Equal(42u, response.Node.GenericMapModeId);
        Assert.True(response.Node.Enabled);
    }

    [Fact]
    public void NodeChosen_WithMissingGenericMapNodeTableDoesNotEmitOrTeleport()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        ClientGenericMapNodeChosenHandler handler = CreateChosenHandler(CreateGameTableManager());

        handler.HandleMessage(session, CreateNodeChosen(42u));

        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void NodeChosen_WithNoDestinationAcknowledgesEnabledNodeState()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        ClientGenericMapNodeChosenHandler handler = CreateChosenHandler(CreateGameTableManager(
            genericMapNodes: [new GenericMapNodeEntry { Id = 42u, WorldLocation2Id = 0u }]));

        handler.HandleMessage(session, CreateNodeChosen(42u));

        RecordingDispatchProxy<IWorldSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        ServerGenericMapNode response = Assert.IsType<ServerGenericMapNode>(invocation.Arguments[0]);
        Assert.Equal(42u, response.Node.GenericMapModeId);
        Assert.True(response.Node.Enabled);
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void NodeChosen_WithMissingWorldLocationTableDoesNotEmitOrTeleport()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        ClientGenericMapNodeChosenHandler handler = CreateChosenHandler(CreateGameTableManager(
            genericMapNodes: [new GenericMapNodeEntry { Id = 42u, WorldLocation2Id = 77u }]));

        handler.HandleMessage(session, CreateNodeChosen(42u));

        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void NodeChosen_WithKnownDestinationTeleportsPlayer()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        ClientGenericMapNodeChosenHandler handler = CreateChosenHandler(CreateGameTableManager(
            genericMapNodes: [new GenericMapNodeEntry { Id = 42u, WorldLocation2Id = 77u }],
            worldLocations: [new WorldLocation2Entry
            {
                Id        = 77u,
                WorldId   = 9001u,
                Position0 = 1.5f,
                Position1 = 2.5f,
                Position2 = 3.5f
            }]));

        handler.HandleMessage(session, CreateNodeChosen(42u));

        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        RecordingDispatchProxy<IPlayer>.Invocation invocation =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Equal((ushort)9001, invocation.Arguments[0]);
        Assert.Equal(1.5f, invocation.Arguments[1]);
        Assert.Equal(2.5f, invocation.Arguments[2]);
        Assert.Equal(3.5f, invocation.Arguments[3]);
    }

    private static ClientGenericMapNodeRequestHandler CreateRequestHandler(IGameTableManager gameTableManager)
    {
        return new ClientGenericMapNodeRequestHandler(
            NullLogger<ClientGenericMapNodeRequestHandler>.Instance,
            gameTableManager);
    }

    private static ClientGenericMapNodeChosenHandler CreateChosenHandler(IGameTableManager gameTableManager)
    {
        return new ClientGenericMapNodeChosenHandler(
            NullLogger<ClientGenericMapNodeChosenHandler>.Instance,
            gameTableManager);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 1234u);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IGameTableManager CreateGameTableManager(
        IReadOnlyCollection<GenericMapNodeEntry> genericMapNodes = null,
        IReadOnlyCollection<WorldLocation2Entry> worldLocations = null)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);

        if (genericMapNodes != null)
            proxy.SetProperty(nameof(IGameTableManager.GenericMapNode), CreateGameTable(genericMapNodes));

        if (worldLocations != null)
            proxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(worldLocations));

        return gameTableManager;
    }

    private static ClientGenericMapNodeRequest CreateNodeRequest(uint genericMapNodeId)
    {
        var request = (ClientGenericMapNodeRequest)RuntimeHelpers.GetUninitializedObject(typeof(ClientGenericMapNodeRequest));
        SetNodeId(request, genericMapNodeId);
        return request;
    }

    private static ClientGenericMapNodeChosen CreateNodeChosen(uint genericMapNodeId)
    {
        var chosen = (ClientGenericMapNodeChosen)RuntimeHelpers.GetUninitializedObject(typeof(ClientGenericMapNodeChosen));
        SetNodeId(chosen, genericMapNodeId);
        return chosen;
    }

    private static void SetNodeId(object message, uint genericMapNodeId)
    {
        message.GetType()
            .GetProperty("GenericMapNodeId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(message, (ushort)genericMapNodeId);
    }

    private static GameTable<T> CreateGameTable<T>(IReadOnlyCollection<T> entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));

        typeof(GameTable<T>)
            .GetProperty(nameof(GameTable<T>.Entries), BindingFlags.Instance | BindingFlags.Public)
            ?.SetValue(table, entries.ToArray());

        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        ulong maxId = entries
            .Select(entry => Convert.ToUInt64(idField.GetValue(entry)))
            .DefaultIfEmpty(0ul)
            .Max() + 1ul;

        var lookup = Enumerable.Repeat(-1, (int)maxId).ToArray();
        int index = 0;
        foreach (T entry in entries)
        {
            ulong id = Convert.ToUInt64(idField.GetValue(entry));
            lookup[id] = index++;
        }

        typeof(GameTable<T>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        typeof(GameTable<T>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = maxId });

        return table;
    }
}
