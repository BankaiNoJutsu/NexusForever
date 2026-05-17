using System;
using System.Threading.Tasks;

using NexusForever.Shared.Diagnostics;

namespace NexusForever.Shared.Game.Events
{
    /// <summary>
    /// An <see cref="IEvent"/> that will execute an <see cref="Action"/> after <see cref="Task"/> completion.
    /// </summary>
    public class TaskEvent : IEvent
    {
        private readonly Task task;
        private readonly Action callback;
        private readonly long queuedTimestamp = NexusForeverDiagnostics.GetTimestamp();
        private long completionTimestamp;

        public TaskEvent(Task task, Action callback)
        {
            this.task     = task;
            this.callback = callback;
        }

        /// <summary>
        /// Returns if <see cref="TaskEvent"/> can be executed.
        /// </summary>
        public bool CanExecute()
        {
            if (!task.IsCompleted)
                return false;

            if (completionTimestamp == 0)
                completionTimestamp = NexusForeverDiagnostics.GetTimestamp();

            return true;
        }

        /// <summary>
        /// Executes <see cref="TaskEvent"/> action.
        /// </summary>
        public void Execute()
        {
            if (completionTimestamp != 0)
                NexusForeverDiagnostics.RecordEventTaskWait(nameof(TaskEvent), NexusForeverDiagnostics.GetElapsedMilliseconds(queuedTimestamp));

            long callbackTimestamp = NexusForeverDiagnostics.GetTimestamp();
            callback.Invoke();
            NexusForeverDiagnostics.RecordEventCallback(nameof(TaskEvent), NexusForeverDiagnostics.GetElapsedMilliseconds(callbackTimestamp));
        }
    }
}
