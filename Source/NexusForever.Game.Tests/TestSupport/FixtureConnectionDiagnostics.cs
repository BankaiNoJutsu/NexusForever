using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NexusForever.Game.Tests.TestSupport;

// Opt-in observations for the recurring Challenge drain/teardown failure.
// Weak references do not prolong connection lifetime. No database contents,
// connection strings, file handles or process controls are collected.
// Counts describe observed EF events, not native resource-holder proof.
internal sealed class FixtureConnectionDiagnostics : DbConnectionInterceptor
{
    private sealed record Open(Guid Id, string Context, DateTime Started, int Thread, string Origin,
        WeakReference<DbConnection> Connection, string Phase);
    private sealed record Progress(string Event, DateTime At, string Signature);
    private readonly ConcurrentDictionary<Guid, Open> active = new();
    private readonly ConcurrentQueue<string> closed = new();
    private readonly ConcurrentQueue<string> failed = new();
    private readonly ConcurrentDictionary<string, Progress> progress = new();
    private long openingAttempts, openedCount, closedCount, failedCount, disposedCount;
    private long originSamples, originTicks, maximumOriginTicks;

    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData data, InterceptionResult result)
    {
        long originStart = Stopwatch.GetTimestamp();
        string origin = string.Join(" <- ", new StackTrace(true).GetFrames().Select(frame =>
            (Frame: frame, Type: frame.GetMethod()?.DeclaringType?.FullName))
            .Where(value => value.Type?.StartsWith("NexusForever.", StringComparison.Ordinal) == true
                && value.Type != typeof(FixtureConnectionDiagnostics).FullName).Take(6)
            .Select(value => value.Type + "." + value.Frame.GetMethod()?.Name + ":" + value.Frame.GetFileLineNumber()));
        long elapsed = Stopwatch.GetTimestamp() - originStart;
        Interlocked.Increment(ref originSamples); Interlocked.Add(ref originTicks, elapsed);
        long maximum;
        do { maximum = Interlocked.Read(ref maximumOriginTicks); }
        while (elapsed > maximum && Interlocked.CompareExchange(ref maximumOriginTicks, elapsed, maximum) != maximum);
        active[data.ConnectionId] = new(data.ConnectionId, data.Context?.ContextId.ToString(), DateTime.UtcNow,
            Environment.CurrentManagedThreadId, origin, new(connection), "opening");
        Interlocked.Increment(ref openingAttempts);
        return result;
    }
    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData data,
        InterceptionResult result, CancellationToken cancellationToken = default) => new(ConnectionOpening(connection, data, result));

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData data)
    {
        Interlocked.Increment(ref openedCount);
        if (active.TryGetValue(data.ConnectionId, out var current)) active[data.ConnectionId] = current with { Phase = "open" };
    }
    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData data, CancellationToken cancellationToken = default)
    { ConnectionOpened(connection, data); return Task.CompletedTask; }
    public override void ConnectionClosed(DbConnection connection, ConnectionEndEventData data)
    {
        Interlocked.Increment(ref closedCount);
        Retire(data.ConnectionId, "closed");
    }
    public override Task ConnectionClosedAsync(DbConnection connection, ConnectionEndEventData data)
    { ConnectionClosed(connection, data); return Task.CompletedTask; }

    public override void ConnectionDisposed(DbConnection connection, ConnectionEndEventData data)
    { Interlocked.Increment(ref disposedCount); Retire(data.ConnectionId, "disposed"); }
    public override Task ConnectionDisposedAsync(DbConnection connection, ConnectionEndEventData data)
    { ConnectionDisposed(connection, data); return Task.CompletedTask; }
    public override void ConnectionFailed(DbConnection connection, ConnectionErrorEventData data)
    {
        Interlocked.Increment(ref failedCount);
        active.TryRemove(data.ConnectionId, out var current);
        failed.Enqueue($"id={data.ConnectionId};state={State(connection)};error={data.Exception.GetType().Name};context={current?.Context};origin={current?.Origin}");
        while (failed.Count > 8) failed.TryDequeue(out _);
    }
    public override Task ConnectionFailedAsync(DbConnection connection, ConnectionErrorEventData data, CancellationToken cancellationToken = default)
    { ConnectionFailed(connection, data); return Task.CompletedTask; }

    private void Retire(Guid id, string phase)
    {
        if (!active.TryRemove(id, out var current)) return;
        closed.Enqueue($"phase={phase};id={current.Id};context={current.Context};seconds={(DateTime.UtcNow - current.Started).TotalSeconds:F3};origin={current.Origin}");
        while (closed.Count > 8) closed.TryDequeue(out _);
    }
    private static string State(DbConnection connection)
    {
        try { return connection.State.ToString(); }
        catch (Exception error) { return error.GetType().Name; }
    }

    internal static bool ShouldObserve(EventId id, LogLevel level) => id.Name != null
        && (id.Name.StartsWith("Microsoft.EntityFrameworkCore.Database.Transaction.", StringComparison.Ordinal)
            || id.Name.StartsWith("Microsoft.EntityFrameworkCore.Database.Command.", StringComparison.Ordinal));

    internal void Observe(EventData data)
    {
        if (data is not DbContextEventData { Context: { } context }) return;
        // Store only the event name and SQL shape hash. EventData, commands,
        // parameters and their owning contexts never survive this callback.
        string signature = data is CommandEventData command
            ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.Command.CommandText)))[..12] : "";
        progress[context.ContextId.ToString()] = new(data.EventId.Name, DateTime.UtcNow, signature);
    }

    internal string OriginCost() => $"originSamples={Interlocked.Read(ref originSamples)};originTotalMs={1000d * Interlocked.Read(ref originTicks) / Stopwatch.Frequency:F3};originMaxMs={1000d * Interlocked.Read(ref maximumOriginTicks) / Stopwatch.Frequency:F3}";

    internal string Describe()
    {
        var rows = active.Values.Select(row =>
        {
            string state = row.Connection.TryGetTarget(out var connection) ? State(connection) : "collected";
            progress.TryGetValue(row.Context ?? "", out var last);
            return $"id={row.Id};context={row.Context};phase={row.Phase};state={state};seconds={(DateTime.UtcNow - row.Started).TotalSeconds:F3};thread={row.Thread};origin={row.Origin};lastEvent={last?.Event};eventAge={(last == null ? 0 : (DateTime.UtcNow - last.At).TotalSeconds):F3};sqlShape={last?.Signature}";
        });
        return $"openingAttempts={Interlocked.Read(ref openingAttempts)};opened={Interlocked.Read(ref openedCount)};closed={Interlocked.Read(ref closedCount)};failed={Interlocked.Read(ref failedCount)};disposed={Interlocked.Read(ref disposedCount)};activeAttempts={active.Count};"
            + OriginCost() + ";"
            + string.Join(" | ", rows) + ";recentClosed=" + string.Join(" | ", closed.ToArray()) + ";failedAttempts=" + string.Join(" | ", failed.ToArray());
    }
}
