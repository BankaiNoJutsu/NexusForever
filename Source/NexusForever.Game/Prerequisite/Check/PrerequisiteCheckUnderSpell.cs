using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.UnderSpell)]
    public class PrerequisiteCheckUnderSpell : IPrerequisiteCheck
    {
        private readonly ILogger<PrerequisiteCheckUnderSpell> log;

        public PrerequisiteCheckUnderSpell(
            ILogger<PrerequisiteCheckUnderSpell> log)
        {
            this.log = log;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint spell4Id = value != 0u ? value : objectId;
            bool isUnderSpell = player.HasTrackedSpellState(spell4Id);

            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                    return isUnderSpell;
                case PrerequisiteComparison.NotEqual:
                    return !isUnderSpell;
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.UnderSpell}!");
                    return false;
            }
        }
    }
}
