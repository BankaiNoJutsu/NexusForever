using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.ColdbloodCitadel.Script
{
    [ScriptFilterScriptName("HailstoneGatecrasherEntityScript")]
    public class HailstoneGatecrasherEntityScript : CombatAI
    {
        private bool defeated;

        public HailstoneGatecrasherEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager)
        {
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public override void OnLoad(ICreatureEntity owner)
        {
            base.OnLoad(owner);

            // WIP-guessed from LaughingWS Instances-and-more. Exact retail
            // ability cadence and encounter scripting still need smoke proof.
            autoAttacks = [88044, 88045];
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> is killed.
        /// </summary>
        public override void OnDeath()
        {
            if (defeated)
                return;

            defeated = true;

            // Build 16042 objective 5313 is the Hailstone kill row for objectId
            // 14447. Credit the named objective directly so other 14447 rows stay
            // phase-owned while broader boss choreography remains blocked.
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.DefeatHailStoneGatecrasher, 1);
            entity.RemoveFromMap();
        }
    }
}
