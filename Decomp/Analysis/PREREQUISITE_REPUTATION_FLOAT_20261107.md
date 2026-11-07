# Prerequisite type 5 (Reputation) compares a signed float (2026-11-07)

Evidence tier: **native body read** (evaluator case 5 via `unit+0x118` object virtual `+0x20`, then float helper `FUN_1404a2010`) plus retail value ranges. Source/SQLite only. Signed interpretation of `Value` is **inferred** from the retail data (a negative `Value` in an unsigned field is otherwise meaningless). No retail parity claim.

## Evidence
Retail slots: comparisons >= 274, < 19, <= 2, != 7, == 4, > 2. Signed Values -2000, -30000, -32000 occur and 13 slots test `< 0`.

## Server before
`PrerequisiteCompare.Compare(comparison, (uint)amount, value)`: a negative reputation cast to uint is undefined/saturated, so `< 0` slots and negative-threshold slots could not work.

## Change
`PrerequisiteCheckReputation` now uses `PrerequisiteCompare.CompareFloat(comparison, amount, (int)value)`. `PrerequisiteCheckReputationTests` (13 cases) was written first; 3 of 12 initial cases failed against the old code (the 13th was added when fixing a mistaken expectation). The 1,423-case Prerequisite filter passes (separate scope).
