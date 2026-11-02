using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Network.World.Message.Model.Mail;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InboxRefreshUsesItsConfiguredStrategyAndRetriesWithAFreshTransaction(bool failFirstRead)
    {
        var f = await MailFixture();
        using (var seed = new CharacterContext(Configuration))
        {
            seed.CharacterMail.Add(new CharacterMailModel { Id = 600, RecipientId = 200, SenderId = 100,
                Subject = "Refresh", Message = "Fresh transaction", CreateTime = DateTime.UtcNow.AddMinutes(-1) });
            await seed.SaveChangesAsync();
        }
        var (recipient, inbox) = RecipientMailbox(f);
        var probe = new MailDeliveryReadProbe(failFirstRead);
        var options = new DbContextOptionsBuilder<CharacterContext>();
        options.UseConfiguration(Configuration).AddInterceptors(probe);
        // Configure this scoped policy without creating another cached internal
        // service provider for the test's strategy implementation type.
        if (Configuration.Provider == DatabaseProvider.MySql)
            new MySqlDbContextOptionsBuilder(options).ExecutionStrategy(dependencies => new MailDeliveryRetryStrategy(dependencies));
        else
            new SqliteDbContextOptionsBuilder(options).ExecutionStrategy(dependencies => new MailDeliveryRetryStrategy(dependencies));
        typeof(MailManager).GetField("deliveryContexts", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(inbox, new Func<CharacterContext>(() => new CharacterContext(options.Options)));

        await PollMailbox(recipient, inbox);
        Assert.Equal(600ul, Assert.Single(Assert.Single(BankMessages(recipient.Session).OfType<ServerMailAvailable>()).MailList).MailId);
        Assert.Equal(failFirstRead ? 2 : 1, probe.Attempts.Count);
        Assert.Equal(probe.Attempts.Count, probe.Attempts.Distinct().Count());
        await PollMailbox(recipient, inbox);
        Assert.Single(BankMessages(recipient.Session).OfType<ServerMailAvailable>());
        using var read = new CharacterContext(Configuration);
        Assert.Equal(200ul, Assert.Single(read.CharacterMail).RecipientId);
    }

    private sealed class MailDeliveryTransientException : Exception;

    private sealed class MailDeliveryRetryStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is MailDeliveryTransientException;
    }

    private sealed class MailDeliveryReadProbe(bool failFirstRead) : DbCommandInterceptor
    {
        public List<Guid> Attempts { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("character_mail", StringComparison.Ordinal))
            {
                Assert.NotNull(eventData.Context.Database.CurrentTransaction);
                Attempts.Add(eventData.Context.ContextId.InstanceId);
                if (failFirstRead && Attempts.Count == 1)
                    return ValueTask.FromException<InterceptionResult<DbDataReader>>(new MailDeliveryTransientException());
            }
            return ValueTask.FromResult(result);
        }
    }
}
