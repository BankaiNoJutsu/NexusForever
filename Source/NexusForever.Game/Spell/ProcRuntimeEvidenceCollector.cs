using System.Collections.Concurrent;
using System.Text.Json;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Spell;
using NLog;

namespace NexusForever.Game.Spell
{
    public static class ProcRuntimeEvidenceCollector
    {
        private const string OutputDirectoryOverrideEnvironmentVariable = "NEXUSFOREVER_PROC_EVIDENCE_DIR";
        private const int MaxObservationsPerHolder = 64;
        private const int MaxTrackedHolders = 256;

        private static readonly ILogger log = LogManager.GetLogger("ProcRuntimeEvidence");
        private static readonly ConcurrentDictionary<uint, ProcRuntimeEvidenceHistory> holderHistory = new();
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true
        };

        private static string outputDirectory;

        public static string GetOutputDirectoryHint()
        {
            return ResolveOutputDirectory();
        }

        public static void RecordProcRegistration(uint holderGuid, uint holderSpell4Id, uint holderCastingId, SpellEffectProcSemantics proc, bool applied, bool removed, string skippedReason)
        {
            if (holderGuid == 0u || proc == null)
                return;

            ProcDispatchEvidenceBoundarySnapshot boundary = ProcDispatchEvidenceBoundary.Describe(proc.TriggerEvent, proc.TargetData);
            RecordObservation(holderGuid, new ProcRuntimeEvidenceObservation
            {
                ObservedAtUtc = DateTime.UtcNow,
                Kind = "registration",
                HolderGuid = holderGuid,
                HolderSpell4Id = holderSpell4Id,
                HolderCastingId = holderCastingId,
                ProcTriggerEvent = proc.TriggerEvent,
                ProcTriggerEventSupported = boundary.TriggerEventSupported,
                ProcTriggerEventLabel = boundary.TriggerEventLabel,
                ProcTriggerSpell4Id = proc.TriggerSpell4Id,
                ProcChance = proc.Chance,
                ProcTargetData = proc.TargetData,
                ProcTargetDataSupported = boundary.TargetDataSupported,
                ProcTargetDataLabel = boundary.TargetDataLabel,
                ProcTargetRoute = boundary.TargetRouteLabel,
                ConservativeDispatchBoundary = boundary.DispatchSupportLabel,
                BlockedReason = boundary.BlockedReason,
                Action = applied ? "applied" : removed ? "removed" : "rejected",
                SkippedReason = skippedReason,
                ProcCooldownMsOrSentinel = proc.CooldownMsOrSentinel,
                DataBits05 = proc.DataBits05,
                DataBits06 = proc.DataBits06,
                DataBits07 = proc.DataBits07,
                DataBits08 = proc.DataBits08,
                DataBits09 = proc.DataBits09
            });
        }

        public static void RecordProcProbe(uint holderGuid, string eventName, string phase, uint? observedTriggerEvent, uint sourceGuid, uint targetGuid, uint triggerSpell4Id, uint triggerCastingId, uint triggerSpell4EffectId, uint? rawAmount, uint? adjustedAmount, uint? absorbedAmount, uint? shieldAbsorbAmount, uint? overkillAmount, bool? killedTarget, string combatResult, uint procEffectId, uint holderSpell4Id, uint holderCastingId, uint procTriggerEvent, uint procTriggerSpell4Id, float procChance, uint procTargetData, uint procCooldownMsOrSentinel, uint dataBits05, uint dataBits06, uint dataBits07, uint dataBits08, uint dataBits09)
        {
            if (holderGuid == 0u)
                return;

            ProcDispatchEvidenceBoundarySnapshot boundary = ProcDispatchEvidenceBoundary.Describe(procTriggerEvent, procTargetData);
            RecordObservation(holderGuid, new ProcRuntimeEvidenceObservation
            {
                ObservedAtUtc = DateTime.UtcNow,
                Kind = "probe",
                EventName = eventName,
                Phase = phase,
                HolderGuid = holderGuid,
                SourceGuid = sourceGuid,
                TargetGuid = targetGuid,
                ObservedTriggerEvent = observedTriggerEvent,
                TriggerEventMatches = observedTriggerEvent.HasValue && observedTriggerEvent.Value == procTriggerEvent,
                HolderSpell4Id = holderSpell4Id,
                HolderCastingId = holderCastingId,
                ProcEffectId = procEffectId,
                ProcTriggerEvent = procTriggerEvent,
                ProcTriggerEventSupported = boundary.TriggerEventSupported,
                ProcTriggerEventLabel = boundary.TriggerEventLabel,
                ProcTriggerSpell4Id = procTriggerSpell4Id,
                ProcChance = procChance,
                ProcTargetData = procTargetData,
                ProcTargetDataSupported = boundary.TargetDataSupported,
                ProcTargetDataLabel = boundary.TargetDataLabel,
                ProcTargetRoute = boundary.TargetRouteLabel,
                ConservativeDispatchBoundary = boundary.DispatchSupportLabel,
                BlockedReason = boundary.BlockedReason,
                TriggerSpell4Id = triggerSpell4Id,
                TriggerCastingId = triggerCastingId,
                TriggerSpell4EffectId = triggerSpell4EffectId,
                RawAmount = rawAmount,
                AdjustedAmount = adjustedAmount,
                AbsorbedAmount = absorbedAmount,
                ShieldAbsorbAmount = shieldAbsorbAmount,
                OverkillAmount = overkillAmount,
                KilledTarget = killedTarget,
                CombatResult = combatResult,
                ProcCooldownMsOrSentinel = procCooldownMsOrSentinel,
                DataBits05 = dataBits05,
                DataBits06 = dataBits06,
                DataBits07 = dataBits07,
                DataBits08 = dataBits08,
                DataBits09 = dataBits09
            });
        }

