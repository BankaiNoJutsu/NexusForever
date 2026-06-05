using System.Threading.Tasks;

namespace NexusForever.Database
{
    public static class DatabaseTaskExtensions
    {
        /// <summary>
        /// Block until the database <see cref="Task"/> completes. Prefer async call sites when possible.
        /// </summary>
        public static void WaitUnwrap(this Task task)
        {
            task.ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
