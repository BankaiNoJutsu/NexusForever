using System.Numerics;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class Q3673ContactWithThaydCinematic : CinematicBase, IQ3673ContactWithThaydCinematic
    {
        protected override void Setup()
        {
            Duration          = 89333;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.UsesTransitionDurationSet | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            StartTransition   = new Transition(0, CameraAddFlags.AddCamera, 2, 750, 1000, 1500);
            EndTransition     = new Transition(87833, CameraAddFlags.WhiteOut, 0);

            SetupActors();
            SetupCamera();
            SetupTexts();

            Keyframes.Add(new VisualEffect(21853, Player.Guid));
            Keyframes.Add(new VisualEffect(50826, Player.Guid));

            // Append actor visibility keyframes so they are sent in Play()
            foreach (IActor actor in Actors.Values)
                Keyframes.AddRange(actor.Keyframes);
        }

        private void SetupActors()
        {
            var initialPosition = new Position(new Vector3(4353.265625f, -747.3856201171875f, -5594.56201171875f));
            const float angle = 3.1415929794311523f;
            var flags = EntityCreateFlag.Immediate | EntityCreateFlag.HasInteractionPrereq;

            AddActor(new Actor(70271, flags, angle, initialPosition), [new VisualEffect(45237)]);
            AddActor(new Actor(70272, flags, angle, initialPosition), [new VisualEffect(45237)]);
            AddActor(new Actor(70273, flags, angle, initialPosition), [new VisualEffect(45237)]);
            AddActor(new Actor(70280, flags, angle, initialPosition), [new VisualEffect(45237)]);
            AddActor(new Actor(70274, flags, angle, initialPosition), [new VisualEffect(45237)]);
            AddActor(new Actor(70275, flags, angle, initialPosition), [new VisualEffect(45237)]);
            AddActor(new Actor(70276, flags, angle, initialPosition), [new VisualEffect(45237)]);
            AddActor(new Actor(70277, flags, angle, initialPosition), [new VisualEffect(45237)]);

            IActor actor1 = new Actor(70278, flags, angle, initialPosition);
            AddActor(actor1, [new VisualEffect(45237)]);
            actor1.AddVisibility(30000, true);

            IActor actor2 = new Actor(70279, flags, angle, initialPosition);
            AddActor(actor2, [new VisualEffect(45237)]);
            actor2.AddVisibility(30000, true);

            IActor actor3 = new Actor(70283, flags, angle, initialPosition);
            AddActor(actor3, [new VisualEffect(45237)]);
            actor3.AddVisibility(0, false);
            actor3.AddVisibility(30000, true);

            IActor actor4 = new Actor(70284, flags, angle, initialPosition);
            AddActor(actor4, [new VisualEffect(45237)]);
            actor4.AddVisibility(0, false);
            actor4.AddVisibility(30000, true);

            AddActor(new Actor(71761, flags, angle, initialPosition), [new VisualEffect(45237)]);
        }

        private void SetupCamera()
        {
            ICamera mainCam = new Camera(GetActor(70271), 7, 0, true, 0);
            AddCamera(mainCam);
            mainCam.AddAttach(3633, 8);   mainCam.AddTransition(3633, 0);
            mainCam.AddAttach(6033, 9);   mainCam.AddTransition(6033, 0);
            mainCam.AddAttach(10867, 10); mainCam.AddTransition(10867, 0);
            mainCam.AddAttach(17200, 11); mainCam.AddTransition(17200, 0);
            mainCam.AddAttach(19733, 12); mainCam.AddTransition(19733, 0);
            mainCam.AddAttach(21700, 13); mainCam.AddTransition(21700, 0);
            mainCam.AddAttach(24433, 14); mainCam.AddTransition(24433, 0);
            mainCam.AddAttach(26100, 15); mainCam.AddTransition(26100, 0);
            mainCam.AddAttach(28733, 16); mainCam.AddTransition(28733, 0);
            mainCam.AddAttach(30733, 17); mainCam.AddTransition(30733, 0);
            mainCam.AddAttach(35167, 18); mainCam.AddTransition(35167, 0);
            mainCam.AddAttach(37100, 19); mainCam.AddTransition(37100, 0);
            mainCam.AddAttach(40133, 20); mainCam.AddTransition(40133, 0);
            mainCam.AddAttach(41067, 21); mainCam.AddTransition(41067, 0);
            mainCam.AddAttach(41800, 22); mainCam.AddTransition(41800, 0);
            mainCam.AddAttach(42500, 23); mainCam.AddTransition(42500, 0);
            mainCam.AddAttach(43033, 24); mainCam.AddTransition(43033, 0);
            mainCam.AddAttach(44067, 25); mainCam.AddTransition(44067, 0);
            mainCam.AddAttach(46867, 26); mainCam.AddTransition(46867, 0);
            mainCam.AddAttach(47767, 28); mainCam.AddTransition(47767, 0);
            mainCam.AddAttach(50167, 29); mainCam.AddTransition(50167, 0);
            mainCam.AddAttach(51200, 30); mainCam.AddTransition(51200, 0);
            mainCam.AddAttach(54367, 31); mainCam.AddTransition(54367, 0);
            mainCam.AddAttach(56533, 32); mainCam.AddTransition(56533, 0);
            mainCam.AddAttach(57933, 33); mainCam.AddTransition(57933, 0);
            mainCam.AddAttach(59300, 34); mainCam.AddTransition(59300, 0);
            mainCam.AddAttach(61533, 35); mainCam.AddTransition(61533, 0);
            mainCam.AddAttach(69667, 36); mainCam.AddTransition(69667, 0);
            mainCam.AddAttach(72833, 37); mainCam.AddTransition(72833, 0);
            mainCam.AddAttach(81067, 38); mainCam.AddTransition(81067, 0);
            mainCam.AddAttach(85167, 39); mainCam.AddTransition(85167, 0);
        }

        private void SetupTexts()
        {
            AddText(755544, 1900, 755);
            AddText(755545, 9200, 11800);
            AddText(755542, 13300, 16500);
            AddText(755538, 17800, 21700);
            AddText(755543, 21800, 27400);
            AddText(755539, 27500, 35300);
            AddText(755540, 37700, 41600);
            AddText(755541, 83500, 87500);
        }

        protected override void Play()
        {
            base.Play();

            Player.Session.EnqueueMessageEncrypted(new ServerCinematicDelayFlag
            {
                Delay = 0,
                Flag  = true
            });

            Player.Session.EnqueueMessageEncrypted(new ServerCinematicTransitionDurationSet
            {
                Type          = ScaleTransitionType.StartMinimized,
                DurationStart = 1500,
                DurationMid   = 0,
                DurationEnd   = 1500
            });

            foreach (IKeyframeAction keyframeAction in Keyframes)
                keyframeAction.Send(Player.Session);
        }
    }
}
