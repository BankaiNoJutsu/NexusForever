using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.GeneticArchives.Script
{
    /// <summary>
    /// Build 16042 maps objective 411 to KillTargetGroup 7217,
    /// whose TargetGroup member is Creature2 52974, Phage Maw.
    /// </summary>
    [ScriptFilterCreatureId(52974u)]
    public class PhageMawEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public PhageMawEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatPhageMaw)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 413 to KillTargetGroup 8304,
    /// whose TargetGroup members are the Phagetech Prototype Creature2 rows.
    /// </summary>
    [ScriptFilterCreatureId(54029u, 54030u, 54031u, 54032u)]
    public class PhagetechPrototypesEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public PhagetechPrototypesEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatThePhagetechPrototypes)
        {
        }
    }
}
