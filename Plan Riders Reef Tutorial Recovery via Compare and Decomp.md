# Plan: Riders' Reef Tutorial Recovery via Compare and Decomp

## Goal
Unblock brand-new character progression in Riders' Reef on world `3460` by combining four evidence sources instead of guessing at isolated fixes:

- live runtime verification on `game_rework`
- explicit data mapping from game tables and DB rows
- targeted client decompilation and analysis artifacts
- selective reuse from `https://github.com/NexusForever/NexusForever/compare/game_rework...kirmmin:NexusForever:latest`

Success means a fresh character can enter Riders' Reef, receive the faction tutorial quests, advance the first movement and hoverboard objectives in order, complete the CSI steps, and continue past the current stuck point without manual GM intervention.

## Current Verified Facts
- A fresh character login on world `3460` still accepts tutorial quests `10513` and `10527`, but the world-server log shows no `ServerQuestObjectiveUpdate` after acceptance.
- The authoritative tutorial objectives in the shipped game tables are:
  - `10513` and `10521`: EnterArea chain on `51735 -> 51736 -> 51736 -> 51737 -> 51737`
  - `10527` and `10532`: EnterArea on `51703`, duplicate optional EnterArea on `51703`, `SucceedCSI` on creature `73419`, EnterArea on `51734`, and `SucceedCSI` on creature `73735`
- The server currently uses `WorldLocation2IdIndicator00..03` for EnterArea matching and does not load `QuestDirection` or `QuestDirectionEntry` in the normal `GameTableManager.Initialise()` path because those properties are missing `[GameData]` on [Source/NexusForever.GameTable/GameTableManager.cs](Source/NexusForever.GameTable/GameTableManager.cs).
- The current branch already has partial fixes for tutorial trigger geometry, replay-on-login, and optional-objective preference, but those fixes did not resolve the fresh-character stuck case.
- Existing repo memory already confirms that Riders' Reef restoration hinges on world-location triggers, CSI progression, and careful sequencing around `OnAddToMap`.

## Planning Principle
Treat this as a data-mapping and behavior-recovery problem first, and only then as a code-porting problem. The branch compare is useful if it reduces ambiguity or restores missing infrastructure, but it should not become a bulk merge.

## Subagent Plan

### Subagent 1: Runtime And Table Truth
Mission:
- verify exactly what a new character receives at login
- confirm which quest/objective rows are created and which never advance
- correlate logs, DB rows, and runtime code paths

Inputs:
- `.nexusforever-runtime/logs/NexusForever.WorldServer.stdout.log`
- `nexus_forever_character.character_quest`
- `nexus_forever_character.character_quest_objective`
- [Source/NexusForever.Script.Main/Tutorial/TutorialMapScript.cs](Source/NexusForever.Script.Main/Tutorial/TutorialMapScript.cs)
- [Source/NexusForever.Game/Entity/Trigger/WorldLocationVolumeGridTriggerEntity.cs](Source/NexusForever.Game/Entity/Trigger/WorldLocationVolumeGridTriggerEntity.cs)
- [Source/NexusForever.Game/Quest/Quest.cs](Source/NexusForever.Game/Quest/Quest.cs)

Output:
- one fresh-character progression timeline from login to stuck state
- one list of runtime events that should have happened but did not

### Subagent 2: Data Mapping Crosswalk
Mission:
- build a concrete crosswalk for tutorial progression so the server path, client guidance path, and extracted table data can be compared directly

Inputs:
- `Quest2.tbl`
- `QuestObjective.tbl`
- `WorldLocation2.tbl`
- `QuestDirection.tbl`
- `QuestDirectionEntry.tbl`
- `ClientSideInteraction.tbl`
- `Creature2.tbl`
- `TargetGroup.tbl`
- localized text bins if needed for objective text disambiguation

Output:
- one table per tutorial quest with these columns:
  - quest id
  - objective index
  - objective id
  - objective type
  - flags
  - `data`
  - `WorldLocation2IdIndicator00..03`
  - resolved `QuestDirectionEntry` active and inactive world locations, if `data` points there
  - CSI creature id or target group, if applicable
  - localized objective text

### Subagent 3: Decomp And Client Behavior Recovery
Mission:
- identify how the client evaluates or presents tutorial EnterArea and CSI steps, especially whether the player is being guided to a location that does not match the server trigger

