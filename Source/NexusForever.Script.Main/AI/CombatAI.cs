using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Command;
using NexusForever.Game.Abstract.Entity.Movement.Command.Position;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Story;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script.Template;
using NexusForever.Shared;
using NexusForever.Shared.Game;

namespace NexusForever.Script.Main.AI
{
    public class CombatAI : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private const float ChaseRepathDistance = 2f;
        private const float ChaseFollowDistanceTolerance = 0.25f;
        private static readonly uint SpecialCastInterruptStateMask = CreateCrowdControlMask(CCState.Interrupt);

        protected ICreatureEntity entity;

        private CombatProfile profile = CombatProfile.Default;
        private int autoAttackIndex;
        protected List<uint> autoAttacks = CombatProfile.Default.AutoAttackSpell4Ids.ToList();
        private readonly UpdateTimer autoAttackTimer = new(TimeSpan.FromSeconds(1.5d));
        private bool selectingTarget;
        private readonly List<CombatSpecialAttackState> specialAttacks = [];
        private readonly List<CombatSpecialAttackState> specialAttackSchedule = [];
        private int specialAttackCursor;

        private readonly UpdateTimer idleAggroScanTimer = new(TimeSpan.FromSeconds(0.5d));

        private float chaseDistance = CombatProfile.Default.ChaseDistance;
        private readonly UpdateTimer chaseDistanceTimer = new(TimeSpan.FromSeconds(1d));
        private uint? activeChaseTargetGuid;
        private Vector3 activeChaseTargetPosition;
        private float activeChaseFollowDistance;
        private bool returningToLeash;
        private bool rangeCheckArmed;
        private bool combatEnabled;
        private bool idleAggroEnabled;
        private double specialCastLockoutSeconds;

        #region Dependency Injection

        private readonly IFactory<ISpellParameters> spellParametersFactory;
        private readonly IGameTableManager gameTableManager;
        private readonly ICombatProfileProvider combatProfileProvider;
        private readonly ILogger<CombatAI> log;

        protected virtual bool EnablesDefaultCombatProfile => GetType() != typeof(CombatAI);

        private sealed class CombatSpecialAttackState
        {
            public CombatSpecialAttack Attack { get; init; }
            public UpdateTimer CooldownTimer { get; init; }
            public int Index { get; init; }
        }

        public CombatAI(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager,
            ICombatProfileProvider combatProfileProvider = null,
            ILogger<CombatAI> log = null)
        {
            this.spellParametersFactory = spellParametersFactory;
            this.gameTableManager       = gameTableManager;
            this.combatProfileProvider  = combatProfileProvider ?? DefaultCombatProfileProvider.ForGameTables(gameTableManager);
            this.log                    = log ?? NullLogger<CombatAI>.Instance;
        }

