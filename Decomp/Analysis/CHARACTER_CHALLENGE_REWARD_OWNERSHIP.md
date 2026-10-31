# Character Challenge reward ownership and baseline adoption

Later checkpoint7959: [live producers](CHARACTER_CHALLENGE_LIVE_PRODUCERS.md) connects mapped count-based earning and legacy compatibility. The earlier scope below is historical; real socket earning/claim and native UI remain open.

Later checkpoint7831: [shared Quest/Challenge queue](QUEST_CHALLENGE_REWARD_QUEUE.md) verifies one command/recovery/adoption path for both aggregates. Live earning producers remain open. The earlier checkpoint and its scope below remain historical.

Latest 30 October continuation: [Quest actual-commit durability](QUEST_PERSISTENCE_DURABILITY.md) now supplies committed scalar baselines/revisions, retained deletion and cascade restoration. Full7,599 tests,31 compatibility probes,232 exact-source provider cases and176 runtime cycles pass. Joining completion/Quest participants to the reward unit and live earning remain open. Earlier checkpoint evidence below keeps its original scope.

Latest 30 October continuation: [Typed request routing and published bindings](CHARACTER_CHALLENGE_REWARD_ROUTING.md) now wires type3 choose/claim through the queue, exact published snapshots and bound transactional inventory. Full7,563 tests,31 compatibility probes,196 exact-source provider cases and209 runtime cycles pass. Live earning and completion/quest durability remain open. Earlier checkpoint evidence below retains its original scope.

Latest 30 October continuation: [Actor queue and composed reloads](CHARACTER_CHALLENGE_REWARD_QUEUE.md) now connects the real Player's initial durable load, session-event adoption, bounded recovery and final-disposal cleanup. Full 7,517 tests, 31 compatibility probes, 153 exact-source provider cases and 209 runtime cycles pass. Live earning and type-3 choose/claim routing remain open. The earlier checkpoint evidence below keeps its original scope.

At the 7,483 checkpoint on 30 October 2026, Challenge reward operations can hold normal Player/Challenge saves and adopt authenticated committed results without losing later runtime intent. This supplies the ownership boundary needed by the [executor](CHARACTER_CHALLENGE_REWARD_EXECUTION.md). **The live actor queue, Challenge earning producers, composed track presentation and packet claim routing remain unwired.** Existing direct rewards, legacy null attempt markers and entitlement defaults are preserved.

## Ownership boundary

`ChallengeManager.RewardPersistence` captures a copied runtime intent and its dirty revision. A queue ticket blocks Player saves before Auth enrollment, including gaps between individual SQL units. Tickets are independently idempotent and retain deferred save callbacks until all pending tickets release. The live queue must acquire and retain these tickets; this change does not install that queue.

A reward persistence lease requires a fresh, untracked Character context outside any transaction. It pins the configured database/character identity and shares the process-local Challenge save lease with ordinary saves. Another loaded manager cannot stage competing Challenge state. Direct Character saves overlapping reward ownership permanently reject their context, preventing callers from ignoring the rejection and flushing partial work. An ordinary unfinished save prevents reward lease acquisition.

For non-claim operations the lease also holds an inventory mutation lease, which blocks ordinary full saves. A claim instead uses its actual prepared inventory reservation. Matching character metadata alone is insufficient: the concrete Inventory must own that exact reservation. Failed acquisition unwinds inventory ownership. Provider identity initialization and inventory scope disposal run outside the Challenge lock; no monitor spans asynchronous SQL.

The lease binds the immutable command, database and reservation to the executor's private result proof. A cloned record or a result from another operation cannot adopt a baseline. Disposing an executing lease cannot release ownership. Unknown outcomes stay pinned and resume through the existing exact, single-use executor proof. Cancellation alone never establishes rollback. Abandonment marks the old owner as requiring authoritative reload and releases process ownership only after a genuine result proves the original SQL unit ended.

## Adoption and cleanup

Known uncertain ordinary-save candidates must be resolved by a committed load probe before a reward command binds its `Before` state. The probe validates all Challenge rows against captured candidates; an unrelated writer's row cannot silently become local intent. The subsequent operation retains the same logical ownership.

