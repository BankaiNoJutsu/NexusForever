using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 129: handler table <c>1404a0540</c>; live case <c>0x81</c> walks active spell effects on unit.
    /// NF checks tracked spell state / pending spell for Spell4 <c>value0</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ActiveSpellEffectOnUnit)]
    public class PrerequisiteCheckActiveSpellEffectOnUnit : IPrerequisiteCheck
    {
        private readonly ILogger<PrerequisiteCheckActiveSpellEffectOnUnit> log;

        public PrerequisiteCheckActiveSpellEffectOnUnit(ILogger<PrerequisiteCheckActiveSpellEffectOnUnit> log)
        {
            this.log = log;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IUnitEntity unit = parameters.Target as IUnitEntity ?? player;
            uint spell4Id = value != 0u ? value : objectId;
            bool active = SpellPrerequisiteHelper.HasActiveSpell4(unit, spell4Id);

            return comparison switch
            {
                PrerequisiteComparison.Equal    => active,
                PrerequisiteComparison.NotEqual => !active,
                _ => LogUnhandled(comparison)
            };
        }

        private bool LogUnhandled(PrerequisiteComparison comparison)
        {
            log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.ActiveSpellEffectOnUnit}!");
            return false;
        }
    }
}
