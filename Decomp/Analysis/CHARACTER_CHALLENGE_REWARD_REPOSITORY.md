# Character Challenge reward repository units

Later checkpoint7959: [live producers](CHARACTER_CHALLENGE_LIVE_PRODUCERS.md) connects mapped count-based earning and legacy compatibility. The earlier scope below is historical; real socket earning/claim and native UI remain open.

Latest 30 October continuation: [Quest actual-commit durability](QUEST_PERSISTENCE_DURABILITY.md) now supplies committed scalar baselines/revisions, retained deletion and cascade restoration. Full7,599 tests,31 compatibility probes,232 exact-source provider cases and176 runtime cycles pass. Joining completion/Quest participants to the reward unit and live earning remain open. Earlier checkpoint evidence below keeps its original scope.

Latest 30 October continuation: [Typed request routing and published bindings](CHARACTER_CHALLENGE_REWARD_ROUTING.md) now wires type3 choose/claim through the queue, exact published snapshots and bound transactional inventory. Full7,563 tests,31 compatibility probes,196 exact-source provider cases and209 runtime cycles pass. Live earning and completion/quest durability remain open. Earlier checkpoint evidence below retains its original scope.

Latest 30 October continuation: [Actor queue and composed reloads](CHARACTER_CHALLENGE_REWARD_QUEUE.md) now connects the real Player's initial durable load, session-event adoption, bounded recovery and final-disposal cleanup. Full 7,517 tests, 31 compatibility probes, 153 exact-source provider cases and 209 runtime cycles pass. Live earning and type-3 choose/claim routing remain open. The earlier checkpoint evidence below keeps its original scope.

Latest 30 October continuation: [Challenge reward ownership/adoption](CHARACTER_CHALLENGE_REWARD_OWNERSHIP.md) now implements save/cleanup leases, uncertain-baseline resolution and authenticated committed adoption. Full 7,483 tests, 31 compatibility probes, 119 exact-source provider cases and 182 runtime cycles pass. The live actor queue, producers, composed presentation and routing remain open. The earlier checkpoint evidence below retains its original scope.

Later30 October continuation: [typed execution/recovery](CHARACTER_CHALLENGE_REWARD_EXECUTION.md) now owns whole-unit provider execution, fresh-context witness recovery and database-bound resumption. Full7,462,31 compatibility and98 exact source MySQL cases pass. Actor save ownership, baseline adoption, producers and routing remain open.

At the 7,423 checkpoint on 30 October 2026, the repository stages character-owned activation, medal, completion, selection, reload and inventory-backed claims for all16 complete catalogs. **The later executor supplies the provider strategy; live Challenge hooks, actor queue/save ownership, publication/baseline adoption and packet routing are still unwired.** This is an implemented transaction boundary, not complete Challenge gameplay. Existing direct rewards, historical null markers and entitlement defaults are unchanged.

## Transaction contract

`CharacterChallengeRewardRepository` requires a fresh untracked Character context and one caller-owned serializable transaction. On MySQL it locks the existing character row with FOR UPDATE before reading or initializing any attempt or track; SQLite's serializable write transaction orders independent contexts. It stages exactly one operation. The caller must retain its immutable command and inventory reservation across uncertainty, finish the original unit, resolve outcomes in fresh contexts and retry the complete unit through the provider execution strategy. A process-local Item lease is not a substitute for this database boundary.

The repository rejects partial/reentrant enrollment, foreign entities, a changed transaction, savepoint rollback, detached/deleted rows, changed scalar/original values and commit without a successful flush. Before explicit commit it reads each owned SQL row back in the same transaction and requires the complete staged snapshot. The source Challenge track/tier definition is frozen for that unit. Any staging or validation failure permanently rejects the context. Added EF originals are captured after legitimate staging adjustments; existing originals remain guarded. UTC receipt times are normalized to the schema's microsecond precision before enrollment.

## Operations

