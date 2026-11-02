using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NexusForever.Shared.Game.Events.Static;
using NLog;

namespace NexusForever.Shared.Game.Events
{
    public class EventQueue : IUpdate
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        
        private class PendingEvent
        {
            public IEvent Event { get; init; }
            public ConditionalEventType Type { get; init; }
        }
        
        /// <summary>
        /// Returns if there are any <see cref="IEvent"/>'s in the queue.
        /// </summary>
        public bool PendingEvents => events.Count != 0 || !externalEvents.IsEmpty;

        private readonly Queue<PendingEvent> events = new();
        private readonly ConcurrentQueue<PendingEvent> externalEvents = new();

        // Cross-actor producers never write the actor's ordinary Queue.
        public void EnqueueThreadSafe(IEvent @event, ConditionalEventType type = ConditionalEventType.Standard)
            => externalEvents.Enqueue(new PendingEvent { Event = @event, Type = type });

        /// <summary>
        /// Optional handler invoked when an enqueued event throws while checking or executing.
        /// </summary>
        public Action<Exception> UnhandledExceptionHandler { get; set; }

        /// <summary>
        /// Enqueue <see cref="IEvent"/> to be executed.
        /// </summary>
        public void EnqueueEvent(IEvent @event, ConditionalEventType type = ConditionalEventType.Standard)
        {
            events.Enqueue(new PendingEvent
            {
                Event = @event,
                Type  = type
            });
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public virtual void Update(double lastTick)
        {
            while (externalEvents.TryDequeue(out PendingEvent incoming)) events.Enqueue(incoming);
            if (!PendingEvents)
                return;

            var newEvents = new List<PendingEvent>();
            try
            {
                while (events.TryPeek(out PendingEvent pending))
                {
                    bool ready;
                    try { ready = pending.Event.CanExecute(); }
                    catch (Exception exception)
                    {
                        // A failed predicate must not poison the front forever.
                        events.Dequeue();
                        ReportFailure(exception);
                        continue;
                    }

                    if (!ready)
                    {
                        if (pending.Type == ConditionalEventType.Standard)
                        {
                            newEvents.Add(events.Dequeue());
                            continue;
                        }

                        if (pending.Type == ConditionalEventType.Blocking)
                            break;

                        throw new InvalidOperationException($"Unsupported conditional event type {pending.Type}.");
                    }

                    events.Dequeue();
                    try { pending.Event.Execute(); }
                    catch (Exception exception) { ReportFailure(exception); }
                }
            }
            finally
            {
                // Even an error hook that intentionally propagates must not
                // discard events already deferred earlier in this tick.
                foreach (PendingEvent @event in newEvents)
                    events.Enqueue(@event);
            }
        }

        private void ReportFailure(Exception exception)
        {
            log.Error(exception);
            UnhandledExceptionHandler?.Invoke(exception);
        }
    }
}
