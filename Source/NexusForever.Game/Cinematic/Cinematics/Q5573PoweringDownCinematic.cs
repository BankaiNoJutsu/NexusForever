using System.Numerics;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class Q5573PoweringDownCinematic : CinematicBase, IQ5573PoweringDownCinematic
    {
        protected override void Setup()
        {
            Duration          = 14500;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.Unknown2 | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            StartTransition   = new Transition(0, CameraAddFlags.AddCamera, 2, 750, 1000, 1500);
            EndTransition     = new Transition(13000, CameraAddFlags.WhiteOut, 0);

            SetupActors();
            SetupCamera();

            Keyframes.Add(new VisualEffect(22508, Player.Guid));
            Keyframes.Add(new VisualEffect(22513, Player.Guid));
            Keyframes.Add(new VisualEffect(36672, Player.Guid));
        }

        private void SetupActors()
        {
            var initialPosition = new Position(new Vector3(-7763.1142578125f, -949.2301025390625f, -273.4617919921875f));
            AddActor(
                new Actor(33458, EntityCreateFlag.Immediate | EntityCreateFlag.HasInteractionPrereq, 0f, initialPosition, activePropId: 1373867),
                [new VisualEffect(11096), new VisualEffect(11096)]);
        }

        private void SetupCamera()
        {
            ICamera mainCam = new Camera(9879, 0u, 0u, 1f, useRotation: true);
            mainCam.AddTransition(0, 0, 1500, 0, 1500);
            AddCamera(mainCam);

            ICamera cam2 = new Camera(9880, 0u, 4600u, 1f, useRotation: true);
            cam2.AddTransition(4600, 0, 1500, 0, 1500);
            AddCamera(cam2);
        }

        protected override void Play()
        {
            base.Play();

            Player.Session.EnqueueMessageEncrypted(new ServerCinematicTransitionDurationSet
            {
                Type          = ScaleTransitionType.StartMinimized,
                DurationStart = 1500,
                DurationMid   = 3000,
                DurationEnd   = 1500
            });

            foreach (IKeyframeAction keyframeAction in Keyframes)
                keyframeAction.Send(Player.Session);
        }
    }
}
