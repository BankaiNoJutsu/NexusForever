using System.Data;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Account.Option;
using NexusForever.Game.Entity;
using NexusForever.Game.Option;
using NexusForever.Game.Static.Option;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Option;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Option;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using NetworkBinding = NexusForever.Network.World.Message.Model.Shared.Binding;

namespace NexusForever.Game.Tests.Entity;

public sealed class PlayerKeybindingPersistenceTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public PlayerKeybindingPersistenceTests() { }
    internal PlayerKeybindingPersistenceTests(DbContextOptions<CharacterContext> options) => this.options = options;
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    [Theory]
    [InlineData("new")] [InlineData("edit")] [InlineData("delete")]
    public async Task AcceptedCharacterBindingsSurviveRejectedPlayerFlushAndRetry(string action)
    {
        using var f = new Fixture(options); f.Seed(); if (action != "new") Seed(f);
        using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor);
        manager.Update(action == "delete" ? Set() : Set(Binding(1, 20)));
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true)); await Persist(f, actor);
        using var stored = f.Context(); if (action == "delete") Assert.Empty(stored.CharacterKeybinding); else Assert.Equal(20u, Assert.Single(stored.CharacterKeybinding).Code00);
        Assert.Empty(stored.CharacterEntitlement);
    }

    [Theory]
    [InlineData("dispose")] [InlineData("rollback")] [InlineData("suppressed")] [InlineData("lost-ack")]
    public async Task CompleteInputSetRecoversAfterAnUncertainOutcome(string fault)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true); using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor);
        manager.Update(Set(Binding(1, 20), Binding(3, 30)));
        if (fault == "lost-ack") await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Context(new LostAcknowledgement()), 42, actor.Player.Save));
        else
        {
            await using var context = f.Context(new SuppressedFlush(fault == "suppressed")); await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            actor.Player.Save(context); if (fault != "dispose") await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        await Persist(f, actor); using var stored = f.Context(); Assert.Equal(new ushort[] { 1, 3 }, stored.CharacterKeybinding.OrderBy(row => row.InputActionId).Select(row => row.InputActionId));
        Assert.Equal(new uint[] { 20, 30 }, stored.CharacterKeybinding.OrderBy(row => row.InputActionId).Select(row => row.Code00));
        using var reload = new Actor(f); await reload.Load(); var reloaded = Configure(f, reload);
        Assert.Equal(new[] { (Action: (ushort)1, Code: 20u), (Action: (ushort)3, Code: 30u) }, reloaded.Build().Bindings.OrderBy(row => row.InputActionId).Select(row => (row.InputActionId, row.Code00)));
        await Persist(f, reload);
    }

    [Theory]
    [InlineData("new")] [InlineData("edit")] [InlineData("delete")]
    public async Task NormalSaveFreezesBindingMembershipAndValuesBeforeAuth(string action)
    {
        using var f = new Fixture(options); f.Seed(); if (action != "new") Seed(f);
        using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor);
        actor.OnAuth = () => manager.Update(action == "delete" ? Set() : Set(Binding(1, 20)));
        await actor.Player.SaveDirect(); await actor.Drain();
        using (var first = f.Context()) { if (action == "new") Assert.Empty(first.CharacterKeybinding); else Assert.Equal(10u, Assert.Single(first.CharacterKeybinding).Code00); }
        actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain();
        using var stored = f.Context(); if (action == "delete") Assert.Empty(stored.CharacterKeybinding); else Assert.Equal(20u, Assert.Single(stored.CharacterKeybinding).Code00);
    }

    [Theory]
    [InlineData("edit")] [InlineData("delete")] [InlineData("restore")]
    public async Task FlushedAndExplicitlyAcceptedValuesRetainLaterEditsDeletionOrRestoration(string action)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f); using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor);
        manager.Update(action == "restore" ? Set() : Set(Binding(1, 20)));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.Save(context); await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges();
            manager.Update(action == "delete" ? Set() : Set(Binding(1, 30))); await transaction.CommitAsync();
        }
        using (var first = f.Context()) { if (action == "restore") Assert.Empty(first.CharacterKeybinding); else Assert.Equal(20u, Assert.Single(first.CharacterKeybinding).Code00); }
        await Persist(f, actor); using var stored = f.Context(); if (action == "delete") Assert.Empty(stored.CharacterKeybinding); else Assert.Equal(30u, Assert.Single(stored.CharacterKeybinding).Code00);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RecoveryRefusesAMixtureOfSeparatelyMatchingBindings(bool mixedSelection)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true); using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor);
        actor.Player.InputKeySet = InputSets.Character; manager.Update(Set(Binding(1, 20), Binding(3, 30))); await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        using (var foreign = f.Context())
        {
            if (mixedSelection) await foreign.Character.ExecuteUpdateAsync(set => set.SetProperty(row => row.InputKeySet, (sbyte)InputSets.Character));
            else await foreign.CharacterKeybinding.Where(row => row.InputActionId == 1).ExecuteUpdateAsync(set => set.SetProperty(row => row.Code00, 20u));
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => Persist(f, actor)); using var stored = f.Context(); Assert.Equal(new ushort[] { 1, 2 }, stored.CharacterKeybinding.OrderBy(row => row.InputActionId).Select(row => row.InputActionId));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ForeignSqlOrTrackedBindingIntentCannotBeOverwritten(bool tracked)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f); using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor); manager.Update(Set(Binding(1, 20)));
        if (!tracked) { using var foreign = f.Context(); await foreign.CharacterKeybinding.ExecuteUpdateAsync(set => set.SetProperty(row => row.Code00, 90u)); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context => { actor.Player.Save(context); if (tracked) context.CharacterKeybinding.Local.Single().Code00 = 90; }));
        using var stored = f.Context(); Assert.Equal(tracked ? 10u : 90u, Assert.Single(stored.CharacterKeybinding).Code00);
    }

    [Fact]
    public async Task UnknownLoadedActionAndAllInputSlotsSurviveAnotherBindingChange()
    {
        using var f = new Fixture(options); f.Seed();
        using (var seed = f.Context()) { seed.Character.Single().InputKeySet = -7; seed.CharacterKeybinding.Add(new() { Id = 42, InputActionId = ushort.MaxValue, DeviceEnum00 = uint.MaxValue, DeviceEnum01 = 91, DeviceEnum02 = 92,
            Code00 = 0xdeadbeef, Code01 = 94, Code02 = 95, MetaKeys00 = 96, MetaKeys01 = 97, MetaKeys02 = 98, EventTypeEnum00 = 99, EventTypeEnum01 = 100, EventTypeEnum02 = uint.MaxValue }); seed.SaveChanges(); }
        using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor); var update = manager.Build(); update.Bindings.Add(Binding(1, 20)); manager.Update(update); await Persist(f, actor);
        using var stored = f.Context(); var unknown = stored.CharacterKeybinding.Single(row => row.InputActionId == ushort.MaxValue);
        Assert.Equal(new uint[] { uint.MaxValue, 91, 92, 0xdeadbeef, 94, 95, 96, 97, 98, 99, 100, uint.MaxValue },
            new[] { unknown.DeviceEnum00, unknown.DeviceEnum01, unknown.DeviceEnum02, unknown.Code00, unknown.Code01, unknown.Code02, unknown.MetaKeys00, unknown.MetaKeys01, unknown.MetaKeys02, unknown.EventTypeEnum00, unknown.EventTypeEnum01, unknown.EventTypeEnum02 });
        Assert.Equal(20u, stored.CharacterKeybinding.Single(row => row.InputActionId == 1).Code00);
        Assert.Equal((sbyte)-7, stored.Character.Single().InputKeySet);
    }

    [Fact]
    public async Task ManagerSetAndMemberSaveApisShareAcknowledgedInputSetState()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor); manager.Update(Set(Binding(1, 20))); await Persist(f, actor);
        manager.Update(Set(Binding(1, 30), Binding(2, 40))); var bindings = Bindings(manager);
        await using (var context = f.Context()) { bindings.First().Save(context); bindings.Save(context); manager.Save(context); await context.SaveChangesAsync(); }
        manager.Update(Set(Binding(2, 45))); await Persist(f, actor); using var stored = f.Context(); var remaining = Assert.Single(stored.CharacterKeybinding); Assert.Equal(2, remaining.InputActionId); Assert.Equal(45u, remaining.Code00);
    }

    [Theory]
    [InlineData("manager")] [InlineData("set")] [InlineData("member")]
    public async Task ReplacedRealPlayerOwnersCannotWriteAnObsoleteInputSet(string entryPoint)
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor); manager.Update(Set(Binding(1, 20))); await Persist(f, actor);
        var bindings = Bindings(manager); var member = bindings.Single(); Configure(f, actor); using var context = f.Context();
        Assert.Throws<InvalidOperationException>(() => { if (entryPoint == "manager") manager.Save(context); else if (entryPoint == "set") bindings.Save(context); else member.Save(context); });
        Assert.True(context.IsSaveRejected); using var stored = f.Context(); Assert.Equal(20u, Assert.Single(stored.CharacterKeybinding).Code00);
    }

    [Fact]
    public async Task AnUnflushedCreateCanBeCancelledAndRecreatedInTheSameSaveUnit()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor);
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            manager.Update(Set(Binding(1, 20))); manager.Save(context); manager.Update(Set()); manager.Save(context);
            manager.Update(Set(Binding(1, 30))); manager.Save(context); await context.SaveChangesAsync(); await transaction.CommitAsync();
        }
        await Persist(f, actor); using var stored = f.Context(); Assert.Equal(30u, Assert.Single(stored.CharacterKeybinding).Code00);
    }

    [Fact]
    public async Task FailedInputSetSaveCannotMoveItsAcceptedBindingsToAnotherDatabase()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor); manager.Update(Set(Binding(1, 20)));
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true)); using var foreign = new Fixture(null); foreign.Seed();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Persist(foreign, actor)); await Persist(f, actor);
        using var stored = f.Context(); using var other = foreign.Context(); Assert.Equal(20u, Assert.Single(stored.CharacterKeybinding).Code00); Assert.Empty(other.CharacterKeybinding);
    }

    [Fact]
    public async Task AManagerReplacedDuringAuthCannotUseTheFrozenInputSet()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor); manager.Update(Set(Binding(1, 20)));
        actor.OnAuth = () => Configure(f, actor);
        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.Player.SaveDirect()); await actor.Drain(true);
        using var stored = f.Context(); Assert.Empty(stored.CharacterKeybinding);
    }

    [Fact]
    public async Task AnotherLoadedSetCannotOverlapAnUnfinishedCharacterSave()
    {
        using var f = new Fixture(options); f.Seed(); using var read = f.Context(); var model = read.Character.AsNoTracking().Include(row => row.Keybinding).Single();
        var first = new KeybindingSet(model); var second = new KeybindingSet(model); first.Update(Set(Binding(1, 20))); second.Update(Set(Binding(1, 30)));
        using (var held = f.Context()) { first.Save(held); using var blocked = f.Context(); Assert.Throws<InvalidOperationException>(() => second.Save(blocked)); }
        await using (var retry = f.Context()) { second.Save(retry); await retry.SaveChangesAsync(); }
        using var stored = f.Context(); Assert.Equal(30u, Assert.Single(stored.CharacterKeybinding).Code00);
    }

    [Fact]
    public async Task PacketHandlerKeepsAccountAndCharacterInputSetsIndependent()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor);
        var account = new AccountKeybindingManager(new AccountModel { Id = 1 }); ((RecordingDispatchProxy<IAccount>)(object)actor.Player.Account).SetProperty(nameof(IAccount.KeybindingManager), account);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var proxy); proxy.SetProperty(nameof(IWorldSession.Player), actor.Player); proxy.SetProperty(nameof(IWorldSession.Account), actor.Player.Account);
        var handler = new BiInputKeySetHandler(); var accountUpdate = Set(Binding(7, 70)); accountUpdate.CharacterId = 0; handler.HandleMessage(session, accountUpdate);
        handler.HandleMessage(session, Set(Binding(1, 20))); await Persist(f, actor);
        Assert.Equal(0ul, account.Build().CharacterId); Assert.Equal(7, Assert.Single(account.Build().Bindings).InputActionId); Assert.Equal(42ul, manager.Build().CharacterId);
        using var stored = f.Context(); Assert.Equal(1, Assert.Single(stored.CharacterKeybinding).InputActionId);
    }

    [Theory]
    [InlineData("create-edit")] [InlineData("delete-restore")] [InlineData("create-resave")] [InlineData("delete-resave")]
    public async Task AFlushedSetCanBeRestagedWithoutAcceptingUnrelatedEfIntent(string action)
    {
        bool deleted = action.StartsWith("delete", StringComparison.Ordinal), resave = action.EndsWith("resave", StringComparison.Ordinal);
        using var f = new Fixture(options); f.Seed(); if (deleted) Seed(f);
        using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor);
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.InputKeySet = InputSets.Character;
            manager.Update(deleted ? Set() : Set(Binding(1, 20))); manager.Save(context); await context.SaveChangesAsync(false);
            var unrelated = context.Character.Single(); unrelated.TotalXp = 765; context.ChangeTracker.DetectChanges();
            actor.Player.InputKeySet = InputSets.Account;
            if (!resave) manager.Update(Set(Binding(1, 30))); manager.Save(context);
            Assert.True(context.Entry(unrelated).Property(row => row.TotalXp).IsModified);
            await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges(); await transaction.CommitAsync();
        }
        using var stored = f.Context(); if (resave && deleted) Assert.Empty(stored.CharacterKeybinding); else Assert.Equal(resave ? 20u : 30u, Assert.Single(stored.CharacterKeybinding).Code00);
        Assert.Equal(765u, stored.Character.Single().TotalXp);
        Assert.Equal((sbyte)InputSets.Account, stored.Character.Single().InputKeySet);
    }

    [Theory]
    [InlineData("flush")] [InlineData("auth-gap")]
    public async Task PublicInputSelectionAndBindingsShareRecoveryAndNormalFreeze(string fault)
    {
        using var f = new Fixture(options); f.Seed();
        using (var seed = f.Context()) { seed.Character.Single().InputKeySet = (sbyte)InputSets.Account; seed.SaveChanges(); }
        using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var proxy); proxy.SetProperty(nameof(IWorldSession.Player), actor.Player); proxy.SetProperty(nameof(IWorldSession.Account), actor.Player.Account);
        void Change()
        {
            var packet = new ClientSetInputKeySet(); using var reader = new GamePacketReader(new MemoryStream(BitConverter.GetBytes((uint)InputSets.Character))); packet.Read(reader);
            new ClientSetInputKeySetHandler().HandleMessage(session, packet); new BiInputKeySetHandler().HandleMessage(session, Set(Binding(1, 20)));
        }
        if (fault == "flush") { Change(); await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true)); await Persist(f, actor); }
        else
        {
            actor.OnAuth = Change; await actor.Player.SaveDirect(); await actor.Drain();
            using (var first = f.Context()) { Assert.Equal((sbyte)InputSets.Account, first.Character.Single().InputKeySet); Assert.Empty(first.CharacterKeybinding); }
            actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain();
        }
        using var stored = f.Context(); Assert.Equal((sbyte)InputSets.Character, stored.Character.Single().InputKeySet); Assert.Equal(20u, Assert.Single(stored.CharacterKeybinding).Code00);
    }

    [Fact]
    public async Task LaterInputSelectionAndBindingValuesRemainPendingAfterAFlush()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor);
        actor.Player.InputKeySet = InputSets.Character; manager.Update(Set(Binding(1, 20)));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.Save(context); await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges();
            actor.Player.InputKeySet = InputSets.Account; manager.Update(Set(Binding(1, 30))); await transaction.CommitAsync();
        }
        using (var first = f.Context()) { Assert.Equal((sbyte)InputSets.Character, first.Character.Single().InputKeySet); Assert.Equal(20u, first.CharacterKeybinding.Single().Code00); }
        await Persist(f, actor); using var stored = f.Context(); Assert.Equal((sbyte)InputSets.Account, stored.Character.Single().InputKeySet); Assert.Equal(30u, stored.CharacterKeybinding.Single().Code00);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ForeignSqlOrTrackedInputSelectionCannotBeOverwritten(bool tracked)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using (var seed = f.Context()) { seed.Character.Single().InputKeySet = (sbyte)InputSets.Account; seed.SaveChanges(); }
        using var actor = new Actor(f); await actor.Load(); var manager = Configure(f, actor); actor.Player.InputKeySet = InputSets.Character; manager.Update(Set(Binding(1, 20)));
        if (!tracked) { using var foreign = f.Context(); await foreign.Character.ExecuteUpdateAsync(set => set.SetProperty(row => row.InputKeySet, (sbyte)InputSets.Default2)); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        { actor.Player.Save(context); if (tracked) context.Character.Local.Single().InputKeySet = (sbyte)InputSets.Default2; }));
        using var stored = f.Context(); Assert.Equal((sbyte)(tracked ? InputSets.Account : InputSets.Default2), stored.Character.Single().InputKeySet); Assert.Equal(10u, stored.CharacterKeybinding.Single().Code00);
    }

    private static KeybindingSet Bindings(CharacterKeybindingManager manager) => (KeybindingSet)typeof(CharacterKeybindingManager).GetField("bindingSet", Instance)!.GetValue(manager)!;
    private static BiInputKeySet Set(params NetworkBinding[] bindings) => new() { CharacterId = 42, Bindings = bindings.ToList() };
    private static NetworkBinding Binding(ushort action, uint code) => new() { InputActionId = action, Code00 = code, DeviceEnum00 = 1, EventTypeEnum00 = 3 };
    private static void Seed(Fixture f, bool two = false)
    {
        using var context = f.Context(); context.CharacterKeybinding.Add(new() { Id = 42, InputActionId = 1, Code00 = 10 });
        if (two) context.CharacterKeybinding.Add(new() { Id = 42, InputActionId = 2, Code00 = 11 }); context.SaveChanges();
    }
    private static CharacterKeybindingManager Configure(Fixture f, Actor actor)
    {
        using var context = f.Context(); var model = context.Character.AsNoTracking().Include(row => row.Keybinding).Single();
        typeof(Player).GetField("inputKeySet", Instance)!.SetValue(actor.Player, (InputSets)model.InputKeySet);
        var manager = new CharacterKeybindingManager(actor.Player, model); actor.Set(nameof(Player.KeybindingManager), manager); return manager;
    }
    private static Task Persist(Fixture f, Actor actor, bool fail = false)
        => CharacterAtomicSave.Execute(() => { var context = f.Context(); if (fail) context.SavingChanges += (_, _) => throw new IOException("Owned keybinding flush fault."); return context; }, 42, actor.Player.Save);
    private sealed class SuppressedFlush(bool suppress) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => new(suppress ? InterceptionResult<int>.SuppressWithResult(0) : result);
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned keybinding acknowledgement loss."));
    }
}
