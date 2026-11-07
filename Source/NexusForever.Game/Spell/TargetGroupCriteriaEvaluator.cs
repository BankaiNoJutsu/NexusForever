using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell
{
    /// <summary>
    /// Evaluates <see cref="TargetGroupEntry"/> criteria against a world entity, implementing the
    /// client-side <c>ValidTargetsCriteria_Evaluate</c> logic decoded from Ghidra analysis
    /// (function at 1403b4a20). Returns <c>true</c> when the entity passes all criteria (valid
    /// target), <c>false</c> when the entity is rejected.
    /// </summary>
    internal static class TargetGroupCriteriaEvaluator
    {
        private const int MaxRecursionDepth = 5;

        /// <summary>
        /// Evaluate the supplied <see cref="TargetGroupEntry"/> against <paramref name="entity"/>.
        /// </summary>
        /// <param name="entry">The criteria entry to evaluate. A null entry always passes.</param>
        /// <param name="entity">The entity being tested.</param>
        /// <param name="gameTableManager">Game-table manager used for recursive sub-group lookups.</param>
        /// <param name="depth">Current recursion depth; capped at <see cref="MaxRecursionDepth"/>.</param>
        /// <returns><c>true</c> if the entity satisfies the criteria; <c>false</c> to reject it.</returns>
        internal static bool Evaluate(TargetGroupEntry entry, IWorldEntity entity, IGameTableManager gameTableManager, int depth = 0)
        {
            if (entry == null)
                return true;

            if (depth > MaxRecursionDepth)
                return true;

            switch (entry.Type)
            {
                // Types 1 & 2: the native evaluator compares the unit id from wrapper virtual +0x10 with the
                // seven data ids. Retail rows of both types list Creature2 ids (see the evaluator tests), so
                // type 1 is a creature id include list and type 2 its exclusion.
                case 1:
                    return ContainsValue(entry.DataEntries, entity.CreatureId);

                case 2:
                    return !ContainsValue(entry.DataEntries, entity.CreatureId);

                // Type 3: Faction2Id must-include
                case 3:
                {
                    uint faction2 = (uint)entity.Faction2;
                    return ContainsValue(entry.DataEntries, faction2);
                }

                // Type 4: Faction2Id must-exclude
                case 4:
                {
                    uint faction2 = (uint)entity.Faction2;
                    return !ContainsValue(entry.DataEntries, faction2);
                }

                // Type 5: RaceId must-include
                case 5:
                {
                    uint raceId = entity is IPlayer player5 ? (uint)player5.Race : 0u;
                    return ContainsValue(entry.DataEntries, raceId);
                }

                // Type 6: RaceId must-exclude
                case 6:
                {
                    uint raceId = entity is IPlayer player6 ? (uint)player6.Race : 0u;
                    return !ContainsValue(entry.DataEntries, raceId);
                }

                // Type 7: ClassId must-include
                case 7:
                {
                    uint classId = entity is IPlayer player7 ? (uint)player7.Class : 0u;
                    return ContainsValue(entry.DataEntries, classId);
                }

                // Type 8: ClassId must-exclude
                case 8:
                {
                    uint classId = entity is IPlayer player8 ? (uint)player8.Class : 0u;
                    return !ContainsValue(entry.DataEntries, classId);
                }

                // Type 9: Creature2Id list-match (any matching entry)
                case 9:
                {
                    uint creatureId = entity.CreatureId;
                    return ContainsValue(entry.DataEntries, creatureId);
                }

                // Type 10: sub-TargetGroup all-pass (recursive AND — all sub-entries must pass)
                case 10:
                    return EvaluateSubGroups(entry, entity, gameTableManager, depth, mustAllPass: true);

                // Type 11: sub-TargetGroup any-pass (recursive OR — the first passing sub-entry passes the group)
                case 11:
                    return EvaluateSubGroups(entry, entity, gameTableManager, depth, mustAllPass: false);

                // Type 12: UnitRaceId must-include
                case 12:
                {
                    uint unitRaceId = entity.CreatureEntry?.UnitRaceId ?? 0u;
                    return ContainsValue(entry.DataEntries, unitRaceId);
                }

                // Type 13: UnitRaceId must-exclude
                case 13:
                {
                    uint unitRaceId = entity.CreatureEntry?.UnitRaceId ?? 0u;
                    return !ContainsValue(entry.DataEntries, unitRaceId);
                }

                default:
                    return true;
            }
        }

        /// <summary>
        /// Returns whether <paramref name="value"/> appears in <paramref name="dataEntries"/>,
        /// stopping at the first zero slot (entries are zero-terminated).
        /// </summary>
        private static bool ContainsValue(uint[] dataEntries, uint value)
        {
            if (dataEntries == null)
                return false;

            for (int i = 0; i < dataEntries.Length; i++)
            {
                if (dataEntries[i] == 0u)
                    break;

                if (dataEntries[i] == value)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Evaluates each non-zero sub-group id of <paramref name="entry"/> (skipping its own id, as the native loop does) recursively.
        /// When <paramref name="mustAllPass"/> is <c>true</c> (type 10), all sub-groups must pass.
        /// When <c>false</c> (type 11), the first passing sub-group passes the group and none passing fails it.
        /// A sub-group row missing from a loaded table does not pass (native lookup failure); without a loaded
        /// table the legacy pass-through is kept.
        /// </summary>
        private static bool EvaluateSubGroups(TargetGroupEntry entry, IWorldEntity entity, IGameTableManager gameTableManager, int depth, bool mustAllPass)
        {
            uint[] dataEntries = entry.DataEntries;
            if (dataEntries == null)
                return true;

            GameTable<TargetGroupEntry> table = gameTableManager?.TargetGroup;
            for (int i = 0; i < dataEntries.Length; i++)
            {
                if (dataEntries[i] == 0u)
                    break;

                if (dataEntries[i] == entry.Id)
                    continue;

                bool subResult;
                if (table == null)
                    subResult = true;
                else
                {
                    TargetGroupEntry subEntry = table.GetEntry(dataEntries[i]);
                    subResult = subEntry != null && Evaluate(subEntry, entity, gameTableManager, depth + 1);
                }

                if (mustAllPass && !subResult)
                    return false;

                if (!mustAllPass && subResult)
                    return true;
            }

            return mustAllPass;
        }
    }
}
