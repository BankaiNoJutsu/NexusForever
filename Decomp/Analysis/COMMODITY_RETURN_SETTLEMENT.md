# Commodity cancellation and expiry settlement

## Live expiry notice recovery

4 November 2026. A confirmed expiry releases the original order and account
reservations before scheduling the removal notice. Each send builds a fresh
packet from the captured order. An enqueue failure retries on the original
actor and queue. A rebound session or a live order with the same ID suppresses
the notice. The cancellation request still returns its result through its
existing path.

Two baseline tests reproduce the enqueue failure escaping the expiry event.
Their controls cover a successful retry and replacement during the failed
send. Both check that the command runs once and the account and order
reservations are free before the notice is attempted. The owning build and 83
scoped tests passed, including the commodity command and legacy handler
controls. Test execution took 54 seconds. Evidence is in
`artifacts/continuation-20261104/commodity-expiry-publication/`.
Restart replay, deployed socket delivery and native presentation remain open.

## Original settlement qualification

Completed in source/private SQLite on1November2026. Full solution build:
0warnings/errors,22.53seconds MSBuild (23.13seconds command).
Full solution tests:11,264passed,0failed/skipped,642.34seconds command.
824affected checks pass (3m7s). Final Spec0 / Standards0 findings.

Public commodity cancellation and real manager expiry delete the order atomically
with full held Credits refund for buy orders, or normal fungible Item/satchel/
fixed-mail returns for sell orders. Price is total buy escrow, not Price times
Quantity. Direct stack merges, binding/metadata, split stacks, material eligibility/
cap, offline/full-bag mail, absent wallets and legacy IPlayer adapters retain their
semantics. No won/outbid/auction-expiry packet policy is invented.

Original Character/account/database/session ownership is retained through
preflight, receipt-backed SQL outcome resolution, complete ordinary reconciliation
and per-packet publication. Accepted later ItemCreate intent survives main/late
custody conflict, prior ordinary rollback and bound metadata in fixed recovery
mail; conflicted inventory requires authoritative reload. Material intent rebases
against current SQL without replaying the main return. Exact opaque rollback
proof owns cached-write retirement and Item uncertainty resolution. One scope
starts before wallet publication, protects resident Item scalars/cached codec
collections/nested rune slots and satchel APIs/direct setters, and releases with
the original actor. Lazy default sockets initialize before ordinary save so
SQL/live/wire agree and subsequent crafting works. Frozen initialization allowance
cannot absorb pending semantic rune edits during witness refresh.

Evidence: artifacts/continuation-20261031/commodity-returns/.
- qualified-affected.trx:824passed, including39 public commodity cases,
 18 new SQL-role/graph cases, actor/recovery/currency/mail/Item/normal save,
 rune/microchip controls, two public legacy-socket/crafting controls and exact
 semantic-write retirement/retry.
- full-build.log/full-build-time.json, full.trx/full-test.log/full-test-time.json
 record the one final coherent full gate.
- review.json/review-inputs.json/review.diff and review-reports.md bind final
 review. Earlier initial through sixth reports preserve resolved findings.
- final-inputs.json/final-inputs.zip/final-binding.json/verification.json bind
 6,145inputs and9owning/test-copy DLL pairs.
 SourceToolsSHA256:8c4f31890fe18e80f2f8f353e1e74a9c3656b2c0eaeefb23eadaf83ef3a3f8b7
 ArchiveSHA256:13505e5a6ccf21c647d22f0829576be40a41dabbdbab956bf7d49be5b7dccc4d
 VerifiedUtc:2026-11-01T20:24:00.812569+00:00

Meaningful failed evidence remains: producer-sql-negative (legacy public adapter,
not archived baseline binaries), review-negative (lost accepted grant/material
retry/publication), late-grant-negative (late/rollback/bound recovery),
wallet-publication-negative (API mutation after final drain),
item-publication-negative (wire stack4 versus SQL3, durability/codec mutation),
and stored-sockets-negative (missing first-delivery socket). Iteration-history.md
retains detailed prior counts and corrections. item-publication-exclusions.md
records the test namespace build failure and excluded accidentally launched
older-DLL affected run. final-affected.trx822passes at superseded92691 is historical;
qualified-affected.trx is the final candidate result. No assertion/deadline was
weakened, no overlapping output mutation or native/process intervention was used.

No new schema/migration or runtime/provider setup. Historical Verify-Continuation
checks remain retained; later Source divergence is expected and old records were
not rewritten to pass. Configured MySQL execution-strategy/isolation/locking,
deployment/copied-upgrade, actual-table, socket/native and gameplay/retail gates
remain separate. Contract public07CE/reset/unclaimed/excess/full726-choice delivery
remains held on its documented evidence gates. No automatic44 or seven baseline
grants; defaults15:1/17:1 retained. Computer Use/native launch remain stopped;
no commit or push.

Next integrated outcome: public commodity posting/matching/immediate remainder
settlement. See [the source-grounded continuation note](../../artifacts/continuation-20261031/commodity-posting-next.md). The overall Goal remains active; this
milestone does not complete the remaining assessment or broader feature work.
