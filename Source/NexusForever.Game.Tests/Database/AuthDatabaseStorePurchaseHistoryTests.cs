using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Configuration.Model;

namespace NexusForever.Game.Tests.Database;

public sealed class AuthDatabaseStorePurchaseHistoryTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"NexusForeverAuthHistoryTests_{Guid.NewGuid():N}");

    [Fact]
    public void TryAddStorePurchaseHistory_RecordsOnceAndRespectsVelocityLimit()
    {
        Directory.CreateDirectory(directory);
        DatabaseConnectionString connectionString = CreateConnectionString();
        uint accountId = CreateAccount(connectionString);
        var database = new AuthDatabase();
        database.Initialise(connectionString);

        var purchasedUtc = new DateTime(2026, 6, 20, 17, 8, 8, DateTimeKind.Utc);
        var first = new AccountStorePurchaseHistoryModel
        {
            AccountId    = accountId,
            OfferId      = 2278u,
            CurrencyId   = 6,
            Price        = 1ul,
            PurchasedUtc = purchasedUtc
        };
        var second = new AccountStorePurchaseHistoryModel
        {
            AccountId    = accountId,
            OfferId      = 2278u,
            CurrencyId   = 6,
            Price        = 1ul,
            PurchasedUtc = purchasedUtc.AddSeconds(1)
        };

        Assert.True(database.TryAddStorePurchaseHistory(first, purchasedUtc.AddMinutes(-1), 1));
        Assert.NotEqual(0ul, first.Id);
        Assert.False(database.TryAddStorePurchaseHistory(second, purchasedUtc.AddMinutes(-1), 1));
        Assert.Equal(0ul, second.Id);

        using var context = new AuthContext(connectionString);
        AccountStorePurchaseHistoryModel row = Assert.Single(context.AccountStorePurchaseHistory.AsNoTracking());
        Assert.Equal(first.Id, row.Id);
        Assert.Equal(2278u, row.OfferId);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private DatabaseConnectionString CreateConnectionString()
    {
        return new DatabaseConnectionString
        {
            Provider         = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={Path.Combine(directory, "auth.sqlite")}"
        };
    }

    private static uint CreateAccount(DatabaseConnectionString connectionString)
    {
        using var context = new AuthContext(connectionString);
        context.Database.EnsureCreated();

        var account = new AccountModel
        {
            Email      = "store-history@example.test",
            S          = string.Empty,
            V          = string.Empty,
            GameToken  = string.Empty,
            SessionKey = string.Empty
        };

        context.Account.Add(account);
        context.SaveChanges();
        return account.Id;
    }
}
