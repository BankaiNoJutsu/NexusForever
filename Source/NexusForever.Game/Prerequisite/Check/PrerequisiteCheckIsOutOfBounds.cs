using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 141: native test <c>FUN_14049e540</c> looks up the WorldLocation2 row <c>ObjectId</c> (<c>FUN_14024b980</c>); a missing row, or one in a different world from the
    /// current one, fails for every comparison. Otherwise the unit's 3D distance from the row position is compared with the row's <c>Radius</c> (strictly less than): Equal returns that
    /// result, NotEqual its negation and any other comparison is false. Retail: 35 Equal, 1 NotEqual, <c>ObjectId</c> a WorldLocation2 id, <c>Value</c> unused.
    /// See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.IsOutOfBounds)]
    public class PrerequisiteCheckIsOutOfBounds : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckIsOutOfBounds(IGameTableManager gameTableManager)
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
            bool inside = MathF.Sqrt(x * x + y * y + z * z) < location.Radius;

            return comparison switch
            {
                PrerequisiteComparison.Equal    => inside,
                PrerequisiteComparison.NotEqual => !inside,
                _                               => false
            };
        }
    }
}
