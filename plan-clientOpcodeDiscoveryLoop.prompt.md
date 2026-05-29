## Plan: Client Opcode Discovery Loop

Run the next reverse-engineering window as a repeatable client-driven opcode loop instead of a one-off naming pass. Each cycle should decompile the WildStar client, uncover new opcode registrations and stronger real names, map the packet and runtime surfaces that become understandable from that evidence, implement the narrow server-side work that is now genuinely unblocked, record the remaining blockers, and then immediately start the next cycle. The point is not just to collect more unknown opcodes. The point is to steadily shrink the placeholder surface in `GameMessageOpcode`, packet models, handlers, diagnostics, and adjacent gameplay abstractions while converting verified findings into durable names, tests, and conservative runtime behavior. Use the repo’s current naming bar as the precedent: `ServerCinematic022B` becoming `ServerCinematicDelayFlag`, and `HasUnknown0200` becoming `DisablesDynamicProgress` across the quest-objective abstractions.

**Steps**
1. Start each cycle by inventorying the current unresolved opcode surface. Read `i:\GIT\NexusForever\Source\NexusForever.Network\Message\GameMessageOpcode.cs`, `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\ClientUnresolvedDiagnosticPackets.cs`, `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\ServerUnresolvedOutputPackets.cs`, and `i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Network\PacketPlaceholderNamingTests.cs`. Group the remaining `Client0x*`, `Server0x*`, `Unknown*`, and `Data*` surfaces by family so each cycle has one bounded, high-yield target slice.
2. Run a focused client decompile and export pass for the chosen slice. Center the loop on `i:\GIT\NexusForever\Decomp\Analysis\run_ghidra_analysis.ps1`, `i:\GIT\NexusForever\Decomp\Analysis\scripts\ExportNexusForeverAnalysis.java`, the current label set in `i:\GIT\NexusForever\Decomp\Analysis\function_labels.csv`, and the focused export artifacts under `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe`. Use `WildStar64.exe` as the primary client witness, widen selection only when the current cache and exporter cannot surface the needed readers, writers, callers, or string anchors.
3. Recover opcode evidence from the client before touching source names. For each opcode in the slice, collect the best available mix of witnesses: registration entries, read/write helper shape, caller or consumer context, adjacent opcode-family alignment, UI or string anchors, runtime server surface, existing findings, and any corroborating tracked external source already present in the repo. Prefer at least two independent witnesses before promoting a real name.
4. Rename and map what the evidence now supports. Update `GameMessageOpcode`, packet model classes, handler types, tests, comments, and any owning abstractions or enums together when an opcode or related client-driven surface can move from a numeric placeholder or `Unknown*` name to a real packet name or a durable structural source name. If the client evidence clarifies a gameplay-facing constant or interface flag, propagate that rename through the domain surface instead of leaving an older placeholder beside the newly named packet. If the opcode is still semantically blocked, land the strongest structural decode you can defend so the placeholder carries accurate fields, rows, counts, and text instead of opaque raw payload bytes.
5. Implement only what the new client evidence actually unblocks. Safe implementation work includes packet model cleanup, handler decode improvements, diagnostics, conservative runtime send or receive behavior, and narrow server logic that now has verified packet semantics. If the consumer is mapped but the producer intent is still blocked, keep the packet modeled and tested without inventing runtime emission.
6. Validate the implementation slice immediately. After any rename, structural decode, or runtime change, rerun focused tests, packet shape checks, and any narrow build or export verification needed for the touched family. The same cycle should prove both that the new name is defensible and that the mapped or implemented surface still matches the client wire format.
7. Record durable outcomes before moving on. Update `i:\GIT\NexusForever\Decomp\Analysis\INITIAL_FINDINGS.md`, `i:\GIT\NexusForever\Decomp\Analysis\function_labels.csv`, the relevant packet models and tests, and any focused tracker note that owns the family. When the evidence clarifies a broader gameplay surface, also update the owning abstraction or enum so the repo keeps one consistent real name instead of a renamed packet layered on top of an old placeholder flag or property. For every opcode that remains blocked, write down the exact missing witness so the next cycle aims at the real gap instead of redoing the same read.
8. Loop without resetting context. Once the current slice is exhausted, choose the next highest-yield family from the updated unresolved inventory and rerun the same flow: decompile client, recover names and structure, map or implement what is newly unblocked, record blockers, and continue. If a cycle stops yielding rename or implementation wins, adjust selection, widen export coverage, or switch families instead of forcing speculative names.

