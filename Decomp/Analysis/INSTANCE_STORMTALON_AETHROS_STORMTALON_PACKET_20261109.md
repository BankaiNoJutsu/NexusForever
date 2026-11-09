# Instance packet: Stormtalon's Lair, Aethros, Stormtalon, Arcanist Breeze-Binder, Overseer Drift-Catcher, challenges (2026-11-09)

Phase B packet per [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md). Companion to [INSTANCE_STORMTALON_BLADEWIND_PACKET_20261109.md](INSTANCE_STORMTALON_BLADEWIND_PACKET_20261109.md) and [DUNGEON_VIDEO_SURVEY_STORMTALON_20261109.md](DUNGEON_VIDEO_SURVEY_STORMTALON_20261109.md). Documents only: no code, no git. No retail parity claim.

Evidence tiers used below. **Observed** = an agent read the subtitle text of a clip (auto-captions, error-prone: Aethros is captioned "AOS", "Atheros", "Gus convergence" and similar; no frame was viewed for this packet). **Observed x2/x3** = two or three independent commentators agree. **Single** = one clip only. **Mapped (table read)** = a raw game-table row read by an agent script, keyed by id, not re-derived by the lead. **Correlated** = a name, text or structure match without a direct id link; **Correlated (weak)** = name only. Nothing here is Verified. Every table read and video read in this file is "agent-written, not re-derived / not re-viewed" until the lead re-checks it.

## 0. Findings that change earlier records (read first)

1. **The Spell4 "cast" and "cd" columns printed by `instance_spell_rows.py` are not cast time and cooldown.** The "cast" value (1 to 7) is the **TierIndex** (field order of `Spell4Entry`: Id, Description, Spell4BaseIdBaseSpell, TierIndex, RavelInstanceId, CastTime, SpellDuration, SpellCoolDown, ranges). The script's "cd" value is the **CastTime in milliseconds**. Check used: with this field order, row columns 10 to 12 read as the float ranges (0, 100 or 300, -1) in every candidate row, and CastTime equals the end of the row's telegraph window in four rows (Static Wave 39681 10000 ms, Lightning Rod 63110 15000, Thunder Cross 63231 1500, Static Wave 40014 500 against 550). The real `SpellCoolDown` column is 0 in every candidate row, so **no cast interval is stored in Spell4**. Earlier statements "Tempest raw cooldown 15000", "Thunder Cross cooldown 1500" and "cast time holds values 1 to 5" should read CastTime 15000, CastTime 1500 and TierIndex 1 to 5. Tier: Mapped (field order from the repo model plus the alignment check above; not natively verified).
2. **Spell4 ids 39xxx and 63xxx are tiers of the same base spell, not two spell sets.** Rows sharing `Spell4BaseIdBaseSpell` differ by TierIndex. Examples (Mapped): Tempest base 23995 = tier 1 row 39682 (CastTime 30000) and tier 2 row 63114 (CastTime 15000); Lightning Rod base 24049 = 39796 (tier 1) and 63110 (tier 2); Static Charge base 23931 = 39613 and 63127; Lightning Storm base 24058 = 39825 and 63116; Lightning Strike base 23920 = tiers 1 to 5 = 39596, 63252, 63257, 63258, 63259. This explains why one ability name has many rows (the roughly 40 "Lightning Strike" rows are several bases times up to five tiers). Which tier a normal or veteran boss casts is **not** read from any table (see section 3.3).
3. **There is a creature-keyed override table that already answers health and interrupt armor.** `Creature2OverrideProperties` (Creature2Id, UnitPropertyIndex, UnitPropertyValue) holds BaseHealth (property 7), InterruptArmorThreshold (44), InterruptArmorRechargeTime (45), InterruptArmorRechargeCount (46) and more for these bosses (section 3.1). The repo already loads this table (`CreatureInfo`). These are the strongest binding facts in this packet.
4. **A family of internal "Messaging" Spell4 rows names the boss and the ability** (ids 45583 to 45683, section 3.4). They are RavelSignal effects (effect type 0x51), the script-signal hook. They are the closest thing to an internal ownership tag besides the two `[STL]` rows.
5. **Blade-Wind second sources found** (section 5): armor 2 is agreed by three clips plus a table computation; the 60% phase-2 threshold is **not** agreed (two clips say about 60, two say 50); the three-times Electrostatic Pulse is agreed by three clips but its interruptibility is disputed.
6. **Aethros thresholds are disputed between clips** (80% and 40% in one 2015 clip, 66% and 33% in one 2014 clip). Do not enable either.

## 1. Footage (post-launch first; URLs only; corpus ids changed on 2026-11-09)

All clips: retail footage uploaded 2014-06-02 to 2015-12-04, dated, with subtitles; none is emulator or post-shutdown. Resolution was not re-probed for this packet (the survey recorded 1280x720 for every Stormtalon clip it probed, so cast-bar names are not legible; no frame was viewed). Tier column: what the clip states; "not stated" means normal versus veteran cannot be told from the subtitles.

