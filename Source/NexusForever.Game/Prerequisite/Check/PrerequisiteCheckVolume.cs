using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 219 (native <c>FUN_1404a15d0</c> to <c>FUN_1404a80b0</c>, 3 retail slots, all NotEqual with Value 0): <c>ObjectId</c> is a DatacubeVolume id and the flag is true
    /// when the player's collected-bit word for the volume has at least as many bits set as the volume has datacubes for the player's faction (a volume with none is never complete);
    /// Value is not read. Only a Player or Ghost subject is evaluated (anything else gives false for every comparison). The datacube count is built by the client's table loader
    /// (<c>FUN_1404a8d30</c>): a datacube is kept only when its WorldZone row exists, the first kept datacube fixes the volume's faction (<c>DatacubeFactionEnum</c>: 0 both, 1 Exile only,
    /// 2 Dominion only) and later datacubes of a different faction are dropped; a volume with no kept datacube is discarded. The progress word is the Journal datacube of the volume id (the
    /// client keeps volumes and chronicles in one map by id; a shadowing collision was not excluded). See Decomp/Analysis/PREREQUISITE_24_25_219_NATIVE_READ_20261108.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Volume)]
    public class PrerequisiteCheckVolume : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckVolume(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, IsComplete(player, objectId));

        private bool IsComplete(IPlayer player, uint volumeId)
        {
            if (volumeId > ushort.MaxValue)
                return false;

            uint need = RequiredCount(volumeId, player.Faction1);
            if (need == 0u)
                return false;

            IDatacube volume = player.DatacubeManager?.GetDatacube((ushort)volumeId, DatacubeType.Journal);
            return volume != null && (uint)BitOperations.PopCount(volume.Progress) >= need;
        }

        private uint RequiredCount(uint volumeId, Faction faction)
        {
            DatacubeVolumeEntry volume = gameTableManager.DatacubeVolume?.GetEntry(volumeId);
            if (volume == null)
                return 0u;

            uint[] ids =
            [
                volume.DatacubeId00, volume.DatacubeId01, volume.DatacubeId02, volume.DatacubeId03,
                volume.DatacubeId04, volume.DatacubeId05, volume.DatacubeId06, volume.DatacubeId07,
                volume.DatacubeId08, volume.DatacubeId09, volume.DatacubeId10, volume.DatacubeId11,
                volume.DatacubeId12, volume.DatacubeId13, volume.DatacubeId14, volume.DatacubeId15
            ];

            uint kept = 0u;
            uint? volumeFaction = null;
            foreach (uint id in ids)
            {
                if (id == 0u)
                    continue;

                DatacubeEntry datacube = gameTableManager.Datacube?.GetEntry(id);
                if (datacube == null || gameTableManager.WorldZone?.GetEntry(datacube.WorldZoneId) == null)
                    continue;

                volumeFaction ??= datacube.DatacubeFactionEnum;
                if (datacube.DatacubeFactionEnum != volumeFaction)
                    continue;

                kept++;
            }

            if (volumeFaction == null)
                return 0u;

            bool allowed = faction switch
            {
                Faction.Dominion => volumeFaction is 0u or 2u,
                Faction.Exile    => volumeFaction is 0u or 1u,
                _                => false
            };
            return allowed ? kept : 0u;
        }
    }
}
