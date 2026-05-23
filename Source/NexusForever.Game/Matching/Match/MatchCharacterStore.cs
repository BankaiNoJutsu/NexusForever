using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Shared;

namespace NexusForever.Game.Matching.Match
{
    public class MatchCharacterStore : IMatchCharacterStore
    {
        private readonly Dictionary<Identity, IMatchCharacter> characters = [];

        private readonly IFactory<IMatchCharacter> matchCharacterFactory;

        public MatchCharacterStore(
            IFactory<IMatchCharacter> matchCharacterFactory)
        {
            this.matchCharacterFactory = matchCharacterFactory;
        }

        public IMatchCharacter GetMatchCharacter(Identity identity)
        {
            if (!characters.TryGetValue(identity, out IMatchCharacter characterInfo))
            {
                characterInfo = matchCharacterFactory.Resolve();
                characterInfo.Initialise(identity);
                characters.Add(identity, characterInfo);
            }

            return characterInfo;
        }
    }
}