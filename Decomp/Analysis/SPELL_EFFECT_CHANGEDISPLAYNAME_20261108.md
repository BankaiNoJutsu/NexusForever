# ChangeDisplayName spell effect (0x52): inferred meaning and implementation (2026-11-08)

Inputs: build 16042 `DB\Spell4Effects.tbl` (268 rows of type 0x52), `DB\Creature2.tbl` and the English `en-US.bin` text table, parsed directly; the repository's own wire models for 0x0939
`ServerUnitNameAndTitleChange` ([F025_STATIC_HANDLER_PROXY_MAPPING.md](F025_STATIC_HANDLER_PROXY_MAPPING.md)) and the wide-string unit name change.
**Evidence tier: Source/SQLite tests plus a table/text correlation. The client has no handler for effect 0x52 itself and there is no retail witness of the packet an effect produced; do not restate this as retail parity.**

## What the rows say
* DataBits00 is a LocalizedText id. It resolves to an English string for **234 of 268 rows** (204 distinct names); 34 ids are absent from the text table; none is 0. The names are unit names:
  "Dominion Shocktrooper", "Hacked Dominion Turret", "Commandeered Suppressionbot", "Captured Dominion Airman", "Tempest Refuge Medic", "Shovel-Wielding Prisoner", "Rowsdower in Love",
  "Skeech Totem", "The Caretaker", "Hycrest Rebel", a test row "MY NAME HAS CHANGED!". That matches the earlier candidate reading (control/convert-a-unit spells).
* The ids are **not** Creature2 name ids (only 3 of 268 coincide with a `Creature2.LocalizedTextIdName`), so the effect cannot be "show creature N's name through the client's Creature2 lookup"; the server has to
  send the string.
* TargetFlags: 141 rows target the target, 119 the caster, 6 flag 4, 2 flag 3. Only **10 rows have a duration** (9, 12, 30, 45, 61 and 120 seconds); the other 258 are permanent renames.
* DataBits01 is 4000 on 2 rows and DataBits02 is 39983 on 13 rows (above the 14-bit title id range); both are unexplained and ignored.
* The client does have a way to rename a unit: 0x0939 `ServerUnitNameAndTitleChange` (Creature2Id lookup or a wide-string name) and the wide-string name change the server already uses for scanner units.

## Inferred semantics (implemented)
`SpellHandler.HandleEffectChangeDisplayName` resolves DataBits00 in the English text table (as for other server-sent names) and calls `IUnitEntity.AddDisplayName`; an unresolved id is ignored. `DisplayNameStack` keeps
the names in application order (the latest wins, a recast replaces its own record). The rename is sent to everyone who can see the unit (the wide-string name change) and repeated for observers who start
seeing the unit later (after the create packet in `SendVisibleEntityCreate`). A row with a duration is undone when it expires or by generic spell-state removal (`SpellStateRemovalKind.DisplayName`);
undoing shows the previous spell-given name or restores the unit's own name: a creature through 0x0939 with its Creature2 id (the client looks the name up), a player through the wide-string name change with
the character name. Rows without a duration are permanent until another rename or the unit is gone.

## Not determinable (open)
* Which packet retail used, and whether 0x0939 with a name string was used for the rename itself. 0x0939 also carries a title id, which this implementation sets to 0 when restoring a creature.
* Whether a restored creature's name really comes back through the Creature2 lookup in the client (no witness).
* The unexplained DataBits01/02, the 34 rows whose text id is missing, and the language: the server sends English regardless of the client language, like its other names.
* The 258 permanent rows: if retail reset names on some other event (respawn, leaving the spell's context), that is not modelled.
* MimicDisplayName (0x63, 4 rows) is separate and not implemented.

## Code
`DisplayNameStack`, `DisplayNameState`, `DisplayNameSync` (packet choice), `IUnitEntity.DisplayNameOverride` / `AddDisplayName` / `RemoveDisplayName`, `UnitEntity` (state, broadcast, spell-state removal),
`SpellEffectChangeDisplayNameSemantics`, `SpellHandler.HandleEffectChangeDisplayName`, lifetime removal in `Spell.BuildLifetimeRemovalAction`, resend in `Player.Visibility`. Tests:
`ChangeDisplayNameSpellEffectTests` (12 cases). They were written before the production code but first run after it (green on the first run), not red. Filter Spell|Entity|Player|Prerequisite|Visib:
6,045 cases pass (Source/SQLite scope). The spell-state removal path, the resend for late observers and the broadcast in `UnitEntity` have no direct test; the handler was tested directly, not through a full cast.
