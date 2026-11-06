using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Pvp;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Store = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Pvp;

public partial class PvpFlagCooldownTests
{
    private readonly DbContextOptions<CharacterContext> pvpProviderOptions;
    public PvpFlagCooldownTests() { }
    internal PvpFlagCooldownTests(DbContextOptions<CharacterContext> options) => pvpProviderOptions = options;

    [Theory]
    [InlineData(false, "rollback")]
    [InlineData(false, "dispose")]
    [InlineData(false, "reject")]
    [InlineData(false, "unflushed")]
    [InlineData(false, "savepoint")]
    [InlineData(true, "rollback")]
    [InlineData(true, "dispose")]
    [InlineData(true, "reject")]
    [InlineData(true, "unflushed")]
    [InlineData(true, "savepoint")]
    public void PublicToggleCooldownRetriesAfterUnconfirmedCharacterSave(bool cancel, string outcome)
    {
        using var store = new Store(pvpProviderOptions);
        DateTime? initial = cancel ? DateTime.UtcNow.AddMinutes(5) : null;
        using (var seed = store.Context()) { seed.Character.Single().PvpFlagDisableUntilUtc = initial; seed.SaveChanges(); }
        using (var loaded = store.Context()) initial = loaded.Character.Single().PvpFlagDisableUntilUtc;
        using var actor = PvpActor(store);
        if (cancel) InitialisePvPFlagDisable(actor.Player, initial);
        else actor.Player.SetPvPFlag(PvPFlag.Enabled);
        var session = (IWorldSession)actor.Player.Session;
        new ClientPvpToggleFlagsHandler(NullLogger<ClientPvpToggleFlagsHandler>.Instance)
            .HandleMessage(session, CreateToggleFlags(cancel));
        var intended = GetPrivateField<DateTime?>(typeof(Player), actor.Player, "pvpFlagDisableUntilUtc");
        Assert.Equal(cancel, intended == null);
        var expected = ExpectedStoredExpiry(store, intended);
        using (var failed = store.Context())
        {
            using var transaction = failed.Database.BeginTransaction();
            if (outcome == "savepoint") transaction.CreateSavepoint("before_pvp");
            actor.Player.Save(failed);
            if (outcome == "reject")
            {
                failed.SavingChanges += (_, _) => throw new IOException("Owned PvP flush refusal.");
                Assert.Throws<IOException>(() => failed.SaveChanges());
            }
            else if (outcome != "unflushed") failed.SaveChanges();
            if (outcome == "rollback") transaction.Rollback();
            if (outcome == "unflushed") transaction.Commit();
            if (outcome == "savepoint") { transaction.RollbackToSavepoint("before_pvp"); transaction.Commit(); }
        }
        using (var before = store.Context()) Assert.Equal(initial, before.Character.Single().PvpFlagDisableUntilUtc);
        using (var retry = store.Context()) { actor.Player.Save(retry); retry.SaveChanges(); }
        using var read = store.Context();
        Assert.Equal(expected, read.Character.Single().PvpFlagDisableUntilUtc);
        using var fresh = PvpActor(store);
        InitialisePvPFlagDisable(fresh.Player, read.Character.Single().PvpFlagDisableUntilUtc);
        Assert.Equal(cancel ? PvPFlag.Disabled : PvPFlag.Enabled, fresh.Player.PvPFlag);
    }

    [Fact]
    public void LaterCooldownRequestRemainsDirtyAfterOlderSaveCommits()
    {
        using var store = new Store(pvpProviderOptions); using var actor = PvpActor(store);
        actor.Player.SetPvPFlag(PvPFlag.Enabled); actor.Player.RequestPvPFlagDisable(300000);
        using (var save = store.Context())
        {
            using var transaction = save.Database.BeginTransaction();
            actor.Player.Save(save); save.SaveChanges();
            actor.Player.RequestPvPFlagDisable(600000);
            transaction.Commit();
        }
        var later = GetPrivateField<DateTime?>(typeof(Player), actor.Player, "pvpFlagDisableUntilUtc");
        var expected = ExpectedStoredExpiry(store, later);
        using (var retry = store.Context()) { actor.Player.Save(retry); retry.SaveChanges(); }
        using var read = store.Context(); Assert.Equal(expected, read.Character.Single().PvpFlagDisableUntilUtc);
    }

    [Fact]
    public void SuppressedCooldownWriteRemainsPending()
    {
        using var store = new Store(pvpProviderOptions); using var actor = PvpActor(store);
        actor.Player.SetPvPFlag(PvPFlag.Enabled); actor.Player.RequestPvPFlagDisable(300000);
        var intended = GetPrivateField<DateTime?>(typeof(Player), actor.Player, "pvpFlagDisableUntilUtc");
        var expected = ExpectedStoredExpiry(store, intended);
        using (var save = store.Context())
        {
            actor.Player.Save(save);
            save.Entry(save.Character.Local.Single()).Property(row => row.PvpFlagDisableUntilUtc).IsModified = false;
            save.SaveChanges();
        }
        using (var retry = store.Context()) { actor.Player.Save(retry); retry.SaveChanges(); }
        using var read = store.Context(); Assert.Equal(expected, read.Character.Single().PvpFlagDisableUntilUtc);
    }

