using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.WorldRequirement)]
    public class PrerequisiteCheckWorldRequirement : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckWorldRequirement> log;

        public PrerequisiteCheckWorldRequirement(
            ILogger<PrerequisiteCheckWorldRequirement> log)
        {
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint currentValue = 0u;

            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                    return currentValue == value;
                case PrerequisiteComparison.NotEqual:
                    return currentValue != value;
                case PrerequisiteComparison.GreaterThan:
                    return currentValue > value;
                case PrerequisiteComparison.GreaterThanOrEqual:
                    return currentValue >= value;
                case PrerequisiteComparison.LessThan:
                    return currentValue < value;
                case PrerequisiteComparison.LessThanOrEqual:
                    return currentValue <= value;
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.WorldRequirement}!");
                    return false;
            }
        }
    }
}
