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
                // Types 1 & 2: client-side FactionGroupId from the faction-state component.
                // NexusForever only exposes Faction1/Faction2, and those are not a proven analog,
                // so keep these rows pass-through until the relationship mapping is decoded.
                case 1:
                case 2:
                    return true;

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
                    return EvaluateSubGroups(entry.DataEntries, entity, gameTableManager, depth, mustAllPass: true);

                // Type 11: sub-TargetGroup all-fail (recursive NOT — all sub-entries must fail)
                case 11:
                    return EvaluateSubGroups(entry.DataEntries, entity, gameTableManager, depth, mustAllPass: false);

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
        /// Evaluates each non-zero sub-group id in <paramref name="dataEntries"/> recursively.
        /// When <paramref name="mustAllPass"/> is <c>true</c> (type 10), all sub-groups must pass.
        /// When <c>false</c> (type 11), all sub-groups must fail (entity is included only if every
        /// sub-group rejects it).
        /// </summary>
        private static bool EvaluateSubGroups(uint[] dataEntries, IWorldEntity entity, IGameTableManager gameTableManager, int depth, bool mustAllPass)
        {
            if (dataEntries == null)
                return true;

            for (int i = 0; i < dataEntries.Length; i++)
            {
                if (dataEntries[i] == 0u)
                    break;

                TargetGroupEntry subEntry = gameTableManager?.TargetGroup?.GetEntry(dataEntries[i]);
                bool subResult = Evaluate(subEntry, entity, gameTableManager, depth + 1);

                if (mustAllPass && !subResult)
                    return false;

                if (!mustAllPass && subResult)
                    return false;
            }

            return true;
        }
    }
}
