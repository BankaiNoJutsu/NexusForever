using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Matching.Queue
{
    public class MatchingRoleCheck : IMatchingRoleCheck
    {
        public Guid Guid { get; } = Guid.NewGuid();
        public MatchingRoleCheckStatus Status { get; private set; }
        public IMatchingQueueProposal MatchingQueueProposal { get; private set; }

        private readonly Dictionary<Identity, IMatchingRoleCheckMember> members = [];
        private UpdateTimer expiryTimer;
        private Func<bool> canPublish;

        #region Dependency Injection

        private readonly ILogger<MatchingRoleCheck> log;
        private readonly IFactory<IMatchingRoleCheckMember> matchingRoleCheckMemberFactory;

        public MatchingRoleCheck(
            ILogger<MatchingRoleCheck> log,
            IFactory<IMatchingRoleCheckMember> matchingRoleCheckMemberFactory)
        {
            this.log                            = log;
            this.matchingRoleCheckMemberFactory = matchingRoleCheckMemberFactory;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="IMatchingRoleCheck"/> with the supplied <see cref="IMatchingQueueProposal"/> and character ids.
        /// </summary>
        public void Initialise(IMatchingQueueProposal matchingQueueProposal, List<Identity> identities, Func<bool> canPublish)
        {
            if (MatchingQueueProposal != null)
                throw new InvalidOperationException();

            this.canPublish = canPublish ?? throw new ArgumentNullException(nameof(canPublish));
            MatchingQueueProposal = matchingQueueProposal;

            foreach (Identity identity in identities)
            {
                IMatchingRoleCheckMember matchingRoleCheckMember = matchingRoleCheckMemberFactory.Resolve();
                matchingRoleCheckMember.Initialise(identity);
                members.Add(identity, matchingRoleCheckMember);
            }

            SendMatchingRoleCheckStarted();

            expiryTimer = new UpdateTimer(TimeSpan.FromSeconds(30d));

            log.LogTrace($"MatchingRoleCheck {Guid} has started, waiting for {members.Count} to respond.");
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            if (Status != MatchingRoleCheckStatus.Pending || !expiryTimer.IsTicking)
                return;

            expiryTimer.Update(lastTick);
            if (expiryTimer.HasElapsed)
            {
                Status = MatchingRoleCheckStatus.Expired;
                SendMatchingRoleCheckCancelled();
                log.LogTrace($"MatchingRoleCheck {Guid} has expired.");
            }
        }

        public IEnumerable<IMatchingRoleCheckMember> GetMembers()
        {
            return members.Values;
        }

        /// <summary>
        /// Set role response for supplied character id and <see cref="Role"/>.
        /// </summary>
        /// <remarks>
        /// This will also update the <see cref="MatchingRoleCheckStatus"/> to <see cref="MatchingRoleCheckStatus.Success"/> if all members have responded or <see cref="MatchingRoleCheckStatus.Declined"/> if any member has declined.
        /// </remarks>
        public void Respond(Identity identity, Role roles)
        {
            if (Status != MatchingRoleCheckStatus.Pending)
                throw new InvalidOperationException();

            if (!members.TryGetValue(identity, out IMatchingRoleCheckMember matchingRoleCheckMember))
                throw new InvalidOperationException();

            if (roles == Role.None) { Cancel(); return; }
            matchingRoleCheckMember.SetRoles(roles);

            uint respondedMembers = (uint)members.Values.Count(member => member.Roles.HasValue);
            log.LogTrace($"MatchingRoleCheck {Guid} received role {roles} response from {identity}, waiting for {members.Count - respondedMembers} to respond.");
            if (respondedMembers == members.Count)
            {
                Status = MatchingRoleCheckStatus.Success;
                log.LogTrace($"MatchingRoleCheck {Guid} has been completed.");
            }
        }

        public void Cancel()
        {
            if (Status is MatchingRoleCheckStatus.Declined or MatchingRoleCheckStatus.Expired) return;
            Status = MatchingRoleCheckStatus.Declined;
            SendMatchingRoleCheckCancelled();
        }

        private void SendMatchingRoleCheckStarted()
        {
            foreach (IMatchingRoleCheckMember member in members.Values.ToArray())
            {
                if (Status != MatchingRoleCheckStatus.Pending || !canPublish()) break;
                member.Send(new ServerMatchingRoleCheckStarted
                {
                    // this will change the UI to prompt the client for a role selection or just accept or decline
                    RolesRequired = !member.Roles.HasValue
                });
            }
        }

        private void SendMatchingRoleCheckCancelled()
        {
            Broadcast(() => new ServerMatchingQueueResultAnnounce { Result = MatchingQueueResult.Role });
        }

        private void Broadcast(Func<IWritable> message)
        {
            foreach (IMatchingRoleCheckMember member in members.Values.ToArray())
            {
                if (!canPublish()) break;
                member.Send(message());
            }
        }
    }
}