        #endregion

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public virtual void OnLoad(ICreatureEntity owner)
        {
            entity = owner;
            CombatProfile resolvedProfile = combatProfileProvider.GetProfile(entity);
            idleAggroEnabled = resolvedProfile != null || EnablesDefaultCombatProfile;
            combatEnabled = true;
            CombatProfile fallbackProfile = combatProfileProvider is IDefaultCombatProfileProvider defaultProvider
                ? defaultProvider.GetDefaultProfile()
                : CombatProfile.Default;
            profile = resolvedProfile ?? fallbackProfile;
            autoAttacks = combatEnabled
                ? profile.AutoAttackSpell4Ids.ToList()
                : [];
            specialAttacks.Clear();
            specialAttackSchedule.Clear();
            specialAttackCursor = 0;
            if (combatEnabled)
            {
                int specialAttackIndex = 0;
                foreach (CombatSpecialAttack attack in profile.SpecialAttacks ?? [])
                {
                    if (attack.Spell4Id == 0u || attack.CooldownSeconds <= 0d)
                        continue;

                    var state = new CombatSpecialAttackState
                    {
                        Attack        = attack,
                        CooldownTimer = new UpdateTimer(attack.CooldownSeconds),
                        Index         = specialAttackIndex++
                    };
                    ApplySpecialAttackCooldownOffset(state);
                    specialAttacks.Add(state);

                    int weightSlots = GetSpecialAttackWeightSlots(attack);
                    for (int i = 0; i < weightSlots; i++)
                        specialAttackSchedule.Add(state);
                }
            }

            chaseDistance = profile.ChaseDistance;
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to <see cref="IBaseMap"/>.
        /// </summary>
        public virtual void OnAddToMap(IBaseMap map)
        {
            if (!idleAggroEnabled)
                return;

            rangeCheckArmed = true;
            entity.SetInRangeCheck(GetEffectiveLeashRange());

            if (profile.TraceCombat)
            {
                log.LogDebug(
                    "Profiled combat AI loaded for creature {CreatureId} guid {Guid}: leashRange={LeashRange}, effectiveLeashRange={EffectiveLeashRange}, aggroRange={AggroRange}, assistRange={AssistRange}, stationary={Stationary}, leashPosition=({LeashX}, {LeashY}, {LeashZ}), position=({X}, {Y}, {Z}).",
                    entity.CreatureId,
                    entity.Guid,
                    entity.LeashRange,
                    GetEffectiveLeashRange(),
                    GetEffectiveAggroRange(),
                    profile.AssistRange,
                    profile.Stationary,
                    entity.LeashPosition.X,
                    entity.LeashPosition.Y,
                    entity.LeashPosition.Z,
                    entity.Position.X,
                    entity.Position.Y,
                    entity.Position.Z);
            }
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is removed from <see cref="IBaseMap"/>.
        /// </summary>
        public virtual void OnRemoveFromMap(IBaseMap map)
        {
            rangeCheckArmed = false;
            activeChaseTargetGuid = null;
            returningToLeash = false;
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public virtual void Update(double lastTick)
        {
            if (!combatEnabled || !entity.IsAlive)
                return;

            if (!entity.TargetGuid.HasValue)
            {
                if (entity.ThreatManager.IsThreatened)
                    SelectTarget();
                else if (rangeCheckArmed)
                    UpdateIdleAggroScan(lastTick);

                return;
            }

            if (!ValidateCurrentTarget())
                return;

            UpdateAI(lastTick);
        }

        protected virtual void UpdateAI(double lastTick)
        {
            if (TryCastSpecialAttack(lastTick))
                return;

            autoAttackTimer.Update(lastTick);
            if (autoAttackTimer.HasElapsed)
            {
                DoAutoAttack();
                autoAttackTimer.Reset();
            }

            if (profile.Stationary)
                return;

            chaseDistanceTimer.Update(lastTick);
            if (chaseDistanceTimer.HasElapsed)
            {
                DoChase();
                chaseDistanceTimer.Reset();
            }
        }

        private bool TryCastSpecialAttack(double lastTick)
        {
            if (specialAttacks.Count == 0 || !entity.TargetGuid.HasValue)
                return false;

            IUnitEntity target = GetCurrentVisibleTarget();
            if (target == null || !entity.CanAttack(target))
                return false;

            foreach (CombatSpecialAttackState state in specialAttacks)
            {
                state.CooldownTimer.Update(lastTick);
            }

            if (UpdateSpecialCastLockout(lastTick))
                return true;

            HashSet<CombatSpecialAttackState> attempted = [];
            for (int offset = 0; offset < specialAttackSchedule.Count; offset++)
            {
                CombatSpecialAttackState state = specialAttackSchedule[(specialAttackCursor + offset) % specialAttackSchedule.Count];
                if (!attempted.Add(state))
                    continue;

                if (!state.CooldownTimer.HasElapsed)
                    continue;

                if (!TryCastSpecialAttack(state.Attack, target))
                    continue;

                ResetSpecialAttackCooldown(state);
                specialAttackCursor = (specialAttackCursor + offset + 1) % specialAttackSchedule.Count;
                return true;
            }

            return false;
        }

        private void ResetSpecialAttackCooldown(CombatSpecialAttackState state)
        {
            state.CooldownTimer.Reset();
            ApplySpecialAttackCooldownOffset(state);
        }

        private void ApplySpecialAttackCooldownOffset(CombatSpecialAttackState state)
        {
            double maxOffset = Math.Min(1.25d, state.CooldownTimer.Duration * 0.2d);
            if (maxOffset <= 0d)
                return;

            double offset = GetDeterministicSpecialAttackOffset(state) * maxOffset;
            if (offset > 0d)
                state.CooldownTimer.Update(offset);
        }

        private double GetDeterministicSpecialAttackOffset(CombatSpecialAttackState state)
        {
            uint hash = HashSpecialAttack(entity?.CreatureId ?? 0u, entity?.Guid ?? 0u, state.Attack.Spell4Id, (uint)state.Index);
            return (hash % 1000u) / 1000d;
        }

        private static int GetSpecialAttackWeightSlots(CombatSpecialAttack attack)
        {
            if (!double.IsFinite(attack.Weight) || attack.Weight <= 0d)
                return 1;

            return Math.Clamp((int)Math.Round(attack.Weight, MidpointRounding.AwayFromZero), 1, 10);
        }

        private static uint HashSpecialAttack(uint creatureId, uint guid, uint spell4Id, uint index)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ creatureId) * 16777619u;
                hash = (hash ^ guid) * 16777619u;
                hash = (hash ^ spell4Id) * 16777619u;
                hash = (hash ^ index) * 16777619u;
                return hash;
            }
        }

        private bool TryCastSpecialAttack(CombatSpecialAttack attack, IUnitEntity target)
        {
            Spell4Entry spell4Entry = gameTableManager.Spell4?.GetEntry(attack.Spell4Id);
            if (spell4Entry == null)
            {
                if (ShouldLogStarterTutorialCombat(target))
                    log.LogTrace("Profiled combat AI special attack skipped for creature {CreatureId} guid {Guid}: spell {Spell4Id} was not found.", entity.CreatureId, entity.Guid, attack.Spell4Id);

                return false;
            }

            SpellRangeInfo rangeInfo = GetSpellRangeInfo(entity, target);
            if (!rangeInfo.IsFinite)
            {
                SelectTarget();
                return false;
            }

            if (!IsWithinSpecialAttackRange(attack, spell4Entry, rangeInfo))
                return false;

            if (attack.FaceTarget)
                entity.MovementManager.SetRotationFaceUnit(target.Guid);

            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.PrimaryTargetId = target.Guid;
            CastResult castResult = entity.TryCastSpell(attack.Spell4Id, spellParameters);
            if (castResult != CastResult.Ok)
            {
                if (ShouldLogStarterTutorialCombat(target))
                    log.LogTrace("Profiled combat AI special attack skipped for creature {CreatureId} guid {Guid}: spell {Spell4Id} returned {CastResult}.", entity.CreatureId, entity.Guid, attack.Spell4Id, castResult);

                return false;
            }

            StartSpecialCastLockout(spell4Entry);
            return true;
        }

