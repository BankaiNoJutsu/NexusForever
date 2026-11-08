# Taxi fare and purchase: native read (WildStar.exe build 16042, 2026-11-08)

Track: settle (1) why the displayed taxi fare is not the raw `TaxiRoute.Price` sum, and (2) where the debit happens. Tier: native decompile plus disassembly = **Observed (static)**; agreement with the video record [VIDEO_WITNESS_TAXI_20261108.md](VIDEO_WITNESS_TAXI_20261108.md) = **Correlated**. No server change made. Ghidra 12.1.4 headless, project `WildStar64`, image base 0x140000000.

## Verdict
The server's `sum(TaxiRoute.Price)` is **wrong** for retail parity. The client never reads the `TaxiRoute.Price` column on the fare path. Per hop, the fare is a function of the **destination node's `TaxiNode.ContentTier`** and one `GameFormula` float. All three observed data points fit exactly, with no other modifier in the native code.

## Formula (per hop, integer copper)
`fare(hop) = trunc( (float)T * F * (float)T )` where `T = TaxiNode[destination].ContentTier` (column index 7, record offset 0x1C) and `F = GameFormula[952].Datafloat0` (record offset 0x18, 32-bit float; table value **132.75**; compiled-in fallback at 0x140b7a0f8 is also 132.75 = 0x4304C000).
- Multiplication order is `(T*F)*T` in float32 (MULSS twice), conversion is CVTTSS2SI (truncate toward zero), then a clamp/zero guard. `T == 0` returns 0. With F = 531/4 every product is exact in float32, so the integer form is `floor(531*T*T / 4)`: T=1 132, T=2 531, T=3 1194, T=4 2124, T=5 3318, T=6 4779, T=7 6504, T=8 8496, T=10 13275.
- Trip fare = integer sum of the per-hop fares along the path (running total in an int, no rounding of the total, no minimum, no player/level/faction/vendor/mentor/distance term, no discount in this code).
- Currency is credits: the price struct is filled with currency type 1 (store `[1] = 1` in 0x1404ad310) and surfaced to Lua as `tPriceInfo.monPrice1`.

## Addresses
| Role | Address |
|---|---|
| Lua `GetFlightPathToPoint` binding (name string 0x140b2b378, slot 0x140c5b610/618) | `FUN_14065e270` |
| Lua `PurchaseFlightPath` (string 0x140b2b390, slot 0x140c5b620/628) | `FUN_14065e6c0` |
| Lua `GetFlightPaths` (slot 0x140c5b600/608) | `FUN_14065d8f0` |
| Builds `tRoute` (node ids; nodes with TaxiNode Flags bit 1 set are skipped) and `tPriceInfo` | `FUN_14065e270`, `FUN_140659eb0` (writes `nAmount1`/`itemExchange1`/`monPrice1`) |
| Route search driver; same-world branch calls the DFS, else a single-edge lookup | `FUN_1404ad310` |
| Branch-and-bound DFS over edges, keeps lowest total, strict-less replace (ties keep first) | `FUN_1404ad660` |
| **Per-hop fare** | `FUN_1404ae170` (CVTTSS2SI at 0x1404ae1c5; GameFormula fetch `FUN_140200220(0x3b8)`) |
| Graph builder from TaxiNode and TaxiRoute tables (edge = route entry ptr, +8 source node, +0x10 destination node) | `FUN_1404ae270` |
| Purchase sender | `FUN_1404acfa0` |
| Node known test / faction test (player faction 0xA7 Exile, 0xA6 Dominion) | `FUN_1404ad9b0`, `FUN_1404adb70` |
| Wire send helper (opcode in 2nd arg) | `FUN_1403f4900` |

