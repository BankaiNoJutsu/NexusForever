using NexusForever.Game.Static.Hazard;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Hazard;

namespace NexusForever.Game.Entity
{
    internal sealed class HazardStateCollection
    {
        private const uint StartsFullFlag = 0x1u;

        private sealed class ActiveHazardState
        {
            public HazardEntry Entry { get; }
            public HashSet<uint> EnablingEffects { get; } = [];
            public float MeterValue { get; set; }
            public DateTime LastUpdateUtc { get; set; }

            public ActiveHazardState(HazardEntry entry, DateTime nowUtc)
            {
                Entry         = entry;
                MeterValue    = (entry.Flags & StartsFullFlag) != 0u ? entry.MeterMaxValue : 0f;
                LastUpdateUtc = nowUtc;
            }
        }

        private sealed record SuspensionState(uint HazardId, HazardType HazardType, uint TargetMode);

        private readonly Dictionary<uint, ActiveHazardState> activeHazards = [];
        private readonly Dictionary<uint, uint> enablingEffects = [];
        private readonly Dictionary<uint, SuspensionState> suspensionEffects = [];
        private readonly Func<DateTime> utcNow;

        public HazardStateCollection(Func<DateTime> utcNow = null)
        {
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public bool TryEnable(uint effectId, HazardEntry entry, out bool firstEnable, out string skippedReason)
        {
            firstEnable  = false;
            skippedReason = null;

            if (!IsValidEntry(entry))
            {
                skippedReason = "invalid-hazard";
                return false;
            }

            if (enablingEffects.ContainsKey(effectId))
            {
                skippedReason = "duplicate-effect";
                return false;
            }

            DateTime nowUtc = utcNow();
            if (!activeHazards.TryGetValue(entry.Id, out ActiveHazardState state))
            {
                state = new ActiveHazardState(entry, nowUtc);
                activeHazards.Add(entry.Id, state);
                firstEnable = true;
            }
            else
            {
                Advance(state, nowUtc);
            }

            state.EnablingEffects.Add(effectId);
            enablingEffects.Add(effectId, entry.Id);
            return true;
        }

        public bool RemoveEnable(uint effectId, out uint hazardId, out bool lastEnable)
        {
            hazardId  = 0u;
            lastEnable = false;
            if (!enablingEffects.Remove(effectId, out hazardId)
                || !activeHazards.TryGetValue(hazardId, out ActiveHazardState state))
                return false;

            state.EnablingEffects.Remove(effectId);
            if (state.EnablingEffects.Count == 0)
            {
                activeHazards.Remove(hazardId);
                lastEnable = true;
            }

            return true;
        }

        public bool TryModifyMeter(uint hazardId, float amount, out string skippedReason)
        {
            skippedReason = null;
            if (!float.IsFinite(amount))
            {
                skippedReason = "invalid-amount";
                return false;
            }

            if (!activeHazards.TryGetValue(hazardId, out ActiveHazardState state))
            {
                skippedReason = "hazard-not-enabled";
                return false;
            }

            Advance(state, utcNow());
            state.MeterValue = Math.Clamp(state.MeterValue + amount, 0f, state.Entry.MeterMaxValue);
            return true;
        }

        public bool TrySuspend(uint effectId, HazardEntry entry, uint targetMode, out string skippedReason)
        {
            skippedReason = null;
            if (!IsValidEntry(entry))
            {
                skippedReason = "invalid-hazard";
                return false;
            }

            // Retail rows select either the exact Hazard.tbl row (0) or that row's hazard type (2).
            if (targetMode is not 0u and not 2u)
            {
                skippedReason = "unsupported-target-mode";
                return false;
            }

            if (suspensionEffects.ContainsKey(effectId))
            {
                skippedReason = "duplicate-effect";
                return false;
            }

            AdvanceAll(utcNow());
            suspensionEffects.Add(effectId, new SuspensionState(entry.Id, (HazardType)entry.HazardTypeEnum, targetMode));
            return true;
        }

        public bool RemoveSuspension(uint effectId)
        {
            AdvanceAll(utcNow());
            return suspensionEffects.Remove(effectId);
        }

        public List<Hazard> BuildHazards()
        {
            AdvanceAll(utcNow());
            return activeHazards.Values
                .OrderBy(state => state.Entry.Id)
                .Select(BuildHazard)
                .ToList();
        }

        private void AdvanceAll(DateTime nowUtc)
        {
            foreach (ActiveHazardState state in activeHazards.Values)
                Advance(state, nowUtc);
        }

        private void Advance(ActiveHazardState state, DateTime nowUtc)
        {
            double elapsedSeconds = (nowUtc - state.LastUpdateUtc).TotalSeconds;
            state.LastUpdateUtc = nowUtc;
            if (elapsedSeconds <= 0d
                || IsSuspended(state.Entry.Id, (HazardType)state.Entry.HazardTypeEnum)
                || state.Entry.MeterChangeRate == 0f)
                return;

            float meterDelta = state.Entry.MeterChangeRate * (float)elapsedSeconds;
            state.MeterValue = Math.Clamp(state.MeterValue + meterDelta, 0f, state.Entry.MeterMaxValue);
        }

        public ServerHazardModifiers BuildModifiers(int hazardIdCount)
        {
            var packet = new ServerHazardModifiers
            {
                HazardIdModifiers   = CreateDefaultModifiers(hazardIdCount),
                HazardTypeModifiers = CreateDefaultModifiers(Enum.GetValues<HazardType>().Length)
            };

            foreach (SuspensionState suspension in suspensionEffects.Values)
            {
                List<HazardModifier> modifiers = suspension.TargetMode == 2u
                    ? packet.HazardTypeModifiers
                    : packet.HazardIdModifiers;
                int index = suspension.TargetMode == 2u
                    ? (int)suspension.HazardType
                    : checked((int)suspension.HazardId);

                if ((uint)index < (uint)modifiers.Count)
                    modifiers[index].Suspended = true;
            }

            return packet;
        }

        private Hazard BuildHazard(ActiveHazardState state)
        {
            HazardEntry entry = state.Entry;
            bool startsFull = (entry.Flags & StartsFullFlag) != 0u;
            uint threshold = CalculateThreshold(entry, state.MeterValue, startsFull);

            return new Hazard
            {
                HazardId        = (ushort)entry.Id,
                Type            = (HazardType)entry.HazardTypeEnum,
                MeterValue      = state.MeterValue,
                MaxValue        = entry.MeterMaxValue,
                CurrentThreshold = threshold,
                ProcSpell4Id    = ResolveThresholdProc(entry, threshold),
                HazardUnitId    = 0u,
                PulseTimeLeft   = 0u,
                UnitBased       = false,
                StartsFull      = startsFull,
                Enabled         = true,
                Suspended       = IsSuspended(entry.Id, (HazardType)entry.HazardTypeEnum),
                DoNotRefill     = false
            };
        }

        private bool IsSuspended(uint hazardId, HazardType hazardType)
        {
            return suspensionEffects.Values.Any(suspension =>
                suspension.TargetMode == 2u
                    ? suspension.HazardType == hazardType
                    : suspension.HazardId == hazardId);
        }

        private static List<HazardModifier> CreateDefaultModifiers(int count)
        {
            return Enumerable.Range(0, Math.Max(0, count))
                .Select(_ => new HazardModifier
                {
                    Multiplier = 1f,
                    Offset     = 0f,
                    Suspended  = false
                })
                .ToList();
        }

        private static uint CalculateThreshold(HazardEntry entry, float meterValue, bool startsFull)
        {
            if (entry.MeterMaxValue == 0u)
                return 0u;

            float thresholdValue = startsFull ? entry.MeterMaxValue - meterValue : meterValue;
            float percentage = thresholdValue / entry.MeterMaxValue * 100f;
            float[] thresholds = [entry.MeterThreshold00, entry.MeterThreshold01, entry.MeterThreshold02];

            uint crossed = 0u;
            foreach (float threshold in thresholds)
            {
                if (threshold <= 0f || percentage <= threshold)
                    break;

                crossed++;
            }

            return crossed;
        }

        private static uint ResolveThresholdProc(HazardEntry entry, uint threshold)
        {
            return threshold switch
            {
                1u => entry.Spell4IdThresholdProc00,
                2u => entry.Spell4IdThresholdProc01,
                3u => entry.Spell4IdThresholdProc02,
                _  => 0u
            };
        }

        private static bool IsValidEntry(HazardEntry entry)
        {
            return entry != null
                && entry.Id <= 0x3FFFu
                && entry.MeterMaxValue > 0u
                && Enum.IsDefined(typeof(HazardType), (int)entry.HazardTypeEnum);
        }
    }
}
