# Type 196: primary-subject Racial (2026-11-14)

Native read: lead re-derived with LLVM21.1.8 and Ghidra12.1.4 headless,
build16042. Native meaning and retained Source bridge are graded separately.

Starting branch/head: `feature/spell-effects-remaining` / `287c2b1b`.
Question: which unit does type196 inspect, and does native evidence bind its
Value comparison to UnitRace rather than the separate player Race field?

Before edits: **Mapped earlier native reading**, case0xc4 reportedly calls
manager+38 ->14049c520 and compares first DWORD at unit+d0's record (zero when
absent) with Value for Equal/NotEqual only. Source uses CreatureEntry.UnitRaceId;
player wrapper substitutes Target by a prior-handler precedent. Explicit-unit
path retains subject and rejects null. Existing comments/tests are not ground
truth; subject selection, field/accessor identity and comparison need re-derivation.

**Observed prior reach:**40 slots/36 rows and20 direct/weighted references;
rerun census/anchors. Type102 remains **Blocked** on the missing attributable
retail untimed-Fluff lifecycle trace, per user direction.

Candidate files: type196 handler and focused tests, this finding, coverage,
relevant labels/inventory, ledger/handoff/spell tracker and scoped historical
supersession. Verify native identity/indexed dispatch/body, independent native
UnitRace/Race accessor bindings, shipped type/text and reference census. Run
new cases red before production edit, owning Game build and related filtered
Source tests. Static/headless only; native client/CUA remain stopped.
No retail parity claim.

## Verified native selector, null handling and comparison

Executable SHA256:
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Cached selector read:
`Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/1404a2100.fragment.c`.
Original indexed entries, register assignments and body instructions were checked
directly; the label itself was not treated as a read.

**Verified native default-manager rule:** table entry
`1404a4848+(196-1)*4` selects **1404a2217**. The case loads full Value to R9D
at1404a221a, comparison to R8D at1404a221e, **primary** to RDX at1404a2222,
then calls manager virtual+38 at1404a2228. ObjectId and context relationship
are unread. Default vtable140b67740 slot **140b67778 ->14049c520**.
The alternate table140b67e20 slot140b67e58 points to1407db510, the previously
re-derived `_purecall` path; it supplies no alternate Racial rule.

**Verified native body:**14049c520 saves Value in R10D and tests the unit
pointer at14049c523; a null unit returns false at14049c528..14049c52a. For a
present unit it loads the pointer at **unit+d0** at14049c52b. Absent record
means numeric **0** at14049c53e; otherwise it reads the record's first DWORD
at14049c539. Full32-bit equality is evaluated at14049c541. Comparison1 returns
that equality, comparison2 its negation, all other modes false. No masking,
rank, enum-range validation or truthiness rule is applied. Null primary differs
from a present unit whose record is absent: the latter can meet Equal/Value0.

## Verified native UnitRace identity; Source bridge limits

Original Lua Game.Unit method table140c5a770 independently binds:

- **GetUnitRaceId**, entry **140c5a7d0 ->14064a100**. The body resolves the
  entity at14064a121, reads entity+d0 at14064a12b and the first DWORD at
  **14064a152**, returning that signed integer as a Lua number. An absent
  record returns **nil**, not numeric0; type196's numeric-zero fallback comes
  from its own predicate, not this Lua accessor.
- **GetRaceId**, entry **140c5a7c0 ->14064a080**, reads separate **entity+d8**
  at14064a0ab. Thus player Race and UnitRace are distinct native attributes.
- **CriteriaProxy UnitRace accessor1403b49e0** loads proxy+8's entity, then
  entity+d0's first DWORD, returning0 when the record is absent. This provides
  a second consumer of the same UnitRace record and fallback. Its body was
  re-derived; the full TargetGroup type12/13 dispatcher was not requalified here.

**Correlated Source bridge, retained:** the handler reads
`CreatureEntry?.UnitRaceId ??0` and uses the existing boolean equality helper.
The Source Creature2 model names UnitRaceId separately from Player.Race; the
native Lua names establish the attribute being tested. The complete native
record-population/writer and dynamic reassignment chain was **not recovered**
in this pass. A bounded look at the previously mapped creature-info setter
140458a40 does not establish a writer of unit+d0. No claim is made that all
native transformations/overrides or Source CreatureEntry lifetimes are equivalent.
Resolving that requires the full native UnitRace assignment chain plus an
independent transition witness. No dynamic race behavior is invented here.

Competing readings:

- **Rejected:** relationship Target replaces primary, by precedent from
  another handler. The actual case explicitly passes primary; callers can
  still select a target by passing it as primary.
- **Rejected:** Racial means the player's separate Race enum or a boolean
  race flag. Native uses the UnitRace record ID and full Value, confirmed by
  independent GetUnitRaceId/GetRaceId accessors.
- **Verified native:** null unit fails; absent UnitRace record on a present
  unit compares numeric0; Equal/NotEqual only and ObjectId ignored.
- **Unqualified Source bridge:** CreatureEntry.UnitRaceId is retained, with
  complete native writer/override/lifetime equivalence explicitly unknown.

