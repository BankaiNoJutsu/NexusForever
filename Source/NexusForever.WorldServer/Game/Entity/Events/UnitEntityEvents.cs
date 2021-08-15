using System.Collections.Generic;
using System.Numerics;
using System.Text;
using NexusForever.WorldServer.Game.Combat;
using NexusForever.WorldServer.Game.Entity.Movement;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Map;

namespace NexusForever.WorldServer.Game.Entity
{
    public abstract partial class UnitEntity : WorldEntity
    {
        /// <summary>
        /// Fires every time a regeneration tick occurs (every 0.5s)
        /// </summary>
        protected virtual void OnTickRegeneration()
        {
            if (!IsAlive || InCombat)
                return;
            // TODO: This should probably get moved to a Calculation Library/Manager at some point. There will be different timers on Stat refreshes, but right now the timer is hardcoded to every 0.25s.
            // Probably worth considering an Attribute-grouped Class that allows us to run differentt regeneration methods & calculations for each stat.

            if (Health < MaxHealth)
                ModifyHealth((uint)(MaxHealth / 200f));

            if (Shield < MaxShieldCapacity)
                Shield += (uint)(MaxShieldCapacity * GetPropertyValue(Property.ShieldRegenPct) * regenTimer.Duration);
        }

        public override void OnRemoveFromMap()
        {
            // TODO: Delay OnRemoveFromMap from firing immediately on DC. Allow players to die between getting disconnected and being removed from map :D
            ThreatManager.ClearThreatList();

            base.OnRemoveFromMap();
        }

        /// <summary>
        /// Invoked when <see cref="ThreatManager"/> adds a <see cref="HostileEntity"/>.
        /// </summary>
        public virtual void OnThreatAddTarget(HostileEntity hostile)
        {
            if (currentTargetUnitId == 0u)
                SetTarget(hostile.HatedUnitId, hostile.Threat);
        }

        /// <summary>
        /// Invoked when <see cref="ThreatManager"/> removes a <see cref="HostileEntity"/>.
        /// </summary>
        public virtual void OnThreatRemoveTarget(HostileEntity hostile)
        {
            SelectTarget();
        }

        /// <summary>
        /// Invoked when <see cref="ThreatManager"/> updates a <see cref="HostileEntity"/>.
        /// </summary>
        /// <param name="hostiles"></param>
        public virtual void OnThreatChange(IEnumerable<HostileEntity> hostiles)
        {
            CheckCombatStateChange(hostiles);
            SelectTarget();
        }

        /// <summary>
        /// Invoked when this <see cref="WorldEntity"/> combat state is changed.
        /// </summary>
        public virtual void OnCombatStateChange(bool inCombat)
        {
            Sheathed = !inCombat;

            switch (inCombat)
            {
                case true:
                    LeashPosition = Position;
                    LeashRotation = Rotation;
                    StandState = Static.StandState.Stand;
                    AI?.OnEnterCombat();
                    break;
                case false:
                    StandState = Static.StandState.State0;
                    AI?.OnExitCombat();
                    break;
            }
        }

        public override void OnRelocate(Vector3 vector)
        {
            base.OnRelocate(vector);

            foreach (GridEntity entity in visibleEntities.Values)
                CheckEntityRange(entity);
        }

        protected override void OnDeath(UnitEntity killer)
        {
            if (killer is Player player && this is not Player)
            {
                player.QuestManager.ObjectiveUpdate(Quest.Static.QuestObjectiveType.KillCreature, CreatureId, 1u);
                player.QuestManager.ObjectiveUpdate(Quest.Static.QuestObjectiveType.KillCreature2, CreatureId, 1u);
                player.QuestManager.ObjectiveUpdate(Quest.Static.QuestObjectiveType.KillTargetGroup, CreatureId, 1u);
                player.QuestManager.ObjectiveUpdate(Quest.Static.QuestObjectiveType.KillTargetGroups, CreatureId, 1u);
            }
            
            AI?.OnDeath(killer);

            base.OnDeath(killer);
        }
    }
}
