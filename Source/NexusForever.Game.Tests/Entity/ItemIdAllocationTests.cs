using System.Collections.Concurrent;
using System.Reflection;

namespace NexusForever.Game.Tests.Entity;

public sealed class ItemIdAllocationTests
{
    [Fact]
    public void ConcurrentRealAllocatorCallsAreUniqueAndContiguous()
    {
        var manager = Manager(7000);
        var ids = new ConcurrentBag<ulong>();
        Parallel.For(0, 64, _ =>
        {
            for (int index = 0; index < 500; ++index) ids.Add(manager.NextItemId);
        });
        Assert.Equal(32000, ids.Count);
        Assert.Equal(32000, ids.Distinct().Count());
        Assert.Equal(Enumerable.Range(7000, 32000).Select(id => (ulong)id), ids.Order());
        Assert.Equal(39000ul, manager.NextItemId);
    }

    [Fact]
    public void ExhaustionFailsPermanentlyWithoutWrappingToExistingIdentifiers()
    {
        var manager = Manager(ulong.MaxValue - 2);
        Assert.Equal(ulong.MaxValue - 2, manager.NextItemId);
        Assert.Equal(ulong.MaxValue - 1, manager.NextItemId);
        Assert.Throws<InvalidOperationException>(() => manager.NextItemId);
        Assert.Throws<InvalidOperationException>(() => manager.NextItemId);
    }

    private static ItemManager Manager(ulong next)
    {
        var manager = new ItemManager();
        typeof(ItemManager).GetField("nextItemId", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(manager, next);
        return manager;
    }
}
