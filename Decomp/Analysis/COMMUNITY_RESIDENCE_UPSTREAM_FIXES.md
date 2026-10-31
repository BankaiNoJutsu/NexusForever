# Community residence lifecycle reconciliation

This slice reconciles the community housing behavior in upstream PR #391
(`upstream-pr/391`, `684b81a2d8a965e762354c85d17e63e0c78a925b`) with the fork's
dependency-injected guild, residence, map-lock and persistence paths. The full
merge-base diff is preserved in the assessment report at
`remaining-goal/feature-pr-391.diff`.

## Resulting behavior

Leaving a community detaches that member's residence before membership removal.
The same hook runs for offline members and the online player's existing local
guild manager. Disband also detaches orphan child links, unregisters community
search/visiting caches and unloads the community map through its existing return
location flow. An already pending unload is preserved.

The owner, personal residence object, decor, neighbors, privacy and other housing
state remain intact. Detached homes return to standalone property type 35. The
community root's GuildOwnerId becomes null; its database row and decor remain as
archived data. The guild still uses its existing soft-delete behavior, including
archived membership rows on disband. Ordinary leave retains the existing physical
membership-row removal semantics.

Loaded child removal uses the real community map's existing placement-removal
path. A child still pointing at its old solo map during a pending transfer is
detached logically without mutating that unrelated map. Owners are teleported
only when they are currently inside the community map and CanTeleport permits
it. Required standalone entrance data is resolved before mutation.

Housing map entry now emits one random-community list to the arriving player.
The explicit refresh handler uses the same packet builder. Both use the existing
registered visitable-community list and realm identity; this does not expand
privacy/registration policy or change entitlement defaults. The fork already
sets a newly created personal residence through ResidenceManager.GetOrCreateResidence.

## Persistence ownership

GuildBase dirty-state visibility now includes dirty members and ranks, with
default interface implementations preserving compatibility. Previously an offline
member-only deletion could be skipped by the global guild save timer.

Community queues explicit residence association, member removal and soft-delete
intent. These are staged together in the existing character context. Save failure
retains that intent even after the older save masks have been cleared. Normal
residence timer/direct saves defer affected homes to the pending guild unit.
GlobalGuildManager permits one in-flight save per guild.

CharacterContext completion callbacks observe actual EF transaction commit and
rollback, nontransactional SaveChanges, transaction disposal and context disposal.
EF exposes transaction disposal through DiagnosticSource rather than its transaction
interceptor, so a context kept alive after disposing its transaction still reports
the abandoned unit as unsuccessful. Residence records both ordinary and community
staged contexts. The community save waits outside its lifecycle lock for those
earlier units to finish, then checks again before staging; an older context cannot
commit stale parent/plot association after the detach unit. Savepoints do not
count as transaction completion. Rollback to any savepoint conservatively marks
the staged intent uncertain, while retaining ordering until the outer transaction
ends; even a later outer commit leaves idempotent intent available for retry.
A caller-owned transaction retains pending
intent through SaveChanges and releases it only on commit; rollback preserves it
for the normal guild timer retry.

Rejoining before the timer cancels a queued membership deletion through the
existing AddMember reuse path. A never-persisted community disband explicitly
releases its home save leases when the global manager discards the new guild;
personal homes remain writable by their residence timer. Community initialization
assigns the guild identity/name before creating its root residence. Reparenting
between communities preserves ordering for already staged community contexts;
a superseded owner token cannot stage a new stale residence update.

If an earlier delete already removed the in-memory member before its transaction
failed, rejoin creates a new member while the database still contains the old row.
The retry reconciles a live member with current database existence: update the
retained row or insert after an earlier committed delete. Current rank, note and
reservation fields remain authoritative across another failed save. Rejoin also
refreshes the pending operation token, so an older successful delete callback
cannot acknowledge the later join.

## Verification

Dedicated tests cover offline/online leave and disband, loaded/unloaded placement,
orphan children, pending solo transfers, pending map unload, missing travel data,
cache cleanup, real GlobalGuildManager relational flush/retry, transaction
ownership, startup reload, preserved decor/neighbors and single packet emission.

The final community/transaction/bootstrap gate passes **79/79**, with zero
warnings/errors: 41 community lifecycle cases, four community-list producer cases,
ten save-completion cases and 24 existing bootstrap race cases. The overlapping
contexts cover both leave/disband and earlier commit/rollback, context disposal,
and caller-owned commit. The actual timer skips in-flight saves before reading
guild state that acquires the lifecycle lock. Evidence is in the assessment
report's `remaining-goal/community-rejoin-retry-focused.log`.

The broader housing/guild gate also passed **97/97** before the transaction
completion follow-up (`community-housing-guild-focused.log`). The final parent
checkpoint covers the integrated solution after all workers stabilize. These
tests establish the stated source/persistence behavior, not graphical-client or
retail completeness.

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo --filter 'FullyQualifiedName~CommunityResidenceLifecycleTests|FullyQualifiedName~CommunityListProducerTests'
```

The parent goal owns the final full build/test checkpoint and runtime restaging.
