# Wallet load database provenance

1 November 2026. The actual `CharacterDatabase.GetCharacters` query now retains
the configured database route and Character/account/currency row identity in
weak object metadata. Currency saves, frozen snapshots, bank reservations and
Contract credit admission reject a conflicting retained route, including empty
managers and newly added wallets. The shared normalizer preserves Item's
password-removal/key-order behavior. Metadata retains scalars, not model graphs;
it adds no EF properties, serialization or schema.

The executed baseline had eleven wrong-database failures and twenty passing
controls. Equal rows in another SQLite database previously accepted ten foreign
writes; a configuration switch during the original query exposed the eleventh.
All 64 final regressions pass: five save paths, existing/new/empty wallets,
normalized routes, root/row tampering, actor identity changes, real Contract
quote admission, unqualified clones/legacy models and capture without consulting
the changed factory. A configured-MySQL key control opens no connection.

All 11,019 full solution tests pass, zero failures/skips, retaining every prior
10,955 test identity, including 24 conditional currency, fourteen frozen
currency, 264 marketplace and 297 reviewed mail cases. The solution and test-copy
builds have zero warnings/errors. Full build took 63.5 seconds, test-copy refresh
17.1 seconds and full tests 15m26s; focused iterations preceded that gate.
Both final review axes have zero findings at Source/Tools pin
`eb03a67a8f7b699499cadd84c02c4361737edc34d6c72b4b42a344925c5dc1b4`.
Both provider model differentials show zero migration operations/connections.
Final local proof is `artifacts/continuation-20261031/wallet-load-provenance/verification.json`;
four existing phase archives and nine owning/test-copy DLL pairs verify.
The historical handoff checker rejects evolved Source against checkpoint10277;
the previous identical checkpoint-divergence result is retained, not rerun.

Resumption upstream refresh around09:47UTC:55 open,608 newest open, created
since29October609/608, all58 tracked heads unchanged, highest advertised609.
Previously reviewed608 and revised604 heads are reused;609 remains recorded as
closed unmerged. No closure reason is inferred.

This is source/private SQLite qualification, not physical endpoint identity,
live MySQL, native/gameplay or retail completeness. Synthetic/cloned models
remain compatible and explicitly unqualified. Character creation currently
adds its saved model directly to the roster, bypassing GetCharacters; creation
provenance is the next required integration, tied to its actual confirmed create
context. Do not infer authority from a later equal row or mutable factory.

Continue directly into the complete normal-bid public transaction milestone:
ordinary full-save preflight, original actors, listing/wallet/refund/mail receipt,
commit uncertainty, retirement/replacement and later currency intent. Acceptance
boundary: `artifacts/continuation-20261031/normal-auction-bid-acceptance.md`.
These APIs alone do not complete that behavior. Prior sharing/delay cause remains
unproven. Contract reset/unclaimed/excess and full reward delivery remain open.

Defaults15:1/17:1; no automatic44 or seven baseline grants. Computer Use/native
client launch remain stopped. No commit or push. The broad Goal remains active.
