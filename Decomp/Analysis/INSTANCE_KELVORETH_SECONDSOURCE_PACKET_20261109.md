# Instance packet, second-source pass: Ruins of Kel Voreth (2026-11-09)

Companion to [INSTANCE_KELVORETH_PACKET_20261109.md](INSTANCE_KELVORETH_PACKET_20261109.md) (single-source, three moepork 2015 guides). Phase B packet per [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md). This file tests every threshold and mechanic of that packet against other commentators, covers Gurka, the Blood Pit, the Osun elites and the optional objectives, and adds table anchors.

Evidence tier: **Observed** (subtitle reads by an agent, not re-viewed by the lead; no video frame was viewed in this pass) and **Correlated** (Spell4, Spell4Effects, Creature2, TargetGroup, PublicEventObjective, LocalizedText and CommunicatorMessages rows read from `I:\tools\extract\DB` and `I:\tools\extract_en`). Nothing is Mapped or Verified. No retail parity claim. Agent-written, not re-derived and not re-viewed until the lead re-checks it.

Conventions: videos are cited by URL (`https://www.youtube.com/watch?v=<id>`) and cue start second `t=`; corpus ids are never used. Subtitles are YouTube auto-captions: boss and ability names are often garbled (for example "ocean" for Osun, "eonite" for Exanite, "Gron" for Grond), so quoted lines are the agent's reading of the captions. "Agreed" means a second clip by a different channel states the same thing; "disagrees" means a second clip states something incompatible; "not found" means no second clip states it.

## 0. Findings that change the first packet

1. **Build 16042 is the level 25 / level 50 retune; most June 2014 clips are level 20.** `LocalizedText` 313570 says "Minimum Level: 25" and 313631 ("Prime: Ruins of Kel Voreth", 313630) says "Minimum Level: 50". The June 2014 clips call the dungeon a level 20 dungeon (the GameGlitches title, Sparrow, Zap-Robo, the MMORPG.com stream) and SywoGG (2015-08-25) calls it level 25. Differences between 2014 clips and the 2015 moepork guides may be the retune, not contradictions. The closest clips to 16042 are the late-2015 ones (SywoGG, moepork, Steve Mayne, Pretend Like We).
2. **"Veteran" is called "Prime" in the 16042 text** (achievements 300958 to 301111, for example "Slavemaster Drokk (Prime)"). Every boss has a Normal and a Prime Creature2 pair.
3. **Drokk's phase order differs between difficulties in the clips.** Veteran clips (Vhalor, Imperfect Advice) and moepork agree on bombs, then tracking beacon, then tethers (Lockdown, Enslave); the normal/undeclared clips (Carbine stream, HeelvsBabyface, Steve Mayne, Sparrow beta) put Enslave and the Suppression Wave before the beacon.
4. **The challenge objectives name abilities in table text**: 976 "Homing Barrage" (Drokk), 977 "Essence of Primal Fire" (Trogun), 975 "bone cage" (Grond). 977 contradicts moepork's buff name ("Surge of Primal Fire").
5. **Several real text anchors exist**: Spell4 63237 "Tracking Beacon" references text 536695 "Slavemaster Drokk has made you the focus of his Destructo Constructs!"; internal-name tags `Drokk - Sets Interrupt Armor` (Spell4 39292) and `Drokk - Removes Interrupt Armor - Triggers MoO` (Spell4 39290) exist; the Grond banners 302425 "Grond has a nasty itch!" and 344233 "Grond goes into a murderous frenzy!" exist and are not referenced by any table (script-fired, like the Stormtalon yells).

## 1. Footage

Corpus filter: `upload_date <> '' and upload_date < '20181128'`, `meta_fts` match on Kel Voreth, Trogun, Grond, Drokk, Gurka, Osun, Blood Pit: 150 dated clips, of which the ones below were read. Resolution was not probed in this pass (the library is 720p per the Stormtalon survey, so cast-bar names are not legible; none of the claims below depend on frames). Difficulty is "not stated" unless the commentator says it. Launch is 2014-06-02.

Post-launch, subtitled, used (commentator, date, length, role, difficulty):

| Role tag | URL | Date | Len s | Content | Tier |
|---|---|---|---|---|---|
| Carbine stream | https://www.youtube.com/watch?v=PBqB8CiSaWk | 2014-06-19 | 4096 | MMORPG.com livestream, run with Carbine staff commentating (identity taken from the speech, not verified); all bosses, interrupt armor, objectives | normal, rallied to 20 ("not trying veteran") |
| Vhalor | https://www.youtube.com/watch?v=PMeSNlGd1tE | 2014-06-16 | 1042 | commentated full run | Veteran Silver |
| Imperfect Advice | https://www.youtube.com/watch?v=4y7DbS8Bwmw | 2014-06-25 | 1460 | commentated full run, all bosses, Gurka | Veteran Silver |
| HeelvsBabyface | https://www.youtube.com/watch?v=yKNW6IjWZBQ | 2014-06-05 | 693 | boss guide, three bosses | not stated |
| Havok parts 3, 4, 5 | https://www.youtube.com/watch?v=flZ6DPHkeTs , https://www.youtube.com/watch?v=bJbyCp-YeXI , https://www.youtube.com/watch?v=L819YxKBdM8 | 2014-06-19 | about 900 each | Drokk, Gurka, Trogun commentary; part 1 https://www.youtube.com/watch?v=nh3m0AbhJsA (Grond) | not stated |
| Dulfy Grond / Trogun / Gurka | https://www.youtube.com/watch?v=JJtxySqCr34 , https://www.youtube.com/watch?v=bRC17Hepq5k , https://www.youtube.com/watch?v=0HbuYaRufgQ | 2014-07-13 | 180 / 157 / 87 | player chat, little narration | Veteran |
| imbonked Grond / Drokk / Gurka | https://www.youtube.com/watch?v=q8Xu2a0RcAU , https://www.youtube.com/watch?v=Z3SfNZgJXl4 , https://www.youtube.com/watch?v=O_3dAxGBA50 | 2014-06-06 | 340 / 260 / 183 | group chat | not stated |
| likwidpixel Trogun | https://www.youtube.com/watch?v=_bVFhczuqAI | 2014-06-02 | 231 | yell lines only | not stated |
| Chief Sarcan, Ammaross | https://www.youtube.com/watch?v=8f-f13_OntE , https://www.youtube.com/watch?v=5pxouggR6Ho | 2014-07-18, 2014-06-30 | 4557, 5479 | 4-man / group runs, chat | normal and veteran mixed |
| Chronic Gamgi | https://www.youtube.com/watch?v=X7c3VbUIHXU | 2014-11-16 | 291 | Drokk | Veteran |
| Kaos Sverige | https://www.youtube.com/watch?v=pI0uSmO1mW0 | 2015-01-09 | 4733 | full run (yells) | Veteran |
| SywoGG | https://www.youtube.com/watch?v=VCM6cmoxlXg | 2015-08-25 | 1998 | commentated guide, says "level 25 ... level 50 veteran" | Veteran, level 25 |
| Steve Mayne | https://www.youtube.com/watch?v=rymYV9wq8ok | 2015-10-28 | 3010 | commentated run | not stated |
| Jandabear | https://www.youtube.com/watch?v=c_vIxh03mnk | 2015-11-18 | 954 | Veteran guide, almost no speech | Veteran |
| Pretend Like We | https://www.youtube.com/watch?v=4-FzQLzXvEA | 2015-12-17 | 1586 | dungeon guide (very poor captions) | not stated |

