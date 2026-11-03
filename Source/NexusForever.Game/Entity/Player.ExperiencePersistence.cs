using NexusForever.Database.Character;
using NexusForever.Game.Achievement;
using NexusForever.Game.Map;
using NexusForever.Game.Reputation;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private ExperiencePersistence experiencePersistence;
        private XpManager experienceXpOwner;
        private PathManager experiencePathOwner;
        private SpellManager experienceSpellOwner;
        private PetCustomisationManager experiencePetOwner;
        private CharacterAchievementManager experienceAchievementOwner;
        private DatacubeManager experienceDatacubeOwner;
        private GalacticArchiveManager experienceArchiveOwner;
        private ZoneMapManager experienceMapOwner;
        private ReputationManager experienceReputationOwner;

        private ExperiencePersistence GetExperiencePersistence()
        {
            if (experiencePersistence != null) return experiencePersistence;
            var xp = XpManager as XpManager; var path = PathManager as PathManager;
            var spells = SpellManager as SpellManager; var pets = PetCustomisationManager as PetCustomisationManager; var achievements = AchievementManager as CharacterAchievementManager;
            var cubes = DatacubeManager as DatacubeManager; var archive = GalacticArchiveManager as GalacticArchiveManager;
            var maps = ZoneMapManager as ZoneMapManager; var reputation = ReputationManager as ReputationManager;
            if (xp == null && path == null && spells == null && pets == null && achievements == null && cubes == null && archive == null && maps == null && reputation == null) return null;
            ulong owner = CharacterId;
            if (xp != null && xp.ExperienceCharacter != owner || path != null && path.PathCharacter != owner || spells != null && spells.SpellbookCharacter != owner
                || pets != null && pets.PetCharacter != owner || achievements != null && achievements.AchievementCharacter != owner
                || cubes != null && cubes.CollectionCharacter != owner || archive != null && archive.ArchiveCharacter != owner
                || maps != null && maps.ExplorationCharacter != owner || reputation != null && reputation.ReputationCharacter != owner)
                throw new InvalidOperationException("Experience managers changed their Character owner.");
            string[] routes = new[] { xp?.LoadedExperienceDatabase, path?.LoadedPathDatabase, spells?.SpellbookDatabase, pets?.PetDatabase, achievements?.AchievementDatabase,
                cubes?.CollectionDatabase, archive?.ArchiveDatabase, maps?.ExplorationDatabase, reputation?.ReputationDatabase }.Where(route => route != null).Distinct().ToArray();
            if (routes.Length > 1)
                throw new InvalidOperationException("Experience managers were loaded from different databases.");
            string database = routes.SingleOrDefault();
            var baseline = new ExperiencePersistence.Snapshot(xp?.LoadedExperience, path?.LoadedSelection, path?.LoadedPaths ?? [], path?.LoadedMissions ?? [],
                spells?.LoadedSpellbook, pets?.LoadedExperiencePets, achievements?.LoadedExperienceAchievements, cubes?.LoadedCollection, archive?.LoadedArchive,
                maps?.LoadedExploration, reputation?.LoadedReputations);
            ExperiencePersistence.Snapshot Current()
            {
                if (CharacterId != owner || !ReferenceEquals(XpManager as XpManager, xp) || !ReferenceEquals(PathManager as PathManager, path)
                    || !ReferenceEquals(SpellManager as SpellManager, spells) || !ReferenceEquals(PetCustomisationManager as PetCustomisationManager, pets) || !ReferenceEquals(AchievementManager as CharacterAchievementManager, achievements)
                    || !ReferenceEquals(DatacubeManager as DatacubeManager, cubes) || !ReferenceEquals(GalacticArchiveManager as GalacticArchiveManager, archive)
                    || !ReferenceEquals(ZoneMapManager as ZoneMapManager, maps) || !ReferenceEquals(ReputationManager as ReputationManager, reputation))
                    throw new InvalidOperationException("Experience lost its loaded managers.");
                var paths = path?.CapturePathProgress();
                return new(xp?.CaptureExperience(), paths?.Selection, paths?.Paths ?? [], paths?.Missions ?? [],
                    spells?.CaptureSpellbook(), pets?.CaptureExperiencePets(), achievements?.CaptureExperienceAchievements(), cubes?.CaptureCollection(), archive?.CaptureArchive(),
                    maps?.CaptureExploration(), reputation?.CaptureReputations());
            }
            experienceXpOwner = xp; experiencePathOwner = path;
            experienceSpellOwner = spells; experiencePetOwner = pets; experienceAchievementOwner = achievements;
            experienceDatacubeOwner = cubes; experienceArchiveOwner = archive;
            experienceMapOwner = maps; experienceReputationOwner = reputation;
            void PublicationReady()
            {
                try { achievements?.NotifyPublicationReady(); }
                finally { archive?.NotifyPublicationReady(); }
            }
            return experiencePersistence = new(owner, baseline, Current, database, achievements == null ? null : achievements.QueuePublication,
                achievements?.RealmFirst, archive == null ? null : archive.AcknowledgePublication, PublicationReady);
        }
        internal void SaveExperience(CharacterContext context)
        {
            GetExperiencePersistence()?.Save(context);
            if (experienceXpOwner == null) XpManager.Save(context);
            if (experiencePathOwner == null) PathManager.Save(context);
        }
        private Action<CharacterContext> CaptureExperienceSave(object token, string database)
        {
            var frozen = GetExperiencePersistence()?.CaptureNormal(token, database);
            if (frozen == null) return null;
            return context =>
            {
                frozen(context);
                if (experienceXpOwner == null) XpManager.Save(context);
                if (experiencePathOwner == null) PathManager.Save(context);
            };
        }
    }
}
