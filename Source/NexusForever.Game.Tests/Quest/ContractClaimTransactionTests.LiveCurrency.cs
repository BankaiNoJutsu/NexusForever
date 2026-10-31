using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Quest;

public sealed partial class ContractClaimTransactionTests
{
    [Theory]
    [InlineData(0ul)] [InlineData(1250ul)]
    public void OrdinaryCurrencyOverflowIsCheckedAndCappedGrantsNeverWrapBeforeClamping(ulong cap)
    {
        using var f = new LiveCredit(characterOptions, authOptions, cap: cap); Assert.True(f.Ticket.Cancel());
        if (cap == 0)
        {
            Assert.Throws<OverflowException>(() => f.Manager.CurrencyAddAmount(CurrencyType.Credits, ulong.MaxValue));
            Assert.Equal(1234ul, f.Wallet.Amount); Assert.Empty(f.Packets); Assert.Empty(f.Achievements.Invocations);
        }
        else
        {
            f.Manager.CurrencyAddAmount(CurrencyType.Credits, ulong.MaxValue); Assert.Equal(1250ul, f.Wallet.Amount);
            Assert.Equal(1250ul, Assert.Single(f.Packets).NewValue); Assert.Single(f.Achievements.GetInvocations("CheckAchievements"));
        }
    }

