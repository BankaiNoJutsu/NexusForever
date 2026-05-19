using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Account.Inventory;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace NexusForever.Game.Tests.Account.Inventory;

public class AccountItemCooldownTests
{
    [Fact]
    public void Save_UntouchedCooldownPlaceholder_DoesNotTrackEntity()
    {
        using AuthContext context = CreateContext();
        var cooldown = new AccountItemCooldown(42u, 3u);

        cooldown.Save(context);

        Assert.Empty(context.ChangeTracker.Entries<AccountItemCooldownModel>());
    }

    [Fact]
    public void Save_FirstTriggeredCooldown_TracksAddedEntity()
    {
        using AuthContext context = CreateContext();
        var cooldown = new AccountItemCooldown(42u, 3u);

        cooldown.TriggerWithDuration(1800u);
        cooldown.Save(context);

        var entry = Assert.Single(context.ChangeTracker.Entries<AccountItemCooldownModel>());
        Assert.Equal(EntityState.Added, entry.State);
        Assert.Equal(42u, entry.Entity.Id);
        Assert.Equal(3u, entry.Entity.CooldownGroupId);
        Assert.Equal(1800u, entry.Entity.Duration);
        Assert.NotNull(entry.Entity.Timestamp);
    }

    [Fact]
    public void Save_RetriggeredPersistedCooldown_TracksModifiedEntity()
    {
        var cooldown = new AccountItemCooldown(42u, 3u);

        using (AuthContext createContext = CreateContext())
        {
            cooldown.TriggerWithDuration(1800u);
            cooldown.Save(createContext);
        }

        using AuthContext modifyContext = CreateContext();

        cooldown.TriggerWithDuration(28800u);
        cooldown.Save(modifyContext);

        var entry = Assert.Single(modifyContext.ChangeTracker.Entries<AccountItemCooldownModel>());
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.Equal(28800u, entry.Entity.Duration);
        Assert.True(entry.Property(p => p.Timestamp).IsModified);
        Assert.True(entry.Property(p => p.Duration).IsModified);
    }

    [Fact]
    public void Build_UsesRemainingDurationForActiveCooldown()
    {
        var cooldown = new AccountItemCooldown(new AccountItemCooldownModel
        {
            Id              = 42u,
            CooldownGroupId = 2u,
            Timestamp       = DateTime.UtcNow.AddSeconds(-10d),
            Duration        = 60u
        });

        var packet = cooldown.Build();

        Assert.Equal(2u, packet.AccountItemCooldownGroup);
        Assert.InRange(packet.CooldownInSeconds, 49u, 50u);
    }

    [Fact]
    public void Build_ClampsExpiredCooldownToZero()
    {
        var cooldown = new AccountItemCooldown(new AccountItemCooldownModel
        {
            Id              = 42u,
            CooldownGroupId = 2u,
            Timestamp       = DateTime.UtcNow.AddSeconds(-65d),
            Duration        = 60u
        });

        Assert.Equal(0u, cooldown.Build().CooldownInSeconds);
    }

    private static AuthContext CreateContext()
    {
        DbContextOptions<AuthContext> options = new DbContextOptionsBuilder<AuthContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_auth;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        return new AuthContext(options);
    }
}
