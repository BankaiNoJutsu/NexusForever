# Transport location save retry

6 November 2026. The unified Goal resumes in the fresh
`I:/GIT/NexusForever-unified-20261106` worktree at published parent `9a31e976`.
The old machine's ignored paused candidate/checkpoint was unavailable. This is
a narrow reconstruction against the integrated publication, not qualification
of those missing edits or their old-parent binaries.

`Player.Save(CharacterContext)` cleared Location on enrollment. A failed flush,
rollback, disposal, unflushed commit or savepoint rollback could therefore leave
the admitted destination absent on a fresh save even while taxi discovery and
fare intent remained recoverable. Six of eight initial regression cases fail
against the unchanged parent Game DLL, including a suppressed X write.

Location now remains dirty until terminal settlement confirms an actual SQL
flush. The witness includes Character, world, zone, position and rotation; an
explicit commit rechecks SQL after the final flush. Rollback, rejection and
unflushed completion retain the location. A movement version and map-admission
generation prevent an old save from acknowledging later movement, including
return to the same position. A busy admission lock conservatively retains intent
instead of blocking persistence behind a map callback. Other scalar save masks,
taxi pricing/admission policy and PvP declarations remain unchanged.

The real fare-settlement test admits one 12-credit fare and node, rolls back the
full Player Character save, then saves through a fresh context. SQL retains the
single debit (100 to 88), the destination and one node. This uses prepared map,
session and metadata inputs; it is not a socket or physical travel observation.

Verification on the reconstructed candidate:

- Owning Game build passes with zero warnings/errors. Normal Game.Tests project
  builds and copies the changed Game DLL to World and tests; all three hashes
  match. No full-solution build or full suite ran.
- Initial baseline: six failures/two passes. The first fixed revision passes 317
  affected transport, map-admission, normal-save and physical-loot controls.
- Reviewed revision: 319 affected passes and one new fixture reflection failure
  (read-only CurrencyManager property). The fixture now sets the existing backing
  field, without changing production or weakening assertions. All 39 taxi/location
  cases then pass; the final owning graph build and 90 transport-handler and
  taxi/location cases pass, including all 11 new location cases.
- The explicit actual-catalog runner verifies four table hashes against asset
  inventory `295132fd25cd57bd95bdbb6cc1a9364e0f061ab4f2e1e0ba083cf837e1492e4b`.
  All 714 parsed single-route quotes (238 routes, Exile/Dominion/unknown faction)
  pass acceptance/rejection, price, destination and no-premature-debit checks.
  These are flight-path quote cases, not the missing old runner's 1,338 cases.
- Primary-agent Spec review checks commit/flush ownership, later movement,
  savepoint rollback, fare consistency and map-lock ordering. Standards review
  checks the six-file source boundary, existing transaction callbacks, runtime
  table ownership and fixture provenance. No remaining actionable finding; no
  independent reviewer is claimed.

Portable bindings and commands are in
[transport-location-save-retry-20261106.json](coverage/transport-location-save-retry-20261106.json).
Raw TRX/logs and the reflection runner are ignored under
`artifacts/location-save-retry/`; Git carries the reusable catalog entrypoint.
That entrypoint accepts an asset root and a new output directory and loads tables
through the existing GameTable reader. Client assets never enter Git.

The upstream 20-PR updated-order refresh and direct #608/#609 metadata preserve
the previously reviewed heads/states: #608 open at `7db12d9a`, #609 closed/unmerged
at `f9b6f735`, #604 open at `36b59aaa`. No upstream mutation occurred.

This closes the reconstructed Source/SQLite location retry defect. Configured
provider, physical process loss, physical travel, hosted/socket/native and retail
qualification remain separate gates. Overall restoration stays active. Continue
with the character-list wire-row witness on a newly owned copied runtime;
Computer Use and native-client launch remain stopped.