    [Fact]
    public async Task PacketCallbackInvalidationStopsFurtherGameplayAndDeferredEffectsAfterTheCommittedBalancePacket()
    {
        using var f = new LiveCredit(characterOptions, authOptions); f.Manager.CurrencyAddAmount(CurrencyType.Credits, 7);
        var result = await f.Commit(); Assert.True(f.Ticket.Adopt(result));
        f.Source.Inventory.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), arguments => { f.Manager.InvalidateContractCredit(); return null; });
        Assert.False(f.Ticket.Publish()); Assert.True(f.Manager.RequiresContractCurrencyReload); Assert.Single(f.Packets);
        Assert.Empty(f.Achievements.Invocations); Assert.Equal(2501234ul, f.Wallet.Amount);
    }

    [Fact]
    public async Task ReentrantOrdinaryGrantsJoinTheSameQueueWithoutBypassingPublicationAdmissionOrChangingTheirOrder()
    {
        using var f = new LiveCredit(characterOptions, authOptions); f.Manager.CurrencyAddAmount(CurrencyType.Credits, 7);
        var result = await f.Commit(); Assert.True(f.Ticket.Adopt(result)); bool packet = false, achievement = false;
        f.Source.Inventory.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), arguments =>
        {
            if (!packet) { packet = true; f.Manager.CurrencyAddAmount(CurrencyType.Credits, 3); } return null;
        });
        f.Achievements.SetMethodHandler("CheckAchievements", arguments =>
        {
            if (!achievement) { achievement = true; f.Manager.CurrencyAddAmount(CurrencyType.Credits, 5); } return null;
        });
        Assert.True(f.Ticket.Publish()); Assert.Equal(2501249ul, f.Wallet.Amount);
        Assert.Equal(new ulong[] { 2501234, 2501241, 2501244, 2501249 }, f.Packets.Select(row => row.NewValue));
        Assert.Equal(4, f.Achievements.GetInvocations("CheckAchievements").Count); Assert.False(f.Manager.HasPendingContractCurrency);
    }

    [Fact]
    public void ADifferentClaimCannotReserveAnOrdinaryDirtyCurrencyBaseline()
    {
        using var f = new LiveCredit(characterOptions, authOptions); Assert.True(f.Ticket.Cancel());
        f.Manager.CurrencyAddAmount(CurrencyType.Credits, 7);
        Assert.False(f.Manager.TryReserveContractCredit(f.Catalog, f.Quote, Guid.NewGuid().ToString("N"), f.Database, out _));
        Assert.False(f.Manager.HasPendingContractCurrency); Assert.False(f.Manager.RequiresContractCurrencyReload);
        Assert.Equal(1241ul, f.Wallet.Amount); using var read = f.Source.Character(); Assert.Equal(1234ul, read.CharacterCurrency.Single().Amount);
    }

    private sealed class LiveCredit : IDisposable
    {
        internal readonly Fixture Source;
        internal readonly CurrencyManager Manager;
        internal readonly ContractRewardAwardCatalog Catalog;
        internal readonly ContractRewardClaimQuote Quote;
        internal readonly CharacterChallengeRewardCommand Command;
        internal readonly CurrencyManager.ContractCreditReservation Ticket;
        internal readonly RecordingDispatchProxy<ICharacterAchievementManager> Achievements;
        internal readonly string Database;
        internal LiveCredit(DbContextOptions<NexusForever.Database.Character.CharacterContext> character,
            DbContextOptions<NexusForever.Database.Auth.AuthContext> auth, bool absent = false, ulong cap = 0)
        {
            Source = new(character, auth, 69, 0);
            ((RecordingDispatchProxy<IGameTableManager>)(object)Source.Tables).SetProperty(nameof(IGameTableManager.CurrencyType),
                PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = cap }));
            Catalog = new(Source.Tables);
            Quote = ContractRewardClaimQuote.Capture(Catalog, 42, [Source.Before], Source.Before.TrackId, 7, 1, 69, 0);
            using var read = Source.Character();
            if (absent) read.CharacterCurrency.ExecuteDelete();
            Database = NexusForever.Game.Entity.Item.GetRewardDatabaseKey(read);
            var model = new CharacterModel { Id = 42 }; foreach (var row in read.CharacterCurrency.AsNoTracking()) model.Currency.Add(row);
            Manager = new(Source.Tables); Manager.Initialise(Source.Inventory.Player, model);
            Source.Inventory.PlayerProxy.SetProperty(nameof(IPlayer.CurrencyManager), Manager);
            var achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var proxy); Achievements = proxy;
            Source.Inventory.PlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);
            Assert.True(Manager.TryReserveContractCredit(Catalog, Quote, Source.Command.Identity, Database, out Ticket));
            Command = CharacterChallengeRewardCommand.ClaimContract(42, 27, 1, Source.Command.Identity, DateTime.UtcNow,
                Catalog, [Source.Before], Quote, Ticket.Before);
        }
        internal Currency Wallet => Assert.IsType<Currency>(Assert.Single(Manager));
        internal IEnumerable<ServerCombatReward> Packets => Source.Inventory.Messages.OfType<ServerCombatReward>();
        internal async Task<CharacterChallengeRewardExecution> Commit()
        {
            using var items = Source.Reserve(); Ticket.Begin(Command, items);
            return await Source.Executor.ExecuteAsync(Command, items);
        }
        public void Dispose() { if (!Ticket.Cancel()) Manager.InvalidateContractCredit(); Source.Dispose(); }
    }

    [Theory]
    [InlineData(false, 0ul)] [InlineData(true, 0ul)] [InlineData(false, 1250ul)] [InlineData(true, 1250ul)]
    public async Task ARealCommittedCreditAdoptsAndPublishesOnceWithoutMakingANormalSaveDirty(bool absent, ulong cap)
    {
        using var f = new LiveCredit(characterOptions, authOptions, absent, cap);
        Assert.Empty(f.Packets); Assert.False(f.Manager.CanAfford(CurrencyType.Credits, 1)); Assert.True(f.Manager.CanAfford(CurrencyType.Credits, 0));
        var result = await f.Commit(); Assert.Empty(f.Packets); Assert.True(f.Ticket.Adopt(result)); Assert.Empty(f.Packets);
        ulong after = cap == 0 ? 2500000ul + (absent ? 0ul : 1234ul) : cap;
        Assert.Equal(after, f.Wallet.Amount); Assert.False(f.Wallet.NeedsPersistenceSave);
        Assert.True(f.Ticket.Publish()); Assert.False(f.Ticket.Publish()); Assert.False(f.Ticket.Adopt(result));
        Assert.Equal(after, Assert.Single(f.Packets).NewValue); Assert.Single(f.Achievements.GetInvocations("CheckAchievements"));
        Assert.Single(f.Source.Inventory.QuestProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.False(f.Manager.HasPendingContractCurrency); Assert.True(f.Manager.CanAfford(CurrencyType.Credits, after));
        using var read = f.Source.Character(); f.Manager.Save(read); Assert.False(read.ChangeTracker.HasChanges());
    }

    [Theory]
    [InlineData(0ul)] [InlineData(1250ul)]
    public async Task LaterSemanticGrantsAndSpendsWaitAndUseTheSettledCapAndActualEarnedAmount(ulong cap)
    {
        using var f = new LiveCredit(characterOptions, authOptions, cap: cap);
        f.Manager.CurrencyAddAmount(CurrencyType.Credits, 7, true); f.Manager.CurrencySubtractAmount(CurrencyType.Credits, 3);
        Assert.Equal(1234ul, f.Wallet.Amount); Assert.Empty(f.Packets); Assert.Empty(f.Achievements.Invocations);
        var result = await f.Commit(); Assert.True(f.Ticket.Adopt(result)); Assert.True(f.Ticket.Publish());
        ulong after = cap == 0 ? 2501238ul : 1247ul;
        Assert.Equal(after, f.Wallet.Amount); Assert.True(f.Wallet.NeedsPersistenceSave);
        Assert.Equal(cap == 0 ? 2 : 1, f.Achievements.GetInvocations("CheckAchievements").Count);
        Assert.Equal(3, f.Packets.Count());
        using (var read = f.Source.Character()) Assert.Equal(cap == 0 ? 2501234ul : 1250ul, read.CharacterCurrency.Single().Amount);
        await NexusForever.Database.Character.CharacterAtomicSave.Execute(f.Source.Character, 42, f.Manager.Save);
        using var final = f.Source.Character(); Assert.Equal(after, final.CharacterCurrency.Single().Amount); Assert.Single(final.CharacterContractRewardClaim);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CancelingBeforeExecutionReplaysOrdinaryIntentAgainstTheOriginalWallet(bool absent)
    {
        using var f = new LiveCredit(characterOptions, authOptions, absent);
        f.Manager.CurrencyAddAmount(CurrencyType.Credits, 7); Assert.True(f.Ticket.Cancel()); Assert.False(f.Ticket.Cancel());
        Assert.Equal(absent ? 7ul : 1241ul, f.Wallet.Amount); Assert.True(f.Wallet.NeedsPersistenceSave);
        Assert.Single(f.Packets); Assert.Single(f.Achievements.GetInvocations("CheckAchievements"));
        using var read = f.Source.Character(); Assert.Empty(read.CharacterContractRewardClaim); Assert.Empty(read.CharacterContractAccountOutbox);
    }

    [Theory]
    [InlineData("save")] [InlineData("frozen")] [InlineData("absolute")] [InlineData("second")]
    public void APendingCreditBlocksCompetingSaveOrAbsoluteMutationBeforeAnySqlEnrollment(string mode)
    {
        using var f = new LiveCredit(characterOptions, authOptions);
        using var context = f.Source.Character();
        if (mode == "save") { Assert.Throws<InvalidOperationException>(() => f.Manager.Save(context)); Assert.True(context.IsSaveRejected); }
        if (mode == "frozen") Assert.Throws<InvalidOperationException>(() => f.Manager.CaptureNormalSave(new object()));
        if (mode == "absolute") Assert.Throws<InvalidOperationException>(() => f.Wallet.Amount = 900);
        if (mode == "second") Assert.False(f.Manager.TryReserveContractCredit(f.Catalog, f.Quote, Guid.NewGuid().ToString("N"), f.Database, out _));
        Assert.False(context.ChangeTracker.HasChanges()); Assert.Empty(f.Packets); Assert.True(f.Ticket.Cancel());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void DeferredOperationsMustBeValidForBothCommitAndRollbackBeforeEnteringTheQueue(bool subtract)
    {
        using var f = new LiveCredit(characterOptions, authOptions);
        Assert.Throws<OverflowException>(() =>
        {
            if (subtract) f.Manager.CurrencySubtractAmount(CurrencyType.Credits, 1235);
            else f.Manager.CurrencyAddAmount(CurrencyType.Credits, ulong.MaxValue - 1234);
        });
        Assert.True(f.Ticket.Cancel()); Assert.Equal(1234ul, f.Wallet.Amount); Assert.Empty(f.Packets);
    }

    [Fact]
    public async Task AnExecutionRecordWithoutItsPrivateExecutorProofCannotAdoptMoneyOrReplayQueuedIntent()
    {
        using var f = new LiveCredit(characterOptions, authOptions); f.Manager.CurrencyAddAmount(CurrencyType.Credits, 7);
        var result = await f.Commit(); Assert.False(f.Ticket.Adopt(result with { Changed = result.Changed }));
        Assert.True(f.Manager.RequiresContractCurrencyReload); Assert.False(f.Ticket.Publish()); Assert.False(f.Ticket.Cancel());
        Assert.Equal(1234ul, f.Wallet.Amount); Assert.Empty(f.Packets); Assert.Empty(f.Achievements.Invocations);
    }

    [Theory]
    [InlineData("account")] [InlineData("account-id")] [InlineData("session")] [InlineData("cap")] [InlineData("character")] [InlineData("invalidated")]
    public async Task AChangedLiveOwnerCannotPublishAnotherActorsCommittedCredit(string changed)
    {
        using var f = new LiveCredit(characterOptions, authOptions); var result = await f.Commit();
        if (changed == "account") f.Source.Inventory.PlayerProxy.SetProperty(nameof(IPlayer.Account), RecordingDispatchProxy<IAccount>.Create(out _));
        if (changed == "account-id") ((RecordingDispatchProxy<IAccount>)(object)f.Source.Inventory.Player.Account).SetProperty(nameof(IAccount.Id), 28u);
        if (changed == "session") f.Source.Inventory.PlayerProxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        if (changed == "cap") f.Wallet.Entry.CapAmount = 1300;
        if (changed == "character") f.Source.Inventory.PlayerProxy.SetProperty(nameof(IPlayer.CharacterId), 43ul);
        if (changed == "invalidated") f.Manager.InvalidateContractCredit();
        Assert.False(f.Ticket.Adopt(result)); Assert.True(f.Manager.RequiresContractCurrencyReload); Assert.Empty(f.Packets); Assert.Empty(f.Achievements.Invocations);
    }

    [Fact]
    public async Task PublicationHoldsAdmissionUntilPacketAndAchievementCallbacksAndDeferredIntentHaveFinished()
    {
        using var f = new LiveCredit(characterOptions, authOptions); f.Manager.CurrencyAddAmount(CurrencyType.Credits, 7);
        var result = await f.Commit(); Assert.True(f.Ticket.Adopt(result)); int callbacks = 0;
        f.Source.Inventory.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), arguments =>
        {
            ++callbacks; Assert.True(f.Manager.HasPendingContractCurrency);
            Assert.False(f.Manager.TryReserveContractCredit(f.Catalog, f.Quote, Guid.NewGuid().ToString("N"), f.Database, out _));
            using var escape = f.Source.Character(); Assert.Throws<InvalidOperationException>(() => f.Manager.Save(escape)); return null;
        });
        Assert.True(f.Ticket.Publish()); Assert.Equal(2, callbacks); Assert.False(f.Manager.HasPendingContractCurrency);
        Assert.Equal(2501241ul, f.Wallet.Amount); Assert.True(f.Wallet.NeedsPersistenceSave);
    }
}
