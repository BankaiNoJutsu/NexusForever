# Boss ability catalog from Spell4 internal tags (2026-11-09)

Generated from `coverage/spell4_internal_tags_20261109.csv` by `scripts/instance_catalog.py`. Evidence: Mapped-level input from client table descriptions (developer naming), see [INSTANCE_SPELL4_INTERNAL_TAGS_20261109.md](INSTANCE_SPELL4_INTERNAL_TAGS_20261109.md). `eNNN` is joined to the repo `PublicEventObjective` enum by number: a match is a number coincidence checked only for `[STL]` and `[KV]`; unmatched numbers are listed without a name. Each line is a distinct developer ability name with its Spell4 ids (tiers merged) and the tier-1 CastTime in ms when non-zero. This is an inventory, not a cast order or interval.

## [STL]
### e312 (Dungeon/StormtalonsLair: DefeatBladeWindTheInvoker), 30 rows
* Invoker - Static Barrier - Channeler (1): 26739
* Invoker - Static Discharge (1): 26776
* Invoker - Sonic Barrier - Invoker (1): 36674
* Invoker - Lightning Strike - Base (1): 38836
* Invoker - Thunder Cross (1): 38868; castMs [1500]
* Invoker - Auto-Attack (1): 38904; castMs [2000]
* Invoker - Electrostatic Pulse (1): 39165
* Invoker - Jolt - Proxy (3): 40000 40020 40021
* Invoker - Jolt - Base n (3): 40017 40018 40019; castMs [2000]
* Invoker - Lightning Strike - Proxy (Player) (1): 40022
* Invoker - Grounded - Base (1): 40028
* Invoker - Static Field - Base (1): 40682; castMs [3000]
* Invoker - Static Field - Proxy (1): 40683
* Invoker - Sonic Barrier - Fluff Intro (1): 45547
* Invoker - Thunder Cross - Messaging (1): 45658; castMs [2000]
* Invoker - Channel Phase Start - Messaging (2): 45662 45663
* Invoker - Electrostatic Pulse - Messaging (2): 45678 45679
* Invoker - Static Barrier - MOO (1): 63233; castMs [1500]
* Invoker - Static Barrier - Absorb Proxy (1): 63252; castMs [1500]
* Invoker - Thunder Cross - Veteran (1): 63302
* Invoker - Channeler - PBAE (1): 63303; castMs [1500]
* Invoker - Lightning Strike - Proxy (NPC) (1): 63407; castMs [2000]
* Invoker - Lightning Strike - Challenge (1): 63412
* Invoker - Thunder Cross (No Anim) (1): 63603

### e313 (Dungeon/StormtalonsLair: EliminateAethros), 23 rows
* Aethros - Phase Transition (1): 26754; castMs [2000]
* Aethros - Torrent - Special 4 - Base (2): 36104 36105; castMs [2000]
* Aethros - Torrent - Special 4 - Proxy (1): 36120
* Aethros - Twister - Base (1): 39739
* Aethros - Thunderbolt (Gauntlet) (1): 39745
* Aethros - Tempest (2): 39756 63178
* Aethros - MOO Tempest Interrupted (1): 39757
* Aethros - Pull to Location from Invis Unit (5): 45419 83387 83388 83389 83390; castMs [2000]
* Aethros - Gust of Aethros - Static Shock - Base (1): 63197
* Aethros - Gust of Aethros - Static Field (1): 63198
* Aethros - Gust of Aethros - Static Shock - Missile Proxy (1): 63324; castMs [1500]
* Aethros - Gust of Aethros - Static Shock - Damage Proxy (2): 63334 63335
* Aethros - Auto-Attack #2 - L2R (2): 70483 72935
* Aethros - Air Shift - Teleport Random (1): 70632; castMs [1500]
* Aethros - Auto-Attack #1 - R2L (1): 72934

### e314 (Dungeon/StormtalonsLair: DestroyStormtalon), 45 rows
* (N/V) Stormtalon - Auto-Attack #1 (1): 39664
* (N/V) Stormtalon - Lightning Strikes (4): 39682 63325 63331 63333; castMs [1500, 30000]
* (N/V) Stormtalon - Static Charge (1): 39696
* (N/V) Stormtalon - Static Wave - Base (1): 39717
* (V) Stormtalon - Thunder Call - Lightning Proxy (1): 39720
* (N/V) Stormtalon - Thunder Call - Proxy Holder (1): 39721
* (N) Stormtalon - Thunder Call - Lightning Proxy (2): 39728 39729
* (N/V) Stormtalon - Lightning Rod - Base (1): 39852
* (N/V) Stormtalon - Lightning Rod - Proxy (2): 39855 63176; castMs [3500]
* (N/V) Stormtalon - Thunder Call - Base (1): 39863
* (N/V) Stormtalon - Lightning Storm - Base (2): 39900 63179; castMs [3000]
* (N/V) Stormtalon - Lighting Storm - Proxy (4): 39901 63509 63510 63511; castMs [1000]
* (N/V) Stormtalon - Thunder Call 2 - Proxy Holder (1): 39966
* (N/V) Stormtalon - Static Wave - PBAE Explosion Proxy (1): 40079
* (N/V) Stormtalon - Sets Interrupt Armor to 3 for 16 seconds (2): 40082 40094
* (N/V) Stormtalon - Lightning Strike (2): 40728 63508; castMs [4000, 5750]
* (N/V) Stormtalon - Static Wave Interrupt (1): 45684
* (N/V) Stormtalon - Static Wave Start (1): 45686
* (V) Stormtalon - Static Wave Damage (1): 63183
* (V) Stormtalon - Static Wave - Wave Proxy (1): 63184
* (V) Stormtalon - Lighting Strike Target Selector (7): 63213 63214 63326 63327 63328 63329 ...; castMs [1000, 4000]
* (V) Stormtalon - Static Charge Transition (1): 63340
* (V) Stormtalon - Lightning Rod - Proxy Disorient (1): 64878
* (V) Stormtalon - Electric Charge - Auto-Attack Proxy (1): 70568; castMs [3200]
* (V) Stormtalon - Electric Charge - Debuff (1): 70571
* (N/V) Stormtalon - Lighting Rod - Target Selection (1): 75952; castMs [1500]
* (N/V) Stormtalon - Static Wave - Knockdown [DNT] (1): 77414
* (N/V) Stormtalon - Static Wave - Stun [DNT] (1): 77415

## [KV]
### e444 (Dungeon/RuinsOfKelVoreth: DefeatGrondTheCorpsemaker), 20 rows
* Grond - Auto-Attack #2 - Base (1): 37347
* Grond - Thrash - Base (2): 37376 63182
* Grond - Bellow - Base (2): 37665 63175
* Grond - Thrash - Damage Proxy (1): 40167; castMs [3000]
* Grond - Headbutt - Base (2): 40176 40177
* Grond - Buck - Base (1): 40178
* Grond - Expell Flea - Normal - Base (1): 40436
* Grond - Flea - Auto-Attack - Base (1): 40441
* Grond - Bone Trap - CC Proxy (1): 40882; castMs [5000]
* Grond - Thrash - Sand Blast - LAE Damage Proxy (1): 48920; castMs [2500]
* Grond - Bellow - CC Proxy (1): 50311
* Blood Pit - Shame the Cravenhearted- Base (1): 51476
* Grond - Scratch Itch - Base n (2): 58839 63187; castMs [3000]
* Grond - Blood Frenzy - Base (1): 63186
* Grond - Mutilate - Find Random Target (1): 63281
* Grond - Itch Scratching - Normal (1): 80382

### e446 (Dungeon/RuinsOfKelVoreth: DefeatSlavemasterDrokk), 34 rows
* Drokk - Deplete Silo (1): 39249; castMs [8000]
* Drokk - Auto-Attack #1 (1): 39278; castMs [3000]
* Drokk - Auto-Attack #2 (2): 39279 42781; castMs [1250]
* Drokk - Tether CC PBAE - Kel Voreth - Normal - Base (1): 39292
* Drokk - Destructo Bot - Tracking Target (3): 39293 39294 39424
* Drokk - Tether CC PBAE - Kel Voreth - Normal - Proxy (2): 39353 39357
* Drokk - Removes Interrupt Armor - Triggers MoO (2): 39379 39381; castMs [2000]
* Drokk - Interrupt Armor - Sets Interrupt Armor to 2 (2): 39382 69233
* Drokk - Tracking Beacon - Buff Proxy (1): 39383
* Drokk - Destructo Bot - Self Destruct (1): 39390
* Drokk - Bombshell Bot - Detonate (1): 39392
* Drokk - Homing Barrage - Auto Attack Base (1): 39422
* Drokk - Destructo Bot - Enhance Speed (1): 39425
* Drokk - Suppression Wave Messaging (1): 45076
* Drokk - Construct Name Change (1): 50174
* Drokk - Construct Name Revert (1): 50195
* Drokk - Suppression Wave - Proxy (2): 51648 51649
* Drokk - Select Bombshell Targets - Phase 4 (3): 51674 63201 63202
* Drokk - Drokk's Slavebreaking Trophy - Armorer (1): 58004
* Drokk - Drokk's Willbreaking Boots - Outfitter (1): 58347
* Drokk - Suppression Wave - Base (2): 59133 63181
* Drokk - Suppression Field - Base (1): 63188
* Drokk - Lockdown - Base (1): 63199
* Drokk - Tracking Beacon - Effect End Proxy (1): 63312

### e449 (Dungeon/RuinsOfKelVoreth: DefeatForgemasterTrogun), 31 rows
* Forgemaster Trogun - Auto-Attack #2 (1): 38874
* Forgemaster Trogun - Exanite Weapon - Base (1): 38882
* Forgemaster Trogun - Exanite Weapon - NCB (1): 45083; castMs [3500]
* Forgemaster Trogun - Exanite Shards - Base (3): 51965 59861 59862; castMs [750]
* Forgemaster Trogun - Exanite Shards - Damage (1): 51966
* Forgemaster Trogun - Volcanic Strike - Base (2): 52059 52060
* Forgemaster Trogun - Fireball - Base (1): 52075
* Forgemaster Trogun - Inferno - Base (1): 52093
* Forgemaster Trogun - Inferno - Channel (2): 52121 52122
* Forgemaster Trogun - Exanite Weapon - Boss Buff (1): 52412; castMs [2000]
* Forgemaster Trogun - Exanite Weapon - Dash Regen Buff (1): 52413
* Forgemaster Trogun - Exanite Hammer - Base (1): 52515
* Kel Voreth - Forgemaster Trogun - Forgemaster's Call - Base (1): 52639
* Forgemaster Trogun - Eruption - Fluff (1): 52658; castMs [2100]
* Forgemaster Trogun - Rock Show - Damage AE Proxy (1): 52677
* Trogun - Rock Show - Molten Breath (1): 53203
* Forgemaster Trogun - Fireball - AE Proxy (1): 53223
* Forgemaster Trogun - Molten Gusher - Base (2): 54906 54907
* Forgemaster Trogun - Flame Sphere Aura (2): 58834 58851
* Forgemaster Trogun - Flame Sphere Explosion (2): 58837 58838; castMs [4000]
* Forgemaster Trogun - Flame Sphere Aura Buff (4): 58840 58841 60598 73077

### e453 (Dungeon/RuinsOfKelVoreth: DefeatDarkwitchGurka), 1 rows
* Gurka's Non Combat Fluff Visuals (1): 81635; castMs [1200]

### e459 (Dungeon/RuinsOfKelVoreth: DisableTheExoLabDefenses), 1 rows
* Phase Blast Base - Hazard - Kel Voreth (1): 36929

### e461 (Dungeon/RuinsOfKelVoreth: DestroyKelVorethForges), 1 rows
* [KV] e461 Forges - Deep Freeze Grenade (1): 36566

## [SK]
### e321 (Dungeon/Skullcano: DefeatStewShamanTugga), 32 rows
* Tugga - Magma Totem - Proxy (1): 36092
* Tugga - Magma Plume Totem - Base n Channel (2): 36094 63422
* Tugga - Eat Meat - Player (1): 36102
* Tugga - Magma Totem - Base - Summon (2): 36103 63364; castMs [1550]
* Tugga - Magma Plume Totems X 4 - Base n (1): 36111
* Tugga - Meat Cast - Base (1): 36112
* Tugga - Meat Summon - Base (1): 36113
* Tugga - Fire Bolt - Skullcano (1): 36187; castMs [3000]
* Tugga - Magma Plume Totem - All Players- Base n (2): 36203 36204
* Tugga - Blast Force - Base (1): 36256
* Tugga - Meat Wave - Base (2): 41047 63428
* Tugga - Magma Totem Pair - Base (3): 41052 41053 63367
* Tugga - Devour Flesh - Base (1): 41081
* Tugga - Molten Rain - Base (2): 41088 63421; castMs [3600]
* Tugga - Feeding Frenzy Buff - Base (1): 41626
* Tugga - Remove Tugga's IA - Base (1): 43637
* Tugga - Reset Tugga's IA - Base (1): 43638
* Tugga - Granok Stew - Base (1): 50088
* Tugga - Burning - Base (1): 63343
* Tugga - Into the Stew - Base (1): 63378
* Tugga - Into the Stew - CC (1): 63379
* Tugga - Into the Stew - CSI (1): 63383
* Tugga - Magma Totem - Tether (1): 70916
* Tugga - Into the Stew - CC Removal (2): 70922 70923

### e322 (Dungeon/Skullcano: DefeatThunderfoot), 23 rows
* Thunderfoot - Interrupt MOO - 5s - Skullcano (1): 31213
* Thunderfoot - Pulverize - Base N (1): 36385
* Thunderfoot - Auto-Attack #1 L2R (2): 36407 69177
* Thunderfoot - Auto-Attack #2 R2L (2): 36408 69180
* Thunderfoot - Spore Cloud (1): 36415
* Thunderfoot - Enraging Fury (N/V) (2): 36423 70725; castMs [10000]
* Thunderfoot - Summon Spore Cloud (1): 36431
* Thunderfoot - Moodie - Fire Storm (1): 36436; castMs [1]
* Thunderfoot - Thunder Pound - Primary Grow Shroom (2): 36437 36440
* Thunderfoot - Summon Mushroom (1): 36471; castMs [2000]
* Thunderfoot - Summons Moodies (Veteran) (1): 36476
* Thunderfoot - Seismic Tremor (1): 36543
* Thunderfoot - Scale Mushroom (1): 40945; castMs [1000]
* Thunderfoot - Thunder Pound - Secondary No Grow Shroom (1): 40986
* Thunderfoot - Challenge - Failure Signal (1): 63430
* Thunderfoot - Momentum Removal Proc - Base (1): 69826
* Thunderfoot - Momentum Removal - Base (1): 69827; castMs [500]
* Thunderfoot - Momentum - Base (1): 69828
* Thunderfoot - Player Spores - DNT (1): 80338

### e323 (Dungeon/Skullcano: DefeatBosunOctog), 38 rows
* Bosun Octog - Harpoon - Skullcano - Base (2): 31352 31353
* Bosun Octog - Ink Bomb N (1): 36686; castMs [1000]
* Bosun Octog- Auto-Attack #1 (3): 36700 36705 69178
* Bosun Octog- Auto-Attack #2 (1): 36726
* Bosun Octog - Scab Stab - Base (2): 41517 41832
* Bosun Octog - Radio Dispatch - Base N (1): 41528
* Bosun Octog - Expell Squirg (1): 41544; castMs [800]
* Bosun Octog - Ink Shield - Damage Reduction Proxy (2): 41587 41588
* Bosun Octog - Squirg Summon (1): 41882
* Bosun Octog - Squirg Phase - Bosun Octog (1): 41919; castMs [2000]
* Bosun Octog - Razooki Slash - Base n - Bosun Octog (1): 41932; castMs [1333]
* Bosun Octog - Razooki - Clawing Frenzy - Proxy n (1): 41934
* Bosun Octog - Ink Shield - Dance 2 - Base (1): 41946; castMs [2000]
* Bosun Octog - Get Scab Stab Target - Boss - Skullcano (1): 44454
* Bosun Octog - Squirgling Offspring - Squirgsplosion Proxy - Skullcano (2): 44468 44469; castMs [4000]
* Bosun Octog - Growing Squirgsplosion - Skullcano (1): 44474
* Bosun Octog - Squirgling Offspring - Explosion Slow Grow (2): 44487 69271
* Bosun Octog - Scab Stabs Proxy - Boss - Skullcano (1): 44497
* Bosun Octog - Chain Scab Stabs - Boss - Skullcano (1): 44499; castMs [400]
* Bosun Octog - Wipe Threat - Base (1): 50184
* Bosun Octog - Ink - Two Targets (1): 52872
* Razooki - Poop-copter (1): 53015
* Bosun Octog - Monkey Mine - Explosion (2): 63344 63345; castMs [1500, 1800]
* Bosun Octog - Monkey Mine Debuff (1): 63346
* Bosun Octog - Harpoon Stun (1): 63371; castMs [1500]
* Bosun Octog - Razooki Throw Mine - Base (1): 63415; castMs [2000]
* Bosun Octog - Razooki Throw Mine - Proxy (1): 74651
* Bosun Octog - Pull Proxy (1): 74671
* Razooki - Poop-copter (Proxy Base) (1): 74986
* Bosun Octog - Long MOO (1): 83376

### e324 (Dungeon/Skullcano: DefeatMordechaiRedmoon), 36 rows
* Mordechai - Auto Attack #1 Proxy (1): 38531; castMs [1500]
* Mordechai - Auto Attack #2 Proxy (1): 38532; castMs [1000]
* Mordechai - Vicious Barrage - Proxy (1): 38533; castMs [2000]
* Mordechai - Vicious Barrage - Base (1): 38539
* Mordechai - Auto Attack #2 Base (1): 38614
* Mordechai - Molten Gold - Missile Proxy - Skullcano (1): 38632
* Mordechai - Molten Gold - Field Proxy (1): 38637
* Mordechai - Self Destruct (1): 38646
* Mordechai - Terraformer - Molten Torrent - Base (3): 39469 63403 63404; castMs [1500]
* Mordechai - Terraformer - Molten Torrent - Proxy (2): 39473 39474; castMs [2000]
* Mordechai - Disperse Grenades - Base - Skullcano (3): 39566 39567 63442; castMs [1500]
* Mordechai - Turret Grenade- Disperse Proxy - Skullcano (2): 39568 63443; castMs [1500]
* Mordechai - Big Bang - Pin Proxy (1): 39579
* Mordechai - Turret Grenade- Auto Attack - Base - Skullcano (2): 39580 39581; castMs [483]
* Mordechai - Turret Grenade- Damage/Summon Proxy (1): 39582; castMs [3000]
* Mordechai - Evasive Maneuver - Jump to Player - Base (1): 39619; castMs [100]
* Mordechai - Camera Shake (1): 39655
* Mordechai - Molten Gold - Stun Proxy (1): 42313
* Mordechai - Evasive Maneuver - Check for players (1): 52910
* Mordechai - Evasive Maneuver - Signal Self (2): 52911 52912
* Mordechai - Evasive Maneuver - Targeted AOE (1): 52960
* Mordechai - Evasive Maneuver - Proxy Jump to Location - No Check (1): 53020; castMs [3000]
* Mordechai - Evasive Maneuver - Jump Down to Location (1): 53021
* Mordechai - Plasma Ball - Base (1): 63432
* Mordechai - Plasma Ball - Aura (1): 63433
* Mordechai - Terraformer - Molten Torrent - Slag Proxy (2): 63439 63440; castMs [1000]
* Mordechai - Seared Eyes - Base (1): 63462

### e328 (Dungeon/Skullcano: EscortChiefKaskalak), 16 rows
* Lava Tube - Molten Heat - Skullcano (5): 31298 63755 63756 63757 63758
* Lava Tube - Heat Dissipation Field - Skullcano (1): 31299
* Lava Tube - Heat Vent - Base (2): 41367 63766
* Lava Tube - Flame Trail - Base (1): 41392
* Lava Tube - Flame Trail - Proxy Field (1): 41393
* Lava Tube - Collapse - Base (2): 41418 63767
* Lava Tube - Explosive Shot - Base (1): 63770
* Lava Tube - Explosive Shot - Signal Proxy (1): 63778
* Lava Tube - Explosive Shot - Damage Proxy (1): 64881; castMs [3000]
* Lava Tube - Heat Nullification (1): 69824

### e329 (Dungeon/Skullcano: CrossTheLavaFilledChasm), 19 rows
* Molten Chasm - Dark Fireball - Laveka (1): 31148
* Molten Chasm - Dark Fireball - Damage Proxy (1): 39503
* Molten Chasm - Flame Wall (1): 41359
* Molten Chasm - Magma Totem Pair (1): 41423
* Molten Chasm - Magma Totem - Base n (1): 52012
* Molten Chasm - Magma Totem - Proxy (1): 52014
* Molten Chasm - Molten Tide - Channel Proxy (1): 52175
* Molten Chasm - Molten Tide - Base (2): 52178 52201
* Molten Chasm - Molten Wave (3): 52442 52740 52742
* Molten Chasm - Melted Armor (1): 53005
* Molten Chasm - Scorching Embers (1): 53518
* Molten Chasm - Blazing Slag - Targeted (NPC) (1): 53524
* Molten Chasm - Blazing Slag - AE Proxy (1): 53525
* Molten Chasm - Blazing Slag - Base (1): 53529
* Molten Chasm - Blazing Slag - Targeted (Player) (1): 53569
* Molten Chasm - Fireball KD Immunity (1): 75927

## [CBC]
### e5313 (Dungeon/ColdbloodCitadel: DefeatHailStoneGatecrasher), 25 rows
* Ice Elemental - Frigid Temperatures (aura) (1): 87876
* Pell Aura (1): 87899
* Ice Elemental - Ice Rage - LAE (1): 87983; castMs [1000]
* Ice Elemental - Freeze pulse (Proxy) (1): 88040
* Ice Elemental - Howling Winds - Vector Slide (Aura) (2): 88055 88056; castMs [233]
* Ice Elemental - Howling Winds - Base (1): 88110
* Ice Elemental - Howling Winds - Shard Proxy 1 (2): 88113 88114; castMs [450]
* Ice Elemental - Howling Winds - Shard Proxy 2a (1): 88122; castMs [1500]
* Ice Elemental - Howling Winds - Shard Proxy 2b (1): 88123; castMs [1500]
* Ice Elemental - Howling Winds - Shard Proxy 2c (2): 88124 88125; castMs [1500]
* Ice Elemental - Howling Winds - Ice Spikes (Aura) (1): 88129
* Ice Elemental - MoO (1): 88130
* Ice Elemental - Howling Winds - Fail Damage (2): 88131 88132
* Ice Elemental - Howling Winds - Melee Knockback (1): 88133
* Ice Elemental - Sling Snow Boulders - Prog (1): 88136; castMs [1250]
* Ice Elemental - Pound of Ice - SRE1 (1): 88137; castMs [1250]
* Ice Elemental - Auto-Attack #1 (1): 88138; castMs [1250]
* Ice Elemental - Auto-Attack #2 (2): 88139 88140; castMs [1250]
* Ice Elemental - Sling Snow Boulders - Target select (1): 88273
* Ice Elemental - Freeze Blast - Enrage Spell (1): 88429; castMs [1500]

