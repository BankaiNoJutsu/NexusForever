using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Housing;

[Collection(LegacyServiceProviderCollection.Name)]
public class ResidenceTests
{
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
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        try
        {
            LegacyServiceProvider.Provider = BuildRealmProvider(7);

            IResidence residence = RecordingDispatchProxy<IResidence>.Create(out RecordingDispatchProxy<IResidence> residenceProxy);
            residenceProxy.SetProperty(nameof(IResidence.Id), 100ul);

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
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
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

    private static IServiceProvider BuildRealmProvider(ushort realmId)
    {
        IDatabaseManager databaseManager = RecordingDispatchProxy<IDatabaseManager>.Create(out _);
        var realmContext = new RealmContext(databaseManager);
        typeof(RealmContext)
            .GetProperty(nameof(RealmContext.RealmId))!
            .SetValue(realmContext, realmId);

        return new ServiceCollection()
            .AddSingleton(realmContext)
            .BuildServiceProvider();
    }
}
