using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using NexusForever.Shared.Diagnostics;
using NLog;

namespace NexusForever.GameTable
{
    public static class MissingGameDataDiagnostics
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly Counter<long> missingGameDataCount =
            NexusForeverDiagnostics.Meter.CreateCounter<long>("nexus.game_data.missing.count");

        private static readonly ConcurrentDictionary<string, DiagnosticCounter> counters = new();

        public static void ReportMissingTable(
            string tableName,
            string context,
            MissingGameDataSeverity severity = MissingGameDataSeverity.Required,
            string detail = null)
        {
            Report(MissingGameDataDiagnosticKind.MissingTable, severity, tableName, null, context, detail);
        }

        public static void ReportMissingRow(
            string tableName,
            object staticId,
            string context,
            MissingGameDataSeverity severity = MissingGameDataSeverity.Required,
            string detail = null)
        {
            Report(MissingGameDataDiagnosticKind.MissingRow, severity, tableName, staticId?.ToString(), context, detail);
        }

        public static void ReportSkippedGrant(
            string grantType,
            string tableName,
            object staticId,
            string context,
            string detail = null)
        {
            string grantDetail = string.IsNullOrWhiteSpace(detail) ? grantType : $"{grantType}: {detail}";
            Report(MissingGameDataDiagnosticKind.SkippedGrant, MissingGameDataSeverity.PlayerImpacting, tableName, staticId?.ToString(), context, grantDetail);
        }

        public static IReadOnlyList<MissingGameDataDiagnostic> GetSnapshot()
        {
            return counters.Values
                .Select(c => new MissingGameDataDiagnostic(c.Kind, c.Severity, c.TableName, c.StaticId, c.Context, c.Detail, c.Count))
                .OrderBy(c => c.Kind)
                .ThenBy(c => c.Severity)
                .ThenBy(c => c.TableName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.StaticId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Context, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void ResetForTests()
        {
            counters.Clear();
        }

        private static void Report(
            MissingGameDataDiagnosticKind kind,
            MissingGameDataSeverity severity,
            string tableName,
            string staticId,
            string context,
            string detail)
        {
            tableName = string.IsNullOrWhiteSpace(tableName) ? "unknown" : tableName;
            staticId  = string.IsNullOrWhiteSpace(staticId) ? null : staticId;
            context   = string.IsNullOrWhiteSpace(context) ? "unknown" : context;
            detail    = string.IsNullOrWhiteSpace(detail) ? null : detail;

            string key = $"{kind}|{severity}|{tableName}|{staticId}|{context}|{detail}";
            DiagnosticCounter counter = counters.GetOrAdd(key, _ => new DiagnosticCounter(kind, severity, tableName, staticId, context, detail));

            long count = counter.Increment();
            missingGameDataCount.Add(1,
                new KeyValuePair<string, object>("kind", kind.ToString()),
                new KeyValuePair<string, object>("severity", severity.ToString()),
                new KeyValuePair<string, object>("table", tableName),
                new KeyValuePair<string, object>("context", context));

            if (count != 1)
                return;

            if (severity == MissingGameDataSeverity.Optional)
            {
                log.Debug("Missing optional game data: kind={0} table={1} staticId={2} context={3} detail={4}", kind, tableName, staticId, context, detail);
                return;
            }

            log.Warn("Missing game data: kind={0} severity={1} table={2} staticId={3} context={4} detail={5}", kind, severity, tableName, staticId, context, detail);
        }

        private sealed class DiagnosticCounter
        {
            private long count;

            public DiagnosticCounter(
                MissingGameDataDiagnosticKind kind,
                MissingGameDataSeverity severity,
                string tableName,
                string staticId,
                string context,
                string detail)
            {
                Kind      = kind;
                Severity  = severity;
                TableName = tableName;
                StaticId  = staticId;
                Context   = context;
                Detail    = detail;
            }

            public MissingGameDataDiagnosticKind Kind { get; }
            public MissingGameDataSeverity Severity { get; }
            public string TableName { get; }
            public string StaticId { get; }
            public string Context { get; }
            public string Detail { get; }
            public long Count => Interlocked.Read(ref count);

            public long Increment()
            {
                return Interlocked.Increment(ref count);
            }
        }
    }
}
