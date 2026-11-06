# Public guild bank rank settings

The later [actual formula/perk qualification](GUILD_BANK_NATIVE_CONTRACTS_2026_11_05.md#actual-formula-and-perk-inputs-qualified)
resolves catalog values and dependency joins. Financial storage/settlement,
authenticator/banker/reset rules and provider/native/retail remain open; this
does not change the completed rank-settings implementation below.

5 November 2026. Public Guild operations23/24/30 now update bank permissions,
money withdrawal limits and repair limits through the original recoverable
guild graph, with confirmed rank readback on the original World recipients.
The [portable record](coverage/guild-bank-rank-settings-20261105.json) binds the
seven Source files, retained negatives, final assemblies and scoped results.
This implements the settings prerequisite from the
[native contracts](GUILD_BANK_NATIVE_CONTRACTS_2026_11_05.md).
Money/item/tab-open handlers remain diagnostic; banking is unfinished.

## Accepted behavior and ownership

The public decoder/World handler reaches the real global manager and loaded
guild/member/rank graph. Settings require current membership, the existing
lower-rank edit permission/hierarchy, a full-width valid rank index, and the
original player/account/session/epoch. The base handler permits Guild/WarParty
bank types. Positive public fixtures use an ordinary Guild; they do not qualify
a separate WarParty bank runtime or configured provider.

Preserve packed bank permissions as UInt64, including unknown bits. A selector
is not a guessed item count. Money/repair limits accept the nonnegative signed64
domain, including native `7FFFFFFFFFFFFFFF` for “all”; unsigned encodings of
negative limits are rejected. No financial, perk, reset or entitlement rule is
invented, and no schema migration is needed.

An in-memory sequence travels with the existing immutable ordinary guild
snapshot, including accepted no-ops. Settings covered by one save use its final
confirmed graph; edits after staging retain their later sequence. Failed saves
retain intent without success. Lost acknowledgement uses the existing stored
outcome observation before publication becomes eligible.

Capture the requester and online recipients with their exact member, player,
account, epoch, session and EventQueue. SQL confirmation queues immutable rank
descriptions. Fresh ten-slot rank packets and results are built/enqueued on each
original World queue after the save unit releases. Every packet checks those
owners and original global guild/player manager instances. A replacement stops
that recipient's remaining packets while other original recipients can finish.
Rejected mutable packets retry at their cursor without replaying accepted
earlier output, game mutations or SQL. A later login does not inherit that cursor.

Initial rank views during the pending window use the confirmed graph. Ordinary
rank snapshots/results suppressed by the window retain their own sequences and
publish behind older committed bank snapshots. Ordinary rank publication outside
that window keeps its existing behavior; this result does not close that broader
committed-publication boundary.

## Failures, verification and review

Four baseline public cases failed: none of the three settings reached SQL, and
ordinary rank readback emitted while a bank request was pending. The first
candidate passed them. Both review axes found one preservation defect: a later
ordinary edit could lose its readback behind an older queued bank packet. Its
regression produced one snapshot instead of two. Retained sequence-bound
snapshots/results fix that loss.

A separate public probe reproduced `UInt32 rank256 -> byte rank0` in the existing
permission editor: a member allowed to edit lower ranks could change the leader's
permissions. All public rank-indexed operations now reject indices>=10 before
delegate narrowing. The decoder still preserves UInt32, and direct/admin storage
is unchanged. The corrected case retains leader permissions. Intermediate test
fixture compile corrections are retained separately from production negatives.

All22 new cases pass within346 affected cases. They cover the three operations,
sentinel/raw64 values, no-op/coalesced/frozen saves, graph rollback/retry, lost
acknowledgement, initial views, later ordinary notices, rank authority, original
queues, stale ownership, fresh packet retry and reentrant recipient replacement.
Affected controls include guild creation/mutation/affiliation/rename/disband/
achievement/Community, packet shapes, Community housing views/handlers and atomic
Character saves, including the unchanged bank diagnostic boundary.

The fixtures use the real public handler/global manager/Guild and disposable
Character SQLite, with recorded player/account/session actors and driven World
queues. Enqueue acceptance is the measured reply boundary. No socket, physical
restart or durable cross-process packet replay is established. Both final Source
reviews have zero remaining findings, with no later bank Source edit.

Final owning WorldServer/test-only builds passed with zero warnings/errors in
41.18s/18.11s; the final affected body took about68s. Game.Abstract, Game and
WorldServer test DLL copies match their owning outputs.

The checkout acquired separate upstream integration merge`8749af17` during
evidence publication. Bank files were preserved. Final owning/test outputs and
affected validation were refreshed against that parent; the earlier pre-merge
346-case result remains separate. The merge's broader qualification has
[its own report](UPSTREAM_MERGE_2026_11_05.md). Latest20 PR heads/bases/states/
merge states remain unchanged. No full solution or full suite ran for this slice.

## Remaining Goal scope

Bank balances/tabs/custody and Character/guild financial settlement still need
a coherent service. Actual formula/perk rows, authenticator authority, daily
counter/reset ownership, banker interaction, overflow/replay/concurrency and
committed financial replies remain open. Preserve diagnostic money/item/tab-open
handling until that service is complete. Fresh provider/copied-upgrade, physical
failure/restart, broker/socket/deployment/native/retail and broader roadmap gates
remain required.

Continue independent progression/gameplay work while bank inputs are qualified.
Zone35 still needs its named map/credit/cycle evidence; generic Challenge formulas
do not enable it. Defaults15:1/17:1, no automatic44/seven grants and the native
client/CUA stop remain unchanged.
