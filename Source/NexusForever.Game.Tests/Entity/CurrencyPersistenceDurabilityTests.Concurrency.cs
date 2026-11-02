using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using Fixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class CurrencyPersistenceDurabilityTests
{
    public static IEnumerable<object[]> CurrencyRaceCases()
    {
        foreach (string path in new[] { "normal", "frozen-normal", "bank", "frozen-bank" })
            foreach (bool asynchronous in new[] { false, true })
                foreach (string fault in new[] { "amount", "delete", "none" })
                    yield return [path, asynchronous, fault];
    }

    [Theory]
    [MemberData(nameof(CurrencyRaceCases))]
    public async Task CurrencyWritesRetainACompetingCommittedBalance(string path, bool asynchronous, string fault)
    {
        using var f = new Fixture(options); var wallet = Wallet(f); object token = new();
        bool frozen = path.StartsWith("frozen-", StringComparison.Ordinal), bank = path.EndsWith("bank", StringComparison.Ordinal);
        using var identity = f.Context();
        using var lease = frozen ? QuestSaveScope.AcquireFrozen(wallet, token, GameItem.GetRewardDatabaseKey(identity), 42) : null;
        var bankOwner = bank ? frozen ? wallet.CaptureFrozenBankChange(90, token) : wallet.CaptureBankChange(90) : null;
        Action<CharacterContext> stage = bankOwner != null ? bankOwner.Stage
            : frozen ? wallet.CaptureNormalSave(token) : wallet.Save;
        int writes = 0;
        var race = new BeforeCurrencyTransaction(() =>
        {
            using var external = f.Context();
            if (fault == "amount") external.CharacterCurrency.ExecuteUpdate(set => set.SetProperty(row => row.Amount, 107ul));
            if (fault == "delete") external.CharacterCurrency.ExecuteDelete();
            writes++;
        });
        using (var save = f.Intercepted(race))
        {
            save.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
            stage(save);
            save.Character.Single().TimePlayedTotal = 123;
            Exception error = asynchronous ? await Record.ExceptionAsync(() => save.SaveChangesAsync())
                : Record.Exception(() => save.SaveChanges());
            Assert.Equal(1, writes);
            if (fault == "none") Assert.Null(error); else Assert.IsType<DbUpdateConcurrencyException>(error);
        }
        if (fault == "none" && bankOwner != null) { bankOwner.Adopt(); bankOwner.End(); }
        else bankOwner?.End();
        using var read = f.Context();
        Assert.Equal(fault == "none" ? 123u : 0u, read.Character.Single().TimePlayedTotal);
        if (fault == "delete") Assert.Empty(read.CharacterCurrency);
        else Assert.Equal(fault == "amount" ? 107ul : bank ? 90ul : 125ul, Assert.Single(read.CharacterCurrency).Amount);
        Assert.Equal(bank && fault == "none" ? 90ul : 125ul, wallet.Amount);
        if (fault != "none")
        {
            Assert.True(wallet.NeedsPersistenceSave);
            lease?.Dispose();
            using var retry = f.Context();
            Assert.Throws<InvalidOperationException>(() => wallet.Save(retry));
            Assert.True(retry.IsSaveRejected);
        }
        else Assert.False(wallet.NeedsPersistenceSave);
    }

    private sealed class BeforeCurrencyTransaction(Action write) : DbTransactionInterceptor
    {
        private int entered;
        public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
        {
            if (Interlocked.Exchange(ref entered, 1) == 0) write();
            return result;
        }
        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
            => new(TransactionStarting(connection, eventData, result));
    }
}