        public static void RecordProcDispatch(uint holderGuid, string eventName, string phase, uint? observedTriggerEvent, uint sourceGuid, uint targetGuid, uint resolvedTargetGuid, uint procEffectId, uint holderSpell4Id, uint holderCastingId, uint procTriggerEvent, uint procTriggerSpell4Id, float procChance, uint procTargetData, uint procCooldownMsOrSentinel, double procCooldownRemainingSeconds, string action, string skippedReason)
        {
            if (holderGuid == 0u)
                return;

            ProcDispatchEvidenceBoundarySnapshot boundary = ProcDispatchEvidenceBoundary.Describe(procTriggerEvent, procTargetData);
            RecordObservation(holderGuid, new ProcRuntimeEvidenceObservation
            {
                ObservedAtUtc = DateTime.UtcNow,
                Kind = "dispatch",
                EventName = eventName,
                Phase = phase,
                HolderGuid = holderGuid,
                SourceGuid = sourceGuid,
                TargetGuid = targetGuid,
                ObservedTriggerEvent = observedTriggerEvent,
                TriggerEventMatches = observedTriggerEvent.HasValue && observedTriggerEvent.Value == procTriggerEvent,
                ResolvedTargetGuid = resolvedTargetGuid == 0u ? null : resolvedTargetGuid,
                ProcEffectId = procEffectId,
                HolderSpell4Id = holderSpell4Id,
                HolderCastingId = holderCastingId,
                ProcTriggerEvent = procTriggerEvent,
                ProcTriggerEventSupported = boundary.TriggerEventSupported,
                ProcTriggerEventLabel = boundary.TriggerEventLabel,
                ProcTriggerSpell4Id = procTriggerSpell4Id,
                ProcChance = procChance,
                ProcTargetData = procTargetData,
                ProcTargetDataSupported = boundary.TargetDataSupported,
                ProcTargetDataLabel = boundary.TargetDataLabel,
                ProcTargetRoute = boundary.TargetRouteLabel,
                ConservativeDispatchBoundary = boundary.DispatchSupportLabel,
                BlockedReason = boundary.BlockedReason,
                Action = action,
                SkippedReason = skippedReason,
                ProcCooldownMsOrSentinel = procCooldownMsOrSentinel,
                ProcCooldownRemainingSeconds = procCooldownRemainingSeconds
            });
        }

