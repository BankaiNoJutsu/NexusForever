using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Quest
{
    internal static class CommunicatorQuestState
    {
        private const uint NotInQuestLog = 11u;

        public static bool Meets(QuestState? actualState, uint requiredState)
        {
            if (requiredState == NotInQuestLog)
                return actualState == null;

            return TryGetServerQuestState(requiredState, out QuestState questState)
                && actualState == questState;
        }

        public static bool TryGetServerQuestState(uint requiredState, out QuestState questState)
        {
            switch (requiredState)
            {
                case (uint)QuestState.Unknown:
                case (uint)QuestState.Accepted:
                case (uint)QuestState.Achieved:
                case (uint)QuestState.Completed:
                case (uint)QuestState.Botched:
                case (uint)QuestState.Mentioned:
                case (uint)QuestState.Abandoned:
                case (uint)QuestState.Ignored:
                    questState = (QuestState)requiredState;
                    return true;
                default:
                    questState = default;
                    return false;
            }
        }
    }
}
