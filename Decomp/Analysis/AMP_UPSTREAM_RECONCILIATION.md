# AMP prerequisite and lifecycle reconciliation

## Owned unfiltered proc augmentations

6 November 2026. Existing login, active-spec/node changes and bounded retry now
apply supported permanent self-target proc AMPs through the normal cast/effect
producer. Full/section/single respec and spec changes remove only registrations
created by that AMP cast. Shared selected spells retain one installation;
pre-existing same-spell registrations are neither recast nor claimed.

The effect handler enrolls its actual AddProc return in an internal per-cast
parameter record only when the target is the original caster. This protects an
external registration inserted during a cast callback, including one sharing
the numeric casting ID. Removal uses the existing reference-checked handle.
Later replacements remain external. Reentrant respec retires an accepted old
installation; newly eligible reentrant work can use the existing one-second
retry. Existing property/cooldown, learned spells, grants and child-cast rules
remain. No schema or entitlement default changes.

### Actual catalog and limits

The owning reader qualifies all218 pure-proc augmentation spells. Of these,
69 pass zero-timing, passive group, permanent-caster and existing dispatch
screening. Seven also match the established Source fixture's raw tail pattern
DataBits05=1 and DataBits06..09=0:

| Spell4 | AMP node | Triggered Spell4 |
| --- | --- | --- |
| 43219 | 572 | 43220 |
| 57161 | 370 | 57162 |
| 57336 | 498 | 57337 |
| 58984 | 718 | 79899 |
| 59126 | 148 | 60979 |
| 59142 | 163 | 60992 |
| 59154 | 50 | 60941 |

Existing allocation/class/prerequisite checks remain the authority for selected
nodes; catalog eligibility does not grant or activate an unallocated node.
The other62 structural candidates have additional uninterpreted tail data.
The other149 pure-proc spells fail that structural screening. These counts
classify Source-compatible eligibility; they do not assign retail meaning to
the opaque fields or qualify every triggered child's gameplay.

All seven admitted actual effects register through SpellHandler on a concrete
Unit, enroll the exact cast-owned handle, and pass identity/duplicate-removal
controls. Raw node148/effect141788 and its owning-reader38483 outer projection
also drive login/spec/respec and Character save/reload regressions with a
prepared player/cast adapter. The other six have actual catalog/registration
qualification, separate from full live ability execution.

### Validation

The qualified baseline has12 failures and10 controls, with the read-only
capture API present and automatic proc reconciliation still unchanged. An
earlier same-result fixture used a stale outer base; it is retained separately.
The review's callback-insertion defect is reproduced in one further regression,
then fixed by per-cast enrollment. The earlier240-pass candidate is retained
without being promoted to the final ownership claim.

Final owning/test builds are clean in12.31/13.92seconds. All23 new/all241
selected AMP, action-set, proc, cooldown and Unit combat cases pass; the scoped
runner reports14seconds. The standalone actual-catalog build takes2.50seconds.
No full solution or full suite ran. Game.Abstract/Game/World owning DLLs match
the tested copies; the actual probe uses the same Game DLL. Game/World/test
DLLs differ from the baseline, so this is not a same-test-DLL comparison.

The isolated candidate contains published HEAD plus seven owned Source paths
and eight historical project/filter or BOM/newline variants. Foreign personal
damage/healing interface and effect-handler hunks are retained in the shared
working tree and excluded from this commit/candidate. The
[portable record](coverage/proc-amp-lifecycle-20261106.json) binds Source/binary
identity, complete scoped results, actual catalog inputs and review. Local
logs are under artifacts/continuation-20261106/proc-amp-lifecycle/.
Raw-header/probe-access setup errors remain separate from behavior failures.

### Standards

One original cast-enrollment finding was reproduced and resolved. Final review:
zero remaining actionable findings. Only original-cast-enrolled handles are
owned; callback-inserted same-spell registrations survive. All seven isolated
candidate identities match; foreign hunks are excluded. Inspection only.

