# Contract cycle reset: retail evidence and implementation direction

## 5 November source availability follow-up

The [original 2017 discussion is directly readable again](CONTRACT_WEB_UNBLOCK_RESEARCH_2026_11_05.md).
This supersedes the historical page-cache limitation below and adds the
follow-up about holding active Contracts across days. It still supplies no
earlier-unclaimed milestone or excess-point rule. Public `07CE` remains gated.

## Client progress replacement cross-check

4 November 2026. REA identified the archived build 16042 executable at
SHA-256 `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Its native provider was unavailable. Pinned PE reads and LLVM followed
`07D5` from dispatch slot `1403f3114` through `1403f112e` to `1404d18e0`.
The handler requires an existing track ID. It calls `1404d13a0`, which
unconditionally replaces progress `+0x24`, claimed mask `+0x20` and unlock
`+0x28` with the incoming values, then dispatches `RewardTrackUpdated`.
Only the separate positive-point notification compares old and new progress.
An inactive update skips the branch that assigns an active track.

The exact packaged `Contracts.lua` handler redraws from the updated track
even when progress decreases. The increasing-progress check selects sounds.
This establishes client support for a lower authoritative value and a
cleared mask on an existing track. It supplies no server rule for unclaimed
milestones or excess points. Public claims remain gated. The verifier, REA
results and disassembly are in `artifacts/continuation-20261104/contract-reload-recovery/`.

On2026-10-31 the user instructed this work to reproduce live WildStar behavior,
search online where uncertain and choose from that evidence. A configurable
preference between all-claimed and final-claimed reset is no longer pending.
This evidence pass does not itself enable reset or public claims.

Latest independent source gate,31 October: [player-mail persistence](MAIL_SEND_TRANSACTION_AND_INBOX_RECOVERY.md)
passes10,421 full tests/223 bank cases with zero full-build warnings/errors.
It does not change the Contract reset evidence below or enable public07CE.
Newly discovered [upstream609](UPSTREAM_609_COMMUNICATOR_RECONCILIATION.md) has source
reconciliation,162 focused and10499 full passing cases; this does not resolve
the retail Contract reset edges. Source/SQLite mail tests do not qualify retail reset,
unclaimed/excess-point rules, full-table delivery or fresh provider/deployment
behavior. Rebuilt DB DLLs have new identities; historical provider/upgrade
bindings remain historical. The Goal remains active and native launch stays stopped.

## Contemporaneous first-hand evidence

In a September6,2017 exchange, a player asks whether the Contract reward bar
resets. The reply says: "Once you fill it all the way, collect the max rewards,
yes it resets." The follow-up distinguishes this from a daily pressure/reset.
[Original player exchange, posts53504–53506](https://www.mmo-champion.com/threads/958664-Megathread-Wildstar-Now-F2P/page2676).
This is first-hand gameplay testimony, not an official server specification.
The checkpoint10165 continuation rechecked that indexed exchange online. A bounded
follow-up search for live-era Contract overflow/carry, unclaimed rewards and reset
timing found no additional source establishing those edge rules. Results about
other games' contracts or modern private servers were excluded. This does not
turn absence of an indexed answer into proof of any reset policy.
Search retrieved the indexed exchange; directly opening the same page returned
a cache miss. Its exact final-first/unclaimed and excess-point edge cases are
not stated. No conclusion here relies on modern emulator behavior.

The destination continuation repeated bounded searches on31 October for Contract
reset, earlier unclaimed rewards and excess/carry-over points, including targeted
WildStar player-forum queries. The directly opened first-person
[27 January2018 Contract board account](https://www.greenyneko.com/2018/01/wildstar-contract-board-track-completion.html)
corroborates separate PvE/PvP tracks, their5000/3000 final milestones and repeated
board completion. It also describes the unlock-container fallback after its
unlock pool is exhausted. It does not state the final-first/unclaimed transition
or excess-point handling. Pre-release previews, unattributed mirrors and results
for other games do not resolve those edges. This search adds a dated primary
cross-check, not a server reset specification; public claims remain disabled.

## Build16042 client cross-check

The handoff-only commit `a50dfa13` supplies the lean evidence store. All2,154
members verified and restored in the destination continuation. The local
`.continuation-evidence/assessment-report/remaining-goal/contract-reset-evidence/ArchiveAnchors/Contracts.lua`
has no redactions: its current SHA-256 matches both the published/original index
and `ArchiveAnchors/result.json` at
`F47FCBCB091D95CB4DDC34F9BBC04F6C79215E891E6EF6AD4191D5CEAA5677B2`.
The handler below was reread from those exact bytes. This restores the historical
asset/provenance, not an observation of the server reset transition. General
decompiler cache fragments and fresh Ghidra analysis remain unavailable here. A
later [static byte cross-check](NATIVE_STATIC_CONTRACT_COMMUNICATOR_CROSSCHECK.md)
now reconstructs1404d2240 directly from the matching archived executable with
LLVM, including all four chained unwind parts. Its selected-bit/cost checks
corroborate the historical fragment without recovering a server reset rule.

The already extracted retail `UI/Contracts/Contracts.lua` distinguishes the final
reward by the last list index in `OnRewardConfirmTakeBtnSignal` at1167–1180 and
then calls the ordinary native claim method. Its reward list displays the native
`bCanClaim` and `bIsClaimed` flags at644–648. There is no extra Lua requirement
to claim earlier milestones first.

Native `1404d2240`, `RewardTrack_CanClaimRewardPoint`, checks the transmitted list
position, that position's claimed bit and its cumulative point cost. It does not
scan earlier claimed bits. The locally retained decompile has the same build
hash `231BB2BB3FC6C37F3E8A43A0BA965CC3645287BBC6CCAD83D073A495C396B3E5`
as the asset provenance. This establishes that the client can present final-first
claimability, not the server's subsequent reset transition.

A fresh read-only scan of configured `en-US.bin` retained174 reward-track/Contract
text candidates under
`I:\GIT\NexusForever-assessment-report\remaining-goal\contract-reset-evidence`.
None of these candidates resolves reset/unclaimed behavior. This table is not
assumed to contain the complete Apollo UI localization corpus. The absent wording
is not negative proof about retail server behavior.

## 1 November2026 bounded online follow-up

Twelve additional indexed queries covered WildStar Contract reset, unclaimed
rewards, overflow/excess points, carry-over,5000-point milestones, the WildStar
subreddit and publisher Steam announcements. Results for Rust, Marathon,
Throne and Liberty, Star Citizen and financial contracts were excluded. A
current emulator review and mirrored patch notes were not treated as independent
retail server evidence. This is a bounded search, not proof that no report exists.

The directly reopened [27 January2018 first-person Contract board account](https://www.greenyneko.com/2018/01/wildstar-contract-board-track-completion.html)
still describes separate tracks, final milestones and repeated completion. Its
rounded daily-income calculations do not establish whether excess points carry
or how earlier unclaimed milestones behave when the final reward is collected.
No stronger edge-rule account was recovered in this follow-up.

An indexed [publisher Steam announcement for1.7.1,2 May2017](https://store.steampowered.com/news/posts/?enddate=1493766765&feed=steam_community_announcements)
describes a Troubleshooter completion fix for Prime Evil from the Ether. That
is a separate Contract producer fact, not a reset rule. The search returned the
publisher excerpt; directly opening the app-filtered page was unavailable, so
this is not promoted to a fully inspected independent reset/producer proof.
No source behavior, public07CE gate, reset policy, client/runtime configuration
or entitlement default changed from this evidence pass.

## Decision and remaining evidence

The subsequent archive pass inventoried131,399 main entries and28,989 English
entries through the REA-inspected Nexus.Archive managed API. The English archive
has38 non-audio entries, with no separate named UI string dictionary identified.
A targeted main-DB name inventory found11 text/Lua/string/UI/scripting candidates;
the obsolete source model `TextEntry` does not establish a real `DB/Text.tbl`.
The configured-file lookup for that table failed and its log is retained. This
closes that lookup path; it is not a request to recover another missing input.

Two fresh targeted extractions establish provenance rather than comparing
incompatible hashes. For both `UI/Contracts/Contracts.lua` and `en-US.bin`, the
index's20-byte entry hash equals SHA-1 of the **compressed** bytes. SHA-256 of the
decompressed files exactly matches the previously extracted Contracts addon and
configured English file. The apparent English SHA-1 mismatch was a compressed/
uncompressed comparison, not an asset-version mismatch. Source archives, runtime
and configuration were unchanged. Results and exact external decoded files are
under `contract-reset-evidence/ArchiveAnchors`; the two-record REA managed-library
evidence bundle is `contract-reset-evidence/rea-managed-archive.json`.

These inventories establish where was searched and bind the actual packaged
inputs. They do not reveal a retail server reset rule and do not turn absent
matching wording into proof about unclaimed rewards or excess points.

The working direction is **reset after successful final-reward collection**, based
on the player account and the client's independent final-first claimability.
This is an inference from the available primary evidence, not a recovered server
routine. Do not relabel the fork's existing Challenge all-slots-claimed policy as
retail Contract evidence. Do not reset merely when points reach the threshold,
before final payout commits, or in a migration.

Before connecting the transition, resolve how retail handled earlier unclaimed
milestones and points earned past the cap, using remaining packaged localization,
historical client footage or stronger first-hand reports. Preserve current
receipts and all already committed delivery/outbox ownership. The current claim
backend deliberately retains cycle/points until this connected transition is
implemented and tested. No arbitrary user preference or extra entitlement/default
grant is authorized or needed. The full assessment Goal remains active.
