using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script
{
    /// <summary>
    /// WIP-guessed from LaughingWS worlddb SQL: the branch only maps this miniboss
    /// script to the Sanctuary objective. Combat mechanics remain blocked.
    /// </summary>
    [ScriptFilterScriptName("FlameCrazedDemonEntityScript")]
    public class FlameCrazedDemonEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public FlameCrazedDemonEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DestroyTheFlameCrazedDemon)
        {
        }
    }
}
