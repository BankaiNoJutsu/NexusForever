using System;
using System.Threading.Tasks;

using NexusForever.Shared.Diagnostics;

namespace NexusForever.Shared.Game.Events
{
    public class TaskGenericEvent<T> : IEvent
    {
        private readonly Task<T> task;
        private readonly Action<T> callback;
        private readonly long queuedTimestamp = NexusForeverDiagnostics.GetTimestamp();
        private long completionTimestamp;

        public TaskGenericEvent(Task<T> task, Action<T> callback)
        {
            this.task     = task;
            this.callback = callback;
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

            long callbackTimestamp = NexusForeverDiagnostics.GetTimestamp();
            callback.Invoke(task.Result);
            NexusForeverDiagnostics.RecordEventCallback(nameof(TaskGenericEvent<T>), NexusForeverDiagnostics.GetElapsedMilliseconds(callbackTimestamp));
        }
    }
}
