# Confirmed marketplace save outcomes

1 November 2026. All 10,909 full solution tests pass with zero failures/skips,
retaining every previous 10,896 identity and adding 13 cases. All 264 marketplace
cases and 297 previously reviewed mail cases occur in rebuilt full. The full
solution and subsequent test-copy rebuilds report zero warnings and errors.
This qualifies source/private SQLite behavior, not complete financial recovery
or live-provider/native/retail behavior.

Global marketplace Persist/PersistRequired and marketplace mail previously
treated every SaveBlocking exception as failed persistence. An error after
CharacterContext had observed terminal commit could therefore trigger book/
wallet compensation or restore an attached Item's old owner despite committed
SQL. A shared marketplace-only helper now wraps those three calls. It observes
terminal completion after the enrollment callback finishes, preserves confirmed
commit across a later exception, rejects normal return without confirmation,
and propagates unconfirmed errors. Diagnostic logging cannot reverse success.
It adds no context factory, transaction executor, provider strategy or replay.

The baseline Source included a behavior-preserving extraction whose helper did
exactly persist(enroll), plus the tests. It was not an untouched parent tree.
Eleven executed cases produced four failures and seven controls: late observed
commit errors(sync/async) threw despite real row presence/independent terminal
confirmation, and normal return from an uncommitted outer transaction lacked
the expected failure. Controls preserve early unobserved physical-commit errors,
genuine SQL rejection, enrollment failure and normal persistence. Enrollments
are counted; physical presence alone never becomes confirmation.

The first prototype did not compile the changed helper. Copy-Item preserved its
older timestamp; an incremental Game build reused the baseline DLL. All nine
negative/prototype DLL hashes were identical, while helper Source differed, and
the run repeated the original four failures/seven controls with forwarding-code
stack lines. This cohort is retained and excluded. It is not a new implementation
failure or fix proof. A forced Game rebuild changed the DLL, refreshed test copies
and passed all eleven cases. Always force owning-project rebuild after source
copies; a successful incremental build alone is not compilation evidence.

Expanded tests prove a no-change save remains valid and an early partial commit
cannot acknowledge incomplete later enrollment. All thirteen pass in the 264-
case run and rebuilt full suite. Unknown early acknowledgement remains an
explicit error even when SQL physically contains the row. Full immutable graph
classification is still required to recover that outcome safely.

Final Source/Tools review pin:
c02b77ea443cee2865862a3d85285f400a470f7b8d98f4eaacc42365b3536d91.
Both independent read-only axes have zero findings and verify four raw snapshots.
Five retained 6,116-input archives/nine owning-test-copy DLL pairs include the
explicitly excluded stale prototype; four cohorts bind valid executions. Local
exact proof:artifacts/continuation-20261031/marketplace-confirmed-save/verification.json.
Historical handoff verification still rejects evolved Source against checkpoint
10277; its failure remains retained. Final git diff --check is clean.

This fixes confirmed terminal-error handling only. Unknown acknowledgement,
complete wallet/listing/Item/mail captures, multi-party lock ordering, original-
actor adoption/publication, normal-save exclusion and durable direct delivery
remain actionable. Provisional wallet packets and offline mail-ID allocation
are not made atomic by this helper. Contract and broader feature/evidence work
also remain. No configured-provider/socket/load/native/retail promotion, client/
Computer Use launch, entitlement/default change, commit or push. Defaults remain
15:1/17:1;44 and seven baseline character grants stay disabled. Goal stays active.

06:23 UTC upstream refresh:55 open PRs,608 newest open; all58 tracked heads
unchanged. Created-since29 October returns609/608, merged-since returns598 only.
Direct609 metadata remains closed unmerged with unchanged head; discussion query
returns no entries, so no closure reason is inferred. No new source reversion.
