using System;
using System.Threading.Tasks;
using NexusForever.Shared.Diagnostics;
using NLog;

namespace NexusForever.Shared.Game.Events
{
    public class TaskGenericEvent<T> : IEvent
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly Task<T> task;
        private readonly Action<T> callback;
        private readonly Action failureCallback;
        private readonly long queuedTimestamp = NexusForeverDiagnostics.GetTimestamp();
        private long completionTimestamp;

        public TaskGenericEvent(Task<T> task, Action<T> callback, Action failureCallback = null)
        {
            this.task            = task;
            this.callback        = callback;
            this.failureCallback = failureCallback;
        }

        public bool CanExecute()
        {
            if (!task.IsCompleted)
                return false;

            if (completionTimestamp == 0)
                completionTimestamp = NexusForeverDiagnostics.GetTimestamp();

            return true;
        }

        public void Execute()
        {
            if (completionTimestamp != 0)
                NexusForeverDiagnostics.RecordEventTaskWait(nameof(TaskGenericEvent<T>), NexusForeverDiagnostics.GetElapsedMilliseconds(queuedTimestamp));

            if (task.IsFaulted)
            {
                log.Error(task.Exception, "TaskGenericEvent<{0}> callback skipped because the task faulted.", typeof(T).Name);
                if (failureCallback != null)
                {
                    failureCallback.Invoke();
                    return;
                }

                throw task.Exception.GetBaseException();
            }

            if (task.IsCanceled)
            {
                log.Warn("TaskGenericEvent<{0}> callback skipped because the task was canceled.", typeof(T).Name);
                if (failureCallback != null)
                {
                    failureCallback.Invoke();
                    return;
                }

                throw new TaskCanceledException(task);
            }

            long callbackTimestamp = NexusForeverDiagnostics.GetTimestamp();
            callback.Invoke(task.Result);
            NexusForeverDiagnostics.RecordEventCallback(nameof(TaskGenericEvent<T>), NexusForeverDiagnostics.GetElapsedMilliseconds(callbackTimestamp));
        }
    }
}
