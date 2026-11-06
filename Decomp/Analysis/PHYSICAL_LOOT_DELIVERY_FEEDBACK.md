# Physical loot delivery and retained feedback — Source/SQLite verified

6 November 2026. This completes the bounded feedback/partial-delivery slice from
[confirmed BOP pickup](LOOT_BOUND_PICKUP.md). The user requested publication and a
handoff to the machine holding runtime assets. Broader retail restoration remains
unfinished; use the [resume prompt](RETAIL_RESTORATION_RESUME_PROMPT.md).

## Acceptance boundary

Confirmed physical BOP loot using the real Inventory must apply its whole award
before invoking inventory/gameplay feedback. A rejected inventory or grant notice
must not repeat any quantity on public GiveLoot retry. The same loot entry must
reject reentrant delivery, including a callback replacing the Player's Inventory.
Preparation failure before acceptance must restore the original inventory.

Retain refused feedback with a cursor for the original Player, Inventory,
session, event queue, account epoch, realm and map admission. Rebuild each retried
packet; do not reuse a packet mutated by a rejecting session callback. A later
item removal must not produce a ghost Add. Normal Player save/reload must persist
the accepted whole award and collect-objective transition independently of
unavailable packet feedback. Accepted global collect progress survives a map
transfer; old map/unit notices do not cross admission generations.

Preserve existing confirmation/binding/capacity rules, ordinary inventory,
other loot types and custom-inventory compatibility. Keep physical process/
loot-receipt restart, provider, socket/native and retail qualification separate.
This is not a new durable loot receipt or a new binding/confirmation policy.

## Delivered behavior

The real Inventory buffers physical creation feedback and uses its existing
operation snapshot to restore preparation failure before acceptance. It marks
the loot delivered after the complete physical mutation, then completes collect
gameplay before invoking any fallible UI channel. Quest progress, flags, script
callbacks and Achieved state are independent of refused ItemAdd/objective notices.

Separate gameplay and notice cursors retain refused feedback for normal Inventory
updates without replaying successfully published steps. Retried item notices read
current owned state and skip removed items. Quest/grant packets are newly built
from captured values rather than rejected mutable DTOs. The original logical
mutation lease excludes concurrent save during preparation; normal save is
available while a refused notice waits for a later update.

A per-loot guard rejects nested claims even if a callback replaces Inventory.
Map admission is acquired before Inventory; a nested caller already holding the
inner gate refuses rather than waiting on another thread's admission gate.
Changed actor/session/account/quest ownership abandons obsolete work. A map
transfer or unit GUID change abandons old UI while preserving already accepted
global collect progress, including same-map-object leave/reentry.

## Reproductions and verification

Three public GiveLoot negatives fail against published parent
`06df7b67edacfd8e40523c6f0d69860f59176e38`: rejecting the first/second ItemAdd in
a 15-item/two-stack award produces 25/30 items on retry; replacing Inventory
inside ItemAdd permits a nested claim. An independent frozen-parent replay
reproduces all three. Review controls additionally reproduced stale map/GUID
publication, interrupted collect transition, admission-gate inversion and lost
collect progress after initial notice refusal followed by transfer.

The final owning Game build passes with zero warnings/errors. The owning test
build passes with zero errors and two existing pinned-package audit warnings
(NU1902 MySqlConnector 2.6.0; NU1903 SQLitePCLRaw.lib.e_sqlite3 2.1.11).
All **19 focused cases** and **239 related cases** pass, with zero failures/skips.
The related filter covers Loot, soulbound creation, crafting loot IDs, collect
objectives, reward capture, Quest persistence and public-event Quest behavior.
Full real Player SQLite save/reload retains accepted bound items and collect
state while feedback is refused, including after a map transfer. The two-thread
control verifies refusal without waiting on the outer gate and successful retry.

Eight selected source files match the final DLL/PDB compiler checksums. Both
final review axes report zero findings. The immutable Source comparison is
parent `06df7b67…` to tree `0f6020d4d2a92b3be6ce68c91995899d7369c295`.
The [portable result](coverage/physical-loot-delivery-feedback-20261106.json)
records exact sources, binaries, checks, retained failures and limitations.

## Qualification limits

Builds use a separately owned export of Game/Game.Tests from the published
parent, plus the eight selected C# files and 138 verified frozen dependency DLLs.
Isolated project files replace project references with frozen assembly references
and explicitly retain the shared framework; production project files are
unchanged. Game uses 120 of those dependency references; Tests uses the fresh
owning Game. This excludes unrelated Source and shared build outputs, and is not
a normal combined-root project-graph build. A mixed/stale shared-scratch run was
discarded; it is retained as contaminated evidence rather than qualification.

Raw logs, TRX, frozen binaries and prior candidate snapshots remain ignored under
`artifacts/continuation-20261106/loot-feedback-delivery/`; Git carries the portable
summary. No schema, entitlement, packet layout or binding policy changed. No
hosted services or native client were launched for this slice. Full durable loot
receipt/crash recovery, arbitrary script-exception recovery, configured provider,
socket acknowledgement, native and retail behavior remain open. Computer Use and
native-client launch stay stopped. Hosted World/Auth still needs verified runtime
assets on the destination machine.