### Spec

Final review: zero remaining findings. Enrollment is tied to the original
cast/caster and existing selected-node lifecycle. All seven candidate hashes
match. The exact parameter-type regression assertion uses the new concrete
parameter type, and final build/test and actual-row executions pass.

Broader proc tails, mixed/timed/conditional AMPs, exact chance/cooldown/recursion
retail ordering and triggered-child semantics remain open. No fresh configured
provider, copied upgrade, physical restart, hosted socket, native UI or retail
qualification is claimed. Client/CUA remain stopped. Defaults15:1/17:1 remain;
no44/seven baseline grants. Continue independent gameplay/progression; the
overall Goal remains active.

## Activated item ownership

4 November 2026. Activated item use retains the player and inventory that
acquired the mutation lease. After prerequisite evaluation it requires the
original item before priming charges or casting. A replaced item retires the
parsed cast request with `ItemBad`. A successful cast can consume only through
the original player and inventory while they are still current. The inventory
checks the exact item reference in its bag slot before changing the item or
awarding consume achievement progress, including when a replacement has the
same item GUID.

The baseline reproduced consumption through a rebound session player and
acceptance of a stale object at an occupied slot. The focused regressions also
cover replacement during prerequisite evaluation. The initial 678-case scoped
run passed in 7 minutes. The final WorldServer build had zero warnings and
errors, and 52 affected tests passed in 7 seconds against the final handler and
the same Game DLL. Local TRX, input and binary identities, and review outcome
are in `artifacts/continuation-20261104/item-use-ownership/`.

This covers shared item-use ownership in source. The final-build bonus AMP
upgrade item and activation row, atomic consumption and grant, and deployed
socket behavior remain open. The historical inlaid AMP unlock path is separate.

## Bonus AMP power publishes after commit

4 November 2026. Build 16042 dispatch slot `0x1403f1844` routes `01A1` to
`0x1403f0f5a`. The client sign-extends the payload's first word and replaces
the bonus power at player state `+0x6e64`. It recomputes AMP state through
`0x1403cdbf0` and dispatches `CharacterEldanAugmentationsUpdated`. This is an
absolute value, so a pending retry can send the latest confirmed value.
REA bound the executable to the same SHA-256 as the inlaid list trace below.

Live spec budgets still update when power is earned. The real Player now
enqueues `ServerAmpPowerUpdate` on the original character's queue only after
the guarded spellbook save confirms its flushed value. The initial sync
reads the same confirmed value. A later grant during the frozen save waits
for its own commit. An enqueue failure retries the latest confirmed value,
including an initial snapshot with no new grant. Actor, manager, session and
queue ownership guard the send, and ownership is checked again before the
send is acknowledged. A reentrant initial sync waits for the in-flight
packet so a newer absolute value cannot be followed by an older one.

Three baseline real-player cases reproduced provisional publication. The
first candidate passed 179 scoped cases across AMP, experience and its
publication companions. The final source passed 108 affected cases. The
strengthened reentry case then passed on the same Game DLL, committing power
5 inside the power 3 enqueue and observing both packets in order. Local PE
verifier, scoped TRX and inputs are in
`artifacts/continuation-20261104/amp-power-publication/`.
Live item consumption, deployed socket behavior and retail qualification
remain open.

## Earned bonus AMP power survives a character save

4 November 2026. The existing bonus AMP power handler updated the four
spec budgets and sent the power packet, but a character reload lost the
bonus. Two real-player baseline cases reproduced a reloaded budget of 45
after earning three bonus points and completing a save.

The guarded spellbook save now persists `bonusAmpPower`, including the
frozen ordinary-save snapshot and rollback recovery. A grant arriving during
the Auth phase remains for the next save. The loaded bonus applies to every
spec before its saved AMP costs are charged. Runtime power still caps at ten
bonus points. MySQL and SQLite add one non-null column with a zero default.

