using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.UltimateProtogames.Downsizer.Script
{
    /// <summary>
    /// Mapped from build 16042 PublicEventObjective 3197 text
    /// "Defeat $(creature=61420)" for The Downsizer.
    /// </summary>
    [ScriptFilterCreatureId(61420u)]
    public class DownsizerEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public DownsizerEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheDownsizer)
        {
        }
    }
}
