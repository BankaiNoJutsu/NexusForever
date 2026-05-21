# NexusForever.LoadTest

Evidence-backed profiling and load harness for the login-to-world path.

The first implemented scenario is `login-world`. It is intentionally optimized for
single-user latency first, then reused for concurrent users after the baseline is
stable.

## Commands

```powershell
dotnet run --project Tools\NexusForever.LoadTest -- seed --profile typical --users 1
dotnet run --project Tools\NexusForever.LoadTest -- run login-world --user loadtest0001@example.local --password loadtest --samples 30 --warmup 3
dotnet run --project Tools\NexusForever.LoadTest -- run login-world --profile typical --concurrent-users 50 --duration 5m
dotnet run --project Tools\NexusForever.LoadTest -- run login-world --client-entered-world-delay-ms 0
dotnet run --project Tools\NexusForever.LoadTest -- report --input artifacts\load-tests
```

`login-world` keeps a 250 ms client-entered-world delay by default to mimic the
client staging between character select and entering the world. Use
`--client-entered-world-delay-ms 0` when measuring server-side latency without
that client delay.

## Evidence Anchors

The traffic shape is based on mapped local decompilation evidence:

- `StsConn_SendLoginStart` at `StsConnLib64.MT.dll:180006fb0`
- `StsConn_SendLoginFinish` at `StsConnLib64.MT.dll:1800043d0`
- `StsConn_SendListMyAccounts` at `StsConnLib64.MT.dll:180004950`
- `StsConn_SendRequestGameToken` at `StsConnLib64.MT.dll:180004ac0`
- `StsConn_SendConsumeGameToken` at `StsConnLib64.MT.dll:180004da0`
- `SceneLifecycle_EnsureInitialisationStages` at `WildStar64.exe:1403e8000`, which correlates with `ClientEnteredWorld` opcode `0x00F2`.

The harness should stay conservative: if a future scenario needs packet fields
that are not mapped, add evidence first rather than guessing.
