namespace NexusForever.Game.Abstract.Matching.Queue
{
    public interface IMatchingRoleEnforcer
    {
        /// <summary>
        /// Check if supplied members have selected at least one valid role.
        /// </summary>
        IMatchingRoleEnforcerResult Check(IEnumerable<IMatchingQueueProposalMember> members);
    }
}
