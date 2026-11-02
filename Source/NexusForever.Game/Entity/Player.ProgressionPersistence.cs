using NexusForever.Database.Character;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private ProgressionPersistence progressionPersistence;
        private string progressionDatabase;
        private readonly List<ProgressionPersistence.Recipe> unavailableSchematics = [];

        private ProgressionPersistence GetProgressionPersistence()
        {
            if (progressionPersistence != null) return progressionPersistence;
            ulong owner = CharacterId;
            var baseline = new ProgressionPersistence.Snapshot(
                tradeskills.Values.Where(row => row.Baseline != null).Select(row => row.Baseline).OrderBy(row => row.Id).ToArray(),
                schematics.Values.Where(row => row.Baseline != null).Select(row => row.Baseline).Concat(unavailableSchematics ?? []).OrderBy(row => row.Id).ToArray());
            ProgressionPersistence.Snapshot Current()
            {
                if (CharacterId != owner || tradeskills.Values.Any(row => row.CharacterId != owner) || schematics.Values.Any(row => row.CharacterId != owner))
                    throw new InvalidOperationException("Progression changed its Character owner.");
                return new(tradeskills.Values.Select(row => ProgressionPersistence.FromModel(row.BuildModel())).OrderBy(row => row.Id).ToArray(),
                    schematics.Values.Select(row => ProgressionPersistence.FromModel(row.BuildModel())).Concat(unavailableSchematics ?? []).OrderBy(row => row.Id).ToArray());
            }
            var candidate = new ProgressionPersistence(owner, baseline, Current, progressionDatabase);
            return System.Threading.Interlocked.CompareExchange(ref progressionPersistence, candidate, null) ?? candidate;
        }
        private void SaveProgression(CharacterContext context)
        {
            if (progressionPersistence == null && tradeskills.Count == 0 && schematics.Count == 0 && (unavailableSchematics?.Count ?? 0) == 0) return;
            GetProgressionPersistence().Save(context);
        }
        private Action<CharacterContext> CaptureProgressionSave(object token, string database)
            => GetProgressionPersistence().CaptureNormal(token, database);
    }
}
