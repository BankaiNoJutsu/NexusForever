using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Guild;

namespace NexusForever.Game.Guild
{
    // One original guild owns its ordinary header, data, rank and member graph.
    // Names and the soft-delete tombstone retain their separate existing owners.
    internal sealed class GuildMutationPersistence
    {
        internal sealed record Rank(byte Index, string Name, uint Permission, ulong Bank, ulong Money, ulong Repair);
        internal sealed record Member(ulong Character, byte Rank, string Note, int Plot);
        internal sealed record Data(string Motd, string Info, ushort Background, ushort Foreground, ushort Scan);
        internal sealed record Snapshot(ulong? Leader, uint Flags, Rank[] Ranks, Member[] Members, Data Data);
        private sealed class Unit(CharacterContext context, Snapshot target, Snapshot sql, bool disband, string name, bool ownsName)
        {
            internal readonly CharacterContext Context = context;
            internal readonly Snapshot Target = target;
            internal readonly bool Disband = disband;
            internal readonly string Name = name;
            internal readonly bool OwnsName = ownsName;
            internal Snapshot Sql = sql;
            internal readonly List<CharacterColumnSaveIntent> Rows = [];
            internal bool Flushed;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
        }
        private static readonly object leaseGate = new();
        private static readonly Dictionary<(string Database, ulong Guild), Unit> leases = [];
        private readonly GuildBase owner;
        private readonly ulong guild;
        private readonly byte type;
        private readonly ushort realm;
        private readonly object gate;
        private readonly Func<Snapshot> current;
        private readonly Action<Snapshot, Snapshot> acknowledge;
        private readonly HashSet<byte> attemptedRanks = [];
        private readonly HashSet<ulong> attemptedMembers = [];
        private string database;
        private Snapshot baseline;
        private readonly List<Snapshot> uncertain = [];
        private Unit active;

