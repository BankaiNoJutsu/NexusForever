using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
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
        private const uint WhirlwindSpell4BaseId = 19778u;
        private const uint RampageSpell4BaseId = 37968u;
        private const uint EngineerArtillerybotBarrageTargetFinderSpell4Id = 49502u;
        private const uint EngineerArtillerybotBarrageTargetFinderBaseSpell4Id = 32710u;
        private const uint EngineerArtillerybotBarragePulseSpell4Id = 34589u;
        private const uint EngineerArtillerybotBarragePulseBaseSpell4Id = 20559u;
        private const uint EngineerArtillerybotBarrageDamageSpell4Id = 35548u;
        private const uint EngineerArtillerybotBarrageDamageBaseSpell4Id = 21229u;
        private const uint WarriorKineticAbilityCost = 250u;

        public ISpellParameters Parameters { get; }
        public uint CastingId { get; }
        public bool IsCasting => status == SpellStatus.Casting || status == SpellStatus.Waiting;
        public bool BlocksCasting => status == SpellStatus.Casting || status == SpellStatus.Waiting || (status == SpellStatus.Executing && (awaitingInitialImpact || channelCompletePending));
        public bool IsFinished => status == SpellStatus.Finished;

        public IUnitEntity Caster { get; }
        private readonly IPrerequisiteManager prerequisiteManager;
        private readonly IGlobalSpellManager globalSpellManager;
        private readonly IScriptManager scriptManager;
        private readonly IGameTableManager gameTableManager;

        private SpellStatus status;
        private bool cancelled;
        private bool awaitingInitialImpact;
        private bool channelCompletePending;
        private bool casterVitalCostsPerChannelPulse;

        private readonly List<ISpellTargetInfo> targets = new();
        private readonly List<ITelegraph> telegraphs = new();
        private readonly List<PendingSpellGoEffect> pendingSpellGoEffects = new();
        private readonly Dictionary<ISpellTargetEffectInfo, ISpellEvent> lifetimeEvents = new();
        private readonly HashSet<(uint TargetId, uint EffectEntryId)> terminatedPersistentEffects = new();
        private uint chargeThresholdValue;

        private readonly ISpellEventManager events = new SpellEventManager();

        private IScriptCollection scriptCollection;

        private sealed record PendingSpellGoEffect(ISpellTargetInfo TargetInfo, ISpellTargetEffectInfo EffectInfo);
        private readonly record struct CasterVitalCost(Vital Vital, uint Amount, CasterVitalCostCadence Cadence);

        private enum CasterVitalCostCadence
        {
            Once,
            ChannelPulse
        }

        public Spell(
            IUnitEntity caster,
            ISpellParameters parameters,
            IPrerequisiteManager prerequisiteManager = null,
            IGlobalSpellManager globalSpellManager = null,
            IScriptManager scriptManager = null,
            IGameTableManager gameTableManager = null)
        {
            Caster     = caster;
            Parameters = parameters;
            this.prerequisiteManager = prerequisiteManager;
            this.globalSpellManager = globalSpellManager;
            this.scriptManager = scriptManager;
            this.gameTableManager = gameTableManager;
            CastingId  = GetGlobalSpellManager().NextCastingId;
            status     = SpellStatus.Initiating;

            parameters.RootSpellInfo ??= parameters.SpellInfo;

            scriptCollection = GetScriptManager().InitialiseOwnedScripts<ISpell>(this, parameters.SpellInfo.Entry.Id);
        }

        public void Dispose()
        {
            SpellRuntimeEvidenceCollector.FinalizeAndExport(this, status == SpellStatus.Finished ? "disposed-finished" : "disposed");

            if (scriptCollection != null)
                GetScriptManager().Unload(scriptCollection);

            scriptCollection = null;
        }

        public void Update(double lastTick)
        {
            scriptCollection.Invoke<IUpdate>(s => s.Update(lastTick));

            events.Update(lastTick);
            UpdatePersistence();

            if (status == SpellStatus.Executing && !events.HasPendingEvent)
            {
                if (Parameters.WaitForClientSideInteractionResponse)
                    return;

                // spell effects have finished executing
                status = SpellStatus.Finished;
                log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} has finished.");

                Finish();
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
                if (UsesGlobalCooldown())
                    player.SpellManager.SetGlobalSpellCooldown(Parameters.SpellInfo.GlobalCooldown.Id, Parameters.SpellInfo.GlobalCooldown.CooldownTime / 1000d);

            if (Caster is not IPlayer || IsChargeReleaseThresholdSpell())
                InitialiseTelegraphs();

            scriptCollection.Invoke<ISpellScript>(s => s.OnCast(this));
            SendSpellStart();

            status = SpellStatus.Casting;
            uint castTime = GetEffectiveCastTime();
            if (castTime == 0u)
                Execute();
            else
                events.EnqueueEvent(new SpellEvent(castTime / 1000d, Execute));

            if (IsChargeReleaseThresholdSpell())
                ScheduleChargeThresholdEvents();

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

                if (UsesGlobalCooldown() && player.SpellManager.GetGlobalSpellCooldown() > 0d)
                    return CastResult.SpellGlobalCooldown;

                if (Parameters.CharacterSpell?.MaxAbilityCharges > 0 && Parameters.CharacterSpell?.AbilityCharges == 0)
                    return CastResult.SpellNoCharges;

                CastResult casterVitalCostResult = CheckCasterVitalCosts();
                if (casterVitalCostResult != CastResult.Ok)
                    return casterVitalCostResult;

                CastResult serviceTokenCostResult = CheckServiceTokenCost(player);
                if (serviceTokenCostResult != CastResult.Ok)
                    return serviceTokenCostResult;
            }

            CastResult targetResult = CheckPrimaryTarget();
            if (targetResult != CastResult.Ok)
                return targetResult;

            return CastResult.Ok;
        }

        private bool UsesGlobalCooldown()
        {
            return !Parameters.IgnoreGlobalCooldown
                && Parameters.SpellInfo.GlobalCooldown != null
                && Parameters.SpellInfo.Entry.GlobalCooldownEnum < 2u;
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
            uint validTargetMask = Parameters.SpellInfo.BaseInfo.ValidTargets?.TargetBitmask ?? 0u;
            if (validTargetMask == 0u)
                return CastResult.Ok;

            if ((validTargetMask & ValidTargetObjectMask) != 0u && IsInteractableObjectTarget(target))
                return CastResult.Ok;

            if (target is not IUnitEntity unitTarget)
                return CastResult.TargetUnknown;

            CastResult result = CheckTargetLivingState(unitTarget, AllowsObjectMaskAsLivingUnitTarget(unitTarget));
            if (result != CastResult.Ok)
                return result;

            return IsHostileToTarget(unitTarget) && UnitStateSetRules.TryGetHostileEffectImmuneState(unitTarget, out _)
                ? CastResult.TargetInvulnerable
                : CastResult.Ok;
        }

        private static bool IsInteractableObjectTarget(IWorldEntity target)
        {
            // Simple entities are used for many interactable world objects even though the
            // server models them as units for entity-create/stat purposes.
            return target is not IUnitEntity || target.Type == EntityType.Simple;
        }

        private bool AllowsObjectMaskAsLivingUnitTarget(IUnitEntity target)
        {
            uint validTargetMask = Parameters.SpellInfo.BaseInfo.ValidTargets?.TargetBitmask ?? 0u;
            if ((validTargetMask & ValidTargetObjectMask) == 0u)
                return false;

            if (IsTargetActivateSpell(target))
                return true;

            if (ReferenceEquals(target, Caster) || (target.Guid != 0u && target.Guid == Caster.Guid))
                return true;

            TargetGroupEntry castGroup = Parameters.SpellInfo.BaseInfo.CastGroup;
            return castGroup != null && TargetGroupCriteriaEvaluator.Evaluate(castGroup, target, GetGameTableManager());
        }

        private bool IsTargetActivateSpell(IUnitEntity target)
        {
            uint spell4Id = Parameters.SpellInfo?.Entry?.Id ?? 0u;
            Creature2Entry creatureEntry = target.CreatureEntry;
            return spell4Id != 0u
                && creatureEntry != null
                && (creatureEntry.Spell4IdActivate00 == spell4Id
                    || creatureEntry.Spell4IdActivate01 == spell4Id
                    || creatureEntry.Spell4IdActivate02 == spell4Id
                    || creatureEntry.Spell4IdActivate03 == spell4Id);
        }

        private CastResult CheckPrimaryTargetCastGroup(IWorldEntity target)
        {
            TargetGroupEntry castGroup = Parameters.SpellInfo.BaseInfo.CastGroup;
            if (castGroup == null)
                return CastResult.Ok;

            IWorldEntity castGroupTarget = target;
            if (Parameters.UseCreatureOverrides && target is IUnitEntity unitTarget && IsTargetActivateSpell(unitTarget))
                castGroupTarget = Caster;

            return TargetGroupCriteriaEvaluator.Evaluate(castGroup, castGroupTarget, GetGameTableManager())
                ? CastResult.Ok
                : CastResult.TargetUnknown;
        }

        private bool IsHostileToTarget(IUnitEntity target)
        {
            return Caster.GetDispositionTo(target.Faction1) < Disposition.Friendly;
        }

        private CastResult CheckTargetLivingState(IUnitEntity target, bool allowObjectMaskAsLivingTarget = false)
        {
            uint validTargetMask = Parameters.SpellInfo.BaseInfo.ValidTargets?.TargetBitmask ?? 0u;
            if (validTargetMask == 0u)
                return CastResult.Ok;

            bool allowsDeadTargets = (validTargetMask & ValidTargetDeadMask) != 0u;
            bool allowsLivingTargets = (validTargetMask & ~(ValidTargetDeadMask | ValidTargetObjectMask)) != 0u
                || (allowObjectMaskAsLivingTarget && (validTargetMask & ValidTargetObjectMask) != 0u);

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
            if (Parameters.SkipPrimaryTargetRangeValidation)
                return CastResult.Ok;

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

            var prerequisiteParameters = new PrerequisiteParameters
            {
                TaxiNode = Parameters.TaxiNode
            };

            if (Parameters.SpellInfo.CasterCastPrerequisite != null && !CheckRunnerOverride(player, prerequisiteParameters))
            {
                if (!GetPrerequisiteManager().Meets(player, Parameters.SpellInfo.CasterCastPrerequisite.Id, prerequisiteParameters))
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

        private bool CheckRunnerOverride(IPlayer player, PrerequisiteParameters prerequisiteParameters)
        {
            foreach (PrerequisiteEntry runnerPrereq in Parameters.SpellInfo.PrerequisiteRunners)
                if (GetPrerequisiteManager().Meets(player, runnerPrereq.Id, prerequisiteParameters))
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
            return MeetsApplyPrerequisite(GetPrerequisite(effect.Entry.PrerequisiteIdCasterApply), Caster, prerequisiteManager)
                && MeetsApplyPrerequisite(GetPrerequisite(effect.Entry.PrerequisiteIdTargetApply), target, prerequisiteManager);
        }

        private PrerequisiteEntry GetPrerequisite(uint prerequisiteId)
        {
            return prerequisiteId == 0u ? null : gameTableManager?.Prerequisite?.GetEntry(prerequisiteId);
        }

        internal static bool MeetsApplyPrerequisite(
            PrerequisiteEntry prerequisite,
            IWorldEntity entity,
            IPrerequisiteManager prerequisiteManager = null)
        {
            if (prerequisite == null)
                return true;

            if (TryEvaluateCreatureDifficultyPrerequisite(prerequisite, entity, out bool result))
                return result;

            if (entity is not IPlayer player)
                return true;

            return GetPrerequisiteManager(prerequisiteManager).Meets(player, prerequisite.Id);
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
                && MeetsPersistencePrerequisite(gameTableManager?.Prerequisite?.GetEntry(effect.Entry.PrerequisiteIdCasterPersistence), Caster)
                && MeetsPersistencePrerequisite(gameTableManager?.Prerequisite?.GetEntry(effect.Entry.PrerequisiteIdTargetPersistence), target);
        }

        private bool MeetsPersistencePrerequisite(PrerequisiteEntry prerequisite, IWorldEntity entity)
        {
            if (prerequisite == null)
                return true;

            if (entity is not IPlayer player)
                return true;

            return GetPrerequisiteManager().Meets(player, prerequisite.Id);
        }

        private IPrerequisiteManager GetPrerequisiteManager()
        {
            return GetPrerequisiteManager(prerequisiteManager);
        }

        private static IPrerequisiteManager GetPrerequisiteManager(IPrerequisiteManager prerequisiteManager)
        {
            return prerequisiteManager ?? throw new InvalidOperationException($"{nameof(Spell)} requires an {nameof(IPrerequisiteManager)}.");
        }

        private IGlobalSpellManager GetGlobalSpellManager()
        {
            return globalSpellManager ?? throw new InvalidOperationException($"{nameof(Spell)} requires an {nameof(IGlobalSpellManager)}.");
        }

        private IScriptManager GetScriptManager()
        {
            return scriptManager ?? throw new InvalidOperationException($"{nameof(Spell)} requires an {nameof(IScriptManager)}.");
        }

        private IGameTableManager GetGameTableManager()
        {
            return gameTableManager ?? throw new InvalidOperationException($"{nameof(Spell)} requires an {nameof(IGameTableManager)}.");
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

            SendSpellCastCancel(result);

            if (IsChargeReleaseThresholdSpell())
                SendThresholdClear();

            events.CancelEvents();
            awaitingInitialImpact = false;
            channelCompletePending = false;
            cancelled = true;
            status = SpellStatus.Finished;

            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} cast was cancelled.");
            Finish();
        }

        private void SendSpellCastCancel(CastResult result)
        {
            if (Caster is not IPlayer player || player.IsLoading)
                return;

            player.Session.EnqueueMessageEncrypted(new ServerSpellCastCancel
            {
                ServerUniqueId = CastingId,
                CastResult     = result,
                CancelCast     = true
            });
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
                Finish();
            }

            return true;
        }

        bool ISpell.TryCancelEffect(IUnitEntity requester) => TryCancelEffect(requester);

        public bool TryCompleteClientSideInteraction(bool wasCancelled)
        {
            if (!Parameters.WaitForClientSideInteractionResponse || status != SpellStatus.Executing)
                return false;

            events.CancelEvents();
            cancelled = wasCancelled;
            status = SpellStatus.Finished;

            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} client-side interaction completed, cancelled={wasCancelled}.");
            Finish();
            return true;
        }

        public bool TryReleaseChargeSpell(ICharacterSpell characterSpell, uint rootSpell4Id, uint primaryTargetId, uint clientContextToken = 0u, string clientRequestSource = null)
        {
            if (!IsChargeReleaseThresholdSpell() || status != SpellStatus.Waiting)
                return false;

            if (!MatchesChargeReleaseSpell(characterSpell, rootSpell4Id))
                return false;

            ISpellInfo thresholdSpellInfo = Parameters.SpellInfo.GetThresholdSpellInfo(chargeThresholdValue, out Spell4ThresholdsEntry thresholdEntry);
            if (thresholdSpellInfo == null || thresholdEntry == null)
                return false;

            uint resolvedPrimaryTargetId = primaryTargetId != 0u ? primaryTargetId : Parameters.PrimaryTargetId;
            var childParameters = new SpellParameters
            {
                ParentSpellInfo        = Parameters.SpellInfo,
                RootSpellInfo          = Parameters.RootSpellInfo ?? Parameters.SpellInfo,
                PrimaryTargetId        = resolvedPrimaryTargetId,
                UserInitiatedSpellCast = Parameters.UserInitiatedSpellCast,
                IgnoreGlobalCooldown   = true,
                ClientContextToken     = clientContextToken != 0u ? clientContextToken : Parameters.ClientContextToken,
                ClientRequestSource    = clientRequestSource ?? Parameters.ClientRequestSource,
                Position               = Parameters.Position
            };

            CastResult result = Caster.TryCastSpell(thresholdSpellInfo.Entry.Id, childParameters);
            if (result == CastResult.Ok)
                ApplyChargeReleaseCostAndCooldown();

            FinishChargeReleaseShell(result != CastResult.Ok);
            return true;
        }

        private void Execute()
        {
            status = SpellStatus.Executing;
            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} has started executing.");

            bool chargeReleaseThresholdSpell = IsChargeReleaseThresholdSpell();

            if (!chargeReleaseThresholdSpell && Caster is IPlayer player)
                if (Parameters.SpellInfo.Entry.SpellCoolDown != 0u)
                    player.SpellManager.SetSpellCooldown(Parameters.SpellInfo.Entry.Id, Parameters.SpellInfo.Entry.SpellCoolDown / 1000d);

            SelectTargets();
            scriptCollection.Invoke<ISpellScript>(s => s.OnExecute(this, targets.AsReadOnly()));
            ExecuteEffects();
            ScheduleChannelCompletion();
            ScheduleClientSideInteractionTimeout();

            if (Caster is IPlayer executingPlayer)
                SpellQuestObjectiveUpdater.UpdateSpellSuccessObjectives(executingPlayer, Parameters.SpellInfo.Entry.Id);

            if (chargeReleaseThresholdSpell)
            {
                SendThresholdStart();
                status = SpellStatus.Waiting;
                return;
            }

            CostSpell();
        }

        private void ScheduleClientSideInteractionTimeout()
        {
            if (!Parameters.WaitForClientSideInteractionResponse)
                return;

            uint durationMs = Parameters.ClientSideInteractionDurationMs;
            if (durationMs == 0u)
                return;

            events.EnqueueEvent(new SpellEvent(durationMs / 1000d, () => TryCompleteClientSideInteraction(wasCancelled: true)));
        }

        private void ScheduleChannelCompletion()
        {
            uint channelMaxTime = Parameters.SpellInfo.Entry.ChannelMaxTime;
            if (channelMaxTime == 0u)
                return;

            channelCompletePending = true;
            events.EnqueueEvent(new SpellEvent(channelMaxTime / 1000d, () => channelCompletePending = false));
        }

        private void CostSpell()
        {
            if (Parameters.CharacterSpell?.MaxAbilityCharges > 0)
                Parameters.CharacterSpell.UseCharge();

            if (!casterVitalCostsPerChannelPulse)
                ConsumeCasterVitalCosts(CasterVitalCostCadence.Once);
        }

        private CastResult CheckCasterVitalCosts(CasterVitalCostCadence? cadence = null)
        {
            if (Caster is not IPlayer)
                return CastResult.Ok;

            IEnumerable<(Vital Vital, uint Amount)> costs = cadence.HasValue
                ? GetCasterVitalCosts(cadence.Value)
                : GetCasterVitalCostsForCheck();

            foreach ((Vital vital, uint amount) in costs)
            {
                if (WarriorOverdriveMechanic.FreezesKineticVital(Caster, vital))
                    continue;

                if (!Caster.TryGetVitalValue(vital, out float value) || value + 0.0001f < amount)
                    return GetCasterVitalCostResult(vital);
            }

            return CastResult.Ok;
        }

        private void ConsumeCasterVitalCosts(CasterVitalCostCadence cadence)
        {
            if (Caster is not IPlayer)
                return;

            foreach ((Vital vital, uint amount) in GetCasterVitalCosts(cadence))
            {
                if (WarriorOverdriveMechanic.FreezesKineticVital(Caster, vital))
                    continue;

                Caster.TryModifyVital(vital, -(float)amount, out _);
            }
        }

        private bool TryConsumeCasterVitalCosts(CasterVitalCostCadence cadence, out CastResult result)
        {
            result = CheckCasterVitalCosts(cadence);
            if (result != CastResult.Ok)
                return false;

            ConsumeCasterVitalCosts(cadence);
            return true;
        }

        private bool TryConsumeChannelPulseCasterVitalCosts()
        {
            if (!casterVitalCostsPerChannelPulse)
                return true;

            if (TryConsumeCasterVitalCosts(CasterVitalCostCadence.ChannelPulse, out CastResult result))
                return true;

            StopChannelForCasterVitalCost(result);
            return false;
        }

        private IEnumerable<(Vital Vital, uint Amount)> GetCasterVitalCostsForCheck()
        {
            Spell4Entry entry = Parameters.SpellInfo.Entry;
            foreach ((Vital vital, uint amount) in GetEntryCasterVitalCosts(entry))
                yield return (vital, amount);

            foreach (CasterVitalCost cost in GetSupplementalCasterVitalCosts(entry))
                yield return (cost.Vital, cost.Amount);
        }

        private IEnumerable<(Vital Vital, uint Amount)> GetCasterVitalCosts(CasterVitalCostCadence cadence)
        {
            Spell4Entry entry = Parameters.SpellInfo.Entry;
            if (casterVitalCostsPerChannelPulse == (cadence == CasterVitalCostCadence.ChannelPulse))
                foreach ((Vital vital, uint amount) in GetEntryCasterVitalCosts(entry))
                    yield return (vital, amount);

            foreach (CasterVitalCost cost in GetSupplementalCasterVitalCosts(entry))
                if (cost.Cadence == cadence)
                    yield return (cost.Vital, cost.Amount);
        }

        private static IEnumerable<(Vital Vital, uint Amount)> GetEntryCasterVitalCosts(Spell4Entry entry)
        {
            if (TryCreateCasterVitalCost(entry.InnateCostType0, entry.InnateCost0, out (Vital Vital, uint Amount) cost0))
                yield return cost0;

            if (TryCreateCasterVitalCost(entry.InnateCostType1, entry.InnateCost1, out (Vital Vital, uint Amount) cost1))
                yield return cost1;
        }

        private static bool TryCreateCasterVitalCost(uint vitalType, uint amount, out (Vital Vital, uint Amount) cost)
        {
            cost = default;
            if (vitalType == 0u || amount == 0u || !Enum.IsDefined(typeof(Vital), (int)vitalType))
                return false;

            Vital vital = (Vital)vitalType;
            if (vital == Vital.Invalid)
                return false;

            cost = (vital, amount);
            return true;
        }

        private static bool HasChannelPulseCasterVitalCosts(Spell4Entry entry)
        {
            return GetEntryCasterVitalCosts(entry).Any()
                || GetSupplementalCasterVitalCosts(entry).Any(c => c.Cadence == CasterVitalCostCadence.ChannelPulse);
        }

        private static IEnumerable<CasterVitalCost> GetSupplementalCasterVitalCosts(Spell4Entry entry)
        {
            if (HasEntryKineticCost(entry))
                yield break;

            if (entry.Spell4BaseIdBaseSpell == WhirlwindSpell4BaseId)
                yield return new CasterVitalCost(Vital.Resource1, WarriorKineticAbilityCost, CasterVitalCostCadence.ChannelPulse);

            if (entry.Spell4BaseIdBaseSpell == RampageSpell4BaseId)
                yield return new CasterVitalCost(Vital.Resource1, WarriorKineticAbilityCost, CasterVitalCostCadence.Once);
        }

        private static bool HasEntryKineticCost(Spell4Entry entry)
        {
            return GetEntryCasterVitalCosts(entry)
                .Any(c => c.Amount != 0u && IsKineticVital(c.Vital));
        }

        private static bool IsKineticVital(Vital vital)
        {
            return vital is Vital.KineticCell or Vital.Resource1;
        }

        private static CastResult GetCasterVitalCostResult(Vital vital)
        {
            return vital switch
            {
                Vital.Health            => CastResult.CasterVitalCostHealth,
                Vital.ShieldCapacity    => CastResult.CasterVitalCostShieldCapacity,
                Vital.KineticCell       => CastResult.CasterVitalCostResource1,
                Vital.Resource0         => CastResult.CasterVitalCostResource0,
                Vital.Resource1         => CastResult.CasterVitalCostResource1,
                Vital.Resource2         => CastResult.CasterVitalCostResource2,
                Vital.Resource3         => CastResult.CasterVitalCostResource3,
                Vital.Resource4         => CastResult.CasterVitalCostResource4,
                Vital.Resource5         => CastResult.CasterVitalCostResource5,
                Vital.Resource6         => CastResult.CasterVitalCostResource6,
                Vital.StalkerA          => CastResult.CasterVitalCostResource3,
                Vital.StalkerB          => CastResult.CasterVitalCostResource3,
                Vital.StalkerC          => CastResult.CasterVitalCostResource3,
                Vital.Focus             => CastResult.CasterVitalCostFocus,
                Vital.Resource7         => CastResult.CasterVitalCostResource7,
                Vital.MedicCore         => CastResult.CasterVitalCostResource1,
                Vital.SpellSurge        => CastResult.CasterVitalCostResource4,
                Vital.InterruptArmor    => CastResult.CasterVitalCostInterruptArmor,
                Vital.Absorption        => CastResult.CasterVitalCostAbsorption,
                Vital.PublicResource0   => CastResult.CasterVitalCostPublicResource0,
                Vital.PublicResource1   => CastResult.CasterVitalCostPublicRes,
                Vital.PublicResource2   => CastResult.CasterVitalCostPublicResource2,
                Vital.Volatility        => CastResult.CasterVitalCostResource1,
                Vital.Resource8         => CastResult.CasterVitalCostResource8,
                Vital.Resource9         => CastResult.CasterVitalCostResource9,
                Vital.Resource10        => CastResult.CasterVitalCostResource10,
                _                       => CastResult.CasterVitalCost
            };
        }

        private bool IsChargeReleaseThresholdSpell()
        {
            return Parameters.ParentSpellInfo == null
                && Parameters.SpellInfo.BaseInfo.CastMethod == SpellCastMethod.ChargeRelease
                && (Parameters.SpellInfo.Thresholds?.Count ?? 0) != 0;
        }

        private bool MatchesChargeReleaseSpell(ICharacterSpell characterSpell, uint rootSpell4Id)
        {
            if (characterSpell != null && ReferenceEquals(Parameters.CharacterSpell, characterSpell))
                return true;

            if (rootSpell4Id == 0u)
                return false;

            return Parameters.SpellInfo.Entry.Id == rootSpell4Id
                || Parameters.RootSpellInfo?.Entry.Id == rootSpell4Id;
        }

        private void ScheduleChargeThresholdEvents()
        {
            uint nextThresholdTime = Parameters.SpellInfo.Entry.CastTime;
            foreach (Spell4ThresholdsEntry threshold in Parameters.SpellInfo.Thresholds.OrderBy(t => t.OrderIndex))
            {
                nextThresholdTime += threshold.ThresholdDuration;
                if (threshold.OrderIndex == 0u)
                    continue;

                uint thresholdValue = threshold.OrderIndex;
                events.EnqueueEvent(new SpellEvent(nextThresholdTime / 1000d, () =>
                {
                    if (status != SpellStatus.Waiting)
                        return;

                    chargeThresholdValue = thresholdValue;
                    SendThresholdUpdate();
                }));
            }

            uint thresholdTime = Parameters.SpellInfo.Entry.ThresholdTime;
            if (thresholdTime == 0u)
                return;

            events.EnqueueEvent(new SpellEvent(thresholdTime / 1000d, () =>
            {
                if (status != SpellStatus.Waiting)
                    return;

                TryReleaseChargeSpell(
                    Parameters.CharacterSpell,
                    Parameters.RootSpellInfo?.Entry.Id ?? Parameters.SpellInfo.Entry.Id,
                    Parameters.PrimaryTargetId,
                    Parameters.ClientContextToken,
                    Parameters.ClientRequestSource);
            }));
        }

        private void ApplyChargeReleaseCostAndCooldown()
        {
            if (Caster is IPlayer player && Parameters.SpellInfo.Entry.SpellCoolDown != 0u)
                player.SpellManager.SetSpellCooldown(Parameters.SpellInfo.Entry.Id, Parameters.SpellInfo.Entry.SpellCoolDown / 1000d);

            CostSpell();
        }

        private void FinishChargeReleaseShell(bool wasCancelled)
        {
            if (status == SpellStatus.Finished)
                return;

            events.CancelEvents();
            cancelled = wasCancelled;
            SendThresholdClear();
            status = SpellStatus.Finished;
            Finish();
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

            foreach (TelegraphTargetCandidate candidate in SelectTelegraphTargets())
                AddTarget(SpellEffectTargetFlags.Telegraph, candidate.Entity, candidate.PhaseFlags);

            SpellEffectDiagnostics.TraceTargetSelection(this, targets, telegraphs.Count);
        }

        private uint GetEffectiveCastTime()
        {
            uint spellCastTime = Parameters.SpellInfo.Entry.CastTime;
            if (!Parameters.UseCreatureOverrides)
                return spellCastTime;

            IWorldEntity primaryTarget = GetPrimaryTargetWorldEntity();
            uint creatureCastTime = primaryTarget?.CreatureEntry?.ActivateSpellCastTime ?? 0u;
            return creatureCastTime != 0u ? creatureCastTime : spellCastTime;
        }

        private void AddTarget(SpellEffectTargetFlags flags, IWorldEntity entity, uint telegraphPhaseFlags = 0u)
        {
            SpellTargetInfo target = targets.OfType<SpellTargetInfo>().FirstOrDefault(t => t.Entity.Guid == entity.Guid);
            if (target != null)
            {
                target.AddFlags(flags);
                if ((flags & SpellEffectTargetFlags.Telegraph) != 0)
                    target.SetTelegraphPhaseFlags(telegraphPhaseFlags);

                return;
            }

            targets.Add(new SpellTargetInfo(flags, entity, telegraphPhaseFlags));
        }

        private readonly record struct TelegraphTargetCandidate(IUnitEntity Entity, uint PhaseFlags);

        private IEnumerable<TelegraphTargetCandidate> SelectTelegraphTargets()
        {
            Dictionary<uint, TelegraphTargetCandidate> candidates = [];
            foreach (ITelegraph telegraph in telegraphs)
            {
                foreach (IUnitEntity entity in telegraph.GetTargets())
                    candidates.TryAdd(entity.Guid, new TelegraphTargetCandidate(entity, telegraph.TelegraphDamage.PhaseFlags));
            }

            Spell4AoeTargetConstraintsEntry constraints = Parameters.SpellInfo.AoeTargetConstraints;
            Vector3 selectionOrigin = GetAoeSelectionOrigin();
            Vector3 selectionRotation = GetAoeSelectionRotation(selectionOrigin);
            IEnumerable<IUnitEntity> constrainedCandidates = candidates.Values
                .Select(c => c.Entity)
                .Where(e => MeetsAoeTargetConstraints(e, constraints, selectionOrigin, selectionRotation));

            IEnumerable<IUnitEntity> orderedCandidates = OrderAoeTargetCandidates(constrainedCandidates, constraints, selectionOrigin);

            uint targetCount = constraints?.TargetCount ?? 0u;
            if (targetCount > 0u)
                orderedCandidates = orderedCandidates.Take((int)targetCount);

            return orderedCandidates.Select(e => candidates[e.Guid]);
        }

        private bool MeetsAoeTargetConstraints(IUnitEntity entity, Spell4AoeTargetConstraintsEntry constraints, Vector3 selectionOrigin, Vector3 selectionRotation)
        {
            if (!IsTargetLivingStateAllowed(entity))
                return false;

            if (IsEngineerArtillerybotBarrageHostileAoeSpell() && !Caster.CanAttack(entity))
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
            if (aoeGroup != null && !TargetGroupCriteriaEvaluator.Evaluate(aoeGroup, entity, GetGameTableManager()))
                return false;

            return true;
        }

        private bool IsEngineerArtillerybotBarrageHostileAoeSpell()
        {
            Spell4Entry entry = Parameters.SpellInfo?.Entry;
            if (entry == null)
                return false;

            if (entry.Id is EngineerArtillerybotBarrageTargetFinderSpell4Id
                or EngineerArtillerybotBarragePulseSpell4Id
                or EngineerArtillerybotBarrageDamageSpell4Id)
                return true;

            if (entry.Spell4BaseIdBaseSpell is EngineerArtillerybotBarrageTargetFinderBaseSpell4Id
                or EngineerArtillerybotBarragePulseBaseSpell4Id
                or EngineerArtillerybotBarrageDamageBaseSpell4Id)
                return true;

            uint baseSpell4Id = Parameters.SpellInfo?.BaseInfo?.Entry?.Id ?? 0u;
            return baseSpell4Id is EngineerArtillerybotBarrageTargetFinderBaseSpell4Id
                or EngineerArtillerybotBarragePulseBaseSpell4Id
                or EngineerArtillerybotBarrageDamageBaseSpell4Id;
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
                position = GetCurrentCasterPosition();
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
            Vector3 casterPosition = GetCurrentCasterPosition();
            if (GetHorizontalDistance(casterPosition, position) <= 0.001f)
                return Caster.Rotation;

            return new Vector3(casterPosition.GetAngle(position), Caster.Rotation.Y, Caster.Rotation.Z);
        }

        private Vector3 GetCurrentCasterPosition()
        {
            Vector3 position = Caster.MovementManager?.GetPosition() ?? Caster.Position;
            if (float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z))
                return position;

            return Caster.Position;
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
            List<SpellEffectInterpretation> channelPulseEffects = [];

            foreach (Spell4EffectsEntry spell4EffectsEntry in Parameters.SpellInfo.Effects)
            {
                SpellEffectInterpretation effect = SpellEffectInterpreter.Interpret(spell4EffectsEntry);
                if (IsChannelPulseEffect(effect))
                {
                    channelPulseEffects.Add(effect);
                    continue;
                }

                if (ScheduleEffect(effect))
                {
                    scheduledEffects = true;
                    continue;
                }

                ExecuteEffect(effect);
            }

            if (ScheduleChannelPulseEffects(channelPulseEffects))
                scheduledEffects = true;

            if (status == SpellStatus.Finished)
                return;

            awaitingInitialImpact = scheduledEffects;
            SendSpellGo(!scheduledEffects);
        }

        private bool ScheduleChannelPulseEffects(IReadOnlyList<SpellEffectInterpretation> channelPulseEffects)
        {
            if (channelPulseEffects.Count == 0)
                return false;

            Spell4Entry entry = Parameters.SpellInfo.Entry;
            if (entry.ChannelPulseTime == 0u || entry.ChannelMaxTime == 0u)
                return false;

            uint initialDelay = entry.ChannelInitialDelay;
            if (initialDelay > entry.ChannelMaxTime)
                return false;

            casterVitalCostsPerChannelPulse = HasChannelPulseCasterVitalCosts(entry);

            if (initialDelay == 0u)
            {
                if (!TryConsumeChannelPulseCasterVitalCosts())
                    return true;

                ExecuteChannelPulseEffects(channelPulseEffects);
                ScheduleNextChannelPulse(channelPulseEffects, entry.ChannelPulseTime);
                return true;
            }

            SpellEffectDiagnostics.TraceChannelPulseSchedule(this, initialDelay, entry.ChannelPulseTime, entry.ChannelMaxTime, channelPulseEffects);
            ScheduleChannelPulse(channelPulseEffects, initialDelay, initialDelay);
            return true;
        }

        private bool IsChannelPulseEffect(SpellEffectInterpretation effect)
        {
            Spell4Entry entry = Parameters.SpellInfo.Entry;
            return entry.ChannelPulseTime > 0u
                && entry.ChannelMaxTime > 0u
                && effect.Timing.DelayTime == 0u
                && effect.Timing.TickTime == 0u
                && effect.Timing.DurationTime == 0u
                && IsChannelPulseEffectType(effect.Entry.EffectType);
        }

        private static bool IsChannelPulseEffectType(SpellEffectType effectType)
        {
            return effectType
                is SpellEffectType.VitalModifier
                or SpellEffectType.Transference
                or SpellEffectType.Damage
                or SpellEffectType.Heal
                or SpellEffectType.DistanceDependentDamage
                or SpellEffectType.ProxyLinearAE
                or SpellEffectType.ProxyChannel
                or SpellEffectType.Proxy
                or SpellEffectType.ProxyRandomExclusive
                or SpellEffectType.Absorption
                or SpellEffectType.SapVital
                or SpellEffectType.DistributedDamage
                or SpellEffectType.ProxyChannelVariableTime
                or SpellEffectType.HealShields
                or SpellEffectType.DamageShields
                or SpellEffectType.HealingAbsorption;
        }

        private void ScheduleChannelPulse(IReadOnlyList<SpellEffectInterpretation> channelPulseEffects, uint delayTime, uint elapsedTime)
        {
            events.EnqueueEvent(new SpellEvent(delayTime / 1000d, () =>
            {
                if (!TryConsumeChannelPulseCasterVitalCosts())
                    return;

                if (ExecuteChannelPulseEffects(channelPulseEffects))
                    SendSpellGo();

                uint pulseTime = Parameters.SpellInfo.Entry.ChannelPulseTime;
                if (elapsedTime <= uint.MaxValue - pulseTime)
                    ScheduleNextChannelPulse(channelPulseEffects, elapsedTime + pulseTime);
            }));
        }

        private void ScheduleNextChannelPulse(IReadOnlyList<SpellEffectInterpretation> channelPulseEffects, uint nextElapsedTime)
        {
            Spell4Entry entry = Parameters.SpellInfo.Entry;
            if (nextElapsedTime == 0u || nextElapsedTime > entry.ChannelMaxTime)
                return;

            SpellEffectDiagnostics.TraceChannelPulseSchedule(this, entry.ChannelPulseTime, entry.ChannelPulseTime, entry.ChannelMaxTime, channelPulseEffects);
            ScheduleChannelPulse(channelPulseEffects, entry.ChannelPulseTime, nextElapsedTime);
        }

        private bool ExecuteChannelPulseEffects(IReadOnlyList<SpellEffectInterpretation> channelPulseEffects)
        {
            bool executed = false;
            foreach (SpellEffectInterpretation effect in channelPulseEffects)
                executed |= ExecuteEffect(effect);

            return executed;
        }

        private void StopChannelForCasterVitalCost(CastResult result)
        {
            SpellRuntimeEvidenceCollector.RecordCancellation(this, result);
            SendSpellCastCancel(result);
            events.CancelEvents();
            awaitingInitialImpact = false;
            channelCompletePending = false;
            cancelled = true;
            status = SpellStatus.Finished;
            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} channel stopped because caster vital cost could not be paid.");
            Finish();
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
                .Where(t => IsEffectTargetStillValid(effect, t))
                .ToList();

            SpellEffectDelegate handler = GetGlobalSpellManager().GetEffectHandler((SpellEffectType)effect.Entry.EffectType);
            SpellEffectDiagnostics.TraceEffectDispatch(this, effect, effectTargets.Count, handler != null);

            if (handler == null)
            {
                log.Warn($"Unhandled spell effect {(SpellEffectType)effect.Entry.EffectType} for spell {Parameters.SpellInfo.Entry.Id} (base {Parameters.SpellInfo.BaseInfo.Entry.Id}, effect {effect.Entry.Id}, caster {Caster.Guid}, targetCount {effectTargets.Count}, {FormatClientRequestContext()}).");
                return false;
            }

            uint effectId = GetGlobalSpellManager().NextEffectId;
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
                    log.Error(ex, $"Unhandled exception while executing spell effect {(SpellEffectType)effect.Entry.EffectType} for spell {Parameters.SpellInfo.Entry.Id} on target {unitTarget.Guid} ({FormatClientRequestContext()}).");
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
            foreach (TelegraphTargetCandidate candidate in SelectTelegraphTargets())
                AddTarget(SpellEffectTargetFlags.Telegraph, candidate.Entity, candidate.PhaseFlags);
        }

        internal bool IsEffectTargetStillValid(SpellEffectTargetFlags effectTargetFlags, ISpellTargetInfo targetInfo)
        {
            return IsEffectTargetStillValid(effectTargetFlags, targetInfo, 0u);
        }

        private bool IsEffectTargetStillValid(SpellEffectInterpretation effect, ISpellTargetInfo targetInfo)
        {
            return IsEffectTargetStillValid((SpellEffectTargetFlags)effect.Entry.TargetFlags, targetInfo, effect.Entry.PhaseFlags);
        }

        private bool IsEffectTargetStillValid(SpellEffectTargetFlags effectTargetFlags, ISpellTargetInfo targetInfo, uint effectPhaseFlags)
        {
            if ((effectTargetFlags & SpellEffectTargetFlags.Telegraph) == 0)
                return true;

            if ((targetInfo.Flags & SpellEffectTargetFlags.Telegraph) == 0)
                return true;

            if (targetInfo.Entity is not IUnitEntity unitTarget)
                return false;

            IEnumerable<ITelegraph> matchingTelegraphs = GetMatchingTelegraphs(effectPhaseFlags);
            if (!matchingTelegraphs.Any(t => t.InsideTelegraph(unitTarget.Position, unitTarget.HitRadius)))
                return false;

            if (targetInfo is SpellTargetInfo spellTargetInfo
                && IsSpecificPhase(effectPhaseFlags)
                && IsSpecificPhase(spellTargetInfo.TelegraphPhaseFlags)
                && (spellTargetInfo.TelegraphPhaseFlags & effectPhaseFlags) == 0u)
                return false;

            return true;
        }

        private IEnumerable<ITelegraph> GetMatchingTelegraphs(uint effectPhaseFlags)
        {
            if (!IsSpecificPhase(effectPhaseFlags))
                return telegraphs;

            List<ITelegraph> matchingTelegraphs = telegraphs
                .Where(t => !IsSpecificPhase(t.TelegraphDamage.PhaseFlags) || (t.TelegraphDamage.PhaseFlags & effectPhaseFlags) != 0u)
                .ToList();

            return matchingTelegraphs.Count != 0 ? matchingTelegraphs : telegraphs;
        }

        private static bool IsSpecificPhase(uint phaseFlags)
        {
            return phaseFlags != 0u && phaseFlags != uint.MaxValue;
        }

        private bool ExecuteWorldEntityEffect(SpellEffectInterpretation effect, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            switch ((SpellEffectType)effect.Entry.EffectType)
            {
                case SpellEffectType.Proxy:
                    SpellHandler.HandleEffectProxyWorld(this, target, info);
                    return true;
                case SpellEffectType.ProxyRandomExclusive:
                    SpellHandler.HandleEffectProxyRandomExclusiveWorld(this, target, info);
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
                    log.Warn($"Unhandled world-target spell effect {(SpellEffectType)effect.Entry.EffectType} for target {target.Guid} on spell {Parameters.SpellInfo.Entry.Id} (base {Parameters.SpellInfo.BaseInfo.Entry.Id}, effect {effect.Entry.Id}, caster {Caster.Guid}, {FormatClientRequestContext()}).");
                    return false;
            }
        }

        private string FormatClientRequestContext()
        {
            return $"source={Parameters.ClientRequestSource ?? "unknown"}, contextToken={Parameters.ClientContextToken}, primaryTarget={Parameters.PrimaryTargetId}, position={FormatPosition(Parameters.Position)}";
        }

        private static string FormatPosition(Position position)
        {
            return position == null
                ? "n/a"
                : $"{position.Vector.X},{position.Vector.Y},{position.Vector.Z}";
        }

        private void ScheduleEffectLifetime(SpellEffectInterpretation effect, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            if (!TryGetEffectLifetimeDelay(effect, out uint durationTime))
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
            if (!TryGetEffectLifetimeDelay(effect, out uint durationTime))
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
                case SpellEffectType.UnitPropertyConversion:
                    if (effect.UnitPropertyConversion == null)
                        return null;

                    return () =>
                    {
                        if (target.RemoveSpellProperty(effect.UnitPropertyConversion.TargetProperty, info.EffectId))
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.VendorPriceModifier:
                    if (effect.VendorPriceModifier == null || target is not IPlayer vendorPlayer)
                        return null;

                    return () =>
                    {
                        if (vendorPlayer.RemoveVendorPriceModifier(info.EffectId))
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.HazardEnable:
                    if (effect.HazardEnable == null || target is not IPlayer hazardPlayer)
                        return null;

                    return () =>
                    {
                        if (hazardPlayer.RemoveHazard(info.EffectId))
                            SendRemoveBuff(target.Guid);
                    };
                case SpellEffectType.HazardSuspend:
                    if (effect.HazardSuspend == null || target is not IPlayer suspendedHazardPlayer)
                        return null;

                    return () =>
                    {
                        if (suspendedHazardPlayer.RemoveHazardSuspension(info.EffectId))
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
                case SpellEffectType.Fluff:
                case SpellEffectType.UnlockMount:
                    return () => { };
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
                case SpellEffectType.VectorSlide:
                    if (effect.VectorSlide == null)
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
                case SpellEffectType.SummonPet:
                    if (effect.SummonPet == null || info.CreatedEntities.Count == 0)
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

        internal static bool TryGetEffectLifetimeDelay(SpellEffectInterpretation effect, out uint durationTime)
        {
            durationTime = GetEffectLifetimeDuration(effect);

            // Zero-duration VectorSlide rows are one-shot velocity commands. Queue
            // their removal for the next spell update so the movement owner can
            // broadcast the impulse once without leaving velocity latched.
            return durationTime > 0u || effect.VectorSlide != null;
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
                    ContextToken = Parameters.ClientContextToken,
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
                UseCreatureOverrides   = Parameters.UseCreatureOverrides,
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

        private void Finish()
        {
            scriptCollection.Invoke<ISpellScript>(s => s.OnFinish(this, cancelled));
            Parameters.FinishCallback?.Invoke(this, cancelled);
            SendSpellFinish();
        }

        private void SendThresholdStart()
        {
            if (Caster is not IPlayer player || player.IsLoading)
                return;

            player.Session.EnqueueMessageEncrypted(new ServerSpellThresholdStart
            {
                Spell4Id       = Parameters.SpellInfo.Entry.Id,
                RootSpell4Id   = Parameters.RootSpellInfo?.Entry.Id ?? Parameters.SpellInfo.Entry.Id,
                ParentSpell4Id = Parameters.ParentSpellInfo?.Entry.Id ?? 0u,
                CastingId      = CastingId
            });
        }

        private void SendThresholdUpdate()
        {
            if (Caster is not IPlayer player || player.IsLoading)
                return;

            player.Session.EnqueueMessageEncrypted(new ServerSpellThresholdUpdate
            {
                Spell4Id = Parameters.SpellInfo.Entry.Id,
                Stage    = (byte)chargeThresholdValue
            });
        }

        private void SendThresholdClear()
        {
            if (Caster is not IPlayer player || player.IsLoading)
                return;

            player.Session.EnqueueMessageEncrypted(new ServerSpellThresholdClear
            {
                Spell4Id = Parameters.SpellInfo.Entry.Id
            });
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

                    AddStandaloneCombatLogs(combatLogs, targetEffectInfo);
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

        private static void AddStandaloneCombatLogs(List<ICombatLog> combatLogs, ISpellTargetEffectInfo targetEffectInfo)
        {
            foreach (ICombatLog combatLog in targetEffectInfo.CombatLogs)
            {
                if (ShouldSuppressStandaloneCombatLog(targetEffectInfo, combatLog))
                    continue;

                combatLogs.Add(combatLog);
            }
        }

        private static bool ShouldSuppressStandaloneCombatLog(ISpellTargetEffectInfo targetEffectInfo, ICombatLog combatLog)
        {
            // Plain damage is already carried by ServerSpellGo target effect rows.
            return targetEffectInfo.Damage != null
                && combatLog.GetType() == typeof(CombatLogDamage);
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
