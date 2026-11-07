# Prerequisite types 83 (Vehicle) and 84 (Mount) (2026-11-07)

Evidence tier: **native bodies read**; one reading is **inferred** (the seat field is the seat *type*, below). Source/SQLite only. No retail parity claim.

## Found by

An audit comparing which of `Value` and `ObjectId` the native evaluator passes to each body (`prereq_arg_shapes.json`, the evaluator switch) with which the server check uses. Most of the 85 mismatches were
false positives (the body reads the slot through the evaluator context, or the server reads the field through a helper); ranked by retail impact the real ones were the vehicle and mount tests.

## Native bodies

`FUN_14049cbb0` (type 83, vtable `+0xe0`) and `FUN_14049ccc0` (type 84, `+0xe8`) are the same test with a different entity gate:

- the unit's vehicle entity is looked up from the guid at `unit+0xc0`; type 83 requires its virtual `+0x20` to be non-zero, type 84 requires its entity type (`+0x80`) to be **7** (`EntityType.Mount`);
- without such an entity, **Equal is false and NotEqual true**;
- otherwise it matches when `ObjectId` is 0 or equals the integer at the start of the entity's data pointer (`entity[3]`), and, unless `Value` is **-1**, when the integer at `unit+200` equals `Value`;
- Equal returns the match, NotEqual its negation, any other comparison false.

Retail: 83 has 97 slots (Equal 58, NotEqual 39) and 84 has 15 (Equal 7, NotEqual 8). `Value` is 0xffffffff in 80 and 14 slots (any seat) and 0 in 17 and 1. All 57 distinct non-zero vehicle `ObjectId`s are `Creature2` ids
(none is a `UnitVehicle` id), so the compared integer is the vehicle's creature id.

## What the server did

`PrerequisiteCheckVehicle` compared "the platform is a vehicle" (1 or 0) with `Value`, and `PrerequisiteCheckMount` (also registered for type 289) "is on a mount platform" with `Value`. With `Value` 0xffffffff, Equal was
false and NotEqual true for every slot, and `ObjectId` was ignored: 112 slots (83 and 84) evaluated wrongly whenever the player was on a vehicle or mount.

## Change

`MovementPrerequisiteHelper.MeetsPlatformVehicle` implements the body above for a platform entity type; type 83 uses `EntityType.Vehicle` and type 84 `EntityType.Mount`. Type 289 (a different slot, comparison only,
body not read) keeps the earlier behaviour in the new `PrerequisiteCheckMountScalar`. `PrerequisiteCheckVehicleTests` (29 cases) failed 13 of 29 before the change (8 vehicle, 5 mount) and pass now. The existing
`PrerequisiteCheckTests.Mount_ComparesPlatformEntityType` encoded the superseded "Value is a mounted flag" and was replaced by native-semantics cases. 1,571 Prerequisite/Vehicle/Mount/Movement/Transport Source/SQLite
cases pass (separate scope).

## Inferred

- That `unit+200` is the seat **type** (`VehicleSeatType`: Pilot 0, Passenger 1, Gunner 2, Invalid 3) rather than the seat position. Supporting: the server's own enum note says every client entity starts with seat type 3,
  and the retail `Value`s 0 and -1 fit "pilot seat" and "any seat" (the 17 `Value` 0 slots name creatures such as 43809/43810/32249 that read as vehicles the player must pilot). The same result for the pilot seat would
  follow from seat position 0. Next evidence: the client writer of `unit+200`.
- Type 83's gate is the entity's virtual `+0x20`, which was taken to mean "is a vehicle" (entity type 3); a mount is excluded from type 83, as before.
