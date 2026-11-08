# ChangePlane spell effect (0x85) and prerequisite 232: inferred layout and implementation (2026-11-08)

Inputs: build 16042 `DB\Spell4Effects.tbl` and `DB\Prerequisite.tbl` parsed directly; the native reads in
[PREREQUISITE_24_25_219_NATIVE_READ_20261108.md](PREREQUISITE_24_25_219_NATIVE_READ_20261108.md) (packet `0x172` stores two dwords at `player+0x6754` / `+0x6758`, only `+0x6758` is read) and
[PREREQUISITE_NATIVE_COMPARISONS_20261107.md](PREREQUISITE_NATIVE_COMPARISONS_20261107.md) (type 232 = `(word & Value) != 0`).
**Evidence tier: Source/SQLite tests plus a table-shape inference. The client has no handler for effect 0x85 and there is no retail witness; do not restate this as retail parity.**

## The 16 rows
* DataBits00 == DataBits01 in every row; DataBits02/03 are zero. 14 rows add `4` (bit 2), 1 row adds `5`, 1 row adds `1`.
* The 14 value-4 rows (spells 37218, 43923, 56975, 58428, 58437 to 58444, 70511, 70512) all have a duration of 3.5 to 8 seconds; 11 of them have a 200 ms delay and DataBits04 = 1 (meaning unknown, ignored).
  A timed bit that is gone afterwards is what a "void" state looks like, and the packet is named `...SpellSlingerVoid`.
* The two no-duration rows (spells 79236 adding `5`, 79237 adding `1`, `Flags` = 2) look like a toggle pair as in ChangePhase; the OR rule below does not make them one.

## Where plane values are consumed
Ten prerequisites contain a type 232 slot: 28187 (Equal 4), 29468 (NotEqual 4), 34027 (Equal 1), 34083 (Equal 2), 38853 (NotEqual 4 plus type 116), and 10672, 16018, 35363, 36098, 37819 (each with a NotEqual 4
slot). They are referenced from `TargetGroup`, `Creature2` and `Creature2DisplayGroupEntry`, `Spell4`, `Spell4Effects`, `Spell4Visual`/`Spell4VisualGroup`/`Spell4Telegraph`, `SpellPhase`,
`AchievementChecklist`, `ClientEvent`, `Quest2` and others. Value 4 in eight of the ten slots matches the value 4 of the timed rows, which is the main support for "this effect feeds that word".

## Inferred semantics (implemented)
`PlanesCanSee |= DataBits00`, `PlanesCanBeSeen |= DataBits01` over the active changes, both words starting at 0 (the client constructor zero-initialises them); the result is sent as packet `0x172`
(`PlanesCanBeSeen`, `PlanesCanSee`) when it changed, and again after add-to-map when non-zero; a row with a duration is undone on expiry and by the generic spell-state removal; a recast of the same
effect instance replaces its record; non-player targets are ignored. DataBits00 is mapped to the word the client reads (`PlanesCanSee`) by analogy with ChangePhase; the rows always carry equal words, so
the data cannot distinguish the two assignments.

Prerequisite 232 (`PrerequisiteCheckPlane`) now follows the native test: `(player.PlanesCanSee & Value) != 0`, Equal = flag, NotEqual = negation, other comparisons false, a non-player subject has no planes.

## Behavioural consequence (not verified against content)
`PrerequisiteCheckPlane` returned true for every comparison before ("plane prerequisites are used by mount content"). Now a plane Equal test is false until a `ChangePlane` effect sets the bit, and a NotEqual
test is true. So anything gated by 28187 (Equal 4), 34027 (Equal 1) or 34083 (Equal 2), including target groups, creature display groups and spell visuals, is hidden/failed unless a spell has set the bit.
That is the client's own rule, but the Equal-1/2 gates have no known producer (only the unexplained 79236/79237 rows could set them), and the "mount content" remark was not checked.

## Open
* OR versus assign for the two no-duration rows; DataBits04 = 1 on 11 rows; whether retail also sent packet `0x172` at login or map entry (the client's zero default makes it unnecessary).
* The server still has no phase or plane filtering of entity visibility.

## Code
`PlaneChange`, `IPlayer.PlanesCanSee` / `PlanesCanBeSeen` / `AddPlaneChange` / `RemovePlaneChange` / `GetPlaneChanges`, `PlaneChangeRules.Fold`, `SpellEffectChangePlaneSemantics`, `SpellHandler.HandleEffectChangePlane`,
lifetime removal in `Spell.BuildLifetimeRemovalAction`, `SpellStateRemovalKind.PlaneChange`, `PrerequisiteCheckPlane`. Tests: `ChangePlaneSpellEffectTests` (9 cases, all green on first run) and
`PlaneMaskPrerequisiteTests` (9 cases; 8 failed against the old always-true check, then passed). Filter Plane|Phase|Spell|Prerequisite|Player|Mount: 3,935 cases pass (Source/SQLite scope).
