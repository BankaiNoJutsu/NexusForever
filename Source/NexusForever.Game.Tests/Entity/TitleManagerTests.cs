using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class TitleManagerTests
{
    [Fact]
    public void Constructor_WithKnownCharacterTitle_LoadsPersistedTitleAndKeepsActiveTitle()
    {
        GameTableManager gameTableManager = CreateGameTableManagerWithCharacterTitle(new CharacterTitleEntry
        {
            Id = 21u
        });

        var manager = new TitleManager(CreatePlayer(), CreatePersistedModel(activeTitleId: 21), gameTableManager);

        Assert.True(manager.HasTitle(21));
        Assert.Equal((ushort)21, manager.ActiveTitleId);
        Assert.Single(manager);
    }

    [Fact]
    public void Constructor_WithMissingCharacterTitleTable_SkipsPersistedTitleAndClearsActiveTitle()
    {
        GameTableManager gameTableManager = CreateGameTableManagerWithoutCharacterTitle();

        var manager = new TitleManager(CreatePlayer(), CreatePersistedModel(activeTitleId: 21), gameTableManager);

        Assert.False(manager.HasTitle(21));
        Assert.Equal((ushort)0, manager.ActiveTitleId);
        Assert.Empty(manager);
    }

    [Fact]
    public void AddTitle_WithMissingCharacterTitleTable_ThrowsBeforeAchievementOrPacket()
    {
        GameTableManager gameTableManager = CreateGameTableManagerWithoutCharacterTitle();

        IPlayer player = CreatePlayer(
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy);
        var manager = new TitleManager(player, new CharacterModel { Id = 42ul }, gameTableManager);

        Assert.Throws<InvalidPacketValueException>(() => manager.AddTitle(21));

        Assert.False(manager.HasTitle(21));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        Assert.Empty(GetMessages<ServerTitleUpdate>(sessionProxy));
    }

    [Fact]
    public void AddTitle_WithKnownCharacterTitle_AddsTitleAndSendsUpdate()
    {
        GameTableManager gameTableManager = CreateGameTableManagerWithCharacterTitle(new CharacterTitleEntry
        {
            Id = 21u
        });

        IPlayer player = CreatePlayer(
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy);
        var manager = new TitleManager(player, new CharacterModel { Id = 42ul }, gameTableManager);

        manager.AddTitle(21);

        Assert.True(manager.HasTitle(21));
        Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        ServerTitleUpdate update = Assert.Single(GetMessages<ServerTitleUpdate>(sessionProxy));
        Assert.Equal((ushort)21, update.TitleId);
        Assert.False(update.Alreadyowned);
        Assert.False(update.Revoked);
    }

    [Fact]
    public void RevokeTitle_WithMissingCharacterTitleTable_ThrowsBeforeRevocationOrPacket()
    {
        GameTableManager gameTableManager = CreateGameTableManagerWithCharacterTitle(new CharacterTitleEntry
        {
            Id = 21u
        });

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new TitleManager(player, CreatePersistedModel(activeTitleId: 21), gameTableManager);

        SetPrivateField(manager, "gameTableManager", CreateGameTableManagerWithoutCharacterTitle());

        Assert.Throws<InvalidPacketValueException>(() => manager.RevokeTitle(21));

        Assert.True(manager.HasTitle(21));
        Assert.Equal((ushort)21, manager.ActiveTitleId);
        Assert.Empty(GetMessages<ServerTitleUpdate>(sessionProxy));
    }

    [Fact]
    public void AddAllTitles_WithMissingCharacterTitleTable_SendsCurrentEmptyTitleList()
    {
        GameTableManager gameTableManager = CreateGameTableManagerWithoutCharacterTitle();

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new TitleManager(player, new CharacterModel { Id = 42ul }, gameTableManager);

        manager.AddAllTitles();

        ServerTitles titles = Assert.Single(GetMessages<ServerTitles>(sessionProxy));
        Assert.Empty(titles.Titles);
    }

    private static IPlayer CreatePlayer()
    {
        return CreatePlayer(out _, out _);
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return CreatePlayer(out sessionProxy, out _);
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        ICharacterAchievementManager achievementManager =
            RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 9001u);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        return player;
    }

    private static CharacterModel CreatePersistedModel(ushort activeTitleId)
    {
        var model = new CharacterModel
        {
            Id    = 42ul,
            Title = activeTitleId
        };
        model.CharacterTitle.Add(new CharacterTitleModel
        {
            Id    = 42ul,
            Title = 21
        });
        return model;
    }

    private static GameTableManager CreateGameTableManagerWithoutCharacterTitle()
    {
        return (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
    }

    private static GameTableManager CreateGameTableManagerWithCharacterTitle(params CharacterTitleEntry[] characterTitleEntries)
    {
        var manager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        SetAutoProperty(manager, nameof(GameTableManager.CharacterTitle), CreateGameTable(characterTitleEntries));
        return manager;
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

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        var lookup = new int[(int)(entries.Max(GetEntryId) + 1u)];
        Array.Fill(lookup, -1);
        for (int i = 0; i < entries.Count; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public);
        return (uint)idField.GetValue(entry);
    }
}