| Role | Date | URL | Length | Tier | Used for |
|---|---|---|---|---|---|
| Boss guide, Aethros (moepork) | 2015-10-04 | https://www.youtube.com/watch?v=A7d1h77Ag60 | 117 s | not stated (series treated as veteran by the survey) | Aethros skeleton, 80/40 |
| Boss guide, Stormtalon (moepork) | 2015-10-05 | https://www.youtube.com/watch?v=99dod9Ba-Ks | 121 s | not stated | Stormtalon skeleton, 90/50, armor 4 |
| Boss guide, Blade-Wind (moepork) | 2015-09-28 | https://www.youtube.com/watch?v=8M53-Kf90a4 | 131 s | not stated | Blade-Wind facts |
| Spoken guide with run footage, all bosses (Hakurai) | 2014-07-01 | https://www.youtube.com/watch?v=fXt0jlfD5lA | 936 s | veteran silver | Aethros 66/33, Stormtalon, both optional bosses, Blade-Wind second source |
| Veteran guide with run (SywoGG) | 2015-08-24 | https://www.youtube.com/watch?v=T2OJbSH7aD8 | 1437 s | veteran; the commentator says near the end that the run is on a beta client | Aethros, Stormtalon, challenges, armor counts, Blade-Wind second source |
| Dungeon overview (HaasGaming, spellslinger healer) | 2014-06-08 | https://www.youtube.com/watch?v=oLIr-cY_sts | 675 s | not stated | All bosses and Drift-Catcher; says Blade-Wind 50% |
| Normal-mode guide (TinyBlueGames) | 2015-10-08 | https://www.youtube.com/watch?v=_Y0MWGDS8QU | 1052 s | normal (notes veteran differences) | Aethros two cycles, Stormtalon "Eye of the Storm", objective counts |
| Silver guide (akathecoot) | 2014-07-22 | https://www.youtube.com/watch?v=l-STQmH9T00 | 810 s | veteran silver | Aethros, Stormtalon, optional boss, challenges |
| Veteran run (Grievance Gaming) | 2015-12-04 | https://www.youtube.com/watch?v=UCnVYIG1JAI | 2558 s | veteran | Aethros teleporting, Blade-Wind 50% and pulse |
| Dungeon impressions (Chilluminati Podcast) | 2014-06-10 | https://www.youtube.com/watch?v=JzzHleJf6d4 | 1403 s | not stated | Aethros phases, "two interrupts" |
| Boss guide 1/3 (Seatin Man of Legends) | 2014-06-09 | https://www.youtube.com/watch?v=P7eQtXKxVlM | 249 s | veteran | Blade-Wind second source |
| Veteran run callouts (Zybak) | 2014-07-16 | https://www.youtube.com/watch?v=Kz2j5zQvuX4 | 1119 s | veteran gold | cast-bar callout of the Eye of the Storm name only |
| Veteran silver run | 2014-07-16 | https://www.youtube.com/watch?v=Rj-6XiQqKyE | 1482 s | veteran silver | Drift-Catcher shield and convergence callouts |
| Veteran silver run | 2014-07-10 | https://www.youtube.com/watch?v=V4v1g64b5sM | 1524 s | veteran silver | convergence callout only |
| TheHiveLeader guide | 2015-10-14 | https://www.youtube.com/watch?v=as4B7DPTmZQ | 277 s | not stated | Aethros and Stormtalon outline, no numbers |

