using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Spell;

namespace NexusForever.Game.Tests.Spell;

public class ClientSetStanceHandlerTests
{
    [Fact]
    public void HandleMessage_WithMissingClassTableThrowsInvalidWithoutMutating()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientSetStanceHandler(CreateGameTableManager());

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateSetStance(1)));

        Assert.Empty(playerProxy.GetInvocations($"set_{nameof(IPlayer.InnateIndex)}"));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void HandleMessage_WithKnownClassAndInnateSendsStanceChanged()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientSetStanceHandler(CreateGameTableManager([new ClassEntry
        {
            Id = (uint)Class.Warrior,
            Spell4IdInnateAbilityActive = [111u, 222u, 0u]
        }]));

        handler.HandleMessage(session, CreateSetStance(1));

        RecordingDispatchProxy<IPlayer>.Invocation setInnate =
            Assert.Single(playerProxy.GetInvocations($"set_{nameof(IPlayer.InnateIndex)}"));
        Assert.Equal((byte)1, setInnate.Arguments[0]);

        RecordingDispatchProxy<IWorldSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        ServerStanceChanged response = Assert.IsType<ServerStanceChanged>(invocation.Arguments[0]);
        Assert.Equal((byte)1, response.InnateIndex);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Class), Class.Warrior);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IGameTableManager CreateGameTableManager(IReadOnlyCollection<ClassEntry> classEntries = null)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);

        if (classEntries != null)
            proxy.SetProperty(nameof(IGameTableManager.Class), CreateGameTable(classEntries));

        return gameTableManager;
    }

    private static ClientSetStance CreateSetStance(byte innateIndex)
    {
        var request = (ClientSetStance)RuntimeHelpers.GetUninitializedObject(typeof(ClientSetStance));
        typeof(ClientSetStance)
            .GetProperty(nameof(ClientSetStance.InnateIndex), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(request, innateIndex);
        return request;
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
