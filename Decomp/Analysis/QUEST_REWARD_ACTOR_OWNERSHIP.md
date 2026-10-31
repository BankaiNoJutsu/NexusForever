# Quest reward actor ownership and exact completion capture

Later checkpoint7870: [normal-save snapshots](REWARD_NORMAL_SAVE_SNAPSHOTS.md) verifies ready direct/deferred Player save enrollment with immediate reward capture, explicit objective ordinals and loaded ordering. Live producers/startup readiness remain open; the earlier checkpoint below remains historical.

Later checkpoint7831: [shared Quest/Challenge queue](QUEST_CHALLENGE_REWARD_QUEUE.md) verifies one command/recovery/adoption path for both aggregates. Live earning producers remain open. The earlier checkpoint and its scope below remain historical.

30 October2026, checkpoint7774. Internal actor capture, participant ownership and committed-baseline adoption are implemented and verified. **Challenge queue integration and live earning remain open.** Existing direct prizes, normal Quest effects, entitlement defaults and published type2/3 routing remain unchanged. No production caller invokes the new completion capture API yet. Computer Use stays stopped.

## Actor and worker boundary

QuestManager captures the actual completion action once between complete before/after censuses of completed, inactive and active Quests. It retains concrete Quest/objective owners, immutable scalars, dirty revisions and deletion intent. Script-created Quests, scalar changes, objective progress, removal and replacement are participants. It does not replay a broad CompleteChallenge event during SQL recovery. Existing scripts and objective sequencing still run through the real event.

A retained process/database/character reservation excludes normal Quest root/direct-objective units, other loaded managers and unknown newly created Quests. Multiple captures from the same manager retain separate references and execute FIFO. Capture refuses before invoking the event if a normal Quest unit or real Player direct/deferred save is active. Pending/quarantined Quest work blocks Player before Auth enrollment. Direct saves now retain an active-operation count across both Auth and Character phases. The live producer must still define the policy for a completion arriving during that phase gap; delaying an unbound event could credit a newly accepted Quest.

At the FIFO head, the actor copies current persisted-baseline candidates, including uncertain normal-save outcomes. Later queued captures therefore follow the earlier committed baseline rather than their original loaded state. A fresh serializable worker probe validates the live Character and complete SQL graphs, including missing/extra objectives. It receives copied scalar values and an opaque token, never actor owners or callbacks. MySQL reset conversion uses the actual session DATETIME representation. The probe resolves immutable before/after participants for the existing shared reward SQL unit.

Preparation identity is registered independently of its visible values. An invented preparation with the same token and graphs is rejected. The exact executor result is bound to command, database and terminal proof; cloned/foreign results cannot acknowledge or release ownership. The executor returns copied Quest SQL states from inside its write/recovery transaction. Actor adoption preflights every frozen participant, advances persistence baselines and clears only matching captured revisions whose live values still match. Later progress, timers, flags and same-value touches remain dirty. Rebase does not supersede a captured dirty revision merely to mark an already-dirty replacement owner for saving.

Committed deletion retires the removed owner. A deletion canceled after capture retains its actor intent and recreates cascaded objectives on its next normal save. Replacement owners receive the committed baseline without acknowledging their new intent; old owners cannot save over them. Failure after the event requires authoritative reload, including script-created owners when the event throws. Unknown write units remain pinned. Final invalidation can release only after genuine terminal proof and performs bookkeeping without adopting late actor values or publishing UI.

## Verification and preservation

Evidence: `I:/GIT/NexusForever-assessment-report/remaining-goal/quest-reward-actor/final-review`; runtime: `remaining-goal/runtime-checkpoint7774`.

- Full solution build: **0 warnings, 0 errors**. Full suite: **7,774/7,774**, zero skipped. Full TRX SHA256: `D0591003D2DF455308691DFDCC1C0057B915E10D0E62C4ED066F09B37A6EFB30`.
- **46 new actor cases** cover every supported catalog, real objective dispatch, script-created Quests, later/same-value dirty revisions, FIFO baseline chaining, root/direct-leaf normal-save exclusion, other loaded owners, exact preparation/result proofs, cascade retirement/restoration, replacements and queued replacements, foreign SQL roots/leaves, uncertain ordinary saves, reward commit faults, final disposal during a paused commit, provider reset conversion, pre-Auth guards and active full-save refusal. **152 focused cases** include the prior normal Quest/shared SQL/Challenge ownership regressions. Early compile/fault-test and replacement-rebase failures are retained outside final-review; the final code passes all gates.
- **327 exact-source provider cases** pass without exclusions in fresh retained `nexus_assessment_quest_reward_actor_30a1`:46 new plus281 previous repository/executor/ownership/queue/routing/Quest durability/transaction cases. Each case has hashed SQL graph snapshots before guarded synthetic Character42 cleanup. This count includes provider-independent/dispatch-only cases and SQLite Auth guards; it is not327 concurrent MySQL workloads. Current-model EnsureCreated is not migration proof. No schema changes were needed for this slice.
- Verification binds **169 DLLs, 5,762 Source files, 16 copied test/helper files and14 changed Source paths since7728**. The31 unchanged compatibility probes pass. Production entitlement/Loyalty sources are unchanged. Content validation remains31 CSVs/167,484 rows, all not_retail_complete. Original checkout stays clean, branch/head unchanged; no new commit/push.
- Frozen WorldPID34408, unchanged AuthPID9380, passes **224 complete protocol cycles** and224 initial composed reward reloads across Eastern, three mixed-world fixtures and an older account. Lifecycle errors, unfinished admissions and late/duplicate bootstraps are zero. Nine services remain alive; the other8 retain their process identities. Six queues naturally drain, presence closes the latest durable epochs without repair/relog, private configurations/assets and original data remain preserved. Defaults stay15:1/17:1, no44; reused legacy reward tables remain empty with null attempt markers. The first preflight ran before the World listener was ready; no login sample was attempted by that preflight. These login/reload checks do not invoke the internal Quest reward mutation API or prove live earning/claims/native UI.

## Connected work still required

The World and Game.Tests dependency graphs retain two pre-existing transitive
binary differences, Humanizer and RabbitMQ.Client. The verifier binds both
versions to checkpoint7728 in `dependency-graph-differences.json`; all shared
NexusForever assemblies match. SQL replay uses the frozen test graph and real
protocol cycles use the separately frozen World graph.

See [shared Quest/Challenge queue plan](QUEST_CHALLENGE_QUEUE_INTEGRATION_PLAN.md)
for the next ownership and cancellation boundaries.

Join Quest capture/preparation/execution/adoption with Challenge queue and its persistence lease so the same exact command/result owns both aggregates. Handle active full-save refusal before any provisional completion effects. Then wire activation-time mode selection, frozen per-medal modifiers and validated completion/timeout/abandon/relog producers across all16 catalogs. Preserve legacy NULL attempts, the five empty catalogs and unsupported/unvalidated producer paths. Avoid direct-plus-track double grants.

Real protocol earning/choose/claim/relog/reset and native UI remain separate gates. The broader assessment, Contracts/Zoneprogress semantics, remaining spell/AMP evidence, bank work, later Hycrest content and strict content rows remain open. The user has no June19 Q5597 historical bundle; the dated recovery review does not recover native UI evidence. Computer Use remains stopped pending explicit resumption. F-033 stays partial and the full assessment Goal remains active.
