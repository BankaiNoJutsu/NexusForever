# F-025 aux apply table and handler proxy mapping

Later [scanner name integration](SCIENTIST_SCANBOT_NAME_PUBLICATION.md) connects
the corroborated08CC contract to an existing public rename flow. The mapping
checkpoint below is79568022, before that Source change; its no-emitter statements
record that historical boundary. Other five aux producers remain unqualified.

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
| 0889 | `1403f33e4` -> `1403ede04` -> `1403dede0` | Payload+4/+8 replace entity+0x24/+0x28. In the second word, setting bit `0x100` clears base Health and eleven resource slots. Bit `0x4000` clears a matching selection and prevents selecting that candidate. See the [consumer trace](#5-november-0889-vital-reset-and-target-selection). |
| 08CC | `1403f34f0` -> `1403edadd` | Entity lookup, then payload+8 wide-string pointer -> `14045b030`. That helper copies name text and references the exact `UnitNameChanged` event string at `140afb7c0`; full naming/fallback policy and server producer remain separate. |
| 08F4 | `1403f3590` -> `1403ed7a3` | Five-bit selector values 0 through 23 write payload+8 to entity+0x510+4*selector. Selector 6 is checked by target validation and a nonzero update can clear the current selection. See the [consumer trace](#5-november-08f4-target-validation-and-selection). |
| 0939 | `1403f36a4` -> `1403ed8ff` | `ServerUnitNameAndTitleChange` resolves `UnitId`, uses the 18-bit `Creature2Id` for localized name lookup with a wide `Name` fallback, then applies the 14-bit `TitleId`. [Field evidence](#3-november-0939-name-and-title-fields) and [source reconciliation](#5-november-0939-wire-model). |
| 093D | `1403f36b4` -> `1403ed56a` | Payload+4 selector has an unsigned upper bound26. Payload+8 writes entity+0xBE8+8*selector, and raw payload+0xC is consumed as float32 at entity+0xBEC+8*selector. The reader transports both as32 raw bits; do not silently change the existing UInt32 wire model or infer the selector enum. |
| 093E | `1403f36b8` -> `1403ed98f` | Resolves `UnitId` and skips the local entity. Payload+4 is `UnitTagOwner` and payload+8 is `GroupTagOwner`. Both go to the setter shared with entity creation. See the [tag-owner trace](#5-november-093e-tag-owner-update). |

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

## 3 November: 0939 name and title fields

The matching build16042 binary and installed LLVM identities above are unchanged.
Two independent native data paths now corroborate upstream514's field names:
the14-bit argument selects CharacterTitle text, and the18-bit argument selects
a cached Creature2 row's localized name. This resolves these two field identities;
it does not establish a new server producer or retail emission order. The current
serializer retains its compatibility fields and wire widths. The existing active
title producer uses ServerTitleSet and is not replaced by this observation.

For the14-bit field, the mapped0939 case passes payload+4 through14045b790 to
14045b6f0. When the entity's name at+10 is nonempty, the latter stores the ID at
entity+168, calls1403ca8b0 and writes display text at+170 (pointer+178).
The formatter passes the ID to1401f1860 and the resulting row+8 to localized-text
helper14034bdd0. The lookup uses140c653d0 and initializer1401f1600, whose UTF-16
literals at140a60ac8/140a60a48 name CharacterTitle and DB\CharacterTitle.tbl.
The independent Unit:GetTitle binding140c5afa0 ->140652a40 reads entity+178.
The empty-name branch clears display text without storing the ID at+168; do not
infer arbitrary title-before-name ordering from the nonempty branch.

For the18-bit field, the already mapped case calls1404835c0 on140c65918, reads
cached row+8 and passes it to14034bdd0 before14045b030. The selected lookup uses
the cache's hash index at+18. Initialization140482b70 constructs140482c00,
initializes through140482ea0 and installs the result at140c65918. The initializer
tail-calls140485790, which enumerates the table at140c63d10 after1401f6b00.
The latter's UTF-16 literals140a7c5f0/140a7c5c8 are Creature2 and DB\Creature2.tbl.
The population's virtual+8 call at140485b9b resolves through the constructor's
vtable140b67130 to1404836c0. That routine builds the+18 hash index and renders
rows through virtual+10 ->140483d20. The renderer passes the original row ID to
140485490 ->1404852b0;1404852b0 rereads Creature2 through1401f6d60, copies its ID
to cached+0 and native row+10 to cached+8. The index stores that cached row keyed
by its ID at140483ae4–140483aeb. These cache-construction edges distinguish the
lookup from a direct table accessor and establish the Creature2 ID/name path.
Zero or an unsuccessful cached lookup takes the packet's existing wide-text
fallback; this is an observed consumer branch, not a server fallback policy.

Raw selected ranges, commands, byte/output hashes and table strings are retained
in `artifacts/continuation-20261102/title-field-corroboration/` and
`artifacts/continuation-20261103/entity-name-title-evidence/`. The latter's
verification.json checks69 unique selected ranges against the pinned PE and
retained output hashes. Its77 displacement candidates are a selection aid;
only the decoded instructions cited above are accepted references. This is not
an exhaustive cache/type/call-graph inventory. The formatter's entity+34 meaning,
full locale/gender formatting, cache eligibility and complete producer/ordering
requirements remain unresolved. No client launch, native UI observation,
production emitter, source behavior or retail-matrix promotion occurred.

## 5 November: 0939 wire model

The build 16042 reader at `0x140097EE0` reads the unit ID, the 14-bit
CharacterTitle ID, the 18-bit Creature2 ID, and the wide name in that order.
The apply case uses the wide name if the Creature2 ID is zero or the cached
lookup misses. It calls the name setter before the title setter on both paths.

The model and opcode symbol now match upstream head `c684d1fd`.
The `0x0939` wire layout and the empty-string default are preserved.
[Upstream #514](https://github.com/NexusForever/NexusForever/pull/514)

The recheck binds the retained lookup ranges and ten reader, apply and
table-string anchors to the pinned PE and REA record. The owning build
passed without warnings or errors and all 169 scoped tests passed, including
opcode registration, maximum-width IDs, and wide-name serialization. See
`artifacts/continuation-20261105/f025-0939-model/` for local results.

Production emission remains gated pending evidence for sender eligibility and
order. The existing `ServerTitleSet` path retains its own behavior and tests.

## 5 November: 093D capacity consumers

The matching build 16042 constructor at `0x140454380` installs the table
`0x140b66fd0` and clears 216 bytes beginning at entity `+0xBE8`.
That equals 27 pairs at the eight-byte stride used by the `0x093D` apply
case. The unsigned compare against 26 accepts selectors 0 through 26.
Two paths read the second word of selected pairs.

| Selector | Entity field | Observed consumer |
| --- | --- | --- |
| 21 | `+0xC94` | The capacity table at `0x1404669FC` routes Vital 0x14 (Interrupt Armor) to `0x1404668D5`. The branch truncates the base float at `+0x724`. For a base other than -1 it adds the truncated float at `+0xC94`. The separate entity update switch at `0x1403DA044` uses the same operands and sum to clamp the value stored at `+0x4B0`. |
| 22 | `+0xC9C` | Vital 0x15 (Absorption) takes the capacity branch at `0x140466908`. It calls virtual slot `+0x90`. On the constructor's base table that slot points to `0x140466510`, which truncates the float at `+0xC9C`. The current-value branch calls slot `+0x88`, whose base target reads `+0x4C`. |

The [19 verified instruction and data anchors](coverage/f025-093d-consumers-20261105.json)
bind the case table, stores, constructor and consumers to the same PE and
REA identity record. The exact-displacement scan also found unrelated objects
and stack uses. A bounded Lua-name check found capacity/current-value strings
without a direct binding witness, so the mappings above rest on the decoded
vital tables and base entity paths.

The first word, 25 other selectors, possible virtual overrides, server
producer and packet order remain open. The serializer retains raw words
and the five-bit selector. All 105 scoped wire and emission controls passed
using the unchanged binaries. Static inspection did not execute the client.

## 5 November: 08F4 target validation and selection

The build 16042 entity constructor zeroes `0x60` bytes at entity `+0x510`
at `0x140454FA3`, independently confirming storage for 24 DWORDs. The reader
at `0x140097657` reads the five-bit selector into payload `+4`. The apply case
sign-extends that field and uses a signed upper-bound comparison against 24,
so decoded selectors 24 through 31 skip the store.

- **Target validation.** The spell validation parent resolves a target ID
  through the same `0x1403D90D0` entity lookup as the packet apply case. It
  passes that pointer at `0x1403AEC8B` to `0x1403B0880`, which forwards it at
  `0x1403B0D78` to `0x1403B2240`. The callee tests DWORD `+0x528`, the exact
  storage for selector 6, and returns `0x0060` for any nonzero value on this
  path. The existing source enum names this result `CastResult.TargetInvulnerable`.
- **Current selection.** The apply case stores the value first. If the
  selector is 6, the value is nonzero and the updated entity matches the
  current selection ID, it passes ID zero to `0x14055B0E0`. The selection
  routine writes the new ID to `+0x108` and takes the `ClientEntitySelect`
  (`0x0185`) send path with a zero ID when clearing the selection.

The [22 verified byte anchors](coverage/f025-08f4-consumers-20261105.json)
bind the lookup, argument forwarding, branches and storage to the same PE and
REA evidence record. The other 23 selectors, selector 6's exact server-side
value domain, its producer and packet ordering still need independent evidence.
The packet model remains neutral and production emission remains gated. All
105 scoped wire, naming and emission controls passed with the existing
binaries. The client was inspected statically.

## 5 November: 0889 vital reset and target selection

The apply helper compares the old and new second words with XOR before
testing `0x100`. A clear-to-set transition writes `1` to entity `+0x1348`,
zeroes `+0x40`, and clears `0x2C` bytes beginning at `+0x21C`. A set-to-clear
transition writes `0` to `+0x1348`. An unchanged bit skips these writes.
The independent initializer at `0x14045BA00` uses the same reset when the
bit is set. The routine at `0x14046CDC0` compares `+0x1348` with its previous
value at `+0x134C` before branching on state `1`.

The current-vital switch at `0x140466520` indexes the table at `0x140466740`
with `(vitalId - 1)`. In the existing `Vital` enum, ID `0x01` is Health.
That branch calls slot `+0x40` of the entity vtable. The constructor's base
table binds this slot to `0x14019F800`, which returns the DWORD at entity
`+0x40`. Eleven other switch entries load the float fields cleared by the
reset. IDs `0x05` through `0x0B` read `+0x21C` through `+0x234` (Resource0
through Resource6), `0x10` reads `+0x238` (Resource7), and `0x1B` through
`0x1D` read `+0x23C` through `+0x244` (Resource8 through Resource10).

Independently of the `0x100` transition, the apply helper tests the new second
word for `0x4000`. If the updated entity is the selected target, it calls
`0x14055B0E0` with ID zero. The same selection routine reads the candidate's
`+0x28` and exits on `0x4000` before replacing the selected ID or taking the
`ClientEntitySelect` send path. This establishes the selection behavior of
that bit on the two traced paths.

The first word's bit `0x01` is read by the relationship gate at `0x1403B44E0`.
Its `0x59` branch corresponds to `CastResult.TargetGroup` in source. The
separate `TargetMustBeDead` and `TargetCannotBeDead` branches test `+0x250`
and entity type `0x17`. The relationship read leaves the first word's bit
name open.

The [52 verified byte anchors](coverage/f025-0889-consumers-20261105.json)
bind the dispatcher, transition, vital getters and selection paths to the
same build 16042 PE and REA record. The precise flag definitions, derived
entity overrides, server producer and emission order still need independent
evidence. All 105 scoped wire, naming and emission controls passed with the
existing binaries.

## 5 November: 093E tag owner update

The build 16042 registration at `0x14007501B` binds `0x093E` to the
16-byte reader at `0x140097F70`. The apply case resolves the first DWORD as
the target unit ID. It skips a failed lookup or the local player. The
remaining DWORD and QWORD go to `0x14045BCD0`, which stores them at entity
`+0x124` and `+0x128`. If both are zero, the setter also clears `+0x130`.

The entity-create reader stores the corresponding `UnitTagOwner` and
`GroupTagOwner` at decoded offsets `+0xDC` and `+0xE0`. The creation path at
`0x1404578E0` forwards both fields to that same setter. The two adjacent
name/function rows at `0x140C5B740` confirm how the client uses the values.

- `IsTagged` (`0x1406612C0`) tests for either owner after checking a flag on
  the entity's state component.
- `IsTaggedByMe` (`0x140661360`) returns true for a nonzero `+0x130`. Otherwise
  it checks the unit owner against the local player's unit ID, then the
  nonzero group owner against the local player's group ID at `+0x18A0`.
  The separate `0x0936` group-association handler writes that local group
  field and references `UnitGroupChanged`.

The [24 verified byte anchors](coverage/f025-093e-tag-owner-20261105.json)
tie both packet paths and the named consumers to the same PE and REA record.
Source now names this model `ServerEntityTagOwnerUpdate` with `UnitId`,
`UnitTagOwner`, and `GroupTagOwner`. Its opcode and payload layout match the
reader, including the all-zero owner clear. The exact role of `+0x130`,
server ownership policy, producer, recipients, and send order remain open.
The production emitter is still gated. The owning build passed with zero
warnings or errors and all 151 scoped tests passed, including registration,
full-width owner serialization, clear serialization, and the pre-create guard.

## 5 November: 093B and entity-create objective index

The build 16042 registration at `0x14007504C` selects the shared reader
at `0x14007A040`. It reads a 32-bit unit ID followed by a 32-bit
objective index. The apply case at `0x1403ED9C2` calls `0x1403DC280`.
That helper resolves the unit, skips the local player and unchanged values,
and stores a changed index at entity `+0x35F8`.

Entity creation also supplies that field. The reader table at
`0x140C1EA30` routes types 0, 8, 10, and 32 through `0x140095FB0`.
It reads an 18-bit Creature2 ID followed by an eight-bit checklist index.
The matching apply cases at `0x140457247` and `0x140457669` sign-extend
the index byte into `+0x35F8`. The constructor initializes this field
to `-1`. The check at `0x1404A0057` skips the objective mask test for
that sentinel and otherwise uses the index as the shift amount.

The existing NPC and collectable producers previously wrote zero regardless
of the placement's index or a script override. Both now send the stored
byte, including the Northern Wilds Granok drop-pod fallback's `0xFF`.
The existing creation packet widths and zero defaults are preserved.
The server's `0x093B` model already matches the two-word reader. Dynamic
emission still requires evidence for its server producer and send order.

The [28 verified byte anchors](coverage/f025-093b-objective-index-20261105.json)
bind the registration, readers, both apply paths and the objective mask test
to the pinned PE and REA record. After supplying the missing table stubs,
all four baseline cases reproduced the zero byte. The final owning build
passed without warnings or errors, and all 184 scoped entity-create,
visibility, objective and map-script tests passed. Local disassembly, build
and regression evidence is under `artifacts/continuation-20261105/f025-093b-objectives/`.
Native client and deployed socket behavior remain open.
