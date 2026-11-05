# Map-tracked units: authored UI corroboration

5 November 2026. Research-only follow-up for `0849/0848` and
`GetMapTrackedUnitData`, using original client-authored addons/API material
preserved in public mirrors. No Source changed, no client launched and no
shared REA/Ghidra session was used.

## Result and resolved key identity

The authored update event carries a display key and position. Update replaces
the overlay under that key; disable removes it. The same update key is passed
to an unqualified global metadata getter. No authored ID allocator or explicit
TrackingSlot argument was found.

The parent pinned-artifact follow-up below resolves the naming/lookup question
exposed by this source: the actual legacy/global getter binds to `140511c80`
and directly reads TrackingSlot rows. Independent objective-cache initialization
also keys its association by those row IDs. The first wire32 field is therefore
the TrackingSlot ID. The trailing 15-bit field is separately cached and remains
neutral; its older TrackingSlotId label is superseded. No arbitrary UnitId or
new ID allocation scheme should be inferred from the authored parameter name.

## Authored event contract

At the inspected 2017 snapshot, ZoneMap registers both tracked-unit events.
`OnMapTrackedUnitUpdate(idTrackedUnit, tPos)` calls global
`GetMapTrackedUnitData(idTrackedUnit)`. Missing metadata aborts that update.
Otherwise, it uses `label` and `iconPath`, removes the old TrackedUnit overlay
with that ID, and adds its replacement at `tPos`, retaining the ID as user data.
`OnMapTrackedUnitDisable(idTrackedUnit)` removes overlays with the same ID.
[Authored handlers, lines 3762-3781](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/ZoneMap/ZoneMap.lua#L3762).

The initial document setup registers the events at 683-684. The inspected
world-change handler at 1231-1239 updates current-zone metadata and calls
`SetControls`; it contains no explicit tracked-unit replay, ID allocation or
disable request. Only the update handler calls the tracked metadata getter in
this file. These are source boundaries, not proof about native widget/cache
initialization or how the retail server schedules updates.
[Event registration](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/ZoneMap/ZoneMap.lua#L683),
[world-change handler](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/ZoneMap/ZoneMap.lua#L1231).

The older NASADocs Carbine snapshot has the same two-argument update and
single-argument disable contract, the same global getter argument and overlay
replacement/removal key. It corroborates historical UI continuity, not
final-build equality or server producer timing.
[2015 authored handlers, lines 3390-3409](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/ZoneMap/ZoneMap.lua#L3390).

## Historical API documentation cannot fill the missing contract

The extracted original Global API help marks `GetMapTrackedUnitData` deprecated
and supplies no useful parameter/return contract there. The generated event
data marks MapTrackedUnitUpdate/Disable incomplete. Values7003/7004 in that
XML are documentation item IDs; they are not wire opcodes or tracked-unit IDs.
This incomplete documentation cannot establish a missing initial snapshot or
map-transition cleanup rule.
[Original Global API help](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Help%20File%20Extracted/LuaAPI/Global/Global.htm#GetMapTrackedUnitData),
[generated event metadata](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/LuaDocData/000003.xml).

## Native targets that can reconcile the UI key

The following table records the earlier investigation checkpoint. Its alias,
row-key and association questions are answered by the pinned follow-up below;
the native lifecycle/initial replay questions remain open.

The current [native labels](function_labels.csv) and
[tracked-unit roadmap](ENTITY_AUX_DECODE_ROADMAP.md) already identify the update,
disable, event dispatcher, getter and TrackingSlot loader. Their known client
consumer status must not be regressed to reader-only.

| Target | Existing/native-preview observation | Specific next question |
| --- | --- | --- |
| Update `1403f4170` | First wire word is sent to the update event; tail at payload+0x10 is cached under that key | What is the actual cached value's read-side use? |
| Event `140430f80` | Builds position fields and dispatches the two-argument event | Verify first argument and global getter namespace against the same pinned build |
| Getter `140511c80` | Public native excerpt parses arg1, directly calls `140242900`, then returns label/iconPath | Is the authored global name bound here, and is arg1 a table-row key? |
| Row lookup `140242900` | Public excerpt calls TrackingSlot loader `1402426a0` and row store `140c653c0`; no entity-cache traversal appears there | Confirm this direct table ownership in the pinned artifact |
| Marker update/remove `140496ca0/140496e20` | Public excerpts use another mapping store at `140c65988` before positional marker work | Does that owner/map mapping explain the cached trailing field? |
| Disable `1403f4200` | Removes cached state, then dispatches the first word as the disable key | Find independent world/session teardown and initial-cache replay semantics |

The public native previews are from
`ChargeIn/WildstarClientIDA@5a6e5549050d0be88e4389dd890f4fe32062c54a`,
which supplies no inspected original executable hash/build binding. Their
addresses are trace leads; the pinned parent pass owns final corroboration.
[Update excerpt](https://github.com/ChargeIn/WildstarClientIDA/blob/5a6e5549050d0be88e4389dd890f4fe32062c54a/IDA/functions/sub_1403F4170.c),
[getter excerpt](https://github.com/ChargeIn/WildstarClientIDA/blob/5a6e5549050d0be88e4389dd890f4fe32062c54a/IDA/functions/sub_140511C80.c),
[row-lookup excerpt](https://github.com/ChargeIn/WildstarClientIDA/blob/5a6e5549050d0be88e4389dd890f4fe32062c54a/IDA/functions/sub_140242900.c).

Two distinctions mattered at that checkpoint:

- The UI identity alone did not prove its namespace. The later native trace
  establishes a TrackingSlot row ID, rather than a newly allocated UnitId.
- The authored call is a **global** API. The older
  `Lua_GameLib_GetMapTrackedUnitData` analyst label is not proof of a GameLib
  alias; the later global registration resolves the actual callable directly.

## What can and cannot narrow the producer gate

| Question | Supported conclusion | Still required |
| --- | --- | --- |
| Repeated updates | UI replaces the prior marker under the same TrackingSlot row ID | Correct row selection, duplicate-source and update timing rules |
| Disable | UI removes the marker by that key | Authoritative end conditions and map/session timing |
| Metadata | The registered global getter directly consumes the TrackingSlot row ID | Trailing-field meaning and authoritative metadata/row availability |
| TrackingSlot selection | Lua does not select a row from a PublicEventObjective | Actual native/table/capture proof of the selection rule |
| Initial load | Event handlers are registered; no explicit replay API was found in this file | Native cache/widget initialization and accepted startup packet order |
| Cleanup/reuse | Explicit disable removes display state | Whether server IDs may be reused and who clears state on teardown |

The fork's [TrackingSlotEntry](../../Source/NexusForever.GameTable/Model/TrackingSlotEntry.cs)
declares an ID, label, icon and public-event objective. That schema does not
select between multiple rows sharing one objective. Preserve the existing
one-way lookup guard until the wire key and scope field have been resolved.
No quest/public-event emitter is justified merely by this authored display code.

## Source provenance and byte identities

The 2017 UI source was inspected at mirror revision
`aa608d0b837ff8037eb8509123d8e3ed4efcb9e4`. ZoneMap's last change is
`d37c0399f1d481faf23c027cced44b05a07ab3da`, 22 March 2017, whose message
claims v1.7.0.15134 API15 and copied PTR changes. The file identifies NCsoft
copyright; the mirror does not authenticate build16042 source equality.
NASADocs is pinned to its 19 June 2015 snapshot. The two copies differ in bytes.

| File | Bytes | SHA256 of GitHub-decoded bytes |
| --- | ---: | --- |
| 2017 ZoneMap.lua | 184452 | `84B63D71F4DCD2396F02AD126BB0F1EC33886C1F807B3CE56643075DDD90073C` |
| 2015 ZoneMap.lua | 174413 | `12F92113A8CD8F853CD891DFF40A208A5EA6B55A9878D967CCD2BCA026952110` |
| Original Global.htm | 269074 | `0556BFF27F746EB984C0F73AC4C7B8FDBE59008A58CB1D7D195D0449E1512D24` |
| Event metadata000003.xml | 154111 | `07EEC6655824FDBD0474ADDD111E3712770CF03A85CB929D0969D6EFDA02F3B6` |

Files were read and hashed in memory, without adding client source to this
checkout. No publisher-authenticated binary, traffic capture or actual table row
corpus was recovered by the web pass.

## Search boundary and verification

Explicit web searches covered the exact event/getter names. Authenticated
GitHub code searches located authored ZoneMap, original generated help and
selected public native excerpts. Searches then followed the specific getter,
row lookup and marker helpers instead of repeating general protocol searches.
The secondary wiki event/API inventories returned by web search were discovery
leads; they were not promoted to primary parameter or lifecycle evidence.

Authored-source checks covered Live ZoneMap, MiniMap, ObjectiveTracker and its
navpoints module. No tracked API call was found in the latter three inspected
files. This is bounded file coverage, not absence from every addon or native
widget. Historical API event metadata is explicitly incomplete.

GitHub revisions, source line anchors, byte identities, local links and
whitespace were checked. No application build/tests were needed for this
research-only note. The remaining work is the native lifecycle and producer
policy described below, not another unchanged generic web search.

## Pinned follow-up: global binding and TrackingSlot key verified

The parent inspected the supported executable with SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
This note reread the retained outputs under
`artifacts/continuation-20261105/lifecycle-unblock/` without using the shared
session. The parent aggregate is
`TRACKED_UNIT_AND_STATION_LIFECYCLE_2026_11_05.md`; its portable instruction/byte
record owns final artifact binding.

### The authored global call reaches the named getter

`14050b520` registers the 68 name/callback pairs beginning at `140c59af0`
through `1400e9c20`. GetMapTrackedUnitData is the final pair, index67, at
`140c59f20`; its name string is at `140b11338` and its callback is `140511c80`.
The registration helper creates a closure for each callback and inserts it
under its name into the common Lua-state environment at `+0x78`.

Thus the unqualified global name used by authored ZoneMap is not merely a
possible alias: its actual registered callback is known. No separate GameLib
namespace alias is needed or asserted. `140511c80` passes argument1 directly to
`140242900`; that lookup's table name and loader `1402426a0` independently
identify TrackingSlot. The getter returns that row's label/icon presentation.

### Independent objective initialization corroborates the row key

`140497390` enumerates TrackingSlot rows. For a nonzero objective field at
row `+0x10`, when the corresponding objective exists, it appends the row's
first word (`Id`) to that objective's associated tracking-slot list. It then
inserts a map entry at owner `+0xA8` using row Id as key and the objective ID as
value. The map-marker update/remove helpers `140496ca0/140496e20` use that
association for their positional marker work.

This is an independent table-to-objective path corroborating the key passed
through `0849`, the event and the global getter. The first wire word is a
TrackingSlot row ID. A public-event objective can have multiple associated
slot rows; the trace does not choose which should represent a given producer.

### The trailing15 bits remain a different field

The payload word at `+0x10` is stored in a different context map at `+0x7F30`,
keyed by the first wire word. No observed getter/table lookup uses that value
as the TrackingSlot ID. It must therefore retain a neutral name until its own
consumer is mapped. Do not replace the older mistake with a guessed world,
instance, entity or scope label.

Retained outputs inspected:

- `tracked-unit-consumers-console.log`: getter, update, disable and event paths.
- `cache-world-helpers-console.log`: direct row lookup and marker association.
- `tracked-registration-initialization-console.log`: global registration call
  and TrackingSlot-to-objective initialization.
- `global-registration-helper-console.log`: named closure insertion into the
  common Lua environment.
- `global-table-candidates.json`: exact table pair and callback identities.

This **supersedes the pending alias/key caveat** in the earlier source-only
checkpoint. No material doubt remains about the first field's table-key
identity within this evidence. Actual startup snapshots, update/disable
cadence, complete map/session teardown, producer row selection and the trailing
field's meaning remain open. The older authored UI/version limits still apply
to those source files; they do not weaken the separate exact-build native proof.