- Activation creates a stable attempt identity, records its source Challenge/track and explicit reward mode, and writes the active Challenge marker atomically. Supported/eligible catalogs initialize a durable canonical selection once; duplicate activation replays do not resample. Unsupported or explicitly disabled sources can retain LegacyDirect mode without inventing points or track slots. Existing active attempts cannot be replaced and the previous marked attempt must be terminal before a new activation.
- Medal credit requires the same live source attempt, ordered bronze/silver/gold receipt identity and the matching Challenge baseline/transition. It freezes one native100-point increment multiplied/truncated by the supplied whole modifier snapshot, increments the source track's progress/revision and acknowledges Challenge state in one unit. A duplicate receipt returns its original points/modifier rather than evaluating a new modifier. Final medals mark the attempt terminal. A positive-tier timeout can finish without erasing earlier medals or synthesizing another one.
- Selection validates the full known track revision set and prerequisites. It changes presentation-active ownership independently of earning ownership. A Challenge always earns on its source track. New selected tracks retain their insert state while the previous active track is updated.
- Claims validate catalog, prerequisite, valid choice, persisted selected row/cost, points, cycle and revision before allocating delivery IDs. Prepared reservation metadata must match the character and canonical Item2/count. Receipt, claimed bit, track revision and all Item rows stage together through the inventory delivery owner. Final-first claims are permitted. Only all-claimed cycles advance; maximum points are subtracted exactly once and excess carries. Old slots and receipts are retained, including when the same static row occurs in another cycle.
- Duplicate claims return the first durable receipt without delivering again, including after its Item rows were consumed. Another choice cannot replace it. Reload validates current ordinals/costs, claimed witnesses, the complete delivery payload's Item identity/owner/destination/quantity arithmetic and attempt/mask/medal/marker consistency. Historical null attempt markers do not create a ledger or backfill medals. Returned states are copied immutable snapshots.

`IInventoryRewardReservation` adds compatible metadata defaults and the real owner exposes its frozen character/Item2/count before Stage. This lets the repository reject a mismatched prepared delivery before allocating IDs. No general item-creation, satchel, bank or entitlement policy is changed.

## Verification

Evidence: `I:/GIT/NexusForever-assessment-report/remaining-goal/character-reward-repository/final-review`.

- Full `Source/NexusForever.slnx` build:0 warnings,0 errors.
- Full suite: **7,423/7,423**, zero skipped;59 new repository cases since7,364. `full.trx` SHA256: `5F34916F9930507B62627313E41C7A79BA9FA6595B211859CC50C1F02AC4FB17`.
- The focused59-case repository gate passes. It includes all16 actual build16042 catalogs with synthetic Challenge source rows; it does not prove every retail objective/timer producer.
- **59 exact source MySQL cases** pass in fresh retained `nexus_assessment_character_reward_repo_30a2`, including two independent-context serialization cases. None are excluded. The fixture uses current-model EnsureCreated, not a migration or native gameplay observation. Per-case attempts, medals, Challenge states, tracks, slots, claims and Items are snapshotted/hashed before cleanup of only synthetic owner42 in this dedicated schema. Private configuration is unchanged.
- **31 unchanged compatibility probes** pass against the frozen final references.
- `VERIFICATION.json` binds169 DLLs,5,725 Source files and4 exact test/helper sources. The six changed Source paths since7,364 contain the repository/state/tests and reservation metadata only; entitlement sources are unchanged. Strict coverage remains31 CSVs/167,484 rows, all not_retail_complete.

The earlier7,417/53-case provider result is retained under `character-reward-repository/final`, with its fresh retained `nexus_assessment_character_reward_repo_30a1` schema. It predates the complete-payload and mode guard refinements and is not relabeled as the final review.

WorldPID48568 and AuthPID48880 remain the frozen7,364 deployment. Its200-cycle lifecycle replay, original-data/default checks and natural broker drain retain that earlier scope. No new native UI replay occurred. Computer Use remains stopped. No additional migration, runtime reward row, historical backfill, entitlement44 or seven-item grant is introduced by this repository pass.

## Remaining connected implementation

Connect an actor-bound queue to the separately verified fresh-context executor/provider strategy. Coordinate Player saves before SQL, adopt committed Challenge baselines while retaining later runtime intent, and resolve ambiguous inventory outcomes before retry or publication. Freeze modifiers per newly earned medal; preserve old active null-marker attempts and unsupported producers/catalogs on the legacy reward path. Route type2 exclusively to Loyalty/Auth and type3 to the character owner using the latest published cycle binding. Compose account and character snapshots on every full reload and suppress stale actors. The wire has no cycle nonce; do not invent a guarantee that it distinguishes an old delayed packet from an identical selected row in a newly published cycle. Native earning, claiming, relog and reset checks remain open. F-033 stays partial.
