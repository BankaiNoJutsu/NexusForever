# Tracked marker identity and station candidate lifecycle - 5 November 2026

Subsequent Source result: [the full-width marker correction](TRACKED_MARKER_IDENTITY.md)
now implements the packet/helper fix supported by this research, with its own
baseline failures,118 affected passes, build and reviews. Source descriptions
below retain the research checkpoint; producer/timing and trailing-value gates
remain open.

This pass corrects a tracked-marker field interpretation and maps when the
client's crafting service candidates are cleared and replaced. It uses the
pinned build16042 client, read-only Ghidra/LLVM and independently preserved
authored UI. No production Source behavior changed and the client was not run.

## Tracked marker: the first ID selects TrackingSlot

The old claimed chain, `TrackedUnitId -> trailing 15-bit TrackingSlotId -> UI`,
is not the observed path. The incoming update's **first 32-bit word** goes
unchanged to `MapTrackedUnitUpdate(id, position)`. The authored ZoneMap handler
passes that ID to the global `GetMapTrackedUnitData(id)` and replaces its
overlay under the same key. The disable handler removes that same key.
[Independent authored UI and version limits](TRACKED_UNIT_UI_CORROBORATION_2026_11_05.md).

The exact native connection is now established:

1. `14050b520` registers the table beginning at `140c59af0` with count `0x44`
   through `1400e9c20`. The helper creates closures and inserts each name into
   the common Lua environment at state `+0x78`. This block includes `Print`
   and `GetUnit`, rather than introducing a `GameLib` namespace for these names.
2. `GetMapTrackedUnitData` is entry 67, the last of those 68 entries: string
   `140b11338`, registration pair `140c59f20`, function `140511c80`. Thus the
   unqualified global in the authored UI resolves to this exact callback.
   The earlier `Lua_GameLib_...` label did not establish the namespace.
3. At `140511c8a..140511c9e`, instructions parse argument 1 and pass it directly
   to `140242900`. That lookup names `TrackingSlot` and uses the same database
   instance initialized by `1402426a0`, whose loader path is
   `DB\\TrackingSlot.tbl`. There is no intermediary tracked-unit-to-slot lookup
   in this getter.
4. The getter's label lookup uses the selected row's DWORD `+4`, independently
   visible at `140511d44`. Its icon uses the row's string reference at `+8`.
   It does not need a PublicEventObjective row to display the label/icon.

An independent public-event initialization path supplies the other table edge.
The `TrackingSlot` block inside `140497390` enumerates actual rows. For a
nonzero row `+0x10` (the mapped `PublicEventObjectiveId` field), it finds the
associated objective, appends row `+0` to that objective's marker list and
inserts `row.Id -> row.PublicEventObjectiveId` into the manager map at `+0xA8`.
The marker update/removal helpers `140496ca0/140496e20` use that association.
This corroborates a table row identity for the incoming first word. It does
not justify inventing a separate entity ID allocator or selecting an arbitrary
slot when several rows share an objective.

| Update wire component | Evidence-backed interpretation |
| --- | --- |
| First uint32 | `TrackingSlot` row ID and UI marker key |
| Three float32 words | Marker position passed to the event |
| Final 15 bits | Neutral raw value; observed cache write, semantic name unresolved |

The update helper `1403f4170` stores that final decoded word at `+0x10` as the
value in the context map at `+0x7F30`, keyed by the first word. The getter above
does not consult this map. The analyzed-instruction displacement scan found
only constructor/destructor and update/disable references to `0x7F30`; this
bounded negative does not exclude generic or indirect cache consumers. The
15-bit width alone is not evidence for a World ID or another replacement name.

The existing [packet model](../../Source/NexusForever.Network.World/Message/Model/PublicEvent/ServerMapTrackedUnitUpdate.cs),
its XML comments and historical naming tests still assign `TrackingSlotId` to
the final field. This note supersedes that semantic assertion; the existing
wire widths and ordering are preserved. A bounded follow-up can correct the
model to a first-word slot ID and neutral trailing value. Production cadence,
recipient selection, disable lifetime, initial replay and the trailing value
remain open. No emitter or Source rename was added here.

