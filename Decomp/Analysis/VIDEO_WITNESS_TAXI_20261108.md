# Video witness V6: taxi / flight-master travel (2026-11-08)

Question: what does retail taxi travel look like (path, camera, timer, cost, arrival, transfers, map UI) and does it agree with `TaxiNode.tbl` / `TaxiRoute.tbl` and the server? Tier: video = **Observed**; Correlated only where a table row or the extracted Lua agrees. No server change, no parity claim. Server state read: `Player.Taxi.cs` (`QueueTaxiFlight` is a teleport, fare debited at admission by `TrySettleTaxiFare`), `ClientFlightPathPurchaseHandler` (route-id list, fare = sum of raw `TaxiRoute.Price`), [TAXI_MOUNT_UPSTREAM_FIXES.md](TAXI_MOUNT_UPSTREAM_FIXES.md) ("does not claim client fare parity or spline-based taxi rides"), [UPSTREAM_604_TAXI_SAVE_RECONCILIATION.md](UPSTREAM_604_TAXI_SAVE_RECONCILIATION.md).

## Clips (all uploads before 2018-11-28; library-relative names under `I:\Videos\_Additional\`)
| # | File | Upload | Era / res | Moments |
|---|---|---|---|---|
| A | `Let's Play Wildstar ep 11 - A tour of Illium [vtiogaGN0cQ].mp4` (youtube vtiogaGN0cQ) | 2015-12-29 | live, winter fest, 720p | kiosk 262 s, taxi window 266-272, ride 273-295, wallet checks 272/297 |
| B | `Wildstar - Leveling up tips [yNTXEWDFXvs].mp4` | 2015-08-29 | live, 720p | taxi window hover tooltips 703-710 s, commentary 695-715 |
| C | `Let's Play Wildstar： Reloaded Co-op Part 9 - CANCELED [Archive Week] [nJ0N3g9BqBI].mp4` | 2018-02-16 (near final build) | live, 360p | taxi window 384-390, ride 394-446 |
| D | `Wildstar Beta E.014 - Getting to Thayd [tQkJEV6ZRzo].mp4` | 2014-04-21 | **open beta, pre-launch**, 480p | taxi window 55-90 and 143-147, ride 147-266 |
| E | `WildStar： Reloaded Let's Play - Part 5 - Ellevar [pVCatxW6sgM].mp4` | 2015-11-09 | live, 720p | first-unlock tutorial popup 462-466 |
D is dated before launch (2014-06-03): treat its numbers as beta (lead strength), not as the 16042 behaviour.

## Observed
1. **Travel is a physical flight, not a teleport/loading screen.** After the click a taxi vehicle (yellow checkered "TSI" hover-car with a hologram driver) lands on the kiosk pad, the character sits in the rear seat, and the camera orbits the vehicle while it flies over the terrain (A 273-295, C 394-446, D 148-264). Normal HUD stays up (action bar, minimap, tracker); minimap subzone names change during the ride (D: Gallow, Gallow Fields, Annihilator Silo 14, Algoroc, Galeras, High Henge, Windswept Cliffs, Crash Site, Spaceport Horizon). The driver talks (D, speech lines about safety statutes, about Galeras). No ride progress bar, ETA or countdown seen in any clip.
2. **Durations (seconds, wall clock from video frames; error about +/-2 s, sampled 0.5-1 s apart):**
   - A: Mistymurk Camp (Ellevar) -> Illium: click about 272.3, vehicle leaves pad about 273.5, hard cut to Illium at about 295.3 = **about 23 s** click to arrival (about 21 s flight). The flight crosses into the Illium map; there is **no loading screen or fade** visible between the last flight frame (about 294.8) and the on-foot frame (295.5), only a cut.
   - C: Feralplain Testing Range (Deradune) -> probably Bloodfire Village (price tooltip 5s31 was the last hover; destination label not readable): takeoff about 394.5, landing/dismount about 445.5 = **about 51 s**. Same zone, vehicle lands on a pad, character is then standing on foot.
   - D (beta): Gallow kiosk -> Spaceport Horizon, Galeras (Exile): click about 146, takeoff about 148, landing about 264 = **about 117 s** over 8+ subzones; it did **not** stop at Thayd.
   Durations are therefore route-dependent (21 s to 117 s); a flight rate cannot be derived (distances unmeasured).
3. **Cost.** Shown only in the hover tooltip of the destination on the taxi map, label "Transport Price" (A, C) or "Transport Cost" (B, 2015-08), format "N silver M copper". Wallet debited at purchase: A shows 7g 19s 26c at 272.0 and 7g 17s 94c at 272.5 and 297 (-1s 32c, equal to the tooltip price 1s 32c) **before takeoff**, not at arrival. No confirm dialog: one click on the node starts the purchase (matches `OnTaxiMapButtonDown` -> `PurchaseFlightPath`).
4. **Taxi map UI** (window title "Taxi Vendor", map titled with the continent "Olyssia", Cancel button): zone silhouettes, a taxi icon per node, green arrow = player; hover draws a white polyline through every hop of the route and a tooltip "Node name [min - max]" + price (B 704-710: Gildgrass Airfield, Auroria [16 - 20] 23s88c, two hops via Hycrest; Hycrest, Auroria [14 - 18] 11s94c; C 385-388: Mistymurk Camp, Ellevar [7 - 14] about 6s63c via a two-hop line, Endless Vigil, Deradune [10 - 14] 5s31c, Bloodfire Village, Deradune [6 - 10] 5s31c). Red error text appears for unreachable/locked nodes (B). Unlocked-only nodes get a price.
5. **Transfers**: multi-hop destinations are one purchase with one summed price and a polyline through the intermediate nodes (B, C). Commentary in B 705-712 ("from Ilium to there and then to here") agrees. A multi-hop ride itself was not captured (no intermediate stop seen: unobserved, not disproved).
6. **Discovery**: E shows a tutorial popup "You unlocked a Taxi Destination on your Taxi Vendor map! Right-click on Taxi Kiosks whenever you find a new one." appearing as the taxi window opens at a new kiosk; that favours interaction-time unlock, the proximity-unlock path in `DiscoverNearbyTaxiNodes` (30 units) is not witnessed.
7. Mount: in A (speech 262 s) the player dismounts a mount before using the kiosk; arrival is on foot in A and C. Whether an active mount is auto-dismissed is not shown.

