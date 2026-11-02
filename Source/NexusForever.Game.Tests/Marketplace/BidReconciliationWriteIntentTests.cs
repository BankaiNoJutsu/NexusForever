using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class BidReconciliationWriteIntentTests
{
    private static MarketplaceBidReconciliation Receipt() => new(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), new string('A', 64), false);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetirementCannotDiscardARetainedSemanticSocketEdit(bool rune)
    {
        using var f = new Fixture(null); f.Seed();
        var before = new ItemModel { Id = 1000, OwnerId = 42, ItemId = 7001, StackCount = 1, Location = (ushort)InventoryLocation.Inventory };
        using (var seed = f.Context()) { seed.Item.Add(before); await seed.SaveChangesAsync(); }
        var receipt = Receipt();
        var proof = await Assert.ThrowsAsync<BidReconciliationRolledBackException>(() => CharacterAtomicSave.ExecuteBidReconciliation(
            () => f.Context(), 42, context =>
            {
                var changed = context.Item.Single(row => row.Id == 1000);
                changed.MicrochipIds = rune ? "" : "999"; changed.RuneSlots = rune ? "7:999" : "";
                context.Entry(changed).Property(row => row.MicrochipIds).IsModified = true;
                context.Entry(changed).Property(row => row.RuneSlots).IsModified = true;
                throw new IOException("Retained socket edit before complete enrollment.");
            }, receipt));
        var witness = new NexusForever.Game.Entity.Inventory.AuctionDeliveryWitness("unused", NexusForever.Game.Marketplace.GlobalMarketplaceManager.BidItem.Capture(before), null,
            rune ? "7:0" : null);
        var normalization = witness.ItemCodecPostimage;
        Assert.Throws<InvalidOperationException>(() => receipt.RetireDeliveryItem(proof, before, normalization));
        await CharacterAtomicSave.ExecuteBidReconciliation(() => f.Context(), 42, _ => { },
            receipt with { Id = Guid.NewGuid().ToString("N") });
        using var read = f.Context();
        Assert.Equal(rune ? "7:999" : "", read.Item.Single().RuneSlots);
        Assert.Equal(rune ? "" : "999", read.Item.Single().MicrochipIds);
        Assert.Single(read.MarketplaceBidReceipt);
    }

    [Fact]
    public async Task RetainedInsertKeepsUntouchedColumnsWhenRetrySuppliesANewerPartialUpdate()
    {
        using var f = new Fixture(null); f.Seed();
        var receipt = Receipt();
        await Assert.ThrowsAsync<BidReconciliationRolledBackException>(() => CharacterAtomicSave.ExecuteBidReconciliation(
            () => f.Context(), 42, context =>
            {
                context.CharacterTradeskill.Add(new() { Id = 42, TradeskillId = (uint)TradeskillType.Armorer, TradeskillXp = 50, IsActive = 1, TalentPoints = 7 });
                throw new IOException("First enrollment failed after consuming its create marker.");
            }, receipt));
        await CharacterAtomicSave.ExecuteBidReconciliation(() => f.Context(), 42, context =>
        {
            var row = new CharacterTradeskillModel { Id = 42, TradeskillId = (uint)TradeskillType.Armorer, TradeskillXp = 60 };
            context.Attach(row).Property(value => value.TradeskillXp).IsModified = true;
        }, receipt with { Id = Guid.NewGuid().ToString("N") });
        using var read = f.Context();
        var saved = Assert.Single(read.CharacterTradeskill);
        Assert.Equal(60u, saved.TradeskillXp); Assert.Equal(1u, saved.IsActive); Assert.Equal(7u, saved.TalentPoints);
        Assert.Single(read.MarketplaceBidReceipt);
    }

    [Fact]
    public async Task OmittedWriteCannotUseAnotherCharactersStoredItemAsItsPreimage()
    {
        using var f = new Fixture(null); f.Seed();
        using (var seed = f.Context())
        {
            seed.Character.Add(new() { Id = 200, Name = "Foreign", OriginalName = "Foreign" });
            seed.Item.Add(new() { Id = 1000, OwnerId = 200, ItemId = 7001, StackCount = 1, Location = (ushort)InventoryLocation.Inventory });
            await seed.SaveChangesAsync();
        }
        var receipt = Receipt();
        await Assert.ThrowsAsync<BidReconciliationRolledBackException>(() => CharacterAtomicSave.ExecuteBidReconciliation(
            () => f.Context(), 42, context =>
            {
                var row = new ItemModel { Id = 1000, OwnerId = 42, ItemId = 7001, StackCount = 2, Location = (ushort)InventoryLocation.Inventory };
                context.Attach(row).Property(value => value.StackCount).IsModified = true;
                throw new IOException("Partial eager-clear enrollment never acquired the foreign Item.");
            }, receipt));
        await Assert.ThrowsAsync<BidReconciliationRolledBackException>(() => CharacterAtomicSave.ExecuteBidReconciliation(
            () => f.Context(), 42, _ => { }, receipt with { Id = Guid.NewGuid().ToString("N") }));
        using var read = f.Context();
        var saved = Assert.Single(read.Item);
        Assert.Equal(200ul, saved.OwnerId); Assert.Equal(1u, saved.StackCount); Assert.Empty(read.MarketplaceBidReceipt);
    }
}
