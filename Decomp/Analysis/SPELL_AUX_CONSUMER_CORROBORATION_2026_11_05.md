# Spell auxiliary consumer corroboration

5 November 2026. Follow-through for `080F`, `0810` and `0812` from
[the next-target investigation](NEXT_REA_UNBLOCK_TARGETS_2026_11_05.md).
This note records static client observations and bounded field interpretations.
It adds no Source rename, producer, runtime policy or client execution.

## Result

- `0810` rows identify a **spell wrapper**, a **unit-local node**, and an
  **absolute count for that node**. The count is not a delta and is not
  necessarily the total count displayed for a shared buff record.
- `080F` uses the same first two identities to rebuild existing variant children.
  Its third wire word is not read by the observed consumer; its intended producer
  meaning remains unknown. Its three-word wire shape must remain intact.
- `0812` identifies a wrapper and unit-local node, selects a child by the
  variant's second header word, and replaces a duration/window value before
  refreshing its timer. That child key is **not Spell4EffectId**. Its stronger
  server meaning remains unknown.
- A repeated `0810` absolute count is **not** a harmless heartbeat: the client
  refreshes child timers and timestamps before setting that count. Equal numeric
  count does not make the complete packet application idempotent.

## Inputs and provenance

The parent investigation owns REA and the standalone Ghidra project. Its REA PE
identity for the archived build16042 `WildStar64.exe` is
`ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`, SHA-256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
The input is at
`C:/GIT/_archive/cluster-3d/NexusForever/Decomp/Client64/WildStar64.exe`.
This pass reread that file without executing or changing it.

Fresh standalone Ghidra output supplied by the parent is under
`artifacts/continuation-20261105/blocker-next-targets/`:

- `apply-owners-console.log`: direct `080F/0810` consumers and `0812` helper.
- `deep-helpers-console.log`: absolute-count setter and refresh/rebuild helpers.
- `deep-fields-console.log`: independent node construction and remaining-time getter.
- `deep-matrix-buff-console.log`: the named Unit:GetBuffs entry point.
- `deep-named-getters-console.log`: variant creation and timer rearming helpers.
- `deep-ui-fields-console.log`: current named buff fields, variant child header
  copying and the wrapper-runtime-key lookup.
- `deep-save-timer-console.log`: named buff remaining-time calculation at
  `140453ee0`.

These are Ghidra inspector observations, not REA decompiler output or retail
execution. Parent-owned byte anchors remain in
[the portable next-target record](coverage/next-rea-targets-20261105.json).
The [deeper-contract record](coverage/deeper-packet-contracts-20261105.json)
adds the selected helper, constructor, named-getter and clock anchors. Across
the Matrix and spell follow-through, all 30 code ranges and 13 additional
anchors match both the pinned PE and the read-only Ghidra program.

This agent additionally used read-only LLVM at
`C:/Program Files/LLVM/bin/llvm-objdump.exe`, SHA-256
`3AE6715601F2EB0C1D0839929C46089906BA0F1E5E032FE519656A40C936B863`, for the complete
observed `1405a5a10..1405a5a91` and `1405a5980..1405a5a05` consumer bodies and
the `0812` case `1403ee2f6..1403ee32b`, and variant reader
`140094890..14009496b`. The terminating returns were observed;
padding after those returns was not treated as function behavior.

## Wire shape and current Source boundary

The existing reader `140095da0` and row reader `140080bf0` establish a uint32
count followed by 12-byte rows for `080F/0810`. `14007fef0` establishes four
uint32 words for `0812`. Those unchanged wire contracts are represented by:

- [spell triplet-list models](../../Source/NexusForever.Network.World/Message/Model/ServerSpellUInt32TripletList.cs).
- [shared scalar/list payloads](../../Source/NexusForever.Network.World/Message/Model/Shared/ServerScalarAuxPackets.cs).
- [four-word spell model](../../Source/NexusForever.Network.World/Message/Model/ServerSpellFourUInt32.cs).

The spell lists currently inherit `Rows` as a list of the shared neutral
`ServerUInt32Triplet`. Realm `05A1` also uses that shared payload. Any future
meaningful spell field names must be scoped to spell-specific row contracts;
the shared scalar row must not acquire spell semantics globally.

The selected Game source search found no constructor/send usage of these
auxiliary list models. This note does not add one.

## Independent unit-node identity

