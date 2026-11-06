# Guild completion and Character feedback - in progress

6 November2026. The retained Source/SQLite baseline probes reproduce Character
feedback committing and publishing before its originating Guild achievement.
The integrated durable handoff now passes240 scoped achievement/Guild cases,
104 experience-owner cases and51 configured-MySQL cases, including two actual-data
controls. Six forced producer-loss windows and a normal control pass, followed
by a normal MySQL restart with both pending and delivered intents. Fifteen source
snapshots match the qualified DLL compiler checksums; both final review axes
have zero findings. This qualifies prepared headless owners, with hosted World/
Auth, socket, native and wider retail gates still open.

## Reproduced boundary

`GuildAchievementManager.CompleteAchievement(target, achievement)` immediately
calls the target's Character achievement manager with AchievementComplete. The
Character's ordinary experience/achievement save owns that new row independently
of the Guild's mutation save. Under a Guild-achievement insert rejection trigger,
Guild world updates leave the parent absent, while the original real Player save
persists and publishes the Character feedback. Removing the trigger later allows
the Guild row to commit; that later success does not repair the earlier ordering.

The synthetic public producer exercises Character CheckAchievements, Guild
completion, the real Player's normal save and committed Character publication.
Its counter deliberately requires two contributions so a replay must not turn one
Guild completion into two credits. The actual-data control invokes the public
Guild completion manager with complete real checklist rows and the same real
Player save/publication path. It explicitly prepares prerequisites; it does not
qualify raid earning, all-Guild-group admission or native prerequisite policy.

Both probes observe first Guild rows0, Character rows1 and Character packets1.
After the rejection is removed, both rows exist and the Character contribution
remains1. The desired first phase has no durable or published Character feedback.

## Actual build16042 profile

The owning readers enumerate4943 Achievement rows,296 Guild rows and229 Character
AchievementComplete rows. There is one Character meta-achievement connected to
Guild completions under the current flag/type interpretation:

- Character7194, Guild: Genetic Archives, type7/flags0, RequiredProgress0.
- Its six checklist entries10875..10880 reference Guild achievements7187..7192.
- The first parent7187 is Guild: Faulty Hypothesis (Prime1), describing Experiment
  X-89 with an all-Guild group. The other parents cover the remaining raid bosses.
- Character server prerequisite44606 contains unknown types290 and203. Those
  remain unmapped here. Prepared test gates do not authorize runtime bypasses.

The actual first-parent control observes ProgressState1 (checklist bit0), separate
CreditedChecklistMask0 and no completion date. This is partial progress, not a
completed raid meta-achievement. The first control incorrectly expected that bit
in the separate credited mask; the corrected oracle preserves the same precommit
failure and is retained as a separate result. Parent/Character metadata, all
checklist rows and input hashes are in the local reports below.

## Required integrated outcome

Character feedback must not become durable or visible before the Guild completion
is proven committed. A committed Guild completion must retain recoverable feedback
intent even if its original Player leaves, is replaced, fails a save or reloads.
Packet deferral alone does not solve the database failure, and an in-memory callback
alone does not solve recovery after the Guild commit.

Use a complete same-database transaction or a typed, durable handoff that satisfies
both requirements. Preserve eligibility as accepted on the original actor; do not
recompute unknown prerequisites on a replacement actor to invent or discard credit.
Any handoff needs stable event identity, frozen recipient/metadata intent and a
delivery outcome committed together with Character effects. Retry and unknown
commit replies must not repeat scalar progress, checklist credit or completion
consequences. Later unrelated Character/Guild edits must survive.

Keep original database routing, actor/session and manager identity, complete row
witnesses and existing owner scopes. Guild SQL workers must not enumerate or mutate
live Character gameplay. Evaluate existing Guild mutation, experience persistence
and receipt mechanisms before adding a new save protocol. Reusing the immutable
receipt table must preserve its guards; a new schema requires proper migrations.

Required checks include rejected parent and recipient SQL, caller rollback,
unknown commit acknowledgement, duplicate/reentrant delivery, actor/session/manager
replacement, concurrent later progress, fresh-owner recovery and actual7194
partial/full checklist behavior. Distinguish process-local recovery, configured
providers and physical restart. Preserve paid Guild/Community creation and existing
achievement/realm-first behavior. All-member fanout and durable global realm-first
arbitration need their own evidence; do not infer them from this single-recipient
probe.

## Retained work and next action

`artifacts/continuation-20261106/guild-achievement-atomicity/` contains
`results/baseline.trx`, `results/baseline-rejected.trx`, `results/probe-baseline.trx`,
`catalog-details.json`, `actual-baseline.json` (initial credited-mask oracle), and
`actual-baseline-corrected.json`. The last result isolates the ordering failure
with the correct partial-checklist interpretation. Test and control builds are
clean; the initial catalog compiler diagnostics are retained separately.

