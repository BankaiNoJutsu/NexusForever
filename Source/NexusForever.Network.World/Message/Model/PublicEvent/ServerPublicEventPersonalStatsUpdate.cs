using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    // Client reader WildStar64.exe 14007b9e0-compatible payload: public-event stats only.
    [Message(GameMessageOpcode.ServerPublicEventPersonalStatsUpdate)]
    public class ServerPublicEventPersonalStatsUpdate : PublicEventStats
    {
        // Not seen in sniffs as there are other options for sending the same data but this is still usable.
    }
}
