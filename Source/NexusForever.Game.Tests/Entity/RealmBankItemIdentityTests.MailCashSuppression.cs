using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MailCashReceiptCannotAcknowledgeASuppressedWalletAndHeaderFlush(bool cod)
    {
        var f = await MailFixture(); var (_, mail) = await IncomingCashMail(f, cod);
        var receipt = mail.CaptureCashReceipt(100);
        var currency = f.Currency.CaptureBankCurrency(f.Player,
            [new CurrencyManager.BankCurrencyChange(CurrencyType.Credits, 30, cod)], () => true, _ => { });
        var options = new DbContextOptionsBuilder<CharacterContext>();
        options.UseConfiguration(Configuration); options.AddInterceptors(new SuppressMailCashFlush());
        try
        {
            await using var context = new CharacterContext(options.Options);
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            receipt.Stage(context); currency.Stage(context);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.True(context.IsSaveRejected); await transaction.RollbackAsync();
        }
        finally { receipt.Abort(); currency.Abort(); }
        Assert.False(mail.HasPaidOrCollectedCurrency); Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(0, read.CharacterMail.Single().HasPaidOrCollectedCurrency);
        Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
    }

    private sealed class SuppressMailCashFlush : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
    }
}
