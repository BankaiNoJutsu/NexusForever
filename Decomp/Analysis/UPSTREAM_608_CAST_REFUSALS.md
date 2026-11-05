# Upstream #608: cast refusal reconciliation

The later [action-bar lookup result](#action-bar-cast-resolution---5-november-2026)
fixes ordinary/position stale-slot feedback with21 new/all116 scoped passes.
Broader casting and native UI remain open. Original upstream reconciliation
is implemented at the pinned head below. The2 November
all-state refresh found no updates; see the
[provider milestone record](DESTINATION_PROVIDER_QUALIFICATION.md). The native
and separate follow-up limits below remain open.

Reviewed on 31 October 2026 against
[`7db12d9a7c694bc5e66d9e0f4cdbc1b82155e69b`](https://github.com/NexusForever/NexusForever/pull/608),
including all 11 changed-file patches and both commits. The second commit adds
the item-use refusals that the description still lists as a TODO. The earlier
same-day55-open-PR/head inventory found only #608 new since the saved assessment;
#598 merged and all other open heads are unchanged. See
[PR triage](assessment-snapshot/PR-TRIAGE.md).

The interim31October inventory had56 open PRs, including [609 source reconciliation](UPSTREAM_609_COMMUNICATOR_RECONCILIATION.md). On1November609 closed unmerged, returning the inventory to55 open PRs with608 newest open. All55 earlier heads, including608, remain unchanged. The20:24UTC all-state incremental refresh finds no PR updated since19:26:49UTC; its response is retained in commodity-returns/upstream-final.json.

## Existing fork behavior retained

The fork already propagates `ClientContextToken`, correlates failures from
`Spell.Cast`, and sends one owner-only07FD acceptance with primary target, float
yaw, pose and prepared telegraphs immediately before visible spell start.
`SPELL_CAST_CONFIRMATION_07FD.md` records the exact-build consumer and pending
counter proof. A zero token is valid: explicit `HasClientCastRequest` owns the
request. The upstream nonzero-token guard is therefore not imported. Existing
packet names/aliases, widths, cooldown timing, interpreter and parent/observer
boundaries are preserved.

## Reconciled source paths

- `UnitEntity` now replies on the pre-Spell death, disabled base/spell and
  occupied active-slot returns. Existing return values, including `SpellRemoved`,
  and disabled-spell system messages remain. The shared `ClientCastRequest`
  sender requires a parsed root request and a non-loading owner, including token
  zero. Existing Spell-owned validation failures are not sent a second time.
- `ClientItemUse` passes explicit parsed-request presence into `ItemUseHelper`.
  Missing item/definition/special rows, absent activation spell, failed
  prerequisites, exhausted stack/charges and unavailable inventory leases receive
  correlated refusal without consumption. A missing item can be an ordinary
  double-use/move race and no longer forces disconnect. Failed client-item
  prerequisites use the existing typed `SpellPreRequisites` result; context-action
  generic-error behavior remains separate.
- Cast-before-consume ordering, stackable-charge priming and currency treasures
  are retained. A currency treasure still grants its existing currency; its
  non-spell path retires a parsed pending token with `ItemNoSpellId`/spell0.
  Context actions have no parsed cast token and now leave request presence false,
  so they cannot invent a zero-token07FD acceptance. Their supported item/currency
  actions remain available.

This ports the useful behavior into the fork's architecture; it does not import
the expedition spell factory, change entitlement defaults or implement all of
the PR's separate transactional/threshold/repress follow-ups.

## Action-bar cast resolution - 5 November 2026

Parsed `ClientCastSpell` and `ClientCastSpellPosition` requests now receive one
existing `SpellUnknown` result if the requested ability item or learned-spell
entry has disappeared. The reply preserves the original32-bit context token,
including zero; spell ID0 represents the unavailable lookup. Both public
handlers return without attempting a cast after failed resolution. The
[portable record](coverage/actionbar-cast-resolution-20261105.json) binds the
three Source/test files, frozen baseline and final assemblies, checks and reviews.

Lookup captures the original Player and verifies its session and the session's
bound Player before returning a spell or replying. Loading suppression remains
owned by `ClientCastRequest.SendFailure`. A removed/replaced actor does not
inherit the old request. Accepted resolved tier, target/position, root request,
combo completion and downstream refusal ownership retain their existing path.
No packet fields, IDs or widths changed. The continuous packet has no context
token and is untouched; this does not invent a zero-token continuous request.

Eight corrected public probes fail against the previously qualified unchanged
WorldServer DLL: both decoded request forms throw `InvalidPacketValueException`
for missing item/book lookups, with zero and nonzero tokens. All eight pass after
integration. Early raw diagnostics include four fixture failures caused by using
UInt64 for `IItem.Id`, whose contract is UInt32. They remain separate from the
corrected eight production negatives. The frozen DLL is restored to its candidate
owning copy before candidate/final execution.

All21 new cases pass within116 unique affected cases:106 casting/item-use
controls plus10 disjoint selected/floating-action-bar controls. They cover
accepted zero/max tokens, resolved tiers, targets/positions, loading/removal,
replacement actors/sessions and single downstream replies. Existing confirmation,
combo, continuous-cast, item/decor/loot-bag and selected-spell controls pass.
Owning WorldServer and test-only builds have zero warnings/errors. No full
solution or full suite ran. An isolated published-base tree excludes concurrent
PvP/prerequisite/group/native changes; explicit historical clean-filter and
BOM/newline checkout variants are retained without rewriting Source.

The latest20 all-state upstream heads/bases/states are unchanged, including608
open and609 closed/unmerged. This is an additional fork Source lookup result,
not a claim that upstream implements this slice or that its open PR merged.
Malformed static metadata, broader threshold/repress/transactional casting,
post-acceptance item settlement and provider/socket/deployment/native/retail
qualification remain open. No native client or Computer Use ran.

## Standards

The three-file actionbar candidate has0 findings. The change reuses the existing
correlated-refusal path, checks original Player/session ownership, and preserves
successful casting and downstream reply ownership. Decoded public-request tests
cover missing lookups, zero tokens and replacement boundaries. No actionable
smell-baseline issues were found.

## Spec

The three-file candidate has0 findings. Missing items and learned-spell entries
receive the existing refusal with the original token; explicit request presence
preserves zero. Both handlers stop after failed resolution. Original ownership
and loading suppression are retained, accepted target/position/tier/combo paths
stay unchanged, and downstream refusals are not answered twice. Continuous
casting and malformed metadata remain outside scope. Full read-only reports are
retained with the portable record's local evidence links.

Final findings: Standards0, Spec0; no outstanding issue within either axis.

## Historical verification and limits

Sixteen selected regressions fail against the pre-change bank checkpoint's
Game/WorldServer binaries. The Game SHA-256 is
`0ef36c570aa01fd5deb9b9be6bffc299fab8a4e06835b252ca4f802b5a8902d4`.
Frozen binaries and `pr608-baseline.trx` are retained in
`artifacts/continuation-20261031`. The earlier private-setter test compilation
failure is retained separately as `pr608-baseline-compile.log` and is not a
behavioral negative. The corrected test-only build used
`BuildProjectReferences=false`; the production Game hash still matched the frozen
bank checkpoint before executing the 16 failures.

The implementation passes **125 focused checks**, including27 new refusal cases,
existing confirmation/context-token tests, real UnitEntity early returns and
item/decor/loot-bag compatibility. The complete solution builds with zero
warnings/errors and **10,318 tests pass**, none skipped, under SDK10.0.401.
That includes all122 bank identity cases from the preceding10,291 checkpoint.
Current logs are `pr608-focused.trx`, `pr608-full-build.log` and `pr608-full.trx`
under that local evidence directory. `pr608-inputs.json` binds the Source files
and Game/WorldServer/test binaries held during the full run;
`current-verification.json` binds that inventory, results and retained evidence.

Evidence for new refusal adaptation is the upstream source and the fork's
existing mapped07FC/07FD request/packet contract. No new native consumer trace,
retail failure text, predicted-cooldown UI, threshold/repress or transactional
cast lifecycle is claimed. Malformed table references, every possible item-use
exception and post-acceptance item settlement are not newly qualified. Native
launch and Computer Use remain stopped. No deployment or push was performed.
