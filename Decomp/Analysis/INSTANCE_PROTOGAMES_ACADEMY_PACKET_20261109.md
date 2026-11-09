# Instance packet: Protogames Academy (world 3173, PE 667) (2026-11-09)

Phase B packet per [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md). Evidence tier: Observed (subtitle reads by an agent; no frame viewed) and Correlated (client text/table scans). Agent-written, not re-derived by the lead and not re-viewed. No code, no retail parity claim. Corpus ids changed on 2026-11-09; clips are given by URL.

Repo scaffold: `Source/NexusForever.Script.Instance/Dungeon/ProtogamesAcademy` (kill credit and objective routing; five scripts filter by script name, two final-leg scripts by Creature2 id; no boss ability logic). Level-10 five-man event dungeon, no medal system (SywoGG guide: "does not use the metal rating system"; PTR/livestream remark that it teaches interrupt armor and moments of opportunity).

## 1. Footage
Dated clips about 45 for "Protogames" in total (survey count, shared with Ultimate Protogames), of which about 30 are Academy, 2014-12-17 (content drop) to 2018-11-14. Everything is post-launch (the dungeon shipped December 2014). Nearly all runs are normal or veteran-labelled level 10 with commentary focused on how to play, not on numbers.

| Role | Date | URL | Length | Subs | Notes |
|---|---|---|---|---|---|
| Content Drop #4 feature video (channel Readycheck, appears to be a re-upload of the official feature video; origin not verified) | 2014-12-17 | https://www.youtube.com/watch?v=2plrOpjZhEw | 799 s | yes | Voice-over by the in-game host character; quotes the room scripts ("learn to interrupt and dodge", "50% more damage" moment of opportunity, Wrathbone intro). |
| Dungeon guide (SywoGG) | 2015-08-31 | https://www.youtube.com/watch?v=hhahp97rHvw | 749 s | yes | Best boss-by-boss source; all fights in order; filmed on a F2P closed-beta server. |
| Casual's guide (TinyBlueGames) | 2015-05-17 | https://www.youtube.com/watch?v=GU19_yGmqSg | 895 s | yes | Independent commentator, healer POV, first two sections in detail. |
| Normal playthrough (Maximus Grahamicus) | 2016-06-26 | https://www.youtube.com/watch?v=2OWPy717tKk | 708 s | yes | Normal difficulty; reads the host's quotes; independent. |
| "What is the Protogames Academy?" | 2015-09-30 | https://www.youtube.com/watch?v=X-zCpsRz62w | 130 s | yes | Overview of the three games. |
| Gamescom 2014 stream (official team, via Readycheck) | 2014-08-13 | https://www.youtube.com/watch?v=gJaTTyB7t3g | 1018 s | yes | Developer says the level-10 Academy is "based around the rooms" of Ultimate Protogames, mechanics dialed back. |
Other subtitled runs (not read): `D5VZM-Y8BdQ`, `FDmnmRM59_s`, `5hvcXY-lnjU`, `Fafk17va6l4`, `71H6liK5qfg`, `x6KaOJkntqU`, `MB56MdQFKzM`. About 12 are no-subtitle runs.

## 2. Skeleton (Observed; "A" = two or more independent commentators, "S" = single-source)
The dungeon is a sequence of arenas entered through blue circles (no fight starts while someone is outside), no trash, three "sections", teleporters between sections, and a final boss. The host character's quoted lines (Content Drop video and the 2016 playthrough) agree with the commentators: "learn to interrupt and dodge ... don't stand in the red".

