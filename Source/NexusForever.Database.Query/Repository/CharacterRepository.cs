using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Query.Model;
using System.Runtime.CompilerServices;

namespace NexusForever.Database.Query.Repository
{
    public class CharacterRepository
    {
        #region Dependency Injection

        private readonly QueryContext _context;

        public CharacterRepository(
            QueryContext context)
        {
            _context = context;
        }

        #endregion

        public void AddCharacter(CharacterModel character)
        {
            _context.Character.Add(character);
        }

        /// <summary>
        /// Seed a missing API cache row without replacing a concurrent writer's data.
        /// Participates in the caller's transaction when one exists.
        /// </summary>
        public async Task<CharacterModel> EnsureCharacterAsync(CharacterModel character)
        {
            bool sqlite = _context.Database.IsSqlite();
            string provider = _context.Database.ProviderName;
            string suffix = sqlite ? " ON CONFLICT (`characterId`, `realmId`) DO NOTHING"
                : provider is "Microting.EntityFrameworkCore.MySql" or "Pomelo.EntityFrameworkCore.MySql"
                    ? " ON DUPLICATE KEY UPDATE `characterId` = `characterId`"
                    : throw new NotSupportedException($"Unsupported query cache provider {provider}.");

            const string insert = "INSERT INTO `character` (`characterId`, `realmId`, `name`, `realmName`, `race`, `class`, `path`, `faction`, `sex`, `currentRealmId`, `worldZoneId`, `level`, `guildName`, `lastOnline`) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13})";
            await _context.Database.ExecuteSqlInterpolatedAsync(FormattableStringFactory.Create(insert + suffix,
                character.CharacterId, character.RealmId, character.Name, character.RealmName,
                (byte)character.Race, (byte)character.Class, (byte)character.Path, (uint)character.Faction,
                (byte)character.Sex, character.CurrentRealmId, character.WorldZoneId, character.Level,
                character.GuildName, character.LastOnline));

            // An outer MySQL transaction may already have a repeatable-read snapshot.
            if (!sqlite && _context.Database.CurrentTransaction != null)
                return (await _context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE `characterId` = {character.CharacterId} AND `realmId` = {character.RealmId} FOR UPDATE").ToListAsync()).Single();

            return await GetCharacterAsync(character.CharacterId, character.RealmId);
        }

        public async Task<CharacterModel> GetCharacterAsync(ulong characterId, uint realmId)
        {
            return await _context.Character.SingleOrDefaultAsync(c => c.CharacterId == characterId && c.RealmId == realmId);
        }
    }
}
