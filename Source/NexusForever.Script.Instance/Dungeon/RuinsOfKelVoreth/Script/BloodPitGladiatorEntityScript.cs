using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    [ScriptFilterScriptName("BloodPitGladiatorEntityScript")]
    public class BloodPitGladiatorEntityScript : CombatAI
    {
        private bool defeated;

        public BloodPitGladiatorEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager)
        {
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> is killed.
        /// </summary>
        public override void OnDeath()
        {
            if (defeated)
                return;

            defeated = true;
            // Build 16042 objective 445 owns the Blood Pit gate. Objective 846
            // reuses the same KillTargetGroup/objectId pair, so keep this script
            // scoped to the main phase objective until the timed route is proven.
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.FightYourWayThroughTheBloodPit, 1);
        }
    }
}