A historical spec may have saved more than 45 points of allocations while
the old volatile bonus was available. Such a spec keeps its saved nodes and
shows zero spendable power until respec or a new grant clears the deficit.
The migration has no record of the original earned bonus to recover. It
assigns zero rather than inferring an award from a saved spec.

The 149-case scoped run covered the guarded character save, both migration
models, and the historical upgrade fixtures. A copied SQLite fixture retained
XP, active spec, bonus tier points and a 50-point AMP spec. It exercised
loading and respec without granting bonus power. The final owning build and
all 74 affected AMP and migration cases passed after the refund bounds and
analyzer follow-up. Local evidence is in
`artifacts/continuation-20261104/amp-power-persistence/`.

The same two real-player cases passed on separate migrated MySQL 8.4.13
schemas with the configured retry strategy. The provider run took 54 seconds
after startup and verified all 48 migrations in each schema. The disposable
process and listener stopped afterward. See the
[provider record](DESTINATION_PROVIDER_QUALIFICATION.md#earned-bonus-amp-power-save).

The save-and-reload tests exercise the existing `AddAmpPower` entry point.
See the later [post-commit notification](#bonus-amp-power-publishes-after-commit).
Live item consumption, deployed sockets and retail qualification remain open.

## Unlocked inlaid AMP list and prerequisite 231

4 November 2026. Build 16042 static evidence identifies the character's
unlocked inlaid AMP list separately from the active spec's selected AMPs.
REA evidence `ev_da27f0ff8de8665b152bc739b7654db7aa4f78845a3d9775591172e68e5ccf3d`
binds the client image SHA-256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Pinned PE reads and LLVM disassembly verify these paths.

| Path | Client evidence |
| --- | --- |
| Player create `025E` | Dispatch slot `0x1403f1b38` routes through `0x1403ec91d` to `0x1403b5f80`. Reader `0x14008d205` reads a 6-bit count into payload `+0xe4`, then a count-sized ushort array through `+0xe8`. The apply path at `0x1403b626e` writes the count to player state `+0x6df0` and copies the IDs to `+0x6df4`. |
| Unlock list update `019C` | Dispatch slot `0x1403f1830` routes through `0x1403f0f48` to `0x1403d1c00`. Reader `0x14008e222` reads a 7-bit count and the same ushort IDs. The apply routine replaces the same cache and dispatches `CharacterUnlockedInlaidEldanAugmentation`. The pinned Carbine AMP add-on subscribes to that event and rebuilds its view. [Archived consumer](https://github.com/NexusInstruments/CarbineAddons/blob/cf063b3e22b08f49ff51c12cec258a9f1cf86d68/AbilityAMPs/AbilityAMPs.lua#L88) |
| Prerequisite 231 | Live dispatch at `0x1404a2ead` uses slot `0x140b678e0` and handler `0x14049d7b0`. It requires the local player, compares the row's `objectId` with a zero-extended ushort in the unlock cache, and handles Equal and NotEqual. Type 227 instead reads the active spec's selected AMPs. |
| AMP allocation | The branch at `0x1403d0746` checks the node's field `+0x1c` (`Item2IdUnlock` in the table model). When nonzero, it tests membership by the node ID and returns result `0x20` (`EldanAugmentationLockedInlaid`) if absent. |

The update apply caps the stored count at 54 but uses the incoming count for
the copy. The following player-state field is at `+0x6e60`, exactly 54
ushort slots after `+0x6df4`. Both outgoing models now reject oversized lists
before writing. They expose the named IDs and keep the legacy collection
aliases. The player-create field was previously called `KnownDyes` and had
no producer. Empty payloads retain their existing encoding.

Two baseline tests reproduced oversized serialization. The owning build and
all 73 scoped tests passed, including empty and 54-ID lists for both packets
and rejection at 55. Native verifier, PE reads, bounded disassembly, and the
pinned add-on are in
`artifacts/continuation-20261104/amp-prerequisite-231/`.

### Release boundary for item unlocks

Carbine announced the removal of the AMP item requirement for Reloaded. The
29 September 2015 release notes for build 10512 record that AMPs no longer
required unlock items and those items left world loot, bosses and vendors.
Build 16042 still carries the client cache and allocation branch. Their presence
does not establish an active final-build item grant. [Publisher announcement](https://www.prnewswire.com/news-releases/wildstar-will-become-free-to-play-this-fall-300089957.html) · [Archived Reloaded patch notes](https://wildstar.fandom.com/wiki/Patch_09/29/2015)

The final binary's enum entry at `0x140c34920` pairs `UnlockInlaidAugment`
with `0x7f`. The table descriptor at `0x140ad61f8` identifies
`item2IdUnlock` as a four-byte field at `+0x1c`. The effect 127 validator byte
at `0x140568246` selects slot 19 and `0x14056815a`, which returns success
without inspecting an operand. The pinned verifier and extracts are in
`artifacts/continuation-20261104/amp-unlock-effect/`.

The two retained build 16042 AMP node witnesses have zero `Item2IdUnlock`.
The full table and a final-build live grant remain unverified. A legacy item
producer, prerequisite 231 server evaluation and `019C` emission need a
contrary final-build row or capture before entering the runtime roadmap.
Bonus AMP power is a separate progression path already used by effect 128.

## Property ownership when an effect key is reused

4 November 2026. A same-spell replacement could reuse the original AMP's
property, effect and casting IDs. Respec then removed the replacement because
the ownership check compared numeric keys. Two baseline cases reproduced the
loss with and without an intervening augmentation refresh.

The supported property path now retains the installed effect instance itself.
Removal checks that exact instance against the current unit state, so a
replacement survives even when the numeric IDs match. The existing numeric
snapshot remains available for lookup, and supported casts still avoid
overwriting any current same-spell modifier. The held-cooldown path continues
to use its own instance identity.

The regressions exercise the normal respec and property-effect handler with an
injected same-key replacement. The owning build and all 160 scoped cases passed.
Local evidence is in
`artifacts/continuation-20261104/amp-property-ownership/`. Actual proc and mixed
AMP families still require row evidence and safe effect-lifetime ownership.

## Proc registration lifetime

5 November 2026. Spell expiry and cancellation now remove the exact proc
instance installed by that effect. A replacement with the same spell, casting
and effect IDs keeps its registration and buff. An effect whose registration
was rejected cannot remove a later proc. The current registration can still
be removed once through its own lifetime.

Both baseline cases removed the replacement. The regression uses the retained
registration fields for node 148, holder 59126 and effect 141788, with a
deliberately missing trigger spell in the rejection case. The retained
feasibility inventory also identifies node 50, holder 59154 and effect
141824. The owning build completed in 32 seconds with zero warnings and
errors. All 198 scoped tests passed in 3 seconds. Local source and input evidence is under
`artifacts/continuation-20261105/proc-lifetime-ownership/`.
Automatic application, trigger-child lifetime and retail behavior for proc
AMPs remain open.

### Triggered cast cooldown

A proc now starts its cooldown only when its own triggered cast succeeds.
An action proc can admit a nested spell before the outer spell fails target
validation. The pending queue grew in the failing baseline and incorrectly
started both cooldowns. The regression keeps the nested spell and its cooldown
in both cases and checks that the outer cooldown starts only on admission.
The owning build took 19 seconds with zero warnings and errors. All 213
scoped cases passed in 3 seconds with the updated Game DLL. Local baseline
and scoped verification are under
`artifacts/continuation-20261105/proc-dispatch-result/`.

### Triggered cast admission

5 November 2026. A triggered spell carries the registration that dispatched
it. Spell setup checks that identity before firing action-cast procs, and
admission checks it again after cast validation. If a script or nested cast
replaces the registration, the retired cast stops without sending a client
failure. The replacement can trigger on a later event. A spell that has
already been admitted keeps its existing lifetime when the proc is removed.

Both baseline regressions admitted the retired cast. The owning build took
30 seconds with zero warnings or errors. All 225 scoped combat, proc, AMP and
cast admission tests passed in 3 seconds on the matching Game DLL. Evidence is
under `artifacts/continuation-20261105/proc-trigger-admission/`. Automatic proc
AMP application and retail rules for already admitted child effects remain open.

## Original reconciliation

2026-10-29. Fork: `legacy-di-storefront-hardening`.

## Evidence and upstream scope

Reviewed the saved full comparisons and local PR heads:

| PR | Exact reviewed head | Disposition |
| --- | --- | --- |
| #418 | `5f21ebc415851b638373ccd0812c324e586c235a` | AMP-enabled prerequisite and grant-order intent reconciled. Existing checked ushort persistence retained and exercised with node300. |
| #471 | `7fea6ce184945ffb9656787b25dd40e1a638b91d` | Existing dependency injection retained; grant initialization corrected beyond the upstream constructor reorder. Active-node changes grant newly eligible spells without relogging. |
| #463 | `96d7290d9309a80a686eb8dd379520a4bf8b3eb2` | Instant permanent property-only and pure held-cooldown augmentation application/removal implemented with concrete ownership, including the actual passive group2 table contract. Broader augment spell families remain pending. |

WildStar64.exe build16042 SHA256:
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Cached fragments reside under `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_<hash>/`.

- Live prerequisite dispatch `1404a2100`, case `0xe3`, selects vtable `140b67740+0x198`, function `14049d760`. The handler requires the local player, supports Equal/NotEqual, and passes **objectId** to `1403cdf50` with active spec at `+0x6dec`; value is not the AMP identity.
- `1403cdf50` scans the selected spec's ushort AMP array, plus the active pending list; spec indices above3 fail. Server state includes newly added nodes and excludes pending deletion.
- Type231 selects `14049d7b0` through the separate `+0x1a0` slot and scans a global ushort list at `+0x6df0/+0x6df4`. The later [unlocked inlaid AMP mapping](#unlocked-inlaid-amp-list-and-prerequisite-231) identifies the list. This change registers only227.

## Initialization and retained behavior

`SpellManager` now loads persisted spells, all action sets/shortcuts/nodes and the saved active spec before grants. Its explicit idempotent `Initialise()` must run after the owner assigns `SpellManager`; production `Player.Initialise` does so immediately. Calling it before assignment fails explicitly. Every in-repository direct constructor consumer is adapted. This avoids both the original early constructor grant and upstream's still-unassigned owner reference/late active-spec assignment.

Active node changes and successful spec changes reevaluate normal grants. Inactive node edits wait until activation. Existing learned class spells, shortcut state, learned-passive classification, save requests, cast guards, entitlement defaults and AMP point/persistence policy remain intact.

## Narrow augmentation runtime

Desired augment spell IDs come from the active set and are deduplicated across nodes/specs. Supported spells have zero cast, duration and channel timing, and nonempty exclusively permanent, immediate UnitPropertyModifier effects or exclusively held ModifySpellCooldown effects. Mixed families remain excluded. Telegraph/unknown target flags and effect persistence/suspension predicates are excluded. Normal self-targeted non-user casts retain prerequisite and effect-handler behavior, with explicit global cooldown bypass.

**Real-data correction:** the earlier group0-only filter passed synthetic property tests but excluded all 206 actual property-only augmentation spells in the extracted build16042 dataset. Those spells use group2; its exact row is `Id=2, StackCap=0, StackTypeEnum=4`. The gate now permits group0 compatibility and only this exact table-backed group2 contract. Missing rows, different identities, positive caps and other arbitration types fail closed. The existing property handler only invokes cap arbitration for positive caps; no unknown arbitration rule was introduced.

The original ownership guard recorded `(Property, EffectId)` after a successful cast and intersected those keys with current instances of the same source spell. The later [instance check](#property-ownership-when-an-effect-key-is-reused) covers same-spell key reuse. A pre-existing same-spell modifier keeps its existing owner, and a different spell reusing a key retains its modifier. Shared augment IDs remain applied until the final active node stops requiring them. Augmentations stay outside the learned-spell database.

Pure held-cooldown ownership uses the actual immutable `SpellCooldownModifier` objects installed by the normal cast/handler73 path. Reference identity matters: a value-equal replacement with reused effect and casting IDs is still a different owner and survives removal. Pre-existing same-spell records prevent recasting and are never claimed. Removal drops only owned records and does not undo already-running cooldowns. The existing native effect73 scope/key/mode/magnitude interpretation, shared-node handling, wire behavior and lifetime rules are reused without Spell/UnitEntity changes.

World entry, spec/node changes and a bounded one-second retry restore missing supported properties or held cooldown records after unavailable/dead/failed-cast states without stacking successful instances.

## Actual build16042 augmentation inventory

Read the extracted `EldanAugmentation`, `Spell4`, `Spell4Effects` and `Spell4StackGroup` records. There are 910 nodes, 781 nonzero augmentation references and 556 distinct augmentation spells. Largest pure families: Proc218, UnitPropertyModifier206, Fluff57. The supported property family covers425 node references. The only pure cooldown spell is node881 (Stalker) -> Spell59417/base38718/effect142298: caster-targeted, permanent flag2, scope3, node182, mode2, magnitude -3000ms. All207 supported spells use the exact group2 row above and satisfy the retained timing/target/persistence guards. This is eligibility coverage, not a claim of complete AMP retail validation.

Actual property witness: Stalker node770 -> Spell56845/base36464/effect135617, BaseCritChance +0.01, immediate permanent caster effect. The spell is also referenced by nodes23/175/327/410/626 in other classes. Dedicated tests embed its raw node/effect records, the cooldown node/effect records, and the raw group2 row; they are byte-for-byte checked against extracted data during this investigation.

SHA256 inputs:

- EldanAugmentation.tbl: `f8bd7b45dbbbf74bd31be239fc85173e7cb2362ef4118f9e6a95e5dad7058b4e`
- Spell4.tbl: `303c03cf9ee268b1556cc65c2b56c62d21d083504a7a16eac2cac80d34f1914d`
- Spell4Effects.tbl: `ad49cb47af98a4b9b85e5f715af85545a8b286fb3ca1f26ad8cf8ea09097b3ed`
- Spell4StackGroup.tbl: `7f74eff744de5f3fc9022a4fd6a54c33c9b9a5ba5eb7d9e185e873bd7fbfe1a0`

## Verification and remaining scope

`AmpLifecycleTests` covers initialization, saved active/inactive specs, real prerequisite registration and row dispatch, objectId/value distinctions, high IDs, pending deletion, full/section/single resets, shared IDs, dead/out-of-world retries, failures, ownership/replacement collisions and SQLite save/remove/relog. Existing learned-passive, grant, action-set save and basic native-prerequisite suites run alongside it. Exact latest counts and TRX are recorded in the external `remaining-goal/AMP_RECONCILIATION.md` report.

`AmpTableAugmentationTests` adds the actual property/cooldown raw witnesses, exact passive group identity/cap/type gates, login and active/inactive/shared spec transitions, removal/recast/retry, unchanged running cooldowns, pre-existing/unrelated/value-equal replacement ownership, unsupported timing/mixed/scope/mode payloads and SQLite node save/remove/relog without learned spells or entitlements. Combined focused gate:146 passed, zero failures, skips, warnings or errors.

Mixed, proc, timed, periodic, delayed, channeled, other stack-group contracts, property-conversion and conditional-persistence augment families need effect-wide runtime ownership and retail semantics before automatic application is safe. Server authority for the inlaid unlock list remains open.
