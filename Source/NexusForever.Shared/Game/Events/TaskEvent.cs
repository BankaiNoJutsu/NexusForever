using System;
using System.Threading.Tasks;
using NexusForever.Shared.Diagnostics;
using NLog;

namespace NexusForever.Shared.Game.Events
{
    /// <summary>
    /// An <see cref="IEvent"/> that will execute an <see cref="Action"/> after <see cref="Task"/> completion.
    /// </summary>
    public class TaskEvent : IEvent
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly Task task;
        private readonly Action callback;
        private readonly Action failureCallback;
        private readonly long queuedTimestamp = NexusForeverDiagnostics.GetTimestamp();
        private long completionTimestamp;

        public TaskEvent(Task task, Action callback, Action failureCallback = null)
        {
            this.task            = task;
            this.callback        = callback;
            this.failureCallback = failureCallback;
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

            if (task.IsFaulted)
            {
                log.Error(task.Exception, "TaskEvent callback skipped because the task faulted.");
                if (failureCallback != null)
                {
                    failureCallback.Invoke();
                    return;
                }

                throw task.Exception.GetBaseException();
            }

            if (task.IsCanceled)
            {
                log.Warn("TaskEvent callback skipped because the task was canceled.");
                if (failureCallback != null)
                {
                    failureCallback.Invoke();
                    return;
                }

                throw new TaskCanceledException(task);
            }

            long callbackTimestamp = NexusForeverDiagnostics.GetTimestamp();
            callback.Invoke();
            NexusForeverDiagnostics.RecordEventCallback(nameof(TaskEvent), NexusForeverDiagnostics.GetElapsedMilliseconds(callbackTimestamp));
        }
    }
}
