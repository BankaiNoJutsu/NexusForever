# Starter Area Quest Guidance Evidence

## Scope

- Evidence source: local `QuestTableInspector` runs against `.nexusforever-runtime/assets/tbl` after extending the inspector to print quest-level receiver/completion guidance and objective `QuestDirectionId` expansion.
- Runtime cross-check: `Source/NexusForever.Network/Message/GameMessageOpcode.cs` and `Source/NexusForever.Network.World/Message/Model/ServerQuestObjectiveWorldLocation.cs`.
- Witness set: tutorial `10513`, `10521`, `10527`, `10532`; Crimson Isle `5573`, `5575`; Levian Bay `5868`; Everstar Grove `6302`; Celestion `6677`, `6986`.

## Main Findings

- The tutorial witness quests use direct objective `WorldLocation2` indicators and carry no receiver, alt-receiver, or completion quest-direction guidance at all.
- Early outdoor starter quests are mixed:
  - `5573` uses an explicit objective `QuestDirectionId` fan-out.
  - `5868` uses a quest-level completion direction.
  - `6302` and `6986` use only direct receiver and objective world locations.
- `6677` `Restoring the Balance` is the clearest starter witness for real quest-guidance logic because it combines direct `QuestDirectionEntry` references on `EnterArea` objectives with non-zero objective `QuestDirectionId` rows on its CSI steps.
- The server already defines `ServerQuestObjectiveWorldLocation`, but there is no current sender in `Source`, so the runtime gap is not just table interpretation. Packet emission is missing too.

## Witness Breakdown

### Tutorial: `10513`, `10521`, `10527`, `10532`

- All four witnesses have `receiverWl=0`, `altReceiverWl=[0,0,0]`, `altReceiverDir=[0,0,0]`, and `completionDir=0`.
- `10513` and `10521` rely on `EnterArea` objective indicators `51735`, `51736`, and `51737` in world `3460`, zone `6009`.
- `10527` and `10532` rely on indicator rows `51703`, `51734`, and `51738` across Rider's Reef zones `6009` and `6010`.
- The `EnterArea` data values used by those tutorial steps (`8539`, `8540`, `8542`, `8543`, `8560`) do not resolve as `QuestDirection` or `QuestDirectionEntry` rows in the loaded tables.

Practical implication:

- The tutorial guidance surface is currently direct objective markers plus world-location trigger logic. It is not blocked on quest-direction table reconstruction.

### Crimson Isle: `5573` and `5575`

- `5573` has `receiverWl=17803` and objective `8229` with `questDir=2451`.
- Direction `2451` expands to entries `4000`, `4001`, and `4002`, which resolve to active world locations `17816`, `17929`, and `17930`.
- Those same three locations already appear as the objective indicator world locations on `8229`.
- `5575` is the near-parallel witness: `receiverWl=17817`, same shield-generator indicator locations, but objective `8371` has `questDir=0`.

Practical implication:

- `5573` shows that starter quest guidance can be table-driven even when the visible objective already carries direct indicator markers.
- `5575` is a useful control case because the sibling quest keeps the same visible marker set without the extra quest-direction indirection.

### Levian Bay: `5868` `Enter the Station`

- `5868` has `receiverWl=19370` and `completionDir=151`.
- Completion direction `151` expands to entries `241` and `242`.
- Entry `241` resolves to world location `19370` in zone `1309`.
- Entry `242` resolves to world location `19318` in zone `1306`.
- Objective `9206` is an `EnterArea` step whose visible indicator is also `19318`.

Practical implication:

- `5868` is the cleanest low-level witness that quest completion guidance and objective indicator guidance can point at the same area through different table seams.

### Everstar Grove: `6302` `Waking Elderoot`

- `6302` has `receiverWl=21407`, `altReceiverWl=[21407,0,0]`, and `completionDir=0`.
- The CSI objective `9675` also uses indicator world location `21407`.
- No `QuestDirection` or `QuestDirectionEntry` rows surface for the witness.

Practical implication:

- `6302` is a direct-location starter witness, not a quest-direction witness.

### Celestion: `6677` `Restoring the Balance`

- `6677` has `receiverWl=23559` and no quest-level completion direction.
- Its `EnterArea` objectives use data values `3746`, `3747`, and `3748`, and those do resolve directly as `QuestDirectionEntry` rows:
  - `3746 -> wlActive 49150` in world `3094`, zone `4617`
  - `3747 -> wlActive 48938` in world `3094`, zone `4656`
  - `3748 -> wlActive 49186` in world `3094`, zone `4617`
- Its CSI objectives also carry non-zero objective directions:
  - Objective `10389` uses `questDir=96`, which expands to entries `140` and `139`, pairing CSI location `23705` with indicator location `23810`.
  - Objective `10390` uses `questDir=97`, which expands to entries `142` and `141`, pairing CSI location `23706` with indicator location `23811`.
  - Objective `10391` uses `questDir=98`, which expands to entries `144` and `143`, pairing CSI location `23707` with indicator location `23812`.

Practical implication:

- `6677` is the best starter-area witness for the missing guidance boundary because it exercises both direct `QuestDirectionEntry` resolution and multi-entry `QuestDirection` expansion on the same quest.

### Celestion: `6986` `Shoot for the Stars`

- `6986` has `receiverWl=25139` and no non-zero quest directions.
- Its CSI objective `12757` uses direct indicator world location `26694`.

Practical implication:

- `6986` is another direct-location control case, useful when testing whether runtime guidance code should stay quiet for quests without explicit quest-direction rows.

## Runtime Packet Boundary

- `GameMessageOpcode.ServerQuestObjectiveWorldLocation = 0x012C` is already defined.
- `ServerQuestObjectiveWorldLocation` serializes exactly three fields: `QuestId`, `QuestObjectiveIndex`, and `WorldLocation2Id`.
- A workspace search found no current sender or call site for that packet model outside the opcode definition and the model class itself.

Practical implication:

- Any retail-style starter guidance recovery will need both:
  - table-side resolution of direct indicator rows and quest-direction rows
  - runtime emission of `ServerQuestObjectiveWorldLocation`

## Utility Update

- `artifacts/verify/QuestTableInspector/Program.cs` now prints:
  - quest-level receiver and alt-receiver world locations
  - quest-level completion and alt-receiver direction expansion
  - objective-level `QuestDirectionId` expansion

That makes the utility reusable for future starter and non-starter guidance passes without falling back to schema-fragile SQL joins.

## Recommended Next Passes

1. Use `6677` and `5868` as the primary client decompile witnesses for quest-guidance and objective-world-location handling.
2. Use tutorial quests `10513`, `10521`, `10527`, and `10532` as negative-control witnesses where direct objective indicators should be sufficient and quest-direction tables should stay absent.
3. Trace the client handler for opcode `0x012C` and the code path that converts `QuestDirection` / `QuestDirectionEntry` into the displayed objective arrow or map marker.
4. If runtime work resumes after the client boundary is clearer, emit `ServerQuestObjectiveWorldLocation` conservatively only for objectives or quest states with explicit resolved world-location guidance.