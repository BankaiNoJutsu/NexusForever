# AMP prerequisite and lifecycle reconciliation

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
- Type231 selects `14049d7b0` through the separate `+0x1a0` slot and scans a global ushort list at `+0x6df0/+0x6df4`. The old cached function label is misleading; the live dispatch and body identify a distinct list. This change registers only227 and does not equate231 with enabled AMPs.

## Initialization and retained behavior

`SpellManager` now loads persisted spells, all action sets/shortcuts/nodes and the saved active spec before grants. Its explicit idempotent `Initialise()` must run after the owner assigns `SpellManager`; production `Player.Initialise` does so immediately. Calling it before assignment fails explicitly. Every in-repository direct constructor consumer is adapted. This avoids both the original early constructor grant and upstream's still-unassigned owner reference/late active-spec assignment.

Active node changes and successful spec changes reevaluate normal grants. Inactive node edits wait until activation. Existing learned class spells, shortcut state, learned-passive classification, save requests, cast guards, entitlement defaults and AMP point/persistence policy remain intact.

## Narrow augmentation runtime

Desired augment spell IDs come from the active set and are deduplicated across nodes/specs. Supported spells have zero cast, duration and channel timing, and nonempty exclusively permanent, immediate UnitPropertyModifier effects or exclusively held ModifySpellCooldown effects. Mixed families remain excluded. Telegraph/unknown target flags and effect persistence/suspension predicates are excluded. Normal self-targeted non-user casts retain prerequisite and effect-handler behavior, with explicit global cooldown bypass.

**Real-data correction:** the earlier group0-only filter passed synthetic property tests but excluded all 206 actual property-only augmentation spells in the extracted build16042 dataset. Those spells use group2; its exact row is `Id=2, StackCap=0, StackTypeEnum=4`. The gate now permits group0 compatibility and only this exact table-backed group2 contract. Missing rows, different identities, positive caps and other arbitration types fail closed. The existing property handler only invokes cap arbitration for positive caps; no unknown arbitration rule was introduced.

Ownership records the actual `(Property, EffectId)` instances produced by a successful cast. Removal intersects those handles with the current source spell's instances and removes only those exact properties. A pre-existing same-spell modifier is neither overwritten nor claimed. A later replacement remains owned by its source, including a different spell reusing a concrete key. No augmentation is inserted into the learned-spell database and no global source-spell removal is used. Shared augment IDs remain applied until the final active node stops requiring them.

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

Mixed, proc, timed, periodic, delayed, channeled, other stack-group contracts, property-conversion and conditional-persistence augment families need effect-wide runtime ownership/retail semantics before automatic application is safe. They are intentionally unsupported here. Type231's separate global-list ownership is also unresolved; this is not a claim of complete AMP gameplay parity.
