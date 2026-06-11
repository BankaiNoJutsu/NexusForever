using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Chat;
using NexusForever.Game.Static.Chat;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Chat.Model;

namespace NexusForever.Game.Tests.Chat;

public class ChatMessageBuilderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AppendItem_WithMissingStaticDataThrowsBeforeAppending(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(itemTable: includeEmptyTable ? CreateGameTable<Item2Entry>() : null);
        var builder = new ChatMessageBuilder(gameTableManager);
        builder.AppendText("before");

        Assert.Throws<ArgumentException>(() => builder.AppendItem(123u));

        Assert.Equal("before", builder.Text);
        Assert.Empty(builder.Formats);
    }

    [Fact]
    public void AppendItem_WithKnownItemAppendsItemFormat()
    {
        GameTableManager gameTableManager = CreateGameTableManager(itemTable: CreateGameTable(new Item2Entry
        {
            Id = 123u
        }));
        var builder = new ChatMessageBuilder(gameTableManager);
        builder.AppendText("before");

        builder.AppendItem(123u);

        Assert.Equal("before[I]", builder.Text);
        Assert.Single(builder.Formats);
        Assert.Equal(ChatFormatType.ItemId, builder.Formats[0].Type);
        ChatFormatItemId model = Assert.IsType<ChatFormatItemId>(builder.Formats[0].Model);
        Assert.Equal(123u, model.Item2Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AppendQuest_WithMissingStaticDataThrowsBeforeAppending(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(questTable: includeEmptyTable ? CreateGameTable<Quest2Entry>() : null);
        var builder = new ChatMessageBuilder(gameTableManager);
        builder.AppendText("before");

        Assert.Throws<ArgumentException>(() => builder.AppendQuest(456));

        Assert.Equal("before", builder.Text);
        Assert.Empty(builder.Formats);
    }

    [Fact]
    public void AppendQuest_WithKnownQuestAppendsQuestFormat()
    {
        GameTableManager gameTableManager = CreateGameTableManager(questTable: CreateGameTable(new Quest2Entry
        {
            Id = 456u
        }));
        var builder = new ChatMessageBuilder(gameTableManager);
        builder.AppendText("before");

        builder.AppendQuest(456);

        Assert.Equal("before[Q]", builder.Text);
        Assert.Single(builder.Formats);
        Assert.Equal(ChatFormatType.QuestId, builder.Formats[0].Type);
        ChatFormatQuestId model = Assert.IsType<ChatFormatQuestId>(builder.Formats[0].Model);
        Assert.Equal(456u, model.Quest2Id);
    }

    private static GameTableManager CreateGameTableManager(GameTable<Item2Entry> itemTable = null, GameTable<Quest2Entry> questTable = null)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (itemTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), itemTable);
        if (questTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Quest2), questTable);

        return gameTableManager;
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

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public);
        return (uint)idField.GetValue(entry);
    }
}
