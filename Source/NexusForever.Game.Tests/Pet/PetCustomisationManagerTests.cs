using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pet;

namespace NexusForever.Game.Tests.Pet;

public class PetCustomisationManagerTests
{
    [Fact]
    public void Constructor_WithKnownPetFlair_LoadsPersistedFlairAndCustomisation()
    {
        var manager = new PetCustomisationManager(
            CreatePlayer(),
            CreatePersistedModel(),
            CreateGameTableManagerWithPetFlair(new PetFlairEntry
            {
                Id             = 12u,
                UnlockBitIndex = [5u, 0u]
            }),
            CreateTextFilterManager());

        Assert.True(manager.HasFlair(12));
        IPetCustomisation customisation = manager.GetCustomisation(PetType.ScanBot, 77u);
        Assert.NotNull(customisation);
        Assert.Equal((ushort)12, customisation.Build().SlotFlairIds[0]);
    }

    [Fact]
    public void Constructor_WithMissingPetFlairTable_SkipsPersistedFlairAndZerosCustomisationSlot()
    {
        var manager = new PetCustomisationManager(
            CreatePlayer(),
            CreatePersistedModel(),
            CreateGameTableManagerWithoutPetFlair(),
            CreateTextFilterManager());

        Assert.False(manager.HasFlair(12));
        IPetCustomisation customisation = manager.GetCustomisation(PetType.ScanBot, 77u);
        Assert.NotNull(customisation);
        Assert.Equal((ushort)0, customisation.Build().SlotFlairIds[0]);
    }

    [Fact]
    public void AddCustomisation_WithZeroFlairAndMissingPetFlairTable_ClearsSlotWithoutStaticData()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new PetCustomisationManager(
            CreatePlayer(session),
            new CharacterModel { Id = 42ul },
            CreateGameTableManagerWithoutPetFlair(),
            CreateTextFilterManager());

        manager.AddCustomisation(PetType.ScanBot, 77u, 2, 0);

        IPetCustomisation customisation = manager.GetCustomisation(PetType.ScanBot, 77u);
        Assert.NotNull(customisation);
        Assert.Equal((ushort)0, customisation.Build().SlotFlairIds[2]);

        ServerPetCustomisation message = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<ServerPetCustomisation>());
        Assert.Equal((ushort)0, message.PetCustomisation.SlotFlairIds[2]);
    }

    private static IPlayer CreatePlayer(IGameSession session = null)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.IsLoading), false);
        return player;
    }

    private static CharacterModel CreatePersistedModel()
    {
        var model = new CharacterModel { Id = 42ul };
        model.PetFlair.Add(new CharacterPetFlairModel
        {
            Id         = 42ul,
            PetFlairId = 12u
        });
        model.PetCustomisation.Add(new CharacterPetCustomisationModel
        {
            Id          = 42ul,
            Type        = (byte)PetType.ScanBot,
            ObjectId    = 77u,
            Name        = "Scanbot",
            FlairIdMask = 12ul
        });
        return model;
    }

    private static GameTableManager CreateGameTableManagerWithoutPetFlair()
    {
        return (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
    }

    private static GameTableManager CreateGameTableManagerWithPetFlair(params PetFlairEntry[] petFlairEntries)
    {
        var manager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        SetAutoProperty(manager, nameof(GameTableManager.PetFlair), CreateGameTable(petFlairEntries));
        return manager;
    }

    private static ITextFilterManager CreateTextFilterManager()
    {
        ITextFilterManager textFilterManager = RecordingDispatchProxy<ITextFilterManager>.Create(out RecordingDispatchProxy<ITextFilterManager> proxy);
        proxy.SetMethodHandler(nameof(ITextFilterManager.IsTextValid), _ => true);
        return textFilterManager;
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