Owning failing test and reusable probe are in
`Source/NexusForever.Game.Tests/Guild/GuildAchievementAtomicityTests.cs`.
The existing creation fixture now accepts an optional prepared prerequisite manager
for these tests; all existing defaults remain intact. The production implementation
and recovery controls now follow this baseline. Original failing reports and
frozen baseline DLLs remain unchanged.

## Implementation checkpoint

Guild completions retain immutable accepted Character recipes in the existing
receipt store. An ordered related Guild completion chain creates one intent in
its complete Guild transaction. One Character delivery applies its ordered parts
and commits one marker with the full frozen ordinary Character graph. Every
part's metadata is checked before any part mutates; a blocked earlier part
cannot seal a later part. Both owners retain complete row/route/context guards.
No schema or authoring/reference-database dependency was added.

Actor-bound events apply proven committed intents. Login/periodic recovery reads
pending intents for a fresh original-database Character owner. Only the World
actor changes gameplay. Eligibility decisions are frozen on the accepting actor;
causal achievement-completion decisions preserve sibling, deep-chain and
converging branches without querying a replacement actor's prerequisites.
Original cleanup may finish its captured save after registry removal; a different
actor/session/epoch/manager rejects the write, and stale publication is suppressed.

Source controls cover parent rollback/lost replies, recipient achievement/receipt
rejection and fresh-owner recovery, duplicate/reentrant delivery, later progress,
manager/session/epoch replacement and preparation failure. Ordinary failed saves
retain their existing authoritative-reload quarantine. The first recovery run
had two incorrect same-owner retry expectations (67/69); corrected controls prove
that the quarantined owner stays blocked and a fresh owner recovers. A subsequent
19 focused and211/218/224 owning cases passed successive intermediate candidates.
The final result is240 unique owning cases, including the existing57 creation
controls; do not add intermediate overlapping totals to it.

Reviews prompted fixes for unfinished preparation, terminal-context enrollment,
conditional causal eligibility, partial-chain acknowledgement and forwarding
retry. Forwarding retains its catalog/cursor; inner unfinished transitions resume
before parents, without repeating partial sibling credit, completion dates or
points. Metadata blocking, scalar setters, queue/session/manager/epoch replacement,
recipient SQL-identity tampering and later progress/cleanup writes are exercised.
Final Standards0 and Spec0 findings are recorded separately. Earlier build03/08/18
failures came from concurrent Scientist/Reward edits, which were preserved.

The sealed six-parent actual control (actual-sealed-v4.json) reaches
7194 progress63/completion, six committed Guild parents, six intents/deliveries
and six Character updates; its first contribution remains partial bit0. The
qualified table readers find no Character descendant of7194 and no tradeskill
reward for it. Four separate Guild AchievementComplete definitions (4144,4146,
5860,7028) remain relevant: preserve their same-Guild forwarding in the original
parent transaction; the synthetic rejection and retry controls pass.
They do not reference7187..7192. These controls prepare eligibility and do not
qualify raid/group admission or unknown native prerequisite types.

## Reproducible result and continuation

Final authority is artifacts/continuation-20261106/guild-achievement-atomicity/:

- candidate-build-20.log: clean owning/test build, existing ASPIRE010 tooling warning.
- results/sealed-owning-v4.trx:240/240; actual-sealed-v4.json: both actual controls.
- sealed-probe-v4/bin: frozen qualified DLLs; sealed-source-v4 and its manifest:13 files.
- sealed-source-symbol-proof-v4.json: all13 match their compiler document checksums.
- final-reviews-v4.json: separate zero-finding axes; sealed-evidence-v4.json and
  seal-v4.py validate baseline failure, compatibility, scope, data and candidates.
  SourceSnapshots/DLLs named final-* are the preceding validated candidate;
  sealed-* also includes the uniform60-bit random receipt suffix. Both pass240
  cases and both actual controls. This retained set is the Source/SQLite baseline;
  the following provider result includes the four-file timestamp correction.

Tracked coverage is [guild-character-achievement-atomicity-20261106.json](coverage/guild-character-achievement-atomicity-20261106.json).
Unreleased protocol1..3 candidates and all failing/intermediate reports remain
retained; they were isolated test data, never deployments. Protocol4 is the
qualified implementation. Every recorded process/session from these completed
checks is terminal. Do not rerun unchanged baselines or old probes.

## Configured MySQL and bounded restart qualification

