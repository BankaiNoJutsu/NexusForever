# Restoration gap 5: taxi debit timing (2026-11-08)

Part of [RESTORATION_GAPS_GOAL_PROMPT.md](RESTORATION_GAPS_GOAL_PROMPT.md). Inputs: [VIDEO_WITNESS_TAXI_DEBIT_20261108.md](VIDEO_WITNESS_TAXI_DEBIT_20261108.md) (agent-written; the lead re-viewed only clip W1),
[VIDEO_WITNESS_TAXI_20261108.md](VIDEO_WITNESS_TAXI_20261108.md), [TAXI_FARE_NATIVE_READ_20261108.md](TAXI_FARE_NATIVE_READ_20261108.md), the server (`ClientFlightPathPurchaseHandler`, `Player.Taxi.cs`) and the extracted client Lua.
**Outcome: no code change. The gap is closed as a recorded decision, with the missing inputs stated exactly.** No retail parity claim.

## What the server does (read this session)
* `ClientFlightPathPurchaseHandler` validates the route list, nodes, kiosk range, destination and `CanTeleport`, computes the fare (`TaxiFare.Calculate`, the native-read formula), rejects with
  `GenericError.VendorNotEnoughCash` when the wallet is short, then calls `Player.QueueTaxiFlight`.
* `QueueTaxiFlight` is a **map teleport** (`TeleportToCore` with a `PendingTaxiFlight`). `TryApplyTaxiAdmission` re-checks funds before the source is removed and at grid readiness; `TrySettleTaxiFare` debits
  atomically at destination admission, marks the flight admitted, learns the destination node and publishes the balance. A failed or cancelled admission never charges.
* There is **no in-flight state**: no vehicle, no spline, no legs, no duration. The ride the footage shows (21 to 117 s of visible flight) does not exist on the server.

## Why the debit timing is not restructured
1. **Single hop (W1 to W3, four clips with W4's first leg):** retail debits when the taxi reaches the pad, a few seconds before takeoff. On this server the purchase, the teleport and the admission follow
   one another with only a loading gap between them, and the wallet is not visible during that gap. After arrival the wallet is identical either way (one exact fare, no second change). The two orders are
   indistinguishable to the player except in failure windows.
2. **Multi-hop (W4):** retail charged the wallet in four steps during one ride, summing to the tooltip price. Reproducing that needs legs with their own timing, which needs a flight. Without a flight, a
   per-leg debit has no moment to happen at; a lump debit at purchase would match none of the multi-hop footage, and the current arrival debit matches none either (as the witness record already says).
3. **Failure window:** debiting at purchase would create the case "charged, then the teleport fails or is cancelled" and force a refund rule. No retail clip shows a failed or interrupted ride, a short wallet,
   a second taxi request, a death or dismount in flight, or a logout during a ride (the witness record lists the queries tried across 198 videos). The current atomic settlement can never charge for a ride that
   did not arrive, which is the safe side of the unknown.

**Decided failure rule (for the current teleport model):** keep one atomic settlement at arrival; a short wallet is rejected before departure and again at admission; a failed admission charges nothing; no
refund path is needed because nothing is charged early. Revisit only together with a flight system, and then use W4 (debit at each leg start, last step at landing) as the lead observation, with the
leg-start versus node-passage question still open.

## Missing inputs (exact)
* A retail clip of: a taxi click with too little money (message, wallet unchanged); a ride interrupted by death, dismount, a second request or logout (paid legs kept or refunded); a multi-hop ride with
  0.1 s wallet samples at each leg boundary and intermediate node names. The library search found none; nothing else available supplies them.
* The vehicle path data and native flight movement (duration per route), needed before any ride can be simulated.

## New client fact (Observed, static Lua; not a server change)
The server answers a short wallet with `GenericError.VendorNotEnoughCash`. In the extracted client Lua only two addons listen to `GenericError`: `FloatText` suppresses its floater for that error
(`FloatText.lua` 206 to 239, mapped to the empty string) and `Vendor` shows an alert only while the vendor window is open (`Vendor.lua` 749 to 790). So a player who clicks a taxi node with too little
money gets **no on-screen message** from this error, and the taxi map itself only tints the price red (`TaxiMap.lua`, see the witness record). Whether retail answered a short taxi wallet with this error is unknown;
the rejection stays as it is until a clip or a native read of the taxi purchase reply shows the real error, because changing the code to one that prints text would invent a message retail may not have shown.

## Evidence tier and limits
Server facts: read directly this session. Footage facts: agent-read (W1 re-viewed by the lead); the multi-hop stepping was not re-checked. Lua facts: read directly (Observed, static). The equivalence argument in
point 1 is reasoning over those facts, not a test; no test was added because no behaviour changed.
