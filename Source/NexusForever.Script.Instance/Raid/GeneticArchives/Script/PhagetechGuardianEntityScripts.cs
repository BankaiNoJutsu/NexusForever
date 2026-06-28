using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.GeneticArchives.Script
{
    /// <summary>
    /// Build 16042 maps objective 2393 to KillTargetGroup 8417,
    /// whose TargetGroup member is Creature2 54785, Phagetech Guardian C-148.
    /// </summary>
    [ScriptFilterCreatureId(54785u)]
    public class PhagetechGuardianC148EntityScript : PublicEventObjectiveCreditEntityScript
    {
        public PhagetechGuardianC148EntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatPhagetechGuardianC148)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 2394 to KillTargetGroup 8418,
    /// whose TargetGroup member is Creature2 54787, Phagetech Guardian C-432.
    /// </summary>
    [ScriptFilterCreatureId(54787u)]
    public class PhagetechGuardianC432EntityScript : PublicEventObjectiveCreditEntityScript
    {
        public PhagetechGuardianC432EntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatPhagetechGuardianC432)
        {
        }
    }
}
