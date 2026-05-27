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
    [ScriptFilterScriptName("KuralakTheDefilerEntityScript")]
    public class KuralakTheDefilerEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public KuralakTheDefilerEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatKuralakTheDefiler)
        {
        }
    }
}
