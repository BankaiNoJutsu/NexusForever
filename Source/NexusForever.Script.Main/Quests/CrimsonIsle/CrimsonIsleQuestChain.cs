using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    internal static class CrimsonIsleQuestChain
    {
        public const ushort Q5573PoweringDown = 5573;
        public const ushort Q5575SeizingPower = 5575;
        public const ushort Q5580EnforcedRadioSilence = 5580;
        public const ushort Q5583HeavyArmor = 5583;
        public const ushort Q5594LastResistance = 5594;
        public const ushort Q5596OrdnanceRecovery = 5596;
        public const ushort Q8855StasisInterrupted = 8855;

        public static void GrantQuestsIfMissing(
            IQuest owner,
            IGlobalQuestManager globalQuestManager,
            ILogger log,
            params ushort[] questIds)
        {
            foreach (ushort questId in questIds)
                GrantQuestIfMissing(owner, globalQuestManager, log, questId);
        }

        public static void GrantQuestIfPrerequisitesComplete(
            IQuest owner,
            IGlobalQuestManager globalQuestManager,
            ILogger log,
            ushort questId,
            params ushort[] prerequisiteQuestIds)
        {
            foreach (ushort prerequisiteQuestId in prerequisiteQuestIds)
            {
                if (owner.Player.QuestManager.GetQuestState(prerequisiteQuestId) != QuestState.Completed)
                    return;
            }

            GrantQuestIfMissing(owner, globalQuestManager, log, questId);
        }

        private static void GrantQuestIfMissing(
            IQuest owner,
            IGlobalQuestManager globalQuestManager,
            ILogger log,
            ushort questId)
        {
            if (owner.Player.QuestManager.GetQuestState(questId) != null)
                return;

            IQuestInfo info = globalQuestManager.GetQuestInfo(questId);
            if (info == null)
            {
                log.LogWarning("Crimson Isle follow-up quest {NextQuestId} info missing after quest {QuestId}.",
                    questId,
                    owner.Id);
                return;
            }

            owner.Player.QuestManager.QuestAdd(info);
            log.LogDebug("Granted Crimson Isle follow-up quest {NextQuestId} after quest {QuestId}.",
                questId,
                owner.Id);
        }
    }
}