**Relevant files**
- `i:\GIT\NexusForever\Decomp\Analysis\run_ghidra_analysis.ps1`
- `i:\GIT\NexusForever\Decomp\Analysis\scripts\ExportNexusForeverAnalysis.java`
- `i:\GIT\NexusForever\Decomp\Analysis\function_labels.csv`
- `i:\GIT\NexusForever\Decomp\Analysis\INITIAL_FINDINGS.md`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\functions.csv`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\selected_decompiled.c`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\selected_xrefs.csv`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\selected_reasons_summary.csv`
- `i:\GIT\NexusForever\Source\NexusForever.Network\Message\GameMessageOpcode.cs`
- `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\ClientUnresolvedDiagnosticPackets.cs`
- `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\ServerUnresolvedOutputPackets.cs`
- `i:\GIT\NexusForever\Source\NexusForever.WorldServer\Network\Message\Handler`
- `i:\GIT\NexusForever\Source\NexusForever.Game.Abstract\Quest\IQuestObjectiveInfo.cs`
- `i:\GIT\NexusForever\Source\NexusForever.Game.Abstract\Achievement\IAchievement.cs`
- `i:\GIT\NexusForever\Source\NexusForever.Game.Static\Quest\QuestObjectiveFlags.cs`
- `i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Network\PacketPlaceholderNamingTests.cs`
- `i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Network\ClientDiagnosticPacketShapeTests.cs`
- `i:\GIT\NexusForever\plan-unresolvedPacketPlaceholderDecode.prompt.md`
- `i:\GIT\NexusForever\plan-decompileThroughputRoadmap.prompt.md`

**Verification**
1. After each label or selection change, rerun a focused client export and confirm the intended opcode-family readers, writers, or callers still appear in the export artifacts.
2. After each rename slice, confirm the touched opcode or related gameplay surface no longer survives only as `Client0x*`, `Server0x*`, `Unknown*`, or stale placeholder naming unless the blocker is explicitly recorded and the packet now carries the best current structural decode.
3. After each packet-model or handler change, run `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PacketPlaceholderNamingTests|FullyQualifiedName~ClientDiagnosticPacketShapeTests" -v minimal --nologo` and add narrower family-specific tests when the slice touches runtime behavior beyond diagnostics.
4. For any runtime implementation unlocked by the client pass, require at least two agreeing witnesses before enabling behavior: client decompile evidence plus packet, handler, fixture, or live runtime evidence.
5. At the end of each cycle, confirm the repo state improved in at least one of these ways: fewer placeholder opcode names, stronger structural packet models, new durable function labels, new verified tests, or new conservative runtime behavior. If none improved, adjust the next cycle instead of repeating the same slice unchanged.

**Decisions**
- The core unit of progress is one completed opcode cycle: decompile, recover, map, implement what is unblocked, document blockers, repeat.
- Prefer real packet names over numeric placeholders whenever the evidence honestly supports them.
- Propagate evidence-backed names through adjacent abstractions and flags when the same client finding clarifies them; do not stop at the packet enum if the repo still exposes a stale placeholder elsewhere.
- When semantics remain blocked, use structural source names or structural field layouts rather than opaque raw payload placeholders.
- Implementation is allowed only when the client evidence actually unblocks a safe server-side change; modeled-only is still the right outcome for blocked producer intent.
- Do not end the workflow on decompile notes alone if the same evidence clearly unlocks packet-model cleanup, handler work, tests, or conservative runtime code.

**Further Considerations**
1. If one cycle exposes multiple related opcodes in the same family, land them as a batch so naming, models, handlers, and tests stay aligned instead of fragmenting the family over multiple sessions.
2. If the loop repeatedly stalls because the right client functions are not entering the focused export, prioritize exporter-selection widening or cache-warming work before switching to a lower-yield opcode family.