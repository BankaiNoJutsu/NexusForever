# NexusForever Login-to-World Profiling and Load-Test Plan

## Summary
Build an evidence-first profiling/load-test workflow focused first on single-user login-to-world latency, with mock data realistic enough to reveal bottlenecks before scaling.

The first scenario is `LoginWorld`: STS/auth login, token/realm handoff, world connect, character list, character select, enter world, and a short idle heartbeat/statistics window. Success means every run identifies where time is spent: client step, packet queue wait, handler execution, database calls, tick/subsystem update, outbound flush, broker/API calls, and server-side backlog.

Once single-user latency and interaction time are optimized and stable, extend the same harness to concurrent-user testing with ramp profiles.

## Key Changes
- Add opt-in diagnostics using built-in `ActivitySource`/`Meter` under `NexusForever.Shared`, disabled by default via `Diagnostics:Profiling:Enabled`.
- Instrument the login/world hot path: `WorldManager` tick duration, `NetworkManager` session counts/queue work, `GameSession` packet read/handler/flush timings, STS transaction handling, async event/task completion delay, EF auth/character DB operations, and major world subsystem update buckets.
- Add `Tools/NexusForever.LoadTest` with commands:
  `seed`, `run login-world`, and `report`.
- Add mock data profiles:
  `minimal`, `typical`, and `heavy`, with deterministic users like `loadtest0001@example.local`.
- Add a second-phase concurrent mode after single-user optimization:
  `run login-world --concurrent-users 10 --ramp 10,50,100,250,500`, reusing the same scenario and bottleneck attribution.
- Use decomp before harness finalization to verify STS/world-login packet order and fields, especially `/Auth/*`, `/GameAccount/ListMyAccounts`, `ClientHelloRealm`, `ClientCharacterList`, `ClientCharacterSelect`, and `ClientEnteredWorld`.

## Public Interfaces
- New config section: `Diagnostics:Profiling` with `Enabled`, `SlowOperationThresholdMs`, and `IncludePacketPayloadSizes`.
- New metrics/spans:
  `nexus.tick.duration_ms`, `nexus.tick.subsystem.duration_ms`, `nexus.packet.queue_wait_ms`, `nexus.packet.handler_ms`, `nexus.packet.flush_ms`, `nexus.db.operation_ms`, `nexus.session.active`, `nexus.packet.queue.length`.
- Load-test CLI:
  `dotnet run --project Tools/NexusForever.LoadTest -- seed --profile typical --users 1`
  `dotnet run --project Tools/NexusForever.LoadTest -- run login-world --user loadtest0001@example.local --password loadtest --samples 30 --warmup 3`
  `dotnet run --project Tools/NexusForever.LoadTest -- run login-world --profile typical --concurrent-users 50 --duration 5m`

## Test Plan
- Build: `dotnet build Source\NexusForever.sln` and `dotnet build Tools\NexusForever.LoadTest\NexusForever.LoadTest.csproj`.
- Seed test: create one `typical` user, rerun seed idempotently, verify no duplicate rows.
- Protocol test: run one `LoginWorld` sample and verify it reaches `ServerPlayerEnteredWorld`.
- Profiling test: run 30 serial samples and verify p50/p95/max, per-step timings, server bottleneck attribution, DB timings, packet handler timings, and tick backlog indicators.
- Concurrent test, after single-user optimization: run ramp stages and verify the report shows saturation points, error rates, queue growth, p95/p99 latency, and first bottleneck by subsystem.
- Regression check: run with profiling disabled and verify normal server startup/login still works with minimal overhead.

## Assumptions
- First optimization target is single-user latency and interaction time.
- Concurrent testing starts only after the single-user path has stable baseline targets and no obvious bottlenecks left.
- Decomp evidence is authoritative for mock traffic shape; implementation should not guess packet order or fields when evidence is missing.
- Larger architecture changes should be proposed from measured bottleneck data, especially around single-thread tick saturation, large EF include graphs, synchronous socket sends, service resolution per packet, broker/API roundtrips, or missing database indexes.