        public static ProcRuntimeEvidenceSummary CreateSummary(IUnitEntity holder)
        {
            uint holderGuid = holder?.Guid ?? 0u;
            IReadOnlyCollection<ProcRegistrationSnapshot> registrations = holder?.CreateProcRegistrationSnapshot() ?? Array.Empty<ProcRegistrationSnapshot>();
            List<ProcRuntimeEvidenceActiveRegistration> activeRegistrations = registrations
                .Select(CreateActiveRegistration)
                .ToList();
            List<ProcRuntimeEvidenceObservation> recentObservations = GetRecentObservations(holderGuid);

            var summary = new ProcRuntimeEvidenceSummary
            {
                HolderGuid = holderGuid,
                ActiveRegistrationCount = activeRegistrations.Count,
                SupportedRegistrationCount = activeRegistrations.Count(static registration => registration.IsConservativelyDispatchSupported),
                UnsupportedRegistrationCount = activeRegistrations.Count(static registration => !registration.IsConservativelyDispatchSupported),
                RecentObservationCount = recentObservations.Count,
                RecentUnsupportedObservationCount = recentObservations.Count(IsUnsupportedObservation)
            };

            summary.UnsupportedActiveTriggerEvents.AddRange(CreateValueCounts(
                activeRegistrations
                    .Where(static registration => !registration.TriggerEventSupported)
                    .Select(static registration => (registration.TriggerEvent, (string)null))));
            summary.UnsupportedActiveTargetData.AddRange(CreateValueCounts(
                activeRegistrations
                    .Where(static registration => !registration.TargetDataSupported)
                    .Select(static registration => (registration.TargetData, (string)null))));
            summary.RecentUnsupportedTriggerEvents.AddRange(CreateValueCounts(
                recentObservations
                    .Where(static observation => !observation.ProcTriggerEventSupported)
                    .Select(static observation => (observation.ProcTriggerEvent, (string)null))));
            summary.RecentUnsupportedTargetData.AddRange(CreateValueCounts(
                recentObservations
                    .Where(static observation => !observation.ProcTargetDataSupported)
                    .Select(static observation => (observation.ProcTargetData, (string)null))));
            summary.RecentBlockedReasons.AddRange(CreateStringCounts(recentObservations.SelectMany(GetBlockedReasons)));

            return summary;
        }

        public static string ExportReport(IUnitEntity holder, string captureSource, string detail = null)
        {
            if (holder == null)
                return null;

            ProcRuntimeEvidenceRecord record = CreateRecord(holder, captureSource, detail);

            try
            {
                string directory = ResolveOutputDirectory();
                Directory.CreateDirectory(directory);
                File.WriteAllText(record.OutputPath, JsonSerializer.Serialize(record, jsonOptions));
                log.Info(
                    "ProcRuntimeEvidence exported holder={0} registrations={1} observations={2} path={3}",
                    record.HolderGuid,
                    record.ActiveRegistrationCount,
                    record.RecentObservationCount,
                    record.OutputPath);
                return record.OutputPath;
            }
            catch (Exception exception)
            {
                log.Warn(exception,
                    "Failed to export proc runtime evidence for holder={0}.",
                    record.HolderGuid);
                return null;
            }
        }

        private static ProcRuntimeEvidenceRecord CreateRecord(IUnitEntity holder, string captureSource, string detail)
        {
            IReadOnlyCollection<ProcRegistrationSnapshot> registrations = holder.CreateProcRegistrationSnapshot() ?? Array.Empty<ProcRegistrationSnapshot>();
            List<ProcRuntimeEvidenceActiveRegistration> activeRegistrations = registrations
                .Select(CreateActiveRegistration)
                .ToList();
            List<ProcRuntimeEvidenceObservation> recentObservations = GetRecentObservations(holder.Guid);
            ProcRuntimeEvidenceSummary summary = CreateSummary(holder);

            return new ProcRuntimeEvidenceRecord
            {
                OutputPath = Path.Combine(ResolveOutputDirectory(), BuildFileName(holder.Guid, captureSource)),
                CreatedAtUtc = DateTime.UtcNow,
                CaptureSource = captureSource,
                Detail = string.IsNullOrWhiteSpace(detail)
                    ? "Manual proc evidence report exported without widening unsupported trigger-event or targetData dispatch."
                    : detail,
                HolderGuid = holder.Guid,
                ActiveRegistrationCount = summary.ActiveRegistrationCount,
                SupportedRegistrationCount = summary.SupportedRegistrationCount,
                UnsupportedRegistrationCount = summary.UnsupportedRegistrationCount,
                RecentObservationCount = summary.RecentObservationCount,
                RecentUnsupportedObservationCount = summary.RecentUnsupportedObservationCount,
                ActiveRegistrations = activeRegistrations,
                RecentObservations = recentObservations,
                UnsupportedActiveTriggerEvents = summary.UnsupportedActiveTriggerEvents,
                UnsupportedActiveTargetData = summary.UnsupportedActiveTargetData,
                RecentUnsupportedTriggerEvents = summary.RecentUnsupportedTriggerEvents,
                RecentUnsupportedTargetData = summary.RecentUnsupportedTargetData,
                RecentBlockedReasons = summary.RecentBlockedReasons,
                Blockers =
                [
                    "Unsupported proc trigger events and unsupported targetData tails remain evidence-only; this report does not widen dispatch.",
                    "Recent observations are a bounded in-memory history per holder intended for live comparison before any routing changes."
                ],
                Notes =
                [
                    "Export this report after !spell procstates or immediately after a live action to compare proc registration, probe, and dispatch evidence in one artifact.",
                    "Use !spell procunsupported for a compact unsupported-tail summary when you do not need the full JSON artifact."
                ]
            };
        }

