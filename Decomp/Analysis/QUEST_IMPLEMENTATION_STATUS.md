# Quest Implementation Status

Last updated: 2026-06-19

Latest supplemental update: 2026-06-19 F-022 Q5597 Chua Explosives indicator 17899 activation UI smoke review.

Supplemental update: 2026-06-19 F-022 Q5597 Chua Explosives indicator 17899 activation UI smoke review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_world_dependencies` now records `QuestObjective.worldLocationsIdIndicator`
slot `worldLocationsIdIndicator03` / WorldLocation2 `17899` as
`runtime_q5597_chua_explosives_indicator17899_spawn_and_credit_tested_pending_client_ui_smoke`.
WorldLocation2 `17899` is the build 16042 Scarhide Camp indicator in world
`870`, WorldZone `1325`, position `-7144,-995.343,-1008.03`, radius `55`,
all phases `4294967295`. QuestObjective `8256` is `VirtualCollect` data `364`
count `6`, TargetGroup `4373` contains Creature2 `24286`, and
`CrimsonIsleMapScript` fallback-spawns all 30 reviewed Chua Explosives
placements in world `870` area `1218`. Focused tests load all 30 fallbacks,
activate every fallback to credit virtual item `364` while Q5597 is accepted,
remove the entity, reject missing-quest activation, and avoid duplicate credit.
This records server/emulator spawn and credit evidence only. The row remains
`not_retail_complete` pending live build 16042 proof that indicator `17899`
appears/routes correctly, Chua Explosives activation/objective UI smoke,
Scarhide Camp density/placement confirmation, despawn/respawn visual state,
duplicate/negative cases, Mondo dialog, reward/achievement UI, and end-to-end
Q5596->Q5597->Q5604 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-161627-quest-5597-chua-explosives-indicator-17899-activation-ui-smoke-review`.
Focused world-dependency generator coverage passed, content-retail audit
regenerated, validator output stayed all `not_retail_complete`, the focused
Q5597 xUnit filter passed `15/15`, and the generated Q5597 queue row remains
rank `1` with `32` blocker details and `retail_claim_allowed=false`, with
indicator `17899` still blocker rank `17` and achievement checklist row `5495`
next at blocker rank `18`.

Supplemental update: 2026-06-19 F-022 Q5597 Chua Explosives indicator 17897 activation UI smoke review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_world_dependencies` now records `QuestObjective.worldLocationsIdIndicator`
slot `worldLocationsIdIndicator02` / WorldLocation2 `17897` as
`runtime_q5597_chua_explosives_indicator17897_spawn_and_credit_tested_pending_client_ui_smoke`.
WorldLocation2 `17897` is the build 16042 Scarhide Camp indicator in world
`870`, WorldZone `1325`, position `-7270.21,-992.368,-828.018`, radius `55`,
all phases `4294967295`. QuestObjective `8256` is `VirtualCollect` data `364`
count `6`, TargetGroup `4373` contains Creature2 `24286`, and
`CrimsonIsleMapScript` fallback-spawns all 30 reviewed Chua Explosives
placements in world `870` area `1218`. Focused tests load all 30 fallbacks,
activate every fallback to credit virtual item `364` while Q5597 is accepted,
remove the entity, reject missing-quest activation, and avoid duplicate credit.
This records server/emulator spawn and credit evidence only. The row remains
`not_retail_complete` pending live build 16042 proof that indicator `17897`
appears/routes correctly, Chua Explosives activation/objective UI smoke,
Scarhide Camp density/placement confirmation, despawn/respawn visual state,
duplicate/negative cases, Mondo dialog, reward/achievement UI, and end-to-end
Q5596->Q5597->Q5604 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-160917-quest-5597-chua-explosives-indicator-17897-activation-ui-smoke-review`.
Focused world-dependency generator coverage passed, content-retail audit
regenerated, validator output stayed all `not_retail_complete`, the focused
Q5597 xUnit filter passed `15/15`, and the generated Q5597 queue row remains
rank `1` with `32` blocker details and `retail_claim_allowed=false`, with
indicator `17897` still blocker rank `16` and indicator `17899` next at blocker
rank `17`.

Supplemental update: 2026-06-19 F-022 Q5597 Chua Explosives indicator 17898 activation UI smoke review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_world_dependencies` now records `QuestObjective.worldLocationsIdIndicator`
slot `worldLocationsIdIndicator01` / WorldLocation2 `17898` as
`runtime_q5597_chua_explosives_indicator17898_spawn_and_credit_tested_pending_client_ui_smoke`.
WorldLocation2 `17898` is the build 16042 Scarhide Camp indicator in world
`870`, WorldZone `1325`, position `-7022.49,-995.437,-946.002`, radius `55`,
all phases `4294967295`. QuestObjective `8256` is `VirtualCollect` data `364`
count `6`, TargetGroup `4373` contains Creature2 `24286`, and
`CrimsonIsleMapScript` fallback-spawns all 30 reviewed Chua Explosives
placements in world `870` area `1218`. Focused tests load all 30 fallbacks,
activate every fallback to credit virtual item `364` while Q5597 is accepted,
remove the entity, reject missing-quest activation, and avoid duplicate credit.
This records server/emulator spawn and credit evidence only. The row remains
`not_retail_complete` pending live build 16042 proof that indicator `17898`
appears/routes correctly, Chua Explosives activation/objective UI smoke,
Scarhide Camp density/placement confirmation, despawn/respawn visual state,
duplicate/negative cases, Mondo dialog, reward/achievement UI, and end-to-end
Q5596->Q5597->Q5604 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-155949-quest-5597-chua-explosives-indicator-17898-activation-ui-smoke-review`.
Focused world-dependency generator coverage passed, content-retail audit
regenerated, validator output stayed all `not_retail_complete`, the focused
Q5597 xUnit filter passed `15/15`, and the generated Q5597 queue row remains
rank `1` with `32` blocker details and `retail_claim_allowed=false`, with
indicator `17898` still blocker rank `15` and indicator `17897` next at blocker
rank `16`.

Supplemental update: 2026-06-19 F-022 Q5597 Chua Explosives indicator 17896 activation UI smoke review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_world_dependencies` now records `QuestObjective.worldLocationsIdIndicator`
slot `worldLocationsIdIndicator00` / WorldLocation2 `17896` as
`runtime_q5597_chua_explosives_indicator17896_spawn_and_credit_tested_pending_client_ui_smoke`.
WorldLocation2 `17896` is the build 16042 Bloodstone Canyon indicator in world
`870`, WorldZone `1218`, position `-7133.34,-994.206,-856.854`, radius `55`,
all phases `4294967295`. QuestObjective `8256` is `VirtualCollect` data `364`
count `6`, TargetGroup `4373` contains Creature2 `24286`, and
`CrimsonIsleMapScript` fallback-spawns all 30 reviewed Chua Explosives
placements in world `870` area `1218`. Focused tests load all 30 fallbacks,
activate every fallback to credit virtual item `364` while Q5597 is accepted,
remove the entity, reject missing-quest activation, and avoid duplicate credit.
This records server/emulator spawn and credit evidence only. The row remains
`not_retail_complete` pending live build 16042 proof that indicator `17896`
appears/routes correctly, Chua Explosives activation/objective UI smoke,
density/placement confirmation, despawn/respawn visual state, duplicate/negative
cases, Mondo dialog, reward/achievement UI, and end-to-end
Q5596->Q5597->Q5604 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-154422-quest-5597-chua-explosives-indicator-17896-activation-ui-smoke-review`.
Focused world-dependency generator coverage passed, content-retail audit
regenerated, validator output stayed all `not_retail_complete`, the focused
Q5597 xUnit filter passed `15/15`, and the generated Q5597 queue row remains
rank `1` with `32` blocker details and `retail_claim_allowed=false`, with
indicator `17896` still blocker rank `14` and indicator `17898` next at blocker
rank `15`.

Supplemental update: 2026-06-19 F-022 Q5597 Mondo receiver location dialog/completion smoke review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_world_dependencies` now records `Quest2.WorldLocation2IdReceiver` `17803`
as
`runtime_q5597_mondo_receiver_spawn_accept_complete_tested_pending_dialog_ui_smoke`.
WorldLocation2 `17803` is the build 16042 Mondo Zax receiver in world `870`,
WorldZone `1227` (`Bloodstone Canyon`), position `-7662.75,-942.948,-671.901`,
radius `1.76183`, all phases `4294967295`. `CrimsonIsleMapScript`
fallback-spawns Creature2 `24187` (`Mondo Zax`) at this receiver location,
matching reviewed DataMapping `source_coordinate_id` `15089` within the eight
reviewed Mondo placements in world `870` area `1885`. Creature2 `24187` carries
Q5597 in `QuestIdGiven` and `QuestIdReceive`; focused QuestManager coverage
accepts Q5597 only with visible Mondo, completed Q5596, Dominion faction, and
the level gate, completes achieved Q5597 only with visible Mondo, grants
Quest2Reward rows `3000` and `5236`, and updates Bloodstone achievement `4134`.
`Q5596QuestScript` grants Q5597, and `Q5597QuestScript` grants Q5604. This
records server/emulator receiver and lifecycle evidence only. The row remains
`not_retail_complete` pending live build 16042 Mondo accept/completion dialog
smoke, receiver placement/subzone/phase visibility proof, reward UI and
inventory persistence, achievement UI/progression, duplicate/negative cases,
and end-to-end Q5596->Q5597->Q5604 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-153253-quest-5597-mondo-receiver-location-dialog-completion-smoke-review`.
Focused world-dependency generator coverage passed, content-retail audit
regenerated, validator output stayed all `not_retail_complete`, the focused
Q5597 xUnit filter passed `15/15`, and the generated Q5597 queue row remains
rank `1` with `32` blocker details and `retail_claim_allowed=false`, with the
Mondo receiver location still blocker rank `13` and the Q5597 Chua Explosives
objective indicator location next at blocker rank `14`.

Supplemental update: 2026-06-19 F-022 Q5597 Crimson Isle world-zone map visibility smoke review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_world_dependencies` now records `Quest2.worldZoneId` `622` as
`runtime_q5597_crimson_isle_world_zone_assets_tested_pending_client_visibility_smoke`.
Quest2 world zone `622` is Crimson Isle, `CrimsonIsleMapScript` is owned by
world `870`, focused map-script tests load the map, spawn Mondo Zax `24187` at
receiver WorldLocation2 `17803`, and spawn 30 Chua Explosives `24286`
placements used by QuestObjective `8256` (`VirtualCollect` `364`). QuestManager
and chain coverage prove Q5597 accept, completion, and follow-up gates only
after the server assets are available. This records server/emulator map-surface
evidence only. The row remains `not_retail_complete` pending live build 16042
proof for WorldZone `622` map/zone activation and visibility, route/path-arrow
or map guidance, episode/zone ordering, Mondo accept/complete dialog, Chua
Explosives objective UI, rewards, achievements, duplicate/negative cases, and
end-to-end Q5596->Q5597->Q5604 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-152429-quest-5597-crimson-isle-world-zone-map-visibility-smoke-review`.
Focused world-dependency generator coverage passed, content-retail audit
regenerated, validator output stayed all `not_retail_complete`, the focused
Q5597 xUnit filter passed `15/15`, and the generated Q5597 queue row remains
rank `1` with `32` blocker details and `retail_claim_allowed=false`, with the
world-zone dependency still blocker rank `12` and the Q5597 Mondo receiver
location next at blocker rank `13`.

Supplemental update: 2026-06-19 F-022 Q5597 Mondo starter accept dialog/prerequisite UI smoke review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_creatures` now records starter relation `1922` as
`runtime_q5597_mondo_starter_spawn_accept_gate_and_followup_tested_pending_dialog_ui_smoke`.
The bridge maps Jabbithole creature `3367` to reviewed build 16042 Creature2
`24187` (`Mondo Zax`), whose client row carries `QuestIdGiven=5597`.
`CrimsonIsleMapScript` fallback-spawns Mondo at Quest2 receiver WorldLocation2
`17803` matching reviewed DataMapping `source_coordinate_id` `15089` within the
eight reviewed Mondo placements in world `870` area `1885`. `Q5596QuestScript`
grants Q5597 as the follow-up, and focused QuestManager tests prove Q5597
accepts only with visible Mondo, completed Q5596, Dominion faction, and the
level gate satisfied, while rejecting missing Mondo, missing Q5596, wrong
faction, or low level. This records server/emulator starter availability and
accept-gate evidence only. The row remains `not_retail_complete` pending live
build 16042 Mondo accept dialog smoke, prerequisite visibility and denial
text/UI, exact Mondo spawn choice and density confirmation, reward/achievement
side effects, and end-to-end Q5596->Q5597->Q5604 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-151528-quest-5597-mondo-starter-accept-dialog-prereq-ui-smoke-review`.
Focused creature generator coverage passed, content-retail audit regenerated,
validator output stayed all `not_retail_complete`, the focused Q5597 xUnit
filter passed `15/15`, and the generated Q5597 queue row remains rank `1` with
`32` blocker details and `retail_claim_allowed=false`, with relation `1922`
still blocker rank `11` and the Q5597 world-zone dependency next at blocker
rank `12`.

Supplemental update: 2026-06-19 F-022 Q5597 Chua Explosives objective activation UI smoke review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_creatures` now records the objective relation `1090` as
`runtime_q5597_chua_explosives_30_fallbacks_virtual_collect_guards_tested_pending_client_ui_smoke`.
The bridge maps Jabbithole creature `3462` to reviewed build 16042 Creature2
`24286` (`Chua Explosives`). QuestObjective `8256` is `VirtualCollect` data
`364` count `6` with reward-pane TargetGroup `4373`, and
`CrimsonIsleMapScript` fallback-spawns all 30 reviewed Chua Explosives
placements in world `870` area `1218`. `Q5597ChuaExplosivesEntityScript` plus
the creature wrapper credit virtual item `364` while Q5597 is accepted, remove
the entity, ignore missing quest state, and suppress repeated activation.
Focused tests cover both collectable-unit and creature activation paths plus all
30 fallback activations. This records server/emulator objective activation
evidence only. The row remains `not_retail_complete` pending live build 16042
activation/objective UI smoke, exact density and placement confirmation,
respawn/despawn visual-state smoke, Mondo dialog/client flow, reward UI and
inventory persistence, achievement UI/progression smoke, duplicate/negative
cases, and end-to-end Q5597->Q5604 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-150153-quest-5597-chua-explosives-objective-activation-ui-smoke-review`.
Focused creature generator coverage passed, content-retail audit regenerated,
validator output stayed all `not_retail_complete`, the focused Q5597 xUnit
filter passed `15/15`, and the generated Q5597 queue row remains rank `1` with
`32` blocker details and `retail_claim_allowed=false`, with relation `1090`
still blocker rank `10` and starter relation `1922` next at blocker rank `11`.

Supplemental update: 2026-06-19 F-022 Q5597 Mondo finisher reward/achievement UI smoke review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_creatures` now records the finisher relation `1564` as
`runtime_q5597_mondo_finisher_spawn_rewards_achievement_and_followup_tested_pending_dialog_ui_smoke`.
The bridge maps Jabbithole creature `3367` to reviewed build 16042 Creature2
`24187` (`Mondo Zax`), whose client row carries `QuestIdReceive=5597`.
`CrimsonIsleMapScript` fallback-spawns Mondo at Quest2 receiver WorldLocation2
`17803` matching reviewed DataMapping `source_coordinate_id` `15089`, focused
QuestManager coverage completes achieved Q5597 only with visible Mondo, grants
fixed Quest2Reward rows `3000` and `5236`, updates Bloodstone achievement
`4134`, and Q5597QuestScript grants Q5604 as the follow-up. This records
server/emulator completion, reward, achievement, and follow-up evidence only.
The row remains `not_retail_complete` pending live build 16042 Mondo completion
dialog smoke, reward UI and inventory persistence smoke, achievement
UI/progression smoke, duplicate/negative completion cases, and end-to-end
Q5597->Q5604 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-144826-quest-5597-mondo-finisher-reward-achievement-ui-smoke-review`.
Focused creature generator coverage passed, content-retail audit regenerated,
validator output stayed all `not_retail_complete`, the focused Q5597 xUnit
filter passed `15/15`, and the generated Q5597 queue row remains rank `1` with
`32` blocker details and `retail_claim_allowed=false`, with relation `1564`
still blocker rank `9`.

Supplemental update: 2026-06-19 F-022 Q5597 Crimson Isle quest-zone WorldZone 12 bridge review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_zone_evidence` now records the `quest_zones` relation `633` as
`datamapping_q5597_crimson_isle_quest_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof`.
The bridge maps archived Jabbithole zone `9` (`Crimson Isle`) to WorldZone `12`
with `match_status=matched` and source `last_seen_in=3`, but current build 16042
`world_zone_client_map.csv` shows WorldZone `12` has no client name, parent zone
`0`, `allow_access=0`, and no Crimson Isle ownership. Current named Crimson
Isle evidence instead uses Q5597's `Quest2.worldZoneId` `622` plus child rows
`623`, `629`, `1217`, `1218`, `1219`, `1227`, and `1284`. This records a
stale/incomplete DataMapping route-evidence blocker, not runtime routing
behavior or retail completion. The row remains `not_retail_complete` pending a
reviewed current WorldZone replacement, MapZone/QuestDirection evidence, or
live build 16042 route/visibility/map-guidance smoke, plus Mondo dialog, Chua
Explosives objective UI, reward/achievement side effects, negative cases, and
end-to-end Q5597 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-143751-quest-5597-crimson-isle-quest-zone-worldzone12-bridge-review`.
Focused zone generator coverage passed, content-retail audit regenerated,
validator output stayed all `not_retail_complete`, the focused Q5597 xUnit
filter passed `15/15`, and the generated Q5597 queue row remains rank `1` with
`32` blocker details and `retail_claim_allowed=false`, with relation `633`
still blocker rank `8`.

Supplemental update: 2026-06-19 F-022 Q5597 Auroria call-zone WorldZone 6 missing bridge review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_zone_evidence` now records the `quest_call_zones` relation `4641` as
`datamapping_q5597_auroria_call_zone_worldzone6_missing_reviewed_nonprimary_pending_route_proof`.
The bridge maps archived Jabbithole zone `5` (`Auroria`) to WorldZone `6` with
`match_status=partial` and source `last_seen_in=0`, but current build 16042
`world_zone_client_map.csv` has no WorldZone `6` row or client name. Current
named Auroria evidence instead uses WorldZone `36` (`Auroria`) plus child rows
`697` (`Northeastern Auroria`), `1228` (`Northwestern Auroria`), `1302`
(`Central Auroria`), and `1303` (`Southern Auroria`). Q5597's current quest
WorldZone remains `622` (`Crimson Isle`). This records a stale/incomplete
DataMapping route-evidence blocker, not runtime routing behavior or retail
completion. The row remains `not_retail_complete` pending a reviewed current
WorldZone replacement, MapZone/QuestDirection evidence, or live build 16042
route/visibility/map-guidance smoke, plus Mondo dialog, Chua Explosives
objective UI, reward/achievement side effects, negative cases, and end-to-end
Q5597 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-142700-quest-5597-auroria-call-zone-worldzone6-missing-bridge-review`.
Focused zone generator coverage passed, content-retail audit regenerated,
validator output stayed all `not_retail_complete`, the focused Q5597 xUnit
filter passed `15/15`, and the generated Q5597 queue row remains rank `1` with
`32` blocker details and `retail_claim_allowed=false`, with relation `4641`
still blocker rank `7`.

Supplemental update: 2026-06-19 F-022 Q5597 Malgrave call-zone WorldZone 42 Protostar Honeyworks bridge review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_zone_evidence` now records the `quest_call_zones` relation `4640` as
`datamapping_q5597_malgrave_call_zone_worldzone42_protostar_honeyworks_reviewed_nonprimary_pending_route_proof`.
The bridge maps archived Jabbithole zone `35` (`Malgrave`) to current WorldZone
`42` (`Protostar Honeyworks`) with source `last_seen_in=0`; current build 16042
`world_zone_client_map.csv` shows WorldZone `42` is Protostar Honeyworks under
parent WorldZone `733` (`Protostar Honeyworks Master`), while named Malgrave
evidence uses WorldZone `377` (`Malgrave`), WorldZone `1711` (`The Malgrave
Trail`), WorldZone `1890` (`Southern Malgrave`), WorldZone `1928` (`Central
Malgrave`), WorldZone `1933` and `2433` (`Malgrave`), and WorldZone `4372`
(`Malgrave`). Q5597's current quest WorldZone remains `622` (`Crimson Isle`).
This records a stale/incomplete DataMapping route-evidence blocker, not runtime
routing behavior or retail completion. The row remains `not_retail_complete`
pending a reviewed current WorldZone replacement, MapZone/QuestDirection
evidence, or live build 16042 route/visibility/map-guidance smoke, plus Mondo
dialog, Chua Explosives objective UI, reward/achievement side effects, negative
cases, and end-to-end Q5597 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-141909-quest-5597-malgrave-call-zone-worldzone42-protostar-honeyworks-bridge-review`.
Focused zone generator coverage passed, content-retail audit regenerated,
validator output stayed all `not_retail_complete`, the focused Q5597 xUnit
filter passed `15/15`, and the generated Q5597 queue row remains rank `1` with
`32` blocker details and `retail_claim_allowed=false`, with relation `4640`
still blocker rank `6`.

Supplemental update: 2026-06-19 F-022 Q5597 Northern Wastes call-zone WorldZone 41 Fort Glory bridge review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_zone_evidence` now records the `quest_call_zones` relation `4639` as
`datamapping_q5597_northern_wastes_call_zone_worldzone41_fort_glory_reviewed_nonprimary_pending_route_proof`.
The bridge maps archived Jabbithole zone `34` (`Northern Wastes`) to current
WorldZone `41` (`Fort Glory`) with source `last_seen_in=0`; current build 16042
`world_zone_client_map.csv` shows WorldZone `41` is Fort Glory under parent
WorldZone `848`, while named Northern Wastes evidence uses WorldZone `1784`,
child WorldZone `1785`, and child rows `1786`, `1787`, `1788`, `1790`, `1792`,
`1798`, `4036`, `4374`, `4377`, `4378`, and `4483`. Q5597's current quest
WorldZone remains `622` (`Crimson Isle`). This records a stale/incomplete
DataMapping route-evidence blocker, not runtime routing behavior or retail
completion. The row remains `not_retail_complete` pending a reviewed current
WorldZone replacement, MapZone/QuestDirection evidence, or live build 16042
route/visibility/map-guidance smoke, plus Mondo dialog, Chua Explosives
objective UI, reward/achievement side effects, negative cases, and end-to-end
Q5597 smoke.

A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-140947-quest-5597-northern-wastes-call-zone-worldzone41-fort-glory-bridge-review`.
Focused zone generator coverage passed, content-retail audit regenerated,
validator output stayed all `not_retail_complete`, the focused Q5597 xUnit
filter passed `15/15`, and the generated Q5597 queue row remains rank `1` with
`32` blocker details and `retail_claim_allowed=false`, with relation `4639`
still blocker rank `5`.