Boss order as seen in the SywoGG guide (A for the shape of each fight with the casual's guide and the 2016 clip, but fight-to-name binding is weak, see section 3):
1. Section 1, fight 1: a boss that tethers the whole group and explodes unless interrupted (casual's guide); throws the weapon away / pushes players and has a long cast (SywoGG: "30 seconds"); four interrupt armor, then a moment of opportunity (A: SywoGG and 2016 clip both say 4 interrupt armor, the 2016 clip says "two interrupt armor" on one early boss and "one interrupt armor" on another, so counts differ by fight); afterwards a "tornado" teleports players to the next circle.
2. Section 1, fight 2: telegraph-dodging boss with 4 interrupt armor (3 later), moment of opportunity, teleports away, a maze/laser grid of fire to run through, flaming pillars, and telegraphs on the floor; the 2016 clip calls the boss "her" and says she is interrupted four times and "dodged her attacks" (A for the maze/pillars).
3. Section 1, fight 3: Iruki Boldbeard (named in the SywoGG caption as "rookie bald beard"): DPS phase, then the room fills with a red telegraph and a safe zone must be reached before a "big bang"; a second safe-zone round; then a telegraph on each player to spread out from (SywoGG, casual's guide for the safe-zone mechanic: A).
4. Section 2, three robot fights (class-role lesson): (a) a robot that spawns one bomb to kill, then puts a telegraph on a random player (move it away), plus one interruptible cast (A: SywoGG, casual's guide); (b) a robot with an unavoidable tank telegraph and a pie-shaped red area covering about 75% of the room with one safe zone (A: SywoGG "75%", casual's "one safe zone"), adds that can be cleaved; (c) a robot with a frontal cleave, a shield that takes only 1 damage per hit, spinning robots, and a pillar the boss must be taken to for a stun and moment of opportunity (A: SywoGG, casual's guide "spinning robot adds ... safe zone ... interrupt for moment of opportunity").
5. Section 3, Wrathbone (final, named by the host: "straight from the skeleton-pits of Celestion ... the late, the great Wrathbone"): random telegraphs on DPS that explode into smaller ones; infinite interrupt armor early (2016 clip: "he's got infinite interrupt armor"), an invulnerable phase with fireballs/meteors that must be walked through, a telegraph on the tank that follows and cannot be avoided, a 30 s cast that must be interrupted (SywoGG), and elemental/fire-remnant adds with a timer after which his shield drops (2016 clip: "waiting for his shield to drop ... timer on the elemental swarms"). A: SywoGG and the 2016 clip on meteors, invulnerable phase, shield timer, interrupt.
No health thresholds are stated in any clip read (phase changes are described only as "after the interrupt", "after the safe zone"). All interrupt-armor counts are per-fight single statements, not cross-checked against a table.

## 3. Row binding (Correlated; nothing Mapped)
Client name text (LocalizedText on Creature2 column 4, scanned): Invulnotron 67475 (text 627491; a second row 71082 uses text 687722), Gromka the Flamewitch 67594 (628142), Iruki Boldbeard 67663 (628705), Seek-N-Slaughter 67668 (628775), Icebox Mk. 2 67757 (629569), Wrathbone 67944 (631487), Super-Invulnotron 68096 (633135). The repo scripts already use Creature2 67944 (Wrathbone) and 68096 (Super-Invulnotron, TargetGroup 12361, objective 4499); the others are script-name filtered (`GromkaEntityScript`, `InvulnotronEntityScript`, `IrukiBoldbeardEntityScript`, `SeekNSlaughterEntityScript`, `IceboxMk2EntityScript`), so their creature ids exist only in the table and the ids above come from the text scan, not from the repo.
PublicEventObjective ids in `PublicEventObjective.cs`: 4341 initiate, 4342 Invulnotron, 4495 Iruki, 4496 Gromka, 4497 Seek-N-Slaughter, 4498 Icebox Mk2, 4499 Super-Invulnotron, 4346 Wrathbone, 4500/4501 go-to-next teleporters, 4490-4494 gather.
Fight-to-creature binding: only Iruki (caption), Wrathbone (host line) and, weakly, Gromka (the female fire-pillar boss, "Flamewitch" in the table) can be tied from the clips. Invulnotron/Super-Invulnotron (invulnerable shield, "only do one damage") is the best guess for robot (c) but a guess. The mapping of the other robots to Seek-N-Slaughter and Icebox Mk. 2 is unverified.
Spell4: the helper name search found no distinctive Academy boss ability names (searched Wrathbone, Invulnotron, Gromka, Iruki, Seek, Icebox, Protogames); only "Initiate Protogames Academy" 77481 and "Protostar Intern" rows appear. The Academy spell rows therefore cannot be identified by name; the likely authoring window (December 2014, ids around 74000-78000) was not enumerated. Challenge texts exist only for "Immortal: Protogames Academy" (no deaths, Normal text 631450/631451 and Prime text 695693/695694), so no boss-specific text anchors were found.

## 4. Timing
None measured. The only numbers are: Wrathbone's interruptible cast "30 seconds" (SywoGG, single), the Content Drop video's "50% more damage" during a moment of opportunity (host line, A with SywoGG "50% more damage" statement), and the general interrupt-armor counts above.

## 5. Open inputs
* Boss-name to fight binding (robots and section-1 bosses); a creature-order dump from PE 667's phase/objective table would settle the order (not read in this pass).
* All cast lists, intervals, damage, telegraph sizes, safe-zone geometry and spawn positions: a native read or packet capture; video does not give them.
* Interrupt-armor counts per boss from the Creature2/spell tables (not read).
* Health thresholds: none stated in any clip.
* Normal vs veteran differences: clips cover both labels but nobody compares them.
* Spell4 rows for the Academy abilities (no name anchors found).

## Verdict
Academy footage is plentiful (dozens of subtitled runs) but all commentary is introductory, so the packet has an agreed room-by-room skeleton and a good set of Creature2 name rows, no agreed thresholds, no cast intervals and no Spell4 binding. Academy is a teaching dungeon with simple, repeated mechanics, so after the creature-order check it is the cheapest of the five dungeons here to implement from tables (interrupt-armor counts, moments of opportunity), but it needs those table reads first; a video-only implementation would encode guessed names. Next input: read the PE 667 objective/phase table for boss order and the Creature2 rows' interrupt-armor fields. None started.