### e5314 (Dungeon/ColdbloodCitadel: DefeatTheIcebloodCoven), 21 rows
* Coven Challenge - Shared Health Pool (1): 87990; castMs [1000]
* Coven - Shield and Invulnerable (1): 87991
* Coven Fight - Blood Tornado - Aura (2): 88107 88108
* Coven Fight - Blood Seed (1): 88257; castMs [2000]
* Coven Fight - Blood transfer - Heal (1): 88259; castMs [3000]
* Coven Fight - Mark of Corruption - Healing Absorb (1): 88260
* Coven Fight - Blood Seed Burst - Player AoE (1): 88303; castMs [1500]
* Coven Fight - Blood Caster Auto-Attack (1): 88304
* Coven Fight - Soulfrost Trail - Base (1): 88328
* Coven Fight - Soulfrost Trail - Proxy Field (1): 88329
* Coven Fight - Frost Witch Auto Attack (1): 88330
* Coven Fight - Frost Witch Shard Storm GTAE (1): 88333
* Coven Fight - Frost Witch Shard Storm GTAE (proxy) (1): 88334
* Coven Fight - Seeking Shadow - Base (1): 88336
* Coven Fight - Seeking Shadow - Proxy (1): 88337
* Coven Fight - Soulfrost Armor (1): 88338
* Coven Fight - Blood Cosmetic Beam (1): 88340
* Coven Fight - Shadow Cosmetic Beam (1): 88341
* Coven Fight - Frost Cosmetic Beam (1): 88342
* Coven Fight - Shadow Witch Auto Attack (1): 88364; castMs [1500]

### e5315 (Dungeon/ColdbloodCitadel: DefeatTheRisenHarizog), 2 rows
* Harizog - Stasis Spell (1): 87761
* Harizog - Jump Down (1): 87762

### e5317 (Dungeon/ColdbloodCitadel: GatherSoulfrostShards), 1 rows
* Collect Soulfrost (1): 88430

### e5318 (Dungeon/ColdbloodCitadel: DestroySoulrotCanisters), 2 rows
* Destroy Soulrot - Explosion (1): 88363; castMs [1500]
* Destroy Trap (1): 88441; castMs [1500]

## [GA]
### e410 (Raid/GeneticArchives: DefeatExperimentX89), 43 rows
* Strain Mauler - Auto-Attack #1 (1): 47341
* Strain Mauler - Auto-Attack #2 (1): 47342
* Strain Mauler - Shattering Shockwave - Proxy (2): 47346 47837
* Strain Mauler - Strain Bomb - Base (1): 47353; castMs [2000]
* Strain Mauler - Corruption Globule - Proxy (3): 47379 58501 58502
* Strain Mauler - Corruption Globule - Damage Proxy (1): 47384; castMs [1500]
* Strain Mauler - Corruption Globule - Signal Proxy (1): 47395
* Strain Mauler - Repugnant Spew - Debuff Proxy (1): 47398
* Strain Mauler - Repugnant Spew - Channeled Proxy (1): 47416
* Strain Mauler - Platform AE (2): 47538 47818
* Strain Mauler - Repugnant Spew - Spin Proxy CW (2): 47795 47796
* Strain Mauler - Repugnant Spew - Spin Proxy CCW (1): 47797
* Strain Mauler - Strain Bomb - Remove Proxy (1): 58582
* Strain Mauler - Leap (Choose Platform) (2): 58778 58779
* Strain Mauler - Platform - Destroyed Signal (1): 58791
* Strain Mauler - Leap (Re-choose) (2): 58802 58803
* Strain Mauler - Corruption Globule - Target Selection - Base (1): 58878
* Strain Mauler - Strain Bomb - Target Selection - Base (2): 58880 58881
* Strain Mauler - Shattering Shockwave - Bridge Destruction (2): 58955 58956
* Strain Mauler - Bridge Destruction - Cam Shake (1): 59135
* Strain Mauler - Bridge Destruction - Aggro (1): 60910
* Strain Mauler - Corruption Globule - Highlighter - Channel (2): 60931 60932; castMs [3000]
* Strain Mauler - Corruption Globule - Highlighter - Proxies (2): 60946 60947
* Strain Mauler - Corruption Globule - Highlighter - 1000ms (3): 60948 60949 74595
* Strain Mauler - Shattering Shockwave - Highlighter - Proxies (2): 60987 60988
* Strain Mauler - Leap (Choose Platform 2) (1): 70128
* Strain Mauler - Mighty Leap (no cast time) (1): 70130
* Strain Mauler - Leap (Choose Platform 3) (1): 71166
* Strain Mauler - Shattering Shockwave - Highlighter - Channel (1): 74594; castMs [2750]

### e411 (Raid/GeneticArchives: DefeatPhageMaw), 75 rows
* Metal Maw - Bombardment (1): 56591
* Metal Maw - Bombardment - LH Target (1): 56593
* Metal Maw - Bombardment - LH Missile (1): 56594
* Metal Maw - Bombardment - Target RH (2): 56595 56596
* Metal Maw - Bombardment - Crash (1): 56597
* Metal Maw - Plasma Flower- Proxy (1): 56602
* Metal Maw - Harpoon Missile (2): 56862 58877
* Metal Maw - Plasma Tank - Throw Ball (1): 56874
* Metal Maw - Plasma Tank - Move Ball (2): 56875 56876
* Metal Maw - Utility - Despawn Primary Target (1): 56879
* Metal Maw - Plasma Tank - Give Action Bar (4): 56881 56882 57209 57210
* Metal Maw - Plasma Tank - Remove Action Bar (2): 56885 56886
* Metal Maw - Plasma Tank - Despawn Soccer Ball (2): 56920 56921
* Metal Maw - Soccer - Throw Ball (1): 56926; castMs [1500]
* Metal Maw - Player - Super Shield - Cast (1): 57018
* Metal Maw - Player - Super Shield - Shield (1): 57022
* Metal Maw - Player - Super Shield - Give Super Shield (2): 57068 57069
* Metal Maw - Player - Super Shield - Remove Action Bar (2): 57071 57072
* Metal Maw - Moment of Opportunity (2): 57196 57197
* Metal Maw - Plasma Tank - Harpoon Get Resource (1): 57232; castMs [1000]
* Metal Maw - Aerial Bombardment (1): 57306
* Metal Maw - Aerial Bombardment - Rocket Proxy (1): 57317
* Metal Maw - Harpoon Terminal Activate (1): 57343
* Metal Maw - Break Chains (1): 57423
* Metal Maw - Plasma Tank - Harpoon Cannon Resource Knock (4): 57496 57497 58980 58981
* Metal Maw - Prism Ray - Splitting Prism - Proxy (2): 57681 57682
* Metal Maw - Bombardment - RH Missile (1): 57707
* Metal Maw - Player - Super Shield - Remove Action Bar on Everyone (2): 57724 57725
* Metal Maw - Bombardment - Missile Explosion (1): 57756
* Metal Maw - Clean Up Fluffs and Spells (1): 57769
* Metal Maw - Plasma Tank - Action Bar Spell - Base (1): 57845
* Metal Maw - Clean Up Action Bars (1): 57883
* Metal Maw - Aerial Bombardment - Cast Event Signal (1): 57920
* Metal Maw - Plasma Tank VFX (1): 57935
* Metal Maw - Plasma Tank - Aura (1): 58579
* Metal Maw - Obliterate - Fluff (1): 58737
* Metal Maw - Obliterate - Cast (1): 58774; castMs [1500]
* Metal Maw - Aerial Bombardment - Targeted Rocket Proxy (2): 58884 58885
* Metal Maw - Aerial Bombardment - Infinite Flying Fluff (1): 60514
* Metal Maw - Laser Blast - Base (1): 60537
* Metal Maw - Bombardment - Raid Wipe Smash (1): 60542
* Metal Maw - Detonation Bombs (1): 60702
* Metal Maw - Detonation Bomb - Knock Random (1): 60703; castMs [3000]
* Metal Maw - Detonation Bombs - Detonate (1): 60704; castMs [3000]
* Metal Maw - Laser Blast - Field (1): 60929
* Metal Maw - Harpoon - Charge Player (2): 60998 60999
* Metal Maw - Harpoon Charge Visual (3): 61006 61007 61008
* Metal Maw - Detonation Bomb - Proxy Damage (2): 61013 61014
* Metal Maw - Aerial Bombardment - Check for players (2): 65739 65740
* Metal Maw - Aerial Bombardment - Interrupt (1): 65741
* Metal Maw - Recovery after MoO (1): 69655
* Metal Maw - Laser Blast - Missile Proxy (2): 71150 71151

### e412 (Raid/GeneticArchives: DefeatKuralakTheDefiler), 58 rows
* Vileness Volley AOE - Genetic Architect (2): 56552 56553
* Pillar - Overload - Field Proxy (1): 56651; castMs [4000]
* Genetic Architect - DNA Siphon (Tank) Base (1): 56660; castMs [2000]
* Genetic Architect - DNA Siphon (Tank) - Threat Proxy (1): 56669; castMs [4000]
* Genetic Architect - Chromosome Corruption - Base (2): 56737 56738
* Genetic Architect - Chromosome Corruption - Target Proxy (2): 56739 56740
* Genetic Architect - Chromosome Corruption - Buff Proxy (3): 56742 56743 56744
* Vileness GTAE - Genetic Architect (1): 57817
* Genetic Architect - Anesthetized (1): 57877
* Genetic Architect - Putrid Discharge (1): 57923
* Genetic Architect - Cocoon Interrupted (2): 57938 57939
* Pillar - Overload (Intro) - Base (1): 58497
* Pillar - Overload (P1 Outer) - Base (1): 58498
* Pillar - Overload (P2 Inner With Field) - Base (1): 58499
* Genetic Architect - Incubating (1): 58657
* Genetic Architect - Archive Overload (Intro) (2): 60213 60214
* Genetic Architect - Hide (2): 60215 60549
* Genetic Architect - Contaminate - Cast (1): 60259
* Genetic Architect - Tainted Distortion (2): 60260 60261
* Genetic Architect - MOO - Shield Break (1): 60262
* Genetic Architect - Tainted Ventilation (2): 60454 60455
* Genetic Architect - Spin (2): 60491 60492
* Genetic Architect - Cultivate - Base (2): 60546 60547
* Genetic Architect - Cultivate - Target Proxy (1): 60657
* Genetic Architect - Outbreak (1): 60715; castMs [30000]
* Genetic Architect - Despawn (2): 60830 60831
* Center Phase Donut - Base (1): 60938
* Genetic Architect - Overload (Center) - Base (3): 61050 64978 64979
* Genetic Architect - Overload KB (1): 61135
* Genetic Architect - Siphon Interrupt Proc Buff (2): 61148 61149
* Genetic Architect - Contaminate (1): 64935
* Genetic Architect - Contaminate - Pillar Cast (1): 64953
* Genetic Architect - Hide Signal Cast Event (1): 64961
* Genetic Architect - Pillar Stand State Cast Event (3): 64974 64975 64976
* Center Phase Donut - Aura Proxy (1): 64988
* Kuralak the Defiler - Dark Sky (1): 69663
* Kuralak the Defiler - Bright Sky (1): 69667
* Genetic Architect - DNA Siphon Kill Signal Cast Event (2): 70293 70294
* Genetic Architect - Challenge Fail (2498) (1): 72031

### e413 (Raid/GeneticArchives: DefeatThePhagetechPrototypes), 71 rows
* Ghobots - Create Radiation Pool - Base - NEW (1): 58820
* Ghobots - Radiation Pool - Base - NEW (2): 58821 58822; castMs [2750]
* Radiation Pool - AE Proxy - NEW (1): 58823
* 4Bots - Command Bot - Auto-Attack #1 - Base (1): 59720
* 4Bots - Command Bot - Auto-Attack #2 - Base (2): 59721 59722
* 4Bots - Command Bot - Pound Tank (1): 59747
* Protector Bot - Wave Builder (2): 59792 59793
* Protector Bot - Wave Aura (1): 59794
* Augmentor Bot - Repair Bot - Repair (1): 59795
* Fabricator Bot - Destructo Bot - Self Destruct (2): 59796 59797
* Protector Bot - Pulse Waves (1): 59805; castMs [1500]
* Protector Bot - Pulse Wave Knockback (2): 59811 59812
* Fabricator Bot - Summon Destructo Bot (1): 59856
* Augmentor Bot - Summon Repair Bot (1): 59857
* Augmentor Bot - Probe Force Move (1): 59867; castMs [500]
* Phagetech Prototypes - Powering Down (2): 59903 59904
* Phagetech Prototypes - Powering p (2): 59905 59906
* Command Bot - Forced Production (1): 60909
* Fabricator Bot - Destructo Bots - Remove Facility (2): 61040 61041
* Augmentor #1 - Drill Telegraph (1): 64922
* Augmentor #1 - Drill Telegraph - DMG Proxy (1): 64923
* Protector Bot - Position Swap (1): 64925; castMs [2500]
* Augmentor #2 - Sparks (1): 64926
* Augmentor #1 - Drill Telegraph - Cast Proxy (1): 64927; castMs [2000]
* Ghobots - Protector - Black Hole (2): 66543 66546
* Ghobots - Protector - Black Hole - Pull (1): 66545
* Ghobots - Protector - Revolver Missile Fluff (2): 66547 66548
* Ghobots - Fabricator Basketball Missile Proxy (2): 66558 66559
* Ghobots - Fabricator Basketball Damage Proxy (1): 66560
* Ghobots - Basketball Signal Cast Events (2): 66562 66631
* Ghobots - Fabricator Basketball Missed Damage Proxy (1): 66563
* Ghobots - Fabricator Basketball Target Selector (3): 66566 66569 66570
* Ghobots - Augmentor Phasing Target Selector (2): 66680 66681
* Ghobots - Return from Phase Buff (1): 66699
* Ghobots - Signal World Cast Event Phase Damage (1): 66703
* Phase Realm - Field Dropping Buff YLW (4): 66704 66706 66707 66708
* Phase Realm - Damage Field (1): 66705
* Ghobots - Command Break the Chain (2): 66731 66733; castMs [5000, 10000]
* Ghobots - Command Break the Chain - Dmg Proxy (1): 66923; castMs [3000]
* Ghobots - Command - Break the Chain (1): 67477; castMs [4000]
* Ghobots- Command Break the Chain - Spell End Cast Event (1): 67913
* Ghobots- Command Break the Chain - Fluff for Caster Buff (1): 68005; castMs [3000]
* Phagetech Augmentors - Auto-Attack #1 (1): 70214
* Phagetech Augmentors - Auto-Attack #2 (1): 70215
* Phagetech Protector - Auto-Attack #1 (1): 70217; castMs [4100]
* Phagetech Protector - Auto-Attack #2 (1): 70218
* Phase Realm - Field Dropping Buff BLU (2): 70246 70247
* Phase Realm - Field Dropping Buff PPL (1): 70248
* Phase Realm - Field Dropping Buff GRN (1): 70250
* Protector Bot - Position Swap - Base (2): 71122 71124; castMs [5000]

### e415 (Raid/GeneticArchives: DefeatDreadphageOhmna), 52 rows
* Dreadphage Ohmna - Erupt - Base (4): 47425 59864 72740 72787
* [GA] e415 Dreadphage Ohmna - Genetic Torrent - Base (2): 47427 72742; castMs [400, 3000]
* Dreadphage Ohmna - Genetic Torrent - Damage AE (3): 47504 47505 72794; castMs [800, 2000]
* Ohmna - Gene Splice - Heal (1): 47553
* Ohmna - Gene Splice - Base (1): 47558
* Dreadphage Ohmna - Auto-attack (1): 47562
* [GA] e415 Dreadphage Ohmna - Genetic Goop - Base (5): 47746 59688 59689 59690 59691; castMs [2500, 3000]
* Dreadphage Ohmna - Sap Power - Base (1): 47810
* Ohmna - Moment of Opportunity - MOO - Base (2): 47950 47951; castMs [1100]
* Dreadphage Ohmna - Gene Splice - AE Proxy (1): 48012
* Ohmna - Invulnerability - Base (1): 48033
* Dreadphage Ohmna - Select Platform for Tentacle (2): 48102 48103
* Genetic Archives - Ohmna - Genetic Torrent Knockback - Base (2): 48224 48234; castMs [1500]
* Genetic Archives - Ohmna - Strain Spit - Power Core Tentacle - Enhanced (1): 48433; castMs [2700]
* Genetic Archives - Ohmna - Find Auto-Attack Target - Power Core Tentacle - Enhanced (2): 48435 48436; castMs [1000]
* Genetic Archives - Ohmna - Genetic Torrent - Rotate CCW (2): 50134 50136
* Genetic Archives - Ohmna - Genetic Torrent - Rotate CW (1): 50137
* Dreadphage Ohmna - Platform Tentacle - Genetic Spew - Base (1): 58886
* Dreadphage Ohmna - Genetic Spew - Interrupted Proxy (1): 58891
* Dreadphage Ohmna - Ravage - Base (1): 59724; castMs [850]
* Dreadphage Ohmna - Ravage Knockback (1): 59748
* [GA] e415 Dreadphage Ohmna - Genetic Goop Debuff - Base (2): 60075 60076
* Dreadphage Ohmna - Devour and Consume - Base (2): 61009 61010
* Dreadphage Ohmna - Corner Prey - Base (2): 61016 61017
* Dreadphage Ohmna - Corner Prey - Tether Beam VFX (1): 61018
* Dreadphage Ohmna - Strain Spit - Power Core Tentacle Damage Proxy (2): 61111 61112
* Dreadphage Ohmna - Phase 3 Scale Buff (2): 72716 72717
* Dreadphage Ohmna - Body Slam - Base (1): 72741; castMs [400]
* Dreadphage Ohmna - Spawn Tentacle at Player - Phase 3 (3): 72792 75799 75800
* Dreadphage Ohmna - Body Slam - Giant Phase (1): 75796; castMs [4000]

## [DS]
### e385 (Raid/Datascape: DefeatTheSystemDaemons), 108 rows
* Eldan Sentinels - Molecular Instability - Teleporter DoT - Base (1): 42952
* Eldan Sentinels - Radiation - Defuff Proxy (1): 42955; castMs [2000]
* Eldan Sentinels - Holdout - Power Transfer - Base (9): 42985 43120 43317 43318 44040 64955 ...; castMs [2000]
* Eldan Sentinels - Holdout - Defrag T1 - Base (2): 43033 43034; castMs [2000]
* Eldan Sentinels - Holdout - Defrag T2 - Base (1): 43036
* Eldan Sentinels - Holdout - Defrag T3 - Base (1): 43039
* Eldan Sentinels - Holdout - Defrag T4 - Base (1): 43040; castMs [2000]
* Eldan Sentinels - Holdout - Defrag T5 - Base (1): 43041
* Eldan Sentinels - System Restore - Base (2): 43081 45436; castMs [3000]
* Eldan Sentinels - Overload (Power Surge Interrupted) - Base (2): 43087 43088; castMs [600, 2000]
* Eldan Sentinels - Holdout - Runner Speed - Base (4): 43121 43122 43123 43124
* Eldan Sentinels - Holdout - Repair Sequence - Base (1): 43133; castMs [2000]
* Eldan Sentinels - Holdout - Intercept - Base (1): 43134
* Eldan Sentinels - Holdout - Data Wipe - Base (2): 43139 43140; castMs [4000]
* Eldan Sentinels - Holdout - Tractor Beam - Base (2): 43143 43145
* Eldan Sentinels - Holdout - Scramble - Base (1): 43160
* Eldan Sentinels - Holdout - Fetch - Base (1): 43163
* Eldan Sentinels - Holdout - Hack and Slash - Base (1): 43164; castMs [2000]
* Eldan Sentinels - Holdout - Fluctuation - Base (1): 43165
* Eldan Sentinels - Holdout - Data Spike - GTAE (2): 43167 43170
* Eldan Sentinels - Holdout - Data Mine - Base (2): 43171 43172; castMs [2000]
* Eldan Sentinels - Holdout - Data Mine - Extract - Aura Proxy (4): 43173 43175 43176 43181; castMs [1000, 3000]
* Eldan Sentinels - Holdout - Overclock - Base (1): 43335
* Eldan Sentinels - Synchronized - Base (1): 43435
* Eldan Sentinels - Power Enhancement - Base (1): 43441
* Eldan Sentinels - Memory Wipe - Base (1): 43454
* Eldan Sentinels - Disconnect (Sentinel) - Base (1): 43460
* Eldan Sentinels - Synchronized- AE Proxy (1): 43511
* Eldan Sentinels - Download - Proxy (1): 43523
* Eldan Sentinels - Upload - Proxy (1): 43536; castMs [2000]
* Eldan Sentinels - Download - Base (1): 43544
* Eldan Sentinels - Upload - Base (1): 43545
* Eldan Sentinels - Invalidate - T1 - Base (1): 43603
* Eldan Sentinels - Invalidate - T2 - Base (1): 43604
* Eldan Sentinels - Invalidate - T3 - Base (2): 43605 43606
* Eldan Sentinels - Invalidate - T4 - Base (1): 43607
* Eldan Sentinels - Invalidate - T5 - Base (1): 43608
* Eldan Sentinels - Invalidate - GTAE Proxy (6): 43609 43610 43611 43612 43613 43614; castMs [1000]
* Eldan Sentinels - Meltdown - Base (2): 43663 43667
* Eldan Sentinels - Power Enhancement - Buff Proxy (1): 43964; castMs [1500]
* Eldan Sentinels - Armor Enhancement - Buff Proxy (1): 43966
* Eldan Sentinels - Laser- Base (1): 43970
* Eldan Sentinels - Holdout - Self-Destruct - Base (2): 44033 44034; castMs [1500]
* Eldan Sentinels - Holdout - Black IC - Interrupted Proxy DNT (1): 44052; castMs [2000]
* Eldan Sentinels - Holdout - Black IC - Base (1): 44059
* Eldan Sentinels - Holdout - Black IC - Damage Proxy (2): 44065 44066
* Eldan Sentinels - Holdout - Data Sniffer - TPAoE Proxy (2): 44080 44081
* Eldan Sentinels - Purge - Base (1): 44099
* Eldan Sentinels - Purge - AE Proxy (2): 44108 44113; castMs [2500]
* Eldan Sentinels - Holdout - Data Sniffer - Base (1): 44128; castMs [900]
* Eldan Sentinels - Enable Generator - Base n (1): 44297
* Eldan Sentinels - Digitize - Base (1): 64902; castMs [2500]
* Eldan Sentinels - Data Mine - Extract - Base (1): 64929; castMs [5300]
* Eldan Sentinels - Data Dart - Base (1): 66710; castMs [1500]
* Eldan Sentinels - Zap - Base (2): 67391 67400
* Eldan Sentinels - Fully Charged Firewall - Base (1): 70507; castMs [3000]
* Sentinels - Auto-Attack #1 (1): 72432
* Eldan Sentinels - Holdout Healer - Auto-Attack #1 (2): 72531 72532
* Eldan Sentinels - Holdout Healer - Auto-Attack #2 (1): 72533
* Eldan Sentinels - Holdout Disruptor - Auto-Attack #1 (1): 72534; castMs [1500]
* Eldan Sentinels - Holdout Disruptor - Auto-Attack #2 (1): 72535
* Eldan Sentinels - Holdout Miniboss - Radiator - Auto-Attack #1 (2): 72536 72537
* Eldan Sentinels - Holdout Miniboss - Radiator - Auto-Attack #2 (1): 72538; castMs [4000]
* Eldan Sentinels - Holdout Controller - Auto-Attack #1 (1): 72539
* Eldan Sentinels - Holdout Controller - Auto-Attack #2 (1): 72540
* Eldan Sentinels - Holdout Brute - Auto Attack #1 (1): 72543
* Eldan Sentinels - Holdout Brute - Auto-Attack #2 (1): 72544; castMs [5000]
* Eldan Sentinels - Holdout Zone Denial - Auto-Attack #1 (1): 72545; castMs [1000]
* Eldan Sentinels - Holdout Zone Denial - Auto-Attack #2 (1): 72546
* Eldan Sentinels - Holdout - Data Spike - Base (1): 74638; castMs [20000]
* Eldan Sentinels - Intercept - Find Random Target (1): 74642
* Eldan Sentinels - Charged (1): 74983
* Eldan Sentinels - Purge - Debuff (1): 79473; castMs [1500]

