using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 130: live <c>Prerequisite_CheckActiveSpellEffectOnTarget</c> (<c>140469a70</c>),
    /// handler table <c>1404a0590</c>. NF checks caster has active Spell4 <c>value0</c> affecting prerequisite target.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ActiveSpellEffectOnTarget)]
    public class PrerequisiteCheckActiveSpellEffectOnTarget : IPrerequisiteCheck
    {
        private readonly ILogger<PrerequisiteCheckActiveSpellEffectOnTarget> log;

        public PrerequisiteCheckActiveSpellEffectOnTarget(ILogger<PrerequisiteCheckActiveSpellEffectOnTarget> log)
        {
            this.log = log;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Target == null)
                return false;

            uint spell4Id = value != 0u ? value : objectId;
            bool active = SpellPrerequisiteHelper.HasActiveSpell4(player, spell4Id);

            return comparison switch
            {
                PrerequisiteComparison.Equal    => active,
                PrerequisiteComparison.NotEqual => !active,
                _ => LogUnhandled(comparison)
            };
        }

        private bool LogUnhandled(PrerequisiteComparison comparison)
        {
            log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.ActiveSpellEffectOnTarget}!");
            return false;
        }
    }
}