The new handoff itself passes51 cases on owned MySQL8.4.13 with required TLS,
configured execution strategies and all49 Character migrations per fixture.
The three final shards contain17 cases each:48 source handoff cases, actual7194
rejected-parent and six-parent controls, and the acknowledged-date regression.
Account/Auth and prerequisite admission are prepared fixtures. This is distinct
from the earlier57-case creation-provider result and from hosted Auth SQL.

The provider regression found a previously acknowledged achievement whose live
UTC timestamp ended in05.8. MySQL initially stored06; changing the session's
fractional-time mode made an independent cast produce05. The experience owner
correctly retained06, but delivery validation used05 and rolled back valid
Character feedback. The frozen baseline records ChildRows0/IntentRows1/
DeliveryRows0. Delivery now receives the experience owner's exact frozen SQL
target, including retained dates, instead of independently recasting live
timestamps. The final observation keeps06 and records1/1/1. Protocol4 and the
schema are unchanged; no authoring/reference database was added.

This follows MySQL's documented [fractional-time rounding and session truncation
behavior](https://dev.mysql.com/doc/refman/8.4/en/fractional-seconds.html).
[MySqlConnector's date-storage guidance](https://mysqlconnector.net/troubleshooting/datetime-storage/)
also documents default `DateTime.Kind.Unspecified` reads. Connection settings
were not changed to hide either representation issue.

The final physical controller passes seven cases: before parent commit, after
parent commit, recipient before flush, recipient before commit, recipient after
commit, after ordinary save/event drain, and a normal control. The first six
terminate only their validated producer PID/start/token/assembly/checkpoint.
Each case uses two separate fresh recovery processes. Independent audits retain
complete achievement rows and every receipt ID/payload/timestamp, preserving
both existing paid-creation finance receipts. A partial scalar remains progress1,
credited mask0 and incomplete; the delivery marker exactly mirrors its original
intent. The event-drain checkpoint does not claim observed packet acceptance.

A further producer is terminated after parent commit, retaining an undelivered
intent. Both this pending schema and the already-delivered normal control survive
normal MySQL shutdown and a new engine generation using the same verified datadir
and configured endpoint. Exact pre-recovery witnesses match after startup. Two
fresh Character recovery processes per schema recover the pending credit once
and leave delivered credit unchanged. Changing the endpoint would change the
guarded database route; changed-route migration is a separate qualification.

The owning build passes with the existing ASPIRE010 warning. All240 scoped
achievement/Guild and104 experience-owner SQLite tests pass. An initial direct
DLL invocation lacked testhost runtime metadata and aborted before tests; the
complete104-case rerun used the project with identical qualified test DLL hashes.
Separate final Standards0 and Spec0 reviews cover the production correction,
provider helpers, exact process witnesses and guarded restart/cleanup controls.

All120 owned schemas across13 retained inventories are independently absent.
Both MySQL generations shut down normally, with recorded generation, listener
and datadir-process absence. No native client, CUA, Java or Ghidra was launched.
The local evidence root is
`artifacts/continuation-20261106/guild-achievement-atomicity/mysql-qualification/`:

- `date-evidence` and frozen `date-runner/bin`: failing acknowledged-date baseline.
- `date-fixed-runner/bin`, `qualified-source`, its15-file manifest and
  `qualified-source-symbol-proof.json`: qualified runtime/source binding.
- `final-shard-0..2`, `sqlite-results`: provider51 and scoped SQLite240+104.
- `physical-restart-v2`, `pending-db-restart`, `database-restart`: exact loss,
  fresh-owner, engine-restart and cleanup witnesses. Preliminary process results
  remain separate. Final deletion uses the explicit-acknowledgement restart runner.
- `final-reviews.json`, `seal.py`, `sealed-evidence.json`: checked bounded result.

Tracked provider coverage is
[guild-character-achievement-mysql-restart-20261106.json](coverage/guild-character-achievement-mysql-restart-20261106.json).
Retain the earlier Source/SQLite coverage and all terminal baselines.

Next qualify Guild publication's UTC interpretation of provider-loaded dates.
The [paused machine-transfer handoff](PROVIDER_PACKET_HOSTED_RESTART_HANDOFF.md)
records the follow-up: Microsoft's contract treats unspecified values as UTC
in `ToFileTimeUtc`, so the current call is not a reproduced offset defect.
Six unfinished fixture checks failed before timestamp comparison and are retained
outside active Source. Provider packet and hosted restart gates remain open.
Prepared recovery does not qualify autonomous hosted World/Auth startup,
socket/native delivery, raid earning/all-Guild-group admission, prerequisite
types290/203, all-member fanout, durable global realm-first arbitration, other
providers or changed-route recovery. Client/CUA remain stopped.
