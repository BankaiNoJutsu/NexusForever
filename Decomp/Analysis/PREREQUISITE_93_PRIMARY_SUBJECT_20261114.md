# Type 93: primary-subject Spell4EffectCategoryOnUnit (2026-11-14)

Native read: lead re-derived with LLVM21.1.8 and Ghidra12.1.4 headless on
build16042; tiers below distinguish native semantics from Source list coverage.

Starting branch/head: `feature/spell-effects-remaining` / `450be0b7`.
Question: which unit does type93 inspect, what does its active-list predicate
compare, and how far does native evidence support the current SpellClass bridge?

Before edits: **Mapped earlier native read**, case0x5d reportedly walks primary
unit+15c8, follows node+38 -> spell+138 -> data+70 -> row+f8, compares ObjectId,
ignores Value and uses boolean Equal/NotEqual. Source's player wrapper substitutes
relationship Target; explicit-unit path retains subject. **Implemented, inferred**
Spell4Base.SpellClass interpretation and Source tracked-spell proxy were based
on five imported slots with ObjectId36/38. Earlier comments/tests are not ground
truth. Native selector/list/field chain needs lead re-derivation before promotion.

**Observed prior reach:** five slots/rows and33 weighted spell references; rerun
census and anchors. Type102 remains **Blocked** on missing attributable retail
untimed-Fluff lifecycle trace by user direction; this pass does not reopen it.

Candidate files: type93 handler, focused context and predicate tests, this note,
coverage, ledger/handoff/tracker and only directly relevant labels if needed.
Verification: executable hash/indexed dispatcher/disassembly, native list and
row bindings where recoverable, shipped type/text, read-only imported census;
regressions red before production edit, owning Game build and related filtered
Prerequisite/Spell/Cooldown/Movement/state-packet tests. Static/headless only;
native client/CUA remain stopped. No retail parity claim.

## Verified native selector and predicate

Executable identity: SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
The cached selector at
`Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/1404a2100.fragment.c`
was read and checked against original indexed entries and instructions.

**Verified native:** table entry `1404a4848+(93-1)*4` resolves to
**1404a2b0b**. It loads primary from `[RDX]`, rejects null at1404a2b0e..1404a2b11,
then loads the primary's **+0x15c8** list at1404a2b17. It never loads the
relationship at context+8. ObjectId comes from slot+8 at1404a2b27; Value at
slot+12 is unread. The loop follows node+18 at1404a2b4c and compares the
full DWORD at **node+38 -> wrapper+138 -> data+70 -> record+f8** with ObjectId
at1404a2b30..1404a2b3f. This is equality with any matching list entry, not an
effect-group lookup, count comparison, bit mask or applying-caster filter.

Empty list/no match uses1404a2575 (flag0); a match uses1404a257c (flag1).
Both enter the shared boolean tail1404a22ec: comparison1 returns the flag
through1404a305c, comparison2 negates it, other modes fail. Prologue1404a2111
zeros the result; null primary returns false before either boolean mode.
This null behavior is directly supported here, unlike type149's helper.

## Verified native SpellClass binding and receiving-unit list

The earlier unknown DWORD can now be named from independent witnesses:

- **Verified native builder:** `140568540` resolves Spell4 via140236580,
  loads its base ID, then calls **140236e00** at14056857d. The getter uses
  descriptor **140a6d118**, whose PE name pointer **140ae1248** is UTF-16
  **Spell4Base**. The builder reads the returned row+58 at **14056879d** and
  stores it into the execution record reached through data+70, at record+f8,
  at **1405687a0**. Native override paths may resolve different runtime data;
  this does not establish Source support for such overrides.
- **Verified shipped schema/readback:** reused extracted `DB/Spell4Base.tbl`
  from the prior focused ArchiveProbe pass, SHA256
  `34e3faeeee7c4b9889b808a36ec69b40218ebe013cbf95e9e26fc6440005c93a`.
  The existing owning `GameTable<Spell4BaseEntry>` reader materializes44838
  rows; every row's modeled **SpellClass** equals its raw DWORD+58, with ID
  alignment checked and record size176. Existing GameTableHeader/GameTableField
  structures also read shipped field20's own name **spellClass**, type **UInt**.
  No new table-format reader is used.
  Thus the runtime field is **Spell4Base.SpellClass**, not an unidentified
  category inferred solely from36/38. Probe wrapper and output remain ignored.
