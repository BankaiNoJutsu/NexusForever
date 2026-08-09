using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Shared.Diagnostics;
using NLog;

namespace NexusForever.Database.Character
{
    [Database(DatabaseType.Character)]
    public class CharacterDatabase : IDatabase
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private IConnectionString config;

        public void Initialise(IConnectionString connectionString)
        {
            config = connectionString;
        }

        public async Task Save(Action<CharacterContext> action)
        {
            await NexusForeverDiagnostics.MeasureDatabaseAsync("character", nameof(Save), async () =>
            {
                await using var context = new CharacterContext(config);
                action.Invoke(context);
                await context.SaveChangesAsync();
            });
        }

        /// <summary>
        /// Synchronously persist changes. Blocks the calling thread until the save completes.
        /// </summary>
        public void SaveBlocking(Action<CharacterContext> action)
        {
            Save(action).WaitUnwrap();
        }

        public async Task Save(IDatabaseCharacter entity)
        {
            await NexusForeverDiagnostics.MeasureDatabaseAsync("character", nameof(Save), async () =>
            {
                await using var context = new CharacterContext(config);
                entity.Save(context);
                await context.SaveChangesAsync();
            });
        }

        public async Task Save(IEnumerable<IDatabaseCharacter> entities)
        {
            await NexusForeverDiagnostics.MeasureDatabaseAsync("character", nameof(Save), async () =>
            {
                await using var context = new CharacterContext(config);
                foreach (IDatabaseCharacter entity in entities)
                    entity.Save(context);
                await context.SaveChangesAsync();
            });
        }

        public void Migrate()
        {
            using var context = new CharacterContext(config);

            List<string> migrations = context.Database.GetPendingMigrations().ToList();
            if (migrations.Count > 0)
            {
                log.Info($"Applying {migrations.Count} authentication database migration(s)...");
                foreach (string migration in migrations)
                    log.Info(migration);

                context.Database.Migrate();
            }
        }

        public List<CharacterModel> GetAllCharacters()
        {
            using var context = new CharacterContext(config);
            return context.Character.Where(c => c.DeleteTime == null).ToList();
        }

        public ulong GetNextCharacterId()
        {
            using var context = new CharacterContext(config);
            return GetMaxId(context.Character.Select(r => r.Id));
        }

        public async Task<CharacterModel> GetCharacterById(ulong characterId)
        {
            await using var context = new CharacterContext(config);
            return await context.Character.FirstOrDefaultAsync(e => e.Id == characterId);
        }

        public async Task<CharacterModel> GetCharacterByName(string name)
        {
            await using var context = new CharacterContext(config);
            return await context.Character.FirstOrDefaultAsync(e => e.Name == name);
        }

        public ulong GetNextItemId()
        {
            using var context = new CharacterContext(config);
            return GetMaxId(context.Item.Select(r => r.Id));
        }

        public List<RealmBankItemModel> GetRealmBankItems(uint accountId, ushort realmId)
        {
            using var context = new CharacterContext(config);
            return context.RealmBankItem
                .Where(i => i.AccountId == accountId && i.RealmId == realmId)
                .OrderBy(i => i.BagIndex)
                .ToList();
        }

        public ulong GetNextResidenceId()
        {
            using var context = new CharacterContext(config);
            return GetMaxId(context.Residence.Select(r => r.Id));
        }

        public ulong GetNextDecorId()
        {
            using var context = new CharacterContext(config);
            return GetMaxId(context.ResidenceDecor.Select(r => r.DecorId));
        }

