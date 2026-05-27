using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class JourneyIntoOMNICore1OnCreate : CinematicBase, IJourneyIntoOMNICore1OnCreate
    {
        protected override void Setup()
        {
            // WIP-guessed from LaughingWS Instances-and-more: the branch queues an
            // OMNICore on-create cinematic through IJourneyIntoOMNICore1OnCreate,
            // but does not include actor, camera, text, timing, or visual payloads.
            // Keep this as an immediate completion-only hook until retail/client
            // evidence maps the actual cinematic data.
            Duration          = 0;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            CinematicId       = 0;
        }
    }
}
