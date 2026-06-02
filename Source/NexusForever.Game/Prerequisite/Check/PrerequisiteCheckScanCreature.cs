using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 62: Unable to scan this creature. NF reads scientist scan progress bits from
    /// <see cref="IDatacubeManager.GetScientistCreatureScanProgress"/>.
    /// Native live dispatch case <c>0x3e</c> uses vtable <c>+0x4a8</c> ->
    /// <c>Prerequisite_CheckScanCreature_LiveCase3E</c> (<c>14049ff30</c>): when a target unit is present,
    /// requires scan state at unit <c>+0x3754</c>, scan subobject <c>+0x18</c> with <c>+0x188</c>/<c>+0x1c8</c>,
    /// then compares checklist bitmask at unit <c>+0x3750</c> to <c>value0</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ScanCreature)]
    public class PrerequisiteCheckScanCreature : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckScanCreature(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint creatureInfoId = objectId != 0u ? objectId : value;
            PathScientistCreatureInfoEntry entry = gameTableManager.PathScientistCreatureInfo.GetEntry(creatureInfoId);
            if (entry == null)
                return false;

            uint progress = player.DatacubeManager.GetScientistCreatureScanProgress((ushort)creatureInfoId);

            uint measured;
            if (value < 32u && entry.ChecklistCount > 1u)
                measured = PathScientistScanHelper.IsChecklistBitSet(progress, value) ? 1u : 0u;
            else
                measured = PathScientistScanHelper.IsFullyScanned(progress, entry) ? 1u : 0u;

            return PrerequisiteCompare.Compare(comparison, measured, 1u);
        }
    }
}
