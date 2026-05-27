using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.RedMoonTerror.Script
{
    /// <summary>
    /// WIP-guessed from the LaughingWS Red Moon Terror Laveka placement row plus
    /// the current final raid objective. The source row has no entity_script hook,
    /// so this only grants objective credit on death; Laveka combat, awakening,
    /// apparition challenge logic, and choreography remain blocked pending proof.
    /// </summary>
    [ScriptFilterScriptName("LavekaTheDarkHeartedEntityScript")]
    public class LavekaTheDarkHeartedEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public LavekaTheDarkHeartedEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatLavekaTheDarkHearted)
        {
        }
    }
}
