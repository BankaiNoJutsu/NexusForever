using NexusForever.Shared.Game.Events;
using NexusForever.Shared.Game.Events.Static;
using Xunit;

namespace NexusForever.Game.Tests.Shared
{
    public class EventQueueTests
    {
        [Fact]
        public async Task ExternalProducersAreDrainedExactlyOnceByTheActor()
        {
            var queue = new EventQueue();
            var executed = new HashSet<int>();
            int calls = 0;
            var producers = Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
            {
                for (int i = 0; i < 250; ++i)
                {
                    int id = worker * 250 + i;
                    queue.EnqueueThreadSafe(new PredicateEvent(() => true, () => { ++calls; executed.Add(id); }));
                }
            })).ToArray();
            var all = Task.WhenAll(producers);
            while (!all.IsCompleted) { queue.Update(0); await Task.Yield(); }
            await all; queue.Update(0); queue.Update(0);
            Assert.Equal(2000, executed.Count); Assert.Equal(2000, calls); Assert.False(queue.PendingEvents);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void BlockingEventPreservesDeferredEvents(int deferredCount)
        {
            var queue = new EventQueue();
            var executed = new List<int>();
            bool ready = false;
            for (int i = 0; i < deferredCount; i++)
            {
                int id = i;
                queue.EnqueueEvent(new PredicateEvent(() => ready, () => executed.Add(id)));
            }
            queue.EnqueueEvent(new PredicateEvent(() => ready, () => executed.Add(-1)), ConditionalEventType.Blocking);
            queue.EnqueueEvent(new PredicateEvent(() => true, () => executed.Add(-2)));

            queue.Update(0);
            queue.Update(0);
            Assert.Empty(executed);
            Assert.True(queue.PendingEvents);

            ready = true;
            queue.Update(0);
            Assert.Equal(new[] { -1, -2 }.Concat(Enumerable.Range(0, deferredCount)), executed);
            Assert.False(queue.PendingEvents);

            queue.Update(0);
            Assert.Equal(deferredCount + 2, executed.Count);
        }

        [Fact]
        public void DeferredEventCanBecomeReadyAfterBlockerHasCompleted()
        {
            var queue = new EventQueue();
            bool deferredReady = false;
            bool blockerReady = false;
            int executions = 0;
            queue.EnqueueEvent(new PredicateEvent(() => deferredReady, () => executions++));
            queue.EnqueueEvent(new PredicateEvent(() => blockerReady, () => { }), ConditionalEventType.Blocking);

            queue.Update(0);
            blockerReady = true;
            queue.Update(0);
            Assert.True(queue.PendingEvents);
            Assert.Equal(0, executions);

            deferredReady = true;
            queue.Update(0);
            Assert.Equal(1, executions);
            Assert.False(queue.PendingEvents);
        }

        [Fact]
        public void StandardEventsDoNotBlockReadyEvents()
        {
            var queue = new EventQueue();
            bool ready = false;
            var executed = new List<int>();
            queue.EnqueueEvent(new PredicateEvent(() => ready, () => executed.Add(1)));
            queue.EnqueueEvent(new PredicateEvent(() => true, () => executed.Add(2)));

            queue.Update(0);
            Assert.Equal(new[] { 2 }, executed);
            ready = true;
            queue.Update(0);
            Assert.Equal(new[] { 2, 1 }, executed);
            Assert.False(queue.PendingEvents);
        }
        [Theory]
        [InlineData(ConditionalEventType.Standard, false)]
        [InlineData(ConditionalEventType.Blocking, false)]
        [InlineData(ConditionalEventType.Standard, true)]
        [InlineData(ConditionalEventType.Blocking, true)]
        public void PredicateFailureReportsOnceAndRetainsDeferredWork(ConditionalEventType type, bool external)
        {
            var queue = new EventQueue(); var errors = new List<Exception>(); var executed = new List<string>();
            var fault = new InvalidOperationException("Predicate failed."); int predicates = 0; bool ready = false;
            queue.UnhandledExceptionHandler = errors.Add;
            queue.EnqueueEvent(new PredicateEvent(() => ready, () => executed.Add("deferred")));
            Enqueue(new PredicateEvent(() => { predicates++; throw fault; }, () => throw new InvalidOperationException("Failed predicate executed.")), type);
            Enqueue(new PredicateEvent(() => true, () => executed.Add("ready")), ConditionalEventType.Standard);

            Assert.Null(Record.Exception(() => queue.Update(0)));
            Assert.Same(fault, Assert.Single(errors)); Assert.Equal(new[] { "ready" }, executed);
            Assert.Equal(1, predicates); Assert.True(queue.PendingEvents);
            queue.Update(0); Assert.Single(errors); Assert.Equal(1, predicates);
            ready = true; queue.Update(0);
            Assert.Equal(new[] { "ready", "deferred" }, executed); Assert.False(queue.PendingEvents);

            void Enqueue(IEvent item, ConditionalEventType condition)
            { if (external) queue.EnqueueThreadSafe(item, condition); else queue.EnqueueEvent(item, condition); }
        }

        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void ThrowingErrorHookStillRestoresDeferredWork(bool predicateFailure)
        {
            var queue = new EventQueue(); var executed = new List<string>(); bool ready = false;
            var fault = new InvalidOperationException("Event failed."); var hook = new InvalidOperationException("Hook failed.");
            queue.UnhandledExceptionHandler = _ => throw hook;
            queue.EnqueueEvent(new PredicateEvent(() => ready, () => executed.Add("deferred")));
            queue.EnqueueEvent(new PredicateEvent(() => predicateFailure ? throw fault : true, () => throw fault));
            queue.EnqueueEvent(new PredicateEvent(() => true, () => executed.Add("ready")));

            Assert.Same(hook, Assert.Throws<InvalidOperationException>(() => queue.Update(0)));
            Assert.Empty(executed); Assert.True(queue.PendingEvents);
            queue.UnhandledExceptionHandler = _ => throw new InvalidOperationException("Failed event was retried.");
            ready = true; queue.Update(0);
            Assert.Equal(new[] { "ready", "deferred" }, executed); Assert.False(queue.PendingEvents);
        }

        [Theory]
        [InlineData(ConditionalEventType.Standard)]
        [InlineData(ConditionalEventType.Blocking)]
        public void ExecuteFailureKeepsItsExistingReportingAndDeferredOrdering(ConditionalEventType type)
        {
            var queue = new EventQueue(); var errors = new List<Exception>(); var executed = new List<string>();
            var fault = new InvalidOperationException("Execute failed."); bool ready = false;
            queue.UnhandledExceptionHandler = errors.Add;
            queue.EnqueueEvent(new PredicateEvent(() => ready, () => executed.Add("deferred")));
            queue.EnqueueEvent(new PredicateEvent(() => true, () => throw fault), type);
            queue.EnqueueEvent(new PredicateEvent(() => true, () => executed.Add("ready")));
            queue.Update(0); Assert.Same(fault, Assert.Single(errors)); Assert.Equal(new[] { "ready" }, executed);
            ready = true; queue.Update(0);
            Assert.Equal(new[] { "ready", "deferred" }, executed); Assert.False(queue.PendingEvents);
        }
    }
}
