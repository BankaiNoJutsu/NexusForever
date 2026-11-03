using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Housing
{
    public class Decor : IDecor
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="IDecor"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum DecorSaveMask
        {
            None               = 0x0000,
            Create             = 0x0001,
            Delete             = 0x0002,
            Type               = 0x0004,
            Position           = 0x0008,
            Rotation           = 0x0010,
            Scale              = 0x0020,
            DecorParentId      = 0x0040,
            ColourShiftId      = 0x0080,
            PlotIndex          = 0x0100,
            DecorInfo          = 0x0200,
            DecorData          = 0x0400,
            HookBagIndex       = 0x0800,
            HookIndex          = 0x1000,
            ActivePropUnitId   = 0x2000
        }

        public ulong Id => Residence.Id;
        public ulong DecorId { get; }
        public uint DecorInfoId => decorInfoId;
        public HousingDecorInfoEntry Entry { get; private set; }

        private uint decorInfoId;

        public DecorType Type
        {
            get { lock (MutationGate) return type; }
            set
            {
                lock (MutationGate)
                {
                    (Residence as Housing.Residence)?.RequireDecorEditable(this);
                    type = value;
                    saveMask |= DecorSaveMask.Type;
                }
            }
        }

        private DecorType type;

        public uint DecorData
        {
            get { lock (MutationGate) return decorData; }
            set
            {
                lock (MutationGate)
                {
                    (Residence as Housing.Residence)?.RequireDecorEditable(this);
                    decorData = value;
                    saveMask |= DecorSaveMask.DecorData;
                }
            }
        }

        private uint decorData;

        public uint HookBagIndex
        {
            get { lock (MutationGate) return hookBagIndex; }
            set
            {
                lock (MutationGate)
                {
                    (Residence as Housing.Residence)?.RequireDecorEditable(this);
                    hookBagIndex = value;
                    saveMask |= DecorSaveMask.HookBagIndex;
                }
            }
        }

        private uint hookBagIndex;

        public uint HookIndex
        {
            get { lock (MutationGate) return hookIndex; }
            set
            {
                lock (MutationGate)
                {
                    (Residence as Housing.Residence)?.RequireDecorEditable(this);
                    hookIndex = value;
                    saveMask |= DecorSaveMask.HookIndex;
                }
            }
        }

        private uint hookIndex;

        public uint PlotIndex
        {
            get { lock (MutationGate) return plotIndex; }
            set
            {
                lock (MutationGate)
                {
                    (Residence as Housing.Residence)?.RequireDecorEditable(this);
                    plotIndex = value;
                    saveMask |= DecorSaveMask.PlotIndex;
                }
            }
        }

        private uint plotIndex;

        public Vector3 Position
        {
            get { lock (MutationGate) return position; }
            set
            {
                lock (MutationGate)
                {
                    (Residence as Housing.Residence)?.RequireDecorEditable(this);
                    position = value;
                    saveMask |= DecorSaveMask.Position;
                }
            }
        }

        private Vector3 position;

        public Quaternion Rotation
        {
            get { lock (MutationGate) return rotation; }
            set
            {
                lock (MutationGate)
                {
                    (Residence as Housing.Residence)?.RequireDecorEditable(this);
                    rotation = value;
                    saveMask |= DecorSaveMask.Rotation;
                }
            }
        }

        private Quaternion rotation;

        public float Scale
        {
            get { lock (MutationGate) return scale; }
            set
            {
                lock (MutationGate)
                {
                    (Residence as Housing.Residence)?.RequireDecorEditable(this);
                    scale = value;
                    saveMask |= DecorSaveMask.Scale;
                }
            }
        }

        private float scale;

        public uint ActivePropUnitId
        {
            get { lock (MutationGate) return activePropUnitId; }
            set
            {
                lock (MutationGate)
                {
                    (Residence as Housing.Residence)?.RequireDecorEditable(this);
                    activePropUnitId = value;
                    saveMask |= DecorSaveMask.ActivePropUnitId;
                }
            }
        }

        private uint activePropUnitId;

        public ulong DecorParentId
        {
            get { lock (MutationGate) return decorParentId; }
            set
            {
                lock (MutationGate)
                {
                    (Residence as Housing.Residence)?.RequireDecorEditable(this);
                    decorParentId = value;
                    saveMask |= DecorSaveMask.DecorParentId;
                }
            }
        }

        private ulong decorParentId;

        public ushort ColourShiftId
        {
            get { lock (MutationGate) return colourShiftId; }
            set
            {
                lock (MutationGate)
                {
                    (Residence as Housing.Residence)?.RequireDecorEditable(this);
                    colourShiftId = value;
                    saveMask |= DecorSaveMask.ColourShiftId;
                }
            }
        }

        private ushort colourShiftId;

        public IResidence Residence { get; }

        private DecorSaveMask saveMask;
        private object MutationGate => (Residence as Housing.Residence)?.MutationGate ?? this;
        internal ResidencePersistence.Decoration CapturePersistence() => new(DecorId, DecorInfoId, (uint)Type, DecorData,
            HookBagIndex, HookIndex, PlotIndex, Scale, Position.X, Position.Y, Position.Z, Rotation.X, Rotation.Y, Rotation.Z, Rotation.W,
            ActivePropUnitId, DecorParentId, ColourShiftId);
        internal void AcknowledgePersistence(ResidencePersistence.Decoration target)
        {
            saveMask &= ~DecorSaveMask.Create;
            if (!PendingDelete && CapturePersistence() == target) saveMask = DecorSaveMask.None;
        }

        /// <summary>
        /// Returns if <see cref="IDecor"/> is enqueued to be saved to the database.
        /// </summary>
        public bool PendingCreate => (saveMask & DecorSaveMask.Create) != 0;

        /// <summary>
        /// Returns if <see cref="IDecor"/> is enqueued to be deleted from the database.
        /// </summary>
        public bool PendingDelete => (saveMask & DecorSaveMask.Delete) != 0;

        /// <summary>
        /// Enqueue <see cref="IDecor"/> to be deleted from the database.
        /// </summary>
        public void EnqueueDelete(bool set)
        {
            lock (MutationGate)
            {
                (Residence as Housing.Residence)?.RequireDecorEditable(this);
                saveMask = DecorSaveMask.Delete;
            }
        }

        /// <summary>
        /// Create a new <see cref="IDecor"/> from an existing database model.
        /// </summary>
        public Decor(IResidence residence, ResidenceDecor model, HousingDecorInfoEntry entry)
        {
            DecorId       = model.DecorId;
            decorInfoId   = model.DecorInfoId;
            Entry         = entry;
            type          = (DecorType)model.DecorType;
            decorData     = model.DecorData;
            hookBagIndex  = model.HookBagIndex;
            hookIndex     = model.HookIndex;
            plotIndex     = model.PlotIndex;
            position      = new Vector3(model.X, model.Y, model.Z);
            rotation      = new Quaternion(model.Qx, model.Qy, model.Qz, model.Qw);
            scale         = model.Scale;
            activePropUnitId = model.ActivePropUnitId;
            decorParentId = model.DecorParentId;
            colourShiftId = model.ColourShiftId;
            Residence     = residence;

            saveMask = DecorSaveMask.None;
        }

        /// <summary>
        /// Create a new <see cref="IDecor"/> from a <see cref="HousingDecorInfoEntry"/> template.
        /// </summary>
        public Decor(IResidence residence, ulong decorId, HousingDecorInfoEntry entry)
        {
            DecorId   = decorId;
            decorInfoId = entry?.Id ?? throw new ArgumentNullException(nameof(entry));
            Entry     = entry;
            type      = DecorType.Crate;
            position  = Vector3.Zero;
            rotation  = Quaternion.Identity;
            Residence = residence;

            saveMask = DecorSaveMask.Create;
        }

        public Decor(IResidence residence, ulong decorId, uint decorInfoId, DecorType type)
        {
            DecorId          = decorId;
            this.decorInfoId = decorInfoId;
            this.type        = type;
            position         = Vector3.Zero;
            rotation         = Quaternion.Identity;
            Residence        = residence;

            saveMask = DecorSaveMask.Create;
        }

        /// <summary>
        /// Create a new <see cref="IDecor"/> from an existing <see cref="IDecor"/>.
        /// </summary>
        /// <remarks>
        /// Copies all data from the source <see cref="IDecor"/> with a new id.
        /// </remarks>
        public Decor(IResidence residence, IDecor decor, ulong decorId)
        {
            DecorId       = decorId;
            decorInfoId   = decor.DecorInfoId;
            Entry         = decor.Entry;
            type          = decor.Type;
            decorData     = decor.DecorData;
            hookBagIndex  = decor.HookBagIndex;
            hookIndex     = decor.HookIndex;
            plotIndex     = decor.PlotIndex;
            position      = decor.Position;
            rotation      = decor.Rotation;
            scale         = decor.Scale;
            activePropUnitId = decor.ActivePropUnitId;
            decorParentId = decor.DecorParentId;
            colourShiftId = decor.ColourShiftId;
            Residence     = residence;

            saveMask = DecorSaveMask.Create;
        }

        public void Save(CharacterContext context)
        {
            if (Residence is Housing.Residence owner && owner.SaveDecor(this, context)) return;
            if (saveMask == DecorSaveMask.None)
                return;

            if ((saveMask & DecorSaveMask.Create) != 0)
            {
                // decor doesn't exist in database, all infomation must be saved
                context.Add(new ResidenceDecor
                {
                    Id            = Id,
                    DecorId       = DecorId,
                    DecorInfoId   = DecorInfoId,
                    DecorType     = (uint)Type,
                    DecorData     = DecorData,
                    HookBagIndex  = HookBagIndex,
                    HookIndex     = HookIndex,
                    PlotIndex     = PlotIndex,
                    X             = Position.X,
                    Y             = Position.Y,
                    Z             = Position.Z,
                    Qx            = Rotation.X,
                    Qy            = Rotation.Y,
                    Qz            = Rotation.Z,
                    Qw            = Rotation.W,
                    Scale         = Scale,
                    ActivePropUnitId = ActivePropUnitId,
                    DecorParentId = DecorParentId,
                    ColourShiftId = ColourShiftId
                });
            }
            else if ((saveMask & DecorSaveMask.Delete) != 0)
            {
                var model = new ResidenceDecor
                {
                    Id      = Id,
                    DecorId = DecorId
                };

                context.Entry(model).State = EntityState.Deleted;
            }
            else
            {
                // decor already exists in database, save only data that has been modified
                var model = new ResidenceDecor
                {
                    Id      = Id,
                    DecorId = DecorId
                };

                // could probably clean this up with reflection, works for the time being
                EntityEntry<ResidenceDecor> entity = context.Attach(model);
                if ((saveMask & DecorSaveMask.DecorInfo) != 0)
                {
                    model.DecorInfoId = DecorInfoId;
                    entity.Property(p => p.DecorInfoId).IsModified = true;
                }
                if ((saveMask & DecorSaveMask.Type) != 0)
                {
                    model.DecorType = (uint)Type;
                    entity.Property(p => p.DecorType).IsModified = true;
                }
                if ((saveMask & DecorSaveMask.DecorData) != 0)
                {
                    model.DecorData = DecorData;
                    entity.Property(p => p.DecorData).IsModified = true;
                }
                if ((saveMask & DecorSaveMask.HookBagIndex) != 0)
                {
                    model.HookBagIndex = HookBagIndex;
                    entity.Property(p => p.HookBagIndex).IsModified = true;
                }
                if ((saveMask & DecorSaveMask.HookIndex) != 0)
                {
                    model.HookIndex = HookIndex;
                    entity.Property(p => p.HookIndex).IsModified = true;
                }
                if ((saveMask & DecorSaveMask.PlotIndex) != 0)
                {
                    model.PlotIndex = PlotIndex;
                    entity.Property(p => p.PlotIndex).IsModified = true;
                }
                if ((saveMask & DecorSaveMask.Position) != 0)
                {
                    model.X = Position.X;
                    entity.Property(p => p.X).IsModified = true;
                    model.Y = Position.Y;
                    entity.Property(p => p.Y).IsModified = true;
                    model.Z = Position.Z;
                    entity.Property(p => p.Z).IsModified = true;
                }
                if ((saveMask & DecorSaveMask.Rotation) != 0)
                {
                    model.Qx = Rotation.X;
                    entity.Property(p => p.Qx).IsModified = true;
                    model.Qy = Rotation.Y;
                    entity.Property(p => p.Qy).IsModified = true;
                    model.Qz = Rotation.Z;
                    entity.Property(p => p.Qz).IsModified = true;
                    model.Qw = Rotation.W;
                    entity.Property(p => p.Qw).IsModified = true;
                }
                if ((saveMask & DecorSaveMask.Scale) != 0)
                {
                    model.Scale = Scale;
                    entity.Property(p => p.Scale).IsModified = true;
                }
                if ((saveMask & DecorSaveMask.ActivePropUnitId) != 0)
                {
                    model.ActivePropUnitId = ActivePropUnitId;
                    entity.Property(p => p.ActivePropUnitId).IsModified = true;
                }
                if ((saveMask & DecorSaveMask.DecorParentId) != 0)
                {
                    model.DecorParentId = DecorParentId;
                    entity.Property(p => p.DecorParentId).IsModified = true;
                }
                if ((saveMask & DecorSaveMask.ColourShiftId) != 0)
                {
                    model.ColourShiftId = ColourShiftId;
                    entity.Property(p => p.ColourShiftId).IsModified = true;
                }
            }

            saveMask = DecorSaveMask.None;
        }

        /// <summary>
        /// Move <see cref="IDecor"/> to supplied position.
        /// </summary>
        public void Move(DecorType type, Vector3 position, Quaternion rotation, float scale, uint plotIndex)
        {
            lock (MutationGate)
            {
                (Residence as Housing.Residence)?.RequireDecorEditable(this);
                Type      = type;
                Position  = position;
                Rotation  = rotation;
                Scale     = scale;
                PlotIndex = plotIndex;
            }
        }

        public void UpdateEntry(HousingDecorInfoEntry entry)
        {
            lock (MutationGate)
            {
                (Residence as Housing.Residence)?.RequireDecorEditable(this);
                Entry = entry ?? throw new ArgumentNullException(nameof(entry));
                decorInfoId = entry.Id;
                saveMask |= DecorSaveMask.DecorInfo;
            }
        }

        public void UpdateDecorInfoId(uint decorInfoId)
        {
            lock (MutationGate)
            {
                (Residence as Housing.Residence)?.RequireDecorEditable(this);
                this.decorInfoId = decorInfoId;
                Entry = null;
                saveMask |= DecorSaveMask.DecorInfo;
            }
        }

        /// <summary>
        /// Move <see cref="IDecor"/> to the crate.
        /// </summary>
        public void Crate()
        {
            lock (MutationGate)
            {
                (Residence as Housing.Residence)?.RequireDecorEditable(this);
                Move(DecorType.Crate, Vector3.Zero, Quaternion.Identity, 0f, int.MaxValue);
                DecorParentId = 0u;
            }
        }

        public ServerHousingResidenceDecor.Decor Build()
        {
            lock (MutationGate)
            {
                return new()
                {
                    RealmId       = Residence.RealmId,
                    DecorId       = DecorId,
                    ResidenceId   = Residence.Id,
                    DecorType     = Type,
                    DecorData     = DecorData,
                    HookBagIndex  = HookBagIndex,
                    HookIndex     = HookIndex,
                    PlotIndex     = PlotIndex,
                    Scale         = Scale,
                    Position      = Position,
                    Rotation      = Rotation,
                    DecorInfoId   = DecorInfoId,
                    ActivePropUnitId = ActivePropUnitId,
                    ParentDecorId = DecorParentId,
                    ColourShift   = ColourShiftId
                };
            }
        }
    }
}
