# Spell4Effects handler coverage census (2026-11-06)

Inputs: build 16042 `DB\Spell4Effects.tbl` (131,010 rows, 134 distinct `EffectType` values) from the installed
client ([RAW_TABLE_EXTRACTION_20261106.md](RAW_TABLE_EXTRACTION_20261106.md)) against the 104 distinct
`[SpellEffectHandler(SpellEffectType.X)]` registrations in `Source/NexusForever.Game` (static grep, 2026-11-06;
effect types handled by some other mechanism would be missed, but the top ones below have no reference at all
under `SpellEffectType.<name>` in Game or WorldServer outside tests).

Result: 2,478 of 131,010 effect rows (1.9%) have no registered handler; 128,532 (98.1%) do.

| EffectType | Rows | Notes |
|---|---|---|
| 0x47 FacilityModification | 771 | no reference in source |
| 0x7F UnlockInlaidAugment | 281 | no reference |
| 0x52 ChangeDisplayName | 268 | no reference |
| 0x37 ModifySpell | 255 | AMP/modifier family (F-021 gate) |
| 0x02 SpellCounter | 200 | no reference |
| 0x22 ChangePhase | 161 | no reference |
| 0x0D Script | 104 | no reference |
| 0x18 ForcedAction | 95 | no reference |
| 0x38 ModifySpellEffect | 82 | AMP/modifier family |
| 0x3C ModifyCreatureFlags | 75 | no reference |
| 0x3A AddSpellEffect | 25 | AMP/modifier family |
| 0x6D RestedXpDecorBonus | 25 | housing |
| 0x4B ChangeIcon | 23 | |
| 0x87 HousingPlantSeed | 20 | |
| 0x00 (unnamed) | 18 | |
| 0x3B SuppressSpellEffect, 0x85 ChangePlane, 0x94 SharedHealthPool | 16, 16, 13 | |
| 0x3E VacuumLoot, 0x63 MimicDisplayName, 0x14 UnlockActionBar | 5, 4, 4 | |
| 0x55, 0x71, 0x83, 0x2D, 0x30, 0x7C, 0x81, 0x45, 0x31 | 1 to 3 each | PathActionExplorerDig, RewardBuffModifier, MiniMapIcon, NpcLootTableModify, WarplotTeleport, WarplotPlugUpgrade, TemporarilyUnflagPvp, ApplyLASChanges, CraftItem |

Gate for every row: the `DataBits`/parameter semantics are decided by the retail server, not by the table, and the
client binary performs only presentation/prediction. Implementing any of them requires a retail witness (capture of
the effect's observable result) or the native client's predicted-effect routine for that type. No handler was
written from the row counts alone. Evidence tier: raw-table census plus static source grep; no tests run.
