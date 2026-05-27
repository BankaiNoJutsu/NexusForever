using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
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
            // WIP-guessed from the branch entity_script row for Coldblood
            // Citadel; pins the mapped objective credit while broader boss
            // choreography stays blocked.
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.KillEventObjectiveUnit, 14447, 1);
            entity.RemoveFromMap();
        }
    }
}
