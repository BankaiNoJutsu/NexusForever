using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.OtherPrerequisite)]
    public class PrerequisiteCheckOtherPrerequisite : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckOtherPrerequisite> log;
        private readonly IPrerequisiteManager prerequisiteManager;

        public PrerequisiteCheckOtherPrerequisite(
            ILogger<PrerequisiteCheckOtherPrerequisite> log,
            IPrerequisiteManager prerequisiteManager)
        {
            this.log                 = log;
            this.prerequisiteManager = prerequisiteManager;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            // Native (evaluator case 0xb): comparison 2 negates the nested result, every other comparison returns it. Retail uses comparison 0 in 22 slots.
            bool nested = prerequisiteManager.Meets(player, objectId, parameters);
            return comparison == PrerequisiteComparison.NotEqual ? !nested : nested;
        }

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            bool nested = prerequisiteManager.MeetsUnit(subject, objectId, parameters);
            return comparison == PrerequisiteComparison.NotEqual ? !nested : nested;
        }
    }
}
