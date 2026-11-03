using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network.Internal;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Guild;

public sealed class CommunityResidenceLifecycleTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"nf-community-{Guid.NewGuid():N}.sqlite");
    private DatabaseConnectionString Config => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={file};Foreign Keys=True" };
    private CharacterContext Context() => new(Config);

    [Fact]
    public void OfflineLeaveDetachesOnlyTheMembersResidenceAndPreservesNeighborsAndPrivacy()
    {
        Fixture f = Create();
        f.Community.Remove(42);
        f.Community.Remove(42);
        AssertDetached(f.Child);
        Assert.Equal(42ul, f.Child.OwnerId);
        Assert.Equal("Home", f.Child.Name);
        Assert.Equal(ResidencePrivacyLevel.Private, f.Child.PrivacyLevel);
        Assert.True(f.Child.HasNeighbor(43));
        Assert.Same(f.Root, f.Other.Parent);
        Assert.Equal(77ul, f.Other.GuildOwnerId);
        Assert.Null(f.Community.GetMember(42));
        Assert.NotNull(f.Community.GetMember(43));
        Assert.True(f.Community.NeedsSave);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void LoadedLeaveRemovesPlacementAndMovesOnlyAvailableOwnerInCommunity(bool ownerInCommunity, bool canTeleport, bool expectTeleport)
    {
        Fixture f = Create();
        var map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out var mapProxy);
        f.Root.Map = map;
        f.Child.Map = map;
        mapProxy.SetMethodHandler(nameof(IResidenceMapInstance.RemoveChild), args =>
        {
            var child = (IResidence)args[0];
            f.Root.RemoveChild(child);
            child.Map = null;
            return null;
        });
        var player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Map), ownerInCommunity ? map : null);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), canTeleport);
        f.Players.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] == 42 ? player : null);
        f.Community.Remove(42);
        AssertDetached(f.Child);
        Assert.Single(mapProxy.GetInvocations(nameof(IResidenceMapInstance.RemoveChild)));
        Assert.Equal(expectTeleport ? 1 : 0, playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)).Count);
        if (expectTeleport)
        {
            var teleport = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
            Assert.Same(f.Entrance.Entry, teleport.Arguments[0]);
            Assert.Equal(f.Entrance.Position, teleport.Arguments[1]);
            Assert.Single(f.Locks.GetInvocations(nameof(IMapLockManager.GetResidenceLock)));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PendingMoveFromOldSoloMapIsNotReparentedThroughThatMap(bool communityLoaded)
    {
        Fixture f = Create();
        var soloMap = RecordingDispatchProxy<IResidenceMapInstance>.Create(out var soloProxy);
        var communityMap = RecordingDispatchProxy<IResidenceMapInstance>.Create(out var communityProxy);
        f.Root.Map = communityLoaded ? communityMap : null;
        f.Child.Map = soloMap;
        var player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Map), soloMap);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
        f.Players.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);
        f.Community.Remove(42);
        AssertDetached(f.Child);
        Assert.Same(soloMap, f.Child.Map);
        Assert.Empty(soloProxy.GetInvocations(nameof(IResidenceMapInstance.RemoveChild)));
        Assert.Empty(communityProxy.GetInvocations(nameof(IResidenceMapInstance.RemoveChild)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void LoadedDetachUsesRealResidenceMapRemovalAndKeepsOtherPlacement()
    {
        Fixture f = Create();
        var map = new ResidenceMapInstance(null, null, null, null, f.Tables, null, null);
        map.Initialise(f.Root);
        Assert.Same(map, f.Child.Map);
        f.Community.Remove(42);
        AssertDetached(f.Child);
        Assert.Null(f.Child.Map);
        Assert.Same(map, f.Root.Map);
        Assert.Same(map, f.Other.Map);
    }

    [Fact]
    public void MissingEntranceRejectsBeforeChangingMemberOrResidence()
    {
        Fixture f = Create();
        var map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out _);
        var player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        f.Root.Map = map;
        f.Child.Map = map;
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
        f.Players.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);
        f.Residences.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceEntrance), null);
        Assert.Throws<InvalidOperationException>(() => f.Community.Remove(42));
        Assert.Same(f.Root, f.Child.Parent);
        Assert.NotNull(f.Community.GetMember(42));
        Assert.False(f.Community.PendingDelete);
    }

    [Fact]
    public void DisbandAlsoDetachesOrphanedChildLinks()
    {
        Fixture f = Create();
        var orphan = new Residence(new ResidenceModel
        {
            Id = 103, OwnerId = 99, GuildOwnerId = 77, PropertyInfoId = (byte)PropertyInfoId.CommunityResidence3
        }, gameTableManager: f.Tables);
        f.Root.AddChild(orphan, true);
        f.Community.DisbandGuild();
        AssertDetached(orphan);
        Assert.Empty(f.Root.GetChildren());
    }

    [Fact]
    public void DisbandUsesOnlineGuildLeavePathBeforeUnloadingCommunity()
    {
        Fixture f = Create();
        var map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out var mapProxy);
        f.Root.Map = f.Child.Map = f.Other.Map = map;
        mapProxy.SetMethodHandler(nameof(IResidenceMapInstance.RemoveChild), args =>
        {
            var child = (IResidence)args[0];
            f.Root.RemoveChild(child);
            child.Map = null;
            return null;
        });
        var player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
        var guildManager = RecordingDispatchProxy<IGuildManager>.Create(out var guildProxy);
        guildProxy.SetMethodHandler(nameof(IGuildManager.LeaveGuild), args =>
        {
            f.Community.LeaveGuild(player, (GuildResult)args[1]);
            return null;
        });
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guildManager);
        f.Players.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] == 42 ? player : null);
        f.Community.DisbandGuild();
        AssertDetached(f.Child);
        AssertDetached(f.Other);
        Assert.Single(guildProxy.GetInvocations(nameof(IGuildManager.LeaveGuild)));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Single(mapProxy.GetInvocations(nameof(IResidenceMapInstance.Unload)));
    }

    [Fact]
    public void DisbandPreservesAlreadyPendingCommunityUnload()
    {
        Fixture f = Create();
        var map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out var proxy);
        proxy.SetProperty(nameof(IResidenceMapInstance.UnloadStatus), MapUnloadStatus.AwaitingGridActions);
        f.Root.Map = map;
        f.Community.DisbandGuild();
        Assert.Empty(proxy.GetInvocations(nameof(IResidenceMapInstance.Unload)));
        AssertDetached(f.Child);
        AssertDetached(f.Other);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisbandDetachesAllChildrenAndRetainsRootAndOwnerObjects(bool loaded)
    {
        Fixture f = Create();
        var map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out var mapProxy);
        if (loaded)
        {
            f.Root.Map = f.Child.Map = f.Other.Map = map;
            mapProxy.SetMethodHandler(nameof(IResidenceMapInstance.RemoveChild), args =>
            {
                var child = (IResidence)args[0];
                f.Root.RemoveChild(child);
                child.Map = null;
                return null;
            });
        }
        f.Community.DisbandGuild();
        f.Community.DisbandGuild();
        AssertDetached(f.Child);
        AssertDetached(f.Other);
        Assert.Null(f.Root.GuildOwnerId);
        Assert.Same(f.Root, f.Community.Residence);
        Assert.Empty(f.Root.GetChildren());
        Assert.True(f.Community.PendingDelete);
        Assert.Single(f.Residences.GetInvocations(nameof(IGlobalResidenceManager.RemoveCommunity)));
        Assert.Equal(loaded ? 1 : 0, mapProxy.GetInvocations(nameof(IResidenceMapInstance.Unload)).Count);
    }

    [Fact]
    public void GlobalResidenceManagerRemovesCommunityIndexesAndVisitsWithoutRemovingHomes()
    {
        Fixture f = Create();
        var manager = new GlobalResidenceManager();
        Field<Dictionary<ulong, IResidence>>(manager, "residences").Add(f.Root.Id, f.Root);
        Field<Dictionary<ulong, IResidence>>(manager, "residences").Add(f.Child.Id, f.Child);
        Field<Dictionary<ulong, ulong>>(manager, "communityOwnerCache").Add(77, f.Root.Id);
        Field<Dictionary<string, ulong>>(manager, "communitySearchCache").Add("Old name", f.Root.Id);
        manager.RegisterCommunityVisits(f.Root, f.Community, "Owner");
        manager.RemoveCommunity(f.Community);
        manager.RemoveCommunity(f.Community);
        Assert.Null(manager.GetCommunityByOwner(77ul));
        Assert.Null(manager.GetResidence(f.Root.Id));
        Assert.Same(f.Child, manager.GetResidence(f.Child.Id));
        Assert.Empty(Field<Dictionary<string, ulong>>(manager, "communitySearchCache"));
        Assert.Empty(manager.GetRandomVisitableCommunities());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualGlobalGuildFlushPersistsOfflineDetachAtomicallyAndRetriesFailedSave(bool disband)
    {
        Fixture f = Create();
        await Seed(f);
        var manager = Manager(f);
        using (var context = Context())
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_detach BEFORE UPDATE ON residence BEGIN SELECT RAISE(ABORT, 'owned detach failure'); END");
        if (disband) f.Community.DisbandGuild(); else f.Community.Remove(42);
        manager.Update(61);
        await WaitForIdle(manager);
        using (var verify = Context())
        {
            Assert.Equal(77ul, (await verify.Residence.SingleAsync(r => r.Id == 101)).GuildOwnerId);
            Assert.Equal(2, await verify.GuildMember.CountAsync());
            Assert.Null((await verify.Guild.SingleAsync()).DeleteTime);
            await verify.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_detach");
        }
        Assert.True(f.Community.NeedsSave);
        manager.Update(61);
        await WaitForIdle(manager);
        using (var verify = Context())
        {
            ResidenceModel child = await verify.Residence.SingleAsync(r => r.Id == 101);
            Assert.Null(child.GuildOwnerId);
            Assert.Equal((byte)PropertyInfoId.Residence, child.PropertyInfoId);
            Assert.Equal(42ul, child.OwnerId);
            Assert.Equal("Home", child.Name);
            Assert.Equal((byte)ResidencePrivacyLevel.Private, child.PrivacyLevel);
            Assert.Single(await verify.ResidenceNeighbor.ToListAsync());
            Assert.Equal(3, await verify.Residence.CountAsync());
            Assert.Equal(2, await verify.ResidenceDecor.CountAsync());
            GuildModel guild = await verify.Guild.SingleAsync();
            if (disband)
            {
                Assert.NotNull(guild.DeleteTime);
                Assert.Null(guild.Name);
                Assert.Equal("Community", guild.OriginalName);
                Assert.Null((await verify.Residence.SingleAsync(r => r.Id == 100)).GuildOwnerId);
                Assert.Equal(2, await verify.GuildMember.CountAsync()); // soft-delete archive semantics
                Assert.Null(manager.GetGuild(77));
            }
            else
            {
                Assert.Null(guild.DeleteTime);
                Assert.Single(await verify.GuildMember.ToListAsync());
                Assert.Same(f.Community, manager.GetGuild(77));
            }
        }
        Assert.False(f.Community.NeedsSave);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResidenceTimerAndDirectSaveDeferPendingDetachToGuildCommit(bool disband)
    {
        Fixture f = Create();
        await Seed(f);
        if (disband) f.Community.DisbandGuild(); else f.Community.Remove(42);
        var residences = new GlobalResidenceManager(databaseManager: Databases());
        Field<Dictionary<ulong, IResidence>>(residences, "residences").Add(f.Child.Id, f.Child);
        residences.Update(61);
        using (var context = Context())
        {
            f.Child.Save(context);
            Assert.Empty(context.ChangeTracker.Entries());
            await context.SaveChangesAsync();
            Assert.Equal(77ul, (await context.Residence.SingleAsync(r => r.Id == 101)).GuildOwnerId);
            Assert.Equal(2, await context.GuildMember.CountAsync());
        }
        var guilds = Manager(f);
        guilds.Update(61);
        await WaitForIdle(guilds);
        f.Child.Name = "After committed detach";
        using (var context = Context())
        {
            f.Child.Save(context);
            await context.SaveChangesAsync();
            Assert.Equal("After committed detach", (await context.Residence.SingleAsync(r => r.Id == 101)).Name);
        }
    }

    [Fact]
    public async Task DisbandReloadKeepsOwnedHomesAndDecorWithoutResolvingDeletedCommunityRoot()
    {
        Fixture f = Create();
        await Seed(f);
        f.Community.DisbandGuild();
        var guilds = Manager(f);
        guilds.Update(61);
        await WaitForIdle(guilds);
        var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var characterProxy);
        characterProxy.SetMethodHandler(nameof(ICharacterManager.GetCharacter), args =>
        {
            var character = RecordingDispatchProxy<ICharacter>.Create(out var proxy);
            proxy.SetProperty(nameof(ICharacter.Name), $"Owner{args[0]}");
            return character;
        });
        var residences = new GlobalResidenceManager(characters, guilds, databaseManager: Databases(), gameTableManager: f.Tables);
        residences.Initialise();
        Assert.Null(residences.GetCommunityByOwner(77ul));
        Assert.Null(residences.GetResidence(100));
        IResidence home = residences.GetResidenceByOwner(42);
        AssertDetached(home);
        Assert.True(home.HasNeighbor(43));
        Assert.Single(home.GetDecor());
        using var context = Context();
        Assert.Equal(3, await context.Residence.CountAsync());
        Assert.Equal(2, await context.ResidenceDecor.CountAsync());
    }

    [Fact]
    public async Task CallerTransactionRollbackRetainsDetachIntentForNormalFlush()
    {
        Fixture f = Create();
        await Seed(f);
        f.Community.Remove(42);
        using (var context = Context())
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            f.Community.Save(context);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        Assert.True(f.Community.NeedsSave);
        using (var context = Context()) Assert.Equal(2, await context.GuildMember.CountAsync());
        var manager = Manager(f);
        manager.Update(61);
        await WaitForIdle(manager);
        using (var context = Context())
        {
            Assert.Single(await context.GuildMember.ToListAsync());
            Assert.Null((await context.Residence.SingleAsync(r => r.Id == 101)).GuildOwnerId);
        }
        Assert.False(f.Community.NeedsSave);
    }

    [Fact]
    public async Task CallerTransactionCommitReleasesDetachIntentOnlyAfterActualCommit()
    {
        Fixture f = Create();
        await Seed(f);
        f.Community.Remove(42);
        using var context = Context();
        using var transaction = await context.Database.BeginTransactionAsync();
        f.Community.Save(context);
        await context.SaveChangesAsync();
        Assert.True(f.Community.NeedsSave);
        await transaction.CommitAsync();
        Assert.True(f.Community.NeedsSave); // Confirmed SQL still owns an unpublished member removal.
        var manager = Manager(f); manager.Update(61); await WaitForIdle(manager);
        Assert.False(f.Community.NeedsSave);
        f.Child.Name = "After caller commit";
        using var next = Context();
        f.Child.Save(next);
        await next.SaveChangesAsync();
        Assert.Equal("After caller commit", (await next.Residence.SingleAsync(r => r.Id == 101)).Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RollingBackCommunityWritesToSavepointRetainsIntentAfterOuterCommit(bool disband)
    {
        Fixture f = Create();
        await Seed(f);
        if (disband) f.Community.DisbandGuild(); else f.Community.Remove(42);
        using (var context = Context())
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            await transaction.CreateSavepointAsync("before_detach");
            f.Community.Save(context);
            await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("before_detach");
            await transaction.CommitAsync();
        }
        Assert.True(f.Community.NeedsSave);
        using (var verify = Context())
        {
            Assert.Equal(77ul, (await verify.Residence.SingleAsync(r => r.Id == 101)).GuildOwnerId);
            Assert.Equal(2, await verify.GuildMember.CountAsync());
        }
        var manager = Manager(f);
        manager.Update(61);
        await WaitForIdle(manager);
        using (var verify = Context())
        {
            Assert.Null((await verify.Residence.SingleAsync(r => r.Id == 101)).GuildOwnerId);
            Assert.Equal(disband ? 2 : 1, await verify.GuildMember.CountAsync());
        }
        Assert.False(f.Community.NeedsSave);
    }

    [Fact]
    public async Task DisposingOnlyCommunityTransactionKeepsIntentAcrossLaterEmptySave()
    {
        Fixture f = Create();
        await Seed(f);
        f.Community.Remove(42);
        using var context = Context();
        var transaction = await context.Database.BeginTransactionAsync();
        f.Community.Save(context);
        await context.SaveChangesAsync();
        await transaction.DisposeAsync();
        await context.SaveChangesAsync();
        Assert.True(f.Community.NeedsSave);
        var manager = Manager(f);
        manager.Update(61);
        await WaitForIdle(manager);
        using var verify = Context();
        Assert.Single(await verify.GuildMember.ToListAsync());
        Assert.Null((await verify.Residence.SingleAsync(r => r.Id == 101)).GuildOwnerId);
    }

    [Fact]
    public async Task RejoiningBeforeFlushCancelsQueuedMembershipDeletion()
    {
        Fixture f = Create();
        await Seed(f);
        f.Community.Remove(42);
        f.Community.AddAgain(42);
        var manager = Manager(f);
        manager.Update(61);
        await WaitForIdle(manager);
        Assert.NotNull(f.Community.GetMember(42));
        using var verify = Context();
        Assert.Equal(2, await verify.GuildMember.CountAsync());
        Assert.NotNull(await verify.GuildMember.SingleOrDefaultAsync(m => m.Id == 77 && m.CharacterId == 42));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejoiningAfterFailedDetachReconcilesExistingRowAndRetainsFieldsAcrossRetry(bool failRejoinOnce)
    {
        Fixture f = Create();
        await Seed(f);
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_detach BEFORE UPDATE ON residence BEGIN SELECT RAISE(ABORT, 'owned failure'); END");
        IGuildMember original = f.Community.GetMember(42);
        f.Community.Remove(42);
        var manager = Manager(f);
        manager.Update(61);
        await WaitForIdle(manager);
        using (var setup = Context())
        {
            Assert.Equal(2, await setup.GuildMember.CountAsync());
            await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_detach");
            if (failRejoinOnce)
                await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_rejoin BEFORE UPDATE ON guild_member BEGIN SELECT RAISE(ABORT, 'owned failure'); END");
        }
        f.Community.AddAgain(42);
        IGuildMember member = f.Community.GetMember(42);
        Assert.Same(original, member);
        Assert.False(member.PendingCreate);
        member.Note = "Rejoined after rollback";
        member.CommunityPlotReservation = 2;
        manager.Update(61);
        await WaitForIdle(manager);
        if (failRejoinOnce)
        {
            Assert.True(f.Community.NeedsSave);
            using (var setup = Context())
                await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_rejoin");
            manager.Update(61);
            await WaitForIdle(manager);
        }
        using var verify = Context();
        GuildMemberModel persisted = await verify.GuildMember.SingleAsync(m => m.Id == 77 && m.CharacterId == 42);
        Assert.Equal("Rejoined after rollback", persisted.Note);
        Assert.Equal(2, persisted.CommunityPlotReservation);
        Assert.Equal(2, await verify.GuildMember.CountAsync());
        Assert.NotNull(f.Community.GetMember(42));
        Assert.False(f.Community.NeedsSave);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task RejoinWhileDeleteIsInFlightSurvivesItsCommitOrRollback(bool commitDelete, bool failRejoinOnce)
    {
        Fixture f = Create();
        await Seed(f);
        f.Community.Remove(42);
        using var earlier = Context();
        using var transaction = await earlier.Database.BeginTransactionAsync();
        f.Community.Save(earlier);
        await earlier.SaveChangesAsync();
        f.Community.AddAgain(42);
        f.Community.GetMember(42).Note = "Rejoined in flight";
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        if (failRejoinOnce)
            f.Community.SaveHook = () =>
            {
                entered.TrySetResult();
                Assert.True(release.Wait(TimeSpan.FromSeconds(10)));
            };
        var manager = Manager(f);
        manager.Update(61);
        AssertSaving(manager);
        if (commitDelete) await transaction.CommitAsync(); else await transaction.RollbackAsync();
        if (failRejoinOnce)
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            try
            {
                using var setup = Context();
                await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_rejoin_insert BEFORE INSERT ON guild_member BEGIN SELECT RAISE(ABORT, 'owned failure'); END");
                await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_rejoin_update BEFORE UPDATE ON guild_member BEGIN SELECT RAISE(ABORT, 'owned failure'); END");
            }
            finally { release.Set(); }
        }
        await WaitForIdle(manager);
        if (failRejoinOnce)
        {
            Assert.True(f.Community.NeedsSave);
            using (var setup = Context())
            {
                await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_rejoin_insert");
                await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_rejoin_update");
            }
            f.Community.SaveHook = null;
            manager.Update(61);
            await WaitForIdle(manager);
        }
        using var verify = Context();
        GuildMemberModel persisted = await verify.GuildMember.SingleAsync(m => m.Id == 77 && m.CharacterId == 42);
        Assert.Equal("Rejoined in flight", persisted.Note);
        Assert.Equal(2, await verify.GuildMember.CountAsync());
        Assert.NotNull(f.Community.GetMember(42));
        Assert.False(f.Community.NeedsSave);
    }

    [Fact]
    public async Task NeverPersistedDisbandReleasesExistingPersonalHomeForResidenceTimer()
    {
        Fixture f = Create();
        using (var seed = Context())
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Character.Add(new CharacterModel { Id = 42, Name = "Owner" });
            seed.Residence.Add(new ResidenceModel { Id = 101, OwnerId = 42, Name = "Personal", PropertyInfoId = (byte)PropertyInfoId.Residence });
            await seed.SaveChangesAsync();
        }
        var fresh = AdditionalCommunity(f, null);
        f.Residences.SetMethodHandler(nameof(IGlobalResidenceManager.CreateCommunity), args =>
        {
            var community = (ICommunity)args[0];
            Assert.Equal(77ul, community.Id);
            Assert.Equal("New community", community.Name);
            return new Residence(new ResidenceModel { Id = 100, GuildOwnerId = community.Id, Name = community.Name, PropertyInfoId = (byte)PropertyInfoId.Community }, gameTableManager: f.Tables);
        });
        fresh.Initialise("New community", "Leader", "Council", "Member");
        var personal = new Residence(new ResidenceModel { Id = 101, OwnerId = 42, Name = "Personal", PropertyInfoId = (byte)PropertyInfoId.Residence }, gameTableManager: f.Tables);
        personal.PropertyInfoId = PropertyInfoId.CommunityResidence1;
        fresh.Residence.AddChild(personal, false);
        fresh.AddAgain(42);
        fresh.DisbandGuild();
        var guilds = Manager(f with { Community = fresh });
        guilds.Update(61);
        Assert.Null(guilds.GetGuild(77));
        personal.Name = "Still writable";
        var residences = new GlobalResidenceManager(databaseManager: Databases());
        Field<Dictionary<ulong, IResidence>>(residences, "residences").Add(personal.Id, personal);
        residences.Update(61);
        await personal.PreviousSaveUnits(null).WaitAsync(TimeSpan.FromSeconds(10));
        using var verify = Context();
        ResidenceModel home = await verify.Residence.SingleAsync();
        Assert.Equal("Still writable", home.Name);
        Assert.Equal(42ul, home.OwnerId);
        Assert.Null(home.GuildOwnerId);
        Assert.Equal((byte)PropertyInfoId.Residence, home.PropertyInfoId);
        Assert.Empty(await verify.Guild.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommunityTransfersCannotCommitStaleAssociationAfterNewOwnerDetach(bool oldOwnerStagedBeforeNewDetach)
    {
        Fixture f = Create();
        await Seed(f);
        var model = new GuildModel { Id = 88, Name = "Second", Type = (byte)GuildType.Community,
            GuildRank = [new GuildRankModel { Id = 88, Index = 9, Name = "Member" }],
            GuildMember = [new GuildMemberModel { Id = 88, CharacterId = 42, Rank = 9, Note = "", CommunityPlotReservation = -1 }] };
        var second = AdditionalCommunity(f, model);
        second.Residence = new Residence(new ResidenceModel { Id = 200, GuildOwnerId = 88, PropertyInfoId = (byte)PropertyInfoId.Community }, gameTableManager: f.Tables);
        using (var seed = Context())
        {
            seed.Guild.Add(model);
            seed.Residence.Add(new ResidenceModel { Id = 200, GuildOwnerId = 88, Name = "RootB", PropertyInfoId = (byte)PropertyInfoId.Community });
            await seed.SaveChangesAsync();
        }
        f.Community.Remove(42);
        second.Residence.AddChild(f.Child, false);
        f.Child.PropertyInfoId = PropertyInfoId.CommunityResidence1;
        using var oldContext = Context();
        if (oldOwnerStagedBeforeNewDetach) f.Community.Save(oldContext);
        second.Remove(42);
        if (!oldOwnerStagedBeforeNewDetach)
        {
            f.Community.Save(oldContext);
            Assert.Empty(oldContext.ChangeTracker.Entries<ResidenceModel>());
        }
        var manager = Manager(f with { Community = second });
        manager.Update(61);
        if (oldOwnerStagedBeforeNewDetach)
        {
            lock (Field<object>(manager, "cacheLock"))
                Assert.Contains(88ul, Field<HashSet<ulong>>(manager, "guildSavesInFlight"));
            await oldContext.SaveChangesAsync();
            await WaitForIdle(manager);
        }
        else
        {
            await WaitForIdle(manager);
            await oldContext.SaveChangesAsync();
        }
        using var verify = Context();
        ResidenceModel home = await verify.Residence.SingleAsync(r => r.Id == 101);
        Assert.Null(home.GuildOwnerId);
        Assert.Equal((byte)PropertyInfoId.Residence, home.PropertyInfoId);
        Assert.Empty(await verify.GuildMember.Where(m => m.CharacterId == 42).ToListAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task GuildFlushWaitsForEarlierStaleResidenceContextActualTransactionCompletion(bool disband, bool commitEarlier)
    {
        Fixture f = Create();
        await Seed(f);
        using var earlier = Context();
        using var transaction = await earlier.Database.BeginTransactionAsync();
        f.Child.GuildOwnerId = 77;
        f.Child.PropertyInfoId = PropertyInfoId.CommunityResidence1;
        f.Child.Save(earlier); // stage the old association before the detach lease
        if (disband) f.Community.DisbandGuild(); else f.Community.Remove(42);
        var manager = Manager(f);
        manager.Update(61);
        AssertSaving(manager);
        Assert.True(f.Community.NeedsSave);
        await earlier.SaveChangesAsync();
        AssertSaving(manager); // SaveChanges is only a savepoint inside this transaction
        Assert.True(f.Community.NeedsSave);
        if (commitEarlier) await transaction.CommitAsync(); else await transaction.RollbackAsync();
        await WaitForIdle(manager);
        using var verify = Context();
        ResidenceModel child = await verify.Residence.SingleAsync(r => r.Id == 101);
        Assert.Null(child.GuildOwnerId);
        Assert.Equal((byte)PropertyInfoId.Residence, child.PropertyInfoId);
        Assert.Equal(disband ? 2 : 1, await verify.GuildMember.CountAsync());
        Assert.Equal(disband, (await verify.Guild.SingleAsync()).DeleteTime.HasValue);
        Assert.False(f.Community.NeedsSave);
    }

    [Fact]
    public async Task DisposingUncommittedEarlierContextUnblocksGuildFlushWithoutRestoringAssociation()
    {
        Fixture f = Create();
        await Seed(f);
        var earlier = Context();
        f.Child.GuildOwnerId = 77;
        f.Child.Save(earlier);
        f.Community.Remove(42);
        using (var direct = Context())
        {
            Assert.Throws<InvalidOperationException>(() => f.Community.Save(direct));
            Assert.Empty(direct.ChangeTracker.Entries());
            Assert.True(f.Community.NeedsSave);
        }
        var manager = Manager(f);
        manager.Update(61);
        AssertSaving(manager);
        await earlier.DisposeAsync();
        await WaitForIdle(manager);
        using var verify = Context();
        Assert.Null((await verify.Residence.SingleAsync(r => r.Id == 101)).GuildOwnerId);
        Assert.Single(await verify.GuildMember.ToListAsync());
    }

    [Fact]
    public async Task ChildOnlyDirtyFieldsAreFlushedByGlobalGuildManager()
    {
        Fixture f = Create();
        await Seed(f);
        f.Community.GetMember(42).Note = "Updated offline";
        f.Community.GetMember(42).Rank.Name = "Renamed rank";
        var manager = Manager(f);
        manager.Update(61);
        await WaitForIdle(manager);
        using var context = Context();
        Assert.Equal("Updated offline", (await context.GuildMember.SingleAsync(m => m.CharacterId == 42)).Note);
        Assert.Equal("Renamed rank", (await context.GuildRank.SingleAsync()).Name);
    }

    [Fact]
    public async Task RepeatedTimerTicksDoNotDispatchConcurrentGuildSaveUnits()
    {
        Fixture f = Create();
        await Seed(f);
        f.Community.Remove(42);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        int attempts = 0;
        f.Community.SaveHook = () =>
        {
            Interlocked.Increment(ref attempts);
            entered.SetResult();
            Assert.True(release.Wait(TimeSpan.FromSeconds(10)));
        };
        var manager = Manager(f);
        Task first = Task.Run(() => manager.Update(61));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            manager.Update(61);
            Assert.Equal(1, Volatile.Read(ref attempts));
        }
        finally { release.Set(); }
        await first;
        await WaitForIdle(manager);
        using var context = Context();
        Assert.Single(await context.GuildMember.ToListAsync());
        Assert.False(f.Community.NeedsSave);
    }

    private async Task Seed(Fixture f)
    {
        using var context = Context();
        await context.Database.EnsureCreatedAsync();
        context.Character.AddRange(new CharacterModel { Id = 42, Name = "Owner" }, new CharacterModel { Id = 43, Name = "Neighbor" });
        context.Guild.Add(f.Model);
        context.Residence.AddRange(
            new ResidenceModel { Id = 100, GuildOwnerId = 77, Name = "Root", PropertyInfoId = (byte)PropertyInfoId.Community },
            ChildModel(101, 42), ChildModel(102, 43));
        context.ResidenceNeighbor.Add(new ResidenceNeighborModel { ResidenceId = 101, NeighborCharacterId = 43, PermissionLevel = 1 });
        context.ResidenceDecor.AddRange(new ResidenceDecor { Id = 100, DecorId = 500, DecorInfoId = 5, DecorType = (uint)DecorType.Crate },
            new ResidenceDecor { Id = 101, DecorId = 501, DecorInfoId = 5, DecorType = (uint)DecorType.Crate });
        await context.SaveChangesAsync();
        // Clear the loaded AddChild's association dirtiness without touching database state.
        using var staging = Context();
        f.Root.Save(staging); f.Child.Save(staging); f.Other.Save(staging);
    }

    private GlobalGuildManager Manager(Fixture f)
    {
        var manager = new GlobalGuildManager(null, Databases());
        Field<Dictionary<ulong, IGuildBase>>(manager, "guilds").Add(f.Community.Id, f.Community);
        return manager;
    }

    private IDatabaseManager Databases()
    {
        var database = new CharacterDatabase();
        database.Initialise(Config);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var proxy);
        proxy.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        return databases;
    }

    private static async Task WaitForIdle(GlobalGuildManager manager)
    {
        var active = Field<HashSet<ulong>>(manager, "guildSavesInFlight");
        var sync = Field<object>(manager, "cacheLock");
        for (int i = 0; i < 200; i++)
        {
            lock (sync) if (active.Count == 0) return;
            await Task.Delay(10);
        }
        Assert.Fail("Global guild persistence did not finish.");
    }

    private static void AssertSaving(GlobalGuildManager manager)
    {
        lock (Field<object>(manager, "cacheLock"))
            Assert.Contains(77ul, Field<HashSet<ulong>>(manager, "guildSavesInFlight"));
    }

    private static Fixture Create()
    {
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var publisherProxy);
        publisherProxy.SetMethodReturn("PublishAsync", Task.CompletedTask);
        var residences = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out var residenceProxy);
        var entrance = RecordingDispatchProxy<IResidenceEntrance>.Create(out var entranceProxy);
        entranceProxy.SetProperty(nameof(IResidenceEntrance.Entry), new WorldEntry { Id = 900 });
        entranceProxy.SetProperty(nameof(IResidenceEntrance.Position), new Vector3(1, 2, 3));
        entranceProxy.SetProperty(nameof(IResidenceEntrance.Rotation), Quaternion.Identity);
        residenceProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceEntrance), entrance);
        var locks = RecordingDispatchProxy<IMapLockManager>.Create(out var lockProxy);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var playerProxy);
        var globals = RecordingDispatchProxy<IGlobalGuildManager>.Create(out _);
        var community = new TestCommunity(realm, publisher, residences, locks, globals, players);
        var model = new GuildModel
        {
            Id = 77, Type = (byte)GuildType.Community, Name = "Community", LeaderId = 43,
            GuildRank = [new GuildRankModel { Id = 77, Index = 9, Name = "Member" }],
            GuildMember = [new GuildMemberModel { Id = 77, CharacterId = 42, Rank = 9, Note = "Old", CommunityPlotReservation = -1 }, new GuildMemberModel { Id = 77, CharacterId = 43, Rank = 9, Note = "Other", CommunityPlotReservation = -1 }]
        };
        // This fixture constructs the loaded actor before seeding. Match the
        // database-generated -1 plot value instead of an EF pre-insert sentinel.
        community.Initialise(model);
        var tables = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        var table = (GameTable<HousingPlotInfoEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<HousingPlotInfoEntry>));
        typeof(GameTable<HousingPlotInfoEntry>).GetField("<Entries>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, Array.Empty<HousingPlotInfoEntry>());
        typeof(GameTableManager).GetField("<HousingPlotInfo>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tables, table);
        var decorTable = (GameTable<HousingDecorInfoEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<HousingDecorInfoEntry>));
        typeof(GameTable<HousingDecorInfoEntry>).GetField("<Entries>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(decorTable, new[] { new HousingDecorInfoEntry { Id = 5 } });
        typeof(GameTable<HousingDecorInfoEntry>).GetField("header", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(decorTable, new GameTableHeader { MaxId = 6 });
        typeof(GameTable<HousingDecorInfoEntry>).GetField("lookup", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(decorTable, new[] { -1, -1, -1, -1, -1, 0 });
        typeof(GameTableManager).GetField("<HousingDecorInfo>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tables, decorTable);
        var root = new Residence(new ResidenceModel { Id = 100, GuildOwnerId = 77, PropertyInfoId = (byte)PropertyInfoId.Community }, gameTableManager: tables);
        var child = new Residence(ChildModel(101, 42), gameTableManager: tables);
        var other = new Residence(ChildModel(102, 43), gameTableManager: tables);
        child.AddNeighbor(43, 1);
        root.AddChild(child, false); root.AddChild(other, false);
        community.Residence = root;
        return new(community, model, root, child, other, residenceProxy, playerProxy, lockProxy, entrance, tables);
    }

    private static TestCommunity AdditionalCommunity(Fixture f, GuildModel model)
    {
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var publisherProxy);
        publisherProxy.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var globals = RecordingDispatchProxy<IGlobalGuildManager>.Create(out var globalProxy);
        globalProxy.SetProperty(nameof(IGlobalGuildManager.NextGuildId), 77ul);
        var community = new TestCommunity(realm, publisher, (IGlobalResidenceManager)(object)f.Residences,
            (IMapLockManager)(object)f.Locks, globals, (IPlayerManager)(object)f.Players);
        if (model != null) community.Initialise(model);
        return community;
    }

    private static ResidenceModel ChildModel(ulong id, ulong owner) => new()
    {
        Id = id, OwnerId = owner, GuildOwnerId = 77, Name = "Home", PropertyInfoId = (byte)(owner == 42 ? PropertyInfoId.CommunityResidence1 : PropertyInfoId.CommunityResidence2),
        PrivacyLevel = (byte)ResidencePrivacyLevel.Private
    };

    private static void AssertDetached(IResidence residence)
    {
        Assert.Null(residence.Parent);
        Assert.Null(residence.GuildOwnerId);
        Assert.Equal(PropertyInfoId.Residence, residence.PropertyInfoId);
    }

    private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(value)!;
    private sealed record Fixture(TestCommunity Community, GuildModel Model, Residence Root, Residence Child, Residence Other,
        RecordingDispatchProxy<IGlobalResidenceManager> Residences, RecordingDispatchProxy<IPlayerManager> Players,
        RecordingDispatchProxy<IMapLockManager> Locks, IResidenceEntrance Entrance, IGameTableManager Tables);

    private sealed class TestCommunity(IRealmContext realm, IInternalMessagePublisher publisher, IGlobalResidenceManager residences,
        IMapLockManager locks, IGlobalGuildManager globals, IPlayerManager players)
        : Community(realm, publisher, residences, null, locks, globalGuildManager: globals, playerManager: players)
    {
        public Action SaveHook { get; set; }
        public void Remove(ulong characterId) => RemoveMember(characterId, false);
        public void AddAgain(ulong characterId)
        {
            var player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
            proxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
            proxy.SetProperty(nameof(IPlayer.Identity), new Identity { Id = characterId, RealmId = 1 });
            typeof(GuildBase).GetMethod("AddMember", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(this, [player, (byte)9]);
        }
        public override void Save(CharacterContext context)
        {
            SaveHook?.Invoke();
            base.Save(context);
        }
    }

    public void Dispose()
    {
        using var pool = new Microsoft.Data.Sqlite.SqliteConnection(Config.ConnectionString);
        Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
        if (File.Exists(file)) File.Delete(file);
    }
}