The `0810` consumer at `1405a5980` looks up row word0 through
`SpellService_LookupSpellWrapperByWrapperId` (`140561c30`), walks the wrapper's
list at `+0x48`, compares word1 with node `+0x40`, then passes word2 to
`1407188d0`. The instruction at `1405a59e5` loads word2; the call at
`1405a59e9` reaches that helper.

This node identity is corroborated away from the auxiliary consumer:

1. `14053d680` takes a nested `0818` tier-entry, resolves its leading word with
   `Entity_ResolveById`, then finds or creates the matching wrapper node.
2. `140717990` copies the resolved entity's ID at entity `+8` into node `+0x40`
   and links the node into both the entity and wrapper lists.
3. `140717df0` resolves node `+0x40` through `Entity_ResolveById` again before
   modifying the count/buff record.

These paths corroborate **UnitId**, rather than Spell4Id, a tier number, pet ID
or client buff-record ID, for auxiliary row word1. They do not establish which
units a retail server chooses as recipients.
The initializer also stores node `+0x40` into buff-record `+0x54`; the named UI
builder publishes that record field as `unitTarget`, separately from `+0x58`
as `unitCaster`. Thus the observed node belongs to the affected target unit.

The existing [0818 model](../../Source/NexusForever.Network.World/Message/Model/ServerSpellWrapperTierEntry.cs)
already names its outer identity `SpellWrapperId`. The older
[SpellList TierEntry](../../Source/NexusForever.Network.World/Message/Model/Abilities/ServerSpellList.cs)
still calls its nested leading word `Spell4Id`, despite the entity lookup above;
that stale name must not be used to rename the new auxiliary field incorrectly.
Likewise, the runtime wrapper key is not automatically the public spell-table ID,
an aura ID, or the server's current cast identifier.
The fresh lookup at `140561c30` uses the service's `+0x7A8/+0x7B0`
hash-map family; the separately named Spell4 lookup uses a different map.

## 0810 absolute per-node count and timing side effects

`1407188d0` walks the node's existing children and calls `1405d6b80` for each.
It then refreshes the attached buff record's timestamp/window fields and calls
`140717df0` with the incoming word2.

The latter routine computes `incoming - node.oldCount`, stores `incoming` into
node `+0x50`, and applies only that delta to attached buff-record `+0x1C`.
The shared record can cover more than one wrapper; its wrapper-ID list is
separately managed at `+0x40/+0x48`. Thus the packet carries an absolute
**node** count, while the UI may observe an aggregated record count.

If the aggregate falls below one, the client prunes/releases the corresponding
record. Otherwise it requests a buff-manager update. This is a consumer rule,
not permission to infer server stack caps, accepted negative/high-bit counts,
which effects create records, or exact send cadence.
The named UI table builder `1405bc680` independently inserts buff-record `+0x1C`
as `nCount`. Its `idBuff` instead comes from record `+0x10`; those identities
must not be collapsed into the wrapper/unit/count row.

`1405d6b80` releases/rearms a child's timer using its stored duration at `+0xBC`
unless its effect flags select the exempt path. Consequently a retransmission
with unchanged count can still change timer state. A future producer must be
driven by a proven event/order rule, not by periodic numeric synchronization.

## 080F rebuild semantics and ignored third word

`1405a5a10` reads row word0 at `1405a5a38` and word1 at `1405a5a61`. On a node
match it calls `140718980` at `1405a5a75`. The complete observed body through
its return at `1405a5a91` contains no row `+8` load. It does not pass the wire
word2 as a count to that helper.

`140718980` snapshots each child's remaining timer value, destroys the old
children, and re-applies the saved variant entries using the node's existing
count. It recomputes the cached window and can remove the count when no active
window remains. This differs materially from `0810`'s explicit count update.

The consumer also stops its remaining rows when a wrapper lookup fails;
`0810` skips an unresolved wrapper/node and continues. These client behaviors
are useful future capture controls, not new server validation policies.

Only **unused on this observed consumer path** is established for word2.
Do not delete the word, assign it count/delta/flags semantics from its neighbor,
or claim it is unused by every possible handler/producer.

## 0812 selected child and duration update

The indexed case at `1403ee2f6` resolves payload word0 using the same wrapper
lookup, then passes the payload to `14053f620` at `1403ee31f`.