        private static ProcRuntimeEvidenceActiveRegistration CreateActiveRegistration(ProcRegistrationSnapshot registration)
        {
            ProcDispatchEvidenceBoundarySnapshot boundary = ProcDispatchEvidenceBoundary.Describe(registration.TriggerEvent, registration.TargetData);
            return new ProcRuntimeEvidenceActiveRegistration
            {
                EffectId = registration.EffectId,
                HolderSpell4Id = registration.Spell4Id,
                HolderCastingId = registration.CastingId,
                TriggerEvent = registration.TriggerEvent,
                TriggerEventSupported = boundary.TriggerEventSupported,
                TriggerEventLabel = boundary.TriggerEventLabel,
                TriggerSpell4Id = registration.TriggerSpell4Id,
                Chance = registration.Chance,
                TargetData = registration.TargetData,
                TargetDataSupported = boundary.TargetDataSupported,
                TargetDataLabel = boundary.TargetDataLabel,
                TargetRoute = boundary.TargetRouteLabel,
                DispatchSupportLabel = boundary.DispatchSupportLabel,
                BlockedReason = boundary.BlockedReason,
                CooldownMsOrSentinel = registration.CooldownMsOrSentinel,
                CooldownRemainingSeconds = registration.CooldownRemainingSeconds,
                DataBits05 = registration.DataBits05,
                DataBits06 = registration.DataBits06,
                DataBits07 = registration.DataBits07,
                DataBits08 = registration.DataBits08,
                DataBits09 = registration.DataBits09
            };
        }

        private static void RecordObservation(uint holderGuid, ProcRuntimeEvidenceObservation observation)
        {
            ProcRuntimeEvidenceHistory history = holderHistory.GetOrAdd(holderGuid, static _ => new ProcRuntimeEvidenceHistory());
            lock (history.SyncRoot)
            {
                history.Observations.Add(observation);
                if (history.Observations.Count > MaxObservationsPerHolder)
                    history.Observations.RemoveAt(0);

                history.LastUpdatedUtc = observation.ObservedAtUtc;
            }

            TrimHolderHistoryIfNeeded();
        }

        private static List<ProcRuntimeEvidenceObservation> GetRecentObservations(uint holderGuid)
        {
            if (holderGuid == 0u || !holderHistory.TryGetValue(holderGuid, out ProcRuntimeEvidenceHistory history))
                return [];

            lock (history.SyncRoot)
            {
                return history.Observations.ToList();
            }
        }

        private static IEnumerable<string> GetBlockedReasons(ProcRuntimeEvidenceObservation observation)
        {
            if (!string.IsNullOrWhiteSpace(observation.BlockedReason))
                yield return observation.BlockedReason;

            if (!string.IsNullOrWhiteSpace(observation.SkippedReason)
                && !string.Equals(observation.SkippedReason, observation.BlockedReason, StringComparison.Ordinal))
            {
                yield return observation.SkippedReason;
            }
        }

        private static bool IsUnsupportedObservation(ProcRuntimeEvidenceObservation observation)
        {
            if (observation == null)
                return false;

            return !observation.IsConservativelyDispatchSupported
                || ContainsUnsupported(observation.BlockedReason)
                || ContainsUnsupported(observation.SkippedReason);
        }

        private static bool ContainsUnsupported(string reason)
        {
            return !string.IsNullOrWhiteSpace(reason)
                && reason.Contains("unsupported", StringComparison.OrdinalIgnoreCase);
        }

