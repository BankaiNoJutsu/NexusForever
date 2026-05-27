using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class FragmentZeroFirstWarning : CinematicBase, IFragmentZeroFirstWarning
    {
        protected override void Setup()
        {
            // WIP-guessed from LaughingWS Instances-and-more: the branch queues this
            // Fragment Zero warning cinematic during the continuation phase, but the
            // real actor/camera/text/timing payload is unmapped.
            Duration          = 0;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            CinematicId       = 0;
        }
    }
}
