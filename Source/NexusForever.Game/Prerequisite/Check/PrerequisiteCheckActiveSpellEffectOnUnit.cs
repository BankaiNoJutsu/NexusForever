using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 129: handler table <c>1404a0540</c>; live case <c>0x81</c> walks active spell effects on unit.
    /// NF checks tracked spell state / pending spell for Spell4 <c>value0</c>, then applies the same grouped Spell4
    /// comparison used by rental-license spell rows that share a Spell4 group list.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ActiveSpellEffectOnUnit)]
    public class PrerequisiteCheckActiveSpellEffectOnUnit : IPrerequisiteCheck
    {
        private readonly ILogger<PrerequisiteCheckActiveSpellEffectOnUnit> log;
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckActiveSpellEffectOnUnit(
            ILogger<PrerequisiteCheckActiveSpellEffectOnUnit> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IUnitEntity unit = parameters.Target as IUnitEntity ?? player;
            bool active = HasActiveSpellEffect(unit, value, objectId);

            return comparison switch
            {
                PrerequisiteComparison.Equal    => active,
                PrerequisiteComparison.NotEqual => !active,
                _ => LogUnhandled(comparison)
            };
        }

        private bool HasActiveSpellEffect(IUnitEntity unit, uint spell4Id, uint objectId)
        {
            if (spell4Id != 0u)
            {
                return SpellPrerequisiteHelper.HasActiveSpell4(unit, spell4Id)
                    || SpellPrerequisiteHelper.HasActiveSpellMatchingSpell4Group(unit, spell4Id, gameTableManager);
            }

            return SpellPrerequisiteHelper.HasActiveSpell4(unit, objectId);
        }

        private bool LogUnhandled(PrerequisiteComparison comparison)
        {
            log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.ActiveSpellEffectOnUnit}!");
            return false;
        }
    }
}
