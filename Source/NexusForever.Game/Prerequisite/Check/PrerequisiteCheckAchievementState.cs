using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.AchievementState)]
    public class PrerequisiteCheckAchievementState : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckAchievementState> log;

        public PrerequisiteCheckAchievementState(
            ILogger<PrerequisiteCheckAchievementState> log)
        {
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (value > 1u)
            {
                log.LogWarning($"Unhandled achievement state value {value} for {PrerequisiteType.AchievementState}!");
                return false;
            }

            uint achievementState = player.AchievementManager.HasCompletedAchievement((ushort)objectId)
                ? 1u
                : 0u;

            switch (comparison)
            {
                case PrerequisiteComparison.NotEqual:
                    return achievementState != value;
                case PrerequisiteComparison.Equal:
                    return achievementState == value;
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.AchievementState}!");
                    return false;
            }
        }
    }
}
