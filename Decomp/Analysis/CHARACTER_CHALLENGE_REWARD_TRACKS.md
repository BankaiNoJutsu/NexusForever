# Character Challenge reward-track foundation

Later checkpoint7959: [live producers](CHARACTER_CHALLENGE_LIVE_PRODUCERS.md) connects mapped count-based earning and legacy compatibility. The earlier scope below is historical; real socket earning/claim and native UI remain open.

Latest30 October continuation: [Character reward execution/recovery](CHARACTER_CHALLENGE_REWARD_EXECUTION.md)
passes7,462 tests with zero build warnings/errors,31 unchanged compatibility
probes and98 exact source MySQL cases (59 repository,39 executor; one pure
constructor guard). Immutable commands, fresh whole-unit provider execution,
retained-claim resolution, database pinning and single-use resumption proofs are
implemented. Actor queue/save ownership, baseline adoption, live earning and
routing remain open. Runtime stays frozen7,364; F-033 remains partial.

Earlier30 October continuation: [Character reward repository units](CHARACTER_CHALLENGE_REWARD_REPOSITORY.md)
pass7,423 tests with zero build warnings/errors,31 unchanged compatibility probes
and59 exact source MySQL cases. Activation/medal/completion/selection/reload/claim
units cover all16 complete catalogs, with root-row serialization, durable receipts,
retained cycles and inventory-backed delivery. The actor queue/provider execution
strategy, Challenge baseline adoption, live earning and packet routing remain
unwired. Runtime stays frozen7,364; no new native UI claim. F-033 stays partial.

Earlier30 October continuation: [inventory reward delivery](CHARACTER_REWARD_INVENTORY_DELIVERY.md)
passes7,364 tests with zero build warnings/errors,31 unchanged compatibility
probes and38 final MySQL inventory-owner cases. Whole-delivery reservation,
receipt/item enrollment, recovery, publication, expiration deferral and Player
save/economy preflight are implemented. Runtime-discovered Auth realm refresh
now preserves health for unchanged endpoints. Production Challenge repository,
eligibility/earning/claim routing and native UI remain open. F-033 stays partial.

Current isolated runtime: World48568/Auth48880 use frozen7,364 and pass200/200
audited cycles, including a70-second mixed run across the Auth definition refresh.
Seven other services retain their identities; six queues drain naturally. Defaults,
original data and private configurations are preserved. Computer Use remains stopped.

30 October continuation: [inventory preparation guards](CHARACTER_REWARD_INVENTORY_RESERVATIONS.md)
pass the full7,254-test gate,31 unchanged compatibility probes and11 isolated
MySQL save-validation cases. Aggregate receipt/item enrollment and delivery remain
open; the earlier World7182 runtime and9 migration cases below retain their scope.

2026-10-29. **The persistence, catalog, schema, modifier snapshot and presentation foundations are implemented and verified. Live character-track earning, the character reward repository and transactional claim delivery are not wired yet.** Existing Challenge direct prizes and entitlement defaults remain unchanged. This is not a complete Challenge reward-track feature or a retail-parity claim.

## Implemented scope

### Challenge persistence

[`ChallengeManager.Persistence.cs`](../../Source/NexusForever.Game/Challenges/ChallengeManager.Persistence.cs) retains changed Challenge state until the corresponding SQL snapshot actually flushes and reaches terminal commit. It preserves later mutations and timer movement, including timer changes which do not call MarkDirty. A commit without a flush does not acknowledge staged state; a later unflushed restage cannot replace the acknowledged SQL snapshot.

Failed saves, rollback/disposal and conservative savepoint outcomes retain retry intent. Same-context restaging after savepoint rollback is rejected; a fresh context reconciles the finite known states before retrying. A stale loaded owner or overlapping live owner is rejected rather than silently skipping or overwriting a Challenge row. Shared-context tracked rows must satisfy the loaded baseline contract. Completion releases leases without dropping later dirty state.

The lease remains process-local. Its configured database identity is provider plus connection-string keys normalized in order/case with passwords omitted. It is not a durable multi-process concurrency token or a medal receipt. The new nullable `ActiveAttemptId` field is preserved by persistence; this foundation does not allocate a live attempt or derive one from old completion counts.

### All sixteen complete catalogs and explicit cycle policy

