using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 539 to TargetGroup 2977,
    /// whose Creature2 members are the intro Thundercall Pell rush rows 16728 and 26448.
    /// </summary>
    [ScriptFilterCreatureId(16728u, 26448u)]
    public class ThundercallZealotRushEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ThundercallZealotRushEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.SurviveTheThundercallPellZealots)
        {
        }
    }
}
