using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.InSubZone)]
    public class PrerequisiteCheckInSubZone : IPrerequisiteCheck
    {
        private readonly ILogger<PrerequisiteCheckInSubZone> log;
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckInSubZone(
            ILogger<PrerequisiteCheckInSubZone> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint zoneId = value != 0u ? value : objectId;
            bool isInZone = IsInZone(player.Zone, zoneId);

            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                    return isInZone;
                case PrerequisiteComparison.NotEqual:
                    return !isInZone;
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.InSubZone}!");
                    return false;
            }
        }

        private bool IsInZone(WorldZoneEntry zone, uint zoneId)
        {
            if (zone == null || zoneId == 0u)
                return false;

            HashSet<uint> visitedZones = [];
            WorldZoneEntry currentZone = zone;
            while (currentZone != null && visitedZones.Add(currentZone.Id))
            {
                if (currentZone.Id == zoneId)
                    return true;

                currentZone = currentZone.ParentZoneId == 0u
                    ? null
                    : gameTableManager.WorldZone.GetEntry(currentZone.ParentZoneId);
            }

            return false;
        }
    }
}