        private bool UpdateSpecialCastLockout(double lastTick)
        {
            if (specialCastLockoutSeconds <= 0d)
                return false;

            if (IsSpecialCastInterrupted())
            {
                specialCastLockoutSeconds = 0d;
                activeChaseTargetGuid = null;

                if (profile.TraceCombat)
                    log.LogTrace("Profiled combat AI special cast lockout interrupted for creature {CreatureId} guid {Guid}: activeCCStateMask={ActiveCCStateMask}.", entity.CreatureId, entity.Guid, entity.ActiveCCStateMask);

                return false;
            }

            specialCastLockoutSeconds = Math.Max(0d, specialCastLockoutSeconds - Math.Max(lastTick, 0d));
            return specialCastLockoutSeconds > 0d;
        }

        private bool IsSpecialCastInterrupted()
        {
            return (entity.ActiveCCStateMask & SpecialCastInterruptStateMask) != 0u;
        }

        private void StartSpecialCastLockout(Spell4Entry spell4Entry)
        {
            if (spell4Entry.CastTime == 0u)
                return;

            specialCastLockoutSeconds = Math.Max(specialCastLockoutSeconds, spell4Entry.CastTime / 1000d);
            activeChaseTargetGuid = null;

            if (!profile.Stationary)
                entity.MovementManager.Finalise();
        }

        private static bool IsWithinSpecialAttackRange(CombatSpecialAttack attack, Spell4Entry spell4Entry, SpellRangeInfo rangeInfo)
        {
            if (!rangeInfo.IsFinite)
                return false;

            if (spell4Entry.TargetMinRange > 0f && rangeInfo.HorizontalRange < spell4Entry.TargetMinRange)
                return false;

            float maxRange = attack.MaxRange ?? spell4Entry.TargetMaxRange;
            if (maxRange > 0f && rangeInfo.EffectiveRange > maxRange)
                return false;

            if (spell4Entry.TargetVerticalRange > 0f && rangeInfo.VerticalDelta > spell4Entry.TargetVerticalRange)
                return false;

            return true;
        }

        private static uint CreateCrowdControlMask(params CCState[] states)
        {
            uint mask = 0u;
            foreach (CCState state in states)
                mask |= 1u << (int)state;

            return mask;
        }

        private void UpdateIdleAggroScan(double lastTick)
        {
            idleAggroScanTimer.Update(lastTick);
            if (!idleAggroScanTimer.HasElapsed)
                return;

            idleAggroScanTimer.Reset();

            foreach (IUnitEntity unit in entity.GetInRange<IUnitEntity>(0u)
                .Where(unit => unit != null)
                .Where(unit => unit.Guid != entity.Guid)
                .Where(CanIdleAggroTarget)
                .OrderBy(GetFiniteAggroDistance)
                .ToList())
            {
                AggroEntity(unit, true);

                if (entity.TargetGuid.HasValue || entity.ThreatManager.IsThreatened)
                    return;
            }
        }

        private void DoAutoAttack()
        {
            if (autoAttacks.Count == 0 || !entity.TargetGuid.HasValue)
                return;

            IUnitEntity target = GetCurrentVisibleTarget();
            if (target == null)
            {
                if (ShouldLogStarterTutorialCombat())
                    log.LogTrace("Starter tutorial combat AI auto-attack skipped for creature {CreatureId} guid {Guid}: target {TargetGuid} was not found.", entity.CreatureId, entity.Guid, entity.TargetGuid.Value);

                SelectTarget();
                return;
            }

            if (!entity.CanAttack(target))
            {
                if (ShouldLogStarterTutorialCombat(target))
                    log.LogTrace("Starter tutorial combat AI auto-attack skipped for creature {CreatureId} guid {Guid}: cannot attack target {TargetGuid}.", entity.CreatureId, entity.Guid, target.Guid);

                return;
            }

            uint spell4Id = autoAttacks[autoAttackIndex];
            autoAttackIndex = (autoAttackIndex + 1) % autoAttacks.Count;

            Spell4Entry spell4Entry = gameTableManager.Spell4?.GetEntry(spell4Id);
            if (spell4Entry == null)
            {
                if (ShouldLogStarterTutorialCombat(target))
                    log.LogTrace("Starter tutorial combat AI auto-attack skipped for creature {CreatureId} guid {Guid}: spell {Spell4Id} was not found.", entity.CreatureId, entity.Guid, spell4Id);

                return;
            }

            chaseDistance = GetClampedChaseDistance(chaseDistance, spell4Entry);

            SpellRangeInfo rangeInfo = GetSpellRangeInfo(entity, target);
            if (!rangeInfo.IsFinite)
            {
                SelectTarget();
                return;
            }

            if (!IsWithinSpellPrimaryTargetRange(spell4Entry, rangeInfo))
            {
                if (ShouldLogStarterTutorialCombat(target))
                {
                    log.LogTrace(
                        "Starter tutorial combat AI auto-attack skipped for creature {CreatureId} guid {Guid}: target {TargetGuid} is outside spell range; horizontalRange={HorizontalRange}m, effectiveRange={EffectiveRange}m, verticalDelta={VerticalDelta}m, minRange={MinRange}m, maxRange={MaxRange}m, verticalRange={VerticalRange}m, chaseDistance={ChaseDistance}m.",
                        entity.CreatureId,
                        entity.Guid,
                        target.Guid,
                        rangeInfo.HorizontalRange,
                        rangeInfo.EffectiveRange,
                        rangeInfo.VerticalDelta,
                        spell4Entry.TargetMinRange,
                        spell4Entry.TargetMaxRange,
                        spell4Entry.TargetVerticalRange,
                        chaseDistance);
                }

                return;
            }

            if (ShouldLogStarterTutorialCombat(target))
            {
                log.LogTrace(
                    "Starter tutorial combat AI casting auto-attack {Spell4Id} for creature {CreatureId} guid {Guid} at target {TargetGuid}; horizontalRange={HorizontalRange}m, effectiveRange={EffectiveRange}m, verticalDelta={VerticalDelta}m, maxRange={MaxRange}m.",
                    spell4Id,
                    entity.CreatureId,
                    entity.Guid,
                    target.Guid,
                    rangeInfo.HorizontalRange,
                    rangeInfo.EffectiveRange,
                    rangeInfo.VerticalDelta,
                    spell4Entry.TargetMaxRange);
            }

            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.PrimaryTargetId = entity.TargetGuid.Value;
            entity.CastSpell(spell4Id, spellParameters);
        }

