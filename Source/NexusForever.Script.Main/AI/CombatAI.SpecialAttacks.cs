using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Script.Main.AI
{
    public partial class CombatAI
    {
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
            double lockoutSeconds = (spell4Entry.CastTime + (double)spell4Entry.ChannelMaxTime) / 1000d;
            if (lockoutSeconds <= 0d)
                return;

            specialCastLockoutSeconds = Math.Max(specialCastLockoutSeconds, lockoutSeconds);
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
    }
}