Inputs:
- `Decomp/Analysis/exports/**`
- `Decomp/Analysis/function_labels.csv`
- `Decomp/Analysis/INITIAL_FINDINGS.md`
- existing tutorial and spell reverse-engineering notes in the repo root
- targeted decompilation around quest objective update flows, `QuestDirection`, and `ServerQuestObjectiveWorldLocation`

Output:
- evidence-backed notes on:
  - whether the client uses `QuestObjective.WorldLocation2IdIndicator*` or `QuestDirectionEntry.WorldLocation2Id`
  - whether the client expects `ServerQuestObjectiveWorldLocation` for these steps
  - whether CSI success is driven by entity interaction, spell activation, or a different event path

### Subagent 4: Compare Branch Reuse
Mission:
- mine `game_rework...kirmmin:latest` for reusable pieces that help tutorial recovery without dragging in unrelated systems

Inputs:
- compare URL
- non-destructive `git fetch <url> latest`
- `git diff --stat game_rework..FETCH_HEAD`
- focused file diffs for quest, cinematic, script, interaction, and objective paths

Output:
- direct cherry-pick candidates
- manual-port candidates
- ignore-for-now areas

### Subagent 5: Validation Harness
Mission:
- define the minimum reproducible test loop for each change so tutorial fixes stop being guess-and-retest work

Inputs:
- local launcher scripts
- world-server logs
- DB objective rows
- optional diagnostic helpers under `artifacts/verify`

Output:
- one deterministic test checklist for new-character verification

## Data Mapping Workstream
Build the tutorial progression crosswalk before changing more behavior.

### Mandatory Mapping Set
- `Quest2`: `10513`, `10521`, `10527`, `10532`
- `QuestObjective`: `21271`, `21279`, `21280`, `21272`, `21289`, `21300`, `21301`, `21302`, `21303`, `21304`, `21321`, `21322`, `21323`, `21324`, `21325`, `21352`, `21353`, `21354`, `21355`, `21356`
- `WorldLocation2`: `51735`, `51736`, `51737`, `51703`, `51734`, `51738`
- CSI creatures: `73419`, `73735`
- `QuestDirectionEntry` rows referenced by tutorial objective `data` fields such as `8539`, `8540`, `8542`, `8543`, `8560`

### Mapping Questions To Answer First
1. Does the client-visible target location for the first stuck step come from `WorldLocation2IdIndicator00` or from `QuestDirectionEntry.WorldLocation2Id`?
2. Are the first tutorial markers using a server radius of `1` while the client displays a larger circle?
3. Is the server missing `ServerQuestObjectiveWorldLocation` messages for these objectives?
4. Does the CSI path require a resolved `ClientSideInteraction` or spell activation record that the current handler never touches?

### Likely Implementation Follow-Ups
- Add `[GameData]` to `QuestDirection` and `QuestDirectionEntry` in [Source/NexusForever.GameTable/GameTableManager.cs](Source/NexusForever.GameTable/GameTableManager.cs) if runtime or diagnostics need those tables loaded normally.
- Extend the existing table inspector under `artifacts/verify` so it can print the full tutorial crosswalk in one command.
- Save the resulting mapping as a durable markdown or CSV artifact under `artifacts/verify` for future zone restoration.

## Decomp Workstream
Use decompilation to remove ambiguity, not to drive speculative rewrites.

### Priority Targets
1. Quest objective guidance resolution for EnterArea objectives
2. `ServerQuestObjectiveWorldLocation` usage on the client
3. Client-side interaction completion path for tutorial CSI creatures
4. Any tutorial-specific objective update or novice cinematic handoff code

### Existing Assets To Reuse
- `Decomp/Analysis/function_labels.csv`
- `Decomp/Analysis/exports/Houston64.exe/**`
- `Decomp/Analysis/exports/WildStar64.exe/**`
- `Spell Effect Evidence Matrix.md`
- `Spell Runtime Restoration Plan.md`
- `Global Spell System Reverse Engineering.md`

### Deliverable
Produce a short evidence matrix that ties each tutorial objective type to:
- observed client behavior
- relevant packet or game-table inputs
- current server implementation path
- confirmed mismatch or remaining unknown

## Compare Branch Reuse Assessment
Use the compare branch as a parts bin, not a base branch.