        private void DoChase()
        {
            if (!entity.TargetGuid.HasValue)
                return;

            IUnitEntity target = GetCurrentVisibleTarget();
            if (target == null)
            {
                if (ShouldLogStarterTutorialCombat())
                    log.LogTrace("Starter tutorial combat AI chase skipped for creature {CreatureId} guid {Guid}: target {TargetGuid} was not found.", entity.CreatureId, entity.Guid, entity.TargetGuid.Value);

                SelectTarget();
                return;
            }

            if (!entity.CanAttack(target))
            {
                if (ShouldLogStarterTutorialCombat(target))
                    log.LogTrace("Starter tutorial combat AI chase skipped for creature {CreatureId} guid {Guid}: cannot attack target {TargetGuid}.", entity.CreatureId, entity.Guid, target.Guid);

                return;
            }

            SpellRangeInfo rangeInfo = GetSpellRangeInfo(entity, target);
            if (!rangeInfo.IsFinite)
            {
                SelectTarget();
                return;
            }

            if (rangeInfo.EffectiveRange < chaseDistance)
            {
                StopActiveChase(target.Guid);

                if (ShouldLogStarterTutorialCombat(target))
                {
                    log.LogTrace(
                        "Starter tutorial combat AI chase skipped for creature {CreatureId} guid {Guid}: target {TargetGuid} is within chase distance; effectiveRange={EffectiveRange}m, horizontalRange={HorizontalRange}m, chaseDistance={ChaseDistance}m.",
                        entity.CreatureId,
                        entity.Guid,
                        target.Guid,
                        rangeInfo.EffectiveRange,
                        rangeInfo.HorizontalRange,
                        chaseDistance);
                }

                return;
            }

            float followDistance = chaseDistance / 2f;
            if (!ShouldRepathChase(target, followDistance))
            {
                if (ShouldLogStarterTutorialCombat(target))
                {
                    log.LogTrace(
                        "Starter tutorial combat AI chase update skipped for creature {CreatureId} guid {Guid}: active follow target {TargetGuid} moved less than repath threshold; effectiveRange={EffectiveRange}m, followDistance={FollowDistance}m.",
                        entity.CreatureId,
                        entity.Guid,
                        target.Guid,
                        rangeInfo.EffectiveRange,
                        followDistance);
                }

                return;
            }

            if (ShouldLogStarterTutorialCombat(target))
            {
                log.LogTrace(
                    "Starter tutorial combat AI following target {TargetGuid} for creature {CreatureId} guid {Guid}; effectiveRange={EffectiveRange}m, horizontalRange={HorizontalRange}m, followDistance={FollowDistance}m, creaturePosition=({X}, {Y}, {Z}), targetPosition=({TargetX}, {TargetY}, {TargetZ}).",
                    target.Guid,
                    entity.CreatureId,
                    entity.Guid,
                    rangeInfo.EffectiveRange,
                    rangeInfo.HorizontalRange,
                    followDistance,
                    entity.Position.X,
                    entity.Position.Y,
                    entity.Position.Z,
                    target.Position.X,
                    target.Position.Y,
                    target.Position.Z);
            }

            entity.MovementManager.Chase(target, followDistance);
            activeChaseTargetGuid      = target.Guid;
            activeChaseTargetPosition  = target.Position;
            activeChaseFollowDistance  = followDistance;
            returningToLeash           = false;
        }

