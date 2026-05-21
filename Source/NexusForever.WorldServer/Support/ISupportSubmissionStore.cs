using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Support
{
    public interface ISupportSubmissionStore
    {
        bool TryAppend(IWorldSession session, string type, object payload);
    }
}