        private static List<ProcRuntimeEvidenceValueCount> CreateValueCounts(IEnumerable<(uint Value, string Label)> values)
        {
            return values
                .GroupBy(static value => value.Value)
                .OrderByDescending(static group => group.Count())
                .ThenBy(static group => group.Key)
                .Select(group => new ProcRuntimeEvidenceValueCount
                {
                    Value = group.Key,
                    Count = group.Count(),
                    Label = group.Select(static entry => entry.Label).FirstOrDefault(static label => !string.IsNullOrWhiteSpace(label))
                })
                .ToList();
        }

        private static List<ProcRuntimeEvidenceStringCount> CreateStringCounts(IEnumerable<string> values)
        {
            return values
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .GroupBy(static value => value)
                .OrderByDescending(static group => group.Count())
                .ThenBy(static group => group.Key, StringComparer.Ordinal)
                .Select(group => new ProcRuntimeEvidenceStringCount
                {
                    Value = group.Key,
                    Count = group.Count()
                })
                .ToList();
        }

        private static void TrimHolderHistoryIfNeeded()
        {
            if (holderHistory.Count <= MaxTrackedHolders)
                return;

            foreach (KeyValuePair<uint, ProcRuntimeEvidenceHistory> history in holderHistory
                         .OrderBy(static pair => pair.Value.LastUpdatedUtc)
                         .Take(holderHistory.Count - MaxTrackedHolders)
                         .ToList())
            {
                holderHistory.TryRemove(history.Key, out _);
            }
        }

