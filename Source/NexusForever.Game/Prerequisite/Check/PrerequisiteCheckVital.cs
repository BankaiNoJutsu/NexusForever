using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.Vital)]
    public class PrerequisiteCheckVital : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;
        private readonly ILogger<PrerequisiteCheckVital> log;

        public PrerequisiteCheckVital(
            IGameTableManager gameTableManager,
            ILogger<PrerequisiteCheckVital> log)
        {
            this.gameTableManager = gameTableManager;
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            if (!TryGetNativeVital(subject, objectId, out float currentValue))
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

        /// <summary>
        /// Resolve a vital id the way the client switch 140466520 does.
        /// </summary>
        /// <remarks>
        /// The switch reads Health, Breath, Shield, Resource0 to Resource6, Focus, Resource7, InterruptArmor, Absorption, three public event resources and Resource8 to Resource10 directly.
        /// Any other id goes through the class object of the local player (virtual +0x60, 1405e78c0): the first of the eight Class.vitalEnumResource words equal to the id whose resource
        /// slot holds a positive value gives that slot; otherwise, and for any unit that is not the local player, the value is 0 and the comparison still runs.
        /// Breath is stat 19 (see <see cref="Stat.Breath"/>). Not modelled here, answered false as before: the public event resources and Resource8 to Resource10.
        /// Evidence tier: native bodies. No retail parity claim.
        /// </remarks>
        private bool TryGetNativeVital(IUnitEntity subject, uint vitalId, out float value)
        {
            switch ((Vital)vitalId)
            {
                case Vital.Health:
                case Vital.ShieldCapacity:
                case Vital.Resource0:
                case Vital.Resource1:
                case Vital.Resource2:
                case Vital.Resource3:
                case Vital.Resource4:
                case Vital.Resource5:
                case Vital.Resource6:
                case Vital.Focus:
                case Vital.Resource7:
                case Vital.InterruptArmor:
                case Vital.Breath:
                    return subject.TryGetVitalValue((Vital)vitalId, out value);
                case Vital.Absorption:
                    value = subject.CurrentAbsorption;
                    return true;
                case Vital.PublicResource0:
                case Vital.PublicResource1:
                case Vital.PublicResource2:
                case Vital.Resource8:
                case Vital.Resource9:
                case Vital.Resource10:
                    value = 0f;
                    return false;
                default:
                    value = GetClassResource(subject as IPlayer, vitalId);
                    return true;
            }
        }

        private float GetClassResource(IPlayer player, uint vitalId)
        {
            ClassEntry entry = player == null ? null : gameTableManager?.Class?.GetEntry((uint)player.Class);
            if (entry == null)
                return 0f;

            uint[] classVitals =
            [
                entry.VitalEnumResource00, entry.VitalEnumResource01, entry.VitalEnumResource02, entry.VitalEnumResource03,
                entry.VitalEnumResource04, entry.VitalEnumResource05, entry.VitalEnumResource06, entry.VitalEnumResource07
            ];

            for (int slot = 0; slot < classVitals.Length; slot++)
            {
                if (classVitals[slot] != vitalId)
                    continue;

                Vital slotVital = slot == 7 ? Vital.Resource7 : Vital.Resource0 + slot;
                if (player.TryGetVitalValue(slotVital, out float value) && value > 0f)
                    return value;
            }

            return 0f;
        }

        private bool LogUnhandledComparison(PrerequisiteComparison comparison)
        {
            log.LogWarning($"Unhandled {comparison} for {PrerequisiteType.Vital}!");
            return false;
        }
    }
}