        internal GuildMutationPersistence(GuildBase owner, object gate, Snapshot baseline, string database,
            Func<Snapshot> current, Action<Snapshot, Snapshot> acknowledge)
        {
            this.owner = owner; this.gate = gate; this.baseline = baseline; this.database = database;
            this.current = current; this.acknowledge = acknowledge;
            guild = owner.Id; type = (byte)owner.Type; realm = owner.Identity.RealmId;
        }
        internal bool NeedsSave { get { lock (gate) return active != null || uncertain.Count != 0 || !Equal(current(), baseline); } }
        internal bool Owns(CharacterContext context) { lock (gate) return active != null && ReferenceEquals(active.Context, context); }
        internal bool WasRankAttempted(byte index) { lock (gate) return attemptedRanks.Contains(index); }
        internal bool WasMemberAttempted(ulong character) { lock (gate) return attemptedMembers.Contains(character); }
        internal void RequireAvailable(CharacterContext context)
        {
            lock (gate)
                if (active != null && !ReferenceEquals(active.Context, context))
                {
                    var error = new InvalidOperationException("Guild mutations belong to an unfinished save unit.");
                    context.RejectSave(error); throw error;
                }
        }
        private static bool Equal(Snapshot left, Snapshot right) => left != null && right != null
            && left.Leader == right.Leader && left.Flags == right.Flags && left.Data == right.Data
            && left.Ranks.SequenceEqual(right.Ranks) && left.Members.SequenceEqual(right.Members);
        internal static Snapshot FromModel(GuildModel model) => new(model.LeaderId, model.Flags,
            model.GuildRank.OrderBy(row => row.Index).Select(row => new Rank(row.Index, row.Name, row.Permission, row.BankWithdrawalPermission, row.MoneyWithdrawalLimit, row.RepairLimit)).ToArray(),
            model.GuildMember.OrderBy(row => row.CharacterId).Select(row => new Member(row.CharacterId, row.Rank, row.Note, row.CommunityPlotReservation)).ToArray(),
            model.Type == (byte)GuildType.Guild && model.GuildData is {} data ? new(data.MessageOfTheDay, data.AdditionalInfo, data.BackgroundIconPartId, data.ForegroundIconPartId, data.ScanLinesPartId) : null);
        internal static Snapshot Read(CharacterContext context, ulong guild, byte type, Func<GuildModel, bool> acceptedTombstone = null)
        {
            GuildModel header = context.Guild.AsNoTracking().SingleOrDefault(row => row.Id == guild);
            if (header == null || header.Type != type || header.DeleteTime != null && acceptedTombstone?.Invoke(header) != true)
                throw new InvalidOperationException("Guild mutations lost their original active guild row.");
            return new(header.LeaderId, header.Flags,
                context.GuildRank.AsNoTracking().Where(row => row.Id == guild).OrderBy(row => row.Index)
                    .Select(row => new Rank(row.Index, row.Name, row.Permission, row.BankWithdrawalPermission, row.MoneyWithdrawalLimit, row.RepairLimit)).ToArray(),
                context.GuildMember.AsNoTracking().Where(row => row.Id == guild)
                    .Select(row => new Member(row.CharacterId, row.Rank, row.Note, row.CommunityPlotReservation)).ToArray().OrderBy(row => row.Character).ToArray(),
                type == (byte)GuildType.Guild ? context.GuildData.AsNoTracking().Where(row => row.Id == guild)
                    .Select(row => new Data(row.MessageOfTheDay, row.AdditionalInfo, row.BackgroundIconPartId, row.ForegroundIconPartId, row.ScanLinesPartId)).SingleOrDefault() : null);
        }
        private void RequireOwner(CharacterContext context)
        {
            if (owner.Id != guild || (byte)owner.Type != type || owner.Identity.RealmId != realm || context.IsSaveRejected)
                throw new InvalidOperationException("Guild mutations lost their original actor or fresh context.");
            database ??= CharacterDatabaseIdentity.Get(context);
            CharacterDatabaseIdentity.Require(context, database);
        }
        internal void RequireOriginalDatabase(CharacterContext context)
        {
            lock (gate)
                try { RequireOwner(context); }
                catch (Exception error) { context.RejectSave(error); throw; }
        }
        private Snapshot Read(Unit unit) => Read(unit.Context, guild, type, unit.Disband ? owner.MatchesMutationTombstone : null);
        private void ValidateName(Unit unit)
        {
            if (!unit.OwnsName && !StringComparer.Ordinal.Equals(unit.Context.Guild.AsNoTracking().Where(row => row.Id == guild).Select(row => row.Name).Single(), unit.Name))
                throw new InvalidOperationException("Ordinary guild mutations cannot change the separately owned name.");
        }
        internal void Save(CharacterContext context, bool disband = false)
        {
            lock (gate)
            {
                try
                {
                    RequireAvailable(context);
                    if (active != null) return;
                    if (!disband && !NeedsSave) return;
                    RequireOwner(context);
                    if (context.HasSavepointRollback) throw new InvalidOperationException("Guild mutations require a fresh context after savepoint rollback.");
                    Snapshot actual = Read(context, guild, type, disband ? owner.MatchesMutationTombstone : null), target = current();
                    if (disband) target = target with { Leader = null };
                    if (!Equal(actual, baseline) && !uncertain.Any(value => Equal(value, actual)))
                        throw new InvalidOperationException("Guild mutations changed outside their loaded or retained SQL outcomes.");
                    var unit = new Unit(context, target, actual, disband,
                        context.Guild.AsNoTracking().Where(row => row.Id == guild).Select(row => row.Name).Single(), owner.OwnsMutationNameWrite(context));
                    lock (leaseGate)
                    {
                        if (!leases.TryAdd((database, guild), unit))
                            throw new InvalidOperationException("Guild mutations belong to another loaded actor.");
                    }
                    active = unit;
                    unit.OnFlushed = (_, _) => Flushed(unit);
                    context.SavedChanges += unit.OnFlushed;
                    context.OnSaveCompleted(committed => Complete(unit, committed));
                    context.OnSaveValidation(phase => Validate(unit, phase));
                    foreach (Rank rank in target.Ranks) attemptedRanks.Add(rank.Index);
                    foreach (Member member in target.Members) attemptedMembers.Add(member.Character);
                    Stage(unit, actual, target);
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
        private void Stage(Unit unit, Snapshot before, Snapshot after)
        {
            if (!unit.Disband && before.Leader != after.Leader || before.Flags != after.Flags)
            {
                GuildModel header = unit.Context.Guild.AsNoTracking().Single(row => row.Id == guild);
                Write(unit, header, new GuildModel { Id = guild, Type = type, Name = header.Name, LeaderId = after.Leader, Flags = after.Flags },
                    unit.Disband ? [nameof(GuildModel.Flags)] : [nameof(GuildModel.LeaderId), nameof(GuildModel.Flags)],
                    unit.Disband ? [nameof(GuildModel.Type)] : unit.OwnsName ? [nameof(GuildModel.Type), nameof(GuildModel.DeleteTime)]
                        : [nameof(GuildModel.Type), nameof(GuildModel.DeleteTime), nameof(GuildModel.Name)], true);
            }
            if (before.Data != after.Data)
                Write(unit, DataModel(before.Data), DataModel(after.Data), [nameof(GuildDataModel.MessageOfTheDay), nameof(GuildDataModel.AdditionalInfo),
                    nameof(GuildDataModel.BackgroundIconPartId), nameof(GuildDataModel.ForegroundIconPartId), nameof(GuildDataModel.ScanLinesPartId)]);
            foreach (byte index in before.Ranks.Select(row => row.Index).Union(after.Ranks.Select(row => row.Index)))
            {
                Rank old = before.Ranks.SingleOrDefault(row => row.Index == index), next = after.Ranks.SingleOrDefault(row => row.Index == index);
                if (old != next) Write(unit, RankModel(old), RankModel(next), [nameof(GuildRankModel.Name), nameof(GuildRankModel.Permission),
                    nameof(GuildRankModel.BankWithdrawalPermission), nameof(GuildRankModel.MoneyWithdrawalLimit), nameof(GuildRankModel.RepairLimit)]);
            }
            foreach (ulong character in before.Members.Select(row => row.Character).Union(after.Members.Select(row => row.Character)))
            {
                Member old = before.Members.SingleOrDefault(row => row.Character == character), next = after.Members.SingleOrDefault(row => row.Character == character);
                if (old != next) Write(unit, MemberModel(old), MemberModel(next), [nameof(GuildMemberModel.Rank), nameof(GuildMemberModel.Note), nameof(GuildMemberModel.CommunityPlotReservation)]);
            }
        }
        private GuildDataModel DataModel(Data value) => value == null ? null : new() { Id = guild, MessageOfTheDay = value.Motd,
            AdditionalInfo = value.Info, BackgroundIconPartId = value.Background, ForegroundIconPartId = value.Foreground, ScanLinesPartId = value.Scan };
        private GuildRankModel RankModel(Rank value) => value == null ? null : new() { Id = guild, Index = value.Index, Name = value.Name,
            Permission = value.Permission, BankWithdrawalPermission = value.Bank, MoneyWithdrawalLimit = value.Money, RepairLimit = value.Repair };
        private GuildMemberModel MemberModel(Member value) => value == null ? null : new() { Id = guild, CharacterId = value.Character,
            Rank = value.Rank, Note = value.Note, CommunityPlotReservation = value.Plot };
        private static void Write<T>(Unit unit, T before, T after, string[] fields, string[] protectedFields = null, bool partial = false) where T : class
        {
            CharacterContext context = unit.Context;
            EntityEntry<T> identity = context.Entry(after ?? before);
            string[] keys = identity.Metadata.FindPrimaryKey()!.Properties.Select(property => property.Name).ToArray();
            EntityEntry<T> entry = context.ChangeTracker.Entries<T>().SingleOrDefault(row => keys.All(name => Equals(row.Property(name).CurrentValue, identity.Property(name).CurrentValue)));
            string[] owned = fields.Concat(protectedFields ?? []).ToArray();
            if (entry != null)
            {
                if (before == null || entry.State is EntityState.Added or EntityState.Deleted or EntityState.Detached
                    || owned.Any(name => entry.Property(name).IsModified || !Equals(entry.Property(name).CurrentValue, context.Entry(before).Property(name).CurrentValue)))
                    throw new InvalidOperationException("Guild mutation row already has foreign tracked intent.");
            }
            else
            {
                entry = context.Entry(before ?? after);
                entry.State = before == null ? EntityState.Added : EntityState.Unchanged;
            }
            if (after == null) entry.State = EntityState.Deleted;
            else foreach (string name in fields)
            {
                if (before != null) entry.Property(name).OriginalValue = context.Entry(before).Property(name).CurrentValue;
                entry.Property(name).CurrentValue = context.Entry(after).Property(name).CurrentValue;
                if (before != null) entry.Property(name).IsModified = !Equals(entry.Property(name).CurrentValue, entry.Property(name).OriginalValue);
            }
            var guard = new CharacterColumnSaveIntent(entry, keys.Concat(owned).ToArray(), partial, "Guild mutation");
            context.ChangeTracker.DetectChanges(); guard.Capture(); unit.Rows.Add(guard);
        }
        private void Validate(Unit unit, CharacterSaveValidationPhase phase)
        {
            lock (gate)
            {
                if (!ReferenceEquals(active, unit)) throw new InvalidOperationException("Guild mutations lost their save owner.");
                if (unit.Context.HasSavepointRollback) return;
                RequireOwner(unit.Context); ValidateName(unit); unit.Context.ChangeTracker.DetectChanges();
                foreach (var row in unit.Rows)
                {
                    row.Validate();
                    bool detachedDelete = unit.Flushed && row.State == EntityState.Detached;
                    if (!detachedDelete && !unit.Context.ChangeTracker.Entries().Any(entry => ReferenceEquals(entry.Entity, row.Entry.Entity)))
                        throw new InvalidOperationException("Guild mutation lost its retained tracked row.");
                }
                Snapshot expected = phase == CharacterSaveValidationPhase.BeforeFlush ? unit.Sql : unit.Target;
                if (!Equal(Read(unit), expected))
                    throw new InvalidOperationException("Guild SQL differs from its captured mutation graph.");
            }
        }
        private void Flushed(Unit unit)
        {
            lock (gate)
            {
                if (!ReferenceEquals(active, unit) || unit.Context.HasSavepointRollback) return;
                ValidateName(unit);
                if (!Equal(Read(unit), unit.Target))
                    throw new InvalidOperationException("Guild mutations did not flush their complete captured graph.");
                unit.Sql = unit.Target; unit.Flushed = true;
                foreach (var row in unit.Rows) { row.Capture(); row.Flushed = true; }
            }
        }
        private void Complete(Unit unit, bool committed)
        {
            lock (gate)
            {
                if (!ReferenceEquals(active, unit)) return;
                try
                {
                    if (committed && unit.Flushed && !unit.Context.HasSavepointRollback)
                    {
                        acknowledge(baseline, unit.Target); baseline = unit.Target; uncertain.Clear();
                    }
                    else
                    {
                        if (!uncertain.Any(value => Equal(value, unit.Sql))) uncertain.Add(unit.Sql);
                        if (!uncertain.Any(value => Equal(value, unit.Target))) uncertain.Add(unit.Target);
                    }
                }
                finally
                {
                    unit.Context.SavedChanges -= unit.OnFlushed; active = null;
                    lock (leaseGate) if (leases.TryGetValue((database, guild), out Unit held) && ReferenceEquals(held, unit)) leases.Remove((database, guild));
                }
            }
        }
    }
}
