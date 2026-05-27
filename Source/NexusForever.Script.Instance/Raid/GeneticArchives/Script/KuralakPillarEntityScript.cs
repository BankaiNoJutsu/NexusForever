using NexusForever.Game.Abstract.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Raid.GeneticArchives.Script
{
    /// <summary>
    /// WIP-guessed loader hook for LaughingWS worlddb SQL. The branch attaches a script
    /// name to Kuralak's pillar but provides no safe behavior; mechanics remain blocked.
    /// </summary>
    [ScriptFilterScriptName("KuralakPillarEntityScript")]
    public class KuralakPillarEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        public void OnLoad(ICreatureEntity owner)
        {
        }
    }
}