Beta clips (before 2014-06-02, Correlated at best, used only to check consistency): https://www.youtube.com/watch?v=c0df1AC7x0g (2014-03-24, Aethros splits into three tornado adds, tornado lanes and lightning), https://www.youtube.com/watch?v=N1UJUHts6jk (2014-04-12), https://www.youtube.com/watch?v=jKrfhHoFpZw (2014-05-13).
Not readable (no subtitles): Dulfy veteran guides 2014-07-07 (https://www.youtube.com/watch?v=LpTkOQOFnsM Aethros, https://www.youtube.com/watch?v=9QEyA41N-tQ Stormtalon, https://www.youtube.com/watch?v=48OmhGFbjV8 Blade-Wind) whose descriptions link a written guide at dulfy.net (not fetched), the 2014 Elijaa, Pulse Guild and KP M+ boss videos, and 2014-06-17 "Overseer Drift Catcher" (https://www.youtube.com/watch?v=6TOQnPM1_rA). A PTR "drop 4" clip (2014-12-13, https://www.youtube.com/watch?v=SBjGZEM0ylY) was searched and has no readable Aethros or Stormtalon description.
Not found: any clip with subtitles that is dedicated to Arcanist Breeze-Binder alone; the two optional bosses are covered only inside dungeon guides.

## 2. Phase and ability skeleton (Observed)

Agreement key: x2 or x3 = that many independent commentators; single = one clip; DISPUTED = clips contradict each other.

### 2.1 Aethros (creatures 17166 normal, 32703 veteran)
| Item | Status | Sources |
|---|---|---|
| Three-phase cycle: phase 1 tank and spank with a periodic circle telegraph under every player (Torrent in the 2015 guide, a circle under each player in the others; lightning on all players with knockdown, lethal if two stack) | Observed x4 | A7d1h77Ag60, fXt0jlfD5lA, T2OJbSH7aD8, _Y0MWGDS8QU |
| Phase 2: boss dissipates and **three** adds appear (all captions agree on three; a script message "Aethros tears apart, creating gusts of wind" matches, text 290528, see 3.5) | Observed x5 | A7d1h77Ag60, fXt0jlfD5lA, oLIr-cY_sts, _Y0MWGDS8QU, UCnVYIG1JAI (also JzzHleJf6d4, beta c0df1AC7x0g) |
| Phase 2 adds target players with circle telegraphs that persist on the floor for the rest of the fight (Static Shock in the 2015 guide); advice to drop them at the edges | Observed x3 | A7d1h77Ag60, fXt0jlfD5lA, T2OJbSH7aD8 |
| After the adds die the group is knocked or teleported to the entrance end of the hall | Observed x5 | A7d1h77Ag60, fXt0jlfD5lA, oLIr-cY_sts, T2OJbSH7aD8, UCnVYIG1JAI |
| Phase 3: tornadoes sweep side to side, red lightning lines and circles on the floor, players run back to the boss and interrupt a long charge (Tempest) or the group dies | Observed x5 | same as above |
| Two full cycles per kill (phase 1, 2, 3, then again, then the last phase 1 with tornadoes still moving) | Observed x3 | A7d1h77Ag60, fXt0jlfD5lA, _Y0MWGDS8QU (stated as exactly two, tied to damage and not to time); UCnVYIG1JAI and oLIr-cY_sts compatible |
| Tornado speed in the last stretch alternates slow and fast (veteran) | Observed x2 | A7d1h77Ag60, fXt0jlfD5lA; _Y0MWGDS8QU (normal) says one fixed path and speed, so this may be a veteran-only change (single normal clip) |
| After the first cycle the boss teleports around the room during phase 1 and fires a single faster lightning strike at a player | Observed x3 for the teleporting; the faster single strike is single (fXt0jlfD5lA) | fXt0jlfD5lA, T2OJbSH7aD8, UCnVYIG1JAI |
| **Phase thresholds** | **DISPUTED** | A7d1h77Ag60 (2015-10-04): phase 2 at 80% then 40%. fXt0jlfD5lA (2014-07-01): 66% and 33%, and that at 66% start the sprint to the left of the first tornado, at 33% to the right. Other clips give no numbers. Both are single-source. A patch change between 2014-07 and 2015-10 is possible but no clip says so. |
| Interrupt armor / interrupts needed | DISPUTED | A7d1h77Ag60: infinite interrupt armor, four interrupts to stop the white mechanic. JzzHleJf6d4 (2014-06-10): one armor, two interrupts needed. fXt0jlfD5lA: every group member uses an interrupt. Table: threshold -1 (infinite), see 3.1. |
| Time to reach and interrupt Tempest | Single | A7d1h77Ag60: group wipes after 15 s. T2OJbSH7aD8 (veteran) gives a looser figure of about 30 s. Table: tier 2 CastTime 15000, tier 1 CastTime 30000 (3.3). |
| Torrent interval | Single | fXt0jlfD5lA: about every 10 s in phase 1. |
| Adds are immune to crowd control and taunt | Single | A7d1h77Ag60. fXt0jlfD5lA says the adds can be knocked down, interrupted and stunned (DISPUTED); JzzHleJf6d4 agrees with fXt0jlfD5lA. |
| Boss yells | none found | No Aethros yell text was found in the text table (searched Aethros, tornado, gust, cyclone, dissipate), and chat logs are not legible. |

### 2.2 Stormtalon (creatures 17163 normal, 33406 veteran)
| Item | Status | Sources |
|---|---|---|
| Frontal cone auto attack that slows; do not stand in front; tank kites | Observed x5 | 99dod9Ba-Ks (cone chains lightning, slow 90%), fXt0jlfD5lA, T2OJbSH7aD8, _Y0MWGDS8QU, oLIr-cY_sts |
| Static Charge: telegraphs on random players that strike and leave a ground effect; each cast adds a stacking boss buff, so the fight is a soft enrage | Observed x3 for the soft enrage | 99dod9Ba-Ks, l-STQmH9T00, fXt0jlfD5lA |
| Early knock-back to the centre and stun, then a must-interrupt cast whose telegraph fills the room from the outside in (Static Wave in the 2015 guide); players use a break-out key press, run to the boss and interrupt | Observed x6 | 99dod9Ba-Ks, fXt0jlfD5lA, oLIr-cY_sts, T2OJbSH7aD8, l-STQmH9T00, _Y0MWGDS8QU |
| Lightning strikes in front of each player during that phase, and the lightning telegraph is the optional "Lightning Reflexes" condition | Observed x3 | 99dod9Ba-Ks, fXt0jlfD5lA, l-STQmH9T00 |
| Second mechanic at **50%**: everyone knocked down, one player gets a safe circle and is disoriented (keys reversed), the group stays in the circle, the player moves slowly | Observed x2 for 50% | 99dod9Ba-Ks (50%), fXt0jlfD5lA ("get him to 50%"); circle and disorient seen in T2OJbSH7aD8, l-STQmH9T00, _Y0MWGDS8QU, oLIr-cY_sts (no number) |
| Name of that mechanic: "Lightning Rod" in three clips, "Eye of the Storm" in three other clips | Observed x3 each | Lightning Rod: 99dod9Ba-Ks, fXt0jlfD5lA. Eye of the Storm: Kz2j5zQvuX4 (cast-bar callout), _Y0MWGDS8QU, SBjGZEM0ylY (PTR). Probably one mechanic under two names (cast name versus debuff name, or a rename); see 3.3. |
| First Static Wave at **90%** | Single | 99dod9Ba-Ks. fXt0jlfD5lA says the first one comes after about 100K damage and later ones are time-based; the table gives the veteran BaseHealth as 1,000,000, so 100K is 10% (consistent, Mapped plus Observed, not independent proof of 90%). |
| Static Wave has four interrupt armor | Single | 99dod9Ba-Ks. Table: creature threshold -1; no modify-armor effect found in the candidate rows (3.1). |
| Phases cycle (Static Wave phase, Lightning Strike phase, Lightning Rod phase) until death | Observed x2 | fXt0jlfD5lA, T2OJbSH7aD8 |
| Boss yells | none found | Stormtalon's High Priest yells exist (text 152706 and neighbours) but belong to the High Priest event, not this boss. |

### 2.3 Overseer Drift-Catcher (33361 normal, 33362 veteran) - the cliff optional boss reached by the launch pads
| Item | Status | Sources |
|---|---|---|
| Identity: the optional boss reached through the launch pad / trampoline is Drift-Catcher | Single (oLIr-cY_sts names it right after describing the jump pads). Kz2j5zQvuX4 only names the boss at an early point of the run and does not say which cliff; fXt0jlfD5lA and _Y0MWGDS8QU describe the cliff boss without a name. Correlated, not agreed. | oLIr-cY_sts |
| Only one of the two optional bosses spawns per run (the other being Breeze-Binder) | Observed x3 | oLIr-cY_sts, fXt0jlfD5lA, _Y0MWGDS8QU |
| Interruptible channelled stun (captions give Gust or Dust Convergence); the name is heard in four clips, the stun itself is described in a fifth | Observed x5 | fXt0jlfD5lA, l-STQmH9T00, Rj-6XiQqKyE, V4v1g64b5sM (name); oLIr-cY_sts (stun described) |
| If the stun is not interrupted, adds appear: three tornado adds that get stronger over time (fXt0jlfD5lA) or small wind elementals (oLIr-cY_sts) | Observed x2 for adds; the count three is single | fXt0jlfD5lA, oLIr-cY_sts |
| Two shield phases: knock-back, shield, line telegraphs all over the floor, group spreads and bursts the shield, then the boss can be stunned | Observed x3 | fXt0jlfD5lA, l-STQmH9T00, Rj-6XiQqKyE |
| Shield phases at 66% and 33% | Single | fXt0jlfD5lA |
| Line telegraphs across the whole area during the shield phase (Electro Storm in the table) | Observed x3 (name single) | oLIr-cY_sts (names an electro-storm channel), fXt0jlfD5lA, _Y0MWGDS8QU (telegraphs all over the place) |

### 2.4 Arcanist Breeze-Binder (24474 normal, 34711 veteran) - the other optional boss
| Item | Status | Sources |
|---|---|---|
| Reached by clearing extra trash and using a computer (the Thundercall data altar objective 555 and key objective 556); rated much easier than the other optional boss | Observed x2 | fXt0jlfD5lA, _Y0MWGDS8QU (easy boss in a corner near the end, interrupt his casts, he casts adds) |
| Casts the same convergence cast with three tornado adds on failure | Single | fXt0jlfD5lA only; the other clips attach that name to the cliff boss, unresolved |
| Four interrupt armor, five interrupts needed | Single and unnamed | T2OJbSH7aD8 (about 1002 s: an unnamed "mini boss" with four armor); table property 44 = 4 for veteran Breeze-Binder (3.1). The identification is Correlated (weak). |
Nothing else (thresholds, intervals) was found for Breeze-Binder.

### 2.5 Optional objectives and challenges (video side; table side in 3.2)
* Tornado Chasing: no one caught by a tornado while fighting Aethros. Observed x3 (A7d1h77Ag60, T2OJbSH7aD8, l-STQmH9T00) plus the table text.
* Swift Sweeper: kill the phase 2 adds fast. Observed: T2OJbSH7aD8 says 15 s; A7d1h77Ag60 says phase 2 in 10 s or less (DISPUTED; table says 15 s, see 3.2). Pressure: the adds also drop telegraphs that follow players (T2OJbSH7aD8, _Y0MWGDS8QU).
* Stormchaser (Blade-Wind): every phase 2 Lightning Strike must land on a channeler. Observed x4 (P7eQtXKxVlM, JzzHleJf6d4, UCnVYIG1JAI, 8M53-Kf90a4) plus table text.
* Lightning Reflexes (Stormtalon): no one hit by the Lightning Strike telegraph; Lightning Rod lightning does not count. Observed x2 (99dod9Ba-Ks, l-STQmH9T00) plus table text.
* Deathless: T2OJbSH7aD8 states that any death loses it. Counts seen in video: 15 stem samples (fXt0jlfD5lA), 27 Thundercall Pell on a timer (_Y0MWGDS8QU), 6 grenade kills (_Y0MWGDS8QU), 8 totems (T2OJbSH7aD8); they match the table counts in 3.2. The Events panel count "Eliminate Thundercall Pell 9/20" read earlier from the Blade-Wind clip does not match the table count 19 (unresolved).

## 3. Row binding

### 3.1 Creature2 rows and per-creature properties (Mapped, table read)
Creature2 columns used: name text (col 4), DifficultyId (col 7), ArcheTypeId (col 8), **TierId (col 9)**. The repo uses TierId only as a unit-property multiplier source. The override table is `Creature2OverrideProperties`.

| Creature | Id | Tier / Difficulty | BaseHealth override (property 7) | Interrupt armor overrides (props 44 / 45 / 46) | Other overrides |
|---|---|---|---|---|---|
| Aethros normal | 17166 | 1 / 12 | 97,880 | 44 = -1 (infinite), 46 = 0 | none |
| Aethros veteran | 32703 | 5 / 12 | 1,000,000 | 44 = -1, 45 = 15000, 46 = 3 | none |
| Aethros "[Non-C] Invis Unit" (TargetGroup 3808) | 32937 | 1 / 12 | none | none | non-combat invisible placeholder; use not observed |
| Gust of Aethros (the three adds) | 17316 | 1 / 8 | none | none | property 40 = 0; no veteran row with this name exists |
| Aethros Twister | 31592 | 1 / 11 | none | none | name only, purpose not observed |
| Lightning Storm Bottom / Top | 17318 / 17319 | 1 / 4 | none | none | name only (see 3.4) |
| Stormtalon normal | 17163 | 1 / 12 | 185,630 | 44 = -1 | property 100 (move speed multiplier) = 1.2 |
| Stormtalon veteran | 33406 | 5 / 12 | 1,000,000 | 44 = -1 | property 100 = 1.2 |
| Arcanist Breeze-Binder normal | 24474 | 1 / 11 | none | 24 = 1, 44 = 1, 46 = 1 | |
| Arcanist Breeze-Binder veteran | 34711 | 5 / 11 | 505,220 | 44 = 4, 46 = 4 | |
| Overseer Drift-Catcher normal | 33361 | 1 / 11 | none | 24 = 1, 44 = 1, 46 = 1 | |
| Overseer Drift-Catcher veteran | 33362 | 5 / 11 | 656,000 | none | |
| Skyborn Gust (spawned by Gust Convergence) | 36854 normal, 59876 tier 5 | 1 / 8 and 5 / 8 | 8,230 and 36,000 | none | |

Observations: the paired creature rows differ only by TierId (1 versus 5) and, for health, by the override. The pairs (17160, 33405), (17163, 33406), (17166, 32703), (24474, 34711), (33361, 33362) are the normal and veteran halves of each boss; that the "5" row is the **veteran** version is an inference from the repo's own pairing and the health values, not stated by any table.
Interrupt armor computed from the same product the repo's `CalculateDefaultProperty` uses (CreatureLevel row for property 44, times Creature2Tier, Creature2Difficulty and Creature2ArcheType multipliers; CreatureLevel[44] is 1.0 at levels 1, 20 and 50): Blade-Wind 17160 and 33405 = 1.0 x 2.0 = **2**; Breeze-Binder and Drift-Catcher base = 2 where no override exists. The same computation gives the Stormtalon trash: Storm-Weaver 36796 and 37141 = 2, Storm-Watcher 36862 and 37144 = 1, which is what T2OJbSH7aD8 states in speech (Storm-Weaver two, Storm-Watcher one). That is a cross-check of the method, not proof for the bosses. Tier: Mapped (agent-computed, not re-derived natively).
Server today: `AethrosVeteranEntityScript` (17166, 32703), `StormtalonEntityScript` (17163, 33406), `ArcanistBreezeBinderEntityScript` (24474, 34711), `OverseerDriftCatcherEntityScript` (33361, 33362) are kill-credit scripts only; none of the challenge objectives below is implemented (the enum lists them; nothing updates them).

### 3.2 PublicEventObjective rows (Mapped, table read; event 145)
Columns per the repo model: Flags (col 2), TypeEnum (10), Count (11), ObjectId (12), FailureTimeMs (13), Category (15), MedalPointValue (19). Type 5 is `Script`, so a script must call `UpdateObjective` for those rows.

| Id | Name text / meaning (table) | Type, Count, ObjectId, FailureTimeMs, medal points | Notes |
|---|---|---|---|
| 313 Eliminate Aethros | text 205604 | 0 KillTargetGroup, 1, TargetGroup 2586 (17166, 32703), 0, 500 | TargetGroup label "e313 - Aethros - Stormtalon" |
| 314 Destroy Stormtalon | text 205606 | 5 Script, 1, 0, 0, 500 | credit is script-driven, not a target-group kill; the scaffold already calls it |
| 556 Defeat Arcanist Breeze-Binder for the encryption key | text 235260 | 0, 1, TargetGroup 3030 (24474, 34711), 0, 500 | TargetGroup label "e556 - Thundercall Artificer - Stormtalon" |
| 828 Kill Overseer Drift-Catcher | text 294999/295000 | 0, 1, TargetGroup 3921 (33361, 33362), 0, 500 | |
| 844 Tornado Chasing | "Defeat Aethros without anyone being caught by a tornado" (texts 296170, 296171) | 5 Script, 0, 0, 0, 400, category 3 | |
| 2381 Swift Sweeper | "...without letting any Gusts of Aethros survive for more than 15 seconds" (524769, 524770) | 5 Script, 0, 0, 0, 400 | |
| 2487 Swift Sweeper (variant) | "Defeat the Gusts of Aethros within 15 seconds" (536699, 536700) | 5 Script, 0, 0, **15000**, medal points 0 | a second row with a timer; which of 2381 and 2487 is live in the event is not determined |
| 2380 Stormchaser | "...after causing every one of his Lightning Strikes to strike a Thundercall Channeler" (524767, 524768) | 5 Script, 0, TargetGroup 2585 (Blade-Wind), 0, 400 | Blade-Wind's challenge, not Aethros's |
| 2382 Lightning Reflexes | "...without anyone in your party getting hit by Lightning Strike" (524771, 524772) | 5 Script, 0, 0, 0, 400 | |
| 842 Deathless in the Dungeon | text "Finish the entire Dungeon without anyone dying" | 5 Script, 0, 0, 0 | achievements "Immortal: Stormtalon's Lair (Normal/Prime)" exist as text 301153 to 301158 |
| 843 Kill as many Thundercall Pell as you can | | 0, **27**, TargetGroup 11121, **900000**, 400 | video count 27 agrees (_Y0MWGDS8QU) |
| 3201 Timer Bonus | "within 40 minutes" | 25 ScriptWithoutMax, 0, 0, 0, 2500 | |
| 554 / 557 / 561 / 562 / 464 | stem samples / Pell / grenades / storm totems / sacrificial prisoners | counts 15 / 19 / 6 / 8 / 5 | 15, 6 and 8 agree with video; 19 versus 20 on the Events panel unresolved |

`Achievement.tbl` rows 2275, 2276, 2277 reference the names Stormchaser, Swift Sweeper, Lightning Reflexes (texts 301159, 301162, 301165), so the three challenges also exist as achievements (Mapped, not followed further). The Swift Sweeper discrepancy (10 s in one clip, 15 s in another and in both table rows) is resolved in favour of 15 s for the table and the SywoGG clip; the 10 s statement stays a single contradicting clip.

### 3.3 Spell4 candidate rows (names, tiers, effect shapes; Correlated unless an anchor is stated)
Effect type names from the repo enum (0x1 VitalModifier, 0x3 ForcedMove, 0x4 CCStateSet, 0x8 Damage, 0x11 Fluff, 0x15 SummonCreature, 0x1a Proxy, 0x23 NpcExecutionDelay, 0x51 RavelSignal). CastTime in ms, Mapped by field order (section 0 item 1). Telegraph rows come from `Spell4Telegraph` (Spell4Id to TelegraphDamageId) and `TelegraphDamage` (shape Param00 to Param02, start and end ms); shapes per the repo `DamageShape` enum (0 circle, 1 ring, 2 square, 3 cross, 4 cone, 7 rectangle). Param meanings for rings are not verified.

| Ability (guide name) | Candidate rows (base, tier: id, CastTime) | Anchor and tier | Telegraph / effect notes |
|---|---|---|---|
| **Tempest** (Aethros white mechanic) | base 23995: tier 1 = 39682 (30000), tier 2 = 63114 (15000) | **Text anchors**: both rows reference the text "Gathering power to release a devastating storm" (63114 col 62 text 534487; 39682 col 62 text 328289, same string). Tier 2 CastTime 15000 equals the 15 s in A7d1h77Ag60. Correlated; the owner (Aethros) rests on the text and the guide, not on a creature link. | effects: damage, RavelSignal, CCState 2000 ms, RavelSignal; no telegraph row |
| **Torrent** (Aethros phase 1) | 36042 (2000, effects: damage x2; circle telegraph, Param00 6.5) and 36028 (2000, proxy), bases 21641 and 21630 | name only; **internal row 45595 "Aethros Torrent Messaging"** names Aethros and Torrent. Correlated (weak) for the damage rows, Correlated for the messaging row | the knockdown in the guide is not in these rows; the Lightning Strike family has the CCState 2000 ms (below), so Torrent probably proxies a Lightning Strike (not shown) |
| **Static Shock** then floor circle (Aethros phase 2 adds) | 63141 (2500, proxy, circle 5.0) and **63142 Static Field** (CastTime 1000, **SpellDuration 4294967295 (infinite)**, ring-less circle 5.0, damage ticks 500 ms; text 535340: damage per tick and reduced movement speed) | name and semantics only: infinite duration matches "permanent telegraph", reduced movement speed matches "big slow" (fXt0jlfD5lA). Correlated (weak). Other Static Shock rows (63251, 63260, 45602, 45603, 61330, 62851) are not placed. | |
| **Lightning Storm** (Aethros phase 3 lightning lines) | base 24058: 39825 (2250, proxy, delay 3500), 63116 (tier 2); base 24059: 39826 (1500, damage x2), 63441 to 63444 (tiers 2 to 5); creatures 17318 / 17319 "Lightning Storm Bottom / Top" | name only. Correlated (weak) | |
| **Tornado Alley** (Aethros tornado lanes) | 39982 (summon/proxy, rectangles 4 x 20, 4 rows), 39983 (CastTime 1000, SpellDuration 10000, rectangle 4 x 20 x 20; tier 2 = 80246) | name matches "tornado gauntlet"; Correlated (weak). 39984 Manifest Cyclone / 39985 Cyclone (Duration 15000, circles 7.0 and 3.0) match the Skyborn Tempest trash mob in oLIr-cY_sts, not a boss | |
| **Static Charge** (Stormtalon) | base 23931: 39613 (tier 1), **63127** (tier 2, CastTime 2000); also bases 10217, 30990, 39606 (other content) | **Text anchor**: 63127 col 62 text 534671 "Stormtalon is accumulating static energy ... damage dealt increased per stack"; its effects are a RavelSignal and three UnitPropertyModifier (0xb) rows, matching the stacking buff (damage; cast speed is not shown in the text). Correlated, the strongest Stormtalon spell link | |
| DoT from Static Charge (Stormtalon) | 39955 Static Paroxysm (DoT 30000 ms, tick 1500; text 348182: damage over time and increased magic damage taken) | name and semantics; matches "damage over time you can cleanse" (l-STQmH9T00). Correlated (weak) | |
| **Static Wave** (Stormtalon white mechanic) | 39681 (CastTime 10000, effects damage, FullScreenEffect, damage); 40014 (500); 63124 and 63125 (CastTime 0, 63125 Duration infinite) | **Internal rows 45597 "Stormtalon Static Wave Interrupt Messaging" and 45599 "...Start Messaging"**: Correlated. 39681 has ten ring telegraph rows (shape 1) whose Param00 falls 63, 56, ... 0 in steps of 7 with Param01 70, each active for 1000 ms from 0 to 10000 ms: a ring closing from the outside over 10 s, which matches the "fills from the outside in" descriptions (99dod9Ba-Ks, fXt0jlfD5lA, oLIr-cY_sts, three independent). Correlated. | |
| **Lightning Rod / Eye of the Storm** (Stormtalon 50%) | base 24049: 39796 (tier 1) and 63110 (tier 2), both CastTime 15000; base 28469: 44323 (CastTime 20000, other use); "Eye of the Storm" 51978 and 51985 (CastTime 15000) | 63110 reference text 534284 to 534287 "Disoriented with Reduced Movement Speed. Electricity flows through you"; 39796 has the same text without "Disoriented" (text 345967). The guides say disoriented (fXt0jlfD5lA, 99dod9Ba-Ks), so tier 2 (63110) matches the observed behaviour. 63110 telegraph: ring Param 10 inner, 100 outer, 0 to 15000 ms, plus a small circle 0 to 5100 ms; 39796 ring 14 to 100. Row 51985 "Eye of the Storm" has the same debuff text pattern (text 469511 "Reduced Movement Speed. Electricity flows through you") and a ring 15 to 40 over 5 to 15 s; its owner is unknown (name and mechanic match three "eye of the storm" clips). **Correlated (weak)**. Conduction 39988 to 39990 (text 348184, 348185: lightning bolts drawn to your location; channelling lightning strikes on the target, stun 6000 ms) is a candidate for the chosen-player lightning; Correlated (weak) | |
| **Lightning Strike** (Blade-Wind, Aethros, Stormtalon share the name) | base 23920 tiers 1 to 5 = 39596, 63252, 63257, 63258, 63259 (CastTime 1500; effects damage, CCState 2000 ms, RavelSignal, damage); base 24160 = 39935; base 41305 = 63157, 63253 to 63256; base 41406 = 63267 | name only. Telegraph circle: 63252 radius 3.5, 63259 radius 5.0, both active 1600 ms; 39935 radius 5.0. The tier changes the telegraph radius, so the tier choice matters for fidelity. Correlated (weak) | |
| **Gust Convergence** (Drift-Catcher, perhaps Breeze-Binder) | base 24204: **39979** (CastTime 3000, range 40, effects: three SummonCreature rows at angles 72, 144, 216 spawning Skyborn Gust 36854, plus RavelSignal and delay) and tier 2 = 80333 (spawns 59876, the tier-5 Skyborn Gust); base 24205: **39980** (CCState 4500 ms, the stun) | Correlated: the effects reproduce the described three adds on a failed interrupt and the stun; the owner (Drift-Catcher versus Breeze-Binder) is not table-linked | |
| **Electro Storm** (Drift-Catcher line telegraphs) | base 24191: 39966, tier 2 = 63622; 24194: 39969 (damage x2; six square telegraph rows, Param 5 x 10 x 5, short 0 to 1500 ms windows); internal row 45683 "Electro Storm Messaging" | name and telegraph shape (lines) match three clips; owner not table-linked. Correlated (weak) | |
| Drift-Catcher shield and knockback | not identified | no row found; candidates `Storm Shield` 36043, 36046 are generic and were not matched | |

### 3.4 Internal messaging rows (Mapped table read, Correlated for ownership)
Spell4 rows whose name contains the boss: 45583 "Blade-Wind the Invoker Thunder Cross Messaging", 45586 "...Channel Phase Start Messaging", 45592 "...Electrostatic Pulse Messaging", 45595 "Aethros Torrent Messaging", 45597 and 45599 "Stormtalon Static Wave Interrupt / Start Messaging", 45682 "Stormtalon Manifest Cyclone Messaging", 45683 "Electro Storm Messaging". Each has one RavelSignal effect (type 0x51) whose value column holds 15588, 15590, 15593, 15600, 15609, 15607, 15630, 15631 respectively. Other rows: 40743 "Stormtalon Summon" and 44764 "StormtalonEvent Volume Trigger FX" (High Priest event). The two older `[STL]` rows (63171, 63183, Blade-Wind's Static Barrier) are recorded in the Blade-Wind packet. "Stormtalon Manifest Cyclone" cannot be placed: the guides give Manifest Cyclone to the Skyborn Tempest trash mob, and "Stormtalon" may be a dungeon prefix. Caveat for all of this section: a name tag shows the signal, not that the boss casts the damaging row, and what the signal values mean is not known.

### 3.5 Text anchors
* Script-fired Aethros message: text 290528 "Aethros tears apart, creating gusts of wind!" (double-parenthesis style, no `.tbl` references it, so a script raises it; it sits in the id range of the veteran Aethros rows, 289656 and 290833 to 290859). Matches the pop-up that oLIr-cY_sts describes at the dissipation. Correlated.
* Creature name texts: 155545 Stormtalon, 155550 Aethros, 156587 Gust of Aethros, 205683 Arcanist Breeze-Binder, 294991 Overseer Drift-Catcher, 289656 and 290833 Aethros (veteran rows), 295540 Stormtalon (veteran). Internal TargetGroup labels "e312 - The Invoker", "e313 - Aethros", "e314 - Stormtalon" (texts 205598 to 205600) and "e556 - Thundercall Artificer" (237514).
* No yell texts for Aethros or Stormtalon were found; the Blade-Wind block is 534452 to 534487 (extends the survey's 534470 range: 534452 to 534469 are also Blade-Wind lines, for example "Run, outsiders! Your doom has arrived!" and "Acolytes! Infuse me with pure primal energy!", whose triggers are unknown).

## 4. Timing measurements

**None measured from clips.** No frame was viewed and no telegraph differencing was run for this packet; the Blade-Wind packet already records that a 1 Hz red-area detector over edited guide footage is not usable. Needed for any timing claim: unedited footage with the boss selected, two clips per boss, 2 Hz or better, telegraph events checked against viewed frames.
Speech-stated values (all single-source, none is a measured gap): Aethros Torrent about every 10 s (fXt0jlfD5lA); Tempest wipe after 15 s (A7d1h77Ag60); Stormtalon first Static Wave after about 100K damage then time-based (fXt0jlfD5lA); the Drift-Catcher shield phases need about two stuns (l-STQmH9T00). Table-side values (not observed gaps): Tempest CastTime 15000 (tier 2) and 30000 (tier 1); Static Wave CastTime 10000 with a closing ring over 10 s; Lightning Rod and Eye of the Storm CastTime 15000; Lightning Strike CastTime 1500 and telegraph 1600 ms; Gust Convergence CastTime 3000 and stun 4500 ms; Torrent CastTime 2000; Aethros veteran interrupt-armor recharge time 15000 ms and recharge count 3. `SpellCoolDown` is 0 in every row, so repeat intervals are in neither table: they come from the creature AI or a script and are an open input.

## 5. Second source for the single-source Blade-Wind facts

| Claim in the Blade-Wind packet | Second source | Result |
|---|---|---|
| Phase 2 begins at 60% | P7eQtXKxVlM (2014-06-09, veteran): says about 60 percent. T2OJbSH7aD8 (2015-08-24): says the boss will probably be at 60 to 70% when the phase changes (loose). **Against**: oLIr-cY_sts (2014-06-08, tier not stated) says 50%; UCnVYIG1JAI (2015-12-04, veteran) says the first phase lasts until 50%. | **DISAGREE / not agreed.** Two clips about 60, one loose 60 to 70, two clips 50. The date and tier do not explain it (P7eQtXKxVlM and oLIr-cY_sts are one day apart). Do not enable 60%. |
| Two interrupt armor | T2OJbSH7aD8 (two armor, three interrupt skills needed); fXt0jlfD5lA (three interrupts are enough each time); table computation 1.0 x 2.0 = 2 (3.1). | **AGREE** (three clips plus a Mapped computation). Tier Observed x3 and Mapped. |
| Electrostatic Pulse cast three times | fXt0jlfD5lA (the large telegraph three times in a row, safe spots on the outside); UCnVYIG1JAI (three consecutive large attacks). Name "Electrostatic Pulse" itself only in 8M53-Kf90a4; the table has the internal row 45592 "Blade-Wind the Invoker Electrostatic Pulse Messaging" and Spell4 39074 (CastTime 2500). | **AGREE on the count three** (three clips). Name: Observed single plus Correlated (internal row). **DISAGREE on interruptibility**: 8M53-Kf90a4 says all three interrupted (nine in total); UCnVYIG1JAI says only two can be interrupted and the third cannot; P7eQtXKxVlM and fXt0jlfD5lA use "interrupt, interrupt, dodge". |
Additional Blade-Wind data from this pass: veteran BaseHealth override 1,150,000 (row 33405) and normal 78,220 (17160); Blade-Wind and his channelers have no interrupt-armor override.

## 6. Open inputs (what is missing and what could supply it)

| Missing | Why it is open | Possible source |
|---|---|---|
| Aethros phase thresholds (80/40 or 66/33) | two clips contradict each other; both single | frame reading of the boss bar at the dissipation message in one 2014 and one 2015 clip; native read of the Aethros script; a packet capture |
| Which Spell4 tier each version casts (1 versus 2 to 5) | no creature-to-spell link; the creature TierId pattern (1 versus 5) and spell tier families (up to 5) are suggestive only, and the repo uses TierId for stat multipliers only | native read of NPC spell-tier selection; clip evidence of a tier-specific number (Tempest 30 s versus 15 s, telegraph radius 3.5 versus 5.0) |
| Cast order and repeat intervals for every boss | `SpellCoolDown` is 0; guides only say the casts are periodic | clip telegraph timing at 2 Hz, or the creature AI / script via a native read |
| Which row each boss casts (Torrent damage row, Lightning Storm lines, Drift-Catcher shield, Breeze-Binder abilities) | no creature spell list in any table | native read of the creature spell list (owner decision per the goal prompt), or packet capture |
| Meaning of the RavelSignal values 15588 to 15631 | unknown id space | native read of the signal consumer |
| Static Wave armor value 4 and the Aethros "four interrupts" / recharge count 3 | single clip each; the tables hold only -1 for the creatures | native read of the interrupt-armor handling; a second clip |
| Stormtalon 90% first Static Wave | single clip; only consistent with 100K of 1,000,000 | second clip, or frame reading |
| Drift-Catcher 66% and 33% shield phases; Breeze-Binder abilities | single or absent | more clips (none dedicated exist), native read |
| Gust of Aethros veteran stats, spawn positions, wisp and tornado paths, speed | no veteran creature row for the adds; paths are not in tables | native or capture; positions possibly from spawn tables |
| Which of objectives 2381 and 2487 is live; the Events panel count 20 versus table 19 | two rows, one count mismatch | event row inspection, one frame |
| Yell lines and triggers (Aethros, Stormtalon, Blade-Wind block 534452 to 534469) | no table link for the text, clips not legible | clips (chat log) or script read |

## 7. Verdict and allowed next slices

* **Aethros**: structure agreed by three to five independent clips (three phases, three adds, knock-back to the entrance, tornado run with interrupt, two cycles). The numbers are not: thresholds disputed, no agreed order inside phases, no intervals. Table side gives creature rows, health, an infinite armor override, the Tempest text anchor and its CastTime. Per the prompt, no encounter code is justified. Allowed without inventing values: tests that pin the Mapped creature facts (the override rows in 3.1); a phase-tracking placeholder with the thresholds left empty (no default) until the dispute is resolved; documenting objective rows 844, 2381 and 2487 as script-type objectives that need a hit detector. None started.
* **Stormtalon**: the best-agreed boss of this packet: cone, Static Charge soft enrage, knock-back plus must-interrupt Static Wave (table shows a 10 s closing ring), 50% Lightning Rod / Eye of the Storm with a disorient and a safe circle. 90% is single but is consistent with 1,000,000 health and a 100K statement. Allowed: a disabled phase-tracking placeholder (50% as two-clip, 90% as single-source, same pattern as Blade-Wind), tests pinning the creature and objective rows; the Static Charge buff text anchor (63127, text 534671) is a candidate Correlated row for a later slice. No cast order or interval is available.
* **Drift-Catcher and Breeze-Binder**: partial skeleton (Gust Convergence, shield phases, line telegraphs); Gust Convergence 39979 and 39980 reproduce the stun and three adds in the table. No thresholds agreed (66/33 single). Not enough for code beyond kill credit.
* **Challenges**: texts and objective rows are Mapped. Implementation depends on the boss mechanics (Stormchaser needs the Lightning Strike target selection, Lightning Reflexes the Lightning Strike hit result, Tornado Chasing and Swift Sweeper the tornado hit and the add lifetimes), so it follows the bosses, per the prompt.

## 8. Not verified, and method

* No frame or image was viewed; all video content is subtitle text from auto-captions with known mis-hearings (boss and ability names); names were only used where two clips or a table text agree.
* No native read, no packet capture, no test, no code. The Dulfy written guide and any web source were not fetched.
* Table reads were run with throwaway Python in the scratchpad (not committed), using the same `.tbl` extracts and `en-US` text as the repo helper scripts and the field order of the repo's `GameTable.Model` classes (Spell4Entry, Creature2Entry, Creature2OverridePropertiesEntry, PublicEventObjectiveEntry, TelegraphDamageEntry). Tier columns, CastTime and the property indices were not natively verified.
* Resolution of the clips was not re-probed; the tier (normal or veteran) of several clips is not stated and was left as "not stated".
* Creature2 `CcStateImmunitiesFlags` was not used as evidence (column position not confirmed); the "adds are immune to crowd control" claim stays single-source.
* The commentary of one clip (T2OJbSH7aD8) says it was recorded on a beta client; its statements are treated as post-launch Observed but may reflect a patch under test.
