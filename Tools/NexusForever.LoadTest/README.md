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

`login-world` first waits for both `ServerChangeWorld` and `ServerPlayerCreate`
under one 15-second bootstrap deadline. A delayed map add cannot pass solely
because the server accepts an early `ClientEnteredWorld`. This barrier is
reported as `world-bootstrap` and is a protocol readiness check, not visual UI
or complete gameplay validation.

It then keeps a 250 ms client-entered-world delay by default to mimic the
client staging after those packets. Use
`--client-entered-world-delay-ms 0` when measuring server-side latency without
that client delay.

Use the same `--start-index` for `seed` and concurrent `run` commands to select
an isolated range of users. The default index remains 1. Run reports record the
selected index; for example, `--start-index 9101 --concurrent-users 3` uses
`loadtest9101@example.local` through `loadtest9103@example.local`.

The STS client sends `/Sts/Connect` before password login and does not wait for
a Connect acknowledgement, matching the server's state transition. Connect uses
synthetic harness metadata for the mapped required fields and omits optional
fields. A 15-second deadline bounds connection setup and each complete STS
request/reply transaction, including partial header/body progress. A missing
response therefore fails the sample instead of hanging the run.

## Evidence Anchors

The traffic shape is based on mapped local decompilation evidence:

- `StsInetSocket_SendConnectEnvelope` at `StsConnLib64.MT.dll:18002f410`,
  correlated with `ClientConnectMessage` and the server's `/Sts/Connect` handler
- `StsConn_SendLoginStart` at `StsConnLib64.MT.dll:180006fb0`
- `StsConn_SendLoginFinish` at `StsConnLib64.MT.dll:1800043d0`
- `StsConn_SendListMyAccounts` at `StsConnLib64.MT.dll:180004950`
- `StsConn_SendRequestGameToken` at `StsConnLib64.MT.dll:180004ac0`
- `StsConn_SendConsumeGameToken` at `StsConnLib64.MT.dll:180004da0`
- `SceneLifecycle_EnsureInitialisationStages` at `WildStar64.exe:1403e8000`, which correlates with `ClientEnteredWorld` opcode `0x00F2`.

The harness should stay conservative: if a future scenario needs packet fields
that are not mapped, add evidence first rather than guessing.

## Challenge earning and claims

`run challenge-rewards` exercises mapped cast requests, actual combat Challenge
progress, durable medal points, full reward reloads, choose/claim, logout/login
and cycle reset. It requires a prepared isolated fixture and an explicit placement
opt-in:

```powershell
dotnet run --project Tools\NexusForever.LoadTest -- run challenge-rewards `
  --user loadtest9701@example.local --recipe path\to\recipe.json `
  --allow-fixture-placement --max-kills 15 `
  --auth-connection $authConnection --character-connection $characterConnection
```

Auth and Character must use matching local `nexus_assessment_*_auth` and
`nexus_assessment_*_character` schemas. The user must be an owned
`loadtest<number>@example.local` account. The scenario never seeds accounts,
changes roles/entitlements, writes rewards or forces completion. Its only fixture
write repositions the selected offline Load character before each combat session.
It waits for normal saved logout and the latest closed presence generation before
the next placement. This mode proves socket earning and persistence, separately
from walking and native UI.

The recipe contains `ChallengeId`, `TrackId`, `Spell4Id`, `MaximumPoints`, an array
of allowed `Creatures`, and `Placements` with `EntityId`, `CreatureId`, `World`,
`Zone`, `X`, `Y`, `Z`. Use actual hashed table/world inputs for those values.
Each placement is an existing source spawn; the character starts two units along
its Z axis. The current bounded scenario uses one genuine kill per session. It
recognizes NPC identities from the mapped create prefix and keeps attacking the
target acknowledged by the configured `ServerSpellStart`, rather than assuming
all visible creatures are in range.

The reader observes mapped Challenge updates and complete reward reloads.
Choosing a track waits for a new reload even when it was already active, so a
claim cannot run ahead of that queued operation.
Terminal/tier and initial-activation progress waits for durable SQL and its
published reload before disconnecting. Partial progress must survive normal
cleanup. Claims are split across real logins; selected rows and claimed bits must
remain stable, each receipt must accompany a new inventory row, and the final
reset must preserve the accepted carry policy. `challenge-rewards.json` and
per-kill snapshots retain the bounded evidence and omit credentials. Existing
output directories are never overwritten.

To resume unclaimed earned state after a failed observation, `--placement-offset`
can start at a later supplied spawn. It does not reset Challenge progress,
points, attempts, selected rows or receipts.

### Contract acceptance persistence

On a prepared isolated assessment stack, use an actual offered Contract whose
prerequisites the owned Load character meets:

```powershell
dotnet run --project Tools/NexusForever.LoadTest -- run contract-acceptance `
  --user loadtest9701@example.local --quest 9880 --allow-contract-acceptance `
  --auth-connection $authConnection --character-connection $characterConnection
```

This scenario uses the normal socket login and receiverless Quest accept packet.
It waits for normal logout persistence, then logs in again and requires the same
canonical acceptance identity and accepted/achieved Quest state. SQL observation
is read-only. The selected character must belong to the numbered loadtest account
in matching local `nexus_assessment_*` schemas. All protocol endpoints are loopback.
The additive Contract acceptance migration must already be applied.

An existing active Contract with a valid identity can resume the observation
without sending another accept. Historical active rows without an identity are
rejected by the scenario and remain unmodified. It never places the character,
forces objectives/completion, creates an identity directly, or issues reward
commands. Inventory, currency, Challenge tracks/slots and receipts must remain
unchanged. The output records whether an accept was actually sent, both sessions,
saved states and the remaining native UI and reward-path limits. Evidence
directories are never overwritten.
