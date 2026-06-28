using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 322 to TargetGroup 2600,
    /// whose Creature2 members are Thunderfoot 24475 and 24893.
    /// </summary>
    [ScriptFilterCreatureId(24475u, 24893u)]
    public class ThunderfootNormalEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ThunderfootNormalEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatThunderfoot)
        {
        }
    }
}
