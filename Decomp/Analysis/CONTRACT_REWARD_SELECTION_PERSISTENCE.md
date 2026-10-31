# Durable Contract selections and legacy schema repair

Latest continuation: [Contract track choice through the registered actor queue](CONTRACT_REWARD_TRACK_CHOICES.md)
qualifies checkpoint9122: **9,122 full tests**,32 new/674 focused,1,172 MySQL runs
(1,124 prior +24 choices +24 retry), two model gates and31 compatibility probes,
with zero build warnings/errors. Registered07CF routes Contract0/1 through the
real Quest owner and existing queue. Published/baseline/busy/foreign guards precede
capture; active flags and optional legacy-cycle selections commit together. Choose
provenance records an operation, never fabricated acceptance/history. Other active
types and dirty Quest/Challenge intent survive; lost-ack recovery cannot reroll.
No schema/default changes or runtime staging: the8378 backend,9 identities,
originals/config/schema/defaults15:1/17:1/no44 and6 drained queues remain preserved.
Claims/reset/Auth handoff/full-table/socket/deployment/native gates and the pending
reset-policy preference remain. Computer Use stays stopped; the full Goal is active.

The following records the earlier9090 selection/repair checkpoint.

Checkpoint9090 persists the canonical prerequisite-filtered Contract reward row at
every cost ordinal during the first eligible supported completion of an unselected
cycle. The rows join the same Character transaction as ordinary prizes, Contract
points/receipt and any reentrant Challenge work. Later earning and relog reuse them.
The full build has zero warnings/errors; **9,090 full tests**,645 focused cases,
1,124 exact-source MySQL cases, two model gates and31 compatibility probes pass.

This slice adds275 regressions:255 static selection/provenance cases,13 connected
live selection cases, one standalone selected-cycle preservation case and six
schema/migration cases. MySQL reruns1,097 prior cases and adds13 live cases plus
one standalone case and13 configured-retry runs. The static and schema tests are
not counted as MySQL repository cases. All242 static reward rows are capture/
restore-tested, preserving their726 choices. All51 actual Contract-source cases
now verify atomic selected-slot persistence and publication. Ordinary reward
fixtures still omit full retail fields and use other-manager adapters.

## Selection policy and frozen eligibility

The explicit server policy is `contract-eligible-uniform-v1`. It samples one row
uniformly from each ordinal's evaluated eligible pool using a cryptographic random
index. This policy is not claimed as retail RNG or proof of retail selection timing.
The actor evaluates distinct track/row prerequisites and captures its selection
after acquiring exclusive Quest/Challenge ownership, before ordinary gameplay
callbacks. No actor/prerequisite callback or new random sample runs on the SQL worker.

`ContractRewardSelection` freezes the selected IDs, satisfied prerequisites, current
cycle, accepted generation and policy/version as canonical immutable JSON. Restore
validates the full ordinal count, static owners, captured prerequisites, selected
row positions and canonical representation. Caller collection changes and later
eligibility changes cannot rewrite the plan. Ineligible rows never become a class,
rank or unconditional-final fallback.

Missing reward eligibility or an unavailable custom prerequisite capability keeps
ordinary prizes and point credit intact and leaves the cycle unselected. A later
eligible completion can initialize it without resetting existing points/cycle/
active state. A selected cycle is never sampled again because the actor levels up,
changes eligibility, earns more points or relogs. Selection reset/repeat timing is
a separate remaining policy and implementation requirement; a preference question
is pending, with independent work continuing.

## Atomic rows, provenance and publication

Both provider migrations add `character_contract_reward_slot` and an explicitly
false `selectionInitialized` track marker. Each slot has Character/track/cycle/
ordinal ownership, the selected canonical reward ID, cumulative cost and frozen
selection JSON. Zero cycle and ordinal are real keys, never generated identities.
The FK follows the track's Character ownership and cascades with that owner.

Upgrade creates no selections, points, items, receipt history or entitlements.
Existing unselected point rows remain valid. The marker distinguishes a legitimate
old unselected cycle from a selected cycle with deleted/missing slots: the latter
quarantines and cannot silently reroll.

The normal completion command carries the immutable plan and its exact loaded
selection baseline. A track created/changed during Auth cannot silently become a
new baseline. The normal unit validates the plan, adds all slots and the marker,
and seals their full targets with the point receipt and ordinary reward graph.
Tampering with a selected ID, provenance, marker or row enrollment rejects the whole
unit. Failed/uncertain tasks require reload rather than callback or selection replay.
Only successful settlement publishes the final selected IDs alongside committed
Contract points, Challenge and Loyalty. Claimed/unlock history remains uninferred.

