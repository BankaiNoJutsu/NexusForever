# Q3777 physical fragment pickup

6 November 2026. By Leaps and Bounds now grants real pure-loftite fragment
item6998 through the existing crystal range producer. Collection progress
comes from owned inventory quantity. The previous script incremented the
CollectItem objective without creating an Item and could consume a crystal
while sequential quest gating refused its progress.

## Owning rows and accepted behavior

Recovered build16042 quest3777 orders conversation5075 (type17, data219,
count1), activation5076 (type5, data6952, count1, flags3) and collection4859
(type4, item6998, count8, flags3). Item6998 is stackable with max100 and
Item2Type170. Both the reviewed legacy6987 bridge and recovered13120 creature
filters remain, with Creature and Collectable wrappers.

The producer requires the accepted current quest, prior conversation, original
map/quest-manager/inventory and an admitted inventory mutation lease. It
verifies a real physical grant before advancing first-fragment feedback.
A full/frozen inventory leaves the crystal available; preowned sufficient
fragments synchronize without a ninth item or consuming an unused crystal.
Pre-grant collection feedback must retain the original pickup context.

An applied pickup retains its claim before objective feedback, so duplicate or
reentrant callbacks and rejected item/removal publication cannot grant again.
Removal uses the captured original crystal GUID/map and can retry after refusal.
The original accepted item's progress can reconcile through its current Player
dependencies even if feedback rebinds the script to another crystal.

## Verification and publication scope

Seven baseline failures and one inactive-quest control were reproduced. Three
review counterexamples additionally exposed capacity advancing the first
objective, ownership replacement during pre-grant collection sync, and
activation feedback taking the last slot before physical creation. All are
fixed; failed and superseded outputs remain retained.

All13 new cases and all141 scoped cases pass, with zero skipped. Six older
Q3777 proxy-call cases now assert actual inventory and quest outcomes. Coverage
includes capacity/lease retry, sequential conversation, repeated/reentrant
pickups, original crystal replacement, feedback/removal refusal, preowned
fragments and eight pickups achieving the quest. A failed full real Player
SQLite save followed by retry and fresh actor reload retains all eight physical
fragments and Achieved progress.

The same13 cases also pass with actual recovered QuestInfo and ItemInfo.
The offline adapter verifies all385 recovered payload hashes/extents and loads
21 typed tables. Its first execution lacked the native SQLite dependency and
passed no cases; the exact test-output native DLL was then copied, hash checked,
and all13 passed in a fresh evidence directory. Earlier adapter/setup failures
remain recorded separately from qualification.

Both final Source review axes have zero remaining findings. Final Game/owning
Script/test-only builds are clean in6.46/3.03/16.19 seconds; the offline adapter
build is clean in2.61 seconds. The scoped runner reports19 seconds. No full
solution build or full test suite ran.

A concurrent published Guild achievement commit b3fd74ab advanced the original
96b4d1a6 parent. Its15 exact published Source blobs were adopted into the
isolated candidate before the final builds, scoped run and actual-table run.
The comparison covers6,309 parent tracked Source files, our three tracked
changes, one new test file and eight historical filter/format differences.
All138 DLLs are bound; Script.Main and Game.Tests contain this milestone, while
Game changes only for that published parent. Owning Game/Script match test
copies, and the offline runner uses those same dependencies. Foreign working
tree Source is excluded.

The [portable record](coverage/q3777-fragment-pickup-20261106.json) pins the four
Source files, original/final parents, recovered inputs, DLLs, tests, failures
and reviews. Raw logs/TRX and table exports remain under
artifacts/continuation-20261106/northern-wilds-producers/.

## Remaining qualification and requested stop

This covers an accepted quest with prepared entity/map/session collaborators.
Public NPC acceptance, turnin/rewards, actual world respawn/content, provider
restart, sockets/deployment and native/retail remain open. The existing5m
range/jump activation interpretation is retained compatibility behavior.
Historical phantom progress and already-Achieved characters are not migrated.

No schema, migration, entitlement, setup or provider/native-client/CUA workload
changed. Primal allocation remains diagnostic until authoritative essence
debit/synchronization is established; no ledger was guessed from client caches.
The broader Goal remains incomplete. The user requested finishing this milestone
and stopping; pause after verified commit/push and resume only on instruction.
