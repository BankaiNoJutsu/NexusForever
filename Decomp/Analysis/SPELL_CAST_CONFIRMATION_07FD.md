# Server opcode 0x07FD: pending cast acknowledgement

Status: mapped native consumer; compatible root-request reply implemented and
focused regressions passed. Native accepted-cast/telegraph verification remains
pending; general native world-entry confirmation is a separate checkpoint.

## Exact build-16042 chain

Binary SHA256:
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.

1. Registration `1400743a8` in `14006c290` binds opcode `0x07FD`, native object
   size `0x38`, to reader `140094e10`.
2. `Entity_DispatchHighRangeMessage` (`1403ec6a0`) dispatches through DWORD RVAs at
   `1403f1344`, indexed by `opcode - 0x61`. Exact PE slot `1403f31b4` contains
   RVA `3ee16f`. Control `0x0818` resolves to the independently mapped `1403ee403`.
3. Fresh focused inspection of stub `1403ee16f` proves payload forwarding to
   `1405a4e60`. Sibling `0x07FC` maps through `1403ee1a5` to `1405a5010`.
4. `1405a4e60` calls `14055de80` with payload DWORDs 0 and 1. That helper matches
   DWORD 0 against pending context `+0x60`, unlinks that pending context, assigns
   DWORD 1 at `+0x5c`, and indexes it by the assigned server cast ID.
5. The consumer stores DWORD 2 in context `+0x158`, the primary target slot
   distinct from caster `+0x154`, and applies position/yaw and both row lists.
   It then enters accepted state via `14053dd00` and updates cooldown state.
   Client-side interaction setup is conditional on cast method 5 inside this
   shared helper; the acknowledgement itself is not exclusive to that method.

Native pending-context constructor `14053c500` increments a DWORD counter into
`+0x60`; promotion has no zero-token exclusion. Request presence therefore cannot
be inferred from a nonzero token.

## Wire and semantic ownership

| Payload field | Width | Evidence-backed role |
| --- | --- | --- |
| Legacy `Time` | 32 bits | Client-generated pending cast context token |
| `CastingId` | 32 bits | Assigned server cast ID |
| Legacy `CasterId` | 32 bits | Primary target entity ID |
| `Position` | 3 × 32 bits | Accepted cast's position snapshot |
| Legacy `Unknown16` | 32 bits | Float yaw bits |
| Initial-position count/rows | 8 bits / 200 bits per row | Unit ID32, flags8, position96, yaw32, pitch32 |
| Telegraph count/rows | 8 bits / 216 bits per row | Telegraph16, attached unit32, flags8, position96, yaw32, pitch32 |

Native array strides `0x1c`/`0x20` include object padding; they are not wire row
sizes. Empty-list payload size is 30 bytes. Counted reader helpers are
`140094970` and `140094a00`.

Yaw proof retains the complete value flow: payload `+0x18` to cast context
`+0x170`, through `140546e60` and constructor `140549e10` to visual object `+0x70`.
Exact-hash disassembly at `14054c13d` loads it with `MOVSS` and passes it to
`1408fe3d0`/`1408fc950`; `14054b1b0` builds the horizontal rotation matrix from
the results. No angle conversion is introduced; the reply copies existing
`ServerSpellStart` yaw and pose bits.

## Compatible implementation

The existing opcode/class and writable raw fields remain available. Additive
`ContextToken`, `PrimaryTargetId`, and bit-preserving `Yaw` aliases expose the
mapped meaning without changing layout or zero defaults.

`ISpellParameters.HasClientCastRequest` explicitly represents parsed cast-request
presence, including token zero. It defaults false and has a default interface
implementation for old custom parameter implementations. Existing token-bearing
spell, activation, item, and rapid-transport entry points set it; unrelated stance
and server-generated requests do not. Existing `UserInitiatedSpellCast` values
are retained because several real activation/item/transport requests deliberately
use false for their separate gameplay behavior.

Accepted root casts send one owner-session acknowledgement immediately before the
existing visible `ServerSpellStart`. Both use the same position, yaw, and prepared
unit/telegraph snapshot. Observers retain the existing broadcast. Rejected casts
retain `0x07FC` correlation. NPC, loading-player, no-request, server-originated,
and triggered-child casts do not send acknowledgements. Triggered children can
retain numeric tokens for diagnostics without claiming a new pending request.

## Verification and limits

`SpellCastConfirmationTests` adds 12 cases, including actual client-handler to
real `Spell.Cast` propagation, zero token, packet alias/byte compatibility,
owner-only delivery and ordering, rejection, child/server/NPC/loading exclusions,
prepared telegraphs, and unchanged global/personal cooldown timing. The focused
confirmation/request/target/transport/cooldown gate passed 189/189 without build
warnings/errors. Parent owns the next combined solution gate.

External evidence lives under assessment `remaining-goal`: `opcode07fd-*-summary.json`,
`opcode07fd-yaw-callsite.txt`, and `opcode07fd-focused-tests.log`. Five mapped
native labels are retained in `function_labels.csv`; exact helper bodies were
read from the fingerprinted cache before focused stub inspection.
The recovered-project label apply/readback confirms all five addresses and names
in `reward-07fd-label-readback.log`. The full label batch still reports unrelated
historical missing/data addresses and a duplicate; this is a successful selected
label verification, not an all-label clean application.

This proves the static request/consumer contract and local sender behavior. It
does not prove live retail packet timing, every charge-release request lifecycle,
or native rendering under every telegraph mode. Triggered-child confirmations
remain excluded; no broad cast-state rewrite is implied.
