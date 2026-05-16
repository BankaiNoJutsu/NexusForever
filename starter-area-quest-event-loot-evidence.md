# Starter Area Quest, Event, and Loot Evidence

## Scope

- Evidence source: `wildstar_client` `WorldZone`, `Quest2`, `QuestObjective`, `Quest2Reward`, `ClientEvent`, `PublicEvent`, `PublicEventObjective`, `Item2`, and `stringsenus`.
- Runtime cross-check: `Source/NexusForever.Game.Static/Quest/QuestRewardType.cs` and `Source/NexusForever.Game/Entity/QuestManager.cs` for current reward-type handling.
- Decompile cross-check: existing labeled quest, communicator, CSI, and loot request surfaces in `Decomp/Analysis/function_labels.csv` and `Decomp/Analysis/INITIAL_FINDINGS.md`.
- Loot blocker cross-check: source dump `jabbithole_mysql/creatures.sql` versus the currently imported `jabbithole` schema.

This pass keeps the starter-area scope narrow:

- `4961` `Nexus Virtuality` for the tutorial chain that feeds Riders' Reef.
- `622` `Crimson Isle`.
- `1304` `Levian Bay`.
- `1414` `Everstar Grove`.
- `248` `Celestion`.

## Main Findings

- The tutorial quests currently discussed as Riders' Reef content are table-keyed to `WorldZone` root `4961` `Nexus Virtuality` and child `4965`, not to the visible Riders' Reef display zones `5966`, `6009`, or `6010`.
- The outdoor starter pass is not one small zone pair. The low-level roots split into four surface families with materially different breadth:
  - `Crimson Isle`: `37` quests, `conLevel 3-5`.
  - `Levian Bay`: `34` quests, `conLevel 3-5`.
  - `Everstar Grove`: `31` quests, `conLevel 3-6`.
  - `Celestion`: `179` quests, `conLevel 6-13`, with much broader descendant-zone and public-event overlap than the other starter roots.
- The current decompile labels already cover the request and interaction side of starter quest flow well enough to anchor future work:
  - `QuestInteraction_HandleAcceptOrRetry`
  - `Dialog_SendClientQuestComplete`
  - `Communicator_HandleQuestOrSpamMessage`
  - `CSIKey_HandlePress`
  - `Loot_PrepareAndDispatchBindcheck`
  - `Loot_DispatchBindcheckEvent`
  - `Loot_SendClientLootItemCollect`
  - `Loot_HandlePendingLootInteract`
  - `Loot_CheckInteractionRange`
- The missing durable client boundary for starter restoration is still quest guidance and objective-world-location routing. There is no current durable label for `QuestDirection` / `ServerQuestObjectiveWorldLocation` handling.
- Quest reward rows are straightforward to crosswalk from client tables today. Full creature-drop crosswalk work is still blocked because the imported `jabbithole.creatures` schema no longer exposes the `id` and `zone_id` keys that the source dump defines, while `jabbithole.item_drops` still keys rows by `creature_id`.

## Zone Coverage

| Root `WorldZone` | Root name | Quest count | Con-level range | Quests with quest prerequisites | Quests with reward rows | Quest-linked `ClientEvent` rows | `PublicEvent` rows |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| `4961` | `Nexus Virtuality` | `18` | `1-3` | `10` | `4` | `0` | `0` |
| `622` | `Crimson Isle` | `37` | `3-5` | `17` | `11` | `4` | `1` |
| `1304` | `Levian Bay` | `34` | `3-5` | `16` | `8` | `1` | `0` |
| `1414` | `Everstar Grove` | `31` | `3-6` | `18` | `8` | `3` | `1` |
| `248` | `Celestion` | `179` | `6-13` | `73` | `77` | `4` | `5` |

Practical interpretation:

- `Crimson Isle`, `Levian Bay`, and `Everstar Grove` are compact enough to treat as direct starter-slice content witnesses.
- `Celestion` is still starter-adjacent and low-level, but its descendant tree is broad enough that future passes should target specific early chains rather than the full `179`-quest family at once.

## Tutorial Root: `Nexus Virtuality`

The tutorial root currently contains `18` quest rows:

- `10513`, `10521`, `10527`, `10532`: the known opening `Navigating Nexus` variants.
- `10518`, `10524`: the known `The Face of the Enemy` follow-up pair.
- `10525`, `10526`: `A Claim to Stake`.
- `10540`, `10541`: `Lots in Store!`.
- `10519`, `10522`: `Servicing Circuitry`.
- `10520`, `10523`: `Deconstruct the Construct`.
- `10528`, `10530`: `Bon Voyage`.
- `10529`, `10533`: additional `The Face of the Enemy` variants surfaced under the same root but not currently represented in the narrow Riders' Reef runtime arrays.

Current high-value tutorial chains remain:

