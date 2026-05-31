using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.InfrastructureState)]
    public class PrerequisiteCheckInfrastructureState : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckInfrastructureState> log;

        public PrerequisiteCheckInfrastructureState(
            ILogger<PrerequisiteCheckInfrastructureState> log)
        {
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            SettlerInfrastructureState currentState = player.PathManager.GetSettlerInfrastructureState(objectId);
            uint current = (uint)currentState;
            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                    return current == value;
                case PrerequisiteComparison.NotEqual:
                    return current != value;
                case PrerequisiteComparison.GreaterThanOrEqual:
                    return current >= value;
                case PrerequisiteComparison.GreaterThan:
                    return current > value;
                case PrerequisiteComparison.LessThanOrEqual:
                    return current <= value;
                case PrerequisiteComparison.LessThan:
                    return current < value;
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.InfrastructureState}!");
                    return false;
            }
        }
    }
}
