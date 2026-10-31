# Contract track choice through the registered actor queue

Checkpoint9122 connects the real07CF choose packet for Contract track types0/1 to
QuestManager and the existing Character reward actor queue. Active selection and
any newly eligible durable rows commit together. The full build has zero warnings/
errors; **9,122 full tests**,674 focused cases,1,172 exact-source MySQL cases, two
provider model gates and31 compatibility probes pass.

This slice adds32 tests:23 connected choice cases, one independent recovery case
and eight static provenance cases. MySQL reruns1,124 prior cases and adds those24
choice/recovery cases plus the same24 with
configured `EnableRetryOnFailure`. The eight static cases are not MySQL cases.
The31 compatible legacy probes remain unchanged. No model, schema or migration
changed; both Character/Auth database binaries match9090.

## Ownership, admission and packet routing

The registered choose handler dispatches by the actual static type: Contract0/1,
Loyalty2 and Challenge3. A default-false IQuestManager capability preserves custom
implementations and does not fall through to another reward owner after refusal.
The earlier point-publication note's reversed Loyalty/Challenge labels are corrected;
the existing routing constants were already correct.

The real Quest owner requires its current Player association, a loaded Contract
snapshot actually included in the latest composed reload, an advertised track,
satisfied track prerequisite, and the existing Challenge queue's idle/current/
inventory-save admission. Invalidated, foreign, normal-save, reward-capture,
inventory-busy and unpublished/cache-only requests are rejected before eligibility
evaluation or sampling. Unknown/unadvertised tracks cannot bootstrap state.
An already active initialized track is not resubmitted by the actor.

Eligibility adapters may reenter the actor. The owner and snapshot are checked
again after capture, so a normal save or changed owner cannot be overtaken. The
command retains a copied complete Contract baseline, immutable catalog, operation
identity, evaluated prerequisite set and optional frozen selection. No Player or
prerequisite callback runs on the SQL worker; no second queue or save owner is added.

## Unselected legacy cycles and provenance

A known unselected cycle can initialize eligible rows during an explicit choose
without earning another Contract or discarding existing points/cycle. The same
explicit uniform server sampling policy applies, with no retail RNG claim.
Prerequisite filtering is retained at every cost ordinal; no class/rank fallback
is permitted when a required pool is empty.

New choose selections use canonical version2 JSON with an OperationId. They do
not contain an AcceptanceId and create no completion receipt, XP, ordinary prize,
currency award or invented Quest history. Existing completion-origin version1
selections still restore unchanged. Both formats reject invalid owners, ordinals,
prerequisites, unknown fields, invalid origin IDs and noncanonical payloads. An
initialized cycle's existing selected rows are preserved even if eligibility later
changes; a choose is not a reroll or reset.

## One SQL operation and independent active types

The new command runs inside the existing repository/executor's single serializable
Character unit and owner lock. The complete SQL Contract aggregate must match the
captured baseline exactly, including selected rows/provenance. SQL drift cannot
silently rebase a published request. Only active flags within the selected static
type may change; Contract types0/1 can both be active alongside Loyalty2 and
Challenge3. Points, cycle IDs, ordinary rewards and unrelated active types remain.

Initialization adds every selected row and its marker in that same transaction.
Every changed row joins the repository's protected targets. A final complete SQL
witness checks the Contract aggregate before commit, including read-only repeats.
Selected-row/marker changes and suppressed active writes reject the unit.

A provider-specific failure exposed a shared executor bug: a non-transient commit
acknowledgement error exits the original configured retry-strategy scope. The old
catch-path then opened a recovery transaction outside that scope, which EF rejected
before observation. The shared executor now re-enters a configured strategy for
outcome recovery, reusing the retained idle strategy context when available. Only
read-only outcome observation is scoped/retried, never actor callbacks or RNG.
Cleanup of a strategy-only context cannot downgrade an already proved outcome.

The frozen pre-fix recovery probe fails in ExecutionStrategy.OnFirstExecution;
the corrected probe passes with the same configured MySQL retry options. A new
independent executor regression joins the connected actor fault cases. This repair
applies to the existing reward command families as well as Contract choices.

Lost-ack recovery compares the exact retained before/after aggregates, using the
existing execution proof and queue lease. A proved committed target is loaded and
published without replaying a mutation or sampling again. A rollback or conflict
requires authoritative reload; the old actor cannot submit another unsafe request.
Presentation adopts Contract state before the final Character contribution publishes
one composed reload, retaining actual sent-snapshot ownership across reentrancy.

Unsaved Quest timer and Challenge progress intent survives choices and later normal
saves. Choices do not accidentally acknowledge or flush those unrelated changes.
Normal completion, later Challenge commands and relog preserve the selected/active
Contract state, with no changes to entitlement defaults or ordinary prizes.

## Qualification

Evidence root: `I:/GIT/NexusForever-assessment-report/remaining-goal/contract-choice`.
`full5/full.trx` binds9,122 passing tests; `focused-final3/focused.trx` binds674.
The frozen inventory covers5,833 Source/load-tool files,12 changed paths since9090,
169 DLLs and34 copied test/helper files, including the new partial producer tests.
`final-review3/MySqlProof/result.json` retains1,172 passing per-case SQL snapshots
on fresh schema `nexus_assessment_contract_choice_31a3`; cleanup is restricted to
the verified synthetic owner and the schema is retained.

The connected tests cover both actual Contract types, real registered packet
deserialization/delegate dispatch, legacy initialization, existing initialized
cycles, coexistence with Loyalty/Challenge, dirty Quest/Challenge intent, rejected
requests, reentrant eligibility, stale SQL, flush/precommit/lost-ack faults,
protected-target tampering, suppressed writes and relog. Session dispatch fixtures
are not a genuine socket or native UI run. Other ordinary managers remain adapters
and scalar completion fixtures still omit full retail fields.

The earlier nullable-overload/namespace/test-variable compile failures and the
external provider reflection failure from boxed-int versus uint InlineData, and
the configured-retry recovery failure remain in
the evidence logs; the final frozen runs pass. This slice introduces no schema
changes. The populated migration/legacy-repair evidence from9090 is reused only
after verifying that its frozen Character DLL equals the current one; the same
Auth DLL and unchanged models/migrations are also checked. Its60 old business
tables/68 row projections and nonzero option/decor replay remain qualified within
that synthetic upgrade scope, not as a copied runtime upgrade.

## Runtime and remaining full Goal

Source is not staged into the separately qualified8378 backend. All9 service
identities,6 drained queues, original77 tables/21,989 rows/43 copied legacy Characters,
accepted Contract/Challenge data, private configuration and schema remain preserved.
Entitlement defaults remain15:1/17:1, with no automatic44 grant. The original checkout
is clean; branch/head remain unchanged. No commit/push was made. Computer Use stays
stopped; native launch/UI work requires explicit resumption.

Claims and stale/unpublished cycle guards, repeat/reset/carry policy, all726 delivery
choices, Item2 quantities/binding, final2,500,000 credits and AccountItem2485/2486
compensation remain. Account-side delivery requires idempotent Auth handoff/outbox
and crash/ownership recovery. Reset-policy preference is pending; independent work
continues. Full-table ordinary rewards/pushed items, broader composite producers,
copied-upgrade and connected objective/completion/earn/choose/claim/relog/reset socket
qualification remain before deployment. The running schema still lacks Contract
tables and must not receive this source before the complete upgrade gate.

Zone35, unknown Challenge producers, AMP/combat/upstream/content and every other
assessment requirement remain in the active full Goal. All167,484 content rows remain
`not_retail_complete`. The June19 Q5597 bundle is unavailable and is not requested again.
