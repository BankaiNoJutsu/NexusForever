using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 313 to TargetGroup 2586,
    /// whose Creature2 members are Aethros 17166 and 32703.
    /// </summary>
    [ScriptFilterCreatureId(17166u, 32703u)]
    public class AethrosVeteranEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public AethrosVeteranEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.EliminateAethros)
        {
        }
    }
}
