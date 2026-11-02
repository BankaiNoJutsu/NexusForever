# Build16042 static Contract and communicator cross-check

31 October 2026 destination continuation. This is fresh static shipped-artifact
evidence, not client execution, a recovered server implementation or retail
qualification. Computer Use/native client launch remain stopped.

## Input and method

REA `inspect_pe_imports` binds the archived `Decomp/Client64/WildStar64.exe` to
SHA256 `231BB2BB3FC6C37F3E8A43A0BA965CC3645287BBC6CCAD83D073A495C396B3E5`,
Evidence `ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`.
The input is read-only in the old mixed checkout; no old file was changed.

Ghidra remains unconfigured. Installed LLVM22.1.6 `llvm-objdump` reads selected
address ranges without executing the client. Its executable SHA256 is
`3AE6715601F2EB0C1D0839929C46089906BA0F1E5E032FE519656A40C936B863`.
The REA PE record establishes input identity; the LLVM outputs are separate
inspector observations, not REA decompiler dossiers. REA's process-capture route
is not used because its documented new-capture platform support excludes Windows.

Local scripts parse the PE exception directory and follow version1
UNW_FLAG_CHAININFO to group every observed fragment under its primary entry.
This follows [Microsoft's x64 unwind format](https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64?view=msvc-170).
The first strict lookup stopped at a helper with no unwind entry. That failed
attempt and the earlier narrow outputs remain distinct; no extent was guessed.
Chained groups can comprise several unwind records, so one record is not treated
as the whole function. No unsupported unwind metadata was observed in this image.
This grouping does not prove that every cold or tail-called semantic block has
been recovered. Names below remain analyst/upstream labels, not recovered symbols.

Scripts, input hashes, complete raw REA record, commands, exact range offsets/
hashes and disassembly are under `artifacts/continuation-20261031/native-static`.

## Contract claimability

`1404d2240` has four chained parts spanning `[1404d2240,1404d22e4)`.
The selected index is bounded at2277–227a, its reward pointer is checked at227c–2284,
and its own claimed bit is tested at229b–22a3. The cumulative point cost at that
index is compared with current points at22ab–22bb. The observed group contains
no earlier-slot claimed-bit scan, call to such a scan or reset write.

Together with the exact restored Contracts.lua final-index branch, this locally
corroborates independent final-reward claimability. The earlier retained native
fragment was unavailable here; this fresh byte-level reconstruction replaces
that availability gap. It does not prove whether the retail server discarded,
retained or delivered earlier unclaimed milestones or excess points after a
successful final claim. Public07CE remains disabled pending the connected reset
and full-delivery requirements in the Contract ledger.

## Communicator observations

| Address/group | Locally observed static fact | Qualification |
| --- | --- | --- |
| `14043d870–14043da00`, seven chained parts | Zero quest ID succeeds; missing lookup returns false; effective states5–7 become0; states5–10/14–19 dispatch to the objective helper when the native map lookup succeeds; states11/12/13 have their distinct branches | Quest2 field meanings and native-map/server active-record equivalence still require their complete context; this is not a full-table delivery proof |
| `1405510d0–140551170` | Objective index is checked against the stored objective count; ordinary objective bit positions are index+2 for indices below6; raw state3 has a separate completed shortcut | The outer native-map membership check remains material; do not infer a Completed server record is an active native record |
| `14043d650–14043d7d7` | Existing-native-record branch checks flag0x10000 atd6d3; the other branch checks flag0x4 atd799 and calls its start predicate | The leaf lookup at552f50 itself is not the callback flag test; those tests are in this caller |
| `14043d3d0–14043d64f`, nine chained parts | Class/race/faction, reputation and level gates precede three quest/state pairs, the prerequisite and offer helper | Actual raw tables and all callback/prerequisite semantics remain separate |
| `14043b7b0–14043b87d` | Server-push path tests the row's bit0 and conditionally calls the condition routine | Server pending timing and scripted/chain arming are not established |
| `14043c3f0–14043c487` | Delayed path rechecks the same condition routine before continuing to delivery helpers | No one-second poll,300-second server lifetime or four-round server drain bound is proven retail timing |

The context-only `[140552f50,140553050)` dump has no containing unwind entry.
Its observed body returns the result of a native-map lookup and reaches returns
before padding, but no full semantic function extent is claimed from that
256-byte selection. The client-report group's calls were retained for follow-up;
this note does not claim new packet/UI ordering qualification from their names.

### Reputation branch mapped; implementation verification gate remains

The row layout in `CommunicatorMessagesEntry` maps offset48 to the reputation
faction and offsets4c/50 to minimum/maximum. At `14043d501–14043d510`, a zero
minimum bypasses the lower comparison; otherwise the comparison rejects a
finite amount below the minimum. At `14043d512–14043d524`, a zero maximum bypasses
the upper comparison; otherwise a finite amount at or below the maximum passes.
The finite boundaries are inclusive. The field mapping plus these branch bytes
supersede the earlier statement that the inequality was wholly unverified.

The current fork deliberately preserved its older zero-minimum floor while the
native inequality was unknown. Consequently it still refuses a negative finite
amount when the row's minimum is zero; the observed client branch skips that
comparison. This is a concrete correction proposal, not an implemented or tested
change. The continuation guide's Verified gate still requires corroborating
table data, runtime, sniff or packet evidence before this server behavior changes.
The native instruction/layout correlation alone remains Mapped. Twenty-two
proposed finite-bound/nonfinite-policy cases are retained outside Source as an
unbuilt/unexecuted artifact in artifacts/continuation-20261031/communicator-reputation.
Their earlier temporary Source draft was never built or executed; the identical
proposal SHA256 is D68975FAF5C6C26A1A641D3E2CAD6051A77DB42B983EAF5C48B866BA1172A548.
The existing source predicate and tests remain unchanged. NaN/unordered and infinity behavior has no fresh
runtime observation and must not be promoted silently from floating-point flag
interpretation; preserve or explicitly qualify the server's invalid-data guard.

The restored historical input ledger identifies CommunicatorMessages.tbl.sql as
481759 bytes, SHA2566a46e0e5d3fed29e375206a82707e3c519c2f1ecb7e4a606dc987566f515810c.
The former I: destination and D:/Wildstar_TEMP source are unavailable here.
The old mixed checkout lacks its raw client SQL dump directories, aggregate
wildstar/jabbithole dumps, client Patch directory and the selected generated
communicator CSV. Those are bounded path checks, not a whole-machine absence
claim. Raw-table recovery/independent corroboration remains work; native launch
and Computer Use have not been resumed.

Raw4105-row classification, actual table delivery, complete native lifecycle/
presentation, provider/copied-upgrade/deployment/socket gates and retail
completeness remain open. No entitlement defaults or grants changed.