    [Fact]
    public void SuccessfulFlushedCooldownIsAcknowledged()
    {
        using var store = new Store(pvpProviderOptions); using var actor = PvpActor(store);
        actor.Player.SetPvPFlag(PvPFlag.Enabled); actor.Player.RequestPvPFlagDisable(300000);
        using (var save = store.Context()) { actor.Player.Save(save); save.SaveChanges(); }
        Assert.Equal(Player.PlayerSaveMask.None,
            GetPrivateField<Player.PlayerSaveMask>(typeof(Player), actor.Player, "saveMask") & Player.PlayerSaveMask.PvP);
    }

    [Fact]
    public void LaterRequestAndCancellationRetainTheirOwnIntentWhenExpiryReturnsToNull()
    {
        using var store = new Store(pvpProviderOptions); using var actor = PvpActor(store);
        actor.Player.SetPvPFlag(PvPFlag.Enabled); actor.Player.RequestPvPFlagDisable(300000);
        actor.Player.CancelPvPFlagDisable();
        using var save = store.Context(); using var transaction = save.Database.BeginTransaction();
        actor.Player.Save(save); save.SaveChanges();
        actor.Player.RequestPvPFlagDisable(600000); actor.Player.CancelPvPFlagDisable();
        transaction.Commit();
        Assert.NotEqual(Player.PlayerSaveMask.None,
            GetPrivateField<Player.PlayerSaveMask>(typeof(Player), actor.Player, "saveMask") & Player.PlayerSaveMask.PvP);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BusyMapAdmissionDoesNotBlockCooldownEnrollmentOrSettlement(bool settlement)
    {
        using var store = new Store(pvpProviderOptions); using var actor = PvpActor(store);
        actor.Player.SetPvPFlag(PvPFlag.Enabled); actor.Player.RequestPvPFlagDisable(300000);
        var intended = GetPrivateField<DateTime?>(typeof(Player), actor.Player, "pvpFlagDisableUntilUtc");
        var expected = ExpectedStoredExpiry(store, intended);
        using var save = store.Context();
        using var transaction = settlement ? save.Database.BeginTransaction() : null;
        if (settlement) { actor.Player.Save(save); save.SaveChanges(); }
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        object gate = ((NexusForever.Game.Abstract.Map.IPlayerMapAdmission)actor.Player).MapAdmissionSyncRoot;
        Task owner = Task.Run(() =>
        {
            lock (gate) { entered.Set(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); }
        });
        Assert.True(entered.Wait(TimeSpan.FromSeconds(3)));
        try
        {
            await Task.Run(() =>
            {
                if (settlement) transaction.Commit();
                else { actor.Player.Save(save); save.SaveChanges(); }
            }).WaitAsync(TimeSpan.FromSeconds(3));
            Assert.NotEqual(Player.PlayerSaveMask.None,
                GetPrivateField<Player.PlayerSaveMask>(typeof(Player), actor.Player, "saveMask") & Player.PlayerSaveMask.PvP);
        }
        finally { release.Set(); await owner; }
        using (var retry = store.Context()) { actor.Player.Save(retry); retry.SaveChanges(); }
        using var read = store.Context(); Assert.Equal(expected, read.Character.Single().PvpFlagDisableUntilUtc);
    }

    private DateTime? ExpectedStoredExpiry(Store store, DateTime? expiry)
    {
        if (pvpProviderOptions == null) return expiry;
        // Measure the configured datetime precision using an independent row.
        // The Player must persist the same value; no server rounding rule changes.
        using var control = store.Context();
        var row = new CharacterModel { Id = 43, Name = "PvP provider precision control", PvpFlagDisableUntilUtc = expiry };
        control.Character.Add(row); control.SaveChanges();
        var expected = control.Character.AsNoTracking().Where(value => value.Id == 43)
            .Select(value => value.PvpFlagDisableUntilUtc).Single();
        control.Character.Remove(row); control.SaveChanges();
        return expected;
    }

    private static Actor PvpActor(Store store)
    {
        var events = new EventQueue();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldSession.Events), events);
        var actor = new Actor(store, actorSession: session, actorEvents: events);
        proxy.SetProperty(nameof(IWorldSession.Player), actor.Player);
        proxy.SetProperty(nameof(IWorldSession.Account), actor.Player.Account);
        SetPrivateField(typeof(GridEntity), actor.Player, "visibleEntities", new Dictionary<uint, IGridEntity>());
        return actor;
    }
}
