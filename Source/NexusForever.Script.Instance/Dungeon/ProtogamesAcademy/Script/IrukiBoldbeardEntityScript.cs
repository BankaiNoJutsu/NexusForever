using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.ProtogamesAcademy.Script
{
    /// <summary>
    /// WIP-guessed from LaughingWS worlddb SQL: the branch only maps this boss script
    /// to the Protogames Academy objective. Combat mechanics remain blocked.
    /// </summary>
    [ScriptFilterScriptName("IrukiBoldbeardEntityScript")]
    public class IrukiBoldbeardEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public IrukiBoldbeardEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatIrukiBoldbeard)
        {
        }
    }
}
