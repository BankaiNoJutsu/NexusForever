using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ChallengeRequirement)]
    public class PrerequisiteCheckChallengeRequirement : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckChallengeRequirement> log;

        public PrerequisiteCheckChallengeRequirement(
            ILogger<PrerequisiteCheckChallengeRequirement> log)
        {
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint completionCount = player.ChallengeManager.GetCompletionCount((ushort)objectId);

            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                    return completionCount == value;
                case PrerequisiteComparison.NotEqual:
                    return completionCount != value;
                case PrerequisiteComparison.GreaterThanOrEqual:
                    return completionCount >= value;
                case PrerequisiteComparison.GreaterThan:
                    return completionCount > value;
                case PrerequisiteComparison.LessThanOrEqual:
                    return completionCount <= value;
                case PrerequisiteComparison.LessThan:
                    return completionCount < value;
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.ChallengeRequirement}!");
                    return false;
            }
        }
    }
}
