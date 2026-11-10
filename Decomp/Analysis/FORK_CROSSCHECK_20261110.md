# Cross-check of the open spell and prerequisite items against NexusForever forks (2026-11-10)

Purpose: see whether a fork already solved an item this branch left open. A fork is **another implementation, not evidence**: where its author cites a client address the claim was re-read
here; where it only agrees with an inference made here, that is two inferred readings agreeing. No retail parity claim follows from anything in this file.

## 1. What was surveyed
`NexusForever/NexusForever` has 216 forks. The 70 pushed since June 2024 were listed with their branches (GitHub API). Branches whose names touch spells, damage or prerequisites were fetched
read-only under `refs/remotes/forks/*` and compared with `upstream/game_rework`:

| Fork and branch | Ahead of upstream | Relevant content |
|---|---:|---|
| `cmcnutt0000`, `prerequisite-client-parity` (2026-10-25) | 136 commits | a rebuild of the spell effect handlers on the `spells-v2` design and a prerequisite pass that cites client addresses |
| `onlysubs90`, `add-armor-pierce` | 2 | attacker armor pierce rating and strikethrough in `DamageCalculator` |
| `derdotte` `Prerequisites`, `bobbygriz` `codex/prerequisite-evaluation-context`, `Musaji` `spells-v2-gr-musaji` | 78, 74, 26 | `spells-v2` descendants; nothing on the open items beyond what `cmcnutt0000` has |
| `djjuno` `fix/prerequisite-checks`, `LiminaWayfarer` `Prereq-checks-rework`, `Kinsmir` `feature/prerequisite-update`, `dio85` `damage-calculator-gr` | 1, 4, 0, 0 | small or already upstream |

Not opened: `krakal` (about 70 topic branches of packet models) and `kirmmin` (2019 era).

## 2. Findings by open item

### Aura spells (tick-only rows) - new, checked against the table
The `spells-v2` line has a spell class per `Spell4Base.castMethod`, and `SpellAura` (cast method 10) re-triggers every effect row with a tick for as long as the spell lives, ending at
`spellDuration` when that is finite. The table agrees with that classification better than the split recorded in `SPELL_EFFECT_PROXY_20261110.md` 1c did:

* All 1,387 spells with cast method 10 have a spell duration: 700 finite, 687 `0xFFFFFFFF`, none without.
* Of the 564 effect rows with a tick, no duration of their own and a finite spell duration, 559 belong to cast method 10. The three tooltip witnesses used for the pulse rule (Medic Restrictor
  `38287`, Warrior Polarity Field `38975`, Medic Nullifier `58868`) and Medic Danger Zone (`58912`, permanent) are all cast method 10.
* Of the 828 tick-only Proxy rows, 313 are cast method 10 (154 permanent, 159 finite) and 514 are cast method 0 with no spell duration.

So the rule implemented here (a tick-only row pulses for the finite duration of its spell) is, in the client's own terms, "an Aura spell ticks its rows for its duration". Cast method 10 is named
`Aura` in `SpellCastMethod`. **Permanent auras (687 spells) were implemented in a later commit the same day, in part**: `Spell.GetEffectPulseDuration` pulses the tick-only rows of a permanent
Aura until the spell ends, for effect types that leave no state behind (Damage, Heal, Proxy, VitalModifier, SapVital: 329 of the 385 such rows), and drops each pulse's target record after it is
sent. Two server-side guards that come from no source end it: the caster dying, and the same caster casting the same spell again. Limits: creature casts select no telegraph targets on this
server, so a creature aura with area rows still reaches nobody; the 56 rows of other effect types execute once as before. When this note was first written the text read: The fork shows the shape of the model (the spell stays executing and re-triggers on a timer); doing the same
here needs the spell to stop accumulating per-pulse target effect records first, otherwise a permanent aura grows without bound. The 514 cast method 0 rows remain unexplained by either code base;
the fork's handling of them re-arms a timer that is never enqueued, so they fire once there.

### Proxy DataBits06 - the fork disagrees on which unit is tested
The fork reads word 06 as `PrerequisiteId` and evaluates it on the **proxy target**, skipping it when the target is not a player. This branch evaluates it on the **caster**
(`SPELL_EFFECT_PROXY_20261110.md` 1e). The table favours the caster: 25 rows that target foes or an area carry ability-book conditions (types 59 and 233), for example "Rune Set - Medic - Healer -
Resource Probes" (`82553`, area), which gates each proxy on one of the Medic's own probe abilities being unlocked; tested on each ally in the area that could only pass for other Medics. Neither
reading has a native or retail witness. The fork does not read word 07. Words 03 and 05 are unread in every fork looked at.

### Quest objective types 13 and 18 - the fork made them checklists
`cmcnutt0000` cites the same client function (`0x1405517A0`) and stores progress for types 10, 13, 14 and 18 (and 27, 28 with activate mode 1 or 2) as a bit mask of `QuestChecklistIdx`, complete when
the number of set bits reaches the count. That is the reading the three native consumers recorded in `PREREQUISITE_CHECK_REVIEW_20261110.md` point to. Two further observations made while checking:

* The lookup behind types 27 and 28 is `PathSoldierActivate` in the fork; this branch labelled the same call `SettlerMission_LookupById`. Not re-read; one of the two names is wrong.
* The world data that argued against the mask (objectives 21319, 21378, 21332, 21379: creatures `73422` and `73662`, every spawn index 0) looks like incomplete data rather than retail values:
  objective 8029 is also a single creature id (`23379`) with four spawns, and those carry indices 0 to 3.

Changed in a later commit the same day on the owner's decision (see `PREREQUISITE_CHECK_REVIEW_20261110.md`). When this note was written it read: changing it is a quest-progress behaviour change that also needs the 13 spawns of those two creatures given distinct indices, and the producers for types 13 and 18
(`SpellQuestObjectiveUpdater`, `InteractionObjectiveUpdater`) to pass the creature's checklist index instead of a count of 1. Left as a decision for the repository owner.

### Prerequisite type 88 - a hypothesis, not confirmed
The fork cites the live case body (`0x14049D470`, the list at second unit `+0x2b8`) and reads it as "the subject is a pet of the target" (`subject.SummonerGuid == target.Guid`, or the
target player's vanity pet). Checked natively: `Lua_Unit_GetUnitOwner` (`140661d20`) reads the owner id of a Pet entity (type `0x18`) at `+0x2b0`, so the neighbouring fields are pet-related on that
entity type, but the client's own pet list (`GetPlayerPets`, `1406f80e0`) walks a map on the game object (`+0x6fa8`), not `unit + 0x2b8`. What the list holds is still not identified. Recorded
as the leading hypothesis; the one retail row is unreferenced. Not changed.

### Prerequisite type 292
The fork compares a count of 0 with Value for players ("NexusForever has no Primal Matrix, every node is 0"), which is what this branch does since commit `354cc960`. No fork has allocation state.

### Armor pierce
`onlysubs90` applies the attacker's armor pierce **rating** by multiplying the victim's armor by `1 - pierce` before the mitigation formula, the same form assumed here for the per-row fraction
(`SPELL_EFFECT_DAMAGE_WORDS_20261110.md`). Agreement between two assumptions. That fork does not read Damage word 04, and this branch does not read the rating; the two would have to be combined
and nothing shows how (sum, product or maximum).

### Not found in any fork
Breath, the type 148 lookup miss, Proxy words 03 and 05, Damage word 05.
