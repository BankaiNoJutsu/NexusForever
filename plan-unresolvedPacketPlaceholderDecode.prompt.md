## Plan: Evidence-Backed Opcode Naming And Structural Decode Pass

Continue the recent opcode work with two linked goals: replace placeholder packet names wherever the evidence can honestly support something better than `Client0x####`, `Server0x####`, `Unknown*`, or `Data*`, and tighten the structural decode of still-blocked packets so the remaining placeholder surface is at least accurate, searchable, and ready for the next naming pass. The target outcome is both a smaller placeholder surface in `GameMessageOpcode`, packet models, handlers, and tests, and stronger structural packet models for the opcodes that still lack a defendable real name. Use the strongest name the repo can defend today: true packet semantics when available, durable structural source names when the source role is clear, and improved structural decode with an explicit blocker when a numeric placeholder still has to remain.

**Steps**
1. Reconfirm the active placeholder surface before editing. Read `i:\GIT\NexusForever\Source\NexusForever.Network\Message\GameMessageOpcode.cs`, `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\ClientUnresolvedDiagnosticPackets.cs`, `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\ServerUnresolvedOutputPackets.cs`, `i:\GIT\NexusForever\Source\NexusForever.WorldServer\Network\Message\Handler\Misc\ClientUnresolvedDiagnosticHandlers.cs`, `i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Network\ClientDiagnosticPacketShapeTests.cs`, and `i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Network\PacketPlaceholderNamingTests.cs`. Start by inventorying which `Client0x*`, `Server0x*`, `Unknown*`, and `Data*` names still exist and which ones already have enough evidence to rename.
2. Treat the current strongest recovered evidence as fixed starting points unless contradicted by a better witness:
   - `0x0142` writer `14007e6d0` writes `ulong + uint14 + bool`.
   - `0x00ED` writer `1400a6200` writes `ulong + uint32 + ulong + three bools`.
   - `0x07B6` writer `140080220` writes five `uint32` fields plus a counted row list.
   - `14007ff70` writes each row as `4-bit field + bool + byte + wide string`.
3. Run each placeholder opcode through two coordinated lanes in the same pass. First, attempt a real rename by gathering at least two independent witnesses: direct registration or reader/writer shape, caller or consumer context, adjacent packet family alignment, runtime sender or handler behavior, existing `INITIAL_FINDINGS.md` notes, or a corroborating external source already present in the repo. Second, if the rename still fails the evidence bar, land the best structural decode and comments you can defend so the packet is no longer just an opaque blob while it remains blocked.
4. Rename packet surfaces when the evidence is good enough. Update `GameMessageOpcode`, the packet model class, handler types, tests, comments, and any nearby docs together so the placeholder name disappears as a unit rather than leaving mixed old and new surfaces behind.
5. For opcodes that remain blocked, still land structural progress in the same slice. Convert raw byte-array payloads into accurate field layouts, structured rows, counts, and text fields where the wire shape is proven; update diagnostic handlers and packet-shape tests so the remaining placeholder is at least carrying the best current structure.
6. Prefer the strongest honest real name available. Use this priority order:
   - true gameplay or UI semantic name when behavior is actually established;
   - durable structural source name when the source role is clear but deeper gameplay meaning is still blocked;
   - numeric placeholder only when no stronger name survives the evidence check.
7. Use `0x0142`, `0x00ED`, and `0x07B6` as the concrete proving ground for the dual-track approach. Keep pushing on nearby callers, labels, and adjacent families for a real packet-level name, but if the name still blocks, land the structural model, handler logging, and shape tests anyway instead of leaving the opcode opaque.
8. Avoid placeholder-style field naming drift while doing the packet work. Do not introduce `Data0`, `Data1`, `Flag0`, or fresh `Unknown*` fields as a convenience layer. When field semantics are blocked, use the repo’s established structural fallbacks like `Value`, `Value0`, `Value1`, `Text`, `Rows`, `Count`, `Reserved`, or `Unused`, but only after the packet-level naming decision is settled.
9. Add durable native labels in `function_labels.csv` and supporting notes in `INITIAL_FINDINGS.md` for every rename attempt and every structural decode improvement, including failed rename attempts. If an opcode stays blocked, record the exact blocker so the next pass is aiming at the missing witness instead of re-reading the same ground.
10. Keep the diagnostic handlers and logging path honest. Structured logging is a valid outcome for blocked packets, but do not let a field-layout refactor crowd out rename wins that are already ready elsewhere in the same family.

**Relevant files**
- `i:\GIT\NexusForever\Source\NexusForever.Network\Message\GameMessageOpcode.cs`
- `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\ClientUnresolvedDiagnosticPackets.cs`
- `i:\GIT\NexusForever\Source\NexusForever.WorldServer\Network\Message\Handler\Misc\ClientUnresolvedDiagnosticHandlers.cs`
- `i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Network\ClientDiagnosticPacketShapeTests.cs`
- `i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Network\PacketPlaceholderNamingTests.cs`
- `i:\GIT\NexusForever\Decomp\Analysis\function_labels.csv`
- `i:\GIT\NexusForever\Decomp\Analysis\INITIAL_FINDINGS.md`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\selected_decompiled_cache\functions\sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5\14007e6d0.fragment.c`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\selected_decompiled_cache\functions\sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5\1400a6200.fragment.c`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\selected_decompiled_cache\functions\sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5\140080220.fragment.c`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\selected_decompiled_cache\functions\sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5\14007ff70.fragment.c`
- `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\ServerSpellFourUInt32.cs`
- `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\ServerMatching0x05CF.cs`
- `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\ServerUnresolvedOutputPackets.cs`

**Verification**
1. Run `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~ClientDiagnosticPacketShapeTests|FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo` after each rename or structural-decode slice lands.
2. Confirm the touched opcode either no longer appears under its placeholder form in `GameMessageOpcode`, the owning packet model, and the associated handler or test surface, or now has a stronger structural model plus an explicit recorded blocker explaining why the placeholder name remains.
3. For structurally decoded packets, re-read the same decompile cache fragments and confirm the managed read order and bit widths still match the native write order after the rename.
4. Keep runtime behavior diagnostic-only unless the same evidence pass also proves a safe non-diagnostic handler change.
5. If a candidate rename cannot clear the evidence bar, leave the placeholder in place and record the blocker rather than forcing a speculative real name.

**Decisions**
- The completion boundary is both a smaller placeholder opcode surface and stronger structural coverage for the packets that remain blocked.
- Prefer real packet names over neutral packet names whenever the evidence honestly supports them.
- Structural source names are acceptable intermediate wins when they remove `Client0x*` or `Server0x*` without overclaiming semantics.
- Keep placeholder names only for opcodes that are still explicitly blocked after a direct evidence pass, and require those survivors to carry the best current structural decode rather than opaque raw payloads.

**Further Considerations**
1. If the best current name for a packet is still only a family-level structural role, use that instead of accepting another raw numeric placeholder. `ServerMatchingManagerFlag` is the pattern to emulate, not `Server0x05B0`.
2. If the cache or selected export still misses `0x003D`, `0x0760`, or `0x0762`, expand focused export coverage or run a targeted cache search before accepting that those packets must remain placeholder-named.