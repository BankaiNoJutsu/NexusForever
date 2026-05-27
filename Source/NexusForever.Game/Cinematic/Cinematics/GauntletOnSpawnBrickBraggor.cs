using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class GauntletOnSpawnBrickBraggor : CinematicBase, IGauntletOnSpawnBrickBraggor
    {
        protected override void Setup()
        {
            // WIP-guessed from LaughingWS Instances-and-more: the branch queues this
            // Brick Braggor spawn cinematic with a "make real one" note. Keep it as
            // an immediate placeholder until the actual cinematic payload is mapped.
            Duration          = 0;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            CinematicId       = 0;
        }
    }
}
