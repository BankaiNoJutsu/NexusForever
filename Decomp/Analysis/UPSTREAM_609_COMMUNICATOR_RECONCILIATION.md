# Upstream609 communicator reconciliation

Status refresh1 November03:30UTC: upstream609 is **closed without merging**
(closed_at2026-11-01T02:58:37Z; merged=false, merged_at=null). Its head remains
`f9b6f7356798a41beea0768ff1f6b5a758ffeca3`; game_rework is unchanged. The
comments query returned no explanation, so no reason for closure is inferred.
Current inventory has55 open PRs,608 newest open. All55 current open heads,
retained609 and both official branches remain unchanged from02:54UTC. The all-state
created-since29 October query returns609/608 only; the recent merged query still
returns598 only. Exact metadata, comments and refs are retained under
`artifacts/continuation-20261031/marketplace-rows/upstream-*`.
The tested local reconciliation is retained; closure alone is not contrary
behavioral evidence. Existing missing raw-table, independent Verified/native and
retail gates below stay open.

Discovered during the later 31 October 2026 continuation refresh. This supersedes
the earlier same-day inventory's latest-PR608 boundary. At discovery the inventory had
56 open PRs:609 was the only addition to the earlier55, none disappeared and all55
earlier open heads remain unchanged. Raw search, PR and pull-head responses are
retained locally under `artifacts/continuation-20261031/mail-handoffs`.

