# Quest AccountItem reward (type 6) (2026-11-06)

`QuestManager.RewardQuest` had no `QuestRewardType.AccountItem` case, so completing a quest with a
type-6 reward logged "Unhandled quest reward type" and silently dropped it. The account-save selector
also only paired AccountCurrency, so the reward could not reach the paired Character/Auth settlement.

Now: AccountItem joins the paired account-award bundle (the same idempotent Character outbox / Auth
receipt / committed-publication path as AccountCurrency; delivery already supported
`LootItemType.AccountItem`). Unknown `AccountItem` table ids are rejected with a warning and grant
nothing. The unpaired fallback grants directly through `AddLootItems`, as loot does.

Inference (not a retail witness): `Quest2Reward.ObjectId` for type 6 is an `AccountItem` table id,
matching how Loyalty and loot use that table. No retail routine or capture confirms it.

Evidence tier: Source/SQLite only. `AccountItemRewardsShareTheirCharacterAndAccountBoundary` (commit,
rollback, auth-refusal, replacement, rollback-retry) failed 4/5 before the fix; the harness `Run` was
extended (AccountItem table fixture, Auth refusal also for inventory rows). 32 settlement cases and
2,793 Quest/CraftingAccount/Loot cases pass (separate scopes; not additive). Not covered: MySQL,
socket, native, retail, GenericUnlock/AccountGenericUnlock (types 8/9) and RotationEssence (10), which
remain unhandled.

## Recorded gate for types 8, 9 and 10 (2026-11-06)

`AccountGenericUnlock`(8), `GenericUnlock`(9) and `RotationEssence`(10) stay unhandled. Missing inputs:
(a) a Quest2Reward.tbl row, capture or decompiled routine showing which table `ObjectId` indexes
(`GenericUnlockEntry` vs `GenericUnlockSet`) and the grant/notification routine; (b) for 10, the
RewardRotation/essence grant semantics. The repo holds no Quest2Reward rows with these types, so any
implementation now would be speculative. Revisit when raw TBL or a retail/native witness exists.

## Update with raw table evidence (2026-11-06)

[Raw extraction](RAW_TABLE_EXTRACTION_20261106.md) shows retail `Quest2Reward.tbl` has no type 6, 8 or 9
rows, so those are excluded with evidence. Only type 10 (86 rows, ObjectId 0, amounts 24/40/240) remains;
its currency target is not in the table.
