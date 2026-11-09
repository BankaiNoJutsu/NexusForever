# Lead verification of three bosses: Blade-Wind, Trogun, Kuralak (2026-11-09)

Narrowed goal chosen by the user on 2026-11-09 ("Narrow to a few bosses"): verify and refine only these three bosses against retail clips. Method: the lead searched the subtitles of every retail-dated (non-empty upload date before 2018-11-28), subtitled clip whose title or description names the boss, then read the cited passages. Evidence is Observed (auto-generated subtitles, no frame viewed, cast bars are not legible at 720p); nothing is retail parity; cast intervals remain estimates. Tools: scratchpad `boss_grep.py` and `vid_dump.py` (same corpus queries as `scripts/instance_cue_grep.py`).

## Blade-Wind the Invoker (Stormtalon's Lair, creatures 17160/33405)
Searched 7 clips. Lead-read facts:
* **Phase 2 at about 60%: agreed by two independent post-launch sources.** 2014-06-09 (Seatin Man of Legends) https://www.youtube.com/watch?v=P7eQtXKxVlM at about 66 s: "when Blade-Wind reaches about sixty percent"; 2015-09-28 (moepork) https://www.youtube.com/watch?v=8M53-Kf90a4 at about 36 s: "Phase 2 begins when the boss reaches 60%". The "50%" an agent reported was not found in any of the 7 clips (the only nearby "fifty" is an unintelligible caption fragment at 69 s of the 2014-06-09 clip). This resolves the earlier "disputed" label: the lead found no clip that says 50%.
* **Two interrupt armor:** moepork 2015 at about 12 s; single source.
* **Thundercross** (called "lightning cross" in the 2014 clip) in phase 1 and phase 2: both sources.
* **Electrostatic Pulse in phase 3:** moepork 2015 and a 2014-05-24 beta run https://www.youtube.com/watch?v=TshOwD4sWts at about 244 to 304 s ("a big telegraph that takes the entire room"). Three casts in a row: moepork only.
* **Interrupt, interrupt, dodge** rotation: 2014-06-09 at about 66 s, compatible with two armor.
Effect on code: the Blade-Wind phase placeholder and the provisional plan now say 60% is two-clip agreed (the placeholder stays disabled and not wired to behaviour).

## Forgemaster Trogun (Kel Voreth, creatures 32531/32533)
Searched 11 clips. Lead-read facts:
* **Forge retreat at 50%:** moepork 2015-10-13 https://www.youtube.com/watch?v=_K6WIpU09G4 (about 80 s, "at 50% Trogun will run back to the forge and begin casting Exanite Weapon") and the 2014-05-25 beta run https://www.youtube.com/watch?v=K4_L4d4kl0s at about 593 s ("second quarter ... at about fifty percent you will again return to the forge"; pre-launch, so Correlated). Two sources read by the lead; the other two agent-reported clips were not found by the title search.
* **Four interrupt armor:** moepork only. **Volcanic Strike** ("volcanic hammer" in a 2014-07-13 Dulfy veteran guide https://www.youtube.com/watch?v=bRC17Hepq5k at 91 s) and **Forgemaster's Call** ("I call upon the power of my ancient forges", Dulfy at 129 s) are named; the guide's timing gives no cast interval.
Effect on code: the evidence wording in the Kel Voreth plan and the Trogun placeholder now says two clips read by the lead.

## Kuralak the Defiler (Genetic Archives, creature 52969)
Searched 13 clips; the only mechanically complete narration is moepork 2015-11-13 https://www.youtube.com/watch?v=qVzVREgWB7A, read by the lead through about 230 s:
* **Phase 2 begins at 73%** (81 s). The agent-reported 72% was not found in any clip by the lead; the plan now uses 73% (single source read by the lead).
* **Phase 1:** pillars explode then Vanish into Darkness (boss stealths behind an outer pillar, must be found and interrupted or it wipes; zero interrupt armor); lasers rotate around pillars all of phase 1; **Putrid Discharge** "frequently" on three random players (high damage and a massive slow), continuing through the whole encounter (68 to 95 s).
* **Phase 2:** lasers fade, pillars randomly explode and leave telegraphs; **Cultivate Corruption** makes an egg that must be interrupted or the player dies, interrupters get the **Chromosome Corruption** debuff (+100% damage and healing done, heavy damage over time); **Outbreak** "occasionally" kills anyone standing in it unless interrupted (about 133 s); **DNA Siphon** "occasionally" stuns the current tank (about 143 s); egg counts 1, 2, 4, 8, 16 per Cultivate Corruption (about 221 to 228 s).
* **Not found:** Tainted Ventilation (an agent-selected row) is not mentioned in the guide.
Effect on code: the Genetic Archives plan was changed: phase 2 at 73%; phase 1 casts only Putrid Discharge; phase 2 casts Putrid Discharge, Cultivate (`[GA] e412 Cultivate Base` 60546, the egg cast, replaces Chromosome Corruption which is the resulting debuff), DNA Siphon and Outbreak; Tainted Ventilation and the Vanish row stay out.

## Tests
After the change the filter covering all provisional encounter tests plus the Blade-Wind, Trogun, health-phase and invulnerability tests passed 108, 0 failed (one scope; not added to earlier counts). The Kuralak threshold assertion was updated from 0.72 to 0.73 deliberately because the evidence changed.

## Still unverified for these bosses
Every cast interval and first delay (estimates); which creature casts which row; Blade-Wind's phase 3 and disciples; Trogun's orbs and volcanoes; Kuralak's egg counts, pillar geometry and enrage; frame-level confirmation of anything; behaviour in a client.