Pre-launch (Correlated at best; may differ from 16042): WildStar Weekly https://www.youtube.com/watch?v=Z7a3ijKCp8k (2014-05-26, 3688 s, hosted by the game team, normal, level 20); TheSparrowsJourney https://www.youtube.com/watch?v=K4_L4d4kl0s (2014-05-25, open beta, text-style guide); WildStarESP https://www.youtube.com/watch?v=UrlYsh-99vs (2014-02-27, beta, Spanish commentary auto-translated), https://www.youtube.com/watch?v=Ye_qvNRwaCM and https://www.youtube.com/watch?v=GzUwTw6ePR8 (2014-02-28); Bog Otter https://www.youtube.com/watch?v=KSDTBkXqmIo (Drokk) and https://www.youtube.com/watch?v=gWTNsvOAKGA (Trogun) (2014-04-03/04); Zap-Robo https://www.youtube.com/watch?v=CYIzX9yx0Og (2014-03-16); Gamesnatch https://www.youtube.com/watch?v=HZAhm-T5JTA (Blood Pit, 2014-04-22).

Also read but near-silent: Pulse Guild 2014-04-12 boss guides (captions are music only), Jandabear (almost no speech). Clips with no subtitles were not read.

## 2. Phase and ability skeleton: second-source verdicts

### 2.1 Forgemaster Trogun

| Claim in first packet | Verdict | Evidence |
|---|---|---|
| Threshold 50%: Trogun returns to the forge and channels Exanite Weapon, shards fly out | **Agreed (four clips, three post-retune)** | Pretend Like We https://www.youtube.com/watch?v=4-FzQLzXvEA t=1255 to 1266 ("the second phase starts when the boss hits 50 ... goes back to where he's standing ... throws saw blades"); SywoGG https://www.youtube.com/watch?v=VCM6cmoxlXg t=1879 to 1892 ("goes back to the middle when he's under 50%", describes circles rather than shards); Carbine stream https://www.youtube.com/watch?v=PBqB8CiSaWk t=3011 and 3059 (boss AI bugged, "he'll turn back on at 50%", then t=3277 to 3300 flying shards); Sparrow (beta) https://www.youtube.com/watch?v=K4_L4d4kl0s t=964 to 976 ("below fifty percent ... return to the forge ... Exanite weapon again"). |
| Fight opens with Exanite Weapon channel at the forge, volcanoes fire random fireballs | Agreed | Sparrow t=835 to 859; Imperfect Advice https://www.youtube.com/watch?v=4y7DbS8Bwmw t=1184 to 1195 ("fill the air with exite ... dodge the telegraphs"); HeelvsBabyface https://www.youtube.com/watch?v=yKNW6IjWZBQ t=470 to 493 (meteors, avoidance phase, then he joins); Weekly https://www.youtube.com/watch?v=Z7a3ijKCp8k t=2715. |
| Four interrupt armor | **Agreed (one second clip)** | Imperfect Advice t=1206 to 1215: Volcanic Strike "can be interrupted so it takes four interrupt armor" (Veteran Silver). Contradicting weak hint: Tyler https://www.youtube.com/watch?v=jJ1xIfhnfi0 t=711 "I don't know if there's anything that you can actually interrupt on him" (normal, uninformative). |
| Volcanic Strike tracks the tank, big burst, knockdown, leaves a volcano; interruptible; dodge by moving away | Agreed | Vhalor https://www.youtube.com/watch?v=PMeSNlGd1tE t=915 to 969 ("creates a volcano", "cannot dodge to the sides because he tracks you, dodge by moving backwards"); Imperfect Advice t=1195 to 1228; Weekly t=2726 ("slams down, creates a new volcano"); Sparrow t=895; Pretend Like We t=1245. |
| Forgemaster's Call: orbs drift to Trogun, player pickup is a damage buff, Trogun pickup makes his next attack explode for group damage | Agreed | Weekly t=2737 to 2851 (primal fire balls, about 5% damage per orb, boss with orbs "next auto attack or ability explodes with fire, room wide damage"); Carbine stream t=3232 to 3253; Aaron Nash https://www.youtube.com/watch?v=u1kn3UJem6o t=333 to 368; Imperfect Advice t=1228 to 1240; Vhalor t=888 to 942. |
| 5% damage per orb | Agreed, plus a detail | Weekly t=2759 ("like 5%"); WildStarESP https://www.youtube.com/watch?v=Ye_qvNRwaCM t=1068 ("5% damage bonus for 10 seconds", stacks time independently). Disagrees: Ammaross t=4917 "20% damage boost" (a casual remark). |
| Seven orbs | **Not found** | no second clip counts the orbs. |
| After the 50% phase Forgemaster's Call also shoots shards | Agreed | Weekly t=2807 to 2817; Carbine stream t=3289 ("next time ... he will shoot these blades out of him"); HeelvsBabyface t=594 to 617; Imperfect Advice t=1263 ("same rings out from his body"); Vhalor t=995 to 1008. |
| Challenge: Trogun must not take any orb | Agreed (table) | Objective 977 text 314582: "do not let him gain any stacks of Essence of Primal Fire". The table names the aura "Essence of Primal Fire", not "Surge of Primal Fire" (see 3.4). |
| (new, not in first packet) Forgemaster's Call is first cast earlier than 50% | Mixed | Sparrow t=924 "about seventy percent"; Havok https://www.youtube.com/watch?v=L819YxKBdM8 t=262 to 305 ("I believe 80%", hedged). moepork: "once or twice" after Volcanic Strike. Percent not agreed. |
| (new) a further orb phase near 20% | Single (beta) | Sparrow t=998 "below twenty percent ... forge master's call again" (Havok t=584 says 40%, t=841 30%). Not agreed. |

