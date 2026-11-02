using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Static.Entity;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Database;

public sealed class BankReconciliationOwnershipTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public BankReconciliationOwnershipTests() { }
    internal BankReconciliationOwnershipTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData("foreign-character")]
    [InlineData("delete-owner")]
    [InlineData("unlinked-item")]
    [InlineData("foreign-mail")]
    [InlineData("dual-custody")]
    public async Task CallbackCannotOverrideCharacterOrMailCustody(string change)
    {
        using var f = new Fixture(options); f.Seed();
        using (var seed = f.Context())
        {
            seed.Character.Add(new() { Id = 43, Name = "Foreign", OriginalName = "Foreign", CreateTime = DateTime.UtcNow, PathActivatedTimestamp = DateTime.UtcNow });
            seed.Item.Add(new() { Id = 905, ItemId = 7001, Location = (ushort)InventoryLocation.None, StackCount = 2, Charges = 2 });
            if (change is "foreign-mail" or "dual-custody")
            {
                seed.CharacterMail.Add(new() { Id = 601, RecipientId = change == "foreign-mail" ? 43ul : 42ul, Subject = "Custody", Message = "Bound owner", CreateTime = DateTime.UtcNow });
                seed.CharacterMailAttachment.Add(new() { Id = 601, Index = 0, ItemGuid = 905 });
            }
            if (change == "dual-custody") seed.RealmBankItem.Add(new() { Id = 905, AccountId = 12, RealmId = 1, ItemId = 7001, StackCount = 2 });
            await seed.SaveChangesAsync();
        }
        var receipt = new BankSaveReconciliation(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), new string('A', 64), false,
            _ => { }, _ => { }, _ => true);
        await Assert.ThrowsAsync<CharacterReconciliationRolledBackException>(() => CharacterAtomicSave.ExecuteBankReconciliation(() => f.Context(), 42, context =>
        {
            if (change == "foreign-character") context.Character.Single(row => row.Id == 43).Name = "Overridden";
            else if (change == "delete-owner") context.Character.Remove(context.Character.Single(row => row.Id == 42));
            else context.Item.Single(row => row.Id == 905).Charges = 1;
        }, receipt));
        using var read = f.Context();
        Assert.Equal(2, read.Character.Count()); Assert.Equal("Foreign", read.Character.Single(row => row.Id == 43).Name);
        Assert.Equal(2u, read.Item.Single(row => row.Id == 905).Charges); Assert.Empty(read.BankReconciliationReceipt);
    }
}
