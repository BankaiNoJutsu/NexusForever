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
