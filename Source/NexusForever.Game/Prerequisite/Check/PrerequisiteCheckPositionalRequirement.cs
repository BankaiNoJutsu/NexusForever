using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.PositionalRequirement)]
    public class PrerequisiteCheckPositionalRequirement : IPrerequisiteCheck
    {
        private readonly ILogger<PrerequisiteCheckPositionalRequirement> log;
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckPositionalRequirement(
            ILogger<PrerequisiteCheckPositionalRequirement> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Target is not IUnitEntity facingEntity || objectId == 0)
                return false;

            PositionalRequirementEntry entry = gameTableManager.PositionalRequirement.GetEntry(objectId);
            if (entry == null)
                return false;

            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                case PrerequisiteComparison.NotEqual:
                    return PositionalPrerequisiteHelper.MeetsDirectionalRequirement(player, comparison, facingEntity, entry);
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.PositionalRequirement}!");
                    return false;
            }
        }
    }
}