Graph rules (`FUN_1404ae270`): an edge enters the same-world map only when both nodes' `WorldLocation2.WorldId` (record offset 0x28) are equal; otherwise it goes in two cross-world maps keyed by route id and by source node, and the trip price is that single edge's fare. Of the 238 table rows, 7 cross worlds (e.g. worlds 625/170, 1229/51, 1221/1229); the rest are same-world, so the DFS branch dominates.
`FUN_1404ad9b0` treats a node as known when it is in the unlocked set **or** its TaxiNode record has Type <= 1 and Flags bit 0 set (Illium, node 69, Flags 57, is therefore always usable). Taxi routing ignores nodes the player's faction cannot use (TaxiNodeFactionEnum 0 any, 1 Exile, 2 Dominion), same as `TaxiNodeAccess`.

## The three data points (dest tier from `TaxiNode.tbl`: 60/61/62/63 = 3, 64/67/68/184 = 2, 69 = 1)
| Observation (video) | Hops (route rows) | Computed | Raw table sum |
|---|---|---|---|
| A: Mistymurk Camp(184) to Illium(69) shows 1s32c | 103: dest 69, T=1 | **132** (132.75 truncated) | 1 |
| B: Illium(69) to Hycrest(60) shows 11s94c | 100: dest 60, T=3 | **1194** (1194.75 truncated) | 1195 |
| B: Illium to Gildgrass(62) via Hycrest shows 23s88c | 100 then 84: dest 60 (T=3), dest 62 (T=3) | **1194 + 1194 = 2388** | 2390 |
| C: Feralplain Testing Range(67) to Endless Vigil(64) or Bloodfire(68) shows 5s31c | 95 / 97 etc.: dest T=2 | **531** | 531 |
| C: Feralplain(67) to Mistymurk(184) via Illium shows about 6s63c | 102 (67 to 69, dest T=1), 104 (69 to 184, dest T=2) | **132 + 531 = 663** | 532 |
Note for C: hop 1 costs 132 and hop 2 costs 531 under the destination rule; the sum 663 matches. A source-tier rule would give A = 531, contradicting the 1s32c reading, so the destination rule is the one consistent with every clip. Table `Price` is a stale or tool-side column: its 531 and 1195 values equal tier 2 and 3 fares rounded (531.0, 1194.75 to 1195) but it disagrees for tier 1 (Price 1, 5 to 50 vs 132), tier 4 (Price 1195 vs 2124), tier 7 (Price 0 vs 6504).

## Purchase flow
1. Lua `OnTaxiMapButtonDown` (TaxiMap.lua ~line 626) calls `GetFlightPathToPoint(node)`; if a route exists it calls `PurchaseFlightPath(node)` immediately. **There is no confirm dialog** on the vendor path (the confirm prompt exists only for Rapid Transport). The affordability test is cosmetic: it only colours the tooltip red (`bCantAfford`); the click is not blocked client side.
2. `FUN_14065e6c0` resolves the vendor unit, then `FUN_1404acfa0` re-runs the search from the unit's node (unit field +0x74) to the destination, checks both ends are known, collects the **TaxiRoute ids** of the chosen path and sends opcode **0x00FF** (`ClientFlightPathPurchase`) through `FUN_1403f4900`. Payload as built: a count and an array of route ids (u32 each; wire writer not separately decompiled, but it matches the server model `ClientFlightPathPurchase`: u32 count + u32 ids). **No price, no node id, no money amount is sent**, so the server must compute the fare itself.
3. The client has no local debit. After 0x00FF it only waits. `ServerFlightPathUpdate` 0x0188 (count + u32 node ids, event `FlightPathUpdate`) is an unlock-list refresh, not a debit or ride-start. The footage shows the wallet already reduced about 0.2 s after the click, before takeoff; the native code gives no reason for a deferral to arrival, so the retail server evidently debits (and sends a normal currency update) in the purchase handler.

