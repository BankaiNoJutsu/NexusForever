using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class SpaceMadnessOnCreate : CinematicBase, ISpaceMadnessOnCreate
    {
        protected override void Setup()
        {
            // WIP-guessed from LaughingWS Instances-and-more: the branch queues a
            // Space Madness on-create cinematic through this interface, but supplies
            // no actor, camera, text, timing, or visual payload. Keep this as an
            // immediate completion-only hook until retail/client evidence maps it.
            Duration          = 0;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            CinematicId       = 0;
        }
    }
}
