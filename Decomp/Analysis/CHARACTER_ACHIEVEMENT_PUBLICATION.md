# Committed character achievement publication

Continuation after `d859c3f46e9f9fa21d61045c2d0602351e501b6f`, 3 November 2026.
The broader assessment Goal remains active.

## Observable behavior

Real CharacterAchievementManager progress/grant producers now defer private
achievement updates and realm-first announcements until the original complete
experience save graph confirms its captured SQL values. SQL completion only
retains immutable publication data. The original session's thread-safe event
queue performs delivery on the world update, after an active normal save ends.
Failed writes, caller/savepoint rollback and lost acknowledgements retain intent;
confirmed recovery publishes the original captured values once through this
in-memory queue. Pre-Auth snapshots and later live progress stay distinct.

Initial packets use the acknowledged publication baseline. FIFO updates rebuild
fresh packet objects on retry. Realm-first recipients retain independent cursors,
so an acknowledged recipient does not repeat after another recipient fails.
Stable source/recipient identities, the current player registry, manager and
session references suppress obsolete delivery. Exact queue-head ownership guards
reentry. Outward packet construction/enqueues occur outside the short metadata
lock so a concurrent SQL completion can finish while an earlier send is paused.
Existing interface-only IPlayer behavior remains compatible.

This does not change the existing in-memory realm-first acceptance reservation
into durable arbitration. It defers its announcement until confirmed persistence.

## Executed evidence

Local evidence is under
`artifacts/continuation-20261103/character-achievement-publication/`.

- `before-world-session.trx`: seven failures and one unchanged-save control using
  the corrected real IWorldSession/Player/registry fixture against the original
  preimplementation Game/World DLLs. The copied test runtime, fixture source hash
  and exact original production hashes are retained in its binding record.
- `before.trx` retains the earlier seven failures/control with a bare IGameSession.
  `candidate.log` records two compile corrections. `candidate-corrected.trx`
  records seven fixture failures because that bare session did not implement
  IPlayerSession; these are excluded from production qualification. Correcting
  the fixture gives16 focused passes; a reentry case then joins398 affected passes.
- `review-before.trx`: three actual failures show a terminal SQL observer whose
  completion waits for a paused outward send, an obsolete realm-first recipient
  receiving a retry, and a withdrawn source actor receiving a private update.
  The bounded wait releases the paused callback before asserting, leaving no
  intentionally deadlocked worker. The corrected candidate passes20 focused cases.
- `affected-final.trx`: all401 selected achievement, experience, normal-save,
  collection, exploration and progression cases pass. The prior housing test's
  analyzer warning is corrected with the equivalent Assert.Single predicate
  overload; no housing behavior changes.

## Verification and retained failures

The corrected candidate is recorded separately under
`artifacts/continuation-20261103/character-achievement-publication-corrected/cache-fix/`.
All **23 new cases**, **1,121 affected cases** and **11,954 full solution tests**
pass, with zero full failures/skips and all11,931 prior cases preserved. The full
solution build has zero warnings/errors. Both Source review axes are clear across
12 paths at manifest SHA256 `a94d8ae677a413b30574836fb3b17f23d04bb0285edfebcd8cc7092caa8bd1cf`.

The preceding provider gate passes23 invocations:20 live character achievement
cases,1 live guild graph round trip and2 offline provider-model controls. Those
method sources and all11 production DLLs remain unchanged. Six changed retry
fixture cases separately pass live MySQL8.4.13 on the final test DLL. This is
versioned23+6 evidence, not29 executions on one DLL. The owned server generation/
listener and recorded validation generations are absent after shutdown.

The initial full run retains11,950 passes and one existing guild seed-graph
failure. A no-write cached-model probe reproduces temporary rank0 values255
downward and a collision with explicit rank9 at graph247. Two provider-model
regressions fail on the original mapping. GuildRank Id/Index are application
assigned; ValueGeneratedNever now preserves their explicit zero keys. Runtime
mapping and both current snapshots retain provider-specific column types and
defaults. Three new guild regressions and the original failed guild case pass.
The first correction's two SQLite affinity failures remain retained. Both final
provider create scripts are byte-identical to their originals and both snapshots
match their models; no DDL/table migration is required.

