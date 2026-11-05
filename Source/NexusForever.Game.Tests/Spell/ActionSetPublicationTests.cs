using System.Reflection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Spell;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using SourceFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Spell;

public sealed class ActionSetPublicationTests
{
    [Fact]
    public async Task AcceptedNoOpPublishesWithoutRequiringAnUnrelatedSqlEdit()
    {
        using var f = await Create();
        f.Request(); await f.Persist();
        f.Request(); await f.Persist();
        Assert.Equal(2, f.Packets<ServerActionSet>().Count());
    }

    [Fact]
    public async Task TierAmpAndSelectedVirtualItemRepliesFollowTheirConfirmedRows()
    {
        using var f = await Create(learned: true);
        int items = f.Packets<ServerItemAdd>().Count();
        f.Request(2, amp: true);
        Assert.Empty(f.Packets<ServerSpellUpdate>()); Assert.Empty(f.Packets<ServerAbilityBook>());
        Assert.Empty(f.Packets<ServerAmpList>()); Assert.Equal(items, f.Packets<ServerItemAdd>().Count());
        await f.Persist();
        using var stored = f.Source.Context();
        Assert.Equal(2, stored.CharacterActionSetShortcut.Single().Tier);
        Assert.Equal(1, stored.CharacterActionSetAmp.Single().AmpId);
        Assert.Equal(2, Assert.Single(f.Packets<ServerSpellUpdate>()).TierIndex);
        Assert.All(Assert.Single(f.Packets<ServerAbilityBook>()).Spells, row => Assert.Equal(row.SpecIndex == 0 ? 2 : 1, row.TierIndexAchieved));
        Assert.Equal(items + 1, f.Packets<ServerItemAdd>().Count());
        Assert.Equal(new ushort[] { 1 }, Assert.Single(f.Packets<ServerAmpList>()).Amps);
        Assert.Equal(41u, Assert.Single(f.Packets<ServerAbilityPoints>()).AbilityPoints);
    }

    [Fact]
    public async Task CoalescedRequestsPublishOnlyTheFinalCommittedTier()
    {
        using var f = await Create(learned: true);
        f.Request(2); f.Request(3); await f.Persist();
        Assert.Equal(3, Assert.Single(f.Packets<ServerSpellUpdate>()).TierIndex);
        Assert.Single(f.Packets<ServerActionSet>());
        using var stored = f.Source.Context(); Assert.Equal(3, stored.CharacterActionSetShortcut.Single().Tier);
    }

    [Fact]
    public async Task RejectedTierAndAmpGraphRetriesTogetherBeforeSuccess()
    {
        using var f = await Create(learned: true);
        using (var setup = f.Source.Context()) await setup.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_public_amp BEFORE INSERT ON character_action_set_amp BEGIN SELECT RAISE(ABORT, 'owned AMP failure'); END");
        f.Request(2, amp: true); await Assert.ThrowsAsync<DbUpdateException>(() => f.Persist());
        Assert.Empty(f.Packets<ServerSpellUpdate>()); Assert.Empty(f.Packets<ServerAbilityBook>()); Assert.Empty(f.Packets<ServerAmpList>());
        using (var rejected = f.Source.Context())
        {
            Assert.Equal(1, rejected.CharacterActionSetShortcut.Single().Tier); Assert.Empty(rejected.CharacterActionSetAmp);
            await rejected.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_public_amp");
        }
        await f.Persist();
        using var stored = f.Source.Context(); Assert.Equal(2, stored.CharacterActionSetShortcut.Single().Tier); Assert.Single(stored.CharacterActionSetAmp);
        Assert.Single(f.Packets<ServerSpellUpdate>()); Assert.Single(f.Packets<ServerAmpList>());
    }