        public async Task<List<CharacterModel>> GetCharacters(uint accountId)
        {
            return await NexusForeverDiagnostics.MeasureDatabaseAsync("character", nameof(GetCharacters), async () =>
            {
                using var context = new CharacterContext(config);
                return await context.Character.Where(c => c.AccountId == accountId)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(c => c.Appearance)
                    .Include(c => c.Customisation)
                    .Include(c => c.Item)
                    .Include(c => c.Bone)
                    .Include(c => c.Currency)
                    .Include(c => c.Path)
                    .Include(c => c.PathMission)
                    .Include(c => c.CharacterTitle)
                    .Include(c => c.Stat)
                    .Include(c => c.Costume)
                        .ThenInclude(c => c.CostumeItem)
                    .Include(c => c.PetCustomisation)
                    .Include(c => c.PetFlair)
                    .Include(c => c.Keybinding)
                    .Include(c => c.Spell)
                    .Include(c => c.ActionSetShortcut)
                    .Include(c => c.ActionSetAmp)
                    .Include(c => c.Datacube)
                    .Include(c => c.GalacticArchive)
                    .Include(c => c.Challenge)
                    .Include(c => c.Mail)
                        .ThenInclude(c => c.Attachment)
                            .ThenInclude(c => c.Item)
                    .Include(c => c.ZonemapHexgroup)
                    .Include(c => c.Quest)
                        .ThenInclude(c => c.QuestObjective)
                    .Include(c => c.Entitlement)
                    .Include(c => c.Achievement)
                    .Include(c => c.Tradeskill)
                    .Include(c => c.TradeskillMaterials)
                    .Include(c => c.Reputation)
                    .Include(c => c.Schematic)
                    .ToListAsync();
            });
        }

        public bool CharacterNameExists(string characterName)
        {
            using var context = new CharacterContext(config);
            return context.Character.Any(c => c.Name == characterName);
        }

        public List<ResidenceModel> GetResidences()
        {
            using var context = new CharacterContext(config);
            return context.Residence
                .Include(r => r.Plot)
                .Include(r => r.Decor)
                .Include(r => r.Neighbors)
                .Include(r => r.Character)
                .Include(r => r.Guild)
                // only load residences where the owner character or guild hasn't been deleted
                .Where(r => (r.OwnerId.HasValue && !r.Character.DeleteTime.HasValue) || (r.GuildOwnerId.HasValue && !r.Guild.DeleteTime.HasValue))
                .ToList();
        }

        public ulong GetNextMailId()
        {
            using var context = new CharacterContext(config);
            return GetMaxId(context.CharacterMail.Select(r => r.Id));
        }

        public ulong GetNextGuildId()
        {
            using var context = new CharacterContext(config);
            return GetMaxId(context.Guild.Select(r => r.Id));
        }

        public ulong GetNextMarketplaceAuctionId()
        {
            using var context = new CharacterContext(config);
            return GetMaxId(context.MarketplaceAuction.Select(a => a.Id));
        }

        public ulong GetNextMarketplaceCommodityOrderId()
        {
            using var context = new CharacterContext(config);
            return GetMaxId(context.MarketplaceCommodityOrder.Select(o => o.Id));
        }

        public ulong GetNextLeaderboardPveScoreId()
        {
            using var context = new CharacterContext(config);
            return GetMaxId(context.LeaderboardPveScore.Select(s => s.Id));
        }

        public ulong GetNextLeaderboardPvpScoreId()
        {
            using var context = new CharacterContext(config);
            return GetMaxId(context.LeaderboardPvpScore.Select(s => s.Id));
        }

        private ulong GetMaxId(IQueryable<ulong> ids)
        {
            if (config.Provider == DatabaseProvider.Sqlite)
            {
                long maxId = ids
                    .Select(id => (long?)id)
                    .Max() ?? 0L;
                return checked((ulong)maxId);
            }

            return ids
                .DefaultIfEmpty()
                .Max();
        }

        public List<LeaderboardPveScoreModel> GetLeaderboardPveScores(ushort realmId)
        {
            using var context = new CharacterContext(config);
            return context.LeaderboardPveScore
                .Where(s => s.RealmId == realmId)
                .AsNoTracking()
                .ToList();
        }

        public List<LeaderboardPvpScoreModel> GetLeaderboardPvpScores(ushort realmId)
        {
            using var context = new CharacterContext(config);
            return context.LeaderboardPvpScore
                .Where(s => s.RealmId == realmId)
                .AsNoTracking()
                .ToList();
        }

