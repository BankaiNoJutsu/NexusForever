using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 321 to TargetGroup 2599,
    /// whose Creature2 members are Stew-Shaman Tugga 24493 and 24898.
    /// </summary>
    [ScriptFilterCreatureId(24493u, 24898u)]
    public class StewShamanTuggaNormalEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public StewShamanTuggaNormalEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatStewShamanTugga)
        {
        }
    }
}
