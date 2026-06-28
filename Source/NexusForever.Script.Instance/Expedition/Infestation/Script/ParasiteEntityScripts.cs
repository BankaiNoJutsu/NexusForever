using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.Infestation.Script
{
    /// <summary>
    /// Build 16042 maps objective 229 text directly to Creature2 22669
    /// (Lumbering Parasite). Exact route and spawn timing remain blocked.
    /// </summary>
    [ScriptFilterCreatureId(22669u)]
    public class LumberingParasiteEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public LumberingParasiteEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillLumberingParasites)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1500 text directly to Creature2 26833
    /// (Cyclopean Parasite). Exact route and spawn timing remain blocked.
    /// </summary>
    [ScriptFilterCreatureId(26833u)]
    public class CyclopeanParasiteEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public CyclopeanParasiteEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillCyclopeanParasite)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4698 to KillTargetGroup 12905,
    /// whose Creature2 member is Lashing Fiend 69871.
    /// </summary>
    [ScriptFilterCreatureId(69871u)]
    public class LashingFiendEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public LashingFiendEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheAttackOnMedbay)
        {
        }
    }
}