Supplemental update: 2026-06-19 F-022 Q5597 Housing Skymap call-zone WorldZone 60 Mozyk Quarry bridge review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_zone_evidence` now records the `quest_call_zones` relation `4638` as
`datamapping_q5597_housing_skymap_call_zone_worldzone60_mozyk_quarry_reviewed_nonprimary_pending_route_proof`.
The bridge maps archived Jabbithole zone `40` (`Housing Skymap`) to current
WorldZone `60` (`Mozyk Quarry`) with source `last_seen_in=0`; current build
16042 WorldZone data shows `60` is Mozyk Quarry under parent `734`, while named
Housing/Skymap evidence uses WorldZone `1630` (`Housing`), WorldZone `1136` and
`4373` (`Skymap`), and WorldZone `1265` and `4837` (`Community Skymap`).
Q5597's current quest WorldZone remains `622` (`Crimson Isle`). This records a
stale or incomplete DataMapping route candidate only; runtime routing remains
blocked pending a reviewed current WorldZone replacement, MapZone or
QuestDirection evidence, or live build 16042 route/visibility/map-guidance
smoke. Worksheet:
`artifacts\blocker_evidence\20260619-135937-quest-5597-housing-skymap-call-zone-worldzone60-mozyk-quarry-bridge-review`.
Focused zone generator coverage passed, content-retail audit regenerated,
validator output stayed all `not_retail_complete`, the focused Q5597 xUnit
filter passed `15/15`, and the generated Q5597 queue row remains rank `1` with
`32` blocker details and `retail_claim_allowed=false`.

Supplemental update: 2026-06-19 F-022 Q5597 Illium call-zone WorldZone 78 Ellevar bridge review.
Q5597 remains `quest_validation_ready` / `not_retail_complete`, but generated
`quest_zone_evidence` now records the `quest_call_zones` relation `4637` as
`datamapping_q5597_illium_call_zone_worldzone78_ellevar_reviewed_nonprimary_pending_route_proof`.
The bridge maps archived Jabbithole zone `46` (`Illium`) to current WorldZone
`78` (`Western Ellevar`) with source `last_seen_in=0`; current build 16042
WorldZone data shows `78` belongs under Ellevar parent `37`, while named Illium
evidence uses WorldZone `2191` and child rows `2192`, `2193`, `2194`, `2195`,
`2196`, `3014`, `4202`, `4296`, `4298`, `4365`, `4485`, `4486`, and `4957`.
Q5597's current quest WorldZone remains `622` (`Crimson Isle`). This records a
stale or incomplete DataMapping route candidate only; runtime routing remains
blocked pending a reviewed current WorldZone replacement, MapZone or
QuestDirection evidence, or live build 16042 route/visibility/map-guidance
smoke. Worksheet:
`artifacts\blocker_evidence\20260619-135045-quest-5597-illium-call-zone-worldzone78-ellevar-bridge-review`.
Focused zone generator coverage passed, content-retail audit regenerated,
validator output stayed all `not_retail_complete`, the focused Q5597 xUnit
filter passed `15/15`, and the generated Q5597 queue row remains rank `1` with
`32` blocker details and `retail_claim_allowed=false`.

Supplemental update: 2026-06-19 F-022 Q5597 Crimson Isle call-zone WorldZone 12 bridge review.
Q5597 (`Dregs and Thieves`) remains server-implemented but not
retail-complete. Its generated `quest_zone_evidence` row for
`quest_call_zones` relation `311` now records
`datamapping_q5597_crimson_isle_call_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof`
instead of generic matched-zone runtime visibility smoke. The current evidence
proves only that the archived Jabbithole relation maps zone `9` (`Crimson
Isle`) to a poor current-client route candidate: WorldZone `12` has no client
name, parent zone `0`, `allow_access=0`, and no Crimson Isle ownership in
`world_zone_client_map.csv`. Current build 16042 named Crimson Isle evidence
instead uses Q5597 `Quest2.worldZoneId` `622` plus child WorldZone rows `623`,
`629`, `1217`, `1218`, `1219`, `1227`, and `1284` under parent `622`. The row
remains `not_retail_complete` pending a reviewed current WorldZone replacement,
MapZone or QuestDirection evidence, or live build 16042 route, visibility, and
map-guidance smoke, plus episode/zone ordering, Mondo dialog, objective UI,
reward and achievement side effects, duplicate/negative cases, and end-to-end
Q5597 smoke. A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-133236-quest-5597-crimson-isle-call-zone-worldzone12-bridge-review`.
Focused zone generator coverage passed, the content-retail audit regenerated,
the validator reports `31` files / `168,101` rows all `not_retail_complete`,
and the Q5597 queue row remains first with `32` blocker details /
`retail_claim_allowed=false`.

Supplemental update: 2026-06-19 F-022 Q5597 episode order chain handoff review.
Q5597 (`Dregs and Thieves`) remains server-implemented but not
retail-complete. Its generated EpisodeQuest row `2398` for Episode `464`
(`The Guns of Bloodstone`) now records
`runtime_q5597_episode_order_q5596_q5597_q5604_handoff_tested_pending_client_episode_smoke`
instead of generic pending episode order/progression smoke. The current
evidence proves table/order alignment and emulator chain ownership only:
EpisodeQuest order `2` / flags `0` in WorldZone `622` maps to Jabbithole
episode `98`, `Q5596QuestScript` grants Q5597, `Q5597QuestScript` grants
Q5604, and `Q5604TacticalDemolitionsQuestScript` grants Q5580/Q5583 on
completion. The row remains `not_retail_complete` pending live build 16042
episode tracker/order UI presentation, quest visibility and turn-in state,
Mondo dialog, Chua Explosives objective UI, reward and achievement side
effects, duplicate/negative cases, and end-to-end Q5596->Q5597->Q5604 smoke. A
row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-131927-quest-5597-episode-order-chain-handoff-review`.
Focused episode generator coverage passed, the content-retail audit
regenerated, the validator reports `31` files / `168,101` rows all
`not_retail_complete`, the focused Q5597 plus Crimson Isle chain xUnit filter
passed `28/28`, and the Q5597 queue row remains first with `32` blocker
details / `retail_claim_allowed=false`.

Supplemental update: 2026-06-19 F-022 Q5597 Crimson Isle world-zone map-surface review.
Q5597 (`Dregs and Thieves`) remains server-implemented but not
retail-complete. Its generated `Quest2.worldZoneId` dependency for WorldZone
`622` (`Crimson Isle`) now points at focused server map-load coverage rather
than generic pending map activation smoke. The row records
`runtime_q5597_crimson_isle_world_zone_map_spawn_tested_pending_client_visibility_smoke`
because `CrimsonIsleMapScript` is owned by world `870`, focused tests load that
map script, spawn Mondo Zax `24187` at receiver WorldLocation2 `17803`, and
spawn the reviewed Chua Explosives `24286` placements for objective `8256`.
This is an emulator map-surface claim only. The row remains
`not_retail_complete` pending live build 16042 map/zone activation and
visibility, episode/zone ordering, path or map guidance, Mondo dialog,
objective UI, reward and achievement side effects, duplicate/negative cases,
and end-to-end Q5597 smoke. A row-specific CreateBundleOnly worksheet was
created at
`artifacts\blocker_evidence\20260619-130912-quest-5597-crimson-isle-world-zone-map-surface-review`.
Focused world-dependency generator coverage passed, the content-retail audit
regenerated, and the Q5597 queue row remains first with `32` blocker details /
`retail_claim_allowed=false`.

Supplemental update: 2026-06-19 F-022 Q5597 prerequisite accept-gate review.
Q5597 (`Dregs and Thieves`) remains server-implemented but not
retail-complete. Its generated prerequisite rows for level `1`, Dominion
faction `1`, and required Quest2 `5596` (`Ordnance Recovery`) now point at the
focused QuestManager accept-path tests instead of generic pending accept-smoke
statuses. The server evidence proves only the current emulator gate: Q5597
accepts when Mondo Zax `24187` is visible, the player is Dominion, the level
gate is satisfied, and Q5596 is complete, and it rejects missing Mondo, missing
Q5596, wrong faction, or a low level. The rows remain `not_retail_complete`
pending live build 16042 Mondo availability/dialog and denial UI,
Q5596-to-Q5597 chain presentation, reward and achievement side effects,
duplicate/negative cases, and end-to-end Q5597 smoke. A row-specific
CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-125638-quest-5597-prerequisite-accept-gate-review`.
Focused prerequisite generator coverage passed, the content-retail audit
regenerated, and the Q5597 queue row remains first with `32` blocker details /
`retail_claim_allowed=false`.

Supplemental update: 2026-06-19 F-022 Q5597 Chua Explosives script quest-state guard review.
Q5597 (`Dregs and Thieves`) remains server-implemented but not
retail-complete. Its generated Script.Main progression evidence now links the
Chua Explosives activation credit to the Q5597 quest-state guard instead of
leaving the row as owner-review-only. The scanner resolves line `49` in
`Q5597.cs` to `ObjectiveUpdate(QuestObjectiveType.VirtualCollect, 364, 1)`,
matches the typed/object pair to QuestObjective `8256`, and records the
surrounding accepted-state guard for Quest2 `5597` as
`quest_state_guard_matches_quest`. The row remains
`script_quest_typed_objective_update_matched_pending_trigger_smoke` and
`not_retail_complete`, pending live client/server proof for exact activation
trigger timing, objective UI behavior, duplicate/repeat negative cases, reward
and achievement side effects, and source-drop versus activation-only semantics.
A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-124414-quest-5597-chua-explosives-script-quest-state-guard-review`.
Focused script-progression generator coverage passed, the content-retail audit
regenerated, and the Q5597 queue row remains first with `32` blocker details /
`retail_claim_allowed=false`.

Supplemental update: 2026-06-19 F-022 Q5597 Chua Explosives virtual-loot source review.
Q5597 (`Dregs and Thieves`) remains server-implemented but not
retail-complete. Its six generated quest-loot blocker rows for LaughingWS loot
groups `1200000482` and `1200001069` now distinguish runtime-tested
VirtualItem delivery from unresolved source/drop and client evidence. The
loot groups are gated by QuestObjective `8256` (`VirtualCollect`, data `364`);
their item rows carry item type `6` / staticId `364`; and their entity bindings
target Creature2 `24286` (`Chua Explosives`). The generated audit now cites
`LootInstanceItem.cs` and `LootInstanceDeliveryTests.cs` because generic loot
delivery updates `QuestObjectiveType.VirtualCollect`, while keeping every row
`not_retail_complete` pending proof of the retail source/drop trigger,
activation-only versus loot-source behavior, client-visible loot UI, drop
cadence, inventory cleanup, duplicate/negative cases, and end-to-end Q5597
smoke. A row-specific CreateBundleOnly worksheet was created at
`artifacts\blocker_evidence\20260619-123609-quest-5597-chua-explosives-virtual-loot-source-review`.
Focused quest-loot generator coverage passed, the content-retail audit
regenerated, and the Q5597 queue row remains first with `32` blocker details /
`retail_claim_allowed=false`.

Supplemental update: 2026-06-19 F-022 Q5597 Auroria stale WorldZone blocker classification.
Q5597 (`Dregs and Thieves`) remains server-implemented but not
retail-complete. Its `quest_call_zones` source row `4641` now has explicit
generated blocker wording: Jabbithole zone `5` (`Auroria`) points at WorldZone
`6`, but current build 16042 WorldZone evidence has no WorldZone `6` row/name.
The row stays
`datamapping_quest_zone_partial_match_pending_zone_review` because current
client data instead shows WorldZone `36` (`Auroria`) and child rows such as
WorldZone `1228` (`Northwestern Auroria`), while Q5597 itself is a Crimson Isle
Quest2 row under WorldZone `622`. The audit generator now names the required
unblocker as reviewed WorldZone.tbl, MapZone/QuestDirection, or accepted live
client route evidence that identifies the current replacement before visibility
or map-routing behavior is claimed. A row-specific CreateBundleOnly worksheet
was created at
`artifacts\blocker_evidence\20260619-122203-quest-5597-auroria-stale-worldzone-bridge-review`.
Focused quest-zone generator coverage passed, the content-retail audit
regenerated, and the Q5597 queue row remains first with `32` blocker details /
`retail_claim_allowed=false`.

Supplemental update: 2026-06-19 F-022 Q5597 Chua Explosives bridge review.
Q5597 (`Dregs and Thieves`) remains server-implemented but not
retail-complete. The generated queue still ranks it first as
`quest_validation_ready` with `32` blocker details and
`retail_claim_allowed=false`, but the Chua Explosives objective bridge is now a
reviewed DataMapping match: Jabbithole creature `3462` maps to Creature2
`24286` through tracked override
`Tools\DataMapping\creature_bridge_overrides.csv`. The review is backed by
QuestObjective `8256` (`VirtualCollect`, data `364`, count `6`), reward-pane
TargetGroup `4373` containing Creature2 `24286`, the single DataMapping
Creature2 candidate, all `30` source coordinates in world `870` / area `1218`,
`CrimsonIsleMapScript` fallback placement coverage, and
`Q5597ChuaExplosivesEntityScript` virtual item `364` credit while Q5597 is
accepted. Regenerated mapper/audit outputs now show `creature_map.csv`
`match_status=reviewed` / `original_match_status=unique_name`, and objective
relation `1090` regenerates as reviewed with
`runtime_q5597_chua_explosives_objective_relation_spawn_and_virtual_collect_credit_tested_pending_client_smoke`.
Focused Q5597 tests passed `15/15` with `--no-build`, generated
content-retail validation passed for `31` files / `168,101` rows all
`not_retail_complete`, and the new CreateBundleOnly worksheet is
`artifacts\blocker_evidence\20260619-121140-quest-5597-chua-explosives-bridge-review`.
Interactive client/server smoke is still needed for Mondo dialog and receiver
flow, Chua Explosives activation/objective UI timing, density and
respawn/despawn behavior, reward UI/inventory persistence, achievement UI,
Q5596->Q5597->Q5604 chain behavior, wrong-world/unaccepted/repeat negative
cases, and end-to-end Q5597 validation.

Supplemental update: 2026-06-18 F-022 Q3797 validation-gate recheck.
Q3797 (`Securing the Area`) remains server-implemented but not retail-complete.
The generated next-slice queue still ranks it twelfth as
`quest_validation_ready` with `23` blocker rows, objective `4918`, rewards
`2480;2482;4770`, and script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3797SecuringTheArea.cs`.
The local WildStar client dependency was found at
`I:\WildStar\Client64\WildStar64.exe`, so the remaining blocker is not a
missing executable. A fresh row-specific worksheet was created with
`-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-012134-quest-3797-securing-the-area-recheck`;
it records automated coverage, present client paths, objective/zone evidence,
Durek starter/finisher rows, rootbrute/yeti target rows, reward rows, and the
interactive smoke still required. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3797"`
with `22/22` tests, covering TargetGroup `1177` nested creature expansion,
KillTargetGroups credit for `11945;11948;11962;11963;12212;12213`, Durek
giver/receiver routes, Q3486 prerequisite and visible-Durek accept/reject
guards, selectable rewards `2480;2482;4770`, item ids
`13333;27870;27871`, cash `145`, and achievement hook checks. Harness preset
tests passed `2/2`, and generated content-retail validation passed for `31`
files / `168,101` rows. DataMapping recheck keeps Jabbithole objective row
`732` mapped to QuestObjective `4918` type `8` data `1177` count `8`, partial
zone rows `96` / `166`, reviewed Durek starter/finisher relations `166` /
`1693` at Creature2 `11066`, reviewed Rootbrute Grimspore relation `105` at
Creature2 `12212`, Yeti Frostclaw relation `106` at Creature2 `11948`,
Rootbrute Deathcap relation `3055` at Creature2 `12213`, and Yeti Snowstalker
relation `5204` at reviewed Creature2 `11945`. Interactive client/server smoke
is still needed for Q3486-to-Q3797 visibility and acceptance, Durek dialog,
rootbrute/yeti combat timing, placement density/respawn/tap/threat behavior,
type-9/non-spawned target branches, partial zone row review and routing, reward
UI/inventory persistence, wrong-world/unrelated-kill and repeat negative cases,
and end-to-end Q3797 validation.

