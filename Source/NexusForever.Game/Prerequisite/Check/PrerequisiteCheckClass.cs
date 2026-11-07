using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.Class)]
    public class PrerequisiteCheckClass : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckClass> log;

        public PrerequisiteCheckClass(
            ILogger<PrerequisiteCheckClass> log)
        {
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                    return player.Class == (Class)value;
                case PrerequisiteComparison.NotEqual:
                    return player.Class != (Class)value;
                default:
                    // Native (FUN_14049c560): comparisons other than Equal and NotEqual are false.
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.Class}!");
                    return false;
            }
        }
    }
}
