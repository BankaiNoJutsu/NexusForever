using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class InitializationCoreY83OpenDoor : CinematicBase, IInitializationCoreY83OpenDoor
    {
        protected override void Setup()
        {
            // WIP-guessed from LaughingWS Instances-and-more: the branch queues this
            // Initialization Core door cinematic at boss phase entry, but the real
            // actor/camera/text/timing payload is not mapped.
            Duration          = 0;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            CinematicId       = 0;
        }
    }
}
