# Confirmed public LAS reply publication

5 November 2026. The real public `ClientRequestActionSetChanges` route now
publishes its supported reply batch from the confirmed original Character
spellbook snapshot. The [portable record](coverage/action-set-publication-20261105.json)
binds the seven Source files, owning/test assemblies, retained failures, scoped
results and two Source review axes. This completes the audit's public save/
actor-replacement probe and its reproduced reply defects, within Source/SQLite.
The broader LAS/AMP feature remains partial.

## Integrated behavior

Validation and accepted game mutation retain the existing 12-slot, active-spec,
known-spell, tier/budget, AMP and PvP/combat/death rules. For the real Player and
SpellManager, collect the spell-update and selected virtual-item descriptions
while applying the accepted request once on its original World actor. The
manager requests the existing Character save. Legacy interface adapters retain
their existing handler path.

The existing Experience save captures an in-memory request sequence alongside
its immutable spellbook. Confirmed acknowledgement uses the actual flushed SQL
book and its frozen sequence. Requests covered by one save coalesce to that
confirmed book; a later edit cannot enter or be consumed by an older frozen
save. A valid unchanged request still receives its own acknowledgement boundary.
No schema migration or synthetic native update-in-progress flag was introduced.

Only confirmed acknowledgement queues the supported replies: spell updates,
ability book, cache clear, selected virtual ability items, complete action set,
changed points and requested AMP list. Initial views requested while a public
edit is pending use the last confirmed book. Failed or uncertain saves retain
their accepted intent without publishing success; a stored observation or
successful retry makes the original batch eligible.

Publication runs on the original World EventQueue after the normal save releases
ownership. Every enqueue rechecks the original Player, manager, session, queue,
account and admission epoch. Reentrant replacement stops the remaining packets.
Each batch retains its enqueue cursor and constructs fresh packet objects,
including nested item data. A rejected mutable packet retries at that cursor
without repeating accepted earlier output, applying the request or saving SQL
again. Enqueue acceptance is the measured boundary; socket delivery is separate.

## Executed evidence and review

Three baseline public regressions failed against the unchanged parent owning
outputs: premature LAS replies, success despite rejected shortcut deletion, and
remaining packets delivered after reentrant Player replacement. The initial
candidate passed those three cases. The first implementation build's extracted
static method error was corrected before candidate execution.

Spec review found a fourth defect in the candidate: the Experience no-op early
return ignored the new request sequence. Its public regression failed with one
batch instead of two. The corrected guard includes that sequence. The same
intermediate expanded run also had five table-fixture setup failures; those are
retained separately from the reproduced production defects. The corrected
twelve-case expansion passed.

All 16 final focused cases pass. They exercise real learned tier/AMP/virtual
item publication, shortcut and complete tier/AMP rollback/retry, lost commit
acknowledgement followed by stored observation, coalesced requests, normal-save
capture before Auth with a later request, confirmed initial views, mutable
book/item/set retries, and reentrant Player/manager/account-session/epoch changes.
All252 affected cases pass, including those16 focused cases. The controls cover
the shared Experience/normal save, existing LAS request/
packet/AMP/grant paths, atomic Character saves and reputation publication. Exact
counts and group membership are in the portable record; no full suite ran.

The final owning WorldServer build passed with zero warnings/errors in 14.11s;
the final test-only build passed in 16.53s. The final focused body took about
26s; the affected body took about100s. Test Game and WorldServer DLL copies
match their owning outputs. Standards
and Spec final material reviews each report zero remaining findings; no later
production Source edit follows those reviews. Latest 20 upstream all-state PRs
retain their reviewed heads/bases/states/merge states, including #608 open and
#609 closed/unmerged. No upstream repository mutation occurred.

## Remaining boundaries and next work

This changes the supported public reply boundary. Existing AMP spell grants,
casting, derived properties and broader combat mutation timing are not qualified
as an atomic gameplay transaction by these tests. In particular, the focused
AMP fixture exercises selection, rows and replies without claiming initialized
combat-effect behavior. The existing normal-save uncertainty protocol is reused;
these tests do not establish physical process restart or durable packet replay.

Native `UpdateSpellInProgress` set/clear, failure writers, exact retail async
ordering, authoritative attributes and action-bar lock behavior remain open.
Fresh configured-provider/copied-upgrade, actual-table, socket/deployment/native/
retail qualification remain separate. The client and Computer Use stay stopped;
entitlement defaults15:1/17:1 and no automatic44/seven grants are preserved.

Continue the broader roadmap: examine the remaining public guild-bank financial
owner/recovery probe or the independent progression/gameplay queue before calling
another defect. Contract policy/canonical delivery, Primal allocation, Zone and
unsupported Challenge earning, cross-owner achievements and the five producer
gates remain open. Do not repeat this completed public LAS reply slice.
