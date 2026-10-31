# Character Challenge socket earning and claims

Checkpoint7983 verifies actual Challenge103/track16 earning, claim delivery and reset through the isolated socket stack. Fifteen credited combat kills produced three completed attempts and300 points. Three real07CE claims created three receipts and three inventory rows; relog preserved the retained cycle, then observed cycle1/zero points after the last claim. The run uses explicit offline placement of one owned fixture between kills. Native UI, walking and retail RNG/reset parity remain unverified; F-033 stays partial and Computer Use stays stopped.

## Source and fixture

Read-only discovery hashes the configured build16042 tables and actual runtime world rows. Challenge103 is combat/type0, flags32(auto-activate), track16, target group8851, tier row150 with goal5, world426/zone604. WorldLocation13579 places its indicator near4680.6562,-732.55164,-5692.25. The configured Map/Eastern asset exists. The native target-group closure plus the previously reviewed Northern Wilds creature bridge yields19 allowed creature ids,53 existing world spawns and39 placements in area604. These are source eligibility and fixture placement observations, not a new content-completion claim.

Fresh normal account27/Character74 (`loadtest9701@example.local`, Load9701A) was seeded in the owned assessment schemas. Preparation raises only its level/XP/health to the normal heavy-profile values. Roles remain1, defaults remain15:1/17:1 and entitlement44 is absent. No spell, item, reward or completion was manufactured. Warrior unarmed spell26434 is learned by the normal SpellManager; real selected casts, damage and deaths drive the existing combat auto-activation producer.

The new `run challenge-rewards` harness requires an explicit placement opt-in, an owned loadtest account, matching loopback assessment schemas and finite actual-source recipe placements. Before placement it waits for saved offline state and the latest closed presence generation. Its only fixture write changes the offline character's world/zone/position/rotation. It sends no movement and makes no walking claim. Selected rows, points, attempts, medals and receipts are never written by the driver.

## Fix found by the real terminal operation

The fifth kill in the early socket run exposed a production failure in `QuestManager.QuestRewardCapture.PrepareWorker`: the serializable read transaction ran outside the configured MySQL retry strategy. MySQL rejected the first parent-row query. The operation quarantined the provisional completion; SQL retained count4, its original attempt, zero medal receipts and zero points.

Preparation now obtains the configured execution strategy and executes each whole read transaction in a fresh validated context. Scalar captured input and its opaque token remain the worker boundary. Actor-side Quest effects are not replayed. Three regressions cover retry-enabled preparation without a fault, a transient pre-commit failure and lost read-commit acknowledgement. They verify fresh contexts, unchanged SQL before reward execution and one actor-side effect/terminal award.

The failed operation left the saved online flag set while presence had closed. A normal socket login/logout on the corrected backend settled it without SQL repair and preserved count4. Eleven subsequent kills completed that attempt and two new attempts: points100/200/300, one medal per actual one-tier source completion. The retained four earlier kills were not reset or reearned.

## Claim ordering and observations

An early claim observation found a driver race: track16 was already active, so waiting only for Active=true let07CE run while the preceding07CF choose command still owned the queue. The server correctly rejected the claim; points300, zero claims and zero inventory rows remained. The driver now waits for a new published reload after choosing, including an idempotent choice of the already-active track. A regression verifies that an old identical active snapshot cannot acknowledge this operation.

The final claim-only run consumes retained earned state with no further combat:

| Phase | Socket and durable observation |
| --- | --- |
| First login | Initial300 points/cycle0/rows320,321,324;07CF dispatch and new reload; first07CE commits claimed mask1 and one item. The choose is idempotent because this track was already active. |
| Second login | Same cycle/selected rows/claimed bit; remaining two07CE claims commit their receipts/items; final claim advances cycle0 to1 and carries300 minus300 points. |
| Third login | Cycle1, points0, claimed mask0, three total receipts, three inventory rows and all three completion/medal receipts retained. New selected rows happen to be320,321,324 again; row inequality is not a reset requirement. |

Every claim waits for receipt count, a new inventory row and the emitted full reward reload to agree with SQL. The observer uses mapped packet layouts, complete reload replacement and acknowledged selected-spell targets; it does not infer hidden entity fields.

## Verification and retained evidence

Evidence root: `I:/GIT/NexusForever-assessment-report/remaining-goal/challenge-socket`.

- Final full solution build: **0 warnings,0 errors**. Full tests: **7,983/7,983**, zero skipped. TRX SHA256:`4C56B9950286AFED5A1FED6A905756E38995C62777C4AD76CAD18186DA77D000`. **24 new cases** comprise21 harness/protocol/fixture regressions and3 retry-enabled preparation cases. Final focused78 pass; the earlier producer-inclusive133 pass is separately retained. All31 unchanged compatibility probes pass.
- **515 exact-source MySQL replay cases** and2 model gates pass against fresh retained `nexus_assessment_challenge_socket_30a1`. This includes the new retry-enabled preparation variants; source fixtures, not a native client, supply those provider scenarios. After the final driver-only correction, all21 copied provider helpers, loaded provider/model assemblies and World/API outputs remain byte-identical. `final-review2/provider-runtime-bridge.json` binds the retained provider results to the final source/build; it does not pretend a second provider replay occurred.
- Final verification binds169 DLLs and5,779 Source/tool files. Since7959, the three changed Source paths are the Quest preparation implementation, its tests and the new harness tests; Tools contains the new scenario/observer and preserved login path changes. No models, migrations or runtime schema changes were needed.
- Corrected World PID33188 and Character API PID15496 remain healthy with the other7 unchanged service identities. **16 complete admission/bootstrap/EnteredWorld/disconnect/cleanup cycles**,16 initial composed reloads and10 live reloads are correlated. Fifteen cycles have completed client report entries; the additional rejected-claim observation is explicitly reconciled from its real server lifecycle. There are zero server lifecycle errors, unfinished admissions or late/duplicate bootstraps in this corrected World log.
- All9 private configurations, original checkout, original database rows and entitlement defaults remain preserved. Six broker queues drain naturally. Five fixture owners retain closed latest epochs with no active presence. Reward rows exist only for the explicitly earned Character74; older reward tables/NULL attempt markers retain their earlier invariant. No queue purge, presence repair or reward SQL reset was used.
- Content validation remains31 CSVs/167,484 rows, all `not_retail_complete`. No commit/push.

Authoritative artifacts: `final-review2/VERIFICATION.json`, `runtime-final-state.json`, `lifecycle-log-audit.json`, `run4/kill-*.json`, `run5/challenge-rewards.json`, source discovery/fixture manifests and frozen harness hashes. Attempts1–4 remain retained: initial target-selection timeout, the MySQL preparation failure, offline-flag preflight refusal and the rejected early claim. The normal recovery login and read-only observations document the transitions without erasing failed evidence.

## Remaining gates

Native Challenge reward presentation requires explicit resumption of Computer Use. Unknown time-tiered flag0x8 scoring, producer-specific collect/item/general behavior, challenge-specific achievement triggers and share-init ownership remain open. Exact retail odds/reset remain unproven; uniform/all-claims/carry is still the accepted upstream policy. This count-source socket cycle does not establish those semantics or full retail completion.

The full assessment Goal remains active. The unavailable June19 Q5597 historical UI folder cannot be recovered from the user's backups; its dated recovery review remains a substitute planning record, not historical/native UI evidence.
