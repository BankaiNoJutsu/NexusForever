using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template.Event;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    [ScriptFilterScriptName("KatjaZarkhovEntityScript")]
    public class KatjaZarkhovEntityScript : CombatAI
    {
        private enum Spell
        {
            ClawedFury     = 56037,
            SlicingWind    = 56358,
            PouncingSlice  = 56390,
            TurnedRavenous = 81706,
            RavenousBurst  = 82855
        }

        private enum Creature
        {
            EtherDriveSchematics = 71821,
        }

        private bool hasEnraged;

        #region Dependency Injection

        private readonly IScriptEventFactory eventFactory;
        private readonly IScriptEventManager eventManager;
        private readonly ICreatureInfoManager creatureInfoManager;

        public KatjaZarkhovEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager,
            IScriptEventFactory eventFactory,
            IScriptEventManager eventManager,
            ICreatureInfoManager creatureInfoManager)
            : base(spellParametersFactory, gameTableManager)
        {
            this.eventFactory        = eventFactory;
            this.creatureInfoManager = creatureInfoManager;

            this.eventManager        = eventManager;
            this.eventManager.OnScriptEvent += OnScriptEvent;
        }

        #endregion

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public override void OnLoad(ICreatureEntity owner)
        {
            base.OnLoad(owner);
            autoAttacks = [2605, 2607];
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
                case Spell.ClawedFury:
                case Spell.SlicingWind:
                case Spell.PouncingSlice:
                    eventManager.EnqueueEvent(TimeSpan.FromSeconds(20), @event);
                    break;
            }
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> enters combat.
        /// </summary>
        public override void OnEnterCombat()
        {
            IEntityCastEvent clawedFuryEvent = eventFactory.CreateEvent<IEntityCastEvent>();
            clawedFuryEvent.Initialise(entity, Spell.ClawedFury, false);
            eventManager.EnqueueEvent(TimeSpan.FromSeconds(8), clawedFuryEvent);
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> leaves combat.
        /// </summary>
        public override void OnLeaveCombat()
        {
            eventManager.CancelEvents();
            hasEnraged = false;
        }

        /// <summary>
        /// Invoked when health is changed by source <see cref="IUnitEntity"/>.
        /// </summary>
        public override void OnHealthChange(IUnitEntity source, uint amount, DamageType? type)
        {
            base.OnHealthChange(source, amount, type);

            if (((float)entity.Health) / entity.MaxHealth < 0.55f && !hasEnraged)
                Enrage();
        }

        private void Enrage()
        {
            eventManager.CancelEvents<IEntityCastEvent>();

            IEntityCastEvent ravenousBurstEvent = eventFactory.CreateEvent<IEntityCastEvent>();
            ravenousBurstEvent.Initialise(entity, Spell.RavenousBurst, false);
            eventManager.EnqueueEvent(TimeSpan.Zero, ravenousBurstEvent);

            IEntityCastEvent turnedRavenousEvent = eventFactory.CreateEvent<IEntityCastEvent>();
            turnedRavenousEvent.Initialise(entity, Spell.TurnedRavenous, false);
            eventManager.EnqueueEvent(TimeSpan.FromSeconds(7), turnedRavenousEvent);

            IEntityCastEvent pouncingSliceEvent = eventFactory.CreateEvent<IEntityCastEvent>();
            pouncingSliceEvent.Initialise(entity, Spell.PouncingSlice, true);
            eventManager.EnqueueEvent(TimeSpan.FromSeconds(10), pouncingSliceEvent);

            IEntityCastEvent slicingWindEvent = eventFactory.CreateEvent<IEntityCastEvent>();
            slicingWindEvent.Initialise(entity, Spell.SlicingWind, false);
            eventManager.EnqueueEvent(TimeSpan.FromSeconds(20), slicingWindEvent);

            hasEnraged = true;
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> is killed.
        /// </summary>
        public override void OnDeath()
        {
            ICreatureInfo creatureInfo = creatureInfoManager.GetCreatureInfo(Creature.EtherDriveSchematics);
            if (creatureInfo == null)
                return;

            entity.SummonFactory?.Summon<INonPlayerEntity>(creatureInfo, entity.Position, entity.Rotation);
        }
    }
}
