# Confirmed physical bind-on-pickup loot

6 November2026. Public LootInstance.GiveLoot now routes supported physical BOP
delivery through the existing soulbound-creation API. New Items are bound before
their first inventory packet. Compatible bound stacks can merge; prior unbound
stacks remain unchanged. The existing prompt/confirmation policy is retained.

The former post-creation binding helper sent an unbound ItemAdd first and
could merge BOP quantity into a prior unbound stack that it never bound. Its
quantity preflight also counted that unbound stack's room. A held inventory
save lease could suppress ordinary ItemCreate while the loot was still marked
Delivered. These four failures are reproduced and corrected.

The original inventory mutation lease now guards supported physical BOP
creation. Refused capacity/lease leaves the loot available; freeing a physical
slot or ending the lease permits retry. Shared CanDeliverStaticItem uses the
same bound-capacity rule. Existing custom-inventory fallback,non-BOP,currency,
account/virtual and roll/master behavior remain intact.

## Verification

Four baseline failures and two controls are retained. All six new/all136 scoped
cases pass with zero skipped. Checks cover first-packet flags,unbound and bound
stack behavior,full capacity/retry,save-lease refusal,and failed full real Player
SQLite save followed by retry,fresh reload and the ordinary duplicate guard.
Existing Loot,InventorySoulboundCreation and crafting-LootID controls pass.
Both final review axes have zero findings.

Owning Game/test-only builds are clean in10.42/20.05s. The scoped runner reports
4s; no full solution or full suite ran. The isolated candidate compares6,311
published parent Source files,one owned tracked change,one new test and eight
historical filter/format variants. All138 DLLs are bound; only Game/Game.Tests
change and owning Game matches its test copy. Foreign Source is excluded.

The [portable result](coverage/loot-bound-pickup-20261106.json) pins source,
baseline/final DLLs,failures,controls,filter and reviews. Raw logs/TRX remain
under artifacts/continuation-20261106/loot-bound-pickup/.

## Remaining scope

These are real gameplay/inventory/save routines with prepared metadata and
session collaborators. Actual catalog/native confirmation,retail timing,
provider/socket/deployment and complete physical loot recovery remain open.
The guarantee applies to inventories advertising soulbound creation support;
legacy custom implementations retain their compatibility path.

Feedback enqueue failures,reentrant/partial batch delivery and complete
loot receipt/restart recovery remain unqualified. This binding milestone
does not establish exactly-once delivery under those failures. No schema,
migration,setup,entitlement or provider/native-client/CUA workload changed.

The Goal has resumed and remains unfinished. Hosted World/Auth restart still
requires the asset-owning machine; the three recorded runtime paths remain
absent. Preserve that gate and continue an independent actionable outcome.
