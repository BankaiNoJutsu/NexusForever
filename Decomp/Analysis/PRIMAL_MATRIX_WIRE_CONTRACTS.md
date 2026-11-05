# Primal Matrix request and snapshot wire contracts

5 November2026. The public protocol boundary is implemented and scoped verified.
The allocation service remains unfinished. The
[portable milestone record](coverage/primal-matrix-wire-20261105.json) binds the
native inputs, Source/binaries, two executed baseline failures,16 new/143 scoped
passes, owning WorldServer build and both final Source reviews.

## Registered contracts

| Direction/opcode | Registered model | Counted payload |
| --- | --- | --- |
| C2S`0072` | `ClientPrimalMatrixSave` | uint32 count, uint32 node IDs, uint8 allocation increments |
| S2C`06DF` | `ServerPrimalMatrixSnapshot` | uint32 count, uint32 node IDs, uint8 absolute saved allocation counts, uint32 Matrix AMP-power contribution |

The decoder checks the remaining bytes for both arrays before multiplying or
allocating the untrusted count. It preserves raw byte values, including zero and
high bits, for future authoritative validation. The existing retail composite
helpers handle both arrays. Actual packet-reader/message-registry/handler dispatch
is tested. The handler logs the decoded count without reading session gameplay
state, mutating anything or sending a response.

`06DF` has one canonical registration. The historical `ServerAuctionPostAux`
class remains an unregistered compatibility serializer and delegates to the same
payload writer. Its original bytes remain unchanged. The misleading enum name is
replaced; this is the same corrected-owner approach used for reward-track aux
contracts. No known production auction emitter used the old class. The snapshot
writer checks matching column counts before writing any payload and preserves
the final word at its full uint32 width.

No snapshot producer or purchase acknowledgement is added. The native consumer
clears omitted normal nodes, including for an empty snapshot. Consequently an
empty placeholder reply would alter the client's allocation state and is not a
valid diagnostic response.

## Shipped-artifact evidence

The matching build16042 `WildStar64.exe` has SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Existing REA6.8.0 PE Evidence
`ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`
and its exported bundle are reused. Fresh raw PE identity, section-offset and
hash checks match14 selected code ranges and10 data anchors. These ranges were
also covered by the retained read-only standalone Ghidra verification of30 ranges
and13 anchors. Ghidra/LLVM observations remain separate from REA identity/import
evidence; the known unavailable native provider was not retried or reconfigured.

| Native path | Observed relationship |
| --- | --- |
| `14070c21d` and action factory`140526610` | `SavePrimalMatrix` confirm type41 selects the staged action |
| Setup`1405239b0`, staging`1406e6910` | Selects positive editor-minus-saved increments; node row IDs and increment bytes are appended separately |
| Confirm`140523a60` | Checks retained list lengths and invokes the actual network helper with opcode`0072` |
| Writer`14008be20` | Writes count32, count-times-four ID bytes, then count increment bytes |
| Dispatch`1403f0fb0` to`1404d6af0` | Applies`06DF` as a normal-node saved-state snapshot |
| Cache`1404d64d0`, Lua builder`1406e9330` | Correlate row IDs and `nSavedAllocations`/`nAllocations`/`nMaxAllocations` fields |
| Getters`1406eb160`, `1406eb230`, `1406eb2c0` | Independently distinguish saved, positive pending and table maximum counts |
| `1403cdbf0`, `AbilityBook.GetTotalPower` binding | Separate Matrix contribution at player`+0x6E68`, earned bonus and computed total power |

The named total-power UI getter's signed16 conversion establishes no server cap
for the packet's uint32 Matrix contribution. The missing shared read/thunk body
at`14008bdf0` is not claimed as successfully decompiled: the direct sender and
writer supply the request proof. The implementation copies no decompiled source.
No native client/DLL or Computer Use was executed by this milestone, which
applies no labels/project exports. Existing and separate in-progress label
records remain resolvable by exact addresses; they are outside this Source change.

Raw native scripts, pseudocode and REA bundle remain in
`artifacts/continuation-20261105/blocker-next-targets/`. This milestone's copied
input manifest, fresh checks, regression TRX/logs and review/source/DLL bindings
are in`artifacts/continuation-20261105/primal-matrix-wire/`. Separate research notes
remain unstaged; the portable record carries this implementation's exact evidence
subset without requiring those draft notes or separate label edits to be published.

## Qualification and remaining implementation

The baseline on`4d939e90` reproduced the absent`0072` decoder and the auction
class's incorrect`06DF` registration. Its compatibility-byte control passed.
All16 new and143 affected cases pass; the affected set includes placeholder
packet shapes, raw/composite readers, diagnostic handlers and reward routing.
The owning WorldServer build has zero warnings/errors. Spec and Standards reviews
have zero findings. No full solution build or full suite ran. The historical
handoff Source-divergence check is retained without rerunning its unchanged failure.

Gameplay still needs qualified node/reward catalogs, Character/account ownership,
max-rank/adjacency/prerequisite rules, checked essence settlement, allocation
persistence, reward/AMP adoption and committed actor publication/recovery. This
milestone performs no full-table/provider/deployment qualification and establishes
no refund/reset/respec policy, accepted retail transcript or request/reply timing.
The broader assessment Goal remains active. Preserve defaults15:1/17:1, no
automatic44 or seven baseline grants, and the native client/CUA stop.
