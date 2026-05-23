using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Matching;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Matching
{
    public sealed class DatabaseMatchingPenaltyStore : IMatchingPenaltyStore
    {
        private readonly IDatabaseManager databaseManager;

        public DatabaseMatchingPenaltyStore(IDatabaseManager databaseManager)
        {
            this.databaseManager = databaseManager;
        }

        public MatchingPenaltyState? Get(ulong characterId)
        {
            CharacterDatabase database = databaseManager.GetDatabase<CharacterDatabase>();
            CharacterMatchingPenaltyModel model = database?.GetCharacterMatchingPenalty(characterId);
            if (model == null)
                return null;

            return new MatchingPenaltyState(
                model.IsPvp,
                (MatchType)model.MatchType,
                model.Spell4Id,
                new DateTimeOffset(DateTime.SpecifyKind(model.ExpiresAtUtc, DateTimeKind.Utc)));
        }

        public void Save(ulong characterId, MatchingPenaltyState state)
        {
            CharacterDatabase database = databaseManager.GetDatabase<CharacterDatabase>();
            database?.UpsertCharacterMatchingPenalty(new CharacterMatchingPenaltyModel
            {
                Id           = characterId,
                IsPvp        = state.IsPvP,
                MatchType    = (byte)state.MatchType,
                Spell4Id     = state.Spell4Id,
                ExpiresAtUtc = state.ExpiresAtUtc.UtcDateTime
            });
        }

        public void Delete(ulong characterId)
        {
            CharacterDatabase database = databaseManager.GetDatabase<CharacterDatabase>();
            database?.DeleteCharacterMatchingPenalty(characterId);
        }
    }
}
