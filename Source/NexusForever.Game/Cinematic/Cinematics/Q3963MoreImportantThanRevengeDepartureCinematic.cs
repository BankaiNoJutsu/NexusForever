using System.Numerics;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model.Cinematic;

namespace NexusForever.Game.Cinematic.Cinematics
{
    public class Q3963MoreImportantThanRevengeDepartureCinematic : CinematicBase, IQ3963MoreImportantThanRevengeDepartureCinematic
    {
        private const uint CaptiveExileSoldierCreature2Id = 12537u;
        private const uint DominionDropshipOneCreature2Id = 8531u;
        private const uint DominionDropshipTwoCreature2Id = 8532u;

        private const uint DepartureCameraSplineId = 3860u;
        private const uint ShipControl45401BoardingSplineId = 3159u;
        private const uint ShipControl45402BoardingSplineId = 3160u;
        private const uint ShipControl45401TakeoffSplineId = 3481u;
        private const uint ShipControl45402TakeoffSplineId = 3482u;
        private const uint BoardingCompleteDelay = 6500u;
        private const uint CinematicDuration = 26500u;
        private const uint ActorSplinePlaybackMode = 8u;

        private static readonly Position ShipControl45401PassengerStartPosition = new(new Vector3(4405.98f, -703.761f, -5204.26f));
        private static readonly Position ShipControl45402PassengerStartPosition = new(new Vector3(4409.12f, -702.468f, -5195.14f));
        private static readonly Position ShipControl45401ShipPosition = new(new Vector3(4515.42f, -674.002f, -5183.32f));
        private static readonly Position ShipControl45402ShipPosition = new(new Vector3(4558.71f, -670.928f, -5196.63f));

        protected override void Setup()
        {
            Duration          = CinematicDuration;
            InitialFlags      = CinematicFlags.EndImmediate | CinematicFlags.UsesTransitionDurationSet | CinematicFlags.NotifyServer;
            InitialCancelMode = CancelType.EndImmediate;
            StartTransition   = new Transition(0, CameraAddFlags.AddCamera, 2, 750, 1000, 1500);
            EndTransition     = new Transition(CinematicDuration - 1500u, CameraAddFlags.WhiteOut, 0);

            SetupShips();
            SetupCamera();
        }

        private void SetupShips()
        {
            AddBoardingPassenger(ShipControl45401PassengerStartPosition, ShipControl45401BoardingSplineId);
            AddBoardingPassenger(ShipControl45402PassengerStartPosition, ShipControl45402BoardingSplineId);

            AddShip(DominionDropshipOneCreature2Id, ShipControl45401ShipPosition, ShipControl45401TakeoffSplineId, 36f);
            AddShip(DominionDropshipTwoCreature2Id, ShipControl45402ShipPosition, ShipControl45402TakeoffSplineId, 30f);
        }

        private void AddBoardingPassenger(Position position, uint splineId)
        {
            IActor passenger = new Actor(
                CaptiveExileSoldierCreature2Id,
                EntityCreateFlag.Immediate | EntityCreateFlag.HasInteractionPrereq,
                null,
                position,
                textureLoDBias: 0,
                movementMode: ModeType.Walk);
            passenger.AddPacketToSend(new ServerCinematicActorSpline
            {
                Delay       = 0,
                UnitId      = passenger.UnitId,
                SplineId    = splineId,
                SplineSpeed = 22f,
                SplineMode  = ActorSplinePlaybackMode,
                UseRotation = true
            });
            passenger.AddPacketToSend(new ServerCinematicActorVisibility
            {
                Delay  = BoardingCompleteDelay,
                UnitId = passenger.UnitId,
                Hide   = true
            });

            AddActor(passenger, []);
        }

        private void AddShip(uint creature2Id, Position position, uint splineId, float speed)
        {
            IActor ship = new Actor(
                creature2Id,
                EntityCreateFlag.Immediate | EntityCreateFlag.HasInteractionPrereq,
                null,
                position,
                initialDelay: 0,
                textureLoDBias: 0);
            ship.AddPacketToSend(new ServerCinematicActorSpline
            {
                Delay       = BoardingCompleteDelay,
                UnitId      = ship.UnitId,
                SplineId    = splineId,
                SplineSpeed = speed,
                SplineMode  = ActorSplinePlaybackMode,
                UseRotation = true
            });

            AddActor(ship, []);
        }

        private void SetupCamera()
        {
            ICamera mainCam = new Camera(DepartureCameraSplineId, 0u, 0u, 1f, useRotation: true);
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
        }
    }
}
