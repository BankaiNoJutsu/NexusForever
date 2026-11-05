# Deeper crafting, matching and housing contracts - 5 November 2026

This pass resolves the three previously unnamed crafting station service keys
and independently traces the role-confirmation mask conversion. It also narrows
the legacy housing entry consumer. These are static shipped-client contracts;
no Source behavior, client execution or retail qualification changed.

## Crafting station key names resolved

The exact build16042 Lua registration pairs bind named APIs to three distinct
callbacks. Each callback invokes the same context predicate with a constant:

| Named CraftingLib API | Callback | Service key | Meaning at this boundary |
| --- | --- | --- | --- |
| `IsAtCraftingStation` | `14059b270` | `0x2C` | Ordinary crafting station availability |
| `IsAtEngravingStation` | `14059b2b0` | `0x4F` | Engraving station availability |
| `IsAtMasterCraftsman` | `14059b230` | `0x57` | Master craftsman availability |

The common predicate `1405a4850` searches the context tree at player-manager
`+0x66C8` for an exact integer key. It returns true when that entry's `+0x24`
value is nonzero. Independently, `1403a0d20` looks up the same key and returns
the stored value itself as the station unit ID used by crafting senders.

Two further paths corroborate these roles:

- `CompleteCraft` is bound to `14059bb60`. Its selected completion helpers
  `1405a1df0` and `1405a1ee0` choose `0x4F` for tradeskill22, `0x57` for the
  observed tier-zero/flag4 branch, and `0x2C` otherwise, then resolve the unit
  through `1403a0d20`. The existing selector `1405926a0` has the same branch
  constants. These are client selection rules, not proof that a supplied
  station ID is valid on the server.
- `14045a640` checks per-entity service flags and range before calling the
  cache writer `1404824c0` with the entity's unit ID. LLVM/caller instructions
  bind entity `+0x2574/+0x2580` to `0x2C`, `+0x318C/+0x3198` to `0x57`, and
  `+0x2F4C/+0x2F58` to `0x4F` (flag/range pairs). A nonpositive configured
  range takes a default of 5.0. The shared range helper `1403ad690` accounts
  for entity radii and height offsets; this is not a universal five-unit
  center-distance rule. The scene loop calls this entity update at `1403e8fa7`.

The UI predicate therefore reflects a cached local service candidate. The
[later lifecycle pass](TRACKED_UNIT_AND_STATION_LIFECYCLE_2026_11_05.md)
now maps tree clearing before the observed population loop and same-key
replacement; stable traversal order and server eligibility remain separate.
`1403a0d80` supplies another named behavior: it resolves key `0x2C`,
then dispatches `InvokeCraftingWindow` and an interaction open/close action for
that unit after its additional checks.

The [independent authored UI evidence](CRAFTING_AUX_WEB_CORROBORATION_2026_11_05.md)
uses the ordinary and engraving predicates to control separate station blockers.
Its older source does not establish final-build numeric keys; the exact native
bindings above do. No authored `IsAtMasterCraftsman` consumer was recovered in
that bounded corpus, so its exact native API binding remains the naming witness.

This closes the **service-key naming** gate in F-008. It supports descriptive
constants and diagnostics. It does not establish discovery rolls, complex-craft
settlement, item patch cadence or the `084B/0855` producer. Those auxiliary
opcodes still follow the previously observed dispatcher's default path.

## Matching confirmation: ordinals are converted to bits

The same-build `MatchMakingLib` registration at `14076acb0` assigns Lua role
ordinals Tank=0, Healer=1 and DPS=2. The `Tank` string at `140b4e99c` and `DPS`
at `1409f981c` resolve the two data labels not rendered as strings by the
decompiler. These are ordinal API values, not wire flags.

The `ConfirmRole` string at `140b4e538` has its registration pair at
`140b76370`, pointing to `140769620`. Its instructions at
`140769738..14076974f` check the parser result, skip ordinals outside 0..2,
compute `1 << ordinal`, and OR each selected role into a mask. Thus this
confirmation path maps Tank/Healer/DPS to 1/2/4. The fresh decompile omits the
mask arithmetic; the LLVM instructions, not that incomplete pseudocode, are
the witness for the conversion.

The callback passes the mask to `1405c36e0`. That helper intersects it with
class eligibility (class IDs 1/2/5 use mask 5; IDs 3/4/7 use mask 6; default 0),
then sends `05B2` with the context match type, filtered roles and accepted=1.
The independently named `DeclineRoleCheck` binding at `140b76380` points to
`1407697a0`, which sends the same opcode with roles=0 and accepted=0.

The [authored role UI](MATCHING_ROLE_WEB_CORROBORATION_2026_11_05.md) independently
passes role ordinals to this API. It also separates matching confirmation from
the raid UI's `GroupLib.SetRole*` member-flag flow. The new result corroborates
the C2S role protocol; it does not by itself name incoming `0600`'s three bits.
That consumer chooses localization IDs `72A62..72A68` from low-bit combinations,
but no matching numeric English-text corpus was available. Public `0600`
notification timing and replacement/backfill remain separate integration gates.

A whole-program analyzed-instruction scan for `062A` found its registration
and a non-network return constant; `0634` additionally matched object field
displacements. No direct diagnostic sender was found within that scan. This
does not exclude computed opcodes, missing analysis or other indirect rails.
Both requests remain neutral/log-only.

## Legacy housing entry: do not infer membership from the packet name

The indexed `0501` path calls `1404ba470`. Its observed body reads the decoded
wide-string pointer at payload `+0x18`, frees the previous string in a context
map entry and copies the received string there. It constructs the lookup key
from explicit zero words. It does not consume the packet's other row fields or
dispatch `HousingNeighborhoodRecieved` in this body. This supports the string
field's name-text ownership, not a join/leave or membership transition.

The separate `0506` consumer `1404ba4f0` clears the cached row list, copies each
decoded 48-byte row and duplicates its wide string, then dispatches
`HousingNeighborhoodRecieved`. The observed body does not use the leading
realm field to establish row identity. An empty list still clears the cache and
reaches the event. Server triggers, other field names and publication ordering
remain open. Existing event strings and old debug strings are insufficient
to infer a currently exposed neighborhood request API.

## Evidence, verification and remaining scope

Fresh REA PE identity is
`ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`,
SHA256 `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
The known unavailable REA native provider was not retried. Native inspection
used the existing Ghidra12.0.4/JDK21 project copy read-only with analysis disabled
and LLVM instruction cross-checks. The game executable and STS DLL were not run.

The [portable record](coverage/deeper-roadmap-contracts-20261105.json) binds
selected code ranges, instructions, API strings and registration pairs to the
pinned PE. Raw logs and the independent Ghidra byte verifier are under
`artifacts/continuation-20261105/deeper-roadmap/`. The two linked web notes retain
immutable source revisions, hashes and historical version boundaries.
All 32 selected code ranges and 21 further anchors match the pinned PE and the
read-only Ghidra program. These are byte-identity checks, not gameplay tests.

No application tests were run because this pass changes research documentation
and coverage evidence only. F-008 service-key naming can progress; `084B/0855`,
the five consolidated runtime producer gates, exact Fortune weights, Contract
unclaimed/excess handling and optional STS retail acceptance remain open.
