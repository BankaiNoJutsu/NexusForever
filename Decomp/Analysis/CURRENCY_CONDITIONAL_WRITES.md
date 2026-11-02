# Currency conditional writes

1 November2026. All10,955 rebuilt full solution tests pass, zero failures/skips,
retaining every prior10,931 identity and adding24 cases. All24 conditional-write,
14 previous frozen-currency,264 marketplace and297 reviewed mail cases occur in
qualified full. Full/test-copy rebuilds have zero warnings/errors. The corrected
focused currency/Contract/frozen run passes all156 cases.

A real second SQLite connection committed a balance change after row staging and
preflush validation, immediately before the first implicit transaction opened.
The old UPDATE checked only Id/CurrencyId and silently overwrote it. Eight
executed failures cover ordinary, frozen normal, legacy bank-row and frozen
bank-row paths, each sync/async. Sixteen unchanged/deleted-row controls passed.
Runtime and both provider snapshots now mark original Amount as concurrency.
All24 fixed cases pass; rejection preserves the external balance/deletion, rolls
back coupled Character TimePlayedTotal and rejects a fresh stale-owner retry.

This is the public row-save EF boundary. Existing bank and real Player/Contract
executors already use explicit aggregate transactions; these tests do not claim
that each allows the exact injected SQLite interleaving. All inspected production
currency writers retain loaded originals or Add absent rows. Raw/ExecuteUpdate
SQL bypasses automatic predicates; Amount alone cannot detect ABA or prove a
whole economic receipt, actor/account/database provenance or unknown outcome.

The first full gate had10,889 passes/66 failures, all in one Contract test class.
Its shared helper attached a target-only Amount125 stub against seeded100,
unlike the actual Currency writer. Post-flush fault tests consequently failed
before reaching their intended fault. The helper now loads/attaches the actual
original wallet before assigning125. Every test identity and assertion remains;
Character partial-stub coverage remains. No expected exception was broadened and
no production original-value fallback was added. The failed full run, initial
24-case positive, original eight negatives and initial alias compile failure are
retained separately. Corrected focused and full gates pass.

The first corrected-source default full then had10,954 passes/one Windows
fixture-sharing failure in a Challenge lost-ack producer case. Its TRX retains
a run-level20.014-second drain diagnostic with an execution task/lease still
present; the console omitted it. The original-case stdout/stderr is empty, so
the proposed masked-drain/teardown association remains an inference. Four same-
binary producer variants and the separately recorded default full retry pass.
No deadline was lengthened and no source/configuration was changed for that
retry. The failed full remains retained. A later Restart Manager query of one
observed leftover SQLite file returned zero current users, which does not reveal
the failure-time holder or prove its cause. Scheduling/provider/lock delay and
the fixture worker lifetime require further failure-time evidence.

Both providers' isolated original-snapshot/current-runtime model differences
show zero physical migration operations, zero connection attempts, and currency
tokens0->1. Frozen original DLLs and current models/probe source/DLL are hash
bound. No DDL migration or provider deployment is needed for this metadata-only
change. This does not qualify live MySQL predicates/isolation/affected rows.

Both final read-only reviews have zero findings at pin
77101437bf42b49987e6668591e47ba5a0904c9646dbcda6cfc28f58b81074cf.
Five6,118-input archives/nine owning-test-copy DLL pairs verify, including the
first failed full and corrected epochs. Exact local proof:
artifacts/continuation-20261031/currency-concurrency/verification.json.
Historical handoff verification still rejects evolved Source against checkpoint
10277; its failure is retained. Final git diff --check is clean.

Fresh08:53UTC upstream metadata has55 open PRs,608 newest open/already
reconciled;609 remains closed unmerged with the same tested head. All58 tracked
PR/branch heads are unchanged from07:51UTC, including revised604. No closure
reason is inferred.

Loaded wallet provenance and original-actor coordination, full captured
listing/wallet/Item/mail graphs, known rollback/deferred reconciliation, ambiguous
commits, direct delivery and live provider/Contract/native/retail/broader work
remain. Source/private SQLite tests do not establish retail completeness.
Defaults15:1/17:1; no auto44/seven baseline grants. No client/Computer Use launch,
configuration/deployment, commit or push. The broader Goal remains active.