The [one-way lookup helper](../../Source/NexusForever.Game/PublicEvent/TrackingSlotHelper.cs)
also masks an incoming slot ID with `0x7FFF`, matching the old field-width
assumption. The newly mapped first word is 32 bits, and its native getter has
no observed 15-bit mask. Correcting that helper's width assumption is a separate
bounded follow-up; the existing table corpus and slot-selection policy must
not be inferred from the wire width alone.

## Station candidates are rebuilt on the observed scene pass

The [previous pass](DEEPER_ROADMAP_CONTRACTS_2026_11_05.md) named service keys
`2C/4F/57` but left cache clearing and competing candidates open. The lifecycle
trace now supplies a reset before the observed population loop:

- At `1403e8d21`, the scene routine takes context `+0x66C0` and calls
  `140008410`. The helper frees the tree's nodes, resets the sentinel links,
  clears its root and sets its element count to zero. This is a tree clear,
  not just an overwrite of one station ID.
- Later on the same observed path, the loop at `1403e8fa0` calls
  `14045a640` for the selected entities. The already mapped service flags and
  range checks then populate qualifying station candidates.
- `1404824c0` inserts a missing service key or finds its existing node, then
  unconditionally stores the supplied unit ID at node `+0x24`. It contains no
  distance comparison or nearest-candidate arbitration. On this setter path,
  a later qualifying write for the same key replaces the earlier candidate.

This resolves cache reset/replacement on that scene path. It does not prove
that the path is entered on every tick, establish a stable entity traversal
order, or make an out-of-range cached result authoritative on the server.
Do not implement nearest-station selection from the previous range check alone.

An adjacent-function guess was rejected: `140482450` checks tag-owner state;
it is not the station-cache clear helper. The reset finding rests on the
scene's explicit `+0x66C0` call and the clear helper's body.

## Legacy neighborhood requests: a narrower investigation boundary

The [housing source comparison](HOUSING_NEIGHBORHOOD_WEB_CORROBORATION_2026_11_05.md)
recovers the original API names and compares authored snapshots through
build15736. Old neighborhood callbacks retained in RecallFrame lack their
corresponding handler in the sampled files, while the newer Community browser
has an explicit request/getter/event/visit chain. These snapshots remain older
than the pinned final client.

All eight sampled old API names have no exact NUL-terminated ASCII or UTF16LE
literal in the pinned PE. In contrast, `RequestRandomCommunityList` and
`GetRandomCommunityList` have exact name/function registration pairs at
`140b74870 -> 140736500` and `140b74920 -> 140733960`. The former sends `052C`
with its selected zero first payload byte after local throttle/player guards.
This is a positive control for a distinct Community request, not a `0506`
producer witness. Missing literals do not prove every possible legacy entry
point was removed. Keep `0501/0506` triggers open rather than deriving them
from the current Community browser or a stale event subscription.

## Evidence and remaining gates

REA identity:
`ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`;
client SHA256:
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
REA native-provider unavailability was not retried. The existing standalone
Ghidra12.0.4/JDK21 copy was opened read-only with analysis disabled; selected
LLVM instructions independently resolve omitted arguments and table offsets.

The [portable lifecycle record](coverage/tracked-station-lifecycle-20261105.json)
binds selected ranges, call sites, strings and registration pairs to this PE
and its copied Ghidra program. Raw logs, LLVM text and the verifier remain in
`artifacts/continuation-20261105/lifecycle-unblock/`. The linked web notes retain
their immutable revisions, byte hashes and bounded search results.
All 22 selected code ranges and 18 additional anchors match the pinned PE and
the read-only Ghidra program. These identity checks do not qualify runtime
behavior. The eight exact-name negative searches are retained separately.

Mapped identity and cache lifecycle can progress. The five consolidated runtime
producer gates remain open, including tracked marker emission and legacy
neighborhood publication. No application tests were run for this research-only
pass and no native gameplay or retail acceptance is claimed.
