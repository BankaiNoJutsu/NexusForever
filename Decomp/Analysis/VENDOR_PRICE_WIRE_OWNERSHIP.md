# Vendor multiplier wire ownership

29 October 2026. **Mapped and preserved:** the fork's existing multiplier order
agrees with the exact native client. The upstream #521 field names use the
player's perspective; the fork names use the vendor's perspective. Swapping the
fork's two fields would reverse purchase discounts and sale payout bonuses.

Target: WildStar64.exe build16042, SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Exact-hash cached fragments were read first, then the recovered Ghidra project
was queried without analysis or unrelated labels. No native execution occurred.

| Native boundary | Verified ownership |
| --- | --- |
| Registration14006c290, opcode090B -> reader140097ca0 | Arrays followed by first float at payload+0x20 and second at+0x24 |
| Dispatch table1403f1344, `(090B-0061)*4` -> slot1403f35ec | DWORD RVA003ed76b -> stub1403ed76b; call at1403ed771 ->1403db9a0 |
| Apply1403db9a0 | First float -> selected vendor entity+0x36dc; second ->+0x36e0 |
| Item method registration140c58cd0 /140c58cd8 | `GetBuyPrice` ->14041bf80 -> vendor+0x36dc |
| Item method registration140c58eb0 /140c58eb8 | `GetSellPrice` ->14041bcc0 -> vendor+0x36e0 |

The source `VendorInfo.Build` writes SellPriceMultiplier first (what the vendor
sells to the player) and BuyPriceMultiplier second (what the vendor pays the
player). The actual `VendorPriceCalculator` uses those same channels for
purchase cost and sale payout. This closes the disputed field-ownership
comparison without changing prices, rounding, schema or public APIs.

The native GetSellPrice also performs repair-cost adjustments; GetBuyPrice has
its own price-token and reward-property paths. This pass does not assert full
retail parity for those formulas or import them into the server economy.

`VendorPriceWireOwnershipTests` adds three integration cases through the real
world-model -> VendorInfo -> packet builder and existing server price calculator.
Independent native field offsets distinguish purchase from sale channels; the
values differ in every case. A source comment documents the naming perspective.
These cases pass the full **6,660/6,660** Loyalty checkpoint and the subsequent
22-case harness/STS/vendor subset; the full solution builds with zero warnings
or errors. Results are retained in `loyalty-integrated-tests` and
`harness-bootstrap-tests` under the external evidence directory below.

Retained external evidence in `I:/GIT/NexusForever-assessment-report/remaining-goal`:
`vendor-dispatch-pointer.py/.json` verifies the original PE digest and table slot;
`vendor-native-refs-summary.json`, `vendor-native-price-registration-summary.json`
and `vendor-native-apply-summary.json` link actual Ghidra runs and detailed logs.
Four durable labels are added in `function_labels.csv`. The successful
`vendor-labeled-export-summary.json` run applied that map and re-exported all
four names at their exact addresses in `functions.csv`; its label fingerprint
is `15e44357392f455030c215c08c2d346f9175eb2010a2a2bba1c37204acb461f1`.
No decompiled source is copied here.
