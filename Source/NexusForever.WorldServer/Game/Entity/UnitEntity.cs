using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Database.World.Model;
using NexusForever.Shared.GameTable;
using NexusForever.Shared.GameTable.Model;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Reputation.Static;
using NexusForever.WorldServer.Game.Spell;
using NexusForever.WorldServer.Game.Spell.Static;
using NexusForever.WorldServer.Game.Static;

namespace NexusForever.WorldServer.Game.Entity
{
    public abstract class UnitEntity : WorldEntity
    {
        private readonly List<Spell.Spell> pendingSpells = new();

        public float HitRadius { get; protected set; } = 1f;

        protected UnitEntity(EntityType type)
            : base(type)
        {
            InitialiseHitRadius();
        }

        public override void Initialise(EntityModel model)
        {
            base.Initialise(model);
        }

        private void InitialiseHitRadius()
        {
            if (CreatureId == 0u)
                return;

            Creature2Entry creatureEntry = GameTableManager.Instance.Creature2.GetEntry(CreatureId);
            if (creatureEntry == null)
                return;

            Creature2ModelInfoEntry modelInfoEntry = GameTableManager.Instance.Creature2ModelInfo.GetEntry(creatureEntry.Creature2ModelInfoId);
            if (modelInfoEntry != null)
                HitRadius = modelInfoEntry.HitRadius * creatureEntry.ModelScale;
        }

        public override void Update(double lastTick)
        {
            base.Update(lastTick);

            foreach (Spell.Spell spell in pendingSpells.ToArray())
            {
                spell.Update(lastTick);
                if (spell.IsFinished)
                    pendingSpells.Remove(spell);
            }
        }

        /// <summary>
        /// Cast a <see cref="Spell"/> with the supplied spell id and <see cref="SpellParameters"/>.
        /// </summary>
        public void CastSpell(uint spell4Id, SpellParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException();

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
                throw new ArgumentOutOfRangeException();

            CastSpell(spell4Entry.Spell4BaseIdBaseSpell, (byte)spell4Entry.TierIndex, parameters);
        }

        /// <summary>
        /// Cast a <see cref="Spell"/> with the supplied spell base id, tier and <see cref="SpellParameters"/>.
        /// </summary>
        public void CastSpell(uint spell4BaseId, byte tier, SpellParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException();

            SpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4BaseId);
            if (spellBaseInfo == null)
                throw new ArgumentOutOfRangeException();

            SpellInfo spellInfo = spellBaseInfo.GetSpellInfo(tier);
            if (spellInfo == null)
                throw new ArgumentOutOfRangeException();

            parameters.SpellInfo = spellInfo;
            CastSpell(parameters);
        }

        /// <summary>
        /// Cast a <see cref="Spell"/> with the supplied <see cref="SpellParameters"/>.
        /// </summary>
        public void CastSpell(SpellParameters parameters)
        {
            if (!IsAlive)
                return;

            if (parameters == null)
                throw new ArgumentNullException();

            if (DisableManager.Instance.IsDisabled(DisableType.BaseSpell, parameters.SpellInfo.BaseInfo.Entry.Id))
            {
                if (this is Player player)
                    player.SendSystemMessage($"Unable to cast base spell {parameters.SpellInfo.BaseInfo.Entry.Id} because it is disabled.");
                return;
            }

            if (DisableManager.Instance.IsDisabled(DisableType.Spell, parameters.SpellInfo.Entry.Id))
            {
                if (this is Player player)
                    player.SendSystemMessage($"Unable to cast spell {parameters.SpellInfo.Entry.Id} because it is disabled.");
                return;
            }

            if (parameters.UserInitiatedSpellCast)
            {
                if (this is Player player)
                    player.Dismount();
            }

            var spell = new Spell.Spell(this, parameters);
            spell.Cast();
            pendingSpells.Add(spell);
        }

        /// <summary>
        /// Cancel any <see cref="Spell"/>'s that are interrupted by movement.
        /// </summary>
        public void CancelSpellsOnMove()
        {
            foreach (Spell.Spell spell in pendingSpells)
                if (spell.IsMovingInterrupted() && spell.IsCasting)
                    spell.CancelCast(CastResult.CasterMovement);
        }

        /// <summary>
        /// Cancel a <see cref="Spell"/> based on its casting id
        /// </summary>
        /// <param name="castingId">Casting ID of the spell to cancel</param>
        public void CancelSpellCast(uint castingId)
        {
            Spell.Spell spell = pendingSpells.SingleOrDefault(s => s.CastingId == castingId);
            spell?.CancelCast(CastResult.SpellCancelled);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CanAttack(UnitEntity target)
        {
            if (!IsAlive)
                return false;

            if (!target.IsValidAttackTarget() || !IsValidAttackTarget())
                return false;

            // TODO: Disable when PvP is available.
            if (target is Player && this is Player)
                return false;

            return GetDispositionTo(target.Faction1) < Disposition.Friendly;
        }

        /// <summary>
        /// Returns whether or not this <see cref="UnitEntity"/> is an attackable target.
        /// </summary>
        private bool IsValidAttackTarget()
        {
            // TODO: Expand on this. There's bound to be flags or states that should prevent an entity from being attacked.
            return (this is Player || this is NonPlayer);
        }

        /// <summary>
        /// Deal damage to this <see cref="UnitEntity"/>
        /// </summary>
        public void TakeDamage(UnitEntity attacker, SpellTargetInfo.SpellTargetEffectInfo.DamageDescription damageDescription)
        {
            if (!IsAlive || !attacker.IsAlive)
                return;

            // TODO: Add Threat

            Shield -= damageDescription.ShieldAbsorbAmount;
            ModifyHealth(-damageDescription.AdjustedDamage);

            if (Health == 0u && attacker != null)
                Kill(attacker);
        }

        private void Kill(UnitEntity attacker)
        {
            if (Health > 0)
                throw new InvalidOperationException("Trying to kill entity that has more than 0hp");

            if (DeathState is DeathState.JustSpawned or DeathState.Alive)
                throw new InvalidOperationException($"DeathState is incorrect! Current DeathState is {DeathState}");

            // Fire Events (OnKill, OnDeath)
            OnDeath(attacker);

            // Reward XP
            // Reward Loot
            // Handle Achievements
            // Schedule Respawn
        }

        protected override void OnDeathStateChange(DeathState newState)
        {
            switch (newState)
            {
                case DeathState.JustDied:
                    // Clear Threat

                    foreach (Spell.Spell spell in pendingSpells)
                    {
                        if (spell.IsCasting)
                            spell.CancelCast(CastResult.CasterCannotBeDead);
                    }
                    break;
                default:
                    break;
            }

            base.OnDeathStateChange(newState);
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

            base.OnDeath(killer);
        }
    }
}
