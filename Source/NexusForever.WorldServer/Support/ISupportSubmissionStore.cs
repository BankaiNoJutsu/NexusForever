using NexusForever.WorldServer.Network;
using System.Threading.Tasks;

namespace NexusForever.WorldServer.Support
{
    public interface ISupportSubmissionStore
    {
        // Reports without a response may queue their write. Ticket success
        // must instead wait for AppendAsync to confirm storage completion.
        bool TryAppend(IWorldSession session, string type, object payload);
        Task<bool> AppendAsync(IWorldSession session, string type, object payload);
    }
}
