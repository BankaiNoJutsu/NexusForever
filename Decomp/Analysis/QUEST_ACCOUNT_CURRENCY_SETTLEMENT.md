# Ordinary quest account-currency settlement

6 November 2026. Public ordinary quest turn-ins now retain implemented
`QuestRewardType.AccountCurrency` awards with the Character completion and
deliver them through the existing paired account-award protocol. Previously,
`RewardQuest` changed the account wallet before normal Player save committed
Auth and then attempted Character. A failed Character unit could therefore
leave the reward paid while the stored quest remained achieved.

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

The ordinary path without account rewards is preserved. This does not enable
public Contract claims/reset, Primal allocation, unsupported quest reward types,
or arbitrary script-only completion entry points. In particular, this changes
the save-bound public turn-in route; legacy/direct completion adapters outside
that boundary retain their existing behavior.

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

## Remaining gates

Configured-provider/copied-upgrade, physical process-loss/restart and deployed
socket/native qualification of this new public route remain open. Prior crafting
protocol qualification retains its original frozen source boundaries; it does
not automatically qualify this new caller. The Character failure here is an
owned save-unit refusal, not a new post-flush SQL rollback or lost-ack proof.
Mixed item/account bundles, selected/auto-complete variants, Cosmic/Loyalty
catalogs, fresh-admission retry after Character refusal, arbitrary deferred
callbacks and legacy/script completion entry points need their own applicable
qualification or integration. Full quests, F-022 and restoration remain open.
Native client launch and Computer Use remain stopped; continue independent
integrated gameplay and the recorded remaining gates.
