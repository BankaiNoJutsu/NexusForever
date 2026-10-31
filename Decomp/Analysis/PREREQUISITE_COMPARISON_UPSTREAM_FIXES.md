# Prerequisite comparison and active-duel reconciliation

Implemented 2026-10-29 during the assessment follow-through goal, preserving
the fork's individual checks, cast contexts and entitlement defaults.

## Comparison IDs

[Upstream #594](https://github.com/NexusForever/NexusForever/pull/594), reviewed
head `7377ae5f79200f60919a724203c5376e30d90549`, identifies a numeric enum
ordering error. It is now independently verified against the installed client
`D:\WildStar\Client64\WildStar64.exe`, file version `1.7.8.16042`, SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.

| Raw comparison | Native behavior |
| --- | --- |
| 1 | Equal |
| 2 | Not equal |
| 3 | Greater than |
| 4 | Greater than or equal |
| 5 | Less than |
| 6 | Less than or equal |
| Other | False |

Integer helper `1404a2090` and float helper `1404a2010` agree. Recovered June
2026 cached fragments, an exact-hash recovered Ghidra project, fresh exports
from that project and fresh REA analysis all corroborate the integer mapping.
The fresh REA function record is
`ev_58cafbc08d7e6aeb8d22ba9ccd611b7ec2ee96e57946fcd58b5d4c51ce122085`;
its summary record is
`ev_e8474fe2b25404f293daf848f877f939b2680d5d3b5008aaa860d6f0b6fd632b`.
REA reports partial caller coverage; this does not affect the directly examined
comparison body and does not establish live execution.

Read-only queries against the existing imported `wildstar_client.prerequisite`
table provide concrete boundary cases: row 4992 is level >=1 (raw 4), row 391
is level >8 (raw 3), row 76 is level <=5 (raw 6), and row 6232 is level <1
(raw 5). Raw-ID fixtures pass through the real prerequisite manager and keyed
level check, preventing symbolic enum labels from masking a wrong table mapping.

Only the enum's numeric assignments for 3-6 change. All individual scalar
checks, item/spell contexts, table arrays and unsupported-mode guards remain.

## Type 204: use the existing duel runtime

[Upstream #600](https://github.com/NexusForever/NexusForever/pull/600)'s constant
nondueling assumption cannot represent this fork's duel sessions. Native live
dispatch `1404a2100`, case `0xcc`, forwards the entity and comparison through
manager vtable `140b67740+0x1f0` to `14049db50`. The check requires the evaluated
local player and compares active duel state at `DAT_140c65898+0x6ee4` with 3,
supporting Equal and NotEqual only. It ignores the row's value/object fields.
`Lua_GameLib_ForfeitDuel` at `1407011b0` gates the mapped 0x00EA sender on that
same state, independently identifying the active-duel boundary.

The old InPhase label at `14049db50` was incorrect. It is corrected to
Prerequisite_CheckDueling in the durable label map and reapplied/exported with
the repository workflow. The fork's new check calls DuelManager.IsDueling,
which reads actual session state under the existing duel lock. Invitations,
countdown, finished sessions, outsiders and missing players do not count as
active duels. Unsupported comparisons fail closed. No duel lifecycle or combat
permission behavior is replaced.

Type 269 remains RapidTransport: native live case `0x10d` forwards objectId
through manager +0xd0, matching the existing cast-node context. #600's
UnhealthyTime replacement remains rejected. This pass does not adopt broader
prerequisite architectural rewrites from #554/#568 wholesale.

## Verification and local evidence

Fifty-one new comparison cases cover all six raw IDs below/at/above the
threshold for integer and float values, unsupported IDs, and twelve imported-row
manager evaluations. After correcting a test fixture to supply the actual uint
Level property type, the pre-fix run reproduced **12 failures / 39 passes**;
all 51 pass after the enum fix. The complete prerequisite slice passes 445 tests.

Fourteen new duel cases verify registration for raw type 204, both participants,
all five lifecycle phases, outsiders, null context, ignored value/object fields
and unsupported comparison IDs. The combined prerequisite/duel/account focused
run passes 496 cases before the final registration case was added.

The combined gate including packet scopes, dependency fixes and three further
native-verified protocol fixes passes the full solution build with **zero
warnings and zero errors**, and **6,112 tests pass** (zero failures/skips).
The first combined full run exposed parallel test fixtures sharing fixed
account IDs in the process-wide fallback pending-item repository. Those three
fixture classes now share an isolated test collection; runtime repository
behavior and entitlement defaults are unchanged.

Local artifacts under `I:\GIT\NexusForever-assessment-report\remaining-goal`:

- `prerequisite-level-rows.tsv`: read-only table correlation.
- `prerequisite-integer-compare-extended.evidence.json`: complete REA bundle.
- `prerequisite-before-corrected-fixtures.log`, `prerequisite-after.log` and
  `prerequisite-duel-account-focused.log`: targeted red/green evidence.
- `prerequisite-label-summary.json`: exact-hash project match and refreshed
  durable labels; the repository's ignored Ghidra logs contain focused exports.
- `full-build-second.log`, `full-tests-second.log`, `full-suite-second.trx`:
  combined verification.

REA's first cold analysis hit the 330-second startup deadline. After owned
cleanup finished, the documented longer startup setting allowed a new CLI
attempt to finish. No MCP registration or global tool configuration changed.
The repository's existing Ghidra scripts supplied independent focused exports.
No decompiled client source is copied into tracked code or documentation.
Live-client interaction presentation remains a separate verification gate.
