using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class ItemMarketplacePublicationTests
{
    [Fact]
    public void CachedCollectionsAndNestedSlotsRemainExcludedUntilTheirExactOwnerReleases()
    {
        var item = new NexusForever.Game.Entity.Item(new ItemModel { Id = 900, OwnerId = 42, StackCount = 3, Durability = 1 });
        IList<uint> chips = item.MicrochipIds;
        IList<ItemRuneSlot> runes = item.RuneSlots;
        var slot = new ItemRuneSlot((RuneType)1, 22);
        chips.Add(11); runes.Add(slot);

        var publication = item.BeginMarketplacePublication();
        Assert.Throws<InvalidOperationException>(() => item.BeginMarketplacePublication());
        Assert.Throws<InvalidOperationException>(() => item.Durability = .5f);
        Assert.Throws<InvalidOperationException>(() => chips.Add(33));
        Assert.Throws<InvalidOperationException>(() => chips[0] = 33);
        Assert.Throws<InvalidOperationException>(() => chips.RemoveAt(0));
        Assert.Throws<InvalidOperationException>(() => chips.Clear());
        Assert.Throws<InvalidOperationException>(() => runes.Add(new()));
        Assert.Throws<InvalidOperationException>(() => runes[0] = new());
        Assert.Throws<InvalidOperationException>(() => runes.RemoveAt(0));
        Assert.Throws<InvalidOperationException>(() => runes.Clear());
        Assert.Throws<InvalidOperationException>(() => slot.Type = (RuneType)2);
        Assert.Throws<InvalidOperationException>(() => slot.RuneItem2Id = 33);
        Assert.Throws<InvalidOperationException>(item.TouchRuneSlots);
        Assert.Equal(11u, Assert.Single(chips)); Assert.Same(slot, Assert.Single(runes));
        Assert.Equal((RuneType)1, slot.Type); Assert.Equal(22u, slot.RuneItem2Id);
        publication.Dispose();

        // An old token cannot unlock a later publication of the same Item.
        using (item.BeginMarketplacePublication())
        {
            publication.Dispose();
            Assert.Throws<InvalidOperationException>(() => slot.RuneItem2Id = 33);
            Assert.Throws<InvalidOperationException>(() => chips.Clear());
        }
        item.Durability = .5f; slot.RuneItem2Id = 33; slot.Type = (RuneType)2;
        chips.Clear(); runes.Clear(); item.TouchRuneSlots();
        Assert.Equal(.5f, item.Durability); Assert.Empty(chips); Assert.Empty(runes);
    }
}