## Observed census and independent shipped type witness

Fresh read-only imported `wildstar_client` scan covers all three raw prerequisite
slots and all13 prerequisite columns in spell4/spell4effects: **40 slots /36 rows /
20 direct /20 weighted references**. All20 references are Spell4Effects
**target-apply** columns, spanning11 prerequisite IDs. Raw slots split index0=35,
index1=4,index2=1; Equal34/NotEqual6; all ObjectIds0. There are31 distinct Values,
and each resolves to an imported UnitRace row. These data support reach/ID
membership, not field meaning, cast impact or runtime timing. No early-None
filter is applied; nested/non-spell consumers are outside scope.

Read-only anchor: effects **73892/73893**, Spell4 **36031**, reference prerequisite
**19197** as target-apply. Its first slot is type196/NotEqual/Value97/ObjectId0,
flags0 and later slots empty. Table role describes the caller's selected unit;
it does not alter the native primary argument. Raw queries/results are ignored
in `artifacts/prerequisite-type196/anchors.tsv`; full census is retained there
as `census.py`, `ranking.json`, `slots.json`, `references.json`.

Existing owning GameTable/TextTable readers independently give shipped type196
-> error **419856 -> Racial requirement not met**. Reused payload SHA256:
`9ddd928b6c423072a1e34d6797003297ab8f75bdbca6f906227ca557f5ea3a32`
(PrerequisiteType.tbl) and
`588e385a0e8dee8b92dc79018070a17e78d3efd535b948224702effecafa5cf7`
(en-US.bin). Generic Racial text corroborates the type name; native bodies and
Lua bindings supply its attribute meaning. Payloads/probe output stay ignored.

## Implemented / Source correction and regression evidence

The sole production behavior change is the player wrapper delegating with
**player**. The explicit-unit guard, CreatureEntry bridge, comparison helper,
full Value rule and state producers remain unchanged. No field writers or
dynamic race/model changes are added. The previous target-first test is
rewritten to retain primary, with an explicit target-as-primary control.

**Observed red before production edit:** focused scope runs **31 cases:
11 failed /20 passed /zero skipped**. There are23 new cases: eight real-manager
opposite-primary/relationship checks, two null-primary checks, two null-unit
checks, one absent-parameters check, six raw unsupported modes and four
full32-bit Value/ID controls. Eleven new cases fail;12 new controls and eight
existing cases pass. The earlier historical note's nine-test count is not
the actual current baseline: the existing file has six theory cases and two
facts. New manager checks include absent CreatureEntry versus present ID0,
deliberately contradictory Player.Race, ignored ObjectId and explicit target
selection, while preserving the relationship object. Tests qualify Source
input contracts, not complete retail record population or transitions.

**Observed final verification:**

- `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown|FullyQualifiedName~Movement|FullyQualifiedName~UnitStateFlagPacketTests"`
  passes **3,286 /zero failed /zero skipped**, including all23 new cases and
  the rewritten target-first control. Red/green logs stay ignored. Existing
  Aspire ASPIRE010, five MySqlProviderQualificationTests CS8632 and one
  ReceivedPropertyEffectTests xUnit2031 warning remain; no claim to fix them.
- `dotnet build Source/NexusForever.Game/NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`
  passes with **zero warnings/errors** (`owning-build.log`).
- Three focused labels applied/re-exported read-only: predicate renamed
  **Prerequisite_CheckPrimaryUnitRace14049c520**, plus Lua_Unit_GetUnitRaceId
  and EntityCriteria_GetUnitRaceId comments. Applied3/created1/localLabels0/
  missing0/duplicateRows0; the64-byte predicate function was first defined in
  that read-only session. `labelled.txt` confirms all three names and bodies.
  Changes are discarded from the project; durable CSV is the label source.
  Default export/cache/manifest are not refreshed. No raw decompilation tracked.
- Exactly three existing function inventory records updated, no additions or
  removals;6,211 function records remain. **8,350 mapping rows across seven
  files validate** with `Decomp/Analysis/Validate-DecompileMappingArtifacts.ps1`.
- `python artifacts/prerequisite-type196/verify_slice.py` and `git diff --check`
  pass native identity/indexed/vtable/Lua bindings, shipped payload hashes/name,
  imported census/anchors/31 Value IDs, sole wrapper behavior change, test/build
  logs, scoped label/inventory updates,12 coverage scopes and tracker links.
- Full suite not repeated; its preceding non-green result remains in the
  handoff. No deployment, provider/socket/native execution or retail acceptance.

End state: **implemented with Source verification; Verified native default
primary/null/UnitRace/Value/comparison rule; retained CreatureEntry bridge
correlated**, with complete native writers/dynamic assignment/lifetime unqualified.
Type102 remains **Blocked** on the missing attributable retail untimed-Fluff
lifecycle trace. Next ranked wrapper candidate is **type105 SpellEffectTypeOnUnit**
(10 weighted references /four direct /seven slots /five rows), requiring its
own indexed/body/list-coverage read without reopening type102's retail gate.
Type76 remains a shared native stub. Native client/CUA remain stopped.
No retail parity claim.
