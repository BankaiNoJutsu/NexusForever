# ChangePhase spell effect (0x22): inferred layout and implementation (2026-11-08)

Inputs: build 16042 `DB\Spell4Effects.tbl` (168-byte records, 131,010 rows), parsed directly; the native read in
[PREREQUISITE_24_25_219_NATIVE_READ_20261108.md](PREREQUISITE_24_25_219_NATIVE_READ_20261108.md) for what the client does with the result.
**Evidence tier: Source/SQLite tests plus a table-shape inference. There is no client handler and no retail witness for this effect; do not restate it as retail parity.**

## What the client tells us
The client has no handler for effect type 0x22; the name occurs once as an enum string. The only client-visible consequence is packet `0x0171`
(`PhasesThatPerceiveMe`, `PhasesIPerceive`), which prerequisites 24/25 and `bCanSeeMe` read. So the effect's meaning can only come from the rows.

## The 161 rows
| Shape | Rows |
|---|---|
| DataBits00 == DataBits01 and no clear word | 138 |
| DataBits00 != DataBits01 and no clear word | 6 |
| a clear word, equal in DataBits02 and DataBits03 | 11 |
| a clear word, unequal (`0xFFFA` / `0xFFF2` with add `4` / `13`) | 2 |
| a clear word in DataBits02 only (`0xF0000` twice, the float-looking `0x40A00000` twice) | 4 |

* TargetFlags: 119 rows 1, 32 rows 2, 6 rows 4, 4 other. 40 rows have a `DurationTime` (for example 2,500 ms and 900,000 ms), 121 have none.
* DataBits00 values: 1, 2, 4, 5, 8, 16, 64, `0x100000`, `0x100004`, combinations such as `0x188`/`0x288`/`0x1810`; all small masks like the packet dwords.
* The **clear** words fit a set-bits-to-clear reading in three independent ways: pairs of add/clear words that are complementary within four bits (`3/12`, `5/10`, `9/6`); the exclusive-phase
  form (add `16`, clear `0xFFFFFFEF`, i.e. everything but bit 4); and `4/13` with `0xFFFA/0xFFF2` (clear all of the low sixteen bits except the added ones). **No row has add bits that overlap its
  clear bits** (checked over all 161), so "add then clear" and "clear then add" are indistinguishable in the data.

## Inferred semantics (implemented)
`PhasesIPerceive = (PhasesIPerceive | DataBits00) & ~DataBits02`, `PhasesThatPerceiveMe = (PhasesThatPerceiveMe | DataBits01) & ~DataBits03`, folded over the active changes in the order they were
applied, starting from the login default of 1/1; the result is sent as packet `0x0171` when it changed; an effect with a duration is removed when it expires (and by the generic spell-state removal,
for example on death or dispel); a recast of the same effect instance replaces its record. Only players have phase masks; other targets are ignored.

## Not determinable from the data (open)
* The 138 plain rows (`2 2`, `1 1`, ...) have no clear word and mostly no duration, so with OR semantics they can only widen the mask; consecutive spell pairs such as 2857 (`2 2`) and 2858 (`1 1`)
  look like a toggle between phases 0 and 1, which would need "assign" semantics when no clear word is given. The implementation does **not** assume that. If retail assigned, a player phased by
  such a spell keeps the extra bit here.
* Two rows carry `0x40A00000` (the float 5.0) in DataBits02 (spells 42746 and 75582) and two rows have DataBits04/05 = 1; they are applied as plain clear words and the extra words are ignored.
* The server does not use the phase words to filter which entities or players a client is sent (the entity grid has no phase concept), so today the visible effect is the client-side evaluation of
  prerequisites 24/25 (world hex groups, spells, gossip, items) and `bCanSeeMe`.
* `ChangePlane` (0x85, 16 rows, packet `0x172`) is separate; see [SPELL_EFFECT_CHANGEPLANE_20261108.md](SPELL_EFFECT_CHANGEPLANE_20261108.md).

## Code
`PhaseChange` record and `IPlayer.AddPhaseChange` / `RemovePhaseChange` / `GetPhaseChanges`; `PhaseChangeRules.Fold`; `SpellEffectChangePhaseSemantics`; `SpellHandler.HandleEffectChangePhase`;
lifetime removal in `Spell.BuildLifetimeRemovalAction`; `SpellStateRemovalKind.PhaseChange` removed by `UnitEntity.RemoveTrackedSpellStates`. Tests: `ChangePhaseSpellEffectTests` (17 cases).
The tests were written before the production code but were first run after it (one harness fixture error fixed on the way), not red. Filter ChangePhase|Spell|Prerequisite|Phase|Player: 3,864 cases pass.