Trogun yell lines (Observed, caption readings, order of utterance within clips): likwidpixel https://www.youtube.com/watch?v=_bVFhczuqAI t=4 "Stop me and I will arm the Osun with Exanite and all of Nexus will [fall]", t=75 to 88 "Power forged by fire, I will be unconquerable ... Perish in the flames of Kel Voreth", t=120/148 "The very Earth shatters before my power", t=135/164 "I call upon the power of my ancient forges", t=164/180/194 "By the power of this Hammer you will burn"; Kaos https://www.youtube.com/watch?v=pI0uSmO1mW0 t=3824 "None can stand against the Forge Master"; Ammaross https://www.youtube.com/watch?v=5pxouggR6Ho t=4795 "Intruders impossible. Only fools would challenge the fury of the Forge Master" (trigger on entering his room); Chief Sarcan https://www.youtube.com/watch?v=8f-f13_OntE t=4254 "Seared by the fires of Kel Voreth, I will crush ..."; lincoln3 https://www.youtube.com/watch?v=FL2xi-urR9g t=120 "Witness the power of Exanite"; GameGlitches https://www.youtube.com/watch?v=Sr5vEiTVZpU t=834 "... forged of Exanite, now mine to control". Dominion death line: likwidpixel t=202 "So ends the Osun threat in Auroria ... the Widow's web is everywhere". None of these strings was found in `en-US.bin` (see 3.7).

### 2.2 Grond the Corpsemaker