The loader validates complete current cycles, cost/ordinal/owner/provenance agreement,
and refuses orphan/future cycles. Snapshot comparison includes the deep immutable
slot contents; equal later SQL loads retain the published snapshot identity.
The standalone point repository preserves and observes existing selected rows in
its completion and duplicate results, but refuses a new selection-bearing command:
new gameplay selections belong to the production normal-save owner.

## Historical MySQL migration gap and forward repair

The first real populated migration check failed on missing `character.castingOptions`.
Source inspection identified two historical classes with neither registration
attributes nor a designer: `20260521000000_CharacterOptions` and
`20260522153000_ResidenceDecorState`. Their fields appeared in model snapshots but
were absent from a fresh MySQL migration chain. EnsureCreated-based repository
tests and a zero pending-model difference could not prove this upgrade path.

`20261031033000_RepairLegacyCharacterColumns` is a new discoverable forward migration.
It conditionally adds missing casting/combat-log/shared-Challenge settings and
active-prop/decor-data/hook columns, using the provider pattern already used by
CostumeMessageChanges. Present columns and values are untouched. The historical
classes are not registered retroactively, avoiding duplicate-column failures on
imports or EnsureCreated deployments. Down is intentionally nondestructive because
the repair cannot prove ownership of a pre-existing column. SQLite's initial
migration already has all eight columns and needs no equivalent repair.

## Provider and migration evidence

Evidence root: `I:/GIT/NexusForever-assessment-report/remaining-goal/contract-selection`.
`full3/full.trx` binds9,090 tests; `focused-final2/focused.trx` binds645. The final
frozen proof is `final-review2`:5,830 Source/load-tool paths,22 changed paths since
8815,169 DLLs and33 copied test/helper sources. `MySqlProof/result.json` retains
1,124 passing per-case SQL snapshots including the new Contract slots on fresh
schema `nexus_assessment_contract_sel_31a2`, retained after scoped synthetic-owner
cleanup. Both provider model gates have no pending model changes or relational
model differences. Auth binaries/defaults are unchanged.

`UpgradeProof3/result.json` qualifies an actual populated MySQL migration from the
receipt/point predecessor, latest migration replay and all-current-model column
coverage on fresh schema `nexus_assessment_contract_sel_up_31a3`. All60 old business
tables/68 row projections are hashed unchanged across upgrade:59 migration-seeded
reference rows plus nine synthetic seeded
rows include Characters, accepted Quest/objective, currency, receipt/points and
retained housing/decor. The proof then sets nonzero option/decor values and reruns
the conditional repair SQL, hashing the full current projections unchanged. It
also verifies zero composite keys, owned cascade and unrelated Character retention.
This is a synthetic populated upgrade, not copied retail/runtime upgrade proof.

Earlier failures are retained: the test fixture initially omitted its Quest
prerequisite dependency; UpgradeProof/2 exposed the missing historical columns.
The corrected source and final frozen checks pass. The earlier9088 core proof also
passed1,124 cases, but the legacy discovery required a new build/freeze and rerun.

## Runtime and remaining full Goal

Current source/migrations are not staged into the separately qualified8378 runtime.
All9 service identities,6 naturally drained queues, original77 tables/21,989 rows/
43 copied legacy Characters, existing Contract/Challenge data and private configuration/
schema are preserved. Entitlement defaults remain15:1/17:1, with no automatic44 grant.
The original checkout is clean and branch/head are unchanged. No commit/push was made.

Choose/claim routing and stale/busy/unpublished guards, explicit repeat/reset/carry,
Item2 quantities/binding, final2,500,000 credits and AccountItem2485/2486 compensation
remain required. Account-side delivery needs an idempotent Auth handoff/outbox and
crash/ownership recovery. Full-table ordinary rewards/pushed items, copied-upgrade
preservation and connected objective/completion/earn/choose/claim/relog/reset socket
qualification remain before deployment. The running schema still lacks the new
Contract tables and must not receive this source before the complete upgrade gate.

Zone35, unknown Challenge producers, broader composite producers, AMP/combat/upstream/
content and all other assessment requirements remain in scope. All167,484 content
rows remain `not_retail_complete`. Computer Use stays stopped; native UI requires
explicit resumption. The June19 Q5597 bundle is unavailable and is not requested again.
The complete assessment Goal remains active.
