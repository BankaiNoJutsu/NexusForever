using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model.Cinematic;

namespace NexusForever.Game.Cinematic
{
    public abstract class CinematicBase : ICinematicBase
    {
        protected IPlayer Player { get; set; }
        private IGameSession playbackSession;
        protected IGameSession Session => playbackSession ?? Player.Session;

        public ushort CinematicId { get; set; }
        public uint Duration { get; set; }
        public CinematicFlags InitialFlags { get; set; }
        public CancelType InitialCancelMode { get; set; }
        public Dictionary</* Id */ uint, IActor> Actors { get; } = [];
        public Dictionary</* Delay */ uint, /* TextId */ uint> Texts { get; } = [];
        public List<IKeyframeAction> Keyframes { get; } = [];
        public List<ICamera> Cameras { get; } = [];
        public ITransition StartTransition { get; protected set; }
        public ITransition EndTransition { get; protected set; }

        protected IActor playerActor;

        protected abstract void Setup();

        /// <summary>
        /// Add an <see cref="IActor"/>, and any initial <see cref="IVisualEffect"/> to the Cinematic playback
        /// </summary>
        protected void AddActor(IActor actor, List<IVisualEffect> initialVisuals)
        {
            foreach (IVisualEffect visualEffect in initialVisuals)
                actor.AddVisualEffect(visualEffect);

            Actors.Add(actor.UnitId, actor);
        }

        /// <summary>
        /// Set an <see cref="IActor"/> as the instance the Player will be spawned in. (Important for playback)
        /// </summary>
        protected void SetAsPlayerActor(IActor actor, Position initialPosition, uint attachmentId)
        {
            IActor player = new Actor(0, EntityCreateFlag.UseDefaultBirthSequence | EntityCreateFlag.Immediate | EntityCreateFlag.HasInteractionPrereq, 0f, initialPosition, textureLoDBias: 0);
            player.AddPacketToSend(new ServerCinematicPlatformAdd
            {
                Delay           = 0,
                ActorUnitId     = player.UnitId,
                PlatformUnitId  = actor.UnitId,
                AttachmentId    = attachmentId
            });
            playerActor         = player;
        }

        /// <summary>
        /// Get a readied <see cref="IActor"/> that is queued for the Cinematic playback based on it's CreatureType ID
        /// </summary>
        protected IActor GetActor(uint creatureType)
        {
            return Actors.Values.FirstOrDefault(i => i.Creature2Id == creatureType);
        }

        /// <summary>
        /// Add a localised text entry to the Cinematic playback. Will shown Cinematic subtitles to match if enabled by the Player.
        /// </summary>
        protected void AddText(uint textId, uint start, uint end)
        {
            Texts.Add(start, textId);
            Texts.Add(end, 0);
        }

        /// <summary>
        /// Add a <see cref="ICamera"/> to the Cinematic playback.
        /// </summary>
        protected void AddCamera(ICamera camera)
        {
            Cameras.Add(camera);
        }

        /// <summary>
        /// Starts Playback for this <see cref="ICinematicBase"/>, sending the packets to the Player.
        /// </summary>
        public void StartPlayback(IPlayer player)
        {
            StartPlayback(player, null, null);
        }

        internal void StartPlayback(IPlayer player, Action<ushort> starting, Func<bool> canPublish)
        {
            IGameSession previousSession = playbackSession;
            IPlayer previousPlayer = Player;
            IGameSession originalSession = player.Session;
            playbackSession = new CinematicPlaybackSession(originalSession,
                canPublish ?? (() => ReferenceEquals(player.Session, originalSession)));
            Player = player;
            try
            {
                Setup();
                if (canPublish?.Invoke() == false)
                    return;
                ushort id = CinematicId;
                starting?.Invoke(id);
                Session.EnqueueMessageEncrypted(new ServerCinematicNotify
                {
                    Flags = InitialFlags,
                    CancelMode = InitialCancelMode,
                    Delay = Duration,
                    CinematicId = id
                });
                StartTransition?.Send(Session);
                Play();
                EndTransition?.Send(Session);
                Session.EnqueueMessageEncrypted(new ServerCinematicNotify { Delay = Duration });
                Session.EnqueueMessageEncrypted(new ServerCinematicComplete());
            }
            finally
            {
                playbackSession = previousSession;
                if (previousSession != null)
                    Player = previousPlayer;
            }
        }

        /// <summary>
        /// Sends all packet data to the Player for all <see cref="IActor"/> stored for playback.
        /// </summary>
        protected void SendActors()
        {
            foreach (IActor actor in Actors.Values)
                actor.SendInitialPackets(Session);
        }

        /// <summary>
        /// Sends all packet data to the Player for all VoiceOvers stored for playback.
        /// </summary>
        protected void SendText()
        {
            foreach ((uint delay, uint textId) in Texts)
            {
                Session.EnqueueMessageEncrypted(new ServerCinematicText
                {
                    Delay  = delay,
                    LocalizedTextId = textId
                });
            }
        }

        /// <summary>
        /// Sends packet data to the Player for the Player <see cref="IActor"/> instance stored for playback.
        /// </summary>
        protected void SendPlayerActor()
        {
            playerActor?.SendInitialPackets(Session);

            Session.EnqueueMessageEncrypted(new ServerCinematicStart
            {
                Show = true
            });
            Session.EnqueueMessageEncrypted(new ServerCinematicActorVisibility
            {
                Hide              = true,
                AffectOnlyPlayers = true
            });

            if (playerActor == null)
                return;

            Session.EnqueueMessageEncrypted(new ServerCinematicDelayFlag());
            Session.EnqueueMessageEncrypted(new ServerCinematicTransitionPosition
            {
                Position = new Position(Player.Position)
            });
        }

        /// <summary>
        /// Sends all packet data to the Player for all <see cref="ICamera"/> stored for playback.
        /// </summary>
        protected void SendCameras()
        {
            foreach (ICamera camera in Cameras)
                camera.SendInitialPackets(Session);
        }

        /// <summary>
        /// This method calls all individual packet send methods. It may be overridden by a Cinematic Script if playback needs to be customised.
        /// </summary>
        protected virtual void Play()
        {
            SendActors();
            SendPlayerActor();
            SendText();

            SendCameras();
        }
    }
}
