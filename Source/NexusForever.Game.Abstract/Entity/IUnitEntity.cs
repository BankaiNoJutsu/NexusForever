using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Abstract.Entity
{
    /// <summary>
    /// An <see cref="IUnitEntity"/> is an extension to <see cref="IWorldEntity"/> which can cast spells, be targed by spells and participate in combat.
    /// </summary>
    public interface IUnitEntity : IWorldEntity
    {
        float HitRadius { get; }

        /// <summary>
        /// Guid of the <see cref="IWorldEntity"/> currently targeted.
        /// </summary>
        uint? TargetGuid { get; }

        /// <summary>
        /// Determines whether or not this <see cref="IUnitEntity"/> is alive.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Determines whether or not this <see cref="IUnitEntity"/> is in combat.
        /// </summary>
        bool InCombat { get; }

        public IThreatManager ThreatManager { get; }

        /// <summary>
        /// Bit mask of currently tracked crowd control states affecting this <see cref="IUnitEntity"/>.
        /// </summary>
        uint ActiveCCStateMask { get; }

        bool IsStealthed { get; }
        bool IsAggroImmune { get; }
        bool IsBusy { get; }
        bool IsShieldOverloaded { get; }
        uint CurrentAbsorption { get; }
        uint CurrentHealingAbsorption { get; }

        bool HasUnitState(uint stateId);
        void AddUnitState(uint effectId, uint spell4Id, uint castingId, uint stateId, uint dataBits01, uint dataBits02, uint dataBits03, uint dataBits04, uint dataBits05, uint dataBits06, uint dataBits07, uint dataBits08, uint dataBits09);
        bool RemoveUnitState(uint effectId);

        void AddBusy(uint effectId, uint spell4Id, uint castingId, uint mode, uint contextId, uint dataBits02, uint dataBits03, uint dataBits04, uint dataBits05, uint dataBits06, uint dataBits07, uint dataBits08, uint dataBits09);
        bool RemoveBusy(uint effectId);
        IReadOnlyCollection<uint> ClearBusy(uint spell4Id, uint contextId);

        bool IsImmuneToSpellEffect(SpellEffectType effectType);
        void AddSpellEffectImmunity(uint effectId, uint spell4Id, uint castingId, SpellEffectType effectType);
        bool RemoveSpellEffectImmunity(uint effectId);

        bool IsImmuneToSpell(uint spell4Id);
        void AddSpellImmunity(uint effectId, uint spell4Id, uint castingId, uint immuneSpell4Id, uint mode);
        bool RemoveSpellImmunity(uint effectId);

        void AddDelayDeath(uint effectId, uint spell4Id, uint castingId, uint mode, uint triggerSpell4Id, uint triggerDelayMs, uint dataBits03, uint dataBits04, uint dataBits05, uint dataBits06, uint dataBits07);
        bool RemoveDelayDeath(uint effectId);

        void AddProc(uint effectId, uint spell4Id, uint castingId, uint triggerEvent, uint triggerSpell4Id, float chance, uint targetData, uint cooldownMsOrSentinel, uint dataBits05, uint dataBits06, uint dataBits07, uint dataBits08, uint dataBits09);
        bool RemoveProc(uint effectId);

        /// <summary>
        /// Emit diagnostic-only evidence for active proc states against an observed runtime event.
        /// </summary>
        void ProbeProcEvent(string eventName, uint? triggerEvent, IUnitEntity source, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo effectInfo, IDamageDescription damageDescription, string phase);

        void AddVitalClamp(uint effectId, uint spell4Id, uint castingId, Vital vital, float ratio, uint mode, uint vitalMode);
        bool RemoveVitalClamp(uint effectId);

        void AddShieldOverload(uint effectId, uint spell4Id, uint castingId);
        bool RemoveShieldOverload(uint effectId);

        void AddScale(uint effectId, uint spell4Id, uint castingId, float previousScale, uint restoreTimeMs);
        bool RemoveScale(uint effectId);

        void AddFaction(uint effectId, uint spell4Id, uint castingId, Faction previousFaction);
        bool RemoveFaction(uint effectId);

        void AddDisguiseOutfit(uint effectId, uint spell4Id, uint castingId, ushort previousOutfitInfo, IReadOnlyDictionary<ItemSlot, IItemVisual> previousVisuals);
        bool RemoveDisguiseOutfit(uint effectId);

        void AddMimicDisguise(uint effectId, uint spell4Id, uint castingId, uint previousDisplayInfo, ushort previousOutfitInfo, IReadOnlyDictionary<ItemSlot, IItemVisual> previousVisuals);
        bool RemoveMimicDisguise(uint effectId);

        void AddStealth(uint effectId, uint spell4Id, uint castingId);
        bool RemoveStealth(uint effectId);
        bool RemoveStealth();

        void AddAggroImmune(uint effectId, uint spell4Id, uint castingId);
        bool RemoveAggroImmune(uint effectId);

        void AddAbsorption(uint effectId, uint spell4Id, uint castingId, uint amount);
        uint RemoveAbsorption(uint effectId);
        uint ConsumeAbsorption(uint amount, DamageType damageType);

        void AddHealingAbsorption(uint effectId, uint spell4Id, uint castingId, uint amount);
        uint RemoveHealingAbsorption(uint effectId);
        uint ConsumeHealingAbsorption(uint amount);

        bool TryGetVitalMax(Vital vital, out float maxValue);
        bool TryModifyVital(Vital vital, float amount, out float appliedAmount);

        /// <summary>
        /// Track an active crowd control state applied by a spell effect.
        /// </summary>
        void AddCCState(CCState state, uint effectId, uint spell4Id, uint castingId);

        /// <summary>
        /// Stop tracking an active crowd control state applied by a spell effect.
        /// </summary>
        bool RemoveCCState(CCState state, uint effectId);

        /// <summary>
        /// Stop tracking active crowd control states that match the supplied bit mask.
        /// </summary>
        IReadOnlyCollection<(CCState State, uint EffectId)> RemoveCCStates(uint stateMask);

        /// <summary>
        /// Stop tracking active crowd control states applied by a concrete spell.
        /// </summary>
        IReadOnlyCollection<(CCState State, uint EffectId)> RemoveCCStatesBySpell(uint spell4Id);

        /// <summary>
        /// Stop tracking active spell-driven state that matches the supplied Spell4 predicate.
        /// </summary>
        IReadOnlyCollection<SpellStateRemoval> RemoveTrackedSpellStates(System.Func<uint, bool> spell4Predicate, uint maxCount);

        /// <summary>
        /// Add a <see cref="Property"/> modifier given a Spell4Id and <see cref="ISpellPropertyModifier"/> instance.
        /// </summary>
        void AddSpellModifierProperty(ISpellPropertyModifier modifier, uint spell4Id, uint castingId);

        /// <summary>
        /// Remove a <see cref="Property"/> modifier by a Spell that is currently affecting this <see cref="IUnitEntity"/>.
        /// </summary>
        void RemoveSpellProperty(Property property, uint spell4Id);

        /// <summary>
        /// Remove all <see cref="Property"/> modifiers by a Spell that is currently affecting this <see cref="IUnitEntity"/>
        /// </summary>
        bool RemoveSpellProperties(uint spell4Id);

        /// <summary>
        /// Cast a <see cref="ISpell"/> with the supplied spell id and <see cref="ISpellParameters"/>.
        /// </summary>
        void CastSpell(uint spell4Id, ISpellParameters parameters);

        /// <summary>
        /// Cast a <see cref="ISpell"/> with the supplied spell base id, tier and <see cref="ISpellParameters"/>.
        /// </summary>
        void CastSpell(uint spell4BaseId, byte tier, ISpellParameters parameters);

        /// <summary>
        /// Cast a <see cref="ISpell"/> with the supplied <see cref="ISpellParameters"/>.
        /// </summary>
        void CastSpell(ISpellParameters parameters);

        /// <summary>
        /// Cancel any <see cref="ISpell"/>'s that are interrupted by movement.
        /// </summary>
        void CancelSpellsOnMove();

        /// <summary>
        /// Cancel an <see cref="ISpell"/> based on its casting id.
        /// </summary>
        /// <param name="castingId">Casting ID of the spell to cancel</param>
        void CancelSpellCast(uint castingId);

        /// <summary>
        /// Determine if this <see cref="IUnitEntity"/> can attack supplied <see cref="IUnitEntity"/>.
        /// </summary>
        bool CanAttack(IUnitEntity target);

        /// <summary>
        /// Returns whether or not this <see cref="IUnitEntity"/> is an attackable target.
        /// </summary>
        bool IsValidAttackTarget();

        /// <summary>
        /// Deal damage to this <see cref="IUnitEntity"/> from the supplied <see cref="IUnitEntity"/>.
        /// </summary>
        void TakeDamage(IUnitEntity attacker, IDamageDescription damageDescription);

        /// <summary>
        /// Modify the health of this <see cref="IUnitEntity"/> by the supplied amount.
        /// </summary>
        /// <remarks>
        /// If the <see cref="DamageType"/> is <see cref="DamageType.Heal"/> amount is added to current health otherwise subtracted.
        /// </remarks>
        void ModifyHealth(uint amount, DamageType type, IUnitEntity source);

        /// <summary>
        /// Set target to supplied target guid.
        /// </summary>
        /// <remarks>
        /// A null target will clear the current target.
        /// </remarks>
        void SetTarget(uint? target, uint threat = 0u);

        /// <summary>
        /// Set target to supplied <see cref="IUnitEntity"/>.
        /// </summary>
        /// <remarks>
        /// A null target will clear the current target.
        /// </remarks>
        void SetTarget(IWorldEntity target, uint threat = 0u);

        /// <summary>
        /// Invoked when a new <see cref="IHostileEntity"/> is added to the threat list.
        /// </summary>
        void OnThreatAddTarget(IHostileEntity hostile);

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is removed from the threat list.
        /// </summary>
        void OnThreatRemoveTarget(IHostileEntity hostile);

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is update on the threat list.
        /// </summary>
        void OnThreatChange(IHostileEntity hostile);
    }
}
