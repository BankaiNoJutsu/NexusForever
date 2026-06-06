using System;
using System.Threading.Tasks;
using NexusForever.Shared;

namespace NexusForever.WorldServer.Service
{
    public sealed class BackgroundTaskRunner : IBackgroundTaskRunner
    {
        public void Queue(Func<Task> operation, Action<Exception> onException = null)
        {
            ArgumentNullException.ThrowIfNull(operation);

            Task.Run(operation).FireAndForgetAsync(onException);
        }

        public void Observe(Task operation, Action onCompleted = null, Action<Exception> onException = null)
        {
            ArgumentNullException.ThrowIfNull(operation);

            operation.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    HandleException(task.Exception?.GetBaseException(), onException);
                    return;
                }

                if (task.IsCanceled)
                {
                    HandleException(new TaskCanceledException(task), onException);
                    return;
                }

                onCompleted?.Invoke();
            }, TaskContinuationOptions.ExecuteSynchronously).FireAndForgetAsync(onException);
        }

        private static void HandleException(Exception exception, Action<Exception> onException)
        {
            if (onException != null)
            {
                onException(exception);
                return;
            }

            throw exception;
        }
    }
}
