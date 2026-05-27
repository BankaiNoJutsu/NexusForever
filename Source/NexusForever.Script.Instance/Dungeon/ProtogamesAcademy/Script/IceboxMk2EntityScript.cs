using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.ProtogamesAcademy.Script
{
    /// <summary>
    /// WIP-guessed from the LaughingWS Protogames Academy placement row plus the
    /// current mapped defeat objective. The source row has no entity_script hook,
    /// so this only grants objective credit on death; combat mechanics and arena
    /// choreography remain blocked pending proof.
    /// </summary>
    [ScriptFilterScriptName("IceboxMk2EntityScript")]
    public class IceboxMk2EntityScript : PublicEventObjectiveCreditEntityScript
    {
        public IceboxMk2EntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatIceboxMk2)
        {
        }
    }
}