## Effect on the server (proposal, not implemented)
- `ClientFlightPathPurchaseHandler`: replace `routes.Aggregate(price + route.Price)` with `sum(HopFare(route.TaxiNodeIdDestination))` where `HopFare(node) = (uint)((float)T * F * (float)T)` using `gameTableManager.TaxiNode.GetEntry(node).ContentTier` and `gameTableManager.GameFormula.GetEntry(952).Datafloat0`, falling back to 132.75f when the row is missing; keep the per-hop truncation and `T == 0 => 0`.
- Debit timing: move the debit from `TrySettleTaxiFare` (arrival) to the handler's accept point (after `CanAfford`, before `QueueTaxiFlight`), publish the balance immediately, and add a refund path if map admission is later abandoned (currently the arrival debit avoids that case, so this needs a design choice: pessimistic debit plus refund on cancel).
- The server may optionally validate route minimality; the client always sends the DFS-cheapest path (first found on ties), but a longer contiguous list is only a cost to the player, so it is not a security need.

## Tests that would encode it
1. Table-driven `HopFare` theory: T 0..10 gives 0, 132, 531, 1194, 2124, 3318, 4779, 6504, 8496, 10752 (T=9), 13275.
2. Real catalog: route 103 (184 to 69) = 132; route 100 (69 to 60) = 1194; routes 100+84 = 2388; routes 102+104 = 663; routes 95/97 = 531.
3. Handler: wallet with exactly 131 copper cannot buy route 103, 132 can (today the raw rule would accept 1).
4. Debit-at-purchase: balance already reduced and a currency update enqueued when `QueueTaxiFlight` is called, before arrival; insufficient funds sends `VendorNotEnoughCash` and queues nothing.
5. Fallback: missing GameFormula row still yields 132.75-based fares.

## Remaining unknowns
- The retail server's ride-start messages (vehicle entity, spline, camera) and the exact currency message sent at purchase are server-to-client traffic with no client handler to read here; only the wallet timing is from video.
- Cancel/interrupt refund semantics and whether a mounted player is auto-dismounted.
- Tier 4 to 10 destinations and tier-0/Price-0 rows were not seen in any clip; the formula is read from code, not witnessed there. Type 1 and Type 2 nodes (rapid transport / shuttle, most tier-1 rows) are presumably priced separately (`monCostRapidTransport` in Lua) and need their own read; this note covers vendor taxi (Type 0 and Flags 56) only.
- The 7 cross-world rows use the single-edge branch (`FUN_1404ad310` else-path); same formula, but no clip covers them.
- `GetFlightPaths` (`FUN_14065d8f0`) was not decompiled; it lists reachable nodes and is not needed for the fare.
- Whether `GameFormula[952]` can be changed by a live-patch table; retail 16042 table holds 132.75.

Review and implementation (lead, 2026-11-08): the table values behind the formula were re-checked from the extracted tables: `GameFormula` 952 Datafloat0 = 132.75; `TaxiNode.ContentTier` (column 7) is 1 for Illium (69), 3 for Hycrest (60) and Gildgrass (62), 2 for Bloodfire Village (68), Feralplain Testing Range (67) and Mistymurk Camp (184), which reproduces the four observed fares (1s32c, 11s94c, 23s88c for two tier-3 hops, 5s31c). Implemented: `TaxiFare` (`NexusForever.Game/Entity/TaxiFare.cs`, per hop `trunc((float)T * F * (float)T)`, fallback 132.75 when the formula row is missing) used by `ClientFlightPathPurchaseHandler` in place of the raw `TaxiRoute.Price` sum. Tests: the handler fixture now carries tier 1 nodes and F = 6 so its 5 + 7 fare becomes 6 + 6 = 12 (the settlement tests are unchanged), the overflow test uses a hop of 2^32, new `FlightPathPurchase_ChargesTheRetailFareFormulaPerDestinationTier` (5 cases) and `..._UsesTheCompiledInFormulaWhenTheTableRowIsMissing`, and the actual-catalog test expects the formula (it needs the asset root). Written before the change but not run red. Transport/Taxi/Spell/Rapid filter: 1,192 cases pass (Source/SQLite scope). Not changed: debit timing (the server settles at destination admission under its map-admission guard; retail debits in the purchase handler, per the agent's reading and the footage) and the rapid-transport spell fare (`transport.Fare`), which is a separate path.

