using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 453 to TargetGroup 3850,
    /// whose Creature2 members are Darkwitch Gurka 33049 and 33050.
    /// </summary>
    [ScriptFilterCreatureId(33049u, 33050u)]
    public class DarkwitchGurkaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public DarkwitchGurkaEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatDarkwitchGurka)
        {
        }
    }
}
