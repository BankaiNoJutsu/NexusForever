using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check;

[PrerequisiteCheck(PrerequisiteType.ShieldCapacityPercent)]
public sealed class PrerequisiteCheckShieldCapacityPercent : IPrerequisiteCheck
{
    public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        => MeetsSubject(player, comparison, value);

    public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        => MeetsSubject(subject, comparison, value);

    internal static bool MeetsSubject(IUnitEntity subject, PrerequisiteComparison comparison, uint value)
        => subject != null && PrerequisitePercentCompare.Compare(comparison, subject.Shield, subject.MaxShieldCapacity, value);
}
