# Rider's Reef Tutorial Quest Graph

## Scope

- Evidence source: `wildstar_client_mysql` Quest2, QuestObjective, Prerequisite, CommunicatorMessages, Creature2, QuestDirection, QuestDirectionEntry, and WorldLocation2 tables.
- Runtime cross-check: `Player`, `QuestManager`, `GlobalQuestManager`, `ClientActivateUnitHandler`, and `ClientActivateUnitCastHandler`.
- Decomp cross-check: generic quest dialog and communicator payload labels exist, but they do not add direct giver/receiver routing for this tutorial slice.

## Main Finding

The post-hoverboard tutorial is not a single short follow-up chain. The client tables describe two longer faction-specific chains:

Exile:

`10513 -> 10527 -> 10518 -> 10525 -> 10540 -> 10519 -> 10520 -> 10528`

Dominion:

`10521 -> 10532 -> 10524 -> 10526 -> 10541 -> 10522 -> 10523 -> 10530`

The unresolved seam is the handoff from the hoverboard quests to `10518` and `10524`.

## Current Runtime Gap

- `TutorialMapScript` and `Player` currently track only the opening four quests plus a partial follow-up subset.
- `QuestManager` only allows receiverless completion for `10513`, `10521`, `10527`, and `10532`.
- `10518` and `10524` have no direct creature giver, no direct creature receiver, and no communicator row with `QuestIdDelivered` set to them.
- Because of that, the transition from `10527` to `10518` and from `10532` to `10524` is still unresolved and not safe to script from table data alone.

## Quest Graph

### Opening Slice

- `10513` Exile opener.
  Giver: none in `Creature2`.
  Receiver: none in `Creature2`.
  Objectives: `21271`, `21279`, `21280`, `21272`, `21289`.
  World locations: `51735`, `51736`, `51737`.

- `10521` Dominion opener.
  Giver: none in `Creature2`.
  Receiver: none in `Creature2`.
  Objectives: `21300`, `21301`, `21302`, `21303`, `21304`.
  World locations: `51735`, `51736`, `51737`.

- `10527` Exile hoverboard.
  Giver: none in `Creature2`.
  Receiver: none in `Creature2`.
  Objectives: `21321`, `21322`, `21324`, `21323`, `21325`.
  Objective routing: `51703` start area, `73419` CSI projector, `51734` ride area, `73735` finish CSI.

- `10532` Dominion hoverboard.
  Giver: none in `Creature2`.
  Receiver: none in `Creature2`.
  Objectives: `21352`, `21353`, `21354`, `21355`, `21356`.
  Objective routing: `51703` start area, `73419` CSI projector, `51734` ride area, `73735` finish CSI.

### Exile Follow-up Chain

- `10518` Exile post-hoverboard follow-up.
  Giver: none in `Creature2`.
  Receiver: none in `Creature2`.
  Prerequisite quests: none in `Quest2`.
  Objectives: `21286`, `21287`, `21340`, `21318`, `21288`, `21290`.
  Key zones and locations: `51739`, `51662`, `51663`, `51664`, `51671`, `52753`, `51740`.
  Important note: this is the first unresolved handoff after `10527`.

- `10525` Exile housing handoff.
  Prerequisite quest: `10518`.
  Giver creature ids: `73491` Dorian Walker virtual.
  Receiver creature ids: `73421` Phineas T. Rotostar.
  Receiver world location: `52819`.
  Objectives: `21344`, `21319`, `21332`, `21333`, `21334`, `21382`.
  Direction ids: `2490` and `2491` on objectives `21319` and `21332`.

- `10540` Exile Rotostar return.
  Prerequisite quest: `10525`.
  Giver creature ids: `73421` Phineas T. Rotostar.
  Receiver creature ids: `73491` Dorian Walker virtual.
  Receiver world location: `51743`.
  Objectives: `21371`, `21373`, `21372`.

- `10519` Exile Dorian virtual step.
  Prerequisite quest: `10540`.
  Giver creature ids: `73491` Dorian Walker virtual.
  Receiver creature ids: `73491` Dorian Walker virtual.
  Objectives: `21293`, `21345`, `21346`.

- `10520` Exile Dorian movement step.
  Prerequisite quest: `10519`.
  Giver creature ids: `73491` Dorian Walker virtual.
  Receiver creature ids: `73491` Dorian Walker virtual and `74812` Dorian Walker arkship variant.
  Receiver world location: `51693`.
  Objectives: `21294`, `21297`, `21295`, `21296`, `21298`.
  Direction id: `2488` on objective `21294`.
  Explicit communicator delivery edge: communicator `7993` delivers `10520` from quest `10519` at state `3`.

- `10528` Exile arkship exit.
  Prerequisite quest: `10520`.
  Giver creature ids: `73491` Dorian Walker virtual and `74812` Dorian Walker arkship variant.
  Receiver creature ids: `53532` and `53533` scout Frostfield variants.
  Receiver world location: `42074`.
  Alternate receiver world location: `41641`.
  Objectives: `21326`, `21327`, `21328`, `21329`, `21342`.

### Dominion Follow-up Chain

