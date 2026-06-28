using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 447 to TargetGroup 3909,
    /// whose Creature2 members are Voreth Battlesworn 32555/32556 and
    /// Voreth Darkwitch 32618/32619.
    /// </summary>
    [ScriptFilterCreatureId(32555u, 32556u, 32618u, 32619u)]
    public class VorethBattleswornDarkwitchEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public VorethBattleswornDarkwitchEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillBattleswornAndDarkwitchOsun)
        {
        }
    }
}
