# Primal Matrix save and snapshot contracts - build 16042

Subsequent Source result: [Primal Matrix wire contracts](PRIMAL_MATRIX_WIRE_CONTRACTS.md)
now implements `0072` decoding/diagnostic dispatch and `06DF` snapshot registration.
Statements below about the formerly missing enum/model surface describe the
research checkpoint. Allocation gameplay, timing and retail qualification remain open.

5 November 2026. This follow-through closes the previously missing native save
request and maps the fields of the wrongly named `ServerAuctionPostAux` packet.
It is static shipped-artifact/field corroboration, not a completed allocation
service or retail runtime qualification. No production Source changed.

The later [allocation-rule follow-through](#allocation-rule-follow-through)
maps the editor's hex graph, node states, class reward selection and currency
representations. It narrows the remaining inputs; the allocation handler stays
diagnostic until actual catalogs and authoritative settlement are qualified.

## Two different contracts

| Direction/opcode | Native payload | Meaning established by the client |
| --- | --- | --- |
| C2S `0072` | 32-bit count, that many uint32 node IDs, that many bytes | Positive **allocation increments** selected for purchase |
| S2C `06DF` | 32-bit count, that many uint32 node IDs, that many bytes, final uint32 | **Absolute saved allocation counts** plus the Matrix contribution to AMP power |

The request and snapshot share parallel-array shapes but give the bytes different
meanings. Do not reuse an absolute-allocation mutation for the request, or treat
the snapshot as a delta. No observed request/response pairing or acknowledgement
timing is claimed from these separate client paths.

## Saved-node snapshot fields

The indexed `06DF` dispatch at `1403f0fb0` calls `1404d6af0`. Its cache bootstrap
`1404d64d0` enumerates the actual `PrimalMatrixNode` table, creates node state,
and indexes the normal-node objects by the table row's first DWORD, its ID.
The snapshot's uint32 array selects those same keys. Each parallel byte writes
the selected cached node's `+0x40` field.

This field is now independently named. The pinned `MatrixWindow.GetAllocationCount`
binding at `140c5ea70` points to `1406eb160`, which sums those saved-state bytes.
`GetPendingAllocationCount` at `1406eb230` sums positive editor-count minus saved-
count differences. `GetTotalAllocationCount` at `1406eb2c0` instead sums backing
table row `+0x14`, matching the `MaxAllocations` field. The node-to-Lua builder
`1406e9330` explicitly publishes:

| Native state | Exact Lua field |
| --- | --- |
| editor node `+0x44` | `nAllocations` |
| cached node `+0x40` | `nSavedAllocations` |
| backing row `+0x14` | `nMaxAllocations` |

Thus the incoming bytes are saved allocation **counts**, not Boolean flags.
The [authored UI source](PRIMAL_MATRIX_WEB_CORROBORATION_2026_11_05.md) supplies
independent historical API usage; the exact fields above are verified in the
pinned native artifact, so they do not depend on assuming that the older Lua
snapshot is the final build.

The snapshot first sets every normal cached node count to zero, then applies
the listed rows. Omitted nodes therefore remain cleared on this observed path;
an empty list is not a harmless no-op. An unknown node key returns early, so
the client path is not an atomic validate-then-replace operation. A future
producer needs coherent, validated node IDs and must respect actual timing.

The final uint32 writes player `+0x6E68` and invokes `1403cdbf0`. That routine
combines level-derived power with the separate fields `+0x6E64` and `+0x6E68`,
then writes total power at `+0x6E60`. The exact `AbilityBook.GetTotalPower`
registration points to `140748f00`; its LLVM instructions read signed16
`+0x6E60` for the Lua result. Therefore the final packet word is the Matrix
contribution, not the total or the separate earned-bonus field. The client UI
getter's signed16 conversion is not a server-side cap rule.

Changing the Matrix contribution conditionally dispatches
`CharacterEldanAugmentationsUpdated`; successful completion dispatches
`PrimalMatrixUpdated`. Neither event establishes a save acknowledgement policy.

## Native save request recovered

Pinned instructions at `14070c21d..14070c232` register the exact string
`SavePrimalMatrix` with confirm type `41` (`0x29`). The factory at `140526610`
has a `case 0x29` that creates the action with table `140b6a9c0`.
Its setup callback at `+0x18` is `1405239b0`; its confirm callback at `+8` is
`140523a60`. This independently verifies the public native-excerpt lead against
the matching binary.

Setup retains the selected MatrixWindow and calls `1406e6910`. That staging
routine walks the window's nodes and compares saved cache `+0x40` with editor
`+0x44`. It includes a node only when editor count is greater. It appends:

- The node table ID, through `140003460`, whose four-byte vector append copies
  the first DWORD from the supplied row pointer.
- One byte equal to **editor count minus saved count**.

The staging path also multiplies that increment by the node row's costs at
`+0x18/+0x1C/+0x20/+0x24`, with currency tags `15/16/17/18` for the corresponding
essences. This maps the client quote inputs, not authoritative server balances,
transaction rules or all overflow/invalid-data behavior.

On confirmation, `140523a60` requires the two retained list lengths to match
before invoking `Network_SendOpcodePayloadHelper` (`1403f4900`) with opcode
`0x72`. The generic helper's observed network encode/send calls establish that
this value is an outgoing protocol opcode, not the confirm enum or an internal
event ID. It then raises `PrimalMatrixSaving` and retires its staged action.

Registration at `1400716c0` binds opcode `0072`, object size `0x18`, to writer
`14008be20`. That writer emits count32, a span of `count * 4` node-ID bytes and
a span of `count` allocation-increment bytes. This independently corroborates
the caller's payload. The shared read/thunk address `14008bdf0` has no function
body in the copied project and is not described as successfully decompiled.

At this research checkpoint, `GameMessageOpcode` and handler/model Source had
no `0072` surface; the linked subsequent wire milestone now supplies it.
The former `06DE` adjacency candidate was not used to infer this
request: its only selected immediate hit was registration, while `0072` has
the direct confirmation sender, writer and ID/increment staging above.

## What is now implementable, and what remains gated

This evidence supports a correctly owned `06DF` snapshot wire model and a new
`0072` request decoder with spellings that preserve the increment/absolute
distinction. It also supports wire-focused checks against these mapped widths.
It does not yet support enabling an allocation handler merely because those
models can be added.

The remaining gameplay slice needs authoritative node/reward rows, ownership,
max-rank/adjacency/prerequisite validation, checked currency settlement, saved
allocation persistence, reward/AMP adoption, original-actor publication and
recovery. The older UI warns purchases cannot be refunded after saving;
[its version boundary](PRIMAL_MATRIX_WEB_CORROBORATION_2026_11_05.md#deeper-saveconfirm-lifecycle-follow-up)
is retained. No general post-commit reset, refund or respec API was inferred.

## Evidence and validation

REA PE identity is
`ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`,
binding `WildStar64.exe` to SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
REA's known native-provider configuration was not retried. The separate existing
Ghidra12.0.4/JDK21 project copy was opened read-only with analysis disabled;
LLVM corroborated selected instructions. The game client and STS library were
not executed.

The [portable deeper-contract record](coverage/deeper-packet-contracts-20261105.json)
binds selected code ranges, API pairs and field-name data to this exact PE and
the inspected Ghidra program. Raw scripts, pseudocode, range checks and source
retrieval evidence remain under `artifacts/continuation-20261105/blocker-next-targets/`.
All 30 selected code ranges and 13 additional anchors match independently in
the pinned PE and the read-only Ghidra program.
No application build/test or retail gameplay result is claimed for this
research pass. Related [spell consumer corroboration](SPELL_AUX_CONSUMER_CORROBORATION_2026_11_05.md)
separately identifies target-unit/count fields and retains timer-replay gates.

## Allocation-rule follow-through

5 November 2026, after published `22308c2a`. Read-only Ghidra12.0.4 and LLVM
inspection of the same pinned PE resolves additional client editor and preview
rules. This is mapped-only evidence, with no production Source, handler, schema,
labels or project changes. The unavailable REA native provider was not retried;
the existing shipped-artifact REA identity remains the provenance input.

### Hex graph and admission state

Cache bootstrap `1404d64d0` indexes normal nodes by row ID and coordinates.
Its six coordinate offsets at `140c49e40` are `(0,-1)`, `(1,-1)`, `(1,0)`,
`(0,1)`, `(-1,1)` and `(-1,0)`. Type1 rows form a separate special grouping;
the cache retains one only when all six surrounding normal nodes exist. That
bootstrap is not proof of the group's reward or server admission policy.

The native enum registrar `1406e7f70` names states0 Locked,1 Unlocked,2 Started
and3 Completed. State calculation `1406e4fc0` first handles nonzero editor
allocations: below the row maximum gives Started, otherwise Completed. A zero
allocation is Unlocked when row Flags bit0 is set or one of the six normal
neighbors is Started/Completed; otherwise it is Locked. Input `1406e73e0` clamps
editor increments to the row maximum and refuses to lower a node below its
saved count. Its pending-only deallocation path invokes `1406e6800` and can
restore an attempted edit after a connectivity failure. This establishes no
postcommit refund, reset, respec or complete special-group reward rule.

### Class rewards and numeric representation

Selector `1406dfa40` reads the player's class and chooses node-row reward columns
at offsets28/2C/30/34/38/3C for Warrior1, Engineer2, Esper3, Medic4, Stalker5 and
Spellslinger7 respectively. Other class values return0. The enum table at
`140c33d50`, selected by the registrar's LLVM instructions, gives:

| Value | Name | Observed preview in `1406e8a60` |
| --- | --- | --- |
| 0 | None | No supported reward output |
| 1 | RewardProperty | `tRewardProperty`, property identity and scaled `fValue` |
| 2 | AbilityPoint | Scaled `nAbilityPoints` |
| 3 | AMPPoint | Scaled `nAMPPoints` |
| 4 | AbilityUnlock | Spell-wrapper lookup, `splAbility` and `nTierUnlocked` |
| 5 | UnitProperty | Validated property metadata and scaled `tUnitProperty.fValue` |

LLVM confirms `MULSS` against the table value after converting the allocation
count to single precision. A decompiler's integer-pointer cast must not be used
to replace the existing float fields with integer interpretation. Ability
preview uses the separate tier lookup `[1,5,9]` at `140b720a8`; this is not proof
that every reward grants an ability or that arbitrary object/subobject rows are
valid. Actual reward rows and server application/adoption remain required.

### Currency paths must remain distinct

The `GameLib.GetPlayerCurrency` binding at `140b72980` selects `1406f7b60`.
For Money IDs1 through14 it reads the player currency cache at
`player-global+15F0 + id*8`. Matrix affordability `1406e4e00` reads offsets
1648/1650/1658/1660, matching red11/blue12/green13/purple14, and includes pending
costs before another increment. Node price builder `1406e9330` uses those same
Money IDs. By contrast, confirmation staging `1406e6910` constructs Money type15
with account subtypes15/16/17/18. The separate `GetAccountCurrency` binding
`140b69960 -> 1404e5640` reads a different account cache and constructs that
type/subtype representation. Native account enum records explicitly name the
four essence subtypes.

The immutable authored UI independently uses account-currency metadata for
essence tooltips and `GameLib.GetPlayerCurrency` for displayed amounts and
pending costs. Its editor exposes locked nodes and pending deallocation errors.
[Authored Matrix UI](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/PrimalMatrix/PrimalMatrix.lua#L683-L703).
The fetched bytes match the earlier recorded Lua SHA256. This third-party
mirror preserves NCsoft-authored consumer source; it is not authenticated
final-build server source. Neither representation alone authorizes choosing
Character currencies11-14 or Auth currencies15-18 for a server debit. A matching
allocation/balance or independent server-owned synchronization witness remains
necessary before settlement.

### Verification and next required input

All14 selected FunctionManager body ranges and nine data anchors match raw PE
bytes independently. Full bodies are verified, including chained bodies and
the affordability leaf with no PE runtime-function entry. An initial mapper
assumption about that leaf and an initial multi-script-path invocation error
remain retained as tooling diagnostics. Four successful static passes take104s
between their log timestamps; no C# build or test suite was needed or run.

The bounded exact-name lookup under
`C:/GIT/_archive/cluster-3d/NexusForever` found no `PrimalMatrixNode.tbl` or
`PrimalMatrixReward.tbl`. This is not an all-host/remote absence claim. The
handoff identifies the original installation/archive/table corpus as external.
The next data input is an exact-build, hashed node/reward corpus plus referenced
spell/property rows; the next ownership input is authoritative essence debit/
synchronization evidence. Do not repeat the recovered enum/neighbor/class
search or substitute synthetic rows as that missing corpus. Continue independent
roadmap work while those inputs remain unavailable.

The [portable follow-through record](coverage/primal-matrix-gameplay-contracts-20261105.json)
binds the23 byte checks, six reward enum records, raw LLVM/Ghidra logs, named
getters, authored-source identity and remaining gates. Local evidence is under
`artifacts/continuation-20261105/primal-matrix-gameplay/`. No game client/DLL or
Computer Use was executed. Allocation persistence, rewards, committed original-
actor publication/recovery, provider/deployment/socket and native/retail remain
open. Entitlement defaults15:1/17:1 and no-auto44/seven policy are unchanged.
Spec review corrected one authored-source line-locator error against the
hash-bound raw Lua; the initial artifacts are retained. Both final review axes
have0 remaining findings.
