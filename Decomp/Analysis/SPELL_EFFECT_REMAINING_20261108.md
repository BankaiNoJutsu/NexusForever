# Remaining unhandled spell effects: what blocks each (2026-11-08)

Inputs: build 16042 `DB\Spell4Effects.tbl` (131,010 rows) against the `[SpellEffectHandler]` registrations in `NexusForever.Game` after the ChangePhase, ChangePlane and ChangeDisplayName work, plus spell
names resolved through `Spell4` / `Spell4Base` / the English text table. Supersedes the counts in [SPELL_EFFECT_HANDLER_CENSUS_20261106.md](SPELL_EFFECT_HANDLER_CENSUS_20261106.md): **1,752 rows over 26 types
have no handler** (2,478 on 2026-11-06, 2,020 before ChangeDisplayName). Evidence tier: raw tables and spell names only; nothing here is a retail witness.

| Type | Rows (timed) | What the carrying spells look like | What is missing before a handler is honest |
|---|---|---|---|
| 0x47 FacilityModification | 771 (285) | quest and path gadget spells | meaning of DataBits00 (1 to 29,808); no candidate |
| 0x37 ModifySpell / 0x38 ModifySpellEffect / 0x3A AddSpellEffect / 0x3B SuppressSpellEffect | 255 / 82 / 25 / 16 | talent and AMP spells (for example "Elementalist Talent 23" with DataBits00 2829 and DataBits02 4762, 7395, 7423) | the AMP/talent modifier model (F-021 gate); ids look like spell and effect ids but the combination rule is unwitnessed |
| 0x02 SpellCounter | 200 (0) | not sampled | DataBits01 1000 to 5000, DataBits00/02 float-like; no candidate |
| 0x0D Script | 104 (8) | script hooks | a server script registry keyed by the row; the row only carries the id |
| 0x18 ForcedAction | 95 (87) | "Rewind", "Counterstrike", "Predict: Fumble", "On Heal Other Forced Miss / Shallow / Fierce", "Relentless" | a combat-roll override model; DataBits00 0/1/2 and DataBits01 1/3/5/100/999 are not decoded |
| 0x3C ModifyCreatureFlags | 75 (34) | "Rumble", "Snap Trap", "Sleeping", "Well-Fed", "Smash" | DataBits03 is a flag word (`16416`, `16544`, `16431`, `32`) beyond the current create-flag enum; needs the flag names |
| 0x6D RestedXpDecorBonus | 25 (2) | housing decor "Comfort / Lighting / Aroma - Small / Medium / Large" (0.2 / 0.4 / 0.6), "Rest XP - 99-80 ... 19-0" (1.2 to 2.0), "Takin' a Breather" (0.25 / 0.5) | how the float combines with the server's rested-XP accrual, which is a constant 0.24% of the level span per hour computed at login; no decor-presence tracking |
| 0x4B ChangeIcon | 23 (14) | "Shred Icon 1" style icon swaps (DataBits00 = base spell, DataBits01 = replacement) | presentation only; no server surface |
| 0x87 HousingPlantSeed | 20 (0) | "Plant Seeds - Octopod / Spirovine / Honeywheat ..." (DataBits00 = plant index 1 to 20) | the garden plot and plant lifecycle; only the plant entity type exists |
| 0x00 (unnamed) | 18 | | unnamed type |
| 0x94 SharedHealthPool | 13 (9) | "Shared Fate", "Health Link", "Curse of Shared Fate", "Barrier" | an encounter mechanic with group targeting (TargetFlags 4 to 6) |
| 0x3E VacuumLoot | 5 (0) | "Proto-Coin - Vacuum Loot", "Whimfiber Catcher" (float 5 / 30 / 50 and 360) | loot pickup radius semantics |
| 0x63 MimicDisplayName | 4 (1) | "Clone", "Player Hologram", "Mimic Test", "[Dev] Soulrot Mimic Name"; every data word 0 or 1 | which unit is mimicked (not in the row) |
| 0x14 UnlockActionBar | 4 (0) | "Action Set Unlock", "Action Set 2 / 3 / 4 Unlock" (DataBits00 = 0 to 3) | the server has no action-set unlock state at all (all `MaxActionSets` are usable), so there is nothing to unlock |
| 0x83 MiniMapIcon, 0x71 RewardBuffModifier, 0x55 PathActionExplorerDig | 3 each | "Track Player"; "[Test] Daily Reward Buff - World Pve / PvP / Group PvE" (24 h, x1.5); explorer dig | test spells or unmodelled systems |
| 0x30 WarplotTeleport, 0x2D NpcLootTableModify | 2 each | | warplots and loot-table overrides |
| 0x81 TemporarilyUnflagPvp, 0x7C WarplotPlugUpgrade, 0x45 ApplyLASChanges, 0x31 CraftItem | 1 each | "Not PvP Flagged" (30 s) | PvP unflag semantics for the `Forced` flag; the rest unmodelled |

## Reading
No remaining type has the combination that made ChangePhase, ChangePlane and ChangeDisplayName implementable: a native or packet consumer (0x0171, 0x0172, 0x0939/name change) plus data words that
decode against tables and text. The nearest are `UnlockActionBar` and `TemporarilyUnflagPvp` (the names say what they do) but each needs state the server does not have (an action-set unlock, an unflag rule
for forced flags) and would be an invention rather than a restoration. Where a client-side consumer exists it should be read natively first: candidates are the ForcedAction prediction path, the creature
flag word consumers, and the rested-XP UI for the decor bonus.