[`CharacterChallengeRewardCatalog.cs`](../../Source/NexusForever.Game/Challenges/Reward/CharacterChallengeRewardCatalog.cs) supports **all sixteen complete type 3 catalogs**, retaining all **159 reward rows and 475 Item2 choices**, including their actual quantities. It explicitly identifies five empty catalogs 29/30/31/32/34 as unsupported fallback, without substituting another zone or breaking Loyalty.

The catalog copies interpreted values into immutable definitions and selection blueprints. RewardPointFlags determine each cost ordinal; final-Casque rules preserve the claim-bit and native wire-list positions. Arcterra 36 is `[536,538,539,537]`, not sorted by row ID. Duplicate cumulative costs remain distinct positions. Invalid partial catalogs, identities, masks, flags, choice layouts and unsupported deliveries fail closed. Persisted selections restore without resampling or compacting missing slots. Safe lookup methods distinguish supported ownership from unknown/empty tracks.

The fork uses this **explicit upstream-style server policy**: sample uniformly once from each eligible canonical bucket per durable cycle; reset only after every slot is claimed; subtract the maximum once and carry excess points. Production sampling uses `RandomNumberGenerator.GetInt32`; tests inject deterministic selectors. The pure transition rejects invalid claim bits, uint wire-point overflow, insufficient claimed-cycle points and cycle overflow before sampling. Claiming the final reward first does not reset the cycle. These rules are testable server policy, **not recovered retail RNG probability/reset timing**. No live producer currently calls this transition.

### Frozen whole-multiplier point evaluation

[`ChallengeRewardModifierSnapshot`](../../Source/NexusForever.Game.Abstract/Account/Reward/ChallengeRewardModifierSnapshot.cs) captures the whole property 4/data 3 multiplier and source revision atomically through the additive `IRewardPropertyManager.CaptureChallengeModifier` capability. The real manager verifies the specialized additive-scalar property owner. Unsupported custom managers fail safely through the compatible default hook.

The existing raw delta GetValue API remains unchanged. The whole multiplier includes identity 1, actual Signature/premium sources, active source-aware spells and only committed entitlement 44. One medal increment uses float32 multiplication and truncation, with nonfinite/negative/overflowing results rejected. Evaluate each 100-point medal independently; do not re-award cumulative 100+200+300 totals. The future medal receipt must retain the evaluated amount and revision rather than recomputing it on retry. This slice provides that frozen value capability; it does not issue live medal receipts.

### Additive Character schema

The forward MySQL migration `20261029215652_CharacterChallengeRewardTracks` and SQLite migration `20261029215707_CharacterChallengeRewardTracks` add nullable `character_challenge.activeAttemptId` and five tables:

- `character_challenge_attempt`: character/attempt identity, source Challenge/track, reward mode, earned mask and terminal marker.
- `character_challenge_medal_receipt`: attempt+medal identity and frozen points/multiplier/revision.
- `character_challenge_reward_track`: character/track owner, cycle, uint points, revision and active presentation selection.
- `character_challenge_reward_slot`: persisted cycle+ordinal selected reward, required points and claimed state.
- `character_challenge_reward_claim`: cycle+ordinal claim identity, selected choice and versioned frozen delivery JSON.

[`CharacterChallengeRewards.cs`](../../Source/NexusForever.Database.Character/Model/CharacterChallengeRewards.cs) and context/model snapshots define the current graph. Composite ownership, receipt uniqueness and foreign-key cascades are tested. Historical reward-track models in old migration designers are not imported as current runtime state. The migration creates **no historical attempts, points, claims or items** and grants no entitlements. No account Loyalty schema or default values are changed by this migration.

### Actor-scoped replacing snapshots

Native 07D3 replaces **all** reward-track state. [`RewardTrackPresentation`](../../Source/NexusForever.Game/Reward/RewardTrackPresentation.cs) caches account and character contributions for one actor and composes their defensive copies into a full reload. Account readiness and required character readiness differ from explicitly loaded-empty data; cache-only publication updates later composed reloads without emitting a premature replacing packet.

Invalid wire shapes, duplicate/cross-owner IDs and multiple active tracks within one contribution are rejected atomically. One active account track and one active character track coexist. Input/queued-packet mutation cannot corrupt the cached next snapshot. Session/account/character replacement, closing logout, actual pending NetworkSession disconnect and explicit invalidation reject stale publication. Player Cleanup/Dispose invalidate the lazy owner permanently, even if it is first requested after closure.

