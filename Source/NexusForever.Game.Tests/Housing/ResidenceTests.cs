using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Housing;

public class ResidenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VisualSetters_WithMissingStaticTablesRejectWithoutMutation(bool includeEmptyTables)
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            includeEmptyTables ? CreateGameTable<HousingWallpaperInfoEntry>() : null,
            includeEmptyTables ? CreateGameTable<HousingDecorInfoEntry>() : null);
        var residence = new Residence(new ResidenceModel
        {
            Id             = 100ul,
            OwnerId        = 200ul,
            Name           = "Test Residence",
            PropertyInfoId = (byte)PropertyInfoId.Residence
        }, gameTableManager: gameTableManager);

        Assert.Throws<ArgumentOutOfRangeException>(() => residence.Wallpaper = 10);
        Assert.Throws<ArgumentOutOfRangeException>(() => residence.Roof = 20);
        Assert.Throws<ArgumentOutOfRangeException>(() => residence.Entryway = 21);
        Assert.Throws<ArgumentOutOfRangeException>(() => residence.Door = 22);
        Assert.Throws<ArgumentOutOfRangeException>(() => residence.Music = 11);
        Assert.Throws<ArgumentOutOfRangeException>(() => residence.Ground = 12);
        Assert.Throws<ArgumentOutOfRangeException>(() => residence.Sky = 13);

        Assert.Equal((ushort)0, residence.Wallpaper);
        Assert.Equal((ushort)0, residence.Roof);
        Assert.Equal((ushort)0, residence.Entryway);
        Assert.Equal((ushort)0, residence.Door);
        Assert.Equal((ushort)0, residence.Music);
        Assert.Equal((ushort)0, residence.Ground);
        Assert.Equal((ushort)0, residence.Sky);
        Assert.False(residence.NeedsSave);
    }

    [Fact]
    public void VisualSetters_WithKnownStaticDataAcceptAndMarkDirty()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            CreateGameTable(
                new HousingWallpaperInfoEntry
                {
                    Id = 10u
                },
                new HousingWallpaperInfoEntry
                {
                    Id    = 11u,
                    Flags = 0x100u
                },
                new HousingWallpaperInfoEntry
                {
                    Id    = 12u,
                    Flags = 0x200u
                },
                new HousingWallpaperInfoEntry
                {
                    Id    = 13u,
                    Flags = 0x40u
                }),
            CreateGameTable(
                new HousingDecorInfoEntry
                {
                    Id = 20u
                },
                new HousingDecorInfoEntry
                {
                    Id = 21u
                },
                new HousingDecorInfoEntry
                {
                    Id = 22u
                }));
        var residence = new Residence(new ResidenceModel
        {
            Id             = 100ul,
            OwnerId        = 200ul,
            Name           = "Test Residence",
            PropertyInfoId = (byte)PropertyInfoId.Residence
        }, gameTableManager: gameTableManager);

        residence.Wallpaper = 10;
        residence.Roof = 20;
        residence.Entryway = 21;
        residence.Door = 22;
        residence.Music = 11;
        residence.Ground = 12;
        residence.Sky = 13;

        Assert.Equal((ushort)10, residence.Wallpaper);
        Assert.Equal((ushort)20, residence.Roof);
        Assert.Equal((ushort)21, residence.Entryway);
        Assert.Equal((ushort)22, residence.Door);
        Assert.Equal((ushort)11, residence.Music);
        Assert.Equal((ushort)12, residence.Ground);
        Assert.Equal((ushort)13, residence.Sky);
        Assert.True(residence.NeedsSave);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_WithMissingWallpaperStaticDataForInteriorWallpaperThrowsDatabaseDataException(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            includeEmptyTable ? CreateGameTable<HousingWallpaperInfoEntry>() : null,
            null);

        Assert.Throws<DatabaseDataException>(() => new Residence(new ResidenceModel
        {
            Id             = 100ul,
            OwnerId        = 200ul,
            Name           = "Test Residence",
            PropertyInfoId = (byte)PropertyInfoId.Residence,
            Decor =
            [
                new ResidenceDecor
                {
                    Id          = 100ul,
                    DecorId     = 200ul,
                    DecorInfoId = 300u,
                    DecorType   = (uint)DecorType.InteriorWallpaper
                }
            ]
        }, gameTableManager: gameTableManager));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_WithMissingDecorStaticDataThrowsDatabaseDataException(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            null,
            includeEmptyTable ? CreateGameTable<HousingDecorInfoEntry>() : null);

        Assert.Throws<DatabaseDataException>(() => new Residence(new ResidenceModel
        {
            Id             = 100ul,
            OwnerId        = 200ul,
            Name           = "Test Residence",
            PropertyInfoId = (byte)PropertyInfoId.Residence,
            Decor =
            [
                new ResidenceDecor
                {
                    Id          = 100ul,
                    DecorId     = 200ul,
                    DecorInfoId = 300u,
                    DecorType   = (uint)DecorType.Crate
                }
            ]
        }, gameTableManager: gameTableManager));
    }

    [Fact]
    public void Build_ForPersonalResidence_UsesZeroNeighbourhoodId()
    {
        ServerHousingProperties.Residence packet = new Residence(new ResidenceModel
        {
            Id = 100ul,
            OwnerId = 200ul,
            Name = "Personal Residence",
            PropertyInfoId = (byte)PropertyInfoId.Residence
        }, realmId: 7).Build();

        Assert.Equal(0ul, packet.NeighbourhoodId);
    }

    [Fact]
    public void Build_ForCommunityResidence_UsesGuildOwnerAsNeighbourhoodId()
    {
        ServerHousingProperties.Residence packet = new Residence(new ResidenceModel
        {
            Id = 100ul,
            GuildOwnerId = 0x1122334455667788ul,
            Name = "Community Residence",
            PropertyInfoId = (byte)PropertyInfoId.Community
        }, realmId: 7).Build();

        Assert.Equal(0x1122334455667788ul, packet.NeighbourhoodId);
        Assert.Equal(0x1122334455667788ul, packet.GuildIdOwner);
    }

    [Fact]
    public void Build_ForCommunityChildResidence_UsesGuildOwnerAsNeighbourhoodId()
    {
        ServerHousingProperties.Residence packet = new Residence(new ResidenceModel
        {
            Id = 100ul,
            OwnerId = 200ul,
            GuildOwnerId = 0x1122334455667788ul,
            Name = "Child Residence",
            PropertyInfoId = (byte)PropertyInfoId.Residence
        }, realmId: 7).Build();

        Assert.Equal(0x1122334455667788ul, packet.NeighbourhoodId);
        Assert.Equal(0ul, packet.GuildIdOwner.GetValueOrDefault(0ul));
    }

    [Fact]
    public void Save_RemoveThenReaddPersistedNeighborBeforeSave_DoesNotDeleteNeighbor()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        var residence = new Residence(new ResidenceModel
        {
            Id = 100ul,
            OwnerId = 200ul,
            Name = "Test Residence",
            Neighbors = new List<ResidenceNeighborModel>
            {
                new()
                {
                    ResidenceId = 100ul,
                    NeighborCharacterId = 300ul,
                    PermissionLevel = 1
                }
            }
        });

        Assert.True(residence.RemoveNeighbor(300ul));
        Assert.True(residence.AddNeighbor(300ul, 1));

        using var context = new CharacterContext(options);
        residence.Save(context);

        Assert.Empty(context.ChangeTracker.Entries<ResidenceNeighborModel>());
        Assert.Single(residence.GetNeighbors());
    }

    [Fact]
    public void Decor_BuildPreservesNativeDecorStateFields()
    {
        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out RecordingDispatchProxy<IResidence> residenceProxy);
        residenceProxy.SetProperty(nameof(IResidence.Id), 100ul);
        residenceProxy.SetProperty(nameof(IResidence.RealmId), (ushort)7);

        var decor = new Decor(residence, new ResidenceDecor
        {
            Id = 100ul,
            DecorId = 200ul,
            DecorInfoId = 300u,
            DecorType = (uint)DecorType.InteriorWallpaper,
            DecorData = 400u,
            HookBagIndex = 500u,
            HookIndex = 6u,
            PlotIndex = 700u,
            Scale = 1.5f,
            ActivePropUnitId = 800u,
            DecorParentId = 900ul,
            ColourShiftId = 1000
        }, null);

        ServerHousingResidenceDecor.Decor packet = decor.Build();

        Assert.Equal(7u, packet.RealmId);
        Assert.Null(decor.Entry);
        Assert.Equal(300u, packet.DecorInfoId);
        Assert.Equal(DecorType.InteriorWallpaper, packet.DecorType);
        Assert.Equal(400u, packet.DecorData);
        Assert.Equal(500u, packet.HookBagIndex);
        Assert.Equal(6u, packet.HookIndex);
        Assert.Equal(700u, packet.PlotIndex);
        Assert.Equal(800u, packet.ActivePropUnitId);
        Assert.Equal(900ul, packet.ParentDecorId);
        Assert.Equal(1000u, packet.ColourShift);
    }

    [Fact]
    public void Decor_SaveMarksNativeDecorStateFields()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out RecordingDispatchProxy<IResidence> residenceProxy);
        residenceProxy.SetProperty(nameof(IResidence.Id), 100ul);

        var decor = new Decor(residence, new ResidenceDecor
        {
            Id = 100ul,
            DecorId = 200ul,
            DecorInfoId = 300u,
            DecorType = (uint)DecorType.Crate
        }, new HousingDecorInfoEntry
        {
            Id = 300u
        });

        decor.UpdateEntry(new HousingDecorInfoEntry { Id = 301u });
        decor.DecorData = 401u;
        decor.HookBagIndex = 501u;
        decor.HookIndex = 2u;
        decor.ActivePropUnitId = 801u;

        using var context = new CharacterContext(options);
        decor.Save(context);

        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ResidenceDecor> entry =
            Assert.Single(context.ChangeTracker.Entries<ResidenceDecor>());
        Assert.Equal(301u, entry.Entity.DecorInfoId);
        Assert.Equal(401u, entry.Entity.DecorData);
        Assert.Equal(501u, entry.Entity.HookBagIndex);
        Assert.Equal(2u, entry.Entity.HookIndex);
        Assert.Equal(801u, entry.Entity.ActivePropUnitId);
        Assert.True(entry.Property(p => p.DecorInfoId).IsModified);
        Assert.True(entry.Property(p => p.DecorData).IsModified);
        Assert.True(entry.Property(p => p.HookBagIndex).IsModified);
        Assert.True(entry.Property(p => p.HookIndex).IsModified);
        Assert.True(entry.Property(p => p.ActivePropUnitId).IsModified);
    }

    [Fact]
    public void Decor_SaveCreatePersistsRawWallpaperDecorInfoId()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out RecordingDispatchProxy<IResidence> residenceProxy);
        residenceProxy.SetProperty(nameof(IResidence.Id), 100ul);

        var decor = new Decor(residence, 200ul, 300u, DecorType.InteriorWallpaper);
        decor.HookIndex = 6u;

        using var context = new CharacterContext(options);
        decor.Save(context);

        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ResidenceDecor> entry =
            Assert.Single(context.ChangeTracker.Entries<ResidenceDecor>());
        Assert.Equal(EntityState.Added, entry.State);
        Assert.Equal(300u, entry.Entity.DecorInfoId);
        Assert.Equal((uint)DecorType.InteriorWallpaper, entry.Entity.DecorType);
        Assert.Equal(6u, entry.Entity.HookIndex);
    }

    [Fact]
    public void Decor_SaveMarksRawWallpaperDecorInfoId()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out RecordingDispatchProxy<IResidence> residenceProxy);
        residenceProxy.SetProperty(nameof(IResidence.Id), 100ul);

        var decor = new Decor(residence, new ResidenceDecor
        {
            Id = 100ul,
            DecorId = 200ul,
            DecorInfoId = 300u,
            DecorType = (uint)DecorType.InteriorWallpaper
        }, null);

        decor.UpdateDecorInfoId(301u);

        using var context = new CharacterContext(options);
        decor.Save(context);

        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ResidenceDecor> entry =
            Assert.Single(context.ChangeTracker.Entries<ResidenceDecor>());
        Assert.Equal(301u, entry.Entity.DecorInfoId);
        Assert.True(entry.Property(p => p.DecorInfoId).IsModified);
        Assert.Null(decor.Entry);
    }

    private static GameTableManager CreateGameTableManager(
        GameTable<HousingWallpaperInfoEntry> housingWallpaperInfoTable,
        GameTable<HousingDecorInfoEntry> housingDecorInfoTable)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (housingWallpaperInfoTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.HousingWallpaperInfo), housingWallpaperInfoTable);
        if (housingDecorInfoTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.HousingDecorInfo), housingDecorInfoTable);

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
}
