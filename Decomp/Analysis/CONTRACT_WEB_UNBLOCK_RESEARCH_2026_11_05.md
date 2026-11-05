# Contract and communicator evidence follow-up

5 November 2026. Read-only web, GitHub source and local archive research for the
current Contract public-claim/reset and communicator actual-table blockers.
No client was launched, no archived input was changed, and no production Source,
public `07CE` routing, entitlement, reset or reputation policy was changed.

## Result and qualification

The original September 2017 reset discussion is now directly readable. This
removes the earlier page-availability limitation and adds a directly inspected
follow-up about holding active Contracts across days. It does **not** establish
the fate of earlier unclaimed reward milestones or excess reward-track points.
The Contract public-claim/reset implementation gate remains open.

The exact build-16042 Contracts addon remains available locally with its recorded
hash. Public source searches found an older Carbine addon and communicator schema
definitions, but no accepted actual-table rows or native/runtime corroboration
for the remaining reputation predicate correction. The communicator behavior
change remains Mapped, rather than Verified.

## Primary evidence inspected

| Source | Observed fact | Scope and limitation |
| --- | --- | --- |
| [Original MMO-Champion exchange, posts 53504-53507](https://www.mmo-champion.com/threads/958664-Megathread-Wildstar-Now-F2P/page2676) | On 6 September 2017 Kelimbror answers the reward-bar reset question: "Once you fill it all the way, collect the max rewards, yes it resets." On 7 September the same participant describes retaining active Contracts for later completion. | First-hand live-era testimony; client/server build is unspecified. The follow-up concerns active Contract quests, not earlier unclaimed reward milestones. Neither passage specifies overflow, carry, forfeiture or final-first settlement. |
| [GreenyNeko's first-person Contract board account, 27 January 2018](https://www.greenyneko.com/2018/01/wildstar-contract-board-track-completion.html) | Describes separate PvE/PvP tracks, their 5000/3000 final milestones, repeated board completion and the exhausted-unlock-container fallback. | Live-era account, not a server specification or exact-build binding. Rounded daily-income calculations do not resolve point carry or unclaimed milestone behavior. This corroborates the existing note rather than supplying a new reset rule. |
| Local exact-build `Contracts.lua`, identified below | Claim-button visibility uses the reward's claimable/claimed flags. The final list index chooses the final-milestone sound; the handler then uses the ordinary selected reward claim method. | Authored client UI source. It contains no server cycle-transition implementation. This independently rereads the existing evidence, not a new observation of retail execution. |
| [NASADocs Carbine `Contracts.lua`, immutable commit 5a154a6f](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/Contracts/Contracts.lua) | An original Carbine UI source snapshot is publicly inspectable. Its reward confirmation and updated-track UI are client behavior. | The commit date is 19 June 2015 and its bytes differ from the accepted local addon. Do not treat it as an independent build-16042 artifact or infer server reset rules from it. |
| [Upstream communicator PR 609](https://github.com/NexusForever/NexusForever/pull/609) | Its author identifies build-16042 condition/address observations and a 4105-row classification test. The PR explicitly says its reputation inequality preserves the existing server test and was not verified against the client. | Primary evidence for the author's implementation and reported checks, not independently reproduced native/table proof. The scratch final-client tables and test harness are not supplied in that PR. |

The reset direction remains **after successful final-reward collection**. This is
the existing inference from testimony plus client claimability. No evidence here
supports resetting merely at a point threshold or substituting the Challenge
all-slots-claimed rule for retail Contracts. See
[the canonical reset record](CONTRACT_RETAIL_RESET_EVIDENCE.md).

## Exact local artifact reread

Input:
`.continuation-evidence/assessment-report/remaining-goal/contract-reset-evidence/ArchiveAnchors/Contracts.lua`.

- SHA-256: `F47FCBCB091D95CB4DDC34F9BBC04F6C79215E891E6EF6AD4191D5CEAA5677B2`.
- Lines 637-660: selected reward claimability/claimed presentation.
- Lines 1167-1180: final-index sound branch and normal reward claim call.
- The hash matches the exact addon identified in the canonical reset record.
- This pass did not use REA's shared native session. The existing
  [4 November progress-replacement observation](CONTRACT_RETAIL_RESET_EVIDENCE.md#client-progress-replacement-cross-check)
  and [31 October native claimability cross-check](NATIVE_STATIC_CONTRACT_COMMUNICATOR_CROSSCHECK.md)
  retain their own evidence boundaries.

The public NASADocs snapshot was read in memory, without writing the source into
the checkout. Observed size: 53,310 bytes; SHA-256:
`4675af93285ddf214483b6462258be85cebc54ae69fbf43c043b77f1c888cedb`.
GitHub identifies its commit date as `2015-06-19T21:43:41Z`. Different bytes and
date prevent a same-build substitution.

## Communicator table and predicate corroboration

Three public source repositories contain communicator table definitions:

- [WildStar-Sandbox at 917bfaa0](https://github.com/DeKaDeNcE/WildStar-Sandbox/blob/917bfaa03476b39d17102dbc78b66ac5af6c67ca/Projects/RealmServer/ClientDB/Structures/CommunicatorMessages.cs).
- [ws-tool at d4bed803](https://github.com/CucFlavius/ws-tool/blob/d4bed803d4ecd1ee7d7deab2b4a8e5e99a49af4f/Engine/Database/Definitions/CommunicatorMessages.cs).
- [The-Ham-Sandwich at 5d208117](https://github.com/adarec1994/The-Ham-Sandwich/blob/5d20811732f1d040c4cb4713e7d7b9dfa986a94c/src/Database/Definitions/CommunicatorMessages.h).

Each source declares unsigned faction/reputation minimum/maximum fields in the
same relevant order as the fork. Their common derivation and exact client builds
are unknown. They corroborate an authored schema convention, **not** actual
row values, a zero-bound sentinel policy, or a negative-reputation runtime case.
They therefore cannot supply the missing Verified corroboration by themselves.

The original author of [PR 609](https://github.com/NexusForever/NexusForever/pull/609)
also leaves the inequality unverified. A direct read-only GitHub API request on
5 November observed `state=closed`, `merged=false`, head
`f9b6f7356798a41beea0768ff1f6b5a758ffeca3`, updated
`2026-11-01T02:58:37Z`. No new row corpus or inequality proof was found in the
inspected PR body. This does not replace the broader upstream triage ledger.

A public [IDA table-loader excerpt](https://github.com/ChargeIn/WildstarClientIDA/blob/5a6e5549050d0be88e4389dd890f4fe32062c54a/IDA/functions/sub_1401F4D40.c)
references the communicator table path. Its repository supplies no inspected
executable digest/build binding. It contains no table rows and was not promoted
as build-16042 corroboration. Decompiled target text was not copied into Source
or this note.

The historical missing SQL identity remains the one recorded in the
[native cross-check](NATIVE_STATIC_CONTRACT_COMMUNICATOR_CROSSCHECK.md):
481,759 bytes, SHA-256
`6a46e0e5d3fed29e375206a82707e3c519c2f1ecb7e4a606dc987566f515810c`.
This pass did not recover or hash that SQL file.

## Bounded search and archive coverage

Web queries covered quoted WildStar Contract reset, unclaimed rewards,
overflow/excess/carry points and final milestone terms; communicator table names;
publisher domains and primary-source follow-through. Search results for other
games, financial contracts and modern private-server behavior were excluded.
The 2015 press summary was only a discovery lead, not retail server authority.

Read-only GitHub code searches included `CommunicatorMessages`,
`CommunicatorMessages extension:sql`, `extension:csv`, `extension:json`,
`CommunicatorMessages.tbl`, `Contracts_RewardAlreadyClaimed`, and
`ReputationMin ReputationMax`. SQL and CSV searches returned no indexed results;
the JSON result was only a table-name list. A model, table loader or current
emulator consumer is not a recovered raw table. GitHub search is bounded and may
omit unindexed branches, private repositories, large files and historical files.

The parent supplied the read-only archive
`C:/GIT/_archive/cluster-3d/NexusForever`. Bounded checks covered its immediate root,
the expected `wildstar_client_mysql` table paths, aggregate
`all_wildstar_client_mysql.sql`, `Tools/DataMapping/output`, and targeted
communicator filenames under `Tools/DataMapping` including ignored/hidden files.
No raw communicator SQL/CSV was found in those checked paths. An ordinary
repository filename scan found the schema source only. This is not a statement
that no matching table exists elsewhere on the machine or inside game archives.

## Remaining concrete inputs

1. A retail observation showing a successful final claim while an earlier
   milestone is unclaimed, including the following track/claim state. This must
   distinguish forfeiture, retained entitlement and automatic delivery.
2. A retail observation with an over-cap Contract credit before final collection,
   followed by the next cycle's authoritative points. A capped UI progress bar
   or rounded guide calculation is insufficient.
3. For the reputation correction: accepted raw table rows with provenance, or
   independent runtime/sniff/packet corroboration of the zero-bound behavior;
   then the existing proposed finite/nonfinite cases can be implemented and run
   under the continuation guide's gate.
4. Actual complete-table delivery/recovery qualification remains required for
   public Contract claims. Successful quote construction and client UI source
   do not supply full Item2/stacking/account-delivery semantics.

No gameplay blocker is marked closed by this research pass. The useful change
is narrower evidence availability, source-bound exclusions, and an explicit
next-input list that avoids repeating the same schema and guide searches.
