using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check;

[PrerequisiteCheck(PrerequisiteType.VitalPercent)]
public sealed class PrerequisiteCheckVitalPercent : IPrerequisiteCheck
{
    public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        => MeetsSubject(player, comparison, value, objectId);

    public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        => MeetsSubject(subject, comparison, value, objectId);

    internal static bool MeetsSubject(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId)
    {
        if (subject == null)
            return false;

        Vital vital = (Vital)objectId;
        if (vital == Vital.Health)
            return PrerequisitePercentCompare.Compare(comparison, subject.Health, subject.MaxHealth, value);
        if (vital == Vital.ShieldCapacity)
            return PrerequisitePercentCompare.Compare(comparison, subject.Shield, subject.MaxShieldCapacity, value);

        // Other supported pools use the existing runtime capacity contract.
        // Missing/unsupported capacities are not silently treated as zero.
        return subject.TryGetVitalValue(vital, out float current)
            && subject.TryGetVitalMax(vital, out float capacity)
            && PrerequisitePercentCompare.Compare(comparison, current, capacity, value);
    }
}
