using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    [ScriptFilterScriptName("BloodPitGladiatorEntityScript")]
    public class BloodPitGladiatorEntityScript : CombatAI
    {
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
            // WIP-guessed from LaughingWS Instances-and-more. The branch ties this named gladiator
            // script to KillTargetGroup 3972 for Blood Pit progress, but its auto-attack list was
            // explicitly marked "get real ones", so this port only records the death credit.
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 3972u, 1);
        }
    }
}
