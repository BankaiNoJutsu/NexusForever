using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using Microting.EntityFrameworkCore.MySql.Infrastructure;

namespace NexusForever.Game.Tests.Spell;

public class ActionSetAmpTests
{
    [Fact]
    public void Save_WithHighClientAmpId_PersistsFullUShortId()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        using var context = new CharacterContext(options);
        var actionSet = (ActionSet)RuntimeHelpers.GetUninitializedObject(typeof(ActionSet));
        var amp = new ActionSetAmp(
            actionSet,
            new EldanAugmentationEntry
            {
                Id = 976
            },
            true);

        amp.Save(context);

        CharacterActionSetAmpModel model = Assert.Single(context.ChangeTracker.Entries<CharacterActionSetAmpModel>()).Entity;
        Assert.Equal((ushort)976, model.AmpId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddAmp_WithMissingEldanAugmentationStaticDataThrowsInvalidAmp(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(includeEmptyTable ? CreateGameTable<EldanAugmentationEntry>() : null);
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy, gameTableManager);

        Assert.Throws<ArgumentException>(() => actionSet.AddAmp(42));

        Assert.Empty(actionSet.Amps);
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddAmpFromExistingModel_WithMissingEldanAugmentationStaticDataThrowsInvalidAmp(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(includeEmptyTable ? CreateGameTable<EldanAugmentationEntry>() : null);
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy, gameTableManager);

        Assert.Throws<ArgumentException>(() => actionSet.AddAmp(new CharacterActionSetAmpModel
        {
            Id        = 1ul,
            SpecIndex = 0,
            AmpId     = 42
        }));

        Assert.Empty(actionSet.Amps);
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void AddAmp_WithKnownStaticDataAddsAmpAndRequestsSave()
    {
        GameTableManager gameTableManager = CreateGameTableManager(CreateGameTable(new EldanAugmentationEntry
        {
            Id        = 42u,
            PowerCost = 3u
        }));
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy, gameTableManager);

        actionSet.AddAmp(42);

        Assert.NotNull(actionSet.GetAmp(42));
        Assert.Equal((byte)42, actionSet.AmpPoints);
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void AddAmpFromExistingModel_WithKnownStaticDataAddsAmpWithoutRequestingSave()
    {
        GameTableManager gameTableManager = CreateGameTableManager(CreateGameTable(new EldanAugmentationEntry
        {
            Id        = 42u,
            PowerCost = 3u
        }));
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy, gameTableManager);

        actionSet.AddAmp(new CharacterActionSetAmpModel
        {
            Id        = 1ul,
            SpecIndex = 0,
            AmpId     = 42
        });

        Assert.NotNull(actionSet.GetAmp(42));
        Assert.Equal((byte)42, actionSet.AmpPoints);
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    private static ActionSet CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy, IGameTableManager gameTableManager)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        return new ActionSet(0, player, gameTableManager);
    }

    private static GameTableManager CreateGameTableManager(GameTable<EldanAugmentationEntry> eldanAugmentationTable)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (eldanAugmentationTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.EldanAugmentation), eldanAugmentationTable);

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
