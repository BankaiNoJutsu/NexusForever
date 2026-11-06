# Ordinary quest account-currency settlement

6 November 2026. Public ordinary quest turn-ins now retain implemented
`QuestRewardType.AccountCurrency` awards with the Character completion and
deliver them through the existing paired account-award protocol. Previously,
`RewardQuest` changed the account wallet before normal Player save committed
Auth and then attempted Character. A failed Character unit could therefore
leave the reward paid while the stored quest remained achieved.

The [completion lifecycle follow-through](#completion-lifecycle-follow-through)
below extends the original public-only milestone to concrete direct completion
and tutorial recovery and qualifies additional variants and failure boundaries.
The original milestone and its portable evidence retain their frozen scope.

Two executed baseline cases observed Auth balance 10 while the Character save
was held and the stored quest was still achieved. These are Source/SQLite
failures on published parent `6d4cef26`, not retail-client observations.

## Delivered public behavior

- The public completion path identifies fixed or selected type-7 account awards
  after its existing reward/receiver/quest validation. It requires the normal
  Character save composition to be ready before mutation. An unready owner
  retains the achieved quest and creates no reservation or payout.
- The existing crafting coordinator's award capture and settlement body is
  reused behind a private account-award mutation method. Crafting retains its
  original entry point. The v1 intent already carries a bounded award bundle,
  original account/character/realm/epoch and database routes; it contains no
  recipe-specific fields. Historical receipt/table names and the autonomous
  recovery worker stay compatible. No schema, migration or new delivery service
  is introduced.
- The quest's account award is retained rather than added to the live wallet.
  Its existing earning/achievement hooks still compose with the Character
  mutation. Auth reservation records intent without crediting currency. The
  normal Character unit saves completion and the paid-source outbox together.
- Only the committed source permits Auth credit. The existing idempotent
  account operation, Character acknowledgement and original-actor wallet
  refresh precede the captured quest completion notices. The existing dialog
  completion callback is carried through the same publication boundary.
- Character save refusal leaves the quest achieved and the wallet unpaid.
  Auth refusal after Character commit leaves its durable source pending and
  withholds the completion notice. A fresh outbox pass completes the original
  payout. Actor replacement also withholds the old foreground notice while the
  worker can settle the original account. Repeated turn-in and receipt delivery
  do not regrant a spent award.

The ordinary path without account rewards is preserved. The original milestone
did not enable public Contract claims/reset, Primal allocation, unsupported
quest reward types or arbitrary script-only completion entry points. Its
save-bound public turn-in boundary did not yet cover legacy/direct completion;
the follow-through below integrates the concrete direct route and known caller.

## Executed qualification

Five new cases cover normal completion, Character refusal, Auth refusal/fresh
worker recovery, Player replacement/fresh worker recovery, and unready normal
save composition. They use parsed `ClientQuestComplete`, a real Player and
WorldSession, normal Player saving, concrete inventory/wallet/account managers,
and real Auth/Character SQLite databases. Encrypted queued quest state packets
remain withheld before payout. A duplicate request resends authoritative
completed state, and delivery replay preserves a subsequently spent balance.
All 152 related cases pass, including these five, existing crafting settlement/
outbox, Q5597 public lifecycle/loopback and quest capture controls. Both owning
Game and World builds pass with zero warnings/errors.

Eight additional cases use four recovered `Quest2Reward` rows and actual
`AccountCurrencyType` definitions through the same fixture and payout path:

| Quest/account-reward component | Reward rows | Currency/count |
| --- | --- | --- |
| 10525, A Claim to Stake | 9494 | Omnibit6, 1 |
| 10600, Pell Freezes Over | 9849, 9850, 9851 | Crimson15, Cobalt16, Viridian17 essence, 100 each |

Each bundle passes success/spent replay, Character refusal, Auth refusal with
recovery, and actor replacement with recovery. The owning GameTable reader
checks both payload hashes before loading. This is actual **account-reward
component** qualification: achieved/eligible quest state, objectives, receiver
metadata and other rewards are fixture adapters. It is not qualification of
either full quest, their world flow, or Primal currency spending authority.

The offline entry point is
`QuestAccountRewardActualQualification.RunAsync(assetRoot, freshOutputDirectory)`.
The [portable record](coverage/quest-account-currency-settlement-20261106.json)
retains source/tested assembly identities, exact table pins, results and review.
Raw outputs and the standalone runner are under ignored
`artifacts/quest-account-awards/`; no client table bodies enter Git.

Early fixture attempts had an undeclared namespace, stale objective metadata,
missing receiver/pushed-item adapters and a proxy wallet that selected a
different save seam; none is counted as reproduced payout behavior. The final
baseline reproduces the held-Character/Auth-credit failure. The positive fixture
then needed normal save composition loaded, and the code now explicitly rejects
unready public account turn-ins. Offline runner setup failures (SQLite DLL
resolution, then a missing tutorial-giver adapter) are retained separately from
the eight successful cases. Neither full solution nor full suite was run.

Final primary-agent reviews cover specification and repository conventions.
Upstream #608 remains open at `7db12d9a7c694bc5e66d9e0f4cdbc1b82155e69b`;
#609 remains closed/unmerged at `f9b6f7356798a41beea0768ff1f6b5a758ffeca3`.
Their reviewed unchanged diffs are reused; no upstream merge or PR is made.

## Completion lifecycle follow-through

On published parent `1b71c5dc`, four executed baseline cases reproduced early
Auth credit through concrete direct `QuestComplete` and starter-tutorial
recovery, for both eventual Character success and refusal. Twelve mixed,
selected and auto-complete controls already passed. Earlier baseline adapter
failures are retained separately and are not evidence of payout defects.

Direct ordinary account-reward completion on a concrete Player/WorldSession
now enters the existing paired save boundary. An already paired enclosing quest
mutation retains the award in that unit; an unpaired nested completion is
rejected before mutation or payout. The known starter-tutorial recovery caller
selects paired settlement when its achieved receiverless quests carry account
awards. Its visibility and combat-transition publication stays behind settlement.
The non-account tutorial path and Contract boundary retain their existing flow.
No receipt format, database schema or settlement service changes.

Twenty-two new cases join the original five. The final related scope passes
496 cases, including all 27 account-settlement cases, crafting settlement/outbox,
Q5597 public lifecycle/loopback, quest capture/core and tutorial controls:

- Direct completion and tutorial recovery preserve unpaid Auth and withheld
  completion notices while Character is held. Tutorial Auth refusal and actor
  replacement are recovered by a fresh worker without stale foreground notices.
- Mixed physical-item/account awards, selected account awards and auto-complete
  acceptance/turn-in pass commit, Character refusal, Auth refusal and replacement.
  The mixed item uses real Inventory/Item persistence with a fixture item
  definition; these are synthetic variants, not full actual-catalog quests.
- A refused public turn-in is loaded at a new account epoch through actual
  `CraftingAccountRecovery` admission. The unpaid reservation is cancelled and
  source-fenced; a fresh Player/session retries once and receives one payout.
  This is fresh in-process admission, not a physical restart.
- Transaction interceptors observe flushed SQL completion and its source inside
  the real SQLite Character transaction. Refusal before commit rolls both back
  and leaves Auth unpaid. Lost acknowledgement after commit leaves the durable
  source recoverable by a fresh worker, with no stale completion notice.

Sixteen actual account-reward component cases pass: both previously pinned
10525/10600 bundles now run through parsed public and concrete direct completion,
each with success/spent replay, Character refusal, Auth refusal/recovery and
replacement/recovery. Eligibility, objectives, receiver and other rewards remain
fixture adapters. Both owning Game/World builds pass with zero warnings/errors;
the scoped test graph retains its existing ASPIRE010 warning. No full solution
or full suite was run. The final related run takes 27 seconds.

The [follow-through portable record](coverage/quest-account-completion-lifecycle-20261106.json)
contains final source/DLL identities, baseline and final results, table pins and
both primary-agent review axes. Raw logs remain in ignored
`artifacts/quest-account-variants/`; prepared client table bodies stay outside Git.
Upstream #608 and #609 metadata was refreshed with unchanged heads, bases and
states; their reviewed unchanged diffs are reused.

## Remaining gates

Configured-provider/copied-upgrade, physical process-loss/restart and deployed
socket/native qualification of this new public route remain open. Prior crafting
protocol qualification retains its original frozen source boundaries; it does
not automatically qualify these callers. Cosmic/Loyalty catalogs, complete
actual quests (including their remaining rewards), arbitrary deferred callbacks,
non-concrete adapters and unknown enclosing script mutations need their own
applicable qualification or integration. Unpaired nesting is rejected rather
than silently crediting Auth. Full quests, F-022 and restoration remain open.
Native client launch and Computer Use remain stopped; continue independent
integrated gameplay and the recorded remaining gates.
