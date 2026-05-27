using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    /// <summary>
    /// WIP-guessed from LaughingWS worlddb SQL: the branch only maps this boss script
    /// to the Skullcano objective. Combat mechanics remain blocked.
    /// </summary>
    [ScriptFilterScriptName("ThunderfootNormalEntityScript")]
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
