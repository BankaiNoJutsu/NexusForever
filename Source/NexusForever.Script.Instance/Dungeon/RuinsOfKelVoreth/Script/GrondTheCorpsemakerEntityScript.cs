using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 444 to TargetGroup 3841,
    /// whose Creature2 members are Grond the Corpsemaker 32534 and 32535.
    /// </summary>
    [ScriptFilterScriptName("GrondTheCorpsemakerEntityScript")]
    [ScriptFilterCreatureId(32534u, 32535u)]
    public class GrondTheCorpsemakerEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public GrondTheCorpsemakerEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatGrondTheCorpsemaker)
        {
        }
    }
}
