using System.Reflection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Achievement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Entity;
using NexusForever.Game.Housing;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network.Message.Handler.Housing;
using static NexusForever.Game.Tests.Housing.HousingRenameFeeRecoveryTests;

namespace NexusForever.Game.Tests.Housing;

public sealed class HousingDecorPurchaseTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public HousingDecorPurchaseTests() { }
    internal HousingDecorPurchaseTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Fact]
    public async Task PublicDecorPurchaseWaitsForTheCommittedFeeAndDecorBeforeAdoptionAndBroadcast()
    {
        using var f = await Create();
        Assert.True(f.Owner.Currency.CanAfford(CurrencyType.Credits, 5000));
        new ClientHousingDecorUpdateHandler().HandleMessage(f.Owner.Session, Request(f));
        Assert.True(f.Owner.Currency.CanAfford(CurrencyType.Credits, 5000));
        Assert.Empty(f.Root.GetDecor()); Assert.Empty(DecorPackets(f));
        using (var pending = f.Owner.Source.Context())
        { Assert.Equal(5000ul, pending.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount); Assert.Empty(pending.ResidenceDecor); }
        await Flush(f.Owner);
        using var stored = f.Owner.Source.Context();
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        var row = Assert.Single(stored.ResidenceDecor); Assert.Equal(201u, row.DecorInfoId);
        Assert.Equal(row.DecorId, Assert.Single(f.Root.GetDecor()).DecorId);
        Assert.Equal(row.DecorId, Assert.Single(Assert.Single(DecorPackets(f)).DecorData).DecorId);
    }

    [Theory]
    [InlineData("residence_decor")]
    [InlineData("character_currency")]
    public async Task RejectedHalfOfThePurchaseRollsBackTheWholeFeeAndDecorAndRetriesOnce(string table)
    {
        using var f = await Create();
        using (var setup = f.Owner.Source.Context())
            await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite()
                ? $"CREATE TRIGGER reject_decor_purchase BEFORE {(table == "residence_decor" ? "INSERT" : "UPDATE")} ON {table} BEGIN SELECT RAISE(ABORT, 'owned decor fee rejection'); END"
                : $"CREATE TRIGGER reject_decor_purchase BEFORE {(table == "residence_decor" ? "INSERT" : "UPDATE")} ON {table} FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned decor fee rejection'");
        new ClientHousingDecorUpdateHandler().HandleMessage(f.Owner.Session, Request(f));
        await Flush(f.Owner);
        using (var rejected = f.Owner.Source.Context())
        {
            Assert.Empty(rejected.ResidenceDecor); Assert.Equal(5000ul, rejected.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
            Assert.Empty(f.Root.GetDecor()); Assert.Empty(DecorPackets(f));
            Assert.Throws<InvalidOperationException>(() => f.Root.Save(rejected));
        }
        using (var repair = f.Owner.Source.Context()) await repair.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_decor_purchase");
        await Flush(f.Owner); await Flush(f.Owner);
        using var stored = f.Owner.Source.Context();
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Single(stored.ResidenceDecor); Assert.Single(f.Root.GetDecor()); Assert.Single(DecorPackets(f));
    }

    [Fact]
    public async Task PersonalHomeUsesTheSameCoupledPurchaseWithoutCommunityPermission()
    {
        using var f = await Create(personal: true);
        new ClientHousingDecorUpdateHandler().HandleMessage(f.Owner.Session, Request(f)); await Flush(f.Owner);
        using var stored = f.Owner.Source.Context();
        Assert.Equal(101ul, Assert.Single(stored.ResidenceDecor).Id);
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Single(f.Root.GetDecor()); Assert.Single(DecorPackets(f));
    }

    [Fact]
    public async Task RejectedPurchaseKeepsCharacterAchievementAtomicAndDefersGuildProgressUntilCommit()
    {
        using var f = await Create();
        var (guildAchievements, _) = InstallAchievements(f);
        using (var setup = f.Owner.Source.Context())
            await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite()
                ? "CREATE TRIGGER reject_decor_achievement BEFORE INSERT ON residence_decor BEGIN SELECT RAISE(ABORT, 'owned decor rejection'); END"
                : "CREATE TRIGGER reject_decor_achievement BEFORE INSERT ON residence_decor FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned decor rejection'");
        new ClientHousingDecorUpdateHandler().HandleMessage(f.Owner.Session, Request(f)); await Flush(f.Owner);
        using (var rejected = f.Owner.Source.Context())
        {
            Assert.Empty(rejected.CharacterAchievement); Assert.Empty(rejected.ResidenceDecor);
            Assert.Equal(5000ul, rejected.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
            Assert.Empty(guildAchievements.Capture());
            await rejected.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_decor_achievement");
        }
        await Flush(f.Owner); await Flush(f.Owner);
        using var stored = f.Owner.Source.Context();
        Assert.Equal(1u, Assert.Single(stored.CharacterAchievement).ProgressState);
        Assert.Equal(1u, Assert.Single(guildAchievements.Capture()).Progress);
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Single(stored.ResidenceDecor);
    }

    [Fact]
    public async Task RejectedCommittedGuildEffectResumesWithoutRepeatingThePaidPurchase()
    {
        using var f = await Create(); bool reject = true;
        var (guildAchievements, attempts) = InstallAchievements(f, () => reject);
        new ClientHousingDecorUpdateHandler().HandleMessage(f.Owner.Session, Request(f)); await Flush(f.Owner);
        Assert.True(attempts() > 0); Assert.Empty(guildAchievements.Capture());
        Assert.Empty(f.Root.GetDecor()); Assert.Empty(DecorPackets(f));
        using (var pending = f.Owner.Source.Context())
        {
            Assert.Equal(4900ul, pending.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
            Assert.Single(pending.ResidenceDecor); Assert.Equal(1u, Assert.Single(pending.CharacterAchievement).ProgressState);
        }
        reject = false; await Flush(f.Owner); await Flush(f.Owner);
        using var stored = f.Owner.Source.Context();
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Single(stored.ResidenceDecor); Assert.Single(f.Root.GetDecor()); Assert.Single(DecorPackets(f));
        Assert.Equal(1u, Assert.Single(guildAchievements.Capture()).Progress);
        Assert.Equal(1u, Assert.Single(stored.CharacterAchievement).ProgressState);
    }

    private static (GuildAchievementManager Guild, Func<int> Attempts) InstallAchievements(Frame f, Func<bool> reject = null)
    {
        var characterInfo = new AchievementInfo(new AchievementEntry { Id = 500, AchievementTypeId = (uint)AchievementType.HousingDecorPurchase, RequiredProgress = 2 });
        var guildInfo = new AchievementInfo(new AchievementEntry { Id = 501, AchievementTypeId = (uint)AchievementType.HousingDecorPurchase, RequiredProgress = 2, Flags = (uint)AchievementFlags.Guild });
        var registry = RecordingDispatchProxy<IGlobalAchievementManager>.Create(out var globals);
        globals.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => (ushort)args[0] == 500 ? characterInfo : guildInfo);
        globals.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args => (AchievementType)args[0] == AchievementType.HousingDecorPurchase ? new IAchievementInfo[] { characterInfo } : []);
        globals.SetMethodHandler(nameof(IGlobalAchievementManager.GetGuildAchievements), args => (AchievementType)args[0] == AchievementType.HousingDecorPurchase ? new IAchievementInfo[] { guildInfo } : []);
        var guild = RecordingDispatchProxy<IGuild>.Create(out var guildProxy); guildProxy.SetProperty(nameof(IGuild.Id), 77ul);
        var guildAchievements = new GuildAchievementManager(guild, globalAchievementManager: registry);
        guildProxy.SetProperty(nameof(IGuild.AchievementManager), guildAchievements);
        int attempts = 0;
        if (reject != null)
        {
            var forwarding = RecordingDispatchProxy<IGuildAchievementManager>.Create(out var forwarded);
            forwarded.SetMethodHandler(nameof(IGuildAchievementManager.CheckAchievements), args =>
            {
                attempts++;
                if (reject()) throw new IOException("Owned guild effect rejected before mutation.");
                guildAchievements.CheckAchievements((IPlayer)args[0], (AchievementType)args[1], (uint)args[2], (uint)args[3], (uint)args[4]);
                return null;
            });
            guildProxy.SetProperty(nameof(IGuild.AchievementManager), forwarding);
        }
        ((RecordingDispatchProxy<IGuildManager>)(object)f.Owner.Actor.Player.GuildManager).SetProperty(nameof(IGuildManager.Guild), guild);
        using (var read = f.Owner.Source.Context())
            f.Owner.Actor.Set(nameof(Player.AchievementManager), new CharacterAchievementManager(f.Owner.Actor.Player,
                read.Character.AsNoTracking().Single(row => row.Id == 42), globalAchievementManager: registry));
        return (guildAchievements, () => attempts);
    }

    [Fact]
    public async Task LostPrimaryAcknowledgementCannotChargeOrCreateDecorTwice()
    {
        var fault = new LostAcknowledgement(); using var f = await Create(fault);
        new ClientHousingDecorUpdateHandler().HandleMessage(f.Owner.Session, Request(f)); await Flush(f.Owner); await Flush(f.Owner);
        Assert.True(fault.Fired);
        using var stored = f.Owner.Source.Context();
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Single(stored.ResidenceDecor); Assert.Single(f.Root.GetDecor()); Assert.Single(DecorPackets(f));
    }

    [Fact]
    public async Task FailedDecorPublicationResumesWithoutAnotherFeeOrDecor()
    {
        using var f = await Create(); bool reject = true; var accepted = new List<object>();
        f.Owner.Messages.SetMethodHandler("EnqueueMessageEncrypted", arguments =>
        {
            if (arguments[0] is ServerHousingResidenceDecor && reject) throw new IOException("Owned decor enqueue rejection before acceptance.");
            accepted.Add(arguments[0]); return null;
        });
        new ClientHousingDecorUpdateHandler().HandleMessage(f.Owner.Session, Request(f)); await Flush(f.Owner);
        using (var stored = f.Owner.Source.Context())
        { Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount); Assert.Single(stored.ResidenceDecor); }
        Assert.Empty(accepted.OfType<ServerHousingResidenceDecor>()); Assert.Single(f.Root.GetDecor());
        int wallets = accepted.OfType<ServerCombatReward>().Count(); reject = false;
        await Flush(f.Owner); await Flush(f.Owner);
        Assert.Single(accepted.OfType<ServerHousingResidenceDecor>()); Assert.Equal(wallets, accepted.OfType<ServerCombatReward>().Count());
    }

    [Fact]
    public async Task AReplacementPlayerBeforePreparationCannotSpendTheOldActorsMoney()
    {
        using var f = await Create();
        new ClientHousingDecorUpdateHandler().HandleMessage(f.Owner.Session, Request(f));
        f.Owner.Messages.SetProperty(nameof(IPlayerSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        await Flush(f.Owner);
        using var stored = f.Owner.Source.Context();
        Assert.Equal(5000ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Empty(stored.ResidenceDecor); Assert.Empty(f.Root.GetDecor()); Assert.Empty(DecorPackets(f));
    }

    [Fact]
    public async Task MutableRequestAndLaterTemplatePriceDoNotChangeTheAcceptedPurchase()
    {
        using var f = await Create(); var request = Request(f);
        new ClientHousingDecorUpdateHandler().HandleMessage(f.Owner.Session, request);
        Set(request.DecorUpdates.Single(), nameof(DecorInfo.DecorInfoId), 999u);
        Set(request.DecorUpdates.Single(), nameof(DecorInfo.DecorData), 123u);
        f.Owner.Source.Source.Tables.HousingDecorInfo.GetEntry(201).Cost = 200;
        await Flush(f.Owner);
        using var stored = f.Owner.Source.Context(); var row = Assert.Single(stored.ResidenceDecor);
        Assert.Equal(201u, row.DecorInfoId); Assert.Equal(0u, row.DecorData);
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(value => value.CurrencyId == 1).Amount);
    }

    [Fact]
    public async Task SqlPermissionLossBeforeEnrollmentCannotBeBypassedByTheCachedRank()
    {
        using var f = await Create();
        new ClientHousingDecorUpdateHandler().HandleMessage(f.Owner.Session, Request(f));
        using (var revoked = f.Owner.Source.Context())
        { foreach (var rank in revoked.GuildRank) rank.Permission = 0; await revoked.SaveChangesAsync(); }
        await Flush(f.Owner);
        using var stored = f.Owner.Source.Context();
        Assert.Equal(5000ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Empty(stored.ResidenceDecor); Assert.Empty(f.Root.GetDecor()); Assert.Empty(DecorPackets(f));
    }

    private async Task<Frame> Create(IInterceptor interceptor = null, bool personal = false)
    {
        var owner = await new HousingRenameFeeRecoveryTests(options).Create(interceptor, personalHome: personal);
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)owner.Source.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.HousingDecorInfo), PlayerBindPointTests.Table(
            new HousingDecorInfoEntry { Id = 201, CostCurrencyTypeId = 1, Cost = 100 }));
        var map = new ResidenceMapInstance(null, null, null, owner.Residences, owner.Source.Source.Tables, null, null);
        var root = (Residence)(personal ? owner.Residences.GetResidence(101) : owner.Community.Residence);
        var oldMap = owner.Actor.Player.Map; var oldRootMap = root.Map;
        map.Initialise(root);
        ((IDictionary<uint, IGridEntity>)typeof(BaseMap).GetField("entities", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(map)!)
            [owner.Actor.Player.Guid] = owner.Actor.Player;
        owner.Actor.Set(nameof(IPlayer.Map), map);
        return new(owner, root, map, oldMap, oldRootMap);
    }
    private static ClientHousingDecorUpdate Request(Frame f)
    {
        var update = new DecorInfo(); Set(update, nameof(DecorInfo.DecorInfoId), 201u); Set(update, nameof(DecorInfo.DecorType), DecorType.Crate);
        update.TargetResidence.ResidenceId = f.Root.Id; update.TargetResidence.RealmId = 1;
        var request = new ClientHousingDecorUpdate(); Set(request, nameof(ClientHousingDecorUpdate.Operation), DecorUpdateOperation.Create);
        request.DecorUpdates.Add(update); return request;
    }
    private static IEnumerable<ServerHousingResidenceDecor> DecorPackets(Frame f)
        => f.Owner.Messages.GetInvocations("EnqueueMessageEncrypted").Select(row => row.Arguments[0]).OfType<ServerHousingResidenceDecor>();
    private static void Set(object owner, string property, object value) => owner.GetType().GetProperty(property)!.SetValue(owner, value);
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        internal bool Fired;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        {
            if (!Fired && data.Context!.ChangeTracker.Entries<NexusForever.Database.Character.Model.ResidenceDecor>().Any())
            { Fired = true; return Task.FromException(new IOException("Owned decor acknowledgement loss.")); }
            return Task.CompletedTask;
        }
    }
    private sealed record Frame(Fixture Owner, Residence Root, ResidenceMapInstance Map, IBaseMap OldMap, IResidenceMapInstance OldRootMap) : IDisposable
    {
        public void Dispose() { Owner.Actor.Set(nameof(IPlayer.Map), OldMap); Root.Map = OldRootMap; Owner.Dispose(); }
    }
}
