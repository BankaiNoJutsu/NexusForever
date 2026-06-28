using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Entity
{
    public class PathEntry : IPathEntry
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="IPathEntry"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum PathSaveMask
        {
            None        = 0x0000,
            Create      = 0x0001,
            Unlocked    = 0x0002,
            XPChange    = 0x0004,
            LevelChange = 0x0008
        }

        public Path Path { get; set; }
        public ulong CharacterId { get; set; }

        public bool Unlocked
        {
            get => unlocked;
            set
            {
                if (value != unlocked)
                {
                    unlocked = value;
                    saveMask |= PathSaveMask.Unlocked;
                }
            }
        }
        private bool unlocked;

        public uint TotalXp
        {
            get => totalXp;
            set
            {
                if (value < totalXp)
                    throw new ArgumentException("New Level Rewarded Value must be higher, and not equal to current XP total.");

                totalXp = value;
                saveMask |= PathSaveMask.XPChange;
            }
        }
        private uint totalXp;

        public byte LevelRewarded
        {
            get => levelRewarded;
            set
            {
                if (value < levelRewarded)
                    throw new ArgumentException("New Level Rewarded Value must be higher, and not equal to current XP total.");

                levelRewarded = value;
                saveMask |= PathSaveMask.LevelChange;
            }
        }
        private byte levelRewarded;

        private PathSaveMask saveMask;

        /// <summary>
        /// Create a new <see cref="IPathEntry"/> for a <see cref="IPlayer"/> from <see cref="CharacterPathModel"/>
        /// </summary>
        public PathEntry(CharacterPathModel model)
        {
            CharacterId   = model.Id;
            Path          = (Path)model.Path;
            unlocked      = Convert.ToBoolean(model.Unlocked);
            totalXp       = model.TotalXp;
            levelRewarded = model.LevelRewarded;

            saveMask      = PathSaveMask.None;
        }

        /// <summary>
        /// Create a new <see cref="IPathEntry"/>
        /// </summary>
        public PathEntry(ulong owner, Path path, bool isUnlocked)
        {
            CharacterId = owner;
            Path        = path;
            unlocked    = isUnlocked;

            saveMask    = PathSaveMask.Create;
        }

        /// <summary>
        /// Save the <see cref="CharacterPathModel"/> with it's current state
        /// </summary>
        /// <param name="context">The character context to save against</param>
        public void Save(CharacterContext context)
        {
            if (saveMask == PathSaveMask.None)
                return;

            if ((saveMask & PathSaveMask.Create) != 0)
            {
                CharacterPathModel model = new()
                {
                    Id            = CharacterId,
                    Path          = (byte)Path,
                    Unlocked      = Convert.ToByte(Unlocked),
                    TotalXp       = TotalXp,
                    LevelRewarded = LevelRewarded
                };

                if (!TryUpsertCreatedPath(context, model))
                    SaveCreatedPathWithChangeTracker(context, model);
            }
            else
            {
                // Path already exists in database, save only data that has been modified
                var model = new CharacterPathModel
                {
                    Id   = CharacterId,
                    Path = (byte)Path
                };

                EntityEntry<CharacterPathModel> entity = context.Attach(model);
                if ((saveMask & PathSaveMask.Unlocked) != 0)
                {
                    model.Unlocked = Convert.ToByte(Unlocked);
                    entity.Property(p => p.Unlocked).IsModified = true;
                }

                if ((saveMask & PathSaveMask.XPChange) != 0)
                {
                    model.TotalXp = TotalXp;
                    entity.Property(p => p.TotalXp).IsModified = true;
                }

                if ((saveMask & PathSaveMask.LevelChange) != 0)
                {
                    model.LevelRewarded = LevelRewarded;
                    entity.Property(p => p.LevelRewarded).IsModified = true;
                }
            }

            saveMask = PathSaveMask.None;
        }

        internal static void UpsertTrackedCreates(CharacterContext context)
        {
            List<CharacterPathModel> models = context.ChangeTracker.Entries<CharacterPathModel>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => new CharacterPathModel
                {
                    Id            = e.Entity.Id,
                    Path          = e.Entity.Path,
                    Unlocked      = e.Entity.Unlocked,
                    TotalXp       = e.Entity.TotalXp,
                    LevelRewarded = e.Entity.LevelRewarded
                })
                .ToList();

            foreach (CharacterPathModel model in models)
                if (!TryUpsertCreatedPath(context, model))
                    return;
        }

        private static bool TryUpsertCreatedPath(CharacterContext context, CharacterPathModel model)
        {
            string providerName = context.Database.ProviderName;
            if (providerName == null)
                return false;

            if (providerName.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                DetachTrackedPath(context, model.Id, model.Path);
                context.Database.ExecuteSqlInterpolated($@"
                    INSERT INTO `character_path` (`id`, `path`, `unlocked`, `totalXp`, `levelRewarded`)
                    VALUES ({model.Id}, {model.Path}, {model.Unlocked}, {model.TotalXp}, {model.LevelRewarded})
                    ON DUPLICATE KEY UPDATE
                        `unlocked` = {model.Unlocked},
                        `totalXp` = {model.TotalXp},
                        `levelRewarded` = {model.LevelRewarded};");
                return true;
            }

            if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                DetachTrackedPath(context, model.Id, model.Path);
                long characterId = checked((long)model.Id);
                context.Database.ExecuteSqlInterpolated($@"
                    INSERT INTO ""character_path"" (""id"", ""path"", ""unlocked"", ""totalXp"", ""levelRewarded"")
                    VALUES ({characterId}, {model.Path}, {model.Unlocked}, {model.TotalXp}, {model.LevelRewarded})
                    ON CONFLICT(""id"", ""path"") DO UPDATE SET
                        ""unlocked"" = excluded.""unlocked"",
                        ""totalXp"" = excluded.""totalXp"",
                        ""levelRewarded"" = excluded.""levelRewarded"";");
                return true;
            }

            return false;
        }

        private static void DetachTrackedPath(CharacterContext context, ulong characterId, byte path)
        {
            foreach (EntityEntry<CharacterPathModel> entry in context.ChangeTracker.Entries<CharacterPathModel>().ToList())
            {
                if (entry.Entity.Id != characterId || entry.Entity.Path != path)
                    continue;

                entry.State = EntityState.Detached;
            }
        }

        private static void SaveCreatedPathWithChangeTracker(CharacterContext context, CharacterPathModel model)
        {
            CharacterPathModel existing = context.CharacterPath.Find(model.Id, model.Path);
            if (existing == null)
            {
                // Path doesn't exist in database, all information must be saved.
                context.Add(model);
                return;
            }

            existing.Unlocked      = model.Unlocked;
            existing.TotalXp       = model.TotalXp;
            existing.LevelRewarded = model.LevelRewarded;
        }
    }
}
