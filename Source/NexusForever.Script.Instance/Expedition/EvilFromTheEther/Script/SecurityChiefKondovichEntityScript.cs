using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template.Event;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    /// <summary>
    /// Current implementation uses FocusedAssualt 46790 and CrushingRush 46797 from
    /// <see cref="Spell"/>, then credits
    /// <see cref="PublicEventObjectiveType.KillEventObjectiveUnit"/> on death;
    /// encounter parity is still pending smoke.
    /// </summary>
    [ScriptFilterScriptName("SecurityChiefKondovichEntityScript")]
    public class SecurityChiefKondovichEntityScript : CombatAI
    {
        private enum Spell
        {
            FocusedAssualt = 46790,
            CrushingRush   = 46797
        }

        #region Dependency Injection

        private readonly IScriptEventFactory eventFactory;
        private readonly IScriptEventManager eventManager;

        public SecurityChiefKondovichEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager,
            IScriptEventFactory eventFactory,
            IScriptEventManager eventManager)
            : base(spellParametersFactory, gameTableManager)
        {
            this.eventFactory = eventFactory;
            this.eventManager = eventManager;
            this.eventManager.OnScriptEvent += OnScriptEvent;
        }

        #endregion

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public override void OnLoad(ICreatureEntity owner)
        {
            base.OnLoad(owner);
            autoAttacks = [32920, 32921];
        }

        protected override void UpdateAI(double lastTick)
        {
            base.UpdateAI(lastTick);
            eventManager.Update(lastTick);
        }

        private void OnScriptEvent(IScriptEvent scriptEvent, uint? _)
        {
            if (scriptEvent is IEntityCastEvent castEvent)
                OnEntityCastEvent(castEvent);
        }

        private void OnEntityCastEvent(IEntityCastEvent @event)
        {
            switch ((Spell)@event.SpellId)
            {
                case Spell.FocusedAssualt:
                    eventManager.EnqueueEvent(TimeSpan.FromSeconds(60), @event);
                    break;
                case Spell.CrushingRush:
                    eventManager.EnqueueEvent(TimeSpan.FromSeconds(20), @event);
                    break;
            }
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> enters combat.
        /// </summary>
        public override void OnEnterCombat()
        {
            IEntityCastEvent focusedAssualtEvent = eventFactory.CreateEvent<IEntityCastEvent>();
            focusedAssualtEvent.Initialise(entity, Spell.FocusedAssualt, false);
            eventManager.EnqueueEvent(TimeSpan.FromSeconds(4), focusedAssualtEvent);

            IEntityCastEvent crushingRushEvent = eventFactory.CreateEvent<IEntityCastEvent>();
            crushingRushEvent.Initialise(entity, Spell.CrushingRush, false);
            eventManager.EnqueueEvent(TimeSpan.FromSeconds(20), crushingRushEvent);
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> leaves combat.
        /// </summary>
        public override void OnLeaveCombat()
        {
            eventManager.CancelEvents();
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> is killed.
        /// </summary>
        public override void OnDeath()
        {
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.KillEventObjectiveUnit, 0, 1);
        }
    }
}
