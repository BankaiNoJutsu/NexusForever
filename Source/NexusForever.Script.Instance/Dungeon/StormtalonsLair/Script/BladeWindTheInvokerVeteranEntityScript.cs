using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    /// <summary>
    /// WIP-guessed from LaughingWS worlddb SQL: the branch only maps this boss script
    /// to the Stormtalon's Lair objective. Combat mechanics remain blocked.
    /// </summary>
    [ScriptFilterScriptName("BladeWindTheInvokerVeteranEntityScript")]
    public class BladeWindTheInvokerVeteranEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public BladeWindTheInvokerVeteranEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatBladeWindTheInvoker)
        {
        }
    }
}
