using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 50: live <c>Prerequisite_CheckUnderSpellOnTargetUnit</c> (<c>1404699f0</c>);
    /// handler table[50] shares mount-vehicle pointer — use live helper. <c>value0</c> is Spell4 id on target.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.UnderSpellOnTarget)]
    public class PrerequisiteCheckUnderSpellOnTarget : IPrerequisiteCheck
    {
        private readonly ILogger<PrerequisiteCheckUnderSpellOnTarget> log;

        public PrerequisiteCheckUnderSpellOnTarget(ILogger<PrerequisiteCheckUnderSpellOnTarget> log)
        {
            this.log = log;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Target is not IUnitEntity target)
                return false;

            uint spell4Id = value != 0u ? value : objectId;
            bool underSpell = SpellPrerequisiteHelper.IsUnderSpell(target, spell4Id);

            return comparison switch
            {
                PrerequisiteComparison.Equal    => underSpell,
                PrerequisiteComparison.NotEqual => !underSpell,
                _ => LogUnhandled(comparison)
            };
        }

        private bool LogUnhandled(PrerequisiteComparison comparison)
        {
            log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.UnderSpellOnTarget}!");
            return false;
        }
    }
}
