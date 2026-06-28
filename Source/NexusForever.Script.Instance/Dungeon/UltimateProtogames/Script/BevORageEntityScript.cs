using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.UltimateProtogames.Script
{
    /// <summary>
    /// Mapped from build 16042 PublicEventObjective 2680 / TargetGroup 12179
    /// plus timed child objectives 3206 and 3210 at WorldLocation2 45901, and
    /// the reviewed phase-5 runtime placement for Creature2 61463.
    /// Exact timer and 20%-damage semantics remain blocked.
    /// </summary>
    [ScriptFilterCreatureId(61463u)]
    public class BevORageEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public BevORageEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(
                  spellParametersFactory,
                  gameTableManager,
                  (uint)PublicEventObjective.DefeatBevORage,
                  (uint)PublicEventObjective.Caffeinated,
                  (uint)PublicEventObjective.OutOfOrder)
        {
        }
    }

    /// <summary>
    /// Mapped from build 16042 PublicEventObjective 4669 / TargetGroup 12549,
    /// whose Creature2 member is the Bev-O-Rage Vend-A-Tron 66051.
    /// </summary>
    [ScriptFilterCreatureId(66051u)]
    public class BevORageVendATronEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint BevORageVendATronTargetGroupId = 12549u;

        private IWorldEntity entity;
        private bool activated;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        /// <summary>
        /// Invoked when this entity activation succeeds.
        /// </summary>
        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroup,
                BevORageVendATronTargetGroupId,
                1);
        }
    }
}
