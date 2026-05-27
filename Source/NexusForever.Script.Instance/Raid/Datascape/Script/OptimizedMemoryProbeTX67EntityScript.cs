using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.Datascape.Script
{
    /// <summary>
    /// WIP-guessed from LaughingWS all-in-one SQL: the dump maps this script name to
    /// Optimized Memory Probe TX-67. Exact encounter mechanics remain blocked.
    /// </summary>
    [ScriptFilterScriptName("OptimizedMemoryProbeTX-67EntityScript")]
    public class OptimizedMemoryProbeTX67EntityScript : PublicEventObjectiveCreditEntityScript
    {
        public OptimizedMemoryProbeTX67EntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatOptimizedMemoryProbeTX67)
        {
        }
    }
}