`14053f620` matches payload word3 to wrapper-node `+0x40`, matches word1 to a
child's `+0x64`, writes word2 to that child's `+0xBC`, and calls `1405d6b80`.
It updates the node/buff timing window relative to the current native clock.
The node identity independently established above makes word3 **UnitId**.
The generic child initializer at `1405d6880` provides a critical distinction.
It copies the first eight bytes of the variant header to child `+0x60..+0x67`.
The independent bit-reader at `140094890` reads 19 bits into variant `+0`
(`Spell4EffectId`), then 32 bits into variant `+4`, then the remaining words.
Thus child `+0x60` is the table effect ID, whereas the `0812` comparison uses
child `+0x64`, the next raw variant word. The current model calls that latter
field `VariantEntry.Value0`. A proposed `0812.Spell4EffectId` name is rejected.
An effect-instance/key interpretation is a lead, not an established field name.
The packet is not a generic spellbook, pet, aura-instance or cooldown-group row.

`140195f70` returns the nonnegative difference between a timer's stored deadline
and the current timer service clock. This independently corroborates remaining
time use in the `080F` rebuild path. The helper's pre-existing analyst label
contains `Ms`; the exact wire unit must be supported by current getter/scheduler
arithmetic rather than that label alone.

The independent buff-window getter `140453ee0` completes this dataflow: it reads
the window at buff-record `+0x24` and computes elapsed time as
`DAT_140c636a8 - record.timestamp(+0x20)`. A finite window returns
`window - elapsed` while the observed expiry condition holds; the `-1` sentinel,
expiry and a selected spell flag return zero. This is the raw value scaled by
the named Lua builder below. `0812` updates a duration/window in this same clock
basis, rather than carrying the current absolute clock value. The sentinel and
special-spell branches do not establish a universal server infinite-duration rule.

## Named UI corroboration and version boundary

A read-only PE string/pointer scan found the sole ASCII `GetBuffs` at
`140b2ae68`. The Lua registration cell at `140c5b220` contains that string pointer
followed by function pointer `1406538a0`. Its fresh body resolves a unit, gathers
its buff records and calls `1405bc680` for each entry before publishing the
`arHarmful` and `arBeneficial` tables. The table names in this build are UTF-16;
an earlier ASCII-only lookup did not enumerate them.

The fresh `1405bc680` body inserts record `+0x10` under `idBuff` and record
`+0x1C` under `nCount`. It obtains raw remaining time from `140453ee0`, converts
it to float, multiplies by `0.001`, and publishes that value as
`fTimeRemaining`. These exact named fields corroborate the count aggregation
and a 1000:1 raw-to-UI time conversion. They do not make a wire duration an
absolute server-clock timestamp.

A second named API supplies an independent current-clock cross-check.
`GetGameTime` at `140b42518` has a registration pair at `140b72930` pointing to
`1406f74f0`. Its instructions at `1406f7538..1406f7567` read the same
`DAT_140c636a8` clock, convert it to double and multiply by the constant at
`140b79e98`. The latter's exact bytes `fca9f1d24d62503f` decode to `0.001`.
The [authored Houston GameLib documentation](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Help%20File%20Extracted/LuaAPI/Classes/GameLib.htm#L1143)
also documents a division by 1000 for `GetGameTime`. This corroborates the
native/UI clock scaling; it is not a wall-clock or retail network observation.

The [authored Houston Unit:GetBuffs documentation in the 2015 NASADocs snapshot](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Help%20File%20Extracted/LuaAPI/Classes/Unit.htm#L684)
describes `nCount` as buff stacks and distinguishes `idBuff` from the effect's
spell ID. Its `fTimeRemaining` milliseconds statement does not explain the
current native 0.001 conversion and is not used as exact-build unit proof. This is a dated
primary UI document, not a build16042 packet specification. The current native
table-construction and scaling arithmetic take precedence for unit/field claims;
the document cannot establish wrapper, effect or server producer identities.

## Safe next boundary

The independently corroborated node identity and absolute-count dataflow support
spell-specific wire naming work. Remaining production gates include initial
wrapper/node availability, recipients, order, duplicate/replay timer behavior,
count limits and actual retail producer triggers. `080F`'s third word remains
neutral. Timer units and child identity must use the named initializer/getter
cross-checks before a stronger name is applied. No broad gameplay, native UI or
retail qualification is claimed by these static paths.