        private static float GetClampedChaseDistance(float currentChaseDistance, Spell4Entry spell4Entry)
        {
            if (!float.IsFinite(currentChaseDistance) || currentChaseDistance < 0f)
                currentChaseDistance = CombatProfile.Default.ChaseDistance;

            return spell4Entry.TargetMaxRange > 0f && float.IsFinite(spell4Entry.TargetMaxRange)
                ? MathF.Min(currentChaseDistance, spell4Entry.TargetMaxRange)
                : currentChaseDistance;
        }

        private static bool IsWithinSpellPrimaryTargetRange(Spell4Entry spell4Entry, SpellRangeInfo rangeInfo)
        {
            if (!rangeInfo.IsFinite)
                return false;

            if (spell4Entry.TargetMinRange > 0f && rangeInfo.HorizontalRange < spell4Entry.TargetMinRange)
                return false;

            if (spell4Entry.TargetMaxRange > 0f && rangeInfo.EffectiveRange > spell4Entry.TargetMaxRange)
                return false;

            if (spell4Entry.TargetVerticalRange > 0f && rangeInfo.VerticalDelta > spell4Entry.TargetVerticalRange)
                return false;

            return true;
        }

        private static SpellRangeInfo GetSpellRangeInfo(IUnitEntity caster, IUnitEntity target)
        {
            float horizontalRange = Vector2.Distance(
                new Vector2(caster.Position.X, caster.Position.Z),
                new Vector2(target.Position.X, target.Position.Z));
            float effectiveRange = MathF.Max(0f, horizontalRange - caster.HitRadius * 0.5f - target.HitRadius * 0.5f);
            float verticalDelta = MathF.Abs(caster.Position.Y - target.Position.Y);
            return new SpellRangeInfo(horizontalRange, effectiveRange, verticalDelta);
        }

        private readonly record struct SpellRangeInfo(float HorizontalRange, float EffectiveRange, float VerticalDelta)
        {
            public bool IsFinite => float.IsFinite(HorizontalRange)
                && float.IsFinite(EffectiveRange)
                && float.IsFinite(VerticalDelta);
        }

        private bool ShouldRepathChase(IUnitEntity target, float followDistance)
        {
            if (activeChaseTargetGuid != target.Guid)
                return true;

            if (!IsFinite(activeChaseTargetPosition) || !IsFinite(target.Position))
                return true;

            if (MathF.Abs(activeChaseFollowDistance - followDistance) > ChaseFollowDistanceTolerance)
                return true;

            return Vector2.DistanceSquared(
                new Vector2(activeChaseTargetPosition.X, activeChaseTargetPosition.Z),
                new Vector2(target.Position.X, target.Position.Z)) >= ChaseRepathDistance * ChaseRepathDistance;
        }

        private void StopActiveChase(uint targetGuid)
        {
            if (activeChaseTargetGuid != targetGuid)
                return;

            activeChaseTargetGuid = null;
            entity.MovementManager.Finalise();
        }

