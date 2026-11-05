using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Shared;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.PublicEvent
{
    public class PublicEventTeamMember : IPublicEventTeamMember
    {
        public ulong CharacterId { get; private set; }

        private IPlayer owner;
        private IGameSession session;
        private IBaseMap map;
        private uint unitId;
        private Static.Entity.Class @class;
        private Static.PlayerPath.Path path;

        private readonly IPublicEventStats stats = new PublicEventStats();

        #region Dependency Injection

        private readonly ILogger<PublicEventTeamMember> log;

        private readonly IPlayerManager playerManager;
        private readonly IRealmContext realmContext;

        public PublicEventTeamMember(
            ILogger<PublicEventTeamMember> log,
            IPlayerManager playerManager,
            IRealmContext realmContext)
        {
            this.log           = log;
            this.playerManager = playerManager;
            this.realmContext  = realmContext;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="PublicEventTeamMember"/> with <see cref="IPlayer"/>.
        /// </summary>
        public void Initialise(IPlayer player)
        {
            if (CharacterId != 0)
                throw new InvalidOperationException();

            CharacterId = player.CharacterId;
            owner       = player;
            session     = player.Session;
            map         = player.Map;
            unitId      = player.Guid;
            @class      = player.Class;
            path        = player.Path;

            log.LogTrace($"Initialised public event team member {CharacterId} with class {@class} and path {path}.");
        }

        public IPlayer GetCurrentPlayer()
        {
            if (session is null or NetworkSession { IsDisconnecting: true }
                || !ReferenceEquals(playerManager.GetPlayer(CharacterId), owner)
                || !ReferenceEquals(owner.Map, map)
                || !ReferenceEquals(owner.Session, session)
                || (session is IPlayerSession bound && !ReferenceEquals(bound.Player, owner)))
                return null;

            return owner;
        }

        /// <summary>
        /// Update <see cref="PublicEventStat"/> with supplied value.
        /// </summary>
        public void UpdateStat(PublicEventStat stat, uint value)
        {
            stats.UpdateStat(stat, value);
            log.LogTrace($"Updated public event team member {CharacterId} stat {stat} to {value}.");
        }

        /// <summary>
        /// Update custom stat with supplied value.
        /// </summary>
        public void UpdateCustomStat(uint index, uint value)
        {
            stats.UpdateCustomStat(index, value);
            log.LogTrace($"Updated public event team member {CharacterId} custom stat {index} to {value}.");
        }

        /// <summary>
        /// Send <see cref="IWritable"/> to <see cref="IPublicEventTeamMember"/>.
        /// </summary>
        public void Send(IWritable message)
        {
            if (GetCurrentPlayer() == null)
                return;

            try
            {
                session.EnqueueMessageEncrypted(message);
            }
            catch (Exception error)
            {
                // A member's delivery failure must not interrupt shared event progress.
                log.LogWarning(error, "Could not enqueue public event notice {Notice} for character {CharacterId}.",
                    message.GetType().Name, CharacterId);
            }
        }

        /// <summary>
        /// Build <see cref="Network.World.Message.Model.Shared.PublicEventStats"/> for <see cref="IPublicEventTeamMember"/>.
        /// </summary>
        public Network.World.Message.Model.Shared.PublicEventStats BuildStats()
        {
            return stats.Build();
        }

        /// <summary>
        /// Build <see cref="PublicEventParticipantStats"/> for <see cref="IPublicEventTeamMember"/>.
        /// </summary>
        public PublicEventParticipantStats BuildParticipantStats()
        {
            return new PublicEventParticipantStats
            {
                UnitId = unitId,
                Player = new NetworkIdentity
                {
                    Id      = CharacterId,
                    RealmId = realmContext.RealmId,
                },
                Class  = @class,
                Path   = path,
                Stats  = BuildStats()
            };
        }
    }
}
