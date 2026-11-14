# Type 28: primary-subject combat state (2026-11-14)

Starting branch/head: `feature/spell-effects-remaining` / `7702f101`.
Question: does the relationship target replace the primary unit for InCombat?

Before edits: **Mapped native selector**, direct evaluator case `0x1c` in
`1404a2100` reads primary unit `+0x2ac`; the player wrapper substitutes Target,
while the explicit-unit wrapper retains its subject. The existing test
`OldPlayerTargetApiRetainsTargetProxyWhileUnitApiUsesExplicitSubject` encodes the
discrepancy, not retail evidence. Recheck field meaning and caller selection
before changing behavior. Type 102 stays **Blocked** on the missing attributable
retail untimed-Fluff lifecycle trace; this slice does not reopen it.

**Observed reference census:** among the remaining target-substituting wrappers,
type 28 has highest raw spell dependency reach: 230 slots / 230 prerequisite
rows / 1,293 direct and slot-weighted spell-column references. Next are 39
(1,140 weighted), 40 (705) and 12 (364). These are dependency counts, not broken
casts; nested/non-spell consumers and original activation policy are outside
the count. Read-only imported reference DB census; no runtime data changes.

Candidate files: type-28 check, focused context/flag tests, this finding, ledger,
handoff and spell tracker. Native labels only if a new mapping is needed.
Verification: native body/disassembly and field witness; shipped/reference
anchors; new focused cases red before production edit; owning Game build;
Prerequisite/Spell/Cooldown tests and `git diff --check`. Static/headless only,
no native client execution or Computer Use. No retail parity claim.

## Native witnesses re-derived by the lead

**Verified native selector/field:** installed build 16042 SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`,
matching the cached Ghidra fragment. LLVM 21.1.8 disassembly and direct PE reads
corroborate the bodies; no earlier label is accepted as a field witness.

- Evaluator `1404a2100.fragment.c`, case `0x1c`, lines 234–245, uses `*param_2`
  (primary unit), not `param_2[1]` (relationship unit). Null primary fails. The
  switch entry at `1404a4848 + (28-1)*4` resolves to `1404a3045` in the matching
  executable. Disassembly `1404a3045..1404a305c` reads the primary pointer and its
  `+0x2ac` field, then enters `1404a22ec`'s shared comparison tail. Equal returns
  the field, NotEqual tests zero, other comparisons fail; Value/ObjectId are not
  read by this case. The existing boolean comparison implementation is retained.
- Lua method table `140c5a770`, entry `140c5b1d0`, contains the actual string
  `IsInCombat` and function pointer `140653ed0`. That function resolves its unit
  then compares its `+0x2ac` field to zero at `140653f01`, publishing a Lua
  boolean. This independently binds the field's meaning; it does not establish
  the retired server's combat-entry/exit scheduling.
- Cache path: `exports/WildStar64.exe/selected_decompiled_cache/functions/`
  `sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/`
  `1404a2100.fragment.c`. Read-only local evidence is under ignored
  `artifacts/prerequisite-type28/`: `native-binding.json`,
  `dispatcher-disassembly.txt`, `combat-ui-disassembly.txt`. No raw native code
  is promoted into tracked files. Existing selector/Lua labels need no change.

**Observed Source caller contract:** `Spell.CheckPrerequisites` passes Caster
and target separately through `MeetsUnit`, reversing the relationship for target
checks. `ClientActivateUnitCastHandler.MeetsActivateSpellPrerequisite` supplies
player as subject and activated entity as Target through the legacy API.
`IPrerequisiteCheck` documents the explicit subject/relationship distinction.
This proves current Source routing, not native activation-caller policy.

**Rejected reading:** Target substitution for type 28 contradicts the direct
native selector. **Retained limit:** no change to combat state production,
timing, group/aggro rules or activation selection policy follows from this read.

## Reference anchors and selection boundary

**Observed imported data:** all three slots per prerequisite and all 13
prerequisite columns in `spell4` / `spell4effects` were counted without treating
an empty earlier slot as a reach filter. All type-28 slots use Equal (71) or
NotEqual (159); Value is zero in 204 of 230. Caster-cast Spell4 25506 references
prerequisite 390, while target-cast Spell4 26778 references 28199. Both caller
roles therefore exist in the data; the evaluator's primary unit must remain
caller-selected. Tables corroborate the two roles, not the meaning of the field.
Ignored `ranking.json`, `slots.json`, `references.json` and `rank.py` retain the
read-only census and exact column scope for reproduction.

**Verified native selector / planned Source correction:** delegate the player
wrapper to `MeetsUnit(player, ...)`, preserving Target as relationship context.
Rewrite the obsolete context test and `InCombat_ComparesTargetCombatState` to
assert the primary player, retaining explicit-target controls. Seven new cases
cover contradictory primary/target combat states through both real manager
entry points, Equal/NotEqual, null primary with a matching target, and absent
relationship parameters. Their red run precedes the production edit.

## Result: Implemented / Source

- **Observed red run:** seven new context cases failed, zero passed, before
  changing production. This replaces the old one-case contradictory-API test;
  the net test count increases by six. `red.log` retains the failures.
- **Implemented / Source:** only the player wrapper delegates to `MeetsUnit`
  with player. The renamed `InCombat_ComparesPrimaryPlayerCombatState` test
  uses opposite player/target states, preserving boolean/value comparisons.
- **Observed verification:**
  `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown"`
  passes **3,020 / zero failed / zero skipped** (`green.log`). Warnings are in
  unchanged Aspire configuration, MySqlProviderQualificationTests (five CS8632)
  and ReceivedPropertyEffectTests (one xUnit2031); none is represented as fixed.
- Owning `dotnet build Source/NexusForever.Game/NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`
  passes with **zero warnings/errors** (`owning-build.log`). Native identity,
  switch/Lua binding and reference counts reproduce; `git diff --check` passes.
- Full suite not repeated for this narrow selector correction; its preceding
  non-green result remains in the handoff. No deployment, provider/socket,
  native execution or retail acceptance qualification.

End state: **implemented with Source verification; Verified native selector**.
Type 102 remains blocked. Next raw-reach wrapper is type 39 (1,140 references),
which needs its own selector/body and caller review. No retail parity claim.
