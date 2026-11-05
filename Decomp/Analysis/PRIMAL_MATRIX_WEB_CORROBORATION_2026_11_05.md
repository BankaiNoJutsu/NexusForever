# Primal Matrix 06DF: authored UI and publisher corroboration

The [same-build native follow-through](PRIMAL_MATRIX_NATIVE_CONTRACT_2026_11_05.md)
now verifies the save/confirmation leads below against the pinned executable,
including opcode `0072` and positive allocation increments. This public-source
note retains its historical version limits; native proof is recorded separately.

5 November 2026. This pass follows the newly identified native consumer in
[NEXT_REA_UNBLOCK_TARGETS_2026_11_05.md](NEXT_REA_UNBLOCK_TARGETS_2026_11_05.md).
The parent investigation owns native disassembly, packet corrections and Source.
This note supplies independent authored-UI names and bounded publisher evidence;
it does not launch the client, execute REA/Ghidra, change Source or claim a
production producer.

## Result

An archived Carbine-authored Primal Matrix addon supplies precise MatrixWindow
API/field names for the native investigation. Its node state is an allocation
count with separate saved and maximum counts. Its AMP reward and generic AMP
display APIs are independently visible. The public snapshot is older than the
supported final client and cannot by itself bind an opcode or native offset.

The parent follow-up has now traced the allocation APIs in the pinned build16042
binary, as recorded below. A packet rename or field interpretation must rest on
that same-build native connection, not the community repository's build claim
alone. Producer and server allocation policy remain separate gates.

## Authored UI source and identity

The source is a community mirror claiming to contain Carbine's official addons;
the Lua header identifies NCsoft copyright. This is authored client source
preserved by a third party, not a publisher-authenticated binary download. It
was inspected in memory; no source files were copied into this checkout.

