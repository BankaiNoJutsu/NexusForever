# Committed community placement and removal

Continuation after `531c7436ad9c8270a2e2c60f8109d23e656b9b13`, 3 November 2026.
The broader assessment Goal remains active.

## Observable behavior

The public community placement/removal handlers now retain registered residence
requests before changing the live parent, property geometry, member reservation,
map membership, travel or placement reply. A command captures the original
community and both complete residence graphs, then saves the complete unit in a
single Serializable Character transaction. Failed writes roll back together;
retry accepts only the original complete before/after outcome on the original
database route. Earlier saves settle first; one-sided saves cannot consume a
prepared association. Original interface/unregistered handler compatibility is
preserved.

SQL completion only makes a command ready. The world update resumes ordered map
detach, domain apply, destination attach, travel and publication stages. Plug
work retains its cursor through failures. Publication creates fresh packets per
recipient and retry; acknowledged stages do not repeat. Accepted placements keep
FIFO slot/reply order and reject occupied reservations. Later unrelated flags
and member notes stay dirty. Untouched unresolved plot rows keep their stored
identity through both persistence and loaded-map packet publication.

Original root/session/map guards suppress stale effects. A failed travel
submission retries only its exact retained pending teleport; a later independent
map move cannot be overridden. Membership departure/disband can retain later
cleanup without stale placement publication. Authorised administrator removal
still publishes deletion when the residence owner has no member row, without
travelling the administrator.

## Executed defects and controls

Local evidence: `artifacts/continuation-20261103/housing-association-publication/`.

- `before-corrected.trx`: six actual public-handler failures and one realm
  control against the prior production candidate. The compiled baseline and
  source/DLL identities are retained in `baseline/` and `baseline-binding.json`.
- `departure-before-corrected.trx`: one membership-ordering failure and one
  disband control. The first attempt used an unavailable public test method;
  the corrected fixture invokes the protected membership lifecycle, not public
  leader-leave permission.
- `travel-before.trx`: failed AddToMap submission stranded the actual Player's
  pending teleport instead of retrying its original request.
- `route-before.trx`: a rejected foreign database attempt poisoned the retained
  SQL witness, preventing recovery on the correct route.
- `review-map-before.trx`: four failures cover unresolved retained geometry,
  stale map travel and partial plug attachment/removal.
- `reentrant-departure-before.trx`: departure during travel still received
  placement success. `review-fixed.trx` retains the intermediate 28/29 result:
  reply suppression worked, but the fixture had not advanced the next ordinary
  guild save interval after the reentrant departure. The corrected test retains
  both assertions and explicitly advances that interval.
- `raw-publication-before.trx`: an unresolved raw plot survived domain apply but
  prevented publication to a loaded community map. The stronger regression
  checks the existing raw65000 value in the actual plot packet.
- `admin-removal-before.trx`: the new active-owner guard suppressed a permitted
  administrator removal's deletion packet. The removal-only exception restores
  the existing public handler contract.

Initial fixture compilation/setup failures and earlier passing candidates remain
recorded separately. `baseline-fixture-initial.*` and `before-map-fixed.*` lacked
complete real-map or packet setup; they are not behavior qualification.

## Verification

All **30 new SQLite cases**, **526 affected housing/guild/map-admission cases**,
**30 configured MySQL8.4.13 cases** and **11,931 full solution tests** pass,
with zero full failures/skips and all11,901 previous cases preserved. The full
solution build has zero warnings/errors. Both Source review axes are clear
across18 paths at manifest SHA256 `4dfc5dc694d24c355ff7e636aab6a7bd4708b016b2090e8c1bbc5b5ec17877c5`.

Source/Tools hash `9fa7bc75eee35cde07ace1d66c7c27facc3b7f64d806ab5555c3f319563f7f9c` binds 6,245
inputs. Test assembly SHA256 `41f93af23694b7d2f0e65be53e8826969d0383fcc92ff3cc2acb718d12c32a2b` and all
11 owner/test/provider DLL triples match. The owned MySQL
generation/listener and all recorded validation generations are stopped.
Build: 35.398s; MySQL: 319.915s; full
test command: 779.565s. These are observed validation
durations, not performance claims. verification.json links the final results,
retained negatives, reviews, source/binary bindings and workload shutdown proof.

Earlier29-focused/525-affected passing candidates remain intermediate evidence;
only the final gate includes the administrator compatibility correction.

## Boundaries and next roadmap work

Fixtures use the real public handlers, original registered roots, an actual
Player and ResidenceMapInstance, with owned relational databases. Geography,
map locks, tables and destination admission are controlled. Plug failure tests
control entity creation/removal. No physical travel, native presentation, full
retail tables, fee policy, broker/restart, durable outbox, raw-ADO/distributed
writer or exactly-once socket-delivery guarantee is established. The retained
raw ID is preserved data, not newly classified retail geometry.

No schema or database-source change, entitlement/default change or native/client
launch is included. Defaults15:1/17:1 remain; no automatic44 or seven baseline
character grants. Computer Use and native launch remain stopped.

A separate real CharacterAchievementManager/Player-save probe now reproduces
the next publication gap: one progress update, and for realm-first completion
one announcement plus an already-consumed in-memory claim, while SQL stays empty
through a trigger-rejected save. Removing the owned trigger lets existing
recovery persist progress1 or completed progress2. Table/global lookup and player
enumeration are controlled; the actual realm-first claim method is used.
next-achievement-evidence.json and next-achievement-binding.json bind both
observations to the frozen test/11-DLL candidate. This is not distributed,
restart, native, retail or socket qualification. Complete committed character
achievement publication next; durable realm-first arbitration remains separate.

After this placement/removal milestone, reassess the broader roadmap by impact
and available evidence: remaining economic/notification recovery, durable
realm-first/restart requirements, destination qualification and evidence-backed
gameplay. Contract public07CE/reset/unclaimed/excess/full726-choice delivery and
the documented missing native/table evidence stay open. Do not extend housing
with speculative cases merely to avoid moving to the broader queue.
