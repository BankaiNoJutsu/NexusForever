using System.Numerics;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class Q5604TacticalDemolitionsCinematic : CinematicBase, IQ5604TacticalDemolitionsCinematic
    {
        protected override void Setup()
        {
            Duration          = 15000;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.UsesTransitionDurationSet | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            StartTransition   = new Transition(0, CameraAddFlags.AddCamera, 2, 750, 1000, 1500);
            EndTransition     = new Transition(13500, CameraAddFlags.WhiteOut, 0);

            SetupActors();
            SetupCamera();
            SetupTexts();

            Keyframes.Add(new VisualEffect(22518, Player.Guid));
            Keyframes.Add(new VisualEffect(22517, Player.Guid));
            Keyframes.Add(new VisualEffect(30478, Player.Guid, delay: 1300));
            Keyframes.Add(new VisualEffect(30480, Player.Guid, delay: 6600));
            Keyframes.Add(new VisualEffect(30479, Player.Guid, delay: 10600));
        }

        private void SetupActors()
        {
            var initialPosition = new Position(new Vector3(-7856.59521484375f, -941.21630859375f, -1357.636962890625f));
            AddActor(
                new Actor(33566, EntityCreateFlag.Immediate | EntityCreateFlag.HasInteractionPrereq, 3.1415929794311523f, initialPosition),
                [new VisualEffect(11096)]);
        }

        private void SetupCamera()
        {
            ICamera mainCam = new Camera(9894, 0u, 0u, 1f, useRotation: true);
            mainCam.AddTransition(0, 0, 1500, 0, 1500);
            AddCamera(mainCam);
        }

        private void SetupTexts()
        {
            AddText(578425, 1400, 3800);
            AddText(578426, 3867, 6500);
            AddText(578427, 6667, 10400);
            AddText(578428, 10500, 13500);
        }

        protected override void Play()
        {
            base.Play();

            Player.Session.EnqueueMessageEncrypted(new ServerCinematicTransitionDurationSet
            {
                Type          = ScaleTransitionType.StartMinimized,
                DurationStart = 1500,
                DurationMid   = 1000,
                DurationEnd   = 1500
            });

            foreach (IKeyframeAction keyframeAction in Keyframes)
                keyframeAction.Send(Player.Session);
        }
    }
}
