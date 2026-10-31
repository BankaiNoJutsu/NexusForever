# Character Challenge reward actor queue and reloads

Later checkpoint7959: [live producers](CHARACTER_CHALLENGE_LIVE_PRODUCERS.md) connects mapped count-based earning and legacy compatibility. The earlier scope below is historical; real socket earning/claim and native UI remain open.

Later checkpoint7831: [shared Quest/Challenge queue](QUEST_CHALLENGE_REWARD_QUEUE.md) verifies one command/recovery/adoption path for both aggregates. Live earning producers remain open. The earlier checkpoint and its scope below remain historical.

Latest 30 October continuation: [Quest actual-commit durability](QUEST_PERSISTENCE_DURABILITY.md) now supplies committed scalar baselines/revisions, retained deletion and cascade restoration. Full7,599 tests,31 compatibility probes,232 exact-source provider cases and176 runtime cycles pass. Joining completion/Quest participants to the reward unit and live earning remain open. Earlier checkpoint evidence below keeps its original scope.

Latest 30 October continuation: [Typed request routing and published bindings](CHARACTER_CHALLENGE_REWARD_ROUTING.md) now wires type3 choose/claim through the queue, exact published snapshots and bound transactional inventory. Full7,563 tests,31 compatibility probes,196 exact-source provider cases and209 runtime cycles pass. Live earning and completion/quest durability remain open. Earlier checkpoint evidence below retains its original scope.

At the7,517 checkpoint on30 October2026, the real Player configures a character reward queue. Initial Challenge packets enqueue a durable state load, and the session event queue handles its result on the actor. Persisted character-track rows contribute to the existing composed account/character reload. **Live Challenge earning producers and type-3 choose/claim packet routing remain unwired.** Old direct rewards, null attempt markers and entitlement defaults are preserved.

## Queue and execution

`ChallengeManager.CharacterRewardQueue` serializes copied runtime intents and retains a separate save-blocking ticket for every accepted request. It waits for an existing normal inventory/save unit and keeps all later tickets through the gaps between operations. Each subsequent builder sees the baseline adopted from earlier commits. Builders execute on the actor; callers must capture immutable event fields, identities, modifier/prerequisite values and times before enqueue. Only immutable commands reach the executor. No actor or prerequisite callback runs on the worker.

The first worker creates an unused fresh Character context, initializes the provider, captures immutable database identities and disposes it. This keeps even provider discovery off the actor. Persistence/inventory capabilities are then acquired on the actor. A known uncertain ordinary-save baseline gets a committed load probe before the queued command is built. Acquisition failures unwind ownership; foreign reservation objects are rejected before a ticket or identity worker starts.

The executor runs the complete provider transaction. Session events adopt authenticated committed rows, publish the retained inventory reservation after adoption and then deliver the committed snapshot. Unknown outcomes retain command, reservation and ownership and resume the exact executor proof. The default queue allows three delayed recovery passes. Exhaustion, conflict or a failed request requires authoritative reload and disconnect; a provisional reward transition cannot leak into an ordinary full save. An original unit without proved termination stays pinned. Exceptions after commit never reopen an award or rebuild the command.

## Logout and disposal

Session events continue to run when Player world updates stop during logout. Invalidation cancels later queued requests and their tickets, suppresses callbacks and prevents new SQL. Canceling later provisional intent requires reload even if the current operation can commit. An executing known result can still finish bookkeeping before normal cleanup when safe.

Final disposal may outlive session event polling. A retained task continuation can abandon the dead owner's Challenge lease only after an authentic result proves the original unit ended. This tail never adopts live runtime state or sends client messages. Unknown original ownership remains pinned. All ticket/lease releases are idempotent. Callback/notification failures cannot turn a committed receipt into a new claim.

`IInventoryRewardReservation.InvalidateOwner` is an additive capability. The real token suppresses its own notifications immediately, independently of broader Inventory invalidation. Its volatile flag is also consulted between packet sends. Already running SQL can settle committed inventory or quarantine uncertain ownership through the existing terminal path, without notifying a dead actor. Legacy/custom adapters retain default behavior and cannot masquerade as a concrete Inventory's reservation.

