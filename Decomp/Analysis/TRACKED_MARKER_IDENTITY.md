# Map-tracked marker identity correction

5 November 2026. The update/disable packet contracts and one-way metadata lookup
now preserve the actual full-width TrackingSlot row ID. This corrects the prior
misassignment of the final15 bits as the slot ID; it adds no marker emitter.
The [portable milestone record](coverage/tracked-marker-identity-20261105.json)
binds the native evidence, Source/DLLs, failing baseline, final scoped gate and
two Source reviews.

## Implemented boundary

| Packet | Fields in unchanged wire order |
| --- | --- |
| S2C0849 `ServerMapTrackedUnitUpdate` | `TrackingSlotId` uint32, Position three float32, neutral `WireUInt15`15 bits |
| S2C0848 `ServerMapTrackedUnitDisable` | `TrackingSlotId` uint32 |

Both packets use the same table-row/UI marker identity. The global metadata
getter directly selects that TrackingSlot row's label/icon; it does not use the
trailing field or require an objective row to display them. The trailing value
is cached separately and its semantic meaning remains unresolved.

`TrackingSlotHelper.TryGetEntry` now passes the complete ID to the existing table
lookup. Previously `0x19234` aliased row`0x1234`, and missing high IDs could
resolve unrelated low rows. Null manager, missing table, zero-ID handling and
the one-way objective association remain compatible. No objective-to-slot
selector was introduced: several slots can share an objective.

Source callers must migrate the former leading `TrackedUnitId` property to
`TrackingSlotId`, and the former trailing `TrackingSlotId` to `WireUInt15`.
The disable property follows the same leading-key correction. All repository
callers are tests; the production search found no emitter or helper caller.
Wire widths, ordering, positions and the existing15-bit range check are unchanged.
Literal-byte controls retain update/disable compatibility.

## Shipped-artifact and independent evidence

Build16042 WildStar64.exe SHA256:
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
REA6.8.0 PE identity/import Evidence
`ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`
is reused from its retained bundle. Fresh PE section-offset/byte/hash checks
match16 selected ranges and11 anchors to the retained read-only standalone
Ghidra verification. The portable record carries the selected ranges/anchors
and input hashes without requiring separate research drafts to be published.

| Native path | Supported observation |
| --- | --- |
| Update`1403f4170`, event`140430f80` | First payload word is passed unchanged as the event key; final decoded word is stored separately in the context`+0x7F30` map |
| Disable`1403f4200` | Removes cached state and dispatches the same first word as the disable key |
| Registration`14050b520`, helper`1400e9c20`, pair`140c59f20` | Registers global `GetMapTrackedUnitData` to`140511c80`; the historical analyst GameLib label does not establish a namespace |
| Getter`140511c80`, direct lookup`140242900`, loader`1402426a0` | Argument1 selects TrackingSlot directly, with no15-bit mask; row`+4`/`+8` supply label/icon |
| Initialization`140497390`, helpers`140496ca0/140496e20` | Independent objective association stores TrackingSlot row IDs; it does not establish arbitrary slot-selection policy |

The independently preserved authored ZoneMap handler calls the same global
getter with its update key, replaces the overlay under that key and removes it
on disable. Its [2017 immutable source](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/ZoneMap/ZoneMap.lua#L3762)
corroborates the display relationship; this older snapshot alone would not prove
final-build field identity or server scheduling. The pinned native registration,
getter and independent objective cache provide the additional proof.

REA identity/import evidence remains distinct from standalone Ghidra12.0.4 and
LLVM observations. The unavailable REA native provider was not retried or
reconfigured. No native program, client or Computer Use was executed.

## Verification and remaining work

The rebuilt baseline on`a621990e` reproduced four failures with three controls
passing. One earlier five-case run used an old test assembly while compilation
was still active; it is retained as invalid evidence (`stale-test-copy.*`) and
is not a baseline or qualification result. The final Game owning build and
test-project build have zero warnings/errors. All118 affected cases pass,
covering the helper, packet shapes/names and existing entity no-emission controls.
Spec and Standards reviews each have zero findings. There were no Source edits
after review. No full solution build/full suite was run.

Local evidence: `artifacts/continuation-20261105/tracked-marker-contract/`;
retained inspector output: `artifacts/continuation-20261105/lifecycle-unblock/`.
Observed builds took64.29s baseline-test,18.08s owning Game and25.98s final-test;
the final118-case test body took294ms. The unchanged historical handoff
Source-divergence failure was not rerun or reclassified.

The read-only upstream refresh examined100 all-state PRs in update order;
the latest20 reviewed identities/heads/bases/states remain unchanged, including
open608 and closed/unmerged609. No upstream changes were applied here.

Production slot selection, recipient/cadence rules, initial replay, disable
lifetime and trailing-field semantics remain open. There is no new producer,
actual-table corpus qualification, configured-provider/deployment/socket/native
or retail acceptance claim. This closes the incorrect Source identity/mask
assumption, not tracked-marker gameplay. Preserve defaults15:1/17:1 and no
automatic44 or seven baseline grants. The broader Goal remains active.
