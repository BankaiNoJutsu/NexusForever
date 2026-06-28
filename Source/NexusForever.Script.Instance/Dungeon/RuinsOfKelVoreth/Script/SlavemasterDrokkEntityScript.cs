using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 446 to TargetGroup 3842,
    /// whose Creature2 members are Slavemaster Drokk 32536 and 32539.
    /// </summary>
    [ScriptFilterScriptName("SlavemasterDrokkEntityScript")]
    [ScriptFilterCreatureId(32536u, 32539u)]
    public class SlavemasterDrokkEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public SlavemasterDrokkEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatSlavemasterDrokk)
        {
        }
    }
}