The inspected repository revision is
`aa608d0b837ff8037eb8509123d8e3ed4efcb9e4`, committed 12 September 2017.
The Live PrimalMatrix Lua/XML files last changed in
`d37c0399f1d481faf23c027cced44b05a07ab3da`, committed 22 March 2017, whose
message identifies `v1.7.0.15134 API 15` and says the changes were copied from
PTR. The Lua blob is identical at these two revisions. No inspected commit or
file establishes final build16042 provenance.
[Repository/file history](https://github.com/Zod-/Wildstar-Carbine-Addons/commit/d37c0399f1d481faf23c027cced44b05a07ab3da).

| File at inspected revision | Bytes | Git blob | SHA256 of decoded GitHub blob bytes |
| --- | ---: | --- | --- |
| Live/PrimalMatrix/PrimalMatrix.lua | 58687 | `0c0942be569b0f19fdb7816c12e4be7b1ddcd722` | `BB82D3F633E2482661C1B0AC96D5CD3C2401C11905A8781234ABDA922E5748F3` |
| Live/PrimalMatrix/PrimalMatrix.xml | 78281 | `92a1314358b59121e52a4c21dd5e397307da26ff` | `EF1E8AA8078830B4839F9E68797B78CA821AB1FAD00096A54EAAE7BC967CCC34` |
| Live/AbilityAMPs/AbilityAMPs.lua | 34768 | `d6c8e4c17f794f284c1e35e38c4013de9c371c47` | `48AFA18998BD92C9EA7629D208FCB47AA856C9D49675A346EA670126A5B73F5D` |

### MatrixWindow node and allocation names

| Source location | Observed authored API/field relationship | Native question it can corroborate |
| --- | --- | --- |
| PrimalMatrix.lua 449-450, 1152-1157 | `GetNodeAtHexCoord` and `GetStarterNode` return nodes; the starter's `id` attaches a window to that node | Does the consumer key match the native node's table/object ID? |
| 494-519, 573-574 | `nAllocations`, `nSavedAllocations`, `nMaxAllocations` drive rank, next-rank and pending-refund presentation | Is the parallel wire byte the saved allocation count? |
| 656-674 | `GetAllocationCount`, `GetPendingAllocationCount`, `GetTotalAllocationCount` drive completion bars | Distinguish committed, pending and maximum totals |
| 395-396, 845-849 | AMP rewards expose `nAMPPoints`; aggregate rewards expose current/maximum values under `AMPPoint` | Trace reward-derived AMP power, without assuming a packet scalar |
| 94, 704-714 | `PrimalMatrixUpdated` invokes `Revert`, then refreshes currency/allocation/progress display | Corroborate the already mapped client refresh event |

These are observations of this older source. They neither identify `06DF` nor
prove its final scalar's offset/meaning. The refund visibility test separates
pending changes from saved allocation state; it is not evidence of a full
post-commit respec/refund policy.
[Immutable PrimalMatrix Lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/PrimalMatrix/PrimalMatrix.lua).

### Save, revert and UI events

The XML declares a MatrixWindow and binds node allocation, affordability and
deallocation-prevention events. It gives discard/save separate controls; save
confirmation reports `PrimalMatrixSaving`. The corresponding Lua assigns the
`SavePrimalMatrix` confirm action with the MatrixWindow as its action data.
This supplies an authored save-action lead. It does not supply the server
transaction, C2S opcode, validation, cost, reset, reply grammar or send timing.
[Immutable XML, lines 4-25](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/PrimalMatrix/PrimalMatrix.xml#L4).

### Generic AMP power display

AbilityAMPs.lua registers `CharacterEldanAugmentationsUpdated` at line89 and
displays `AbilityBook.GetTotalPower()` and `AbilityBook.GetAvailablePower()`
at 484-494. It also uses `GetAvailableLockedInPower` for AMP reset eligibility.
This corroborates a generic AMP UI consumer for the event emitted by the new
native `06DF` mapping; it cannot independently assign a Primal Matrix bonus to
player offset `+0x6E68`. This file last changed at
`5492ad2de7a597abd951043722c3f568e677789b`, 29 July 2016, labeled
`v1.5.4.13938`, predating Primal Matrix.
[Immutable AMP UI source](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/AbilityAMPs/AbilityAMPs.lua#L484).

## Publisher corroboration and its build boundary

The 9 February 2017 publisher article says individual nodes can have multiple
tiers, Common Essences improve stats, and Violet nodes grant AMP/ability points
or class ability unlocks/upgrades. It also describes per-node Heroism values.
This independently supports count/rank semantics and distinguishes Heroism
from AMP points. **The article explicitly describes PTR content subject to
change.** Its exchange ratios and tier descriptions are not final-build
transaction specifications.
[Examining the Primal Matrix, Steam publisher API gid 75835794574490092](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1486662943).

That exact API selector was checked. The returned publication time is
`2017-02-09T17:55:42Z`; its `contents` UTF-8 encoding is 7046 bytes with SHA256
`5E434135B5B2166824CD0DA1563282908DCC3E80AD4303507A28400CE0B984C3`.

The 8 March release announcement confirms the system went live and links to
the February explainer. It establishes release context, not that every PTR
detail was unchanged through build16042. It supplies no exact allocation cap,
full node/reward catalog, packet fields or reset contract.
[Power of the Primal Matrix Now Live, publisher API gid 75838332253842375](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1488998368).

## Existing table schema is a corroboration lead

The fork's [PrimalMatrixNodeEntry](../../Source/NexusForever.GameTable/Model/PrimalMatrixNodeEntry.cs)
declares `Id`, `MaxAllocations`, four essence costs and six class-specific
reward IDs. [PrimalMatrixRewardEntry](../../Source/NexusForever.GameTable/Model/PrimalMatrixRewardEntry.cs)
declares four reward-type/object/subobject/value tuples. The authored UI's
maximum allocations and rewards are consistent with those schema concepts.
These C# declarations are not an independently recovered actual row corpus,
native enum mapping or proof that a particular wire byte must be Boolean.

## Parent same-build native follow-up

The shared native investigation supplied named Lua binding cells and retained
`deep-named-getters-console.log`, `deep-lua-binding-candidates.json`,
`deep-fields-console.log` and `deep-matrix-buff-console.log` under
`artifacts/continuation-20261105/blocker-next-targets/`. This note reread those
outputs without running the shared session.

| Exact build16042 named getter | Binding cell -> code | Observed retained result |
| --- | --- | --- |
| MatrixWindow GetAllocationCount | `140c5ea70` -> `1406eb160` | Sums saved cache-node bytes at `+0x40` |
| MatrixWindow GetPendingAllocationCount | `140c5ea80` -> `1406eb230` | Sums positive differences between editor byte `+0x44` and saved cache byte `+0x40` |
| MatrixWindow GetTotalAllocationCount | `140c5ea90` -> `1406eb2c0` | Sums backing-table-row word `+0x14`, consistent with the declared MaxAllocations column |
| AbilityBook GetAvailablePower | `140b75250` -> `140748f40` | Calls shared availability helper `1403cdd00` with mode0 |
| AbilityBook GetAvailableLockedInPower | `140b75260` -> `140748f80` | Calls the same helper with mode1 |

These independent named getters strengthen the parallel-byte interpretation as
saved allocation counts rather than Booleans. The parent already mapped
`06DF` to the saved cache's `+0x40` writes. Retain its artifact binding and
portable byte checks when applying that field interpretation.

The same binding scan locates GetTotalPower at cell `140b75240` -> `140748f00`.
The selected decompile pass reports no FunctionManager body there; it is **not**
a successful decompilation. The parent subsequently checked the LLVM
instructions: this named getter reads the signed16 total at player `+0x6E60`.
The separate `1403cdbf0` calculation adds player `+0x6E68` and `+0x6E64` to
level-derived power and writes that total; `06DF` writes `+0x6E68` and invokes
the calculation. Thus the packet contribution is distinct from the total AMP
power returned to the UI. The parent's instruction/byte record owns the exact
same-build binding; the earlier decompile error remains retained.

## Remaining gates and excluded substitutions

1. Complete the same-build node-key and final-scalar interpretation using the
   parent traces. The allocation getter relationship above is already observed;
   do not repeat it as unknown. Preserve the distinction between a total AMP
   count and the Matrix-derived contribution.
2. Reset/deallocation behavior after commit, costs, prerequisite adjacency,
   recipient rules, persistence and producer timing need their own evidence.
   Pending UI revert does not establish any of these.
3. Node IDs, per-node maximums and reward types need accepted table values and
   native readers before server allocation policy. No global hard cap is
   inferred from an observed UI progress bar.
4. An accepted final-build packaged UI hash would strengthen source provenance.
   The 2015 NASADocs/Hammster API inventories examined in this pass did not
   provide the required MatrixWindow definitions. They are not substitutes for
   the pinned binary's API implementations.

Searches were directed to the newly found authored-source repository and its
immutable file histories, MatrixWindow calls, AMP display getters and original
publisher's specific February/March posts. No modern emulator implementation
was used as retail proof. The shared REA/Ghidra session was left untouched.

Validation: GitHub blob identities, byte hashes, source line anchors and both
publisher API GIDs were checked. Local links and whitespace were checked.
No application build or tests are claimed for this research-only file.

## Deeper save/confirm lifecycle follow-up

The authored Lua's addon `OnSave` at 61-80 persists only the confirmation-dialog
preference. It does not serialize node allocations. Both save-button paths
pass the MatrixWindow as opaque confirm-action data. The Lua consumes counts
and node presentation, but exposes no typed pending node arrays or numeric
SavePrimalMatrix enum value. Its `OnConfirmSaveConfirmed` callback updates
dialog/preference state; that callback is not an authoritative server-commit
acknowledgement.
[Authored save/cancel handlers](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/PrimalMatrix/PrimalMatrix.lua#L297).

The XML at line147 explicitly warns that purchased allocations cannot be
refunded after saving. It therefore supplies historical UI evidence separating
pending discard from committed purchases. The text is present in the same
15134-era source, not independently extracted from build16042. Its event at
149 is named `PrimalMatrixSaving`; it does not report server success.
[Immutable confirmation form](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/PrimalMatrix/PrimalMatrix.xml#L147).

### Public native leads and subsequent pinned verification

The [subsequent native pass](PRIMAL_MATRIX_NATIVE_CONTRACT_2026_11_05.md)
verified confirm type 41, the callback and outgoing opcode `0072` in build16042,
including its positive-increment staging and writer. The public excerpts below
were leads; their source bytes alone do not authenticate an executable.

Authenticated exact-name GitHub search found a public native excerpt registering
`CodeEnumConfirmButtonType.SavePrimalMatrix` as **41 / 0x29**. Its helper and
registration context are visible, but the repository supplies no inspected
executable digest/build binding. The separate pinned verification establishes
41 as the final-client confirm enum value, distinct from the network opcode.
[Registration excerpt, lines 1604-1646](https://github.com/ChargeIn/WildstarClientIDA/blob/5a6e5549050d0be88e4389dd890f4fe32062c54a/IDA/functions/sub_140709460.c#L1604).

A second excerpt at `140523A60` emits the exact saving event. It stores paired
lengths at action `+0x20/+0x30` and pointers at `+0x18/+0x28`. Equal lengths
allow a count-and-two-pointer temporary to reach `1403F4900` with internal code
`0x72`; the action then performs MatrixWindow cleanup and clears its references
and lengths. This excerpt does not identify the element widths, payload writer,
wire opcode, accepted save order or server transaction. The separate pinned
sender/writer verification establishes `0x72` as an outgoing network opcode;
accepted save order and server transaction rules remain open.
[Saving callback excerpt](https://github.com/ChargeIn/WildstarClientIDA/blob/5a6e5549050d0be88e4389dd890f4fe32062c54a/IDA/functions/sub_140523A60.c).

The callback's `1406E6AD0` helper performs editor/animation cleanup and ends at
`14010B100(matrix+0xAA0)` in the inspected excerpt. This supplies another precise
native target; it is not a discovered wire sender.
[Cleanup excerpt](https://github.com/ChargeIn/WildstarClientIDA/blob/5a6e5549050d0be88e4389dd890f4fe32062c54a/IDA/functions/sub_1406E6AD0.c).

The three public excerpt files were decoded from GitHub's base64 blobs in
memory. SHA256 values bind those source bytes, **not** the original executable:

| File | Bytes | SHA256 |
| --- | ---: | --- |
| sub_140709460.c | 78993 | `506945EE56FCCC5AA8A527451BE4DD66BE5FBDE6E88CB6C9F81DFAFB0EA319C3` |
| sub_140523A60.c | 1081 | `AD3B15B457941EA3A298216A56F2DE50EC47FF8CF86EDB4C2191BE7EB9F6CB04` |
| sub_1406E6AD0.c | 917 | `97BCDC96DED8CAC2DF1306AED15A49F23600BA8308152085E6BEADB2CFC5D71C` |

No decompiled source was copied into Source or this note. The numbered enum,
paired action buffers and dispatch code have now received the separate pinned
verification linked above. No public typed array API,
final-build reset policy or packet send order was established by this deeper
web pass.
