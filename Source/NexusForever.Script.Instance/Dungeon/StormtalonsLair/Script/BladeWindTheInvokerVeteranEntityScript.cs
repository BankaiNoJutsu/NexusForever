using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 312 to TargetGroup 2585,
    /// whose Creature2 members are Blade-Wind the Invoker 17160 and 33405.
    /// </summary>
    [ScriptFilterCreatureId(17160u, 33405u)]
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
