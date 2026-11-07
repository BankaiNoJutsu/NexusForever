using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell
{
    /// <summary>
    /// Resolves a <see cref="TargetGroupEntry"/> to the set of creature ids that are members of it, following the semantics of the client evaluator
    /// <c>FUN_1403b4a20</c> (see <see cref="TargetGroupCriteriaEvaluator"/>): type 10 is an AND of its sub-groups, type 11 an OR, types 3/4 test the creature's faction and
    /// 12/13 its unit race. Quest objectives and activations identify creatures by id, so groups that only combine creature id lists are resolved with set algebra and
    /// every other group is evaluated against each Creature2 definition. See Decomp/Analysis/TARGETGROUP_QUEST_OBJECTIVE_MEMBERSHIP_20261107.md.
    /// </summary>
    internal static class TargetGroupCreatureResolver
    {
        /// <summary>
        /// Resolve <paramref name="entry"/> to creature ids using only creature id lists (types 1 and 9) and their AND/OR combinations (types 10 and 11).
        /// Returns <c>null</c> when the group needs unit attributes (any other type); a missing entry is an empty set.
        /// </summary>
        internal static HashSet<uint> TryResolveCreatureLists(TargetGroupEntry entry, IGameTableManager gameTableManager)
            => ResolveLists(entry, gameTableManager, new HashSet<uint>());

        /// <summary>
        /// Resolve <paramref name="entry"/> by evaluating it against every Creature2 definition (creature id, faction and unit race; a definition has no player race or class).
        /// Returns <c>null</c> when the Creature2 table is not loaded.
        /// </summary>
        internal static HashSet<uint> ResolveByCreatureAttributes(TargetGroupEntry entry, IGameTableManager gameTableManager)
        {
            GameTable<Creature2Entry> creatures = gameTableManager?.Creature2;
            if (creatures?.Entries == null)
                return null;

            var members = new HashSet<uint>();
            foreach (Creature2Entry creature in creatures.Entries)
            {
                var subject = new TargetGroupSubject(creature.Id, creature.FactionId, 0u, 0u, creature.UnitRaceId);
                if (TargetGroupCriteriaEvaluator.Evaluate(entry, subject, gameTableManager))
                    members.Add(creature.Id);
            }

            return members;
        }

        /// <summary>
        /// Whether the creature with id <paramref name="creatureId"/> is a member of <paramref name="entry"/>. The creature's faction and unit race come from its Creature2
        /// definition when that is loaded (0 otherwise), so creature id lists work without the table and attribute criteria need it.
        /// </summary>
        internal static bool IsMember(TargetGroupEntry entry, uint creatureId, IGameTableManager gameTableManager)
        {
            if (entry == null)
                return false;

            Creature2Entry creature = gameTableManager?.Creature2?.GetEntry(creatureId);
            var subject = new TargetGroupSubject(creatureId, creature?.FactionId ?? 0u, 0u, 0u, creature?.UnitRaceId ?? 0u);
            return TargetGroupCriteriaEvaluator.Evaluate(entry, subject, gameTableManager);
        }

        /// <summary>
        /// Resolve <paramref name="entry"/> to creature ids, by set algebra where possible and by Creature2 attributes otherwise.
        /// Returns an empty set when the group cannot be resolved because the Creature2 table is not loaded.
        /// </summary>
        internal static HashSet<uint> Resolve(TargetGroupEntry entry, IGameTableManager gameTableManager)
            => TryResolveCreatureLists(entry, gameTableManager)
                ?? ResolveByCreatureAttributes(entry, gameTableManager)
                ?? [];

        private static HashSet<uint> ResolveLists(TargetGroupEntry entry, IGameTableManager gameTableManager, ISet<uint> path)
        {
            if (entry == null)
                return [];

            switch (entry.Type)
            {
                case 1:
                case 9:
                    return new HashSet<uint>(NonZero(entry.DataEntries));
                case 10:
                case 11:
                    break;
                default:
                    return null;
            }

            // A cycle contributes nothing; the path (not a global visited set) is tracked so a sub-group shared by two siblings is still counted for both.
            if (!path.Add(entry.Id))
                return [];

            try
            {
                bool allOf = entry.Type == 10;
                HashSet<uint> result = null;
                foreach (uint subGroupId in NonZero(entry.DataEntries))
                {
                    if (subGroupId == entry.Id)
                        continue;

                    TargetGroupEntry subGroup = gameTableManager?.TargetGroup?.GetEntry(subGroupId);
                    if (subGroup == null)
                    {
                        // A missing row cannot pass: it adds nothing to an OR and makes an AND empty.
                        if (allOf)
                            return [];

                        continue;
                    }

                    HashSet<uint> members = ResolveLists(subGroup, gameTableManager, path);
                    if (members == null)
                        return null;

                    if (result == null)
                        result = new HashSet<uint>(members);
                    else if (allOf)
                        result.IntersectWith(members);
                    else
                        result.UnionWith(members);
                }

                // An AND with no sub-groups passes every unit, which only the attribute evaluation can express.
                return result ?? (allOf ? null : []);
            }
            finally
            {
                path.Remove(entry.Id);
            }
        }

        private static IEnumerable<uint> NonZero(uint[] dataEntries)
        {
            if (dataEntries == null)
                yield break;

            // The native loops skip zero slots rather than stopping at the first one.
            foreach (uint value in dataEntries)
            {
                if (value != 0u)
                    yield return value;
            }
        }
    }
}
