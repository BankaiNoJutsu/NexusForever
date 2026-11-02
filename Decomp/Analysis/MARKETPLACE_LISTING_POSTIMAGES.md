# Marketplace listing SQL postimages

1 November 2026. Source/private SQLite qualification only. All 10,860 full
solution tests pass with no failures or skips, including all 215 marketplace
cases and 297 previously reviewed mail cases. The 141 owned-SQL cases and 74
remaining marketplace compatibility cases also pass separately. All 10,836
prior test identities remain; 24 cases were added. Full solution and subsequent
test-copy rebuilds report zero warnings and errors.

Loaded-row checks did not prove what SQL actually stored. Twelve executed
public-API cases produced six failures and six controls: triggers rewrote a
successful auction bid/partial commodity fill or recreated deleted auction/
commodity listings. APIs still reported success or charged the buyer and could
publish a refund/return/delivery while the row retained a different outcome.
The corrected initial 12 cases pass. Faults are private fixture triggers, not
claims about a configured production database or retail behavior.

Auction and commodity row observers now share a listing write witness. It
captures complete immutable mapped scalars, tracked identity/state, original
values, modified-column mask and normalized database route. Before flush it
rejects altered or suppressed enrollment and checks the expected SQL preimage.
It forces AutoTransactionBehavior.Always, matching existing Item/mail owners,
so even one statement has a rollback boundary. Before commit it compares the
complete SQL postimage or required absence. Existing additional mail callbacks
share that context and rollback; no gameplay callbacks or ID allocation replay.

Repeated explicit flushes update tracking expectations; terminal settlement
removes the event observer. Book preimages advance only through the existing
known-commit observer. Savepoint rollback rejects the listing unit. An explicit
outer commit still validates SQL after the last flush. Raw ADO.NET transactions
and arbitrary SQL performed after the validation hook remain outside this EF
contract; this is not a complete CAS or competing-writer qualification.

Expanded tests cover changed/suppressed states, masks, originals, scalar values,
transaction suppression, repeated flush/commit/rollback, outer-commit SQL
tampering and insert postimages. A new regression was executed: the first
candidate reread SQL from SavedChanges after an implicit commit. A subsequent
independent SQL write then caused an already committed save to throw. That
12-case run had 11 passes and one failure. The observer now rereads only while
an explicit transaction remains; implicit saves retain their pre-commit proof.
The corrected case passes in the 141-case run and full suite. The failed
candidate/source/DLL archive remains retained separately.

Final review pin:
296c2d305f75e5819fb364198516644719edb0c8586cf3fe712574b1adee885e.
Both independent read-only axes found zero actionable issues and verified the
three raw snapshots/manifest. Five complete archives bind 6,111 negative and
6,112 later inputs, including eight owning/test-copy DLL pairs. Local proof:
artifacts/continuation-20261031/marketplace-postimages/verification.json.
The historical handoff verifier still rejects evolved Source against checkpoint
10277; its failure is retained. Final git diff --check is clean.

The larger explicit serializable marketplace executor drafted outside Source
was not implemented. Existing EF validation supports this smaller postimage
fix. Full captured listing/wallet/Item/mail outcomes, CAS, commit ambiguity,
provisional wallet compensation and post-commit direct-delivery recovery remain
required, alongside Contract and broader feature work. No configured-provider,
upgrade/socket/load/native/retail promotion follows. No client/Computer Use
launch, entitlement/default change, commit or push. Defaults remain 15:1/17:1;
44 and seven baseline character entitlements are not automatically granted.
The broader Goal remains active.

A 05:29 UTC Git-head refresh found all 58 tracked refs unchanged from 04:23,
with 609 the highest advertised PR head. This checks heads; open/closed metadata
is the separately timestamped inventory. Revised 604 and 608 stay reconciled;
609's closed-unmerged status and lack of a supplied reason remain separate.