The superseded initial20-case MySQL run retains19 passes and one cleanup command
timeout after RejectedSqlRetainsProgressWithoutPublishing(False) returned. Both
unchanged false/true diagnostic cases then pass, including cleanup, with unchanged
compiled tests and deadlines. The original timeout's cause remains unproven.
Narrow cleanup samples have no active InnoDB transactions and include checking
permissions/waiting-for-handler-commit states; they are not an original-failure
trace. The corrected23-invocation run is a separate fresh candidate qualification,
not evidence that the earlier failure did not occur or that its cause is fixed.

The second full run retains11,948 passes and6 failures. Four presence cases
directly report EF's internal-provider cache warning; two atomic cases fail before
their actor callback. A bounded warmed-cache replay reproduces all6 with cache
warnings. The two fixtures now configure their original strategies through the
selected provider's ExecutionStrategy callback instead of service replacement.
All56 affected fixture cases and6 warmed-cache cases pass;5 deliberate warm-
profile warning controls still fail as expected. Warning policy, assertions,
faults, retry counts and deadlines remain unchanged. The two swallowed original
constructor errors were not directly traced; the replay supplies their diagnosis.

Source/Tools SHA256 `4d6bb244ceaa2636f498a0ba282832af686d51d9514fd372b1cc3008624f9b5d` binds6,248
inputs. Test assembly SHA256 `782b6564eb5769b7740ccd04f46c8da7621c5e7b73f28520250921cfe858b58c` and all
11 owner/test/provider DLL triples match. verification.json
retains the full/affected/provider results, model proof and executed negatives.
The earlier6247-input/c43d4238 candidate and its failed full/provider results
remain historical and do not qualify the corrected binaries.

Observed durations: build24.917s; corrected provider
33.721s (6 final cases, separate from the earlier23); full test command714.862s.
These are validation durations, not performance claims.

## Remaining work and limits

A separate shared-store probe reproduces the next arbitration gap: two actual
GlobalAchievementManager instances initialize against the same configured
CharacterDatabase. After the first claim and a controlled committed completion
write, the already-loaded peer still accepts; a freshly loaded peer rejects.
The store has exactly one completed row. Table501 metadata and the completion
write are controlled; this is not a two-player/public-completion, migration,
distributed-winner or retail qualification. next-realm-first-evidence.json and
next-realm-first-binding.json retain the original manager/store observation,
fresh-load control and frozen source/test/11-DLL bindings.
The same probe also reproduces against the corrected candidate in its separate
artifact directory. That directory's next-realm-first-evidence.json and
next-realm-first-binding.json bind that candidate's6,248-input source, test
assembly52d5dfec and all11 owner/test/provider/probe DLL copies. This predates the
two retry-fixture corrections; the11 production DLLs remain unchanged. Both probe
records retain their original bindings. This still uses a controlled completion
write and does not qualify two public player completions or a distributed winner.

Durable cross-process/global realm-first arbitration, restart/outbox delivery,
cross-character notification atomicity and companion title/archive/tradeskill/
other reward publication remain separate actionable roadmap requirements.
The next arbitration acceptance gate must exercise two real character/guild
producers against one store, with the claim and original achievement graph
committing together. Cover rollback, uncertain acknowledgement and a stale peer;
preserve separate character/guild scope and historical completed rows without
inventing a historical winner. The existing first-claim path selects an
announcement after owner progress completes; preserve that behavior. Save
validation callbacks prohibit writes and ordinary saves can use EF's implicit
transaction, so an independent claim write cannot establish the required atomic
boundary. The current direct-claim probe alone cannot close that integration.
The existing acceptance reservation is still in memory; partial socket delivery
may repeat on retry. Source tests do not establish exactly-once delivery.

Fixtures exercise the actual public manager, Player save and relational contexts;
lookup tables, player registry and sessions are controlled. Native presentation,
full retail content, deployed broker/socket behavior and retail timing remain
unqualified. Contract public07CE/reset/unclaimed/excess/full726-choice evidence
requirements and other documented external inputs remain open.

The guild-rank correction changes database mapping/snapshot source; complete
provider create-script equivalence is verified without DDL changes. No
entitlement/default change is included. Defaults
15:1/17:1 remain; no automatic44 or seven baseline character grants. Computer Use
and native client launch remain stopped.
