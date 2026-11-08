# Video witness V1: zone reward track UI (Arcterra track 35), 2026-11-08

Goal: [VIDEO_WITNESS_GOAL_PROMPT.md](VIDEO_WITNESS_GOAL_PROMPT.md), question V1. Gate fed: public 07CE claim/reset and track 35 ([ZONE_REWARD_TRACK_35_EVIDENCE.md](ZONE_REWARD_TRACK_35_EVIDENCE.md)).
Evidence tier: **Observed** (one video; on-screen retail UI), with two **Correlated** points against the recovered tables. Not Mapped/Verified; no code changed; no retail parity claim.

## Video
`_Additional/Quontom's Arcterra Patch Preview - Community Video Spotlight [JKC3T3JrGSg].mp4`, official WildStarOnline channel, uploaded 2016-03-08 (Arcterra patch preview, so a pre-final build era; the final build is 16042), https://www.youtube.com/watch?v=JKC3T3JrGSg. Frame at about 84 s (the same panel is faded in at about 80 s). The narration at 81 to 83 s says questing and the zone content fill "the zone's reward track, similar to challenges and contracts".

## What is on screen (84 s)
- Panel title "Reward Track - Arcterra" in the zone tracker, beside Quests, Missions, Challenges and Public Events.
- Panel text: "Gain points and unlock free items by completing various activities in specific zones. Your progress will reset upon reaching 100%."
- Counter "925185 / 600000 (154% Complete)" with a full bar and **one** milestone marker (a crown icon) at the right end of the bar.
- Hovering the marker shows "Icebound Pouch", Loot Bag, Item Level 50, Bind on pickup, Untradeable, "Open for a chance to obtain one of Arcterra's rare rewards!", "Right-click to salvage".
- Other Arcterra tracker content visible at 70 to 76 s: Events with a Soulfrost Collection timer ("Time Until Next Bosses Arrive") and public events listed as separate entries (relevant to V3).

## Correlation with the recovered build-16042 tables
- Correlated: track 35 has a single threshold row and its first reward choice is Item 86274, which the item catalog resolves as "Icebound Pouch". One marker, one named item: matches.
- Not matching, recorded as unresolved: the video denominator is 600,000 while the 16042 table threshold is 6,000,000 (a factor of 10); the tooltip shows Item Level 50 while the catalog `ItemPower` is 25. Either the preview build differed from the final build, the preview used a test character or test values, or the UI scales the numbers; the footage cannot say which. The table value stays authoritative for build 16042.

## What this changes at the 07CE / track 35 gate
- New Observed fact: the retail UI of that era displayed progress above 100% (154%) without clamping or resetting; the stated rule is that progress "will reset upon reaching 100%". Taken together, an over-100% value existed and persisted on screen until some action (claiming, or a periodic reset) resets it. Whether excess points carry over after the reset, and when the reset fires (on claim, on reaching the threshold, or on a timer), is **still not visible**.
- The single-marker, single-threshold shape of the Zone track supports the table reading; it says nothing about repeatability.
- Public 07CE stays unwired; the 15:1/17:1 defaults are untouched. This record sharpens the witness needed: footage of a claim, or of the counter right after crossing 100%, in a later (Free-to-Play or final-era) build.

## Leads not yet worked
Other hits: F2P overview videos describe a "challenge reward track" with Signature 25% bonus points and "Cosmic reward points" (account-level, a different system), the 2016 Livestream at about 5056 s reading the track tooltip, and the F2P launch impressions at about 565 and 647 s. A different uploader and a later build showing the track after completion would be the second Observed.