        /// <summary>
        /// Invoked when <see cref="IPositionCommand"/> is finalised.
        /// </summary>
        public void OnPositionEntityCommandFinalise(IPositionCommand command)
        {
            if (!combatEnabled)
                return;

            activeChaseTargetGuid = null;

            if (!entity.TargetGuid.HasValue)
            {
                if (returningToLeash)
                    TryResumePatrolSpline();

                return;
            }

            entity.MovementManager.SetRotationFaceUnit(entity.TargetGuid.Value);
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to range check range.
        /// </summary>
        public void OnEnterRange(IGridEntity entity)
        {
            AggroEntity(entity, true);
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is removed from range check range.
        /// </summary>
        public void OnExitRange(IGridEntity entity)
        {
            if (!combatEnabled)
                return;

            if (entity is not IUnitEntity unit)
                return;

            this.entity.ThreatManager.RemoveHostile(unit.Guid);
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is removed from vision range.
        /// </summary>
        public void OnRemoveVisibleEntity(IGridEntity entity)
        {
            if (!combatEnabled)
                return;

            if (entity.Guid == this.entity.TargetGuid)
                SelectTarget();
        }

        /// <summary>
        /// Invoked when health is changed by source <see cref="IUnitEntity"/>.
        /// </summary>
        public virtual void OnHealthChange(IUnitEntity source, uint amount, DamageType? type)
        {
            if (type is DamageType.Heal or null)
                return;

            AggroEntity(source, false);
        }

        private bool IsWithinLeash(IUnitEntity unit)
        {
            float distance = GetLeashDistance(unit);
            return float.IsFinite(distance) && distance <= GetEffectiveLeashRange();
        }

        private bool IsWithinAggroRange(IUnitEntity unit)
        {
            float distance = GetAggroDistance(unit);
            return float.IsFinite(distance) && distance <= GetEffectiveAggroRange();
        }

        private float GetLeashDistance(IUnitEntity unit)
        {
            return Vector2.Distance(
                new Vector2(entity.LeashPosition.X, entity.LeashPosition.Z),
                new Vector2(unit.Position.X, unit.Position.Z));
        }

        private float GetAggroDistance(IUnitEntity unit)
        {
            return Vector2.Distance(
                new Vector2(entity.Position.X, entity.Position.Z),
                new Vector2(unit.Position.X, unit.Position.Z));
        }

        private float GetFiniteAggroDistance(IUnitEntity unit)
        {
            float distance = GetAggroDistance(unit);
            return float.IsFinite(distance)
                ? distance
                : float.PositiveInfinity;
        }

        private float GetEffectiveAggroRange()
        {
            return profile.AggroRange ?? GetEffectiveLeashRange();
        }

        private void AggroEntity(IGridEntity source, bool requireAggroRange)
        {
            if (!combatEnabled || (requireAggroRange && !rangeCheckArmed))
                return;

            if (source == null || source.Guid == entity.Guid)
            {
                if (profile.TraceCombat)
                    log.LogTrace("Starter tutorial combat AI aggro skipped for creature {CreatureId} guid {Guid}: source is self or null.", entity.CreatureId, entity.Guid);

                return;
            }

            if (!entity.IsAlive)
            {
                if (profile.TraceCombat)
                    log.LogTrace("Starter tutorial combat AI aggro skipped for creature {CreatureId} guid {Guid}: alive={IsAlive}.", entity.CreatureId, entity.Guid, entity.IsAlive);

                return;
            }

            if (source is not IUnitEntity unit)
            {
                if (profile.TraceCombat)
                    log.LogTrace("Starter tutorial combat AI aggro skipped for creature {CreatureId} guid {Guid}: source {SourceGuid} is not a unit.", entity.CreatureId, entity.Guid, source?.Guid);

                return;
            }

            IUnitEntity visibleUnit = entity.GetVisible<IUnitEntity>(unit.Guid);
            if (visibleUnit == null)
            {
                if (profile.TraceCombat)
                    log.LogTrace("Starter tutorial combat AI aggro skipped for creature {CreatureId} guid {Guid}: source unit {SourceGuid} is not visible.", entity.CreatureId, entity.Guid, unit.Guid);

                return;
            }

            unit = visibleUnit;

            if (!CanAcquireTarget(unit))
            {
                if (profile.TraceCombat)
                    log.LogTrace("Starter tutorial combat AI aggro skipped for creature {CreatureId} guid {Guid}: source unit {SourceGuid} is not an allowed target source.", entity.CreatureId, entity.Guid, unit.Guid);

                return;
            }

            if (requireAggroRange && !CanIdleAggroTarget(unit))
            {
                if (ShouldLogStarterTutorialCombat(unit))
                    log.LogTrace("Starter tutorial combat AI aggro skipped for creature {CreatureId} guid {Guid}: source unit {SourceGuid} is not hostile for idle aggro.", entity.CreatureId, entity.Guid, unit.Guid);

                return;
            }

            if (!entity.CanAttack(unit))
            {
                if (ShouldLogStarterTutorialCombat(unit))
                    log.LogTrace("Starter tutorial combat AI aggro skipped for creature {CreatureId} guid {Guid}: cannot attack unit {TargetGuid}.", entity.CreatureId, entity.Guid, unit.Guid);

                return;
            }

            if (entity.InCombat && (requireAggroRange || entity.TargetGuid != unit.Guid))
            {
                if (ShouldLogStarterTutorialCombat(unit))
                    log.LogTrace("Starter tutorial combat AI aggro skipped for creature {CreatureId} guid {Guid}: already in combat with target {CurrentTargetGuid}.", entity.CreatureId, entity.Guid, entity.TargetGuid);

                return;
            }

            if (!IsWithinLeash(unit))
            {
                if (ShouldLogStarterTutorialCombat(unit))
                {
                    log.LogTrace(
                        "Starter tutorial combat AI aggro skipped for creature {CreatureId} guid {Guid}: unit {TargetGuid} outside leash; leashDistance={LeashDistance}m, leashRange={LeashRange}m, unitPosition=({UnitX}, {UnitY}, {UnitZ}), leashPosition=({LeashX}, {LeashY}, {LeashZ}).",
                        entity.CreatureId,
                        entity.Guid,
                        unit.Guid,
                        GetLeashDistance(unit),
                        GetEffectiveLeashRange(),
                        unit.Position.X,
                        unit.Position.Y,
                        unit.Position.Z,
                        entity.LeashPosition.X,
                        entity.LeashPosition.Y,
                        entity.LeashPosition.Z);
                }

                return;
            }

            if (requireAggroRange && !IsWithinAggroRange(unit))
            {
                if (ShouldLogStarterTutorialCombat(unit))
                {
                    log.LogTrace(
                        "Starter tutorial combat AI aggro skipped for creature {CreatureId} guid {Guid}: unit {TargetGuid} outside aggro range; aggroDistance={AggroDistance}m, aggroRange={AggroRange}m, creaturePosition=({X}, {Y}, {Z}), unitPosition=({UnitX}, {UnitY}, {UnitZ}).",
                        entity.CreatureId,
                        entity.Guid,
                        unit.Guid,
                        GetAggroDistance(unit),
                        GetEffectiveAggroRange(),
                        entity.Position.X,
                        entity.Position.Y,
                        entity.Position.Z,
                        unit.Position.X,
                        unit.Position.Y,
                        unit.Position.Z);
                }

                return;
            }

            if (ShouldLogStarterTutorialCombat(unit))
            {
                log.LogDebug(
                    "Starter tutorial combat AI aggro accepted for creature {CreatureId} guid {Guid}: target {TargetGuid}, requireAggroRange={RequireAggroRange}, aggroDistance={AggroDistance}m, aggroRange={AggroRange}m, leashDistance={LeashDistance}m, leashRange={LeashRange}m.",
                    entity.CreatureId,
                    entity.Guid,
                    unit.Guid,
                    requireAggroRange,
                    GetAggroDistance(unit),
                    GetEffectiveAggroRange(),
                    GetLeashDistance(unit),
                    GetEffectiveLeashRange());
            }

            if (profile.AggroSpell4Id != 0u)
            {
                ISpellParameters spellParameters = spellParametersFactory.Resolve();
                entity.CastSpell(profile.AggroSpell4Id, spellParameters);
            }

            activeChaseTargetGuid = null;
            returningToLeash = false;
            entity.MovementManager.Finalise();
            entity.MovementManager.SetRotationFaceUnit(unit.Guid);

            if (entity.ThreatManager.GetHostile(unit.Guid) == null)
                entity.ThreatManager.UpdateThreat(unit, 1);

            AssistNearbyAllies(unit);
        }

        private void AssistNearbyAllies(IUnitEntity target)
        {
            if (profile.AssistRange <= 0f)
                return;

            foreach (IUnitEntity unit in entity.GetInRange<IUnitEntity>(0u).Where(unit => unit != null).ToList())
            {
                if (unit is not ICreatureEntity ally || ReferenceEquals(ally, entity) || ally.Guid == entity.Guid)
                    continue;

                CombatProfile allyProfile = combatProfileProvider.GetProfile(ally) ?? CombatProfile.Default;
                float assistRange = MathF.Min(profile.AssistRange, allyProfile.AssistRange);
                if (assistRange <= 0f)
                    continue;

                if (!CanAssist(ally, allyProfile, target, assistRange))
                    continue;

                ally.ThreatManager.UpdateThreat(target, 1);

                if (profile.TraceCombat)
                {
                    log.LogDebug(
                        "Profiled combat AI assist accepted: sourceCreature={CreatureId} sourceGuid={Guid}, allyCreature={AllyCreatureId} allyGuid={AllyGuid}, targetGuid={TargetGuid}, assistRange={AssistRange}.",
                        entity.CreatureId,
                        entity.Guid,
                        ally.CreatureId,
                        ally.Guid,
                        target.Guid,
                        assistRange);
                }
            }
        }

        private bool CanAssist(ICreatureEntity ally, CombatProfile allyProfile, IUnitEntity target, float assistRange)
        {
            if (!ally.IsAlive || ally.InCombat)
                return false;

            if (ally.ThreatManager.GetHostile(target.Guid) != null)
                return false;

            if (ally.GetVisible<IUnitEntity>(target.Guid) == null)
                return false;

            if (!SharesFaction(entity, ally))
                return false;

            if (!CanAcquireTarget(allyProfile, target))
                return false;

            if (!ally.CanAttack(target))
                return false;

            if (!IsWithinRange(entity.Position, ally.Position, assistRange))
                return false;

            return IsWithinLeash(ally, allyProfile, target);
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> enters combat.
        /// </summary>
        public virtual void OnEnterCombat()
        {
            // deliberately empty
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> leaves combat.
        /// </summary>
        public virtual void OnLeaveCombat()
        {
            // deliberately empty
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> is killed.
        /// </summary>
        public virtual void OnDeath()
        {
            // deliberately empty
        }

        /// <summary>
        /// Invoked when a new <see cref="IHostileEntity"/> is added to the threat list.
        /// </summary>
        public void OnThreatAddTarget(IHostileEntity hostile)
        {
            if (!combatEnabled)
                return;

            SelectTarget();
        }

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is removed from the threat list.
        /// </summary>
        public void OnThreatRemoveTarget(IHostileEntity hostile)
        {
            if (!combatEnabled)
                return;

            SelectTarget();
        }

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is update on the threat list.
        /// </summary>
        public void OnThreatChange(IHostileEntity hostile)
        {
            if (!combatEnabled)
                return;

            SelectTarget();
        }

        protected virtual void SelectTarget()
        {
            if (selectingTarget)
                return;

            selectingTarget = true;
            try
            {
                IHostileEntity nextHostile = null;
                List<uint> invalidHostiles = [];

                foreach (IHostileEntity hostile in entity.ThreatManager.OrderByDescending(hostile => hostile.Threat))
                {
                    IUnitEntity target = entity.GetVisible<IUnitEntity>(hostile.HatedUnitId);
                    if (target == null || !CanAcquireTarget(target) || !entity.CanAttack(target) || !IsWithinLeash(target))
                    {
                        invalidHostiles.Add(hostile.HatedUnitId);
                        continue;
                    }

                    nextHostile = hostile;
                    break;
                }

                foreach (uint hostileId in invalidHostiles)
                    entity.ThreatManager.RemoveHostile(hostileId);

                if (nextHostile == null)
                {
                    Reset();
                    return;
                }

                if (entity.TargetGuid == nextHostile.HatedUnitId)
                    return;

                entity.SetTarget(nextHostile.HatedUnitId, nextHostile.Threat);
            }
            finally
            {
                selectingTarget = false;
            }
        }

        private bool ValidateCurrentTarget()
        {
            IUnitEntity target = GetCurrentVisibleTarget();
            if (target != null && CanAcquireTarget(target) && entity.CanAttack(target) && IsWithinLeash(target))
                return true;

            IUnitEntity mapTarget = entity.Map.GetEntity<IUnitEntity>(entity.TargetGuid.Value);
            if (ShouldLogStarterTutorialCombat(target))
            {
                log.LogDebug(
                    "Starter tutorial combat AI target invalid for creature {CreatureId} guid {Guid}: targetGuid={TargetGuid}, targetFound={TargetFound}, canAttack={CanAttack}, withinLeash={WithinLeash}.",
                    entity.CreatureId,
                    entity.Guid,
                    entity.TargetGuid.Value,
                    mapTarget != null,
                    mapTarget != null && entity.CanAttack(mapTarget),
                    mapTarget != null && IsWithinLeash(mapTarget));
            }

            if (target != null)
                entity.ThreatManager.RemoveHostile(target.Guid);
            else
                SelectTarget();

            return false;
        }

        private void Reset()
        {
            activeChaseTargetGuid = null;
            specialCastLockoutSeconds = 0d;

            IUnitEntity previousTarget = entity.TargetGuid.HasValue
                ? entity.Map.GetEntity<IUnitEntity>(entity.TargetGuid.Value)
                : null;

            entity.SetTarget((IWorldEntity)null);
            entity.ThreatManager.ClearThreatList();

            entity.ModifyHealth(entity.MaxHealth, DamageType.Heal, null);

            if (previousTarget is IPlayer player)
            {
                player.Session.EnqueueMessageEncrypted(new ServerGenericFloaterLocalised
                {
                    LocalisedTextId = 0x5F95C
                });
            }

            if (!IsFinite(entity.Position))
            {
                if (IsFinite(entity.LeashPosition))
                    entity.MovementManager.SetPosition(entity.LeashPosition, true);
                else
                    entity.MovementManager.Finalise();

                return;
            }

            float speed = entity.GetPropertyValue(Property.MoveSpeedMultiplier) * 10f;
            if (float.IsFinite(speed)
                && speed > 0f
                && IsFinite(entity.Position)
                && IsFinite(entity.LeashPosition)
                && Vector3.DistanceSquared(entity.Position, entity.LeashPosition) > float.Epsilon)
            {
                returningToLeash = true;
                entity.MovementManager.LaunchPath(entity.LeashPosition, speed, SplineMode.OneShot);
            }
            else
            {
                returningToLeash = false;
                entity.MovementManager.Finalise();
                TryResumePatrolSpline();
            }
        }

        private void TryResumePatrolSpline()
        {
            returningToLeash = false;

            if (entity.Spline == null)
                return;

            if (!SplineAI.TryResolveSpline(entity.Spline.Mode, entity.Spline.Speed, out SplineMode mode, out float speed))
                return;

            entity.MovementManager.SetMode(ModeType.Walk);
            entity.MovementManager.LaunchSpline(entity.Spline.SplineId, mode, speed, false);
        }

        private IUnitEntity GetCurrentVisibleTarget()
        {
            return entity.TargetGuid.HasValue
                ? entity.GetVisible<IUnitEntity>(entity.TargetGuid.Value)
                : null;
        }

        private float GetEffectiveLeashRange()
        {
            if (profile.MinimumLeashRange.HasValue)
                return Math.Max(entity.LeashRange, profile.MinimumLeashRange.Value);

            return entity.LeashRange;
        }

        private static bool IsWithinLeash(IWorldEntity owner, CombatProfile ownerProfile, IUnitEntity target)
        {
            float effectiveLeashRange = ownerProfile.MinimumLeashRange.HasValue
                ? Math.Max(owner.LeashRange, ownerProfile.MinimumLeashRange.Value)
                : owner.LeashRange;

            return IsWithinRange(owner.LeashPosition, target.Position, effectiveLeashRange);
        }

        private bool CanAcquireTarget(IUnitEntity target)
        {
            return target != null
                && target.Guid != entity.Guid
                && CanAcquireTarget(profile, target);
        }

        private bool CanIdleAggroTarget(IUnitEntity target)
        {
            return CanAcquireTarget(target)
                && entity.GetDispositionTo(target.Faction1) == Disposition.Hostile;
        }

        private static bool CanAcquireTarget(CombatProfile ownerProfile, IUnitEntity target)
        {
            return target != null
                && (ownerProfile.AllowNonPlayerTargets || target is IPlayer);
        }

        private static bool IsWithinRange(Vector3 source, Vector3 target, float range)
        {
            if (range < 0f)
                return false;

            float distance = Vector2.Distance(
                new Vector2(source.X, source.Z),
                new Vector2(target.X, target.Z));
            return float.IsFinite(distance) && distance <= range;
        }

        private static bool SharesFaction(IWorldEntity first, IWorldEntity second)
        {
            return SharesFaction(first.Faction1, second.Faction1)
                || SharesFaction(first.Faction1, second.Faction2)
                || SharesFaction(first.Faction2, second.Faction1)
                || SharesFaction(first.Faction2, second.Faction2);
        }

        private static bool SharesFaction(Faction first, Faction second)
        {
            return first != Faction.None && first == second;
        }

        private bool ShouldLogStarterTutorialCombat(IUnitEntity unit = null)
        {
            return profile.TraceCombat
                && (unit == null || unit is IPlayer);
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.X)
                && float.IsFinite(value.Y)
                && float.IsFinite(value.Z);
        }
    }
}