## Table / Lua correlation (`I:\tools\extract\DB`, en-US text, `UI\TaxiMap\TaxiMap.lua`)
| Observation | Row | Result |
|---|---|---|
| Level bracket in tooltip | `TaxiNode` cols RecommendedMin/Max: Hycrest 60 = 14-18, Gildgrass 62 = 16-20, Illium 69 = 14-18, Mistymurk Camp 184 = 7-14, Endless Vigil 64 = 10-14, Bloodfire 68 = 6-10 | **Correlated, exact** (Lua `TaxiMap_LocationNameTooltip` uses these columns) |
| Node names | text ids of nodes 60, 62, 64, 68, 69, 184 | **Correlated, exact** |
| Route graph | routes 93-98 (Bloodfire/Feralplain Testing Range/Endless Vigil), 99-102 (Illium<->Hycrest, Illium<->Feralplain), 82/84 (Hycrest<->Gildgrass), 103/104 (Mistymurk Camp<->Illium) | **Correlated**: observed polylines (Feralplain -> Illium -> Mistymurk; Illium -> Hycrest -> Gildgrass) are exactly the 2-hop paths these rows form |
| Single-hop price 5s31 | routes 93-98 Price 531 | **Agrees** |
| Illium -> Hycrest | route 100 Price 1195 (11s95c) | observed 11s94c (B), 2 hops 2388 vs 2390: **differs by 1 copper per hop** (2015 build vs final table, or a price modifier; unresolved) |
| Mistymurk Camp -> Illium | route 103 Price **1** | observed **1s32c = 132** (A, 2015-12); C (2018-02) two-hop Feralplain -> Illium -> Mistymurk reads about 6s63c = 531 + 132. **Table raw Price 1 does not match the displayed fare** even in the 2018 clip |
| Ride duration | no duration column in `TaxiRoute` / `TaxiNode` (4 cols / 11 cols: Id, text, type, flags, flightPathType, faction, worldLocation2, contentTier, autoUnlock, min, max) | no table cross-check possible; durations come from the vehicle path data/native movement |
`tPriceInfo.monPrice1` is produced natively (`GetFlightPathToPoint`), so the fare is not necessarily `Price` verbatim; the table Price=1 rows (103, 104, 133, 134, 138, 139) are the suspect class.

## Against the server (no code change made here)
- Server `QueueTaxiFlight` teleports at once; retail flies 21 to about 117 s with a camera and speaking driver and cuts across a zone change without a loading screen. Not modelled (already recorded as "no spline-based rides").
- Server debits `Price` at map admission (arrival); retail debits at purchase (A, 0.2 s after click).
- Server charges raw `sum(Price)`; retail displayed 531/1194 for 531/1195-table hops and 132 for a Price-1 hop: fare formula is not the raw column for at least the Price=1 class.
- Server wire shape (route-id list, contiguous legs, single purchase) is consistent with the polyline-per-route UI and single click.

## Confidence
Ride is a visible flight with camera, not a loading screen: high (3 clips, 3 eras). Durations: medium, only 3 samples, one pre-launch. Fare debited at purchase: medium (one wallet pair, A). Fare vs table: the 1195->1194 delta is low-confidence (video compression, one reading of "94"/"88" repeated across several frames, so likely real); the Price-1 -> 132 mismatch is repeated in two clips (A, C).

## Still missing
- A clip timing a **multi-hop** ride (does the vehicle stop at the intermediate node, is it one flight?) and a clip of a ride whose route has a `TaxiRoute` row so duration per row can be tabulated.
- Mount state before/after a ride with an active mount; whether movement/combat/interrupts cancel the flight.
- The fare formula (client native `GetFlightPathToPoint` / price info): one Ghidra read needed to explain 1 -> 132 and 1195 -> 1194.
- Proximity-unlock (vs right-click unlock) of a node.
Queries tried: `taxi` (892 cues), `taxi ride`, `taxi driver`, `taxi kiosk`, `taxi vendor`, `taxi network`, `flight path`, `fly to`; `taxi fare`, `taxi cost`, `taxi route` have zero cue hits.

Review note (lead, 2026-11-08): written by a research subagent; the lead has not re-viewed the clips or re-derived the table comparisons. Treat the debit-timing (retail debits at purchase) and fare observations as Observed leads until a second clip or the native fare function (`GetFlightPathToPoint`) confirms them.

Update (same day): the fare question is resolved by a native read, see [TAXI_FARE_NATIVE_READ_20261108.md](TAXI_FARE_NATIVE_READ_20261108.md) (per hop trunc(T * 132.75 * T), T the destination ContentTier) and is implemented; the debit-at-purchase observation is still unimplemented.
