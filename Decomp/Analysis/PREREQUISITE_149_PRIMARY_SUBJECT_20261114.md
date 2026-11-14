# Type 149: primary-subject State and field writers (2026-11-14)

Native read: lead re-derived with LLVM21.1.8 and Ghidra12.1.4 headless, build16042;
Verified native default predicate and named writer/reader bindings, Source checks below.

Starting branch/head: `feature/spell-effects-remaining` / `e5b31c4e`.
Question: does State retain the primary unit, and do the native writers establish
the meanings and input contracts of its `+0x1e4` and `+0x2ac` fields?

Before edits: **Mapped native selector/predicate**, case `0x95` calls virtual
`+0x88` / `14049c840` on primary unit and compares normalized OR of the two
nonzero fields with full Value for Equal/NotEqual only. Source substitutes Target
in the player wrapper; explicit-unit checks retain subject. Source maps fields
to IsBusy/InCombat. The old in-use writer note is agent-written and not re-derived;
the prior combat getter binding does not itself recover its writers. Existing
tests/comments are not sufficient to promote either producer path.

**Observed earlier reach:** 46 slots / 46 rows / 36 weighted spell references;
recheck census and anchors. Type 102 remains **Blocked** on the missing
attributable retail untimed-Fluff lifecycle trace, per user direction.

Candidate files: type-149 check, focused context/state/packet tests as warranted,
this finding, coverage, labels if a new mapping is applied, ledger, handoff and
spell tracker. Verification: native identity, indexed dispatch, predicate, in-use
reader/consumer/registration and combat-writer chain; Source packet producers;
shipped type/text and read-only reference census. New cases red before production
edit; owning Game build; relevant Prerequisite/Spell/Cooldown/Movement and state
packet tests; `git diff --check`. Static/headless only; no client launch, Computer
Use, runtime data or deployment. No retail parity claim.

## Verified native: primary predicate and field identities

Lead re-derived from build16042 `I:/WildStar/Client64/WildStar64.exe`, SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
LLVM21.1.8 direct disassembly, indexed-table byte reads and Ghidra12.1.4 focused
headless exports were used. Cached selector witness:
`Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/1404a2100.fragment.c`.
The old agent-written writer note is superseded for type149 by this read;
type9 is not reviewed or promoted by this pass.

**Verified native default-manager selector:** the actual indexed entry at
`1404a4848+(149-1)*4` resolves to **1404a2505**. Value enters R9D at1404a2508,
comparison R8D at1404a250c, primary unit RDX at1404a2510, and the call at
1404a2516 uses manager virtual+0x88. Slot **140b677c8**, from default vtable
140b67740, points to **14049c840**. ObjectId and relationship unit are unread.
An alternate table140b67e20 has +0x88 -> **1407db510**, the existing `_purecall`
label: direct bytes show the failure/termination path, not a second State rule.
No behavior is inferred for an abstract/alternate manager.

**Verified native predicate:** 14049c840..14049c875 normalizes nonzero
`unit+0x1e4 OR unit+0x2ac` to integer0/1. Comparison1 tests full Value equality;
comparison2 tests inequality; other modes fail. It does not reinterpret any
nonzero Value as true. There is **no local null guard** in this helper. Source's
existing null rejection is retained as its API contract, not claimed as native
null behavior.

**Verified native in-use writer and packet binding:** original-server dispatcher
`1403f1344+(opcode-0x61)*4` binds **0x0755 ->1403ee6ac**; the call at1403ee6b2
reaches **140641640**. It resolves packet UInt32 UnitId via1403d90d0, reads the
decoded flag at packet+4 (140641658), compares the old entity+1e4 and stores on
change at **14064166a**, then marks +18a8 dirty. A true flag can close a matching
open dialog through1405548f0. That side effect corroborates an in-use state;
it does not recover original-server busy admissions or timing.

Registration **14006e2f0..14006e31c** binds reader **14007c340**, decoded size8,
opcode755. The reader sets width32 at14007c365 and reads payload+0 via14006c090
at14007c36e; it then selects payload+4 at14007c377, width1 at14007c37b and calls
the same primitive at14007c384. The wire input is **UInt32 then one bit**;
decoded structure size8 does not mean eight wire bytes.

**Verified native combat writer and independent getter:** dispatcher **0x089A
->1403ed716** resolves packet UnitId at1403ed71b, reads packet+4 at1403ed728,
then calls **14045c500** at1403ed72e. The setter reads +2ac at14045c515, skips
unchanged values and stores EDX at **14045c541**, before its side effects.
Registration **140074e75..140074ea1** binds the same reader14007c340, size8,
opcode89A. Native Lua method table140c5a770 entry **140c5b1d0** independently
names **IsInCombat ->140653ed0**; its compare at140653f01 reads +2ac and setne
at140653f13 produces the Lua boolean. This confirms combat field meaning,
without qualifying all native combat transitions, animations or server policy.

**Verified native initialization; bounded writer search:** constructor
**140454380**, vtable140b66fd0, zeros ESI at14045438f; store **1404545d9** clears
+1e4, and qword store **14045462d** clears +2ac and the adjacent word. No meaning
is assigned to that adjacent field. The whole-text explicit-displacement scan
found **211 instruction hits /47 syntactic write candidates** for1e4/2ac. The
entity-region direct writes observed are the two constructor stores and the
two packet/setter stores above. Offsets also occur in other objects, stack
locations and vtable calls. Alias, computed-address and bulk/vector writes are
not exhaustively traced: an exhaustive "only writers" claim remains unverified.

## Source binding and hypotheses

