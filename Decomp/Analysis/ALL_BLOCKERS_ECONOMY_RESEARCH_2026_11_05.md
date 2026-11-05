# Economy and progression blocker audit - 5 November 2026

Research-only disposition of the assigned economy/progression families. No
production Source, canonical roadmap, data, runtime, shared REA/Ghidra session,
commit or push was changed. This is not a claim that all remaining roadmap
work is complete. The parent owns the integrated ledger and native work.

The most useful new result is **separating unavailable retail evidence from
unfinished implementation and qualification**. The historical marketplace aux
pair is no longer a marketplace producer question. Reward Rotation's old
`07CD` content-context blocker is superseded. Guild bank and creation finance
have concrete Source work, while Contract reset edges and Fortune weights
still require external retail evidence.

## Current-record reconciliation

The audit starts from the [current next-work queue](ASSESSMENT_REMAINING_WORK.md#current-next-evidence-and-implementation-queue),
the relevant feature sections in [CURRENT_STATUS](../../CURRENT_STATUS.md), and
the latest linked implementation records. Earlier dated paragraphs retain
historical claims which later records supersede. Source observations below
were read with local HEAD `a621990e27f9bdbe64899d981052820b7b6b7a50`; concurrent
work may subsequently change that head or the observed files.

| Family | Disposition after this audit | Specific actionable boundary |
| --- | --- | --- |
| Contract unclaimed/excess/reset | Retail behavior evidence still missing | Observe final claim with an earlier unclaimed milestone, and an over-cap credit followed into the next cycle |
| Contract complete delivery | Qualification plus data-backed integration; not a missing quote implementation | Exercise canonical awards through real ItemInfo binding/stacking, capacity, Character/Auth settlement and recovery; 726 quotes alone are insufficient |
| Fortune exact weights/active catalog | Retail server/catalog evidence still missing | Obtain item/money IDs plus parallel probabilities, applicability date/region/catalog and card-selection relation |
| Reward Rotation mapping/slots | Mixed evidence and implementation | Verify content/difficulty/catalog association and schedule timing; connect actual reward delivery rather than recording a grant bit |
| Marketplace errors/settlement | Remaining implementation/operational qualification alongside specific retail semantics | Retained fallback errors, broader provider/restart/socket/replay and filter/partial-stack/CREDD semantics must be named separately |
| Former marketplace `06DF/07D5` | Ownership question superseded | `06DF` is Primal Matrix; `07D5` is reward-track update; no marketplace emitter should be invented |
| Primal Matrix allocation | Protocol complete for the new slice; gameplay unfinished | Qualified node/reward rows, rule validation, essence settlement, persistence, rewards and committed publication |
| Guild bank | Implementation plus targeted native/data qualification | Bank balances/tabs/limits/custody/transactions are missing; concrete UI API paths already exist |
| Guild/community creation finance | Source transaction integration remains | Couple accepted creation fee, guild graph and membership/affiliation outcome; cost-table availability is a distinct admission guard |

## Contract: an upstream carry implementation is not a retail witness

The newer authored `Contracts.lua` still gates selection with `bCanClaim` and
`bIsClaimed`, then invokes `ClaimRewardPoint`. It supplies no server cycle
transition or policy for earlier unclaimed rewards. Its hash differs from the
accepted exact-build local addon, so this public copy does not replace the
packaged source already bound in the [reset record](CONTRACT_RETAIL_RESET_EVIDENCE.md).
[Authored U15736 Contracts.lua:647,682,701,1180](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/Contracts/Contracts.lua#L1167-L1180)

A directly inspected original proposal provides an explicit tempting alternative:
[upstream PR272](https://github.com/NexusForever/NexusForever/pull/272), head
`34f666a0c636092d0bdd98304fb840961ff1bccb`, defines completion as no milestone
having `Choice == -1`. After a successful choice it resets only when complete;
reset clears the milestones and subtracts `MaximumPoints`, retaining surplus.
The author's test instructions use manual `!rewardtrack addpoints` commands.
These are facts about that emulator proposal, **not** retail carry/forfeiture
proof. The inspected 48-file change has no test/capture/fixture-named path.
[Proposed RewardTrack.cs:23-26,58-97](https://github.com/NexusForever/NexusForever/blob/34f666a0c636092d0bdd98304fb840961ff1bccb/Source/NexusForever.WorldServer/Game/RewardTrack/RewardTrack.cs#L58-L97)

The current evidence-backed working direction remains reset after successful
final-reward collection; the [prior research](CONTRACT_WEB_UNBLOCK_RESEARCH_2026_11_05.md)
already recovered the live-era testimony. This pass found no primary server
artifact, accepted retail transcript or original authored contract that resolves
earlier-unclaimed delivery/forfeiture or surplus points. Do not replace the
remaining question with PR272's all-chosen policy.

Delivery is a separate boundary. The [award audit](CONTRACT_REWARD_AWARD_QUOTES.md#qualification-and-preservation)
already used six real tables and quoted all 726 choices. The
[claim transaction record](CONTRACT_CLAIM_TRANSACTION_AND_ACCOUNT_HANDOFF.md)
explicitly identifies synthetic ItemInfo stack metadata in delivery fixtures.
Thus the missing outcome is **full canonical delivery and recovery**, not another
quote parser or an assumption that all six tables were never inspected.
Reacquiring exact qualified inputs may be necessary: the checked lean-store
path `remaining-goal/contract-awards/AwardSourceAudit/` is absent, while the
existing `contract-reset-evidence/ArchiveAnchors/Contracts.lua` remains present.
This bounded lookup says nothing about every other archive or asset location.
The unavailable Q5597 bundle is not requested again.

## Fortune: exact server data still missing; two narrower publisher witnesses

The [weight audit](FORTUNE_WEIGHT_AUDIT.md) still distinguishes the current
rarity-tier approximation from retail probabilities. The source
[FortuneRewardWeights](../../Source/NexusForever.Game/Fortune/FortuneRewardWeights.cs)
uses 1000/200/50; static AccountItem candidate rows do not identify an active
Madame Fay catalog. Previously recovered Medieval/Island Paradise rotation
policies are retained as prior evidence, not counted as a new unblock.

A fresh read of 100 publisher Steam announcements covered timestamps
1467137027 through 1543528832 and found nine Fortune/Fay-titled entries. Two
narrowly useful additional witnesses are:

- The 29 September 2017 announcement says the old Arcterra-themed Fortunes
  retire on **4 October**, when the fall collection arrives. This gives a
  historical catalog boundary, not the exact active build16042 catalog or IDs.
  [Publisher announcement, gid2175660043466330019](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1506646221).
- The 14 December 2016 Winterfest announcement describes the filled carrot
  meter guaranteeing a purple-or-better item in the next Fortune. This is a
  dated qualitative guarantee, not per-item weights, a numeric meter threshold,
  or evidence that the same rule persisted unchanged in the final client.
  [Publisher announcement, gid91593325386781766](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1481746167).

No numeric per-item probability corpus was recovered. [PR548](https://github.com/NexusForever/NexusForever/pull/548)
adds Fortune message models, including the arrays needed to carry probabilities;
its inspected 11-file change supplies no test/capture/fixture-named path or
active catalog. Neither a transport model nor a sale discount is a drop rate.
Needed input remains an accepted `03D2` catalog/probability payload or server
catalog artifact with identity and applicability, plus card/payout correlation
where required. Current settlement/provider tests qualify the emulator, not
those unknown retail values.

## Reward Rotation: retire the false content-context blocker

The [October native routing correction](REWARD_TRACK_ROTATION_ROUTING_FIXES.md)
already proves `07CD/07D3` are reward-track add/reload and `07D5` is reward-track
update. Rotation state consumers are separate. The current
[content-context delivery helper](../../Source/NexusForever.Game/Account/Reward/RewardRotationContentContextDelivery.cs)
accordingly returns no packets. Older F007 text demanding a `07CD` rotation
apply/Flag consumer is superseded. The
[runtime evidence collector](../../Source/NexusForever.Game/Account/Reward/RewardRotationRuntimeEvidenceCollector.cs)
still contains that old blocker string; this is a concrete documentation/report
cleanup candidate, not a reason to seek another nonexistent rotation owner.

Authored MatchMaker source requests refresh for the named content-type enum
members and reads `GameLib.GetRewardRotations`. Display consumes content type,
veteran state, reward expiry, reward kind, multiplier and `bGranted`. It hides
already-granted item rewards in selected views and suppresses an essence entry
whose multiplier is below two in the featured list. These are **display filters**,
not server eligibility or a claim/settlement algorithm. Enum iteration supplies
no numeric slot values by itself.
[U15736 MatchMaker.lua:1165-1176,1209-1241,1382-1444](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/MatchMaker/MatchMaker.lua#L1165-L1241)

Real remaining evidence: exact content/difficulty-to-reward association and
schedule timing. Current
[catalog selection](../../Source/NexusForever.Game/Account/Reward/RewardRotationPerContentRewardCatalog.cs)
chooses `contentId % eligibleRowCount`; this deterministic fallback is not a
retail join. Current [duration defaults](../../Source/NexusForever.Game/Account/Reward/RewardRotationScheduleDefaults.cs)
reuse auction GameFormula821 with a two-day fallback; that correlation is not
an independent retail rotation schedule. The seven request-index slots are an
already mapped native boundary, not seven unknown gameplay producers.

Real remaining implementation: [GrantClaimService](../../Source/NexusForever.Game/Account/Reward/RewardRotationGrantClaimService.cs)
records grant state and builds an upsert, while explicitly withholding item/
currency delivery. Its focused fixture verifies the recorded schedule row, not
an award. A delivery coordinator, authoritative selection snapshot, deduplication
and committed publication/recovery still require work after the reward linkage
is qualified. [PR534](https://github.com/NexusForever/NexusForever/pull/534)
is original protocol-model evidence, not a source of the missing server catalogs.

## Marketplace: do not retain misowned auxiliary tasks

Both formerly named auction auxiliaries are now assigned elsewhere: `06DF` to
the [Primal Matrix snapshot](PRIMAL_MATRIX_WIRE_CONTRACTS.md), `07D5` to reward
tracks. Old F005/BLOCKER_EVIDENCE_PLAN paragraphs should not create separate
marketplace producer searches for them. This closes their **marketplace
ownership premise**, not retail marketplace completeness.

The [latest public rejection milestone](NORMAL_AUCTION_BID_TRANSACTION.md#posting-and-cancellation-rejection-replies)
already covers five real Player-backed public purchase/post/cancel routes.
Its separate limits name legacy/transient fallback errors, physical sockets,
cross-process replay and deployment. Those are Source/operational qualification,
not unavailable retail catalogs. The [public handler source](../../Source/NexusForever.WorldServer/Network/Message/Handler/Marketplace/ClientMarketplaceHandlers.cs)
retains synchronous fallback replies after `TryQueue*` declines. A targeted
fault reproduction, followed by scope-specific implementation, is the next
evidence for that fallback; another publisher guide cannot qualify it.

Authored auction UI handles post/bid result codes, and successful bid results
refresh the listing; the commodity UI handles post/order result plus actual
cost. Their error-name tables support consumer behavior but do not prove SQL
atomicity, multi-party ownership or restart replay. [Auction.lua:1299-1324](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/MarketplaceAuction/MarketplaceAuction.lua#L1299-L1324),
[Commodity.lua:929-959](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/MarketplaceCommodity/MarketplaceCommodity.lua#L929-L959).

Original [PR524](https://github.com/NexusForever/NexusForever/pull/524) and
[PR519](https://github.com/NexusForever/NexusForever/pull/519) are message-model
changes, not accepted settlement transcripts. Specific retail gaps remain
property/rune/equippable filtering, partial-stack/fill behavior beyond the
qualified cases, and nonempty CREDD exchange/history semantics. Keep these
distinct from the already implemented listing/wallet/mail coordinators and
their broader provider/physical-failure qualification. No new retail fixture
was recovered from the inspected proposals.

## Primal Matrix: current request is implemented; allocation is not

The [new wire milestone](PRIMAL_MATRIX_WIRE_CONTRACTS.md) already implements
registered `0072` increments and `06DF` absolute saved-state snapshot. The
[current handler](../../Source/NexusForever.WorldServer/Network/Message/Handler/PrimalMatrix/ClientPrimalMatrixSaveHandler.cs)
is diagnostic-only. Do not list the missing decoder or auction ownership as an
open blocker, and do not send an empty snapshot as a diagnostic response.

The inspected authored UI calculates affordability after pending costs,
compares allocations with the table maximum, and checks a locked-node state.
Its refund control compares edited versus saved counts. These corroborate
validation inputs and pending-editor behavior, not a postcommit refund/respec
rule or authoritative adjacency algorithm.
[U15736 PrimalMatrix.lua:538-574](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/PrimalMatrix/PrimalMatrix.lua#L538-L574)

Required data/rules: qualified PrimalMatrixNode/Reward rows, class-specific
reward interpretation, max-rank and adjacency/prerequisite behavior. Required
Source integration: owner-bound validation, checked four-essence debit,
allocation persistence, reward/AMP adoption and original-actor publication/
recovery. Existing node/reward schema fields and client quote arithmetic are
not a complete allocation service. This pass recovers no independent missing
retail allocation transcript; the [pinned native report](PRIMAL_MATRIX_NATIVE_CONTRACT_2026_11_05.md)
remains stronger than the older UI for wire and cost staging.

## Guild bank and creation: implementation gates with concrete API leads

The [money](../../Source/NexusForever.WorldServer/Network/Message/Handler/Guild/ClientGuildBankMoneyTransactionHandler.cs)
and [tab-open](../../Source/NexusForever.WorldServer/Network/Message/Handler/Guild/ClientGuildBankTabOpenHandler.cs)
handlers remain log-only; the existing boundary test intentionally expects no
bank state. Missing guild balances, tabs, custodial item operations, daily
withdrawal/permission checks and durable finance cannot all be labelled missing
retail data. Keep the non-emitting boundary until a coherent service exists.

Newly useful authored API distinctions: bank display uses `GetBankTabCount`,
`GetBankTab` and withdrawn-today getters; opening a tab calls `OpenBankTab`.
Cash uses separate `DepositMoney` / `WithdrawMoney` calls. Purchasing a tab
calls `PurchasePerk` with **`idPerk`**, not the displayed tab ordinal. Its UI
checks `bSpendInfluence`, `nPurchaseInfluenceCost`, current influence and the
first locked tab. This supplies exact native binding/selector seeds and avoids
inventing a standalone tab-price API. It does not prove numeric wire signs,
server entitlement/limit rules or transactional ordering.
[U15736 GuildBank.lua:183-201,288-361,637-657,1153-1185,1239-1244](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/GuildBank/GuildBank.lua#L1153-L1185)

Guild creation UI obtains `GuildLib.GetCreateCost` and sends `GuildLib.Create`.
This confirms an ordinary fee-bearing action, not its persistence mechanism.
[U15736 GuildRegistration.lua:192,398](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/GuildRegistration/GuildRegistration.lua#L386-L398)
Current [registration code](../../Source/NexusForever.WorldServer/Network/Message/Handler/Guild/ClientGuildRegisterHandler.cs)
guards missing GameFormula764/1159 before admission, but calls `RegisterGuild`
and separately deducts Character credits or Auth Service Tokens. The canonical
[guild recovery record](GUILD_DISBAND_RECOVERY.md#committed-character-affiliation-publication)
still leaves fee and combined graph/membership/affiliation atomicity open.
Connecting those existing owners is Source work; a cost-table guard and graph
save tests do not close it. Exact qualified costs/perks and broader provider/
restart/broker/deployment evidence remain separate requirements.

## Source identities and bounded search

All authored Lua links above use the immutable `Live/` snapshot
`0827880c8a820b5cbb3f1dff3e3888c9ab9f0263`, committed 12 September 2017 and
labelled `v1.7.3.15736 API16`. These files identify NCsoft authorship; the mirror
does not establish build16042 equality. Original LF line numbers are used.

| Fetched authored file | Bytes | SHA256 |
| --- | ---: | --- |
| `Live/Contracts/Contracts.lua` | 58839 | `153cdf7eef305895ee585350db430c4e3b0715ee53da6708a4c7b405447209a3` |
| `Live/MatchMaker/MatchMaker.lua` | 161419 | `7e8637829d2f3c88369c0e832e42e1087cf170ffc776ec5dc329dc253cdd6de7` |
| `Live/MarketplaceAuction/MarketplaceAuction.lua` | 77128 | `ee42ff1f818553350aa66d7701152cd4f582df03ddc438cc4f2f3953b79d3472` |
| `Live/MarketplaceCommodity/MarketplaceCommodity.lua` | 61568 | `f387c6b11169dd150a6ef794ff70d1a0ae64da2800a17f6f641a03bb17c95285` |
| `Live/PrimalMatrix/PrimalMatrix.lua` | 58687 | `bb82d3f633e2482661c1b0ac96d5cd3c2401c11905a8781234abda922e5748f3` |
| `Live/GuildBank/GuildBank.lua` | 56895 | `514775b153253e11909c546cee427cf932dcd12010f041082aa68ef51039a442` |
| `Live/GuildRegistration/GuildRegistration.lua` | 25133 | `c41a15e90191d307efdebd5d8ae362cca6b96aca18607de9e3037c22cce70b54` |

PR metadata/files were reread on 5 November 2026. Their inspected heads are
272:`34f666a0c636092d0bdd98304fb840961ff1bccb`,
534:`ce95068ea0bd5c1a4f2a19d8c0adcfcf64be3165`,
548:`ef689a10337bf20e613726ac4d6d916083d289da`,
524:`245dcc05129a2f3688ae99b7e2135c0d1fcc478f`,
519:`cf330d6049f8697825178a80f52696b9d8062d8d`.
PR272 remained open/unmerged; the other four were merged protocol proposals.
The proposed PR272 `RewardTrack.cs` is 6606 bytes, SHA256
`dc4a32bce2d20ebe6638ad6b01979d1766652607d37a475f3026c174932aafbb`.

For reproducibility, UTF8 encoding of the Steam API's decoded `contents` field
has these identities: gid2175660043466330019 is2964 bytes, SHA256
`1bc1c111b222b04d49622b5b97c31ad935db44697ddaa0630387fa4364059d64`;
gid91593325386781766 is3379 bytes, SHA256
`f85a5def2a49d972b57881eaf4ae6e5f578a1799c9cf9786377e417b4b887fa0`.
These are content-field hashes, not hashes of the variable complete API response.

Quoted Contract unclaimed/excess and Fortune drop-rate searches found no new
primary retail result. Irrelevant modern games named WildStar, financial
contracts, derivative wikis and current private-server behavior were excluded.
This is a bounded public-source search, not proof no private/unindexed capture
exists. No code tests were run; existing test totals retain their original
source/provider/runtime limits. No family is promoted to full retail parity by
this note.