- **Verified independent native accessor:** original PE Lua method entry
  **140c5a570** binds ASCII **GetClass ->1405ec1c0**. The body obtains Game.Spell
  runtime data with1403acd90 at1405ec21c; it reads data+70 at1405ec226 and
  record+f8 at1405ec233, returning that integer as a Lua number. This is a
  spell class, not the entity's character-class field.
- **Verified native list ownership:** `140717990` stores the owner wrapper
  at target-record+38 (**1407179d7**), copies the receiving entity ID to+40
  (**1407179ee**), and links that record into the receiving unit+15c8 list
  selected at **1407179e7**. Next-link+18 is used at140717a1d and the list
  head is updated in the following stores. The selector therefore inspects
  spells received by its primary, not merely pending spells cast by it.
  This re-derives the relevant list witness without reopening type102.

**Rejected stale label:** durable address **1403d6f1c** was named
Prerequisite_CheckSpell4EffectCategoryOnUnit. Original bytes show `INT3`
padding between a preceding return1403d6f13 and code starting1403d6f20;
Ghidra finds no function there. That label and its inventory record are
replaced by the actual inline case **Prerequisite_CheckPrimarySpellClass
1404a2b0b**, preserving the label CSV's physical row count. The prior
14049f690/table-index reading is already rejected as type6 QuestState;
neither old pointer-run label is evidence for type93.

## Source boundary and competing hypotheses

**Implemented / Source correction:** only the player wrapper's chosen unit
changes from Target-or-player to **player**. The existing direct-unit/null
check, boolean comparison, ObjectId handling and helper remain unchanged.
The public legacy enum/handler name is retained; it does not redefine the
native predicate as an effect category.

**Retained inferred coverage:** `SpellPrerequisiteHelper.HasTrackedSpellOfClass`
requires a concrete UnitEntity and calls its existing helper. That walks
`TrackedSpell4Ids()`, resolves Spell4 -> Spell4Base and compares SpellClass.
The field comparison is now Verified native, but those Source state families
are a proxy for the native receiving-target list. Their complete membership,
registration/removal lifetime, unrepresented effects and original-server
scheduling are not qualified. Missing-table/row guards remain Source behavior.
No pending-spell fallback, new state family, registration policy, timer or
unobserved lifetime is invented in this slice. Type102 remains Blocked.

Disposition of the competing readings:

- **Rejected:** relationship Target selects the checked unit. The native
  branch explicitly loads primary; a caller may pass a target as primary.
- **Rejected:** Value identifies an effect group or any nonzero ObjectId is
  merely true. Native compares full ObjectId against SpellClass and ignores Value.
- **Verified native:** a matching SpellClass in any primary receiving-target
  record supplies the boolean result, Equal/NotEqual only; null is always false.
- **Unverified:** Source tracked state exactly reproduces all native list
  members/lifetimes. A complete family-by-family producer/removal mapping and
  attributable original-server traces would distinguish that from partial coverage.

## Observed imported and shipped witnesses

Fresh read-only `wildstar_client` census covers all three raw slots and all13
prerequisite columns in spell4/spell4effects: **five slots /five rows /33 direct
and weighted references**. All slots are index0, Equal, Value0. Rows23731/43043
have ObjectId38;23732/24119/43044 have ObjectId36. Later slots are empty in all
five rows. Raw census does not apply an early-None filter; nested/non-spell
consumers are outside scope. These are dependency counts, not affected casts.

The33 references split **28 Spell4 AoE-preferred-target /five Spell4Effects
caster-apply**. This supersedes the older note's claim that only Antidote
follow-ups reference type93. Example: Spell4 **36806** references row43043 as
AoE-preferred-target; effect **124062** references row23731 as caster-apply.
Both prerequisites ask Equal/ObjectId38/Value0. The column role does not
change the native primary argument. Query and rows are in ignored `anchors.tsv`;
`census.py`, `ranking.json`, `slots.json`, `references.json` retain the full scan.

