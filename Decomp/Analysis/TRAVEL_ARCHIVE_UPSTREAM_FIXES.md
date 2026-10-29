# Travel and archive upstream fixes

Implemented on 2026-10-29 in `I:\GIT\NexusForever-assessment`, after loot/group
commit `5b94d21dc9021978c59198a6f5040e2a7b7e4ef9`. This follows the assessment's
next integration priority: missing bind-point/travel behavior and immediate
archive completion hooks. Upstream `game_rework` was refreshed and remains
`1f643f6374ec017ae46ac3bd72ad99d42c902d46`.

Reviewed open upstream heads:

| PR | Head | Reconciled behavior |
| --- | --- | --- |
| [#601](https://github.com/NexusForever/NexusForever/pull/601) | `79a36684101423cfd199047b466a2c2c839e9c2e` | Persist transmat binds, notify the client, restore the bind at login. |
| [#602](https://github.com/NexusForever/NexusForever/pull/602) | `ffe5dc0512452fe3e383b8a8b454017c04120df4` | Zero-location Teleport recalls to the saved bind; teach the recall spell from GameFormula 777. |
| [#606](https://github.com/NexusForever/NexusForever/pull/606) | `4ffa050ba994a82e0fb52f0b5b733c94b139367e` | Last-open-world/bind fallback, queued pending-removal Leave action, safe return handling and group-instance removal. |
| [#588](https://github.com/NexusForever/NexusForever/pull/588) | `e41fb57514cc202425430684e11e5fdd8f1b71f0` | Refresh archive unlocks immediately after achievement completion/quest hand-in; article titles require all entries. |

## Bind persistence and recall

`CharacterBindModel` is a one-to-one, cascading character-owned record containing
bind id, world id and position. Both MySQL and SQLite have additive CharacterBind
migrations, designers and updated snapshots, generated against the fork's
current models. Character loading includes the optional bind record. No existing
character or entitlement data is rewritten or automatically bound.

Player bind state is loaded at initialization and staged during the existing
character save. A successful EF save acknowledges the bind revision; a failed
INSERT remains retryable, and rebinding during a save leaves the newer revision
dirty. Bind state reads, staging, changes and save acknowledgments share a lock.

The fork already owns the relevant packet registrations:

- 0x0098 is `ClientActivateUnitCastTarget`, a context-token/target pair. Bind
  handling extends that activation path after its existing hostile, busy and
  range guards. Other activation variants and tutorial/pet fallbacks remain in
  place. Zero and oversized ids are rejected instead of silently truncating.
- 0x016D already has the required 16-bit `ServerActionSetUInt16` shape. Use that
  producer rather than adding a duplicate registration. `ServerPlayerCreate`
  gains a `BindPointId` alias over the existing `UnknownBC` field, preserving
  source compatibility and serialization order.

A zero-location Teleport uses the saved bind when teleporting is available.
Explicit WorldLocation2 teleports keep their existing path. Recall teaching is
table-driven and idempotent and does not occupy a LAS shortcut.

The existing stuck handler now prefers the bind but retains its zone-exit
fallback for characters that have never bound, cooldown tracking, context-token
error packets, housing recovery and suicide behavior.

The prerequisite changes carried by #602 are not replayed: in this fork type
269 is the client-evidence-backed RapidTransport check, documented in
`PrerequisiteCheckRapidTransport.cs`. Replacing it with an always-healthy check
would discard existing cast-context validation. Existing duel and prerequisite
implementations remain intact.

## Instance return and removal

The player remembers the last open-world position when requesting a map
teleport. Transfers from another instance do not overwrite it. Content returns
keep the existing priority of match-owned return positions and cached portal
returns, then use the player's open-world/bind fallback. An unknown bind world
does not produce a destination.

Portal return lookup no longer consumes its cached value while merely scheduling
a pending removal: cancellation and rescheduling can still return to the same
position. The cache is cleared when the player actually leaves.

Leave and externally requested removals are queued and drained in the instance
update. They verify the player object still belongs to that map, avoiding stale
requests affecting a replacement with the same GUID. Null return destinations
are logged and skipped instead of passed to TeleportTo.

Group removal requests are limited to the removed group's own Group/Raid map
lock. Leaving a secondary group cannot eject a character from another group's
instance. The existing warning timer and cancellation API remain available.

## Archive behavior

The existing archive manager, persistence table, linked-article validation,
entry rewards, path-rule evaluation, login backfill and world-entry packets are
retained. No replacement archive schema or manager is introduced.

Successful achievement completion refreshes the achievement owner's archive,
including direct grants and events with a different target player. Successful
quest hand-in refreshes after the quest has entered the completed collection;
an achieved but unsubmitted quest is insufficient. The new refresh option sends
updates only for changed articles. Initial login refresh retains its existing
batching behavior.

An article's title now requires all its populated entry slots to be unlocked.
Partial progress still grants each entry's own reward once. Repeated completion
refreshes do not re-send unchanged articles or re-grant the article title.

## Verification

The first focused runs exposed fixture problems with default value-type `out`
parameters, a missing group leader in a message fixture and reflection against
an inherited map setter. Those fixtures were corrected; no production guards
were removed to satisfy tests. The shared recording proxy now initializes
unconfigured value-type `out` arguments before returning, matching its existing
default-return behavior.

There are 28 additional regression cases relative to the 5,857-test loot/group
baseline, covering bind insert/load/update, failed-save retry, in-flight rebind,
invalid binds, nested-instance returns, packet aliasing, guarded activation,
saved-bind stuck recall/cooldown, formula-based recall teaching, zero-location
teleports, queued/stale/null-destination removals, portal-return retention,
group-instance ownership, SQLite upgrade/rollback, archive progress/title rules
and archive owner selection. An existing achievement test additionally verifies
the immediate hook.

```powershell
dotnet build Source\NexusForever.slnx -v minimal --nologo
dotnet test Source\NexusForever.slnx --no-build --no-restore -v minimal --nologo
git diff --check
```

The focused travel/archive/migration slice passed 60 tests; the final owner-hook
slice passed five tests. The final solution build passed with zero errors and
114 existing warnings; all **5,885 tests passed**, with zero failures and zero
skips. Full solution results are recorded in
`I:\GIT\NexusForever-assessment-report\travel-archive\full-build.log` and
`full-suite.trx`.

SQLite regression tests apply all runtime migrations, and explicitly upgrade a
populated character database to CharacterBind and roll it back while preserving
character and entitlement rows. The generated MySQL delta script contains only
the bind table/FK and migration-history insert; it is saved as
`mysql-bind-migration.sql` beside the logs. Migration generation used a scratch
design-time factory outside the checkout with a fixed server version and no
database connection.

Live MySQL application, broker traffic and client gameplay were not exercised.
Deployments need the CharacterBind migration for their selected database
provider before running this code. Entitlement defaults and grant policy are
unchanged.
