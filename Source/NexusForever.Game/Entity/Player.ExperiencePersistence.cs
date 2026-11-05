using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Achievement;
using NexusForever.Game.Map;
using NexusForever.Game.Reputation;
using NexusForever.Network.Session;

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
                var standing = reputation?.CaptureReputations();
                return new(xp?.CaptureExperience(), paths?.Selection, paths?.Paths ?? [], paths?.Missions ?? [],
                    spells?.CaptureSpellbook(), pets?.CaptureExperiencePets(), achievements?.CaptureExperienceAchievements(), cubes?.CaptureCollection(), archive?.CaptureArchive(),
                    maps?.CaptureExploration(), standing?.Rows, standing?.Sequence ?? 0, spells?.ActionSetSequence ?? 0);
            }
            experienceXpOwner = xp; experiencePathOwner = path;
            experienceSpellOwner = spells; experiencePetOwner = pets; experienceAchievementOwner = achievements;
            experienceDatacubeOwner = cubes; experienceArchiveOwner = archive;
            experienceMapOwner = maps; experienceReputationOwner = reputation;
            void PublicationReady()
            {
                cubes?.NotifyPublicationReady();
                maps?.NotifyPublicationReady();
                reputation?.NotifyPublicationReady();
                spells?.NotifyAmpPowerPublicationReady();
                spells?.NotifyActionSetPublicationReady();
                path?.NotifyPathPublicationReady();
                try { achievements?.NotifyPublicationReady(); }
                finally { archive?.NotifyPublicationReady(); }
            }
            return experiencePersistence = new(owner, baseline, Current, database, achievements == null ? null : achievements.QueuePublication,
                achievements?.RealmFirst, archive == null ? null : archive.AcknowledgePublication, PublicationReady,
                cubes == null ? null : cubes.AcknowledgePublication, maps == null ? null : maps.AcknowledgePublication,
                reputation == null ? null : reputation.AcknowledgePublication, spells == null ? null : spells.AcknowledgeAmpPowerPublication,
                path == null ? null : path.AcknowledgePathPublication, spells == null ? null : spells.AcknowledgeActionSetPublication);
        }
        internal bool OwnsDatacubePublication(DatacubeManager manager, Identity identity, IGameSession session)
            => Identity?.Id == identity.Id && Identity.RealmId == identity.RealmId
                && ReferenceEquals(DatacubeManager, manager) && ReferenceEquals(Session, session)
                && (playerManager == null || ReferenceEquals(playerManager.GetPlayer(identity), this))
                && session is IPlayerSession current && ReferenceEquals(current.Player, this)
                && session is not NetworkSession { IsDisconnecting: true };
        internal bool OwnsExplorationPublication(ZoneMapManager manager, Identity identity, IGameSession session)
            => Identity?.Id == identity.Id && Identity.RealmId == identity.RealmId
                && ReferenceEquals(ZoneMapManager, manager) && ReferenceEquals(Session, session)
                && (playerManager == null || ReferenceEquals(playerManager.GetPlayer(identity), this))
                && session is IPlayerSession current && ReferenceEquals(current.Player, this)
                && session is not NetworkSession { IsDisconnecting: true };
        internal bool OwnsReputationPublication(ReputationManager manager, Identity identity, IGameSession session)
            => Identity?.Id == identity.Id && Identity.RealmId == identity.RealmId
                && ReferenceEquals(ReputationManager, manager) && ReferenceEquals(Session, session)
                && (playerManager == null || ReferenceEquals(playerManager.GetPlayer(identity), this))
                && session is IPlayerSession current && ReferenceEquals(current.Player, this)
                && session is not NetworkSession { IsDisconnecting: true };
        internal bool OwnsAmpPowerPublication(SpellManager manager, Identity identity, IGameSession session)
            => Identity?.Id == identity.Id && Identity.RealmId == identity.RealmId
                && ReferenceEquals(SpellManager, manager) && ReferenceEquals(Session, session)
                && (playerManager == null || ReferenceEquals(playerManager.GetPlayer(identity), this))
                && session is IPlayerSession current && ReferenceEquals(current.Player, this)
                && session is not NetworkSession { IsDisconnecting: true };
        internal bool OwnsPathPublication(PathManager manager, Identity identity, IGameSession session)
            => Identity?.Id == identity.Id && Identity.RealmId == identity.RealmId
                && ReferenceEquals(PathManager, manager) && ReferenceEquals(Session, session)
                && (playerManager == null || ReferenceEquals(playerManager.GetPlayer(identity), this))
                && session is IPlayerSession current && ReferenceEquals(current.Player, this)
                && session is not NetworkSession { IsDisconnecting: true };
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