Real `AccountLoyaltyManager.SendInitialPackets`, ChooseTrack and ClaimReward now publish through the actor capability; custom null-capability paths retain the old fallback. SendUpdates caches its complete account contribution without an extra reload, then checks captured state/actor/session/current lifetime before each incremental packet. Reentrant actor changes stop the old tail. A reentrant real ChooseTrack on the same manager also stops the old state batch and produces final reloads reflecting the newer durable selection. Existing rotation service/grant rows are not repurposed.

This does **not** create or load a real character-track contribution yet. The future character owner must call ExpectCharacterTracks before the initial replacing snapshot and publish its loaded contribution; a missing contribution cannot silently mean loaded-empty.

## Verification

The final full-solution build completed with **0 warnings and 0 errors**. The final suite passed **7,182/7,182**, **0 failed and 0 skipped**, bound to [challenge-foundation-final.trx](I:/GIT/NexusForever-assessment-report/remaining-goal/challenge-track-foundation/challenge-foundation-final.trx), SHA256 `8327B334840DAFB3BD86DA069BB0990F60989E377E3176EC78A029250F0BAD3A`.

| Focused slice | Passing cases |
|---|---:|
| Challenge persistence durability | 29 |
| Complete Challenge catalogs/selection policy | 56 |
| Character reward schema | 8 |
| Pure presentation/real lifetime controls | 30 |
| Actual Loyalty/public-entrypoint presentation integration | 11 |
| Medal arithmetic/modifier snapshot | 21 |
| Challenge property controls | 40 (existing 32 plus 8) |

The retained Challenge persistence negative control against frozen 7019 has **10 failed and 2 passed out of 12**, under [challenge-durability-baseline](I:/GIT/NexusForever-assessment-report/remaining-goal/challenge-durability-baseline). It demonstrates the original dirty-intent failure; there is no fabricated baseline for the newly introduced catalog.

The initial combined 7173 run had 7171 passes and two fixture-assumption failures, retained in [challenge-foundation-full.trx](I:/GIT/NexusForever-assessment-report/remaining-goal/challenge-track-foundation/challenge-foundation-full.trx). One delivery assertion incorrectly included generic `save-unit:*` witnesses; it now checks exactly one logical `loyalty:1:9:0` receipt. The taxi migration test incorrectly assumed TaxiNode was the newest migration; it now targets that migration and its actual predecessor while retaining its original preservation assertions. An earlier catalog assertion also incorrectly normalized actual choice quantities to 1; it now compares every actual row/index quantity, preserving real counts such as 2 and 5.

Independent review found and corrected a real disconnect/lifetime gap: pending disconnect and closing logout were initially accepted, and an incremental loop could continue after actor/state replacement. New real pending-disconnect, logout, reentrant actor and reentrant same-manager ChooseTrack controls cover that correction. See [CHALLENGE_TRACK_FOUNDATION_INDEPENDENT_REVIEW.md](I:/GIT/NexusForever-assessment-report/remaining-goal/CHALLENGE_TRACK_FOUNDATION_INDEPENDENT_REVIEW.md) and [CHALLENGE_REWARD_SCHEMA_REVIEW.md](I:/GIT/NexusForever-assessment-report/remaining-goal/CHALLENGE_REWARD_SCHEMA_REVIEW.md).

## Isolated real-MySQL schema/receipt proof

[MySqlCharacterRewardSchemaProof_29h1](I:/GIT/NexusForever-assessment-report/remaining-goal/MySqlCharacterRewardSchemaProof_29h1) passed **nine aggregate checks** on a new owned schema `nexus_assessment_character_rewards_proof_29h1`. The source clone contained **54 tables and 22,530 rows**. The proof freezes **169 DLLs and 190 relevant source files**, bound to the actual 7182 TRX/hash above; manifests and loaded-assembly identity are retained alongside `result.json`.

Raw schema graph insertion does not validate gameplay eligibility or selected-row ownership; those remain explicit repository obligations. The next-cycle repeated-row witness exercises key identity, not a public claim request.

The executed checks cover populated clone fidelity; additive migration and idempotent second application; no retroactive attempts/points/claims/delivery; actual provider attempt/track/slot/frozen-medal graph; receipt+Item+claimed-bit rollback then commit; receipt survival after item consumption and distinct next-cycle identity for a repeated static row; duplicate cycle-slot receipt rejection; owned synthetic graph cleanup/cascade; and preservation of every old copied field and the owned source tables.

