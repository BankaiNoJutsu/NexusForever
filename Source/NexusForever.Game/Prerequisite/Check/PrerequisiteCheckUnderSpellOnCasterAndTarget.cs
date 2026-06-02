using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 279: live case <c>0x117</c> requires caster and target to pass
    /// <c>Prerequisite_CheckUnderSpell</c> (<c>1404a4ec0</c>).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.UnderSpellOnCasterAndTarget)]
    public class PrerequisiteCheckUnderSpellOnCasterAndTarget : IPrerequisiteCheck
    {
        private readonly ILogger<PrerequisiteCheckUnderSpellOnCasterAndTarget> log;

        public PrerequisiteCheckUnderSpellOnCasterAndTarget(ILogger<PrerequisiteCheckUnderSpellOnCasterAndTarget> log)
        {
            this.log = log;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint spell4Id = value != 0u ? value : objectId;

            if (!Evaluate(player, comparison, spell4Id))
                return false;

            if (parameters.Target is not IUnitEntity target)
                return false;

            return Evaluate(target, comparison, spell4Id);
        }

        private bool Evaluate(IUnitEntity unit, PrerequisiteComparison comparison, uint spell4Id)
        {
            bool underSpell = SpellPrerequisiteHelper.IsUnderSpell(unit, spell4Id);
            return comparison switch
            {
                PrerequisiteComparison.Equal    => underSpell,
                PrerequisiteComparison.NotEqual => !underSpell,
                _ => LogUnhandled(comparison)
            };
        }

        private bool LogUnhandled(PrerequisiteComparison comparison)
        {
            log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.UnderSpellOnCasterAndTarget}!");
            return false;
        }
    }
}
