using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Game.Matching.Queue
{
    public class MatchingRoleCheckMember : IMatchingRoleCheckMember
    {
        public Identity Identity { get; private set; }
        public Role? Roles { get; private set; }
        private MatchingManager.Participant participant;

        #region Dependency Injection

        private readonly ILogger<MatchingRoleCheckMember> log;
        private readonly IPlayerManager playerManager;

        public MatchingRoleCheckMember(
            IPlayerManager playerManager,
            ILogger<MatchingRoleCheckMember> log)
        {
            this.playerManager = playerManager;
            this.log           = log;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="IMatchingRoleCheckMember"/> with supplied character id.
        /// </summary>
        public void Initialise(Identity identity)
        {
            if (Identity != null)
                throw new InvalidOperationException();

            Identity = identity;
            if (playerManager.GetPlayer(identity) is { } player) participant = new(player);
        }

        /// <summary>
        /// Set <see cref="Role"/> for member.
        /// </summary>
        public void SetRoles(Role roles)
        {
            if (Roles.HasValue)
                throw new InvalidOperationException();

            Roles = roles;

            log.LogTrace($"MatchingRoleCheckMember: {Identity} has responded with {Roles}.");
        }

        /// <summary>
        /// Send <see cref="IWritable"/> message to member.
        /// </summary>
        public void Send(IWritable message)
        {
            if (participant?.Current(playerManager) == true)
                participant.Player.Session.EnqueueMessageEncrypted(message);
        }
    }
}