        private static string ResolveOutputDirectory()
        {
            string overrideDirectory = Environment.GetEnvironmentVariable(OutputDirectoryOverrideEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(overrideDirectory))
                return overrideDirectory;

            if (!string.IsNullOrWhiteSpace(outputDirectory))
                return outputDirectory;

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "Source"))
                    && File.Exists(Path.Combine(directory.FullName, "README.md")))
                {
                    outputDirectory = Path.Combine(directory.FullName, "artifacts", "verify", "proc-evidence");
                    return outputDirectory;
                }

                directory = directory.Parent;
            }

            outputDirectory = Path.Combine(AppContext.BaseDirectory, "proc-evidence");
            return outputDirectory;
        }

        private static string BuildFileName(uint holderGuid, string captureSource)
        {
            return $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-holder-{holderGuid}-{SanitizePathPart(captureSource)}.json";
        }

        private static string SanitizePathPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "capture";

            return string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) || char.IsWhiteSpace(c) ? '-' : c))
                .Trim('-');
        }

        private sealed class ProcRuntimeEvidenceHistory
        {
            public object SyncRoot { get; } = new();
            public List<ProcRuntimeEvidenceObservation> Observations { get; } = [];
            public DateTime LastUpdatedUtc { get; set; }
        }
    }

    public sealed class ProcRuntimeEvidenceRecord
    {
        public string OutputPath { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string CaptureSource { get; set; }
        public string Detail { get; set; }
        public uint HolderGuid { get; set; }
        public int ActiveRegistrationCount { get; set; }
        public int SupportedRegistrationCount { get; set; }
        public int UnsupportedRegistrationCount { get; set; }
        public int RecentObservationCount { get; set; }
        public int RecentUnsupportedObservationCount { get; set; }
        public List<ProcRuntimeEvidenceActiveRegistration> ActiveRegistrations { get; set; } = [];
        public List<ProcRuntimeEvidenceObservation> RecentObservations { get; set; } = [];
        public List<ProcRuntimeEvidenceValueCount> UnsupportedActiveTriggerEvents { get; set; } = [];
        public List<ProcRuntimeEvidenceValueCount> UnsupportedActiveTargetData { get; set; } = [];
        public List<ProcRuntimeEvidenceValueCount> RecentUnsupportedTriggerEvents { get; set; } = [];
        public List<ProcRuntimeEvidenceValueCount> RecentUnsupportedTargetData { get; set; } = [];
        public List<ProcRuntimeEvidenceStringCount> RecentBlockedReasons { get; set; } = [];
        public List<string> Blockers { get; set; } = [];
        public List<string> Notes { get; set; } = [];
    }

    public sealed class ProcRuntimeEvidenceSummary
    {
        public uint HolderGuid { get; set; }
        public int ActiveRegistrationCount { get; set; }
        public int SupportedRegistrationCount { get; set; }
        public int UnsupportedRegistrationCount { get; set; }
        public int RecentObservationCount { get; set; }
        public int RecentUnsupportedObservationCount { get; set; }
        public List<ProcRuntimeEvidenceValueCount> UnsupportedActiveTriggerEvents { get; } = [];
        public List<ProcRuntimeEvidenceValueCount> UnsupportedActiveTargetData { get; } = [];
        public List<ProcRuntimeEvidenceValueCount> RecentUnsupportedTriggerEvents { get; } = [];
        public List<ProcRuntimeEvidenceValueCount> RecentUnsupportedTargetData { get; } = [];
        public List<ProcRuntimeEvidenceStringCount> RecentBlockedReasons { get; } = [];
    }

    public sealed class ProcRuntimeEvidenceActiveRegistration
    {
        public uint EffectId { get; set; }
        public uint HolderSpell4Id { get; set; }
        public uint HolderCastingId { get; set; }
        public uint TriggerEvent { get; set; }
        public bool TriggerEventSupported { get; set; }
        public string TriggerEventLabel { get; set; }
        public uint TriggerSpell4Id { get; set; }
        public float Chance { get; set; }
        public uint TargetData { get; set; }
        public bool TargetDataSupported { get; set; }
        public string TargetDataLabel { get; set; }
        public string TargetRoute { get; set; }
        public string DispatchSupportLabel { get; set; }
        public string BlockedReason { get; set; }
        public uint CooldownMsOrSentinel { get; set; }
        public double CooldownRemainingSeconds { get; set; }
        public uint DataBits05 { get; set; }
        public uint DataBits06 { get; set; }
        public uint DataBits07 { get; set; }
        public uint DataBits08 { get; set; }
        public uint DataBits09 { get; set; }

        public bool IsConservativelyDispatchSupported => string.Equals(DispatchSupportLabel, "dispatch-supported", StringComparison.Ordinal);
    }

    public sealed class ProcRuntimeEvidenceObservation
    {
        public DateTime ObservedAtUtc { get; set; }
        public string Kind { get; set; }
        public string EventName { get; set; }
        public string Phase { get; set; }
        public uint HolderGuid { get; set; }
        public uint SourceGuid { get; set; }
        public uint TargetGuid { get; set; }
        public uint? ObservedTriggerEvent { get; set; }
        public bool? TriggerEventMatches { get; set; }
        public uint? ResolvedTargetGuid { get; set; }
        public uint? ProcEffectId { get; set; }
        public uint HolderSpell4Id { get; set; }
        public uint HolderCastingId { get; set; }
        public uint ProcTriggerEvent { get; set; }
        public bool ProcTriggerEventSupported { get; set; }
        public string ProcTriggerEventLabel { get; set; }
        public uint ProcTriggerSpell4Id { get; set; }
        public float ProcChance { get; set; }
        public uint ProcTargetData { get; set; }
        public bool ProcTargetDataSupported { get; set; }
        public string ProcTargetDataLabel { get; set; }
        public string ProcTargetRoute { get; set; }
        public string ConservativeDispatchBoundary { get; set; }
        public string BlockedReason { get; set; }
        public string Action { get; set; }
        public string SkippedReason { get; set; }
        public uint TriggerSpell4Id { get; set; }
        public uint TriggerCastingId { get; set; }
        public uint TriggerSpell4EffectId { get; set; }
        public uint? RawAmount { get; set; }
        public uint? AdjustedAmount { get; set; }
        public uint? AbsorbedAmount { get; set; }
        public uint? ShieldAbsorbAmount { get; set; }
        public uint? OverkillAmount { get; set; }
        public bool? KilledTarget { get; set; }
        public string CombatResult { get; set; }
        public uint ProcCooldownMsOrSentinel { get; set; }
        public double? ProcCooldownRemainingSeconds { get; set; }
        public uint DataBits05 { get; set; }
        public uint DataBits06 { get; set; }
        public uint DataBits07 { get; set; }
        public uint DataBits08 { get; set; }
        public uint DataBits09 { get; set; }

        public bool IsConservativelyDispatchSupported => string.Equals(ConservativeDispatchBoundary, "dispatch-supported", StringComparison.Ordinal);
    }

    public sealed class ProcRuntimeEvidenceValueCount
    {
        public uint Value { get; set; }
        public int Count { get; set; }
        public string Label { get; set; }
    }

    public sealed class ProcRuntimeEvidenceStringCount
    {
        public string Value { get; set; }
        public int Count { get; set; }
    }
}
