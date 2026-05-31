using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Abstract.Spell.Event;
using NexusForever.Game.Combat.CrowdControl;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Quest;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Spell.Event;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Spell
{
    public partial class Spell : ISpell
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const uint ValidTargetObjectMask = 0x02u;
        private const uint ValidTargetDeadMask = 0x08u;
        private const uint TargetTypeTargetAoe = 3u;
        private const uint TargetTypePositionAoe = 4u;

        public ISpellParameters Parameters { get; }
        public uint CastingId { get; }
        public bool IsCasting => status == SpellStatus.Casting;
        public bool BlocksCasting => status == SpellStatus.Casting || (status == SpellStatus.Executing && awaitingInitialImpact);
        public bool IsFinished => status == SpellStatus.Finished;

        public IUnitEntity Caster { get; }

        private SpellStatus status;
        private bool cancelled;
        private bool awaitingInitialImpact;

        private readonly List<ISpellTargetInfo> targets = new();
        private readonly List<ITelegraph> telegraphs = new();
        private readonly List<PendingSpellGoEffect> pendingSpellGoEffects = new();
        private readonly Dictionary<ISpellTargetEffectInfo, ISpellEvent> lifetimeEvents = new();
        private readonly HashSet<(uint TargetId, uint EffectEntryId)> terminatedPersistentEffects = new();

        private readonly ISpellEventManager events = new SpellEventManager();

        private IScriptCollection scriptCollection;

        private sealed record PendingSpellGoEffect(ISpellTargetInfo TargetInfo, ISpellTargetEffectInfo EffectInfo);

        public Spell(IUnitEntity caster, ISpellParameters parameters)
        {
            Caster     = caster;
            Parameters = parameters;
            CastingId  = GlobalSpellManager.Instance.NextCastingId;
            status     = SpellStatus.Initiating;

            parameters.RootSpellInfo ??= parameters.SpellInfo;

            scriptCollection = ScriptManager.Instance.InitialiseOwnedScripts<ISpell>(this, parameters.SpellInfo.Entry.Id);
        }

        public void Dispose()
        {
            SpellRuntimeEvidenceCollector.FinalizeAndExport(this, status == SpellStatus.Finished ? "disposed-finished" : "disposed");

            if (scriptCollection != null)
                ScriptManager.Instance.Unload(scriptCollection);

            scriptCollection = null;
        }

        public void Update(double lastTick)
        {
            scriptCollection.Invoke<IUpdate>(s => s.Update(lastTick));

            events.Update(lastTick);
            UpdatePersistence();

            if (status == SpellStatus.Executing && !events.HasPendingEvent)
            {
                // spell effects have finished executing
                status = SpellStatus.Finished;
                log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} has finished.");

                scriptCollection.Invoke<ISpellScript>(s => s.OnFinish(this, cancelled));
                SendSpellFinish();
            }

            if (status == SpellStatus.Finished)
                SpellRuntimeEvidenceCollector.FinalizeAndExport(this, "finished");
        }

        /// <summary>
        /// Begin cast, checking prerequisites before initiating.
        /// </summary>
        public CastResult Cast()
        {
            if (status != SpellStatus.Initiating)
                throw new InvalidOperationException();

            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} has started initating.");

            CastResult result = CheckCast();
            if (result != CastResult.Ok)
            {
                SpellRuntimeEvidenceCollector.RecordCastAttempt(this, result);
                SendSpellCastResult(result);
                status = SpellStatus.Finished;
                SpellRuntimeEvidenceCollector.FinalizeAndExport(this, "cast-blocked");
                return result;
            }

            if (!TryConsumeServiceTokenCost())
            {
                SpellRuntimeEvidenceCollector.RecordCastAttempt(this, CastResult.ServiceTokensInsufficentFunds);
                SendSpellCastResult(CastResult.ServiceTokensInsufficentFunds);
                status = SpellStatus.Finished;
                SpellRuntimeEvidenceCollector.FinalizeAndExport(this, "service-token-blocked");
                return CastResult.ServiceTokensInsufficentFunds;
            }

            if (Caster is IPlayer player)
                if (!Parameters.IgnoreGlobalCooldown && Parameters.SpellInfo.GlobalCooldown != null)
                    player.SpellManager.SetGlobalSpellCooldown(Parameters.SpellInfo.GlobalCooldown.CooldownTime / 1000d);

            if (Caster is not IPlayer)
                InitialiseTelegraphs();

            scriptCollection.Invoke<ISpellScript>(s => s.OnCast(this));
            SendSpellStart();

            // enqueue spell to be executed after cast time
            events.EnqueueEvent(new SpellEvent(Parameters.SpellInfo.Entry.CastTime / 1000d, Execute));
            status = SpellStatus.Casting;
            SpellRuntimeEvidenceCollector.RecordCastAttempt(this, CastResult.Ok);

            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} has started casting.");
            return CastResult.Ok;
        }

        CastResult ISpell.Cast() => Cast();

        private CastResult CheckCast()
        {
            CastResult preReqCheck = CheckPrerequisites();
            if (preReqCheck != CastResult.Ok)
                return preReqCheck;

            CastResult ccResult = CheckCCConditions();
            if (ccResult != CastResult.Ok)
                return ccResult;

            CastResult activeCCRestriction = CheckActiveCrowdControlRestrictions();
            if (activeCCRestriction != CastResult.Ok)
                return activeCCRestriction;

            if (Caster is IPlayer player)
            {
                if (player.SpellManager.GetSpellCooldown(Parameters.SpellInfo.Entry.Id) > 0d)
                    return CastResult.SpellCooldown;

                // this isn't entirely correct, research GlobalCooldownEnum
                if (!Parameters.IgnoreGlobalCooldown
                    && Parameters.SpellInfo.Entry.GlobalCooldownEnum == 0
                    && player.SpellManager.GetGlobalSpellCooldown() > 0d)
                    return CastResult.SpellGlobalCooldown;

                if (Parameters.CharacterSpell?.MaxAbilityCharges > 0 && Parameters.CharacterSpell?.AbilityCharges == 0)
                    return CastResult.SpellNoCharges;

                CastResult serviceTokenCostResult = CheckServiceTokenCost(player);
                if (serviceTokenCostResult != CastResult.Ok)
                    return serviceTokenCostResult;
            }

            CastResult targetResult = CheckPrimaryTarget();
            if (targetResult != CastResult.Ok)
                return targetResult;

            return CastResult.Ok;
        }

        private CastResult CheckServiceTokenCost(IPlayer player)
        {
            if (!Parameters.UseServiceTokenCost)
                return CastResult.Ok;

            if (!Parameters.SpellInfo.HasServiceTokenCost || Parameters.SpellInfo.ServiceTokenCostEntry == null)
                return CastResult.ServiceTokensInsufficentFunds;

            uint serviceTokenCost = Parameters.SpellInfo.ServiceTokenCostEntry.ServiceTokenCost;
            return player.Account.CurrencyManager.CanAfford(AccountCurrencyType.ServiceToken, serviceTokenCost)
                ? CastResult.Ok
                : CastResult.ServiceTokensInsufficentFunds;
        }

        private bool TryConsumeServiceTokenCost()
        {
            if (!Parameters.UseServiceTokenCost)
                return true;

            if (Caster is not IPlayer player)
                return false;

            uint serviceTokenCost = Parameters.SpellInfo.ServiceTokenCostEntry?.ServiceTokenCost ?? 0u;
            if (serviceTokenCost == 0u)
                return true;

            if (!player.Account.CurrencyManager.CanAfford(AccountCurrencyType.ServiceToken, serviceTokenCost))
                return false;

            player.Account.CurrencyManager.CurrencySubtractAmount(AccountCurrencyType.ServiceToken, serviceTokenCost);
            return true;
        }

        private CastResult CheckPrimaryTarget()
        {
            if (Parameters.PrimaryTargetId == 0u)
                return CastResult.Ok;

            IWorldEntity target = GetPrimaryTargetWorldEntity();
            if (target == null)
            {
                SpellEffectDiagnostics.TracePrimaryTargetValidation(this, null, CastResult.TargetUnknown, 0f, 0f, 0f);
                return CastResult.TargetUnknown;
            }

            float horizontalRange = GetHorizontalDistance(Caster.Position, target.Position);
            float effectiveRange = GetEffectivePrimaryTargetRange(horizontalRange, target);
            float verticalDelta = MathF.Abs(Caster.Position.Y - target.Position.Y);

            CastResult result = CheckPrimaryTargetValidMask(target);
            if (result == CastResult.Ok)
                result = CheckPrimaryTargetCastGroup(target);

            if (result == CastResult.Ok)
                result = CheckPrimaryTargetAngle(target);

            if (result == CastResult.Ok)
                result = CheckPrimaryTargetRange(horizontalRange, effectiveRange, verticalDelta);

            SpellEffectDiagnostics.TracePrimaryTargetValidation(this, target.Guid, result, horizontalRange, effectiveRange, verticalDelta);
            return result;
        }

        private float GetEffectivePrimaryTargetRange(float horizontalRange, IWorldEntity target)
        {
            float targetRadius = target is IUnitEntity unitTarget
                ? unitTarget.HitRadius * 0.5f
                : 0f;

            return MathF.Max(0f, horizontalRange - Caster.HitRadius * 0.5f - targetRadius);
        }

        private CastResult CheckPrimaryTargetValidMask(IWorldEntity target)
        {
            if (target is not IUnitEntity unitTarget)
                // Current verified non-unit target bit: 0x02 = interactable/world-object target.
                return ((Parameters.SpellInfo.BaseInfo.ValidTargets?.TargetBitmask ?? 0u) & ValidTargetObjectMask) != 0u
                    ? CastResult.Ok
                    : CastResult.TargetUnknown;

            CastResult result = CheckTargetLivingState(unitTarget);
            if (result != CastResult.Ok)
                return result;

            return IsHostileToTarget(unitTarget) && UnitStateSetRules.TryGetHostileEffectImmuneState(unitTarget, out _)
                ? CastResult.TargetInvulnerable
                : CastResult.Ok;
        }

        private CastResult CheckPrimaryTargetCastGroup(IWorldEntity target)
        {
            TargetGroupEntry castGroup = Parameters.SpellInfo.BaseInfo.CastGroup;
            if (castGroup == null)
                return CastResult.Ok;

            return TargetGroupCriteriaEvaluator.Evaluate(castGroup, target, GameTableManager.Instance)
                ? CastResult.Ok
                : CastResult.TargetUnknown;
        }

        private bool IsHostileToTarget(IUnitEntity target)
        {
            return Caster.GetDispositionTo(target.Faction1) < Disposition.Friendly;
        }

        private CastResult CheckTargetLivingState(IUnitEntity target)
        {
            uint validTargetMask = Parameters.SpellInfo.BaseInfo.ValidTargets?.TargetBitmask ?? 0u;
            if (validTargetMask == 0u)
                return CastResult.Ok;

            bool allowsDeadTargets = (validTargetMask & ValidTargetDeadMask) != 0u;
            bool allowsLivingTargets = (validTargetMask & ~(ValidTargetDeadMask | ValidTargetObjectMask)) != 0u;

            if (!allowsDeadTargets && !allowsLivingTargets)
                return CastResult.TargetUnknown;

            if (!target.IsAlive && !allowsDeadTargets)
                return CastResult.TargetCannotBeDead;

            if (target.IsAlive && allowsDeadTargets && !allowsLivingTargets)
                return CastResult.TargetMustBeDead;

            return CastResult.Ok;
        }

        private bool IsTargetLivingStateAllowed(IUnitEntity target)
        {
            return CheckTargetLivingState(target) == CastResult.Ok;
        }

        private CastResult CheckPrimaryTargetAngle(IWorldEntity target)
        {
            float targetAngle = Parameters.SpellInfo.BaseInfo.TargetAngle?.TargetAngle ?? 0f;
            if (!ShouldApplyPrimaryTargetAngle(Caster.Guid, target.Guid, targetAngle))
                return CastResult.Ok;

            return IsWithinCasterAngle(target, targetAngle)
                ? CastResult.Ok
                : CastResult.TargetOrientation;
        }

        internal static bool ShouldApplyPrimaryTargetAngle(uint casterGuid, uint targetGuid, float targetAngle)
        {
            if (targetAngle <= 0f || targetAngle >= 360f)
                return false;

            return casterGuid != targetGuid;
        }

        private CastResult CheckPrimaryTargetRange(float horizontalRange, float effectiveRange, float verticalDelta)
        {
            Spell4Entry entry = Parameters.SpellInfo.Entry;
            if (entry.TargetMinRange > 0f && horizontalRange < entry.TargetMinRange)
                return CastResult.TargetRangeMin;

            if (entry.TargetMaxRange > 0f && effectiveRange > entry.TargetMaxRange)
                return CastResult.TargetRangeMax;

            if (entry.TargetVerticalRange > 0f && verticalDelta > entry.TargetVerticalRange)
                return CastResult.TargetRangeVertical;

            return CastResult.Ok;
        }

        private CastResult CheckPrerequisites()
        {
            if (Caster is not IPlayer player)
                return CastResult.Ok;

            if (Parameters.SpellInfo.CasterCastPrerequisite != null && !CheckRunnerOverride(player))
            {
                if (!PrerequisiteManager.Instance.Meets(player, Parameters.SpellInfo.CasterCastPrerequisite.Id))
                    return CastResult.PrereqCasterCast;
            }

            // not sure if this should be for explicit and/or implicit targets
            if (Parameters.SpellInfo.TargetCastPrerequisites != null)
            {
            }

            // this probably isn't the correct place, name implies this should be constantly checked
            if (Parameters.SpellInfo.CasterPersistencePrerequisites != null)
            {
            }

            if (Parameters.SpellInfo.TargetPersistencePrerequisites != null)
            {
            }

            return CastResult.Ok;
        }

        private bool CheckRunnerOverride(IPlayer player)
        {
            foreach (PrerequisiteEntry runnerPrereq in Parameters.SpellInfo.PrerequisiteRunners)
                if (PrerequisiteManager.Instance.Meets(player, runnerPrereq.Id))
                    return true;

            return false;
        }

        private void UpdatePersistence()
        {
            if (status != SpellStatus.Executing)
                return;

            foreach (SpellTargetInfo targetInfo in targets.OfType<SpellTargetInfo>())
            {
                foreach (ISpellTargetEffectInfo info in targetInfo.Effects.ToArray())
                {
                    if (info.DropEffect || info.LifetimeEnded)
                        continue;

                    SpellEffectInterpretation effect = SpellEffectInterpreter.Interpret(info.Entry);
                    if (!HasPersistencePrerequisites(effect) || MeetsPersistencePrerequisites(effect, targetInfo.Entity))
                        continue;

                    terminatedPersistentEffects.Add((targetInfo.Entity.Guid, effect.Entry.Id));
                    CancelLifetimeEvent(info);
                    if (!TryRemoveActiveEffect(effect, targetInfo.Entity, info))
                        info.LifetimeEnded = true;
                }
            }
        }

        private bool HasPersistencePrerequisites(SpellEffectInterpretation effect)
        {
            return Parameters.SpellInfo.CasterPersistencePrerequisites != null
                || Parameters.SpellInfo.TargetPersistencePrerequisites != null
                || effect.Entry.PrerequisiteIdCasterPersistence != 0u
                || effect.Entry.PrerequisiteIdTargetPersistence != 0u;
        }

        private bool MeetsApplyPrerequisites(SpellEffectInterpretation effect, IWorldEntity target)
        {
            return MeetsApplyPrerequisite(GetPrerequisite(effect.Entry.PrerequisiteIdCasterApply), Caster)
                && MeetsApplyPrerequisite(GetPrerequisite(effect.Entry.PrerequisiteIdTargetApply), target);
        }

        private static PrerequisiteEntry GetPrerequisite(uint prerequisiteId)
        {
            return prerequisiteId == 0u ? null : GameTableManager.Instance.Prerequisite.GetEntry(prerequisiteId);
        }

        internal static bool MeetsApplyPrerequisite(PrerequisiteEntry prerequisite, IWorldEntity entity)
        {
            if (prerequisite == null)
                return true;

            if (TryEvaluateCreatureDifficultyPrerequisite(prerequisite, entity, out bool result))
                return result;

            if (entity is not IPlayer player)
                return true;

            return PrerequisiteManager.Instance.Meets(player, prerequisite.Id);
        }

        private static bool TryEvaluateCreatureDifficultyPrerequisite(PrerequisiteEntry prerequisite, IWorldEntity entity, out bool result)
        {
            result = false;

            if (prerequisite.PrerequisiteTypeId == null)
                return false;

            if (prerequisite.PrerequisiteTypeId.Any(t => t != PrerequisiteType.None && t != PrerequisiteType.CreatureDifficulty))
                return false;

            uint creatureDifficultyId = entity?.CreatureInfo?.DifficultyEntry?.Id ?? entity?.CreatureEntry?.Creature2DifficultyId ?? 0u;
            if (creatureDifficultyId == 0u)
            {
                result = true;
                return true;
            }

            result = prerequisite.Flags switch
            {
                EvaluationMode.EvaluateAND => EvaluateCreatureDifficultyAnd(prerequisite, creatureDifficultyId),
                EvaluationMode.EvaluateOR  => EvaluateCreatureDifficultyOr(prerequisite, creatureDifficultyId),
                _                          => false
            };
            return true;
        }

        private static bool EvaluateCreatureDifficultyAnd(PrerequisiteEntry prerequisite, uint creatureDifficultyId)
        {
            for (int i = 0; i < prerequisite.PrerequisiteTypeId.Length; i++)
            {
                if (prerequisite.PrerequisiteTypeId[i] == PrerequisiteType.None)
                    continue;

                if (!ComparePrerequisiteValue(creatureDifficultyId, prerequisite.PrerequisiteComparisonId[i], prerequisite.ObjectId[i]))
                    return false;
            }

            return true;
        }

        private static bool EvaluateCreatureDifficultyOr(PrerequisiteEntry prerequisite, uint creatureDifficultyId)
        {
            for (int i = 0; i < prerequisite.PrerequisiteTypeId.Length; i++)
            {
                if (prerequisite.PrerequisiteTypeId[i] == PrerequisiteType.None)
                    continue;

                if (ComparePrerequisiteValue(creatureDifficultyId, prerequisite.PrerequisiteComparisonId[i], prerequisite.ObjectId[i]))
                    return true;
            }

            return false;
        }

        private static bool ComparePrerequisiteValue(uint currentValue, PrerequisiteComparison comparison, uint expectedValue)
        {
            return comparison switch
            {
                PrerequisiteComparison.Equal              => currentValue == expectedValue,
                PrerequisiteComparison.NotEqual           => currentValue != expectedValue,
                PrerequisiteComparison.GreaterThanOrEqual => currentValue >= expectedValue,
                PrerequisiteComparison.GreaterThan        => currentValue > expectedValue,
                PrerequisiteComparison.LessThanOrEqual    => currentValue <= expectedValue,
                PrerequisiteComparison.LessThan           => currentValue < expectedValue,
                _                                         => false
            };
        }

        private bool MeetsPersistencePrerequisites(SpellEffectInterpretation effect, IWorldEntity target)
        {
            return MeetsPersistencePrerequisite(Parameters.SpellInfo.CasterPersistencePrerequisites, Caster)
                && MeetsPersistencePrerequisite(Parameters.SpellInfo.TargetPersistencePrerequisites, target)
                && MeetsPersistencePrerequisite(GameTableManager.Instance.Prerequisite.GetEntry(effect.Entry.PrerequisiteIdCasterPersistence), Caster)
                && MeetsPersistencePrerequisite(GameTableManager.Instance.Prerequisite.GetEntry(effect.Entry.PrerequisiteIdTargetPersistence), target);
        }

        private static bool MeetsPersistencePrerequisite(PrerequisiteEntry prerequisite, IWorldEntity entity)
        {
            if (prerequisite == null)
                return true;

            if (entity is not IPlayer player)
                return true;

            return PrerequisiteManager.Instance.Meets(player, prerequisite.Id);
        }

        private CastResult CheckCCConditions()
        {
            if (Parameters.SpellInfo.CasterCCConditions != null)
            {
                CastResult result = CheckCCConditions(Parameters.SpellInfo.CasterCCConditions, Caster, true);
                if (result != CastResult.Ok)
                    return result;
            }

            // not sure if this should be for explicit and/or implicit targets
            if (Parameters.SpellInfo.TargetCCConditions != null && Parameters.PrimaryTargetId != 0u)
            {
                IUnitEntity target = GetPrimaryTargetEntity();
                if (target != null)
                {
                    CastResult result = CheckCCConditions(Parameters.SpellInfo.TargetCCConditions, target, false);
                    if (result != CastResult.Ok)
                        return result;
                }
            }

            return CastResult.Ok;
        }

        private CastResult CheckActiveCrowdControlRestrictions()
        {
            uint activeMask = Caster.ActiveCCStateMask;
            if (activeMask == 0u)
                return CastResult.Ok;

            foreach (CCState state in Enum.GetValues<CCState>())
            {
                uint stateMask = 1u << (int)state;
                if ((activeMask & stateMask) == 0u)
                    continue;

                if (IsCasterCrowdControlStateExplicitlyAllowed(state))
                    continue;

                if (!CrowdControlStateRules.BlocksCasting(state, Parameters.SpellInfo.BaseInfo.School))
                    continue;

                return GetCasterCannotBeCCResult(state);
            }

            return CastResult.Ok;
        }

        private bool IsCasterCrowdControlStateExplicitlyAllowed(CCState state)
        {
            Spell4CCConditionsEntry conditions = Parameters.SpellInfo.CasterCCConditions;
            if (conditions == null)
                return false;

            uint stateMask = 1u << (int)state;
            return (conditions.CcStateMask & stateMask) != 0u
                && (conditions.CcStateFlagsRequired & stateMask) != 0u;
        }

        private static CastResult CheckCCConditions(Spell4CCConditionsEntry conditions, IUnitEntity unit, bool caster)
        {
            uint conditionMask = conditions.CcStateMask;
            if (conditionMask == 0u)
                return CastResult.Ok;

            uint activeMask = unit.ActiveCCStateMask & conditionMask;
            uint requiredMask = conditions.CcStateFlagsRequired & conditionMask;
            if (activeMask == requiredMask)
                return CastResult.Ok;

            bool missingRequiredState = (activeMask & requiredMask) != requiredMask;
            uint failedMask = missingRequiredState
                ? requiredMask & ~activeMask
                : activeMask & ~requiredMask;

            CCState? state = GetFirstCCState(failedMask == 0u ? conditionMask : failedMask);
            if (state == null)
                return caster ? CastResult.CasterNotIncontrolOfSelf : CastResult.FailSpecialRestrictions;

            return GetCCConditionCastResult(state.Value, caster, !missingRequiredState);
        }

        private static CCState? GetFirstCCState(uint mask)
        {
            for (int i = 0; i < 32; i++)
                if ((mask & (1u << i)) != 0u)
                    return (CCState)i;

            return null;
        }

        private static CastResult GetCCConditionCastResult(CCState state, bool caster, bool cannotBe)
        {
            if (caster)
                return cannotBe
                    ? GetCasterCannotBeCCResult(state)
                    : GetCasterMustBeCCResult(state);

            return cannotBe
                ? GetTargetCannotBeCCResult(state)
                : GetTargetMustBeCCResult(state);
        }

        private static CastResult GetCasterCannotBeCCResult(CCState state)
        {
            return state switch
            {
                CCState.Stun               => CastResult.CasterCannotBeStun,
                CCState.Sleep              => CastResult.CasterCannotBeSleep,
                CCState.Root               => CastResult.CasterCannotBeRoot,
                CCState.Disarm             => CastResult.CasterCannotBeDisarm,
                CCState.Silence            => CastResult.CasterCannotBeSilence,
                CCState.Polymorph          => CastResult.CasterCannotBePolymorph,
                CCState.Fear               => CastResult.CasterCannotBeFear,
                CCState.Hold               => CastResult.CasterCannotBeHold,
                CCState.Knockdown          => CastResult.CasterCannotBeKnockdown,
                CCState.Vulnerability      => CastResult.CasterCannotBeVulnerability,
                CCState.VulnerabilityWithAct => CastResult.CCVulnerabilityWithAct,
                CCState.Disorient          => CastResult.CasterCannotBeDisorient,
                CCState.Disable            => CastResult.CasterCannotBeDisable,
                CCState.Taunt              => CastResult.CasterCannotBeTaunt,
                CCState.DeTaunt            => CastResult.CasterCannotBeDeTaunt,
                CCState.Blind              => CastResult.CasterCannotBeBlind,
                CCState.Knockback          => CastResult.CasterCannotBeKnockback,
                CCState.Pushback           => CastResult.CasterCannotBePushback,
                CCState.Pull               => CastResult.CasterCannotBePull,
                CCState.PositionSwitch     => CastResult.CasterCannotBePositionSwitch,
                CCState.Tether             => CastResult.CasterCannotBeTether,
                CCState.Snare              => CastResult.CasterCannotBeSnare,
                CCState.Interrupt          => CastResult.CasterCannotBeInterrupt,
                CCState.Daze               => CastResult.CasterCannotBeDaze,
                CCState.Subdue             => CastResult.CasterCannotBeSubdue,
                CCState.Grounded           => CastResult.CasterCannotBeGrounded,
                CCState.DisableCinematic   => CastResult.CasterCannotBeDisableCinematic,
                CCState.AbilityRestriction => CastResult.CasterCannotBeAbilityRestriction,
                _                          => CastResult.CasterNotIncontrolOfSelf
            };
        }

        private static CastResult GetCasterMustBeCCResult(CCState state)
        {
            return state switch
            {
                CCState.Stun               => CastResult.CasterMustBeStun,
                CCState.Sleep              => CastResult.CasterMustBeSleep,
                CCState.Root               => CastResult.CasterMustBeRoot,
                CCState.Disarm             => CastResult.CasterMustBeDisarm,
                CCState.Silence            => CastResult.CasterMustBeSilence,
                CCState.Polymorph          => CastResult.CasterMustBePolymorph,
                CCState.Fear               => CastResult.CasterMustBeFear,
                CCState.Hold               => CastResult.CasterMustBeHold,
                CCState.Knockdown          => CastResult.CasterMustBeKnockdown,
                CCState.Vulnerability      => CastResult.CasterMustBeVulnerability,
                CCState.Disorient          => CastResult.CasterMustBeDisorient,
                CCState.Disable            => CastResult.CasterMustBeDisable,
                CCState.Taunt              => CastResult.CasterMustBeTaunt,
                CCState.DeTaunt            => CastResult.CasterMustBeDeTaunt,
                CCState.Blind              => CastResult.CasterMustBeBlind,
                CCState.Knockback          => CastResult.CasterMustBeKnockback,
                CCState.Pushback           => CastResult.CasterMustBePushback,
                CCState.Pull               => CastResult.CasterMustBePull,
                CCState.PositionSwitch     => CastResult.CasterMustBePositionSwitch,
                CCState.Tether             => CastResult.CasterMustBeTether,
                CCState.Snare              => CastResult.CasterMustBeSnare,
                CCState.Interrupt          => CastResult.CasterMustBeInterrupt,
                CCState.Daze               => CastResult.CasterMustBeDaze,
                CCState.Subdue             => CastResult.CasterMustBeSubdue,
                CCState.Grounded           => CastResult.CasterMustBeGrounded,
                CCState.DisableCinematic   => CastResult.CasterMustBeDisableCinematic,
                CCState.AbilityRestriction => CastResult.CasterMustBeAbilityRestriction,
                _                          => CastResult.FailSpecialRestrictions
            };
        }

        private static CastResult GetTargetCannotBeCCResult(CCState state)
        {
            return state switch
            {
                CCState.Stun               => CastResult.TargetCannotBeStun,
                CCState.Sleep              => CastResult.TargetCannotBeSleep,
                CCState.Root               => CastResult.TargetCannotBeRoot,
                CCState.Disarm             => CastResult.TargetCannotBeDisarm,
                CCState.Silence            => CastResult.TargetCannotBeSilence,
                CCState.Polymorph          => CastResult.TargetCannotBePolymorph,
                CCState.Fear               => CastResult.TargetCannotBeFear,
                CCState.Hold               => CastResult.TargetCannotBeHold,
                CCState.Knockdown          => CastResult.TargetCannotBeKnockdown,
                CCState.Vulnerability      => CastResult.TargetCannotBeVulnerability,
                CCState.VulnerabilityWithAct => CastResult.CCVulnerabilityWithAct,
                CCState.Disorient          => CastResult.TargetCannotBeDisorient,
                CCState.Disable            => CastResult.TargetCannotBeDisable,
                CCState.Taunt              => CastResult.TargetCannotBeTaunt,
                CCState.DeTaunt            => CastResult.TargetCannotBeDeTaunt,
                CCState.Blind              => CastResult.TargetCannotBeBlind,
                CCState.Knockback          => CastResult.TargetCannotBeKnockback,
                CCState.Pushback           => CastResult.TargetCannotBePushback,
                CCState.Pull               => CastResult.TargetCannotBePull,
                CCState.PositionSwitch     => CastResult.TargetCannotBePositionSwitch,
                CCState.Tether             => CastResult.TargetCannotBeTether,
                CCState.Snare              => CastResult.TargetCannotBeSnare,
                CCState.Interrupt          => CastResult.TargetCannotBeInterrupt,
                CCState.Daze               => CastResult.TargetCannotBeDaze,
                CCState.Subdue             => CastResult.TargetCannotBeSubdue,
                CCState.Grounded           => CastResult.TargetCannotBeGrounded,
                CCState.DisableCinematic   => CastResult.TargetCannotBeDisableCinematic,
                CCState.AbilityRestriction => CastResult.TargetCannotBeAbilityRestriction,
                _                          => CastResult.FailSpecialRestrictions
            };
        }

        private static CastResult GetTargetMustBeCCResult(CCState state)
        {
            return state switch
            {
                CCState.Stun               => CastResult.TargetMustBeStun,
                CCState.Sleep              => CastResult.TargetMustBeSleep,
                CCState.Root               => CastResult.TargetMustBeRoot,
                CCState.Disarm             => CastResult.TargetMustBeDisarm,
                CCState.Silence            => CastResult.TargetMustBeSilence,
                CCState.Polymorph          => CastResult.TargetMustBePolymorph,
                CCState.Fear               => CastResult.TargetMustBeFear,
                CCState.Hold               => CastResult.TargetMustBeHold,
                CCState.Knockdown          => CastResult.TargetMustBeKnockdown,
                CCState.Vulnerability      => CastResult.TargetMustBeVulnerability,
                CCState.Disorient          => CastResult.TargetMustBeDisorient,
                CCState.Disable            => CastResult.TargetMustBeDisable,
                CCState.Taunt              => CastResult.TargetMustBeTaunt,
                CCState.DeTaunt            => CastResult.TargetMustBeDeTaunt,
                CCState.Blind              => CastResult.TargetMustBeBlind,
                CCState.Knockback          => CastResult.TargetMustBeKnockback,
                CCState.Pushback           => CastResult.TargetMustBePushback,
                CCState.Pull               => CastResult.TargetMustBePull,
                CCState.PositionSwitch     => CastResult.TargetMustBePositionSwitch,
                CCState.Tether             => CastResult.TargetMustBeTether,
                CCState.Snare              => CastResult.TargetMustBeSnare,
                CCState.Interrupt          => CastResult.TargetMustBeInterrupt,
                CCState.Daze               => CastResult.TargetMustBeDaze,
                CCState.Subdue             => CastResult.TargetMustBeSubdue,
                CCState.Grounded           => CastResult.TargetMustBeGrounded,
                CCState.DisableCinematic   => CastResult.TargetMustBeDisableCinematic,
                CCState.AbilityRestriction => CastResult.TargetMustBeAbilityRestriction,
                _                          => CastResult.FailSpecialRestrictions
            };
        }

        private void InitialiseTelegraphs()
        {
            telegraphs.Clear();
            (Vector3 position, Vector3 rotation) = ResolveTelegraphAnchor();

            foreach (TelegraphDamageEntry telegraphDamageEntry in Parameters.SpellInfo.Telegraphs)
                telegraphs.Add(new Telegraph(telegraphDamageEntry, Caster, position, rotation));
        }

        /// <summary>
        /// Cancel cast with supplied <see cref="CastResult"/>.
        /// </summary>
        public void CancelCast(CastResult result)
        {
            if (!BlocksCasting)
                throw new InvalidOperationException();

            SpellRuntimeEvidenceCollector.RecordCancellation(this, result);

            if (Caster is IPlayer player && !player.IsLoading)
            {
                player.Session.EnqueueMessageEncrypted(new ServerSpellCastCancel
                {
                    ServerUniqueId = CastingId,
                    CastResult     = result,
                    CancelCast     = true
                });
            }

            events.CancelEvents();
            cancelled = true;
            status = SpellStatus.Executing;

            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} cast was cancelled.");
        }

        public bool TryCancelEffect(IUnitEntity requester)
        {
            if (requester == null)
                throw new ArgumentNullException();

            if (status != SpellStatus.Executing || requester.Guid != Caster.Guid)
                return false;

            var removableEffects = new List<(SpellEffectInterpretation Effect, IWorldEntity Target, ISpellTargetEffectInfo Info)>();

            foreach (ISpellTargetInfo targetInfo in targets)
            {
                foreach (ISpellTargetEffectInfo info in targetInfo.Effects)
                {
                    if (info.DropEffect || info.LifetimeEnded)
                        continue;

                    SpellEffectInterpretation effect = SpellEffectInterpreter.Interpret(info.Entry);
                    if (BuildLifetimeRemovalAction(effect, targetInfo.Entity, info) == null)
                        continue;

                    if (targetInfo.Entity.Guid != requester.Guid)
                        return false;

                    removableEffects.Add((effect, targetInfo.Entity, info));
                }
            }

            if (removableEffects.Count == 0)
                return false;

            bool removedAny = false;
            foreach ((SpellEffectInterpretation effect, IWorldEntity target, ISpellTargetEffectInfo info) in removableEffects)
            {
                CancelLifetimeEvent(info);
                removedAny |= TryRemoveActiveEffect(effect, target, info);
            }

            if (!removedAny)
                return false;

            if (lifetimeEvents.Count == 0)
            {
                cancelled = true;
                status = SpellStatus.Finished;
                scriptCollection.Invoke<ISpellScript>(s => s.OnFinish(this, cancelled));
                SendSpellFinish();
            }

            return true;
        }

        bool ISpell.TryCancelEffect(IUnitEntity requester) => TryCancelEffect(requester);

        private void Execute()
        {
            status = SpellStatus.Executing;
            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} has started executing.");

            if (Caster is IPlayer player)
                if (Parameters.SpellInfo.Entry.SpellCoolDown != 0u)
                    player.SpellManager.SetSpellCooldown(Parameters.SpellInfo.Entry.Id, Parameters.SpellInfo.Entry.SpellCoolDown / 1000d);

            SelectTargets();
            scriptCollection.Invoke<ISpellScript>(s => s.OnExecute(this, targets.AsReadOnly()));
            ExecuteEffects();

            if (Caster is IPlayer executingPlayer)
                SpellQuestObjectiveUpdater.UpdateSpellSuccessObjectives(executingPlayer, Parameters.SpellInfo.Entry.Id);

            CostSpell();
        }

        private void CostSpell()
        {
            if (Parameters.CharacterSpell?.MaxAbilityCharges > 0)
                Parameters.CharacterSpell.UseCharge();
        }

        private void SelectTargets()
        {
            targets.Clear();
            AddTarget(SpellEffectTargetFlags.Caster, Caster);

            if (Parameters.PrimaryTargetId != 0)
            {
                IWorldEntity primaryTargetEntity = GetPrimaryTargetWorldEntity();
                if (primaryTargetEntity != null)
                    AddTarget(SpellEffectTargetFlags.Target, primaryTargetEntity);
            }

            if (Caster is IPlayer)
                InitialiseTelegraphs();

            foreach (IUnitEntity entity in SelectTelegraphTargets())
                AddTarget(SpellEffectTargetFlags.Telegraph, entity);

            SpellEffectDiagnostics.TraceTargetSelection(this, targets, telegraphs.Count);
        }

        private void AddTarget(SpellEffectTargetFlags flags, IWorldEntity entity)
        {
            SpellTargetInfo target = targets.OfType<SpellTargetInfo>().FirstOrDefault(t => t.Entity.Guid == entity.Guid);
            if (target != null)
            {
                target.AddFlags(flags);
                return;
            }

            targets.Add(new SpellTargetInfo(flags, entity));
        }

        private IEnumerable<IUnitEntity> SelectTelegraphTargets()
        {
            Dictionary<uint, IUnitEntity> candidates = [];
            foreach (ITelegraph telegraph in telegraphs)
            {
                foreach (IUnitEntity entity in telegraph.GetTargets())
                    candidates.TryAdd(entity.Guid, entity);
            }

            Spell4AoeTargetConstraintsEntry constraints = Parameters.SpellInfo.AoeTargetConstraints;
            Vector3 selectionOrigin = GetAoeSelectionOrigin();
            Vector3 selectionRotation = GetAoeSelectionRotation(selectionOrigin);
            IEnumerable<IUnitEntity> constrainedCandidates = candidates.Values
                .Where(e => MeetsAoeTargetConstraints(e, constraints, selectionOrigin, selectionRotation));

            IEnumerable<IUnitEntity> orderedCandidates = OrderAoeTargetCandidates(constrainedCandidates, constraints, selectionOrigin);

            uint targetCount = constraints?.TargetCount ?? 0u;
            if (targetCount > 0u)
                orderedCandidates = orderedCandidates.Take((int)targetCount);

            return orderedCandidates;
        }

        private bool MeetsAoeTargetConstraints(IUnitEntity entity, Spell4AoeTargetConstraintsEntry constraints, Vector3 selectionOrigin, Vector3 selectionRotation)
        {
            if (!IsTargetLivingStateAllowed(entity))
                return false;

            if (constraints != null)
            {
                float range = GetHorizontalDistance(selectionOrigin, entity.Position);
                if (constraints.MinRange > 0f && range < constraints.MinRange)
                    return false;

                if (constraints.MaxRange > 0f && range > constraints.MaxRange)
                    return false;

                if (constraints.Angle > 0f && constraints.Angle < 360f && !IsWithinAngle(entity, selectionOrigin, selectionRotation, constraints.Angle))
                    return false;
            }

            TargetGroupEntry aoeGroup = Parameters.SpellInfo.BaseInfo.AoeGroup;
            if (aoeGroup != null && !TargetGroupCriteriaEvaluator.Evaluate(aoeGroup, entity, GameTableManager.Instance))
                return false;

            return true;
        }

        private IEnumerable<IUnitEntity> OrderAoeTargetCandidates(IEnumerable<IUnitEntity> candidates, Spell4AoeTargetConstraintsEntry constraints, Vector3 selectionOrigin)
        {
            if (constraints == null)
                return OrderByDistance(candidates, selectionOrigin);

            return constraints.TargetSelection switch
            {
                (uint)AoeSelectionType.Closest => OrderByDistance(candidates, selectionOrigin),
                (uint)AoeSelectionType.Furthest => OrderByDistanceDescending(candidates, selectionOrigin),
                (uint)AoeSelectionType.Random => ShuffleCandidates(candidates),
                // Client rows explicitly name selection 4 as "lowest absolute health".
                (uint)AoeSelectionType.LowestAbsoluteHealth => candidates
                    .OrderBy(e => e.Health)
                    .ThenBy(e => Vector3.DistanceSquared(selectionOrigin, e.Position)),
                // Client rows explicitly name selection 5 as "missing the most health".
                (uint)AoeSelectionType.MissingMostHealth => candidates
                    .OrderByDescending(e => e.MaxHealth > e.Health ? e.MaxHealth - e.Health : 0u)
                    .ThenBy(e => Vector3.DistanceSquared(selectionOrigin, e.Position)),
                _ => OrderByDistance(candidates, selectionOrigin)
            };
        }

        private IEnumerable<IUnitEntity> OrderByDistance(IEnumerable<IUnitEntity> candidates, Vector3 selectionOrigin)
        {
            return candidates.OrderBy(e => Vector3.DistanceSquared(selectionOrigin, e.Position));
        }

        private IEnumerable<IUnitEntity> OrderByDistanceDescending(IEnumerable<IUnitEntity> candidates, Vector3 selectionOrigin)
        {
            return candidates.OrderByDescending(e => Vector3.DistanceSquared(selectionOrigin, e.Position));
        }

        private static IEnumerable<IUnitEntity> ShuffleCandidates(IEnumerable<IUnitEntity> candidates)
        {
            List<IUnitEntity> shuffled = candidates.ToList();
            for (int index = shuffled.Count - 1; index > 0; index--)
            {
                int swapIndex = Random.Shared.Next(index + 1);
                (shuffled[index], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[index]);
            }

            return shuffled;
        }

        private bool IsWithinCasterAngle(IWorldEntity entity, float angleDegrees)
        {
            return IsWithinAngle(entity.Position, Caster.Position, Caster.Rotation, angleDegrees);
        }

        private static bool IsWithinAngle(IUnitEntity entity, Vector3 origin, Vector3 rotation, float angleDegrees)
        {
            return IsWithinAngle(entity.Position, origin, rotation, angleDegrees);
        }

        private static bool IsWithinAngle(Vector3 targetPosition, Vector3 origin, Vector3 rotation, float angleDegrees)
        {
            float targetAngle = (origin.GetAngle(targetPosition) - rotation.X).NormaliseRotationRadians();
            return MathF.Abs(targetAngle.ToDegrees()) <= angleDegrees / 2f;
        }

        private static float GetHorizontalDistance(Vector3 source, Vector3 target)
        {
            return Vector2.Distance(new Vector2(source.X, source.Z), new Vector2(target.X, target.Z));
        }

        private (Vector3 Position, Vector3 Rotation) ResolveTelegraphAnchor()
        {
            uint targetType = Parameters.SpellInfo.BaseInfo.TargetMechanics?.TargetType ?? 0u;
            IWorldEntity primaryTarget = GetPrimaryTargetWorldEntity();
            Vector3 position;
            string source;

            if (targetType == TargetTypeTargetAoe && primaryTarget != null)
            {
                position = primaryTarget.Position;
                source = "target-aoe:primary-target";
            }

            else if (targetType == TargetTypePositionAoe)
            {
                if (Parameters.Position != null)
                {
                    position = Parameters.Position.Vector;
                    source = "position-aoe:explicit-position";
                }

                else if (primaryTarget != null)
                {
                    position = primaryTarget.Position;
                    source = "position-aoe:primary-target-fallback";
                }

                else
                {
                    position = Caster.Position;
                    source = "position-aoe:caster-fallback";
                }
            }

            else
            {
                position = Caster.Position;
                source = "caster-centered";
            }

            SpellEffectDiagnostics.TraceTelegraphAnchorResolution(this, source, position, primaryTarget?.Guid);
            return (position, GetRotationToward(position));
        }

        private Vector3 GetAoeSelectionOrigin()
        {
            return telegraphs.Count != 0 ? telegraphs[0].Position : ResolveTelegraphAnchor().Position;
        }

        private Vector3 GetAoeSelectionRotation(Vector3 selectionOrigin)
        {
            return telegraphs.Count != 0 ? telegraphs[0].Rotation : GetRotationToward(selectionOrigin);
        }

        private Vector3 GetRotationToward(Vector3 position)
        {
            if (GetHorizontalDistance(Caster.Position, position) <= 0.001f)
                return Caster.Rotation;

            return new Vector3(Caster.Position.GetAngle(position), Caster.Rotation.Y, Caster.Rotation.Z);
        }

        private IUnitEntity GetPrimaryTargetEntity()
        {
            return GetPrimaryTargetWorldEntity() as IUnitEntity;
        }

        private IWorldEntity GetPrimaryTargetWorldEntity()
        {
            if (Parameters.PrimaryTargetId == 0u)
                return null;

            if (Parameters.PrimaryTargetId == Caster.Guid)
                return Caster;

            return Caster.GetVisible<IWorldEntity>(Parameters.PrimaryTargetId);
        }

        private void ExecuteEffects()
        {
            bool scheduledEffects = false;

            foreach (Spell4EffectsEntry spell4EffectsEntry in Parameters.SpellInfo.Effects)
            {
                SpellEffectInterpretation effect = SpellEffectInterpreter.Interpret(spell4EffectsEntry);
                if (ScheduleEffect(effect))
                {
                    scheduledEffects = true;
                    continue;
                }

                ExecuteEffect(effect);
            }

            awaitingInitialImpact = scheduledEffects;
            SendSpellGo(!scheduledEffects);
        }

        private bool ScheduleEffect(SpellEffectInterpretation effect)
        {
            // Tick-only and duration-only rows need more evidence before they are safe to move off the immediate path.
            if (effect.Timing.TickTime > 0u && effect.Timing.DurationTime > 0u)
            {
                uint firstPulseDelay = effect.Timing.DelayTime > 0u
                    ? effect.Timing.DelayTime
                    : effect.Timing.TickTime;

                if (firstPulseDelay > effect.Timing.DurationTime)
                    return false;

                SpellEffectDiagnostics.TraceEffectSchedule(this, effect, firstPulseDelay, effect.Timing.TickTime, effect.Timing.DurationTime, true);
                ScheduleEffectPulse(effect, firstPulseDelay, firstPulseDelay, effect.Timing.TickTime, effect.Timing.DurationTime);
                return true;
            }

            if (effect.Timing.DelayTime > 0u)
            {
                SpellEffectDiagnostics.TraceEffectSchedule(this, effect, effect.Timing.DelayTime, 0u, 0u, false);
                ScheduleEffectPulse(effect, effect.Timing.DelayTime, effect.Timing.DelayTime, 0u, 0u);
                return true;
            }

            return false;
        }

        private void ScheduleEffectPulse(SpellEffectInterpretation effect, uint delayTime, uint elapsedTime, uint tickTime, uint durationTime)
        {
            events.EnqueueEvent(new SpellEvent(delayTime / 1000d, () =>
            {
                if (ExecuteEffect(effect))
                    SendSpellGo();

                if (tickTime == 0u || durationTime == 0u)
                    return;

                uint nextElapsedTime = elapsedTime + tickTime;
                if (nextElapsedTime <= durationTime)
                    ScheduleEffectPulse(effect, tickTime, nextElapsedTime, tickTime, durationTime);
            }));
        }

        private bool ExecuteEffect(SpellEffectInterpretation effect)
        {
            SpellEffectTargetFlags effectTargetFlags = (SpellEffectTargetFlags)effect.Entry.TargetFlags;
            if ((effectTargetFlags & SpellEffectTargetFlags.Telegraph) != 0)
                RefreshTelegraphTargets();

            // select targets for effect
            List<ISpellTargetInfo> effectTargets = targets
                .Where(t => (t.Flags & effectTargetFlags) != 0)
                .Where(t => IsEffectTargetStillValid(effectTargetFlags, t))
                .ToList();

            SpellEffectDelegate handler = GlobalSpellManager.Instance.GetEffectHandler((SpellEffectType)effect.Entry.EffectType);
            SpellEffectDiagnostics.TraceEffectDispatch(this, effect, effectTargets.Count, handler != null);

            if (handler == null)
            {
                log.Warn($"Unhandled spell effect {(SpellEffectType)effect.Entry.EffectType}");
                return false;
            }

            uint effectId = GlobalSpellManager.Instance.NextEffectId;
            bool executed = false;
            foreach (SpellTargetInfo effectTarget in effectTargets)
            {
                if (terminatedPersistentEffects.Contains((effectTarget.Entity.Guid, effect.Entry.Id)))
                    continue;

                if (!MeetsApplyPrerequisites(effect, effectTarget.Entity))
                    continue;

                var info = new SpellTargetInfo.SpellTargetEffectInfo(effectId, effect.Entry);
                effectTarget.Effects.Add(info);
                pendingSpellGoEffects.Add(new PendingSpellGoEffect(effectTarget, info));
                SpellRuntimeEvidenceCollector.RecordEffectPreparation(this, effectTarget.Entity, info);

                if (effectTarget.Entity is not IUnitEntity unitTarget)
                {
                    if (!ExecuteWorldEntityEffect(effect, effectTarget.Entity, info))
                    {
                        info.DropEffect = true;
                        SpellEffectDiagnostics.TraceEffectResult(this, effectTarget.Entity, info);
                        continue;
                    }

                    ScheduleEffectLifetime(effect, effectTarget.Entity, info);
                    SpellEffectDiagnostics.TraceEffectResult(this, effectTarget.Entity, info);
                    executed = true;
                    continue;
                }

                if (effect.Entry.EffectType != SpellEffectType.SpellImmunity && unitTarget.IsImmuneToSpell(Parameters.SpellInfo.Entry.Id))
                {
                    info.DropEffect = true;
                    info.AddCombatLog(new CombatLogImmune
                    {
                        CastData = new CombatLogCastData
                        {
                            CasterId     = Caster.Guid,
                            TargetId     = effectTarget.Entity.Guid,
                            SpellId      = Parameters.SpellInfo.Entry.Id,
                            CombatResult = CombatResult.Hit
                        }
                    });
                    SpellEffectDiagnostics.TraceSpellImmunityBlocked(this, unitTarget, effect, Parameters.SpellInfo.Entry.Id);
                    QueueDiagnosticSpellBroadcast(unitTarget.Guid, $"spell-immunity:{Parameters.SpellInfo.Entry.Id}", effect);
                    SpellEffectDiagnostics.TraceEffectResult(this, unitTarget, info);
                    executed = true;
                    continue;
                }

                if (effect.Entry.EffectType != SpellEffectType.SpellEffectImmunity && unitTarget.IsImmuneToSpellEffect(effect.Entry.EffectType))
                {
                    info.DropEffect = true;
                    info.AddCombatLog(new CombatLogImmune
                    {
                        CastData = new CombatLogCastData
                        {
                            CasterId     = Caster.Guid,
                            TargetId     = effectTarget.Entity.Guid,
                            SpellId      = Parameters.SpellInfo.Entry.Id,
                            CombatResult = CombatResult.Hit
                        }
                    });
                    SpellEffectDiagnostics.TraceSpellEffectImmunityBlocked(this, unitTarget, effect);
                    QueueDiagnosticSpellBroadcast(unitTarget.Guid, $"effect-immunity:{effect.Entry.EffectType}", effect);
                    SpellEffectDiagnostics.TraceEffectResult(this, unitTarget, info);
                    executed = true;
                    continue;
                }

                if (IsHostileToTarget(unitTarget)
                    && UnitStateSetRules.TryGetHostileEffectImmuneState(unitTarget, out uint blockingStateId))
                {
                    info.DropEffect = true;
                    info.AddCombatLog(new CombatLogImmune
                    {
                        CastData = new CombatLogCastData
                        {
                            CasterId     = Caster.Guid,
                            TargetId     = effectTarget.Entity.Guid,
                            SpellId      = Parameters.SpellInfo.Entry.Id,
                            CombatResult = CombatResult.Hit
                        }
                    });
                    SpellEffectDiagnostics.TraceUnitStateImmuneBlocked(this, unitTarget, effect, blockingStateId);
                    QueueDiagnosticSpellBroadcast(unitTarget.Guid, $"state-immunity:{blockingStateId}", effect);
                    SpellEffectDiagnostics.TraceEffectResult(this, unitTarget, info);
                    executed = true;
                    continue;
                }

                try
                {
                    handler.Invoke(this, unitTarget, info);
                    ScheduleEffectLifetime(effect, unitTarget, info);
                    executed = true;
                }
                catch (Exception ex)
                {
                    info.DropEffect = true;
                    log.Error(ex, $"Unhandled exception while executing spell effect {(SpellEffectType)effect.Entry.EffectType} for spell {Parameters.SpellInfo.Entry.Id} on target {unitTarget.Guid}.");
                }
                finally
                {
                    SpellEffectDiagnostics.TraceEffectResult(this, unitTarget, info);
                }
            }

            return executed;
        }

        private void RefreshTelegraphTargets()
        {
            foreach (IUnitEntity entity in SelectTelegraphTargets())
                AddTarget(SpellEffectTargetFlags.Telegraph, entity);
        }

        internal bool IsEffectTargetStillValid(SpellEffectTargetFlags effectTargetFlags, ISpellTargetInfo targetInfo)
        {
            if ((effectTargetFlags & SpellEffectTargetFlags.Telegraph) == 0)
                return true;

            if ((targetInfo.Flags & SpellEffectTargetFlags.Telegraph) == 0)
                return true;

            if (targetInfo.Entity is not IUnitEntity unitTarget)
                return false;

            return telegraphs.Any(t => t.InsideTelegraph(unitTarget.Position, unitTarget.HitRadius));
        }

        private bool ExecuteWorldEntityEffect(SpellEffectInterpretation effect, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            switch ((SpellEffectType)effect.Entry.EffectType)
            {
                case SpellEffectType.Proxy:
                    SpellHandler.HandleEffectProxyWorld(this, target, info);
                    return true;
                case SpellEffectType.SpellForceRemove:
                    SpellHandler.HandleEffectSpellForceRemoveWorld(this, target, info);
                    return true;
                case SpellEffectType.Activate:
                    SpellHandler.HandleEffectActivateWorld(this, target, info);
                    return true;
                case SpellEffectType.SetBusy:
                    SpellHandler.HandleEffectSetBusyWorld(this, target, info);
                    return true;
                case SpellEffectType.DespawnUnit:
                    SpellHandler.HandleEffectDespawnUnitWorld(this, target, info);
                    return true;
                case SpellEffectType.RavelSignal:
                    SpellHandler.HandleEffectRavelSignalWorld(this, target, info);
                    return true;
                case SpellEffectType.Fluff:
                    return true;
                default:
                    log.Warn($"Unhandled world-target spell effect {(SpellEffectType)effect.Entry.EffectType} for target {target.Guid} on spell {Parameters.SpellInfo.Entry.Id}.");
                    return false;
            }
        }

        private void ScheduleEffectLifetime(SpellEffectInterpretation effect, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            uint durationTime = GetEffectLifetimeDuration(effect);
            if (durationTime == 0u)
                return;

            Action removalAction = BuildLifetimeRemovalAction(effect, target, info);
            if (removalAction == null)
                return;

            SpellEffectDiagnostics.TraceEffectLifetime(this, effect, target.Guid, durationTime);

            var lifetimeEvent = new SpellEvent(durationTime / 1000d, () =>
            {
                lifetimeEvents.Remove(info);
                TryRemoveActiveEffect(effect, target, info);
            });

            lifetimeEvents[info] = lifetimeEvent;
            events.EnqueueEvent(lifetimeEvent);
        }

        private void ScheduleEffectLifetime(SpellEffectInterpretation effect, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            uint durationTime = GetEffectLifetimeDuration(effect);
            if (durationTime == 0u)
                return;

            Action removalAction = BuildLifetimeRemovalAction(effect, target, info);
            if (removalAction == null)
                return;

            SpellEffectDiagnostics.TraceEffectLifetime(this, effect, target.Guid, durationTime);

            var lifetimeEvent = new SpellEvent(durationTime / 1000d, () =>
            {
                lifetimeEvents.Remove(info);
                TryRemoveActiveEffect(effect, target, info);
            });

            lifetimeEvents[info] = lifetimeEvent;
            events.EnqueueEvent(lifetimeEvent);
        }

        private void CancelLifetimeEvent(ISpellTargetEffectInfo info)
        {
            if (!lifetimeEvents.TryGetValue(info, out ISpellEvent lifetimeEvent))
                return;

            lifetimeEvents.Remove(info);
            events.CancelEvent(lifetimeEvent);
        }

        private bool TryRemoveActiveEffect(SpellEffectInterpretation effect, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (info.LifetimeEnded)
                return false;

            Action removalAction = BuildLifetimeRemovalAction(effect, target, info);
            if (removalAction == null)
                return false;

            info.LifetimeEnded = true;
            removalAction();
            return true;
        }

        private bool TryRemoveActiveEffect(SpellEffectInterpretation effect, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            if (target is IUnitEntity unitTarget)
                return TryRemoveActiveEffect(effect, unitTarget, info);

            if (info.LifetimeEnded)
                return false;

            Action removalAction = BuildLifetimeRemovalAction(effect, target, info);
            if (removalAction == null)
                return false;

            info.LifetimeEnded = true;
            removalAction();
            return true;
        }

        private Action BuildLifetimeRemovalAction(SpellEffectInterpretation effect, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            switch (effect.Entry.EffectType)
            {
                case SpellEffectType.SetBusy:
                    if (effect.SetBusy == null || !effect.SetBusy.Busy)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveBusy(info.EffectId);
                        SpellEffectDiagnostics.TraceSetBusy(this, target, effect.SetBusy, removed, false, removed, removed ? 1u : 0u, null);
                    };
                default:
                    return null;
            }
        }

        private Action BuildLifetimeRemovalAction(SpellEffectInterpretation effect, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            switch (effect.Entry.EffectType)
            {
                case SpellEffectType.UnitPropertyModifier:
                    if (effect.UnitPropertyModifier == null)
                        return null;

                    return () =>
                    {
                        if (target.RemoveSpellProperty(effect.UnitPropertyModifier.Property, info.EffectId))
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.PersonalDmgHealMod:
                    if (effect.PersonalDmgHealMod == null)
                        return null;

                    if (!SpellHandler.TryResolvePersonalDmgHealModProperty(effect.PersonalDmgHealMod, out Property personalProperty, out _))
                        return null;

                    return () =>
                    {
                        if (target.RemoveSpellProperty(personalProperty, info.EffectId))
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.CCStateSet:
                    if (effect.CCState == null)
                        return null;

                    return () =>
                    {
                        if (target.RemoveCCState(effect.CCState.State, info.EffectId))
                            SendCCStateRemove(target.Guid, effect.CCState.State, info.EffectId);
                    };
                case SpellEffectType.ModifyInterruptArmor:
                    if (effect.ModifyInterruptArmor == null)
                        return null;

                    uint interruptArmorAmount = info.CombatLogs.OfType<CombatLogModifyInterruptArmor>().LastOrDefault()?.Amount
                        ?? effect.ModifyInterruptArmor.Amount;
                    if (interruptArmorAmount == 0u)
                        return null;

                    return () =>
                    {
                        uint removedAmount = Math.Min(target.InterruptArmor, interruptArmorAmount);
                        target.InterruptArmor -= removedAmount;
                        SpellEffectDiagnostics.TraceModifyInterruptArmor(this, target, effect.ModifyInterruptArmor, removedAmount, true);

                        if (removedAmount > 0u)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.Absorption:
                    if (effect.Absorption == null)
                        return null;

                    return () =>
                    {
                        uint removedAmount = target.RemoveAbsorption(info.EffectId);
                        SpellEffectDiagnostics.TraceAbsorption(this, target, effect.Absorption, removedAmount, true);

                        if (removedAmount > 0u)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.HealingAbsorption:
                    if (effect.HealingAbsorption == null)
                        return null;

                    return () =>
                    {
                        uint removedAmount = target.RemoveHealingAbsorption(info.EffectId);
                        SpellEffectDiagnostics.TraceHealingAbsorption(this, target, effect.HealingAbsorption, removedAmount, true);

                        if (removedAmount > 0u)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.DelayDeath:
                    if (effect.DelayDeath == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveDelayDeath(info.EffectId);
                        SpellEffectDiagnostics.TraceDelayDeath(this, target, effect.DelayDeath, false, removed, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.Proc:
                    if (effect.Proc == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveProc(info.EffectId);
                        SpellEffectDiagnostics.TraceProc(this, target, effect.Proc, false, removed, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.ClampVital:
                    if (effect.ClampVital == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveVitalClamp(info.EffectId);
                        SpellEffectDiagnostics.TraceClampVital(this, target, effect.ClampVital, Vital.Health, target.Health, target.Health, false, removed, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.ShieldOverload:
                    if (effect.ShieldOverload == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveShieldOverload(info.EffectId);
                        SpellEffectDiagnostics.TraceShieldOverload(this, target, effect.ShieldOverload, target.Shield, target.Shield, false, removed, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.UnitStateSet:
                    if (effect.UnitStateSet == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveUnitState(info.EffectId);
                        SpellEffectDiagnostics.TraceUnitStateSet(this, target, effect.UnitStateSet, false, false, removed, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.SetBusy:
                    if (effect.SetBusy == null || !effect.SetBusy.Busy)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveBusy(info.EffectId);
                        SpellEffectDiagnostics.TraceSetBusy(this, target, effect.SetBusy, removed, false, removed, removed ? 1u : 0u, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.SummonMount:
                    if (target is not IPlayer mountPlayer)
                        return null;

                    return () =>
                    {
                        if (mountPlayer.PlatformGuid == null)
                            return;

                        mountPlayer.Dismount();
                        SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.ForcedMove:
                    if (effect.ForcedMove == null)
                        return null;

                    return () =>
                    {
                        target.MovementManager.SetVelocity(Vector3.Zero, false);
                        target.MovementManager.SetState(target.MovementManager.GetState() & ~StateFlags.Velocity);
                    };
                case SpellEffectType.Stealth:
                    if (effect.Stealth == null)
                        return null;

                    return () =>
                    {
                        if (!target.RemoveStealth(info.EffectId))
                            return;

                        if (!target.IsStealthed)
                        {
                            target.CreateFlags &= ~EntityCreateFlag.IsStealthed;
                            SendStealthCombatLog(target.Guid, true);
                        }

                        SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.AggroImmune:
                    if (effect.AggroImmune == null)
                        return null;

                    return () =>
                    {
                        if (target.RemoveAggroImmune(info.EffectId))
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.SpellEffectImmunity:
                    if (effect.SpellEffectImmunity == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveSpellEffectImmunity(info.EffectId);
                        SpellEffectDiagnostics.TraceSpellEffectImmunity(this, target, effect.SpellEffectImmunity, false, removed, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.SpellImmunity:
                    if (effect.SpellImmunity == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveSpellImmunity(info.EffectId);
                        SpellEffectDiagnostics.TraceSpellImmunity(this, target, effect.SpellImmunity, false, removed, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.Scale:
                    if (effect.Scale == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveScale(info.EffectId);
                        SpellEffectDiagnostics.TraceScale(this, target, effect.Scale, 0f, false, removed, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.FactionSet:
                    if (effect.FactionSet == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveFaction(info.EffectId);
                        SpellEffectDiagnostics.TraceFactionSet(this, target, effect.FactionSet, 0u, false, removed, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.ItemVisualSwap:
                    if (effect.ItemVisualSwap == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveItemVisualSwap(info.EffectId);
                        SpellEffectDiagnostics.TraceItemVisualSwap(this, target, effect.ItemVisualSwap, false, removed ? null : "restore-missing");

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.DisguiseOutfit:
                    if (effect.DisguiseOutfit == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveDisguiseOutfit(info.EffectId);
                        SpellEffectDiagnostics.TraceDisguiseOutfit(this, target, effect.DisguiseOutfit, 0, false, false, false, removed, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.MimicDisguise:
                    if (effect.MimicDisguise == null)
                        return null;

                    return () =>
                    {
                        bool removed = target.RemoveMimicDisguise(info.EffectId);
                        SpellEffectDiagnostics.TraceMimicDisguise(this, target, effect.MimicDisguise, Caster.Guid, 0u, 0, 0u, 0, false, false, false, removed, null);

                        if (removed)
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.RewardPropertyModifier:
                    if (effect.RewardPropertyModifier == null)
                        return null;

                    IPlayer rewardPlayer = target as IPlayer ?? Caster as IPlayer;
                    if (rewardPlayer == null)
                        return null;

                    if (!SpellHandler.TryResolveRewardPropertyModifier(
                        effect.RewardPropertyModifier,
                        out RewardPropertyEntry rewardPropertyEntry,
                        out float rewardPropertyValue,
                        out _))
                        return null;

                    return () =>
                    {
                        rewardPlayer.Account.RewardPropertyManager.UpdateRewardProperty(
                            rewardPropertyEntry,
                            -rewardPropertyValue,
                            effect.RewardPropertyModifier.Data);
                        SpellEffectDiagnostics.TraceRewardPropertyModifier(this, target, effect.RewardPropertyModifier, rewardPlayer.Guid, -rewardPropertyValue, false, true, null);
                        SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.SummonCreature:
                    if (effect.SummonCreature == null || info.CreatedEntities.Count == 0)
                        return null;

                    return () =>
                    {
                        foreach (IGridEntity entity in info.CreatedEntities)
                        {
                            if (entity.InWorld)
                                entity.RemoveFromMap();
                        }
                    };
                case SpellEffectType.SummonTrap:
                    if (effect.SummonTrap == null || info.CreatedEntities.Count == 0)
                        return null;

                    return () =>
                    {
                        foreach (IGridEntity entity in info.CreatedEntities)
                        {
                            if (entity.InWorld)
                                entity.RemoveFromMap();
                        }
                    };
                case SpellEffectType.NpcExecutionDelay:
                    if (effect.NpcExecutionDelay == null)
                        return null;

                    return () => { };
                default:
                    return null;
            }
        }

        private static uint GetEffectLifetimeDuration(SpellEffectInterpretation effect)
        {
            if (effect.ForcedMove?.DurationTime > 0u)
                return effect.ForcedMove.DurationTime;

            return effect.Timing.DurationTime;
        }

        public bool IsMovingInterrupted()
        {
            return Parameters.SpellInfo.BaseInfo.IsMovingInterrupted;
        }

        private void SendSpellCastResult(CastResult castResult)
        {
            if (castResult == CastResult.Ok)
                return;

            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} failed to cast {castResult}.");

            if (Caster is IPlayer player && !player.IsLoading)
            {
                player.Session.EnqueueMessageEncrypted(new ServerSpellCastResult
                {
                    Spell4Id   = Parameters.SpellInfo.Entry.Id,
                    CastResult = castResult
                });
            }
        }

        private void SendSpellStart()
        {
            var spellStart = new ServerSpellStart
            {
                CastingId              = CastingId,
                CasterId               = Caster.Guid,
                PrimaryTargetId        = Parameters.PrimaryTargetId != 0u ? Parameters.PrimaryTargetId : Caster.Guid,
                Spell4Id               = Parameters.SpellInfo.Entry.Id,
                RootSpell4Id           = Parameters.RootSpellInfo?.Entry.Id ?? 0,
                ParentSpell4Id         = Parameters.ParentSpellInfo?.Entry.Id ?? 0,
                FieldPosition          = new Position(Caster.Position),
                Yaw                    = Caster.Rotation.X,
                UserInitiatedSpellCast = Parameters.UserInitiatedSpellCast,
                InitialPositionData    = new List<ServerSpellStart.InitialPosition>(),
                TelegraphPositionData  = new List<ServerSpellStart.TelegraphPosition>()
            };

            var unitsCasting = new List<IWorldEntity>
            {
                Caster
            };

            foreach (IWorldEntity unit in unitsCasting.Where(u => u != null))
            {
                spellStart.InitialPositionData.Add(new ServerSpellStart.InitialPosition
                {
                    UnitId      = unit.Guid,
                    Position    = new Position(unit.Position),
                    TargetFlags = 3,
                    Yaw         = unit.Rotation.X
                });
            }

            foreach (IWorldEntity unit in unitsCasting.Where(u => u != null))
            {
                foreach (ITelegraph telegraph in telegraphs)
                {
                    spellStart.TelegraphPositionData.Add(new ServerSpellStart.TelegraphPosition
                    {
                        TelegraphId    = (ushort)telegraph.TelegraphDamage.Id,
                        AttachedUnitId = unit.Guid,
                        TargetFlags    = 3,
                        Position       = new Position(telegraph.Position),
                        Yaw            = telegraph.Rotation.X
                    });
                }
            }

            Caster.EnqueueToVisible(spellStart, true);
        }

        private void SendSpellFinish()
        {
            if (status != SpellStatus.Finished)
                return;

            Caster.EnqueueToVisible(new ServerSpellFinish
            {
                ServerUniqueId = CastingId,
            }, true);
        }

        private void SendSpellGo(bool sendEmpty = false)
        {
            List<PendingSpellGoEffect> pendingEffects = pendingSpellGoEffects.ToList();
            pendingSpellGoEffects.Clear();
            if (pendingEffects.Count != 0 || sendEmpty)
                awaitingInitialImpact = false;

            if (pendingEffects.Count == 0 && !sendEmpty)
                return;

            List<ICombatLog> combatLogs = [];

            var serverSpellGo = new ServerSpellGo
            {
                ServerUniqueId     = CastingId,
                PrimaryDestination = new Position(Caster.Position),
                Phase              = -1
            };

            foreach (IGrouping<ISpellTargetInfo, PendingSpellGoEffect> targetEffectGroup in pendingEffects.GroupBy(e => e.TargetInfo))
            {
                ISpellTargetInfo targetInfo = targetEffectGroup.Key;
                List<ISpellTargetEffectInfo> targetEffects = targetEffectGroup.Select(e => e.EffectInfo).ToList();

                if (!targetEffects.Any(x => x.DropEffect == false))
                {
                    combatLogs.AddRange(targetEffects.SelectMany(i => i.CombatLogs));
                    continue;
                }

                var networkTargetInfo = new TargetInfo
                {
                    UnitId        = targetInfo.Entity.Guid,
                    TargetFlags   = 1,
                    InstanceCount = 1,
                    CombatResult  = CombatResult.Hit
                };

                foreach (ISpellTargetEffectInfo targetEffectInfo in targetEffects)
                {
                    if (targetEffectInfo.DropEffect)
                    {
                        combatLogs.AddRange(targetEffectInfo.CombatLogs);
                        continue;
                    }

                    if (targetEffectInfo.Entry.EffectType == SpellEffectType.Proxy)
                        continue;

                    var networkTargetEffectInfo = new TargetInfo.EffectInfo
                    {
                        Spell4EffectId = targetEffectInfo.Entry.Id,
                        EffectUniqueId = targetEffectInfo.EffectId,
                        TimeRemaining  = -1
                    };

                    if (targetEffectInfo.Damage != null)
                    {
                        networkTargetEffectInfo.InfoType = 1;
                        networkTargetEffectInfo.DamageDescriptionData = new TargetInfo.EffectInfo.DamageDescription
                        {
                            RawDamage          = targetEffectInfo.Damage.RawDamage,
                            RawScaledDamage    = targetEffectInfo.Damage.RawScaledDamage,
                            AbsorbedAmount     = targetEffectInfo.Damage.AbsorbedAmount,
                            ShieldAbsorbAmount = targetEffectInfo.Damage.ShieldAbsorbAmount,
                            AdjustedDamage     = targetEffectInfo.Damage.AdjustedDamage,
                            OverkillAmount     = targetEffectInfo.Damage.OverkillAmount,
                            GlanceAmount       = 0u,
                            KilledTarget       = targetEffectInfo.Damage.KilledTarget,
                            CombatResult       = targetEffectInfo.Damage.CombatResult,
                            DamageType         = targetEffectInfo.Damage.DamageType
                        };
                    }

                    networkTargetInfo.EffectInfoData.Add(networkTargetEffectInfo);

                    combatLogs.AddRange(targetEffectInfo.CombatLogs);
                }

                serverSpellGo.TargetInfoData.Add(networkTargetInfo);
            }

            var unitsCasting = new List<IUnitEntity>
            {
                Caster
            };

            foreach (IUnitEntity unit in unitsCasting)
            {
                serverSpellGo.InitialPositionData.Add(new InitialPosition
                {
                    UnitId      = unit.Guid,
                    Position    = new Position(unit.Position),
                    TargetFlags = 3,
                    Yaw         = unit.Rotation.X
                });
            }

            foreach (IUnitEntity unit in unitsCasting)
            {
                foreach (ITelegraph telegraph in telegraphs)
                {
                    serverSpellGo.TelegraphPositionData.Add(new TelegraphPosition
                    {
                        TelegraphId    = (ushort)telegraph.TelegraphDamage.Id,
                        AttachedUnitId = unit.Guid,
                        TargetFlags    = 3,
                        Position       = new Position(telegraph.Position),
                        Yaw            = telegraph.Rotation.X
                    });
                }
            }

            foreach (ICombatLog combatLog in combatLogs)
            {
                Caster.EnqueueToVisible(new ServerCombatLog
                {
                    CombatLog = combatLog
                }, true);
            }

            SpellEffectDiagnostics.TraceSpellGo(
                this,
                serverSpellGo.TargetInfoData.Count,
                serverSpellGo.TargetInfoData.Sum(t => t.EffectInfoData.Count),
                combatLogs.Count);

            Caster.EnqueueToVisible(serverSpellGo, true);
            SendDiagnosticSpellBroadcasts();
            SendPostSpellGoEffectMessages(pendingEffects);

        }

        private void SendPostSpellGoEffectMessages(IEnumerable<PendingSpellGoEffect> pendingEffects)
        {
            foreach (PendingSpellGoEffect pendingEffect in pendingEffects)
            {
                SpellEffectInterpretation effect = SpellEffectInterpreter.Interpret(pendingEffect.EffectInfo);
                if (effect.CCState == null)
                    continue;

                Caster.EnqueueToVisible(new ServerEntityCCStateSet
                {
                    UnitId              = pendingEffect.TargetInfo.Entity.Guid,
                    CCType              = effect.CCState.State,
                    SpellEffectUniqueId = pendingEffect.EffectInfo.EffectId
                }, true);
            }
        }

        private void SendCCStateRemove(uint unitId, CCState state, uint effectId)
        {
            Caster.EnqueueToVisible(new ServerEntityCCStateRemove
            {
                UnitId              = unitId,
                CCType              = state,
                SpellCastUniqueId   = CastingId,
                SpellEffectUniqueId = effectId,
                Removed             = true
            }, true);
        }

        private void SendRemoveBuff(uint unitId)
        {
            if (!Parameters.SpellInfo.BaseInfo.HasIcon)
                return;

            Caster.EnqueueToVisible(new ServerSpellBuffRemove
            {
                CastingId = CastingId,
                CasterId  = unitId
            }, true);
        }

        private void SendStealthCombatLog(uint unitId, bool exiting)
        {
            Caster.EnqueueToVisible(new ServerCombatLog
            {
                CombatLog = new CombatLogStealth
                {
                    UnitId   = unitId,
                    BExiting = exiting
                }
            }, true);
        }
    }
}
