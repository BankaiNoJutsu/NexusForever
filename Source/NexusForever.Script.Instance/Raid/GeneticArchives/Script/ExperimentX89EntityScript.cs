using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.GeneticArchives.Script
{
    /// <summary>
    /// WIP-guessed from LaughingWS worlddb SQL: the branch only maps this boss script
    /// to the Genetic Archives objective. Combat mechanics remain blocked.
    /// </summary>
    [ScriptFilterScriptName("ExperimentX-89EntityScript")]
    public class ExperimentX89EntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ExperimentX89EntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatExperimentX89)
        {
        }
    }
}
