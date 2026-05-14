using System.Numerics;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class EvilFromTheEtherOnOpenMedbay : CinematicBase, IEvilFromTheEtherOnOpenMedbay
    {
        private const uint ActorCamera      = 75227;
        private const uint ActorMordeshMM01 = 75228;
        private const uint ActorProps       = 75229;
        private const uint ActorMordeshMF01 = 75230;
        private const uint ActorMordeshMF02 = 75231;

        protected override void Setup()
        {
            Duration          = 19800;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.Unknown2 | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            CinematicId       = 0;

            StartTransition = new Transition(0, CameraAddFlags.AddCamera, 2, 1500, 0, 1500);
            EndTransition   = new Transition(18300, CameraAddFlags.WhiteOut, 0);

            SetupActors();
            SetupCamera();

            Keyframes.Add(new VisualEffect(46873, Player.Guid));
            Keyframes.Add(new VisualEffect(51065, Player.Guid));
        }

        private void SetupActors()
        {
            var position = new Position(new Vector3(70.8604f, -850.25f, -121.10999f));

            AddHiddenActor(ActorCamera, position);
            AddHiddenActor(ActorMordeshMM01, position);
            AddHiddenActor(ActorMordeshMF01, position);
            AddHiddenActor(ActorMordeshMF02, position);
            AddHiddenActor(ActorProps, position);
        }

        private void AddHiddenActor(uint creature2Id, Position position)
        {
            IActor actor = new Actor(creature2Id, EntityCreateFlag.Immediate | EntityCreateFlag.HasInteractionPrereq, 3.141593f, position);
            AddActor(actor, [new VisualEffect(45237)]);
        }

        private void SetupCamera()
        {
            IActor cameraActor = GetActor(ActorCamera);

            ICamera camera = new Camera(cameraActor, 7, 0, true, 0);
            camera.AddAttach(6933, 7);
            camera.AddTransition(6933, 0);
            camera.AddAttach(9567, 8);
            camera.AddTransition(9567, 0);
            camera.AddAttach(14300, 9);
            camera.AddTransition(14300, 0);

            AddCamera(camera);
        }

        protected override void Play()
        {
            base.Play();

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
