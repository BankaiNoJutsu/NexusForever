# Type 102: receiving-unit discovery and untimed state (2026-11-14)

Starting branch/head: feature/spell-effects-remaining / 983f8934. Lead re-read
of build-16042 native receive/remove bodies with Ghidra 12.1.4 and the original
executable bound through REA to SHA256
231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5.
Evidence tier: Verified native target-list identity and flag-2 lifetime;
Source authoritative state admission/removal is examined independently.
No retail parity claim. Static/headless; no native execution or GUI.

## Question and discrepancies, before edits

How does a receiving unit discover its effects without walking the caster's
pending castings, and what installs/retires an untimed instance? Current lookup
only walks its own pending castings. The preceding target filter removed false
positives but did not create a receiving-unit path. A finite lifetimeEvents entry
cannot define registration: the native flag-2 node is registered without it.

Candidate first slice: property modifiers, conversions and personal-healing
modifiers already retain EffectEntryId in their receiving unit's SpellPropertyState.
All three handlers pass info.Entry.Id to the existing accepted state. Rejected
handlers install nothing. The state's existing exact-instance, whole-spell,
dispel and force-removal paths provide retirement without a new timer/registry.
Query that authoritative identity and prevent stale pending-spell fallback from
resurrecting removed property effects. Tests must run red first and cover actual
handlers, receiver/caster isolation, rejected shapes, explicit removal, stale
expiry and existing finite timing. Do not alter property admission/stacking/timers.

Candidate second slice: native Fluff uses the generic effect node but its Source
handler is empty. A typed no-gameplay marker may be justified only after the
native receive/untimed/removal chain and Source removal ownership are recovered.
No guessed generic registry or permanent instantaneous marker is authorized by
this note.

## Reach (current read-only imported census)

Type 102: 353 slots / 303 rows / 302 direct spell columns / 352 weighted
references (prior full column census; dependency reach, not affected casts).
Current join: 266 slots resolve to 226 shipped effect rows. Fluff (17): 197
slots / 164 IDs, 49 flag-2 slots; UnitPropertyModifier (11): 11 slots / 11 IDs,
one flag-2 slot. Other effect kinds retain their own evidence gates. Counts
and native/source verification will be recorded per completed slice.

## Property-state slice (Implemented / Source)

The receiving unit's accepted `SpellPropertyState.EffectEntryId` is now queried
before the legacy pending-spell fallback. `HandleEffectPropertyModifier`,
`HandleEffectUnitPropertyConversion` and `HandleEffectPersonalDmgHealMod` already
write the effect row ID on that receiver. The pending-spell helper excludes
these three producer families so an old timer cannot resurrect explicitly
removed state. No admission, interpreter, stacking or timer rule changed.

Thirteen new Source cases exercise actual producers, receiver/caster/reused-guid
isolation, finite and untimed state, exact removal, finite expiry, personal
replacement ownership, whole-spell removal and rejected shapes. Final red before
production: **11 failed / two passed**. An initial negative fixture changed its
row after the effect's interpretation had been cached; reconstructing that
fixture before dispatch corrected the test setup before the final red run.
No existing expectation was rewritten.

Rebuilt Prerequisite/Spell/Cooldown gate: **3,014 passed / zero failed/skipped**
with `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj
--no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1
--filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown"`.
Owning Game build with `--no-restore -v minimal --nologo
-p:UseSharedCompilation=false -m:1`: **zero warnings/errors**. Full suite is
deferred until the coherent receiving/marker slices finish; the preceding full
suite remains non-green. No retail parity claim.

## Native receive/removal follow-through

Original dispatcher slots at `1403f1344 + (opcode - 0x61) * 4` resolve 0x07f4
to `1403ee1c9`, 0x0813 to `1403ee349`, 0x0818 to `1403ee403`, and 0x0819 to
`1403ee3af`. The known 0x0818/0x0819 cases corroborate the mechanical table read.
0x07f4 goes through wrapper-id lookup to `14053e5a0`, which resolves each target
record's UnitId and calls `14053d680`. That function finds/creates a wrapper-local
record for that receiving entity, then `140717990` / `1405d6880` attach children
to the receiver's +0x15d0 list. The receiver need not own the casting.

0x0813 reads wrapper ID first and receiver ID second, then calls `140717df0`
with current count minus one. The shared two-UInt32 reader `14007a040` confirms
the wire order. This matches Source `ServerSpellBuffRemove` (0x0813); no packet
layout change is needed. It is distinct from 0x0819's reversed field roles and
shape-10 prune. `140717df0` changes the displayed buff count, and
`1404694a0` / `140717fa0` release displayed buff records. Those bodies alone do
not prove effect-node destruction; the initial count-zero destruction reading
is rejected pending the later cleanup path. No untimed marker is implemented
from that reading.

Ignored outputs under `artifacts/prerequisite-type102-receiving/` include
`native-receive-remove.txt`, `receive-message.txt`, `count-retirement.txt`,
`remove-empty-and-refresh.txt`, `remove-reader.txt`, `opcode-dispatch-slots.txt`,
`native-go.txt`, `native-0813.txt`, `native-0818-0819.txt`,
`reference-kind-census.tsv`, `property-red-final.log`, `property-green.log`,
`property-build.log` and matching headless logs. Native element binding and
flag-2 timer suppression remain as in the prior node-lifetime note; remaining
retirement and presentation-to-authoritative-state gates are explicit.
