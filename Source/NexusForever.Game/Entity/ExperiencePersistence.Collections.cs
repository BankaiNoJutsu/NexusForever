using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Entity
{
    internal sealed partial class ExperiencePersistence
    {
        // Collection progress shares the path/achievement save boundary. Do not
        // accept independently matching rows from different uncertain graphs.
        private static void StageCollections(Unit unit, Snapshot before, Snapshot desired, Snapshot previous)
        {
            if (desired.Datacubes != null)
            {
                var cubes = before.Datacubes.ToDictionary(row => (row.Type, row.Id));
                var previousCubes = previous.Datacubes.ToDictionary(row => (row.Type, row.Id));
                if (cubes.Keys.Except(desired.Datacubes.Select(row => (row.Type, row.Id))).Any())
                    throw new InvalidOperationException("Experience cannot delete collection progress.");
                foreach (DatacubeState row in desired.Datacubes)
                {
                    DatacubeState old = cubes.GetValueOrDefault((row.Type, row.Id));
                    if (row == old && row == previousCubes.GetValueOrDefault((row.Type, row.Id))) continue;
                    Write(unit, old == null ? null : Model(old), Model(row),
                        [nameof(CharacterDatacubeModel.Id), nameof(CharacterDatacubeModel.Type), nameof(CharacterDatacubeModel.Datacube)],
                        [nameof(CharacterDatacubeModel.Progress)]);
                }
            }
            if (desired.Articles != null)
            {
                var articles = before.Articles.ToDictionary(row => row.Id);
                var previousArticles = previous.Articles.ToDictionary(row => row.Id);
                if (articles.Keys.Except(desired.Articles.Select(row => row.Id)).Any())
                    throw new InvalidOperationException("Experience cannot delete archive progress.");
                foreach (ArchiveState row in desired.Articles)
                {
                    ArchiveState old = articles.GetValueOrDefault(row.Id);
                    if (row == old && row == previousArticles.GetValueOrDefault(row.Id)) continue;
                    Write(unit, old == null ? null : Model(old), Model(row),
                        [nameof(CharacterGalacticArchiveModel.Id), nameof(CharacterGalacticArchiveModel.ArchiveArticleId)],
                        [nameof(CharacterGalacticArchiveModel.UnlockedFlags), nameof(CharacterGalacticArchiveModel.ViewedFlags)]);
                }
            }
            if (desired.MapHexgroups != null)
            {
                var groups = before.MapHexgroups.ToDictionary(row => (row.Zone, row.Group));
                var previousGroups = previous.MapHexgroups.ToDictionary(row => (row.Zone, row.Group));
                if (groups.Keys.Except(desired.MapHexgroups.Select(row => (row.Zone, row.Group))).Any())
                    throw new InvalidOperationException("Experience cannot delete explored hexgroups.");
                foreach (ExploredHexgroup row in desired.MapHexgroups)
                {
                    ExploredHexgroup old = groups.GetValueOrDefault((row.Zone, row.Group));
                    if (row == old && row == previousGroups.GetValueOrDefault((row.Zone, row.Group))) continue;
                    Write(unit, old == null ? null : Model(old), Model(row),
                        [nameof(CharacterZonemapHexgroupModel.Id), nameof(CharacterZonemapHexgroupModel.ZoneMap), nameof(CharacterZonemapHexgroupModel.HexGroup)], []);
                }
            }
            if (desired.Reputations != null)
            {
                var reputations = before.Reputations.ToDictionary(row => row.Faction);
                var previousReputations = previous.Reputations.ToDictionary(row => row.Faction);
                if (reputations.Keys.Except(desired.Reputations.Select(row => row.Faction)).Any())
                    throw new InvalidOperationException("Experience cannot delete reputation progress.");
                foreach (ReputationState row in desired.Reputations)
                {
                    ReputationState old = reputations.GetValueOrDefault(row.Faction);
                    if (row == old && row == previousReputations.GetValueOrDefault(row.Faction)) continue;
                    Write(unit, old == null ? null : Model(old), Model(row),
                        [nameof(CharacterReputation.Id), nameof(CharacterReputation.FactionId)], [nameof(CharacterReputation.Amount)]);
                }
            }
        }
        private static CharacterDatacubeModel Model(DatacubeState row) => new() { Id = row.Owner, Type = row.Type, Datacube = row.Id, Progress = row.Progress };
        private static CharacterGalacticArchiveModel Model(ArchiveState row) => new() { Id = row.Owner, ArchiveArticleId = row.Id, UnlockedFlags = row.Unlocked, ViewedFlags = row.Viewed };
        private static CharacterZonemapHexgroupModel Model(ExploredHexgroup row) => new() { Id = row.Owner, ZoneMap = row.Zone, HexGroup = row.Group };
        private static CharacterReputation Model(ReputationState row) => new() { Id = row.Owner, FactionId = row.Faction, Amount = row.Amount };
    }
}