The [upstream PR](https://github.com/NexusForever/NexusForever/pull/609) was initially open at
`f9b6f7356798a41beea0768ff1f6b5a758ffeca3`, based on game_rework
`d8e43d2c3cc5a05f03e56dfc388288a4126c7ed9`. Its source behavior has now been reconciled in the fork, with the verification
and remaining evidence boundaries below. The integration retains injected
services, Quest save/reward ownership, starter/Contract receiverless exceptions,
existing packet names and current tutorial behavior. The full Goal is unfinished.

## Initial source comparison before integration

| Area | Current fork evidence | Gap at initial review |
| --- | --- | --- |
| State-triggered popup | `Quest.OnStateChange` already calls `message.Send`, then mentions a delivered quest | Retain popup behavior while evaluating scheduling/condition semantics |
| Calls with no delivered quest | `GlobalQuestManager.InitialiseCommunicatorQuestStateTriggers` indexes all communicator rows | Preserve this existing fix; avoid claiming upstream's original absence applies here |
| Levels and parent zones | `CommunicatorMessage.Meets` has the correct MinLevel comparison, MaxLevel, and a cycle-bounded zone-parent walk; tests cover parent matches | Compare exact gate ordering, race and missing-table behavior |
| Quest/state pairs | `CommunicatorQuestState` accepts server state numbers, with11 meaning only no record | Compare objective states5-10/14-19, active records, normalized absent records, repeatable state11 and states12/13 |
| Client action | Existing `ClientCommunicatorActionHandler` logs the report | Assess validated mention handling and stale/invalid reports without bypassing quest admission |
| Scheduling | Current state trigger is immediate | Assess event/objective queueing, deferred conditions, expiration, world/zone/level arming, chain and client-owned row classification |

`Source/NexusForever.Game.Tests/Quest/CommunicatorMessageTests.cs` currently
asserts that known Completed quests fail state11. A change to repeatable quest
semantics needs new evidence-specific tests, not deletion of the existing
nonrepeatable behavior. The tutorial communicator checkpoint tests and existing
quest/Contract ownership suite remain required regression gates.

## Evidence boundary and next actions

The PR cites build16042 addresses for the client flag gate, condition predicate,
quest record getter, callback offer check and client report. These are upstream
author claims, not newly reproduced local native evidence. Its4105-row table
classification and scratch harness are also reported evidence; the scratch
harness is absent here. The PR itself explicitly leaves the reputation inequality
unverified and scripted/unconditioned rows without server triggers. Do not infer
retail completeness or invent triggers from those gaps.

## Integrated source behavior

The complete26-file, three-commit patch was fetched as the pinned local Git ref
`reconciliation/pr609`. The recorded patch is under
`artifacts/continuation-20261031/communicator609/upstream609.patch`. Integration
adapts its components rather than replacing the fork's larger QuestManager or
Player implementations.

- `CommunicatorIndex` classifies server, client, world and chain/script rows and
  indexes state/objective triggers once. Missing Quest2 metadata fails all quest
  conditions; missing state columns fail closed. The old state-trigger lookup
  remains an inspection API, while automatic production uses the new index.
- `CommunicatorConditions` implements the pinned upstream interpretation of
  effective states, objective bits, states11/12/13 and repeatability. Offers
  still require no existing record, normal start prerequisites and callback
  flag0x4. State11 permitting a repeated Completed quest does not make that
  existing quest a new offer. Flag0x10000 does not replace the callback flag.
- `CommunicatorTracker` drains after Quest updates, coalesces reentrant delivery,
  handles new Accepted records and objective completion (including automatically
  completed optional objectives), and rechecks pending conditions while their
  trigger holds. It checks zone rows on entry and level rows on a crossed level,
  without sending every passed level at login. The one-second pending poll,
 300-second lifetime and four-round drain bound are inherited server policies,
  not independently proven retail timing.
- Automatic server-owned calls send the existing packet with CheckConditions
  true, then mention an offered quest. Client-owned rows are mentioned without
  a redundant server push. Explicit scripted Send(session) calls retain the
  original packet shape and localized-text guard. The existing Meets predicate
  and getter APIs remain; indexed messages retain the prerequisite/table
  services needed by older predicate callers. Parent-zone cycle guards and the
  fork's minimum-zero/unbounded-maximum reputation behavior are preserved.
- The existing client-report handler now delegates to validated QuestManager
  handling. The handler first requires the caller's actual Player session and
  account to match; actor validation alone cannot establish that request binding.
  Only engaged, currently reachable/startable offers with no current
  record can create a mention. Zone is skipped for report/accept because the
  client can have left it, while world, class, race, faction, prerequisites and
  other gates remain. A known Mentioned quest remains acceptable from its missed
  call list; ordinary acceptance prerequisites and Contract exceptions remain.
- The actor retains its original manager, account/session, Character and realm.
  Loading, frozen normal saves, reward/Contract work and inventory ownership hold
  delivery. Stale/invalidated owners discard queued calls; reentrant publication
  cannot mention a quest after losing its owner or becoming busy. This preserves
  the fork's persistence/adoption boundary for provisional reward state.

## Verification

The full solution rebuild passes with zero warnings/errors and **10,499 tests
pass**, zero failures/skips.71 communicator cases and seven cleanup cases were
added since10421; all prior test case names remain. All6,053 source/tool/binary
inputs and complete frozen archive members match. The verified result is in
`artifacts/continuation-20261031/communicator609-cleanup/verification.json` with frozen
binaries, full build/test logs and TRX. The earlier mail proof remains historical
and has not been rebound. Six real pre-change failures were executed and frozen
with exact source/tool hashes and Game/WorldServer/Abstract/test binaries: one
race-gate failure and five table-state failures. Compile/fixture attempts are
retained separately. Intermediate gates pass80 communicator/partial-table cases,
2,489 quest/Contract cases and then90 communicator cases, before final additional
service-injection, reputation and publication-reentrancy checks.

The initial10,489-test and NaN-corrected10,490-test full runs each passed with
zero warnings/errors and retain distinct manifests, frozen binaries and complete
6,051-input archives. They were not rebound after later changes. Final review
executed one introduced NaN regression with three finite controls and two
introduced foreign-caller routing regressions. The inclusive original reputation
predicate now rejects NaN exactly as the fork did; the handler now applies the
same session/account binding as existing reward handlers. Corrected routing
fixtures exercise a genuine matching IWorldSession. The99-case communicator
focused gate passes before the final full run. These three introduced failures
are distinct from the six original pre-implementation negatives.

`communicator609-final` contains the NaN failure/controls and its passing full
proof; `communicator609-routing` contains the foreign-caller failures, final input
archive and both failed routing full runs. The latest passing full proof is
under `communicator609-cleanup`. The first archive's capture note records reconstruction
of only the later test line and its newline conversion; every archived payload
matches the original pre-run SHA256. A failed partial archive is retained and
explicitly labeled separately.

SQLite lifecycle tests verify that a client-reported Mentioned quest retains its
save intent after rollback, commits once, reloads as Mentioned and causes no
repeat push. Further regressions cover source quest and both objective producer
paths, optional objectives, packet order, pending conditions/expiry, missing
metadata, row classification, world/level checks, stale managers/sessions, shared
ownership barriers, client-report validation and missed-call acceptance. All
prior test case names must remain in the new full result.

Two routing full runs each reported10491 passes and one failure while removing
the existing Challenge fixture's temporary repository.sqlite: Windows reported
a sharing violation in Fixture.Dispose, in different Challenge cases. Both full
TRX/logs remain retained. Four lost-ack variants passed unchanged in isolation.
The lock holder/root cause is unidentified, and cleanup exceptions may obscure
earlier assertions; neither failed full run qualifies as passing gameplay.

The existing fixture cleanup now retries only Windows sharing/lock violations
for at most two seconds, after preserving its temp-root/prefix guard. Persistent
locks and other I/O/access errors still fail; gameplay deadlines are unchanged.
Seven in-memory exception tests include two executed pre-fix failures and five
controls. The focused communicator/cleanup/live-Challenge gate passes162 cases.
The rebuilt6053-input source/tool/binary set and complete archive were frozen
before that gate and the passing10499-case full run. This passing run establishes
that the complete suite passed with the bounded cleanup grace; it does not
establish which process held either earlier lock. An optional real file-sharing probe
was rejected by automatic review with only "blocked by policy"; it never ran.

## Later static corroboration

The [fresh build16042 byte cross-check](NATIVE_STATIC_CONTRACT_COMMUNICATOR_CROSSCHECK.md)
uses a REA-bound input and installed LLVM to corroborate selected state/objective,
offer-flag, push and delayed-recheck branches. Ghidra remains unconfigured and no
client was launched. Chained unwind groups and the one missing leaf entry are
recorded explicitly. This supersedes the blanket absence of local corroboration
below for those selected observations, not the raw-table/runtime boundaries.
The finite reputation inequality is now locally mapped: each zero bound skips
its comparison. The fork's retained zero-minimum floor is a concrete next source
correction; its new regressions and implementation are still pending.

## Remaining evidence

The client-native predicate addresses and full4105-row classification counts
remain upstream author evidence. The raw final-client tables and upstream scratch
harness were not available in this checkout or the restored lean evidence; the
workspace example's tbl path is absent. This does not establish their absence
elsewhere on the machine. No local full-table count is claimed. REA's native
Ghidra provider was observed unconfigured, while managed static inspection works.
No setup/configuration changes or native launch were performed.

Fresh actual-table classification/delivery, native call ordering and presentation,
provider/copied-upgrade/socket/deployment qualification, reputation inequality and
script/unconditioned/chain arming beyond the supported interpretation remain
open. The tracker is an ephemeral actor scheduler, not a durable call journal.
Source tests are not retail completeness. All broader Contract/retail, track35,
AMP/combat/content and assessment requirements remain; defaults15:1/17:1 and no
new automatic44/seven-character grants are preserved. Computer Use/native launch
remain stopped and no push is authorized.
