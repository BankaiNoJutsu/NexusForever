using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 70: handler table <c>Prerequisite_CheckChallengeTier</c> (<c>14049ed20</c>).
    /// NF uses challenge completion count as tier progress until medal/tier runtime is modeled.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ChallengeTier)]
    public class PrerequisiteCheckChallengeTier : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint completionCount = player.ChallengeManager.GetCompletionCount((ushort)objectId);
            return PrerequisiteCompare.Compare(comparison, completionCount, value);
        }
    }
}
