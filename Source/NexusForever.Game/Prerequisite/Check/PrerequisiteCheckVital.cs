using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.Vital)]
    public class PrerequisiteCheckVital : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckVital> log;

        public PrerequisiteCheckVital(
            ILogger<PrerequisiteCheckVital> log)
        {
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            if (!subject.TryGetVitalValue((Vital)objectId, out float currentValue))
            {
                log.LogWarning("Unhandled vital {Vital} for {PrerequisiteType}!", objectId, PrerequisiteType.Vital);
                return false;
            }

            // Live type73 truncates the float vital to signed int64 before
            // comparing its low32 bits as unsigned (1404a23ed / 1404a2090).
            uint current = PrerequisiteCompare.TruncateNativeScalar(currentValue);
            return comparison switch
            {
                PrerequisiteComparison.Equal              => current == value,
                PrerequisiteComparison.NotEqual           => current != value,
                PrerequisiteComparison.GreaterThanOrEqual => current >= value,
                PrerequisiteComparison.GreaterThan        => current > value,
                PrerequisiteComparison.LessThanOrEqual    => current <= value,
                PrerequisiteComparison.LessThan           => current < value,
                _                                         => LogUnhandledComparison(comparison)
            };
        }

        private bool LogUnhandledComparison(PrerequisiteComparison comparison)
        {
            log.LogWarning($"Unhandled {comparison} for {PrerequisiteType.Vital}!");
            return false;
        }
    }
}
