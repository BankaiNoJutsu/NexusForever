using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check;

/// <summary>
/// Native type 16 compares the evaluated unit's 3D distance from a WorldLocation2
/// center, provided the location belongs to the current world.
/// </summary>
[PrerequisiteCheck(PrerequisiteType.DistanceToWorldLocation)]
public sealed class PrerequisiteCheckDistanceToWorldLocation : IPrerequisiteCheck
{
    private readonly IGameTableManager gameTableManager;

    public PrerequisiteCheckDistanceToWorldLocation(IGameTableManager gameTableManager)
    {
        this.gameTableManager = gameTableManager;
    }

    public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        => MeetsUnit(player, comparison, value, objectId, parameters);

    public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
    {
        WorldLocation2Entry location = gameTableManager?.WorldLocation2?.GetEntry(objectId);
        if (subject?.Map?.Entry == null || location == null || subject.Map.Entry.Id != location.WorldId)
            return false;

        float x = subject.Position.X - location.Position0;
        float y = subject.Position.Y - location.Position1;
        float z = subject.Position.Z - location.Position2;
        float distance = MathF.Sqrt(x * x + y * y + z * z);

        // The live dispatcher uses MOVD/CVTDQ2PS on value0 before vtable +0x278.
        // Preserve signed int32 conversion and IEEE comparisons, including NaN.
        float threshold = unchecked((int)value);
        return comparison switch
        {
            PrerequisiteComparison.Equal              => distance == threshold,
            PrerequisiteComparison.NotEqual           => distance != threshold,
            PrerequisiteComparison.GreaterThan        => distance > threshold,
            PrerequisiteComparison.GreaterThanOrEqual => distance >= threshold,
            PrerequisiteComparison.LessThan           => distance < threshold,
            PrerequisiteComparison.LessThanOrEqual    => distance <= threshold,
            _ => false
        };
    }
}