The proof's final source audit reports **SourceChangedTables=0 across 54 source tables during the proof**. This is a time-bounded preservation result, not a promise that the parent will never later apply an authorized migration to the isolated runtime schema. The proof's own `LiveRuntimeMigrated=false` and `NativeGameplayClaim=false` remain accurate.

This proves real provider schema/constraint/receipt graph behavior. Its direct transactional Item/receipt exercise is **not an invocation of a completed Game claim repository, live Challenge producer, inventory reservation/publication API or native gameplay**. At that foundation checkpoint those owners did not exist. The later inventory aggregate is separately verified above; these nine checks still do not prove production character reward delivery.

## Remaining connected implementation

The merge-capable reservation proposed in [CHARACTER_REWARD_INVENTORY_RESERVATION_PROPOSAL.md](I:/GIT/NexusForever-assessment-report/remaining-goal/CHARACTER_REWARD_INVENTORY_RESERVATION_PROPOSAL.md) is now implemented and tested by the [inventory delivery continuation](CHARACTER_REWARD_INVENTORY_DELIVERY.md). It preserves compatible stack merges and new-stack allocation, stages frozen quantity overrides and receipt together, resolves ambiguous outcomes and publishes only after commit.

The [repository units](CHARACTER_CHALLENGE_REWARD_REPOSITORY.md) now cover all sixteen supported catalogs. The executor/provider strategy is now separately verified. The next connected slice is actor queue/save ownership, per-attempt medal production, selected-row claim routing, committed baseline adoption and lifecycle integration across **all sixteen supported catalogs**. An activation-time LegacyDirect/CharacterTrack mode must preserve old active attempts, unsupported empty catalogs, existing prizes, challenge lifecycle/quests/cooldowns and account defaults, while preventing direct-prize plus track double grants. Historical completion counts are not a medal bootstrap. Remaining timer/objective adapters and accepted repeat policy must be explicit; do not replace the scope with a single-track demonstration or call this foundation complete gameplay.

The earlier native evidence remains in [CHALLENGE_REWARD_TRACK_EVIDENCE.md](I:/GIT/NexusForever-assessment-report/remaining-goal/CHALLENGE_REWARD_TRACK_EVIDENCE.md) and [REWARD_CLAIM_INDEX_CONTRACT.md](I:/GIT/NexusForever-assessment-report/remaining-goal/REWARD_CLAIM_INDEX_CONTRACT.md). Baseline 100/200/300 cumulative medals and property 4/data 3 ownership are established; no new native analysis was required for this foundation.

## Runtime and UI boundary

The reviewed additive migration was applied twice to the owned Character database: **54→59 tables**, with all old fields preserved, one history row added, no backfill and no pending model change. Its five new tables remain empty after replay; all old attempt markers remain null. See [OwnedCharacterMigration7182/result.json](I:/GIT/NexusForever-assessment-report/remaining-goal/OwnedCharacterMigration7182/result.json).

Frozen **World7182/PID31676** passes **26/26** audited admission/bootstrap/ACK request/reply/assigned-disconnect/cleanup cycles:1 Eastern first session,24 balanced mixed-world samples and1 existing-account sample. There are10 Eastern and16 NPE cycles, zero errors, no unfinished/late/duplicate admissions,9 owned services alive,8 other service identities retained and6 drained queues. Three reused canonical fixtures retain15/17 with no44/unearned rewards; all12 synthetic accounts are offline/null-active. Original77 tables,43 copied legacy characters and13 old entitlement rows retain their checks.232 World files and97 harness files are frozen;231 World files were staged while preserving private configuration. See [RUNTIME_CHECKPOINT7182.md](I:/GIT/NexusForever-assessment-report/remaining-goal/RUNTIME_CHECKPOINT7182.md).

All31 original compatibility probes pass against frozen7182 references. A pristine pre-upgrade baseline, `nexus_assessment_character_rewards_baseline_29h1`, retains the original54-table/22,530-row DDL/data with exact before hashes for future migration replays. The populated clone proof preceded the intentional owned-runtime migration; its source-unchanged claim is scoped to that execution. Native Challenge UI validation has not been performed; computer use remains stopped after Escape. Build, unit/provider results and ordinary protocol replay cannot substitute for a native Challenge earning/claim/reset observation.
