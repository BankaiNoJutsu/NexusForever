# ICComm membership and publication ownership

6 November 2026. A reused numeric world GUID no longer permits a different
Player to send through or remove another ICComm membership. Membership now
retains its original Player, GUID and session. A replacement session requires
an explicit new join, and late packets on the old socket cannot operate the
new membership. This completes a Source/public-handler boundary in F-012;
ICComm native/socket/retail qualification remains open.

## Reproduced problem and resulting behavior

The previous manager stored Player values under a numeric GUID and admitted a
sender using ContainsKey alone. A different Player with that GUID could send;
a stale actor could also remove a newer actor's membership. Delivery selected
Players once, then reread their sessions and sender name after outward enqueue
callbacks. That let accepted traffic cross to replacement sessions, recipients
that had left their scope, or a newly created membership with the same key.

The manager now stores an immutable membership handle and checks its reference,
Player, original session, bound Player where available, world lifetime and
current Group/Guild scope. Its captured GUID remains the cleanup index even if
the actor changes its GUID. Cleanup removes only the exact retained handle and
preserves a newer owner's other channel. A leave/rejoin creates a new handle;
a repeated join by the unchanged Player/session retains its existing handle.

Before every acknowledgement, ordered echo and directed delivery, the accepted
sender and relevant recipient are checked again. Enqueues run outside the
membership monitor and target the captured session. Reentrant callbacks that
replace a session, remove/rejoin a member, change scope or remove the sender
stop stale work. Once Sent was acknowledged, cancellation does not emit a
contradictory second result. SenderName is the name captured at acceptance.
The public join/message/not-joined handlers also require the original socket
and Player binding; join/error replies are suppressed if that binding changes.

Global canonicalization, exact request-name replies, named Group/Guild keys,
the 512-character message limit and ordinary acknowledgement/echo behavior are
preserved. This introduces no entitlement, quota, throttle, circle, automatic
rejoin or persistent-channel policy.

## Qualification and retained evidence

The initial 20 public-handler regression cases all failed against unchanged
production. Review strengthened the GUID cleanup control to require deletion
of the abandoned channel, and added reply-rebinding and unchanged-repeat-join
controls. Replaying the same final 24 cases against production from published
parent `3fae8296` produced 23 failures and one passing control. The fixed source
passes all 24 and all 50 selected ICComm cases, including the existing 26 join,
message, exact-name, scope, packet and cleanup controls. Baseline and final
binaries and TRX reports are retained separately.

WorldServer and Game.Tests were built incrementally in a source-only candidate
archived from the published parent plus the three owned ICComm files. This
excludes the concurrent Q3777 provider-fixture edits from qualification. Final
owning/test build times are 10.62/14.73 seconds. The owning build has zero
warnings/errors; the test dependency graph retains the existing ASPIRE010
warning and introduces no analyzer warning. No full solution or full suite ran.

Commands, run against the isolated source candidate:

```powershell
dotnet build Source/NexusForever.WorldServer/NexusForever.WorldServer.csproj --no-restore -v quiet --nologo
dotnet build Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v quiet --nologo
dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~ICComm
```

The [portable qualification record](coverage/iccomm-membership-ownership-20261106.json)
pins all three source files, frozen baseline/final Game/World/network/test
DLLs and PDBs, TRX hashes and both review axes. Compiler checksums match 1,018
Game, 460 World and 726 test documents. Final owning and test runtime DLL hashes
agree. Separate primary-agent Spec and Standards self-reviews have no open
material findings; no independent reviewer is claimed.

Raw evidence remains at
`I:/GIT/NexusForever-restoration-20261106/artifacts/iccomm-ownership-20261106/`.
The editable continuation is isolated at `I:/GIT/NexusForever-iccomm-20261106`
on `codex/iccomm-membership-ownership-20261106`. Only byte-verified ICComm edits
were transferred from the shared checkout; Q3777 source/docs/coverage remain
there unstaged by this work. The raw ignored candidate, binaries and reports
require separate transfer; Git carries the implementation and portable record.

## Remaining gates

The [earlier native and authored contracts](ICCOMM_JOIN_CORRELATION.md) remain
inputs for request-name/ready/join/left meaning. This pass uses existing Source
ownership invariants and public handlers, not new native execution. No database
was written and no server, broker or client was launched for this slice.
Computer Use and native-client launch remain stopped.

Socket/native callback execution, disconnect/reconnect ordering, cross-realm
behavior, circles, entitlement identity, joined-channel quotas, active formula
budgets, server throttling and persistent membership still need their own
witnesses. This Source candidate does not inherit the prior hosted World/Auth
qualification: that milestone binds its different frozen DLLs at `3fae8296`.
Do not claim addon receipt or retail parity from in-process enqueue observations.
The overall restoration remains unfinished; continue independent actionable
restoration work or a separately owned ICComm protocol qualification.
