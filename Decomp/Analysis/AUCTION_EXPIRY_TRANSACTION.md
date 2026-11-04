# Auction expiry settlement

## Retry for terminal auction notices

4 November 2026. The live expiry producer reached committed item, wallet,
mail and receipt rows before the injected won/expired packet failure. All
three baseline cases exposed the exception on the actor event queue.

The committed transfer path now snapshots the won or expired notice and
retries a failed send on the original actor queue after one second. Every
attempt checks the session, bound player, account, queue and logout state.
The settlement finishes and releases its reservations independently of the
notice retry. Reconciliation, item transfer and mailbox adoption run once.
Winning and unsold expiry regressions exercise recovery and a rebound session.
The shared buyout terminal notice uses the same path. Outbid notices, request
replies, restart replay and wire or retail qualification remain separate.

The owning build and 203 scoped cases passed, including the three new expiry
regressions. The scoped tests ran in 68 seconds. Evidence is retained in
`artifacts/continuation-20261104/auction-terminal-notice/`.

## Original expiry settlement

1 November 2026. Qualified source/private-SQLite boundary11,173: zero full-test
failures/skips, zero build warnings/errors and zero remaining findings on either
review axis. SourceTools pin
53e199ffbf523fe7572a340798413457ba3ee1c0d7e1606a873bed89e9884ba4.

GlobalMarketplaceManager.Update now advances a retained settlement queue before
its one-second expiry scan. Winning expiry consumes the stored held bid exactly
once, delivers the same whole Item directly or by fixed mail, and pays seller
proceeds with existing fees/caps. It never debits the receiver again. Unsold
expiry returns the original Item to its seller without proceeds or new binding.
Offline-only cases complete on the manager queue without a player session.

The shared immutable command derives Bid/Buyout/WinningExpiry/UnsoldReturn roles
from complete stored preimages. It validates expiry time and receiver/account/
wallet/custody ownership, deterministic locks and SQL postimages, with durable
main receipt/current-state observation. Original online actors perform complete
preflight and ordinary reconciliation using the qualified delivery witnesses,
rollback recovery, elapsed-expiry hold and guarded publication. Zero-change
receiver wallets suppress settlement notifications and preserve absent rows;
later earning intent may create the qualified wallet normally.

Game.Abstract.IPlayerSession supplies the existing Player getter to autonomous
publication, without a WorldServer dependency. Real WorldSession publication
checks its original Player on every boundary and in deferred mail callbacks.
Retirement/replacement cannot retarget packets. Account and listing reservations
exclude competing purchase/cancellation/expiry. Legacy IPlayer adapters retain
their previous helper route; real Players require the admission coordinator.

Local evidence: artifacts/continuation-20261031/auction-expiry/.

- producer-negative.trx: actual real-producer failure: SQL100 instead of120 at
  the winning notification, before complete ordinary intent was persisted.
- initial-integration.trx105 pass, including the corrected producer and prior
  auction controls. lifecycle.trx16 pass for winning/unsold, online/offline,
  direct/mail, absent wallet, proceeds, acknowledgement loss and replacement.
- marketplace-integration.trx395 pass/three adapter-compatibility failures.
  Reordering adapter recognition before missing-coordinator refusal restores
  their original oracles; marketplace-corrected.trx398 pass.
- recovery-and-roles.trx139 pass. Adds absent-wallet later intent, ordinary
  rollback/lost acknowledgement, held competing buy/cancel requests, wrong-role/
  double-debit/proceeds/recipient/unreserved-slot and future-expiry rejection.
- Builds have zero warnings/errors. Initial Spec review has zero findings;
  Standards has zero findings after the naming advisory was addressed with
  receiver/isReceiver and explicit subtraction roles. Finalreview pin
  53e199ffbf523fe7572a340798413457ba3ee1c0d7e1606a873bed89e9884ba4
  binds6,134inputs. affected-integration.trx passes3,918cases in9m44s. Full
  solution build passes0warnings/errors in24.03seconds. The reviewed archive and
  nine owning/test DLL pairs verify in verification.json after the unchanged
  full run. full.trx passes11,173 cases in10m37s (command648.96seconds).

No schema migration is introduced. Live MySQL/configured strategies, deployed
upgrades, actual tables, sockets/native/gameplay and retail remain separate.
Historical handoff Source divergence is expected; reuse its recorded result.
Defaults15:1/17:1 remain, no automatic44 or seven baseline grants. Native client
and Computer Use remain stopped; no setup/configuration, commit or push.

Continue public auction cancellation/other economic recovery, then
Contract public claim/reset and full reward delivery and the broader feature
queue. Missing unclaimed/excess Contract evidence remains explicit.

18:16UTC upstream metadata returns no PR updated since17:14UTC;604/608/609
heads remain unchanged.608 is open;609 closed unmerged. Reuse their reviewed
reconciliation evidence. All implementation remains unstaged and uncommitted.