- Exile witness:
  `10518 -> 10525 -> 10540 -> 10519 -> 10520 -> 10528`
- Dominion witness:
  `10524 -> 10526 -> 10541 -> 10522 -> 10523 -> 10530`

The earlier evidence from `artifacts/verify/riders-reef-tutorial-quest-graph.md` still holds, but this broader root scan matters because it shows the runtime tutorial slice is scoped more narrowly than the client root actually is.

## High-Signal Surface Quest Chains

These are the best next content witnesses for decompile-targeted starter restoration.

### Crimson Isle

- Main resistance chain:
  `5593 Mind the Mines, Scrap the Scrab -> 5573 Powering Down -> 8855 Stasis, Interrupted -> 5596 Ordnance Recovery -> 5597 Dregs and Thieves -> 5604 Tactical Demolitions -> 5580 Enforced Radio Silence -> 5583 Heavy Armor -> 5623 Moving Up -> 5594 Last Resistance`
- Parallel act chain:
  `5595 Blood in the Sand -> 5575 Seizing Power -> 8856 Stasis, Interrupted -> 7036 Introduction to the Zax -> 5814 Forward March`

### Levian Bay

- Main low-level chain:
  `5855 Lighting the Way -> 5856 Find Artemis Zin -> 5857 Eldan Insecurity -> 5867 Rerouting Power -> 5859 Seizing Control -> 5860 Automated Repairs -> 5959 The Key to Power -> 5861 Unlocking Knowledge -> 5864 Unwelcome Guests -> 8826 The Elder Cube -> 5865 Lost in the Fog -> 5866 A Daring Escape`
- Compact station handoff:
  `5868 Enter the Station -> 6789 Introduction to the Caretaker`

### Everstar Grove

- Main grove chain:
  `6296 Nature's Uprising -> 7499 The Arboretum -> 8804 The Arboretum -> 6298 Living Batteries -> 6301 Root Resuscitation -> 6302 Waking Elderoot -> 6303 A Plagued Forest -> 6304 Save the Keepers -> 7540 Elderoot's Request -> 6305 Queen of the Blight -> 6306 Mender Bender -> 6310 Arwick's Revenge -> 6311 No Escape -> 8784 Everything You Ever Wanted to Know -> 6312 Dominion's Demise -> 6335 Taking the Offensive -> 7539 By the Throat -> 6844 The Path to Celestion`

### Celestion

- Early sanctuary chain:
  `6618 Sanctuary Enshrined -> 6620 Predators and Prey -> 6621 Tools of the Trade -> 6623 Stitched Together -> 6626 End of the Elders -> 6622 Preyfinder's Demise -> 6627 Back to Woodhaven -> 6997 Wayfarer's Passage -> 7055 Wayfarer's Passage`
- Early covert/ICI chain:
  `6968 Gather the Crew -> 6969 Making Contact -> 6970 Follow the Data -> 6973 Careful Pickings -> 6974 Of Augmentation and Aggression -> 6975 GOOPed! -> 6971 A Taste of Augmentation -> 6972 Caretaker Showdown -> 6981 Ghost Intel -> 7051 Covert Communications -> 6982 Reprogramming Interrogation -> 7385 Mechanisms of War -> 6983 Sum Zero Aggressor -> 6987 Invasive Intelligence -> 6989 Listen Like Shadows -> 6990 Research and Development`

Celestion warning:

- The root is broad enough that later passes should pick one witness chain at a time instead of treating the whole family as a single starter script target.

## Quest-Linked Client Events

Only a small number of starter quests currently surface explicit `ClientEvent` quest-change rows.

### Crimson Isle

- `106`: `Q5573 Act 1 Sky Change` for `5573` `Powering Down`.
- `107`: `Q5575 Act 1 Sky Change` for `5575` `Seizing Power`.
- `129`: `Q5573 Act 1 - Shield Generator` for `5573`.
- `130`: `Q5575 Act 1 - Shield Generator` for `5575`.

### Levian Bay

- `274`: `GC030 Q5868 - Star Comm On Sky - MLB10` for `5868` `Enter the Station`.

### Everstar Grove

- `149`: `Q6302 - Elderoot Awake` for `6302` `Waking Elderoot`.
- `151`: `Q6302 - Elderoot Asleep` for `6302` `Waking Elderoot`.
- `152`: `Elderoot Dead - Quest Change` for `8784` `Everything You Ever Wanted to Know`.

### Celestion

- `162`, `163`, `164`: three `Q6677` Embertree quest-change rows for `6677` `Restoring the Balance`.
- `173`: `Celestion-Tract 2-ICI Falling Debris On 1-DJU` for `6986` `Shoot for the Stars`.

### Tutorial

- No `ClientEvent` quest-change rows surfaced for `Nexus Virtuality` tutorial quests `10513` through `10541`.
- Practical implication: the tutorial chain still looks more dependent on quest/objective, communicator, CSI, and world-script seams than on the generic `ClientEvent` quest-change table.

