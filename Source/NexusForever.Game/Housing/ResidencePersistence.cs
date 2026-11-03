using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Housing
{
    // The registered residence owns one complete root/plot/decor/neighbor graph.
    internal sealed class ResidencePersistence
    {
        internal sealed record Header(ulong? Owner, ulong? Guild, byte Property, string Name, byte Privacy,
            ushort Wallpaper, ushort Roof, ushort Entry, ushort Door, ushort Ground, ushort Music, ushort Sky,
            ushort Flags, byte Resource, byte Garden);
        internal sealed record Plot(byte Index, ushort Info, ushort Plug, byte Facing, byte State);
        internal sealed record Decoration(ulong Id, uint Info, uint Type, uint Data, uint Bag, uint Hook, uint Plot,
            float Scale, float X, float Y, float Z, float Qx, float Qy, float Qz, float Qw, uint Active, ulong Parent, ushort Colour);
        internal sealed record Neighbor(ulong Character, byte Permission);
        internal sealed record Snapshot(Header Header, Plot[] Plots, Decoration[] Decor, Neighbor[] Neighbors);
        private sealed class Unit(CharacterContext context, Snapshot target, Snapshot sql, object reservation)
        {
            internal readonly CharacterContext Context = context;
            internal readonly object Reservation = reservation;
            internal readonly Snapshot Target = target;
            internal Snapshot Sql = sql;
            internal bool Flushed;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
            internal readonly List<CharacterColumnSaveIntent> Rows = [];
        }
        private static readonly object leaseGate = new();
        private static readonly Dictionary<(string Database, ulong Residence), Unit> leases = [];
        private readonly Residence owner;
        private readonly string database;
        private readonly Func<Snapshot> current;
        private readonly Action<Snapshot> acknowledge;
        private readonly Func<bool> registered;
        private Snapshot baseline;
        private readonly List<Snapshot> uncertain = [];
        private Unit active;
        private ulong Id => owner.Id;
        private object Gate => owner.MutationGate;
        internal ResidencePersistence(Residence owner, string database, Snapshot baseline, Func<Snapshot> current,
            Action<Snapshot> acknowledge, Func<bool> registered)
        {
            this.owner = owner; this.database = database; this.baseline = baseline;
            this.current = current; this.acknowledge = acknowledge; this.registered = registered;
        }
        internal void RequireEditable()
        {
            if (!registered()) throw new InvalidOperationException("Residence is no longer owned by its original registry.");
        }
        internal bool NeedsSave { get { lock (Gate) return baseline == null || active != null || uncertain.Count != 0 || !Equal(current(), baseline); } }
        internal bool Owns(CharacterContext context) { lock (Gate) return active != null && ReferenceEquals(active.Context, context); }
        private static bool Equal(Snapshot a, Snapshot b) => ReferenceEquals(a, b) || a != null && b != null
            && a.Header == b.Header && a.Plots.SequenceEqual(b.Plots) && a.Decor.SequenceEqual(b.Decor) && a.Neighbors.SequenceEqual(b.Neighbors);
        internal static Header HeaderFrom(ResidenceModel row) => new(row.OwnerId, row.GuildOwnerId, row.PropertyInfoId, row.Name, row.PrivacyLevel,
            row.WallpaperId, row.RoofDecorInfoId, row.EntrywayDecorInfoId, row.DoorDecorInfoId, row.GroundWallpaperId,
            row.MusicId, row.SkyWallpaperId, row.Flags, row.ResourceSharing, row.GardenSharing);
        internal static Plot PlotFrom(ResidencePlotModel row) => new(row.Index, row.PlotInfoId, row.PlugItemId, row.PlugFacing, row.BuildState);
        internal static Decoration DecorFrom(ResidenceDecor row) => new(row.DecorId, row.DecorInfoId, row.DecorType, row.DecorData, row.HookBagIndex,
            row.HookIndex, row.PlotIndex, row.Scale, row.X, row.Y, row.Z, row.Qx, row.Qy, row.Qz, row.Qw, row.ActivePropUnitId, row.DecorParentId, row.ColourShiftId);
        internal static Snapshot FromModel(ResidenceModel row) => new(HeaderFrom(row), row.Plot.OrderBy(p => p.Index).Select(PlotFrom).ToArray(),
            row.Decor.OrderBy(p => p.DecorId).Select(DecorFrom).ToArray(), row.Neighbors.OrderBy(p => p.NeighborCharacterId).Select(p => new Neighbor(p.NeighborCharacterId, p.PermissionLevel)).ToArray());
        private Snapshot Read(CharacterContext context)
        {
            ResidenceModel header = context.Residence.AsNoTracking().SingleOrDefault(row => row.Id == Id);
            var plots = context.ResidencePlot.AsNoTracking().Where(row => row.Id == Id).ToArray();
            var decor = context.ResidenceDecor.AsNoTracking().Where(row => row.Id == Id).ToArray();
            var neighbors = context.ResidenceNeighbor.AsNoTracking().Where(row => row.ResidenceId == Id).ToArray();
            if (header == null)
            {
                if (plots.Length != 0 || decor.Length != 0 || neighbors.Length != 0)
                    throw new InvalidOperationException("Residence lost its complete absent creation graph.");
                return null;
            }
            header.Plot = plots; header.Decor = decor; header.Neighbors = neighbors;
            return FromModel(header);
        }
        private void RequireOwner(CharacterContext context, object reservation)
        {
            if (context.IsSaveRejected || !registered() && !owner.OwnsCommunityRetirement(reservation))
                throw new InvalidOperationException("Residence lost its original registered owner or fresh context.");
            CharacterDatabaseIdentity.Require(context, database);
        }
        internal void Save(CharacterContext context, object reservation = null)
        {
            lock (Gate)
            {
                try
                {
                    RequireOwner(context, reservation);
                    if (active != null)
                    {
                        if (ReferenceEquals(active.Context, context)) return;
                        throw new InvalidOperationException("Residence belongs to an unfinished save unit.");
                    }
                    if (context.HasSavepointRollback) throw new InvalidOperationException("Residence requires a fresh context after savepoint rollback.");
                    Snapshot actual = Read(context), target = current();
                    if (!Equal(actual, baseline) && !uncertain.Any(value => Equal(value, actual)))
                        throw new InvalidOperationException("Residence SQL differs from its loaded or retained complete graph.");
                    var unit = new Unit(context, target, actual, reservation);
                    lock (leaseGate)
                        if (!leases.TryAdd((database, Id), unit)) throw new InvalidOperationException("Residence is enrolled by another actor.");
                    active = unit;
                    unit.OnFlushed = (_, _) => Flushed(unit);
                    context.SavedChanges += unit.OnFlushed;
                    context.OnSaveCompleted(committed => Complete(unit, committed));
                    context.OnSaveValidation(phase => Validate(unit, phase));
                    Stage(unit);
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
        private ResidenceModel Model(Header row) => row == null ? null : new() { Id = Id, OwnerId = row.Owner, GuildOwnerId = row.Guild,
            PropertyInfoId = row.Property, Name = row.Name, PrivacyLevel = row.Privacy, WallpaperId = row.Wallpaper, RoofDecorInfoId = row.Roof,
            EntrywayDecorInfoId = row.Entry, DoorDecorInfoId = row.Door, GroundWallpaperId = row.Ground, MusicId = row.Music,
            SkyWallpaperId = row.Sky, Flags = row.Flags, ResourceSharing = row.Resource, GardenSharing = row.Garden };
        private ResidencePlotModel Model(Plot row) => row == null ? null : new() { Id = Id, Index = row.Index, PlotInfoId = row.Info,
            PlugItemId = row.Plug, PlugFacing = row.Facing, BuildState = row.State };
        private ResidenceDecor Model(Decoration row) => row == null ? null : new() { Id = Id, DecorId = row.Id, DecorInfoId = row.Info,
            DecorType = row.Type, DecorData = row.Data, HookBagIndex = row.Bag, HookIndex = row.Hook, PlotIndex = row.Plot,
            Scale = row.Scale, X = row.X, Y = row.Y, Z = row.Z, Qx = row.Qx, Qy = row.Qy, Qz = row.Qz, Qw = row.Qw,
            ActivePropUnitId = row.Active, DecorParentId = row.Parent, ColourShiftId = row.Colour };
        private ResidenceNeighborModel Model(Neighbor row) => row == null ? null : new() { ResidenceId = Id, NeighborCharacterId = row.Character, PermissionLevel = row.Permission };
        private void Stage(Unit unit)
        {
            Snapshot before = unit.Sql, after = unit.Target;
            // Retain an unchanged root witness as well, including community rename enrollment.
            Write(unit, Model(before?.Header), Model(after.Header));
            foreach (byte key in (before?.Plots ?? []).Select(row => row.Index).Union(after.Plots.Select(row => row.Index)))
            {
                Plot old = before?.Plots.SingleOrDefault(row => row.Index == key), next = after.Plots.SingleOrDefault(row => row.Index == key);
                if (old != next) Write(unit, Model(old), Model(next));
            }
            foreach (ulong key in (before?.Decor ?? []).Select(row => row.Id).Union(after.Decor.Select(row => row.Id)))
            {
                Decoration old = before?.Decor.SingleOrDefault(row => row.Id == key), next = after.Decor.SingleOrDefault(row => row.Id == key);
                if (old != next) Write(unit, Model(old), Model(next));
            }
            foreach (ulong key in (before?.Neighbors ?? []).Select(row => row.Character).Union(after.Neighbors.Select(row => row.Character)))
            {
                Neighbor old = before?.Neighbors.SingleOrDefault(row => row.Character == key), next = after.Neighbors.SingleOrDefault(row => row.Character == key);
                if (old != next) Write(unit, Model(old), Model(next));
            }
        }
        private static void Write<T>(Unit unit, T before, T after) where T : class
        {
            CharacterContext context = unit.Context;
            EntityEntry<T> identity = context.Entry(after ?? before);
            string[] keys = identity.Metadata.FindPrimaryKey()!.Properties.Select(property => property.Name).ToArray();
            string[] fields = identity.Metadata.GetProperties().Select(property => property.Name).Where(name => !keys.Contains(name)).ToArray();
            object[] values = keys.Select(name => identity.Property(name).CurrentValue).ToArray();
            EntityEntry<T> entry = context.ChangeTracker.Entries<T>().SingleOrDefault(row => keys.All(name => Equals(row.Property(name).CurrentValue, identity.Property(name).CurrentValue)));
            if (entry != null)
            {
                if (before == null || entry.State != EntityState.Unchanged || fields.Any(name => !Equals(entry.Property(name).CurrentValue, context.Entry(before).Property(name).CurrentValue)))
                    throw new InvalidOperationException("Residence row already contains foreign tracked intent.");
            }
            else
            {
                entry = context.Entry(before ?? after); entry.State = before == null ? EntityState.Added : EntityState.Unchanged;
                if (before == null) for (int index = 0; index < keys.Length; index++)
                {
                    entry.Property(keys[index]).IsTemporary = false; entry.Property(keys[index]).CurrentValue = values[index];
                }
            }
            if (after == null) entry.State = EntityState.Deleted;
            else foreach (string name in fields)
            {
                if (before != null) entry.Property(name).OriginalValue = context.Entry(before).Property(name).CurrentValue;
                entry.Property(name).CurrentValue = context.Entry(after).Property(name).CurrentValue;
                if (before != null) entry.Property(name).IsModified = !Equals(entry.Property(name).CurrentValue, entry.Property(name).OriginalValue);
            }
            var guard = new CharacterColumnSaveIntent(entry, keys.Concat(fields).ToArray(), false, "Residence graph");
            context.ChangeTracker.DetectChanges(); guard.Capture(); unit.Rows.Add(guard);
        }
        private void ValidateTrackedGraph(Unit unit)
        {
            void ReadOnly(EntityEntry entry, bool matches)
            {
                if (!unit.Rows.Any(row => ReferenceEquals(row.Entry.Entity, entry.Entity)) && (entry.State != EntityState.Unchanged || !matches))
                    throw new InvalidOperationException("Residence graph contains foreign tracked intent.");
            }
            foreach (var entry in unit.Context.ChangeTracker.Entries<ResidenceModel>().Where(row => row.Entity.Id == Id))
                ReadOnly(entry, unit.Sql?.Header == HeaderFrom(entry.Entity));
            foreach (var entry in unit.Context.ChangeTracker.Entries<ResidencePlotModel>().Where(row => row.Entity.Id == Id))
                ReadOnly(entry, unit.Sql?.Plots.SingleOrDefault(row => row.Index == entry.Entity.Index) == PlotFrom(entry.Entity));
            foreach (var entry in unit.Context.ChangeTracker.Entries<ResidenceDecor>().Where(row => row.Entity.Id == Id))
                ReadOnly(entry, unit.Sql?.Decor.SingleOrDefault(row => row.Id == entry.Entity.DecorId) == DecorFrom(entry.Entity));
            foreach (var entry in unit.Context.ChangeTracker.Entries<ResidenceNeighborModel>().Where(row => row.Entity.ResidenceId == Id))
                ReadOnly(entry, unit.Sql?.Neighbors.SingleOrDefault(row => row.Character == entry.Entity.NeighborCharacterId) == new Neighbor(entry.Entity.NeighborCharacterId, entry.Entity.PermissionLevel));
        }
        private void Validate(Unit unit, CharacterSaveValidationPhase phase)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit)) throw new InvalidOperationException("Residence lost its save unit.");
                if (unit.Context.HasSavepointRollback) return;
                RequireOwner(unit.Context, unit.Reservation); unit.Context.ChangeTracker.DetectChanges(); ValidateTrackedGraph(unit);
                foreach (var row in unit.Rows)
                {
                    row.Validate();
                    if (!(unit.Flushed && row.State == EntityState.Detached)
                        && !unit.Context.ChangeTracker.Entries().Any(entry => ReferenceEquals(entry.Entity, row.Entry.Entity)))
                        throw new InvalidOperationException("Residence lost its retained tracked row.");
                }
                if (!Equal(Read(unit.Context), phase == CharacterSaveValidationPhase.BeforeFlush ? unit.Sql : unit.Target))
                    throw new InvalidOperationException("Residence SQL changed outside its complete captured graph.");
            }
        }
        private void Flushed(Unit unit)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit) || unit.Context.HasSavepointRollback) return;
                RequireOwner(unit.Context, unit.Reservation);
                if (!Equal(Read(unit.Context), unit.Target)) throw new InvalidOperationException("Residence did not flush its complete captured graph.");
                unit.Sql = unit.Target; unit.Flushed = true;
                foreach (var row in unit.Rows) { row.Capture(); row.Flushed = true; }
            }
        }
        private void Complete(Unit unit, bool committed)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit)) return;
                try
                {
                    if (committed && unit.Flushed && !unit.Context.HasSavepointRollback)
                    {
                        acknowledge(unit.Target); baseline = unit.Target; uncertain.Clear();
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
                    lock (leaseGate) if (leases.TryGetValue((database, Id), out Unit held) && ReferenceEquals(held, unit)) leases.Remove((database, Id));
                }
            }
        }
    }
}