### e390 (Raid/Datascape: DefeatTheMaelstromAuthority), 87 rows
* Air Boss - No Falling Damage (1): 44229
* Air Boss - Lightning Rod - Base (1): 44437
* Air Boss - Lightning Rod - AE Damage Proxy (1): 44438
* Air Boss - Conduction - Base (1): 44445; castMs [3000]
* Air Boss - Lightning Rod - GTAE Proxy (1): 44456
* Air Boss - Static Bombshell - Stun Proxy (1): 44481
* Air Boss - Static Bombshell - Field Proxy (1): 44482; castMs [5100]
* Air Boss - Typhoon - AE Proxy (6): 44483 44629 44630 69230 72999 73000; castMs [3000, 3666]
* Air Boss - Typhoon -  Slow Fall Proxy (1): 44484
* Air Boss - Galeforce - Base (1): 44492
* Air Boss - Wind Wall - Aura Proxy (1): 44493
* Air Boss - Ice Breath - Channel AoE Proxy (2): 44593 44594
* Air Boss - Crystallize - Stun Break Proxy (1): 44600
* Air Boss - Ice Breath - Tether AoE (2): 44603 44604
* Air Boss - Freeze - Freeze Friction Proxy (1): 44605; castMs [1500]
* Air Boss - Ice Breath - Base (1): 44607; castMs [1500]
* Air Boss - Conduction Proxy Fluff (1): 44610
* Air Boss - Ice Breath - Blind Proxy (1): 44651
* Air Boss - Galeforce - Find Target - Base (1): 44683
* Air Boss - Freeze - Base (1): 44924
* Air Boss - Center Station Activate (1): 44945
* Air Boss - Shifting Currents - Intro - Base (Cast) (1): 44981
* Air Boss - Enhance Weather Condition (1): 45116
* Air Boss - Freeze - GTAE Proxy (1): 45133
* Air Boss - Invulnerable Weather Station (1): 45135
* Air Boss - Activate Weather Cycle - Base (1): 45164
* Air Boss - Hook - Use Crate (1): 45177; castMs [2000]
* Air Boss - Force Remove Break Deep Freeze - Base (1): 45212; castMs [1300]
* Air Boss - Typhoon - Knockback CC (5): 45226 45228 45229 69229 72998; castMs [2000, 4500, 5000]
* Air Boss - Full Power - Base (1): 45266
* Air Boss - Crystallize - Stun Proxy (1): 45330
* Air Boss - Freeze - Tracking Proxy (1): 45400
* Air Boss - Wind Wall - Summon Proxy (1): 45405
* Air Boss - Wind Wall - Rotate Caster Proxy (1): 45465
* Air Boss - Shatter (1): 45567
* Air Boss - Shifting Currents - Base (1): 45569
* Air Boss - Shifting Currents - Leap Proxy (2): 45570 45571
* Air Boss - Hook - Sky Hook - Base (1): 45587
* Air Boss - Hook - Grapply Hook - Base (1): 45589
* Air Boss - Hook - Player Hook - Pull Proxy (2): 45590 45591; castMs [1000]
* Air Boss - Hook - Sky Hook - Reset CD Proxy (1): 45600; castMs [1500]
* Air Boss - Hook - Player Hook - Reset CD Proxy (1): 45604
* Air Boss - Cancel Spells/Buffs (2): 45867 45868; castMs [500]
* Air Boss - Hook - Sky Hook - Miss VFX (1): 45917; castMs [1000]
* Air Boss - Crystallize - Base (1): 45920
* Air Boss - Auto Attacks - Base (2): 65247 74393
* Air Boss - Auto Attack - Damage Proxy (2): 65248 65249
* Air Boss - Accelerate Weather Cycle - Channel (2): 65511 79506
* Air Boss - Conduction - Target Proxy (1): 66473
* Air Boss - Conduction - Aura Proxy (1): 66474
* Air Boss - Shifting Currents - Intro - AE Proxy (1): 69243; castMs [750]
* Air Boss - Set Interrupt Armor (4): 69365 69366 69367 69368
* Air Boss - Condensed Tempest (1): 72941; castMs [1000]
* Air Boss - Moment of Opportunity - Base (2): 72942 73064
* Air Boss - Hook - Grant Tether (1): 72970
* Air Boss - Shifting Currents - Knockback (2): 72977 72978; castMs [2500]
* Air Boss - Auto Attack - AE Proxy (1): 74346
* Air Boss - Freeze - Damage Proxy (1): 78122
* Air Boss - Typhoon - Prereq Proxy (1): 78141; castMs [20000]
* Air Boss - Accelerate Weather Cycle - Cast (1): 78389; castMs [3000]
* Air Boss - Wind Wall - Base (1): 78392; castMs [3000]
* Air Boss - Auto Attacks - Fluff Proxy - DNT (1): 78395
* Air Boss - Accelerate Weather Cycle - Cast - No Interrupt (2): 78399 78400; castMs [4000]
* Air Boss - Static Surge - Base (1): 78401

### e392 (Raid/Datascape: EscapeTheLimboInfomatrix), 80 rows
* Void Desert - Primal Water - Base (1): 46392
* Void Desert - Knock Water Droplet - Base (1): 46398; castMs [1000]
* Void Desert - Dehydrate - Base (1): 46423
* Void Desert - Exhaust - Base (1): 46426
* Void Desert - Desiccate - Base (2): 46427 46428
* Void Desert - Desert Call - Base (2): 46430 46431
* Void Desert - Mini Sandworm - Chomp - Base (1): 46432
* Void Desert - Super Soaker - Base (1): 46433
* Void Desert - Water Bomb - Base (1): 46434
* Void Desert - Guzzle - Base (1): 46435
* Void Desert - Gastric Juices - Base (1): 46436
* Void Desert - Water Action Bar - Base (1): 46443
* Void Desert - Consume - Base (1): 46493; castMs [2000]
* Void Desert - Mirage Platform - Sand Sweep - Base (1): 46506
* Void Desert - Spurring Strikes - Base (2): 46637 46638
* Void Desert - Spurring Strikes - Damage Proxy (2): 46647 51862
* Void Desert - Rend - Base (1): 46654
* Void Desert - Discard - Base (1): 46655; castMs [3000]
* Void Desert - Thrash - Base - BEAX (1): 46657; castMs [2000]
* Void Desert - Heat Stroke - Base - T2 (1): 46665; castMs [2000]
* Void Desert - Heat Stroke - Base - T3 (1): 46666
* Void Desert - Consume - Proxy 1 (2): 46713 54607
* Void Desert - Exit Belly Knock Random - Base (1): 47190
* Void Desert - Shield Bubble - Knock Players Back - Base (1): 47212
* Void Desert - Mirage Platform - Slow Fall - Base (1): 47757
* Void Desert - Activate - Base (1): 48612
* Void Desert - Exhausted - Base (3): 65453 65455 65456; castMs [1500]
* Void Desert - Sand Trap - Base (1): 66482
* Void Desert - Sand Trap - GTAE Proxy (1): 66484; castMs [1000]
* Void Desert - Void Stare - Base (1): 66489
* Void Desert - Empty Energies - Base (1): 66490
* Void Desert - Empty Energies - PPAE Proxy (1): 66492
* Void Desert - Awaken Matrix - GTAE Proxy (2): 66500 66501
* Void Desert - Summon Remnants - Base (2): 66505 66506
* Void Desert - Tail Slap - Base (2): 66530 66531
* Void Desert - Ravage - Base (1): 66535
* Void Desert - Inevitable Extermination - Base (1): 66536
* Void Desert - Final Rest - Base (1): 66674
* Void Desert - Keeper of Sands - 2H Staff - Auto-Attack (2): 70688 70689
* Void Desert - Keeper of Sands - Organic Remnant - Auto-Attack #1 (1): 70690
* Void Desert - Keeper of Sands - Organic Remnant - Auto-Attack #2 (2): 70691 70692
* Void Desert - Keeper of Sands - Synthetic Remnant - Auto-Attack #1 (1): 70693
* Void Desert - Keeper of Sands - Synthetic Remnant - Auto-Attack #2 (2): 70694 70695
* Void Desert - Desertification - Base - BEAX (1): 72989
* Void Desert - Haste - Base - BEAX (1): 73021; castMs [1500]
* Void Desert - Wormsign - Base - BEAX (1): 73071
* Void Desert - Empowered - Creature - BEAX (1): 73112
* Void Desert - Haste - Proxy - BEAX (1): 73131
* Void Desert - Cinematic Disable - Base - BEAX (2): 73152 73153
* Void Desert - Cyclone - Base - BEAX (1): 74327
* Void Desert - Cyclone - Proxy - BEAX (1): 74329
* Void Desert - Death Wail - Base - BEAX (1): 74338
* Void Desert - Death Wail - Proxy - BEAX (1): 74339
* Void Desert - Revenge of the Fallen - Base - BEAX (2): 74341 74342
* Void Desert - Antlion - Earth Shock - Auto-Attack - BEAX (1): 74347
* Void Desert - Sand Shatter - Base - BEAX (1): 74419; castMs [2000]
* Void Desert - Desert Swarm - Aura - BEAX (1): 74452
* Void Desert - Desert Swarm - Spin CCW - BEAX (1): 74453
* Void Desert - Desert Swarm - Spin CW - BEAX (1): 74454
* Void Desert - Desert Swarm - Target Unit - BEAX (1): 74455
* Void Desert - Call Desert Swarm - Base - BEAX (2): 74466 74467
* Void Desert - Water Aura - BEAX (1): 74546
* Void Desert - Wormquake! - BEAX (1): 74659
* Void Desert - Desert Call - Summon (1): 78349

### e393 (Raid/Datascape: DefeatGloomclaw), 23 rows
* Gloomclaw - Ravager Add - Channel (1): 44167; castMs [2000]
* Gloomclaw - Buffed Strain Parasite - Command Attack Crystal (1): 44252
* Gloomclaw - Buffed Asura - Remove Attack Crystal Command (1): 44395
* Gloomclaw - Strain Parasite - Caustic Barrage (1): 44477
* Gloomclaw - Ravager - Ravage (1): 44490
* Gloomclaw - Ravager - Spinning Jumpable Telegraph Beam (2): 45118 45139; castMs [1000]
* Gloomclaw - Strain Parasite - Deranged Paroxysm (1): 62518
* Gloomclaw - Asura - Claws - Auto-Attack #1 (1): 72849
* Gloomclaw - Asura - Claws - Auto-Attack #2 (1): 72850
* Gloomclaw - Ravager - Auto-Attack #1 (1): 72851
* Gloomclaw - Ravager - Auto-Attack #2 (2): 72852 72853
* Gloomclaw - Claw Skurge - Auto-Attack #1 (1): 72854
* Gloomclaw - Claw Skurge - Auto-Attack #2 (1): 72855
* Gloomclaw - Claw Skurge Crippling Slash - Dash + Damage (1): 72863; castMs [300]
* Gloomclaw - Claw Skurge - Crippling Slash - CC Proxy (1): 72864
* Gloomclaw - Strain Parasite - Caustic Barrage - Missile Proxy (2): 72914 72915
* Gloomclaw - Strain Parasite Caustic Barrage - AE Proxy (1): 72916
* Gloomclaw - Corrupted Fraz - Ranged Auto-Attack (2): 72937 72938
* Gloomclaw Healing Aura (1): 77652

### e394 (Raid/Datascape: EscapeAvatusAttention), 60 rows
* Volatility Lattice - Spotlight Beam - Base (1): 46795
* Volatility Lattice - Spotlight Beam - Aura Proxy (2): 46796 46797
* Volatility Lattice - Spotlight Target Select (1): 46803
* Volatility Lattice - Activate Gong (1): 46867
* Volatility Lattice - Super Jump (2): 46909 70804; castMs [1500, 2000]
* Volatility Lattice - Protection Bubble - Base (1): 46910
* Volatility Lattice - Activate Fight (1): 46927; castMs [5000]
* Volatility Lattice - Obliterate - Base (2): 46939 72841; castMs [1000]
* Volatility Lattice - Guardian - OmniCharge - Base (1): 46941
* Volatility Lattice - Guardian - OmniCharge - Find Target (1): 46943
* Volatility Lattice - Shard - Collect Resource - Base (1): 46958
* Volatility Lattice - Guardian - Data Buffer - Base (1): 46971; castMs [450]
* Volatility Lattice - Safe Zone - Base (1): 46973
* Volatility Lattice - Guardian - Nullify - Base (2): 46986 46990; castMs [2000]
* Volatility Lattice - Guardian - OmniCharge - Find Target - Shockwave Version (2): 46999 47000
* Volatility Lattice - Devourer - Data Devour - Base (2): 47093 47094
* Volatility Lattice - Laser Sweep - Base (2): 47096 47097
* Volatility Lattice - Laser Sweep - AE Laser Proxy (1): 47098
* Volatility Lattice - Despawn Self - Base (1): 47100
* Volatility Lattice - Barrage - Base (1): 47104
* Volatility Lattice - Move Laser (2): 47107 47239; castMs [1000]
* Volatility Lattice - Activate Boss - Base (1): 47135
* Volatility Lattice - Enlarge Avatus - Base (2): 47154 47155; castMs [2500]
* Volatility Lattice - Spawn Devourer (2): 47195 47324
* Volatility Lattice - Spotlight Target (1): 47234
* Volatility Lattice - Omega Laser - Base (2): 47236 47493; castMs [1000]
* Volatility Lattice - Omega Laser - AE Laser Proxy (4): 47237 47238 47494 47495; castMs [1000]
* Volatility Lattice - Shrink Avatus - Base (1): 47385
* Volatility Lattice - Spotlight End (1): 69532
* Volatility Lattice - Null and Void (1): 69565
* Volatility Lattice - Interrupt Beams (2): 69569 69570
* Volatility Lattice - Summon Guardian - Summon Proxy (1): 69585; castMs [2300]
* Volatility Lattice - Summon Guardians - Base (1): 69586
* Volatility Lattice - Shard - Detect Player (2): 69622 73277
* Volatility Lattice - Spotlight - Disintegration Sector - Base (2): 69644 69645
* Volatility Lattice - Activate Breaker (1): 69779; castMs [4000]
* Volatility Lattice - Guardian - Nullify - GTAE Proxy (1): 70696
* Volatility Lattice - Guardian - Auto Attck #1 (1): 72083
* Volatility Lattice - Guardian - Auto Attck #2 (1): 72084
* Volatility Lattice - Devourer - Discharge - Base (3): 72820 72821 72822; castMs [1500, 4500]
* Volatility Lattice - Fall Damage Immunity (1): 72826