Supplemental update: 2026-06-18 F-022 Q5580 validation-gate recheck.
Q5580 (`Enforced Radio Silence`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it eleventh as
`quest_validation_ready` with `26` blocker rows, objective `8227`, rewards
`3005;3007;3008;3009;3010;3011`, achievement `4135`, and script owner
`Source\NexusForever.Script.Main\Quests\CrimsonIsle\Q5580.cs`. The local
WildStar client dependency was found at `I:\WildStar\Client64\WildStar64.exe`,
and the client `Logs` / `Errors` folders are present, so the remaining blocker
is not a missing executable or log path. A fresh row-specific worksheet was
created with `-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-011658-quest-5580-enforced-radio-silence-recheck`;
it records automated coverage, present client paths, objective/zone/episode
evidence rows, Tower Controls placement rows, Kezrek starter/finisher rows, and
the interactive smoke still required. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5580"`
with `2/2` tests, covering the Q5594 merge gate for Q5580 completion while
Q5583 is missing and the Q5580 Tower Controls fallback placement/checklist
coverage. Harness preset tests passed `2/2`, and generated content-retail
validation passed for `31` files / `168,101` rows. DataMapping recheck keeps
Jabbithole objective row `1452` mapped to QuestObjective `8227` type `14` data
`2889` count `2`, zone rows `309` / `620` matched for Crimson Isle, episode
row `2389` mapped to Operation Annihilator, Tower Controls objective relation
`1053` mapped uniquely at Creature2 `26559` with source-coordinate ids
`15122;15123`, and reviewed Kezrek Warbringer relations `1559` / `465` at
Creature2 `24158` with source-coordinate ids `15181;170390`. Interactive
client/server smoke is still needed for Q5604-to-Q5580/Q5583 grant/accept,
Kezrek accept/turn-in dialog or confirmed script-grant-only route, Tower
Controls activation packet/UI/timing, active-prop visual state, reward-choice
and achievement UI/persistence, quest-direction and episode presentation,
wrong-world/repeat negative cases, and the Q5594 merge gate before and after
Q5583 completion.

Supplemental update: 2026-06-18 F-022 Q5575 validation-gate recheck.
Q5575 (`Seizing Power`) remains server-implemented but not retail-complete. The
generated next-slice queue still ranks it tenth as `quest_validation_ready`
with `25` blocker rows, objectives `8523;8371;12871`, rewards
`2993;2994;2995`, achievement `4133`, and script owner
`Source\NexusForever.Script.Main\Quests\CrimsonIsle\Q5575.cs`. The local
WildStar client dependency was found at `I:\WildStar\Client64\WildStar64.exe`,
so the remaining blocker is not a missing executable. A fresh row-specific
worksheet was created with `-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-011114-quest-5575-seizing-power-recheck`;
it records automated coverage, present client executable, reviewed Kezrek/Power
Regulator rows, objective-evidence rows, zone rows, and interactive smoke still
required. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5575"`
with `3/3` tests, covering Power Regulator completion hidden-objective credit,
repeat-after-achieved suppression, and Q5575 completion grant of Q5596. Harness
preset tests passed `2/2`, and generated content-retail validation passed for
`31` files / `168,101` rows. DataMapping recheck keeps Kezrek Warbringer
starter relation `2212` reviewed at Creature2 `24158`, Power Regulator
objective relation `2561` reviewed at Creature2 `24999`, objective rows `2703`
/ `2704` mapped to objectives `8523` / `8371`, and zone rows `810` / `1315`
matched. Interactive client smoke is still needed for Q5595-to-Q5575
grant/accept, Kezrek dialog, Shield Generator and Power Regulator UI/timing,
reward-choice and achievement UI, wrong-world/repeat negative cases, and the
`Q5575->Q5596` branch flow.

Supplemental update: 2026-06-18 F-022 Q5573 validation-gate recheck.
Q5573 (`Powering Down`) remains server-implemented but not retail-complete. The
generated next-slice queue still ranks it ninth as `quest_validation_ready`
with `31` blocker rows, objectives `8524;8229;12870`, rewards
`2989;2991;2992`, achievement `4132`, and script owner
`Source\NexusForever.Script.Main\Quests\CrimsonIsle\Q5573PoweringDown.cs`. The
local WildStar client dependency was found at
`I:\WildStar\Client64\WildStar64.exe`, so the remaining blocker is not a
missing executable. A fresh row-specific worksheet was created with
`-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-010811-quest-5573-powering-down-recheck`;
it records automated coverage, present client executable, reviewed Mondo/Power
Regulator rows, objective-evidence rows, zone rows, cinematic timing boundary,
and interactive smoke still required. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5573"`
with `3/3` tests, covering Power Regulator completion cinematic/hidden objective
credit, repeat-after-achieved suppression, and Q5573 completion grant of Q5596.
Harness preset tests passed `2/2`, and generated content-retail validation
passed for `31` files / `168,101` rows. DataMapping recheck keeps Mondo Zax
relations `starter:1919` and `finisher:1558` reviewed at Creature2 `24187`,
Power Regulator objective relation `1065` reviewed at Creature2 `24999`,
objective rows `1450` / `1451` mapped to objectives `8524` / `8229`, and zone
rows `308` / `627` matched; zone row `2122` remains partial. Interactive client
smoke is still needed for Q5593-to-Q5573 grant/accept, Mondo dialog, Shield
Generator and Power Regulator UI/timing, cinematic timing, reward-choice and
achievement UI, wrong-world/repeat negative cases, and the `Q5573->Q5596`
branch flow.

Supplemental update: 2026-06-18 F-022 Q3963 validation-gate recheck.
Q3963 (`More Important Than Revenge`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it eighth as
`quest_validation_ready` with `21` blocker rows, objective `5201`, rewards
`3706;3707;4771`, achievement `3491`, and script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3963MoreImportantThanRevenge.cs`.
The local WildStar client dependency was found at
`I:\WildStar\Client64\WildStar64.exe`, so the remaining blocker is not a
missing executable. A fresh row-specific worksheet was created with
`-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-010457-quest-3963-more-important-than-revenge-recheck`;
it records the automated coverage, present client executable, Deadeye starter
identity boundary, unmatched source `23645`, zone rows, Ship Controls
activation boundary, and interactive smoke still required. Focused verification
passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3963|FullyQualifiedName=NexusForever.Game.Tests.Transport.WorldLocationTeleporterEntityScriptTests.OnActivateSuccess_WithShipControlsOnNorthernWilds_DoesNotTeleport"`
with `16/16` tests, covering Ship Controls `27196` objective credit, outside
Northern Wilds rejection, non-teleporter behavior, Q3487 follow-up to Q3963,
Galeras Deadeye `16622` receiver completion, current receiver indexing,
selectable rewards, and achievement hook `3491`. Harness preset tests passed
`2/2`, and generated content-retail validation passed for `31` files /
`168,101` rows. DataMapping recheck keeps reviewed rows for
`finisher:96:201->16622`, `objective:445:1090->27196`, and
`starter:1621:1073->11063`, but leaves `starter:2130:23645` blocked because
source creature `23645` remains `Unknown` / `unmatched`. Zone rows `67` and
`259` remain partial Northern Wilds rows, while `1888` bridges source zone `13`
to world zone `17`; those route claims still need live client/runtime evidence.
Interactive client smoke is still needed for Q3487-to-Q3963 handoff, Ship
Controls activation packet/timing and UI, Deadeye starter/receiver dialog,
reward-choice and achievement UI, wrong-world/repeat negative cases, and
end-to-end Q3963 validation.

Supplemental update: 2026-06-18 F-022 Q3886 validation-gate recheck.
Q3886 (`Fiery Distraction`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it seventh as
`quest_validation_ready` with `23` blocker rows, objectives `5053;5052`,
rewards `2192;2430;4767`, achievement `3490`, and script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3886FieryDistraction.cs`.
The local WildStar client dependency was found at
`I:\WildStar\Client64\WildStar64.exe`, so the remaining blocker is not a
missing executable. A fresh row-specific worksheet was created with
`-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-010130-quest-3886-fiery-distraction-recheck`;
it records the automated coverage, the present client executable, exact
zone/creature evidence rows, the duplicate Burning Torch review boundary, and
the interactive smoke still required. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3886"`
with `11/11` tests, covering Durek `11066` starter/receiver indexing, Q3886
follow-up to Q3673, fallback Burning Torch `13630` and Skeech Hut `13623`
spawns/checklist indexes, selectable rewards, and achievement hook `3490`.
Harness preset tests passed `2/2`, and generated content-retail validation
passed for `31` files / `168,101` rows. DataMapping recheck keeps Durek
relations `finisher:102` and `starter:1231` reviewed, but leaves objective
relations `343:577:13623`, `344:613:13630`, and duplicate Burning Torch
relation `8506:32183:13630` blocked on client-visible activation/placement
proof. Zone rows `76` and `235` remain partial Northern Wilds rows, while
`5459` and `3439` bridge source zone `202` to world zone `219`; those route
claims still need live client/runtime evidence. Interactive client smoke is
still needed for Durek dialog, Burning Torch CSI behavior, Skeech Hut checklist
and visual state, reward-choice and achievement UI, wrong-world/repeat negative
cases, and the `Q3886->Q3673` flow.

Supplemental update: 2026-06-18 F-022 Q3673 validation-gate recheck.
Q3673 (`Contact with Thayd`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it sixth as
`quest_validation_ready` with `30` blocker rows, objectives
`4748;4888;4889;4890;13391`, rewards
`2187;2188;2189;2481;3001;3846`, achievement `3490`, and script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3673ContactWithThayd.cs`.
The local WildStar client dependency was found at
`I:\WildStar\Client64\WildStar64.exe`, so the remaining blocker is not a
missing executable. A fresh row-specific worksheet was created with
`-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-005717-quest-3673-contact-with-thayd-recheck`;
it records the automated coverage, the present client executable, the
unmatched finisher relation, and the exact interactive smoke still required.
Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3673"`
with `26/26` tests, covering Signal Flare checklist completion to hidden
objective `13391`, incomplete-checklist rejection, duplicate callback
suppression, Achieved-vs-Completed cinematic/follow-up behavior, Deadeye
receiver indexing, selectable rewards, achievement hook `3490`, and map-load
fallback spawns for Signal Flares `12521`, `13150`, and `13151`. Harness preset
tests passed `2/2`, and generated content-retail validation passed for `31`
files / `168,101` rows. Runtime promotion remains blocked on reviewed current
Creature2 bridge evidence for source creature `23645`: anchored DataMapping
searches still show `23645` as `Unknown` / `unmatched` with zero candidates,
including the Q3673 finisher relation `1842`. Interactive client smoke is still
needed for Deadeye dialog/receiver flow, Signal Flare activation and CSI
visibility, hidden completion/cinematic timing, reward-choice and achievement
UI, wrong-world/repeat negative cases, and the `Q3886->Q3673->Q3670` chain.

Supplemental update: 2026-06-18 F-022 Q3668 validation-gate recheck.
Q3668 (`Indigenous Intelligence`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it fifth as
`quest_validation_ready` with `39` blocker rows, objectives `4744;4745;4791`,
rewards `2190;2191;4768`, achievement `3490`, and script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3668.cs`. The local
WildStar client dependency was found at `I:\WildStar\Client64\WildStar64.exe`,
so the remaining blocker is not a missing executable. A fresh row-specific
worksheet was created with `-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-005208-quest-3668-indigenous-intelligence-recheck`;
it records the automated positive coverage, missing placement evidence for
TargetGroup members `14054` and `11913`, and the exact interactive smoke still
required. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3668"`
with `14/14` tests, covering target-group expansion/credit for `14054;11913`
when present, current fallback spawns for Bartol, Lusk, imprisoned survivor,
and reviewed Skeech rows, selectable rewards, and achievement hook `3490`.
Harness preset tests passed `2/2`, and generated content-retail validation
passed for `31` files / `168,101` rows. Runtime promotion remains blocked on
reviewed current Creature2/source-coordinate evidence for `14054` and `11913`;
anchored DataMapping searches still show `14054` only as Creature2 `25857`
(`Enhanced Travel Speed Station`) in world `1061` / zone `2083`, while `11913`
has no anchored mapping/quest relation hit. Interactive client smoke is still
needed for Deadeye/Bartol/Lusk dialog, survivor activation, Skeech combat and
tap behavior, pushed item `6912`, reward-choice and achievement UI, negative
cases, and the `Q3486->Q3671->Q3668` chain.

Supplemental update: 2026-06-18 F-022 Q3486 validation-gate recheck.
Q3486 (`Empowered Tower`) remains server-implemented but not retail-complete.
The generated next-slice queue still ranks it fourth as `quest_validation_ready`
with `39` blocker rows, objectives `4987;4485`, rewards `1706;1707;2147`,
achievements `3469;5327`, and script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3486EmpoweredTower.cs`.
The local WildStar client dependency was found at
`I:\WildStar\Client64\WildStar64.exe`, so the remaining blocker is not a
missing executable. A fresh row-specific worksheet was created with
`-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-004908-quest-3486-empowered-tower-recheck`;
it records the automated coverage, the present client executable, the retained
`WIP/GUESSED` boundaries, and the exact interactive smoke still required.
Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3486"`
with `34/34` tests, covering arrival story/objective credit, Loftite crystal
collection and duplicate guards, Crystal Guardian/Frostbite virtual collect
credit, Master Control Panel route, selectable rewards/cash, achievement
hooks, Q3671/Q3797 mentions, and Q3797 prerequisite gating. Harness preset
tests passed `2/2`, and generated content-retail validation passed for `31`
files / `168,101` rows. Runtime promotion remains blocked on interactive
client smoke for Master Control Panel dialog, arrival story-panel timing,
Loftite crystal collision/visual state, Crystal Guardian/Frostbite combat and
tap behavior, reward-choice and achievement UI, repeat-completion negatives,
and the `Q3667->Q3486->Q3797` flow. The DataMapping/Jabbithole source reward
row `5319` remains provenance-only: it maps Item2 `1377` to source
`game_reward_id` `2147`, while build 16042 canonical Quest2Reward `1707`
grants Item2 `1377` and canonical Quest2Reward `2147` grants Item2 `14102`.

Supplemental update: 2026-06-18 F-022 Q3480 validation-gate recheck.
Q3480 (`Reporting for Duty`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it third as
`quest_validation_ready` with `32` blocker rows, objectives `4470;4565`,
rewards `1676;1677;1678`, achievement `5327`, and script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3480.cs`. The local
WildStar client dependency was found at `I:\WildStar\Client64\WildStar64.exe`,
so the remaining blocker is not a missing executable. A fresh row-specific
worksheet was created with `-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-004645-quest-3480-reporting-for-duty-recheck`;
it records the automated coverage, the present client executable, and the
exact interactive smoke still required. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3480"`
with `36/36` tests, covering Commander Durek accept gating, Q3479 branch
exclusion, Trapped Survivor CSI credit for objective `4470`, shared-yeti
TargetGroup credit for objective `4565`, Deadeye Brightland receiver
completion, selectable rewards/cash, achievement hook `5327`, and Q3667
follow-up. Harness preset tests passed `2/2`, and generated content-retail
validation passed for `31` files / `168,101` rows. Runtime promotion remains
blocked on interactive client smoke for Commander Durek and Deadeye dialogs,
survivor CSI packets/UI, shared-yeti combat/tap/respawn behavior,
reward-choice and achievement UI, alternate receiver or prerequisite routing,
wrong-world and repeat-completion negative cases, and the end-to-end Q3480
client flow.

Supplemental update: 2026-06-18 F-022 Q3479 validation-gate recheck.
Q3479 (`From the Wreckage`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it second as
`quest_validation_ready` with `44` blocker rows, objectives `4467;4564`,
rewards `2476;2479;8569`, achievements `1432;1433;1434`, and script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3479FromTheWreckage.cs`.
The local WildStar client dependency was found at
`I:\WildStar\Client64\WildStar64.exe`, so the remaining blocker is not a
missing executable. A fresh row-specific worksheet was created with
`-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-004411-quest-3479-from-the-wreckage-recheck`;
it records the automated coverage, the present client executable, and the
exact interactive smoke still required. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3479"`
with `38/38` tests, covering Bosun Redmark accept gating, Q3480 branch
exclusion, Trapped Survivor CSI credit for objective `4467`, shared-yeti
TargetGroup credit for objective `4564`, Deadeye Brightland receiver
completion, selectable rewards/cash, achievement hooks, and Q3667 follow-up.
Harness preset tests passed `2/2`, and generated content-retail validation
passed for `31` files / `168,101` rows. Runtime promotion remains blocked on
interactive client smoke for Bosun and Deadeye dialogs, survivor CSI packets
and UI, shared-yeti combat/tap/respawn behavior, reward and achievement UI,
alternate receiver or prerequisite routing, wrong-world and repeat-completion
negative cases, and the `Q3479->Q3667` chain.

Supplemental update: 2026-06-18 F-022 Q5597 validation-gate recheck.
Q5597 (`Dregs and Thieves`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it first as
`quest_validation_ready` with `32` blocker rows, objective `8256`, rewards
`3000;5236`, achievement `4134`, and script owner
`Source\NexusForever.Script.Main\Quests\CrimsonIsle\Q5597.cs`. The local
WildStar client dependency was found at `I:\WildStar\Client64\WildStar64.exe`,
so the remaining blocker is not a missing executable. A fresh row-specific
worksheet was created with `-CreateBundleOnly` at
`artifacts\blocker_evidence\20260618-004018-quest-5597-dregs-and-thieves-recheck`;
it records the automated coverage, the present client executable, and the
exact interactive smoke still required. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5597"`
with `15/15` tests, covering visible Mondo accept/completion gates, Chua
Explosives `VirtualCollect` item `364` credit for objective `8256`, one-shot
activation/removal, all reviewed fallback placements, fixed rewards `3000` and
`5236`, and Bloodstone achievement `4134` progression. Harness preset tests
passed `2/2`, and generated content-retail validation passed for `31` files /
`168,101` rows. Runtime promotion remains blocked on interactive client smoke
for Mondo Zax dialog, Chua Explosives activation/UI, reward UI and inventory
persistence, achievement toast/log UI, wrong-world/objective rejection,
duplicate-completion behavior, and the `Q5596->Q5597->Q5604` chain.

Supplemental update: 2026-06-17 F-022 Q3797 validation-gate recheck and evidence bundle.
Q3797 (`Securing the Area`) remains server-implemented but not retail-complete.
The generated next-slice queue still ranks it as `quest_validation_ready` with
`23` blocker-detail rows, script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3797SecuringTheArea.cs`,
objective `4918`, selectable rewards `2480;2482;4770`, and
`datamapping_review` as the blocking evidence source. Focused verification
passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3797"`
with `22/22` tests, covering TargetGroup `1177` nested creature-list expansion,
generic `KillTargetGroups` credit for members
`11945;11948;11962;11963;12212;12213`, Durek giver/receiver routing, Q3486
prerequisite and visible-Durek accept guards, selectable rewards
`2480;2482;4770`, and compatibility receiver indexing. The row-specific bundle
skeleton
`artifacts\blocker_evidence\20260617-220423-quest-3797-securing-the-area`
records world `426`, objective `4918`, rewards `2480;2482;4770`, TargetGroup
`1177`, target members `11945;11948;11962;11963;12212;12213`, reviewed
rootbrute/yeti source-coordinate ids
`8272460;8272461;8272462;8272464;3837530;3785147;8122314;8122316`, Durek
Creature2 `11066`, receiver WorldLocation2 `7727`, compatibility Durek `11061`,
Jabbithole objective evidence row `732`, partial zone rows `96;166`, and
negative cases for missing objective, wrong world/objective, and repeat
interaction after completion. Runtime promotion is still blocked on reviewed
current zone bridge evidence for partial rows `96;166`, interactive client smoke
for Durek dialog/activation and Q3486 prerequisite flow, live combat proof for
rootbrute/yeti placement density, respawn, tap, and threat behavior, type-9
non-spawned target branch review, reward UI/inventory persistence, objective
text/order reconciliation for row `732`, and end-to-end Q3797 validation.

Supplemental update: 2026-06-17 F-022 Q5580 validation-gate recheck and evidence bundle.
Q5580 (`Enforced Radio Silence`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it as
`quest_validation_ready` with `26` blocker-detail rows, script owner
`Source\NexusForever.Script.Main\Quests\CrimsonIsle\Q5580.cs`, objective
`8227`, selectable rewards `3005;3007;3008;3009;3010;3011`, achievement hook
`4135`, and `client_smoke` as the blocking evidence source. Focused verification
passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5580"`
with `2/2` tests, covering Tower Controls fallback spawn/index/activeProp
coverage, visible Kezrek receiver routing, Q5604 granting Q5580/Q5583, and the
Q5594 merge gate remaining locked until both Q5580 and Q5583 complete. The
row-specific bundle skeleton
`artifacts\blocker_evidence\20260617-220251-quest-5580-enforced-radio-silence`
records world `870`, objective `8227`, rewards
`3005;3007;3008;3009;3010;3011`, achievement `4135`, Tower Controls
source-coordinate ids `15122;15123`, activeProp ids `1137353;1137404`, Kezrek
Warbringer source-coordinate ids `15181;170390`, Jabbithole objective evidence
row `1452`, zone rows `309;620`, episode quest `2389`, and negative cases for
missing objective, wrong world/objective, and repeat interaction after
completion. Runtime promotion is still blocked on interactive client smoke for
Tower Controls activation/UI and active-prop visual despawn/respawn behavior,
Kezrek Warbringer dialog and starter-slot behavior, reward and achievement UI,
objective text/order reconciliation for Jabbithole objective `1452`, zone
bridge/routing review for generated rows `309;620`, episode ordering/progression
UI, and the `Q5604->Q5580/Q5583->Q5594` branch flow.

Supplemental update: 2026-06-17 F-022 Q5575 validation-gate recheck and evidence bundle.
Q5575 (`Seizing Power`) remains server-implemented but not retail-complete. The
generated next-slice queue still ranks it as `quest_validation_ready` with `25`
blocker-detail rows, script owner
`Source\NexusForever.Script.Main\Quests\CrimsonIsle\Q5575.cs`, objectives
`8523;8371;12871`, selectable rewards `2993;2994;2995`, achievement hook
`4133`, and `client_smoke` as the blocking evidence source. Focused verification
passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5575"`
with `3/3` tests, covering Power Regulator completion credit, hidden objective
`12871`, achieved-state duplicate guard, and Q5596 branch grant. The
row-specific bundle skeleton
`artifacts\blocker_evidence\20260617-220052-quest-5575-seizing-power`
records world `870`, objectives `8523;8371;12871`, rewards `2993;2994;2995`,
achievement `4133`, Kezrek Warbringer source-coordinate ids `15181;170390`,
reviewed Power Regulator source-coordinate ids `15177;15176;15178`, and
negative cases for missing objective, wrong world/objective, and repeat
interaction after completion. Runtime promotion is still blocked on interactive
client smoke for Kezrek Warbringer spawn/dialog, Megatech Shield Generator
objective trigger timing, Power Regulator activation/UI and active-prop visual
despawn/respawn behavior, hidden-objective timing, reward and achievement UI,
objective text/order reconciliation for Jabbithole objectives `2703;2704`, zone
bridge/routing review for generated rows `810;1315`, and the
`Q5575->Q5596` branch flow.

Supplemental update: 2026-06-17 F-022 Q5573 validation-gate recheck and evidence bundle.
Q5573 (`Powering Down`) remains server-implemented but not retail-complete. The
generated next-slice queue still ranks it as `quest_validation_ready` with `31`
blocker-detail rows, script owner
`Source\NexusForever.Script.Main\Quests\CrimsonIsle\Q5573PoweringDown.cs`,
objectives `8524;8229;12870`, selectable rewards `2989;2991;2992`,
achievement hook `4132`, and `client_smoke` as the blocking evidence source.
Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5573"`
with `3/3` tests, covering the Power Regulator completion cinematic, hidden
objective `12870`, achieved-state duplicate guard, and Q5596 branch grant.
The row-specific bundle skeleton
`artifacts\blocker_evidence\20260617-215735-quest-5573-powering-down`
records world `870`, objectives `8524;8229;12870`, rewards `2989;2991;2992`,
achievement `4132`, reviewed Power Regulator source-coordinate ids
`15177;15176;15178`, unpromoted Mondo Zax source-coordinate ids
`15087;15088;15089;21174;136782;1863809;2480207;2561752`, and negative cases
for missing objective, wrong world/objective, and repeat interaction after
completion. Runtime promotion is still blocked on interactive client smoke for
Mondo Zax accept/completion dialog, Megatech Shield Generator arrival/objective
trigger timing, Power Regulator activation/UI and active-prop visual
despawn/respawn behavior, cinematic/hidden-objective timing, reward and
achievement UI, objective text/order reconciliation for Jabbithole objectives
`1450;1451`, zone bridge/routing review for generated rows `308;2122;627`,
and the `Q5573->Q5596` branch flow.

Supplemental update: 2026-06-17 F-022 Q3963 validation-gate recheck and evidence bundle.
Q3963 (`More Important Than Revenge`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it as
`quest_validation_ready` with `21` blocker-detail rows, script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3963MoreImportantThanRevenge.cs`,
objective `5201`, selectable rewards `3706;3707;4771`, achievement hook
`3491`, and `datamapping_review` as the blocking evidence source. Focused
verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3963|FullyQualifiedName=NexusForever.Game.Tests.Transport.WorldLocationTeleporterEntityScriptTests.OnActivateSuccess_WithShipControlsOnNorthernWilds_DoesNotTeleport"`
with `16/16` tests, covering the Q3487 handoff, Ship Controls `27196`
ActivateEntity credit on Northern Wilds world `426`, the non-teleport guard for
reused Ship Controls teleporter data, Galeras Deadeye `16622` receiver
completion, selectable rewards, and achievement hooks. The row-specific bundle
skeleton
`artifacts\blocker_evidence\20260617-215544-quest-3963-more-important-than-revenge`
records worlds `426;51`, objective `5201`, rewards `3706;3707;4771`,
achievement `3491`, the `11063` starter-context caveat, unmatched source
relation `starter:2130:23645`, and negative cases for missing objective, wrong
world/objective, and repeat interaction after completion. Runtime promotion is
still blocked on zone bridge/routing review for generated rows `67;1888;259`,
client-visible proof for whether Deadeye `11063` directly offers Q3963 or only
participates as Q3487 chain context, reviewed current Creature2 evidence for
unmatched source relation `2130` / Jabbithole creature `23645`, and interactive
client smoke for Ship Controls activation packet/timing, Q3487-to-Q3963 flow,
Galeras Deadeye completion dialog, reward and achievement UI, and end-to-end
Q3963 validation.

Supplemental update: 2026-06-17 F-022 Q3886 validation-gate recheck and evidence bundle.
Q3886 (`Fiery Distraction`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it as
`quest_validation_ready` with `23` blocker-detail rows, script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3886FieryDistraction.cs`,
objectives `5053;5052`, selectable rewards `2192;2430;4767`, achievement hook
`3490`, and `datamapping_review` as the blocking evidence source. Focused
verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3886"`
with `11/11` tests, covering the Land's Reach Commander Durek starter/receiver
route, Burning Torch SucceedCSI credit, Skeech Hut checklist credit, selectable
rewards, achievement hooks, and Q3673 follow-up handoff. The row-specific
bundle skeleton
`artifacts\blocker_evidence\20260617-215359-quest-3886-fiery-distraction`
records world `426`, objectives `5053;5052`, rewards `2192;2430;4767`,
achievement `3490`, duplicate objective relation `8506`, zone-review rows
`76;5459;235;3439`, and negative cases for missing objective, wrong
world/objective, and repeat interaction after completion. Runtime promotion is
still blocked on zone bridge/routing review for the generated zone rows,
duplicate Burning Torch relation placement review, and interactive client smoke
for Durek accept/completion dialog, torch activate/CSI packet behavior, hut
checklist activation and visual burn/despawn/respawn behavior, reward and
achievement UI, and the `Q3886->Q3673` flow.

Supplemental update: 2026-06-17 F-022 Q3673 validation-gate recheck and evidence bundle.
Q3673 (`Contact with Thayd`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it as
`quest_validation_ready` with `30` blocker-detail rows, script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3673ContactWithThayd.cs`,
objectives `4748;4888;4889;4890;13391`, selectable rewards
`2187;2188;2189;2481;3001;3846`, achievement hook `3490`, and
`client_smoke` as the blocking evidence source. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3673"`
with `26/26` tests, covering Signal Flare checklist and CSI credit, hidden
objective `13391` duplicate suppression, achieved-state cinematic queuing,
completion handoff to Q3670, Deadeye receiver coverage, selectable rewards,
and achievement hooks. A targeted DataMapping recheck confirms
`Tools\DataMapping\output\creature_map.csv` keeps Jabbithole creature `23645`
as `Unknown` / `match_status unmatched` / `candidate_count 0`, and
`Tools\DataMapping\output\creature_quest_map.csv` keeps Q3673 finisher
`source_relation_id 1842` on that unmatched source. The row-specific bundle
skeleton
`artifacts\blocker_evidence\20260617-215214-quest-3673-contact-with-thayd`
records worlds `426;51`, objectives `4748;4888;4889;4890;13391`, rewards
`2187;2188;2189;2481;3001;3846`, achievement `3490`, unmatched relation
`finisher:1842:23645`, and negative cases for missing objective, wrong
world/objective, and repeat interaction after completion. Runtime promotion is
still blocked on interactive client smoke for Deadeye accept/completion dialog,
Signal Flare activation/CSI packets and visibility, hidden completion/cinematic
timing, reward and achievement UI, alternate receiver/prerequisite routing, the
unmatched source relation `1842` review, and the `Q3886->Q3673->Q3670` flow.

Supplemental update: 2026-06-17 F-022 Q3668 validation-gate recheck and evidence bundle.
Q3668 (`Indigenous Intelligence`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it as
`quest_validation_ready` with `39` blocker-detail rows, script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3668.cs`, objectives
`4744;4745;4791`, selectable rewards `2190;2191;4768`, pushed item `6912x0`,
achievement hook `3490`, and `datamapping_review` as the blocking evidence
source. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3668"`
with `14/14` tests, covering nested TargetGroup `7293` expansion, blocked
member credit for `14054`/`11913`, receiver/objective fallbacks, selectable
rewards, and achievement hooks. A targeted evidence recheck found
`Tools\DataMapping\output\creature_map.csv` maps source creature `14054` to
Creature2 `25857` (`Enhanced Travel Speed Station`) in world `1061` /
WorldZone `2083`, with spawn rows `443894`, `1425310`, and `7150035`; this is
not reviewed Q3668 Coldburrow kill-target placement evidence. Exact
`creature_spawn_map.csv` / `creature_map.csv` searches found no `11913`
creature or spawn-map row. The row-specific bundle skeleton
`artifacts\blocker_evidence\20260617-215006-quest-3668-indigenous-intelligence`
records world `426`, objectives `4744;4745;4791`, rewards `2190;2191;4768`,
achievement `3490`, blocked target members `14054;11913`, and negative cases
for missing objective, wrong world/objective, and repeat interaction after
completion. Runtime promotion is still blocked on reviewed current
Creature2/source-coordinate evidence for target members `14054` and `11913`,
zone bridge review for the remaining generated blocker rows, and interactive
client smoke for Deadeye/Lusk/Bartol dialogs, survivor activation/transform
timing, Skeech combat/tap/respawn behavior, pushed item/reward/achievement UI,
and the `Q3486->Q3671->Q3668` chain.

Supplemental update: 2026-06-17 F-022 Q3486 validation-gate recheck and evidence bundle.
Q3486 (`Empowered Tower`) remains server-implemented but not retail-complete.
The generated next-slice queue still ranks it as `quest_validation_ready` with
`39` blocker-detail rows, script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3486EmpoweredTower.cs`,
objectives `4987;4485`, selectable rewards `1706;1707;2147`, achievement
hooks `3469;5327`, and `client_smoke` as the blocking evidence source. Focused
verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3486"`
with `34/34` tests, covering arrival/story credit, Loftite crystal collection,
Crystal Guardian/Frostbite virtual-item credit, Master Control Panel
starter/receiver fallback, selectable rewards/cash, achievement hooks,
completion cinematic, and Q3671/Q3797 mention behavior. The row-specific bundle
skeleton
`artifacts\blocker_evidence\20260617-214714-quest-3486-empowered-tower`
records world `426`, objectives `4987;4485`, rewards `1706;1707;2147`,
achievements `3469;5327`, and negative cases for missing objective, wrong
world/objective, and repeat interaction after completion. Runtime promotion is
still blocked on interactive client smoke for panel accept/completion dialogs,
arrival trigger/story-panel presentation, Loftite crystal collision and respawn
timing, Crystal Guardian and Frostbite combat/tap/respawn/drop behavior,
reward and achievement UI, duplicate and negative cases, and the
`Q3667->Q3486->Q3797` flow. The DataMapping/Jabbithole source reward row
`5319` remains explicitly provenance-only because it maps Item2 `1377` to
source `game_reward_id` `2147`, colliding with the canonical build 16042
Quest2Reward row `2147` for Item2 `14102`; do not use that source id as
canonical reward proof until the mismatch is reconciled.

Supplemental update: 2026-06-17 F-022 Q3480 validation-gate recheck and evidence bundle.
Q3480 (`Reporting for Duty`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it as
`quest_validation_ready` with `32` blocker-detail rows, script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3480.cs`, objectives
`4470;4565`, selectable rewards `1676;1677;1678`, achievement hook `5327`,
and `client_smoke` as the blocking evidence source. Focused verification
passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3480"`
with `36/36` tests, covering the Commander Durek accept gate, Q3479 mutual
exclusion, Trapped Survivor CSI credit, shared-yeti target-group credit,
Deadeye Brightland receiver path, selectable rewards, cash reward, achievement
hooks, and follow-up handoff to Q3667. The row-specific bundle skeleton
`artifacts\blocker_evidence\20260617-214531-quest-3480-reporting-for-duty`
records worlds `426;51`, objectives `4470;4565`, rewards `1676;1677;1678`,
achievement `5327`, and negative cases for missing objective, wrong
world/objective, and repeat interaction after completion. Runtime promotion is
still blocked on interactive client smoke for Commander Durek accept dialog,
Deadeye completion dialog, survivor CSI packets/UI, shared-yeti
combat/tap/respawn behavior, reward and achievement UI, alternate receiver or
prerequisite routing, the unmatched older starter relation review, duplicate
and negative cases, and the end-to-end Q3480 flow.

Supplemental update: 2026-06-17 F-022 Q3479 validation-gate recheck and evidence bundle.
Q3479 (`From the Wreckage`) remains server-implemented but not
retail-complete. The generated next-slice queue still ranks it as
`quest_validation_ready` with `44` blocker-detail rows, script owner
`Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3479FromTheWreckage.cs`,
objectives `4467;4564`, selectable rewards `2476;2479;8569`, achievement
hooks `1432;1433;1434`, and `datamapping_review` as the blocking evidence
source. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3479"`
with `38/38` tests, covering the Bosun Redmark accept gate, Trapped Survivor
CSI credit, shared-yeti target-group credit, Deadeye Brightland receiver path,
selectable rewards, cash reward, and quest-completion achievement hooks. The
row-specific bundle skeleton
`artifacts\blocker_evidence\20260617-214306-quest-3479-from-the-wreckage`
records worlds `426;51`, objectives `4467;4564`, rewards `2476;2479;8569`,
achievements `1432;1433;1434`, and negative cases for missing objective, wrong
world/objective, and repeat interaction after completion. Runtime promotion is
still blocked on reviewed current zone/creature bridge evidence for the
remaining generated blocker rows plus interactive client smoke for Bosun and
Deadeye dialogs, survivor CSI packets/UI, shared-yeti combat/tap/respawn
behavior, reward and achievement UI, mutual exclusion with Q3480, and the
`Q3479->Q3667` chain.

Supplemental update: 2026-06-17 F-022 Q5597 validation-gate recheck and evidence bundle.
Q5597 (`Dregs and Thieves`) remains server-implemented but not
retail-complete. Focused verification passed
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5597"`
with `15/15` tests. The local runtime stack was brought to an evidence-ready
state without restarting existing services by running
`Tools\Setup\Start-NexusForeverLocal.ps1 -ClientDirectory "I:\WildStar" -SkipSetup -RestartExistingServers:$false -SkipClientLaunch -EnableClientConsole -EnableClientLogging -LogLevel Trace`, and `127.0.0.1:23115`,
`127.0.0.1:6600`, and `127.0.0.1:24000` were verified reachable afterward.
The row-specific bundle skeleton
`artifacts\blocker_evidence\20260617-213642-quest-5597-dregs-and-thieves`
records quest `5597`, world `870`, objective `8256`, fixed rewards
`3000;5236`, achievement `4134`, and the negative cases for missing objective,
wrong world/objective, and repeat interaction after completion. Runtime
promotion is still blocked on interactive client smoke for Mondo Zax dialog,
Chua Explosives activation/UI, reward UI and inventory persistence,
achievement toast/log UI, duplicate-completion behavior, and the
`Q5596->Q5597->Q5604` chain. This pass also fixed the evidence harness tail
instructions so a root install path such as `I:\WildStar` prints
`I:\WildStar\Logs` and `I:\WildStar\Errors` instead of the parent drive root.
The content-retail validator now normalizes `None` CSV cells in required
columns to ordinary blank-column validation errors instead of crashing before
it can report the offending row. Focused harness tests passed `2/2`, validator
unit tests passed `34/34`, and the generated content-retail validation passed
for `31` CSVs / `168,101` rows, all `not_retail_complete`.

Supplemental update: 2026-06-17 F-022 quest-chain episode blocker evidence.
The generated quest-episode coverage now gives the `1,175` matched
EpisodeQuest rows row-specific blocker text. Each matched row names the
EpisodeQuest id, Quest2 id, order, flags, current Episode id/name, WorldZone
id/name, matched Jabbithole episode id/name, slug, episode quest count, enabled
flag, and last-seen marker. These rows remain not-retail-complete until runtime
episode ordering/progression semantics, quest visibility and turn-in state, UI
presentation, and quest-chain smoke are verified for the row. The `313`
missing-episode-bridge rows and `9` non-current Quest2 rows remain explicitly
blocked. No runtime code queries `wildstar_client`, `jabbithole`, or
`nf_map_*`. Verification passed the focused quest-episode audit unit test, full
content-retail regeneration, and generated-output validation (`31` CSVs /
`168,011` rows, all `not_retail_complete`).

Supplemental update: 2026-06-17 F-022 quest-loot blocker evidence.
The generated quest-loot coverage now gives all `7,650` LaughingWS/runtime
loot rows row-specific blocker text: `5,296` entity-to-loot-group bindings,
`1,168` loot groups, `1,168` loot items, and three `6`-row orphaned rejection
buckets whose current client QuestObjective bridge is absent. Blockers now name
the loot group, entity id, item type/staticId, QuestObjective id, condition
type/id, probability, count range, and source comment. These rows remain
not-retail-complete until source spawn/encounter timing, objective-trigger
ownership, drop cadence, inventory persistence/cleanup, and client loot smoke
are verified together. No runtime code queries `wildstar_client`, `jabbithole`,
or `nf_map_*`. Verification passed the focused quest-loot audit unit test, full
content-retail regeneration, and generated-output validation (`31` CSVs /
`168,011` rows, all `not_retail_complete`).

Supplemental update: 2026-06-17 F-022 quest achievement/checklist blocker evidence.
The generated quest-achievement coverage now gives the `2,045` mapped
AchievementChecklist rows row-specific blocker text. Default achievement and
checklist blockers name the Achievement id/title/type/category/flags,
WorldZone, progress text, source field, Checklist id, bit, object/objectAlt ids,
achievement and checklist prerequisites, parent tier, and character-title
reward where present. Runtime-tested checklist rows keep their custom evidence.
No runtime code queries `wildstar_client`, `jabbithole`, or `nf_map_*`.
Verification passed the focused achievement audit unit test, full
content-retail regeneration, and generated-output validation after this note
(`31` CSVs / `168,011` rows / `2,469` blocker detail rows, all
`not_retail_complete`).

Supplemental update: 2026-06-17 F-022 quest world-dependency blocker evidence.
The generated quest world-dependency coverage now gives the large generic
world/interaction families row-specific blockers while preserving custom
runtime-backed evidence. The updated rows include `12,635` target-group
members, `6,884` objective indicator locations, `4,954` quest WorldZone
dependencies, `4,467` target groups, `3,738` QuestDirectionEntry locations,
`3,234` receiver locations, `1,390` nested target groups, `1,122`
QuestDirection rows, `334` alternate receivers, and `107` inactive direction
locations. Blockers now name Quest2 ids, objective ids/types, TargetGroup ids
and member slots, WorldLocation2/WorldZone ids, coordinates/radius,
QuestDirection/entry ids, prerequisites, and missing-row fail-closed evidence.
No runtime code queries `wildstar_client`, `jabbithole`, or `nf_map_*`.
Verification passed the focused world-dependency audit unit test, full
content-retail regeneration, and generated-output validation (`31` CSVs /
`167,952` rows, all `not_retail_complete`).

Supplemental update: 2026-06-17 F-022 quest-prerequisite blocker evidence.
The generated quest-prerequisite coverage now gives all `14,965` prerequisite
rows row-specific blocker text. Rows name the exact Quest2 id plus level gates,
`questPlayerFactionEnum`, required/excluded quest ids, faction-level slots,
preq item ids, Quest2 `preq_flags`, Prerequisite row ids/flags/failure text,
and Prerequisite slot `prerequisiteTypeId`/object/value/comparison data. The
`2` missing Prerequisite rows still fail closed at runtime and remain blocked
until the current client row or reviewed replacement evidence is recovered. No
runtime code queries `wildstar_client`, `jabbithole`, or `nf_map_*`.
Verification passed the focused prerequisite audit unit test, full
content-retail regeneration, and quiet generated-output validation (`31` CSVs /
`167,952` rows, all `not_retail_complete`).

Supplemental update: 2026-06-17 F-022 quest-reward bridge blockers.
The generated quest-reward coverage now gives matched Jabbithole reward rows
row-specific source/grant blockers: `3,228` item, `2,273` inline cash, `2,422`
reputation, `365` currency, and `307` tradeskill rows name the source reward
id, Quest2 id, Jabbithole reward object id/name, current client reward object
id/name, Quest2Reward id when present, amount, and `fixed_reward` value.
Evidence now cites the reward object table for each kind (`Item2`,
`CurrencyType`, `Faction2`, or `Tradeskill`), and the `17` non-current reward
rows cite DataMapping review guidance. These rows remain not-retail-complete
until fixed/choice semantics, grant persistence, reward UI, and client quest
smoke are verified. No runtime code queries `wildstar_client`, `jabbithole`, or
`nf_map_*`. Verification passed the focused reward-evidence audit unit test,
full content-retail regeneration, and generated-output validation (`31` CSVs /
`167,909` rows, all `not_retail_complete`).

Supplemental update: 2026-06-17 F-022 matched quest-objective bridge blockers.
The generated quest-objective coverage now gives the `2,113` matched
Jabbithole objective rows row-specific blocker text. Each row names the
Jabbithole objective id, Quest2 id, objective order, source objective text,
current QuestObjective id, and current objective type/data/count. These rows
are mapped but remain not-retail-complete until text/order reconciliation,
runtime trigger ownership, objective-credit behavior, and client quest smoke
are verified against the current QuestObjective row. No runtime code queries
`wildstar_client`, `jabbithole`, or `nf_map_*`. Verification passed the focused
objective-evidence audit unit test, full content-retail regeneration, and
generated-output validation (`31` CSVs / `167,909` rows, all
`not_retail_complete`).

Supplemental update: 2026-06-17 F-022 matched quest-zone runtime blockers.
The generated quest-zone coverage now gives the `6,167` matched zone rows
row-specific runtime blockers. Each row names the `quest_zones` or
`quest_call_zones` source relation, source row id, Quest2 id, Jabbithole zone
id/name, current WorldZone id/name, and `match_status matched`. These rows are
mapped but remain not-retail-complete until runtime quest visibility/routing,
map guidance, episode/zone ordering, and client quest smoke are verified
against the current WorldZone bridge. No runtime code queries
`wildstar_client`, `jabbithole`, or `nf_map_*`. Verification passed the focused
zone-evidence audit unit test, full content-retail regeneration, and
generated-output validation (`31` CSVs / `167,909` rows, all
`not_retail_complete`).

Supplemental update: 2026-06-17 F-022 quest-creature bridge blockers.
The generated quest-creature coverage now gives the `210` unmatched Creature2
bridge rows and `3,170` ambiguous Creature2 bridge rows row-specific missing
evidence. Non-overridden rows cite `creature_quest_map.csv`,
`creature_map.csv`, `creature_spawn_map.csv`, Jabbithole creature plus
relation SQL, current `Quest2`/`Creature2` client tables, and DataMapping review
guidance. The blocker now names the relation type, source relation id, Quest2
id, Jabbithole creature id/name, and candidate Creature2 id/name when present.
Unmatched rows are blocked on a reviewed current Creature2 bridge or reviewed
replacement relation; ambiguous rows are blocked on bridge disambiguation,
placement review, runtime spawn/dialog/objective-credit behavior, and
client/manual smoke. No runtime code queries `wildstar_client`, `jabbithole`,
or `nf_map_*`. Verification passed the focused quest-creature audit unit test,
full content-retail regeneration, and generated-output validation (`31` CSVs /
`167,909` rows, all `not_retail_complete`).

Supplemental update: 2026-06-17 F-022 partial quest-zone bridge blockers.
The `5,019` `datamapping_quest_zone_partial_match_pending_zone_review` rows now
carry row-specific missing-zone evidence instead of a generic zone-review
summary. Each generated row cites `Tools/DataMapping/output/quest_zone_map.csv`,
`jabbithole_mysql/quest_zones.sql`, `jabbithole_mysql/quest_call_zones.sql`,
and current `Quest2`/`WorldZone` client tables, and the blocker names the
source relation, Quest2 id, Jabbithole zone id/name, current WorldZone id, and
`match_status partial`. Runtime promotion remains blocked until a reviewed
current WorldZone bridge reconciles the Jabbithole zone/name with build 16042
WorldZone data, followed by quest visibility/routing, map guidance,
episode/zone ordering, and client quest smoke. Verification passed the focused
zone-evidence audit unit test, full content-retail regeneration, and
generated-output validation (`31` CSVs / `167,909` rows, all
`not_retail_complete`).

Supplemental update: 2026-06-16 F-022 missing episode bridge blockers.
The `313` `datamapping_episode_quest_missing_episode_bridge_blocked` rows now
carry row-specific missing-bridge evidence. Each row cites
`Tools/DataMapping/output/episode_quest_client_map.csv`,
`Tools/DataMapping/output/quest_episode_map.csv`,
`jabbithole_mysql/quest_episodes.sql`, and current `Episode`/`EpisodeQuest`/
`Quest2` client tables, and the blocker names the EpisodeQuest row, Quest2 id,
episode id/name, and quest order whose current Episode id has no
`quest_episode_map.csv` bridge. Runtime promotion remains blocked until the
current Episode/EpisodeQuest chain is reconciled with Jabbithole quest episode
evidence or a reviewed replacement bridge, followed by quest order, episode
visibility/progression, UI, and quest-chain smoke. Verification passed the
focused episode-evidence audit unit test, full content-retail regeneration, and
generated-output validation (`31` CSVs / `167,909` rows, all
`not_retail_complete`).

Supplemental update: 2026-06-16 F-022 unmatched Jabbithole objective bridge blockers.
The `2,385` `datamapping_jabbithole_objective_unmatched_blocked` rows now carry
row-specific missing-bridge evidence instead of a generic objective-mapping
summary. Each generated row cites `jabbithole_mysql/quest_objectives.sql`,
`Tools/DataMapping/output/quest_objective_map.csv`, and current
`Quest2`/`QuestObjective` client tables, and the blocker names the Jabbithole
objective id, Quest2 id, objective order, `match_status unmatched`, and the
missing current QuestObjective id. Runtime promotion remains blocked until a
current build Quest2 objective slot and QuestObjective row match the Jabbithole
objective, or a reviewed replacement bridge is recorded in DataMapping output
or review evidence plus runtime trigger/objective-credit/client smoke. No
runtime path queries `wildstar_client`, `jabbithole`, or `nf_map_*`.
Verification passed the focused objective-evidence audit unit test, full
content-retail regeneration, and generated-output validation (`31` CSVs /
`167,909` rows, all `not_retail_complete`).

Supplemental update: 2026-06-16 F-022 rotation-essence reward blocker evidence.
The `86` Quest2Reward type-10 rows are now blocked with concrete
rotation-evidence requirements instead of a generic reward caveat. The generated
quest reward coverage still classifies them as
`blocked_active_reward_rotation_schedule`, but their evidence now cites
`RewardRotationEssence.tbl.sql`, `QuestRewardType.RotationEssence`,
QuestManager's current ungranted path, reward-rotation account systems, and
the reward-rotation opcodes. Runtime promotion remains blocked until the active
RewardRotation schedule/content-context, entry-state mutation, reward key, grant
flags, and client claim timing are mapped from live capture or decompile.
Verification passed the focused quest-reward audit unit test, full
content-retail regeneration, and generated-output validation (`31` CSVs /
`167,909` rows, `2,440` queue blocker rows, all `not_retail_complete`).

Supplemental update: 2026-06-16 F-022 quest-loot orphan rejection closure.
The generated quest-loot coverage now rejects the `18` reviewed LaughingWS
quest-loot rows whose `conditionType = 8` points at QuestObjective ids that are
not owned by any current Quest2 objective column. The affected objective ids are
`15958`, `15988`, and `19193`; the rows are now classified as
`rejected_orphaned_loot_group_missing_current_client_quest_objective`,
`rejected_orphaned_loot_item_missing_current_client_quest_objective`, or
`rejected_orphaned_entity_loot_missing_current_client_quest_objective` instead
of being treated as promotion-ready reviewed loot. Runtime behavior remains on
runtime-owned loot tables and typed quest state only, with no runtime query
against `wildstar_client`, `jabbithole`, or `nf_map_*`. These rejected rows can
only be revived with current build Quest2/QuestObjective ownership evidence or
a reviewed replacement objective mapping plus source entity/drop smoke.
Verification passed the focused quest-loot audit unit test, full content-retail
regeneration, and generated-output validation (`31` CSVs / `167,903` rows, all
`not_retail_complete`).

Supplemental update: 2026-06-16 F-022 no-objective quest script lifecycle row coverage.
The generated quest-script coverage now promotes the current no-objective
Script.Main quest hooks with row-specific lifecycle evidence. Q3670 `Calm
Before the Storm`, Q3671 `Setting Up Camp`, and Q5610 `Dreg Mutations` are now
`implemented_no_objective_script_lifecycle_tests_pending_dialog_smoke`, backed
by the shared QuestManager tests that accept those Quest2 rows into Achieved
state and complete them at visible receivers where applicable. Runtime remains
typed Quest2/QuestManager driven and does not query `wildstar_client`,
`jabbithole`, or `nf_map_*`. These rows remain not retail-complete until
client-visible dialog activation, exact row-specific reward/achievement side
effects, and manual/client smoke are captured. Verification passed the
no-objective QuestManager lifecycle filter (`8/8`; one transient PDB copy retry
warning recovered), the focused quest-script audit unit test, full content-retail
regeneration, and generated-output validation (`31` CSVs / `167,903` rows, all
`not_retail_complete`).

Supplemental update: 2026-06-16 F-022 missing quest-direction-entry fail-closed coverage.
Quest objective guidance now has row-level fail-closed coverage for missing
QuestDirectionEntry rows. Runtime `Quest` direction resolution uses typed
GameTable `QuestDirection` / `QuestDirectionEntry` rows only and leaves
objective guidance unresolved when a referenced entry row is absent; it does
not query `wildstar_client`, `jabbithole`, or `nf_map_*`. The generated quest
world-dependency coverage now classifies `9` Q8208/Q8222 direction-entry
dependencies as
`blocked_missing_client_quest_direction_entry_row_runtime_fail_closed`, with
Q8208 `The Broken Pods` blocked on missing entries `880`-`884` from
QuestDirection `470`, and Q8222 `Before There Were Factions` blocked on
missing entries `893`-`896` from QuestDirection `475`. These rows remain not
retail-complete until the missing client QuestDirectionEntry rows or
replacement route evidence are recovered. Verification passed the focused
guidance regression test, the quest world-dependency audit unit test, full
content-retail regeneration/tracker sync, and generated-output validation
(`31` CSVs / `167,894` rows, all `not_retail_complete`).

Supplemental update: 2026-06-16 F-022 missing nested target-group fail-closed coverage.
Quest objective TargetGroup expansion now has focused fail-closed coverage for
missing nested child TargetGroup rows. Runtime `AssetManager` expansion uses
typed GameTable `TargetGroup` rows only and skips absent nested rows without
creating synthetic objective targets; it does not query `wildstar_client`,
`jabbithole`, or `nf_map_*`. The generated quest world-dependency coverage now
classifies `12` Q5306/Q5577 nested TargetGroup dependencies as
`blocked_missing_client_nested_target_group_row_runtime_fail_closed`, with
Q5306 `Breathing Room` blocked on missing child rows `61647`, `61645`, `61632`,
and `61630`, and Q5577 `Redmoon's Ruin` blocked on missing child rows `24396`
and `24397`. These rows remain not retail-complete until the missing client
TargetGroup rows or replacement member evidence are recovered. Verification
passed `AssetManagerTargetGroupTests` (`17/17`), the quest world-dependency
audit unit test, full content-retail regeneration, and generated-output
validation (`31` CSVs / `167,894` rows, all `not_retail_complete`).

Supplemental update: 2026-06-16 F-022 missing prerequisite fail-closed coverage.
Quest prerequisite evaluation now fails closed when a Quest2 prerequisite id is
absent from the loaded `Prerequisite` GameTable. `PrerequisiteManager` logs the
missing id and returns false instead of throwing, keeping runtime behavior on
typed GameTable/runtime-owned data and away from `wildstar_client`,
`jabbithole`, or `nf_map_*` queries. The generated prerequisite coverage rows
for Q3524 prerequisite `2755` and Q5052 prerequisite `6338` are now
`blocked_missing_client_prerequisite_row_runtime_fail_closed`; both remain
blocked for retail completion until the missing client rows or replacement
evidence are recovered. Focused verification passed the new prerequisite
fail-closed tests (`2/2`), the prerequisite audit unit test, and the content
retail validator (`31` generated CSVs / `167,894` rows with `2,436` blocker
detail rows, all `not_retail_complete`).

Supplemental update: 2026-06-16 F-022 next-slice queue verification.
The generated next-slice queue quests `Q5597`, `Q3479`, `Q3480`, `Q3486`,
`Q3668`, `Q3673`, `Q3886`, `Q3963`, `Q5573`, `Q5575`, `Q5580`, and `Q3797`
were rechecked as one server-side implementation slice. Current runtime coverage
uses only runtime-owned scripts, typed GameTable access, Quest2 rewards, generic
objective handlers, and reviewed DataMapping-promoted placements/caches: no
runtime path queries `wildstar_client`, `jabbithole`, or `nf_map_*`. The focused
filter passed `185/185`:
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5597|FullyQualifiedName~Q3479|FullyQualifiedName~Q3480|FullyQualifiedName~Q3486|FullyQualifiedName~Q3668|FullyQualifiedName~Q3673|FullyQualifiedName~Q3886|FullyQualifiedName~Q3963|FullyQualifiedName~Q5573|FullyQualifiedName~Q5575|FullyQualifiedName~Q5580|FullyQualifiedName~Q3797"`.
Treat this slice as server-implemented with explicit blockers rather than
retail-complete: client-visible dialog, objective timing/UI, reward
presentation and persistence, achievement UI, route/zone guidance, loot/drop
cadence, unmatched or partial DataMapping evidence rows, and end-to-end client
smoke remain tracked in the generated content-retail blocker rows.

Supplemental update: 2026-06-16 F-022 Q5597 row-boundary coverage.
Q5597 (`Dregs and Thieves`) now has focused xUnit coverage for its
row-proven Quest2 and Creature2 acceptance/completion boundaries. The tests
pin Mondo Zax (`Creature2 24187`) as the visible starter/receiver, Dominion
faction and level-1 gates, the completed Q5596 prerequisite, objective `8256`
as `VirtualCollect` item `364` with TargetGroup `4373`, fixed reward rows
`3000` and `5236`, and achievement `4134` checklist rows `5494`-`5496` for
Q5596->Q5597->Q5604. The shared Crimson Isle map coverage now names the Q5597
receiver slice and pins Mondo at `WorldLocation2 17803`. Q5597 remains not
retail-complete until client smoke verifies dialog, objective UI/timing, reward
presentation and persistence, achievement UI, exact routing/guidance, zone
bridge behavior, and the full Q5596->Q5597->Q5604 flow. The generated
DataMapping/Jabbithole reward/objective provenance mismatches and the
zero-row QuestDirection/guidance claim remain blocked and were not promoted
into runtime claims.

Supplemental update: 2026-06-13 F-022 Q5597 CollectableUnit Chua Explosives script binding.
Q5597 (`Dregs and Thieves`) now uses the retail-shaped collectable entity path
for its Chua Explosives runtime fallback. The reviewed DataMapping rows for
`Creature2 24286` carry entity type `8` (`CollectableUnit`), and
`CrimsonIsleMapScript` now creates `ICollectableUnitEntity` fallback objects
for all 30 reviewed Scarhide Camp placements. `CollectableUnitEntity` now
initializes collectable-owned scripts during runtime/model initialization, and
Q5597 binds the activation hook for both collectable owners and legacy
creature owners. Focused tests cover collectable activation credit/removal,
legacy creature compatibility, collectable fallback spawning, and collectable
script initialization. Q5597 remains not retail-complete until client smoke
verifies Chua Explosives activation/UI, exact respawn/despawn and multi-player
lifecycle behavior, Mondo dialog, reward presentation and inventory
persistence, achievement UI, and the full Q5596->Q5597->Q5604 flow.

Supplemental update: 2026-06-13 F-022 Q3963 activate-cast objective producer.
Q3963 (`More Important Than Revenge`) now has focused server-side coverage for
the build 16042 Ship Controls cast-activation path. `Creature2 27196` exposes
activate spell `32386` in client data, and `ClientActivateUnitCastHandler`
now allows ActivateEntity credit only for that Ship Controls creature on
Northern Wilds world `426`; the existing direct-activation path and
TargetGroup `7573` reward-pane expansion still cover objective `5201`.
Focused handler tests verify the positive Q3963/Northern Wilds case and the
negative same-creature/non-Northern-Wilds case. Q3963 remains not
retail-complete pending client-visible Ship Controls activation smoke,
Deadeye receiver dialog smoke, reward-choice UI, inventory persistence,
achievement UI/progression smoke, Q3487 -> Q3963 client flow smoke, and
end-to-end Q3963 validation.

Supplemental update: 2026-06-13 F-022 Q5573/Q5575 Power Regulator runtime restoration.
Q5573 (`Powering Down`) and Q5575 (`Seizing Power`) now have focused
server-side coverage for their shared build 16042 Power Regulator checklist
objective. `CrimsonIsleMapScript` removes the stale one-off `Creature2 24219`
Q5573 terminal fallback and spawns the three reviewed `Creature2 24999` Power
Regulator placements in world `870` / area `1217` from DataMapping
source_coordinate_ids `15176`, `15177`, and `15178`, with checklist indexes
`1`, `2`, and `3` plus activePropIds `5708188`, `5708196`, and `5708174` for
TargetGroup `2541`. Generic interaction/activate-spell
paths credit `ActivateTargetGroupChecklist`; Q5573 credits hidden objective
`12870` and queues the known cinematic after objective `8229`, while Q5575 now
credits hidden objective `12871` after objective `8371` without an unproven
cinematic. Both quests remain not retail-complete until client smoke verifies
activation/UI, active-prop visuals/despawn/respawn, reward/progression side
effects, and the end-to-end Q5573/Q5575->Q5596 branch flow.

Supplemental update: 2026-06-13 F-022 Q3479/Q3480 Trapped Survivor placement-density restoration.
Q3479 (`From the Wreckage`) objective `4467` and Q3480 (`Reporting for Duty`)
objective `4470` now have focused server-side map-load coverage for all `16`
unique-name-mapped DataMapping `Creature2 11070` Trapped Survivor placements in
world `426` / area `646`, expanding the earlier three-placement CSI fallback
slice. The promoted source_coordinate_ids are `3406`, `3407`, `3408`, `15032`,
`17868`, `40681`, `40682`, `40683`, `41559`, `42765`, `114996`, `114997`,
`115687`, `121680`, `156500`, and `156501`. Existing generic interaction
coverage still credits `QuestObjectiveType.SucceedCSI` for the interacted
creature, so the count-3 objective has server-side availability across the full
current reviewed placement set. Q3479/Q3480 remain not retail-complete until
client smoke verifies activate/CSI packets, survivor visibility/phase and map
guidance, rewards, achievements, shared-yeti kill behavior, and the end-to-end
quest flows.

Supplemental update: 2026-06-13 F-022 Q3741 Scattered Supplies placement-density restoration.
Q3741 (`Scattered Supplies`) now has focused server-side map-load coverage for
`26` current reviewed DataMapping `Creature2 12919` Exile Supply Crate
placements in world `426` / area `597`, expanding the earlier six-placement
Settler's Reach fallback slice. The promoted source_coordinate_ids are
`1218`-`1232`, `14881`, `17735`-`17737`, `51949`-`51952`, `87248`, `107347`,
and `115540`; mapped row `2634068` remains blocked from runtime promotion as an
older `coordinate_last_seen_in=4` near-duplicate until live/client density proof
exists. Focused map-load coverage verifies every promoted fallback position,
and the existing Q3741 activation tests verify accepted-quest `VirtualCollect`
item `363` credit, crate removal, and duplicate activation suppression. Q3741
remains not retail-complete until client smoke verifies crate activation/UI,
exact respawn/despawn behavior, Durek dialog, reward presentation and inventory
persistence, achievement UI, and the full Q3741 flow.

Supplemental update: 2026-06-13 F-022 Q5597 Chua Explosives placement-density restoration.
Q5597 (`Dregs and Thieves`) now has focused server-side map-load coverage for
all `30` reviewed DataMapping `Creature2 24286` Chua Explosives placements in
world `870` / worldzone `1218`, expanding the earlier six-placement fallback
slice. The extra `24` source_coordinate_ids were audited by subagent as
mapped-only, high-confidence same-objective Scarhide Camp cluster evidence, and
focused activation coverage verifies one `VirtualCollect` item `364` update and
spawn removal per script instance while Q5597 is accepted. Q5597 remains not
retail-complete until client smoke verifies exact retail density,
respawn/despawn behavior, Mondo dialog, Chua Explosives activation/UI, reward
presentation and inventory persistence, achievement UI, duplicate-completion
behavior, and the full Q5596->Q5597->Q5604 flow.

Supplemental update: 2026-06-13 F-022 Q5597 reward/progression hook validation.
Q5597 (`Dregs and Thieves`) now has focused server-side coverage for its
completion reward and achievement rows. A QuestManager completion test finishes
achieved Q5597 at visible Mondo Zax (`Creature2 24187`), grants both build
16042 fixed Quest2Reward rows `3000` (Item2 `81917`, amount `1`) and `5236`
(Item2 `29664`, amount `1`) with reward selection `0`, and proves the real
`CharacterAchievementManager` updates AchievementChecklist row `5495` for
achievement `4134` (`Episode Completion: Bloodstone Canyon`) with prerequisite
`18` satisfied. Q5597 remains not retail-complete until client smoke verifies
Mondo dialog, Chua Explosives activation/UI, reward presentation and inventory
persistence, achievement UI, duplicate-completion behavior,
Jabbithole-vs-client reward evidence reconciliation, and the full
Q5596->Q5597->Q5604 flow.

Supplemental update: 2026-06-13 F-022 Kezrek Warbringer receiver runtime restoration.
Kezrek Warbringer (`Creature2 24158`) now has focused-test-backed server
availability for the shared Crimson Isle Q5580/Q5583/Q5594 handoff.
`CrimsonIsleMapScript` fallback-spawns Kezrek at Quest2 receiver
`WorldLocation2 17902`, matching reviewed DataMapping source_coordinate_id
`15181` at Megatech Station. Build 16042 Creature2 `24158` directly carries
Q5594 in giver/receiver slots, while Q5580/Q5583 are represented by Quest2
receiver location `17902` and reviewed DataMapping finisher relations
`1559`/`1560`; `GlobalQuestManager` now uses those reviewed finisher relations
as receiver overrides for visible Kezrek completion. Focused tests cover
fallback spawn, duplicate suppression, Q5594 direct cache, Q5580/Q5583 receiver
override cache, and visible-Kezrek completion for achieved Q5580/Q5583. The
quests remain not retail-complete until client smoke verifies Kezrek dialog,
Q5580/Q5583 starter-slot behavior or script-grant route, reward/progression
side effects, and the full Q5604->Q5580/Q5583->Q5594 flow.

Supplemental update: 2026-06-13 F-022 Q5580 Tower Controls runtime restoration.
Q5580 (`Enforced Radio Silence`) now has focused-test-backed server
availability for its build 16042 Tower Controls objective. `CrimsonIsleMapScript`
fallback-spawns both reviewed `Creature2 26559` Simple rows at Megatech Station
from DataMapping source_coordinate_ids `15122` and `15123`, preserving
checklist indexes `1` and `2`, activePropIds `1137353` and `1137404`, and
the reviewed small-world seed coordinates/rotations.
Generic interaction and activate-spell paths credit QuestObjective `8227`
(`ActivateTargetGroupChecklist`, TargetGroup `2889`, count `2`), and
`Q5580QuestScript` keeps the Q5594 merge gate behind both Q5580 and Q5583.
Q5580 remains not retail-complete until client smoke covers exact activation
and objective UI behavior, active-prop visual/despawn/respawn behavior, Kezrek
Warbringer dialog/turn-in, reward/progression side effects, and full
Q5604->Q5580/Q5583->Q5594 flow.

Supplemental update: 2026-06-13 F-022 Evil from the Ether duplicate-credit hardening.
Evil from the Ether (`world 3404`, public event `781`) now has focused
runtime-tested duplicate suppression for the current schematics and portal
objective producers. `EthericDriveSchematicsEntityScript` credits
`PickUpDriveSchematics` (`5013`) once for the Katja-dropped `Creature2 71821`
object, removes the object from the map, and ignores repeated activation on the
same spawn. `EthericPortalEntityScript` credits small portal objective `4921`
and large portal objective `4923` at most once per spawned portal, preventing
duplicate death callbacks from over-advancing the large bridge objective.
Focused Evil from the Ether tests cover direct credit/removal and duplicate
suppression for these paths. This remains an expedition runtime slice only:
portal cadence, exact despawn/replay behavior, phase timing, rewards,
achievements, challenge side effects, and end-to-end client smoke remain
pending.

Supplemental update: 2026-06-13 F-022 Galeras Q4696 fallback duplicate guards.
Galeras Q4696/Q4694 map fallback coverage now includes duplicate guards for
both creature and world-location trigger paths. `GalerasMapScript` skips the
Q4696 EnterArea trigger fallback when WorldLocation2 `12649` is already active
through an `IWorldLocationVolumeGridTriggerEntity`, and focused tests also prove
the reviewed/promoted Corporal Darby receiver `17053` suppresses the Darby
fallback. The focused Galeras suite now passes `20/20` across Q4696 scout kill
credit, Q4694 holdout credit, Loftite crystal compatibility, map fallback
availability, and the new duplicate-suppression cases. This keeps Q4696/Q4694
at server-playability only: client scout combat smoke, rally trigger behavior,
Darby completion dialog, Trooper Vog activation, holdout wave timing, rewards,
achievements, exact spawn lifecycle, and end-to-end Galeras flow remain pending.

Supplemental update: 2026-06-13 F-022 Q3673 hidden completion duplicate guard.
Q3673 (`Contact with Thayd`) now suppresses duplicate hidden objective `13391`
credit if completed Signal Flare checklist objective `4748` is reported to the
quest script more than once. Focused tests cover the normal complete path, the
repeated complete-callback path, and the incomplete-then-complete path. This
does not claim Q3673 retail completion: client-visible Deadeye dialog/activation
smoke, exact flare activate/CSI packet timing, achieved-state/cinematic timing,
Q3886 prerequisite flow, alternate receiver routing, reward-choice UI and
inventory persistence, achievement UI, and end-to-end quest smoke remain
pending.

Supplemental update: 2026-06-13 F-022 Q3781/Q4526 duplicate-credit guards.
Q3781 (`Captives of the Dominion`) now treats each Captive Exile Soldier as a
one-shot rescue producer: successful accepted-quest activation credits
objective `4880` once for that spawned captive and suppresses repeat activation
credit while preserving build 16042 TargetGroup `2143` member coverage for
`12537`/`21015`/`21016`. Q4526 (`Spatial Anomaly`) now suppresses same-player
duplicate catch credit for objective `6165` across proximity and activation
paths. Exact Q3781 captive transform/despawn/respawn behavior, placement review
for Q3781 `21015`/`21016`, Q4526 anomaly movement/chase/despawn timing,
client-visible activation/proximity smoke, reward/achievement side effects,
and end-to-end quest smoke remain pending.

Supplemental update: 2026-06-13 F-022 Q3741 supply crate one-shot cleanup.
Q3741 (`Scattered Supplies`) now treats an Exile Supply Crate collection as a
one-shot world interaction: successful accepted-quest activation credits
`VirtualCollect` item `363`, marks the crate collected, and removes the crate
from the map. Focused tests cover accepted-quest credit/removal,
missing-quest non-removal, and duplicate activation suppression. Exact retail
crate respawn/despawn timing, client-visible crate activation/UI smoke, Durek
dialog/client smoke, reward UI and inventory persistence, achievement UI, and
end-to-end Q3741 smoke remain pending.

Supplemental update: 2026-06-13 F-022 Q3777 Loftite Crystal one-shot cleanup.
Q3777 (`By Leaps and Bounds`) now removes a collected Loftite Crystal from the
map after crediting objective row `5076` through `ActivateEntity` data `6952`
and `CollectItem` `6998`. Focused Q3777 tests cover both the legacy reviewed
bridge Creature2 `6987` and build 16042 TargetGroup member `13120`, verify that
missing-quest players do not remove the crystal, and verify duplicate range
entries do not grant or remove twice. Exact retail respawn/despawn timing,
client-visible Denner dialog, reward/reputation UI, achievement UI, and
end-to-end Q3777 smoke remain pending.

Supplemental update: 2026-06-13 F-022 Q3479 Bosun starter and nested target-group runtime coverage.
Q3479 (`From the Wreckage`) now has focused-test-backed runtime coverage for
the mapped Bosun Redmark starter route and the full build 16042 nested
TargetGroup credit set. `NorthernWildsMapScript` fallback-spawns Bosun Redmark
(`Creature2 11062`) at reviewed world `426` DataMapping position
`3913,-699,-5336`; accept-flow tests prove Q3479 succeeds only with visible
Bosun starter availability. The map script also promotes representative Fierce
Yeti Icefang (`Creature2 36331`) fallbacks from the mapped area `651` cluster.
Focused target-group tests pin TargetGroup `7288` expansion through `7287` and
`1463` to Creature2 `11945`/`11948`/`12844`/`13116`/`13117`/`13959`/`36331`/
`36335`/`51126`, and quest tests prove kill credit for every expanded member if
encountered. Q3479/Q3480 survivor CSI tests now prove objectives `4467`/`4470`
complete after three `11070` interactions. Q3479 remains blocked for retail
completion pending client dialog/CSI/combat smoke, reward and achievement UI
validation, unpromoted target-member placement proof, and full Q3479->Q3667
chain smoke.

Supplemental update: 2026-06-13 F-022 Q3486 Frostbite target-group dispatch.
Q3486 (`Empowered Tower`) now routes build 16042 TargetGroup `4985` member
`11924` Frostbite through the existing `Q3486CrystalGuardianEntityScript`
kill-credit path. Frostbite kills credit objective `4485` / VirtualCollect
item `206` while Q3486 is accepted, and
`NorthernWildsBranchInteractionTests` pins that the script filter includes both
`11924` and Crystal Guardian `11925`. The regenerated tracker marks the
`4985:data0:11924` target-group member as
`runtime_q3486_frostbite_target_group_member_script_credit_tested_pending_spawn_client_smoke`.
Q3486 remains not retail-complete pending Frostbite runtime spawn/drop evidence,
live combat smoke, reward/achievement UI, Master Control Panel dialog smoke,
and end-to-end Q3667->Q3486->Q3797 validation.

Supplemental update: 2026-06-13 F-022 Q3486 reward/progression row coverage.
Q3486 (`Empowered Tower`) now has focused-test-backed completion reward and
achievement-row coverage. Canonical build 16042 Quest2Reward rows
`1706`/`1707`/`2147` grant selectable Item2 `13329`/`1377`/`14102`, fixed
cash `1625` is paid through `QuestInfo.GetRewardMoney()`, visible Master
Control Panel (`11194`) completion succeeds, and generic quest-complete
achievement hooks fire. `AchievementProgressTests` pins the actual Arrival
episode checklist rows for achievement `3469` (`4245` Q3486, `4246` Q3667)
and achievement `5327` (`6907` Q3486, `6908` Q3667, `6910` Q3480). Read-only
subagent evidence keeps stale Jabbithole reward rows
`5318`/`5319`/`5320`/`5321` provenance-only. Reward/achievement CSV rows remain
not retail-complete until row-specific client smoke proves presentation and
persistence.

Supplemental update: 2026-06-13 F-022 Q3797 reward-path and Q3668 target-group credit evidence.
Q3797 (`Securing the Area`) now has focused-test-backed reward-path coverage:
selectable Quest2Reward rows `2480`/`2482`/`4770` grant Item2
`13333`/`27870`/`27871`, fixed cash reward source `163` grants `145`, visible
Durek (`11066`) completion succeeds, and the generic quest-complete
achievement hooks fire. Q3668 (`Indigenous Intelligence`) now separates its
TargetGroup blocker from its credit path: subagent audit confirmed TargetGroup
`7293` expands through `7292`/`960` to include Creature2 `14054` and `11913`,
and focused tests prove both the `AssetManager` expansion and `Quest`
objective-credit path. Runtime fallback spawns for `14054` and `11913` remain
blocked until reviewed Creature2 identity/placement or live-client evidence
ties them to the Coldburrow objective area.

Canonical quest coverage tracker for NexusForever. Pair with feature row **F-022**
in `CURRENT_STATUS.md` and the generated inventory in
`Decomp/Analysis/coverage/QUEST_IMPLEMENTATION_AUDIT.md`.

For the broader retail-completeness goal, use
`Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md`. It links the
row-level quest CSV plus dungeon/raid/expedition/scripted-instance inventory
and keeps table-driven coverage separate from retail-complete gameplay claims.
The tracker also emits
child inventories for quest objectives, quest rewards/dependencies, and
quest prerequisites, reviewed quest-loot overlays, quest world/interaction
dependencies, quest achievement/progression dependencies, quest script-hook
dependencies, quest creature/NPC relationship dependencies, quest
Jabbithole/objective, Jabbithole/reward, Jabbithole/zone, and quest episode
evidence dependencies, public-event DataMapping evidence dependencies,
public-event auxiliary dependencies, and challenge, path mission, and contract
DataMapping evidence dependencies, instance portal dependencies, script
communicator/cinematic presentation dependencies, script public-event flow
dependencies, script objective producer dependencies, script quest progression
dependencies, script runtime action
dependencies with Spell4/World client-reference and source-trace status, instance
script handler evidence dependencies, and instance
public-event/matching/achievement/script plus instance entity/encounter
dependencies and reward dependencies. It also emits a generated next
restoration slice queue, parallel subagent lane snapshot, and top-gap rollup so
mapped evidence, implemented behavior, validation coverage, and retail-complete
claims stay separate.

Supplemental update: 2026-06-12 F-022 Q3777 By Leaps and Bounds reward and achievement hook evidence.
Q3777 (`By Leaps and Bounds`) now has focused-test-backed server-side reward
and achievement hook evidence in addition to its Denner Hazefall and Loftite
Crystal route coverage. Build 16042 Quest2Reward rows
`2244`/`2388`/`7191`/`7192`/`7193`/`7284`/`8867` are selectable Item rewards
for Item2 `28048`/`28031`/`76391`/`76392`/`76393`/`76484`/`28015`. Focused
`QuestManager` coverage completes achieved Q3777 at visible Denner Hazefall
`15759`, grants each selected reward by default/index/reward-id/item-id or
low-15-bit item-id path, and asserts `QuestComplete`,
`QuestCompleteChecklist`, and `QuestCompleteChecklistCount` hooks for quest
`3777`, matching AchievementChecklist row `4420` / achievement `3523`. Q3777
remains pending client-visible Denner dialog/activation smoke, exact Loftite
Crystal jump-through/collision/respawn/despawn behavior, reward-choice UI and
inventory persistence smoke, `Quest2.rewardReputationOverride` runtime grant
proof, achievement UI/progression smoke, and end-to-end Q3777 smoke.

Supplemental update: 2026-06-12 F-022 Q3668 Indigenous Intelligence reward and achievement hook evidence.
Q3668 (`Indigenous Intelligence`) now has focused-test-backed server-side
reward and achievement hook evidence in addition to its Bartol, Lusk, survivor,
and Skeech route coverage. Build 16042 Quest2Reward rows `2190`/`2191`/`4768`
are selectable Item rewards for Item2 `81334`/`81331`/`81329`, with pushed
Item2 `6912` tracked as a quest-state dependency. Focused `QuestManager`
coverage completes achieved Q3668 at visible Bartol Sunward `12737`, grants
each selected reward by default/index/reward-id/low-15-bit item-id path, and
asserts `QuestComplete`, `QuestCompleteChecklist`, and
`QuestCompleteChecklistCount` hooks for quest `3668`, matching
AchievementChecklist row `4260` / achievement `3490`. Q3668 remains pending
client-visible Deadeye/Lusk/Bartol dialog and activation smoke, exact survivor
transform/despawn behavior, spawn/placement proof for TargetGroup members
`14054` and `11913`, reward-choice UI and inventory persistence smoke,
achievement UI/progression smoke, Q3486/Q3671/Q3668 chain smoke, and
end-to-end Q3668 smoke.

Supplemental update: 2026-06-12 F-022 Q3741 Scattered Supplies fixed reward and achievement hook evidence.
Q3741 (`Scattered Supplies`) now has focused-test-backed server-side fixed
reward and achievement hook evidence in addition to its supply-crate and Durek
route coverage. Build 16042 Quest2Reward rows `1712` and `3476` are fixed Item
rewards (`flags 0`) for Item2 `81917` amount `3` and Item2 `29614` amount `1`.
Focused `QuestManager` coverage completes achieved Q3741 at visible Land's
Reach Commander Durek `11066`, grants both fixed reward rows with reward
selection `0`, and asserts `QuestComplete`, `QuestCompleteChecklist`, and
`QuestCompleteChecklistCount` hooks for quest `3741`, matching AchievementChecklist
row `4261` / achievement `3490`. Q3741 remains pending client-visible crate
activation/UI smoke, live density proof for mapped older row `2634068`, exact
respawn/despawn behavior, Durek dialog/client smoke, reward UI and inventory
persistence smoke, achievement UI/progression smoke, and end-to-end Q3741
smoke.

Supplemental update: 2026-06-12 F-022 Q3886 Fiery Distraction reward and achievement hook evidence.
Q3886 (`Fiery Distraction`) now has focused-test-backed server-side reward and
achievement hook evidence in addition to its Durek receiver, Burning Torch,
Skeech Hut, and Q3673 follow-up coverage. Build 16042 Quest2Reward rows
`2192`/`2430`/`4767` are selectable Item rewards for Item2
`27868`/`13332`/`27869`. Focused `QuestManager` coverage completes achieved
Q3886 at visible Land's Reach Commander Durek `11066`, grants each selected
item by default/index/reward-id/item-id path, and asserts `QuestComplete`,
`QuestCompleteChecklist`, and `QuestCompleteChecklistCount` hooks for quest
`3886`, matching AchievementChecklist row `4263` / achievement `3490`. Q3886
remains pending client-visible Durek dialog/activation smoke, exact torch/hut
activate/CSI packet smoke, hut burn/despawn/respawn behavior, reward-choice UI
and inventory persistence smoke, achievement UI/progression smoke, and
end-to-end Q3886 -> Q3673 smoke.

Supplemental update: 2026-06-12 F-022 Q3673 Contact with Thayd reward and achievement hook evidence.
Q3673 (`Contact with Thayd`) now has focused-test-backed server-side reward
and achievement hook evidence in addition to its Deadeye receiver, signal-flare,
hidden completion, cinematic, and Q3670 follow-up coverage. Build 16042
Quest2Reward rows `2187`/`2188`/`2189`/`2481`/`3001`/`3846` are selectable
Item rewards for Item2 `12654`/`12655`/`12656`/`13328`/`14571`/`17924`.
Focused `QuestManager` coverage completes achieved Q3673 at visible Deadeye
`11063`, grants each selected item by default/index/reward-id/item-id path, and
asserts `QuestComplete`, `QuestCompleteChecklist`, and
`QuestCompleteChecklistCount` hooks for quest `3673`, matching
AchievementChecklist row `4262` / achievement `3490`. Q3673 remains pending
client-visible Deadeye dialog/activation smoke, exact activate/CSI packet smoke,
achieved-state/cinematic timing smoke, prerequisite flow from Q3886, alternate
receiver routing for location `12354` / prerequisite `29356`, reward-choice UI
and inventory persistence smoke, achievement UI/progression smoke, and
end-to-end quest smoke.

Supplemental update: 2026-06-12 F-022 Q3963 More Important Than Revenge reward and achievement hook evidence.
Q3963 (`More Important Than Revenge`) now has focused-test-backed server-side
availability for the build 16042 Ship Controls objective and Galeras Deadeye
receiver path. QuestObjective `5201` is an ActivateEntity step with reward-pane
TargetGroup `7573`, whose build 16042 member is Creature2 `27196` Ship
Controls; `NorthernWildsMapScript` fallback-spawns `27196` at WorldLocation2
`45401` and `45402`, matching reviewed DataMapping source-coordinate ids `5168`
and `40678`, and generic ActivateEntity reward-pane target-group expansion
provides objective credit. Quest2 routes Q3963 completion to receiver
WorldLocation2 `12354`; build 16042 Creature2 `16622` carries
`QuestIdReceive 3963`, `GalerasMapScript` fallback-spawns `16622` at that exact
receiver placement from reviewed DataMapping source-coordinate id `837`, and
runtime seed promotes another reviewed Tremor Ridge `16622` placement at
source-coordinate id `2587271`. Focused `GlobalQuestManager` and `QuestManager`
coverage pins `16622` as the Q3963 receiver and requires visible `16622` for
achieved-state turn-in. Focused reward coverage now also grants the selected
build 16042 item reward from Quest2Reward rows `3706`/`3707`/`4771` and asserts
the completion path fires `QuestComplete`, `QuestCompleteChecklist`, and
`QuestCompleteChecklistCount` achievement checks for quest `3963`, matching
AchievementChecklist row `4270` / achievement `3491`. Q3487 grants Q3963
through `Q3487Shellshock`, while the reviewed Deadeye `11063` starter-context
row remains pending direct-client proof because build 16042 Creature2 `11063`
does not list Q3963 in `QuestIdGiven`. Q3963 remains pending client-visible Ship
Controls activation smoke, Deadeye receiver dialog smoke, reward-choice UI
smoke, inventory persistence, achievement UI/progression smoke, Q3487 -> Q3963
client flow smoke, and end-to-end Q3963 validation.

Supplemental update: 2026-06-12 F-022 Q3667 The Tower server playability evidence.
Q3667 (`The Tower`) now has focused-test-backed server-side availability for
the build 16042 Master Control Panel route. Quest2 and QuestObjective map the
objective to ActivateEntity data `11194` / objective `4770` at WorldLocation2
`7778`; DataMapping reviews the same Master Control Panel placement as
source-coordinate id `2501`, while Deadeye Brightland `11063` remains the
reviewed starter relation. `NorthernWildsMapScript` provides the shared panel
fallback and existing Deadeye fallback coverage, and
`Q3667ControlPanelEntityScript` now credits objective `4770` from both direct
activation and the branch-proximity fallback while Q3667 is accepted. Q3667
remains `not_retail_complete` pending Deadeye accept dialog smoke, panel
completion dialog/activation smoke, exact trigger timing, prerequisite flow,
reward/achievement side effects, and end-to-end Q3667 -> Q3486 smoke.

Supplemental update: 2026-06-13 F-022 Q3886 Fiery Distraction placement-density restoration.
Q3886 (`Fiery Distraction`) now has focused-test-backed server-side
availability for the Durek, Burning Torch, and Skeech Hut route. Build 16042
maps receiver WorldLocation2 `7727` to Land's Reach Commander Durek, objective
`5053` to SucceedCSI data `13630` / count `1`, objective `5052` to
ActivateTargetGroupChecklist data TargetGroup `1460` / count `3`, and
TargetGroup `1460` to Creature2 `13623`. `NorthernWildsMapScript` now
fallback-spawns Durek (`11066`) at WL `7727`, four current reviewed Burning
Torches (`13630`) from DataMapping source-coordinate ids `2425`, `124555`,
`142844`, and `1585277`, and nine current reviewed Skeech Huts (`13623`) from
ids `2309`, `2310`, `2311`, `2312`, `2313`, `2314`, `14843`, `115795`, and
`115796` with unique checklist indexes `0` through `8`. QuestObjective `5052`
still has count `3`, so checklist bit-count semantics complete after any three
distinct huts. Generic interaction and activate-spell paths provide the
objective credit, while
`Q3886FieryDistractionQuestScript` preserves the Q3673 follow-up. The quest
remains pending for client-visible Durek dialog/activation smoke, exact
torch/hut activate/CSI packet smoke, hut burn/despawn/respawn behavior,
reward-choice UI/persistence and achievement UI smoke, and end-to-end Q3886 ->
Q3673 validation.

Supplemental update: 2026-06-12 F-022 instance direct objective-id producer evidence.
Instance script objective producers now distinguish exact direct
PublicEventObjective credit from broad typed `PublicEventObjectiveType`/`objectId`
credit. Skullcano (`329`, `362`, `372`), Sanctuary (`613`, `614`, `615`),
Coldblood (`5326`, `5313`), and Evil from the Ether (`4919`, `4927`, `4939`)
use direct objective-id updates where the script already knows the build 16042
objective row, while the generated tracker preserves each row's client type
(`Turnstile`, `ParticipantsInTriggerVolume`, `Script`, or
`KillEventObjectiveUnit`). The remaining broad Evil portal `Script 0` producer
stays blocked for phase-specific evidence. The regenerated producer evidence
also separates `139` direct objective-id rows whose client rows are kill-style
objectives into
`direct_objective_id_matches_client_kill_type_pending_*_script_type_review`,
while keeping `17` `ScriptWithoutCount` and `4` `ScriptWithoutMax` rows
explicit; do not treat those rows as generic kill routing until script-vs-kill
semantics are proven. This is server-side credit-path coverage, not a
retail-complete instance claim.

Supplemental update: 2026-06-12 F-022 Q3783 Off with Her Head and Land's Reach Durek route evidence.
Q3783 (`Off with Her Head!`) now has focused-test-backed server-side
availability and reward-turn-in coverage for its item-started no-objective
route. Build 16042 `Item2` row `6954` starts Q3783, `Quest2` routes completion
to receiver WorldLocation2 `7727`, and `Quest2Reward` row `8830` grants item
`17756`. The stronger client route is Land's Reach Commander Durek (`11066`):
Creature2 `11066` carries Q3741/Q3797/Q3886 in `QuestIdGiven` and
Q3783/Q3741/Q3797/Q3886 in `QuestIdReceive`. `NorthernWildsMapScript` now
fallback-spawns `11066` at WL `7727` (`4339.04,-751.768,-5678.82`), and
QuestManager coverage completes Q3783 at visible `11066` while granting the
mapped reward. `GlobalQuestManager` still keeps the reviewed Q3797
compatibility override for ambiguous Durek row `11061`. Q3783 and the shared
Durek route remain pending for client-visible item-start and Durek
dialog/activation smoke, reward UI/inventory persistence, Q3486->Q3797
prerequisite smoke, crate density/respawn/despawn validation,
achievement/progression side effects, and end-to-end client smoke.

Supplemental update: 2026-06-12 F-022 Q3781 Captives of the Dominion server playability evidence.
Q3781 (`Captives of the Dominion`) now has focused-test-backed server-side
starter, objective, and alternate receiver coverage. Build 16042 maps objective
`4880` to an `ActivateEntity` rescue step with count `3`, indicator
WorldLocation2 `26787`, and TargetGroup `2143` members `12537`, `21015`, and
`21016`. `NorthernWildsMapScript` fallback-spawns Dead Exile Soldier (`50668`)
at reviewed DataMapping source-coordinate id `2055` and eleven Captive Exile
Soldier (`12537`) placements from source-coordinate ids `4344`, `4345`, `4346`,
`4347`, `4348`, `5180`, `5181`, `17747`, `50498`, `6158136`, and `7153684`.
`Q3781CaptiveExileSoldierEntityScript` binds all three TargetGroup member ids
and credits objective `4880` while Q3781 is accepted. `GlobalQuestManager`
applies reviewed starter/finisher overrides for `50668` and Galeras Deadeye
Brightland (`16622`), and `GalerasMapScript` covers alternate receiver
WorldLocation2 `12354`. Q3781 remains pending for client-visible corpse and
Deadeye dialog/activation smoke, exact captive activate/CSI
transform/despawn/respawn behavior, placement review for `21015`/`21016`,
reward-choice UI/persistence, achievement/progression side effects, and
end-to-end Q3781 client smoke.

Supplemental update: 2026-06-12 F-022 Q3777 By Leaps and Bounds server playability evidence.
Q3777 (`By Leaps and Bounds`) now has focused-test-backed server-side
availability and credit coverage for its Galeras Loftite Cliffs route. Build
16042 maps objective `5075` to `EnterZone` zone `219`, objective `5076` to the
first `ActivateEntity` fragment step, and objective `4859` to `CollectItem`
item `6998` count `8` through reward-pane TargetGroup `7534`. `GalerasMapScript`
fallback-spawns Denner Hazefall (`15759`) at Quest2 receiver WorldLocation2
`11687` and eight Loftite Crystal (`13120`) fallbacks in the WL `9578`
objective area using reviewed DataMapping source-coordinate ids `4220179`,
`8274745`, `5660859`, `5660860`, `1857375`, `7309210`, `8274743`, and
`5660857`. `Q3777ByLeapsAndBounds` binds both the reviewed bridge Creature2
`6987` and the build 16042 TargetGroup member `13120`, crediting objective row
`5076` through `ActivateEntity` data `6952` plus `CollectItem` `6998`, then
removing the collected crystal from the map; QuestTests pin the zone `219`
travel credit.
Q3777 remains pending for Denner dialog/activation smoke, exact crystal
jump-through collision/trigger behavior, exact respawn/despawn timing,
reward/achievement side effects, and end-to-end Q3777 client smoke.

Supplemental update: 2026-06-12 F-022 Q3741 Scattered Supplies server playability evidence.
Q3741 (`Scattered Supplies`) now has focused-test-backed server-side
availability for `26` current reviewed Exile Supply Crate (`12919`) placements
around the Settler's Reach objective area and activation credit for objective
`4813` (`VirtualCollect`, data `363`, count `6`). Build 16042 maps the
objective to indicator WorldLocation2 ids `9181`, `9182`, and `9962`, with
reward-pane TargetGroup `4372` containing Creature2 `12919`; DataMapping
relation `234` reviews that objective mapping, and `NorthernWildsMapScript`
fallback-spawns source-coordinate ids `1218`-`1232`, `14881`,
`17735`-`17737`, `51949`-`51952`, `87248`, `107347`, and `115540` in world
`426` / area `597`.
`Q3741ExileSupplyCrateEntityScript` now credits
`QuestObjectiveType.VirtualCollect` data `363` when an accepted Q3741 player
activates a crate, matching the build 16042 objective row without assuming the
client-visible virtual-loot presentation path. The reviewed LaughingWS
quest-loot groups for VirtualItem `363` remain mapped-only pending loot/UI
smoke. The same map fallback now uses build 16042
Land's Reach Commander Durek (`11066`) for the Q3741 starter/receiver route at
WL `7727`; reviewed DataMapping rows that point at `11061` remain ambiguous
compatibility evidence. The generated tracker records the matching objective,
indicator, target-group member, script, and creature-relation rows as runtime
evidence. Q3741 remains pending for client-visible crate activation/UI smoke,
live density proof for mapped older row `2634068`, exact respawn/despawn
behavior, Durek dialog/client smoke, reward UI/inventory persistence and
achievement UI smoke, and end-to-end Q3741 smoke.

Supplemental update: 2026-06-12 F-022 Q3797 Securing the Area server playability evidence.
Q3797 (`Securing the Area`) now has focused-test-backed server-side availability
for build 16042 Land's Reach Commander Durek (`11066`) at Quest2 receiver
WorldLocation2 `7727` and reviewed Rootbrute/Yeti objective fallbacks for
objective `4918` (`KillTargetGroups`, TargetGroup `1177`, count `8`) around
indicator WorldLocation2 ids `9181`, `9182`, and `9962`. Build 16042 has no
`CommunicatorMessages` row delivering Q3797, and Creature2 `11066` carries
Q3797 in both `QuestIdGiven` and `QuestIdReceive`; `GlobalQuestManager` still
applies reviewed DataMapping starter/finisher overrides from ambiguous row
`11061` / relations `166` and `1693` as compatibility evidence. `AssetManager`
now expands `CreatureIdListGroup` target groups for quest objective target
matching, and the generated tracker records the matching receiver, objective,
indicator, target-group member, script, prerequisite, and creature-relation rows
as runtime evidence. Q3797 remains pending for client-visible Durek
dialog/accept/completion activation smoke, Q3486 prerequisite flow smoke, exact
objective placement, density, respawn, tap, and threat behavior,
type-9/non-spawned branch review, loot, reward UI/persistence, and achievement
UI/progression side effects, and end-to-end Q3797 smoke.

Supplemental update: 2026-06-12 F-022 Q3668 Indigenous Intelligence server playability evidence.
Q3668 (`Indigenous Intelligence`) now has focused-test-backed server-side
availability for Bartol Sunward (`12737`) at Quest2 receiver WorldLocation2
`9121`, Scientist Lusk (`12484`) at objective indicator WorldLocation2 `8857`,
seven reviewed Imprisoned Survivor (`14124`) placements for TargetGroup `7261`,
and reviewed Skeech kill-target fallbacks for nested TargetGroup `7293` members
`36429`, `17545`, `11907`, `11910`, `11912`, `11917`, and `36884`. The generic
TalkTo, ActivateEntity target-group, and nested KillTargetGroups credit paths
cover those spawned objective entities, and the generated tracker records the
matching receiver, objective, indicator, target-group member, script, and
creature-relation rows as runtime evidence. Q3668 now has unit-tested
TargetGroup expansion and credit coverage for unplaced members `14054` and
`11913`, but remains pending for client-visible Deadeye/Lusk/Bartol dialog and
activation smoke, exact survivor transform/despawn behavior, reviewed
spawn/placement proof for `14054`/`11913`, reward-choice UI/inventory
validation, achievement/progression side effects, and end-to-end
Q3486/Q3671/Q3668 chain smoke.

Supplemental update: 2026-06-12 F-022 Q3479/Q3480 shared yeti kill target availability.
Q3479 (`From the Wreckage`) objective `4564` and Q3480 (`Reporting for Duty`)
objective `4565` now have focused server-side availability and generic
`KillTargetGroup` credit coverage for the shared Northern Wilds yeti targets.
Build 16042 `QuestObjective.tbl` maps Q3479 objective `4564` to TargetGroup
`7288` count `8` and Q3480 objective `4565` to TargetGroup `1463` count `8`;
TargetGroup `7288` is `OtherTargetGroupCreatures` over TargetGroups `7287` and
`1463`, and TargetGroup `1463` includes Creature2 `11945` (`Yeti Snowstalker`)
and `11948` (`Yeti Frostclaw`). `NorthernWildsMapScript` now fallback-spawns
eight reviewed world `426` DataMapping placements for those shared members at
source-coordinate ids `7915699`, `7915700`, `7956687`, `8401261`, `8110657`,
`8158349`, `8204800`, and `8251657`, while generic target-group expansion and
kill-credit coverage credit those member IDs. The generated content tracker
records objective rows `4564`/`4565`, objective-indicator locations
`12155`/`12156`/`12157`/`12158`, nested target-group/member dependencies, and
creature relations `216`/`218`/`2129`/`2130` as tested runtime evidence.
Q3479/Q3480 remain pending for live/client combat kill smoke, respawn/tap/threat
behavior, loot cadence, reward/achievement side effects, exact survivor CSI
client smoke, full map-guidance review, and end-to-end quest smoke.

Supplemental update: 2026-06-13 F-022 Q3479/Q3480 Trapped Survivor placement-density restoration.
Q3479 (`From the Wreckage`) objective `4467` and Q3480 (`Reporting for Duty`)
objective `4470` now have focused server-side availability evidence for the
shared Trapped Survivor CSI step. Build 16042 `QuestObjective.tbl` marks both
objectives as `SucceedCSI` data `11070` with count `3`; `Creature2.tbl` defines
`11070` as Trapped Survivor with activate spell `1817`; and DataMapping maps
both objective creature relations to Creature2 `11070` with 16 reviewed world
`426` placements. `NorthernWildsMapScript` now fallback-spawns all 16 reviewed
survivor positions at source-coordinate ids `3406`, `3407`, `3408`, `15032`,
`17868`, `40681`, `40682`, `40683`, `41559`, `42765`, `114996`, `114997`,
`115687`, `121680`, `156500`, and `156501` in world `426` / area `646`, while
generic interaction coverage credits `QuestObjectiveType.SucceedCSI` for the
interacted creature. The generated content tracker records objective rows
`4467`/`4470`, objective-indicator location `12156`, and creature relations
`217`/`2131` as tested runtime evidence. Q3479/Q3480 remain pending for exact
client activate/CSI packet smoke, phase/visibility smoke, full map-guidance
review, reward/achievement side effects, the separately tracked shared-yeti kill
objective live smoke, and end-to-end quest smoke.

Supplemental update: 2026-06-12 F-022 Q4696 retreat enemy kill-credit path.
Q4696 objective `6508` now has focused server-side playability coverage for the
Temple of Osiric retreat enemies. Build 16042 QuestObjective `6508` is
`ActivateEntity` count `5` with reward-pane TargetGroup `4323`, whose members
are Creature2 `17189` and `19595`; both are client-labeled Q4696 script-spawn
enemies with no activate spell. `Q4696TempleRetreatEnemyEntityScript` now
credits `QuestObjectiveType.ActivateEntity` for either member on kill while
Q4696 is accepted, and the existing generic target-group expansion maps that
credit to objective `6508`. The generated tracker records objective `6508` as
`runtime_q4696_retreat_enemy_kill_to_activate_target_group_credit_tested_pending_client_smoke`,
member `17189` as
`runtime_q4696_script_spawn_enemy_kill_credit_tested_pending_client_smoke`, and
member `19595` as
`runtime_q4696_runtime_spawn_enemy_kill_credit_tested_pending_client_smoke`.
Q4696 is still not retail-complete until live/client combat objective smoke,
exact spawn/despawn and quest-state visibility timing, placement/density
review, reward/achievement side effects, Darby completion dialog, and the
surrounding retreat/rally flow are validated.

Supplemental update: 2026-06-12 F-022 Q4696 Darby receiver location fallback.
Q4696 (`Leaving the Temple of Osiric`) now has focused receiver availability
coverage for build 16042 Creature2 `17053` (Corporal Darby) at Quest2 receiver
WorldLocation2 `17590` (`6204.08,-888.474,-2809.91`, world `51` / area `973`).
`GalerasMapScript` spawns Darby there only when no nearby Darby is already
active, so the promoted runtime seed entity from reviewed DataMapping
`source_coordinate_id 10020` remains the primary runtime placement while clean
maps still get a visible receiver. The generated retail-completeness tracker
records the receiver-location row as
`runtime_q4696_darby_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke`
and the Darby finisher relation as fallback-spawn plus receiver-cache and
completion-path tested. Q4696 is still not retail-complete until client-visible
Darby completion dialog/activation, reward/achievement UI, phase/placement
visibility, exact scout spawn/despawn and quest-state visibility timing, and
Q4666/Q4667/Q4696 retreat/rally flow are validated.

## Quick counts (client build 16042)

These counts measure table/objective handler support and curated script
coverage. They are not manual gameplay-complete claims; Rider's Reef remains
in progress until the client smoke items below are verified.

| Tier | Quests | % of 5,194 |
| --- | ---: | ---: |
| **Total `Quest2` rows** | 5,194 | 100% |
| Quest lifecycle only (no objectives in table) | 1,291 | 24.9% |
| **Fully curated** (hand-tuned `IQuestScript` chains) | 49 | 0.9% |
| **Generic table-driven** (supported objective types) | 3,854 | 74.2% |
| **Partial** (mixed partial objective types or stub scripts) | 0 | 0.0% |
| **Blocked** (unsupported objective types) | 0 | 0% |
| Current `Quest2` ids with `ScriptFilterOwnerId` | 67 | 1.3% |
| Distinct `ScriptFilterOwnerId` owner ids in `Script.Main` | 87 | 1.7% |

For the **3,903 quests with objectives**:

| Tier | Quests | % |
| --- | ---: | ---: |
| Completable via generic handlers or curated scripts | 3,903 | 100.0% |
| Partial | 0 | 0.0% |
| Blocked | 0 | 0% |

## Generic objective handlers (2026-05-23)

- `SpellSuccess` / `SpellSuccess2` / `SpellSuccess3` / `SpellSuccess4`: credited when a
  player-owned spell reaches execute (`SpellQuestObjectiveUpdater` in `Spell.Execute`).
- `CompleteQuest`: credited when another quest is turned in (`QuestManager` completion).
- `CollectItem`: `Data` = `Item2Id` (274/327 rows; client `QuestObjective.tbl` crosswalk).
  Synced from inventory on item create/stack increase and quest accept
  (`InventoryQuestObjectiveUpdater`).
- `CompleteEvent` / `Unknown31`: `Data` = `PublicEventObjective.Id` (111/142 and 206/257 rows
  vs `PublicEventObjective.tbl`). Credited when a public-event objective succeeds
  (`PublicEventQuestObjectiveUpdater`).
- `CraftSchematic`: `Data` = `TradeskillSchematic2.Id` for recipe crafts (36/171 rows) and
  overlaps public-event ids for event crafts. Credited on successful fixed-recipe craft
  (`CraftingQuestObjectiveUpdater`) plus the public-event path above.
- `ActivateTargetGroup`: credited on direct interaction paths (matches spell activate
  effect) via `InteractionObjectiveUpdater`.
- `GatheResource`: `Data` = `CreatureId`; credited on creature kill and interaction.
- `Unknown20` / `Unknown28` / `CombatMomentum`: `Data` = `PublicEventObjective.Id`
  (same crosswalk as types 25/31); credited on public-event objective success.
- `KillCreature2`: `Data` = `Creature2Difficulty.Id`; credited on kill using difficulty id.
- `EarnCurrency`: `Data` = `CurrencyType`; credited when currency is granted.
- `ParticipateInGroupContent`: `Data` = `MatchingGameType.Id`; credited on match enter.
- `PvPKills`: credited when a player kills another player (`Data` 0 or victim map id).
- `CompleteMaxLevelQuests`: credited when a level-50 player completes a quest.
- `BeginMatrix`: `Data` = 0; credited on account primal-essence grants (types 15-18) and
  quest-accept sync when essence is already held (`PrimalMatrixQuestObjectiveUpdater`).

## What is implemented everywhere

- All `Quest2` rows load through `GlobalQuestManager` / `QuestInfo`.
- Accept, abandon, retry, track, share, objective updates, completion, repeat
  resets, and standard reward grants (item, money, reputation, XP, tradeskill,
  account currency).
- Prerequisite validation: level, faction, class, prerequisite quests,
  `prerequisiteId`, items, exclusions, faction-level gates.
- Wiki/runtime audit for lifecycle surfaces: `Tools/WikiArchiveAudit/reports/quests-wiki-audit.md`.

## Hand-curated focus areas

| Zone | World id | Quest ids (representative) | Gameplay status |
| --- | ---: | --- | --- |
| Rider's Reef tutorial | 3460 | 10513-10532, 10540, 10541 (+ combat 10518/10524) | In progress, not complete |
| Northern Wilds | 426 | 3479, 3480, 3486, 3487, 3667, 3668, 3670, 3671, 3673, 3741, 3777, 3781, 3783, 3797, 3886, 3963, 4526, 4666, 4667, 4696, 4694 | Core chain covered; Q3667 now has tested Master Control Panel (`11194`) fallback coverage at Quest2 receiver/objective WorldLocation2 `7778`, direct activation plus branch-proximity credit for objective `4770`, and reviewed Deadeye Brightland (`11063`) starter relation coverage; Q3781 now has tested Dead Exile Soldier (`50668`) starter fallback coverage at DataMapping source-coordinate id `2055`, eleven Captive Exile Soldier (`12537`) objective fallbacks inside Camp Icefury, Q3781 activation credit for objective `4880` across TargetGroup `2143` members `12537`/`21015`/`21016`, and Galeras Deadeye Brightland (`16622`) alternate receiver/cache coverage at WorldLocation2 `12354`; Q3777 now has tested Denner Hazefall (`15759`) receiver fallback coverage at Quest2 receiver WorldLocation2 `11687`, eight Loftite Crystal (`13120`) objective fallbacks from reviewed DataMapping source-coordinate ids `4220179`, `8274745`, `5660859`, `5660860`, `1857375`, `7309210`, `8274743`, and `5660857`, crystal credit for objective `5076` plus `CollectItem` `6998`, and EnterZone `219` credit for objective `5075`; Q3741 now has tested `26` current Exile Supply Crate (`12919`) fallback spawns for objective `4813` / TargetGroup `4372` plus Q3741 activation credit and build 16042 Land's Reach Commander Durek (`11066`) starter/receiver route coverage at Quest2 receiver WorldLocation2 `7727`; Q3783 now has tested item-start/no-objective turn-in coverage at visible `11066` with Quest2Reward row `8830` item `17756`; Q3797 now has tested Land's Reach Commander Durek (`11066`) giver/receiver route coverage at Quest2 receiver WorldLocation2 `7727`, keeps the reviewed `11061` compatibility override, reviewed Rootbrute/Yeti objective fallback spawns for objective `4918` / TargetGroup `1177`, generic `KillTargetGroups` member credit, and type-9 `CreatureIdListGroup` objective-target expansion, Q3671 now has tested Deadeye Brightland (`11063`) primary receiver availability at Quest2 WorldLocation2 `9340` plus visible-receiver no-objective completion and item reward `19131` coverage, Q3673 now has tested Deadeye Brightland (`11063`) receiver availability plus Signal Flare (`12521`/`13150`/`13151`) fallback spawns at world locations `8867`/`8868`/`8869` with TargetGroup `1106` checklist indexes and generic `SucceedCSI`/`ActivateTargetGroupChecklist` credit coverage; Q4696 objective `6508` reward-pane target-group expansion/target matching plus tested `Q4696TempleRetreatEnemyEntityScript` kill credit for members `17189`/`19595`, promoted runtime spawns for member/objective creature `19595`, client-labeled script-spawn fallback coverage for member `17189` at `17586`/`17588`/`17589`, client Creature2 `17175` Durek fallback coverage at DataMapping `source_coordinate_id 9972` plus quest-giver cache and server accept-path coverage for visible `17175`, completed Q4667, Exile faction, and level 16 requirements, Q4696 completion handoff to Q4694, Q4694 Trooper Vog holdout flag unit (`50904`) fallback coverage at the Crosswind Fields DataMapping placement near source-coordinate ids `10201`/`3742220`, Q4694 Trooper Vog direct activation credit for `ActivateEntity` data `50904` plus activate spell `50151` / `Spell4Effects.Activate` evidence for activate-cast, Q4694 Stormwing Falkrin holdout kill credit for Creature2 `23953` to `CompleteEvent` data `111`, finisher `17053` Darby receiver fallback at Quest2 receiver location `17590` plus receiver-cache/completion-path coverage requiring visible Darby instead of starter `17175`, exact promoted runtime seed match to DataMapping `source_coordinate_id 10020`, and reviewed/unpromoted Darby placement gap `99084`, objective `6457` EnterArea credit, Galeras/world-51 trigger placement, Q4526 wreckage world-location credit for objective `6164`, Q4526 map-load fallback spawn for Scientist Pristan (`12560`) at Quest2 receiver location `11669` plus Spatial Anomaly (`12535`), and Q4526 anomaly activation/proximity catch credit for objective `6165` are test-backed/tracker-backed; exact spawn/despawn and quest-state visibility timing, Q3667 client-visible Deadeye accept dialog, panel completion dialog/activation smoke, exact trigger timing, and Q3667 -> Q3486 end-to-end smoke, Q3781 client-visible starter/Deadeye dialog activation smoke, exact captive activate/CSI transform/despawn/respawn behavior, placement/variant review for `21015`/`21016`, Q3777 Denner dialog/activation smoke, exact crystal jump-through/collision/respawn/despawn behavior, Q3741 client-visible crate activation/UI smoke, live density proof for mapped older row `2634068`, exact Q3741 respawn/despawn behavior, Q3783 client-visible item-start/Durek dialog and reward persistence smoke, Q3797 client-visible Durek dialog/activation and Q3486 prerequisite flow smoke, Q3671 and Q3673 client-visible Deadeye dialog/activation and activate/CSI packet smoke, Q3671 alternate receiver prerequisite routing and Q3486/Q3671/Q3668 chain smoke, Durek dialog activation/client-visible prerequisite-denial/phase validation, Darby completion dialog/client activation smoke, Q4694 Trooper Vog client-visible dialog/activation smoke and exact client packet path confirmation, Q4694 full holdout wave timing/Murgh/ally participation, live/client target-objective smoke, Q4526 exact anomaly movement/chase/despawn behavior, Q4526 Pristan dialog/accept/completion smoke, Q4526 activation/proximity packet smoke, Q4526 rewards/achievement side effects, and retreat/rally mechanics remain pending; local spawn promotion verified to 161/164 enabled Jabbithole creatures; manual smoke still pending |
| Crimson Isle | 870 | 5573, 5575, 5580, 5583, 5584, 5593-5597, 5604, 5610, 8855 | Script-chain covered; Q5610 Mondo Zax receiver fallback tested |

Map scripts and entity hooks live under `Source/NexusForever.Script.Main/Quests/`.

## Build 16042 start model

For the NexusForever build 16042 target, **Rider's Reef** (`world 3460`) is the
Novice new-player experience for both factions. Do not replace it with the older
faction arkship zones when working toward 16042 parity.

| Character creation path | Exile destination | Dominion destination |
| --- | --- | --- |
| Novice (`CreationStart = 4`) | Rider's Reef (`world 3460`) | Rider's Reef (`world 3460`) |
| Veteran (`CreationStart = 3`) | Human/Granok: Northern Wilds (`world 426`); Aurin/Mordesh: Everstar Grove (`world 990`) | Chua/Draken: Crimson Isle (`world 870`); Cassian/Mechari: Levian Bay (`world 1387`) |
| Level 50 (`CreationStart = 5`) | Thayd capital start | Illium capital start |

The Rider's Reef finale uses departure terminals for the same four surface
starter zones: Exile terminals route to Everstar Grove or Northern Wilds, while
Dominion terminals route to Crimson Isle or Levian Bay. The routing helper also
pins the destination welcome quests (`9113`, `9112`, `9127`, `9126`) that unlock
from final Rider's Reef quest completion. Public archived
WildStar docs describe **Gambler's Ruin** (Exile) and **Destiny** (Dominion) as
older level 1-3 arkship training zones with exits to those planet starters, and
note that the arkship tutorial could be skipped after the free-to-play/Reloaded
starting-flow changes. Treat Gambler's Ruin and Destiny as historical/older
patch restoration targets unless a task explicitly asks for pre-16042 arkship
parity.

## Northern Wilds current state (2026-05-25)

Table-backed chain coverage now pins the 20-quest Human/Granok Veteran block:
`3479`, `3480`, `3486`, `3487`, `3667`, `3668`, `3670`, `3671`, `3673`,
`3741`, `3777`, `3781`, `3783`, `3797`, `3886`, `3963`, `4526`, `4666`,
`4667`, and `4696`. Shared `QuestManager` lifecycle coverage now preserves the
auto-achieved state for zero-objective rows after accept and covers
visible-receiver turn-in, representative item rewards, and shared
quest-complete achievement hooks, with focused `QuestTests` passing 6/6 and
pinning Q3670 and Q3783 as representative Northern Wilds no-objective quests
pending row-specific receiver/dialog, reward, and client smoke.
Q3479 and Q3480 now have focused-test-backed availability evidence for their
shared Trapped Survivor CSI objective and shared yeti kill target objective.
`NorthernWildsMapScript` spawns three build 16042 Creature2 `11070` (`Trapped
Survivor`) fallbacks at reviewed DataMapping source-coordinate ids `3406`,
`3407`, and `15032` in world `426` / area `646`, enough for the count-3
`SucceedCSI` objectives `4467` and `4470`. The same map script now also spawns
eight reviewed shared-yeti fallbacks for Creature2 `11945` (`Yeti Snowstalker`)
and `11948` (`Yeti Frostclaw`) across the `12155`/`12156`/`12157`/`12158`
indicator areas, covering Q3479 objective `4564` through nested TargetGroup
`7288` -> `1463` and Q3480 objective `4565` through TargetGroup `1463`.
Generic interaction coverage credits `QuestObjectiveType.SucceedCSI`, and
generic target-group kill coverage credits the yeti member IDs. The content
tracker records the matching objective, WorldLocation2, nested target-group, and
creature-relation rows as tested runtime evidence. Q3479/Q3480 remain pending
for client activate/CSI smoke, live/client combat kill smoke, phase/visibility
smoke, respawn/tap/threat behavior, loot/reward/achievement side effects, full
map-guidance review, and end-to-end quest smoke.
Q3486 now has focused-test-backed server availability and credit coverage for
the Empowered Tower objective loop. `NorthernWildsMapScript` covers the shared
Master Control Panel (`11194`) at Quest2 receiver WorldLocation2 `7778`, the
Loftite Crystal fallback at objective location `7807`, and five Crystal
Guardian (`11925`) fallbacks in Exo-Lab 729 from DataMapping source-coordinate
ids `7798992`, `7798993`, `7855831`, `7857737`, and `8102237`. The Crystal
Guardian script now credits `QuestObjectiveType.VirtualCollect` for virtual
item `206` on kill while Q3486 is accepted, and the same script filter now
routes Frostbite (`11924`) target-group kills through that credit path, matching
client objective `4485` and TargetGroup `4985`; the content tracker records the
receiver, objective, target-group-member, script, and creature-relation rows as
runtime-tested or script-credit evidence but not retail-complete. Remaining blockers are client-visible panel
dialog/activation smoke, exact crystal collision/respawn timing, live combat
smoke, Frostbite `11924` runtime spawn/drop proof, reward
UI/inventory/cash persistence, achievement UI/progression, and end-to-end
Q3486/Q3671/Q3668 chain smoke.
Q3783 now has focused-test-backed item-start/no-objective turn-in coverage:
Item2 `6954` starts the quest, Quest2 receiver WorldLocation2 `7727` points to
Land's Reach, `NorthernWildsMapScript` fallback-spawns build 16042 Commander
Durek (`11066`) at that receiver location, and QuestManager coverage completes
the achieved quest at visible `11066` while granting Quest2Reward row `8830`
item `17756`.
Q3797 now has focused-test-backed server availability, generic credit-path,
and selectable reward-path coverage for the Settler's Reach cleanup step.
`NorthernWildsMapScript` covers Land's Reach Commander Durek (`11066`) at
Quest2 receiver WorldLocation2 `7727`; Creature2 `11066` carries Q3797 in
`QuestIdGiven` and `QuestIdReceive`, while `GlobalQuestManager` still preserves
the reviewed DataMapping `11061` starter/finisher override for ambiguous
relations `166` and `1693`. The same map script fallback-spawns eight reviewed
Rootbrute/Yeti objective targets for objective `4918` (`KillTargetGroups`,
TargetGroup `1177`, count `8`), and `AssetManager` expands both creature-id
and creature-list target groups for quest-objective target matching, so the
generic kill-credit path covers the spawned TargetGroup `6406` members. Focused
completion coverage also grants Quest2Reward rows `2480`/`2482`/`4770` to
Item2 `13333`/`27870`/`27871` plus cash `145`. The content tracker records the
matching receiver, indicator, objective, target-group-member, script,
prerequisite, and creature-relation rows as runtime-tested but not
retail-complete. Remaining blockers are client-visible item-start and Durek
dialog/accept/completion activation smoke, Q3486 prerequisite flow smoke,
exact placement/density/respawn/tap/threat behavior, type-9/non-spawned branch
review, loot, reward UI/persistence, achievement UI/progression, and
end-to-end Q3783/Q3797 smoke.
Q3668 now has focused-test-backed server availability and generic credit
coverage for the Indigenous Intelligence route. `NorthernWildsMapScript`
fallback-spawns Bartol Sunward (`12737`) at Quest2 receiver WorldLocation2
`9121`, Scientist Lusk (`12484`) at objective indicator WorldLocation2 `8857`,
seven Imprisoned Survivor (`14124`) placements for TargetGroup `7261`, and
reviewed Skeech members `36429`, `17545`, `11907`, `11910`, `11912`, `11917`,
and `36884` for nested TargetGroup `7293`. The generic TalkTo, ActivateEntity
target-group, and KillTargetGroups paths cover those spawned entities, and the
content tracker records the matching receiver, objective, target-group, script,
and creature-relation rows as runtime-tested but not retail-complete. Focused
tests also prove `AssetManager` expands the full `7293 -> 7292/960` chain and
that unplaced members `14054` and `11913` would credit objective `4791` if
killed. Remaining blockers are client-visible Deadeye/Lusk/Bartol dialog and
activation smoke, exact survivor transform/despawn behavior, reviewed
spawn/placement proof for `14054` and `11913`, reward-choice UI/inventory
validation, achievement/progression side effects, and end-to-end
Q3486/Q3671/Q3668 chain smoke.
Q3670 now also has focused-test-backed primary and Galeras alternate receiver
availability. `NorthernWildsMapScript` spawns build 16042 Creature2 `11063`
(Deadeye Brightland) at the primary Quest2 receiver location `29526`, matching
reviewed DataMapping `source_coordinate_id 5142` in world `426` / area `597`
near `4486,-724,-5391`. `GalerasMapScript` now spawns build 16042 Creature2
`16622` at alternate Quest2 receiver location `12354`, matching reviewed
unpromoted DataMapping `source_coordinate_id 837` in world `51` / area `23`
near `3833,-1018,-4642`; the same Creature2 also has promoted runtime seed
coverage at reviewed source coordinate `2587271`. Build 16042 prerequisite
`29356` is `InSubZone` value `13`, and local WorldZone data confirms Tremor
Ridge zone `23` parents through `459` to `13`. `GlobalQuestManager` adds
reviewed Q3670 receiver overrides for Creature2 `11063` and `16622` when those
client rows are loaded, so the real receiver cache now matches the reviewed
Deadeye finisher relations instead of relying on manually injected test
receivers. The generated content tracker records the primary receiver-location
row as
`runtime_script_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke`,
the Galeras alternate receiver row as
`runtime_q3670_alt_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke`,
and the two Deadeye Brightland finisher relations as receiver-cache-tested
runtime rows. Q3670 remains pending for client-visible Deadeye dialog/completion
activation smoke, prerequisite chain routing from Q3673, prerequisite-selection
UI smoke for the Galeras path, exact reward and achievement side effects, and
end-to-end quest smoke.
Q3671 now has focused-test-backed primary receiver availability and
visible-receiver completion evidence. `NorthernWildsMapScript` spawns build
16042 Creature2 `11063` (Deadeye Brightland) at the Quest2 receiver location
`9340`, matching WorldLocation2 position `4341.27,-751.765,-5682.93` and
reviewed DataMapping `source_coordinate_id 4368` in world `426` / area `597`.
Creature2 `11063` carries both `QuestIdGiven` and `QuestIdReceive` `3671`, so
the standard quest giver/receiver cache covers the route once Deadeye is
visible. Shared no-objective `QuestManager` tests now cover Q3671's
accept-to-`Achieved` lifecycle, visible Deadeye completion, Quest2Reward row
`6662`, and item `19131` (`Life and Death`) grant. The generated content
tracker records the Q3671 primary receiver row as
`runtime_q3671_deadeye_receiver_location_spawn_and_quest_lifecycle_tested_pending_dialog_client_smoke`,
the Deadeye starter/finisher relations as receiver-location-tested runtime
rows, and reward row `6662` as
`runtime_q3671_item_reward_completion_grant_tested_pending_client_smoke`.
Q3671 remains pending for client-visible Deadeye dialog/accept/completion
activation smoke, Q3486/Q3671/Q3668 chain smoke, alternate-receiver routing for
location `12354` / prerequisite `29356`, reward UI/persistence smoke,
achievement/progression side effects, and end-to-end quest smoke.
Q3673 now has focused-test-backed availability and completion-gate coverage for
the Northern Wilds landing-site signal flares: client QuestObjective rows
`4888`, `4889`, and `4890` point to Creature2 ids `12521`, `13150`, and
`13151`; TargetGroup `1106` orders those same flare ids for checklist objective
`4748`; and `NorthernWildsMapScript` spawns them at world locations `8867`,
`8868`, and `8869` with checklist indexes `0`, `1`, and `2`. Generic
interaction coverage pins `SucceedCSI` and `ActivateTargetGroupChecklist`
objective updates, while `Q3673ContactWithThaydQuestScript` credits hidden
required objective `13391` after checklist objective `4748` completes, matching
the Quest2 objective order without a global data=0 activation rule. The content
tracker records Q3673's Deadeye starter/finisher rows, primary receiver, flare
objective rows, target-group members, hidden objective, and quest script row with
tested runtime evidence. Q3673 remains pending for client-visible Deadeye
dialog/activation smoke, exact activate/CSI packet smoke,
achieved-state/cinematic timing smoke, prerequisite flow from Q3886, alternate
receiver routing for location `12354` / prerequisite `29356`,
reward-choice UI/persistence and achievement UI smoke, and end-to-end quest
smoke.
The pass added implemented scripts for the previously
missing `3741`, `3777`, `3781`, `3783`, `4666`, `4667`, and Q4696 rows, and
Q4526 now has focused-test-backed server-side wreckage/anomaly credit plus
Scientist Pristan receiver-location spawn coverage at Quest2 WorldLocation2
`11669` while still pending client smoke. Q4696 objective `6508` now has
row-specific server playability coverage for the build 16042 `ActivateEntity`
`data=1024` / count `5` objective with reward-pane TargetGroup `4323`.
`AssetManagerTargetGroupTests` and `QuestObjectiveTests` pin the target-group
expansion to Creature2 `17189` and `19595`, while
`Q4696TempleRetreatEnemyEntityScript` credits
`QuestObjectiveType.ActivateEntity` for either member on kill while Q4696 is
accepted. The tracker now correlates member `19595` with `67` promoted
`runtime_world_seed.sql` Stormwing Vanquisher rows in world `51` / area `976`,
and records member `17189` as the client-labeled Q4696 script-spawn creature
with Galeras map-load fallback coverage at objective indicator world locations
`17586`, `17588`, and `17589`; live/client combat objective smoke remains
pending. The
quest creature/NPC tracker also records Q4696
finisher `17053` as one promoted `runtime_world_seed.sql` Corporal Darby row in
world `51` / area `973`, and objective creature `19595` as the same `67`
promoted rows. Focused `GlobalQuestManager` receiver-cache coverage now pins
`17053` as the Q4696 finisher, and focused `QuestManager` completion-path
coverage requires visible Darby instead of starter `17175`; the tracker also
records reviewed DataMapping `source_coordinate_id` `10020` and exact promoted
runtime seed entity `1000010020` placement at world `51` / area `973` /
position `6203,-888,-2809`. `GalerasMapScript` now has a focused-test-backed
fallback spawn for Darby at Quest2 receiver WorldLocation2 `17590`
(`6204.08,-888.474,-2809.91`), with the nearby-creature search preventing
duplicate Darby spawns when the runtime seed is present; the tracker also
records reviewed DataMapping `source_coordinate_id` `99084` as an unpromoted
Darby placement gap at world `51` / area `973` / position `6176,-880,-2910`.
The Q4696 starter row now corrects the ambiguous
DataMapping `11061` bridge to client Creature2 `17175`, the Galeras-specific
Commander Durek with `QuestIdGiven` `4696`, and `GalerasMapScript` has a tested
fallback spawn at DataMapping `source_coordinate_id` `9972`; focused
`GlobalQuestManager` cache coverage also
pins `17175` as the Q4696 starter instead of `11061`, and focused
`QuestManager` accept-path coverage now pins visible `17175`, completed Q4667,
Exile faction, and level 16 requirements.
Q4696 now uses the standard follow-up quest script handoff to Q4694, matching
the client `Quest2` prerequisite chain (`4667 -> 4696 -> 4694`) and focused
`NorthernWildsQuestChainTests` coverage.
Q4694 now has a tracker-backed Galeras map-load fallback for build 16042
Creature2 `50904`, the Trooper Vog holdout flag unit with `QuestIdReceive`
`4694`, at the Crosswind Fields DataMapping placement near source-coordinate
ids `10201` / `3742220` (`6202,-888,-2816`). The fallback makes the immediate
Q4694 interaction dependency present in world `51`. Q4694 objective `16116`
now has a focused-test-backed holdout kill-credit producer: killing build 16042
Creature2 `23953`, the client-labeled Q4694 Stormwing Falkrin, credits
`QuestObjectiveType.CompleteEvent` data `111` only while Q4694 is accepted;
`runtime_world_seed.sql` promotes `93` Creature2 `23953` rows in world `51` /
area `973`, including `23` rows near Trooper Vog's Crosswind Fields placement,
so the follow-on kill target is present in the current runtime seed.
Q4694 objective `16115` also has server-side activation credit coverage: direct
`ClientActivateUnit` success on Trooper Vog credits `QuestObjectiveType.ActivateEntity`
data `50904`, and Creature2 `50904` points to activate spell `50151` with a
`Spell4Effects` `Activate` row for the activate-cast path. This does not claim
retail completion: client-visible Vog dialog/activation smoke, exact client
packet path confirmation, holdout wave/spawn timing, Murgh/ally participation,
reward side effects, and client/manual quest smoke remain open.
The content tracker also emits `quest_faction` rows for Exile/Dominion
`QuestPlayerFactionEnum` gates and records Q4696-specific server accept-path
evidence for its level, faction, and required-quest rows. Q4696 objective `6457` now
has the generic `EnterArea` world-location
trigger credit path pinned by
`Source/NexusForever.Game.Tests/Entity/VolumeGridTriggerEntityTests.cs` for
world location `12649`, and `GalerasMapScript` now spawns that trigger plus the
Darby receiver, three script-spawn scouts, and Q4694 Trooper Vog fallback on
world-51 map load with coverage in
`Source/NexusForever.Game.Tests/Quests/GalerasMapScriptTests.cs`.
Q4526, Q4696 Galeras gameplay, and Q4694 follow-up are still not retail-complete:
Q4526 now has server-side wreckage credit at world location `9706`, a Scientist
Pristan fallback spawn at Quest2 receiver WorldLocation2 `11669` matching
reviewed DataMapping `source_coordinate_id 116070`, a Spatial Anomaly fallback
spawn, and anomaly activation/proximity catch credit, but still needs exact
Spatial Anomaly movement/chase/despawn proof, client-visible Pristan
dialog/accept/completion smoke, activation/proximity packet smoke, rewards, and
end-to-end quest smoke.
Q4696 still needs exact spawn/despawn and quest-state visibility timing, Durek dialog activation/client-visible
prerequisite-denial/phase validation, Darby completion dialog/client activation
smoke, live/client target-objective smoke, map guidance/client quest smoke,
and Temple of Osiric retreat/rally flow behavior; Q4694 still needs Trooper Vog
client-visible dialog/activation smoke, exact client packet path confirmation,
exact holdout wave mechanics, Murgh/ally participation, rewards, and
client/manual smoke before those rows can move out
of `not_retail_complete`. Focused Q4526 NorthernWilds branch coverage passes
19/19. Focused Q4696
quest/objective/target-group/cache/accept-path/completion-path coverage passes,
and the full `NexusForever.Game.Tests` project passes 3295/3295.
Implemented rows include
Jabbithole/objective-table-backed credit for Exile Supply Crates
(`4813`), Loftite Crystals (`5076` plus item `6998` collection credit), and
Q3781 Dead Exile Soldier starter (`50668`), Galeras Deadeye receiver (`16622`),
and Captive Exile Soldier rescue coverage (`4880` / TargetGroup `2143` members
`12537`, `21015`, and `21016`). Focused regressions cover the Q3486 and Q3673
cinematic/quest-handoff boundaries in
`Source/NexusForever.Game.Tests/Quests/NorthernWildsQuestChainTests.cs`, and
`Source/NexusForever.Game.Tests/Quests/EarlyZoneEntityObjectiveCreditTests.cs`
pins Q3667 control-panel objective `4770`, Q3487 cannon checklist credit,
Q3741 crate credit, Q3777 crystal credit, and Q3781 three-member captive rescue
credit. `GlobalQuestManagerPartialTableTests` and `QuestTests` also pin the
Q3781 reviewed starter/receiver override cache plus visible-starter accept and
visible-Deadeye completion paths.

Northern Wilds event/challenge/path state from the 2026-05-25 Veteran log pass:

- Public event: Jabbithole zone `1` maps one event, `154` Dominion Ultrabot,
  with objective `371` ("Defeat the Dominion Ultrabot in Camp Icefury"). The
  Northern Wilds map script now creates/joins the event in Camp Icefury
  (`WorldZone 602`), and `DominionUltrabotPublicEventScript` credits objective
  `371` when creature `12526` dies.
- Challenges: Jabbithole zone `1` maps `103` Skeech Slayer and `105`
  Rootbrute Slayer. Client `Challenge.target` values for these are target-group
  IDs (`8851`, `1852`), so `ChallengeManager` now resolves direct and nested
  `TargetGroup.tbl` creature lists before advancing combat challenges. The
  logged `ChallengeRequirement` prerequisite warning is now backed by a
  completion-count check against `IChallengeManager.GetCompletionCount`.
- Path missions: client `PathMission.pathEpisodeId` yields the 13 Northern
  Wilds missions (`33`, `34`, `156`, `35`, `36`, `158`, `1254`, `42`, `160`,
  `648`, `650`, `651`, `652`). The map script activates the correct episode
  and Jabbithole XP value (`25`) for the player's active path, while Explorer
  progress and power-map client reports can now complete active missions through
  `PathManager`. Soldier tower-defense build packets now resolve
  `PathSoldierTowerDefense -> PathSoldierEvent -> PathMission`, and Settler
  build-tier packets resolve `PathSettlerImprovementGroup -> PathSettlerHub ->
  PathMission`. Jabbithole `path_mission_creatures` now backs the Northern
  Wilds Soldier control-point hook:
  `Source/NexusForever.Script.Main/Quests/NorthernWilds/NorthernWildsSoldierPathMissionScripts.cs`
  starts the table-backed PathSoldierEvent lifecycle for missions `33`, `34`,
  and `156` from Creature2 control points `11139`, `11141`, and `12508`,
  emits `ServerPathSoldierHoldoutStatus`, `ServerPathSoldierHoldOutNextWave`,
  and delayed `ServerPathSoldierHoldoutEnd` success packets from client
  `PathSoldierEvent`/`PathSoldierEventWave` timing rows, and completes through
  active `PathMission.objectId` ownership instead of completing immediately on
  beacon activation. Actual Soldier creature spawning, defended survivor health,
  leave/death/timeout/participation failure states, and local client smoke
  remain blocked. Jabbithole `path_mission_creatures` also backs the Northern
  Wilds Scientist runtime hook: `Source/NexusForever.Script.Main/Quests/NorthernWilds/NorthernWildsScientistPathMissionScripts.cs`
  emits `ServerPathScientistAddCreatureInfoToCreature` and
  `ServerPathScientistUnitScanParameters` for mapped mission creatures and
  completes missions `42`, `160`, and `648` on activation success. Generic
  Soldier spawn simulation and the generic Scientist scan-result packet remain
  mapped-only pending packet/result evidence.
- NPC data: Jabbithole has `164` enabled Northern Wilds creatures. The staging
  bridge now maps `161` of them to client Creature2 IDs and leaves `3`
  unmatched (`Ability Training Kiosk`, `Invisible Channel Unit`, and `Unknown`).
  `Tools/DataMapping/sql/apply_safe_world_imports_from_staging.sql` now has
  opt-in entity-spawn promotion from `nf_map_world_entity_candidate` plus a
  targeted direct Jabbithole coordinate fallback for source rows whose
  `worldid` is blank but whose coordinates resolve near staged world geometry.
  Local verification promoted `4,375` mapped entity rows (`1000000000 +
  source_coordinate_id`) and `15,423` per-spawn stat rows across world `426`
  plus the quest-relevant world `51` subareas `23`, `973`, and `976`. Northern
  Wilds runtime world `426` coverage rose from `90` to `161` enabled
  Jabbithole creatures. The reviewed local bridge aliases were `1055`
  Quartermaster Windward -> `11705` Supply Officer Windward, `3351` Basic
  Critical Strike Station -> `25876` Basic Critical Hit Station, and `11313`
  Hacked Dominion Turret -> `18680` Inactive Dominion Turret. The remaining `3`
  enabled zone-1 entries need stronger Creature2 evidence before runtime
  promotion; this is the accepted safe stop boundary for the Veteran Northern
  Wilds pass.

## Crimson Isle current state (2026-06-13)

Table-backed chain coverage now pins the branch roots and merge gates:
Q5595 grants Q5575; Q5593 grants Q5573 and Q8855; Q5573 or Q5575 can unlock
Q5596 via `preq_flags = 1`; Q5596 grants Q5597; Q5597 grants Q5604; Q5604
grants both Q5580 and Q5583; Q5594 is only granted after both Q5580 and Q5583
are complete. Q5594's final warbot kill now credits objective `8249` and guards
achievement `1730` only while Q5594 is accepted. Focused regressions live in
`Source/NexusForever.Game.Tests/Quests/CrimsonIsleQuestChainTests.cs`.
`Source/NexusForever.Game.Tests/Quests/EarlyZoneEntityObjectiveCreditTests.cs`
also pins Q8855 Dominion soldier activation/despawn objective credit.
Shared zero-objective lifecycle coverage now also pins Q5610 accept-to-achieved
and visible-receiver completion behavior through `QuestManager`. Q5610 now also
has a focused-test-backed `CrimsonIsleMapScript` fallback for build 16042
Creature2 `24187` (Mondo Zax) at the primary Quest2 receiver location `17803`,
matching reviewed DataMapping `source_coordinate_id 15089` in world `870` /
area `1885` near `-7664,-942,-672`. The generated content tracker records the
receiver-location row as
`runtime_script_receiver_location_spawn_tested_pending_dialog_client_smoke` and
the Mondo Zax finisher relation as
`runtime_script_spawn_present_client_creature2_finisher_relation_tested_pending_dialog_smoke`.
Crimson Isle Q5610 remains pending for client-visible Mondo dialog/completion
activation smoke, alternate receiver prerequisite routing, exact reward-choice
and cash side effects, achievement/progression side effects, and end-to-end
quest smoke.

Q5596 now has a server-side runtime restoration slice for the dead Dominion
Demolitions Expert search step. `CrimsonIsleMapScript` fallback-spawns six
build-16042/LaughingWS-backed `Creature2 24703` interactables at Crash Site
Alpha in world `870`, with `QuestChecklistIdx` `1..6` and a tight duplicate
placement check so nearby checklist rows are not collapsed. Focused tests pin
the six spawn positions/rotations/checklist indexes and prove QuestObjective
`8468` (`ActivateTargetGroupChecklist`, TargetGroup `2619`, count `3`) completes
from the restored checklist targets. Q5596 remains not retail-complete until
client smoke covers dead-expert activation UI, Mondo Zax dialogs, reward choice
and inventory persistence, achievement `4134`, exact crash-site guidance/timing,
and full Q5573/Q5575->Q5596->Q5597 flow.

Q5597 now has a server-side runtime restoration slice for the Chua Explosives
collection step. `CrimsonIsleMapScript` fallback-spawns `30` reviewed build
16042 `Creature2 24286` Chua Explosives placements in Scarhide Camp from
DataMapping source_coordinate_ids `15489`-`15494`, `15857`-`15862`, `38874`,
`51929`, `59776`-`59781`, `64334`, `101493`-`101494`, `127169`,
`134970`-`134972`, `136408`, `136864`, and `147754`, and
`Q5597ChuaExplosivesEntityScript`
credits QuestObjective `8256` as `VirtualCollect` item `364` once per spawned
object before removing the collected entity. The generated tracker records the
Jabbithole objective bridge row `1455`, TargetGroup `4373`, the Mondo Zax
`24187` starter/finisher relations at receiver WorldLocation2 `17803`, and the
Q5604 follow-up handoff. Q5597 remains not retail-complete until client smoke
covers Mondo dialog, explosives activation/UI, exact retail density
confirmation, respawn/despawn behavior, rewards, achievements, inventory
persistence, and full
Q5596->Q5597->Q5604 flow.

Q5604 now has a server-side runtime restoration slice for the Tactical
Demolitions cannon step. `CrimsonIsleMapScript` fallback-spawns the three
reviewed build 16042 `Creature2 24298` Exile Anti-Air Cannon Simple rows from
DataMapping source_coordinate_ids `15316`-`15318` at the Megatech Gun
Emplacement, with checklist indexes `1..3` and activePropIds `1085356`,
`1085429`, and `1081959` aligned to
`Tools/DataMapping/sql/laughingws_small_world_wip_seed.sql`. Generic direct
interaction and activate-spell paths credit QuestObjective `8268`
(`ActivateTargetGroupChecklist`, TargetGroup `2546`, count `3`), while
`Q5604TacticalDemolitionsQuestScript` queues the completion cinematic, credits
hidden objective `15918`, and grants Q5580/Q5583 on completion. Q5604 remains
not retail-complete until client smoke covers exact activate/destroy packets,
active-prop visual/despawn/respawn behavior, rewards/progression side effects,
and full Q5597->Q5604->Q5580/Q5583 flow.

Q5580 now has a server-side runtime restoration slice for the Enforced Radio
Silence tower-controls step. `CrimsonIsleMapScript` fallback-spawns the two
reviewed build 16042 `Creature2 26559` Tower Controls Simple rows from
DataMapping source_coordinate_ids `15122` and `15123` at Megatech Station, with
checklist indexes `1..2` and activePropIds `1137353` and `1137404` aligned to
the safe-world import verifier. Generic direct interaction and activate-spell
paths credit QuestObjective `8227` (`ActivateTargetGroupChecklist`,
TargetGroup `2889`, count `2`), while `Q5580QuestScript` grants Q5594 only
after both Q5580 and Q5583 are completed. Q5580 remains not retail-complete
until client smoke covers exact activation/objective UI behavior, active-prop
visual/despawn/respawn behavior, Kezrek Warbringer dialog and turn-in,
reward/progression side effects, and full Q5604->Q5580/Q5583->Q5594 flow.

Q5583 now has a server-side runtime restoration slice for the Heavy Armor tank
and Megatech kill steps. `CrimsonIsleMapScript` fallback-spawns reviewed build
16042 target-group members for objective `8231` (`Hellfire Tank 24255` and
`Vindicator Tank 24452`) plus six reviewed members for objective `8372`
(`Megatech Gunner 24030`, `Megatech Swordsmaster 26591`, `Megatech Warbot
31792`, `Megatech Merc 31864`, `Megatech Contractor 31895`, and `Megatech
Shock Trooper 38228`). `AssetManagerTargetGroupTests` pins exact TargetGroup
`3737` and nested TargetGroup `3812 -> 2536/3811/12540` expansion, while generic
`UnitEntity` kill-credit coverage provides the server objective producer.
`Q5594WarbotEntityScript` now requires accepted Q5594 before granting the shared
warbot's Q5594 objective/achievement credit, keeping Q5583 kills isolated.
Q5583 remains not retail-complete until client tank/combat smoke, full target
density review, Kezrek Warbringer dialog and turn-in, rewards/progression,
achievement UI, and full Q5604->Q5580/Q5583->Q5594 smoke are verified.

## Rider's Reef current state (2026-05-25)

Rider's Reef is not complete. Verified code paths now include tutorial
communicator checkpoint ordering, combat-projector quest delta recovery,
map-cache fallback anchoring, delta-only tutorial quest initialisation, build 16042
character-create start rows, tutorial quest follow-up grants, terminal-to-world
routing, terminal fallback behavior, destination welcome-quest grants, terminal
final quest completion before visible surface receiver, no-duplicate/no-teleport
guards, terminal recording on spell/direct activation,
direct activation checklist credit for the actual departure checklist
interactables, per-player terminal storage,
known-terminal filtering, cross-faction/missing terminal default routing,
both-faction starter quest grants and follow-up re-entry root-skip behavior,
logout/re-entry recovery boundary excluding the final departure quests from
auto-completion,
table-backed reward coverage for cryopod Omnibit and final-departure item
rewards,
fallback-spawned departure terminals/checklist
interactables, combat lane faction overrides, and
faction-specific projector text. Focused tests live
under
`Source/NexusForever.Game.Tests/Map` and
`Source/NexusForever.Game.Tests/Quests`, with the spell-activate hook pinned in
`Source/NexusForever.Game.Tests/Spell`.

Timestamped evidence matrix:
`Decomp/Analysis/coverage/RIDERS_REEF_EVIDENCE_MATRIX_2026-05-25.md`.

Remaining gameplay verification:

- Exile and Dominion client playthroughs for quest acceptance, objective
  updates, turn-in UI, rewards, and chain handoff.
- Combat and hoverboard loops for respawn/re-entry, kill credit, CSI/projector
  reliability, and final terminal/checklist behavior.
- Retail VO/timing comparison against tutorial video evidence and runtime logs.
- Read-only local DB/log audit on 2026-05-25 proved only partial early NPE
  coverage: 28 local characters were still on `worldId=3460`; one local
  character was on `426` at the Human/Granok Veteran start coordinates; no
  character rows were on `990`, `870`, or `1387`, and no persisted `10528`,
  `10530`, `9112`, `9113`, `9126`, or `9127` rows were found.
- Initial-chain correction on 2026-05-25 now grants only the movement root
  (`10513` Exile / `10521` Dominion) on fresh Rider's Reef entry; the paired
  hoverboard quest (`10527` / `10532`) is recovered only after the movement root
  is completed. The focused Rider's Reef filter passed 79/79 after the change,
  and the narrow map/chain slice passed 17/17. Auth/World restart was clean, but
  full Exile/Dominion client playthrough smoke remains blocked for native-client
  automation safety.
- Follow-up correction from live UI smoke on 2026-05-25 removed the mid-play
  `ServerQuestInit` refresh after the movement root auto-advances into the
  hoverboard quest. `QuestAdd` already emits the live state delta; avoiding the
  extra quest snapshot keeps the "three points -> platform -> projector" opening
  beat to one client-visible tutorial chain. `QuestManager.SendInitialPackets`
  still suppresses the completed movement root during login snapshots when the
  paired hoverboard quest is active. The latest focused quest/map/chain/projector
  filter passed 35/35 and the broader `FullyQualifiedName~Tutorial` filter
  passed 74/74.
- Hoverboard presentation/recovery pass on 2026-05-25 added table-backed
  objective-ring lightning FX (`82460`), projector-completion trail start
  (`82298`) when `10527`/`10532` grants the hoverboard, booster speed/visual casts
  (`85424`/`82298`) plus a stronger forward boost, finish snap to
  `WorldLocation2 51734`, direct remount recovery through hoverboard equip
  `85562` while the ride objective is incomplete, and mounted-vehicle
  ring/booster pilot targeting regressions. A follow-up removes the wrong
  opening step-pad `81662` scan cast, keeps a narrow suppression for only
  `81662`'s server-applied `CCState.Disable` effect after live smoke showed it
  presenting "Disable" and stranding movement, and leaves the exact opening pad
  player pose/SFX blocked pending direct producer evidence. Follow-up live
  validation narrowed ring contact back to `82460` only and leaves `84387`
  Power Boost pulse/trail/speed mapped-only until the exact retail producer is
  proven; it also makes projector activation robust by casting/falling back to
  hoverboard equip `85562` from both direct and cast activation handlers. A
  follow-up finish-snap correction
  now also snaps to `51734` when the ride objective was completed by the normal
  area-objective sync before recovery sees it, as long as the finish
  interactable objective is still incomplete. Focused effect/communicator tests
  passed 20/20 after rebuilding `Game`, `Script.Main`, and `Game.Tests`; the
  finish-snap/hoverboard/opening-chain filter passed 58/58, the broader
  `FullyQualifiedName~Tutorial` filter passed 66/66, the scan-disable focused
  filter passed 22/22, the ring Power Boost remap focused filter passed 23/23,
  the projector/ring correction filter passed 11/11, the broader correction
  slice passed 82/82, the rebuilt tutorial filter passed 78/78, and the owning
  `Game`/`Script.Main` builds succeeded in the earlier same-day pass.

## Rider's Reef Codex boundary (2026-05-25)

- Tutorial quests `10513..10541` can be active, tracked, and objective-synced, but they are not ordinary browseable quest-log/Codex rows in the stock client.
- Probe result: after moving `ServerQuestInit` to the post-`ClientEnteredWorld` bootstrap and replaying quest state/objective deltas, an injected comparison quest (`6708`) appeared in the Codex while the Rider's Reef chain still did not.
- Client data cause: the Rider's Reef rows in `wildstar_client.Quest2` have `groupId = 0`, `questCategoryId = 0`, `questContentFinderTypeEnum = 0`, and no `EpisodeQuest` rows. Treat them as HUD task-style tutorial content unless client tables or client UI logic are patched.

## Blocked objective families (remaining)

No quest objectives use unsupported types 19, 27, or 29 in client data. Primal Matrix
UI open packets remain unmapped; `BeginMatrix` (type 48) uses essence-grant sync above.

Reward gap: **86** `RotationEssence` (`Quest2Reward` type 10) rows need active
reward-rotation schedule support.

## Regenerate reports

From the repository root, with `wildstar_client_mysql` present:

```powershell
# Quest coverage buckets (this tracker + coverage/QUEST_IMPLEMENTATION_AUDIT.md)
python Tools\WikiArchiveAudit\quest_implementation_audit.py

# Row-level quest and dungeon/raid retail-completeness inventories
python Tools\WikiArchiveAudit\content_retail_completeness_audit.py

# Wiki + lifecycle/objective/reward surface audit (optional wiki archive)
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --domain quests `
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18 `
  --output Tools\WikiArchiveAudit\reports\quests-wiki-audit.md
```

## `artifacts/` vs tracked files

| Location | Git | Keep? |
| --- | --- | --- |
| `artifacts/` | ignored | Local scratch: wiki dump, load-test JSON, build `verify-obj`, spell/loot capture JSON |
| `Tools/WikiArchiveAudit/reports/` | tracked | Committed audit snapshots (wiki/lifecycle) |
| `Decomp/Analysis/coverage/QUEST_IMPLEMENTATION_AUDIT.md` | tracked | Generated quest bucket inventory |
| `Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md` | tracked | Row-level retail-completeness rollup |
| `Decomp/Analysis/coverage/content_retail_completeness_quests.csv` | tracked | Generated per-Quest2 evidence/status inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_objectives.csv` | tracked | Generated per-objective dependency/status inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_rewards.csv` | tracked | Generated quest reward and pushed-item dependency/status inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_prerequisites.csv` | tracked | Generated direct Quest2 and Prerequisite.tbl dependency/status inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_loot.csv` | tracked | Generated reviewed LaughingWS quest-loot group/item/entity dependency inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_world_dependencies.csv` | tracked | Generated quest world-zone, receiver, direction, location, and target-group dependency inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_achievements.csv` | tracked | Generated client Achievement/AchievementChecklist quest progression dependency inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_scripts.csv` | tracked | Generated Script.Main owner-hook inventory for current Quest2 scripts and related runtime owner ids |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_creatures.csv` | tracked | Generated DataMapping starter, finisher, objective creature/NPC relationship, source-coordinate, and promoted runtime placement-match inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_objective_evidence.csv` | tracked | Generated DataMapping/Jabbithole quest objective text/order evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_reward_evidence.csv` | tracked | Generated DataMapping/Jabbithole quest item, currency, reputation, and tradeskill reward evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_zone_evidence.csv` | tracked | Generated DataMapping/Jabbithole quest zone and call-zone evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_episode_evidence.csv` | tracked | Generated DataMapping quest episode order/progression evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_public_event_evidence.csv` | tracked | Generated DataMapping public-event objective, mission, zone, and creature evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_public_event_auxiliary_evidence.csv` | tracked | Generated public-event depot, virtual-item depot, stat, reward-modifier, bomb, gather-resource, state, and vote evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_challenge_evidence.csv` | tracked | Generated DataMapping challenge definition, creature target, and reward item evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_path_mission_evidence.csv` | tracked | Generated DataMapping path mission definition, episode, creature target, reward, and zone evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_contract_evidence.csv` | tracked | Generated DataMapping contract Quest2, creature target, and reward item evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_instance_portal_evidence.csv` | tracked | Generated InstancePortal definition, Creature2 portal link, and DataMapping placement/review evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_script_presentation_evidence.csv` | tracked | Generated script communicator message, cinematic queue, runtime payload/link, and cinematic-finish hook evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_script_event_flow_evidence.csv` | tracked | Generated script PublicEventObjective/PublicEventPhase reference, owner-link, and client event/objective linkage evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_script_objective_producer_evidence.csv` | tracked | Generated script ActivateObjective, UpdateObjective, optional-objective/range-seeding helper, and PublicEventObjectiveCreditEntityScript producer linkage evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_script_quest_progression_evidence.csv` | tracked | Generated Script.Main ObjectiveUpdate, QuestAchieve, QuestAdd, QuestMention, and achievement-grant progression evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_script_runtime_action_evidence.csv` | tracked | Generated script spell cast, teleport, entity lifecycle, and public-event finish runtime-action evidence inventory, including literal/constant Spell4 and World client-row linkage plus source traces for routed dynamic references |
| `Decomp/Analysis/coverage/content_retail_completeness_instance_script_handler_evidence.csv` | tracked | Generated dungeon/raid/expedition script On* lifecycle, public-event, map-entity, range-trigger, cinematic, combat, callback, and interaction handler evidence inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_instances.csv` | tracked | Generated dungeon/raid/expedition/scripted-instance evidence/status inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_instance_dependencies.csv` | tracked | Generated instance public-event, matching, achievement, and script dependency inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_instance_entities.csv` | tracked | Generated reviewed LaughingWS instance entity, event, script, stat, and reconciliation dependency inventory |
| `Decomp/Analysis/coverage/content_retail_completeness_instance_rewards.csv` | tracked | Generated instance reward-rotation and matching-random reward dependency inventory |
| `Tools/WikiArchiveAudit/quest_implementation_audit.py` | tracked | Generator for quest bucket inventory |
| `Tools/WikiArchiveAudit/content_retail_completeness_audit.py` | tracked | Generator for row-level retail-completeness inventories |
| `Tools/WikiArchiveAudit/validate_content_retail_completeness_outputs.py` | tracked | Validation gate for generated content-retail CSV evidence/status/manual-validation coverage and unsafe `retail_complete` claims |

Do **not** commit `artifacts/wildstar-fandom-wiki-*` (tens of thousands of pages),
`artifacts/verify-obj`, `artifacts/review-bin`, or per-run load-test output.
