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
        {
            if (!TryGetVitalValue(player, (Vital)objectId, out uint currentValue))
            {
                log.LogWarning("Unhandled vital {Vital} for {PrerequisiteType}!", objectId, PrerequisiteType.Vital);
                return false;
            }

            return comparison switch
            {
                PrerequisiteComparison.Equal              => currentValue == value,
                PrerequisiteComparison.NotEqual           => currentValue != value,
                PrerequisiteComparison.GreaterThanOrEqual => currentValue >= value,
                PrerequisiteComparison.GreaterThan        => currentValue > value,
                PrerequisiteComparison.LessThanOrEqual    => currentValue <= value,
                PrerequisiteComparison.LessThan           => currentValue < value,
                _                                         => LogUnhandledComparison(comparison)
            };
        }

        private static bool TryGetVitalValue(IPlayer player, Vital vital, out uint currentValue)
        {
            currentValue = vital switch
            {
                Vital.Health          => player.Health,
                Vital.ShieldCapacity  => player.Shield,
                Vital.InterruptArmor  => player.InterruptArmor,
                _                     => 0u
            };

            return vital is Vital.Health or Vital.ShieldCapacity or Vital.InterruptArmor;
        }

        private bool LogUnhandledComparison(PrerequisiteComparison comparison)
        {
            log.LogWarning($"Unhandled {comparison} for {PrerequisiteType.Vital}!");
            return false;
        }
    }
}
