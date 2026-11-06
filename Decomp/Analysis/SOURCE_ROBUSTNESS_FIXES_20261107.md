# Source robustness fixes found by the matrix triage (2026-11-07)

Three actionable independent Source gaps from the feature-matrix triage, each reproduced by a failing test first.
Evidence tier: Source/SQLite only; no retail behavior is claimed.

1. **Mail attachment delete (F-013).** `MailItem.AttachmentDelete(attachment, index)` marked the supplied attachment
   deleted but removed whatever sat at `index` (list positions shift after a removal), and threw after marking when the
   index was out of range. It now validates the index and the attachment identity before any mutation and throws
   `ArgumentOutOfRangeException`/`ArgumentException`. Two new `MailItemTransactionTests` cases failed before; 343 Mail
   cases pass. The only caller derives the attachment from the same index, so correct use is unchanged. This does
   not settle the open wire-index (`nServerIndex`) question.
2. **Tradeskill tier table (F-008).** `Player.AddTradeskillXp` granted XP and sent the profession update, then threw
   `NullReferenceException` in `CheckTradeskillTierAchievements` when `TradeskillTier` was absent; `AddTradeskillXpForTier`
   and `TradeskillPrerequisiteHelper.GetPlayerTradeskillTierRank` had the same unguarded read. All three now tolerate a
   missing table (`?.`), matching the partial-table pattern used elsewhere. One new `PlayerTradeskillArchiveTests` case
   failed before; 1,108 Tradeskill/Prerequisite/Crafting cases pass.
3. **Housing plug and decor tables (F-004).** `ResidenceMapInstance.PlugPlaceOrRotate` and `DecorCreate` dereferenced
   `HousingPlugItem`/`HousingDecorInfo` without a null check. A missing plug table now yields `Plug_InvalidPlug` (and a
   missing decor table the existing invalid-packet path). A new theory (unknown plug id, missing table) passes the
   unknown-plug case on first run (first test asserting `Plug_InvalidPlug`) and failed the missing-table case before;
   388 Housing/Residence cases pass.

Remaining low-impact candidates not done: the same unguarded `Spell4` read in `UnitEntity` (stack-group cap and three other
sites; needs private spell-property state, no direct test exists) and a handler-level RavelSignal dispatch pin.
