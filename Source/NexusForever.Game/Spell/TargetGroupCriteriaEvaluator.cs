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
    /// <summary>
    /// The unit attributes the native target-group evaluator reads: creature id, Faction2, player race and class (0 for non-players) and the creature's unit race.
    /// </summary>
    internal readonly record struct TargetGroupSubject(uint CreatureId, uint Faction2, uint PlayerRace, uint PlayerClass, uint UnitRaceId);

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
            var subject = new TargetGroupSubject(
                entity.CreatureId,
                (uint)entity.Faction2,
                entity is IPlayer player ? (uint)player.Race : 0u,
                entity is IPlayer classPlayer ? (uint)classPlayer.Class : 0u,
                entity.CreatureEntry?.UnitRaceId ?? 0u);

            return Evaluate(entry, subject, gameTableManager, depth);
        }

        /// <summary>
        /// Evaluate <paramref name="entry"/> against the attributes of one unit. Used directly for creature definitions (see <c>TargetGroupCreatureResolver</c>),
        /// which have a creature id, faction and unit race but no player race or class.
        /// </summary>
        internal static bool Evaluate(TargetGroupEntry entry, in TargetGroupSubject subject, IGameTableManager gameTableManager, int depth = 0)
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
                    return ContainsValue(entry.DataEntries, subject.CreatureId);

                case 2:
                    return !ContainsValue(entry.DataEntries, subject.CreatureId);

                // Type 3: Faction2Id must-include
                case 3:
                    return ContainsValue(entry.DataEntries, subject.Faction2);

                // Type 4: Faction2Id must-exclude
                case 4:
                    return !ContainsValue(entry.DataEntries, subject.Faction2);

                // Type 5: RaceId must-include
                case 5:
                    return ContainsValue(entry.DataEntries, subject.PlayerRace);

                // Type 6: RaceId must-exclude
                case 6:
                    return !ContainsValue(entry.DataEntries, subject.PlayerRace);

                // Type 7: ClassId must-include
                case 7:
                    return ContainsValue(entry.DataEntries, subject.PlayerClass);

                // Type 8: ClassId must-exclude
                case 8:
                    return !ContainsValue(entry.DataEntries, subject.PlayerClass);

                // Type 9: Creature2Id list-match (any matching entry)
                case 9:
                    return ContainsValue(entry.DataEntries, subject.CreatureId);

                // Type 10: sub-TargetGroup all-pass (recursive AND — all sub-entries must pass)
                case 10:
                    return EvaluateSubGroups(entry, subject, gameTableManager, depth, mustAllPass: true);

                // Type 11: sub-TargetGroup any-pass (recursive OR — the first passing sub-entry passes the group)
                case 11:
                    return EvaluateSubGroups(entry, subject, gameTableManager, depth, mustAllPass: false);

                // Type 12: UnitRaceId must-include
                case 12:
                    return ContainsValue(entry.DataEntries, subject.UnitRaceId);

                // Type 13: UnitRaceId must-exclude
                case 13:
                    return !ContainsValue(entry.DataEntries, subject.UnitRaceId);

                default:
                    return true;
            }
        }

        /// <summary>
        /// Returns whether <paramref name="value"/> appears in a non-zero slot of <paramref name="dataEntries"/>.
        /// The native loop (<c>FUN_1403b4a20</c>) tests every slot and skips zeros rather than stopping at the first zero; 18 retail rows have ids after a zero slot.
        /// </summary>
        private static bool ContainsValue(uint[] dataEntries, uint value)
        {
            if (dataEntries == null)
                return false;

            for (int i = 0; i < dataEntries.Length; i++)
            {
                if (dataEntries[i] != 0u && dataEntries[i] == value)
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
        private static bool EvaluateSubGroups(TargetGroupEntry entry, in TargetGroupSubject subject, IGameTableManager gameTableManager, int depth, bool mustAllPass)
        {
            uint[] dataEntries = entry.DataEntries;
            if (dataEntries == null)
                return true;

            GameTable<TargetGroupEntry> table = gameTableManager?.TargetGroup;
            for (int i = 0; i < dataEntries.Length; i++)
            {
                if (dataEntries[i] == 0u || dataEntries[i] == entry.Id)
                    continue;

                bool subResult;
                if (table == null)
                    subResult = true;
                else
                {
                    TargetGroupEntry subEntry = table.GetEntry(dataEntries[i]);
                    subResult = subEntry != null && Evaluate(subEntry, subject, gameTableManager, depth + 1);
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