Independent owning GameTable/TextTable readback gives shipped type93 ->
localized error **158824 -> Spell conditions not met**. The generic text does
not itself identify a class or an effect group. Reused payloads under the
ignored type86 extraction directory have SHA256
`9ddd928b6c423072a1e34d6797003297ab8f75bdbca6f906227ca557f5ea3a32`
(PrerequisiteType.tbl) and
`588e385a0e8dee8b92dc79018070a17e78d3efd535b948224702effecafa5cf7`
(en-US.bin). No raw payloads or native bodies are tracked.

## Source regression and verification

**Observed red before production edit:** after fixing a test-fixture DI
registration compile error, the focused scope runs **31 cases:12 failed /
19 passed /zero skipped**. There are26 new cases: eight opposite-primary/
relationship manager checks with Value0/uint.MaxValue, four null-primary
controls, two null-unit controls, two absent-parameters checks, six unsupported
comparison modes and four generic full-ObjectId/any-entry controls. Twelve new
cases fail;14 new controls and five existing class tests pass. Production
target-substitution had not been changed before this red result. The old
five-case class theory retains all assertions; only its stale evidence comment
is corrected. Actual Source Player/UnitEntity records and both real manager
APIs are used, with explicit target-as-primary and relationship identity controls.
Fixture records show Source behavior, not retail list admission/lifetime.

**Observed final verification:**

- `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown|FullyQualifiedName~Movement|FullyQualifiedName~UnitStateFlagPacketTests"`
  passes **3,263 /zero failed /zero skipped**, including all26 new cases.
  Existing Aspire ASPIRE010, five MySqlProviderQualificationTests CS8632 and
  one ReceivedPropertyEffectTests xUnit2031 warning remain. Initial fixture
  compile error is preserved in `fixture-build.log`; it is not counted as red.
- Owning `dotnet build Source/NexusForever.Game/NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`
  passes with **zero warnings/errors**. Logs remain ignored in the slice folder.
- Four focused labels are applied and re-exported in read-only Ghidra:
  actual inline primary-class case, Lua GetClass, existing runtime builder and
  existing Spell4Base getter. Applied4/created0/localLabels1/missing0/
  duplicateRows0. `label-symbols.txt` confirms the exact inline name, while
  `labelled.txt` confirms the function names/bodies. Tracked labels change
  only the rejected padding replacement and two comments. The project changes
  are discarded; default export/cache/manifest are not refreshed.
- Inventory replaces the bad helper record with the inline-case record and
  updates GetClass and builder; total **6,211 function records** is unchanged.
  **8,350 mapping rows across seven files validate** with
  `Decomp/Analysis/Validate-DecompileMappingArtifacts.ps1`. Unrelated records
  and label-row numbering are preserved. Stale cached labels are superseded
  by the durable CSV and this finding.
- `python artifacts/prerequisite-type93/verify_slice.py` and `git diff --check`
  check native identity, indexed/Lua/descriptor bindings, shipped payload hashes,
  table readback, census/anchors, Source boundary, test/build results, scoped
  metadata, coverage and tracker links. Raw payloads/native output stay ignored.
- Full suite is not repeated; its preceding non-green result remains in the
  handoff. No deployment, provider/socket/native execution or retail acceptance.
- During this pass the branch advanced to **70650da8**, a concurrent CLAUDE.md
  skill-repository link update. **Observed diff:** CLAUDE.md only, with no Source,
  test or evidence changes; inspected and preserved. This slice commits on that
  parent without repeating unaffected tests. Investigation started at450be0b7.

End state: **implemented with Source verification; Verified native primary/null/
ObjectId/class/comparison and receiving-list binding; retained list coverage
inferred**. Type102 remains **Blocked** on the missing attributable retail
untimed-Fluff lifecycle trace. Next ranked wrapper candidate is **type196 Racial**
(20 direct/weighted references;40 slots/36 rows), requiring its own indexed
dispatcher/body/caller read. Type76 remains a shared native stub. Native client/
CUA remain stopped. No retail parity claim.
