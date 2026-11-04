# Build 16042 account-inventory CREDD redemption

4 November 2026. REA PE evidence
`ev_da27f0ff8de8665b152bc739b7654db7aa4f78845a3d9775591172e68e5ccf3d`
identifies the archived WildStar64.exe as SHA-256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Pinned PE reads and bounded LLVM disassembly establish the following static
observations. The client was not launched.

| Address | Observation |
| --- | --- |
| `0x140b69840`, `0x140b694e0` | Both `RedeemCREDD` Lua registration pairs point to `0x1404e5410`. |
| `0x1404e543c`, `0x140006fa5` | Each sender supplies `0x0770` to the account-item send helper at `0x1400161d0`. |
| `0x140077d30` | The registration's object size is one byte. The writer at `0x140001ba0` returns zero without emitting bits. The callback at `0x140001b70` returns without reading fields. |
| `0x14051b6c0` | The confirm action calls the sender at `0x140006f80`, then dispatches the local `AccountCREDDRedeemed` event without waiting for a response. |
| `0x14006df1d`, `0x1400804ee`, `0x1400054ca` | Server `0x097B` registers the six-bit reader and dispatches `CREDDRedeemResult` on its consumer path. |

The archived AccountInventory addon assigns the CREDD confirmation action,
displays one CREDD, and refreshes the UI on the local event. Its exact Git
blob is `3f2b1766ddc9b514a2ce2d861f0ef2a6f07db4b8`. The UI event can occur
before a server response, so it cannot establish that a redemption succeeded.
[Pinned Carbine addon](https://github.com/NexusInstruments/CarbineAddons/blob/cf063b3e22b08f49ff51c12cec258a9f1cf86d68/AccountInventory/AccountInventory.lua#L746)

The emulator now models `0x0770` as `ClientAccountCREDDRedeem` with an empty
payload. No economy handler is bound to that opcode. The existing `0x0268`
emulator exchange keeps its 64-bit amount and 1000:1 rule. The selected
`0x0268` immediate matches include object-size and other unrelated uses.
They establish neither a build 16042 send path for that opcode nor absence of
such a path elsewhere.

The native byte verifier and 103 scoped packet and account tests passed for
the original wire mapping. Local evidence and the verifier are under
`artifacts/continuation-20261104/credd-native-wire/`.

## Signature policy and subscription wire

WildStar's publisher confirmed on 26 September 2018 that used CREDD had
redeemed Signature membership with active days remaining. At sunset the
publisher handled unused CREDD and remaining redeemed days through NCoin
reimbursement for eligible accounts, and the final update granted Signature
globally. The announcement establishes the historical grant and the separate
shutdown policy. It gives no normal redemption duration or build 16042 result
mapping. [WildStar Signing Off November 28](https://store.steampowered.com/oldnews/?appgroupname=WildStar&appids=376570&feed=steam_community_announcements&headlines=0)

On the same pinned client, registration at `0x14006d8ba` assigns `0x033D` to
reader `0x14007e970`. It reads 32 flag bits, one expiry bit, and an unaligned
32-bit float. The consumer at `0x1400225e0` references `SubscriptionExpired`
and `GameTimeHoursRemaining`. It converts the hours to ticks with the
`36,000,000,000` multiplier at `0x140b7a098` and adds them to the current
time value. This matches the existing `ServerSubscriptionInfo` model. The
native verifier and both scoped packet tests passed, including the byte-level
regression for the width and order of its fields. Results and disassembly are under
`artifacts/continuation-20261104/credd-signature-policy/`.

The emulator derives Signature from `Permission.Signature`, and no producer
sends `ServerSubscriptionInfo`. A durable timed grant needs an account-level
expiry that participates in tier and reward checks. Evidence is still needed
for build 16042 redemption eligibility, duration and extension rules, result
values and packet order, and any accompanying rewards. `0x0770` remains
model-only pending those inputs.
