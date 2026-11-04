# Normal auction bid transaction

1 November 2026. Qualified source/private-SQLite boundary11,095: zero full-test
failures/skips, zero build warnings/errors and zero remaining findings on either
review axis. SourceTools pin d212179e32372141aad6dbc8423a4cf7b67dfe61fac093f74e47f418135efc11.
The [4 November MySQL qualification](DESTINATION_PROVIDER_QUALIFICATION.md#normal-bid-and-buyout-sql-settlement)
adds migrated-schema command and reconciliation evidence. Public-flow,
deployment and retail gates remain open.

The existing `ClientAuctionBuyOrderSubmitHandler` now sends persisted normal
bids through one owned operation. Its asynchronous callback retains the three
request scalars, original player/session and logger. Buyouts retain their
existing path. Original-actor complete ordinary saves precede wallet capture;
the loaded or confirmed-created Character/account/database identity authorizes
each currency owner. Self-bid increases debit only the difference.

The SQL command freezes listing/escrow preimages, wallets, Character owners,
refund-mail identity and command identity. It locks affected Characters in a
deterministic order, checks custody/deletion/expiration and writes listing,
buyer debit, online prior-wallet credit or offline refund mail and outcome
receipt in the same serializable transaction. A lost acknowledgement triggers
a fresh receipt observation; uncertain observations retain ownership. Later
SQL state is read and adopted without rewriting the historical bid outcome.

Original actors adopt the confirmed balances, apply deferred semantic currency
intent and persist their complete ordinary Character graphs before publishing.
Those saves have their own complete-graph receipt. A fresh locked receipt
absence proves rollback and allows the same frozen graph to retry on its
original actor, without repeating Auth saving or earning effects. Partial
enrollment stays owned too; unsealed flush/commit remains forbidden. Failed
observations retain the original owners rather than guessing rollback.

Account admission/deletion and listing mutations exclude the held operation.
Retired actors finish persistence while suppressing wallet/outbid replies;
replacement sessions receive no original request result. Separate actor queues
perform each actor's preparation, adoption and publication. No worker calls
live gameplay callbacks. Confirmed creation provenance is a prerequisite within
this milestone; it does not redesign creation-token settlement.

Evidence is local in `artifacts/continuation-20261031/normal-auction-bid/`:

- `public-enrollment-reproduction.trx`: six public-flow controls pass; two
  enrollment faults fail with SQL120 versus125 after abort and110 versus115
  after commit. The later+5 intent was not durable.
- `public-enrollment-corrected.trx`:43 pass, including both corrected faults.
- `public-lifetime-boundaries.trx`:113 pass, including public first/self/online/
  offline bids, main-acknowledgement loss, retirement/replacement, account
  exclusion, partial enrollment, rejected early flush and existing auction tests.
- `integrated-affected.trx`:1,037 pass; adds the insufficient-funds compatibility
  regression and existing bank/mail/Contract/Challenge/currency/migration cases.
- `masked-state-baseline.trx`: eight actual failures/eleven controls for a
  masked Player field cleared before a rolled-back save. The earlier19-case
  `masked-state-reproduction.trx` fixture query failure is excluded.
- `retained-graph-corrected.trx`:52 pass. `legacy-newer-intent.trx`: four pass,
  covering actual legacy keybinding creates/updates/deletes, tradeskill/schematic
  mutations and newer explicitly modified fields winning over retained values.
- `review-fixes-affected.trx`:391 pass, including consecutive rollback and an
  online prior bidder with a known-empty wallet.
- `replay-boundary-negative.trx`: two actual failures for incomplete retained
  insert merging and a foreign stored Item preimage. The latter is a raw
  reconciliation-layer control, not a public Inventory exploit reproduction.
- `replay-boundary-corrected.trx`:61 pass. Final review axes have zero remaining
  findings at SourceTools pin1d5bd36087005ef90182ab39ef997c389f0ca19396bdcb0f9863a46fbe00862d.
- Three full builds passed with zero warnings/errors, but full tests remain
  unqualified: `failed-full.trx`11,086 passes/six failures;
  `second-failed-full.trx`11,091 passes/one failure;
  `diagnostic-failed-full.trx`11,092 passes/two failures. Five initial readiness
  failures were corrected as a cleanup/admission fixture mismatch. Intermittent
  Challenge drain/teardown failures remain unresolved; isolated reruns passed.
  The third full observed one managed reward connection open during the wait,
  without identifying its exact SQL phase or native resource holder.
- Phase-only diagnostics and producer/routing coverage pass102 cases;28 healthy
  cost controls pass, with2.627–5.061ms total origin capture per traced fixture.
  Both review axes have zero findings for the diagnostic delta. `phase-full.trx`
  passes11,095 cases at SourceTools pin
  d212179e32372141aad6dbc8423a4cf7b67dfe61fac093f74e47f418135efc11;
  its full build passed with zero warnings/errors in29.19seconds. Full tests
  took11m40s (command712.27seconds). The6,131-input archive and nine DLL pairs
  verify in `verification.json`. No timeout cause is inferred. Detailed failure records
  and limitations are retained in the local `CHALLENGE_TIMEOUT.md` artifact.
- `review.diff` compares this consolidated milestone against the qualified
  wallet11,019 archive. Earlier failed fixture/build experiments remain saved.

The MySQL and SQLite migrations add the three-column `marketplace_bid_receipt`
table. Generation/model/source checks do not qualify an installed MySQL schema,
configured-provider execution strategy, upgrade, socket or native operation.
Actual provider/table/gameplay evidence remains open. The historical handoff
checker deliberately rejects changed Source; its recorded checkpoint result is
reused under the continuation policy.

Remaining economic work includes buyout, proceeds/direct item delivery and other
multi-party operations. Contract public07CE claim/reset remains disabled until
unclaimed/excess-point rules and all726 canonical reward choices are qualified.
Entitlement defaults15:1/17:1 remain; no44 or seven baseline grants, native client
launch, setup/configuration, commit or push is authorized by this record.
