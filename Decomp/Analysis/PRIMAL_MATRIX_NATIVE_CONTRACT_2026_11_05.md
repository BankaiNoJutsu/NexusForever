# Primal Matrix save and snapshot contracts - build 16042

Subsequent Source result: [Primal Matrix wire contracts](PRIMAL_MATRIX_WIRE_CONTRACTS.md)
now implements `0072` decoding/diagnostic dispatch and `06DF` snapshot registration.
Statements below about the formerly missing enum/model surface describe the
research checkpoint. Allocation gameplay, timing and retail qualification remain open.

5 November 2026. This follow-through closes the previously missing native save
request and maps the fields of the wrongly named `ServerAuctionPostAux` packet.
It is static shipped-artifact/field corroboration, not a completed allocation
service or retail runtime qualification. No production Source changed.

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
