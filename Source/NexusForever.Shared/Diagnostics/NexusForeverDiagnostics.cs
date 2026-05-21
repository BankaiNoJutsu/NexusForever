using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NexusForever.Shared.Diagnostics
{
    public static class NexusForeverDiagnostics
    {
        public const string ActivitySourceName = "NexusForever";
        public const string MeterName = "NexusForever";

        public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
        public static readonly Meter Meter = new(MeterName);

        private static readonly Histogram<double> tickDuration = Meter.CreateHistogram<double>("nexus.tick.duration_ms", "ms");
        private static readonly Histogram<double> tickSubsystemDuration = Meter.CreateHistogram<double>("nexus.tick.subsystem.duration_ms", "ms");
        private static readonly Histogram<double> packetQueueWait = Meter.CreateHistogram<double>("nexus.packet.queue_wait_ms", "ms");
        private static readonly Histogram<double> packetHandlerDuration = Meter.CreateHistogram<double>("nexus.packet.handler_ms", "ms");
        private static readonly Histogram<double> packetFlushDuration = Meter.CreateHistogram<double>("nexus.packet.flush_ms", "ms");
        private static readonly Histogram<double> databaseOperationDuration = Meter.CreateHistogram<double>("nexus.db.operation_ms", "ms");
        private static readonly Histogram<double> eventTaskWaitDuration = Meter.CreateHistogram<double>("nexus.event.task_wait_ms", "ms");
        private static readonly Histogram<double> eventCallbackDuration = Meter.CreateHistogram<double>("nexus.event.callback_ms", "ms");
        private static readonly Histogram<double> stsTransactionDuration = Meter.CreateHistogram<double>("nexus.sts.transaction_ms", "ms");
        private static readonly Counter<long> slowOperationCount = Meter.CreateCounter<long>("nexus.slow_operation.count");
        private static readonly UpDownCounter<long> activeSessions = Meter.CreateUpDownCounter<long>("nexus.session.active");
        private static readonly Histogram<long> packetQueueLength = Meter.CreateHistogram<long>("nexus.packet.queue.length");

        private static ProfilingOptions options = new();
        private static readonly object activeSessionsLock = new();
        private static readonly Dictionary<string, long> lastActiveSessionsByType = new();

        public static bool Enabled => options.Enabled;
        public static bool IncludePacketPayloadSizes => options.IncludePacketPayloadSizes;

        public static void Configure(IConfiguration configuration)
        {
            options = configuration?.Get<ProfilingOptions>() ?? new ProfilingOptions();
        }

        public static Activity StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        {
            return Enabled ? ActivitySource.StartActivity(name, kind) : null;
        }

        public static long GetTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        public static double GetElapsedMilliseconds(long startTimestamp)
        {
            return Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        }

        public static void RecordTick(double elapsedMs)
        {
            if (!Enabled)
                return;

            tickDuration.Record(elapsedMs);
            RecordSlowOperation("tick", elapsedMs);
        }

        public static void MeasureTickSubsystem(string subsystem, Action action)
        {
            if (!Enabled)
            {
                action();
                return;
            }

            using Activity activity = StartActivity("nexus.tick.subsystem");
            activity?.SetTag("nexus.subsystem", subsystem);

            long start = GetTimestamp();
            try
            {
                action();
            }
            finally
            {
                double elapsedMs = GetElapsedMilliseconds(start);
                tickSubsystemDuration.Record(elapsedMs, Tag("subsystem", subsystem));
                RecordSlowOperation("tick.subsystem", elapsedMs, Tag("subsystem", subsystem));
            }
        }

        public static void RecordPacketQueueWait(string protocol, string opcode, double elapsedMs)
        {
            if (!Enabled)
                return;

            packetQueueWait.Record(elapsedMs, Tags(("protocol", protocol), ("opcode", opcode)));
        }

        public static void RecordPacketHandler(string protocol, string opcode, string handler, double elapsedMs)
        {
            if (!Enabled)
                return;

            KeyValuePair<string, object>[] tags = Tags(("protocol", protocol), ("opcode", opcode), ("handler", handler));
            packetHandlerDuration.Record(elapsedMs, tags);
            RecordSlowOperation("packet.handler", elapsedMs, tags);
        }

        public static void RecordStsTransaction(string uri, string handler, double elapsedMs)
        {
            if (!Enabled)
                return;

            KeyValuePair<string, object>[] tags = Tags(("uri", uri), ("handler", handler));
            stsTransactionDuration.Record(elapsedMs, tags);
            RecordSlowOperation("sts.transaction", elapsedMs, tags);
        }

        public static void RecordPacketFlush(string protocol, string opcode, int bytes, double elapsedMs)
        {
            if (!Enabled)
                return;

            KeyValuePair<string, object>[] tags = IncludePacketPayloadSizes
                ? Tags(("protocol", protocol), ("opcode", opcode), ("bytes", bytes))
                : Tags(("protocol", protocol), ("opcode", opcode));
            packetFlushDuration.Record(elapsedMs, tags);
            RecordSlowOperation("packet.flush", elapsedMs, tags);
        }

        public static void RecordPacketQueueLength(string protocol, string direction, long length)
        {
            if (!Enabled)
                return;

            packetQueueLength.Record(length, Tags(("protocol", protocol), ("direction", direction)));
        }

        public static void RecordActiveSessions(string sessionType, long count)
        {
            if (!Enabled)
                return;

            long delta;
            lock (activeSessionsLock)
            {
                lastActiveSessionsByType.TryGetValue(sessionType, out long lastActiveSessions);
                delta = count - lastActiveSessions;
                lastActiveSessionsByType[sessionType] = count;
            }

            if (delta != 0)
                activeSessions.Add(delta, Tag("session_type", sessionType));
        }

        public static T MeasureDatabase<T>(string database, string operation, Func<T> func)
        {
            if (!Enabled)
                return func();

            using Activity activity = StartActivity("nexus.db.operation", ActivityKind.Client);
            activity?.SetTag("db.system", database);
            activity?.SetTag("db.operation", operation);

            long start = GetTimestamp();
            try
            {
                return func();
            }
            finally
            {
                RecordDatabase(database, operation, GetElapsedMilliseconds(start));
            }
        }

        public static void MeasureDatabase(string database, string operation, Action action)
        {
            if (!Enabled)
            {
                action();
                return;
            }

            using Activity activity = StartActivity("nexus.db.operation", ActivityKind.Client);
            activity?.SetTag("db.system", database);
            activity?.SetTag("db.operation", operation);

            long start = GetTimestamp();
            try
            {
                action();
            }
            finally
            {
                RecordDatabase(database, operation, GetElapsedMilliseconds(start));
            }
        }

        public static async Task<T> MeasureDatabaseAsync<T>(string database, string operation, Func<Task<T>> func)
        {
            if (!Enabled)
                return await func();

            using Activity activity = StartActivity("nexus.db.operation", ActivityKind.Client);
            activity?.SetTag("db.system", database);
            activity?.SetTag("db.operation", operation);

            long start = GetTimestamp();
            try
            {
                return await func();
            }
            finally
            {
                RecordDatabase(database, operation, GetElapsedMilliseconds(start));
            }
        }

        public static async Task MeasureDatabaseAsync(string database, string operation, Func<Task> func)
        {
            if (!Enabled)
            {
                await func();
                return;
            }

            using Activity activity = StartActivity("nexus.db.operation", ActivityKind.Client);
            activity?.SetTag("db.system", database);
            activity?.SetTag("db.operation", operation);

            long start = GetTimestamp();
            try
            {
                await func();
            }
            finally
            {
                RecordDatabase(database, operation, GetElapsedMilliseconds(start));
            }
        }

        public static void RecordEventTaskWait(string eventType, double elapsedMs)
        {
            if (!Enabled)
                return;

            eventTaskWaitDuration.Record(elapsedMs, Tag("event_type", eventType));
        }

        public static void RecordEventCallback(string eventType, double elapsedMs)
        {
            if (!Enabled)
                return;

            eventCallbackDuration.Record(elapsedMs, Tag("event_type", eventType));
            RecordSlowOperation("event.callback", elapsedMs, Tag("event_type", eventType));
        }

        private static void RecordDatabase(string database, string operation, double elapsedMs)
        {
            KeyValuePair<string, object>[] tags = Tags(("database", database), ("operation", operation));
            databaseOperationDuration.Record(elapsedMs, tags);
            RecordSlowOperation("db.operation", elapsedMs, tags);
        }

        private static void RecordSlowOperation(string kind, double elapsedMs, params KeyValuePair<string, object>[] tags)
        {
            if (elapsedMs < options.SlowOperationThresholdMs)
                return;

            var allTags = new KeyValuePair<string, object>[tags.Length + 1];
            allTags[0] = new KeyValuePair<string, object>("kind", kind);
            Array.Copy(tags, 0, allTags, 1, tags.Length);
            slowOperationCount.Add(1, allTags);
        }

        private static KeyValuePair<string, object>[] Tag(string key, object value)
        {
            return [new KeyValuePair<string, object>(key, value)];
        }

        private static KeyValuePair<string, object>[] Tags(params (string Key, object Value)[] tags)
        {
            var pairs = new KeyValuePair<string, object>[tags.Length];
            for (int i = 0; i < tags.Length; i++)
                pairs[i] = new KeyValuePair<string, object>(tags[i].Key, tags[i].Value);

            return pairs;
        }
    }
}
