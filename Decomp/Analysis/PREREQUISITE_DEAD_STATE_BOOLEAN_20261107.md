# Prerequisite 12 (DeadState): native flag and boolean tail (2026-11-07)

Evidence tier: **native body read (decompile and disassembly of `FUN_14049c800`)**. Source/SQLite only. No retail parity claim.

2026-11-14 follow-through: [primary-subject re-read](PREREQUISITE_12_PRIMARY_SUBJECT_20261114.md)
corrects the player wrapper to retain its primary. **Verified native** Lua IsDead
binds `+0x250` alone; the complete native predicate's equivalence to Source
`!IsAlive` remains **inferred**. The shared field writer and remaining bridge/
Ghost-route limits are recorded there. Source tests do not promote that proxy.
No retail parity claim.

Body: `flag = (*(int*)(unit+0x250) != 0 || *(int*)(unit+0x254) != 0 || *(int*)(unit+0x80) == 0x17) ? 1 : 0`; comparison 1 returns `flag == Value`, comparison 2 returns `flag != Value`, anything else false. `0x17` is `EntityType.Ghost` on the server.
Changes: (1) a Ghost entity counts as dead even when the alive flag is set; (2) comparisons other than Equal and NotEqual are false (the earlier `Compare` also accepted ordering comparisons). Retail: 168 slots, 159 Equal and 9 NotEqual, Value 0 or 1, so (2) changes no retail row; (1) changes rows only for Ghost entities.
Inferred, not verified: that the two unmapped fields `+0x250`/`+0x254` correspond to `!IsAlive` (the previous proxy, kept). Falsifier: the writers of those fields.
Tests: `PrerequisiteCheckDeadStateTests` (10 cases; 4 failed first: the Ghost rows and the two ordering-comparison rows). Prerequisite filter: 1,632 cases pass (Source/SQLite scope).
