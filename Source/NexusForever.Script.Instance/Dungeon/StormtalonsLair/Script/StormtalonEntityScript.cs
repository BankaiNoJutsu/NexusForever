using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    /// <summary>
    /// Mapped/WIP Stormtalon final-boss objective credit for the normal and level-50
    /// Creature2 rows tied to Stormtalon's Lair public event 145. Boss spawn/version
    /// selection and combat choreography remain blocked pending dungeon smoke.
    /// </summary>
    [ScriptFilterCreatureId(17163u, 33406u)]
    public class StormtalonEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public StormtalonEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DestroyStormtalon)
        {
        }
    }
}
