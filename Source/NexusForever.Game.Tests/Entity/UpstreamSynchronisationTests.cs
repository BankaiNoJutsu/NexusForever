using NexusForever.Game.Entity;
using NexusForever.Game.Entity.Synchronisation;

namespace NexusForever.Game.Tests.Entity;

public class UpstreamSynchronisationTests
{
    [Fact]
    public async Task ActionAndFunctionRunOnceOnTheEntityUpdate()
    {
        var entity = new TestEntity();
        int called = 0;
        Task action = entity.SynchroniseAsync(() => { called++; });
        Task<int> function = entity.SynchroniseAsync(() => 42);
        Assert.False(action.IsCompleted);
        Assert.False(function.IsCompleted);
        Assert.Equal(0, called);
        entity.Update(0);
        await action;
        Assert.Equal(42, await function);
        entity.Update(0);
        Assert.Equal(1, called);
    }

    [Fact]
    public async Task FailedActionDoesNotPreventTheFollowingFunction()
    {
        var entity = new TestEntity();
        Task action = entity.SynchroniseAsync(() => { throw new InvalidOperationException("action failed"); });
        Task<int> function = entity.SynchroniseAsync(() => 42);
        entity.Update(0);
        Assert.Equal("action failed", (await Assert.ThrowsAsync<InvalidOperationException>(() => action)).Message);
        Assert.Equal(42, await function);
    }

    [Fact]
    public async Task ExistingSynchronisationTaskTypeRemainsCompatible()
    {
        var legacy = new SynchronisationTask<int>();
        Task<int> task = legacy.Initialise(() => 42);
        legacy.Execute();
        Assert.Equal(42, await task);
    }

    private sealed class TestEntity : GridEntity;
}
