# Arcterra Zone reward track 35

## Actual map, world and reward inputs qualified

6 November 2026, parent d878d4c0bb0256cd55b446c1e33b91c58d8018c9.
The recovered build 16042 catalog resolves the previously missing map-to-Zone-track
association. Among all 490 MapZone rows, only 476 (Arcterra) has a nonzero
RewardTrackId: 35, whose type is 4. MapZoneWorldJoin 450 connects MapZone 476 to
World 3335, with asset path Map\Arcterra. The MapZone's WorldZoneId is 0; no
WorldZone identity is inferred from the map number. This is an actual static
join, not a physical map-entry or earning observation.

Track 35 retains a single 6000000 threshold and reward row 535: point mask 1,
flags 3, CurrencyType 1/amount 250000 and first choice Item 86274/count 1.
The complete ItemInfo constructor resolves that item as Icebound Pouch,
family 25/category 138/type 449/quality 5, maximum stack 1 and finite ItemPower 25.
Raw BindFlags 8 remains uninterpreted. These metadata do not decide whether
the pouch/currency is automatic, claimed, repeatable or reset with excess.

The separate type 3 Challenge track 36 retains thresholds 300/600/900/1200 and
reward rows 536/538/539/537 in slot order. The real CharacterChallengeRewardCatalog
admits 36 and excludes 35. Six definitions 1126/1127/1128/1129/1130/1134 reference 36;
they do not expose a Zone base-point amount. All 49 PublicEvent rows naming
World 3335 have no joined PublicEventRewardModifier rows in this recovered
corpus. This is a diagnostic absence, not proof events give no Zone progress.

The qualification verifies all 385 recovered payload identities, loads 22 typed
tables plus English, and exercises the owning GameTableManager, ItemInfo and
Challenge catalog against unchanged published candidate binaries. The helper
build is clean in 2.62 s and execution takes 1.713 s. All 138 dependency DLL copies
match the preceding published milestone; all 6290 tracked Source inputs match
the published parent apart from the eight retained historical candidate
project/config/BOM variants. Both review axes have zero findings. Initial
helper namespace/table-property declaration errors remain retained separately.

The [portable record](coverage/zone-track-catalog-20261106.json) pins the 23
loaded payloads, reader/runtime/helper identities, 29 unchanged Source inputs,
joins, assertions and review record. Raw evidence is under
artifacts/continuation-20261106/zone-track-catalog/. No production Source,
schema, handler, entitlement or reward was changed, and no owning build,
xUnit suite, provider or runtime deployment was repeated for this input audit.

The specific remaining input is a controlled or accepted historical before/after
credit tied to an identified Challenge/PublicEvent completion, including point
amount and ownership, plus payout/claim/reset/excess behavior. The old missing
map row and unknown pouch name are no longer blockers. Keep Zone earning and
public claims disabled; move independent implementation while those event/policy
inputs remain missing. Client launch and Computer Use remain stopped.

4 November 2026. The retained build 16042 reward rows and English text
identify two Arcterra tracks with the same display name.

| Track | Type | Name and summary IDs | Thresholds | Retained reward rows |
| --- | --- | --- | --- | --- |
| 35 | 4 | 744816, 744817 | 6,000,000 | 535 |
| 36 | 3 | 745873, 745874 | 300, 600, 900, 1200 | 536, 538, 539, 537 in slot order |

All four strings resolve to “Reward Track - Arcterra” in the lean
handoff's `contract-reset-evidence/text-result.json` export. The export
identifies its English input as SHA-256
`588e385a0e8dee8b92dc79018070a17e78d3efd535b948224702effecafa5cf7`.
The row values are preserved in
[LoyaltyCatalogBuild16042Fixture.cs](../../Source/NexusForever.Game.Tests/Account/Reward/LoyaltyCatalogBuild16042Fixture.cs).
Row 535 has point flags 1, reward flags 3, currency type 1 and amount
250,000, and first choice ID 86274 with count 1. That row alone does not
resolve item 86274's name, the award timing or the interpretation of
those flags.

In a February 2016 public test realm play report, Gareth Harmer describes
earning Arcterra zone-track progress from completing challenges and
events and earning Icebound Pouches after filling the track. The 2016
publisher patch notes independently place the pouch among Arcterra
rewards. This identifies useful completion producers to investigate.
The play report does not isolate individual events, point amounts or
the final build's payout transition. [First-hand play report](https://www.mmorpg.com/previews/the-arcterra-assessment-2000105324)
· [Publisher update notes](https://store.steampowered.com/news/posts/?appids=376570&enddate=1470882039)

REA identified the build 16042 executable at SHA-256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Its deep provider was unavailable. Pinned PE reads and LLVM disassembly
verified the following client paths.

- `RewardTrackLib.GetActiveRewardTrackByType` binds at `0x140b72610` to
  `0x1406f2fd0`. It rejects values at or above 5 and reads the slot
  at `+0xb8 + type * 8`. Type 4 can therefore be looked up numerically.
- The generic add and update handlers index the active table using the
  static track type at row offset `+4`. See `0x1404d1841` and
  `0x1404d1977`. Type 4 can use the generic client track state.
- `ChallengesLib.GetRewardTrackByZone` has a separate binding at
  `0x140b73920` to `0x140711010`. The archived Challenge Log uses that
  API for its selected zone and filters its active-track iteration to the
  Challenge type. This is evidence about the type 3 consumer.
  [Challenge Log archive](https://github.com/NexusInstruments/CarbineAddons/blob/cf063b3e22b08f49ff51c12cec258a9f1cf86d68/Challenges/ChallengeLog.lua#L1268)

The existing modifier audit identifies three property 4/data 4 spell
effects. Build 16042 English strings 757857, 757870 and 757873 describe
boosts to Zone points alongside Contract and Challenge points. The
separate key supports investigating a Zone multiplier after a base
earning producer is confirmed. [Modifier evidence](CONTRACT_REWARD_MODIFIERS.md)

The retained localization and table rows distinguish the two tracks.
The next inputs needed are a build 16042 Arcterra map-to-track row,
at least one controlled or historical before/after credit tied to a
specific Challenge or PublicEvent completion, and evidence of whether
the payout is automatic or claimed and how it resets. The historical
`D:/WildStar/Patch` archive is absent on this machine. The photographs
attached to the play report show the zone without the progress UI.
Neither the generic claim code nor the Challenge medal formula supplies
the type 4 producer, credit scale, ownership or cycle rule.

The 639 scoped catalog controls passed using the current verified binaries.
This evidence pass leaves type 4 earning and public claims disabled.
Local verifier, disassembly, asset pins and TRX are in
`artifacts/continuation-20261104/zone-reward-track/`.