Committed adoption preflights the complete result before modifying baselines or runtime. Non-target rows must still match their captured/resolved baselines. The target merges each persisted field against the captured runtime intent: untouched fields adopt durable state, while later timer/progress changes survive. A newer attempt identity quarantines the result rather than being overwritten. Share timers remain actor-owned. Dirty state clears only when the revision and complete merged row permit it. This prevents the next ordinary save from restoring an old attempt marker or repeating already committed reward progress.

Player direct, deferred and cleanup saves consult Challenge ownership before starting Auth. Cleanup invalidates new reward work and waits for queued/executing work while a known completion can still be adopted. Prepared invalidation and final disposal require authoritative reload. The timeout/quarantine path skips stale full saves. A finalized old owner never resumes saving, even if its known committed bookkeeping subsequently finishes. Legacy/custom inventory and Challenge adapters retain additive default interface behavior.

Adopting a claim's Challenge state does not publish its Item. The actual inventory reservation keeps its own save block until the actor publishes or settles it. Stale publication, pending work cancellation, retry scheduling and client notifications must be connected by the next actor queue slice.

## Verification

Evidence root: `I:/GIT/NexusForever-assessment-report/remaining-goal/character-reward-actor-ownership/final-review`.

- Full `Source/NexusForever.slnx` build: **0 warnings, 0 errors**.
- Full suite: **7,483/7,483**, zero skipped; **21 new ownership cases** since 7,462. `full.trx` SHA256: `D6EAC84B2AA248CD5D675A7BD2AFC3A91347B83DB9665866C29AB678CF41F856`.
- Focused ownership, ordinary Challenge durability, executor and inventory gate: **128/128**.
- **119 exact-source cases** pass with no exclusions in fresh retained MySQL schema `nexus_assessment_character_reward_actor_30a3`: 59 repository, 39 executor, 21 ownership. The unchanged source/helper copies are compiled against 169 frozen DLLs. Per-case attempts, medals, Challenge states, tracks, slots, claims and Items are hashed before cleanup of only synthetic owner 42. Two repository cases use independent concurrent contexts. The suite includes a provider-independent constructor guard and a SQLite Auth context in the save guard; these counts do not represent 119 concurrent MySQL workloads.
- The harness includes a frozen/hash-checked native SQLite dependency for that unchanged Auth guard. The first run in `MySqlProof`/30a1 stopped after 116 passing cases because that dependency was absent. `MySqlProof-final` stopped before schema creation because its Windows path guard compared `.dll` and `.DLL` with case sensitivity. Both failed results are retained; `MySqlProof-complete` checks the loaded module's output boundary and exact hash and passes the complete suite.
- **31 unchanged upstream compatibility probes** pass against the final frozen references. The source manifest binds **5,730 Source files** and six exact test/helper sources. Ten changed Source paths since 7,462 cover ownership contracts, lease/adoption code, Player/Inventory integration and tests; entitlement sources are unchanged.
- Content validation passes: 31 CSVs / 167,484 rows, all `not_retail_complete`. The original `I:/GIT/NexusForever` checkout remains clean.

The new frozen World/Auth build completed **182** login/world/bootstrap/disconnect/cleanup cycles: one cold Eastern login, 180 balanced mixed-world cycles across 70 seconds, and one existing-account login. Lifecycle matching found no errors, unfinished admissions or late/duplicate bootstraps. The early snapshot taken before the final asynchronous cleanup finished is retained separately. The Group queue drained naturally from 22,026 ready / 50 unacknowledged through 6,358 / 50 to zero; all six queues are empty. No queue was purged. World PID 26272 and Auth PID 35800 serve the new frozen build, with all nine owned services healthy and the other seven process identities unchanged. Final broker/process/hash verification passes in `runtime-checkpoint7483/runtime-final-state.json` and the combined `VERIFICATION.json`. Private configurations and all preserved original rows/defaults are unchanged. These runtime cases exercise existing login and cleanup paths; no live new reward claim or native UI result follows.

## Remaining connected work

The actor queue and persisted-cycle reload contribution are implemented in the later checkpoint above. Connect activation, per-medal earning, terminal completion, track choice and inventory-backed claims across all16 supported catalogs. Preserve empty-catalog fallbacks and historical legacy-direct attempts, prevent direct-plus-track double awards and apply the latest-published claim binding policy. Native earning/claim/relog/reset UI checks remain open while Computer Use is stopped. F-033 stays partial.