        public CharacterMatchingPenaltyModel GetCharacterMatchingPenalty(ulong characterId)
        {
            using var context = new CharacterContext(config);
            return context.CharacterMatchingPenalty
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == characterId);
        }

        public void UpsertCharacterMatchingPenalty(CharacterMatchingPenaltyModel model)
        {
            using var context = new CharacterContext(config);
            CharacterMatchingPenaltyModel existing = context.CharacterMatchingPenalty.Find(model.Id);
            if (existing == null)
            {
                context.CharacterMatchingPenalty.Add(model);
            }
            else
            {
                existing.IsPvp       = model.IsPvp;
                existing.MatchType   = model.MatchType;
                existing.Spell4Id    = model.Spell4Id;
                existing.ExpiresAtUtc = model.ExpiresAtUtc;
            }

            context.SaveChanges();
        }

        public void DeleteCharacterMatchingPenalty(ulong characterId)
        {
            using var context = new CharacterContext(config);
            CharacterMatchingPenaltyModel existing = context.CharacterMatchingPenalty.Find(characterId);
            if (existing == null)
                return;

            context.CharacterMatchingPenalty.Remove(existing);
            context.SaveChanges();
        }

        public List<MarketplaceAuctionModel> GetMarketplaceAuctions()
        {
            using var context = new CharacterContext(config);
            return context.MarketplaceAuction
                .Include(a => a.Item)
                .AsNoTracking()
                .ToList();
        }

        public List<MarketplaceCommodityOrderModel> GetMarketplaceCommodityOrders()
        {
            using var context = new CharacterContext(config);
            return context.MarketplaceCommodityOrder
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Credit a character's currency while offline. Online players should use <see cref="ICurrencyManager"/> instead.
        /// </summary>
        public void CreditCharacterCurrency(ulong characterId, byte currencyId, ulong amount)
        {
            if (amount == 0ul)
                return;

            using var context = new CharacterContext(config);
            CharacterCurrencyModel currency = context.CharacterCurrency
                .FirstOrDefault(c => c.Id == characterId && c.CurrencyId == currencyId);
            if (currency == null)
            {
                context.CharacterCurrency.Add(new CharacterCurrencyModel
                {
                    Id         = characterId,
                    CurrencyId = currencyId,
                    Amount     = amount
                });
            }
            else
                currency.Amount += amount;

            context.SaveChanges();
        }

        public List<GuildModel> GetGuilds()
        {
            using var context = new CharacterContext(config);
            return context.Guild
                .Where(g => g.DeleteTime == null)
                .Include(g => g.GuildRank)
                .Include(g => g.GuildMember)
                .Include(g => g.GuildData)
                .Include(g => g.Achievement)
                .ToList();
        }

        public HashSet<ushort> GetCompletedCharacterAchievementIds()
        {
            using var context = new CharacterContext(config);
            return context.CharacterAchievement
                .Where(a => a.DateCompleted != null)
                .Select(a => a.AchievementId)
                .Distinct()
                .ToHashSet();
        }

        public HashSet<ushort> GetCompletedGuildAchievementIds()
        {
            using var context = new CharacterContext(config);
            return context.GuildAchievement
                .Where(a => a.DateCompleted != null)
                .Select(a => a.AchievementId)
                .Distinct()
                .ToHashSet();
        }

        public List<ChatChannelModel> GetChatChannels()
        {
            using var context = new CharacterContext(config);
            return context.ChatChannel
                .Include(c => c.Members)
                .ToList();
        }

        public List<CharacterCreateModel> GetCharacterCreationData()
        {
            using var context = new CharacterContext(config);

            return context.CharacterCreate.ToList();
        }

        public List<PropertyBaseModel> GetProperties(uint type)
        {
            using var context = new CharacterContext(config);
                
            return context.PropertyBase.Where(p => p.Type == type).ToList();
        }
    }
}
