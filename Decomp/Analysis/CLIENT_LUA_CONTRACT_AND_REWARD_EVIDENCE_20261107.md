# Client UI Lua evidence for Contract choices and quest reward types (2026-11-07)

Inputs: the 203 `.lua` files in the installed client's `ClientData.archive` (build 16042), extracted read-only with
`Tools/ArchiveProbe` to `I:\tools\extract_lua` (not committed), and a Ghidra 12.1.4 headless import/analysis of
`WildStar64.exe` with a string cross-reference script (outside the repository). The Lua is client presentation code: it
shows what the UI offers and sends, not what the server decides.

## Contract reward choices (07CE choice policy)

`UI/Contracts/Contracts.lua` (lines ~537-760, ~1160-1180):
- Each reward point (`tReward`) carries `tRewardChoices`; every choice is drawn as a selectable item. Choice types are
  `RewardTrack.RewardTrackRewardType.Item` and `.AccountItem` (account items may be a currency amount or an item). The
  header text is the localized "Reward Choices" / "Please choose reward".
- The player selects one choice (`OnRewardItemSelectionBtnCheck` stores `tItemChoice.nChoiceIdx`), then
  `OnRewardConfirmTakeBtnSignal` calls `rtRewardTrack:ClaimRewardPoint(nRewardIdx, nRewardItemChoiceIdx)`.
- The claim button is enabled only when `tReward.bCanClaim and not tReward.bIsClaimed`; when `#tRewardChoices == 0` it is
  enabled without a selection (direct claim).
- The final milestone is `nRewardIdx == rtRewardTrack:GetNumRewards() - 1` (gold-milestone sound), and claimed state is
  per reward (`bIsClaimed`). The Lua contains no reset, forfeit, unclaimed-milestone or excess-point handling.

Consequences for the 07CE gate: the choice policy is "the player picks one of the offered choices by index"; it is not a
faction-derived slot. This matches the already implemented selection-based Character claim. The reset edge rules
(unclaimed milestones, excess points after the final claim) remain without evidence because the client does not
show them, so public 07CE stays unwired. The `nChoiceIdx` numbering (0- or 1-based and how it maps to the table's three
choice columns) comes from the native `RewardTrack` object, not from this Lua, and is still unmapped.

## Quest reward type 10 (RotationEssence)

The client names `Quest2RewardType_RotationEssence` (a string referenced from a client function at 0x140662ab7,
`FUN_1406625d0`), i.e. the type is exposed as a Lua enum, but no extracted Lua file references `RotationEssence`, while
`UI/CommDisplay/CommDisplay.lua` renders Money, AccountCurrency, Item, TradeSkillXp, GrantTradeskill and Reputation reward
types. Type 10 rewards are therefore not rendered by the quest-reward UI. Which essence currency they credit is still not
recoverable from the client.

## Effect-type names

`FacilityModification`, `UnlockInlaidAugment` and `ChangeDisplayName` appear once each as strings (0x140abbc48,
0x140abc370, 0x140abbec0) with no direct code cross-reference, consistent with an enum-name table; `SpellCounter` has one
referencing function (0x1405e8230). No server semantics are present.

Evidence tier: client UI Lua text and Ghidra string cross-references; no Source change, no test.

## Native decompilation of the reward-table builder (2026-11-07)

Ghidra 12.1.4 decompilation of `FUN_1406ba350` (the function that builds the Lua `tReward` table; it references the strings
`nRewardIdx`, `tRewardChoices`, `nChoiceIdx`, `bIsClaimed`, `bCanClaim`, `nCost`, `nNumRewardChoices` and `idReward`):
- It walks the reward's choice list in 12-byte records (three 32-bit values: type, id, count), the same triple the table stores
  as `RewardTrackRewardTypeEnum0n`, `RewardChoiceId0n`, `RewardChoiceCount0n`. Record type 0 becomes an `itemReward`
  (an item object), type 1 an `accountItemReward` (account item object), and any other type is reported as an error
  (call with code 0x74). This independently confirms the enum classification derived from the raw table in
  [CONTRACT_FULL_TABLE_CLASSIFICATION_20261106.md](CONTRACT_FULL_TABLE_CLASSIFICATION_20261106.md): enum 0 = Item2,
  enum 1 = AccountItem.
- `nRewardIdx` is the zero-based index of the reward in the track's reward list (the same value the Lua compares with
  `GetNumRewards() - 1` for the final milestone).
- The choice loop counter is zero-based and is held next to the per-choice table being built; `nChoiceIdx` is therefore most
  likely the zero-based position of the choice in the reward's choice list. The `nChoiceIdx` assignment itself sits in a helper
  that was not read, so this is an inference, not a confirmed field mapping. For Contract tracks 4 and 5 every row has three
  non-empty choices, so list position and table column (00, 01, 02) coincide, whether or not the native list skips empty slots.

Tier: native decompilation of presentation code. It supports the table classification and the zero-based choice index; it does not
show reset edge rules or any server-side behavior.
