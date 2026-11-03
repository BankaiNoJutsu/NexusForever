using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Runtime.CompilerServices;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Achievement
{
    // Actor-local intent; the database key, not this dictionary, arbitrates
    // between actors, contexts and processes.
    internal sealed class RealmFirstPersistence(bool isGuild, ulong owner)
    {
        internal sealed record Claim(ushort Id, string Name);
        private sealed record Candidate(ushort Id, string Name, string Token);
        private sealed record Stored(bool Guild, ushort Id, ulong? Owner, string Token, string Name);
        // Only claims staged by a real producer in this context may be shared
        // before SQL exists. Arbitrary tracked rows are not historical evidence.
        private static readonly ConditionalWeakTable<CharacterContext, Dictionary<RealmFirstAchievementModel, Stored>> staged = new();
        internal sealed class Save(ushort id, string token, string name, bool winner)
        {
            internal readonly ushort Id = id;
            internal readonly string Token = token, Name = name;
            internal readonly bool Winner = winner;
            internal bool Flushed;
        }
        private readonly object gate = new();
        private readonly Dictionary<ushort, Candidate> pending = [];
        internal bool HasPendingFor(IEnumerable<ushort> completed)
        {
            lock (gate) return completed.Any(id => pending.ContainsKey(id));
        }
        internal void Retain(ushort id, string name)
        {
            lock (gate) pending.TryAdd(id, new(id, name, Guid.NewGuid().ToString("N")));
        }

        internal Save[] Stage(CharacterContext context, IEnumerable<ushort> completed)
        {
            var ids = completed.ToHashSet();
            Candidate[] candidates;
            lock (gate) candidates = pending.Values.Where(row => ids.Contains(row.Id)).ToArray();
            var saves = new List<Save>();
            try
            {
                if (context.IsSaveRejected || context.HasSavepointRollback)
                    throw new InvalidOperationException("Realm-first claims require a fresh save unit.");
                foreach (Candidate candidate in candidates)
                {
                    Stored before = Read(context, candidate.Id);
                    var local = context.RealmFirstAchievement.Local.SingleOrDefault(row => row.IsGuild == isGuild && row.AchievementId == candidate.Id);
                    if (local != null)
                    {
                        var state = context.Entry(local).State;
                        if (state is not (EntityState.Added or EntityState.Unchanged) || before != null && From(local) != before)
                            throw new InvalidOperationException("An immutable realm-first claim was changed.");
                        if (before == null && (state != EntityState.Added || !staged.TryGetValue(context, out var owned)
                            || !owned.TryGetValue(local, out Stored original) || original != From(local)))
                            throw new InvalidOperationException("An uncommitted realm-first claim belongs to another save intent.");
                    }
                    else if (before == null)
                    {
                        bool historical = isGuild
                            ? context.GuildAchievement.Any(row => row.AchievementId == candidate.Id && row.DateCompleted != null)
                            : context.CharacterAchievement.Any(row => row.AchievementId == candidate.Id && row.DateCompleted != null);
                        local = new RealmFirstAchievementModel { IsGuild = isGuild, AchievementId = candidate.Id,
                            OwnerId = historical ? null : owner, Token = historical ? null : candidate.Token, Name = historical ? null : candidate.Name };
                        context.RealmFirstAchievement.Add(local);
                        staged.GetOrCreateValue(context).Add(local, From(local));
                    }
                    Stored target = local == null ? before : From(local);
                    bool winner = target.Owner == owner && StringComparer.Ordinal.Equals(target.Token, candidate.Token);
                    if (winner && !StringComparer.Ordinal.Equals(target.Name, candidate.Name))
                        throw new InvalidOperationException("The retained realm-first name differs from its stored winner.");
                    var save = new Save(candidate.Id, candidate.Token, candidate.Name, winner);
                    CharacterColumnSaveIntent guard = local == null ? null : new(context.Entry(local),
                        [nameof(RealmFirstAchievementModel.IsGuild), nameof(RealmFirstAchievementModel.AchievementId),
                         nameof(RealmFirstAchievementModel.OwnerId), nameof(RealmFirstAchievementModel.Token), nameof(RealmFirstAchievementModel.Name)], false, "Realm-first claim");
                    guard?.Capture();
                    context.OnSaveValidation(phase =>
                    {
                        if (context.HasSavepointRollback) return;
                        guard?.Validate();
                        if (guard?.Entry.State == EntityState.Added && context.Database.CurrentTransaction == null && context.Database.AutoTransactionBehavior == AutoTransactionBehavior.Never)
                            throw new InvalidOperationException("A realm-first claim cannot bypass its graph transaction.");
                        Stored expected = phase == CharacterSaveValidationPhase.BeforeFlush && !save.Flushed ? before : target;
                        if (Read(context, candidate.Id) != expected)
                            throw new InvalidOperationException("Realm-first SQL differs from its captured claim.");
                        if (winner && before == null && phase != CharacterSaveValidationPhase.BeforeFlush && !HasCompletion(context, candidate.Id))
                            throw new InvalidOperationException("A new realm-first winner lost its completed achievement graph.");
                    });
                    EventHandler<SavedChangesEventArgs> flushed = (_, _) =>
                    {
                        if (context.HasSavepointRollback || Read(context, candidate.Id) != target) return;
                        save.Flushed = true;
                        if (guard != null) { guard.Capture(); guard.Flushed = true; }
                    };
                    context.SavedChanges += flushed;
                    context.OnSaveCompleted(_ => context.SavedChanges -= flushed);
                    saves.Add(save);
                }
                return saves.ToArray();
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }

        internal Claim[] Acknowledge(Save[] saves)
        {
            var claims = new List<Claim>();
            lock (gate)
                foreach (Save save in saves)
                    if (save.Flushed && pending.TryGetValue(save.Id, out Candidate candidate) && candidate.Token == save.Token)
                    {
                        pending.Remove(save.Id);
                        if (save.Winner) claims.Add(new(save.Id, save.Name));
                    }
            return claims.ToArray();
        }
        private Stored Read(CharacterContext context, ushort id)
        {
            var row = context.RealmFirstAchievement.AsNoTracking().SingleOrDefault(row => row.IsGuild == isGuild && row.AchievementId == id);
            return row == null ? null : From(row);
        }
        private bool HasCompletion(CharacterContext context, ushort id) => isGuild
            ? context.GuildAchievement.Any(row => row.Id == owner && row.AchievementId == id && row.DateCompleted != null)
            : context.CharacterAchievement.Any(row => row.Id == owner && row.AchievementId == id && row.DateCompleted != null);
        private static Stored From(RealmFirstAchievementModel row) => new(row.IsGuild, row.AchievementId, row.OwnerId, row.Token, row.Name);
    }
}
