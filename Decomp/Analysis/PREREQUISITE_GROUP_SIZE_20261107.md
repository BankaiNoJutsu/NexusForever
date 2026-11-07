# Prerequisite 20 (GroupSize) compares the group member count (2026-11-07)

Evidence tier: **native body read** (`FUN_14049e820`, evaluator vtable slot `+0x308`). Source/SQLite only. No retail parity claim.

Native: the count at `+0x60` of the group manager at `DAT_140c65898 + 0x6c50` (the manager type 42 uses; 0 when there is none) is compared with `Value` through the integer helper `FUN_1404a2090`, count first. `ObjectId` is not read (0 in all 8 retail slots). Retail: comparisons
`>` x4, `==`, `>=`, `<`, `<=` with `Value` 0 to 4.

Server: `PrerequisiteCheckGroupSize` uses the player's `GroupAssociation` and `IGroupStateManager.TryGetGroup`; the size is the group's member count, 0 without a group or when the state manager does not know it, then the shared integer compare. `PrerequisiteCheckGroupSizeTests` (13 cases) were written with the
check (no red run); the 2,074-case Prerequisite/Group Source/SQLite filter passes (separate scope).
