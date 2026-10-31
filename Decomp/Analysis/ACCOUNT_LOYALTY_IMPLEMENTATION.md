# Account Loyalty reward tracks

2026-10-29. This implements the account Loyalty path of upstream #272 using the
verified build16042 reward-track owner, separate from the existing rotation grant
service. It does not close Contract, Challenge or Zone reward-track progression.

## Evidence and declared server policy

The client executable SHA256 is
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
The corrected owners/readers are documented in REWARD_TRACK_ROTATION_ROUTING_FIXES.md.
Fresh Ghidra and exact-build static evidence are retained in the assessment report
under remaining-goal, including REWARD_SCOPE_PROGRESSION.md and
REWARD_CLAIM_INDEX_CONTRACT.md. Native inspection establishes positional cost/claim
bits, selected row IDs, choice0 for these one-choice rewards, cumulative thresholds,
linear parent availability and the Casque/AutoSoulbind/AutoClaim flags.

Account persistence and initialization are explicit server policy. Loyalty type2
correlates with account-owned CosmicReward currency8. Canonical root9 starts
active at zero; zero points grant no item or entitlement. Existing durable wallet
balance bootstraps earned progress once as a lower bound. Historical earnings
spent before that initialization are unknown and are not reconstructed. Future
committed earnings accumulate independently of wallet spending. Each tier uses
its final threshold as the next tier's offset. Completing an active tier through
new earning advances to the next unfinished available tier; explicit selection
in that unit remains authoritative. Receipt-only replay does not override another
character's later choice. No repeat/reset cycle is inferred.

LoyaltyCatalog validates all eight tiers and44 positions from the actual32-track,
446-reward table data. Slots follow RewardPointFlags plus final-only Casque rules,
not row-ID sorting or grouping equal costs. Track9 is
`276,312,278,280,311,279`; its two3200 thresholds remain distinct positions.
Malformed/missing/ambiguous slots, invalid topology, unsupported choices/currency
payloads and identities exceeding the14-bit claim wire limit fail closed.

## Complete gameplay and persistence path

Account/Player initialization sends committed Loyalty reload state. Real07CF and
07CE handlers route selection and claims to AccountLoyaltyManager. The manager
validates account/actor ownership, track and row prerequisites, parent availability,
selected position, earned cumulative threshold and exact choice. Earned native
AutoClaim positions and explicit client requests share the same claim receipt.
Persisted ordered slots retain their selected row identity and claimed state.

The Auth account-row lock serializes currency8 mutations, operation receipts,
earned ledger, slot claims and pending AccountItem delivery in one transaction.
Each claim uses a deterministic account/track/position delivery identity through
StagePendingItemAppend. Consumed append receipts remain durable, so retry never
recreates a consumed reward. Online pending state adopts committed rows while
preserving local mutations and newer baseline ownership. An earlier Inventory
completion cannot turn an adopted durable reward into another local append.

Currency8 uses current locked wallet deltas and stable operation identities;
other currencies retain their existing save behavior. Actual outer completion
acknowledges only captured operations. Later mutations survive, and monotonic
revisions keep older completion state from replacing a newer baseline. Reconciled
wallet amounts emit currency/wallet packets when the visible amount changes.
Provider retry and ambiguous successful commit use the verified whole-unit witness.

Nested currency scopes protect storefront and grant-plan operations. Provisional
purchase mutations cannot be flushed by an unrelated periodic save; the intended
Auth context authorizes their flush. Failed delivery/refund compensation drops
the scoped earning events. Canceled captured operations are revalidated before
staging. A scope canceled after SQL flush prevents outer commit through an Auth
commit guard, leaving rollback ownership with the caller. A completed child cannot
outlive a canceled ancestor. Real currency owners without Loyalty keep the legacy
save/dirty-state behavior through the optional capability fallback.

Cosmic account-item redemption derives its earning identity from the persisted
source generation plus operation ordinal. Legacy rows use a stable legacy identity;
new rows persist a creation GUID. Two stale owners redeeming the same source earn
once. Numeric inventory ID reuse creates a distinct generation, and late old
deletes preserve the replacement. New earning validates the durable source or
an authorized never-persisted local creation. Account inventory create/update/delete
intent follows actual commit and survives failure, savepoint uncertainty and
in-flight removal. Durable creation receipts prevent consumed uncertain creations
from being recreated.

