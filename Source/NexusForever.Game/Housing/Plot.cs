using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Housing
{
    public class Plot : IPlot
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="IPlot"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum PlotSaveMask
        {
            None       = 0x0000,
            Create     = 0x0001,
            PlugItemId = 0x0002,
            PlugFacing = 0x0004,
            BuildState = 0x0008,
            PlotInfoId = 0x0010
        }

        public ulong Id { get; }
        public byte Index { get; }

        public HousingPlotInfoEntry PlotInfoEntry
        {
            get { lock (MutationGate) return plotInfoEntry; }
            set
            {
                lock (MutationGate)
                {
                    saveOwner?.RequirePlotEditable(this);
                    saveOwner?.RequireAssociationPlotInfoAssignment();
                    ArgumentNullException.ThrowIfNull(value);
                    ushort next = checked((ushort)value.Id);
                    plotInfoEntry = value;
                    plotInfoId = next;
                    saveMask |= PlotSaveMask.PlotInfoId;
                }
            }
        }

        private HousingPlotInfoEntry plotInfoEntry;
        private ushort plotInfoId;

        public HousingPlugItemEntry PlugItemEntry
        {
            get { lock (MutationGate) return plugItemEntry; }
            set
            {
                lock (MutationGate)
                {
                    saveOwner?.RequirePlotEditable(this);
                    ushort next = checked((ushort)(value?.Id ?? 0));
                    plugItemEntry = value;
                    plugItemId = next;
                    saveMask |= PlotSaveMask.PlugItemId;
                }
            }
        }

        private HousingPlugItemEntry plugItemEntry;
        private ushort plugItemId;

        public HousingPlugFacing PlugFacing
        {
            get { lock (MutationGate) return plugFacing; }
            set
            {
                lock (MutationGate)
                {
                    saveOwner?.RequirePlotEditable(this);
                    plugFacing = value;
                    saveMask |= PlotSaveMask.PlugFacing;
                }
            }
        }

        private HousingPlugFacing plugFacing;

        public byte BuildState
        {
            get { lock (MutationGate) return buildState; }
            set
            {
                lock (MutationGate)
                {
                    saveOwner?.RequirePlotEditable(this);
                    buildState = value;
                    saveMask |= PlotSaveMask.BuildState;
                }
            }
        }

        private byte buildState;

        private PlotSaveMask saveMask;
        private Residence saveOwner;
        private object MutationGate => saveOwner?.MutationGate ?? this;
        internal void BindSaveOwner(Residence owner)
        {
            if (saveOwner != null && !ReferenceEquals(saveOwner, owner)) throw new InvalidOperationException("Plot changed its original save owner.");
            saveOwner = owner;
        }
        internal ResidencePersistence.Plot CapturePersistence() => new(Index, plotInfoId, plugItemId, (byte)PlugFacing, BuildState);
        internal void AcknowledgePersistence(ResidencePersistence.Plot target)
        {
            saveMask &= ~PlotSaveMask.Create;
            if (CapturePersistence() == target) saveMask = PlotSaveMask.None;
        }

        public IPlugEntity PlugEntity { get; set; }
        private readonly IGameTableManager gameTableManager;

        /// <summary>
        /// Create a new <see cref="IPlot"/> from an existing database model.
        /// </summary>
        public Plot(
            ResidencePlotModel model,
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;

            Id            = model.Id;
            Index         = model.Index;
            plotInfoEntry = gameTableManager?.HousingPlotInfo?.GetEntry(model.PlotInfoId);
            plugItemEntry = gameTableManager?.HousingPlugItem?.GetEntry(model.PlugItemId);
            plotInfoId    = model.PlotInfoId;
            plugItemId    = model.PlugItemId;
            plugFacing    = (HousingPlugFacing)model.PlugFacing;
            buildState    = model.BuildState;

            saveMask = PlotSaveMask.None;
        }

        /// <summary>
        /// Create a new <see cref="IPlot"/> from a <see cref="HousingPlotInfoEntry"/>.
        /// </summary>
        public Plot(
            ulong id,
            HousingPlotInfoEntry entry,
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;

            Id            = id;
            Index         = (byte)entry.HousingPropertyPlotIndex;
            plotInfoEntry = entry;
            plotInfoId    = checked((ushort)entry.Id);
            plugFacing    = HousingPlugFacing.East;

            if (entry.HousingPlugItemIdDefault != 0u)
            {
                if (entry.HousingPlugItemIdDefault > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(entry.HousingPlugItemIdDefault));

                SetPlug((ushort)entry.HousingPlugItemIdDefault);
            }

            saveMask = PlotSaveMask.Create;
        }

        public void Save(CharacterContext context)
        {
            if (saveOwner?.SavePlot(this, context) == true) return;
            if (saveMask == PlotSaveMask.None)
                return;

            if ((saveMask & PlotSaveMask.Create) != 0)
            {
                // plot doesn't exist in database, all infomation must be saved
                context.Add(new ResidencePlotModel
                {
                    Id         = Id,
                    Index      = Index,
                    PlotInfoId = plotInfoId,
                    PlugItemId = plugItemId,
                    PlugFacing = (byte)PlugFacing,
                    BuildState = BuildState
                });
            }
            else
            {
                // plot already exists in database, save only data that has been modified
                var model = new ResidencePlotModel
                {
                    Id    = Id,
                    Index = Index,
                };

                EntityEntry<ResidencePlotModel> entity = context.Attach(model);
                if ((saveMask & PlotSaveMask.PlotInfoId) != 0)
                {
                    model.PlotInfoId = plotInfoId;
                    entity.Property(p => p.PlotInfoId).IsModified = true;
                }

                if ((saveMask & PlotSaveMask.PlugItemId) != 0)
                {
                    model.PlugItemId = plugItemId;
                    entity.Property(p => p.PlugItemId).IsModified = true;
                }

                if ((saveMask & PlotSaveMask.PlugFacing) != 0)
                {
                    model.PlugFacing = (byte)PlugFacing;
                    entity.Property(p => p.PlugFacing).IsModified = true;
                }

                if ((saveMask & PlotSaveMask.BuildState) != 0)
                {
                    model.BuildState = BuildState;
                    entity.Property(p => p.BuildState).IsModified = true;
                }
            }

            saveMask = PlotSaveMask.None;
        }

        public void SetPlug(ushort plugItemId)
        {
            lock (MutationGate)
            {
                saveOwner?.RequirePlotEditable(this);
                HousingPlugItemEntry entry = gameTableManager?.HousingPlugItem?.GetEntry(plugItemId);
                if (entry == null)
                    throw new ArgumentOutOfRangeException(nameof(plugItemId));

                PlugItemEntry = entry;
                BuildState = 4;
            }
        }
    }
}
