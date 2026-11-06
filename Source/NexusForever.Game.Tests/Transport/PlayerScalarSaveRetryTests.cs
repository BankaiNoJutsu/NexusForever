using Microsoft.EntityFrameworkCore;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Option;
using NexusForever.Game.Entity;
using PlayerSaveMask = NexusForever.Game.Entity.Player.PlayerSaveMask;

namespace NexusForever.Game.Tests.Transport;

public partial class PlayerTaxiDiscoveryTests
{
    [Theory]
    [InlineData("rollback")]
    [InlineData("dispose")]
    [InlineData("reject")]
    [InlineData("unflushed")]
    [InlineData("savepoint")]
    public void ScalarCharacterFieldsRetryAfterUnconfirmedSave(string outcome)
    {
        using var connection = TaxiPersistenceDatabase();
        var f = new Fixture(); PrepareNormalSave(f.Player);
        Set(typeof(Player), f.Player, "saveMask", PlayerSaveMask.None);
        f.Player.InnateIndex = 3;
        f.Player.CastingOptions = (CastingOptionFlags)5;
        f.Player.SharedChallengeEnabled = true;
        using (var failed = Context(connection))
        {
            using var transaction = failed.Database.BeginTransaction();
            if (outcome == "savepoint") transaction.CreateSavepoint("before_scalar");
            f.Player.Save(failed);
            if (outcome == "reject")
            {
                failed.RejectSave(new IOException("Rejected scalar flush."));
                Assert.ThrowsAny<Exception>(() => failed.SaveChanges());
            }
            else if (outcome != "unflushed") failed.SaveChanges();
            if (outcome == "rollback") transaction.Rollback();
            if (outcome == "unflushed") transaction.Commit();
            if (outcome == "savepoint")
            {
                transaction.RollbackToSavepoint("before_scalar");
                transaction.Commit();
            }
        }
        using (var before = Context(connection))
        {
            var row = before.Character.AsNoTracking().Single();
            Assert.Equal(0, row.InnateIndex);
            Assert.Equal(0, row.CastingOptions);
            Assert.False(row.SharedChallengeEnabled);
        }
        using (var retry = Context(connection)) { f.Player.Save(retry); retry.SaveChanges(); }
        using var read = Context(connection);
        var saved = read.Character.AsNoTracking().Single();
        Assert.Equal(3, saved.InnateIndex);
        Assert.Equal(5, saved.CastingOptions);
        Assert.True(saved.SharedChallengeEnabled);
    }

    [Fact]
    public void ScalarCharacterFieldsStopRetryingAfterCommittedSave()
    {
        using var connection = TaxiPersistenceDatabase();
        var f = new Fixture(); PrepareNormalSave(f.Player);
        Set(typeof(Player), f.Player, "saveMask", PlayerSaveMask.None);
        f.Player.InnateIndex = 3;
        using (var first = Context(connection)) { f.Player.Save(first); first.SaveChanges(); }
        using (var external = Context(connection))
        {
            external.Character.Single().InnateIndex = 9; external.SaveChanges();
        }
        using (var second = Context(connection)) { f.Player.Save(second); second.SaveChanges(); }
        using var read = Context(connection);
        Assert.Equal(9, read.Character.AsNoTracking().Single().InnateIndex);
    }
}
