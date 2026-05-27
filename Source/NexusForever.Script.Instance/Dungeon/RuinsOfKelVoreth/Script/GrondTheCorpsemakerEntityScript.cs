using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    /// <summary>
    /// WIP-guessed from LaughingWS worlddb SQL: the branch only maps this boss script
    /// to the Ruins of Kel Voreth objective. Combat mechanics remain blocked.
    /// </summary>
    [ScriptFilterScriptName("GrondTheCorpsemakerEntityScript")]
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