- `10524` Dominion post-hoverboard follow-up.
  Giver: none in `Creature2`.
  Receiver: none in `Creature2`.
  Prerequisite quests: none in `Quest2`.
  Objectives: `21312`, `21313`, `21314`, `21315`, `21316`, `21317`.
  Key zones and locations: `52898`, `52899`, `52900`, `52901`, `52902`, `52903`, `53015`.
  Important note: this is the unresolved Dominion handoff after `10532`.

- `10526` Dominion housing handoff.
  Prerequisite quest: `10524`.
  Giver creature ids: `74861` Artemis Zin virtual.
  Receiver creature ids: `73421` Phineas T. Rotostar.
  Receiver world location: `52819`.
  Objectives: `21377`, `21378`, `21379`, `21380`, `21381`, `21383`.
  Direction ids: `2490` and `2491` on objectives `21378` and `21379`.

- `10541` Dominion Rotostar return.
  Prerequisite quest: `10526`.
  Giver creature ids: `73421` Phineas T. Rotostar.
  Receiver creature ids: `74861` Artemis Zin virtual.
  Receiver world location: `51743`.
  Objectives: `21375`, `21376`, `21374`.

- `10522` Dominion Artemis virtual step.
  Prerequisite quest: `10541`.
  Giver creature ids: `74861` Artemis Zin virtual.
  Receiver creature ids: `74861` Artemis Zin virtual.
  Objectives: `21306`, `21359`, `21360`.

- `10523` Dominion Artemis movement step.
  Prerequisite quest: `10522`.
  Giver creature ids: `74861` Artemis Zin virtual.
  Receiver creature ids: `74861` Artemis Zin virtual and `74863` Artemis Zin arkship variant.
  Receiver world location: `52906`.
  Objectives: `21307`, `21309`, `21308`, `21310`, `21311`.
  Direction id: `2488` on objective `21307`.
  Explicit communicator delivery edge: communicator `8062` delivers `10523` from quest `10522` at state `3`.

- `10530` Dominion arkship exit.
  Prerequisite quest: `10523`.
  Giver creature ids: `74861` Artemis Zin virtual and `74863` Artemis Zin arkship variant.
  Receiver creature ids: `53619` and `53620` Lieutenant Ticus variants.
  Receiver world location: `42150`.
  Alternate receiver world location: `42148`.
  Objectives: `21347`, `21348`, `21349`, `21350`, `21351`.

## Communicator Delivery Edges

- `10519 -> 10520` is explicitly delivered by communicator message `7993`.
- `10522 -> 10523` is explicitly delivered by communicator message `8062`.
- `10527` and `10532` have communicator state rows (`7981`, `7997`, `8000`, `8025`), but none of those rows deliver `10518` or `10524`.
- No communicator row in this quest set delivers `10518`, `10524`, `10525`, `10526`, `10540`, `10541`, `10528`, or `10530` directly.

## 73735 Post-hoverboard Handoff

- Objective data confirms that `73735` is the finish CSI target for both hoverboard quests:
  - `10527` objective `21325` is `type=12`, `data=73735`, `location=51734`.
  - `10532` objective `21356` is `type=12`, `data=73735`, `location=51734`.

- Runtime handlers already treat `73735` as a valid tutorial activation entity.

- `Creature2` entry `73735` is not a quest giver and not a quest receiver.
  - Activate spell: `87061`.
  - Activate spell prerequisites: none.
  - `prereqAnim = 43088`.

- `43088` is an OR prerequisite over faction-specific hoverboard completion-state checks:
  - `41950`: `10527` objective index `4` active, or `10527` in achieved/completed state.
  - `43087`: `10532` objective index `4` active, or `10532` in achieved/completed state.

- Practical conclusion:
  `73735` is a visibility or quest-animation gate, not a quest giver/receiver handoff node. The missing restoration seam is not spell resolution on `73735`; it is the transition after the hoverboard quest completes into `10518` or `10524`.

## What Is Safe To Script Today

- Keep the existing narrow recovery for `10513`, `10521`, `10527`, and `10532`.
- Keep the existing conservative hoverboard ride recovery after mount attach.
- Do not extend the scripted chain arrays yet.

## What Still Needs Live Validation

- Validate how `10518` is actually introduced after Exile hoverboard completion.
- Validate how `10524` is actually introduced after Dominion hoverboard completion.
- Validate whether the first post-hoverboard quest is auto-added, auto-mentioned, communicator-triggered without `QuestIdDelivered`, or granted by a world-script seam not represented by giver arrays.
- Validate that the `10525 -> 10540 -> 10519 -> 10520 -> 10528` Exile route and the `10526 -> 10541 -> 10522 -> 10523 -> 10530` Dominion route appear in live runtime with the expected visible receiver creatures.

## Recommended Next Runtime Checks

- Log quest add or mention events for `10518` and `10524` immediately after `10527` or `10532` completion.
- Log visibility and spawn state for `73491`, `74861`, `73421`, `74812`, and `74863` as the player crosses world zones `5967`, `5968`, `5969`, and `5998`.
- Capture the first successful `73735` activation in `worldserver.log` before changing any follow-up chain logic.