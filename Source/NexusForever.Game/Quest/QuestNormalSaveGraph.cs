using NexusForever.Database.Character;
using NexusForever.Game.Challenges.Reward;

namespace NexusForever.Game.Quest
{
    internal sealed record QuestNormalSaveGraph(Quest Owner, bool Needed, bool Deleted, Action<CharacterContext> Stage,
        CharacterChallengeQuestSnapshot Target);
}