### e395 (Raid/Datascape: DefeatTheElementalGuardians1), 388 rows
* Water - Geyser - Subdued Proxy (1): 44222
* Air - Eye of the Storm -  Slow Fall Minigame (2): 46212 47242
* Air - Eye of the Storm - Slow Fall Minigame - Launch (1): 46214
* Air - Eye of the Storm - Slow Fall Minigame Fall Damage Signal (2): 46250 46251; castMs [2000]
* Water - Glacial Icestorm - Spinning Missile Ice (1): 46381
* Water Air - Glacial Icestorm - Missile (1): 46382
* Earth Elemental - Auto-Attack #1 - Base (1): 46393
* Earth Elemental - Auto-Attack #2 - Base (1): 46394
* Water - Auto-Attack #2 R2L (1): 46395
* Life - Auto-Attack #2 (1): 46397
* Fire - Auto Attack #1 (1): 46399
* Air - Twirl Bowling (1): 46868
* Air - Knock Up And Away Base (1): 46869
* Air - Knock Up And Away Proxy 1 (1): 46870; castMs [5000]
* Air - Knock Up And Away Proxy 2 (1): 46872; castMs [4700]
* Air - Walls of Wind - Base (1): 46942
* Air - Eye of the Storm - Slow Fall Proxy (2): 46951 78569; castMs [2000]
* Water - Sinkhole - Base (1): 47039; castMs [3000]
* Water - Sinkhole - Teleport Proxy (1): 47044; castMs [1000]
* Water - Sinkhole - Pool Proxy (1): 47045
* Water - Sinkhole - Disruption Base (1): 47108
* Air - Galeforce - Signal Self (1): 47110
* Water - Geyser - Base (1): 47149; castMs [6300]
* Water - Geyser - Tracking Proxy (1): 47150; castMs [2000]
* Water - Absorption Shield (2): 47151 74598; castMs [2000]
* Water - Absorption Shield Buff (1): 47152; castMs [2500]
* Water Air - Glacial Icestorm - Glacial Icestorm Missile Impact (1): 47177; castMs [2500]
* Air - Walls of Wind - Galeforce (Facing) (5): 47370 47371 47372 47373 47374
* Water - Raging Rapid - Base (1): 47393; castMs [3500]
* Water - Raging Rapid - Proxy 1 (1): 47394
* Air - Eye of the Storm - Slow Fall Minigame - Knock Back to Eye (2): 47431 47432; castMs [2000]
* Water - Raging Rapid - Proxy 3 (1): 47450
* Logic - Snake That Make (1): 47602
* Logic - Snake Make - Vibromatic Synthesis - Field (1): 47603; castMs [3000]
* Logic - Alphanumeric Hash - Base (5): 47651 47753 47754 47755 47756; castMs [1000, 2000]
* Logic - Slow Field Missile - Proxy Random Piece (1): 47653
* Logic - Alphanumeric Hash - Force Move (1): 47656
* Life - Blind Rotating Cones - Base (1): 47658
* Logic - Derez - Derezzify - Base (1): 47804
* Logic - Derez - Aura Corners (2): 47806 74557
* Logic - Derez - Move Telegraphs (2): 47808 74558
* Logic - Derez Piece - Aura (1): 47823
* Logic - Waiting for Res (1): 47917
* Logic (Life) - Combination Lockpick (4): 47965 47967 47968 47969; castMs [2000, 2500]
* Logic (Life) - Combined Lockpick (7): 47966 47970 47971 47972 48074 66268 ...; castMs [2500]
* Logic Life - Combination Pull to Target (1): 48097
* Logic Life Elemental - Combo - Center Expanding Sphere (1): 48172
* Logic (with Life) - Derez (Target Selection) (1): 48619
* Logic (with Life) - Derez (PBAE #1) (1): 48622
* Logic (with Life) - Derez (PBAE #2) (1): 48624
* Logic (with Life) - Derez (PBAE #3) (1): 48625; castMs [2967]
* Earth - Erupt - Platform Obstruction - Base (1): 48816; castMs [15000]
* Fire - Flame Wave - Base (1): 49955
* Earth Elemental - Fierce Swipe 1 - Base (1): 49978
* Fire - Ragnarok - Lava Floor - Damage (1): 49979
* Fire - Ragnarok - Lava Floor - Base (1): 49984; castMs [1000]
* Earth Elemental - Rockfall 1 - Base (1): 50014
* Earth Elemental - Rock Smash - Base (2): 50169 50170
* Earth - Erupt - Platform Obstruction - Proxy (1): 50191
* Fire - Summon Fire Mines (1): 50294
* Fire - Lava Mine Trigger (1): 50313
* Earth Elemental - Fierce Swipe 2 - Base (1): 51378
* Earth Elemental - Fierce Swipe 3 - Base (1): 51379; castMs [2000]
* Earth Elemental - Fierce Swipe 2 - Proxy (1): 51380
* Earth Elemental - Fierce Swipe 3 - Proxy (1): 51381
* Earth Elemental - Rocks Fall 2 - Base (1): 51382
* Earth Elemental - Rocks Fall 3 - Base (1): 51383; castMs [1500]
* Earth - Momentum - Speed Buff (1): 51444; castMs [500]
* Fire - Ragnarok - Steam Blast - Base (2): 51447 51448; castMs [1000]
* Water - Glacial Icestorm Knockback Aura (1): 51477
* Logic - Destructible Prison - Find Random Targets (1): 51819
* Logic - Destructible Prison - Base (2): 51820 51821
* Logic - Spread Out and Explode - Base (2): 52109 57339
* Logic - Spread Out and Explode - TAoE Proxy (2): 52110 52111
* Logic - Explode Rock Pillar - Base (1): 52402
* Water - Auto-Attack - Freezing Cleave Proxy (1): 52711; castMs [2000]
* Water - Raging Rapid - Random Spin (3): 52713 52714 52715; castMs [2000]
* Logic - Puzzle Blocks - Player Buff - Star Power - Remove (1): 52808
* Earth - Targeted, Unrestricted Moment of Opportunity (1): 52812
* Logic - Puzzle Blocks - Player Buff - Star Power (1): 52948
* Fire - Fire Buffet (1): 52955
* Water - Pair Off With Fire - On Water - Base (1): 53042
* Fire - Pair Off With Water - On Fire - Base (1): 53043
* Water - Pair Off With Fire (1): 53044
* Fire - Pair Off With Water (1): 53048
* Water Fire - Pair Off Bomb Explosion (2): 53070 63295; castMs [1500]
* Fire - Self Buff (1): 53105
* Water - Raging Rapid - Proxy 2 (2): 53137 53178
* Earth Elemental - Superquake - Base (2): 53202 73279; castMs [5000]
* Water - Wet Wipe - Threat Wipe (1): 53263
* Fire - Funeral Pyre - Threat Wipe (1): 53267
* Water - Wipe Threat from Water (1): 53268; castMs [6000]
* Fire - Wipe Threat from Fire (1): 53269
* Fire - Mine Debuff (1): 53318
* Water - Tsunami Fire Variant (1): 53338
* Air - Tornado - Aura (1): 53486
* Air - Earth Smash - Base (1): 53488
* Air - Earth Smash - Remove Buffs (1): 53489
* Air - Earth Smash - Aura (1): 53490
* Life - Annihilation Orb Base (1): 53831
* Life - Signal Annihilation Orbs (1): 54108
* Fire - Lava Pool (AOE) (1): 54315
* Life - Bounce (Force Move) (1): 54346
* Fire - Lava Pool (Summon) (1): 54643; castMs [1000]
* Logic Life - Combination Fluff (1): 56970
* Logic Life - Combo - Scaling On Vortex (1): 63212
* Logic Life - Combo - Center Expanding Sphere Visuals (2): 63306 63307
* Earth Elemental - Rock Smash - TAoE Proxy - Find Nearest Platform (2): 69857 69858; castMs [2500, 3300]
* Water - Tsunami Signal Start (1): 69863
* Water - Tsunami Signal End (1): 69864
* Earth - Fractured Crust - Base (1): 69907
* Fire - Eruption - Base (1): 69961
* Earth Elemental - Erupt - Base Proxy (1): 69987; castMs [3000]
* Air - Rock Hurricane - PBAE - GTAE Target Finder (2): 70001 70002; castMs [8000]
* Air - Rock Hurricane - GTAE Missile (1): 70006
* Air - Rock Hurricane - GTAE damage (1): 70007
* Air Elemental - Twirl - With Earth - Base (1): 70033
* Air Elemental - Strike! - With Earth - Base (2): 70034 70035
* Air - Vulnerability (1): 70039
* Air - Eye of the Storm - Signal End (2): 70047 70056
* Logic - Defragment - Puzzle Blocks (1): 70098
* Water - MoO Signal End (1): 70100
* Logic - Vulnerability (1): 70101
* Logic - Interrupt Armor Buff (1): 70110
* Air (with Life/Water) - Twirl - Base (1): 70510
* Air (with Life/Water) - Twirl - Damage (1): 70511
* Life - Bramble (Aura) (1): 70517
* Life (with Air) - Bramble (Summon) (2): 70518 70519
* Air - Tornado - Knockup (1): 70565
* Air - Tornado - Stat Mods (1): 70569
* Logic - Cloud Computing - Aura Base (1): 70587
* Fire Elemental (with Life) - Eruption - Base (1): 70709
* Fire - Meteor (Base) (1): 70792
* Fire - Meteor (Target Selection) (1): 70793
* Fire - Meteor (AOE) (1): 70796
* Air - Eye of the Storm - Slow Fall Minigame Stacking Fluff Debuff (2): 70934 70935
* Water - Disruption Mode - Buff (2): 71121 71123
* Water - Tsunami 01 (1): 72168; castMs [3900]
* Water - Tsunami 02 (1): 72169
* Water - Tsunami 03 (1): 72170
* Water - Tsunami 05 (1): 72171
* Water - Tsunami 06 (1): 72172
* Water - Tsunami 08 (1): 72173
* Fire - Flame Wave - Random Direction Force Move (1): 72313
* Air (with Life/Water) - Twirl - Kill Bramble/Tomb (1): 72549
* Air (with Life/Water) - Twirl - Aura (for Bramble/Ice Tomb) (1): 72609
* Air (with Life/Water) - Twirl (Target Selection) (1): 72921
* Life - Ability Restrict - Aura - Inner - BEAX (1): 72951
* Life (with Fire) - Self Buff (1): 73118; castMs [1000]
* Life - Caretaker - Telegraph [DNT] (1): 73128
* Life - Bomb - Detonate (Cast) (1): 73134; castMs [2000]
* Life - Bomb - Detonate (Remove Debuff) (1): 73135; castMs [2000]
* Life - Bramble - Damage Reflect (Proc) (1): 73139; castMs [2000]
* Life - Bramble - Damage Reflect (Damage) (1): 73140; castMs [1000]
* Fire - Sticky Fire (Base) (1): 73145
* Fire - Sticky Fire (Target Selection) (1): 73146
* Fire - Sticky Fire (AOE) (1): 73147
* Fire - Sticky Fire (Remove) (1): 73150
* Fire - Sticky Fire (Single Target) (1): 73166
* Life - Scrab - Deathtouch (Target Selection) (1): 73189
* Life - Summon Scrab (1): 73198
* Life - Fire Event - Life Essence - On Death AOE (1): 73212
* Life - Fire Event - Add Facility (Target Selection) (2): 73225 73226
* Life - Fire Event - Shield (Create) (1): 73235; castMs [2000]
* Life - Area Heal - Buff (1): 73236
* Life - Fire Event - Shield Grant (Target Selection) (1): 73237
* Life - Fire Event - Add Facility (Facility) (1): 73244; castMs [500]
* Life - Fire Event - Shield Grant (Bestow Skill) (1): 73248
* Life - Fire Event - Shield Remove (AOE) (1): 73250
* Life - Fire Event - Remove Facility (Target Selection) (2): 73251 73252
* Life - Fire Event - Remove Facility (Dispel) (1): 73253
* Life - Area Heal - Aura (1): 73258; castMs [1500]
* Life - Fire Event - Root (Target Selection) (7): 73259 74429 74430 74431 74432 74433 ...; castMs [10000]
* Life - Fire Event - Root (AOE) (1): 73260
* Life - Fire Event - Flame Wave 01 (2): 73267 73269
* Life - Fire Event - Flame Wave (Move + Despawn) (1): 73276
* Earth Elemental - Raw Power - Base (1): 73278
* Water - Disruption - Geyser (1): 73280
* Air - Rock Hurricane - Conversion (1): 73282
* Earth Elemental - Superquake - Raw Power Version (1): 73284
* Earth Elemental - Tectonic Steps - Base (1): 73285
* Earth Elemental - Tectonic Steps - PBAE (1): 73286; castMs [1700]
* Life - Fire Event - Flame Wave (Damage) (1): 73289
* Life - Fire Event - Flame Wave 02 (1): 73292; castMs [950]
* Life - Fire Event - Flame Wave 04 (1): 73293
* Life - Fire Event - Flame Wave 05 (1): 73294
* Air - Galeforce - Base (1): 73308
* Fire - Fire Buffet - Damage Proxy (1): 73309
* Water - Water Buffet - Damage Proxy (1): 73310
* Air - Walls of Wind - Parry Galeforce - Signal (1): 73313
* Life - Fire Event - Event Completed (AOE) (1): 74323
* Life - Fire Event - Re-Add Facility (Target Selection) (11): 74325 74436 74437 74438 74439 74440 ...
* Life - Summon - Life Force (Force Move Out ) (1): 74336
* Logic - Tsunami Combo - Outer Ring - Water (1): 74343
* Logic - Tsunami Combo - Fluff Channel (1): 74345
* Fire - Flame Wave (Base) (1): 74348; castMs [2000]
* Life - Summon - Life Force (Base) (1): 74391
* Life - Summon - Life Force (Create Unit) (1): 74392
* Air - Tornado Speed Buff - Base (1): 74394
* Water - Ice Block Tether (1): 74399; castMs [500]
* Earth - Fissure - Base (1): 74401
* Water - Ice Block Tether Target Selector (3): 74422 74423 74424; castMs [1500, 2700]
* Life - Summon - Life Force (Debuff) (1): 74446
* Life - Summon - Life Force (Aura) (1): 74447
* Air - Tempest - Aura - Pacify (1): 74451; castMs [1]
* Air - Tempest - Cast (1): 74468; castMs [1000]
* Life - Swarm - Auto-Attack (2): 74471 74478
* Life - Hive - Summon Swarm (Proc & Proxy) (1): 74479
* Life - Hive - Summon Swarm (Summon) (1): 74480
* Life - Hive (Base) (1): 74481; castMs [1500]
* Fire - Eruption - AA version (w/Earth) (1): 74496
* Life - Fire - Buff (Base) (1): 74502
* Air - Lightning Strike - Tracker (1): 74516; castMs [1000]
* Air - Lightning Strike - Strike (Player) (2): 74528 74529
* Earth - Gronyx Add - Base (1): 74550
* Earth - Gronyx Adds - Proxy (1): 74551
* Air - Tempest Remove (1): 74553
* Earth - Gronyx - Flame Hardened - Buff (1): 74554
* Air - Lightning Strike - Target Selection (1): 74559
* Life - Trigger Tree Cycle (1): 74560
* Air (with Life) - Twirl (Inf Proxy) (1): 74583
* Life - Logic Leash - Remove Leash (1): 74617
* Life (with Logic) - Buff (Inf Ticking) (7): 74619 74620 74622 74624 74625 74626 ...; castMs [1500]
* Life (with Logic) - Event (MOO + Cleanup) (1): 74621
* Life (with Logic) - Buff (Event Start Signal) (1): 74629
* Logic - Logic Leash - Logic - Visual (1): 74630
* Air - Eye of the Storm - Break Beam (1): 74632
* Life (with Logic) - Bramble (Summon) [DNT] (1): 74634
* Logic (with Life) - Snake (Follow Target) (2): 74636 74637
* Logic (with Life) - Snake (Follow Target - Dispel) (1): 74639
* Logic - Alphanumeric Hash (Base) (1): 74648
* Logic - Alphanumeric Hash (Target Selection) (1): 74649
* Life - Logic Leash - Life - Targeting (1): 74663
* Life - Logic Leash - Aura (1): 74664
* Life - Logic Leash - Damage Proxy (1): 74666
* Life - Logic Leash - Remove Facilty (1): 74670; castMs [3000]
* Life - Logic Leash - (Master Cast -Non Tank) (2): 74678 74679
* Earth - Gronyx - Flame Hardened - Base (1): 74681; castMs [7000]
* Logic - Tsunami Combo - Outer Ring - Logic (1): 74682
* Logic - Event - Kill on Pull Fail (1): 74701
* Life - Logic Leash - Broadcast for Damage (1): 74709
* Logic - Cooldown Buff (1): 74715
* Air - Earth Smash - Overcharged - Single Target Proxy (2): 74848 74849
* Air - Earth Smash - Overcharged - PBAE Proxy (1): 74851
* Life - Bramble - Damage Reflect Dot (1): 75107
* Water - Pair Off With Fire - On Water - Proxy (1): 75127
* Fire - Pair Off With Water - On Fire - Proxy (1): 75128
* Life - Logic Leash - Remove Leash - Dead Player (1): 75145
* [DS] e395 e396 e397 - Obliteration - BEAX (7): 78439 78447 78448 78449 78450 78451 ...; castMs [2000]
* [DS] e395 e396 e397 - Obliteration - Base - BEAX (1): 78453; castMs [2000]
* Water - Elemental Discharge - Knockout Timer (1): 78461; castMs [2000]
* Logic (Water) - Combinatorial Genomics (6): 78463 78464 78465 78466 78467 78468; castMs [2000]
* Life - Bomb - Detonate (Triggered) (2): 78477 78478; castMs [2000]
* Life - Bomb - Detonate (Aura) (1): 78479; castMs [2000]
* Logic (Water) - Data Disrupter - Pickup (1): 78480; castMs [2000]
* Logic (Water) - Data Disrupter - Player (1): 78481; castMs [2000]
* Life - Bomb - Detonate (Proc) (1): 78482; castMs [2000]
* Logic Elemental - Defragment - Base - With Earth (2): 78483 78484; castMs [2000]
* Logic (Water) - Circuit Breaker - Base (1): 78485; castMs [2000]
* Logic (Water) - Circuit Breaker - AE Proxy (1): 78486; castMs [2000]
* Air - Eye of the Storm - Impact (1): 78544; castMs [2000]
* Fire - Eruption - AA version (w/Earth) - Damage - BEAX (1): 78678; castMs [1200]
* Fire - Eruption - AA version (w/Earth) - Base - BEAX (2): 78697 78698
* Fire - Eruption - AA version (w/Earth) - Cone Dmg - BEAX (1): 78699
* Anti Cleave Aura - Damage Shield (1): 78815
* Life - Aura - Damage Shield (1): 78819
* Logic (Water) - Memory Leak (1): 78941; castMs [1000]
* Logic (Water) - Combo Signal End (1): 78952
* Logic - Health Adjustment (DNT) (2): 78993 78996
* Water - Health Adjustment (DNT) (3): 78994 78997 78998
* Logic - Circuit Breaker AoE Pull - DNT (1): 79009
* Water - Geyser - Aura Cast - DNT (1): 79010
* Air - Health Adjustment (DNT) (2): 79294 79299
* Fire - Health Adjustment (DNT) (3): 79300 79303 79304
* Earth - Health Adjustment (DNT) (3): 79306 79307 79308; castMs [2000]
* Life - Health Adjustment (DNT) (3): 79309 79310 79311; castMs [2500]
* Life (with Logic) - Bramble (Base) [DNT] (1): 79341
* Logic Elemental - Snake Make - Vibromatic Synthesis - Field (Brambles) [DNT] (1): 79344; castMs [1500]
* Earth Elemental - Fierce Swipe - Debuff Proxy (1): 79411
* Earth Elemental - Fierce Swipe - Disable Proxy (1): 79413
* Air (with Water) - Area Denial Bomb (1): 79430
* Air (with Water) - Area Denial Hurricane (1): 79431; castMs [2750]
* Life - Logic - OnStackApplied AOE (1): 79439
* Earth - Gronyx - Flame Hardened - Casting Invuln Proxy (1): 79491
* Air - Tornado - Cast (1): 79713; castMs [2000]
* Air (with Life/Water) - Remove Twirl (1): 79716; castMs [2000]
* Earth - Momentum - Speed Buff Multi - DNT (1): 80034
* Logic (with Life) - Snake (Follow Target) On Death Proxy (1): 80197; castMs [633]
* Air (with Water) - Twirl Target Selection (1): 82385; castMs [1000]
* Life (with Logic) - Buff (Stacking) - End Buff (1): 83851

### e399 (Raid/Datascape: DefeatAvatus), 488 rows
* Avatus - Auto-Attack - Holo-Swipe - Cleave Proxy R2L - Boss Datascape (1): 44403
* Avatus - Auto-Attack - Holo-Swipe - Cleave Proxy L2R - Boss Datascape (2): 44523 44525; castMs [3000]
* Avatus - Holo-Crush - 2H Cleave Telegraph - Boss Datascape (1): 44526
* Avatus - Holo Hand - Auto-Attack 1 - L2R - Smack (1): 44540; castMs [10000]
* Avatus - Holo Hand - Electric Field - Base (1): 44546
* Avatus - Holo Hand - Electric Field - Proxy (1): 44548
* Avatus - Quick Beam - Gun Grid - Datascape (2): 44595 44596; castMs [1900]
* Avatus - Holo Hand - Ground and Pound - Base (1): 44597
* Avatus - Disruption Beam - Telegraph Fluff Proxy (1): 44620; castMs [1500]
* Avatus - Holo Hand - Strikethrough - Base (1): 44624; castMs [1600]
* Avatus - Holo Hand - Strikethrough - Proxy (2): 44626 44627
* Avatus - Holo Hand - Crushing Blow (1): 44638; castMs [2200]
* Avatus - Roaming Disruption Beam - Random Direction Left Offset (1): 44649
* Avatus - Roaming Disruption Beam - Random Direction Right Offset (1): 44650
* Avatus - Channeled Beam - Aperture - Gun Grid - Base n (1): 44669
* Avatus - MoO - Vulnerable (1): 44701
* Avatus - Shoulder Cannon Random - Cast Proxy - Boss Datascape (1): 44727
* Avatus - Rear - Nullifying Rays - Rear Lasers #1 (1): 44732; castMs [7000]
* Avatus - Holo Hands Tracker Buff - Boss Datascape (1): 44779
* Avatus - Rear - Nullifying Rays - Rear Lasers #2 (2): 44782 44783
* Avatus - Giant Red Beam - Base n (1): 44784
* Caretaker - Giant Green Beam - Base n (1): 44785; castMs [2800]
* Avatus - Auto-Attack #2 - L2R (1): 44786; castMs [2000]
* Avatus - Rear - Burning Exhaust - Rear Flame #1 (2): 44804 44805; castMs [3000]
* Avatus - Rear - Burning Exhaust - Rear Flame #2 (1): 44806; castMs [1000]
* Avatus - Remove Holo Hands Tracker Buff - Boss Datascape (1): 44857
* Avatus - Remove Guns Tracker Buff - Boss Datascape (1): 44858
* Avatus - Guns Tracker Buff - Boss Datascape (1): 44859
* Avatus - Summon Holo Hands - Base n (1): 44860
* Avatus - Toggle Active Tracker Buff - Boss Datascape (1): 44869; castMs [2000]
* Avatus - Summon Holo Cannons - Base n (1): 44876
* Avatus - Summon Adds - Boss Datascape (1): 44877
* Avatus - Quick Beam - Targeting Reticle - Boss Datascape (1): 44878
* Avatus - Annihilation - Double Cannon - Tank Spike - Base n (1): 44894
* Avatus - Rear Laser Proxy Pattern - Boss Datascape (2): 44900 44901
* Avatus - Holo Hand - Clobbering Assault- Fixate Target - Base (1): 44902
* Avatus - Charged Beam - Gun Grid - Datascape (4): 44904 44905 44907 44908
* Avatus - Holo Hand - Clobbering Assault - Fixate Move (2): 44909 44910
* Avatus - Holo Hand - Clobbering Assault (1): 44912
* Avatus - Convergent Force - Force Move Proxy (1): 44923; castMs [2750]
* Avatus - Green Buff - Scientist Matrix Mechanic - Boss Datascape (10): 44936 45010 45011 45012 45013 45015 ...; castMs [2000, 16000]
* Avatus - Red Buff - Scientist Matrix Mechanic - Boss Datascape (10): 44941 45001 45002 45003 45004 45005 ...; castMs [500, 1000]
* Avatus - Blue Buff - Scientist Matrix Mechanic - Boss Datascape (9): 44942 44993 44994 44995 44996 44997 ...
* Avatus - Convergent Force - Ring 6 - Arc 1 (1): 44953
* Avatus - Convergent Force - Ring 6 - Arc 3 (1): 44956
* Avatus - Convergent Force - Ring 6 - Arc 4 (1): 44957
* Avatus - Convergent Force - Ring 7 - Arc 2 (1): 44958
* Avatus - Convergent Force - Ring 7 - Arc 3 (1): 44959
* Avatus - Convergent Force - Ring 7 - Arc 4 (1): 44960
* Avatus - Convergent Force - Ring 8 - Arc 2 (1): 44961
* Avatus - Convergent Force - Ring 8 - Arc 3 (1): 44962
* Avatus - Convergent Force - Ring 8 - Arc 4 (1): 44963
* Avatus - Convergent Force - Ring 9 - Arc 1 (1): 44965
* Avatus - Convergent Force - Ring 9 - Arc 2 (1): 44966
* Avatus - Convergent Force - Ring 9 - Arc 4 (1): 44967
* Avatus - Convergent Force - Center Sphere (1): 44970
* Avatus - Convergent Force - Arc - Knockback Proxy (1): 44972
* Avatus - Invalidate - One Shot (1): 44973
* Avatus - Green Matrix Dispelled Proxy - Boss Datascape (7): 45022 45023 45024 45025 45026 45027 ...; castMs [1500]
* Avatus - Red Matrix Dispelled Proxy - Boss Datascape (7): 45029 45030 45031 45032 45033 45034 ...; castMs [1000]
* Avatus - Blue Matrix Dispelled Proxy - Boss Datascape (9): 45036 45037 45038 45039 45040 45041 ...; castMs [1500]
* Avatus - Blue Buff Dispel Beam - Scientist Path - Boss Avatus (1): 45049; castMs [9000]
* Avatus - Red Buff Dispel Beam - Scientist Path - Boss Avatus (1): 45050
* Avatus - Green Buff Dispel Beam - Scientist Path - Boss Avatus (1): 45051
* Avatus - Red Buff Dispel Extra Action Button - Avatus - Datascape (3): 45100 45101 66221
* Avatus - Miniboss Telegraph Smash (1): 45102
* Avatus - Binding Strike - Lateral Shot - Left (1): 45121; castMs [2000]
* Avatus - Binding Strike - Lateral Shot - Proxy Stun (1): 45124
* Avatus - Binding Strike - Lateral Shot - Right (1): 45128
* Conflict Point - Movement Speed 0.25 (1): 45143; castMs [2500]
* Conflict Point - Movement Speed 0.5 (1): 45145
* Conflict Point - Movement Speed 0.75 (1): 45152
* Conflict Point - Movement Speed 1.0 (1): 45153; castMs [2500]
* Avatus - Holo Cannon - Resonant Discharge - Row 01 (1): 45156
* Avatus - Holo Cannon - Energized Bombardment - Grenade 01 (1): 45172
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel 01 (1): 45178; castMs [2000]
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel - Move Angle 0 (1): 45181; castMs [2000]
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel - Move Angle 45 (1): 45183; castMs [2000]
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel 02 (1): 45185
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel - Move Angle 90 (1): 45188; castMs [2000]
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel - Move Angle 135 (2): 45189 45190; castMs [2000]
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel - Move Angle 180 (1): 45191
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel - Move Angle 225 (2): 45192 45193
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel - Move Angle 270 (2): 45194 45195; castMs [3000]
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel - Move Angle 315 (1): 45196
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel 03 (2): 45197 45198
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel 04 (1): 45199
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel 05 (1): 45200
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel 06 (1): 45201; castMs [2000]
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel 07 (2): 45202 45203
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel 08 (2): 45205 45206
* Avatus - Gun Adds - Minor Red Beam - Removal Signal (1): 45211
* Avatus - Holo Cannon - Energized Bombardment - Base n (1): 45224
* Avatus - Blue Matrix Buff Consequence Target Selection - Scientist (1): 45235
* Avatus - Blue Matrix Buff Consequence Damage - Scientist Boss - Datascape (1): 45238
* Avatus - Holo Cannon - Energized Bombardment - Grenade 02 (1): 45242; castMs [3000]
* Avatus - Holo Cannon - Energized Bombardment - Grenade 03 (2): 45243 45244
* Avatus - Holo Cannon - Energized Bombardment - Grenade 04 (1): 45245
* Avatus - Holo Cannon - Energized Bombardment - Grenade 05 (1): 45246; castMs [2500]
* Avatus - Holo Cannon - Energized Bombardment - Grenade 06 (1): 45247
* Avatus - Holo Cannon - Energized Bombardment - Grenade 07 (2): 45248 45250
* Avatus - Holo Cannon - Energized Bombardment - Grenade 08 (1): 45251
* Avatus - Holo Cannon - Energized Bombardment - Grenade 09 (1): 45254
* Avatus - Holo Cannon - Energized Bombardment - Grenade 10 (1): 45256
* Avatus - Holo Cannon - Energized Bombardment - Grenade 11 (1): 45257
* Avatus - Holo Cannon - Energized Bombardment - Grenade 12 (1): 45258
* Avatus - Holo Cannon - Energized Bombardment - Grenade 13 (1): 45260; castMs [2050]
* Avatus - Holo Cannon - Resonant Discharge - Row 02 (1): 45267
* Avatus - Holo Cannon - Resonant Discharge - Row 03 (1): 45268; castMs [3000]
* Avatus - Holo Cannon - Resonant Discharge - Row 04 (1): 45269; castMs [4000]
* Avatus - Holo Cannon - Resonant Discharge - Row 05 (1): 45271
* Avatus - Holo Cannon - Resonant Discharge - Base n (2): 45272 45273; castMs [3000]
* Avatus - Blue Buff Dispel Extra Action Button - Avatus - Datascape (2): 45288 45289; castMs [2550]
* Avatus - Green Buff Dispel Extra Action Button - Avatus - Datascape (1): 45290
* Avatus - Holo Cannon - Shock Bola - Proxy 01 (1): 45295; castMs [2000]
* Avatus - Holo Cannon - Shock Bola - Proxy 02 (3): 45304 45305 70003
* Avatus - Holo Cannon - Shock Bola - Base n (2): 45306 87831
* Avatus - Holo Cannon - Shock Bola - Proxy 03 (2): 45307 87832
* Avatus - Holo Cannon - Shock Bola - Proxy 04 (2): 45308 87833
* Avatus - Holo Cannon - Shock Bola - Proxy 05 (2): 45309 87834
* Avatus - Blue Facility - Scientist Matrix Mechanic - Boss Datascape (1): 45322
* Avatus - Red Facility - Scientist Matrix Mechanic - Boss Datascape (2): 45324 45325
* Avatus - Green Facility - Scientist Matrix Mechanic - Boss Datascape (1): 45326; castMs [3000]
* Avatus - Holo Cannon (Air) - Cannon Blast (1): 45332
* Avatus - Holo Cannon (Air) - Viral Infection - Base n (2): 45362 45363
* Avatus - Holo Cannon (Air) - Viral Infection - Proxy 1 (1): 45364
* Avatus - Holo Cannon (Air) - Viral Infection - Proxy 2 (1): 45365
* Avatus - Holo Cannon (Air) - Viral Infection (DOT) (2): 45366 45367
* Avatus - Protective Barrier - Base n (1): 45371; castMs [1800]
* Avatus - Remove Protective Barrier - Base n (1): 45372
* Avatus - Explorer Phase Laser Grid - Boss - Datascape (1): 45395; castMs [1000]
* Avatus - Red Buff Consequence - Scientist Matrix Mechanic - Boss Datascape (1): 45401
* Avatus - Red Buff Consequence Damage Proxy - Inner Rings (3): 45402 45403 45404; castMs [2000]
* Avatus - Red Buff Consequence Damage Proxy - Outer Rings (4): 45409 45410 45413 45414; castMs [1000]
* Avatus - Green Buff Consequence - Scientist Phase (1): 45418
* Avatus - Phase Punch - Base n (1): 45446
* Avatus - Summon Guns - Showdown - Minor Beams (1): 45457
* Avatus - Holo Cannon (Ground) - Energy Transfusion (1): 45481; castMs [6000]
* Avatus - Warrior Clone - Auto Attack #1 R2L (1): 45491
* Avatus - Warrior Clone - Auto Attack #2 L2R (1): 45492
* Avatus - Medic Clone - Auto Attack #1 (1): 45494; castMs [3150]
* Avatus - Medic Clone - Auto Attack #2 (1): 45495
* Avatus - Spellslinger Clone - Auto Attack #1 (2): 45496 45497; castMs [2000]
* Avatus - Spellslinger Clone - Auto Attack #2 (1): 45498
* Avatus - Stalker Clone - Auto Attack #1 L2R (1): 45499
* Avatus - Esper Clone - Auto Attack #1 (1): 45501
* Avatus - Conflict Point - Deflection Beam - Proxy 2 (1): 45505; castMs [1500]
* Avatus - Conflict Point - Deflection Beam - Proxy 3 (2): 45506 45507
* Avatus - Conflict Point - Deflection Beam - Base n (1): 45509; castMs [3000]
* Avatus - Spellslinger Clone Ignite - Soldier Phase - Datascape (1): 45510
* Avatus - Holo Cannon (Air) - Eradication (1): 45512
* Avatus - Warrior Clone Charge - Soldier Phase - Datascape (1): 45521
* Avatus - Holo Cannon (Ground) - Interrupt Eradication (1): 45524
* Avatus - Warrior Clone Whirlwind - Soldier Phase - Datascape (1): 45525
* Avatus - Warrior Clone Enrage - Soldier Phase - Datascape (2): 45527 45528
* Avatus - Engineer Clone Pulse Blast - Soldier Phase - Datascape (1): 45533
* Avatus - Esper Clone Snare Aura - Soldier Phase - Datascape (1): 45543; castMs [2000]
* Avatus - Esper Clone Concentrated Blades Proxy 1 - Soldier Phase - Datascape (2): 45545 45546
* Avatus - Esper Clone Concentrated Blades Proxy 2 - Soldier Phase - Datascape (2): 45548 45549
* Avatus - Esper Clone Concentrated Blades Proxy 3 - Soldier Phase - Datascape (2): 45550 45551
* Avatus - Esper Clone Concentrated Blades Proxy 4 - Soldier Phase - Datascape (1): 45552
* Avatus - Esper Clone Concentrated Blades Proxy 5 - Soldier Phase - Datascape (3): 45553 45554 45555; castMs [2400]
* Avatus - Esper Clone Concentrated Blades - Soldier Phase - Datascape (1): 45556
* Avatus - Esper Clone Concentrated Blades Knockdown Proxy - Soldier Phase - Datascape (1): 45564
* Avatus - Esper Clone Snare Aura Proxy - Soldier Phase - Datascape (1): 45568
* Avatus - Esper Clone Shockwave - Soldier Phase - Datascape (1): 45572
* Avatus - Medic Clone Probes - Soldier Phase - Datascape (1): 45573
* Avatus - Medic Clone Probe Detonate - Soldier Phase - Datascape (2): 45574 45575
* Avatus - Medic Clone Probes Field - Soldier Phase - Datascape (2): 45576 45581
* Avatus - Holo Cannon (Air) - Eradication - Signal (1): 45588
* Avatus - Stalker Clone Collapse - Soldier Phase - Datascape (1): 45601
* Avatus - Stalker Clone Collapse Proxy - Soldier Phase - Datascape (2): 45602 45603
* Avatus - Stalker Clone Collapse CC Proxy - Soldier Phase - Datascape (1): 45605; castMs [1900]
* Avatus - Stalker Clone Mark Damage Proxy - Soldier Phase - Datascape (2): 45610 45612
* Avatus - MoO End - Signal Showdown Restart (1): 45622; castMs [2100]
* Avatus - Center - Knockback & Damage - Base n (1): 45833
* Avatus - God Unit - Permanent Delete - Wipe (1): 45841
* Avatus - God Unit - Signal Start (1): 45862
* Avatus - God Unit - Signal Cancel (1): 45863
* Avatus - Holo Cannon (Air) - Cannon Blast - Target Selection (1): 45893
* Avatus - Positional Blind - Explorer Phase - Datascape (2): 45902 45903
* Avatus - Holo Hand - Crushing Blow - Proxy (1): 45925
* Avatus - Holo Cannon - Energized Bombardment - Damage Proxy (1): 45930
* Avatus - Obliteration Beam - Channeled Proxy (2): 45948 45949
* Avatus - Obliteration Beam - Initial Cast (1): 45955
* Danger Room - Exit (1): 45964
* Avatus - Obliteration Beam - Signal World - Base n (1): 45965
* Avatus - Phase Punch - Proxy - Danger Room (1): 46009
* Avatus - Engineer Clone Summon Bots - Datascape (1): 46015; castMs [1100]
* Avatus - Spellslinger Clone Gate - Datascape (2): 46016 46017
* Avatus - Spellslinger Clone Gate Stun - Proxy - Datascape (1): 46018
* Avatus - Spellslinger Clone Wild Barrage Progressive - Datascape (2): 46019 46020
* Avatus - Data Storm -  Channel Proxy (1): 46030
* Avatus - Explorer Phase Jump Buff - Datascape (1): 46071
* Avatus - Holo Cannon - Energized Bombardment - Energized Shrapnel Proxy (2): 46075 46076
* Danger Room - Patrol Knockback (1): 46094
* Avatus - Miniboss - Auto-Attack #1 - L2R (2): 46115 46116
* Avatus - Medic Clone Telegraph - Soldier Phase (1): 46121; castMs [20500]
* Danger Room - Ring 3 - AB (1): 46131
* Avatus - Medic Clone Heal - Soldier Phase - Datascape (2): 46139 46140
* Avatus - Medic Clone Proxy Move - Datascape (1): 46141
* Danger Room - Random CC (2): 46150 46153
* Danger Room - Blind Proxy (1): 46151
* Danger Room - Disorient Proxy (1): 46152
* Danger Room - Knockdown Proxy (1): 46154; castMs [1000]
* Danger Room - Ring 5 - BASE CCW (1): 46192
* Danger Room - Ring 5 - Proxy Rotate (Counter) (1): 46197
* Danger Room - Ring 5 - Proxy Rotate (Clock) (1): 46201
* Danger Room - Ring 5 - Spinner - BASE (1): 46203; castMs [3000]
* Danger Room - Ring 6 - Proxy (1): 46238
* Avatus - Obliteration Beam - Spin Proxy CCW (1): 46240
* Avatus - Obliteration Beam - Spin Proxy CW (1): 46241; castMs [2000]
* Danger Room - Ring 6 (1): 46248; castMs [2000]
* Danger Room - Snare Proxy (1): 46253
* Avatus - Binding Strike - Lateral Shot Cannon - Lateral Shot (1): 46254
* Danger Room - Protection Circle (1): 46255
* Avatus - Binding Strike - Summon Lateral Shot Cannon 1 (1): 46258
* Avatus - Binding Strike - Summon Lateral Shot Cannon 2 (2): 46259 46261
* Avatus - Binding Strike - Summon Lateral Shot Cannon 3 (2): 46262 46263
* Avatus - Binding Strike - Summon Lateral Shot Cannon 4 (1): 46265
* Avatus - Binding Strike - Summon Lateral Shot Cannon 5 (1): 46266
* Avatus - Binding Strike Field Left - Lateral Shot - Proxy (1): 46279
* Avatus - Binding Strike Field Right - Lateral Shot - Proxy (3): 46280 46281 46282; castMs [2800]
* Danger Room - Protection Effect (1): 46298
* Avatus - Explorer Phase Projectile - Datascape (1): 46299; castMs [2000]
* Avatus - Data Storm - Meteor Proxy 1 - Far (1): 46313
* Avatus - Shock Sphere Field - Explorer Phase - Datascape (2): 46314 46315
* Avatus - Shock Sphere Damage - Explorer Phase (1): 46317
* Avatus - Data Storm - Meteor Proxy 2 - Far (1): 46323
* Avatus - Data Storm - Meteor Proxy 3 - Far (1): 46324
* Avatus - Data Storm - Meteor Proxy 4 - Far (1): 46325
* Avatus - Data Storm - Meteor Proxy 5 - Far (2): 46326 46327
* Avatus - Data Storm - Meteor Proxy 1 - Near (1): 46328
* Avatus - Data Storm - Meteor Proxy 2 - Near (1): 46329; castMs [500]
* Avatus - Data Storm - Meteor Proxy 3 - Near (2): 46330 46331
* Avatus - Data Storm - Meteor Proxy 4 - Near (1): 46333
* Avatus - Data Storm - Meteor Proxy 5 - Near (1): 46334
* Avatus - Data Storm - Target Collection 1 (1): 46335
* Avatus - Data Storm - Target Collection 2 (1): 46336
* Avatus - Data Storm - Target Collection 3 (1): 46337
* Avatus - Data Storm -  Base n (1): 46343
* Avatus - Delay Fluff (1): 46349
* Danger Room - Fragment Disguise (1): 46352
* Avatus - Holo Cannon - Teleport Signal (1): 46371
* Avatus - Binding Strike - Lateral Shot - Base n (1): 46402; castMs [2500]
* Avatus - Rear Attacks - Base n (2): 46411 46413
* Avatus - Delay Fluff - Rear Attack (1): 46437
* Avatus - Settler - Tesla Tower - Tesla Strike - Base n (2): 46787 46788
* Avatus - Settler - Tesla Tower - Tesla Strike - Proxy 1 (1): 46789
* Avatus - Settler - Tesla Tower - Tesla Strike - Proxy 2 (1): 46790; castMs [2000]
* Avatus - Settler - Tesla Tower - Tesla Strike - Proxy 3 (1): 46792; castMs [2000]
* Avatus - Settler - Tesla Tower - Tesla Strike - Dmg Proxy (2): 46801 46802; castMs [1700]
* Avatus - Settler - Shoulder Cannon - Infinite Proxy (1): 46807
* Avatus - Settler - Defensive Tower - Barrier (1): 46812; castMs [2000]
* Avatus - Settler - Melee Swipes - Infinite Proxy (1): 46814; castMs [1500]
* Avatus - Settler - Front Left Swipe (1): 46815
* Avatus - Settler - Rear Left Swipe (1): 46821
* Avatus - Settler - Rear Right Swipe (1): 46822
* Avatus - Settler - Front Right Swipe (1): 46823
* Avatus - Settler - Melee Swipes (Group 2) (1): 46826
* Avatus - Settler - Melee Swipe - FL + RL (1): 46827
* Avatus - Settler - Melee Swipe - FL + RR (1): 46828
* Avatus - Settler - Melee Swipe - FL + FR (1): 46829
* Avatus - Settler - Melee Swipe - RL + RR (1): 46830
* Avatus - Settler - Melee Swipe - RL + FR (1): 46831
* Avatus - Settler - Melee Swipe - RR + FR (1): 46832
* Avatus - Settler - Melee Swipes (Random Group) (1): 46833; castMs [2000]
* Avatus - Settler - Supply Probe - Tower Part (Wall) (2): 46849 46850; castMs [1500]
* Avatus - Settler - Build Tower (1): 46852
* Avatus - Settler - Defensive Shield - Base n (2): 46854 46855; castMs [160]
* Avatus - Settler - Defensive Tower - Ping Tower (1): 46856; castMs [2000]
* Avatus - Settler - Defensive Tower - Construct Barrier (1): 46858
* Avatus - Settler - Defensive Tower - Barrier - Channel Proxy (1): 46873; castMs [1250]
* Avatus - Settler - Destroy Towers (1): 46874; castMs [750]
* Avatus - Settler - Defensive Tower - Barrier - Debuff Proxy (1): 46900
* Avatus - Settler - Self Fluff - Infinite Proxy (1): 46954
* Avatus - Settler - Self Fluff (1): 46955
* Avatus - Settler - Probe - Protection (BLUE) (1): 46985
* Avatus - Settler - Probe - Debuff (1): 46996; castMs [160]
* Avatus - Settler - Tower Fluff (1): 47001
* Avatus - Settler - Supply Probe - Tower Part (Shield) (1): 47005
* Avatus - Settler - Supply Probe - Tower Part (Attack) (1): 47007
* Avatus - Settler - Attack Tower - Neutralize (1): 47027; castMs [10000]
* Avatus - Settler - Static Electricity (1): 47031; castMs [2500]
* Avatus - Settler - Drag Signal (1): 47033; castMs [3200]
* Avatus - Settler - Drag Signal - Infinite Proxy (1): 47034
* Avatus - Settler - Static Electricity - AE Proxy (1): 47052
* Avatus - Settler - Probe - Protection (RED) (1): 47060
* Avatus - Settler - Probe - Protection (YELLOW) (1): 47061
* Danger Room - Ability Restricted (1): 47080
* Avatus - Settler - Static Electricity - Base n (1): 47083
* Avatus - Settler - Static Charge (1): 47121
* Avatus - Data Storm - Summon - Proxy (1): 47131
* Avatus - Settler - Remove Static Electricity (1): 47147
* Avatus - Holo Cannon - Beam Signal (1): 47182
* Avatus - EMP - Vulnerable - Proxy (1): 47185; castMs [3000]
* Avatus - Clone Name Change - Soldier Phase (8): 47199 47201 47203 47204 47205 47206 ...; castMs [1, 2000]
* Avatus - Holo Hand - Phase Cross - Danger Room - Target Selection (2): 47213 47214
* Avatus - Holo Hand - Phase Cross - Danger Room (1): 47216
* Avatus - Holo Hand - Phase Cross - Danger Room - Field (1): 47217
* Avatus - Convergent Force - Base n (1): 47260
* Avatus - Convergent Force - Ring 0 - Arc 2 (1): 47269
* Avatus - Convergent Force - Ring 0 - Arc 3 (2): 47270 47271; castMs [2000]
* Avatus - Convergent Force - Ring 0 - Arc 4 (1): 47272
* Avatus - Auto-Attack #2 - L2R - Base n (1): 47283
* Avatus - Caretaker - Permanent Delete - EMP - Wipe (1): 47356; castMs [3000]
* Avatus - Convergent Force - Center Sphere 01 (1): 50094
* Avatus - Convergent Force - Center Sphere 02 (1): 50095
* Avatus - Convergent Force - Center Sphere 03 (1): 50096
* Avatus - Convergent Force - Center Sphere 04 (1): 50097
* Avatus - Convergent Force - Center Sphere 05 (1): 50098
* Avatus - Convergent Force - Center Sphere 06 (1): 50099; castMs [1000]
* Avatus - Convergent Force - Center Sphere 07 (1): 50100
* Avatus - Convergent Force - Center Sphere 08 (1): 50103
* Avatus - Convergent Force - Center Sphere 09 (1): 50104
* Avatus - Convergent Force - Center Sphere 10 (1): 50105
* Avatus - Convergent Force - Center Sphere 00 (1): 50109
* Avatus - Knockdown - Fluff (2): 50163 70162
* Avatus - Sky - Datascape_Avatus_Engaged (1): 64880
* Avatus - Sky - Datascape_Avatus_Dangerroom (1): 64882
* Avatus - Sky - Datascape_Avatus_Showdown (1): 64886; castMs [200]
* Avatus - Sky - Datascape_Avatus_Soldier (1): 64887
* Avatus - Sky - Datascape_Avatus_Scientist (1): 64888; castMs [3000]
* Avatus - Sky - Datascape_Avatus_Explorer (1): 64889; castMs [1000]
* Avatus - Sky - Datascape_Avatus_Settler (1): 64890
* Avatus - Holo Hand - Auto-Attack 2 - R2L - Smack (1): 64939
* Avatus - Holo Hand - Crushing Blow - GTAE Proxy (2): 64958 64959
* Avatus - Unstable Driver - Destruction (1): 69617
* Avatus - Phase Split (1): 69873
* Avatus - Holo Cannon - Absorption Shield (1): 69874; castMs [1000]
* Avatus - Holo Cannon - Moment of Opportunity (1): 69875
* Avatus - Holo Cannon - Interrupted (1): 69880
* Avatus - Phase Punch 2 - Targeted AOE - Base (1): 69926; castMs [2500]
* Avatus - Phase Punch 2 - Target Selection (1): 69927; castMs [5750]
* Avatus - Phase Punch Immunity (1): 69928
* Avatus - Engineer Clone - Artillery Bot Barrage (3): 70044 70045 70054
* Avatus - Engineer Clone - Artillery Bot Barrage - GTAE Proxy (2): 70046 70055
* Avatus - Engineer Clone - Artillery Bot Barrage - Missile Proxy (4): 70049 70050 70058 70059
* Avatus - Fragments - Initialization (4): 70052 70053 70061 70062
* Avatus - Fragments - Initialization - Add Hate (1): 70063
* Avatus - Fragments - Initialization - Wipe Signal (1): 70066
* Avatus - Engineer Clone - Eldan Bombardment Proxy (1): 70076; castMs [12000]
* Avatus - Engineer Clone - Eldan Bombardment Target Selection (2): 70078 70079
* Avatus - Engineer Clone - Artillery Bot Auto Attack (1): 70086; castMs [1250]
* Avatus - Conflict Point - Deflection Beam Target - Overload Proxy (1): 70224
* Avatus - Holo Hand - United (1): 75090; castMs [3000]
* Danger Room - Fragment Disguise - Proxy (2): 75131 75132
* Avatus - Auto-Attack - Holo-Swipe - Pet Cleave Proxy - R2L (1): 75172
* Avatus - Auto-Attack - Holo-Swipe - Pet Cleave Proxy - L2R (1): 75174
* Avatus - Holo Hand -  MoO - 5s (1): 79468
* Avatus - Blue Matrix Buff Consequence - Scientist (1): 79525
* Avatus - Gun Grid Quick Beam - Target Selection (1): 80267; castMs [500]

### e1888 (Raid/Datascape: DefeatTheAbstractAugmentationAlgorithm), 6 rows
* Miniboss Auto Attack Beam - Escher Room (2): 58681 58682
* Miniboss Interrupt Proc - Escher Room (1): 58688
* Processor Damage Aura - Escher Room (2): 61249 61283; castMs [600, 3750]
* Processor Amplilfication - Escher Room (1): 61275; castMs [875]

### e2678 (Dungeon/UltimateProtogames: SneakThroughThePrototentiary), 2 rows
* Sneaky Prison - Spotlight - Base (1): 72153
* Sneaky Prison - Disable Alarm - Base (1): 72270

## [RMT]
### e4550 (Raid/RedMoonTerror: DefeatTheEngineers), 70 rows
* Engineers - Gun - Blast - Summon Creature (1): 75290; castMs [7433]
* Engineers - Gun - Blast - Damage Aura (1): 75291
* Engineers - Hammer - Fusion Armor (1): 75298; castMs [1500]
* Engineers - Gun - Blast - Base (1): 75313; castMs [600]
* Engineers - Hammer - Crush - AA 1 (1): 75389
* Engineers - Hammer - Crush - AA 2 (1): 75390
* Engineers - Shared - Rocket Jump (1): 83875
* Engineers - Vent - Air (1): 83882
* Engineers - Gun - Blast - Boss Detect Aura (1): 83886; castMs [2000]
* Engineers - Pulse - Base (1): 83891; castMs [2000]
* Engineers - Gun - Electroshock - Damage Proxy (1): 83892; castMs [2000]
* Engineers - Gun - Electroshock - Debuff (1): 83893; castMs [1000]
* Engineers - Gun - Electroshock - Target Selection (4): 83894 83896 86757 86758; castMs [300, 4000]
* Engineers - Rev Up - Boss Cast (1): 83898; castMs [3000]
* Engineers - Rev Up - Turbine Cast (1): 83902
* Engineers - Hammer - Liquidate Knockback (1): 83904; castMs [1500]
* Engineers - Gun - Primer (1): 83906
* Engineers - Hammer - Liquidate (1): 83907; castMs [3000]
* Engineers - Gun - Liquidate (1): 83908
* Engineers - Hammer - Fastening Glue (1): 83911; castMs [4000]
* Engineers - Engine - Revving - Base (1): 83992; castMs [1500]
* Engineers - Engine - Exhaust - PBAE Proxy (1): 83993
* Engineers - Gun - Overdrive (1): 84013
* Engineers - Hammer - Overdrive (1): 84014
* Engineers - Engine - Plasma Beam - Aura (1): 84021
* Engineers - Engine - Insulation (1): 84054
* Engineers - Fastening Hook - Find Target (2): 84070 84071; castMs [1500, 3000]
* Engineers - Backlash (1): 84072
* Engineers - Jumpstart - Base (1): 84073; castMs [333]
* Engineers - Jumpstart - On Hit Buff (1): 84074
* Engineers - Jumpstart - Knockback (1): 84078
* Engineers - Fastening Chord - Base (1): 84089
* Engineers - Cooling Turbine - Air Current - Base (1): 84094
* Engineers - Cooling Turbine - Air Current - Target Proxy (2): 84095 84096
* Engineers - Cooling Turbine - Air Current - Aura (1): 84097
* Engineers - Spark Plug - Ignition Spark - Base (2): 84115 84116; castMs [2000]
* Engineers - Spark Plug - Ignition Spark - Target Proxy (2): 84118 84119; castMs [3000]
* Engineers - Spark Plug - Ignition Spark - Chain Proxy (3): 84120 84121 84122
* Engineers - Spark Plug - Rampant Voltage (1): 84124
* Engineers - Spark Plug - Juiced - Normal (1): 84126
* Engineers - Spark Plug - Juiced - Hard (2): 84131 84197
* Engineers - Fusion Core - Ion Clash - Targeting - All (2): 84134 84135; castMs [2000]
* Engineers - Fusion Core - Ion Clash - Missile Proxy (1): 84136; castMs [1500]
* Engineers - Fusion Core - Atomic Attraction - Targeting (1): 84137
* Engineers - Fusion Core - Atomic Attraction - Debuff (2): 84150 84151
* Engineers - Fusion Core - Neutron Rupture (1): 84152
* Engineers - Fusion Core - Plasma - Aura (1): 84169; castMs [5000]
* Engineers - Lubrication Nozzle - Aura (1): 84170
* Engineers - Spark Plug - Base (1): 84173; castMs [300]
* Engineers - Fusion Core - Base (1): 84174; castMs [2000]
* Engineers - Lubrication Nozzle - Base (1): 84175; castMs [2000]
* Engineers - Gun - Electroshock - Tracking Proxy (1): 84211; castMs [5000]
* Engineers - Fusion Core - Ion Clash - TAE Damage (1): 84212; castMs [5000]
* Engineers - Shared - Supercharger (1): 84223
* Engineers - Tank Fluff (1): 86767
* Engineers - Fusion Core - Stacking Scale Down (1): 87298; castMs [5000]
* Engineers - Platform Rotation Buff (1): 87572; castMs [2000]
* Engineers - Lubricant Nozzle Target Selection (1): 87577

### e4556 (Raid/RedMoonTerror: DefeatStarEaterTheVoracious), 89 rows
* Bosun Octog - Tether Bomb - Base - BEAX (2): 75399 75400
* Bosun Octog - Tether Bomb - Explode - BEAX (1): 75401
* Bosun Octog - Tether Bomb - Proxy - BEAX (1): 75402
* Bosun Octog - Bomb - Detonation - Base - BEAX (2): 77806 77807
* Bosun Octog - Bomb - Detonation - Damage - BEAX (1): 77846; castMs [6000]
* Bosun Octog - Auto-Attack #1 - BEAX (1): 85486
* Bosun Octog - Auto-Attack #2 - BEAX (1): 85487
* Jetpack - Boost - BEAX (2): 85505 86880
* Jetpack - Actionbar - BEAX (1): 85506
* Jetpack - Remove - BEAX (1): 85507
* Jetpack - Ground Slam - BEAX (1): 85509
* Jetpack - Ground Slam - Remove Slam - BEAX (1): 85510
* Jetpack - Ground Slam - AoE - BEAX (1): 85513
* Bosun Octog - Auto-Attack - Debuff (1): 85526
* Bosun Octog - Shred - BEAX (1): 85528; castMs [500]
* Bosun Octog - Hookshot - Pull - BEAX (1): 85530
* Bosun Octog - Hold - BEAX (2): 85531 85532
* Bosun Octog - Noxious Ink - Base (1): 85605
* Bosun Octog - Noxious Ink - Aura - Base (1): 85607
* Bosun Octog - Summon Squirgling (1): 85611
* Bosun Octog - Noxious Ink - Heal Debuff (2): 85613 85614
* Bosun Octog - Flamethrower - Aura (1): 85620; castMs [8033]
* Bosun Octog - Flaming Ink Pool - Base (1): 85621
* Bosun Octog - Chaos Orbs - Target Selection (6): 85653 86958 86959 86960 86962 86963; castMs [1800, 5000, 12000]
* Bosun Octog - Chaos Orb - Missile / Summon (1): 85654
* Bosun Octog - Chaos Orb - Initial Damage (2): 85655 85656
* Bosun Octog - Chaos Orb - Debuff (1): 85657
* Bosun Octog - Chaos Tether - Tether (1): 85658
* Bosun Octog - Chaos Tether - Kill (1): 85660; castMs [3000]
* Bosun Octog - Chaos Orb - Detonation (1): 85661
* Bosun Octog - Chaos Shield - Base (1): 85664
* Bosun Octog - Summon Squirgling - Mass (1): 85670
* Bosun Octog - Summon Squirgling - Periodic (4): 85672 86771 86772 86773
* Bosun Octog - Astral Shard - Aura (2): 85684 85697; castMs [2000]
* Bosun Octog - Astral Shard - Buff (2): 85685 85698
* Bosun Octog - Supernova - Base (2): 85713 86770; castMs [1500]
* Bosun Octog - Devastation - Astral Shield - Stacking Buff (1): 85714
* Bosun Octog - Devastation - Astral Shield Removal (1): 85724
* Bosun Octog - Chaos Shield - Removal (1): 85725; castMs [1500]
* Bosun Octog - Devastation - Astral Shield - Immunity (1): 85745; castMs [1500]
* Bosun Octog - Inksplosion (2): 86768 86775
* Bosun Octog - Noxious Ink - Aura - Scaling (13): 86860 86861 86862 86864 86866 86867 ...; castMs [1000, 15000]
* Bosun Octog - Squirgling Smasher Buff (1): 86878
* Bosun Octog - Squirgling - Auto-Attack (1): 86896; castMs [1000]
* Bosun Octog - Flamethrower - Set Interrupt Armor (2): 86930 86932
* Bosun Octog - Flamethrower - Remove Interrupt Armor (1): 86934
* Bosun Octog - Flamethrower - Buff (1): 86943; castMs [1500]
* Bosun Octog - Flamethrower - MoO (1): 86944
* Bosun Octog - Flamethrower - Base (1): 86947
* Bosun Octog - Flamethrower - Remove (1): 86948; castMs [600]
* Bosun Octog - Fireball (1): 86949; castMs [600]
* Bosun Octog - Chaos Orbs - Base (1): 86957; castMs [5000]
* Bosun Octog - Chaos Orbs - Buff (1): 86964
* Bosun Octog - Chaos Orbs - NPC Delay (1): 86974; castMs [1500]
* Bosun Octog - Chaos Orb - Kill Stacks (1): 87154
* Bosun Octog - Flamethrower - Lockout (1): 87165
* Jetpack - Super Boost - BEAX (1): 87168
* Bosun Octog - Flamethrower DOT (1): 87230

### e4558 (Raid/RedMoonTerror: DefeatSwabbieSkiLi), 61 rows
* Shredder - Floating - Lift Up (1): 77805
* Shredder - Shredder Pull to Fan (3): 77808 77809 77820
* Shredder - Zombie - Base (1): 77917
* Shredder - Zombie - Disabled State (1): 77918
* Shredder - Death (1): 77925; castMs [3000]
* Shredder - Boss - Poke 2 - Proxy Cast (2): 84022 84023
* Shredder - Boss - Poke 3 - GTAE (1): 84033
* Shredder - Oghra - Ooze Dmg Aura (2): 84404 84405; castMs [1500]
* Shredder - Oghra - Pull (1): 85411; castMs [466]
* Shredder - Grund - Stacking Debuff (1): 85412
* Shredder - Grund - Auto Attack 1 (1): 85414
* Shredder - Grund - Auto Attack 2 (1): 85415
* Shredder - Grund - Projectile Target Selection (1): 85416
* Shredder - Grund - Projectile Proxy (1): 85417; castMs [1500]
* Shredder - Grund - Necrotic Napalm AOE (1): 85418
* Shredder - Grund - Necrotic Detonation Raid Dmg (2): 85460 85461
* Shredder - Grund - Necrotic Detonation Tank Dmg (1): 85527
* Shredder - Razortail - Pounce - Base (1): 85548
* Shredder - Razortail - Pounce Target Selector (1): 85562
* Shredder - Vector Slide Shredder Intake (1): 85566
* Shredder - Boss - Zombie Proximity Checker (1): 85567
* Shredder - Shredder Intake Phase Transition AOE (1): 85633; castMs [1500]
* Shredder - Add Boss Immunities (1): 85642; castMs [25000]
* Shredder - Boss - Poke 1 - Target Selection Back (2): 85681 85682
* Shredder - Boss - Poke 1- BASE TARGET SELECTION (1): 85683
* Shredder - Boss - Poke 1 - Target Selection Failsafe on Miss All (2): 85686 85687
* Shredder - Boss - Knock Back AOE - Players (1): 85718
* Shredder - Zombies - Proximity and Saw Buff (1): 86764; castMs [2000]
* Shredder - Grund - Oozing Bile Proc (1): 86795; castMs [2000]
* Shredder - Grund - Invisible Tank Proc Protection (1): 86796
* Shredder - Boss - Junk Trap Nearby Target Selection (1): 86804
* Shredder - Boss - Junk Trap Line AE (1): 86805
* Shredder - Boss - Tether Mine Individual Tether Targeting (2): 86811 86812; castMs [1000]
* Shredder - Boss - Tether Mine Individual Tether (1): 86817
* Shredder - Grund Champion - Auto Attack 1 (1): 86848
* Shredder - Grund Champion - Auto Attack 2 (1): 86850; castMs [5000]
* Shredder - Grund Champion - Stun AOE (1): 86852
* Shredder - Grund Champion - Knockdown AOE (1): 86853
* Shredder - Boss - Phase Finish Runspeed Boost (1): 86927
* Shredder - Boss - Raid Wipe (1): 86929; castMs [3000]
* Shredder - Razortail - Pounce Channel (1): 86952
* Shredder - Boss - Knock Back AOE - Damage (1): 86973
* Shredder - Infinite Snare Test (1): 87019
* Shredder - Zombie - Zombie Rise Again (1): 87022
* Shredder - Boss - Intro Leap - Swabbie Swoop (1): 87026
* Shredder - Grund Champion 2 Display Name Spell (2): 87028 87029; castMs [10000]
* Shredder - Oghra - Auto Attack L2R (1): 87194
* Shredder - Oghra - Auto-Attack R2L (1): 87195
* Shredder - Razortail - Auto Attack D2U (1): 87200
* Shredder - Razortail - Auto-Attack R2L (1): 87201
* Engineers - Lubricant Mechanic (1): 87578
* Shredder - Oghra - Pull Target Selector (1): 87608

## [IC]
### e2681 (Raid/InitializationCoreY83: UnlockTheQuarantineDoor), 79 rows
* Augmentors - Strain Injection - Base (3): 48692 71560 75218
* Augmentors - Chest Burst - Base (1): 48705
* Augmentors - Augmentation Beam - Base (1): 48792
* Augmentors - Corrupt Augmenter - Base (1): 48820
* Augmentors - Strain Incubation - Base (2): 49401 77745
* Augmentors - Find Nearest Augmenter (1): 49983; castMs [500]
* Augmentors - Nanostrain Infusion - Base (5): 50164 50165 70163 70164 80554; castMs [500]
* Augmentors - Nano Bite - Base (3): 50167 50168 70165; castMs [1333]
* Augmentors - Radiation Protect Tank Fluff (1): 71236
* Augmentors - Radiation Bath Target Select (2): 71237 75533
* Augmentors - Radiation Bath - Base (1): 71243
* Augmentors - Radiation Aura - Damage Field 1 (1): 71245
* Augmentors - Radiation Aura - Removal Field (2): 71270 71271
* Augmentors - Circuit Corruption (1): 71344
* Augmentors - Augmentation Beam - Circuit Proxy (2): 71354 71355; castMs [500]
* Augmentors - Strain Injection - AE Proxy (1): 71356; castMs [12000]
* Augmentors - Corruption Spike - Select Target (3): 71357 75489 75490
* Augmentors - Corruption Spike - Base (1): 71358
* Augmentors - Corruption Spike - Circuit Proxy (1): 71359
* Augmentors - Fallout - Transmission Proxies (6): 71423 71424 71430 71432 71441 75251; castMs [1500, 1600]
* Augmentors - Fallout AoE Proxy (2): 71449 71450
* Augmentors - Transmission (2): 71451 75431; castMs [2750]
* Augmentors - Ruptured Insides - Base (1): 71540
* Augmentors - Transmission - Group Pull Proxy (1): 71577
* Augmentors - Transmission - Pull (1): 71579
* Augmentors - Fallout - Persistent Proxies (6): 75252 75253 75254 75255 75256 75257
* Augmentors - Fallout - Spin Proxy (5): 75258 75259 75261 75262 75263; castMs [5000]
* Augmentors - Fallout - Cast Base (1): 75264
* Augmentors - Fallout Removal (1): 75269
* Augmentors - Laser - Interrupt Proc (1): 75330; castMs [500]
* Augmentors - Laser - Beam Cast (1): 75332
* Augmentors - Laser - Beam Aura (2): 75333 81577; castMs [4100]
* Augmentors - Laser - Spin (1): 75369; castMs [2000]
* Augmentors - Spread - AE Proxy (2): 75424 75677
* Augmentors - Spread - Kill Proxy (1): 75430
* Augmentors - Open Quarantine Door (1): 75637
* Augmentors - OOC Find Circuit (1): 75667
* Augmentors - Spread - Retrigger Spread (1): 75670
* Augmentors - Laser - Surge (2): 79981 81898; castMs [1000]
* Augmentors - Reinforcement (1): 79984
* Augmentors - System Reboot (2): 80481 80482
* Augmentors - Fallout Script Trigger - DNT (1): 81714
* Augmentors - Fallout TEMP - DNT (1): 81893; castMs [5000]
* Augmentors - Fallout - Proxy Spokes - DNT (2): 82938 82939

## [UP]
### e2673 (Dungeon/UltimateProtogames: ClearTheLostAndFound), 101 rows
* Crate Destruction - Volatile Goodies - Big Crate - Base (2): 72369 72370
* Crate Destruction - Volatile Goodies - Crate Opened - Normal (2): 72374 72375; castMs [500, 750]
* Crate Destruction - Poison Cloud - Base (2): 72379 72380
* Crate Destruction - Super Buff - Base (1): 72390
* Crate Destruction - Damage Buff - Base (1): 72391
* Crate Destruction - Healing Buff - Base (1): 72392
* Crate Destruction - Speed Buff - Base (1): 72393
* Crate Destruction - Defense Buff - Base (1): 72394; castMs [2250]
* Crate Destruction - Thunderstorm - Base (2): 72396 72397; castMs [750]
* Crate Destruction - Thunderstorm - Lightning Strike Proxy (1): 72398
* Crate Destruction - Good Luck - Base (1): 72399; castMs [2000]
* Crate Destruction - Bad Luck - Base (1): 72400; castMs [1599]
* Crate Destruction - Volatile Goodies - Hit Proxy - Explosive (1): 72404
* Crate Destruction - Volatile Goodies - Big Crate - Damage Self (2): 72405 72406; castMs [1000]
* Crate Destruction - Volatile Goodies - Timed Explosive - Base (7): 72407 73039 73040 73041 73052 73054 ...; castMs [2000, 6150]
* Crate Destruction - Blast Wave - Base (1): 72408
* Crate Destruction - Sickness - Base (1): 72409; castMs [400]
* Crate Destruction - Sickness - Disorient Proxy (1): 72411
* Crate Destruction - Sickness - Blind Proxy (1): 72412; castMs [2200]
* Crate Destruction - Sickness - Subdue Proxy (1): 72413
* Crate Destruction - Sickness - Knockdown Proxy (1): 72414
* Crate Destruction - Sickness - Stun Proxy (1): 72415; castMs [750]
* Crate Destruction - Polymorph - Base (1): 72431
* Crate Destruction - Ram - Polymorph - Base (1): 72433
* Crate Destruction - Energy Storm - Base (2): 72441 72442
* Crate Destruction - Explosion - Base (1): 72443
* Crate Destruction - Time Warp - Base (1): 72445
* Crate Destruction - Suit Up - Base (1): 72446
* Crate Destruction - Blast - Base (1): 72447
* Crate Destruction - Bouncing Bomb - Base (1): 72450
* Crate Destruction - Bouncing Bomb - Knockback Proxy (1): 72451
* Crate Destruction - Detonate - Base (1): 72453
* Crate Destruction - Swipe - Auto-Attack - Base (3): 72857 77314 77315
* Crate Destruction - Swing - Auto-Attack - Base (2): 72858 77316
* Crate Destruction - Pounce - Base (3): 72859 73074 73076
* Crate Destruction - Pinch - Auto-Attack #1 - Base (1): 72880
* Crate Destruction - Snap - Auto-Attack #2 - Base (1): 72881
* Crate Destruction - Earthquake - Base (1): 72882
* Crate Destruction - Toxic Lance - Base (1): 72883
* Crate Destruction - Burrowed Pursuit - Base (2): 72884 72885
* Crate Destruction - Burrowed Pursuit - Channel (1): 72886
* Crate Destruction - Entomb - Base (1): 72887
* Crate Destruction - Piercing Posion - Base (4): 72888 74765 74766 74767
* Crate Destruction - Burrowed Pursuit - Activate (Pull) (1): 72892
* Crate Destruction - Burrowed Pursuit - CSI (1): 72893
* Crate Destruction - Burrowed Pursuit - CSI Failure (1): 72894
* Crate Destruction - Earthquake - Fluff Counter (1): 72895
* Crate Destruction - Earthquake - Remove Fluff Counter (2): 72897 72898; castMs [2000]
* Crate Destruction - Burrowed Pursuit - Remove Base (1): 72904
* Crate Destruction - Entomb - CC Proxy (1): 72912
* Crate Destruction - Entomb - Despawn Proxy (1): 72917
* Crate Destruction - Beam - Base (2): 73090 73091
* Crate Destruction - Wrath of the Crate God - Base (1): 74761; castMs [483]
* Crate Destruction - Ring Out - PBAE (1): 75370
* Crate Destruction - Piercing Poison - Buff Stacks (1): 75386
* Crate Destruction - Shield - Base (1): 75388
* Crate Destruction - Crush - Base (1): 75396
* Crate Destruction - Burrowed Pursuit - Remove Marks (2): 76843 76844
* Crate Destruction - Burrowed Pursuit - Move Towards Target (1): 76845; castMs [5000]
* Crate Destruction - Burrowed Pursuit - Damage (1): 76847
* Crate Destruction - Bash - Base (1): 76871
* Crate Destruction - Report in Triplicate! - Base (2): 76872 76873
* Crate Destruction - MoO - With Points (4): 77117 77118 77119 77120
* Crate Destruction - Earthquake - Damage and CC Proxy (2): 77325 77326
* Crate Destruction - Shield - Hold Proxy (1): 77356
* Crate Destruction - Shield - Remove Channel (2): 77357 77358; castMs [1500]
* Crate Destruction - Energy Storm - Damage Proxy (1): 77553; castMs [12000]
* Crate Destruction - Medicine - Base (1): 77573
* Crate Destruction - Medicine - Debuff Removal and Despawn (1): 77574
* Crate Destruction - Medicine - Knock Random (2): 77575 77576

### e2674 (Dungeon/UltimateProtogames: MasterTheElements), 98 rows
* Beacon Aura - Dropped - Default (1): 72091; castMs [2000]
* Player Aura - Carried (1): 72093; castMs [30000]
* Creature Buff - Fire Buff (1): 72097; castMs [30000]
* Creature Buff - Water Buff (1): 72098; castMs [30000]
* Creature Buff - Air Buff (2): 72099 72100; castMs [30000]
* Creature Buff - Earth Buff (1): 72101
* Player Aura - Fire Aura Debuff (1): 72103
* Player Aura - Water Aura Debuff (1): 72177
* Player Aura - Air Aura Debuff (1): 72178
* Player Aura - Earth Aura Debuff (1): 72179; castMs [5000]
* Stationary Beacon Aura - Water Beacon Aura (1): 72225
* Stationary Beacon Aura - Air Beacon Aura (1): 72226
* Stationary Beacon Aura - Earth Beacon Aura (1): 72227
* Room Hazard - Damage (1): 72305
* Beacon Aura - Activate Spell - Default (1): 72314; castMs [2000]
* Beacon Aura - Dropped - Water (1): 72381
* Beacon Aura - Dropped - Earth (1): 72382; castMs [10000]
* Beacon Aura - Dropped - Air (1): 72383
* Beacon Aura - Activate Spell - Fire (1): 72384
* Beacon Aura - Activate Spell - Water (1): 72385
* Beacon Aura - Activate Spell - Earth (1): 72387
* Fire Wave - Stumpkin - Auto-Attack #1 (2): 73013 73014
* Fire Wave - Stumpkin - Auto-Attack #2 (2): 73015 73016; castMs [300]
* Fire Wave - Stumpkin - Telegraph #1 (1): 73017; castMs [1500]
* Fire Wave - Stumpkin - Telegraph #2 (1): 73019; castMs [1500]
* Fire Wave - Dracus - Auto-Attack #1 (2): 73022 73023; castMs [3500]
* Fire Wave - Dracus - Auto-Attack #2 (1): 73024
* Fire Wave - Dracus - Telegraph #1 (1): 73025
* Fire Wave - Dracus - Telegraph #2 (1): 73026
* Water Wave - Spikehorde - Telegraph #1 (2): 73027 73028
* Water Wave - Spikehorde - Telegraph #2 (1): 73029
* Water Wave - Dagun - Auto-Attack #1 (1): 73031; castMs [4000]
* Water Wave - Dagun - Auto-Attack #2 (1): 73032
* Water Wave - Dagun - Telegraph #1 (1): 73033
* Water Wave - Dagun - Telegraph #2 (1): 73034
* Water Wave - Dagun - Telegraph #3 (1): 73035
* Water Wave - Dagun - Telegraph #4 (1): 73036
* Earth Wave - Rockhorde - Auto-Attack #1 (1): 73042
* Earth Wave - Rockhorde - Auto-Attack #2 (1): 73043
* Earth Wave - Rockhorde - Telegraph #1 (1): 73044
* Earth Wave - Rockhorde - Telegraph #2 (1): 73045
* Earth Wave - Spider - Auto-Attack #1 (1): 73046
* Earth Wave - Spider - Auto-Attack #2 (1): 73047
* Earth Wave - Spider - Telegraph #1 (1): 73048
* Earth Wave - Spider - Telegraph #2 (1): 73049
* Earth Wave - Spider - Telegraph #3 (1): 73050
* Air Wave - Air Elemental - Auto-Attack #1 (1): 73051
* Air Wave - Air Elemental - Auto-Attack #2 (1): 73056
* Air Wave - Air Elemental - Telegraph #1 (1): 73057
* Air Wave - Air Elemental - Telegraph #1 (proxy) (1): 73058
* Air Wave - Air Elemental - Telegraph #2 (1): 73059
* Air Wave - Air Elemental - Telegraph #3 (1): 73060
* Air Wave - Air Elemental - Telegraph #4 (2): 73061 73062
* Air Wave - Air Elemental - Telegraph #4 (proxy) (1): 73063
* Air Wave - Vulcarrion - Auto-Attack #1 (1): 73065
* Air Wave - Vulcarrion - Telegraph #1 (1): 73067; castMs [4750]
* Air Wave - Vulcarrion - Telegraph #2 (1): 73068; castMs [2000]
* Water Wave - Spikehorde - Telegraph #2 (proxy) (1): 73069
* Creature - Wave Vulnerability (1): 75924; castMs [3000]
* Creature - Super Buff (1): 75946
* Creature Buff - Absorb Proxy (2): 75959 76894; castMs [500]
* Creature Buff - Prereq Fluff (1): 75960
* Mini Boss - All Forms - Telegraph #1 (6): 75967 76024 76025 76026 76027 76839; castMs [500]
* Mini Boss - Disguise Fire Elemental (1): 75981
* Mini Boss - Disguise Water Elemental (1): 75982
* Mini Boss - Disguise Air Elemental (1): 75983
* Mini Boss - Disguise Earth Elemental (3): 75984 75985 75986
* Mini Boss - Fire - Telegraph #1 (1): 75993
* Mini Boss - Fire - Telegraph #2 (2): 75994 75996
* Mini Boss - Air - Telegraph #2 (1): 75997
* Mini Boss - Water - Telegraph #2 (1): 75999
* Mini Boss - Earth - Telegraph #1 (1): 76001
* Mini Boss - Earth- Telegraph #2 (1): 76002; castMs [1000]
* Fire Wave - Stumpkin - Telegraph #2 (Field) (1): 76130
* Air Wave - Vulcarrion - Telegraph #2 (Field) (1): 76132
* Mini Boss - Melee - Auto-Attack #1 (1): 76286
* Earth Wave - Elemental Biped - Unarmed - Auto-Attack #2 (1): 77427
* Earth Wave - Elemental Biped - Unarmed - Auto-Attack #1 (1): 77428
* Creature - Vulnerability - Self Buff (3): 78059 78060 83525
* Creature - Super Buff - Self Buff (1): 78061
* Creature Buff - Prereq Fluff 2 (1): 78063

### e2675 (Dungeon/UltimateProtogames: DefeatHutHut), 49 rows
* Hut-Hut - Auto-Attack #1 L2R (1): 71201; castMs [1500]
* Hut-Hut - Auto-Attack #2 R2L (1): 71202; castMs [1500]
* Hut-Hut - Line Charge Proxy (2): 71342 71343
* Hut-Hut - Football - Throw Ball (1): 71447
* Hut-Hut - Football - Move Ball (1): 71448
* Hut-Hut - Football -  Remove Action Bar (1): 71490
* Hut-Hut - Football - Give Action Set (2): 71491 71546
* Hut-Hut - Football - Aura (1): 71541
* Hut-Hut - Touchdown Vulnerability (1): 71728
* Hut-Hut - Celebration Dance (3): 71816 71830 71840
* Hut-Hut - Debuff - Celebration Dance (3): 71817 71831 71841
* Hut-Hut - Football -  Drop Football (1): 71965; castMs [2200]
* Hut-Hut - Invis Unit Aura (1): 71998; castMs [1000]
* Hut-Hut - Self Destruct - Slow (1): 72149
* Hut-Hut - Absorb Shield (3): 72782 78123 78124
* Hut-Hut - Going All The Way - Absorb Shield - Stun (1): 72784
* Hut-Hut - Team Member - Vulnerability (1): 72790
* Hut-Hut - Team Member Drop Ball Signal (1): 72791
* Hut-Hut - Fumble Random (1): 72825
* Hut-Hut - Special Teams - Force Fumble Aura (1): 72837
* Hut-Hut - Self Destruct - Fast (1): 72844
* Hut-Hut - Special Teams Member - Vulnerability (1): 75180; castMs [5000]
* Lineman - Auto-Attack #1 L2R (1): 75749
* Lineman - Auto-Attack #2 R2L (2): 75750 79635; castMs [2000]
* Lineman - Health Threshold Debuff (2): 75752 75753
* Lineman - Health Threshold Debuff - Snare Root Heal (8): 75756 75757 75759 75761 75762 75763 ...
* Hut-Hut - Extra Point Pull To Location Spell (1): 75801
* Lineman - Telegraph #1 (1): 75822; castMs [11500]
* Hut-Hut - Teleport Players - Fluff (1): 75880
* Hut-Hut - Football - Carried Fluff (1): 75881
* Hut-Hut - Absorb Fluff (1): 78125
* Hut-Hut - Generic Infinite Invulnerability (1): 78716

### e2676 (Dungeon/UltimateProtogames: DestructODerby), 20 rows
* Tank Destruction - Visual Fluff (Proxy) (1): 72335; castMs [166]
* Tank Destruction - Explosion (1): 72337; castMs [1750]
* Turret - Line Attack (Proxy) (1): 72683; castMs [4000]
* Turret - Line Attack (Base) (1): 72686
* Incinerate - Debuff (1): 72700
* Turret - Line Attack (Field) (1): 73159; castMs [3000]
* Tank Destruction - Visual Fluff (Base) (1): 73228
* Tank - Mine Explosion (Base) (1): 76315
* Tank - PBAE Explosion (proxy 1) (1): 76316
* Tank - Tracking Missiles (Base) (1): 76317
* Tank - PBAE Explosion (Base) (1): 76339
* Tank - Tracking Missiles (P1) (1): 76446
* Tank - Tracking Missiles (P3) (1): 76447
* Tank - Tracking Missiles (P4) (1): 76448
* Tank - Tracking Missiles (P6) (1): 76449
* Tank - Tracking Missiles (Damage) (1): 76450
* Tank - Turret Vulnerability (1): 77382; castMs [4000]
* Tank - MOO Prereq Fluff (1): 77383
* Turret - Combat Heal (1): 77409
* Tank - Discharge (base) (1): 79399

### e2677 (Dungeon/UltimateProtogames: KickMaraudersIntoDeepSpace), 70 rows
* Super Smash - Smash - Base (1): 71790
* Super Smash - Smash - Smash - Combo Step 1 (9): 71792 71793 71794 71795 71797 71798 ...
* Super Smash - Smash Action Bar - Base (1): 71806
* Super Smash - Combo - Base (1): 71871; castMs [1500]
* Super Smash - Combo - Combo Tracker (1): 71872; castMs [1000]
* Super Smash - Combo - Remove Combo (1): 71874
* Super Smash - Combo - Combo Timer (2): 71881 71910
* Super Smash - Smash - Knockout Speed Boost (1): 71925; castMs [500]
* Super Smash - Smash - Knockout Aura (1): 71926; castMs [1000]
* Super Smash - Bombardment - Base (1): 71963
* Super Smash - Bombardment - Damage Proxy (1): 71964
* Super Smash - Drop Pod - Base (1): 71966
* Super Smash - Bombardment - Proxy Random TPAE (1): 71967
* Super Smash - Smash - Distance Cone to Ships (1): 71981
* Super Smash - Smash - Ship Damage and Forced Move (1): 71982
* Super Smash - PBAE Ship Combo Bounce (1): 72000
* Super Smash - Miss All Decrease Combo (1): 72001
* Super Smash - Smash - Smash - Combo Step 2 (1): 72003
* Super Smash - Smash - Smash - Combo Step Max (2): 72004 72005; castMs [2000]
* Super Smash - Smash - Miss All Check (1): 72070
* Super Smash - Smash - Miss All Kill Smashed Unit (1): 72079
* Super Smash - Light Marauder - Right Hook - Auto-Attack - Base (2): 72104 72105
* Super Smash - Light Marauder - Left Jab - Auto-Attack - Base (1): 72552
* Super Smash - Medium Marauder - Shock Pulse - Auto-Attack - Base (2): 72553 72554
* Super Smash - Medium Marauder - Blast Canister - Auto-Attack - Base (2): 72555 72556
* Super Smash - Heavy Marauder - Ripper - Auto-Attack - Base (1): 72557
* Super Smash - Heavy Marauder - Shredder Slice - Auto-Attack - Base (1): 72558; castMs [2000]
* Super Smash - CC Marauder - Blaster Shot - Auto-Attack - Base (1): 72561
* Super Smash - CC Marauder - Zap Charge - Auto-Attack - Base (1): 72562
* Super Smash - Uppercut - Base (1): 72563
* Super Smash - Blast Back - Base (1): 72565
* Super Smash - Explosive Shot - Channel Proxy (1): 72566; castMs [450]
* Super Smash - Explosive Shot - Explosion Proxy (1): 72567; castMs [450]
* Super Smash - Ravager - Base (1): 72568; castMs [450]
* Super Smash - Power Rush - Base (1): 72571; castMs [1200]
* Super Smash - Power Rush - Aura Proxy (1): 72572
* Suepr Smash - Field Capacitor - Base (1): 72573
* Super Smash - Insanity Grenade - Base (1): 72574
* Super Smash - Insanity Grenade - Damage Proxy (2): 72575 72576
* Super Smash - Undercut - Base (1): 72577
* Super Smash - Power Rush - OnInterrupt Proc (2): 73141 73144
* Super Smash - Accelerated Eradication - Base - PO 2952 (1): 73157; castMs [2000]
* Super Smash - Wobbly (Exposed) - Base (1): 73191
* Super Smash - Second Wind - HoT (2): 75867 75868
* Super Smash - Power Rush - Remove Aura (1): 76335
* Super Smash - Power Rush - Knockdown Proxy (1): 76361
* Super Smash - Smash - No Target Knockout Target (1): 76378; castMs [2000]
* Super Smash - Second Wind - Base (1): 76773
* Super Smash - Giant Orange Weakspot - Base (4): 77146 77152 77153 77154
* Super Smash - Explosive Shot - Target Select (1): 77156
* Signal World for Super Smash Vulnerability Check (1): 83352

### e2678 (Dungeon/UltimateProtogames: SneakThroughThePrototentiary), 101 rows
* Sneaky Prison - Detector - Base (1): 72150; castMs [1500]
* Sneaky Prison - Snipe - Damage Proxy (2): 72151 72152; castMs [3000]
* Sneaky Prison - Girrok Trap - Base (1): 72167; castMs [2400]
* Sneaky Prison - Trigger Alarm - Base (1): 72181; castMs [500]
* Sneaky Prison - Spotlight - Tracker (1): 72185
* Sneaky Prison - Girrok Trap - Despawn Caster Proxy (2): 72193 72194
* Sneaky Prison - Gather Scent - Base (1): 72216
* Sneaky Prison - Bash - Auto-Attack - Base (1): 72217
* Sneaky Prison - Gather Clues - Base (1): 72218
* Sneaky Prison - Snipe - Base (1): 72223
* Sneaky Prison - Find Corpses - Base (1): 72224
* Sneaky Prison - Radio Reinforcements - Base (1): 72249
* Sneaky Prison - Check Surroundings - Base (1): 72317; castMs [2000]
* Sneaky Prison - Check Surroundings - Random Rotation Proxy - Left (2): 72318 72319
* Sneaky Prison - Check Surroundings - Random Rotation Proxy - Right (1): 72322
* Sneaky Prison - Divide and Conquer - Base (1): 72605
* Sneaky Prison - Divide and Conquer - Center Damage Proxy (1): 72606; castMs [6150]
* Sneaky Prison - Divide and Conquer - Left Proxy (1): 72607
* Sneaky Prison - Divide and Conquer - Right Proxy (1): 72608; castMs [500]
* Sneaky Prison - Lockdown - Base (1): 72610
* Sneaky Prison - Smoke 'em Out - Base (1): 72611
* Sneaky Prison - Smoke 'em Out - GTAE Proxy (1): 72612
* Sneaky Prison - Spear Shot - Base (1): 72616; castMs [2000]
* Sneaky Prison - Spear Shot - Aura Proxy (1): 72617
* Sneaky Prison - Ranged - Auto-Attack - Electric Shot - Base (2): 72618 72619; castMs [500, 3000]
* Sneaky Prison - Eradication Protocol - Base (1): 72620; castMs [500]
* Sneaky Prison - Terror Capacitor - Base (1): 72633
* Sneaky Prison - Terror Capacitor - DoT and Disorient (1): 72635
* Sneaky Prison - Terror Capacitor - Damage Proxy (1): 72636
* Sneaky Prison - Supercharged Shot - Auto-Attack - Base (1): 72637; castMs [3000]
* Sneaky Prison - Supercharged Shot - Right Damage Proxy (2): 72638 72639; castMs [3000]
* Sneaky Prison - Supercharged Shot - Rear Damage Proxy (1): 72640
* Sneaky Prison - Supercharged Shot - Left Damage Proxy (1): 72641
* Sneaky Prison - Guillotine - Base (1): 72650
* Sneaky Prison - Control the Battlefield - Base (1): 72657; castMs [2000]
* Sneaky Prison - Detain - Base (2): 72658 77421
* Sneaky Prison - Detain - CSI (1): 72659
* Sneaky Prison - Detain - CSI Fail - Detain Player (1): 72660
* Sneaky Prison - Shield Matrix - Channel Base (1): 72661; castMs [2500]
* Sneaky Prison - Control the Battlefield - Target Alive Tracker (1): 72664; castMs [1866]
* Sneaky Prison - Control the Battlefield - Despawn Walls Signal (1): 72665
* Sneaky Prison - Tear - Base (1): 72670
* Sneaky Prison - Shield Matrix - Quadrant Protection Aura (13): 72678 72687 72688 72689 72690 72691 ...
* Sneaky Prison - Detective - Ripper - Auto-Attack - Base (2): 72703 72704; castMs [3000]
* Sneaky Prison - Detective - Shredder Slice - Auto-Attack - Base (1): 72705
* Sneaky Prison - Detain - CSI Success (1): 72793
* Sneaky Prison - Savage Slash - Auto-Attack - Base (2): 72799 72800
* Sneaky Prison - Culling Cut - Auto-Attack - Base (1): 72801
* Sneaky Prison - Hack the Gate - Activate - Start Event (1): 72803; castMs [600]
* Sneaky Prison - Detector -  Sigw (Challenge Fail) (1): 72835; castMs [3150]
* Sneaky Prison - Gnash - Auto-Attack - Base (1): 73221
* Sneaky Prison - Girrok Trap - Despawn (1): 75274
* Sneaky Prison - Overwatch Guard - Shock Pulse - Auto-Attack - Base (2): 75403 75404
* Sneaky Prison - Overwatch Guard - Blast Canister - Auto-Attack - Base (1): 75405
* Sneaky Prison - Snipe - Find Random Target (1): 75972
* Sneaky Prison - Detain - CC (1): 76006; castMs [1000]
* Sneaky Prison - Detain - Find Random Target - Standard (1): 76038
* Sneaky Prison - Snipe - Laser Missile VFX Proxy (1): 76476
* Sneaky Prison - Talk - Base (1): 76487
* Sneaky Prison - High Security Keypad - Base (1): 76489; castMs [2000]
* Sneaky Prison - Hack the Keypad - Base (1): 76491
* Sneaky Prison - Feedback - Base (1): 76492
* Sneaky Prison - Detain - Persist Check (1): 76736; castMs [1250]
* Sneaky Prison - Trigger Alarm - Interrupted Grant Points (2): 77101 77102
* Sneaky Prison - Hack the Cage - Activate - Complete Event (2): 77103 77104
* Sneaky Prison - Snipe - Miss All Points (2): 77105 77106
* Sneaky Prison - Detector - Remove Detector (1): 77114
* Sneaky Prison - Divide and Conquer - Get Target (1): 77253
* Sneaky Prison - Divide and Conquer - Rotate Caster (2): 77254 77255
* Sneaky Prison - Drone Defenses - VFX Targeting (1): 77413
* Sneaky Prison - MoO (1): 77422
* Sneaky Prison - Disable Alarm - Base (1): 78130
* Sneaky Prison - Snipe (1): 79403; castMs [500]
* Sneaky Prison - Spotlight - Gatling Blast (Base) (1): 79405; castMs [8850]
* Sneaky Prison - Spotlight - Gatling Blast (Damage) (2): 79407 79408

### e2680 (Dungeon/UltimateProtogames: DefeatBevORage), 88 rows
* Vending Machine - Straight Spark - Base (1): 71203
* Vending Machine - Spark - Base (1): 71204; castMs [1500]
* Vending Machine - Lazer Spark - Base (1): 71205; castMs [1500]
* Vending Machine - Tri Burst - Base (16): 71264 71283 71285 71286 71287 71289 ...; castMs [3266, 4000]
* Vending Machine - Auto-Attack - Stir - Base (1): 71281
* Vending Machine - Auto-Attack - Shake - Base (1): 71282; castMs [1500]
* Vending Machine - Barrel Roll - Base (1): 71307
* Vending Machine - Pull - Base (1): 71317
* Vending Machine - Barrel Roll - Barrel (1): 71319
* Vending Machine - Pull - Pull to Barrel Start (1): 71323; castMs [1000]
* Vending Machine - Pull - Pull to Giant Start (1): 71324; castMs [1000]
* Vending Machine - Dance Floor - Signal (1): 71325; castMs [500]
* Vending Machine - MoO - Base (2): 71360 71593; castMs [1500]
* Vending Machine - Loose Change - Base (1): 71361; castMs [10000]
* Vending Machine - Knock Random - Coins - Base (1): 71362
* Vending Machine - Coin Pickup Aura - Base (1): 71363
* Vending Machine - Pocketful of Quarters (1): 71364
* Vending Machine - Coin Pickup Aura - Signal and Despawn (1): 71365; castMs [500]
* Vending Machine - Pocketful of Quarters - Buff Removal (1): 71366
* Vending Machine - Protostar Pop - Base (3): 71376 71377 79412
* Vending Machine - Draken Drink - Base (1): 71378
* Vending Machine - Granok Gutcrusher - Base (3): 71379 71380 71381
* Vending Machine - Aurin Amber - Base (3): 71386 71387 79642
* Vending Machine - Chua Celerity - Base (1): 71391; castMs [3500]
* Vending Machine - Drink - MoO - Throw Drink (2): 71393 71394
* Vending Machine - Drink - Drink Pickup Aura - Protostar Pop (2): 71395 71396
* Vending Machine - Drink - Drink Pickup Aura - Draken Drink (2): 71397 71398
* Vending Machine - Drink - Drink Pickup Aura - Granok Gutcrusher (2): 71399 71400
* Vending Machine - Drink - Drink Pickup Aura - Aurin Amber (3): 71401 71402 71403
* Vending Machine - Drink - Drink Pickup Aura - Chua Celerity (2): 71404 71405
* Vending Machine - Drink - Despawn Caster (1): 71422
* Vending Machine - Beverage Barrage - Base (3): 71625 71639 71654
* Vending Machine - Beverage Barrage - Damage TAE (1): 71627; castMs [2000]
* Vending Machine - Acidic Residue - Base (3): 71631 71642 71656
* Vending Machine - Beverage Barrage - Random Target Selection (2): 71729 71744
* Vending Machine - Cheater Cheater - Base (1): 72458; castMs [900]
* Vending Machine - Steam Blast - Base (1): 72459
* Vending Machine - Boost - Base (1): 75092
* Vending Machine - Anti-Theft Protection Shield - Defense Buff (1): 75135
* Vending Machine - Carbonated - PBAE (1): 75186; castMs [3000]
* Vending Machine - Carbonated - Stun (2): 75206 75207
* Vending Machine - Barrel Roll - Death Aura (2): 75243 75244
* Vending Machine - Tri Burst - Miss Check Proxy (1): 76909; castMs [500]
* Vending Machine - Carbonated - Points Signal Proxy (2): 76921 76922
* Vending Machine - Spark - Target Hit Fluff (1): 76924
* Vending Machine - Boost - Shove Player (1): 77008; castMs [3000]
* Vending Machine - Activate - PBAE Points Proxy (1): 77031
* Vending Machine - Carbonated - GTAE Setup Proxy (1): 77041
* Vending Machine - Carbonated Destruction - BEAX (1): 79406; castMs [6750]
* Vending Machine - Carbonated Destruction - DoT - BEAX (1): 79410
* Vending Machine - Carbonated Destruction - MoO - BEAX (1): 79418

## [UPJ]
### e4342 (Dungeon/ProtogamesAcademy: DefeatInvulnotron), 34 rows
* Invulnotron - IA Cycle - Knockdown (Cast) (1): 76166
* Invulnotron - IA Cycle - Knockdown (Effect) (1): 76167; castMs [2000]
* Invulnotron - IA Cycle - Interrupt Armor (Buff) (2): 76169 76170
* Invulnotron - IA Cycle - MOO (1): 76174
* Invulnotron - IA Cycle - Tether (Cast) (2): 76176 76343
* Invulnotron - IA Cycle - Tether (Effect) (2): 76177 76344
* Invulnotron - IA Cycle - Subdue (Cast) (1): 76178
* Invulnotron - IA Cycle - Subdue (Effect) (1): 76179; castMs [3000]
* Invulnotron - IA Cycle - Basic (Cast) (2): 76236 76237
* Invulnotron - IA Cycle - Successful Interrupt (1): 78149
* Invulnotron - IA Cycle - Dispel Buff [DNT] (1): 78150
* Invulnotron - IA Cycle - Proc [DNT] (1): 78154
* Invulnotron - Disintegrate - Base (V) (3): 81631 81957 81958; castMs [2000]
* Invulnotron - Auto Attack #2 (V) (1): 81634
* Invulnotron - Unstable Energy (V) (1): 81771
* Invulnotron - Unstable Energy - Player (V) (1): 81776
* Invulnotron - Shield and Destroy (V) (1): 81781
* Invulnotron - Interrupt Armor (V) (1): 81787; castMs [2000]
* Invulnotron - Disintegrate - Aura Proxy (V) (3): 81812 81828 81829; castMs [1000]
* Invulnotron - Build Up - Stacking Buff (1): 81816
* Invulnotron - Discharge (1): 81817
* Invulnotron - Disintegrate - Damage Proxy (1): 81827
* Invulnotron - Build Up - Base (1): 81916
* Invulnotron - Corroded Armor - Base (2): 81951 81952
* Invulnotron - IA Cycle - Failed interrupt (1): 87146

### e4346 (Dungeon/ProtogamesAcademy: DefeatWrathbone), 70 rows
* Room 3 - Boss - Auto-Attack #1 (1): 76832
* Room 3 - Boss - Auto-Attack #2 (1): 76833
* Room 3 - Boss - Breath (1): 76851
* Room 3 - Fire Add - Auto-Attack #1 (1): 76863
* Room 3 - Fire Add - Auto-Attack #2 (1): 76865
* Room 3 - Boss - Adds (Buff) (1): 76866
* Room 3 - Boss - Adds (Detonate) (1): 76869
* Room 3 - Boss - Leap [DNT] (1): 76889; castMs [500]
* Room 3 - Meteor Add - Meteor (1): 76895; castMs [500]
* Room 3 - Boss - Adds (Summon) [DNT] (N) (1): 76926
* Room 3 - Boss - Transition Signal (Dance) [DNT] (1): 77005
* Room 3 - Boss - Transition Signal (Adds) [DNT] (2): 77045 77046
* Room 3 - Boss - Transition (Adds) (N) (2): 77063 77064
* Room 3 - Boss - Transition (Adds - Dispel) [DNT] (2): 77065 77066
* Room 3 - Boss - Burn Down (Cast) (1): 77073
* Room 3 - Boss - Burn Down (Summon) [DNT] (2): 77074 77075
* Room 3 - Boss - Burn Down (Detonate - AOE) (1): 77083
* Room 3 - Boss - Burn Down (Detonate - Targets) [DNT] (1): 77084
* Room 3 - Boss - Rock (Target Selection) [DNT] (3): 77085 81813 81814
* Room 3 - Boss - Rock (Cast) (1): 77086
* Room 3 - Boss - Rock (GTAE Cast) [DNT] (2): 77087 77088
* Room 3 - Boss - Rock (GTAE Action) (1): 77092
* Room 3 - Boss - Rock (Sharpnel 0) [DNT] (2): 77093 77094
* Room 3 - Boss - Rock (Shrapnel 0 - Move Angle 0) [DNT] (1): 77098
* Room 3 - Boss - Rock (Sharpnel - Damage) (2): 77099 77100
* Room 3 - Boss - Rock (Sharpnel 45) [DNT] (2): 77121 77122
* Room 3 - Boss - Rock (Shrapnel 45 - Move Angle 45) [DNT] (2): 77123 77124
* Room 3 - Boss - Rock (Sharpnel 90) [DNT] (2): 77125 77126
* Room 3 - Boss - Rock (Shrapnel 90 - Move Angle 90) [DNT] (2): 77127 77128
* Room 3 - Boss - Rock (Sharpnel 135) [DNT] (1): 77129
* Room 3 - Boss - Rock (Shrapnel 135 - Move Angle 135) [DNT] (1): 77130
* Room 3 - Boss - Rock (Sharpnel 180) [DNT] (1): 77132
* Room 3 - Boss - Rock (Shrapnel 180 - Move Angle 180) [DNT] (2): 77133 77134
* Room 3 - Boss - Rock (Sharpnel 225) [DNT] (1): 77135
* Room 3 - Boss - Rock (Shrapnel 225 - Move Angle 225) [DNT] (1): 77136
* Room 3 - Boss - Rock (Sharpnel 270) [DNT] (1): 77137
* Room 3 - Boss - Rock (Shrapnel 270 - Move Angle 270) [DNT] (2): 77138 77139
* Room 3 - Boss - Rock (Sharpnel 315) [DNT] (1): 77141; castMs [1100]
* Room 3 - Boss - Rock (Shrapnel 315 - Move Angle 315) [DNT] (1): 77142
* Room 3 - Boss - Channel (Right Hand FX) [DNT] (2): 77147 77148; castMs [600, 8000]
* Room 3 - Boss - Channel (Left Hand FX) [DNT] (1): 77150
* Room 3 - Fire Elemental Stacking Damage - (V) (1): 81832
* Room 3 - Fire Elemental Base Spell - (V) (1): 81833
* Room 3 - Boss - Transition (Dance) (V) (1): 81839; castMs [3000]
* Room 3 - Meteor Add - Meteor (V) (1): 81840
* Room 3 - Boss - Adds (Summon) [DNT] (1): 81876
* Room 3 - Ground and Pound IA [DNT] (1): 81955
* Room 3 - Ground and Pound IA Remove [DNT] (1): 81956
* Room 3 - Boss - Breath - Mid Phase 1 (1): 82187
* Room 3 - Boss - Transition (66%) (V) (1): 82301
* Room 3 - Boss - Transition (33%) (V) (1): 82310
* Room 3 (V)- Boss - Rock (Cast) Midphase (1): 82311
* Room 3 - Boss - Channel (V) (1): 82469
* Inevitable Destruction Damage Intake Increase - Room 3 - Wrathbone (1): 83960

### e4495 (Dungeon/ProtogamesAcademy: DefeatIrukiBoldbeard), 41 rows
* Iruki - (N) Auto-Attack (1): 76330
* Iruki - Tracking (Target Selection - 5 Target) [DNT] (1): 76333
* Iruki - Tracking (Target Cast) (1): 76334; castMs [200]
* Iruki - Donut #1 (Target Selection) [DNT] (2): 76357 76358; castMs [1500]
* Iruki - Donut #1 (Cast) (1): 76360; castMs [1500]
* Iruki - IA Cycle - Basic (Cast) (2): 76368 76369
* Iruki - IA Cycle - Interrupt Armor (Buff) (2): 76370 76371
* Iruki - IA Cycle - Interrupt Armor (Dispel) [DNT] (1): 76372
* Iruki - Donut #2 (Target Selection) [DNT] (1): 76394; castMs [1500]
* Iruki - Donut #2 (Cast) (1): 76395
* Iruki - Donut #3 (Target Selection) [DNT] (2): 76396 76397
* Iruki - Donut #4 (Target Selection) [DNT] (1): 76402
* Iruki - Donut #4 (Cast) (1): 76403
* Iruki - Donut (Telegraph Flash) [DNT] (1): 76697
* Iruki - (V) Annihilate - Base (1): 82319
* Iruki - (V) Annihilate - Visual Proxy (1): 82320; castMs [1000]
* Iruki - (V) Shield - Detect Player - Base (1): 82321
* Iruki - (V) Annihilate - Damage Proxy (1): 82324
* Iruki - (V) Shield - Barrier Proxy (1): 82330
* Iruki - (V) Shield - Anti-Matter Field - Aura (1): 82337
* Iruki - (V) Shield - Initializing - Base (2): 82340 82341; castMs [2000]
* Iruki - (V) Static Instability - Bsse (1): 82342; castMs [1000]
* Iruki - (V) Static Anomoly - Tracking Proxy (1): 82343; castMs [1500]
* Iruki - (V) Static Anomoly - Tracking (Field Cast) [DNT] (2): 82350 82351; castMs [1000]
* Iruki - (V) IA Cycle - Basic (Cast) (1): 82358
* Iruki - (V) Auto-Attack (1): 82359
* Iruki - (V) Meteor Fire - Tracking Proxy (1): 82360
* Iruki - (V) Meteor Fire - Field Cast (1): 82361
* Iruki - (V) Meteor Fire - (Field) (1): 82362
* Iruki - (V) Matter Distortion (1): 82980; castMs [3000]
* Iruki - (V) Static Emission - Aura (1): 82985
* Iruki - (V) Shield - Anti-Matter Field - Base (1): 82986
* Iruki - (V) Static Emission - Base - DNT (2): 83459 83460; castMs [2000]
* Iruki - (V) Vortex (1): 83467; castMs [2000]

### e4496 (Dungeon/ProtogamesAcademy: DefeatGromka), 27 rows
* Gromka - Auto-Attack #1 (1): 76272
* Gromka - Auto-Attack #2 (1): 76273
* Gromka - IA Cycle - Base (N) (1): 76274
* Gromka - IA Cycle - Interrupt Armor (Buff) (1): 76275; castMs [2700]
* Gromka - IA Cycle - Interrupt Armor (Dispel) [DNT] (2): 76276 76277
* Gromka - Teleport (Top) [DNT] (1): 76280
* Gromka - Fire Totem - Flamethrower (1): 76282; castMs [6000]
* Gromka - Fire Totem - Random Spin [DNT] (1): 76283
* Gromka - Fire Totem - Spin Right [DNT] (1): 76284
* Gromka - Flame Wave (Move) [DNT] (1): 76291
* Gromka - IA Cycle - Base (V) (1): 82042
* Gromka - Demon's Step - Base (2): 82063 82261; castMs [3000]
* Gromka - Inferno - Base (1): 82066
* Gromka - Inferno - Wave (Outgoing) (1): 82121
* Gromka - Inferno - Aura Proxy (Outgoing) (1): 82173
* Gromka - Hellfire - Base (1): 82199
* Gromka - Demon's Step - Torch Target Proxy (1): 82279
* Gromka - Demon's Step - Challenge Fail Proxy (1): 82280
* Gromka - Eruption - Base (1): 82281
* Gromka - Blunt Force Trauma (1): 82302
* Gromka - Inferno - Wave (Incoming) (1): 82943
* Gromka - Inferno - Aura Proxy (Incoming) (1): 82944
* Gromka - Inferno - Cast Tele (Out) (1): 82945
* Gromka - Inferno - Cast Tele (In) (1): 82947
* Gromka - Concentrated Heat (1): 83283

### e4497 (Dungeon/ProtogamesAcademy: DefeatSeekNSlaughter), 15 rows
* Seek-N-Slaughter - Auto Attack #1 (1): 76251; castMs [2700]
* Seek-N-Slaughter - Auto Attack #2 (1): 76252; castMs [2733]
* Seek-N-Slaughter - Barrage (1): 76329
* Seek-N-Slaughter - Battlebot - Missile - Base (1): 76347; castMs [500]
* Seek-N-Slaughter - Missile Proxy (1): 76354
* Seek-N-Slaughter - Damage Proxy - Normal (1): 76373; castMs [4000]
* Seek-N-Slaughter - Bomb Explosion - Normal (1): 76442
* Seek-N-Slaughter - Spawn Battlebot (1): 76443; castMs [1250]
* Seek-N-Slaughter - Spawn Explosive (1): 76444
* Seek-N-Slaughter - Bomb Fuse (1): 81769
* Seek-N-Slaughter - Bomb Explosion (1): 81770
* Seek-N-Slaughter - Homing Missile Base - Veteran (1): 81818
* Seek-N-Slaughter - Missile Proxy - Veteran (1): 81819
* Seek-N-Slaughter - Damage Proxy - Veteran (1): 81820
* Seek-N-Slaughter - Invulnerability (1): 83273

### e4498 (Dungeon/ProtogamesAcademy: DefeatIceboxMk2), 16 rows
* Icebox Mk. 2 - Blast - Normal (1): 76518
* Icebox Mk. 2 - Auto-Attack #1 (1): 76598; castMs [450]
* Icebox Mk. 2 - Auto-Attack #2 (1): 76599
* Icebox Mk. 2 - Frost Machine - Frost Blast Aura (1): 76644; castMs [4751]
* Icebox Mk. 2 - Frost Blast - Veteran (1): 76649
* Borerbot - Tower 0 (1): 76890; castMs [500]
* Icebox Mk. 2 - Melee Mech - Auto-Attack (1): 77362
* Icebox Mk. 2 - Frost Blast - Base - Normal (1): 79328
* Icebox Mk. 2 - Frost Machine - Frost Blast - Base - Veteran (2): 79329 79330; castMs [2000]
* Icebox Mk. 2 - Frost Machine - Ice Spikes (1): 81838
* Icebox Mk. 2 - Frost Machine - Spin Proxy CCW (1): 81878
* Icebox Mk. 2 - Frost Machine - Spin Proxy CW (2): 81879 81880
* Icebox Mk. 2 - Blast - Veteran (1): 81900
* Icebox Mk. 2 - Melee Mech - Self Explosion (1): 81984; castMs [3000]

### e4499 (Dungeon/ProtogamesAcademy: DefeatSuperInvulnotron), 15 rows
* Super-Invulnotron - Pillar Explosion (1): 77062
* Super-Invulnotron - Shield (Buff) (1): 77089
* Super-Invulnotron - MOO (2): 77090 77091
* Super-Invulnotron - Auto-Attack #1 (1): 77143; castMs [5000]
* Super-Invulnotron - Auto-Attack #2 (1): 77145
* Super-Invulnotron - Smash and Pound (1): 77155
* Super-Invulnotron - Botherbot - Smashy (1): 77173
* Super-Invulnotron - Botherbot - Force Move (2): 77175 77176
* Super-Invulnotron - Pillar - Damage (1): 77217
* Super-Invulnotron - Tower - Signal (1): 79073
* Super-Invulnotron - Kinetic Lance - Base (1): 82024
* Super-Invulnotron - Botherbot - Shrapnel Base (1): 82044
* Super-Invulnotron - Kinetic Lance - Dmg (1): 82309

## [TS]
### e479 (Dungeon/SanctuaryOfTheSwordmaiden: DefeatDeadringerShallaos), 1 rows
* Shallaos Vignette Flavor - Jump to Chime (1): 54678

### e480 (Dungeon/SanctuaryOfTheSwordmaiden: DefeatOnduLifeweaver), 30 rows
* Ondu Lifeweaver - Auto-Attack - L2R (2): 34324 34326; castMs [2250]
* Ondu Lifeweaver - Auto-Attack - R2L (1): 34327; castMs [2500]
* Ondu - Clover AE - Proxy Circle (1): 42516
* Ondu - PBAE Root 1 - Root Phase (1): 42526; castMs [2000]
* (N/V) Ondu - PBAE Explosion - Root Phase (1): 42540; castMs [9000]
* (N/V) Ondu - Unit Detection Aura - Root Phase (2): 42744 42745; castMs [2000]
* Ondu - Blight Fury - Buff (1): 42917
* Ondu - Heal - Self Buff (1): 42918; castMs [2000]
* Ondu - Vitara's Resilience- Self Buff (1): 42919
* (N/V) Ondu - Fluff - Life Weaver Phase (1): 43002
* (N/V) Ondu - Fluff - Fury Phase (1): 43004
* (N/V) Ondu - Fluff - Resistance Phase (2): 43005 43006
* Ondu - Clover - Base (1): 43253
* (N/V) Ondu - Interrupt Armor Proxy (1): 43306
* (N/V) Ondu - Holder Spell 1 Target - Root Phase Telegraphs (1): 43331
* (N/V) Ondu - Field Proxy - Plague Splatter (2): 43413 63397
* (N/V) Ondu - Pull To Location - Ravel Cast (2): 43452 43453
* (N/V) Ondu - Pull To Location - Ravel Signal (1): 43455
* (N/V) Ondu - Blight Fury - Movement Speed Proxy (1): 45757
* Ondu Vignette - Flower Growth (1): 53871
* (V) Ondu - Clover AE - Proxy Circle (1): 63400
* (V) Ondu -  Fury Debuff - Snare (1): 63405; castMs [2250]
* (V) Ondu - Shotgun Throw - Resilience Base (2): 63434 63435; castMs [1500, 2500]
* Ondu - Line Smash (1): 63453; castMs [750]

### e483 (Dungeon/SanctuaryOfTheSwordmaiden: DefeatMoldwoodOverlordSkash), 42 rows
* Skash - Swarmling - Auto-Attack #2 - Base (1): 42128
* Skash - Death Gaze - Gaze of the Moldwood - Base - Normal (1): 42131
* Skash - Summon the Swarm - Base (1): 42135
* Skash  - Corruption Heartseeker - Tremor Aura Base (1): 42138
* Skash - Smash - Auto-Attack #1 - Base (1): 42139
* Skash - Crush - Auto-Attack #2 - Base (3): 42144 42145 70453
* Skash - Corruption Siphon - Base (3): 42148 42149 87466; castMs [1500]
* Skash - Moldwood Essence Siphon - Base (3): 42161 63564 63565
* Skash - Swarmling Bounce (1): 42177
* Skash - Spawn Signal - Base (2): 42190 63631
* Skash - Corruption Heartseeker - Player Aura (1): 42223
* Skash - Corruption Heartseeker - Tremor Aura (2): 42228 42230
* Skash - Corrupting Spike Limiter (1): 42290; castMs [2000]
* Skash - Corruption Heartseeker - VFX Only - Proxy (1): 43102; castMs [3000]
* Skash - Corruption Heartseeker - Base (1): 45081
* Skash - Blighted Moldwood Peep -  Corruption Pustule - Base (2): 54323 54324
* Skash - Corruption Heartseeker - Summon Proxy (5): 63528 63551 63552 63553 63554; castMs [2000]
* Skash - Death Gaze - Gaze of the Moldwood - Base - Veteran (1): 63536
* Skash - Death Gaze - Gaze of the Moldwood - Rotation (1): 63537
* Skash - Corrupting Revolver - Damage Aura (5): 63538 63543 63544 63545 63546; castMs [5000]
* Skash - Corrupting Revolver - Base (2): 63541 63542
* Skash - Corruption Heartseeker - Peep Summon Controller (1): 63567
* Skash - Corruption Heartseeker - Tracker Signal (1): 63630
* Skash - Moldwood Essence Siphon - Target Selector (1): 80525; castMs [1500]

### e486 (Dungeon/SanctuaryOfTheSwordmaiden: DefeatRaynaDarkspeaker), 39 rows
* Rayna - Auto-Attack (1): 42126
* Rayna - Raging Lava - Base (1): 42134
* Rayna - Flame Leash - Base (1): 42171; castMs [2567]
* Rayna - Flame Leash - Proxy (1): 42176; castMs [2000]
* Rayna - Flame Geyser - Single Target - Proxy (1): 42204
* Rayna - Molten Wave - Signal Spawner (4): 42233 63423 63424 63426; castMs [2000]
* Rayna - Molten Wave 1 - Base (1): 42264; castMs [3000]
* Rayna - Molten Wave 2 - Base (1): 42321
* Rayna - Fire Shield - Molten Wave 1 (1): 42347
* Rayna - Molten Wave 3 - Burndown - Normal (1): 42391
* Rayna - Smoldering - Base (1): 42416
* Rayna - Smoldering Pool - Base (1): 42418; castMs [2000]
* Rayna - Flame Geyser - Group Target -  Base (1): 42437
* Rayna - Scalding Steam - Proxy Signal (1): 54433
* Rayna - Molten Wave - Channel - Var 1 (1): 54487
* Rayna - Molten Wave - Channel Base (6): 54489 63448 63449 63450 63451 63452; castMs [750, 3500]
* Rayna - Molten Wave - Channel - Var 2 (1): 54491
* Rayna - Molten Wave - Channel - Var 3 (1): 54492
* Rayna - Molten Wave - Channel - Var 4 (1): 54493
* Rayna - Molten Wave - Proxy Var 1 or 6 (1): 54496; castMs [2733]
* Rayna - Flame Geyser - Intro - Flavor (1): 54695
* Rayna - Flame Geyser - Proxy - Flavor (1): 54696
* Rayna - Smoldering - PBAE (V) (1): 63395
* Rayna - Smoldering Pool - Damage (1): 76010; castMs [1000]
* Molten Wave Safe Spot - Channel - Var 1 (1): 80512
* Molten Wave Safe Spot - Channel - Var 2 (2): 80513 80514
* Molten Wave Safe Spot - Channel - Var 3 (1): 80515
* Molten Wave Safe Spot - Channel - Var 4 (2): 80516 80517
* Molten Wave Safe Spot - Channel - Var 5 (1): 80518

### e489 (no matching objective enum name), 8 rows
* Selene Holdout - AP/SP Buff (2): 54653 81722; castMs [1000, 20000]
* Selene Holdout - Crit Buff (1): 54654
* Selene Holdout - Heal over Time (2): 54656 81723; castMs [30000]
* Selene Holdout - Interrupt Armor (2): 54659 81726; castMs [1500]
* Selene Holdout - Wave Completed (1): 81718

### e492 (Dungeon/SanctuaryOfTheSwordmaiden: DefeatSpiritmotherSeleneTheCorrupted), 30 rows
* Selene - Shade Prison - Base n (1): 42386; castMs [3000]
* Selene - Auto-Attack #1 (1): 42460; castMs [2000]
* Selene - Luminous Guardian - Base n (1): 42505
* Selene - Luminous Guardian - Buff Proxy n (1): 42522; castMs [6000]
* Selene - Creeping Shadows - Base n (1): 42523
* Selene - Creeping Shadows - Damage Proxy n (2): 42524 42525; castMs [966, 2000]
* Selene - Hack and Slash - Base n (2): 42573 81662
* Selene - Whirlwind (1): 42577
* Selene - Nightmare Ripple (Single) - Base n (1): 42632
* Selene - Converging Shadows (1): 42658
* Selene - Engulfing Shadows - Base n (2): 42755 81663
* Selene - Lights Out! - Base n (1): 42757
* Selene -  Shadow Whip - Single Target Proxy (2): 42857 42858; castMs [3000]
* Selene - Nightmare Ripple - Adds Phase - Base n (1): 42879
* Selene - Shadow Meld - Base n (1): 42880
* Selene - Luminous Sanctuary (1): 56079; castMs [1600]
* Selene - Nightmare Ripple - Aura Proxy (2): 63506 72906; castMs [1500]
* Selene - MOO (1): 63576
* Selene - Nightmare Ripple - Aura Visual (1): 63605
* Selene - Shadow Whip - Pull Proxy (1): 63713
* Selene - Intro - Purify (1): 64896
* Selene - Intro - Intro Knockdown (1): 64903; castMs [2500]
* Selene - Nightmare Ripple - Damage Proxy (3): 81659 81660 81661; castMs [5000]

### e496 (Dungeon/SanctuaryOfTheSwordmaiden: SabotageLifeweaverTechClusters), 1 rows
* Elemental Charge (1): 55190

### e497 (Dungeon/SanctuaryOfTheSwordmaiden: UseTheSoulSporeOnMoldwoodGorgers), 8 rows
* Holding a Spirit Bomb - Actionbar Spell (1): 80522; castMs [1500]
* Spirit Bomb - Moldwood Gorgers Kill Spell (2): 80523 80524; castMs [1500]
* Remove Spirit Bomb Actionbar (1): 80538
* Residual Spirit Essence (1): 80541
* Spirit Bomb - Pickup Aura (1): 80542; castMs [1500]
* Spirit Bomb Explosion (1): 80545
* [DNT] Spirit Bomb Scale (1): 81581

### e499 (Dungeon/SanctuaryOfTheSwordmaiden: DestroyTorineTotemsOfFlame), 2 rows
* Totem of Flame - Flamethrower - Base (1): 43691; castMs [3000]
* Totem of Flame - Flamethrower - Proxy - AE (1): 43692

### e627 (Dungeon/SanctuaryOfTheSwordmaiden: KillDistractedMoldwoodMaulers), 1 rows
* Decoy Splorg - Strain Mauler - Toy (1): 54490

### e1504 (Dungeon/SanctuaryOfTheSwordmaiden: DefeatSkashOrHeWillCorruptThePrisoner), 1 rows
* Torine Prisoner - Murder that Lady (1): 54826

