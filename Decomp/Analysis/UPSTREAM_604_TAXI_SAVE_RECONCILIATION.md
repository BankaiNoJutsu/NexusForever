# Revised upstream604: taxi discovery and save settlement

Date:1 November2026. Reconciled upstream head
36b59aaaaebbcd0ec3e23a94fd3cf9de8975923b against prior
19786895e3bb399a95576295916318b4dfc9bbc2 using an exact two-point Git diff.
The divergent GitHub merge-base comparison includes unrelated base history;
the actual delta has7 files. Exact objects/diff are retained locally without
changing HEAD, branch or remote refs. No push occurred.

The revised PR extracts TaxiManager and includes already-reconciled598 base
changes. The fork retains injected tables, Player.Taxi API, same-map/finite-range
checks,14-bit node bounds, strict unknown-faction rejection, existing flight
handler contracts and provider migrations. Copying the extraction would weaken
these guards and restore premature dirty clearing. Structure was not copied.

Shuttle45 (InvokeShuttlePrompt/0x2D) now preserves target/objective checks without
discovering a taxi node. Upstream explicit interaction enum and narrowed taxi
cases corroborate the fork's existing label. Flight interactions46–48 retain
discovery. One existing parameter-case oracle was corrected from expectedTrue
to expectedFalse; it is explicitly replaced, not counted as an unchanged case.

Taxi pending nodes used to clear at SavedChanges even within an unfinished
transaction. Executed baseline23 cases produced5 failures/18 passes: shuttle45,
flushed disposal/rollback, savepoint rollback followed by outer commit, and
repeated same-context enrollment. A real physically committed lost-ack control
passed and remains. Corrected prototype23 passes; expanded32 and final34 pass.

The per-player save unit retains original Character/context and staged key
identities. Repeated enrollment coalesces; overlapping contexts reject before
staging. SQL/tracker work stays outside the short metadata lock. Validation
rejects altered/suppressed rows and checks flushed SQL presence. Only terminal
commit acknowledges IDs proven by the last flush. Rollback, disposal, savepoint
rollback or failure retains intent; later discoveries/unflushed enrollment stay
pending. Fresh-context presence reconciliation is idempotent for these key-only
rows, including actual lost acknowledgement. Arbitrary gameplay is not replayed.

Additional tests cover suppressed/foreign keys, removed flushed rows, later
discovery, outer commit without flush, atomic-validator enrollment and real
Player.Save(CharacterContext) with TimePlayedTotal committing/rolling back
together and normal-save retry. The latter two address an initial review
advisory; the narrower atomic test was renamed accurately. Other normal-save
managers in this integration fixture have no dirty work and are proxies.

Full solution and subsequent test-copy rebuilds pass with0 warnings/errors.
All10836 full tests pass with0 failures/skips:16 net-new cases plus the one
corrected old oracle; all other10819 prior cases remain. All34 focused identities
occur in the rebuilt full run. Four archives bind6109 negative/6110 later inputs
and eight owning/test-copy DLLs. Final two-axis review has0 findings at pin
8f148eca834170a929bcfb7c559ab0480df1400a83254899693eaf7adcc59721.
Local bindings:artifacts/continuation-20261031/upstream604-refresh/verification.json.
The historical handoff checker rejects changed Source against checkpoint10277;
its failure is retained, not promoted to a current pass. git diff --check is clean.

The04:23UTC inventory remains55 open,608 newest open and already reconciled;
609 closed unmerged with unchanged head.604 was the sole changed head among58
refs and is now source-reconciled. No closure reason for609 is inferred.
A separate05:06UTC Git refresh found all58 tracked heads unchanged, with609
the highest advertised PR head. This checks heads, not fresh open/closed metadata.

This is source/private SQLite evidence, not configured-provider, physical travel,
native/UI/socket/load or retail qualification. Marketplace postimages/CAS and
complete listing/wallet/Item/mail outcomes, Contract and broader feature work
remain. Goal stays active. Client/Computer Use remain stopped. Defaults15:1/17:1
and no automatic44/seven baseline grants are preserved. No commit or push.