| Claim in first packet | Verdict | Evidence |
|---|---|---|
| Unlocks Bellow at 75% | **Not found** | no second clip states 75%. Vhalor t=122 to 147 sees Thrash then Bellow within about 40 s of the pull (consistent with an early unlock, no percent). |
| Unlocks Mutilate at 50% | **Disagrees on the number** | WildStarESP beta https://www.youtube.com/watch?v=UrlYsh-99vs t=186 ("Fury ... around 20% health or less"); imbonked https://www.youtube.com/watch?v=q8Xu2a0RcAU t=227 ("once he gets to 20 ... blood frenzy"); Sparrow t=146 to 171 ("I think it's below thirty percent ... new ability Mutilate"). Vhalor t=160 to 186: "Murderous Frenzy" then Mutilate (no percent) and t=226 "he can use [Thrash] below 50%". These are 2014 level 20 clips and may predate the retune; moepork (2015) is the only source for 50%. |
| Infinite interrupt armor, abilities interruptible | **Disagrees on armor, agreed on interruptible** | Weekly t=1099 to 1111 (game-team speaker): "Grond has two interrupt armor ... hit him with three CCs it will interrupt" (normal, pre-launch). Thrash and Bellow interrupted in Vhalor t=122 to 147, SywoGG t=306 to 362, Imperfect Advice t=129 to 141, Carbine stream t=473 to 485, HeelvsBabyface t=142 to 178. Ammaross t=881 to 900 (before Grond's intro yell) "hit him with three" (weak). |
| Trap ring (damage and stun) | Agreed | Weekly t=1031; Sparrow t=45; Steve Mayne https://www.youtube.com/watch?v=rymYV9wq8ok t=90 and 204; Carbine stream t=194; table 975. |
| Frontal cone auto attack and Buck (cone behind) | Agreed | Weekly t=1165 to 1177; Vhalor t=122; Imperfect Advice t=119 to 129; Sparrow t=77 to 88; SywoGG t=363; Carbine stream t=512 ("donkey kicks"). |
| Headbutt rush at a random player, knockback | Agreed | Sparrow t=88 to 101; HeelvsBabyface t=48 to 61 ("random charge", about 2.2k); Imperfect Advice t=163 to 174 (leaps at a player who strays). |
| Thrash: rotating line, high damage, interrupt | Agreed on shape and interrupt; **counterclockwise not found** | Sparrow t=123 to 146; SywoGG t=306 to 327 ("360 AOE in thin stripes"); HeelvsBabyface t=61 to 84; Carbine stream t=485. |
| Bellow: knockback and disarm, followed by Scratch Itch | Agreed | Vhalor t=135 to 147; SywoGG t=283 to 296; Imperfect Advice t=141 to 152; Carbine stream t=496 (Scratch Itch "later in the fight"); table: Bellow 63109 proxies Subdue 8000 ms plus knockback (3.4). |
| Scratch Itch summons **three** fleas | **Split three vs four** | Three: HeelvsBabyface t=118 to 129, Sparrow t=111 to 123, Pretend Like We t=71 to 95 (2015-12, caption "the three ... cannot be killed"). Four: Vhalor t=135 to 147 ("four ads"), Dulfy https://www.youtube.com/watch?v=JJtxySqCr34 t=59 to 73 ("pop and spawn another four"). The table has both: Spell4 58769 summons three Bloodsucker Fleas (Creature2 33837, Creature2TierId 1) and Spell4 63129 summons four Bloodsucker Fleas (Creature2 56485, TierId 5) (3.5). |
| Fleas explode if uninterrupted and spawn more; lure them into traps | Agreed | Dulfy t=59 ("10 or 15 seconds to get them in the traps"); Imperfect Advice t=141 to 152 ("time limit ... blow up"); Sywo t=296 to 306; Weekly t=1204; Carbine stream t=577 (on normal "you can actually kill them"). |
| Challenge: no one caught in a trap | Agreed (table) | Objective 975 text 314574 "do not let anyone get trapped in a bone cage". |

Grond yell and banner lines (Observed): "Show us your strength and we will break you", "These walls will soon be awash in your blood" (Zap-Robo https://www.youtube.com/watch?v=CYIzX9yx0Og t=393 to 405), "Your shattered corpses will soon litter this sacred ground" (Gamesnatch https://www.youtube.com/watch?v=HZAhm-T5JTA t=107), "You still live? Unthinkable! But none can stand against the fury of Grond", death line "Grond ... another brute battered and beaten" (Ammaross t=443, t=1032). Banners "nasty itch" (Imperfect Advice t=141, Tyler t=85) and "murderous frenzy" (InTheLittleWood https://www.youtube.com/watch?v=DH3L36_nAaA t=458, Vhalor t=160) match table text 302425 and 344233.

### 2.3 Slavemaster Drokk

| Claim in first packet | Verdict | Evidence |
|---|---|---|
| Phase thresholds 85 / 65 / 45 / 25% | **Not found as numbers.** Closest: first phase end at 80% (Havok), "10 to 15%" lost (beta) | Havok https://www.youtube.com/watch?v=flZ6DPHkeTs t=214 to 226 ("tank and spank to 80%, he retreats to the green door") and t=420 to 432 ("he is at 30% ... ads come out on the fourth phase as well"); WildStarESP beta https://www.youtube.com/watch?v=UrlYsh-99vs t=570 to 590 ("when we have taken away 10 to 15% of its health, it disappears and another phase begins"); Sparrow t=587 to 598 (beacon droids at "about fifty percent"). Nobody says 65, 45 or 25. |
| Phase 1 tank and spank with Homing Barrage on random players | Agreed in shape | SywoGG t=950 to 962 ("random red telegraphs on the ground ... one of the challenges"); HeelvsBabyface t=256 to 266 ("random laser beams ... very slow"); Steve Mayne t=1637 ("orbital lasers"); Vhalor t=411; table objective 976 names Homing Barrage. |
| Phase 2: Drokk vanishes, bombshell constructs arrive and blow up; kill one first | Agreed (many clips) | Vhalor t=429 to 441; Imperfect Advice t=584 to 606; HeelvsBabyface t=234 to 256; SywoGG t=1139 to 1149; Carbine stream t=1524 to 1551 ("built ourselves a safe spot"); Havok t=226 to 276; Steve Mayne t=1648 to 1658. |
| Constructs explode about 10 s after arriving | **Not found** | no clip gives the time. |
| After the bombs Drokk returns and keeps summoning constructs at random players | Agreed | Vhalor t=441 to 453; Imperfect Advice t=606 to 617. |
| Phase 3 tracking beacon, constructs chase the marked player | Agreed | Vhalor t=453 to 479; Imperfect Advice t=617 to 641; Bog Otter https://www.youtube.com/watch?v=KSDTBkXqmIo t=302 to 327 ("tracking beacon ... he's made you the focus of his Destructor constructs"); Steve Mayne t=1746 to 1814; Weekly t=2036 to 2059; SywoGG t=1173 to 1301. |
| Exactly three beacons in the phase | **Not found** | SywoGG t=1173 to 1301 sees six retargets (spellslinger, esper, spellslinger, tank, esper, tank) in what may be two beacon phases; Steve Mayne t=1758 and Weekly t=2059 say the target changes "every now and then". |
| Then Lockdown (tether on a random player, broken by CC break or damage) | Agreed (veteran clips) | Vhalor t=506 to 520; Imperfect Advice t=641 to 664; SywoGG t=940, 1020 to 1031; Chronic Gamgi https://www.youtube.com/watch?v=X7c3VbUIHXU t=115 to 125. |
| Enslave: group teleported to the back and tethered; chained Suppression Wave | Agreed on content | Sparrow t=541 to 565; HeelvsBabyface t=267 to 300; Weekly t=1991 to 2026 ("giant expanding ring"); Carbine stream t=1551 to 1591 ("rings you can dive inside"); Vhalor t=520 to 546. Table: Enslave 39274 carries tether CCStateSet 45000 ms (3.4). |
| Suppression Wave leaves a permanent puddle when completed; interruptible later | Partial | Puddle: no second clip. Interrupt: Vhalor t=546 to 563 ("you have to interrupt ... about a 1 second cast time"), Chronic Gamgi t=125, t=192. Table: Suppression Wave 63119 proxies Suppression Field 63131 (periodic damage, Snare), plausible for a puddle; tags 39290/39292 below. |
| Phase 4 repeats beacon with constructs and bombs, then repeats previous phase | Agreed (veteran) | Vhalor t=563 to 629 (construct phase "with bombs", then "one last boss phase ... continue interrupting Suppression Wave"); Havok t=420 to 432. |
| Order bomb, beacon, Lockdown, Enslave, beacon+bombs | **Veteran agrees; normal differs** | Veteran (declared): Vhalor, Imperfect Advice; same order in Pretend Like We t=593 to 800 (beacon, then tethers; difficulty not stated). Normal/undeclared: bombs, **Enslave and Suppression Wave, then beacon**: Carbine stream t=1524 to 1675; HeelvsBabyface t=222 to 392; Steve Mayne t=1648 to 1746; Sparrow t=470 to 650 (beta); Havok t=214 to 432 also puts tether and shock wave before the beacon. |
| Drokk yells | Observed | "Free wills detected. Initiating oppression protocols. Destroy them, my minions" (Chronic Gamgi t=22 to 24, Bog Otter t=211, Ammaross t=2633); "Suppress all resistance/intruders" (imbonked https://www.youtube.com/watch?v=Z3SfNZgJXl4 t=167, Chief Sarcan t=2179, Zap-Robo t=4039). |

### 2.4 Darkwitch Gurka (mini boss)

Not in the first packet. Four clips agree on **three escalating abilities**: telegraphs that chase each player (Deadly Defilement), a cross/X-shaped ground pattern, and a Blind that is the only interruptible one.
* Weekly https://www.youtube.com/watch?v=Z7a3ijKCp8k t=1619 to 1663 ("Deadly Defilement ... gets scarier and scarier", first spikes in red, later "a giant X on the ground", third "blind us all", "all based on health percentages ... escalation mechanic").
* SywoGG t=607 to 666 ("deadly defilement ... telegraphs that chase them", "cross-shaped pattern that stays on the ground", "Blinding Dark ... interrupt").
* Imperfect Advice t=396 to 443 (drops AoEs, then a cross, then Blind, "the only one of hers that can be interrupted", turning away avoids it).
* Sparrow t=323 to 370, imbonked https://www.youtube.com/watch?v=O_3dAxGBA50 t=2 to 167, Dulfy https://www.youtube.com/watch?v=0HbuYaRufgQ t=55 to 76.
* Escalation percentages: **not found** (Havok https://www.youtube.com/watch?v=bJbyCp-YeXI t=575 "I think 40% 30% and she starts doing that AoE"; hedged, single). Optional: skippable (Sparrow t=217 to 227, Imperfect Advice t=396); lasers can be switched off before her (SywoGG t=597, Carbine stream t=1121 to 1149).

### 2.5 Blood Pit, gladiators, announcer

* **Three packs gate the boss**: Vhalor t=16 to 96 (first pack Mighty Swing, second the Necromancer with Wail of the Darkwitch, third again a Mighty Swing pack, boss spawns when it dies); SywoGG t=82 to 170 (Gladiator with slaves, Necromancer with fiends/skeletons, last pack); Imperfect Advice t=43 to 64 (gladiators and a woman with skeletons); WildStarESP https://www.youtube.com/watch?v=UrlYsh-99vs t=113 to 141 ("eliminate three of the main enemies of the three gladiators and then the boss fight is activated"). Agreed by four clips. Table 445 count 3 matches (3.3). Pack members and spell names: Gladiator Mighty Swing cleave, interrupt needs three armor removals "because he has two" (SywoGG t=96), Necromancer summon cast and a second cast "under 25% HP" (SywoGG t=122 to 160).
* **Announcer lines** (Observed, beta audio): "You wish to test your steel in the Blood Pit? Very well, prepare yourselves for the slaughter" (Zap-Robo https://www.youtube.com/watch?v=CYIzX9yx0Og t=335), between waves "Only the strongest can survive the Blood Pit. Prepare yourselves for another battle" (Gamesnatch https://www.youtube.com/watch?v=HZAhm-T5JTA t=36 and t=120), "You have proven your steel, but now you must face Grond the Corpsemaker" (Zap-Robo t=375), after Grond dies "That was quite a show ... Are you ready to venture forth into the Fortress?" (Zap-Robo t=420; SywoGG; Ammaross t=1032). Not found in `en-US.bin` (3.7). Creature2 names exist: "Blood Pit Announcer" 39945, 46165, 46166.
* **Cinematic**: no clip's captions mention an entry cinematic; the repo queues an `IRuinsOfKelVorethEnter` cinematic on trigger owner 445 with no payload. This pass found no evidence for or against it (frames not viewed).

### 2.6 Voreth Battlesworn and Darkwitch Osun elites; optional objectives and challenges

Objective rows read from `PublicEventObjective.tbl` (event 161; count, ObjectId, FailureTimeMs, medal points; texts are `LocalizedText`):

| Id | Short text | Type | Count / ObjectId | Notes |
|---|---|---|---|---|
| 444, 446, 449, 453 | Defeat Grond, Drokk, Trogun (449 type ScriptWithoutCount), Darkwitch Gurka (453 optional, category 1) | KillTargetGroup / script | TG 3841 / 3842 / (script) / 3850 | |
| 445 | Fight your way through the Blood Pit | KillTargetGroup | 3, TG 3972 | TG 3972 is a **race group** (TargetGroupType 12, races 184 and 278), not a creature list |
| 446/847 | 847 "Dodging the Defense": defeat Drokk without anyone hit by the exo-lab defense systems | Script | none | |
| 447 | Kill Battlesworn and Darkwitch Osun | KillTargetGroup | **5**, TG 3909 (32555/32556 Voreth Battlesworn, 32618/32619 Voreth Darkwitch) | |
| 448 | Kill Mechano Slavers and Eldan constructs | KillTargetGroup | 11, TG 4973 | |
| 450 | End the suffering of slaves | KillTargetGroup | 18, TG 3905 | |
| 451, 456, 459, 461, 869 | Burn war supplies (10), Eldan data storage (6), exo-lab defenses (5, hidden-flagged), forges (6), hidden jump puzzles (4) | ActivateTargetGroupChecklist | TG 3916, 4422, 4022, 3903, 4028 | |
| 845 | Deathless in the Dungeon | Script | none | |
| 846 | Kill as many Voreth Osun as you can before time expires | KillTargetGroup | **15 within 600000 ms**, TG 3972 | same race group as 445 |
| 975 / 976 / 977 | Bone-Dancing With Grond / The Sky Is Falling (Homing Barrage) / Trogun Man! (Essence of Primal Fire) | Script | none | text 314574 to 314583 |
| 3202 | Timer Bonus: complete within 40 minutes | type 25 | none | medal points 2500 |
| 5344 | Kill Enemies within the Ruins of Kel Voreth | Script | 18 | |

Video against these rows:
* **Count 5 for 447 agreed**: Vhalor https://www.youtube.com/watch?v=PMeSNlGd1tE t=643 to 660 ("the Battlesworn and Darkwitch ... we end up killing five of them anyway").
* Battlesworn elites need one or two interrupts (Vhalor t=761); SywoGG t=1661 to 1693 (disarm cast and an AoE cast, both interruptible). Voreth Darkwitch: Rupture must be interrupted and a damage-over-time needs a dispel (Vhalor t=786 to 826, SywoGG t=1693 to 1774, Imperfect Advice t=1028 to 1051, Ammaross t=3393 to 3958 "two hits and she can kill someone"); the Weekly host says her ability "at 50%" needs an interrupt (https://www.youtube.com/watch?v=Z7a3ijKCp8k t=2405).
* **Optional objectives come in sets**: Carbine stream t=936 to 947 lists on entering Digsite Scar: slavers and constructs (448), Darkwitch Gurka (453), Dodging the Defense (847), and "killing the slaves will also update one of our optional objectives" (450, t=1063). Vhalor t=243 to 283: you can get "anywhere from one to four, a total of four all the way through". SywoGG t=1487 to 1523: "dungeon quests will update ... we only got one extra: Osun elimination" near the last resurrection point, i.e. **846 appears in the last section, not at the Blood Pit**; the Carbine stream t=2384 to 2397 shows the timed challenge ("two minutes left") before Trogun. The repo's event script never activates 846, 845, 975 to 977 or 3202.
* Medals need all optionals (Carbine stream t=1348 to 1382); clear times seen: Vhalor about 27 min, SywoGG about 29 min (Silver/Gold runs), ZybakTV gold run title 23:20 (https://www.youtube.com/watch?v=eZ3slgUF94A).
* Laser challenge 847: lasers can be switched off at panels (Carbine stream t=905 to 926; SywoGG t=528 to 587) and stop after Gurka (SywoGG t=575).

## 3. Row binding

### 3.1 Creature2 ids (read from `Creature2.tbl`; column 9 is `Creature2TierId`, column 7 `Creature2DifficultyId`)

| Creature | Tier 1 id | Tier 5 id | Notes |
|---|---|---|---|
| Forgemaster Trogun | **32531** | **32533** | closes the first packet's open input. The repo's event script spawns 32531 only; Trogun's script filter is by script name only; 32533 is in no filter. |
| Grond the Corpsemaker | 32534 | 32535 | TG 3841; repo filter has both |
| Slavemaster Drokk | 32536 | 32539 | TG 3842; repo filter has both |
| Darkwitch Gurka | 33049 | 33050 | TG 3850 |
| Voreth Battlesworn / Voreth Darkwitch | 32555 / 32618 | 32556 / 32619 | TG 3909 |
| Bloodsucker Flea | 33837 | 56485 | summoned by Scratch Itch 58769 (3x) and 63129 (4x); Bloodspawn Flea 56575 (tier 5) by Bloodburst 63099 |
| Blood Pit Gladiator, Voreth Gladiator, Blood Pit Necromancer, Skeleton | 51068, 51453, 50991, 50998 | 51069, 51454, 50992 | which of these are in this instance is not established |
| Blood Pit Announcer | 39945, 46165, 46166 | | |

Hypothesis, not established: tier 1 is Normal and tier 5 is Prime. Support: each boss has exactly one tier 1 and one tier 5 row; achievements are split Normal/Prime; the Scratch Itch pair (3 fleas with a tier 1 flea, 4 fleas with a tier 5 flea) matches the clip split of three (normal and one 2015 clip) against four (two 2014 Veteran clips). Against: moepork (Veteran, 2015) and Pretend Like We say three. The three main bosses share `Creature2DifficultyId` 12 in both rows (Gurka 11, the elites 10); the tier table is a stat-multiplier ladder, so the tier ids may be level-scaling tiers rather than a difficulty switch.

### 3.2 TargetGroups and objectives

See the objective table in 2.6. TG 3972 (Blood Pit and 846 timed kills) is a race group 184 and 278 (184 is the race of Trogun, Drokk, Battlesworn, Darkwitch, the Blood Pit Gladiator and Necromancer; 278 is Grond's), so any kill of those races counts. Blood Pit Slaves (races 69 to 78) and skeletons (281) do not, which matches the clips: three leaders of the three packs.

### 3.3 Communicator messages (Mapped text to message id; ordering Observed)

`CommunicatorMessages.tbl` ids and texts: 3943 (text 365393, "What's this? Looks like the Osun have unearthed an exo-lab ..."), **3946 (365531, "By the Scions, look at that mechanical monstrosity! Somehow it's controlling Eldan constructs ... In the name of Dominus ..." ; the Drokk room entry)**, **3947 (365533, "You fought like Azrion himself! Now comes the hard part. The Osun have rediscovered how to forge exanite ...")**, 3948 (365534, "Light of the Luminai! ... Osun savages ... built this fortress"), 3949 (365538, "These Osun are strong ... destroy their forges"), 3950 (365540, "Watch yourself. Battlesworn warriors and Darkwitches ahead"). Clips (Dominion voice) confirm the order 3946 on entering Drokk's room (Carbine stream t=1432, Pretend Like We t=509, Zap-Robo t=2583), 3947 right after Drokk dies (Havok https://www.youtube.com/watch?v=flZ6DPHkeTs t=471 to 481, Chief Sarcan t=2249, Zap-Robo t=4122, Jandabear t=730), then 3948, 3949 (Zap-Robo t=4560, Chief Sarcan t=2929). The repo's `DrokkMessageTriggerScript` (owner 446: AvraDarkos6, ToricAntevian5 = 3946) matches the clip observation; `TheExaniteForgesMessageTriggerScript` (owner 449) sends only AvraDarkos8 (3960), has no Toric partner, and the clips show Toric 3947 and 3949 at the corresponding places: not mapped in the repo.

### 3.4 Spell4 rows (all Correlated; none Mapped). Anchor strength is stated per row.

Method: `instance_spell_names.py`, `instance_spell_rows.py`, `instance_tbl_scan.py`, plus a throwaway script listing text-id columns of Spell4 rows and the Spell4Effects rows of each candidate (effect names from `SpellEffectType.cs`). The Spell4 "cast time" column was not used. Durations below are Spell4Effects parameters (`NpcExecutionDelay`, `CCStateSet`, tick/duration columns), whose meaning is unverified and which are not measured gaps.

Real anchors (text id referenced from a Spell4 row, or an internal-name tag):
* **Tracking Beacon 63237** references text 536695 "Slavemaster Drokk has made you the focus of his Destructo Constructs!" (col 64). Sibling versions carry the same sentence: 39293 (text 467547) and 73248 (596295). 63237 proxies after 2000 ms to 39293, which is a 15000 ms damage-over-time with 1000 ms tick (matches "tick for high damage"). Clip support: Bog Otter t=302 to 327 (reads the on-screen sentence), Steve Mayne t=1746, Havok t=348 ("Beacon"). **Tier: Correlated (text anchor plus two clips).** Which of 39293/63237/73248 is live in 16042 is unknown.
* **Drokk interrupt-armor tags**: Spell4 39292 `Drokk - Sets Interrupt Armor` (UnitPropertyModifier on property 44 = InterruptArmorThreshold, value float 2.0) and 39290 `Drokk - Removes Interrupt Armor - Triggers MoO` (CCState 9 Vulnerability, 7000 ms), Spell4Base text 318326 and 318328; also 70002 `Drokk - Fail`. Neighbours: Enslave 39201/39274, Tracking Beacon 39293, Homing Barrage 39342. **Correlated (internal tag).** Consistent with moepork's "chain-cast then interruptible" and the moment-of-opportunity design (Carbine stream t=1442 to 1465); the value 2.0 and the exact trigger are not verified.
* **Objective-text anchors (not Spell4 references)**: Homing Barrage 39342 and [HIDDEN] 70857 (objective 976, achievement 301101); Essence of Primal Fire 58770 (objective 977: text "Infused with Primal Fire ... increased", col 63 text 507895, stacking UnitPropertyModifier for 20000 ms); bone cage (975). **Correlated.**
* **Spell4 38762** `[DEPRECATE] Select Foe` references text 311988 "Forgemaster Trogun is chasing you!" (col 63), a 10000 ms Fluff; Spell4 59461 (base 38762) is the non-deprecated sibling named "Unknown". **Correlated (weak)**: fits "Volcanic Strike tracks the tank" (Vhalor t=956).
* Grond banners 302425 "Grond has a nasty itch!" and 344233 "Grond goes into a murderous frenzy!": no table references them, so they are script-fired text ids; they match Scratch Itch and Blood Frenzy by name (below).
* Gurka text rows: Spell4 63506 `Afflicted Soil` col 64 text 540323 "The ground holds you in place while the darkwitch saps your lifeforce."; 63508 and 63511 `Blinding Dark` reference "Blinded and Subdued by overwhelming darkness." (544411 to 544414 and 540351/540356 to 540358); Spell4Base names at text 540324 to 540326 and 540347, 540352. **Correlated (weak) to Gurka**: names Deadly Defilement and Blinding Dark are spoken in clips (SywoGG t=619 and t=666), Afflicted Soil is not.

Name and effect-graph bindings (no text anchor; the effect graphs are internally consistent with the guides but do not count as binding under the packet rules, so they stay **Correlated (weak)**):
* **Volcanic Strike 58829**: Damage, Camera Shake proxy, CCState 8 Knockdown 3000 ms, proxy to `Volcano` 58830, NpcExecutionDelay 2000 ms; knockdown text 515346 "Knocked Down for ...s". Better fit than 63106 and 51970 (same name, Damage plus RavelSignals only, no CC or volcano proxy). Name in at least five clips.
* **Forgemaster's Call 52512** (unique name, cooldown column 7000): proxies 58751 `Summon Spheres of Primal Fire` and 59762 `Exanite Shards`, NpcExecutionDelay 1500 ms. Fits both the orbs and "also shoots shards" (but the table row carries both proxies, so the phase gating is not in this row).
* **Exanite Weapon 38802** (cooldown column 17600): NpcExecutionDelay 13200 ms, proxy to Exanite Shards 51859, proxy to 52521; `Exanite Weapon` 52302 is the stacking Armor/Brutality buff ("Abilities enhanced", text 470305), 52303 `Exanite Power`. Fits the forge channel. Exanite Shards other rows: 51860 (damage tick 2000), 58916, 58948, 58986, 59762, 63156.
* **Surge of Primal Fire 58767** (name only in moepork; Damage and SpellForceRemove), with `Aura of Primal Fire` 58764 and `Bad Aura of Primal Fire` 58777 (RavelSignal, Fluff, proxy 60387) and Essence of Primal Fire 58770 in the same id block 58764 to 58777 (adjacent `Scratch Itch` 58769, `Darkwitch Totem` 58765). The challenge text says the stacking buff is Essence of Primal Fire, so moepork's name for the buff is probably wrong or refers to the burst.
* **Scratch Itch 58769** (cooldown column 10134; 3 SummonCreature, Bloodsucker Flea 33837) and **63129** (cooldown 5134; 4 SummonCreature at +1000, 1100, 1200, 1300 ms, flea 56485, duration 5000). Both rows are the same boss ability in two versions; clips agree with each count (3.1).
* **Bloodburst 63099** (Scale 2.25, Damage, ForcedMove, SummonCreature Bloodspawn Flea 56575, self Damage); **Blood Frenzy 63128** and 38051 (UnitPropertyModifier x3, text 535604 "Increases Damage Dealt by ... per stack", NpcExecutionDelay 1500); heard as "blood frenzy" (imbonked t=227) and "murderous frenzy" (banner).
* **Bellow 63109** (Damage, proxy 50232: CCState 24 Subdue 8000 ms plus Knockback, NpcExecutionDelay 1500; eight other Bellow rows exist), **Thrash 63121** (proxies 63123 Damage plus ForcedMove, and 48829; NpcExecutionDelay 1000; a ForcedMove parameter 5500 on the caster), **Mutilate 63209** (proxy 37223: ForcedMove, Knockdown 1500 ms at +200 ms, Knockback, four Damage; its own row 37223 is also named Mutilate). Same-name rows exist at many ids, so these are weak.
* **Suppression Wave 63119** (NpcExecutionDelay 700, proxy 63120: Damage, Knockdown 3000 ms, Fluff 7000 ms; proxy 63131 `Suppression Field`: Damage tick 1000, Snare), **Lockdown 63143** (Tether CCState 20 for 15000 ms, Damage tick 1000, proxy 63145), **Enslave 39274** (Tether 45000 ms, removes Lockdown 63143, DespawnUnit), **Bombshell Target 63144/63145** (RavelSignal only), **Exanite Cuts 63107** (text 534132 "Increases all damage taken by ...", Damage tick 1000 for 4000 ms; not named by any clip, so unconfirmed). Fits knockdown/tether/puddle statements; no clip gives the durations.
* Other candidate names: Wail of the Darkwitch 50304 (Necromancer, Vhalor t=30), Trogun Summon 58725 (RavelSignals), Gurka's Wrath 81564 (Fluff), Rupture and Mighty Swing have dozens of unrelated same-name rows and are not bound.
* No `[KV]`, `[RoKV]` or similar tag exists; the only dungeon internal names are the `Drokk - ...` tags and `e444 / e448 / e456 - ... - Kel Voreth` Creature2 and door names (`e444 - Blood Pit Gate`, `e444 - Grond's Gate`, `e444 - To Act 2 Gate`, `e448 - Mechano Slavers - Bots`, `e456 - Eldan Schematics`).

### 3.5 Text-table facts used above

`LocalizedText` 300958 to 301111 (Normal/Prime medals and boss achievements), 313569 to 313631 (dungeon cards, level 25 and Prime level 50), 314574 to 314583 (challenge texts), 289931 to 289933 (gate door names), 333679 'Project "Corpsemaker" about to commence!' (unreferenced, purpose unknown).

### 3.6 Spell4 id block note

The 63090 to 63260 block already shown to be shared stays shared; the Kel Voreth rows are spread over several blocks: 39280 to 39345 (Drokk tags, Beacon, Homing Barrage), 52302/52512/58751 to 58777 (Trogun forge and orbs, Scratch Itch 58769), 58829/58830 (Volcanic Strike, Volcano), 63099 to 63209 (Grond and Drokk, later versions), 63237 to 63241 (Tracking Beacon), 63506 to 63511 (Gurka). Id adjacency is not a per-dungeon signal.

### 3.7 Yell search

Twenty-plus phrase fragments from the yells above (for example "arm the Osun", "ancient forges", "this hammer", "shattered corpses", "test your steel", "oppression protocols", "Only fools") returned **no** `en-US.bin` text. Only the Communicator messages (3.3), the two Grond banners, the objective texts and Creature2 names were found. The boss and announcer VO lines are therefore not text ids in this table set; their order and trigger can come only from clips, as for Stormtalon.

## 4. Timing

Not measured as cast intervals. Subtitle cue starts are seconds into the clip with 1 to 3 s caption latency, and fight pace depends on group damage, so these are phase-order observations, not timers:
* Trogun, first Forgemaster's Call to the 50% forge phase: Vhalor t=930 to 969 (about 39 s, Veteran), Weekly t=2737 to 2782 (about 45 s, normal), Carbine stream t=3232 to 3277 (about 45 s, normal, boss AI bugged). Spread 39 to 45 s from three clips; DPS dependent.
* Grond, first Thrash to Bellow to adds: Vhalor t=125 to 140; to frenzy and Mutilate about 160 to 173 (veteran, high DPS).
* Table values that could later be compared with 2 Hz telegraph measurements (unverified meaning): Exanite Weapon channel 13200 ms; Volcanic Strike delay 2000 ms; Scratch Itch flea duration 5000 ms; Lockdown tether 15000 ms; Enslave tether 45000 ms; Tracking Beacon 15000 ms; Suppression Wave 700 ms delay.
A two-clip 2 Hz telegraph measurement was not attempted (720p, subtitle-only pass).

## 5. Open inputs

* A second source for Grond 75% and 50%, and a third for Drokk 85 / 65 / 45 / 25%. Search target: the 2015 to 2016 level 25 / Prime clips with commentary (none found among the 150 beyond SywoGG, moepork, Steve Mayne, Pretend Like We, Jandabear). A frame pass reading the boss health-bar percentage at phase banners in two Prime clips could settle the numbers (the boss health bar was visible in the Stormtalon frame pass; whether the percentage is legible at 720p is untested).
* Which of tier 1 / tier 5 is Normal and which is Prime, and which Scratch Itch (58769 / 63129), Tracking Beacon (39293 / 63237 / 73248), Blinding Dark (44315 / 51974 / 63508 / 63511) and Volcanic Strike (51970 / 58829 / 63106) rows are the 16042 ones. A native or packet read of the creature spell list is the only source; no table links creature to spell.
* Whether Grond really has infinite interrupt armor on Prime (moepork) versus two (dev stream, normal 2014); the Trogun four.
* Interrupt-armor values for Drokk (tag value 2.0 unverified) and which Drokk casts they gate.
* Cast intervals for every boss; the number of orbs (seven); Drokk bomb timer (about 10 s); number of beacons per phase; Suppression Wave puddle duration; Thrash rotation direction.
* The activation conditions for objectives 845, 846, 847, 975, 976, 977, 3202 (script-type, no object id) and when 846's 10-minute timer starts (clips: last section).
* Entry cinematic existence; exact Blood Pit announcer trigger points.
* Trigger placement and the Toric / Avra partner ids for the 3947 and 3949 messages.
* Creature2 membership of the Blood Pit packs in this instance.

## Verdict

Trogun is the best-supported boss: the 50% forge phase is agreed by four clips (three post-launch, two from 2015), four interrupt armor by one second clip, the orb and shard sequence by five, and the table adds a real challenge-text name (Essence of Primal Fire) and a 13.2 s channel row. Grond is **not** safe to implement from the first packet: 75% has no second source, 50% for Mutilate is contradicted by three 2014 clips (20 to 30%), and the interrupt armor claim is contradicted by the dev stream. Drokk's phase order is difficulty dependent and the four thresholds have no second source (the clips give 80% and about 30% and 50%). Gurka's three-ability skeleton is agreed by four clips; the Blood Pit's three-pack gate by four clips and by table row 445. The strongest binding anchors are Tracking Beacon 63237 (text), the `Drokk - ... Interrupt Armor` tags, the three challenge-text names and the Scratch Itch 3-versus-4 summon split.

Allowed next slices without invented values, in order: (1) Trogun disabled phase tracking with the 50% forge handler and the orb/shard sequence, challenge 977 hook on Essence of Primal Fire, creature ids 32531 and 32533; (2) Gurka three-ability skeleton as disabled placeholders; (3) activation of 846 in the last section and 975/976/977/847/845 as script objectives, keyed to the table texts; (4) Drokk only after a frame pass on the thresholds and a decision on normal versus Prime ordering; (5) Grond only after 75% and 50% are re-measured. None started.

## Not verified in this pass

No frame was viewed. Captions are auto-generated and several quotes are interpretive readings (named in the text where it matters, for example "the three use cannot be killed"). The identity and role of the Carbine and WildStar Weekly speakers are taken from their speech. Dates are `upload_date`, not recording dates. Spell4Effects parameter meanings (db columns, `NpcExecutionDelay` durations, the value 2.0 of the interrupt-armor modifier, Scale 2.25) were read with the repo's helper script and the `SpellEffectType` and `CCState` enums but not checked against the client. The claim that tier 1 / tier 5 mean Normal / Prime is a hypothesis. No claim in this file is Mapped or Verified, and none is a retail parity claim.
