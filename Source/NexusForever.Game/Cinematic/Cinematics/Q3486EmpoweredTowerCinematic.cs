using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Network.World.Message.Model.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class Q3486EmpoweredTowerCinematic : CinematicBase, IQ3486EmpoweredTowerCinematic
    {
        protected override void Setup()
        {
            Duration          = 17000;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.Unknown2 | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            StartTransition   = new Transition(0, CameraAddFlags.AddCamera, 2, 1500, 0, 1500);
            EndTransition     = new Transition(15500, CameraAddFlags.WhiteOut, 0);

            SetupCamera();

            Keyframes.Add(new VisualEffect(21853, Player.Guid));
            Keyframes.Add(new VisualEffect(7668, Player.Guid));
        }

        private void SetupCamera()
        {
            ICamera mainCam = new Camera(3881, 0u, 0u, 1f, useRotation: true);
            mainCam.AddTransition(0, 0, 1500, 0, 1500);
            AddCamera(mainCam);
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
