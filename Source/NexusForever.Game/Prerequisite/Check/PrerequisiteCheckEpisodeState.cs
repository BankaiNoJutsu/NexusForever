using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 9: the player's state of the episode <c>ObjectId</c> equals <c>Value</c> (Equal) or differs (NotEqual); other comparisons are false.
    /// </summary>
    /// <remarks>
    /// Mirrors the client recompute (<c>FUN_140787d10</c>) and aggregate (<c>FUN_140788b50</c>) as read by an agent, see Decomp/Analysis/PREREQUISITE_9_149_WRITERS_NATIVE_READ_20261108.md (not re-derived).
    /// One node per quest category (Quest2 <c>QuestCategoryId</c>, 0 counts as 1) present among the episode's quests that have a Quest2 row. Per quest, with the player's quest state (0 when none) and
    /// "optional" = <c>EpisodeQuest.Flags &amp; 1</c>: state 0 clears the category's done flag unless optional; states 1, 2 and 4 clear it the same way and raise the node to 2; state 3 raises the node to 2;
    /// states 5, 6 and 7 clear it the same way and raise the node to 1. A category whose done flag survives becomes 3. The episode state is 3 when every node is 3 or there are no nodes, otherwise the
    /// highest node that is not 3 (0 unknown, 1 mentioned, 2 active, 3 complete). An episode that is not in the Episode table has state 0 (inferred, the client's reader returns 0 for a missing key).
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.EpisodeState)]
    public class PrerequisiteCheckEpisodeState : IPrerequisiteCheck
    {
        private const uint OptionalQuestFlag = 1u;

        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckEpisodeState(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, GetEpisodeState(player, objectId) == value);

        private uint GetEpisodeState(IPlayer player, uint episodeId)
        {
            if (gameTableManager.Episode.GetEntry(episodeId) == null)
                return 0u;

            var categories = new Dictionary<uint, (uint Node, bool Done)>();
            foreach (EpisodeQuestEntry episodeQuest in gameTableManager.EpisodeQuest.Entries.Where(e => e.EpisodeId == episodeId))
            {
                Quest2Entry quest = gameTableManager.Quest2.GetEntry(episodeQuest.QuestId);
                if (quest == null)
                    continue;

                uint category = quest.QuestCategoryId == 0u ? 1u : quest.QuestCategoryId;
                (uint node, bool done) = categories.TryGetValue(category, out var existing) ? existing : (0u, true);

                bool optional = (episodeQuest.Flags & OptionalQuestFlag) != 0u;
                QuestState state = episodeQuest.QuestId <= ushort.MaxValue
                    ? player.QuestManager.GetQuestState((ushort)episodeQuest.QuestId) ?? QuestState.Unknown
                    : QuestState.Unknown;

                switch (state)
                {
                    case QuestState.Unknown:
                        done &= optional;
                        break;
                    case QuestState.Accepted:
                    case QuestState.Achieved:
                    case QuestState.Botched:
                        done &= optional;
                        node = Math.Max(node, 2u);
                        break;
                    case QuestState.Completed:
                        node = Math.Max(node, 2u);
                        break;
                    case QuestState.Mentioned:
                    case QuestState.Abandoned:
                    case QuestState.Ignored:
                        done &= optional;
                        node = Math.Max(node, 1u);
                        break;
                }

                categories[category] = (node, done);
            }

            uint[] nodes = categories.Values.Select(c => c.Done ? 3u : c.Node).ToArray();
            uint highestIncomplete = nodes.Where(n => n != 3u).DefaultIfEmpty(3u).Max();
            return highestIncomplete;
        }
    }
}
