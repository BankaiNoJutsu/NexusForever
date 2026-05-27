using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class GauntletFindOutWhatHappened : CinematicBase, IGauntletFindOutWhatHappened
    {
        protected override void Setup()
        {
            // WIP-guessed from LaughingWS Instances-and-more: the branch queues this
            // Gauntlet cinematic at the "what happened" handoff, but the retail
            // payload and post-cinematic teleport timing are still blocked.
            Duration          = 0;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            CinematicId       = 0;
        }
    }
}
