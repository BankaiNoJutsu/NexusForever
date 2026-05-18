using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Static;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Character
{
    public static class CharacterCreationValidation
    {
        public static bool HasSupportedStartingLocation(CharacterCreationEntry entry, ICharacterManager characterManager)
        {
            if (entry.CostumeOnly)
                return false;

            if (!IsSupportedStart(entry.CharacterCreationStartEnum))
                return false;

            return characterManager.GetStartingLocation(entry.RaceId, entry.FactionId, entry.CharacterCreationStartEnum) != null;
        }

        public static bool IsSupportedStart(CharacterCreationStart start)
        {
            return start is CharacterCreationStart.Nexus
                or CharacterCreationStart.PreTutorial
                or CharacterCreationStart.Level50;
        }
    }
}
