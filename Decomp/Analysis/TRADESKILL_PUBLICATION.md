# Committed tradeskill publication

Continuation after `da362da3`, 3 November 2026. Real Player profession,
schematic and successful talent-reset notices now follow their original
confirmed skill/recipe graph and publish from the owning world update.

## Connected behavior

Learning, skill archiving/reactivation, XP/talent changes, new schematic notices
and successful reset cooldowns retain immutable accepted payloads and order.
Ordinary and frozen saves capture notice versions alongside their complete graph;
only flushed versions confirmed by that save are acknowledged. Later mutations
remain pending. Repeated already-active learning can acknowledge its notices
through an unchanged confirmed graph. Existing quest/reward callbacks and refusal
responses remain intact; no crafting/discovery or wire semantics are invented.

Initial profession lists use published confirmed skill/recipe snapshots, including
discovery coordinates and bit ordering. Quiet existing-recipe discovery keeps its
legacy false return and absence of a new-learn notice while committed initial
state advances. Initial sync serializes with world publication. A failed packet
retains its cursor and rebuilds its payload without repeating preceding packets.
Identity, registry, original session and disconnect checks reject stale owners.
Standalone interface-only sessions retain compatibility; once a world owner is
bound it cannot fall back to immediate delivery. Session scheduling occurs outside
the persistence metadata gate and retries when the queue becomes available.

## Evidence and qualification

Evidence root: `artifacts/continuation-20261103/tradeskill-publication/`.
`before.trx` reproduces three early-publication failures against unchanged
production source. `baseline/binding.json` retains matching DLLs, test source and
TRX. That scoped build regenerated informational-version metadata after the
Archive commit, so its DLLs differ from the earlier Archive qualification.

The first implemented candidate passes 25 scoped cases. The expanded candidate
passes 35 and exposes two introduced gaps: an unchanged frozen save does not
retry after event-queue unavailability, and payload retention reads a cleared
live identity. `recovery-compiled.trx` preserves both failures. The corrected
candidate passes **238 scoped cases**, including all **15 new regressions**.
The earlier namespace/import compile errors remain retained and excluded from
behavioral negatives. The pre-existing savepoint retry is a passing control;
the separate failed savepoint hypothesis does not justify a new recovery fix.

Both review axes are clear across seven Source paths; manifest SHA256
`757c2bfd0cb8ef744711cdb1e47166898ca3fd99faa072b7bffecd019162ed7d`.
The owning test-project build has no warnings/errors. All **15 original compiled
case bodies** pass on configured MySQL 8.4.13, with controlled tables, sessions and
owned schemas. Provider execution takes 122.2s; final affected
test execution takes about 77s. The recorded provider runner/helper and owned
MySQL server/listener are absent after graceful shutdown.

`verification.json` binds 6,271 Source/Tools inputs under
`85cc5c60914174d37d5e485c1d81706a0086781539a94d8824da73ebb7e10e81`,
all 11 owner/test/provider DLL triples and test assembly
`7acae857766f70b3fa72fae0c595e145a1bbe8fa805a7beb9bc184df13256167`.
Database source/schema, packages and entitlement defaults are unchanged. Earlier
upgrade proofs retain their own version boundaries. The historical continuation
verifier's expected Source divergence remains separately recorded.

Per the user's 3 November instruction, validation is scoped only: **no full suite
was run for this milestone**. The earlier 12,007-test Archive full gate remains
historical evidence for its own candidate. These results do not qualify actual
retail table delivery, crafting gameplay, deployment, live brokers, restart/outbox
durability, physical gameplay or native client timing. Defaults 15:1/17:1 remain;
no automatic 44 or seven baseline character grants. Client/CUA stay stopped.

## Remaining roadmap

The wider Goal remains active. Continue the economic/progression/gameplay queue
without repeating completed companion publication. Contract public claims still
need unclaimed/excess/reset evidence and complete real-metadata delivery; track35,
unsupported Challenge producers, wider AMP/protocol semantics and the documented
external/native/retail gates remain separate. Revisit blocked evidence only when
its inputs change, and continue independently actionable work.
