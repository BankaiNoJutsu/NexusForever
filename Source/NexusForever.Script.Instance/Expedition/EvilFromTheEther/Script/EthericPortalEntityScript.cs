using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Spell;
using NexusForever.Script.Instance.Expedition.EvilFromTheEther;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Event;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    /// <summary>
    /// Current implementation queues summons of tethered creature 71133, tracks
    /// active summons through <c>portalCount</c>, self-destructs the portal when the
    /// last summon unsummons, and credits the mapped build 16042 portal objective
    /// on portal death; cadence and native mechanic parity are still pending smoke.
    /// </summary>
    public abstract class EthericPortalEntityScript : INonPlayerScript, IOwnedScript<INonPlayerEntity>
    {
        private enum Creature
        {
            TetheredCreature = 71133,
        }

        private INonPlayerEntity entity;
        private uint portalCount;
        private bool deathCreditApplied;

        #region Dependency Injection

        private readonly IScriptEventFactory eventFactory;
        private readonly IScriptEventManager eventManager;
        private readonly ICreatureInfoManager creatureInfoManager;

        public EthericPortalEntityScript(
            IScriptEventFactory eventFactory,
            IScriptEventManager eventManager,
            ICreatureInfoManager creatureInfoManager)
        {
            this.eventFactory        = eventFactory;
            this.eventManager        = eventManager;
            this.creatureInfoManager = creatureInfoManager;
        }

        #endregion

        protected abstract PublicEventObjective Objective { get; }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(INonPlayerEntity owner)
        {
            entity = owner;
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            eventManager.Update(lastTick);
        }

        protected void CreateTetheredOrganism(TimeSpan time, float angle)
        {
            ICreatureInfo creatureInfo = creatureInfoManager.GetCreatureInfo(Creature.TetheredCreature);
            if (creatureInfo == null)
                return;

            float entityAngle = -entity.Rotation.X - MathF.PI / 2;
            Vector3 position = GetPoint2D(entity.Position, entityAngle + angle, 5f);

            var @event = eventFactory.CreateEvent<IEntitySummonEvent>();
            @event.Initialise(entity, creatureInfo, position, entity.Rotation);
            eventManager.EnqueueEvent(time, @event);
        }

        /// <summary>
        /// Invoked when <see cref="IWorldEntity"/> summons another <see cref="IWorldEntity"/>.
        /// </summary>
        public void OnSummon(IWorldEntity summoned)
        {
            portalCount++;
        }

        /// <summary>
        /// Invoked when <see cref="IWorldEntity"/> unsummons another <see cref="IWorldEntity"/>.
        /// </summary>
        public void OnUnsummon(IWorldEntity summoned)
        {
            if (portalCount > 0)
                portalCount--;

            if (portalCount == 0 && entity.IsAlive)
                entity.ModifyHealth(entity.Health, DamageType.Magic, null);
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to <see cref="IBaseMap"/>.
        /// </summary>
        public abstract void OnAddToMap(IBaseMap map);

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is removed from <see cref="IBaseMap"/>.
        /// </summary>
        public void OnRemoveFromMap(IBaseMap map)
        {
            eventManager.CancelEvents();
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> is killed.
        /// </summary>
        public void OnDeath()
        {
            if (deathCreditApplied)
                return;

            deathCreditApplied = true;
            entity.Map.PublicEventManager.UpdateObjective(Objective, 1);
            entity.RemoveFromMap();
        }

        private static Vector3 GetPoint2D(Vector3 origin, float angle, float distance)
        {
            return new Vector3(
                origin.X + MathF.Cos(angle) * distance,
                origin.Y,
                origin.Z + MathF.Sin(angle) * distance);
        }
    }
}
