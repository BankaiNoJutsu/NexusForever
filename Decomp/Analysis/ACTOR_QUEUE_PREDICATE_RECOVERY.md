# Actor-queue predicate failure recovery

1 November 2026. All 10,917 full solution tests pass with zero failures/skips,
retaining every previous 10,909 identity and adding eight cases. All thirteen
queue cases, 264 prior marketplace cases and 297 prior reviewed mail cases occur
in rebuilt full. Full solution and subsequent test-copy rebuilds have zero
warnings/errors. Source behavior only; this is not retail qualification.

Inspection for captured-bid actor preparation found CanExecute outside the
queue's error handling. A throwing predicate remained at the head, poisoning
later ticks. False predicates previously removed into the local deferred list
were also lost on unwind. An error hook throwing after Execute failure could
lose that list even though execution faults otherwise had an established
reporting/removal policy.

The executed thirteen-case baseline had six failures and seven passes: four
Standard/Blocking and ordinary/thread-safe predicate cases, plus two throwing
hook phases failed. Five prior cases and two new Execute-error controls passed.
The fix catches predicate faults, removes them before reporting, and reuses the
existing error path. Deferred restoration runs in finally. Error-hook exceptions
still propagate; false predicates, blocking order and external inbox transfer
keep their prior behavior. Failed callbacks are not retried. All thirteen
focused cases pass, including the existing 2,000 cross-producer deliveries.

Both independent read-only reviews have zero findings at pin
082973398ccf87bcc91b47d4e26c6bd10b91163c07678b5ee56eab44c76da36d.
Three 6,116-input archives/nine owning-test-copy DLL pairs and raw snapshots
verify. The changed Shared DLL differs from baseline. Exact local proof:
artifacts/continuation-20261031/event-queue-recovery/verification.json.
Historical handoff verification rejects evolved Source against checkpoint10277;
its failure is retained. Final git diff --check is clean.

This adds no queue cancellation/shutdown, multiple-consumer, callback-replay or
cross-actor handshake semantics. Full captured financial graphs, unknown commit
classification, original-actor preparation/adoption, direct delivery, configured-
provider/native/Contract/broader requirements remain. No client/Computer Use
launch, configuration/deployment, entitlement/default change, commit or push.
Defaults stay15:1/17:1;44/seven baseline grants stay disabled. Goal remains active.
