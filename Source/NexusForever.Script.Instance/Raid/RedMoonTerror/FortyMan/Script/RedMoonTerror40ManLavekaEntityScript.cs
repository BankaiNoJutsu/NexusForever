using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.RedMoonTerror.FortyMan.Script
{
    /// <summary>
    /// Build 16042 public event 650 maps objective 525 to "Defeat Laveka the
    /// Dark-Hearted" for world 3102. This grants objective credit only; 40-man
    /// Laveka combat and hidden turnstile route semantics remain blocked.
    /// </summary>
    [ScriptFilterScriptName("RedMoonTerror40ManLavekaEntityScript")]
    public class RedMoonTerror40ManLavekaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public RedMoonTerror40ManLavekaEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatLavekaTheDarkHearted)
        {
        }
    }
}
