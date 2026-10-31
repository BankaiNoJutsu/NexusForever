# Automatic material satchel routing

Implemented on 2026-10-29 as a focused reconciliation of upstream
[PR #329](https://github.com/NexusForever/NexusForever/pull/329), reviewed head
`f1e057f0bd58683696e694f8f737cb1fad811757`. The old develop patch routes material
creation to the supply satchel before allocating a bag slot, excluding
ResourceConversion. The fork's richer inventory and delivery contracts are
retained rather than replacing them with that seven-line patch.

## Routing and preservation

SupplySatchelManager exposes a pure TryGetAutoStoreCapacity query, using its
existing material mapping and reward-based maximum stack amount. It does not
create a material record, allocate an item ID, send a packet or mutate a count.
Existing/custom satchel implementations default to no automatic capacity until
they explicitly support this query.

Automatic creation applies only to the player's normal Inventory location and
mapped ordinary materials that are stackable, not equippable, uncharged and
nonexpiring. Loading state and missing static tables retain bag behavior.
Material IDs must fit the existing 512-entry satchel snapshot. The current
CharacterFlag/settings code has no separate automatic-satchel preference;
this slice introduces no new default or entitlement policy.

An active CollectItem objective for the same Item2 ID also retains bag routing.
The fork currently counts and consumes physical bag items for those objectives;
this avoids changing quest progress or turn-in semantics. Inactive quests,
other objective families and objectives for other item IDs do not block normal
material routing. General quest counting across satchel and inventory remains
a separate semantic task.

Before adding anything, Inventory validates that the satchel allowance plus bag
stack/slot space can hold the complete quantity. A normal capacity failure
sends only the existing inventory-full error and leaves both destinations
unchanged. A full satchel falls back to existing bag stacks and then new stacks.
Accepted satchel quantities are subtracted exactly once; only the remainder is
created in bags. A defensive capacity-change branch retains quantities if a
custom manager violates the normal owning-map-thread stability assumption.

Inventory.CanCreateItem now predicts this default creation path without side
effects. Static-loot preflight uses the same pure capacity, so a full physical
bag can receive a material when the satchel fits it. Existing loot map/owner,
generation eligibility, group rules and exactly-once delivery checks are
unchanged. Insufficient combined capacity leaves the loot available for retry.

ResourceConversion remains excluded from automatic creation. This preserves
satchel withdrawal: bag space receives what fits and its existing refund path
returns only the unplaced remainder to the satchel. Equipped/bank/realm-bank
destinations and AddItem/LoadItem paths for existing item instances retain their
original behavior, preserving charges, expiry, binding and other instance data.

## Verification

Thirty-three new regression cases use isolated real Inventory, Bag and
SupplySatchelManager instances; they do not mutate global AssetManager state.
They cover pure capacity/preflight, complete and partial routing, exact stack
and slot boundaries, full/legacy-overfull satchels, no partial mutation on
failure, actual loot delivery and retry/exactly-once grants, missing/mapped data,
network material-ID bounds, other inventory scopes, withdrawal refunds,
ResourceConversion, instance properties, loading, active/inactive quests and
existing reward-derived caps.

```powershell
dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo --filter 'FullyQualifiedName~InventoryAutoSatchelTests|FullyQualifiedName~SupplySatchelManagerTests|FullyQualifiedName~Loot|FullyQualifiedName~InventoryQuestObjectiveUpdaterTests|FullyQualifiedName~RealmBankInventoryTests|FullyQualifiedName~Q3741RewardInventoryPersistenceTests'
```

The focused run passes **172 tests**, zero failures and zero skips. Logs are at
`I:\GIT\NexusForever-assessment-report\remaining-goal\satchel-focused-final.log`.
The combined full build and suite belong to the parent assessment goal.
No schema migration, entitlement default or wire-layout change is introduced.
Live gameplay and populated-server validation are not claimed by these tests.
