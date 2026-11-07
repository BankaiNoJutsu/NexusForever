using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 6: native test <c>FUN_14049f690</c> reads the player's state of quest <c>ObjectId</c> (<c>FUN_1405fbc40</c>), maps the states 4 to 7 (Botched, Mentioned, Abandoned, Ignored) to 0 and
    /// compares the result for equality with <c>Value</c>; Equal returns that, NotEqual its negation and every other comparison is false. A quest the player has no record of has state 0, so
    /// <c>Value</c> 0 (1,151 of 8,790 retail slots) means "not started", which also covers quests that were mentioned, abandoned, botched or ignored. Retail: Equal 6,360, NotEqual 2,430.
    /// The previous implementation compared the raw state, so a quest with no record (null) never equalled 0 and the four mapped states never did either.
    /// See Decomp/Analysis/PREREQUISITE_QUEST_STATE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.QuestState)]
    public class PrerequisiteCheckQuestState : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckQuestState> log;

        public PrerequisiteCheckQuestState(
            ILogger<PrerequisiteCheckQuestState> log)
        {
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            QuestState state = objectId <= ushort.MaxValue
                ? player.QuestManager.GetQuestState((ushort)objectId) ?? QuestState.Unknown
                : QuestState.Unknown;

            uint mapped = state is QuestState.Botched or QuestState.Mentioned or QuestState.Abandoned or QuestState.Ignored
                ? 0u
                : (uint)state;

            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                    return mapped == value;
                case PrerequisiteComparison.NotEqual:
                    return mapped != value;
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.QuestState}!");
                    return false;
            }
        }
    }
}
