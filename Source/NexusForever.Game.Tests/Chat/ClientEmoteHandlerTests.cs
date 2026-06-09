using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Chat;

namespace NexusForever.Game.Tests.Chat;

public class ClientEmoteHandlerTests
{
    [Fact]
    public void HandleMessage_WithMissingEmoteTableThrowsInvalidWithoutBroadcast()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientEmoteHandler(CreateGameTableManager());

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateEmote(10u)));

        Assert.Empty(playerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)));
    }

    [Fact]
    public void HandleMessage_WithKnownEmoteBroadcastsStandState()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientEmoteHandler(CreateGameTableManager([new EmotesEntry
        {
            Id         = 10u,
            StandState = StandState.Sit
        }]));

        handler.HandleMessage(session, CreateEmote(10u));

        RecordingDispatchProxy<IPlayer>.Invocation invocation =
            Assert.Single(playerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)));
        ServerEmote response = Assert.IsType<ServerEmote>(invocation.Arguments[0]);
        Assert.Equal(1234u, response.Guid);
        Assert.Equal(StandState.Sit, response.StandState);
        Assert.Equal(10u, response.EmoteId);
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 1234u);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IGameTableManager CreateGameTableManager(IReadOnlyCollection<EmotesEntry> emotes = null)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);

        if (emotes != null)
            proxy.SetProperty(nameof(IGameTableManager.Emotes), CreateGameTable(emotes));

        return gameTableManager;
    }

    private static ClientEmote CreateEmote(uint emoteId)
    {
        var emote = (ClientEmote)RuntimeHelpers.GetUninitializedObject(typeof(ClientEmote));
        typeof(ClientEmote)
            .GetProperty(nameof(ClientEmote.EmoteId), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(emote, emoteId);
        return emote;
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