    [Fact]
    public async Task LostCommitAcknowledgementWaitsForStoredObservationWithoutReapplyingTheRequest()
    {
        using var f = await Create(learned: true); f.Request(2, amp: true);
        await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Source.Context(new LostAcknowledgement()), 42, f.Actor.Player.Save));
        await f.Pump(); Assert.Empty(f.Packets<ServerSpellUpdate>()); Assert.Empty(f.Packets<ServerActionSet>());
        using (var committed = f.Source.Context()) { Assert.Equal(2, committed.CharacterActionSetShortcut.Single().Tier); Assert.Single(committed.CharacterActionSetAmp); }
        await f.Persist(); Assert.Single(f.Packets<ServerActionSet>()); Assert.Single(f.Packets<ServerSpellUpdate>());
        await f.Persist(); Assert.Single(f.Packets<ServerActionSet>());
    }

    [Fact]
    public async Task FrozenNormalSaveDoesNotPublishOrConsumeALaterRequest()
    {
        using var f = await Create(learned: true);
        f.Request(2); f.Actor.OnAuth = () => f.Request(3);
        await f.Actor.Player.SaveDirect(); await f.Actor.Drain();
        Assert.Equal(2, Assert.Single(f.Packets<ServerSpellUpdate>()).TierIndex);
        using (var first = f.Source.Context()) Assert.Equal(2, first.CharacterActionSetShortcut.Single().Tier);
        f.Actor.OnAuth = null; await f.Actor.Player.SaveDirect(); await f.Actor.Drain();
        Assert.Equal(new byte[] { 2, 3 }, f.Packets<ServerSpellUpdate>().Select(row => row.TierIndex));
        using var stored = f.Source.Context(); Assert.Equal(3, stored.CharacterActionSetShortcut.Single().Tier);
    }

    [Fact]
    public async Task InitialViewDuringUnsavedChangesUsesTheConfirmedBook()
    {
        using var f = await Create(learned: true);
        f.Request(2); f.Spells.SendServerSpellList(); await f.Pump();
        Assert.All(Assert.Single(f.Packets<ServerAbilityBook>()).Spells, row => Assert.Equal(1, row.TierIndexAchieved));
        Assert.Empty(f.Packets<ServerSpellUpdate>());
        await f.Persist();
        Assert.Equal(2, f.Packets<ServerAbilityBook>().Last().Spells.Single(row => row.SpecIndex == 0).TierIndexAchieved);
    }

    [Theory]
    [InlineData("book")] [InlineData("item")] [InlineData("set")]
    public async Task RejectedMutablePacketRetriesFreshWithoutReplayingEarlierPacketsOrSql(string target)
    {
        using var f = await Create(learned: true);
        object rejected = null; int attempts = 0;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            bool matches = target switch { "book" => args[0] is ServerAbilityBook, "item" => args[0] is ServerItemAdd, _ => args[0] is ServerActionSet };
            if (matches && ++attempts == 1)
            {
                rejected = args[0];
                if (rejected is ServerAbilityBook book) book.Spells.Clear();
                if (rejected is ServerItemAdd item) item.InventoryItem.Item.LocationData.BagIndex = 99;
                if (rejected is ServerActionSet set) set.Actions.Clear();
                throw new IOException("Owned LAS enqueue failure.");
            }
            return null;
        });
        f.Request(2); await f.Persist();
        using (var stored = f.Source.Context()) Assert.Equal(2, stored.CharacterActionSetShortcut.Single().Tier);
        object acceptedTarget = target switch { "book" => f.Packets<ServerAbilityBook>().Last(), "item" => f.Packets<ServerItemAdd>().Last(), _ => f.Packets<ServerActionSet>().Last() };
        Assert.NotSame(rejected, acceptedTarget);
        var accepted = f.Packets<ServerActionSet>().Last();
        Assert.Equal(ActionSet.MaxActionCount, accepted.Actions.Count); Assert.Equal(900u, accepted.Actions[0].ObjectId);
        Assert.Equal(4, f.Packets<ServerAbilityBook>().Last().Spells.Count);
        Assert.Equal(0u, f.Packets<ServerItemAdd>().Last().InventoryItem.Item.LocationData.BagIndex);
        Assert.Equal(target == "book" ? 2 : 1, f.Packets<ServerAbilityBook>().Count());
        Assert.Single(f.Packets<ServerSpellUpdate>()); Assert.Single(f.Packets<ServerActionSetClearCache>());
        Assert.Equal(2, attempts);
    }

    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned LAS commit acknowledgement loss."));
    }

    [Theory]
    [InlineData("epoch")] [InlineData("account-session")] [InlineData("manager")]
    public async Task ChangedOriginalOwnerPreventsConfirmedPublication(string change)
    {
        using var f = await Create(); f.Request();
        var account = (RecordingDispatchProxy<NexusForever.Game.Abstract.Account.IAccount>)(object)f.Actor.Player.Account;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerAbilityBook) return null;
            if (change == "epoch") account.SetProperty(nameof(NexusForever.Game.Abstract.Account.IAccount.WorldSessionEpoch), 99ul);
            if (change == "account-session") account.SetProperty(nameof(NexusForever.Game.Abstract.Account.IAccount.Session), RecordingDispatchProxy<IWorldSession>.Create(out _));
            if (change == "manager") f.Actor.Set(nameof(Player.SpellManager), RecordingDispatchProxy<ISpellManager>.Create(out _));
            return null;
        });
        await f.Persist(); Assert.Single(f.Packets<ServerAbilityBook>());
        Assert.Empty(f.Packets<ServerActionSet>()); Assert.Empty(f.Packets<ServerActionSetClearCache>());
    }

    [Fact]
    public async Task PublicActionSetRequestDoesNotPublishItsUncommittedShortcuts()
    {
        using var f = await Create();
        f.Request();
        Assert.Empty(f.Packets<ServerActionSet>());
        Assert.Empty(f.Packets<ServerActionSetClearCache>());
        using var stored = f.Source.Context(); Assert.Single(stored.CharacterActionSetShortcut);
        await f.Persist();
        using var confirmed = f.Source.Context(); Assert.Empty(confirmed.CharacterActionSetShortcut);
        Assert.Single(f.Packets<ServerActionSet>());
    }

    [Fact]
    public async Task RejectedPublicActionSetSaveRetainsItsIntentAndPublishesOnlyAfterRetry()
    {
        using var f = await Create();
        using (var setup = f.Source.Context()) await setup.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_public_las BEFORE DELETE ON character_action_set_shortcut BEGIN SELECT RAISE(ABORT, 'owned LAS failure'); END");
        f.Request();
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Persist());
        Assert.Empty(f.Packets<ServerActionSet>());
        using (var rejected = f.Source.Context())
        {
            Assert.Single(rejected.CharacterActionSetShortcut);
            await rejected.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_public_las");
        }
        await f.Persist();
        using var stored = f.Source.Context(); Assert.Empty(stored.CharacterActionSetShortcut);
        Assert.Single(f.Packets<ServerActionSet>());
    }

    [Fact]
    public async Task ReentrantPlayerReplacementStopsLaterPacketsOfTheOriginalActionSet()
    {
        using var f = await Create();
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerAbilityBook)
                f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            return null;
        });
        f.Request(); await f.Persist();
        Assert.Empty(f.Packets<ServerActionSet>());
        Assert.Empty(f.Packets<ServerActionSetClearCache>());
        using var stored = f.Source.Context(); Assert.Empty(stored.CharacterActionSetShortcut);
    }

    private static async Task<Fixture> Create(bool learned = false)
    {
        var source = new SourceFixture(null); source.Seed();
        using (var setup = source.Context())
        {
            setup.CharacterActionSetShortcut.Add(new CharacterActionSetShortcutModel { Id = 42, SpecIndex = 0,
                Location = (ushort)UILocation.LAS1, ShortcutType = (byte)(learned ? ShortcutType.SpellbookItem : ShortcutType.GameCommand), ObjectId = learned ? 900u : 1u, Tier = 1 });
            if (learned) setup.CharacterSpell.Add(new() { Id = 42, Spell4BaseId = 900, Tier = 1 });
            await setup.SaveChangesAsync();
        }
        var events = new EventQueue();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var connection);
        connection.SetProperty(nameof(IWorldSession.Events), events);
        connection.SetProperty(nameof(IWorldSession.CanProcessIncomingPackets), true);
        var actor = new Actor(source, actorSession: session, actorEvents: events);
        ((RecordingDispatchProxy<NexusForever.Game.Abstract.Account.IAccount>)(object)actor.Player.Account)
            .SetProperty(nameof(NexusForever.Game.Abstract.Account.IAccount.Session), session);
        connection.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var fields = BindingFlags.Instance | BindingFlags.NonPublic;
        var stats = (Dictionary<Stat, IStatValue>)typeof(WorldEntity).GetField("stats", fields)!.GetValue(actor.Player)!;
        stats.Add(Stat.Health, new StatValue(Stat.Health, 1u));
        typeof(GridEntity).GetField("visibleEntities", fields)!.SetValue(actor.Player, new Dictionary<uint, IGridEntity>());
        var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var globalProxy);
        if (learned)
        {
            var bags = (Dictionary<InventoryLocation, IBag>)typeof(Inventory).GetField("bags", fields)!.GetValue(actor.Player.Inventory)!;
            bags.Add(InventoryLocation.Ability, new Bag(InventoryLocation.Ability, 100));
            var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var baseProxy);
            baseProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 900, WeaponSlot = 5 });
            baseProxy.SetProperty(nameof(ISpellBaseInfo.SpellType), new Spell4SpellTypesEntry { Id = 5 });
            baseProxy.SetMethodHandler(nameof(ISpellBaseInfo.GetSpellInfo), args =>
            {
                var info = RecordingDispatchProxy<ISpellInfo>.Create(out var proxy);
                proxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 900u + (byte)args[0], Spell4BaseIdBaseSpell = 900, TierIndex = (byte)args[0] });
                return info;
            });
            globalProxy.SetMethodReturn(nameof(IGlobalSpellManager.GetSpellBaseInfo), baseInfo);
            ((RecordingDispatchProxy<IGameTableManager>)(object)source.Source.Tables).SetProperty(nameof(IGameTableManager.EldanAugmentation),
                PlayerBindPointTests.Table(new EldanAugmentationEntry { Id = 1, PowerCost = 1 }));
        }
        CharacterModel model;
        using (var loaded = source.Context()) model = loaded.Character.AsNoTracking().Include(row => row.ActionSetShortcut).Include(row => row.Spell).Single();
        var spells = new SpellManager(actor.Player, model, globalSpellManager: global, gameTableManager: source.Source.Tables);
        actor.Set(nameof(Player.SpellManager), spells);
        var matches = RecordingDispatchProxy<IMatchManager>.Create(out var matching);
        matching.SetMethodReturn(nameof(IMatchManager.GetMatchCharacter), RecordingDispatchProxy<IMatchCharacter>.Create(out _));
        await actor.Load();
        return new(source, actor, spells, session, connection,
            new ClientRequestActionSetChangesHandler(matches, source.Source.Tables, global, NullLogger<ClientRequestActionSetChangesHandler>.Instance));
    }

    private sealed record Fixture(SourceFixture Source, Actor Actor, SpellManager Spells, IWorldSession Session,
        RecordingDispatchProxy<IWorldSession> Messages, ClientRequestActionSetChangesHandler Handler) : IDisposable
    {
        internal void Request(byte tier = 0, bool amp = false)
        {
            var request = new ClientRequestActionSetChanges();
            request.Actions.AddRange(Enumerable.Repeat(0u, ClientRequestActionSetChanges.ActionCount));
            if (tier != 0)
            {
                request.Actions[0] = 900;
                var value = new ClientRequestActionSetChanges.ActionTier();
                typeof(ClientRequestActionSetChanges.ActionTier).GetProperty(nameof(value.Action))!.SetValue(value, 900u);
                typeof(ClientRequestActionSetChanges.ActionTier).GetProperty(nameof(value.Tier))!.SetValue(value, tier);
                request.ActionTiers.Add(value);
            }
            if (amp) request.Amps.Add(1);
            Handler.HandleMessage(Session, request);
        }
        internal IEnumerable<T> Packets<T>() => Messages.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<T>();
        internal async Task Persist()
        {
            await CharacterAtomicSave.Execute(() => Source.Context(), 42, Actor.Player.Save);
            await Pump();
        }
        internal async Task Pump() { Actor.Events.Update(.01); await Actor.Drain(); }
        public void Dispose() { Actor.Dispose(); Source.Dispose(); }
    }
}