## Composed reload

`ChallengeManager.CharacterRewards` requests the character contribution before Loyalty can publish an account-only initial reload. It installs only copied committed state and sends one union through `RewardTrackPresentation` once both contributions are ready. Durable slot ordinals, sampled row IDs, points and claim bits are preserved. Multiple initial-packet calls do not enqueue duplicate loads. An empty character contribution is explicitly ready and allows the account snapshot to publish.

This read path advertises persisted cycles only. It does not create missing cycles, sample rewards in presentation or claim all type-3 catalogs are already visible on a fresh character. Activation/choice integration must initialize supported cycles through the repository and republish their state. The five empty catalogs remain fallbacks. A type-3 claim still needs the documented latest-published row/cycle binding because the packet has no cycle nonce.

## Verification

Evidence root: `I:/GIT/NexusForever-assessment-report/remaining-goal/character-reward-actor-queue/final-review`.

- Full `Source/NexusForever.slnx` build: **0 warnings, 0 errors**. Full suite: **7,517/7,517**, zero skipped. **34 new queue cases** since 7,483. Final `full.trx` SHA256: `F7179F039745A8FDD229E8471055F11EB65F9597BE065F60406A8043FDC5EAED`.
- Focused queue/ownership/inventory gate: **94/94**. It covers sequential medal baselines and later intent, pre-existing normal saves, uncertain ordinary saves, lost acknowledgements, bounded recovery, prepared/executing/final invalidation, canceled later intent, throwing callbacks, commit-only actor inventory publication and dead-owner inventory settlement without messages or further event polling.
- **153 exact-source provider cases** pass with no exclusions in fresh retained MySQL schema `nexus_assessment_character_reward_queue_30a1`: 59 repository, 39 executor, 21 ownership and 34 queue. All 16 supported catalogs retain their durable ordinal selection on composed reload. Synthetic Challenge sources use the actual build16042 catalogs. One constructor guard is provider-independent; the unchanged Auth save guard uses the frozen native SQLite dependency. Counts are not 153 concurrent MySQL workloads. The earlier injected execution-strategy retry is not a new real-InnoDB-deadlock reproduction.
- Each provider case snapshots/hashes attempts, medals, Challenge rows, tracks, slots, claims and Items before cleanup of only synthetic owner42 in its dedicated schema. Current-model EnsureCreated is not migration proof. Private configuration is unchanged and the schema is retained. Seven exact test/helper sources compile against 169 frozen DLLs. `VERIFICATION.json` binds **5,733 Source files** and nine changed Source paths since 7,483; entitlement sources are unchanged.
- **31 unchanged upstream compatibility probes** pass. Content validation passes: 31 CSVs / 167,484 rows, all `not_retail_complete`. Original `I:/GIT/NexusForever` is clean.
- Frozen World/Auth deployment passes **209** complete login/world/bootstrap/ACK/disconnect/cleanup cycles over a mixed-world run spanning an Auth refresh interval. World PID 31732, Auth PID 45924; all nine owned services are healthy and the other seven process identities unchanged. Lifecycle has zero errors, unfinished admissions or late/duplicate bootstraps, and the frozen log contains exactly one initial composed reward reload per cycle in aggregate. All six broker queues drained naturally. Frozen outputs, loopback listeners, private configurations, original rows and defaults are verified. New reward tables remain empty on the reused login fixtures; these cases do not exercise live new reward earning or claims. Computer Use remains stopped.

The first 7,517 run and focused results before volatile suppression are retained under `before-volatile-suppression`; the final full/focused runs and all provider/runtime proofs use the final source. Initial focused compile warnings and the corrected test-local variable naming error are retained in earlier logs. No new native UI observation follows. F-033 stays partial.

## Remaining connected implementation

Type3 choose/claim routing and published binding are implemented in the later checkpoint above. Connect live activation, per-medal earning and terminal transitions after reconciling completion with quest-objective durability. Preserve legacy paths, sharing, cooldowns and direct rewards without double grants, then verify real network earning/claim/relog/reset and native UI. The active Goal retains the full assessment scope. F-033 stays partial.
