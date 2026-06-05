using System;
using System.Threading.Tasks;
using NLog;

namespace NexusForever.Shared
{
    public static class TaskExtensions
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public static async void FireAndForgetAsync(this Task task, Action<Exception> onException = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (onException ?? DefaultExceptionHandler)(ex);
            }
        }

        private static void DefaultExceptionHandler(Exception ex)
        {
            log.Error(ex, "Fire-and-forget task failed.");
        }
    }
}