## Public Event Overlap

Starter roots also expose a smaller public-event layer that should stay separate from direct quest restoration.

### Celestion

- `221` `A Ravenous Rescue` with `11` objective witnesses.
- `240` `Ravenous Regroup` with `2` objective witnesses.
- `550` `Hidden ICI Lab`.
- `569` `Abandoned Hideout`.
- `593` `Grendelus the Guardian [GROUP 20+]`.

### Crimson Isle

- `142` `The Last Resistance`.

### Everstar Grove

- `252` `Firestorm's Fury`.

### Levian Bay and Tutorial

- No `PublicEvent` rows surfaced under the narrowed roots.

Practical implication:

- Celestion already blends direct quest content with public-event structure. Treat those as separate decompile witnesses unless a specific quest step is proven to depend on a public-event objective.

## Reward And Loot Surface

Current server enum mapping:

- `1 = Item`
- `3 = Money`
- `4 = TradeSkillXp`
- `7 = AccountCurrency`

Starter-root reward shape:

- `Crimson Isle`: item reward rows only in the current client sample.
- `Levian Bay`: item reward rows only in the current client sample.
- `Everstar Grove`: item reward rows only in the current client sample.
- `Celestion`: item, money, and tradeskill-XP reward rows.
- `Nexus Virtuality`: item and account-currency reward rows.

Representative reward witnesses:

- `Crimson Isle`:
  - `14572` `Bloodstone Canyon Minigun`
  - `14576` `Bloodstone Canyon Claws`
  - `27914` `Shieldbreaker Supergrips`
- `Levian Bay`:
  - `15549` `Skeech-Crafted Kilt`
  - `15568` `Demolition Shielding`
  - `29661` `Star-Comm Knapsack`
- `Everstar Grove`:
  - `16309` `Gift of the Livingroot`
  - `16320` `Firestorm Metasuit`
  - `29615` `Livingroot Bag`
- `Celestion`:
  - `17783` `Pollenberry Gauntlets`
  - `17785` `Godwood's Regalia`
  - `17795` `Torine Sisterhood Training Assistant`
- `Nexus Virtuality`:
  - `10528` and `10530` award item `80875` `Protostar's Revolutionary Rucksack 1`
  - `10525` and `10526` surface reward type `7` with `objectId = 6`, which the current server enum reads as `AccountCurrency`

### Creature-Drop Blocker

- The source dump `jabbithole_mysql/creatures.sql` defines `id` and `zone_id`.
- The currently imported `jabbithole.creatures` schema no longer exposes those keys, but `jabbithole.item_drops` still uses `creature_id`.
- Because of that mismatch, full starter-area creature-drop joins are not safe against the current imported schema.

Practical implication:

- For now, treat starter loot evidence as:
  - client-backed quest reward rows
  - client-backed loot interaction request surfaces from the decompile labels
- Do not build a durable starter creature-drop crosswalk until the `jabbithole` import restores `creatures.id` and `creatures.zone_id`, or equivalent temporary tables are rebuilt from the source SQL files.

## Current Decompile Coverage

The current labeled client request and interaction side already covers most starter-area edges that are safe to reason about without widening runtime behavior:

- `14039c8a0` `QuestInteraction_HandleAcceptOrRetry`
- `140557000` `Dialog_SendClientQuestComplete`
- `14043bf30` `Communicator_HandleQuestOrSpamMessage`
- `1404d9450` `CSIKey_HandlePress`
- `14039cee0` `Loot_PrepareAndDispatchBindcheck`
- `140430e00` `Loot_DispatchBindcheckEvent`
- `14039cff0` `Loot_SendClientLootItemCollect`
- `14039d0f0` `Loot_HandlePendingLootInteract`
- `14039d230` `Loot_CheckInteractionRange`

What still needs decompile work for starter restoration:

- quest guidance resolution from `QuestDirection` and `QuestDirectionEntry`
- objective-world-location display and packet handling, especially `ServerQuestObjectiveWorldLocation`
- any starter-specific handoff that uses generic event dispatch rather than explicit quest giver or communicator delivery rows

## Recommended Next Passes

1. Target the missing quest-guidance boundary first: `QuestDirection`, `QuestDirectionEntry`, and `ServerQuestObjectiveWorldLocation`.
2. Treat `Crimson Isle` quest `5573` / `5575`, `Levian Bay` quest `5868`, `Everstar Grove` quest `6302`, and `Celestion` quests `6677` / `6986` as the best client-event witnesses for starter-zone state-change work.
3. Keep the tutorial pass anchored on `Nexus Virtuality` `4961` / `4965`, not on visible Riders' Reef zones alone.
4. Repair the `jabbithole` creature import before attempting any broader creature-drop reconstruction.