### Direct Cherry-Pick Candidates
- script assembly dependency-resolution fixes if the target commits are isolated
- small cinematic or map-script infrastructure commits that match current `game_rework` interfaces
- compact quest objective handling improvements that do not drag in spell or inventory systems

### Manual-Port Candidates
- map-script patterns from Everstar Grove, Crimson Isle, or other novice-zone flows
- objective-update routing for `ActivateEntity`, `SucceedCSI`, `TalkTo`, and `TalkToTargetGroup`
- optional-objective completion logic and checklist-style quest handling where interfaces have diverged
- tutorial-entry cinematic queuing patterns

### Mine For Ideas Only
- large spell-system refactors
- inventory, binding, and reward-track systems
- broad entity or movement rewrites unrelated to tutorial progression
- anything that requires a mass merge into current `game_rework`

### Compare Decision Rule
Only port from `kirmmin:latest` if all of the following are true:
- the change isolates to tutorial, quest, cinematic, scripting, or interaction infrastructure
- the diff is understandable without bringing in unrelated systems
- it compiles cleanly against current `game_rework`
- it reduces ambiguity faster than a local focused fix

## Execution Order
1. Reproduce the fresh-character stuck case and capture DB rows plus the exact log window.
2. Finish the full tutorial data crosswalk, including `QuestDirectionEntry`.
3. Use decomp evidence to determine whether server guidance and server trigger locations disagree.
4. Inspect the compare branch for targeted reusable pieces in quest, cinematic, script, and interaction paths.
5. Implement the smallest code changes that align server behavior with the mapped data and decomp evidence.
6. Rebuild with `NexusForever.AuthServer` and `NexusForever.WorldServer` stopped.
7. Re-run the fresh-character validation loop.
8. Keep only fixes that survive a new-character test and a returning-character test.

## Candidate Code Areas
- [Source/NexusForever.Script.Main/Tutorial/TutorialMapScript.cs](Source/NexusForever.Script.Main/Tutorial/TutorialMapScript.cs)
- [Source/NexusForever.Game/Entity/Trigger/WorldLocationVolumeGridTriggerEntity.cs](Source/NexusForever.Game/Entity/Trigger/WorldLocationVolumeGridTriggerEntity.cs)
- [Source/NexusForever.Game/Quest/Quest.cs](Source/NexusForever.Game/Quest/Quest.cs)
- [Source/NexusForever.Game/Quest/QuestObjectiveInfo.cs](Source/NexusForever.Game/Quest/QuestObjectiveInfo.cs)
- [Source/NexusForever.WorldServer/Network/Message/Handler/Entity/ClientEntityInteractionHandler.cs](Source/NexusForever.WorldServer/Network/Message/Handler/Entity/ClientEntityInteractionHandler.cs)
- [Source/NexusForever.GameTable/GameTableManager.cs](Source/NexusForever.GameTable/GameTableManager.cs)
- [Source/NexusForever.Network.World/Message/Model/ServerQuestObjectiveWorldLocation.cs](Source/NexusForever.Network.World/Message/Model/ServerQuestObjectiveWorldLocation.cs)
- `artifacts/verify/QuestTableInspector`

## Risks
- The client may use `QuestDirectionEntry` world locations while the server currently keys only off `WorldLocation2IdIndicator*`.
- Tutorial world locations use very tight radii in the extracted tables, so a pure geometric fix may still be insufficient if the client UI is broader than the server trigger.
- The compare branch contains large unrelated systems; indiscriminate reuse would slow the tutorial recovery work.
- Missing `QuestDirection` loading in `GameTableManager` can hide the real guidance path during debugging.

## Validation Checklist
- Fresh Exile and fresh Dominion characters both receive the correct tutorial quests.
- First EnterArea objective advances on a brand-new character without GM help.
- Login recovery still works for a character already standing inside a tutorial area.
- CSI objectives for creatures `73419` and `73735` advance reliably.
- No duplicate or optional tutorial objective steals progression from the required visible step.
- World-server logs show the expected objective updates during the test run.
- DB objective rows match what the client shows after each step.

## Out Of Scope
- bulk merging `kirmmin:latest`
- broad spell-system restoration
- inventory or reward-track rewrites
- unrelated zone or expedition restoration work