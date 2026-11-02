using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Entity
{
    internal sealed partial class ExperiencePersistence
    {
        private static void StageRewards(Unit unit, Snapshot before, Snapshot desired, Snapshot previous)
        {
            if (desired.Spells != null)
            {
                var learned = before.Spells.Learned.ToDictionary(row => row.Id); var previousLearned = previous.Spells.Learned.ToDictionary(row => row.Id);
                if (learned.Keys.Except(desired.Spells.Learned.Select(row => row.Id)).Any()) throw new InvalidOperationException("Experience cannot delete learned spells.");
                foreach (LearnedSpell row in desired.Spells.Learned)
                {
                    LearnedSpell old = learned.GetValueOrDefault(row.Id); if (row == old && row == previousLearned.GetValueOrDefault(row.Id)) continue;
                    Write(unit, old == null ? null : Model(old), Model(row), [nameof(CharacterSpellModel.Id), nameof(CharacterSpellModel.Spell4BaseId)], [nameof(CharacterSpellModel.Tier)]);
                }
                var shortcuts = before.Spells.Shortcuts.ToDictionary(row => (row.Spec, row.Location));
                var previousShortcuts = previous.Spells.Shortcuts.ToDictionary(row => (row.Spec, row.Location));
                var targetShortcuts = desired.Spells.Shortcuts.ToDictionary(row => (row.Spec, row.Location));
                string[] shortcutKeys = [nameof(CharacterActionSetShortcutModel.Id), nameof(CharacterActionSetShortcutModel.SpecIndex), nameof(CharacterActionSetShortcutModel.Location)];
                string[] shortcutFields = [nameof(CharacterActionSetShortcutModel.ShortcutType), nameof(CharacterActionSetShortcutModel.ObjectId), nameof(CharacterActionSetShortcutModel.Tier)];
                foreach (Shortcut row in desired.Spells.Shortcuts)
                {
                    Shortcut old = shortcuts.GetValueOrDefault((row.Spec, row.Location)); if (row == old && row == previousShortcuts.GetValueOrDefault((row.Spec, row.Location))) continue;
                    Write(unit, old == null ? null : Model(old), Model(row), shortcutKeys, shortcutFields);
                }
                foreach (Shortcut row in before.Spells.Shortcuts.Where(row => !targetShortcuts.ContainsKey((row.Spec, row.Location)))) Delete(unit, Model(row), shortcutKeys, shortcutFields);
                foreach (Shortcut row in previous.Spells.Shortcuts.Where(row => !shortcuts.ContainsKey((row.Spec, row.Location)) && !targetShortcuts.ContainsKey((row.Spec, row.Location)))) CancelCreate(unit, Model(row), shortcutKeys);
                var amps = before.Spells.Amps.ToDictionary(row => (row.Spec, row.Id)); var previousAmps = previous.Spells.Amps.ToDictionary(row => (row.Spec, row.Id));
                var targetAmps = desired.Spells.Amps.ToDictionary(row => (row.Spec, row.Id));
                string[] ampKeys = [nameof(CharacterActionSetAmpModel.Id), nameof(CharacterActionSetAmpModel.SpecIndex), nameof(CharacterActionSetAmpModel.AmpId)];
                foreach (Amp row in desired.Spells.Amps)
                {
                    Amp old = amps.GetValueOrDefault((row.Spec, row.Id)); if (row == old && row == previousAmps.GetValueOrDefault((row.Spec, row.Id))) continue;
                    Write(unit, old == null ? null : Model(old), Model(row), ampKeys, []);
                }
                foreach (Amp row in before.Spells.Amps.Where(row => !targetAmps.ContainsKey((row.Spec, row.Id)))) Delete(unit, Model(row), ampKeys, []);
                foreach (Amp row in previous.Spells.Amps.Where(row => !amps.ContainsKey((row.Spec, row.Id)) && !targetAmps.ContainsKey((row.Spec, row.Id)))) CancelCreate(unit, Model(row), ampKeys);
            }
            if (desired.Pets != null)
            {
                var pets = before.Pets.ToDictionary(row => (row.Type, row.Id)); var previousPets = previous.Pets.ToDictionary(row => (row.Type, row.Id));
                if (pets.Keys.Except(desired.Pets.Select(row => (row.Type, row.Id))).Any()) throw new InvalidOperationException("Experience cannot delete owned pets.");
                foreach (Pet row in desired.Pets)
                {
                    Pet old = pets.GetValueOrDefault((row.Type, row.Id)); if (row == old && row == previousPets.GetValueOrDefault((row.Type, row.Id))) continue;
                    Write(unit, old == null ? null : Model(old), Model(row), [nameof(CharacterPetCustomisationModel.Id), nameof(CharacterPetCustomisationModel.Type), nameof(CharacterPetCustomisationModel.ObjectId)],
                        [nameof(CharacterPetCustomisationModel.Name), nameof(CharacterPetCustomisationModel.FlairIdMask)]);
                }
            }
            if (desired.Achievements != null)
            {
                var achievements = before.Achievements.ToDictionary(row => row.Id); var previousAchievements = previous.Achievements.ToDictionary(row => row.Id);
                if (achievements.Keys.Except(desired.Achievements.Select(row => row.Id)).Any()) throw new InvalidOperationException("Experience cannot delete achievements.");
                foreach (Achievement row in desired.Achievements)
                {
                    Achievement old = achievements.GetValueOrDefault(row.Id); if (row == old && row == previousAchievements.GetValueOrDefault(row.Id)) continue;
                    Write(unit, old == null ? null : Model(old), Model(row), [nameof(CharacterAchievementModel.Id), nameof(CharacterAchievementModel.AchievementId)],
                        [nameof(CharacterAchievementModel.ProgressState), nameof(CharacterAchievementModel.CreditedChecklistMask), nameof(CharacterAchievementModel.DateCompleted)]);
                }
            }
        }
        private static void Delete<T>(Unit unit, T model, string[] keys, string[] fields) where T : class
        {
            Write(unit, model, model, keys, fields);
            var identity = unit.Context.Entry(model);
            var entry = unit.Context.ChangeTracker.Entries<T>().Single(row => keys.All(name => Equals(row.Property(name).CurrentValue, identity.Property(name).CurrentValue)));
            entry.State = EntityState.Deleted; unit.Rows[entry.Entity].Capture();
        }
        private static void CancelCreate<T>(Unit unit, T model, string[] keys) where T : class
        {
            var identity = unit.Context.Entry(model);
            var entry = unit.Context.ChangeTracker.Entries<T>().Single(row => keys.All(name => Equals(row.Property(name).CurrentValue, identity.Property(name).CurrentValue)));
            if (!unit.Rows.TryGetValue(entry.Entity, out var guard) || entry.State != EntityState.Added) throw new InvalidOperationException("Experience lost its pending create.");
            guard.Validate(); entry.State = EntityState.Detached; guard.Capture();
        }
        private static CharacterSpellModel Model(LearnedSpell row) => new() { Id = row.Owner, Spell4BaseId = row.Id, Tier = row.Tier };
        private static CharacterActionSetShortcutModel Model(Shortcut row) => new() { Id = row.Owner, SpecIndex = row.Spec, Location = row.Location, ShortcutType = row.Type, ObjectId = row.Object, Tier = row.Tier };
        private static CharacterActionSetAmpModel Model(Amp row) => new() { Id = row.Owner, SpecIndex = row.Spec, AmpId = row.Id };
        private static CharacterPetCustomisationModel Model(Pet row) => new() { Id = row.Owner, Type = row.Type, ObjectId = row.Id, Name = row.Name, FlairIdMask = row.Flairs };
        private static CharacterAchievementModel Model(Achievement row) => new() { Id = row.Owner, AchievementId = row.Id, ProgressState = row.Progress, CreditedChecklistMask = row.Credited, DateCompleted = row.Completed };
    }
}
