using System.Collections.Concurrent;
using System.Text.Json;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Spell;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.Spell
{
    public static class SpellRuntimeEvidenceCollector
    {
        private static readonly ILogger log = LogManager.GetLogger("SpellRuntimeEvidence");
        private static readonly ConcurrentDictionary<uint, SpellRuntimeEvidenceRecord> captures = new();
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true
        };

        private static string outputDirectory;

        public static string GetOutputDirectoryHint()
        {
            return ResolveOutputDirectory();
        }

        public static void RecordCastAttempt(ISpell spell, CastResult castResult)
        {
            if (!IsEnabled(spell))
                return;

            SpellRuntimeEvidenceRecord record = GetOrCreate(spell);
            lock (record.SyncRoot)
            {
                record.CastResult = castResult.ToString();
                record.Status = castResult == CastResult.Ok ? "cast-started" : "cast-blocked";
            }
        }

        public static void RecordCancellation(ISpell spell, CastResult result)
        {
            if (!IsEnabled(spell))
                return;

            SpellRuntimeEvidenceRecord record = GetOrCreate(spell);
            lock (record.SyncRoot)
            {
                record.CancelResult = result.ToString();
                record.Status = "cast-cancelled";
            }
        }

        public static void RecordTargetSelection(ISpell spell, IReadOnlyCollection<ISpellTargetInfo> targets, int telegraphCount)
        {
            if (!IsEnabled(spell))
                return;

            SpellRuntimeEvidenceRecord record = GetOrCreate(spell);
            lock (record.SyncRoot)
            {
                record.TelegraphCount = telegraphCount;
                record.SelectedTargets.Clear();
                foreach (ISpellTargetInfo target in targets)
                {
                    record.SelectedTargets.Add(new SpellRuntimeEvidenceTarget
                    {
                        TargetId = target.Entity.Guid,
                        EntityType = target.Entity.Type.ToString(),
                        Flags = target.Flags.ToString(),
                        FlagValue = (uint)target.Flags
                    });
                }
            }
        }

        public static void RecordEffectDispatch(ISpell spell, SpellEffectInterpretation effect, int targetCount, bool hasHandler)
        {
            if (!IsEnabled(spell))
                return;

            SpellRuntimeEvidenceRecord record = GetOrCreate(spell);
            lock (record.SyncRoot)
            {
                record.EffectDispatches.Add(new SpellRuntimeEvidenceEffectDispatch
                {
                    Spell4EffectId = effect.Entry.Id,
                    OrderIndex = effect.Entry.OrderIndex,
                    EffectType = effect.Entry.EffectType.ToString(),
                    TargetFlags = effect.Entry.TargetFlags,
                    TargetCount = targetCount,
                    HasHandler = hasHandler,
                    DelayMs = effect.Timing.DelayTime,
                    TickMs = effect.Timing.TickTime,
                    DurationMs = effect.Timing.DurationTime,
                    DataBits = effect.FormatDataBits(),
                    Parameters = effect.FormatParameters()
                });
            }
        }

        public static void RecordEffectPreparation(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            if (!IsEnabled(spell))
                return;

            SpellRuntimeEvidenceRecord record = GetOrCreate(spell);
            lock (record.SyncRoot)
            {
                record.BeforeSnapshots[new EffectSnapshotKey(info.EffectId, target.Guid)] = CaptureSnapshot(target);
            }
        }

        public static void RecordEffectResult(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            if (!IsEnabled(spell))
                return;

            SpellRuntimeEvidenceRecord record = GetOrCreate(spell);
            lock (record.SyncRoot)
            {
                record.BeforeSnapshots.TryGetValue(new EffectSnapshotKey(info.EffectId, target.Guid), out SpellRuntimeEvidenceEntitySnapshot beforeSnapshot);
                record.EffectResults.Add(new SpellRuntimeEvidenceEffectResult
                {
                    Spell4EffectId = info.Entry.Id,
                    EffectId = info.EffectId,
                    EffectType = info.Entry.EffectType.ToString(),
                    TargetId = target.Guid,
                    DropEffect = info.DropEffect,
                    CombatLogCount = info.CombatLogs.Count,
                    CombatLogTypes = info.CombatLogs.Select(combatLog => combatLog.GetType().Name).ToList(),
                    CreatedEntityCount = info.CreatedEntities.Count,
                    BeforeTargetState = beforeSnapshot,
                    AfterTargetState = CaptureSnapshot(target),
                    RawDamage = info.Damage?.RawDamage,
                    RawScaledDamage = info.Damage?.RawScaledDamage,
                    AdjustedDamage = info.Damage?.AdjustedDamage,
                    AbsorbedAmount = info.Damage?.AbsorbedAmount,
                    ShieldAbsorbAmount = info.Damage?.ShieldAbsorbAmount,
                    OverkillAmount = info.Damage?.OverkillAmount,
                    DamageResult = info.Damage?.CombatResult.ToString(),
                    DamageType = info.Damage?.DamageType.ToString()
                });
            }
        }

        public static void RecordBlockedEffect(ISpell spell, IUnitEntity target, SpellEffectInterpretation blockedEffect, string source, string detail)
        {
            if (!IsEnabled(spell))
                return;

            SpellRuntimeEvidenceRecord record = GetOrCreate(spell);
            lock (record.SyncRoot)
            {
                record.BlockedEffects.Add(new SpellRuntimeEvidenceBlockedEffect
                {
                    Source = source,
                    Detail = detail,
                    TargetId = target.Guid,
                    BlockedSpell4EffectId = blockedEffect.Entry.Id,
                    BlockedEffectType = blockedEffect.Entry.EffectType.ToString()
                });
            }
        }

        public static void RecordPacketEvent(ISpell spell, string packetName, string detail, int? targetInfoCount = null, int? effectInfoCount = null, int? combatLogCount = null, int? entryCount = null)
        {
            if (!IsEnabled(spell))
                return;

            SpellRuntimeEvidenceRecord record = GetOrCreate(spell);
            lock (record.SyncRoot)
            {
                record.PacketEvents.Add(new SpellRuntimeEvidencePacketEvent
                {
                    PacketName = packetName,
                    Detail = detail,
                    TargetInfoCount = targetInfoCount,
                    EffectInfoCount = effectInfoCount,
                    CombatLogCount = combatLogCount,
                    EntryCount = entryCount
                });
            }
        }

        public static void FinalizeAndExport(ISpell spell, string finalStatus)
        {
            if (!IsEnabled(spell))
                return;

            if (!captures.TryRemove(spell.CastingId, out SpellRuntimeEvidenceRecord record))
                return;

            lock (record.SyncRoot)
            {
                record.Status = finalStatus;
                record.CompletedAtUtc = DateTime.UtcNow;
                record.BeforeSnapshots.Clear();
            }

            try
            {
                string directory = ResolveOutputDirectory();
                Directory.CreateDirectory(directory);
                File.WriteAllText(record.OutputPath, JsonSerializer.Serialize(record, jsonOptions));
                log.Info(
                    "SpellRuntimeEvidence exported spell4Id={0} castingId={1} path={2}",
                    record.Spell4Id,
                    record.CastingId,
                    record.OutputPath);
            }
            catch (Exception exception)
            {
                log.Warn(exception,
                    "Failed to export spell runtime evidence for spell4Id={0} castingId={1}.",
                    record.Spell4Id,
                    record.CastingId);
            }
        }

        private static bool IsEnabled(ISpell spell)
        {
            return spell?.Parameters?.CaptureRuntimeEvidence == true;
        }

        private static SpellRuntimeEvidenceRecord GetOrCreate(ISpell spell)
        {
            return captures.GetOrAdd(spell.CastingId, _ => CreateRecord(spell));
        }

        private static SpellRuntimeEvidenceRecord CreateRecord(ISpell spell)
        {
            string outputPath = Path.Combine(
                ResolveOutputDirectory(),
                BuildFileName(spell));

            return new SpellRuntimeEvidenceRecord
            {
                OutputPath = outputPath,
                CreatedAtUtc = DateTime.UtcNow,
                Status = "created",
                CastingId = spell.CastingId,
                Spell4Id = spell.Parameters.SpellInfo.Entry.Id,
                Spell4BaseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id,
                SpellDescription = spell.Parameters.SpellInfo.Entry.Description,
                CasterId = spell.Caster.Guid,
                PrimaryTargetId = spell.Parameters.PrimaryTargetId,
                ClientContextToken = spell.Parameters.ClientContextToken,
                ClientRequestSource = spell.Parameters.ClientRequestSource,
                CaptureRuntimeEvidence = spell.Parameters.CaptureRuntimeEvidence,
                EmitDiagnosticSpellBroadcasts = spell.Parameters.EmitDiagnosticSpellBroadcasts
            };
        }

        private static string ResolveOutputDirectory()
        {
            if (!string.IsNullOrWhiteSpace(outputDirectory))
                return outputDirectory;

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "Source"))
                    && File.Exists(Path.Combine(directory.FullName, "README.md")))
                {
                    outputDirectory = Path.Combine(directory.FullName, "artifacts", "verify", "spell-evidence");
                    return outputDirectory;
                }

                directory = directory.Parent;
            }

            outputDirectory = Path.Combine(AppContext.BaseDirectory, "spell-evidence");
            return outputDirectory;
        }

        private static string BuildFileName(ISpell spell)
        {
            string description = spell.Parameters.SpellInfo.Entry.Description;
            if (string.IsNullOrWhiteSpace(description))
                description = $"spell4-{spell.Parameters.SpellInfo.Entry.Id}";

            string safeDescription = SanitizePathPart(description);
            return $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-spell4-{spell.Parameters.SpellInfo.Entry.Id}-cast-{spell.CastingId}-{safeDescription}.json";
        }

        private static string SanitizePathPart(string value)
        {
            return string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) || char.IsWhiteSpace(c) ? '_' : c))
                .Trim('_');
        }

        private static SpellRuntimeEvidenceEntitySnapshot CaptureSnapshot(IWorldEntity entity)
        {
            var snapshot = new SpellRuntimeEvidenceEntitySnapshot
            {
                EntityId = entity.Guid,
                EntityType = entity.Type.ToString(),
                Health = entity.Health,
                Shield = entity.Shield,
                InterruptArmor = entity.InterruptArmor,
                IsBusy = entity.IsBusy
            };

            if (entity is IUnitEntity unit)
            {
                snapshot.IsUnit = true;
                snapshot.IsAlive = unit.IsAlive;
                snapshot.InCombat = unit.InCombat;
                snapshot.ActiveCCStateMask = unit.ActiveCCStateMask;
                snapshot.IsStealthed = unit.IsStealthed;
                snapshot.IsAggroImmune = unit.IsAggroImmune;
                snapshot.IsShieldOverloaded = unit.IsShieldOverloaded;
                snapshot.CurrentAbsorption = unit.CurrentAbsorption;
                snapshot.CurrentHealingAbsorption = unit.CurrentHealingAbsorption;
            }

            return snapshot;
        }

        private readonly record struct EffectSnapshotKey(uint EffectId, uint TargetId);
    }

    public sealed class SpellRuntimeEvidenceRecord
    {
        public string OutputPath { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string Status { get; set; }
        public string CastResult { get; set; }
        public string CancelResult { get; set; }
        public uint CastingId { get; set; }
        public uint Spell4Id { get; set; }
        public uint Spell4BaseId { get; set; }
        public string SpellDescription { get; set; }
        public uint CasterId { get; set; }
        public uint PrimaryTargetId { get; set; }
        public uint ClientContextToken { get; set; }
        public string ClientRequestSource { get; set; }
        public bool CaptureRuntimeEvidence { get; set; }
        public bool EmitDiagnosticSpellBroadcasts { get; set; }
        public int TelegraphCount { get; set; }
        public List<SpellRuntimeEvidenceTarget> SelectedTargets { get; } = [];
        public List<SpellRuntimeEvidenceEffectDispatch> EffectDispatches { get; } = [];
        public List<SpellRuntimeEvidenceEffectResult> EffectResults { get; } = [];
        public List<SpellRuntimeEvidenceBlockedEffect> BlockedEffects { get; } = [];
        public List<SpellRuntimeEvidencePacketEvent> PacketEvents { get; } = [];

        internal object SyncRoot { get; } = new();
        internal Dictionary<object, SpellRuntimeEvidenceEntitySnapshot> BeforeSnapshots { get; } = new();
    }

    public sealed class SpellRuntimeEvidenceTarget
    {
        public uint TargetId { get; set; }
        public string EntityType { get; set; }
        public string Flags { get; set; }
        public uint FlagValue { get; set; }
    }

    public sealed class SpellRuntimeEvidenceEffectDispatch
    {
        public uint Spell4EffectId { get; set; }
        public uint OrderIndex { get; set; }
        public string EffectType { get; set; }
        public uint TargetFlags { get; set; }
        public int TargetCount { get; set; }
        public bool HasHandler { get; set; }
        public uint DelayMs { get; set; }
        public uint TickMs { get; set; }
        public uint DurationMs { get; set; }
        public string DataBits { get; set; }
        public string Parameters { get; set; }
    }

    public sealed class SpellRuntimeEvidenceEffectResult
    {
        public uint Spell4EffectId { get; set; }
        public uint EffectId { get; set; }
        public string EffectType { get; set; }
        public uint TargetId { get; set; }
        public bool DropEffect { get; set; }
        public int CombatLogCount { get; set; }
        public List<string> CombatLogTypes { get; set; } = [];
        public int CreatedEntityCount { get; set; }
        public uint? RawDamage { get; set; }
        public uint? RawScaledDamage { get; set; }
        public uint? AdjustedDamage { get; set; }
        public uint? AbsorbedAmount { get; set; }
        public uint? ShieldAbsorbAmount { get; set; }
        public uint? OverkillAmount { get; set; }
        public string DamageResult { get; set; }
        public string DamageType { get; set; }
        public SpellRuntimeEvidenceEntitySnapshot BeforeTargetState { get; set; }
        public SpellRuntimeEvidenceEntitySnapshot AfterTargetState { get; set; }
    }

    public sealed class SpellRuntimeEvidenceBlockedEffect
    {
        public string Source { get; set; }
        public string Detail { get; set; }
        public uint TargetId { get; set; }
        public uint BlockedSpell4EffectId { get; set; }
        public string BlockedEffectType { get; set; }
    }

    public sealed class SpellRuntimeEvidencePacketEvent
    {
        public string PacketName { get; set; }
        public string Detail { get; set; }
        public int? TargetInfoCount { get; set; }
        public int? EffectInfoCount { get; set; }
        public int? CombatLogCount { get; set; }
        public int? EntryCount { get; set; }
    }

    public sealed class SpellRuntimeEvidenceEntitySnapshot
    {
        public uint EntityId { get; set; }
        public string EntityType { get; set; }
        public bool IsUnit { get; set; }
        public bool IsAlive { get; set; }
        public bool InCombat { get; set; }
        public uint Health { get; set; }
        public uint Shield { get; set; }
        public uint InterruptArmor { get; set; }
        public uint ActiveCCStateMask { get; set; }
        public bool IsStealthed { get; set; }
        public bool IsAggroImmune { get; set; }
        public bool IsShieldOverloaded { get; set; }
        public uint CurrentAbsorption { get; set; }
        public uint CurrentHealingAbsorption { get; set; }
        public bool IsBusy { get; set; }
    }
}