## Binding and defaults

The44 actual Loyalty AccountItem choices contain33 physical-item and11 entitlement
deliveries. Earned AutoSoulbind metadata is persisted through pending delivery,
account inventory, transfer payloads and reload. The forced path uses the additive
Inventory capability: preflight before consumption, compatible bound stacks only,
no quantity-only satchel loss, and binding before the initial item packet.
Unsupported custom inventories reject before mutation. Unflagged ItemCreate and
old binding defaults remain unchanged.

All new binding columns defaultfalse. Zero progress leaves existing entitlement
defaults unchanged. Earned entitlement AccountItems still use existing redemption
prerequisite, scope and maximum-count checks. Challenge's existing direct-item
producer, rotation grant rows/APIs and premium RewardPropertyManager modifiers
remain separate.

## Schema and verification

MySQL migrations are AccountLoyalty `20261029210000`, AccountInventoryDurability
`20261029220000`, and AccountInventorySourceIdentity `20261029230000`; matching
SQLite migrations are `20261029174036`, `20261029175217`, and `20261029175905`.
Both provider model snapshots are generated. Existing fields/rows are preserved;
new tables hold the ledger, positional claims, currency operations and account-item
creation receipts. No reference schema or raw content SQL is required.

The final focused account/Loyalty/shop/gift/reward/bound-item/vendor gate passes
**800/800**, zero warnings/errors, retained as
remaining-goal/loyalty-account-final-focused.log. The preceding targeted gate
passes169/169. Coverage includes canonical all-tier data; zero/default initialization;
once-only bootstrap; earning/spending/overflow; alternate characters/accounts;
real wire-reader/registered-handler delegates; duplicate and consumed claims;
stale independent contexts; failure/outer transaction/savepoint/scope cancellation;
receipt replay with intervening selection; source consumption/ID reuse; binding
persistence; and existing shop/gift/rotation behavior.

The parent full solution checkpoint passes **6660/6660**, with zero build
warnings/errors, retained in remaining-goal/loyalty-integrated-build.log and
remaining-goal/loyalty-integrated-tests.log. The entire source remains frozen while the final owned MySQL
proof and runtime output snapshots are taken; no later map-lifecycle work is
included in this checkpoint.

The final6660 checkpoint also passes **32 aggregate lifecycle checks on real
MySQL8.0.46**, using a new isolated proof clone and frozen DLL/source hashes.
All three final migrations apply twice, preserve existing columns/defaults and
legacy null creation identities, and leave the model current. The proof exercises
legacy/new-generation duplicate redemption, ID reuse and late old deletes,
source consumption rollback/retry, before/after-commit replay, staged nested
cancellation guards, wallet packet reconciliation, all44 rewards, binding and
alternate-character reload. The original26 source tables remain unchanged.
Artifacts are remaining-goal/MySqlLoyaltyProof_29c3 and
remaining-goal/MYSQL_LOYALTY_LIFECYCLE_PROOF.md. The earlier106-checkpoint proof
is retained separately and is not substituted for this final proof.
No original or active runtime database was modified by this implementation pass.

The connected earned-entitlement redemption follow-up is documented in
EARNED_ENTITLEMENT_COMMIT_FIX.md. Its real pending→account item→benefit rollback
regressions exposed eager entitlement dirty acknowledgement. Actual terminal
acknowledgement, revision-safe snapshots, early aggregate ownership reservation
and permanent overlap rejection now pass all17 dedicated cases. Together with
the separate map-entry/harness slice, the current full build passes with zero
warnings/errors and **6717/6717 tests pass**. The earlier6660/32-check MySQL proof
remains its own checkpoint; the earned-redemption MySQL proof is recorded separately.
That final frozen6717 earned-redemption proof passes34 aggregate checks in a new
owned30-table MySQL clone, preserving defaults and every original source table;
see remaining-goal/MYSQL_EARNED_ENTITLEMENT_PROOF.md.
