using System;
using System.Threading.Tasks;

namespace NexusForever.WorldServer.Service
{
    public interface IBackgroundTaskRunner
    {
        void Queue(Func<Task> operation, Action<Exception> onException = null);

        void Observe(Task operation, Action onCompleted = null, Action<Exception> onException = null);
    }
}
