# Guild bank native request and limit contracts

6 November 2026: [actual formula/perk inputs are now qualified](#actual-formula-and-perk-inputs-qualified).
The missing catalog values below are superseded by recovered build16042 data.
Financial storage/settlement and the named authority/policy gates remain unfinished;
money/item/tab-open handlers remain diagnostic.

Later [public rank-settings implementation](GUILD_BANK_RANK_SETTINGS.md) completes
operations23/24/30 with original-owner persistence and confirmed World readback.
Its22 new/346 affected cases and reviews are separate Source evidence. The
financial handlers remain diagnostic. The observations below describe this
native-mapping checkpoint; its proposed rank-settings integration is superseded.

5 November 2026. **Mapped only; bank economy remains unimplemented.** This
follow-through replaces the proposed financial fault-injection probe with its
actual prerequisite: the public money/item/tab-open handlers only log, so they
have no balance/custody/save operation to reject. No financial defect is claimed
from those stubs. The [portable record](coverage/guild-bank-native-20261105.json)
binds the pinned client,29 freshly checked native code ranges, procedural Lua
registrations, Source observations and retained analysis artifacts.

## Request producers and cached fields

The input is the retained `WildStar64.exe`,13,927,576 bytes, SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Existing REA PE-import evidence is reused for that exact binary. Standalone
Ghidra12.0.4/JDK21 opened the retained analyzed project read-only, without
analysis, labels or project saves. Its executable identity and every selected
function span were checked against fresh bytes read from the original PE.
This is static analysis, not client execution or REA native-provider qualification.
Seven bounded read-only passes have176s of combined log-write windows; that
excludes source inspection and documentation time. No build or suite was rerun
because production Source is unchanged.

The Guild object registers its methods procedurally in `140695c20`. Searching
only static name/function-pointer pairs misses these bindings. Selected entries:

| API / helper | Address | Observed contract |
| --- | --- | --- |
| `DepositMoney` / `WithdrawMoney` | `14069a980` / `14069aa30` | Require a `Game.Money` object with kind1; deposit negates its raw amount and withdrawal passes it unchanged to `14057fb20`. For positive input, deposit is negative and withdrawal positive. Both have a conditional absolute-value trade-cap check; that is not server authorization. |
| Money request sender | `14057fb20` | Sends `04A8` with realm/guild identity and the supplied signed amount. This confirms the existing Source decoder's sign comment; it adds no transaction owner. |
| `OpenBankTab` / open helper | `1406993a0` / `14057ee50` | Translate Lua ordinal1 to wire100 for type1 Guild, or200 for the other branch. A loaded nonzero tab dispatches `GuildBankTab` locally; otherwise sends `0477`. Do not infer that every other Guild type has banking. |
| `GetMoney` | `140698b80` | Returns the cached value at Guild+`1F0` as kind1 `Game.Money`. |
| Withdrawn / repair today | `140698c80` / `140698d80` | Read Guild+`260` / `268`, respectively, as kind1 money. Names do not establish reset timezone or server day ownership. |
| Repair remaining | `140698e80` | Rank repair limit minus cached repair-today; negative results produce Lua nil. |
| `GetBankTabItemWithdrawnToday` | `140698f50` | Looks up the ordinal's cached tab and reads tab+`20`; invalid/unavailable tab returns0. |
| `GetBankTabCount` / `GetBankTab` | `1406998a0` / `140699470` | Count comes from Guild+`278`; tab data comes from its cached tab collection. These are readback paths, not proof tabs are unlocked by default. |
| `GetBankTabSlots` | `140699920` | For type1 Guild/type3 WarParty, uses GameFormula644 (`284` hex), with client fallback64 if the row is absent. Other types return0. This does not qualify the current server table or justify copying an older UI's fixed128 slots. |
| `PurchasePerk` / sender | `14069b520` / `14057fb80` | Nonzero supplied perk ID becomes `04B1`, operation25, in the UInt64 data field. It is not the displayed tab ordinal. |

## Rank settings and withdrawal selectors

`SetBankTabPermissions` (`140699e60`) reads a rank ordinal, subtracts1 and
requires the result below10. It starts with the cached rank's existing packed
bank field and changes the supplied tab entries. For each tab's six-bit group:

| Bits within the group | Meaning established by setter and `GetRanks` (`1406973d0`) |
| --- | --- |
| `0..2` | Withdrawal selector0..7; Lua `idWithdrawLimit` is selector+1. The setter subtracts1 from valid IDs1..8. This is not an item count. |
| `3` (`08`) | `bVisible` |
| `4` (`10`) | `bDeposit` |
| `5` (`20`) | `bAuthenticator` |

The sender `14057f9a0` puts that packed field and zero-based rank in `04B1`
operation23. Money setters `14069a780`/`14069a810` use sender`14057fa20`,
operation24. Repair setters `14069a880`/`14069a910` use sender`14057faa0`,
operation30. The `ToAll` variants send **`7FFFFFFFFFFFFFFF`**, not UInt64 max;
`GetRanks` independently recognizes that same value for the two “all” flags.
The ordinary setters take their raw amount from a Money userdata helper; these
client calls do not establish the server's permission or numeric rejection policy.

The global `GuildLib.GetBankWithdrawLimits` binding at`140b75ea0` points to
`14075cfe0`. It copies the signed int32 vector at`140c7de30` into a Lua array.
The initializer`1405814b0` establishes exactly eight entries:

| Lua ID | Packed selector | Native value source |
| --- | --- | --- |
| 1 | 0 | Constant0 |
| 2..7 | 1..6 | GameFormula757..762 (`2F5..2FA` hex), in order; absent row falls back to0 in this client initializer |
| 8 | 7 | Signed constant-1 |

This resolves the selector-to-row join. It does not qualify the six actual
formula values for a server deployment. The older authored addon fills its
dropdown through `GuildLib.GetBankWithdrawLimits()` and sends rank permissions
and money/repair limits through the mapped APIs. Its comments and UI constants
are corroboration, not build16042 table authority.
[NCsoft-authored API16 GuildBank.lua, immutable snapshot](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/GuildBank/GuildBank.lua#L184-L186),
[permission submission](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/GuildBank/GuildBank.lua#L922-L964).

## Source boundary and next integrated outcome

The current Source [04A8 model](../../Source/NexusForever.Network.World/Message/Model/Guild/ClientGuildBankMoneyTransaction.cs)
already reads a signed long. Its [handler](../../Source/NexusForever.WorldServer/Network/Message/Handler/Guild/ClientGuildBankMoneyTransactionHandler.cs) and the
two item-transfer handlers still only log. `GuildDataModel` has no balance/tab/
custody storage. Rank bank permissions, money limits and repair limits already
have fields in `GuildRankModel`, `GuildRank` and the recoverable ordinary guild
snapshot, but [public operations23/24/30](../../Source/NexusForever.Game/Guild/GuildBaseOperation.cs)
have no handlers. The [diagnostic boundary test](../../Source/NexusForever.Game.Tests/Guild/GuildBankHandlerBoundaryTests.cs)
continues to describe the unimplemented money/item/tab-open surface. No Source behavior,
database, entitlement or diagnostic emission guard changed in this milestone.

The next available integrated Source outcome is those native-mapped public rank
bank-settings operations: validate full-width rank identity and edit authority,
preserve the native data/sentinel contracts, save through the original guild
mutation owner, and publish confirmed rank readback with retry/ownership proof.
This is a prerequisite of finance, not completed deposits, withdrawals or custody.
The existing ordinary rank path's immediate client broadcast needs an executed
save-failure/replacement probe before reusing it as committed publication.

The broader bank service still needs Character/guild debit-credit settlement,
balances/tabs/custody, qualified perk/formula rows and authenticator authority,
daily counter/reset ownership, banker interaction, overflow/replay/concurrency,
committed replies and provider/restart/socket/native/retail proof. This pass
establishes no server reset calendar, unlocked tab count, active formula values
or accepted retail transaction. Keep the log-only money/item/tab-open boundary
until that coherent service exists. Other assessment and gameplay work remains
active; do not repeat the already completed LAS or creation slices.

## Actual formula and perk inputs qualified

6 November 2026, parent `f791dc37977650ecc21450375b45654baffe78a1`.
The recovered build16042 catalog resolves the bank formula/perk input gate.
This is data and existing Source qualification; no production/schema, deployed
configuration, default unlock or grant changes are made.

The focused helper checks six selected payloads against the retained recovery
manifest and independent original-index catalog, including decoded SHA256/SHA1,
packed SHA256/indexed SHA1 and extents. Owning C# readers parse GameFormula,
GuildPerk, GuildPermission, Achievement, Spell4 and English text. It loads1217
formula rows, all25 perks and all30 permission rows. Both manifests retain their
385-file identity; the previous independent decompression qualification is reused.

### Bank dimensions and withdrawal selectors

GameFormula644 has Dataint0=128. Therefore the actual client-table value is128
slots, rather than the native helper's64-slot missing-row fallback.
The retained native selector-to-formula mapping gives:

| Packed selector | Lua choice ID | Actual count/source |
| ---: | ---: | --- |
|0|1|0, native constant|
|1|2|1, GameFormula757|
|2|3|2, GameFormula758|
|3|4|5, GameFormula759|
|4|5|10, GameFormula760|
|5|6|25, GameFormula761|
|6|7|50, GameFormula762|
|7|8|−1, native unlimited sentinel|

These are item-withdrawal selector values, not currency limits or a reset
calendar. They do not establish how many tabs a newly created guild owns.

### Bank tab purchase catalog

The table's Lua names and English titles identify the following chain. Costs
below are raw PurchaseInfluenceCost fields; no display scaling or earning rule
is inferred.

| Display tab | Perk ID | Raw purchase cost | Required perk |
| ---: | ---: | ---: | ---: |
|1|36|100000|none|
|2|3|250000|36|
|3|7|500000|3|
|4|8|750000|7|
|5|9|1000000|8|

All five have zero activation cost/spell. A purchase uses the perk ID, preserving
the mapped operation25 UInt64 contract. Catalog presence does not grant Tab1 or
prove an initial unlocked-tab policy.

All25 actual perk IDs match the current Source enum. Every dependency resolves
and the complete prerequisite graph is acyclic. All eight nonzero activation
spell references resolve. All five achievement prerequisites resolve to rows
carrying the Guild flag. Each perk's English title and description resolves.
The three bank-related permission rows are bSpendInfluence/id4,
bBankTabRename/id13 and bBankTabLog/id29, each with GuildTypeEnumFlags=2.
Their IDs correspond to the current Source bit constants; this is a catalog/
Source join, not qualification of every server permission or WarParty policy.

### Evidence and remaining integration

The helper build passes with zero warnings/errors in2.77s and semantic/data
qualification passes in1.74s. The initial helper string-return compile diagnostic
is retained separately; no production failure was reproduced or claimed.
All29 previously mapped native spans independently match bytes in the same pinned
PE. This rebinds retained evidence without a new Ghidra/REA/client operation.
No Source build or suite was rerun because the relevant fourteen Source inputs
are unchanged. The [portable record](coverage/guild-bank-catalog-20261106.json)
binds the helper, six payloads, reader/enum assemblies, graph/joins, Source
observations, retained native spans and final reviews. Raw evidence remains under
`artifacts/continuation-20261106/guild-bank-catalog/`.

The catalog gate is closed; the bank is still unfinished. GuildDataModel/GuildModel
have no bank balance, tab/custody, influence wallet or purchased-perk graph.
Character/guild debit-credit settlement, durable counters and committed financial
replies remain Source implementation work. Authenticator authority, banker access
and daily reset policy still need their specific evidence. Activation/spell
application and consumable perk delivery require their own integration; a resolved
Spell4 reference is not an implemented effect. Configured providers, copied-upgrade,
failure/restart, sockets/deployment and native/retail remain open. Keep financial
handlers diagnostic until a coherent service connects these requirements.
Continue independent actionable gameplay/progression; client/CUA remain stopped.
