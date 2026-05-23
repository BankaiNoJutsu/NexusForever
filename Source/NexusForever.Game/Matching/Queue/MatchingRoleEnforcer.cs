using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Shared;

namespace NexusForever.Game.Matching.Queue
{
    public class MatchingRoleEnforcer : IMatchingRoleEnforcer
    {
        private const Role ValidRoles = Role.Tank | Role.Healer | Role.DPS;

        #region Dependency Injection

        private readonly IFactory<IMatchingRoleEnforcerResult> resultFactory;

        public MatchingRoleEnforcer(
            IFactory<IMatchingRoleEnforcerResult> resultFactory)
        {
            this.resultFactory = resultFactory;
        }

        #endregion

        /// <summary>
        /// Check if supplied members have selected at least one valid role.
        /// </summary>
        public IMatchingRoleEnforcerResult Check(IEnumerable<IMatchingQueueProposalMember> members)
        {
            IMatchingRoleEnforcerResult result = resultFactory.Resolve();

            foreach (IMatchingQueueProposalMember member in members)
            {
                result.Members.Add(new MatchingRoleEnforcerResultMember
                {
                    Identity = member.Identity,
                    Role     = member.Roles
                });
            }

            Check(result);
            return result;
        }

        private bool Check(IMatchingRoleEnforcerResult result)
        {
            foreach (IMatchingRoleEnforcerResultMember resultMember in result.Members)
            {
                if (resultMember.Role == Role.None || (resultMember.Role & ~ValidRoles) != Role.None)
                {
                    result.Success = false;
                    return false;
                }
            }

            result.Success = true;
            return true;
        }
    }
}
