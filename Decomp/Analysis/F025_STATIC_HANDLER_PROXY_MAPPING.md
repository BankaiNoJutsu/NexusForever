# F-025 aux apply table and handler proxy mapping

2 November 2026. **Apply cases mapped for all six aux opcodes; emission remains
blocked.** This pass resolves indexed dispatch for
`0x0889/0x08CC/0x08F4/0x0939/0x093D/0x093E` and additional forwarding structure.
It does not establish server producers or retail emit timing.
Computer Use and native client launch remain stopped.

## Input and bounded method

REA `inspect_pe_imports` again binds the archived build16042 PE to SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`,
Evidence `ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`.
LLVM22.1.6 identity and PE exception-chain grouping follow the existing
[static input/method record](NATIVE_STATIC_CONTRACT_COMMUNICATOR_CROSSCHECK.md).
Neither tool executes the client. Ghidra configuration and runtime capture were
not retried. The old checkout and binary were read-only.

The new scan tests every eight-byte-aligned 96-byte `.rdata` window for the four
same-index pointers shared by the known diagnostic, console, options and Fortune
nodes: slot0=`140001e50`, slots3/4=`140001b70`, slot8=`140001ba0`.
It finds15 windows, all with12 text pointers, including all four positive controls.
This is a structural heuristic, not a complete class/vtable inventory: overrides,
different bases, table starts, construction and list ownership remain separate.
In particular, the known entity table `140b66880` differs at slot8 and is outside
this four-slot heuristic. Its `+0x58` cell `140b668d8` independently points to
`Entity_DispatchHighRangeMessage` (`1403ec6a0`).
An earlier broad return-one-followed-by-code scan found168 candidates; its
adjacent pointer pairs alone do not establish handler slots.

Selected raw-byte hashes, disassembly commands/outputs, exception groups and
candidate records are retained under
`artifacts/continuation-20261102/f025-apply/`. `inspect-tables.py` reproduces the
structural scan and selected ranges. Leaf helpers without exception entries are
explicitly128-byte context selections; adjacent code is not attributed to them.

## Recovered proxy and distinct list

| Anchor | Observed static relationship | Boundary |
| --- | --- | --- |
| Table `140b55f40`, apply cell `140b55f98` | Points to `140054ce0`; its observed instructions load `node+0x48`, return1 if null, otherwise tail-dispatch delegate vtable slot `+0x58`, preserving the other argument registers | Delegate type and opcode semantics are unknown |
| Table filter cell `140b55f90` -> `140054cc0` | Same forwarding shape through `node+0x48`, with delegate slot `+0x50` | No filter policy recovered |
| `14003e380`, construction at `14003e3b3–14003e3f9` | Allocates0x50 bytes, installs `140b55f40`, initializes node links and delegate slot | Construction proves the table start used here; it does not prove WorldSocket ownership |
| `14003e402–14003e445` | Retains the supplied delegate in `node+0x48` and passes the wrapper to `140048100` | Delegate is an explicit caller argument |
| `140048100` | Splices `node+0x18` backlink / `node+0x20` next through **owner+0x20**, conditioned on owner+0x18 | This is not an observed write to native WorldSocket+0x15b0 |
| `140054b70` | Reinstalls the proxy table during cleanup, releases delegate and unlinks node | Cleanup corroborates link/ownership fields; it is not a constructor |

Seven selected direct-call sites to `14003e380` were checked in their containing
exception groups: `140023db1`, `140023e3c`, `14003830d`, `140045c04`,
`14004652e`, `1400476e8`, `140047783`. Their observed setup includes constructed
delegates and connection/state owners. This does not exhaust indirect callers,
prove all possible delegate types, or connect this list to the native socket
chain. The earlier assertion that only the four persistent socket nodes were
known must not be broadened into a claim that every message-handler node is known.

## Direct dispatch candidates separated from aux

Three additional nontrivial direct table targets have connection/state checks
and opcode dispatch. Their accepted opcode comparisons include cumulative
subtractions; a search only for literal aux values would not classify these.

| Table / target | Observed dispatch boundary |
| --- | --- |
| `140b55380` -> `140020ea0` | Includes the existing PTRCharacterCopyQueued event branch. The inspected opcode branches culminate at0x0862, below the six aux opcodes. The old single-event label describes one branch, not the whole dispatcher. |
| `140b55770` -> `1400370d0` | Connection-specific dispatch branches end at0x063D; other paths perform state/error handling. |
| `140b55940` -> `140045a70` | Connection/state dispatch branches end at0x07A1; other paths perform state/error handling. |
| Fast path `140452670` | The unsigned guard at `14045267d–140452687` bounds dispatch to0x0981–0x0985; it returns1 for the aux inputs. |
| Known Fortune `140b690f0` -> `1404d60f0` | Direct cases are0x03CF–0x03D2. Fallback at `1404d618f–1404d619b` calls a nonnull `node+0x60` delegate's `+0x58` slot. This node cannot be excluded from aux paths merely from its direct cases. |

These are bounded dispatcher observations, not proof that lower-opcode callees
have no unrelated behavior. Purecall/base-only and return-one table candidates
do not establish aux consumers either.

## Indexed aux cases recovered

The Fortune fallback led to setter `1404d6210`: it retains its second argument
at node+0x60 and third at node+0x58. Its selected callers at
`1403f0da7/1403f0dc8` lie in the known entity dispatcher, temporarily setting and
clearing a delegate around an apply call; `1403fa301` supplies an entity owner
and its connection field. This is an additional relationship, not proof that
every aux opcode passes through Fortune.

At `1403ec6a0`, the session pointer at entity+0x7928 must be present and the
incoming channel must match session+0x98. `1403ec6eb–1403ec719` bounds the unsigned
index `(opcode-0x61)` to0x911 and loads a DWORD RVA from `1403f1344+index*4`.
Reading those slots resolves all six cases below. Their payload+0 word is passed
to the existing `Entity_ResolveById` helper `1403d90d0` (inside the helper for0889).
The previous reader-only blocker is superseded for these apply edges.

| Opcode | Table slot -> case | Observed consumer facts |
| --- | --- | --- |
| 0889 | `1403f33e4` -> `1403ede04` -> `1403dede0` | Payload+4/+8 replace entity+0x24/+0x28. Changed bit0x100 triggers additional state clearing; other dirty/selected-entity paths follow. Bit names and producer timing remain unqualified. |
| 08CC | `1403f34f0` -> `1403edadd` | Entity lookup, then payload+8 wide-string pointer -> `14045b030`. That helper copies name text and references the exact `UnitNameChanged` event string at `140afb7c0`; full naming/fallback policy and server producer remain separate. |
| 08F4 | `1403f3590` -> `1403ed7a3` | Sign-extended payload+4 selector is compared with24; accepted native reader values0..23 write payload+8 to entity+0x510+4*selector. Selector6/nonzero has an additional selected-entity path. Selector enum and server behavior remain unknown; do not copy native invalid-input behavior. |
| 0939 | `1403f36a4` -> `1403ed8ff` | Nonzero payload+8 selects a lookup through `1404835c0`; a successful row supplies a localized text ID at+8, otherwise text comes from payload+0x10. Both feed `14045b030`; payload+4 is also sent to `14045b790`. Table identity and the14-bit field's semantic role remain open. |
| 093D | `1403f36b4` -> `1403ed56a` | Payload+4 selector has an unsigned upper bound26. Payload+8 writes entity+0xBE8+8*selector, and raw payload+0xC is consumed as float32 at entity+0xBEC+8*selector. The reader transports both as32 raw bits; do not silently change the existing UInt32 wire model or infer the selector enum. |
| 093E | `1403f36b8` -> `1403ed98f` | Skips the entity equal to dispatcher+0x78; otherwise payload+4/+8 go to `14045bcd0`. Its observed body stores DWORD/QWORD at entity+0x124/+0x128, conditionally clears+0x130 and marks dirty state. Gameplay meaning remains open. |

The six selected switch-case windows are not standalone functions; their
containing exception group and table-byte bindings are retained separately.
Readers are checked against the same pinned PE, including the raw32-bit reads
at `140097690`. No packet field was renamed and no production emitter,
entitlement default/grant or Source behavior changed.

## Remaining evidence

The apply-table requirement is now mapped. Next establish the unresolved field
enums/table identity, producer eligibility and ordering with independent table,
packet or accepted runtime evidence before changing server behavior. Reuse the
known entity table/dispatcher; do not repeat reader-only or literal-opcode
searches for these six apply owners. Preserve the unproved bridge between the
proxy registry's owner+0x20 and native WorldSocket+0x15b0. The Fortune delegate
remains a possible forwarding path and must not be dismissed as Fortune-only.
The rejected viewport/grid and unavailable-provider searches remain excluded.

Validation is static: pinned input/tool hashes,15 structural candidates with
known-node controls, six exact table slots/case ranges, selected observed exception
groups, proxy instruction bytes and source unchanged from the11,634-test
checkpoint `14ea07f0`. That passing
source suite is not a test of these client-derived semantics. No new full suite
is needed for this analysis/label-only change; native and retail qualification
remain open.
