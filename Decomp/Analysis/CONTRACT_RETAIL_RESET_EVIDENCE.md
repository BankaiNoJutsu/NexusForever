# Contract cycle reset: retail evidence and implementation direction

On2026-10-31 the user instructed this work to reproduce live WildStar behavior,
search online where uncertain and choose from that evidence. A configurable
preference between all-claimed and final-claimed reset is no longer pending.
This evidence pass does not itself enable reset or public claims.

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

## Build16042 client cross-check

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
