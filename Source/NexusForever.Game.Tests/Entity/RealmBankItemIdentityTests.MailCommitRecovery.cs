using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task DefaultBankExecutionResolvesACommitTransportFailureWithoutRepaying(bool cod, bool committed)
    {
        var fault = new MailCommitTransportFault(committed);
        var f = await MailCommitFixture(fault); var (manager, mail) = await IncomingCashMail(f, cod);
        if (cod) manager.MailPayCod(mail.Id); else manager.MailTakeCash(mail.Id, 1);
        Assert.Empty(BankMessages(f.Player.Session));
        await WaitForBankMove(f.Inventory);
        Assert.False(f.Inventory.RequiresAuthoritativeReload);
        Assert.Equal(2, fault.Contexts.Count); Assert.Equal(2, fault.Contexts.Distinct().Count());
        Assert.True(mail.HasPaidOrCollectedCurrency);
        ulong expected = cod ? 70ul : 130ul;
        Assert.Equal(expected, Assert.Single(f.Currency).Amount);
        var packets = BankMessages(f.Player.Session); Assert.Equal(2, packets.Length);
        var wallet = Assert.IsType<ServerCombatReward>(packets[0]); Assert.Equal((byte)0, wallet.Stat); Assert.Equal(expected, wallet.NewValue);
        var result = Assert.IsType<ServerMailResult>(packets[1]); Assert.Equal(GenericError.Ok, result.Result); Assert.Equal(mail.Id, result.MailId);
        Assert.Equal(cod ? MailResultAction.PayCashOnDelivery : MailResultAction.TakeCash, result.Action);
        if (cod) manager.MailPayCod(mail.Id); else manager.MailTakeCash(mail.Id, 1);
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerCombatReward>());
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>(), row => row.Result == GenericError.Ok);
        using (var save = new CharacterContext(Configuration)) { f.Currency.Save(save); manager.Save(save); await save.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration);
        Assert.Equal(expected, Assert.Single(read.CharacterCurrency).Amount);
        Assert.Equal(1, read.CharacterMail.Single(row => row.Id == 600).HasPaidOrCollectedCurrency);
        Assert.Equal(cod ? 2 : 1, read.CharacterMail.Count());
        if (cod) Assert.Equal(701ul, read.CharacterMail.Single(row => row.Id != 600).Id);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DefaultBankExecutionDoesNotReplayAnUnrelatedSaveOrCommitFailure(bool duringSave)
    {
        var commit = new MailCommitTransportFault(false, transport: false);
        var flush = new MailFlushTransportFault();
        var f = await MailCommitFixture(duringSave ? flush : commit);
        var (manager, mail) = await IncomingCashMail(f, false);
        manager.MailTakeCash(mail.Id, 1); await WaitForBankMove(f.Inventory);
        Assert.True(f.Inventory.RequiresAuthoritativeReload);
        Assert.Equal(duringSave ? 1 : 0, flush.Calls); Assert.Equal(duringSave ? 0 : 1, commit.Contexts.Count);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerCombatReward>());
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(100ul, read.CharacterCurrency.Single().Amount); Assert.Equal(0, read.CharacterMail.Single().HasPaidOrCollectedCurrency);
    }

    [Fact]
    public async Task CommitTransportRecoveryRejectsAMixedReceiptAndLaterWalletGraph()
    {
        var fault = new MailCommitTransportFault(false, afterRollback: async () =>
        {
            using var other = new CharacterContext(Configuration);
            other.CharacterCurrency.Single().Amount = 130; await other.SaveChangesAsync();
        });
        var f = await MailCommitFixture(fault); var (manager, mail) = await IncomingCashMail(f, false);
        manager.MailTakeCash(mail.Id, 1); await WaitForBankMove(f.Inventory);
        Assert.True(f.Inventory.RequiresAuthoritativeReload);
        Assert.Single(fault.Contexts);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerCombatReward>());
        using var read = new CharacterContext(Configuration);
        Assert.Equal(130ul, read.CharacterCurrency.Single().Amount); Assert.Equal(0, read.CharacterMail.Single().HasPaidOrCollectedCurrency);
    }

    private Task<MailState> MailCommitFixture(IInterceptor fault) => MailFixture(command =>
    {
        var options = new DbContextOptionsBuilder<CharacterContext>(); options.UseConfiguration(Configuration); options.AddInterceptors(fault);
        typeof(RealmBankManager.BankSaveCommand).GetField("contexts", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(command, new Func<CharacterContext>(() => new CharacterContext(options.Options)));
        return command.ExecuteAsync();
    });

    private static MySqlException MailCommitTransportException()
    {
        var error = (MySqlException)typeof(MySqlException).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
            [typeof(MySqlErrorCode), typeof(string), typeof(string), typeof(Exception)], null)!
            .Invoke([(MySqlErrorCode)0, null, "Owned commit response transport failure.", new EndOfStreamException("Owned response ended.")]);
        Assert.False(error.IsTransient); return error;
    }

    private sealed class MailCommitTransportFault(bool committed, bool transport = true, Func<Task> afterRollback = null) : DbTransactionInterceptor
    {
        public List<Guid> Contexts { get; } = [];
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Contexts.Add(eventData.Context.ContextId.InstanceId);
            if (Contexts.Count == 1)
            {
                // Commit the actual store before EF receives any confirmation,
                // or leave the rollback alternative for the same ambiguous error.
                if (committed) await transaction.CommitAsync(cancellationToken);
                if (afterRollback != null) { await transaction.RollbackAsync(cancellationToken); await afterRollback(); }
                if (transport) throw MailCommitTransportException();
                throw new InvalidOperationException("Owned non-transport commit rejection.");
            }
            return result;
        }
    }

    private sealed class MailFlushTransportFault : SaveChangesInterceptor
    {
        public int Calls { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        { ++Calls; throw MailCommitTransportException(); }
    }
}
