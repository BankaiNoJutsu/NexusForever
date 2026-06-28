using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.ProtogamesAcademy.Script
{
    /// <summary>
    /// Build 16042 maps objective 4499 to TargetGroup 12361, whose Creature2
    /// member is Super-Invulnotron 68096. Combat mechanics remain blocked.
    /// </summary>
    [ScriptFilterCreatureId(68096u)]
    public class SuperInvulnotronEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public SuperInvulnotronEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatSuperInvulnotron)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps the final Protogames Academy script objective 4346 and
    /// dungeon quest completions to Wrathbone Creature2 67944. Combat mechanics
    /// and reward side effects remain blocked.
    /// </summary>
    [ScriptFilterCreatureId(67944u)]
    public class WrathboneEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public WrathboneEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatWrathbone)
        {
        }
    }
}
