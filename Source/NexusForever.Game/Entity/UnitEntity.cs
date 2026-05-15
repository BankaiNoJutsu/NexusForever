using System.Numerics;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Combat;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Spell;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Combat;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script.Template;
using NexusForever.Shared.Configuration;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Entity
{
    public abstract class UnitEntity : WorldEntity, IUnitEntity
    {
        public float HitRadius { get; protected set; } = 1f;

        /// <summary>
        /// Guid of the <see cref="IUnitEntity"/> currently targeted.
        /// </summary>
        public uint? TargetGuid { get; private set; }

        /// <summary>
        /// Determines whether or not this <see cref="IUnitEntity"/> is alive.
        /// </summary>
        public bool IsAlive => Health > 0u && deathState == null;

        protected EntityDeathState? DeathState
        {
            get => deathState;
            set
            {
                deathState = value;

                if (deathState is null or EntityDeathState.JustDied)
                {
                    EnqueueToVisible(new ServerEntityDeathState
                    {
                        UnitId    = Guid,
                        Dead      = !IsAlive,
                        Reason    = 0, // client does nothing with this value
                        RezHealth = IsAlive ? Health : 0u
                    }, true);
                }
            }
        }

        private EntityDeathState? deathState;

        /// <summary>
        /// Determines whether or not this <see cref="IUnitEntity"/> is in combat.
        /// </summary>
        public bool InCombat
        {
            get => inCombat;
            private set
            {
                if (inCombat == value)
                    return;

                inCombat = value;

                EnqueueToVisible(new ServerUnitEnteredCombat
                {
                    UnitId   = Guid,
                    InCombat = value
                }, true);

                if (value)
                    ProbeProcEvent("enter-combat", ProcTriggerEventCandidate.EnterCombat, this, this, null, null, null, "after-state-change");

                if (value)
                    scriptCollection?.Invoke<IUnitScript>(s => s.OnEnterCombat());
                else
                    scriptCollection?.Invoke<IUnitScript>(s => s.OnLeaveCombat());
            }
        }

        private bool inCombat;

        public IThreatManager ThreatManager { get; private set; }

        /// <summary>
        /// Initial stab at a timer to regenerate Health & Shield values.
        /// </summary>
        private UpdateTimer statUpdateTimer = new UpdateTimer(0.25); // TODO: Long-term this should be absorbed into individual timers for each Stat regeneration method

        private UpdateTimer respawnTimer;

        private readonly List<ISpell> pendingSpells = new();

        private readonly Dictionary<CCState, Dictionary</*effectId*/uint, TrackedSpellState>> ccStates = new();
        private readonly Dictionary</*effectId*/uint, TrackedSpellState> stealthStates = new();
        private readonly Dictionary</*effectId*/uint, TrackedSpellState> aggroImmuneStates = new();
        private readonly Dictionary</*effectId*/uint, UnitStateSetState> unitStateSetStates = new();
        private readonly Dictionary</*effectId*/uint, BusyState> busyStates = new();
        private readonly Dictionary</*effectId*/uint, SpellEffectImmunityState> spellEffectImmunityStates = new();
        private readonly Dictionary</*effectId*/uint, SpellImmunityState> spellImmunityStates = new();
        private readonly Dictionary</*effectId*/uint, DelayDeathState> delayDeathStates = new();
        private readonly Dictionary</*effectId*/uint, ProcState> procStates = new();
        private readonly HashSet</*effectId*/uint> activeProcDispatchEffectIds = new();
        private readonly Dictionary</*effectId*/uint, VitalClampState> vitalClampStates = new();
        private readonly Dictionary</*effectId*/uint, TrackedSpellState> shieldOverloadStates = new();
        private readonly Dictionary</*effectId*/uint, ScaleState> scaleStates = new();
        private readonly Dictionary</*effectId*/uint, FactionState> factionStates = new();
        private readonly Dictionary</*effectId*/uint, ItemVisualSwapState> itemVisualSwapStates = new();
        private readonly Dictionary</*effectId*/uint, DisguiseOutfitState> disguiseOutfitStates = new();
        private readonly Dictionary</*effectId*/uint, MimicDisguiseState> mimicDisguiseStates = new();
        private readonly Dictionary</*effectId*/uint, AbsorptionState> absorptionStates = new();
        private readonly Dictionary</*effectId*/uint, AbsorptionState> healingAbsorptionStates = new();

        private Dictionary<Property, Dictionary</*spell4Id*/uint, ISpellPropertyModifier>> spellProperties = new();
        private readonly Dictionary</*spell4Id*/uint, /*castingId*/uint> spellPropertyCastingIds = new();
        private readonly List<PendingDelayDeathTrigger> pendingDelayDeathTriggers = new();

        public uint ActiveCCStateMask => ccStates.Keys.Aggregate(0u, (mask, state) => mask | (1u << (int)state));
        public bool IsStealthed => stealthStates.Count != 0;
        public bool IsAggroImmune => aggroImmuneStates.Count != 0;
        public bool IsBusy => busyStates.Count != 0;
        public bool IsShieldOverloaded => shieldOverloadStates.Count != 0;
        public uint CurrentAbsorption => (uint)Math.Min(uint.MaxValue, absorptionStates.Values.Aggregate(0ul, (total, state) => total + state.Amount));
        public uint CurrentHealingAbsorption => (uint)Math.Min(uint.MaxValue, healingAbsorptionStates.Values.Aggregate(0ul, (total, state) => total + state.Amount));

        private class TrackedSpellState
        {
            public uint Spell4Id { get; init; }
            public uint CastingId { get; init; }
        }

        private sealed class SpellEffectImmunityState : TrackedSpellState
        {
            public SpellEffectType EffectType { get; init; }
        }

        private sealed class SpellImmunityState : TrackedSpellState
        {
            public uint ImmuneSpell4Id { get; init; }
            public uint Mode { get; init; }
        }

        private sealed class DelayDeathState : TrackedSpellState
        {
            public uint Mode { get; init; }
            public uint TriggerSpell4Id { get; init; }
            public uint TriggerDelayMs { get; init; }
            public uint DataBits03 { get; init; }
            public uint DataBits04 { get; init; }
            public uint DataBits05 { get; init; }
            public uint DataBits06 { get; init; }
            public uint DataBits07 { get; init; }
        }

        private sealed class ProcState : TrackedSpellState
        {
            public uint TriggerEvent { get; init; }
            public uint TriggerSpell4Id { get; init; }
            public float Chance { get; init; }
            public uint TargetData { get; init; }
            public uint CooldownMsOrSentinel { get; init; }
            public uint DataBits05 { get; init; }
            public uint DataBits06 { get; init; }
            public uint DataBits07 { get; init; }
            public uint DataBits08 { get; init; }
            public uint DataBits09 { get; init; }
            public double CooldownRemainingSeconds { get; set; }
        }

        private sealed class PendingDelayDeathTrigger
        {
            public UpdateTimer Timer { get; init; }
            public uint Spell4Id { get; init; }
            public uint SourceGuid { get; init; }
        }

        private sealed class VitalClampState : TrackedSpellState
        {
            public Vital Vital { get; init; }
            public float Ratio { get; init; }
            public uint Mode { get; init; }
            public uint VitalMode { get; init; }
        }

        private sealed class UnitStateSetState : TrackedSpellState
        {
            public uint StateId { get; init; }
            public uint DataBits01 { get; init; }
            public uint DataBits02 { get; init; }
            public uint DataBits03 { get; init; }
            public uint DataBits04 { get; init; }
            public uint DataBits05 { get; init; }
            public uint DataBits06 { get; init; }
            public uint DataBits07 { get; init; }
            public uint DataBits08 { get; init; }
            public uint DataBits09 { get; init; }
        }

        private sealed class BusyState : TrackedSpellState
        {
            public uint Mode { get; init; }
            public uint ContextId { get; init; }
            public uint DataBits02 { get; init; }
            public uint DataBits03 { get; init; }
            public uint DataBits04 { get; init; }
            public uint DataBits05 { get; init; }
            public uint DataBits06 { get; init; }
            public uint DataBits07 { get; init; }
            public uint DataBits08 { get; init; }
            public uint DataBits09 { get; init; }
        }

        private sealed class ScaleState : TrackedSpellState
        {
            public float PreviousScale { get; init; }
            public uint RestoreTimeMs { get; init; }
        }

        private sealed class FactionState : TrackedSpellState
        {
            public Faction PreviousFaction { get; init; }
        }

        private sealed class ItemVisualSwapState : TrackedSpellState
        {
            public IReadOnlyDictionary<ItemSlot, IItemVisual> PreviousVisuals { get; init; }
        }

        private sealed class DisguiseOutfitState : TrackedSpellState
        {
            public ushort PreviousOutfitInfo { get; init; }
            public IReadOnlyDictionary<ItemSlot, IItemVisual> PreviousVisuals { get; init; }
        }

        private sealed class MimicDisguiseState : TrackedSpellState
        {
            public uint PreviousDisplayInfo { get; init; }
            public ushort PreviousOutfitInfo { get; init; }
            public IReadOnlyDictionary<ItemSlot, IItemVisual> PreviousVisuals { get; init; }
        }

        private sealed class AbsorptionState
        {
            public uint Spell4Id { get; init; }
            public uint CastingId { get; init; }
            public uint Amount { get; set; }
        }

        #region Dependency Injection

        public UnitEntity(IMovementManager movementManager)
            : base(movementManager)
        {
            ThreatManager = new ThreatManager(this);

            InitialiseHitRadius();
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();

            foreach (ISpell spell in pendingSpells)
                spell.Dispose();
        }

        private void InitialiseHitRadius()
        {
            if (CreatureEntry == null)
                return;

            Creature2ModelInfoEntry modelInfoEntry = GameTableManager.Instance.Creature2ModelInfo.GetEntry(CreatureEntry.Creature2ModelInfoId);
            if (modelInfoEntry != null)
                HitRadius = modelInfoEntry.HitRadius * CreatureEntry.ModelScale;
        }

        public override void Update(double lastTick)
        {
            base.Update(lastTick);

            HandleRespawn(lastTick);
            HandleDelayDeathTriggers(lastTick);
            HandleProcCooldowns(lastTick);

            foreach (ISpell spell in pendingSpells.ToArray())
            {
                spell.Update(lastTick);
                if (spell.IsFinished)
                    pendingSpells.Remove(spell);
            }

            statUpdateTimer.Update(lastTick);
            if (statUpdateTimer.HasElapsed)
            {
                HandleStatUpdate(lastTick);
                statUpdateTimer.Reset();
            }
        }

        private void HandleProcCooldowns(double lastTick)
        {
            if (procStates.Count == 0 || lastTick <= 0d)
                return;

            foreach (ProcState state in procStates.Values)
            {
                if (state.CooldownRemainingSeconds <= 0d)
                    continue;

                state.CooldownRemainingSeconds = Math.Max(0d, state.CooldownRemainingSeconds - lastTick);
            }
        }

        /// <summary>
        /// Remove tracked <see cref="IGridEntity"/> that is no longer in vision range.
        /// </summary>
        public override void RemoveVisible(IGridEntity entity)
        {
            if (entity.Guid == TargetGuid)
                SetTarget((IWorldEntity)null);

            ThreatManager.RemoveHostile(entity.Guid);

            base.RemoveVisible(entity);
        }

        public bool IsImmuneToSpellEffect(SpellEffectType effectType)
        {
            return spellEffectImmunityStates.Values.Any(state => state.EffectType == effectType);
        }

        public void AddSpellEffectImmunity(uint effectId, uint spell4Id, uint castingId, SpellEffectType effectType)
        {
            spellEffectImmunityStates[effectId] = new SpellEffectImmunityState
            {
                Spell4Id  = spell4Id,
                CastingId = castingId,
                EffectType = effectType
            };
        }

        public bool RemoveSpellEffectImmunity(uint effectId)
        {
            return spellEffectImmunityStates.Remove(effectId);
        }

        public bool IsImmuneToSpell(uint spell4Id)
        {
            return spell4Id != 0u && spellImmunityStates.Values.Any(state => state.ImmuneSpell4Id == spell4Id);
        }

        public void AddSpellImmunity(uint effectId, uint spell4Id, uint castingId, uint immuneSpell4Id, uint mode)
        {
            if (immuneSpell4Id == 0u)
                return;

            spellImmunityStates[effectId] = new SpellImmunityState
            {
                Spell4Id       = spell4Id,
                CastingId      = castingId,
                ImmuneSpell4Id = immuneSpell4Id,
                Mode           = mode
            };
        }

        public bool RemoveSpellImmunity(uint effectId)
        {
            return spellImmunityStates.Remove(effectId);
        }

        public void AddDelayDeath(uint effectId, uint spell4Id, uint castingId, uint mode, uint triggerSpell4Id, uint triggerDelayMs, uint dataBits03, uint dataBits04, uint dataBits05, uint dataBits06, uint dataBits07)
        {
            delayDeathStates[effectId] = new DelayDeathState
            {
                Spell4Id        = spell4Id,
                CastingId       = castingId,
                Mode            = mode,
                TriggerSpell4Id = triggerSpell4Id,
                TriggerDelayMs  = triggerDelayMs,
                DataBits03      = dataBits03,
                DataBits04      = dataBits04,
                DataBits05      = dataBits05,
                DataBits06      = dataBits06,
                DataBits07      = dataBits07
            };
        }

        public bool RemoveDelayDeath(uint effectId)
        {
            return delayDeathStates.Remove(effectId);
        }

        public void AddProc(uint effectId, uint spell4Id, uint castingId, uint triggerEvent, uint triggerSpell4Id, float chance, uint targetData, uint cooldownMsOrSentinel, uint dataBits05, uint dataBits06, uint dataBits07, uint dataBits08, uint dataBits09)
        {
            procStates[effectId] = new ProcState
            {
                Spell4Id             = spell4Id,
                CastingId            = castingId,
                TriggerEvent         = triggerEvent,
                TriggerSpell4Id      = triggerSpell4Id,
                Chance               = chance,
                TargetData           = targetData,
                CooldownMsOrSentinel = cooldownMsOrSentinel,
                DataBits05           = dataBits05,
                DataBits06           = dataBits06,
                DataBits07           = dataBits07,
                DataBits08           = dataBits08,
                DataBits09           = dataBits09
            };
        }

        public bool RemoveProc(uint effectId)
        {
            return procStates.Remove(effectId);
        }

        public void ProbeProcEvent(string eventName, uint? triggerEvent, IUnitEntity source, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo effectInfo, IDamageDescription damageDescription, string phase)
        {
            if (procStates.Count == 0)
                return;

            uint triggerSpell4Id = spell?.Parameters.SpellInfo.Entry.Id ?? 0u;
            uint triggerCastingId = spell?.CastingId ?? 0u;
            uint triggerSpell4EffectId = effectInfo?.Entry.Id ?? 0u;

            foreach (KeyValuePair<uint, ProcState> procEntry in procStates.ToArray())
            {
                if (!procStates.TryGetValue(procEntry.Key, out ProcState state) || !ReferenceEquals(state, procEntry.Value))
                    continue;

                SpellEffectDiagnostics.TraceProcProbe(
                    this,
                    eventName,
                    phase,
                    triggerEvent,
                    source?.Guid ?? 0u,
                    target?.Guid ?? 0u,
                    triggerSpell4Id,
                    triggerCastingId,
                    triggerSpell4EffectId,
                    damageDescription,
                    procEntry.Key,
                    state.Spell4Id,
                    state.CastingId,
                    state.TriggerEvent,
                    state.TriggerSpell4Id,
                    state.Chance,
                    state.TargetData,
                    state.CooldownMsOrSentinel,
                    state.DataBits05,
                    state.DataBits06,
                    state.DataBits07,
                    state.DataBits08,
                    state.DataBits09);

                TryDispatchProc(procEntry.Key, state, eventName, phase, triggerEvent, source, target);
            }
        }

        private void TryDispatchProc(uint effectId, ProcState state, string eventName, string phase, uint? observedTriggerEvent, IUnitEntity source, IUnitEntity target)
        {
            if (!observedTriggerEvent.HasValue || observedTriggerEvent.Value != state.TriggerEvent)
                return;

            if (!IsSupportedProcTriggerEvent(state.TriggerEvent))
            {
                SpellEffectDiagnostics.TraceProcDispatch(this, eventName, phase, observedTriggerEvent, source?.Guid ?? 0u, target?.Guid ?? 0u, 0u, effectId, state.Spell4Id, state.CastingId, state.TriggerEvent, state.TriggerSpell4Id, state.Chance, state.TargetData, state.CooldownMsOrSentinel, state.CooldownRemainingSeconds, "none", "unsupported-trigger-event");
                return;
            }

            if (!TryResolveProcTarget(state.TargetData, source, target, out IUnitEntity resolvedTarget, out string skippedReason))
            {
                SpellEffectDiagnostics.TraceProcDispatch(this, eventName, phase, observedTriggerEvent, source?.Guid ?? 0u, target?.Guid ?? 0u, 0u, effectId, state.Spell4Id, state.CastingId, state.TriggerEvent, state.TriggerSpell4Id, state.Chance, state.TargetData, state.CooldownMsOrSentinel, state.CooldownRemainingSeconds, "none", skippedReason);
                return;
            }

            if (state.CooldownRemainingSeconds > 0d)
            {
                SpellEffectDiagnostics.TraceProcDispatch(this, eventName, phase, observedTriggerEvent, source?.Guid ?? 0u, target?.Guid ?? 0u, resolvedTarget.Guid, effectId, state.Spell4Id, state.CastingId, state.TriggerEvent, state.TriggerSpell4Id, state.Chance, state.TargetData, state.CooldownMsOrSentinel, state.CooldownRemainingSeconds, "none", "cooldown-active");
                return;
            }

            if (!activeProcDispatchEffectIds.Add(effectId))
            {
                SpellEffectDiagnostics.TraceProcDispatch(this, eventName, phase, observedTriggerEvent, source?.Guid ?? 0u, target?.Guid ?? 0u, resolvedTarget.Guid, effectId, state.Spell4Id, state.CastingId, state.TriggerEvent, state.TriggerSpell4Id, state.Chance, state.TargetData, state.CooldownMsOrSentinel, state.CooldownRemainingSeconds, "none", "reentrant-proc");
                return;
            }

            try
            {
                if (state.Chance <= 0f || (state.Chance < 1f && Random.Shared.NextSingle() > state.Chance))
                {
                    SpellEffectDiagnostics.TraceProcDispatch(this, eventName, phase, observedTriggerEvent, source?.Guid ?? 0u, target?.Guid ?? 0u, resolvedTarget.Guid, effectId, state.Spell4Id, state.CastingId, state.TriggerEvent, state.TriggerSpell4Id, state.Chance, state.TargetData, state.CooldownMsOrSentinel, state.CooldownRemainingSeconds, "none", "chance-roll-failed");
                    return;
                }

                int pendingSpellCount = pendingSpells.Count;
                CastSpell(state.TriggerSpell4Id, CreateProcSpellParameters(state, resolvedTarget));
                if (pendingSpells.Count <= pendingSpellCount)
                {
                    SpellEffectDiagnostics.TraceProcDispatch(this, eventName, phase, observedTriggerEvent, source?.Guid ?? 0u, target?.Guid ?? 0u, resolvedTarget.Guid, effectId, state.Spell4Id, state.CastingId, state.TriggerEvent, state.TriggerSpell4Id, state.Chance, state.TargetData, state.CooldownMsOrSentinel, state.CooldownRemainingSeconds, "none", "cast-rejected");
                    return;
                }

                StartProcCooldown(state);
                SpellEffectDiagnostics.TraceProcDispatch(this, eventName, phase, observedTriggerEvent, source?.Guid ?? 0u, target?.Guid ?? 0u, resolvedTarget.Guid, effectId, state.Spell4Id, state.CastingId, state.TriggerEvent, state.TriggerSpell4Id, state.Chance, state.TargetData, state.CooldownMsOrSentinel, state.CooldownRemainingSeconds, "cast-trigger-spell", null);
            }
            finally
            {
                activeProcDispatchEffectIds.Remove(effectId);
            }
        }

        private static bool IsSupportedProcTriggerEvent(uint triggerEvent)
        {
            return triggerEvent is ProcTriggerEventCandidate.KillTarget
                or ProcTriggerEventCandidate.EnterCombat
                or ProcTriggerEventCandidate.ActionCastAny
                or ProcTriggerEventCandidate.DealDamage
                or ProcTriggerEventCandidate.ReceiveDamage
                or ProcTriggerEventCandidate.HealOther;
        }

        private bool TryResolveProcTarget(uint targetData, IUnitEntity source, IUnitEntity target, out IUnitEntity resolvedTarget, out string skippedReason)
        {
            switch (targetData)
            {
                case 1u:
                case 2u:
                case 9u:
                    resolvedTarget = this;
                    skippedReason = null;
                    return true;
                case 4u:
                case 12u:
                    resolvedTarget = ResolveProcCounterpartTarget(source, target);
                    skippedReason = resolvedTarget == null ? "missing-counterpart-target" : null;
                    return resolvedTarget != null;
                default:
                    resolvedTarget = null;
                    skippedReason = "unsupported-target-data";
                    return false;
            }
        }

        private IUnitEntity ResolveProcCounterpartTarget(IUnitEntity source, IUnitEntity target)
        {
            if (source != null && source.Guid != Guid)
                return source;

            if (target != null && target.Guid != Guid)
                return target;

            return null;
        }

        private void StartProcCooldown(ProcState state)
        {
            if (state.CooldownMsOrSentinel == 0u || state.CooldownMsOrSentinel == uint.MaxValue)
                return;

            state.CooldownRemainingSeconds = state.CooldownMsOrSentinel / 1000d;
        }

        private ISpellParameters CreateProcSpellParameters(ProcState state, IUnitEntity target)
        {
            ISpellInfo procHolderSpellInfo = ResolveSpellInfo(state.Spell4Id);
            return new SpellParameters
            {
                ParentSpellInfo        = procHolderSpellInfo,
                RootSpellInfo          = procHolderSpellInfo,
                PrimaryTargetId        = target?.Guid ?? 0u,
                UserInitiatedSpellCast = false
            };
        }

        private static ISpellInfo ResolveSpellInfo(uint spell4Id)
        {
            if (spell4Id == 0u)
                return null;

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
                return null;

            ISpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4Entry.Spell4BaseIdBaseSpell);
            return spellBaseInfo?.GetSpellInfo((byte)spell4Entry.TierIndex);
        }

        public void AddVitalClamp(uint effectId, uint spell4Id, uint castingId, Vital vital, float ratio, uint mode, uint vitalMode)
        {
            if (!float.IsFinite(ratio) || ratio <= 0f)
                return;

            vitalClampStates[effectId] = new VitalClampState
            {
                Spell4Id  = spell4Id,
                CastingId = castingId,
                Vital     = vital,
                Ratio     = ratio,
                Mode      = mode,
                VitalMode = vitalMode
            };

            ApplyVitalClamps();
        }

        public bool RemoveVitalClamp(uint effectId)
        {
            return vitalClampStates.Remove(effectId);
        }

        public void AddShieldOverload(uint effectId, uint spell4Id, uint castingId)
        {
            shieldOverloadStates[effectId] = new TrackedSpellState
            {
                Spell4Id  = spell4Id,
                CastingId = castingId
            };
        }

        public bool RemoveShieldOverload(uint effectId)
        {
            return shieldOverloadStates.Remove(effectId);
        }

        public bool HasUnitState(uint stateId)
        {
            return stateId != 0u && unitStateSetStates.Values.Any(state => state.StateId == stateId);
        }

        public void AddUnitState(uint effectId, uint spell4Id, uint castingId, uint stateId, uint dataBits01, uint dataBits02, uint dataBits03, uint dataBits04, uint dataBits05, uint dataBits06, uint dataBits07, uint dataBits08, uint dataBits09)
        {
            if (stateId == 0u)
                return;

            unitStateSetStates[effectId] = new UnitStateSetState
            {
                Spell4Id  = spell4Id,
                CastingId = castingId,
                StateId   = stateId,
                DataBits01 = dataBits01,
                DataBits02 = dataBits02,
                DataBits03 = dataBits03,
                DataBits04 = dataBits04,
                DataBits05 = dataBits05,
                DataBits06 = dataBits06,
                DataBits07 = dataBits07,
                DataBits08 = dataBits08,
                DataBits09 = dataBits09
            };
        }

        public bool RemoveUnitState(uint effectId)
        {
            return unitStateSetStates.Remove(effectId);
        }

        public void AddBusy(uint effectId, uint spell4Id, uint castingId, uint mode, uint contextId, uint dataBits02, uint dataBits03, uint dataBits04, uint dataBits05, uint dataBits06, uint dataBits07, uint dataBits08, uint dataBits09)
        {
            bool wasBusy = IsBusy;

            busyStates[effectId] = new BusyState
            {
                Spell4Id  = spell4Id,
                CastingId = castingId,
                Mode      = mode,
                ContextId = contextId,
                DataBits02 = dataBits02,
                DataBits03 = dataBits03,
                DataBits04 = dataBits04,
                DataBits05 = dataBits05,
                DataBits06 = dataBits06,
                DataBits07 = dataBits07,
                DataBits08 = dataBits08,
                DataBits09 = dataBits09
            };

            BroadcastBusyStateIfChanged(wasBusy);
        }

        public bool RemoveBusy(uint effectId)
        {
            bool wasBusy = IsBusy;
            bool removed = busyStates.Remove(effectId);
            if (removed)
                BroadcastBusyStateIfChanged(wasBusy);

            return removed;
        }

        public IReadOnlyCollection<uint> ClearBusy(uint spell4Id, uint contextId)
        {
            bool wasBusy = IsBusy;
            var removedEffectIds = new List<uint>();
            foreach (KeyValuePair<uint, BusyState> state in busyStates.ToArray())
            {
                if (state.Value.Spell4Id != spell4Id)
                    continue;

                if (contextId != 0u && state.Value.ContextId != contextId)
                    continue;

                busyStates.Remove(state.Key);
                removedEffectIds.Add(state.Key);
            }

            BroadcastBusyStateIfChanged(wasBusy);

            return removedEffectIds;
        }

        private void BroadcastBusyStateIfChanged(bool wasBusy)
        {
            if (wasBusy == IsBusy)
                return;

            EnqueueToVisible(new ServerUnitInUse
            {
                UnitId = Guid,
                InUse  = IsBusy
            }, true);
        }

        public void AddScale(uint effectId, uint spell4Id, uint castingId, float previousScale, uint restoreTimeMs)
        {
            scaleStates[effectId] = new ScaleState
            {
                Spell4Id      = spell4Id,
                CastingId     = castingId,
                PreviousScale = previousScale,
                RestoreTimeMs = restoreTimeMs
            };
        }

        public bool RemoveScale(uint effectId)
        {
            if (!scaleStates.Remove(effectId, out ScaleState scaleState))
                return false;

            RestoreScale(scaleState);
            return true;
        }

        private void RestoreScale(ScaleState scaleState)
        {
            float currentScale = MovementManager.GetScale();
            if (scaleState.RestoreTimeMs > 0u && MathF.Abs(currentScale - scaleState.PreviousScale) > 0.0001f)
            {
                MovementManager.SetScaleKeys(
                    new List<uint> { 0u, scaleState.RestoreTimeMs },
                    new List<float> { currentScale, scaleState.PreviousScale });
                return;
            }

            MovementManager.SetScale(scaleState.PreviousScale);
        }

        public void AddFaction(uint effectId, uint spell4Id, uint castingId, Faction previousFaction)
        {
            factionStates[effectId] = new FactionState
            {
                Spell4Id        = spell4Id,
                CastingId       = castingId,
                PreviousFaction = previousFaction
            };
        }

        public bool RemoveFaction(uint effectId)
        {
            if (!factionStates.Remove(effectId, out FactionState factionState))
                return false;

            SetFaction(factionState.PreviousFaction);
            return true;
        }

        public void AddItemVisualSwap(uint effectId, uint spell4Id, uint castingId, IReadOnlyDictionary<ItemSlot, IItemVisual> previousVisuals)
        {
            itemVisualSwapStates[effectId] = new ItemVisualSwapState
            {
                Spell4Id        = spell4Id,
                CastingId       = castingId,
                PreviousVisuals = previousVisuals
            };
        }

        public bool RemoveItemVisualSwap(uint effectId)
        {
            if (!itemVisualSwapStates.Remove(effectId, out ItemVisualSwapState itemVisualSwapState))
                return false;

            RestoreItemVisualSwap(itemVisualSwapState);
            return true;
        }

        private void RestoreItemVisualSwap(ItemVisualSwapState itemVisualSwapState)
        {
            foreach ((ItemSlot slot, IItemVisual previousVisual) in itemVisualSwapState.PreviousVisuals)
            {
                if (previousVisual == null)
                    RemoveVisual(slot);
                else
                    AddVisual(previousVisual);
            }
        }

        public void AddDisguiseOutfit(uint effectId, uint spell4Id, uint castingId, ushort previousOutfitInfo, IReadOnlyDictionary<ItemSlot, IItemVisual> previousVisuals)
        {
            disguiseOutfitStates[effectId] = new DisguiseOutfitState
            {
                Spell4Id           = spell4Id,
                CastingId          = castingId,
                PreviousOutfitInfo = previousOutfitInfo,
                PreviousVisuals    = previousVisuals
            };
        }

        public bool RemoveDisguiseOutfit(uint effectId)
        {
            if (!disguiseOutfitStates.Remove(effectId, out DisguiseOutfitState disguiseOutfitState))
                return false;

            RestoreDisguiseOutfit(disguiseOutfitState);
            return true;
        }

        private void RestoreDisguiseOutfit(DisguiseOutfitState disguiseOutfitState)
        {
            OutfitInfo = disguiseOutfitState.PreviousOutfitInfo;

            foreach ((ItemSlot slot, IItemVisual previousVisual) in disguiseOutfitState.PreviousVisuals)
            {
                if (previousVisual == null)
                    RemoveVisual(slot);
                else
                    AddVisual(previousVisual);
            }
        }

        public void AddMimicDisguise(uint effectId, uint spell4Id, uint castingId, uint previousDisplayInfo, ushort previousOutfitInfo, IReadOnlyDictionary<ItemSlot, IItemVisual> previousVisuals)
        {
            mimicDisguiseStates[effectId] = new MimicDisguiseState
            {
                Spell4Id            = spell4Id,
                CastingId           = castingId,
                PreviousDisplayInfo = previousDisplayInfo,
                PreviousOutfitInfo  = previousOutfitInfo,
                PreviousVisuals     = previousVisuals
            };
        }

        public bool RemoveMimicDisguise(uint effectId)
        {
            if (!mimicDisguiseStates.Remove(effectId, out MimicDisguiseState mimicDisguiseState))
                return false;

            RestoreMimicDisguise(mimicDisguiseState);
            return true;
        }

        private void RestoreMimicDisguise(MimicDisguiseState mimicDisguiseState)
        {
            SetVisualInfo(mimicDisguiseState.PreviousDisplayInfo, mimicDisguiseState.PreviousOutfitInfo);

            foreach ((ItemSlot slot, IItemVisual previousVisual) in mimicDisguiseState.PreviousVisuals)
            {
                if (previousVisual == null)
                    RemoveVisual(slot);
                else
                    AddVisual(previousVisual);
            }
        }

        public void AddStealth(uint effectId, uint spell4Id, uint castingId)
        {
            stealthStates[effectId] = new TrackedSpellState
            {
                Spell4Id  = spell4Id,
                CastingId = castingId
            };
        }

        public bool RemoveStealth(uint effectId)
        {
            return stealthStates.Remove(effectId);
        }

        public bool RemoveStealth()
        {
            if (stealthStates.Count == 0)
                return false;

            stealthStates.Clear();
            return true;
        }

        public void AddAggroImmune(uint effectId, uint spell4Id, uint castingId)
        {
            aggroImmuneStates[effectId] = new TrackedSpellState
            {
                Spell4Id  = spell4Id,
                CastingId = castingId
            };
        }

        public bool RemoveAggroImmune(uint effectId)
        {
            return aggroImmuneStates.Remove(effectId);
        }

        public void AddAbsorption(uint effectId, uint spell4Id, uint castingId, uint amount)
        {
            if (amount == 0u)
                return;

            absorptionStates[effectId] = new AbsorptionState
            {
                Spell4Id  = spell4Id,
                CastingId = castingId,
                Amount    = amount
            };
        }

        public uint RemoveAbsorption(uint effectId)
        {
            if (!absorptionStates.Remove(effectId, out AbsorptionState absorptionState))
                return 0u;

            return absorptionState.Amount;
        }

        public uint ConsumeAbsorption(uint amount, DamageType damageType)
        {
            if (amount == 0u || absorptionStates.Count == 0)
                return 0u;

            uint remaining = amount;
            foreach ((uint effectId, AbsorptionState absorptionState) in absorptionStates.ToArray())
            {
                uint consumed = Math.Min(remaining, absorptionState.Amount);
                absorptionState.Amount -= consumed;
                remaining -= consumed;

                if (absorptionState.Amount == 0u)
                    absorptionStates.Remove(effectId);

                if (remaining == 0u)
                    break;
            }

            return amount - remaining;
        }

        public void AddHealingAbsorption(uint effectId, uint spell4Id, uint castingId, uint amount)
        {
            if (amount == 0u)
                return;

            healingAbsorptionStates[effectId] = new AbsorptionState
            {
                Spell4Id  = spell4Id,
                CastingId = castingId,
                Amount    = amount
            };
        }

        public uint RemoveHealingAbsorption(uint effectId)
        {
            if (!healingAbsorptionStates.Remove(effectId, out AbsorptionState absorptionState))
                return 0u;

            return absorptionState.Amount;
        }

        public uint ConsumeHealingAbsorption(uint amount)
        {
            if (amount == 0u || healingAbsorptionStates.Count == 0)
                return 0u;

            uint remaining = amount;
            foreach ((uint effectId, AbsorptionState absorptionState) in healingAbsorptionStates.ToArray())
            {
                uint consumed = Math.Min(remaining, absorptionState.Amount);
                absorptionState.Amount -= consumed;
                remaining -= consumed;

                if (absorptionState.Amount == 0u)
                    healingAbsorptionStates.Remove(effectId);

                if (remaining == 0u)
                    break;
            }

            return amount - remaining;
        }

        public bool TryGetVitalMax(Vital vital, out float maxValue)
        {
            switch (vital)
            {
                case Vital.Health:
                    maxValue = MaxHealth;
                    return maxValue > 0f;
                case Vital.ShieldCapacity:
                    maxValue = MaxShieldCapacity;
                    return maxValue > 0f;
                case Vital.Focus:
                    return TryGetPositiveProperty(Property.BaseFocusPool, out maxValue);
                case Vital.Resource0:
                    return TryGetPositiveProperty(Property.ResourceMax0, out maxValue);
                case Vital.Resource1:
                    return TryGetPositiveProperty(Property.ResourceMax1, out maxValue);
                case Vital.Resource2:
                    return TryGetPositiveProperty(Property.ResourceMax2, out maxValue);
                case Vital.Resource3:
                    return TryGetPositiveProperty(Property.ResourceMax3, out maxValue);
                case Vital.Resource4:
                    return TryGetPositiveProperty(Property.ResourceMax4, out maxValue);
                case Vital.Resource5:
                    return TryGetPositiveProperty(Property.ResourceMax5, out maxValue);
                case Vital.Resource6:
                    return TryGetPositiveProperty(Property.ResourceMax6, out maxValue);
                case Vital.InterruptArmor:
                    maxValue = GetPropertyValue(Property.InterruptArmorThreshold);
                    if (!float.IsFinite(maxValue) || maxValue <= 0f)
                        maxValue = Math.Max(InterruptArmor, 1u);
                    return true;
                default:
                    maxValue = 0f;
                    return false;
            }
        }

        public bool TryModifyVital(Vital vital, float amount, out float appliedAmount, IUnitEntity source = null, DamageType? damageType = null)
        {
            appliedAmount = 0f;
            if (!float.IsFinite(amount) || MathF.Abs(amount) < 0.0001f)
                return false;

            if (!TryGetVitalValue(vital, out float currentValue))
                return false;

            bool hasMaxValue = TryGetVitalMax(vital, out float maxValue);
            float newValue = hasMaxValue
                ? Math.Clamp(currentValue + amount, 0f, maxValue)
                : Math.Max(currentValue + amount, 0f);

            return TrySetVitalValue(vital, currentValue, newValue, source, damageType, out appliedAmount);
        }

        private bool TryGetPositiveProperty(Property property, out float value)
        {
            value = GetPropertyValue(property);
            return float.IsFinite(value) && value > 0f;
        }

        private bool TryGetVitalValue(Vital vital, out float value)
        {
            switch (vital)
            {
                case Vital.Health:
                    value = Health;
                    return true;
                case Vital.ShieldCapacity:
                    value = Shield;
                    return true;
                case Vital.Focus:
                    value = GetStatFloat(Stat.Focus) ?? 0f;
                    return true;
                case Vital.Resource0:
                    value = GetStatFloat(Stat.Resource0) ?? 0f;
                    return true;
                case Vital.Resource1:
                    value = GetStatFloat(Stat.Resource1) ?? 0f;
                    return true;
                case Vital.Resource2:
                    value = GetStatFloat(Stat.Resource2) ?? 0f;
                    return true;
                case Vital.Resource3:
                    value = GetStatFloat(Stat.Resource3) ?? 0f;
                    return true;
                case Vital.Resource4:
                    value = GetStatFloat(Stat.Resource4) ?? 0f;
                    return true;
                case Vital.Resource5:
                    value = GetStatFloat(Stat.Resource5) ?? 0f;
                    return true;
                case Vital.Resource6:
                    value = GetStatFloat(Stat.Resource6) ?? 0f;
                    return true;
                case Vital.InterruptArmor:
                    value = InterruptArmor;
                    return true;
                default:
                    value = 0f;
                    return false;
            }
        }

        private bool TrySetVitalValue(Vital vital, float oldValue, float newValue, IUnitEntity source, DamageType? damageType, out float appliedAmount)
        {
            appliedAmount = 0f;
            switch (vital)
            {
                case Vital.Health:
                    return TrySetHealthVital(oldValue, newValue, source, damageType, out appliedAmount);
                case Vital.ShieldCapacity:
                    return TrySetUnsignedVital(oldValue, newValue, value => Shield = value, out appliedAmount);
                case Vital.Focus:
                    return TrySetFloatVital(oldValue, newValue, Stat.Focus, out appliedAmount);
                case Vital.Resource0:
                    return TrySetFloatVital(oldValue, newValue, Stat.Resource0, out appliedAmount);
                case Vital.Resource1:
                    return TrySetFloatVital(oldValue, newValue, Stat.Resource1, out appliedAmount);
                case Vital.Resource2:
                    return TrySetFloatVital(oldValue, newValue, Stat.Resource2, out appliedAmount);
                case Vital.Resource3:
                    return TrySetFloatVital(oldValue, newValue, Stat.Resource3, out appliedAmount);
                case Vital.Resource4:
                    return TrySetFloatVital(oldValue, newValue, Stat.Resource4, out appliedAmount);
                case Vital.Resource5:
                    return TrySetFloatVital(oldValue, newValue, Stat.Resource5, out appliedAmount);
                case Vital.Resource6:
                    return TrySetFloatVital(oldValue, newValue, Stat.Resource6, out appliedAmount);
                case Vital.InterruptArmor:
                    return TrySetUnsignedVital(oldValue, newValue, value => InterruptArmor = value, out appliedAmount);
                default:
                    return false;
            }
        }

        private bool TrySetHealthVital(float oldValue, float newValue, IUnitEntity source, DamageType? damageType, out float appliedAmount)
        {
            uint before = Health;
            uint delta = (uint)MathF.Round(MathF.Abs(newValue - oldValue));
            if (delta != 0u)
                ModifyHealth(delta, ResolveVitalDamageType(newValue >= oldValue, damageType), source);

            appliedAmount = (float)Health - before;
            return true;
        }

        private static DamageType ResolveVitalDamageType(bool healing, DamageType? damageType)
        {
            if (healing)
                return DamageType.Heal;

            return damageType switch
            {
                DamageType.Physical => DamageType.Physical,
                DamageType.Tech     => DamageType.Tech,
                DamageType.Magic    => DamageType.Magic,
                DamageType.Fall     => DamageType.Fall,
                DamageType.Suffocate => DamageType.Suffocate,
                _                   => DamageType.Physical
            };
        }

        private static bool TrySetUnsignedVital(float oldValue, float newValue, Action<uint> setter, out float appliedAmount)
        {
            uint roundedNewValue = (uint)MathF.Round(newValue);
            setter(roundedNewValue);
            appliedAmount = roundedNewValue - oldValue;
            return true;
        }

        private bool TrySetFloatVital(float oldValue, float newValue, Stat stat, out float appliedAmount)
        {
            SetStat(stat, newValue);
            appliedAmount = newValue - oldValue;
            return true;
        }

        public void AddCCState(CCState state, uint effectId, uint spell4Id, uint castingId)
        {
            if (!ccStates.TryGetValue(state, out Dictionary<uint, TrackedSpellState> effectIds))
            {
                effectIds = new Dictionary<uint, TrackedSpellState>();
                ccStates.Add(state, effectIds);
            }

            effectIds[effectId] = new TrackedSpellState
            {
                Spell4Id  = spell4Id,
                CastingId = castingId
            };
        }

        public bool RemoveCCState(CCState state, uint effectId)
        {
            if (!ccStates.TryGetValue(state, out Dictionary<uint, TrackedSpellState> effectIds))
                return false;

            if (!effectIds.Remove(effectId))
                return false;

            if (effectIds.Count == 0)
                ccStates.Remove(state);

            return true;
        }

        public IReadOnlyCollection<SpellStateRemoval> RemoveCCStates(uint stateMask)
        {
            var removedStates = new List<SpellStateRemoval>();

            foreach (KeyValuePair<CCState, Dictionary<uint, TrackedSpellState>> state in ccStates.ToArray())
            {
                if ((stateMask & (1u << (int)state.Key)) == 0u)
                    continue;

                foreach (KeyValuePair<uint, TrackedSpellState> effect in state.Value)
                {
                    removedStates.Add(new SpellStateRemoval(
                        SpellStateRemovalKind.CrowdControl,
                        effect.Value.Spell4Id,
                        effect.Value.CastingId,
                        effect.Key,
                        state.Key));
                }

                ccStates.Remove(state.Key);
            }

            return removedStates;
        }

        public IReadOnlyCollection<(CCState State, uint EffectId)> RemoveCCStatesBySpell(uint spell4Id)
        {
            var removedStates = new List<(CCState State, uint EffectId)>();

            foreach (KeyValuePair<CCState, Dictionary<uint, TrackedSpellState>> state in ccStates.ToArray())
            {
                foreach (KeyValuePair<uint, TrackedSpellState> effect in state.Value.ToArray())
                {
                    if (effect.Value.Spell4Id != spell4Id)
                        continue;

                    state.Value.Remove(effect.Key);
                    removedStates.Add((state.Key, effect.Key));
                }

                if (state.Value.Count == 0)
                    ccStates.Remove(state.Key);
            }

            return removedStates;
        }

        public IReadOnlyCollection<SpellStateRemoval> RemoveTrackedSpellStates(System.Func<uint, bool> spell4Predicate, uint maxCount)
        {
            ArgumentNullException.ThrowIfNull(spell4Predicate);

            var removals = new List<SpellStateRemoval>();
            if (maxCount == 0u)
                return removals;

            var removedSpell4Ids = new HashSet<uint>();

            bool CanRemove(uint spell4Id)
            {
                return (removedSpell4Ids.Contains(spell4Id) || !HasReachedLimit()) && spell4Predicate(spell4Id);
            }

            bool HasReachedLimit()
            {
                return maxCount != uint.MaxValue && removedSpell4Ids.Count >= maxCount;
            }

            void AddRemoval(SpellStateRemoval removal)
            {
                removals.Add(removal);
                removedSpell4Ids.Add(removal.Spell4Id);
            }

            foreach (KeyValuePair<Property, Dictionary<uint, ISpellPropertyModifier>> property in spellProperties.ToArray())
            {
                bool propertyChanged = false;
                foreach (uint spell4Id in property.Value.Keys.ToArray())
                {
                    if (!CanRemove(spell4Id))
                        continue;

                    property.Value.Remove(spell4Id);
                    propertyChanged = true;
                    AddRemoval(new SpellStateRemoval(
                        SpellStateRemovalKind.PropertyModifier,
                        spell4Id,
                        spellPropertyCastingIds.GetValueOrDefault(spell4Id)));

                    if (!HasAnySpellProperties(spell4Id))
                        spellPropertyCastingIds.Remove(spell4Id);

                }

                if (property.Value.Count == 0)
                    spellProperties.Remove(property.Key);

                if (propertyChanged)
                    CalculateProperty(property.Key);

            }

            foreach (KeyValuePair<CCState, Dictionary<uint, TrackedSpellState>> state in ccStates.ToArray())
            {
                foreach (KeyValuePair<uint, TrackedSpellState> effect in state.Value.ToArray())
                {
                    if (!CanRemove(effect.Value.Spell4Id))
                        continue;

                    state.Value.Remove(effect.Key);
                    AddRemoval(new SpellStateRemoval(
                        SpellStateRemovalKind.CrowdControl,
                        effect.Value.Spell4Id,
                        effect.Value.CastingId,
                        effect.Key,
                        state.Key));

                }

                if (state.Value.Count == 0)
                    ccStates.Remove(state.Key);

            }

            foreach (KeyValuePair<uint, TrackedSpellState> state in stealthStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                stealthStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.Stealth,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, TrackedSpellState> state in aggroImmuneStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                aggroImmuneStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.AggroImmune,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, SpellEffectImmunityState> state in spellEffectImmunityStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                spellEffectImmunityStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.SpellEffectImmunity,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, SpellImmunityState> state in spellImmunityStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                spellImmunityStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.SpellImmunity,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, DelayDeathState> state in delayDeathStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                delayDeathStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.DelayDeath,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, ProcState> state in procStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                procStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.Proc,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, VitalClampState> state in vitalClampStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                vitalClampStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.VitalClamp,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, TrackedSpellState> state in shieldOverloadStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                shieldOverloadStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.ShieldOverload,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, UnitStateSetState> state in unitStateSetStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                unitStateSetStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.UnitState,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            bool wasBusy = IsBusy;
            foreach (KeyValuePair<uint, BusyState> state in busyStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                busyStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.Busy,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }
            BroadcastBusyStateIfChanged(wasBusy);

            foreach (KeyValuePair<uint, ScaleState> state in scaleStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                scaleStates.Remove(state.Key);
                RestoreScale(state.Value);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.Scale,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, FactionState> state in factionStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                factionStates.Remove(state.Key);
                SetFaction(state.Value.PreviousFaction);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.Faction,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, ItemVisualSwapState> state in itemVisualSwapStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                itemVisualSwapStates.Remove(state.Key);
                RestoreItemVisualSwap(state.Value);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.ItemVisualSwap,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, DisguiseOutfitState> state in disguiseOutfitStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                disguiseOutfitStates.Remove(state.Key);
                RestoreDisguiseOutfit(state.Value);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.DisguiseOutfit,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, MimicDisguiseState> state in mimicDisguiseStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                mimicDisguiseStates.Remove(state.Key);
                RestoreMimicDisguise(state.Value);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.MimicDisguise,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, AbsorptionState> state in absorptionStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                absorptionStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.Absorption,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            foreach (KeyValuePair<uint, AbsorptionState> state in healingAbsorptionStates.ToArray())
            {
                if (!CanRemove(state.Value.Spell4Id))
                    continue;

                healingAbsorptionStates.Remove(state.Key);
                AddRemoval(new SpellStateRemoval(
                    SpellStateRemovalKind.HealingAbsorption,
                    state.Value.Spell4Id,
                    state.Value.CastingId,
                    state.Key));

            }

            return removals;
        }

        /// <summary>
        /// Add a <see cref="Property"/> modifier given a Spell4Id and <see cref="ISpellPropertyModifier"/> instance.
        /// </summary>
        public void AddSpellModifierProperty(ISpellPropertyModifier spellModifier, uint spell4Id, uint castingId)
        {
            if (spellProperties.TryGetValue(spellModifier.Property, out Dictionary<uint, ISpellPropertyModifier> spellDict))
            {
                if (spellDict.ContainsKey(spell4Id))
                    spellDict[spell4Id] = spellModifier;
                else
                    spellDict.Add(spell4Id, spellModifier);
            }
            else
            {
                spellProperties.Add(spellModifier.Property, new Dictionary<uint, ISpellPropertyModifier>
                {
                    { spell4Id, spellModifier }
                });
            }

            spellPropertyCastingIds[spell4Id] = castingId;
            CalculateProperty(spellModifier.Property);
        }

        /// <summary>
        /// Remove a <see cref="Property"/> modifier by a Spell that is currently affecting this <see cref="IUnitEntity"/>.
        /// </summary>
        public void RemoveSpellProperty(Property property, uint spell4Id)
        {
            if (spellProperties.TryGetValue(property, out Dictionary<uint, ISpellPropertyModifier> spellDict))
            {
                spellDict.Remove(spell4Id);
                if (spellDict.Count == 0)
                    spellProperties.Remove(property);
            }

            if (!HasAnySpellProperties(spell4Id))
                spellPropertyCastingIds.Remove(spell4Id);

            CalculateProperty(property);
        }

        /// <summary>
        /// Remove all <see cref="Property"/> modifiers by a Spell that is currently affecting this <see cref="IUnitEntity"/>
        /// </summary>
        public bool RemoveSpellProperties(uint spell4Id)
        {
            List<Property> propertiesWithSpell = spellProperties.Where(i => i.Value.ContainsKey(spell4Id)).Select(p => p.Key).ToList();

            foreach (Property property in propertiesWithSpell)
                RemoveSpellProperty(property, spell4Id);

            return propertiesWithSpell.Count > 0;
        }

        /// <summary>
        /// Return all <see cref="IPropertyModifier"/> for this <see cref="IUnitEntity"/>'s <see cref="Property"/>
        /// </summary>
        private IEnumerable<ISpellPropertyModifier> GetSpellPropertyModifiers(Property property)
        {
            return spellProperties.ContainsKey(property) ? spellProperties[property].Values : Enumerable.Empty<ISpellPropertyModifier>();
        }

        private bool HasAnySpellProperties(uint spell4Id)
        {
            return spellProperties.Values.Any(spellDict => spellDict.ContainsKey(spell4Id));
        }

        protected override void CalculatePropertyValue(IPropertyValue propertyValue)
        {
            base.CalculatePropertyValue(propertyValue);

            // Run through spell adjustments first because they could adjust base properties
            // dataBits01 appears to be some form of Priority or Math Operator
            foreach (ISpellPropertyModifier spellModifier in GetSpellPropertyModifiers(propertyValue.Property)
                .OrderByDescending(s => s.Priority))
            {
                foreach (IPropertyModifier alteration in spellModifier.Alterations)
                {
                    // TODO: Add checks to ensure we're not modifying FlatValue and Percentage in the same effect?
                    switch (alteration.ModType)
                    {
                        case ModType.FlatValue:
                        case ModType.LevelScale:
                            propertyValue.Value += alteration.GetValue(Level);
                            break;
                        case ModType.Percentage:
                            propertyValue.Value *= alteration.GetValue();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles regeneration of Stat Values. Used to provide a hook into the Update method, for future implementation.
        /// </summary>
        private void HandleStatUpdate(double lastTick)
        {
            if (!IsAlive)
                return;

            // TODO: This should probably get moved to a Calculation Library/Manager at some point. There will be different timers on Stat refreshes, but right now the timer is hardcoded to every 0.25s.
            // Probably worth considering an Attribute-grouped Class that allows us to run differentt regeneration methods & calculations for each stat.

            if (Health < MaxHealth)
                ModifyHealth((uint)(MaxHealth / 200f), DamageType.Heal, null);

            if (!IsShieldOverloaded && Shield < MaxShieldCapacity)
                Shield += (uint)(MaxShieldCapacity * GetPropertyValue(Property.ShieldRegenPct) * statUpdateTimer.Duration);
        }

        /// <summary>
        /// Cast a <see cref="ISpell"/> with the supplied spell id and <see cref="ISpellParameters"/>.
        /// </summary>
        public void CastSpell(uint spell4Id, ISpellParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException();

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
                throw new ArgumentOutOfRangeException();

            CastSpell(spell4Entry.Spell4BaseIdBaseSpell, (byte)spell4Entry.TierIndex, parameters);
        }

        /// <summary>
        /// Cast a <see cref="ISpell"/> with the supplied spell base id, tier and <see cref="ISpellParameters"/>.
        /// </summary>
        public void CastSpell(uint spell4BaseId, byte tier, ISpellParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException();

            ISpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4BaseId);
            if (spellBaseInfo == null)
                throw new ArgumentOutOfRangeException();

            ISpellInfo spellInfo = spellBaseInfo.GetSpellInfo(tier);
            if (spellInfo == null)
                throw new ArgumentOutOfRangeException();

            parameters.SpellInfo = spellInfo;
            CastSpell(parameters);
        }

        /// <summary>
        /// Cast a <see cref="ISpell"/> with the supplied <see cref="ISpellParameters"/>.
        /// </summary>
        public void CastSpell(ISpellParameters parameters)
        {
            if (!IsAlive)
                return;

            if (parameters == null)
                throw new ArgumentNullException();

            if (DisableManager.Instance.IsDisabled(DisableType.BaseSpell, parameters.SpellInfo.BaseInfo.Entry.Id))
            {
                if (this is IPlayer player)
                    player.SendSystemMessage($"Unable to cast base spell {parameters.SpellInfo.BaseInfo.Entry.Id} because it is disabled.");
                return;
            }

            if (DisableManager.Instance.IsDisabled(DisableType.Spell, parameters.SpellInfo.Entry.Id))
            {
                if (this is IPlayer player)
                    player.SendSystemMessage($"Unable to cast spell {parameters.SpellInfo.Entry.Id} because it is disabled.");
                return;
            }

            if (parameters.UserInitiatedSpellCast)
            {
                if (this is IPlayer player)
                    player.Dismount();
            }

            var spell = new Spell.Spell(this, parameters);
            ProbeProcEvent("action-cast-any", ProcTriggerEventCandidate.ActionCastAny, this, ResolveProcProbePrimaryTarget(parameters), spell, null, null, "before-cast");
            spell.Cast();
            pendingSpells.Add(spell);
        }

        private IUnitEntity ResolveProcProbePrimaryTarget(ISpellParameters parameters)
        {
            if (parameters.PrimaryTargetId == 0u)
                return null;

            if (parameters.PrimaryTargetId == Guid)
                return this;

            return GetVisible<IGridEntity>(parameters.PrimaryTargetId) as IUnitEntity;
        }

        /// <summary>
        /// Cancel any <see cref="ISpell"/>'s that are interrupted by movement.
        /// </summary>
        public void CancelSpellsOnMove()
        {
            foreach (ISpell spell in pendingSpells)
                if (spell.IsMovingInterrupted() && spell.IsCasting)
                    spell.CancelCast(CastResult.CasterMovement);
        }

        /// <summary>
        /// Cancel an <see cref="ISpell"/> based on its casting id.
        /// </summary>
        /// <param name="castingId">Casting ID of the spell to cancel</param>
        public void CancelSpellCast(uint castingId)
        {
            ISpell spell = pendingSpells.SingleOrDefault(s => s.CastingId == castingId);
            spell?.CancelCast(CastResult.SpellCancelled);
        }

        /// <summary>
        /// Returns an active <see cref="ISpell"/> that is affecting this <see cref="IUnitEntity"/>
        /// </summary>
        public ISpell GetActiveSpell(Func<ISpell, bool> func)
        {
            return pendingSpells.FirstOrDefault(func);
        }

        /// <summary>
        /// Determine if this <see cref="IUnitEntity"/> can attack supplied <see cref="IUnitEntity"/>.
        /// </summary>
        public virtual bool CanAttack(IUnitEntity target)
        {
            if (!IsAlive)
                return false;

            if (IsAggroImmune || target.IsAggroImmune)
                return false;

            if (!target.IsValidAttackTarget() || !IsValidAttackTarget())
                return false;

            return GetDispositionTo(target.Faction1) < Disposition.Friendly;
        }

        /// <summary>
        /// Returns whether or not this <see cref="IUnitEntity"/> is an attackable target.
        /// </summary>
        public bool IsValidAttackTarget()
        {
            // TODO: Expand on this. There's bound to be flags or states that should prevent an entity from being attacked.
            return (this is IPlayer or INonPlayerEntity);
        }

        /// <summary>
        /// Deal damage to this <see cref="IUnitEntity"/> from the supplied <see cref="IUnitEntity"/>.
        /// </summary>
        public void TakeDamage(IUnitEntity attacker, IDamageDescription damageDescription)
        {
            if (!IsAlive || !attacker.IsAlive)
                return;

            bool wasAlive = IsAlive;

            // TODO: Calculate Threat properly
            ThreatManager.UpdateThreat(attacker, (int)damageDescription.RawDamage);

            Shield -= damageDescription.ShieldAbsorbAmount;
            ModifyHealth(damageDescription.AdjustedDamage, damageDescription.DamageType, attacker);

            damageDescription.KilledTarget = wasAlive && !IsAlive;
            if (damageDescription.KilledTarget)
                attacker.ProbeProcEvent("target-killed", ProcTriggerEventCandidate.KillTarget, attacker, this, null, null, damageDescription, "after-apply");
        }

        /// <summary>
        /// Modify the health of this <see cref="IUnitEntity"/> by the supplied amount.
        /// </summary>
        /// <remarks>
        /// If the <see cref="DamageType"/> is <see cref="DamageType.Heal"/> amount is added to current health otherwise subtracted.
        /// </remarks>
        public virtual void ModifyHealth(uint amount, DamageType type, IUnitEntity source)
        {
            if (type != DamageType.Heal && amount >= Health && TryConsumeDelayDeath(source, amount))
                return;

            long newHealth = Health;
            if (type == DamageType.Heal)
                newHealth += amount;
            else
                newHealth -= amount;

            Health = (uint)Math.Clamp(newHealth, 0u, MaxHealth);
            ApplyVitalClamps();

            scriptCollection?.Invoke<IUnitScript>(s => s.OnHealthChange(source, amount, type));

            if (Health == 0)
                OnDeath();
        }

        private void ApplyVitalClamps()
        {
            if (vitalClampStates.Count == 0)
                return;

            foreach (VitalClampState state in vitalClampStates.Values)
            {
                switch (state.Vital)
                {
                    case Vital.Health:
                        ApplyHealthClamp(state.Ratio);
                        break;
                }
            }
        }

        private void ApplyHealthClamp(float ratio)
        {
            if (!float.IsFinite(ratio) || ratio <= 0f || ratio >= 1f || MaxHealth == 0u)
                return;

            uint cap = Math.Max(1u, (uint)MathF.Ceiling(MaxHealth * ratio));
            if (Health > cap)
                Health = cap;
        }

        private bool TryConsumeDelayDeath(IUnitEntity source, uint preventedDamage)
        {
            if (Health == 0u || delayDeathStates.Count == 0)
                return false;

            KeyValuePair<uint, DelayDeathState> statePair = delayDeathStates.First();
            delayDeathStates.Remove(statePair.Key);

            DelayDeathState state = statePair.Value;
            Health = 1u;

            EnqueueToVisible(new ServerCombatLog
            {
                CombatLog = new CombatLogDelayDeath
                {
                    CastData = new CombatLogCastData
                    {
                        CasterId     = source?.Guid ?? Guid,
                        TargetId     = Guid,
                        SpellId      = state.Spell4Id,
                        CombatResult = CombatResult.Hit
                    }
                }
            }, true);

            SpellEffectDiagnostics.TraceDelayDeathTriggered(this, state.Spell4Id, state.CastingId, state.Mode, state.TriggerSpell4Id, state.TriggerDelayMs, state.DataBits03, state.DataBits04, state.DataBits05, state.DataBits06, state.DataBits07, source?.Guid ?? 0u, preventedDamage);
            QueueDelayDeathTrigger(state, source?.Guid ?? 0u);
            return true;
        }

        private void QueueDelayDeathTrigger(DelayDeathState state, uint sourceGuid)
        {
            if (state.TriggerSpell4Id == 0u)
                return;

            if (state.TriggerDelayMs == 0u)
            {
                CastDelayDeathTrigger(state.TriggerSpell4Id, sourceGuid);
                return;
            }

            pendingDelayDeathTriggers.Add(new PendingDelayDeathTrigger
            {
                Timer      = new UpdateTimer(state.TriggerDelayMs / 1000d),
                Spell4Id   = state.TriggerSpell4Id,
                SourceGuid = sourceGuid
            });
        }

        private void HandleDelayDeathTriggers(double lastTick)
        {
            foreach (PendingDelayDeathTrigger trigger in pendingDelayDeathTriggers.ToArray())
            {
                trigger.Timer.Update(lastTick);
                if (!trigger.Timer.HasElapsed)
                    continue;

                pendingDelayDeathTriggers.Remove(trigger);
                CastDelayDeathTrigger(trigger.Spell4Id, trigger.SourceGuid);
            }
        }

        private void CastDelayDeathTrigger(uint spell4Id, uint sourceGuid)
        {
            if (!IsAlive)
                return;

            if (GameTableManager.Instance.Spell4.GetEntry(spell4Id) == null)
                return;

            SpellEffectDiagnostics.TraceDelayDeathTriggerCast(this, spell4Id, sourceGuid);
            CastSpell(spell4Id, new SpellParameters
            {
                PrimaryTargetId = Guid
            });
        }

        protected virtual void OnDeath()
        {
            DeathState = EntityDeathState.JustDied;
            scriptCollection?.Invoke<IUnitScript>(s => s.OnDeath());

            foreach (ISpell spell in pendingSpells)
            {
                if (spell.IsCasting)
                    spell.CancelCast(CastResult.CasterCannotBeDead);
            }

            GenerateRewards();
            ClearCombatState();
            ScheduleRespawn();

            DeathState = EntityDeathState.Dead;
        }

        private void GenerateRewards()
        {
            foreach (IHostileEntity hostile in ThreatManager)
            {
                IUnitEntity entity = GetVisible<IUnitEntity>(hostile.HatedUnitId);
                if (entity is IPlayer player)
                    RewardKiller(player);
            }
        }

        protected virtual void RewardKiller(IPlayer player)
        {
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.KillCreature, CreatureId, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.KillCreature2, CreatureId, 1u);
            player.AchievementManager.CheckAchievements(player, AchievementType.KillCreatureEntry, CreatureId);

            foreach (uint targetGroupId in AssetManager.Instance.GetTargetGroupsForCreatureId(CreatureId) ?? Enumerable.Empty<uint>())
            {
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.KillTargetGroup, targetGroupId, 1u);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.KillTargetGroups, targetGroupId, 1u);
                player.AchievementManager.CheckAchievements(player, AchievementType.KillCreatureGroup, targetGroupId);
            }

            // TODO: Reward XP
            // TODO: Reward Loot
        }

        private void ClearCombatState()
        {
            SetTarget((IWorldEntity)null);
            ThreatManager.ClearThreatList();
            UpdateCombatState();
        }

        private void ScheduleRespawn()
        {
            if (this is not INonPlayerEntity)
                return;

            uint respawnSeconds = SharedConfiguration.Instance.Get<WorldConfig>()?.CreatureRespawnSeconds ?? 30u;
            respawnTimer = new UpdateTimer(respawnSeconds);
        }

        private void HandleRespawn(double lastTick)
        {
            if (respawnTimer == null)
                return;

            respawnTimer.Update(lastTick);
            if (!respawnTimer.HasElapsed)
                return;

            respawnTimer = null;
            Respawn();
        }

        private void Respawn()
        {
            Health = MaxHealth;
            Shield = MaxShieldCapacity;
            DeathState = null;
        }

        /// <summary>
        /// Set target to supplied target guid.
        /// </summary>
        /// <remarks>
        /// A null target will clear the current target.
        /// </remarks>
        public void SetTarget(uint? target, uint threat = 0u)
        {
            SetTarget(target != null ? GetVisible<IWorldEntity>(target.Value) : null, threat);
        }

        /// <summary>
        /// Set target to supplied <see cref="IUnitEntity"/>.
        /// </summary>
        /// <remarks>
        /// A null target will clear the current target.
        /// </remarks>
        public virtual void SetTarget(IWorldEntity target, uint threat = 0u)
        {
            // notify current target they are no longer the target
            if (TargetGuid != null)
                GetVisible<IWorldEntity>(TargetGuid.Value)?.OnUntargeted(this);

            target?.OnTargeted(this);

            EnqueueToVisible(new ServerEntityTargetUnit
            {
                UnitId      = Guid,
                NewTargetId = target?.Guid ?? 0u,
                ThreatLevel = threat
            });

            TargetGuid = target?.Guid;
        }

        /// <summary>
        /// Invoked when a new <see cref="IHostileEntity"/> is added to the threat list.
        /// </summary>
        public virtual void OnThreatAddTarget(IHostileEntity hostile)
        {
            UpdateCombatState();
            scriptCollection?.Invoke<IUnitScript>(s => s.OnThreatAddTarget(hostile));
        }

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is removed from the threat list.
        /// </summary>
        public virtual void OnThreatRemoveTarget(IHostileEntity hostile)
        {
            UpdateCombatState();
            scriptCollection?.Invoke<IUnitScript>(s => s.OnThreatRemoveTarget(hostile));
        }

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is update on the threat list.
        /// </summary>
        public virtual void OnThreatChange(IHostileEntity hostile)
        {
            scriptCollection?.Invoke<IUnitScript>(s => s.OnThreatChange(hostile));
        }

        private void UpdateCombatState()
        {
            // ensure conditions for combat state change are met
            if (ThreatManager.IsThreatened == InCombat)
                return;

            InCombat   = ThreatManager.IsThreatened;
            Sheathed   = !inCombat;
            StandState = inCombat ? StandState.Stand : StandState.State0;
        }
    }
}
