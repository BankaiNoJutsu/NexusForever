# Protocol and network fixes from upstream

Implemented and verified on 2026-10-29 in the
`legacy-di-storefront-hardening` checkout based on `f069e0b9`.
This is a focused compatibility and network reliability slice, not a retail
completeness claim.

## Protocol evidence and changes

The source baseline for the packet changes is official `game_rework` at
`1f643f6374ec017ae46ac3bd72ad99d42c902d46`:

- [PR #587](https://github.com/NexusForever/NexusForever/pull/587), merged via
  `845651e6`: both client Galactic Archive messages and the server article update
  carry a 14-bit article ID. The update contains 78 payload bits, padded to
  10 bytes. Keep the fork's uint APIs, archive persistence and handlers; change
  only the wire width. Existing tests using 32-bit IDs were corrected and raw
  two-byte client fixtures now cover the actual width.
- [PR #591](https://github.com/NexusForever/NexusForever/pull/591), commit
  `235ece6c95c99fb7774f5e564d565c675166a09d`: consume seven diagnostic bits between
  resurrection UnitId and the 32-bit resurrection type; read auction cancellation
  as 18-bit Item2Id followed by 64-bit AuctionId; use a three-bit action-set spec
  index, an 18-bit guild-bank source item ID, and one diagnostic realm-transfer
  request bit. Keep resurrection-type validation and the fork's realm-bank
  location mapping. Unknown fields have no assigned gameplay semantics.

The fork already matches other upstream wire changes despite different names:
64-bit trade item IDs/credits, 32-bit matching roles, storefront float-bit
preservation and shortened autocraft payloads. Those APIs and their handlers
remain intact. This slice does not reinterpret the divergent manual-crafting
count/catalyst fields, replace the pet stance mask conversion, or assign
semantics to opcode 0x0635. Those need their own evidence review.

## Network and shared-runtime changes

- [PR #571](https://github.com/NexusForever/NexusForever/pull/571), commit
  `90b6e2e7b2844649ffa090ad724117f98e33ef1e`: close sockets in finally even if
  endpoint lookup or shutdown fails. Keep disconnect completion/idempotence and
  tolerate sessions disconnected before accepting a socket.
- [PR #572](https://github.com/NexusForever/NexusForever/pull/572), reviewed head
  `39012c403e3d2fcdf0d9f7947c14c95ba5fd9c16`: bind synchronously, track the full
  async accept loop, capture each run's cancellation token, stop in finally and
  wait for the loop/callback to finish before disposing cancellation state.
  Start/Shutdown retain the existing sequential-caller assumption.
- [PR #573](https://github.com/NexusForever/NexusForever/pull/573), reviewed head
  `00f8876ffa6ac239a7b98db153d9fd1c6a81734e`: commit cached byte/bit position only
  after a successful refill. Scalar and optimized bulk readers share that
  boundary, so repeated EOF retries cannot expose stale bits. Existing bulk
  reads, seeking, padding and zero-length behavior remain covered.
- [PR #575](https://github.com/NexusForever/NexusForever/pull/575), reviewed head
  `ac10142e60b6587b27b73da07ef9d4448f09a3af`: read UTF-16LE code units and decode
  the complete string to preserve supplementary characters. The caller's reader
  encoding does not control the table format; missing/truncated terminators
  still fail and following bytes remain unread.
- [PR #574](https://github.com/NexusForever/NexusForever/pull/574), closed head
  `12f77cfa61c7e3820fccb9ebc0bcaf639337ab86`: preserve deferred events when a
  blocking event is unready. Upstream retired Blocking; this fork keeps that
  API and ends the loop through the normal deferred-event requeue path instead.
  Execution exception handling remains intact.

Packet dispatch scopes and the APM receive/framing implementation are unchanged.
The maintainer's runtime concern on closed #565 still requires a separate scope
lifetime investigation. Group dirty jobs, entitlements, loot and travel are
outside this slice.

## Regression evidence

The existing `NexusForever.Game.Tests` project now includes 44 additional cases:

| Area | New cases |
| --- | ---: |
| Scalar/bulk EOF retries and valid bit-stream behavior | 12 |
| Listener failed-bind retry, callback shutdown and restart | 3 |
| Socket shutdown errors, connected/closed/disposed/unaccepted sessions | 5 |
| UTF-16LE strings, supplementary characters and terminators | 9 |
| Deferred events, ordering and exactly-once execution | 4 |
| Upstream packet fixtures and following-field alignment | 11 |

An initial 36-case regression slice reproduced 19 failures / 17 passes before
production edits. Listener restart also failed in that run; its failure was
intermittent in the earlier assessment. The first patched focused slice passed
74/74. After adding two socket lifecycle preservation cases, the full solution
suite passed 5,810/5,810, zero failures and zero skips. The full solution build
passed with zero errors and the same 114 warnings as the pre-change baseline.

Reproduce from the repository root with .NET SDK 10.0.300:

```powershell
dotnet build Source\NexusForever.slnx -v minimal --nologo
dotnet test Source\NexusForever.slnx --no-build --no-restore -v minimal --nologo
git diff --check
```

The assessment machine retains the before/focused/full build logs and TRX files
under `I:\GIT\NexusForever-assessment-report\implementation`. These are local
verification artifacts, not required repository inputs. Client login/gameplay
smoke was not run; the network tests exercise real local loopback sockets.