**Verified native input contract / Source correlation:**
`Source/NexusForever.Network.World/Message/Model/ServerUnitInUse.cs` sends UInt32
UnitId then bool InUse; `ServerUnitEnteredCombat.cs` sends UnitId then InCombat.
`WorldEntity.IsBusy` is `busyStates.Count !=0`; its
`BroadcastBusyStateIfChanged` sends that value as InUse. `UnitEntity.InCombat`
stores its own flag and emits ServerUnitEnteredCombat. Those producer models
match the native reader/consumer/getter contracts. Their original-server
authority, initial/replay delivery, scheduling and full lifetime are **unknown**.
No producer, packet model, animation, timer or wire behavior changes here.

Competing readings and their disposition:

- **Rejected:** relationship Target replaces the primary for type149. The
  native dispatcher explicitly passes primary; caster/target column names
  describe caller context and cannot override that argument.
- **Verified native default-manager rule:** integer OR-normalization of in-use
  and combat followed by full Value equality/inequality. Separate native packet
  readers/writers and Lua getter support field identities; existing tests alone
  were not used to establish them.
- **Unverified:** these are the only possible writers, or Source busy/combat
  production has retail timing. Distinguish these with complete alias/lifetime
  tracing and attributable original-server transition captures. No special
  cases are added to fill those gaps.

## Observed imported and shipped witnesses

Fresh read-only `wildstar_client` census covers all three raw prerequisite slots
and all13 prerequisite columns in spell4/spell4effects: **46 slots /46 rows /
36 direct /36 weighted spell references**. No early-None filter is applied;
nested and non-spell consumers are outside scope. This is dependency reach,
not a count of affected casts. Slots split Equal0=30, Equal1=13, NotEqual1=2,
NotEqual0=1; slot indices0/1/2 count31/12/3. Observed ObjectIds are0,567,6175,
203605; native type149 does not read them, and no new meaning is assigned.

Read-only anchors: target-cast Spell4 **34198 -> prerequisite10739** and
caster-cast Spell4 **53778 -> prerequisite9670**. Both rows are flags0,
type149/Equal/Value0 with later slots empty. Column counts: target-cast5,
caster-cast1, target-apply28, caster-apply2. `anchors.tsv`, `slots.json`,
`references.json`, `ranking.json` and `census.py` stay ignored under
`artifacts/prerequisite-type149/`.

Imported type149 points to localized error218077. Existing owning GameTable /
TextTable readers independently read extracted PrerequisiteType.tbl and
en-US.bin: **149 ->218077 -> State requirement not met**. Payload SHA256:
`9ddd928b6c423072a1e34d6797003297ab8f75bdbca6f906227ca557f5ea3a32`
and `588e385a0e8dee8b92dc79018070a17e78d3efd535b948224702effecafa5cf7`,
respectively, retained in the ignored type86 extraction folder. This names the
type; it does not define state lifetime. Probe output is in
`artifacts/prerequisite-type149/type-text-probe.log`.

## Implemented / Source and verification

The sole production behavior change is the player wrapper delegating with
**player**, retaining the existing explicit-unit path. The OR predicate,
comparison, null guard, producers and packet models remain unchanged. The old
target-first test is rewritten to require primary retention and retains an
explicit target-as-primary control.

**Observed red before production edit:** focused State/context/packet scope
ran **44 cases:21 failed /23 passed /zero skipped**. There are37 new cases:
23 context/null cases, ten integer-Value/raw-comparison cases and four golden
packet payload cases. Twenty-one new cases fail on the old wrapper;16 new
controls and seven old controls pass. Opposite state inputs exercise both real
manager APIs, explicit target-as-subject and unchanged relationship identity.
Direct null controls document Source safety only. Both packets serialize the
mapped UInt32 and one-bit flag for true/false; payloads and Source reader
round-trips are checked, not network runtime or original-server emission.

**Observed final verification:**

- `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown|FullyQualifiedName~Movement|FullyQualifiedName~UnitStateFlagPacketTests"`
  passes **3,237 /zero failed /zero skipped**. Red/green logs are ignored.
  Existing Aspire ASPIRE010, five test CS8632 and one xUnit2031 warning remain;
  this slice does not claim to fix them.
- `dotnet build Source/NexusForever.Game/NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`
  passes with **zero warnings/errors** (`owning-build.log`).
- Three durable labels are applied and re-exported in a **read-only** Ghidra
  session: `Prerequisite_CheckPrimaryState`14049c840,
  `Entity_ApplyUnitInUsePacket`140641640, `Entity_SetInCombatFlag`14045c500.
  Applied3/created0/missing0/duplicateRows0; focused `labelled.txt` confirms names
  and bodies. Changes are discarded from the read-only project; tracked CSV is
  the durable label source. Default export/cache/manifest are not refreshed.
  Old cached ownership/no-reference comments are superseded by this note.
- Mapping inventory changes only those three records; **8,350 mapping rows
  across seven files validate**. The old149 row is updated and two writer rows
  added. Native/hash/reader/Lua binding and coverage/diff checks are recorded
  in the ignored slice directory: `python artifacts/prerequisite-type149/verify_slice.py`
  and `git diff --check` pass. Raw decompilation and payloads are not tracked.
- Full suite is not repeated; its preceding non-green result remains in the
  handoff. No deployment, provider/socket/native execution or retail acceptance.

End state: **implemented with Source verification; Verified native default
selector/predicate and named field/reader/writer bindings**. Full writer coverage
and original-server scheduling remain unqualified. Type102 remains **Blocked**
on the missing attributable untimed-Fluff retail lifecycle trace. Next ranked
native-backed wrapper candidate is type93 (33 weighted references), requiring
its own dispatcher/body/caller read; type76 still has only a shared native stub.
No retail parity claim.
