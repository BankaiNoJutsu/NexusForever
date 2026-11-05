using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Entity
{
    internal sealed record AppearanceChangeState(byte Race, byte Sex,
        AppearanceChangeState.Visual[] Visuals, AppearanceChangeState.Choice[] Choices, AppearanceChangeState.Shape[] Bones)
    {
        internal sealed record Visual(byte Slot, ushort Display);
        internal sealed record Choice(uint Label, uint Value);
        internal sealed record Shape(byte Index, float Value)
        {
            public float Value { get; init; } = Value == 0 ? 0 : Value;
        }
        internal string Hash => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(this)));
        internal static AppearanceChangeState FromModel(CharacterModel model) => new(model.Race, model.Sex,
            model.Appearance.Select(row => new Visual(row.Slot, row.DisplayId)).OrderBy(row => row.Slot).ToArray(),
            model.Customisation.Select(row => new Choice(row.Label, row.Value)).OrderBy(row => row.Label).ToArray(),
            model.Bone.Select(row => new Shape(row.BoneIndex, row.Bone)).OrderBy(row => row.Index).ToArray());

        internal static AppearanceChangeState Read(CharacterContext context, ulong character)
        {
            var header = context.Character.AsNoTracking().Single(row => row.Id == character);
            return new(header.Race, header.Sex,
                context.CharacterAppearance.AsNoTracking().Where(row => row.Id == character).OrderBy(row => row.Slot).Select(row => new Visual(row.Slot, row.DisplayId)).ToArray(),
                context.CharacterCustomisation.AsNoTracking().Where(row => row.Id == character).OrderBy(row => row.Label).Select(row => new Choice(row.Label, row.Value)).ToArray(),
                context.CharacterBone.AsNoTracking().Where(row => row.Id == character).OrderBy(row => row.BoneIndex).Select(row => new Shape(row.BoneIndex, row.Bone)).ToArray());
        }
        internal void Stage(CharacterContext context, ulong character)
        {
            var header = context.Character.Local.SingleOrDefault(row => row.Id == character);
            if (header == null) { header = new() { Id = character }; context.Attach(header); }
            header.Race = Race; header.Sex = Sex;
            context.Entry(header).Property(row => row.Race).IsModified = true;
            context.Entry(header).Property(row => row.Sex).IsModified = true;
            StageRows(context, context.CharacterAppearance.AsNoTracking().Where(row => row.Id == character).ToArray(),
                Visuals.Select(row => new CharacterAppearanceModel { Id = character, Slot = row.Slot, DisplayId = row.Display }).ToArray(),
                row => row.Id == character, row => row.Slot);
            StageRows(context, context.CharacterCustomisation.AsNoTracking().Where(row => row.Id == character).ToArray(),
                Choices.Select(row => new CharacterCustomisationModel { Id = character, Label = row.Label, Value = row.Value }).ToArray(),
                row => row.Id == character, row => row.Label);
            StageRows(context, context.CharacterBone.AsNoTracking().Where(row => row.Id == character).ToArray(),
                Bones.Select(row => new CharacterBoneModel { Id = character, BoneIndex = row.Index, Bone = row.Value }).ToArray(),
                row => row.Id == character, row => row.BoneIndex);
            context.ChangeTracker.DetectChanges();
        }
        private static void StageRows<T, TKey>(CharacterContext context, T[] stored, T[] desired, Func<T, bool> owns, Func<T, TKey> key) where T : class
        {
            var tracked = context.ChangeTracker.Entries<T>().Where(row => owns(row.Entity)).ToArray();
            var keys = stored.Select(key).Concat(desired.Select(key)).Concat(tracked.Select(row => key(row.Entity))).Distinct().ToArray();
            foreach (var id in keys)
            {
                T before = stored.SingleOrDefault(row => EqualityComparer<TKey>.Default.Equals(key(row), id));
                T target = desired.SingleOrDefault(row => EqualityComparer<TKey>.Default.Equals(key(row), id));
                var entry = tracked.SingleOrDefault(row => EqualityComparer<TKey>.Default.Equals(key(row.Entity), id));
                if (target == null)
                {
                    if (before == null) { if (entry != null) entry.State = EntityState.Detached; }
                    else { entry ??= context.Attach(before); entry.State = EntityState.Deleted; }
                    continue;
                }
                if (before == null)
                {
                    entry ??= context.Entry(target); entry.CurrentValues.SetValues(target); entry.State = EntityState.Added;
                }
                else
                {
                    entry ??= context.Attach(before); entry.State = EntityState.Unchanged;
                    entry.CurrentValues.SetValues(before); entry.OriginalValues.SetValues(before); entry.CurrentValues.SetValues(target);
                }
            }
        }
        internal void ValidateEnrolled(CharacterContext context, ulong character)
        {
            context.ChangeTracker.DetectChanges();
            var header = context.Character.Local.Single(row => row.Id == character);
            var actual = Read(context, character) with
            {
                Race = header.Race, Sex = header.Sex,
                Visuals = Overlay(context, context.CharacterAppearance.AsNoTracking().Where(row => row.Id == character).ToArray(), row => row.Id == character, row => row.Slot)
                    .Select(row => new Visual(row.Slot, row.DisplayId)).OrderBy(row => row.Slot).ToArray(),
                Choices = Overlay(context, context.CharacterCustomisation.AsNoTracking().Where(row => row.Id == character).ToArray(), row => row.Id == character, row => row.Label)
                    .Select(row => new Choice(row.Label, row.Value)).OrderBy(row => row.Label).ToArray(),
                Bones = Overlay(context, context.CharacterBone.AsNoTracking().Where(row => row.Id == character).ToArray(), row => row.Id == character, row => row.BoneIndex)
                    .Select(row => new Shape(row.BoneIndex, row.Bone)).OrderBy(row => row.Index).ToArray()
            };
            if (actual.Hash != Hash) throw new InvalidOperationException("Appearance did not enroll its complete frozen target.");
        }
        private static T[] Overlay<T, TKey>(CharacterContext context, T[] stored, Func<T, bool> owns, Func<T, TKey> key) where T : class
        {
            var rows = stored.ToDictionary(key);
            foreach (var entry in context.ChangeTracker.Entries<T>().Where(row => owns(row.Entity)))
            {
                if (entry.State == EntityState.Deleted) rows.Remove(key(entry.Entity));
                else if (entry.State != EntityState.Detached) rows[key(entry.Entity)] = entry.Entity;
            }
            return rows.Values.ToArray();
        }
    }
}
