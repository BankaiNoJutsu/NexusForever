# Retail Feature Evidence (Online Sources)

Updated: 2026-05-22 (pass 7)

Companion to `MISSING_FEATURE_MATRIX.md`. This document records **how blocked or partial NexusForever systems reportedly worked on live WildStar**, gathered from public wikis, Carbine-era articles, **archived patch notes**, **player blogs/forums**, **non-English guides (DE/FR)**, **addon READMEs**, and **UI screenshots**.

**Pass 6 goal:** pin the **Certain retail state of the last client (build 16042 / 1.7.8)** and separate **normal F2P-era mechanics** from **Sept 2018 sunset buffs** that were not the long-run rules.

**Pass 7 goal:** crosswalk terminal-baseline claims to **Carbine client Lua (S7)** and **NexusForever retail constants (S10)**; flag emulator gaps.

It does **not** replace client decompile, packet captures, or game-table proof. Use it to form hypotheses and prioritize decode work.

### Research passes

| Pass | Focus |
| --- | --- |
| **1** | Wikis, Carbine-era press, Ten Ton Hammer / Gamepressure / Massively OP |
| **2** | Patch archives, winter beta pastebin, Mein-MMO (DE), addon galleries, strain patch loot |
| **3** | Slash-command archive, Notes from Nexus Drop 6 calendar, Taugrim arena leaderboards, Bio Break + FR housing, Protogames expedition medals, vgolds AH mail flow, OwnedCore guides, Steam/NCsoft Homecoming, discovery-map community sites |
| **4** | **Carbine official addon Lua** ([Zod-/Wildstar-Carbine-Addons](https://github.com/Zod-/Wildstar-Carbine-Addons)) for Mail COD / Leaderboards / Challenge share UI; Steam **1.7.1** patch notes (PvE leaderboards, realm bank); Wildstar Life **1.0.9 Strain** full notes (warplot queue); July 2014 PvP deserter fixes |
| **5** | Winter beta pastebin **full text** ([raw](https://pastebin.com/raw/Nfy3hr3f)); more **S7** files (`Stuck.lua`, `HousingRemodel.lua`, `MatchMaker.lua`); wiki **Sabotage** warplot surrender label; guest-pass KB link (archived fetch blocked) |
| **6** | **Terminal client baseline** — build **16042 (1.7.8)**; F2P/Signature wiki; **Homecoming** communities wiki + patch 09/06/2017; last wiki patch **12/06/2017** (1.7.4.15948); **Signing Off** Steam post (26 Sep 2018); shutdown 28 Nov 2018 |
| **7** | **S7** `MarketplaceListings.lua`, `RealmBankViewer.lua`; **S10** `RetailCertainRules.cs`, `MarketplaceAccountLimits.cs`, `MarketplaceAccountLimitsTests`, `ClientGuildRegisterHandler` (formula **1159**), `MatchingDeserterManager`, `RetailWarplotQueueRules` |

Pass 7 added the **emulator ↔ retail crosswalk** and confirmed several 16042 rules already exist in source/tests; documented partial wiring (deserter cross-queue, votekick cooldowns).

## Evidence categories

| Category | Meaning | Safe emulator use |
| --- | --- | --- |
| **Certain** | Multiple independent public sources agree on player-visible behavior, or one authoritative wiki plus corroboration. | OK for compatibility UX and docs; still verify wire semantics in-repo. |
| **Need confirmation** | Plausible retail behavior from one guide, beta notes, interview, or post-launch patch; details may have changed (especially F2P). | Use as design hint; confirm with decomp/sniff/tables before state mutation. |
| **Not sure** | No solid public retail description; name inferred from opcodes/code only; or sources disagree / are from other games. | Do not implement from this doc alone. |

## Source tiers (for citations below)

- **S1** — Official or near-official: [WildStar Wiki](https://wildstar.fandom.com/wiki/WildStar_Wiki), [archive wiki](https://wildstaronline-archive.fandom.com/wiki/WildStar), Carbine interviews, Steam/NCsoft news.
- **S2** — Reputable third-party guides: Ten Ton Hammer, Gamepressure, Engadget, PC Gamer, Massively OP, Notes from Nexus.
- **S3** — Community blogs, forums, addon docs, post-shutdown analysis (WildStar Logs, GreenyNeko, etc.).
- **S4** — Archived patch notes: [Wildstar Life patch notes](http://wildstar.mmorpg-life.com/patch-notes/), [Pastebin winter beta notes](https://pastebin.com/Nfy3hr3f), Fandom patch pages.
- **S5** — Non-English retail-era articles (e.g. [Mein-MMO DE](https://mein-mmo.de/)).
- **S6** — Addon CurseForge galleries/READMEs showing client UI labels (e.g. [AutoLoot v1.2 screenshot](https://media.forgecdn.net/attachments/198/64/autoloot-v1_2.png)).
- **S7** — Carbine-shipped client UI Lua mirrored on GitHub ([Zod-/Wildstar-Carbine-Addons](https://github.com/Zod-/Wildstar-Carbine-Addons)): Mail, Leaderboards, Challenges, Stuck, HousingRemodel, MatchMaker, **MarketplaceListings**, **RealmBankViewer**, MarketplaceAuction/Commodity/CREDD (NCsoft copyright).
- **S8** — Primary-source beta documents: [Pastebin winter beta (raw)](https://pastebin.com/raw/Nfy3hr3f) (Group Finder, Housing, Challenges sections).
- **S9** — Terminal-era official posts: [Steam Signing Off (26 Sep 2018)](https://steamcommunity.com/app/376570/allnews/); [Internet Archive client 16042](https://archive.org/details/wildstar_client); wiki patch pages through **Patch 12/06/2017**.
- **S10** — NexusForever retail alignment code cited in this doc: `Source/NexusForever.Game/Retail/RetailCertainRules.cs`, `Source/NexusForever.Game/Marketplace/MarketplaceAccountLimits.cs`, related handlers/tests (build **16042** F2P era, excludes sunset buffs per file comments).

---

## Emulator ↔ retail crosswalk (pass 7)

Maps **terminal-baseline (16042)** claims to **client Lua** and **emulator code**. Status meanings:

| Status | Meaning |
| --- | --- |
| **Aligned** | Constants/tests/handlers match S7/S9; safe default for emulator |
| **Table-driven** | Retail values come from client `.tbl` at runtime (wiki numbers are corroboration only) |
| **Partial** | Evidence + constants exist; queue/enforcement not fully wired |
| **Gap** | Documented retail rule not yet enforced in emulator |

| Retail claim (16042 normal) | S7 / S9 | Emulator (S10) | Status |
| --- | --- | --- | --- |
| AH/CX **3** slots Free, **30** Signature | `MarketplaceListings.lua` uses `CodeEnumRewardProperty.AuctionBids`, `AuctionListings`, `CommodityOrders` + entitlements `ExtraAuctions`, `LoyaltyExtraAuctions` | `MarketplaceAccountLimits` (3/30) + `MarketplaceAccountLimitsTests`; `GlobalMarketplaceManager` enforces sell cap | **Aligned** |
| Store +50 AH/CX entitlement SKUs | Same Lua listens to `AccountEntitlementUpdate` | `ExtraAuctions` / `ExtraCommodityOrders` +50 per stack in `MarketplaceAccountLimits` | **Aligned** |
| Cosmic +10 AH/CX | `LoyaltyExtraAuctions`, `LoyaltyExtraCommodityOrders` in Lua | `LoyaltyExtra*SlotsPerStack = 10` | **Aligned** |
| Community create **50p** or **600 Service Tokens** | Wiki + Communities page | `ClientGuildRegisterHandler` → `GameFormula.GetEntry(1159)` (`Dataint0` credits, `Dataint01` service tokens); `RetailCertainRules.CommunityCreateCostGameFormulaId = 1159` | **Table-driven** (verify against extracted `GameFormula.tbl` from 16042) |
| Guild create credit cost | Client `GuildLib` formula **764** | `ClientGuildRegisterHandler` entry **764**; `RetailCertainRules.GuildCreateCostGameFormulaId` | **Table-driven** |
| PvE deserter **15m** (scaled), PvP **10m** | S8 beta; Strain patch; addon `MatchingPenaltyUpdated` (BCF) | `MatchingDeserterManager` + `MatchingQueueValidator.CanQueue` cross-activity | **Aligned** (2026-05-23; was Partial) |
| Warplot queue **10 online** | Strain 1.0.9 | `RetailWarplotQueueRules` checks `WarplotOnlineMembersRequiredToQueue` | **Partial** — queued-member count (**10 in queue**) not validated in same helper |
| Votekick **10m / 2m** offline | S8 beta; EMM `MatchVoteKickBegin/End` | `Match.Retail.TryInitiateVoteKick` + `RetailCertainRules.VoteKickCooldown*` | **Aligned** (2026-05-23) |
| Warplot surrender **10m**, **60%** | Sabotage + warplot beta; EMM surrender UI | `PvpMatch.Surrender` + `RetailCertainRules.WarplotSurrender*` | **Aligned** (2026-05-23) |
| **Realm bank** per realm | Steam 1.7.1 | `RealmBankViewer.lua`: `SharedRealmBankUnlock` (74), `SharedRealmBankSlots` (75); `ClientEntityInteraction` case **67** `ShowRealmBank` | **Partial** — UI/entitlements modeled; storage logic not cross-walked here |
| **2** concurrent challenges | Wiki + MOP essay | `ChallengeManager` `MaxConcurrentActiveChallenges = 2`; `RetailCertainRules` duplicate | **Aligned** (duplicate const; could share `RetailCertainRules`) |
| Mail **COD**, expiry, VIP cash gate | `Mail.lua` | Mail manager COD path (pass 4) | **Aligned** (per prior passes) |
| Housing harvest split | `HousingRemodel.lua` | Residence APIs in `Residence` / housing handlers | **Partial** — verify `SetNeighborHarvestSplit` server mirror |
| Signature = `Permission.Signature` | F2P wiki | `Player.SignatureEnabled` → RBAC | **Aligned** |
| Sunset universal Signature / Omnibit store | S9 Signing Off | Not in `RetailCertainRules` (explicitly excluded) | **By design** — do not enable for default emulator |

### Carbine marketplace / bank UI (pass 7)

| File | Retail behavior evidenced |
| --- | --- |
| [Live/MarketplaceListings/MarketplaceListings.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/MarketplaceListings/MarketplaceListings.lua) | Listing caps from `GetPlayerRewardProperty(AuctionBids/AuctionListings/CommodityOrders)` vs tier-0 base; CREDD header has **no limit**; entitlement hooks for Extra + Loyalty auction/CX slots |
| [Live/RealmBankViewer/RealmBankViewer.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/RealmBankViewer/RealmBankViewer.lua) | **Shared realm bank** gated by `SharedRealmBankUnlock`; extra slots via `SharedRealmBankSlots` + store link `UnlockRealmBank` / `RealmBankSlots` |
| [Live/MarketplaceCREDD](https://github.com/Zod-/Wildstar-Carbine-Addons/tree/master/Live/MarketplaceCREDD) | CREDD exchange UI (Signature time commodity) — pairs with F2P wiki |

Repo mirrors client enums in `Source/NexusForever.Game.Static/Entity/RewardPropertyType.cs` (`AuctionBids = 16`, `AuctionListings = 17`, `CommodityOrders = 15`) and `EntitlementType.cs` (`ExtraAuctions = 23`, `LoyaltyExtraAuctions = 43`, `SharedRealmBankUnlock = 74`).

---

## Retail terminal baseline (emulator target)

Use this section when implementing NexusForever against **build 16042** (README baseline). It describes **normal live rules on the final client**, not the **two-month sunset carnival** unless you intentionally model shutdown week.

### Client and timeline

| Milestone | Date / ID | Notes |
| --- | --- | --- |
| **Emulator client** | **Build 16042** (~**1.7.8**) | Last non-Steam client widely archived pre-shutdown; matches NexusForever README. **S9** [Archive.org mirror](https://archive.org/details/wildstar_client) labels **16042 (1.7.8)**. |
| **Last wiki-numbered patch** | **6 Dec 2017** — [Patch 12/06/2017](https://wildstar.fandom.com/wiki/Patch_12/06/2017) “Primetime” | Build **1.7.4.15948**, API **16**. Mostly fixes/events; not a new drop. |
| **Last major content drop** | **6 Sep 2017** — [Homecoming](https://wildstar.fandom.com/wiki/Patch_09/06/2017) | **Communities**, Residential Renovation, more Prime content, `/com` chat. |
| **F2P relaunch** | **29 Sep 2015** — [WildStar: Reloaded](https://www.prnewswire.com/news-releases/carbine-studios-wildstar-free-to-play-launches-today-300150262.html) | AH/CX returned after brief pre-launch shutdown; **Signature** replaces subscription. **S1** [Free-to-Play](https://wildstar.fandom.com/wiki/Free-to-Play). |
| **Sunset announcement + final update** | **26 Sep 2018** | Real-money purchases disabled; **Signing Off** buffs (see below). **S9** Steam announcement. |
| **Service shutdown** | **28 Nov 2018** (~5pm EST) | Servers offline; no further client builds documented publicly. **S2** [Wikipedia](https://en.wikipedia.org/wiki/WildStar). |

**Version note:** Wiki patch tables stop at **1.7.4.15948** while archived clients read **1.7.8 / 16042**. Treat **16042** as the authoritative **wire/client** target; treat **1.7.4–1.7.8** as one continuous **API 15–16** retail era for gameplay rules.

### Normal retail rules on 16042 (Certain — implement these)

These systems were live on the final client **before** sunset buffs and stayed structurally the same; passes 1–5 evidence still applies.

| Area | Final-client behavior (headline) | Primary sources |
| --- | --- | --- |
| **Account model** | **Free** accounts: full level/path/content access; **Signature** optional (CREDD or cash); **Cosmic Rewards** unlock social/AH extras. | **S1** F2P wiki; **S1** [Cosmic Reward](https://wildstar.fandom.com/wiki/Cosmic_Reward) |
| **Marketplace** | **Auction House** + **Commodity Exchange** active; wins/refunds via **mail**; **Free** = **3** buy + **3** sell slots each; **Signature** = **30** + **30** (Cosmic tiers add +10). | **S1** F2P wiki; **S7** MarketplaceListings; **S10** `MarketplaceAccountLimits` + tests |
| **CREDD** | Tradeable **Signature time** on CX (post-F2P “Signature” naming); not subscription-only. | **S1** CREDD archive; **S5** Mein-MMO |
| **Housing** | Per-character plot at **14**; neighbors/roommates; **Communities** (5 plots + shared decorate); harvest **%** split UI; Residential Renovation monthly. | **S1** Housing + [Communities](https://wildstar.fandom.com/wiki/Communities); **S7** HousingRemodel; **S8** beta |
| **Group / queue** | Group Finder; **requeue**; **votekick** timing; **15m** deserter (scaled); cross-activity queue; loot **Need/Greed/Master/RoundRobin**. | **S8** beta; **S7** MatchMaker; patch 1.0.9 |
| **Warplots** | 40v40; queue **10 online + 10 in queue**; **surrender** after 10m / 60% vote. | Strain patch; Sabotage + warplot beta doc |
| **Prime / endgame** | **Prime** dungeons/expeditions/raids; **PvE leaderboards**; **realm bank** (per-realm account storage). | Steam **1.7.1**; **S7** Leaderboards + RealmBankViewer; **S10** entitlements 74/75 |
| **Mail** | Attachments, tiers, **COD**, expiry display, VIP cash-attachment gate. | **S7** Mail.lua; **S1** Mail wiki |
| **Challenges** | Timed challenges, share accept/reject, medal tier → **reward win chance**. | **S7** ChallengeLog; **S8** beta |
| **Store currencies** | **Omnibits** (gameplay), **Protobucks** (real-money store), **Service Tokens**, **Fortune Coins**. | **S1** Currency wiki |
| **Stuck** | `/stuck` UI → recall bind / house / death. | **S7** Stuck.lua; slash archive |

### Sunset-only overrides (26 Sep – 28 Nov 2018)

**Do not treat these as default emulator economy** unless modeling the literal shutdown window. All are **Certain** from **S9** [Signing Off Steam post](https://steamcommunity.com/app/376570/allnews/).

| Sunset tweak | Effect |
| --- | --- |
| Universal **Signature** | Every account granted Signature (overrides Free vs Signature AH limits). |
| **Protobucks → Omnibits** 1:1 | Real-money store currency converted; all store SKUs buyable with Omnibits. |
| **Omnibit** drops/cap raised | Faster cosmetic/convenience purchasing. |
| **Store catalog unlocked** | All seasonal/rotating costumes/mounts + Signature Station items on main store. |
| **Prime gear ilevel** buff | Higher base ilevel from Prime instances/raids. |
| **Primal Essence** rate up | Progression acceleration. |
| **Select reputations** maxed | Vendor inventory unlocked without grind. |
| **Always-on XP/Glory/Prestige/PvP/Essence** buffs | Sept 26 – shutdown (event flags, not base rules). |
| **Real-money purchases disabled** | July 1+ purchases refunded; Steam purchases stopped. |

### Communities at final client (Certain detail)

From **S1** [Communities](https://wildstar.fandom.com/wiki/Communities) + **S1** [Patch 09/06/2017](https://wildstar.fandom.com/wiki/Patch_09/06/2017):

- **Create** community: housing unlocked; **Signature** *or* Cosmic **Full Social Access**; **50 platinum** or **600 Service Tokens**; Protostar Community Director in capitals.
- **Skyplot:** up to **20** members, **5** resident plots + shared common area; `/com` channel; ranks/permissions like a small guild.
- **Decor limits:** community crate **5000**; up to **4000** decor in shared area; per-plot limits stack (wiki cites very high combined totals).
- **Chat/API:** Apollo **API 16**; commands `/cominvite`, `/comkick`, `/commaster`, etc.

At **sunset**, universal Signature made the Signature gate moot for new communities; emulator should still enforce **Signature OR Full Social Access** for normal retail.

### AH / CX limits at final client (Certain)

| Account type | Auction House | Commodity Exchange |
| --- | --- | --- |
| **Free** | 3 active buy bids, 3 sell lots | 3 buy orders, 3 sell orders |
| **Signature** | 30 / 30 | 30 / 30 |
| **Cosmic tier bonuses** | +10 auctions/bids (and +10 CX orders per tier perks) | per **S1** Cosmic Reward wiki |

Box purchasers before **29 Sep 2015** retained **12** character slots, **6** costume slots, **5** bank slots, **2000** decor cap (grandfathered).

### What changed vs launch (not sunset) — still true on 16042

| Launch (2014 sub) | Final client (16042) |
| --- | --- |
| Subscription required | **F2P** + optional **Signature** |
| CREDD = sub time | CREDD = **Signature** time on CX |
| Shiphands | **Expeditions** + Prime + veteran medals |
| Neighbors only | **Communities** + neighbors + roommates |
| No PvE leaderboards | **Prime PvE leaderboards** (1.7.1+) |
| No realm bank | **Shared realm bank** (1.7.1+) |
| 2013 “housing per account” press | **Per-character** housing at 14 (wiki + beta) |

---

## Summary by matrix row

| ID | System | Certain (headline) | Need confirmation | Not sure |
| --- | --- | --- | --- | --- |
| F-001 | STS / token crypto | Subscription-era account required login | Handshake field order, optional routes | Full crypto envelope semantics |
| F-002 | Client diagnostic opcodes | Client sent fixed-size unknown requests | Per-opcode UI owner | Any server reply behavior |
| F-003 | Unresolved server opcodes | Server emitted many auxiliary packets | Cluster grouping by enum neighbors | Per-opcode gameplay meaning |
| F-004 | Housing | Plugs, neighbors, **Communities** (5 plots), harvest split; level-14 **per character** | 2013 press “per account” | Many `Server0x00CA` field names |
| F-005 | Marketplace / CREDD | AH+CX live on 16042; **3/3 Free**, **30/30 Signature**; CREDD→Signature; mail settlement | Fee %; CX interest mechanics | Order-matching / offline settlement |
| F-006 | Store / account inventory | Omnibits, Protobucks, Service Tokens; Cosmic Rewards; **sunset** = all-Omnibit store | Coupon sender; sunset universal Signature | Leading fields on `096A..096C` |
| F-007 | Reward rotation | **180-day** daily login (Drop 6); 9+1 reward rhythm; unclaimed until login | Opcode-named “rotation” vs login calendar | `0x07CD` apply / `Flag` consumer |
| F-008 | Crafting | Tech tree, schematics, circuit/coordinate crafting | Discovery rolls, durable rune data/result rules | `ServerCraftingAuxFourUInt32FloatUInt32` / `ServerCraftingAuxUInt32AndTwoFloats` emit semantics |
| F-009 | Transport | Taxi, transmat recall, mounts, service tokens | Flight-path purchase rules | Vehicle seat / deployable modes |
| F-010 | Group / queue / raid | Attunement; loot rules; deserter (cross-activity queue rules); **requeue** when instance finished; votekick rules | Fake-tank LFG anecdotes | Most `Server0x0414+` field effects |
| F-011 | Guild / war party | Warplots: 40v40; **10 online + 10 queued** to match; boss tokens | Guild bank, holomark perks | Recruitment subscription timing |
| F-012 | ICComm / chat | Circles, guild/zone/party/**/com** community channels | ICComm = Global/Group/Guild (decomp) | Entitlement / throttle / persistence |
| F-013 | Mail | COD; tiered fees; inbox **expiry countdown** (days); guest `Mail_GuestAccount` error; VIP attachment gate | Hybrid/Signature trading unlock | Delete-state parity across reload |
| F-014 | Loot | **FreeForAll, RoundRobin, NeedBeforeGreed, Master** (patch + enum); trash rolls to group | BOP confirm; `ServerLootCanLoot` timing | Master-loot assign packet parity |
| F-015 | PvP / duels | Duels `/duel`; PvE/PvP rulesets; arena seasons | Duel leash, observer packets | PvP rating formula parity |
| F-016–F-020 | Spell runtime | Players had procs, CC, summons (felt in combat) | Per-family formulas | Proc order, stack groups, Ravel graph |
| F-021 | LAS / action set | LAS, AMP, action bars existed | Async spell update transaction | `Server0x00B0` meaning |
| F-022–F-024 | Quests / content | Adventures, public events, paths documented | Path mission type 1 | Per-zone script blockers |
| F-025 | Entity / CSI | Core create/update visible in play | Deferred queues, phase rules | Entity stat packet fields |
| F-026 | Items / unlocks | Costumes, pets, titles, holo-wardrobe | Unlock list deltas | `Server0x00B7` item errors |
| F-027 | Options | Combat log / casting options exist | Account-wide option split | Option readback packets |
| F-028 | Support / stuck | **`/stuck`** UI: recall bind / house / death w/ cooldowns; transmat; housing teleporter | GM page workflow | DB moderation workflow |
| F-029 | Client DB / mapping | Tables drive items, zones, challenges | Each promotion case | EF placeholder renames |
| F-030 | Realm transfer | Paid transfer; later free megaserver moves | Online handoff | PTR copy queue |
| F-031 | Fortune | Madame Fay; Fortune Coins in store | Payout tables | Resume session across logout |
| F-032 | Leaderboards | **PvE** (dungeon/solo/group expedition) + **PvP** (arena 3v3, BG by class); prime/medal filters | WildStar Logs vs in-game board parity | Row schema edge cases |
| F-033 | Challenges | Timed challenges, 30m CD; share accept/reject; **gold/silver/bronze → reward win chance** (beta) | Share timeout (emulator 30s); social redesign essay | `Client0x00C8` owner |
| F-034 | Codex / archive | Galactic Archive, datacubes, journals | Path-mission archive rules | Link parent authorization |
| F-035 | Achievements | Achievements, realm-firsts existed | Steam payload grammar | All trigger coverage |
| F-036 | Zone map | Exploration, discoveries, map collectibles | ZoneCompletion row categories | Faction/path completion totals |

---

## Deep pass corroboration (patch notes, forums, addons)

High-confidence additions from the second pass. Per-row sections below repeat only row-specific bullets; this table is the cross-row index.

| Topic | Category | Evidence |
| --- | --- | --- |
| Group loot: Need vs Greed, Master Looter, round robin | **Certain** | Patch **1.0.9 Strain**; repo `LootRule` enum; **S6** AutoLoot Need/Greed UI |
| Trash loot rolls to group members | **Certain** | Patch 1.0.9 Strain |
| Dungeon/BG deserter **15 min**, scales with completion | **Certain** | **S4** [Winter beta pastebin](https://pastebin.com/Nfy3hr3f) |
| **Requeue** after dungeon (group leader) | **Certain** | **S8** winter beta; **S7** `MatchMaker.lua` (`GroupJoin` when `MatchingGameLib.IsFinished()`) |
| **Votekick**: **10 min** before initiating; **2 min** if target offline or not in instance | **Certain** | **S8** winter beta |
| Cross-activity deserter: dungeon deserter can still queue **PvP**; BG deserter can queue **PvE/Rated Arena** | **Certain** | **S8** winter beta |
| Group Finder **My Realm Only** opt-in for dungeons | **Certain** | **S8** winter beta |
| Warplot **Surrender Match** label; `/votesurrender`; **10 min** + **60%** vote | **Certain** | **S1** [Patch 07/31/2014 Sabotage](https://wildstar.fandom.com/wiki/Patch_07/31/2014); **S8** [warplot beta doc](https://pastebin.com/U8Tn4dsC) |
| PvP deserter **5→10 min** (later patch) | **Certain** | Patch 1.0.9 Strain |
| Warplot queue needs **10 online**, **10 in queue** | **Certain** | **S4** [Strain 1.0.9 notes](http://wildstar.mmorpg-life.com/patch-notes/wildstar-patch-notes-1-0-9-strain/) |
| Winning auctions / bids delivered by **mail** | **Certain** | **S4** winter beta pastebin |
| `ItemAuctionWon` marketplace event | **Certain** | Patch 1.0.9 Strain |
| Add neighbor only while **standing on your plot** | **Certain** | **S2** [Gamepressure housing social](https://www.gamepressure.com/wildstar/housing-social/zb6488) |
| Owner receives harvested mats; can share with harvester | **Certain** | **S2** Gamepressure |
| Neighbors can complete **owner’s plot challenges** | **Certain** | **S2** Gamepressure |
| Plugs stop producing when **repair** needed | **Certain** | **S3** [Requnix housing review](https://requnix.com/wildstar-housing-review/) |
| Residence settings **Ctrl+F2**; daily **buff board** (PvE/PvP/group %) | **Certain** | Requnix; **S2** IGN housing preview |
| Stuck on plot: **Return to Teleporter** | **Certain** | **S3** Steam Housing 101 |
| Harvest yield **% distribution** (owner vs neighbor) in **Property Settings** | **Certain** | **S8** winter beta; **S7** `HousingRemodel.lua` (`SetNeighborHarvestSplit`, separate **garden** split) |
| Housing unlocked at **level 14** per character (capital quest) | **Certain** | **S8** winter beta; **S1** wiki |
| Housing plots **per account** (2013 press) vs retail **per-character** unlock | **Need confirmation** | **S2** MMORPG.com 2013 press vs **S8** + **S1** wiki |
| AH closed pre-F2P; listings returned by mail; CREDD **€16.99** + **22h** cooldown | **Certain** | **S5** [Mein-MMO](https://mein-mmo.de/wildstar-auktionshaus-credd-f2p/) |
| Rapid Transport **while mounted** | **Certain** | **S4** winter beta Patch 1 |
| Challenge **gold/silver/bronze** increases **reward win chance** | **Certain** | **S8** winter beta |
| Mail inbox shows **expiry** (days/hours/minutes; red under ~16 days) | **Certain** | **S7** `Mail.lua` (`fExpirationTime`, `CRB_Expired`) |
| Guest account blocked from mail action (`Mail_GuestAccount`) | **Need confirmation** | **S7** Mail.lua `MissingEntitlement`; **S3** [guest-pass KB](https://support.wildstar-online.com/entries/63202917) (fetch blocked) |
| VIP account: mail **cash attachments** gated (`VIP` premium system) | **Certain** | **S7** Mail.lua (`bCashTradeLimited`, `BlockerVIP`) |
| Stuck UI: **Recall Bind / House / Death** with cooldown display | **Certain** | **S7** `Stuck.lua` (`SupportStuckAction`) |
| AutoLoot addon: built-in Need/Greed roll UI | **Certain** | **S6** [AutoLoot CurseForge](https://www.curseforge.com/wildstar/addons/autoloot) |
| **`/stuck`** opens player stuck interface | **Certain** | **S1** [Slash commands archive](https://wildstaronline-archive.fandom.com/wiki/WildStar_Slash_commands); **S2** Wildstar Life |
| In-game **2v2 Arena Leaderboards** (top 250) | **Certain** | **S3** [Taugrim Oct 2014](https://taugrim.com/2014/10/12/two-thirds-of-the-top-rated-players-in-wildstar-2v2-arena-are-inactive/) |
| Drop 6 **180-day** daily login calendar | **Certain** | **S2** [Notes from Nexus](http://wildstar.bigdamnheroes.org/?p=1657) |
| AH/CX sales pay gold **via mail** | **Certain** | **S3** [vgolds AH guide](https://www.vgolds.com/news/detail/30977-news.html) |
| Challenges should be **social** (same mobs/glowies) | **Need confirmation** | **S2** Massively OP redesign essay |
| **Shiphands → Expeditions**; veteran medal UI pane | **Certain** | **S2** [PCGamesN Protogames](https://www.pcgamesn.com/wildstar/wildstar-s-protogames-initiative-update-is-colossal-new-missions-for-both-low-and-high-level-players) |
| Housing buff board **23h**, **lost on death** | **Certain** | **S2** [Gamepressure housing](https://www.gamepressure.com/wildstar/6-housing/z3626e) |
| Neighbor permissions via **Social tab** right-click | **Certain** | **S2** [Wildstar Life housing](http://wildstar.mmorpg-life.com/guides/wildstar-housing-system/) |
| **Mail Helper** addon (bulk take mail) | **Need confirmation** | **S3** Gaming By The Numbers |
| **Mail COD** send/accept/reject UI | **Certain** | **S7** `Live/Mail/Mail.lua` (`CashCODBtn`, `PayCoD`, `AcceptCODBtn`) |
| **PvE leaderboards** in-game (Prime dungeons/expeditions) | **Certain** | **S7** `Leaderboards.lua` + Steam **1.7.1** |
| **Shared challenge** accept/reject notice | **Certain** | **S7** `ChallengeLog.lua` (`ChallengeShared`, `AcceptSharedChallenge`) |
| PvP deserter persists through **death** | **Certain** | **S4** [July 15 2014 patch](http://wildstar.mmorpg-life.com/patch-notes/wildstar-patch-notes-july-15th/) |
| `ItemAuctionWon` fires for bidder **and** owner | **Certain** | **S4** Strain 1.0.9 notes (addon marketplace section) |
| **Terminal client** build **16042 (1.7.8)** | **Certain** | **S9** [Archive.org client](https://archive.org/details/wildstar_client); NexusForever README |
| **F2P** relaunch; AH/CX **3/3** Free, **30/30** Signature | **Certain** | **S1** [Free-to-Play](https://wildstar.fandom.com/wiki/Free-to-Play) |
| **Communities** (5 plots, 20 members, Signature or Cosmic social) | **Certain** | **S1** [Communities](https://wildstar.fandom.com/wiki/Communities); patch 09/06/2017 |
| **Realm bank** (account-wide per realm) | **Certain** | Steam/MOP 1.7.1 (May 2017); present on 16042 |
| **Sunset** universal Signature + Omnibit store (Sep 2018) | **Certain** (sunset-only) | **S9** [Steam Signing Off](https://steamcommunity.com/app/376570/allnews/) |
| Service shutdown **28 Nov 2018** | **Certain** | **S9** Steam; Wikipedia |
| AH/CX limits enforced server-side (3/30) | **Certain** | **S7** MarketplaceListings + **S10** `MarketplaceAccountLimitsTests` |
| Community create cost formula **1159** | **Certain** (table-driven) | **S10** `ClientGuildRegisterHandler`; **S1** wiki 50p/600 tokens |
| Realm bank entitlement **74/75** | **Certain** | **S7** RealmBankViewer.lua; **S10** `EntitlementType` |
| Cross-activity deserter queue (beta) | **Certain** (retail) / **Partial** (emu) | **S8** beta; **S10** `MatchingDeserterManager.CanQueue` not wired to validator |

### Screenshot / UI evidence

| Asset | What it shows | Tier |
| --- | --- | --- |
| [autoloot-v1_2.png](https://media.forgecdn.net/attachments/198/64/autoloot-v1_2.png) | Client Need/Greed roll window (addon hooks retail UI) | **S6** |
| RaidOps README | References Carbine **Master Loot** addon | **S3** |

### Carbine client UI sources (pass 4–5)

| File | Retail behavior evidenced |
| --- | --- |
| [Live/Mail/Mail.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Mail/Mail.lua) | **COD** vs gift cash; `PayCoD()`; inbox **expiry** countdown; `GenericError_Mail_GuestAccount`; **VIP** blocks cash attachments unless Hybrid/Signature trading |
| [Live/Leaderboards/Leaderboards.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Leaderboards/Leaderboards.lua) | **PvE**: `PveDungeon`, `PveExpeditionGroup`, `PveExpeditionSolo`; prime level + bronze/silver/gold medal filters; **PvP**: Arena 3v3 rated + battleground class boards |
| [Live/Challenges/ChallengeLog.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Challenges/ChallengeLog.lua) | `ChallengeShared` event → `ShareChallengeNotice`; `AcceptSharedChallenge` / `RejectSharedChallenge`; `SharedChallengePreference.AutoReject` |
| [Live/Stuck/Stuck.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Stuck/Stuck.lua) | Stuck window offers **RecallBind**, **RecallHouse**, **RecallDeath** via `GameLib.SupportStuck()` with per-action cooldown labels |
| [Live/Housing/HousingRemodel.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Housing/HousingRemodel.lua) | Property settings: **NeighborHarvestSplit** + separate **NeighborGardenSplit** dropdowns (`HarvestSharingDropdown`) |
| [Live/MatchMaker/MatchMaker.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/MatchMaker/MatchMaker.lua) | `GroupJoin` re-enabled when instance **finished**; `MatchingFailure_InvalidRequeueType` when match type disallows requeue |
| [Live/MarketplaceListings/MarketplaceListings.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/MarketplaceListings/MarketplaceListings.lua) | AH/CX slot caps via `RewardProperty`; Extra/Loyalty entitlements |
| [Live/RealmBankViewer/RealmBankViewer.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/RealmBankViewer/RealmBankViewer.lua) | Shared **realm bank** unlock/slot entitlements + storefront links |

### Winter beta primary source (pass 5)

| Section | Retail behavior evidenced |
| --- | --- |
| [Pastebin winter beta (raw)](https://pastebin.com/raw/Nfy3hr3f) | Group Finder **requeue**, **votekick** timing, **15m deserter** + cross-activity queue rules, **My Realm Only**, challenge **medal → win chance**, housing **level 14** + harvest **%** split |
| [Warplot PvP beta doc](https://pastebin.com/U8Tn4dsC) | `/votesurrender`; **10 min** elapsed; **60%** warp party; loser rating applied |

---

## F-001 — STS token crypto and optional auth routes

### Certain

- Retail WildStar used account login before world entry; emulator already implements compatibility login paths (repo baseline).

### Need confirmation

- Optional STS envelope fields and external-account edge routes existed in late builds (inferred from code labels, not player guides). **S3** decomp only.

### Not sure

- Safe production behavior for `PremasterSecret`, signature verification, and token rotation without captured STS sessions.

**Sources:** repo `F-001` row; no useful public crypto write-up.

---

## F-002 — Client diagnostic receive opcodes (`Client0x003D`, etc.)

### Certain

- Client could send small fixed payloads on various UI boundaries (enum neighbor ordering in matrix).

### Need confirmation

- Cluster guesses: `00C8` near challenges, `062A`/`0634` near matching, `0760`/`0762` near realm list (matrix only). **S3**

### Not sure

- Whether server ever replied with gameplay state vs log-only.

---

## F-003 — Unresolved server output opcodes (`Server0xNNNN`)

### Certain

- Live client registered readers for hundreds of server messages; many auxiliary updates existed (loot, group, housing, store terminal cluster).

### Need confirmation

- Likely clusters from opcode enum adjacency (housing `00CA+`, group `0414+`, store `0969+`). **S3** matrix.

### Not sure

- Any single opcode’s field semantics without Ghidra reader proof.

---

## F-004 — Housing, neighbors, communities, decor, plugs

### Certain

- Housing unlocked around **level 14** via capital quest *Housing of the Future*. **S1** [Housing](https://wildstar.fandom.com/wiki/Housing)
- Plot has **six FABkit sockets** (four small, two large) plus central house plug; plugs include mines, farms, craft stations, dailies, expeditions, raid portals. **S1** [Housing](https://wildstar.fandom.com/wiki/Housing), **S2** [Fanatical Swordsman](https://thefanaticalswordsman.com/2014/06/27/guide-to-wildstars-housing/)
- Decor ignores physics (floating placement). **S1** [Housing](https://wildstar.fandom.com/wiki/Housing)
- Logging off at home grants **bonus rested XP**. **S1** [Housing](https://wildstar.fandom.com/wiki/Housing)
- Visitor rules: **Private / Neighbors Only / Roommates Only / Public**. **S1** [Housing](https://wildstar.fandom.com/wiki/Housing)
- Harvest permissions: mail owner, share, or visitor keeps all. **S1** [Housing](https://wildstar.fandom.com/wiki/Housing)
- **Neighbors** list: teleport, visit, public plot browser; **roommates** can decorate interior. **S2** [Fanatical Swordsman](https://thefanaticalswordsman.com/2014/06/27/guide-to-wildstars-housing/), **S2** [Wildstar Life housing](http://wildstar.mmorpg-life.com/guides/wildstar-housing-system/)
- **Communities** (2017 Homecoming): five player plots + shared decorate space. **S1** [Housing](https://wildstar.fandom.com/wiki/Housing)
- **Add Neighbor** only while standing on **your own plot** (not from neighbor’s plot). **S2** [Gamepressure housing social](https://www.gamepressure.com/wildstar/housing-social/zb6488)
- When a **neighbor harvests** your plug, **owner receives materials**; owner can **share a portion** with harvester (separate from wiki “visitor keeps all” permission). **S2** Gamepressure
- **Neighbors** can complete **plot challenges** for the owner. **S2** Gamepressure
- **Plugs stop producing** until repaired (mines/farms/craft stations). **S3** [Requnix](https://requnix.com/wildstar-housing-review/)
- **Residence settings** via **Ctrl+F2** (remodel, permissions). **S3** Requnix
- **Buff board** on plot: rotating **24h** bonuses (PvE survival, PvP power, group XP, etc.). **S3** Requnix; **S2** IGN housing preview
- Stuck on plot: **Return to Teleporter** (Steam guide). **S3** Steam Housing 101
- **Neighbor list** under **Social tab**; right-click names for visit/harvest/roommate permissions. **S2** [Wildstar Life housing](http://wildstar.mmorpg-life.com/guides/wildstar-housing-system/)
- **Roommates** decorate **interior only** (not house shell/surroundings); roommate decor spends their gold into owner’s crate. **S2** Wildstar Life
- **Buff board**: once per day pick dungeon XP, quest/monster XP, or PvP buff; **23 hours**; **lost when you die** (Gamepressure). **S2** [Gamepressure housing](https://www.gamepressure.com/wildstar/6-housing/z3626e); **S3** [MMOThis first house](https://www.mmothis.com/2014/06/05/how-to-build-your-first-wildstar-house/) (24h pick-one-of-three wording)
- Only **one** non-garden **harvesting FABkit** per plot (Steam Housing 101). **S3** Steam
- **Homecoming (2017)**: five plots + shared decorate space on one island; Residential Renovation monthly event. **S1** wiki; **S2** [Massively OP Homecoming](https://massivelyop.com/2017/09/06/wildstar-doubles-down-on-housing-with-todays-homecoming-update/); **S1** Steam news
- **Communities** (final client): create at Protostar Community Director — **50 platinum** or **600 Service Tokens**; requires **Signature** or Cosmic **Full Social Access**; up to **20** members, **5** skyplot slots, shared crate/decor permissions, `/com` channel. **S1** [Communities](https://wildstar.fandom.com/wiki/Communities); **S1** [Patch 09/06/2017](https://wildstar.fandom.com/wiki/Patch_09/06/2017)
- **Residential Renovation** runs **one week per month** with rotating décor reward sets (still advertised on Steam through 2018). **S1** patch 09/06/2017; **S9** Steam Oct 2017 post

### Need confirmation

- Neighbor cold-call / public listing farming meta was common. **S3** [In An Age](https://inanage.com/2014/06/11/wildstar-housing-balance/)
- Housing zone chat (`/nn` vendor search) used by players. **S3** [Fanatical Swordsman](https://thefanaticalswordsman.com/2014/06/27/guide-to-wildstars-housing/)
- Plug vendor/repair costs tied to contribution points (emulator maps `HousingContributionInfo.tbl`; public guides mention costs vaguely). **S2** Steam Housing 101 guide
- Wallpaper currency and prerequisite decor (emulator has partial mapping; wiki silent).
- **Neighbor harvest split**: owner configures **% distribution** for non-garden plugs and a **separate garden split** in property settings (`SetNeighborHarvestSplit` / `SetNeighborGardenSplit`). **S7** [HousingRemodel.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Housing/HousingRemodel.lua); **S8** winter beta (“Housing Options”)
- **Housing at level 14** per character via capital Protostar quest (winter beta wording). **S8** winter beta; **S1** wiki
- **Per-account** housing allotment (level 6 in 2013 press) vs retail **per-character** unlock at 14 — reconcile against build era. **S2** [MMORPG.com housing](https://www.mmorpg.com/editorials/wildstar-housing-2000012988) vs **S8** + **S1** wiki
- **HousingTour** addon: public/neighbor plot list semantics match neighbor browser (addon doc only). **S3** CurseForge HousingTour
- **French** press: friends can harvest your garden while you’re away and **share loot**. **S5** [Warlegend FR housing](https://www.warlegend.net/wildstar-presentation-du-housing/)
- **Jump Start Pack**: plot access at **level 3** (store) vs level **14** quest unlock — alternate onboarding. **S3** Steam Housing 101 (FR locale page corroborates)
- **Cosmic Tier 6** Osun house is **account-wide** (Steam guide). **S3** Steam

### Not sure

- Exact neighbor invite duration, eviction broadcast timing, community donation resource transfer.
- Consumer intent for `ServerHousingResidenceKeyedUpdate` and early `0x00CA..00D1` fields (wire mapped in-repo only).

---

## F-005 — Marketplace, auction, commodity exchange, CREDD

### Certain

- **Two NPC markets** in capital: **Auctioneer** (gear/equipment listings) and **Commodity Broker** (crafting mats + **CREDD**). **S2** [Gamepressure](https://www.gamepressure.com/wildstar/auctions-and-credd/zb6489)
- Commodity UI: **Buy Now / Sell Now** vs **Create Buy/Sell Order**; buy-now pairs with opposite order type. **S2** [Gamepressure](https://www.gamepressure.com/wildstar/auctions-and-credd/zb6489)
- **CREDD** = tradeable **30-day** subscription time (later Signature); bought for real money, sold on CX for gold. **S2** [PC Gamer](https://www.pcgamer.com/wildstars-credd-exchange-launches-allowing-players-to-buy-extra-time-with-in-game-cash/), **S1** [CREDD archive](https://wildstaronline-archive.fandom.com/wiki/C.R.E.D.D.)
- Launch-era CX price often cited **3–4 platinum** per CREDD. **S2** [PC Gamer](https://www.pcgamer.com/wildstars-credd-exchange-launches-allowing-players-to-buy-extra-time-with-in-game-cash/)
- **Winning auction items** and **outbid refunds** delivered via **mail** (beta-era note; aligns with AH shutdown “returned by mail”). **S4** [Pastebin winter beta](https://pastebin.com/Nfy3hr3f)
- Patch **1.0.9**: **`ItemAuctionWon`** fires when auction expires with winner; fires for **both bidder and owner**; marketplace addon uses events instead of polling. **S4** [Strain 1.0.9 notes](http://wildstar.mmorpg-life.com/patch-notes/wildstar-patch-notes-1-0-9-strain/)
- Pre-F2P AH closure: listings returned by **mail**; cash-shop CREDD priced **€16.99** with **22h** purchase cooldown; realm transfer also **€16.99** (DE retail article). **S5** [Mein-MMO](https://mein-mmo.de/wildstar-auktionshaus-credd-f2p/)
- **Trade Facilitation Manager** NPC role post-F2P (German guide). **S5** Mein-MMO
- **Equipment vs material** auctions split; **Sell Now** on CX pays gold **through mail**; listings incur **commission** (post–UI 2.0 guide). **S3** [vgolds AH intro](https://www.vgolds.com/news/detail/30977-news.html)
- Retail CREDD priced **$19.99** vs **$15.99** direct subscription (designer interview). **S2** [Game Developer CREDD explainer](https://www.gamedeveloper.com/business/wildstar-s-credd-system-explained)
- **Supernova** addon: AH/CX watch list and outbid tracking (community). **S3** Gaming By The Numbers
- **F2P (29 Sep 2015 onward):** AH and CX **reopened** after brief pre-F2P shutdown; listings returned by mail when offline. **S2** [Massively OP AH shutdown](https://massivelyop.com/2015/09/21/wildstar-shuts-down-the-auction-house-before-the-free-to-play-switch/)
- **Account limits on final client:** **Free** = **3** AH buy bids + **3** sell lots, **3** CX buy + **3** sell orders; **Signature** = **30** each (Cosmic tiers add +10 per perk). **S1** [Free-to-Play](https://wildstar.fandom.com/wiki/Free-to-Play)
- **CREDD** redeems **Signature** time (not legacy sub-only model). **S1** CREDD archive + F2P wiki
- Server enforces **3 / 30** AH+CX slot caps per `MarketplaceAccountLimits` (Signature via RBAC); fourth free listing returns `GenericError.AuctionTooManyOrders` in tests. **S10** `MarketplaceAccountLimitsTests`

### Need confirmation

- Transaction fee **2% or 5 silver minimum**; listings up to **200** items. **S3** [Gaming By The Numbers](https://gamingbythenumbers.com/blog/how-to-make-money-in-wildstar/)
- Large commodity orders stack and move price when absorbed. **S2** [Gamepressure](https://www.gamepressure.com/wildstar/auctions-and-credd/zb6489)
- AH/CX **shut down briefly** before F2P (Sept 2015) for retune (also **S2** [Massively OP](https://massivelyop.com/2015/09/21/wildstar-shuts-down-the-auction-house-before-the-free-to-play-switch/))
- **Nexian Cartel** blog: which item categories listed on AH vs commodity exchange (community economics, not Carbine doc). **S3** Nexian Cartel
- Unused CREDD expires after **~3 months** without active subscription. **S2** [PC Gamer](https://www.pcgamer.com/wildstars-credd-exchange-launches-allowing-players-to-buy-extra-time-with-in-game-cash/)
- Interest charged on commodity buy orders (Gamepressure wording). **S2**

### Not sure

- Persistent order book storage, offline seller settlement, partial fill/cancel precision.
- Non-empty `ServerCREDDExchangeInfoResults` row layout at runtime.

---

## F-006 — Storefront, account inventory, pending groups, generic unlocks

### Certain

- In-game store (**N** key in later builds; **U** in beta notes) sold cosmetics, boosts, Fortune Coins, Service Tokens. **S1** [Currency](https://wildstar.fandom.com/wiki/Currency)
- **Omnibits** — world/quest drops, store cosmetics. **S1** [OmniBit](https://wildstar.fandom.com/wiki/OmniBit)
- **Service Tokens** — store; Wake Here, Holo-Wardrobe, rune modifications. **S1** [Currency](https://wildstar.fandom.com/wiki/Currency)
- **Protobucks** — real-money store currency. **S1** [Currency](https://wildstar.fandom.com/wiki/Currency)
- **Cosmic Rewards** — account tier from shop/sub spend. **S1** [Cosmic Reward](https://wildstar.fandom.com/wiki/Cosmic_Reward)
- Account items could not be double-claimed if already in inventory/bank. **S2** [Notes from Nexus – Beta Patch 1](http://wildstar.bigdamnheroes.org/?p=1685)
- **Final client currencies:** gameplay **Omnibits**; real-money **Protobucks** (disabled for new purchases **26 Sep 2018**); **Service Tokens** for wake/holo/runes and community creation alt cost. **S1** Currency wiki; **S9** Signing Off
- **Sunset (Sep 2018):** Protobucks converted **1:1** to Omnibits; **all** store items (including seasonal rotators and Signature Station SKUs) purchasable with Omnibits; **everyone** granted Signature. **S9** Steam Signing Off — **sunset-only**, not normal 16042 defaults

### Need confirmation

- **Daily login** on catalog open (180-day track, Drop 6) distinct from opcode “reward rotation”. **S2** [Daily Login Rewards](http://wildstar.bigdamnheroes.org/?p=1657)
- Store purchase velocity / privilege restrictions (emulator: 10/hour gate; no public doc). **S3** repo tests
- CREDD redeem **1000:1** to character credits (emulator mapping). **S3** decomp
- VC package purchase grants currency + result packets (emulator). **S3** decomp

### Not sure

- Real-money billing integration.
- Coupon client sender for `0x0790`.
- `0986` / `098F` live emit sites.
- Leading `uint32` on `096A..096C` account cache packets.

---

## F-007 — Reward rotation and reward properties

### Certain

- **Daily Login Rewards** (Drop 6): **180-day** calendar on PTR (full day-by-day list published); pattern of **9 consumable/currency days** then a **day-10** costume/pet/toy/housing prize; **18** days grant Service Tokens; rewards can sit **granted but unclaimed** until next login; non-consecutive days OK. **S2** [Notes from Nexus](http://wildstar.bigdamnheroes.org/?p=1657)
- Day 1 **Nexus Survival Kit** example: 5 Salted Steaks, 5 Vendishot Mk V, 5 Go Juice. **S2** Notes from Nexus

### Need confirmation

- “Reward rotation” in emulator may combine schedule rows (`0x07CA`), content context (`0x07CD`), entry-state (`0x07C8`) — no player-facing name in wikis. **S3** repo matrix only
- Veteran **Renown/Glory** shifts (Drop 4) affected endgame reward vendors, not necessarily same system. **S1** [Patch 02/03/2015](https://wildstar.fandom.com/wiki/Patch_02/03/2015)

### Not sure

- Per-content authoritative reward mapping and `Flag` bit on apply.
- Account grant persistence for rotation entry-state.

---

## F-008 — Crafting and tradeskills

### Certain

- Tradeskill UI: **Schematics**, **Tech Tree**, **Talents** (Codex **L** / **K**). **S2** [Ten Ton Hammer tradeskills](https://www.tentonhammer.com/guides/wildstar-tradeskills-and-crafting-guide)
- New recipes from **Tech Tree achievements**; star nodes grant **talent points**. **S2** [Ten Ton Hammer](https://www.tentonhammer.com/guides/wildstar-tradeskills-and-crafting-guide), **S3** [Nexus Nightly](http://nexusnightly.blogspot.com/search/label/tradeskills)
- **Circuit board crafting** for equippable gear (power budget, microchips). **S2** [Engadget](https://www.engadget.com/2014-02-06-a-look-at-wildstars-crafting-mechanics.html), Carbine econ devblog via **S3** [Crafting Worlds](http://forums.craftingworlds.com/threads/wildstar-crafting-and-item-modding-preview.2604/)
- **Coordinate crafting** for consumables/housing (grid variants, three attempts). **S2** [Engadget](https://www.engadget.com/2014-02-06-a-look-at-wildstars-crafting-mechanics.html)
- Tiers: Novice → Expert; separate runecrafting; cooking hobby. **S3** [Wildstar Life tradeskills](http://wildstar.mmorpg-life.com/guides/wildstar-tradeskills-guide/)

### Need confirmation

- Discovery as achievement-gated vs random drops (guides emphasize tech tree, not RNG discovery). **S2**
- Crafting vouchers from zone dailies for talent respec. **S3** [Wildstar Life](http://wildstar.mmorpg-life.com/guides/wildstar-tradeskills-guide/)

### Not sure

- Random discovery rolls, exact station service-key meanings, the shared-item/DB bridge for durable rune data, and non-success result rules.

---

## F-009 — Rapid transport, taxi, flight path, vehicles

### Certain

- **Taxi**: station-to-station within **same region**; cost by distance; travel not instant. **S1** [Taxi](https://wildstar.fandom.com/wiki/Taxi)
- **Transmat recall**: bind at terminal, recall from anywhere (also recall home). **S3** [Orcz tutorial](https://orcz.com/Wildstar:_Tutorial_-_Binding_and_Recalling)
- **Mounts**: ground ~15, hoverboard ~25; default **Z**; vendors in capitals. **S1** [Mount](https://wildstar.fandom.com/wiki/Mount), **S2** [Mount vendors](http://wildstar.mmorpg-life.com/guides/mount-vendor-locations/)
- **Rapid Transport** usable **while mounted** (winter beta Patch 1). **S4** [Pastebin winter beta](https://pastebin.com/Nfy3hr3f)
- Taxi map option to **hide POI icons** (beta). **S4** winter beta

### Need confirmation

- **Service Tokens** used for convenience services (Wake Here, etc.), possibly overlapping “rapid transport” bypass. **S1** [Currency](https://wildstar.fandom.com/wiki/Currency)
- Flight-path unlock/purchase progression (wiki stub; players reference flight masters). **S1** [Taxi](https://wildstar.fandom.com/wiki/Taxi) — see also Transmat

### Not sure

- Vehicle passenger seats, deployable turrets, `Server0x077E` alias semantics.
- Route-table correlation for taxi vs flight path packets.

---

## F-010 — Group, raid, queue, matching

### Certain

- **Raid attunement** quests are **per-character**, not account-wide; require large groups for world bosses. **S2** [Ten Ton Hammer attunement](https://www.tentonhammer.com/guides/wildstar-attunement-guide)
- Launch raids: **Genetic Archives** (20-man), **Datascape** (40-man); complex mechanics; loot on bosses. **S2** [GamingBolt interview](https://gamingbolt.com/wildstar-interview-raids-more-raids-and-even-more-raids-in-your-mmo), **S2** [ZAM/Fanbyte](https://legacy.fanbyte.com/story.html?story=34348)
- **Dungeons** appear in **group finder** by level (e.g. Stormtalon’s Lair ~20). **S2** [Stormtalon guide](http://wildstar.mmorpg-life.com/guides/stormtalons-lair-dungeon-guide/)
- Drop 4: **bonus Renown/Glory/prestige/cash** from **random queue** for veteran content. **S1** [Patch 02/03/2015](https://wildstar.fandom.com/wiki/Patch_02/03/2015)
- **15-minute deserter** leaving incomplete **dungeon or battleground**; duration **scales with how much of the run you completed**. **S4** [Pastebin winter beta](https://pastebin.com/Nfy3hr3f)
- **PvP deserter** (BG/Arena/Warplots) increased **5→10 minutes** (Strain 1.0.9 — separate from dungeon deserter). **S4** [Strain patch](http://wildstar.mmorpg-life.com/patch-notes/wildstar-patch-notes-1-0-9-strain/)
- Group loot: **Need vs. Greed**, **Master Looter**, **round robin**; **trash** can roll to group members (patch **1.0.9 Strain**). **S4** strain patch; repo `Source/NexusForever.Game.Static/Group/LootRule.cs`
- **Shiphands** renamed **Expeditions**; scalable **1–5** players; veteran mode at 50 with **medal UI pane** and **Renown/Glory** from medals, dailies, random queue (Protogames). **S1** [Expedition wiki](https://wildstar.fandom.com/wiki/Expedition); **S2** [PCGamesN Protogames](https://www.pcgamesn.com/wildstar/wildstar-s-protogames-initiative-update-is-colossal-new-missions-for-both-low-and-high-level-players); **S5** [Mein-MMO endgame DE](https://mein-mmo.de/wildstar-was-tun-auf-stufe-50/)
- **War of the Wilds** adventure: optional objectives required for silver/gold after difficulty patch (OwnedCore tactics). **S3** [OwnedCore guide](https://www.ownedcore.com/forums/mmo/wildstar/wildstar-guides/480220-guide-veteran-war-of-the-wilds.html)
- **Deserter** debuff should **persist through death**; dying in matchmaking should **not** block deserter application (July 2014 fix). **S4** [July 15 patch](http://wildstar.mmorpg-life.com/patch-notes/wildstar-patch-notes-july-15th/)
- After completing a **Group Finder dungeon**, the **group leader** can **requeue** the group for another dungeon. **S8** winter beta; **S7** `MatchMaker.lua` enables `GroupJoin` when `MatchingGameLib.IsFinished()` (requeue blocked with `MatchingFailure_InvalidRequeueType` for some match types)
- **Votekick**: group must wait **10 minutes** before initiating; **2 minutes** if target has not entered instance or is **offline**. **S8** winter beta
- **Cross-activity deserter**: player with **dungeon** deserter may still queue **PvP**; **BG/Open Arena** deserter may still queue **PvE or Rated Arena**. **S8** winter beta
- Dungeons: **My Realm Only** opt-in; must leave queue to toggle cross-realm. **S8** winter beta
- **Cross-activity deserter** (dungeon penalty still allows PvP queue, etc.) per **S8**; emulator applies PvE/PvP base durations via `MatchingDeserterManager` + `RetailCertainRules` but **does not yet** hook `CanQueue` into `MatchingQueueValidator` (pass 7 **Partial**).
- Group Finder role UI uses the client-facing roles **DPS/Tank/Healer** and class eligibility from `MatchMakingLib.GetEligibleRoles`; selected roles are stored in the queue options table and sent through `MatchMakingLib.Queue` / `QueueAsGroup`. This supports role selection, not a proven forced `1/1/3` server reducer. **S7** [MatchMaker XML](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/MatchMaker/MatchMaker.xml#L57-L85), **S7** [MatchMaker Lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/MatchMaker/MatchMaker.lua#L871-L938)

### Need confirmation
- LFG **fake tank** / long queue anecdotes (Harbinger Zero, Why I Game). **S3** player blogs
- **Inhaltsfinder** (DE) for level-50 expeditions — same as group finder at cap. **S5** Mein-MMO
- Exact group-finder composition policy for partial PUG queues. Do **not** treat the old emulator `1 tank / 1 healer / 3 DPS` reducer as retail proof; full instance groups must not be rejected solely for non-trinity composition without native/server-side or live-capture evidence.

### Not sure

- Queue replacement/vote flows, raid saved lock IDs, cross-realm group data.
- `Client0x062A` / `0634` client signaling.
- Semantics of `ServerGroup*` provisional packets.

---

## F-011 — Guild, recruitment, war party

### Certain

- **Warplots**: **40v40** PvP fortresses; **War Party** roster up to **80**; mercenaries if understaffed. **S2** [GamersNexus](https://gamersnexus.net/news/1405-wildstar-mmorpg-warplot-construction-pvp-combat)
- Warplot build: **seven socketable areas**, traps/turrets, **superweapon** and **guard** plugs on special bar; **boss token** from raids (consumable). **S2** [GameZone](https://gamezone.com/originals/wildstar-how-to-build-a-warplot/), **S2** [Kotaku](https://kotaku.com/how-to-build-a-warplot-in-wildstar-1572326177)
- Match resources from nodes; win by destroying generators or maintenance cost war. **S2** [GameZone](https://gamezone.com/originals/wildstar-how-to-build-a-warplot/)
- Warplot queue: **10 War Party members online** before queueing; then **10 members in queue** before match (Strain 1.0.9). **S4** [Strain patch notes](http://wildstar.mmorpg-life.com/patch-notes/wildstar-patch-notes-1-0-9-strain/)
- Warplot in-match surrender: Group Finder **Vote Disband** reads **Surrender Match** during warplots (Sabotage UI clarity). **S1** [Patch 07/31/2014](https://wildstar.fandom.com/wiki/Patch_07/31/2014)
- `/votesurrender` or Vote Disband; vote only after **10 minutes** match time; **≥60%** warp party; surrendering party treated as **losers** for rating. **S8** [warplot beta doc](https://pastebin.com/U8Tn4dsC)
- PvP **deserter** (BG/Arena/Warplots) **5→10 minutes** (Strain). **S4** Strain patch

### Need confirmation

- Warplot **rating** and seasonal team rewards (similar to arena). **S2** [GamersNexus](https://gamersnexus.net/news/1405-wildstar-mmorpg-warplot-construction-pvp-combat)
- Guild recruitment listing packets (`0767+` — emulator mapped). **S3** decomp

### Not sure

- Guild bank transaction rules, holomark/standard perks.
- War-party boss-token inventory persistence.

---

## F-012 — ICComm, chat, friendship, social options

### Certain

- **Circles**: up to **5** cross-guild groups; channels `/c1`..`/c5`. **S1** [Circle](https://wildstar.fandom.com/wiki/Circle)
- Standard channels: **guild**, **party**, **instance**, **zone**, **Nexus** (renamed from Advice 2016). **S1** [Patch 07/14/2016](https://wildstar.fandom.com/wiki/Patch_07/14/2016), **S3** [Slash commands](https://wildstaronline-archive.fandom.com/wiki/WildStar_Slash_commands)
- Custom player channels via `/chjoin` and numeric channels. **S3** [Slash commands](https://wildstaronline-archive.fandom.com/wiki/WildStar_Slash_commands)
- ChatLog RP filter `{*}` marker for RP vs OOC (not ICComm). **S3** [Killroy addon](https://github.com/baslack/Killroy)
- Retail client exposes **Need/Greed loot roll UI** (AutoLoot hooks built-in frames; see **S6** screenshot in deep-pass table).
- **Circles** (Zirkel): up to **5** circles × **100** members each; joinable from level **1** (DE starter guide). **S5** [ingame.de](https://www.ingame.de/news/wildstar-guide-zirkeln-pvp-schlachtfeldern-crafting-runen-mehr-12795314.html)
- **Community chat** `/com` plus `/cominvite`, `/comkick`, `/commaster`, `/comdisband`, etc. (Homecoming, API 16). **S1** [Patch 09/06/2017](https://wildstar.fandom.com/wiki/Patch_09/06/2017)
- **Cosmic Tier 1** “Full Social Access” unlocks guild/**community**/circle/warplot leadership without Signature (alternative to Signature for community create). **S1** [Cosmic Reward](https://wildstar.fandom.com/wiki/Cosmic_Reward); **S1** [Communities](https://wildstar.fandom.com/wiki/Communities)

### Need confirmation

- Emulator **ICComm** opcodes (`0x0546+`) map **Global / Group / Guild** channel types — likely parallel to scoped instance comm, not documented under “ICComm” on wikis. **S3** `INITIAL_FINDINGS.md`, `ICCommChannelType.cs`
- No public guide or **Carbine addon repo** uses the string **“ICComm”**; client uses `ClientICComm*` opcodes and `CommDisplay` addon for chat UI — treat **ICComm** as decomp/native channel API label (pass 4 reconfirmed).
- Beta slash list documents **`wp*`** war-party commands alongside `p*` party and `g*` guild. **S3** [OwnedCore slash list](https://www.ownedcore.com/forums/mmo/wildstar/wildstar-general/418502-slash-command-list.html)
- Friendship block/ignore at account level (emulator partial). **S3** repo

### Not sure

- ICComm entitlement checks, message ordering guarantees, persistent custom channels.
- Chat auxiliary packets `Server0x01B8`, `01C1`, `01C4`, `01EF`.
- `Client0x0550` owner (ICComm ack vs ability book).

---

## F-013 — Mail

### Certain

- Mail sends **messages, items, and money**; **mailbox** required to send/retrieve attachments. **S1** [Mail](https://wildstar.fandom.com/wiki/Mail)
- Up to **10 items** per message. **S1** [Mail](https://wildstar.fandom.com/wiki/Mail)
- **Text only**: **50 copper**, instant delivery. **S1** [Mail](https://wildstar.fandom.com/wiki/Mail)
- Attachment delivery tiers: **instant** (type-based, min 10c), **hourly** (3c + 2c per extra item), **daily** (1c + 50c per extra). **S1** [Mail](https://wildstar.fandom.com/wiki/Mail)
- Housing harvest can **mail materials** to owner. **S1** [Housing](https://wildstar.fandom.com/wiki/Housing)
- **Auction wins** and **outbid refunds** delivered by mail (winter beta; AH shutdown). **S4** winter beta; **S5** Mein-MMO
- **Cash on Delivery (COD)**: compose mail with **CashCODBtn** + attachment; recipient **AcceptCODBtn** calls `PayCoD()` at mailbox; **Reject** returns to sender; inbox shows **CODOverlay** when `monCod` nonzero. **S7** [Carbine Mail.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Mail/Mail.lua)
- Cannot attach **COD and gift cash** on same message (`Mail_CanNotHaveCoDAndGift`). **S7** Mail.lua error map
- Separate **Send Money** vs **COD** radios (not a single radio group — player can uncheck both). **S7** Mail.lua
- Inbox rows show **expiration** as days (red text under ~16 days), hours, or minutes; expired shows `CRB_Expired`. **S7** Mail.lua (`fExpirationTime`, `bNoExpiry`)
- **Guest accounts**: `MissingEntitlement` maps to `GenericError_Mail_GuestAccount` in compose/send flows. **S7** Mail.lua
- **VIP** (`CodeEnumPremiumSystem.VIP`): cash attachments blocked in compose unless **Hybrid** premium or trading reward property unlocked (`BlockerVIP` UI). **S7** Mail.lua

### Need confirmation

- Full **guest-pass** restriction list (mail, chat, CREDD, etc.) — KB article cited on forums but not archived in-repo. **S3** [support guest pass](https://support.wildstar-online.com/entries/63202917)
- Marketplace/AH returns used mail when exchange offline (Sept 2015). **S2** [Massively OP](https://massivelyop.com/2015/09/21/wildstar-shuts-down-the-auction-house-before-the-free-to-play-switch/)
- Realm transfer blocked by invisible mail (fixed Drop 4). **S1** [Patch 02/03/2015](https://wildstar.fandom.com/wiki/Patch_02/03/2015)
- **Mail Helper** addon cited for **bulk take** from mailbox (50–150 AH settlements); does not document COD. **S3** Gaming By The Numbers

### Not sure

- Server-side **expiry day count** defaults per mail type (client displays `fExpirationTime`; emulator uses `ExpiryTime` days from create — verify table-driven values).
- Delete-state parity across reloads; attachment/cash atomicity edge cases.

---

## F-014 — Loot, bindcheck, rolling, master loot

### Certain

- Raid bosses drop loot; **raid chooses** need/greed/master loot/etc. **S2** [GamingBolt interview](https://gamingbolt.com/wildstar-interview-raids-more-raids-and-even-more-raids-in-your-mmo)
- **Adventures** (5-man): medals by speed/performance → better bags. **S2** [Adventures guide](http://wildstar.mmorpg-life.com/guides/wildstar-adventures-guide/)
- Endgame guilds used **EPGP / master loot** addons. **S3** [RaidOps](https://github.com/Drutol/RaidOps) (extends Carbine **Master Loot** addon)
- Retail loot rules (repo enum IDs): **FreeForAll=0**, **RoundRobin=1**, **NeedBeforeGreed=2**, **Master=3**. **S3** `LootRule.cs`
- Patch **1.0.9 Strain**: **Need vs. Greed** and **Master Looter** rolls; **round robin** bugfix; **trash** eligible for group rolls. **S4** strain patch
- Client **Need/Greed roll window** visible in addon screenshot (retail UI, not addon-only mock). **S6** [autoloot-v1_2.png](https://media.forgecdn.net/attachments/198/64/autoloot-v1_2.png)

### Need confirmation

- Carbine **loot overhaul** improved quest reward specificity (class/spec), trimmed vendor clutter — not group roll rules. **S2** [PCGamesN](https://www.pcgamesn.com/wildstar/carbine-detail-big-changes-to-wildstars-loot-system-lots-of-items-will-be-removed-and-replaced)
- Bind-on-pickup confirmation UI (`LootBindcheck`) — emulator maps `ServerLootBindOnPickup`; public guides silent. **S3** decomp

### Not sure

- Exact roll eligibility, master-loot assign packets, `ServerLootCanLoot` emit timing.
- Whether `Client0x011B`/`011D` are BOP acks (matrix says not proven).

---

## F-015 — PvP and duels

### Certain

- **Duel**: level **3+**, select player, **`/duel`**. **S1** [Duel](https://wildstar.fandom.com/wiki/Duel)
- **PvE servers**: voluntary PvP flag; enemy capital auto-flags. **S1** [PvP archive](https://wildstaronline-archive.fandom.com/wiki/PvP)
- **PvP servers**: flag by zone affiliation; shared zones flagged. **S1** [PvP archive](https://wildstaronline-archive.fandom.com/wiki/PvP)
- **Arenas**: 2v2/3v3/5v5; rated seasons at 50; team **rating** and seasonal rewards (mostly 3v3). **S1** [Arena](https://wildstar.fandom.com/wiki/Arena)
- **Battlegrounds** exist (e.g. Daggerstone Pass — uplinks, bombs). **S1** [Patch 07/31/2014](https://wildstar.fandom.com/wiki/Patch_07/31/2014)

### Need confirmation

- **PvP deserter** duration **5→10 minutes** (strain patch; distinct from dungeon deserter). **S4** strain patch
- **PvP Power vs PvP Defense** scaling; instanced damage **85%** baseline at equal ratings. **S3** [Taugrim](https://taugrim.com/2014/07/26/wildstar-great-combat-system-horrible-endgame-pvp-sytem/) quoting Carbine
- Open-world objective PvP **not** launch focus. **S2** [CAD / Carbine quote](https://cad-comic.com/wildstar-guild-server-pve-or-pvp/)

### Not sure

- Duel observer broadcasts, leash/warning packets, cooldown persistence.
- Full PvP stat itemization parity.

---

## F-016 — Spell runtime: procs

### Certain

- Gear and abilities applied **proc-like effects** in combat (player-visible). **S3** general class guides

### Need confirmation

- Emulator maps `Proc.DataBits00..09` to trigger events, chance, cooldown, target routing (decomp). **S3** `Proc System Unblock Path.md`

### Not sure

- Trigger event enum completeness, `targetData` tails, recursion order, school edge cases.
- Spell auxiliary packets `0x080F`–`0x0812` gameplay rows.

---

## F-017 — Spell runtime: damage / heal / shields / vitals

### Certain

- Classes used role-specific primary stats (e.g. tank/heal/DPS stat priorities). **S2** [Ten Ton Hammer attributes](https://www.tentonhammer.com/guides/wildstar-attribute-guide)

### Need confirmation

- Telegraphed combat, shields, absorbs were core (player experience; formulas not published). **S3** class guides

### Not sure

- All `Spell Effect Evidence Matrix` families without captures.

---

## F-018 — Spell runtime: stack groups, buffs, CC, movement

### Certain

- CC (stun, knockdown, tether, etc.) existed with telegraph gameplay. **S3** dungeon guides

### Need confirmation

- Stack groups and DR mentioned in emulator docs only. **S3** repo

### Not sure

- Stack arbitration, aura persistence across zoning, force-move physics packets.

---

## F-019 — Spell runtime: summons, traps, vehicles, AI services

### Certain

- Classes had **pets**, **traps**, and **vehicle-like** spell effects (e.g. Engineer bots, hoverboards in housing). **S2** [Engineer guide](https://www.tentonhammer.com/guides/engineer-class-guide)

### Need confirmation

- Summon ownership and trap triggers content-specific. **S3** scripts

### Not sure

- AI controller hookup, formation, turret deployable state packets.

---

## F-020 — Spell runtime: RavelSignal and scripted receivers

### Not sure

- Entire receiver graph, signal modes, script callbacks — **no public retail documentation** found. **S3** repo only.

---

## F-021 — Action set, LAS, ability, AMP, attributes

### Certain

- **LAS**, **AMP**, action bars, spec tiers were core progression. **S2** [PvE gearing guide](https://marcleoseguin.com/2015/10/27/wildstar-pve-gearing-guide/) (AMP/ability unlock sources)

### Need confirmation

- `UpdateSpellInProgress` async transaction blocks hotbar changes (decomp). **S3** INITIAL_FINDINGS

### Not sure

- Authoritative attribute refund/allocation server rules.
- `Server0x00B0` and related LAS packet meanings.

---

## F-022 — Quests, path missions, public events

### Certain

- **Paths** (Explorer, Scientist, Settler, Soldier) with unique missions. **S1** [Character boost guide](https://wildstar.fandom.com/wiki/Character_boost_guide)
- **Public events** and zone stories documented on wiki zone maps. **S1** [Zone guides archive](https://wildstaronline-archive.fandom.com/wiki/Zone_guides)
- Drop 4: more quests **shareable in party**. **S1** [Patch 02/03/2015](https://wildstar.fandom.com/wiki/Patch_02/03/2015)

### Need confirmation

- Path mission **type 1** completion still blocked in emulator. **S3** matrix

### Not sure

- Public event vote/scoreboard packets (`0x0139`, `0x06F7`).

---

## F-023 — Content: NPE and Rider's Reef

### Certain

- Tutorial/NPE zones exist in client data; emulator has script fixes (repo).

### Need confirmation

- Player guides cover early zones by level path. **S2** [Ten Ton Hammer leveling](https://www.tentonhammer.com/guides/wildstar-exile-leveling-zones)

### Not sure

- Full manual smoke parity for hoverboard, CSI, final terminal (runtime validation).

---

## F-024 — Content: Evil from the Ether / world 3404

### Need confirmation

- Expedition/housing FAB kits referenced on wiki (Mayday, Evil from the Ether, etc.). **S1** [Housing](https://wildstar.fandom.com/wiki/Housing)

### Not sure

- Live encounter scripts, doors, PE 781 — **content smoke only**.

---

## F-025 — Entity create/update, phasing, CSI

### Certain

- Players saw entities spawn/update with telegraphs and interactions in normal play.

### Need confirmation

- CSI (cutscene/interaction) used heavily in tutorial zones. **S3** NPE smoke docs

### Not sure

- `ServerEntityCreate` substructures, deferred action queues, entity stat packets.

---

## F-026 — Items, supply satchel, costumes, pets, titles, unlocks

### Certain

- **Costumes**, **pets**, **titles**, **holo-wardrobe** were retail systems. **S1** [Currency](https://wildstar.fandom.com/wiki/Currency), **S1** [Free-to-Play](https://wildstar.fandom.com/wiki/Free-to-Play) slot limits
- F2P vs box purchaser slot differences (character, costume, decor limits). **S1** [Free-to-Play](https://wildstar.fandom.com/wiki/Free-to-Play)

### Need confirmation

- Generic unlock account/character lists (emulator packet tests). **S3** repo

### Not sure

- Item swap error packets, supply satchel precision, unlock delta semantics.

---

## F-027 — Options, keybindings, combat-log preferences

### Certain

- Keybindings and options UI existed; combat log toggles are player-facing. **S1** UI nav in [Circle](https://wildstar.fandom.com/wiki/Circle) / Codex

### Need confirmation

- Shared-challenge preference tied to options (emulator maps). **S3** repo

### Not sure

- Account vs character option split; `Server0x056B+` readback packets.

---

## F-028 — Support, reports, surveys, stuck

### Certain

- **Transmat recall** to bound point or home. **S3** [Orcz](https://orcz.com/Wildstar:_Tutorial_-_Binding_and_Recalling)
- Support ticket UI referenced in wiki UI list. **S1** [Circle wiki UI nav](https://wildstar.fandom.com/wiki/Circle)
- Stuck on **housing plot**: **Return to Teleporter** (Steam Housing 101). **S3** Steam community guide
- **`/stuck`** opens the **player stuck interface** when trapped in geometry or out of world (not a silent teleport). **S1** [WildStar Slash commands](https://wildstaronline-archive.fandom.com/wiki/WildStar_Slash_commands); **S2** [Wildstar Life controls](http://wildstar.mmorpg-life.com/guides/wildstar-list-controls-keyboard-commands/); **S2** [Tips & Tricks](http://wildstar.mmorpg-life.com/guides/wildstar-tips-tricks/)
- Player-used **`/stuck`** in instances (e.g. Lone Guardian bugs), Aug 2015 blog. **S3** [Bio Break](https://biobreak.wordpress.com/2015/08/26/wildstar-view-from-the-end-of-the-world/)
- Stuck interface buttons call `GameLib.SupportStuck()` for **RecallBind**, **RecallHouse**, and **RecallDeath** (each shows cooldown from `SupportStuckAction` table). **S7** [Stuck.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Stuck/Stuck.lua)

### Need confirmation

- Emulator `ClientStuck` branches: free suicide, recall-transmat gate (conservative). **S3** repo tests
- Whether **RecallDeath** is always offered or gated by zone/death state (Lua checks `RecallDeath` cooldown == 0 before enable).

### Not sure

- Stuck result packets, recall-house destination tables, moderation DB workflow.

---

## F-029 — Client DB, data mapping, EF placeholders

### Certain

- Game data driven by client **.tbl** and world DB imports (repo architecture). **S1** AGENTS.md / DataMapping docs

### Need confirmation

- Each staging promotion requires verified SQL (project policy). **S1** `Tools/DataMapping/README.md`

### Not sure

- Per-table retail semantics without table inspection.

---

## F-030 — Realm, character select, transfer, PTR copy

### Certain

- **Paid realm transfer** announced **$19.99**; full realms blocked; possible rename. **S2** [Engadget](https://www.engadget.com/2014-06-12-wildstar-announces-paid-realm-transfers-path-gear-vendors.html)
- Later **free PvE ↔ PvP megaserver** transfers indefinitely (monitored). **S2** [PC Gamer](https://www.pcgamer.com/wildstar-enables-free-pvp-pve-megaserver-transfers/)

### Need confirmation

- Character boost / PTR copy queues (wiki boost guide; PTR packets in-repo). **S1** [Character boost](https://wildstar.fandom.com/wiki/Character_boost_guide)

### Not sure

- Online transfer handoff, realm message packets, admin delete results.

---

## F-031 — Fortune minigame (Madame Fay)

### Certain

- **Fortune Coins** bought in store; redeem at **Madame Fay's Fortunes** for chest with **three unique items**. **S1** [Fortune Coin](https://wildstar.fandom.com/wiki/Fortune_Coin)
- Themed rotating inventories; mounts/pets/décor/currencies. **S2** [Steam news](https://store.steampowered.com/news/posts/?appids=376570&appgroupname=WildStar&enddate=1501092525)

### Need confirmation

- Gilding catalyst trade-ins for gold mount variants (some seasons). **S2** [Steam news](https://store.steampowered.com/news/posts/?appids=376570&appgroupname=WildStar&enddate=1505496051)
- Essences tracked fill meter (beta store notes). **S2** [Beta Patch 1](http://wildstar.bigdamnheroes.org/?p=1685)

### Not sure

- Exact payout RNG, eligibility costs, cross-session resume, storefront sync.

---

## F-032 — Leaderboards

### Certain

- **Rated arena** teams earned **rating**, seasonal placement, rewards. **S1** [Arena](https://wildstar.fandom.com/wiki/Arena)
- **Personal Arena Rating** tracked per player (e.g. **1500** for 2v2 Challenger I achievement). **S1** [2v2 Arena: Challenger I](https://wildstar.fandom.com/wiki/2v2_Arena:_Challenger_I)
- In-game **2v2 Arena Leaderboards** listing **top 250** rated players per region (Oct 2014 analysis with exported tabs). **S3** [Taugrim](https://taugrim.com/2014/10/12/two-thirds-of-the-top-rated-players-in-wildstar-2v2-arena-are-inactive/)
- Patch **1.7.1 (May 2017)**: **PvE Leaderboards** for **Prime Expeditions** (solo/group) and **Prime Dungeons** — completion times, medals, group members; filter by **Prime tier**. **S1** [Steam 1.7.1 notes](https://store.steampowered.com/news/posts/?appids=376570&enddate=1494867315); **S2** [Massively OP](https://massivelyop.com/2017/05/02/wildstar-patch-1-7-1-arrives-tomorrow-complete-with-expert-skullcano/)
- Client UI types: `PveDungeon`, `PveExpeditionGroup`, `PveExpeditionSolo`; medal filters bronze/silver/gold; PvP tab includes **Arena3v3** rated and **Battleground** class leaderboards. **S7** [Leaderboards.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Leaderboards/Leaderboards.lua)

### Need confirmation

- Post-shutdown **WildStar Logs** rankings are **combat log parses**, not identical to retail in-game `LeaderboardLib` boards. **S3** [wildstarlogs.com](https://www.wildstarlogs.com/help/ranks)
- **Realm bank** shared across characters on the same realm (introduced 1.7.1; still on build 16042). **S2** Massively OP 1.7.1; Steam news

### Not sure

- Hourly file refresh semantics (`LeaderboardLib.GetNextUpdate`) vs emulator snapshot timing.

---

## F-033 — Challenges and shared challenges

### Certain

- **Challenges**: optional timed objectives; types General, Item, Ability (T-spell), Combat. **S1** [Challenge](https://wildstar.fandom.com/wiki/Challenge)
- Started from **Challenge Log** (Codex **L**); **30 min cooldown** after success (active time only). **S1** [Challenge](https://wildstar.fandom.com/wiki/Challenge)
- Fail: timer, leave area, or cancel → restart from log. **S1** [Challenge](https://wildstar.fandom.com/wiki/Challenge)
- Zone **reward tracks**; speed/objectives → points; Signature earns more. **S1** [Challenge](https://wildstar.fandom.com/wiki/Challenge)
- Up to **two** concurrent challenges with on-screen meters (2015 article). **S2** [Massively OP](https://massivelyop.com/2015/07/18/nexus-telegraph-rethinking-wildstars-challenges/)
- **Housing plot challenges** completable by **neighbors** for the owner. **S2** [Gamepressure housing social](https://www.gamepressure.com/wildstar/housing-social/zb6488)
- **Shared challenges**: `ChallengeShared` event opens **ShareChallengeNotice** popup; **AcceptSharedChallenge** / **RejectSharedChallenge**; optional **Always reject** sets `SharedChallengePreference.AutoReject`. **S7** [ChallengeLog.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Challenges/ChallengeLog.lua)
- Server emits **`ServerChallengeShared`** with challenge id + sharer unit id (emulator). **S3** repo `ChallengeManager`
- Earning **gold/silver/bronze** on a challenge increases the **chance of winning** your selected reward; rewards rebalanced (winter beta). **S8** winter beta

### Need confirmation
- Launch contention: players competed for same challenge spawns; Carbine-era commentary argued challenges should become **social/shared** (team same mobs and clickables without stealing credit). **S2** [Massively OP](https://massivelyop.com/2015/07/18/nexus-telegraph-rethinking-wildstars-challenges/)
- **Housing challenges** on plugs: completable repeatedly for **random rewards** like zone challenges. **S2** Gamepressure housing; **S3** Bio Break (Lopp party plug challenge)
- Reward tier roll vs pick (player preference for picking reward). **S3** [Bio Break](https://biobreak.wordpress.com/2014/08/14/wildstar-the-challenge-challenge/)
- Share **timeout** duration (emulator uses **30s** pending share timer + `ServerChallengeShareTimeout`). **S3** repo; not in Carbine Lua excerpt

### Not sure

- Whether share requires **target** selection vs broadcast (emulator uses visible target + `SharedChallengeEnabled` option).
- `Client0x00C8` diagnostic owner.

---

## F-034 — Datacubes, journals, Galactic Archive

### Certain

- **Galactic Archive** encyclopedia; entries from quests, journals, datacubes, tales. **S1** [Galactic Archives](https://wildstar.fandom.com/wiki/Galactic_Archives)
- Zone maps list datacube/journal locations. **S1** [Zone guides](https://wildstaronline-archive.fandom.com/wiki/Zone_guides)
- **Wildstar Life** lore database: per-zone maps with datacube/journal/tale markers and screenshot walkthroughs. **S2** [Lore collectibles DB](http://wildstar.mmorpg-life.com/news/lore-collectibles-database/)
- **Discovery** spots are partly **random**; community maps list high-frequency spawn points per zone. **S3** [Disciplinary Action discovery maps](https://disciplinaryaction.wordpress.com/2016/05/22/wildstar-discovery-maps-by-zone/)

### Need confirmation

- Creature interact unlock hooks (emulator). **S3** repo

### Not sure

- Archive link parent/child authorization rules.

---

## F-035 — Achievements and realm-firsts

### Certain

- Achievements and **realm-first** broadcasts existed (player-visible). **S1** wiki achievement references; **S3** emulator managers

### Need confirmation

- Steam achievement client messages (`ClientSteamAchievements`) — payload grammar unknown publicly. **S3** repo

### Not sure

- Full trigger coverage and realm-first broadcast timing.

---

## F-036 — Zone maps and zone completion

### Certain

- Zone exploration, **discovery** points, collectibles on maps. **S1** [Zone guides](https://wildstaronline-archive.fandom.com/wiki/Zone_guides)
- Discovery caches / lockboxes could grant gear, pets, mounts, décor. **S3** [Discovery maps blog](https://disciplinaryaction.wordpress.com/2016/05/22/wildstar-discovery-maps-by-zone/)

### Need confirmation

- `ZoneCompletion` titles and map-complete achievements (emulator resolver). **S3** repo
- Hex group updates on map (`ServerZoneMap` — wire mapped in-repo). **S3** repo

### Not sure

- Faction/path-specific completion rows and quest/challenge/datacube total integration.

---

## Cross-cutting retail systems (not single F-row)

### Warplots vs housing expeditions

| Topic | Category | Notes |
| --- | --- | --- |
| Housing expeditions (1–5 players, scalable) | **Certain** | FAB kits on housing plugs. **S1** [Housing](https://wildstar.fandom.com/wiki/Housing) |
| Warplots (40v40 PvP) | **Certain** | See F-011. **S2** GamersNexus, GameZone |

### Subscription → F2P → shutdown timeline

| Topic | Category | Notes |
| --- | --- | --- |
| Box + sub launch model | **Certain** | **S2** [Ten Ton Hammer subscription](https://www.tentonhammer.com/articles/wildstar-subscription-model-revealed) |
| F2P **29 Sep 2015** (Reloaded) | **Certain** | **S1** [Free-to-Play](https://wildstar.fandom.com/wiki/Free-to-Play); PR Newswire launch |
| CREDD → **Signature** after F2P | **Certain** | **S1** CREDD archive |
| **Client 16042 / 1.7.8** final downloadable | **Certain** | **S9** Archive.org; emulator README |
| Signing Off **26 Sep 2018** (sunset buffs) | **Certain** (sunset-only) | **S9** Steam announcement |
| Shutdown **28 Nov 2018** | **Certain** | **S9** Steam; Wikipedia |
| **Communities** + Prime + realm bank on 16042 | **Certain** | Homecoming Sep 2017 + 1.7.1 May 2017 |

### Group loot and marketplace (deep pass)

| Topic | Category | Notes |
| --- | --- | --- |
| Need/Greed/Master/RoundRobin + trash rolls | **Certain** | Patch 1.0.9 Strain; `LootRule.cs`; AutoLoot **S6** |
| Auction settlement via mail | **Certain** | Winter beta + AH shutdown articles |
| Dungeon deserter 15m (scaled); cross-activity queue | **Certain** | **S8** winter beta |
| GF requeue / votekick / My Realm Only | **Certain** | **S8** + **S7** MatchMaker.lua |
| Challenge medal → reward win chance | **Certain** | **S8** winter beta |
| Neighbor harvest % split UI | **Certain** | **S8** + **S7** HousingRemodel.lua |
| Warplot surrender (10m, 60%) | **Certain** | Sabotage patch + **S8** warplot doc |
| Mail expiry countdown + VIP cash gate | **Certain** | **S7** Mail.lua |
| Stuck: recall bind/house/death | **Certain** | **S7** Stuck.lua |
| ICComm retail name | **Not sure** | Decomp label only; wikis use circles/channels |
| Guest-pass full restriction list | **Need confirmation** | **S7** partial (`Mail_GuestAccount`); KB URL blocked |
| `/stuck` stuck UI | **Certain** | Slash archive + Wildstar Life |
| In-game 2v2 leaderboards | **Certain** | Taugrim + Arena wiki |
| In-game **PvE leaderboards** (Prime) | **Certain** | Steam 1.7.1 + **S7** Leaderboards.lua |
| Mail **COD** | **Certain** | **S7** Mail.lua |
| Shared challenge accept UI | **Certain** | **S7** ChallengeLog.lua |
| Expeditions (ex-shiphands) medals | **Certain** | Wiki + Protogames patch article |
| Shared **realm bank** (1.7.1) | **Certain** | Steam 1.7.1 (account-wide per realm) |
| **F2P AH/CX slot limits** (3 vs 30) | **Certain** | F2P wiki; terminal baseline |
| **Communities** on final client | **Certain** | Communities wiki; Homecoming patch |
| **Client 16042** emulator target | **Certain** | Archive.org; README |
| **Sunset buffs** (Sep 2018) | **Certain** (exclude by default) | Steam Signing Off |
| **S10** AH 3/30 + formula 1159 | **Aligned** | `MarketplaceAccountLimitsTests`; `ClientGuildRegisterHandler` |
| Deserter / votekick enforcement | **Partial** / **Gap** | Pass 7 crosswalk table |

---

## Recommended confirmation workflow

0. For emulator scope, start with **Retail terminal baseline** — default to **16042 / F2P-era rules**, not **Sep 2018 sunset** buffs.
0b. Check **Emulator ↔ retail crosswalk** for **Aligned** vs **Gap** before changing marketplace/matching/housing behavior.
1. Treat everything in **Certain** as UX copy and player-facing help text only until opcode/table proof exists.
2. For **Need confirmation**, pick one claim and bind it to a client reader label or game-table row.
3. For **Not sure**, require sniff or live client capture before changing server state.
4. When online sources disagree with decomp, **decomp wins** for implementation; update this doc.
5. Prefer **S4/S8 patch notes**, **S6 addon screenshots**, and **S7 Carbine UI Lua** to upgrade **Need confirmation** → **Certain** when they align with repo enums or reader names.
6. Flag **source conflicts** explicitly (e.g. housing per-account press vs per-character wiki) rather than averaging claims.

## Primary bibliography

| Topic | URL |
| --- | --- |
| Housing | https://wildstar.fandom.com/wiki/Housing |
| CREDD | https://wildstaronline-archive.fandom.com/wiki/C.R.E.D.D. |
| Auctions & CREDD guide | https://www.gamepressure.com/wildstar/auctions-and-credd/zb6489 |
| CREDD launch economy | https://www.pcgamer.com/wildstars-credd-exchange-launches-allowing-players-to-buy-extra-time-with-in-game-cash/ |
| AH shutdown F2P | https://massivelyop.com/2015/09/21/wildstar-shuts-down-the-auction-house-before-the-free-to-play-switch/ |
| Challenges wiki | https://wildstar.fandom.com/wiki/Challenge |
| Challenges commentary | https://massivelyop.com/2015/07/18/nexus-telegraph-rethinking-wildstars-challenges/ |
| Mail wiki | https://wildstar.fandom.com/wiki/Mail |
| Duel / PvP / Arena | https://wildstar.fandom.com/wiki/Duel , https://wildstaronline-archive.fandom.com/wiki/PvP , https://wildstar.fandom.com/wiki/Arena |
| Raids interview | https://gamingbolt.com/wildstar-interview-raids-more-raids-and-even-more-raids-in-your-mmo |
| Attunement | https://www.tentonhammer.com/guides/wildstar-attunement-guide |
| Crafting | https://www.tentonhammer.com/guides/wildstar-tradeskills-and-crafting-guide |
| Warplots | https://gamersnexus.net/news/1405-wildstar-mmorpg-warplot-construction-pvp-combat |
| Daily login | http://wildstar.bigdamnheroes.org/?p=1657 |
| Realm transfer | https://www.engadget.com/2014-06-12-wildstar-announces-paid-realm-transfers-path-gear-vendors.html |
| Megaserver transfer | https://www.pcgamer.com/wildstar-enables-free-pvp-pve-megaserver-transfers/ |
| Drop 4 patch | https://wildstar.fandom.com/wiki/Patch_02/03/2015 |
| Winter beta GF/deserter (raw) | https://pastebin.com/raw/Nfy3hr3f |
| Warplot surrender beta doc | https://pastebin.com/U8Tn4dsC |
| Sabotage patch (warplot surrender UI) | https://wildstar.fandom.com/wiki/Patch_07/31/2014 |
| Carbine Stuck.lua | https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Stuck/Stuck.lua |
| Carbine HousingRemodel.lua | https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Housing/HousingRemodel.lua |
| Carbine MatchMaker.lua | https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/MatchMaker/MatchMaker.lua |
| Guest pass KB (blocked fetch) | https://support.wildstar-online.com/entries/63202917 |
| Fortune / Steam | https://store.steampowered.com/news/posts/?appids=376570&appgroupname=WildStar&enddate=1501092525 |
| Housing social (neighbor/harvest/challenges) | https://www.gamepressure.com/wildstar/housing-social/zb6488 |
| Requnix housing review | https://requnix.com/wildstar-housing-review/ |
| MMORPG.com housing press (2013) | https://www.mmorpg.com/editorials/wildstar-housing-2000012988 |
| Mein-MMO AH/CREDD F2P (DE) | https://mein-mmo.de/wildstar-auktionshaus-credd-f2p/ |
| AutoLoot addon + screenshot | https://www.curseforge.com/wildstar/addons/autoloot |
| Wildstar Life patch notes | http://wildstar.mmorpg-life.com/patch-notes/ |
| Harbinger Zero LFG queue blog | https://harbingerzero.net/2014/08/12/wildstar-group-finder-issues/ |
| Notes from Nexus daily login (180d) | http://wildstar.bigdamnheroes.org/?p=1657 |
| Taugrim 2v2 leaderboards analysis | https://taugrim.com/2014/10/12/two-thirds-of-the-top-rated-players-in-wildstar-2v2-arena-are-inactive/ |
| WildStar slash commands archive | https://wildstaronline-archive.fandom.com/wiki/WildStar_Slash_commands |
| vgolds AH / mail settlement | https://www.vgolds.com/news/detail/30977-news.html |
| Game Developer CREDD pricing | https://www.gamedeveloper.com/business/wildstar-s-credd-system-explained |
| Protogames expeditions/medals | https://www.pcgamesn.com/wildstar/wildstar-s-protogames-initiative-update-is-colossal-new-missions-for-both-low-and-high-level-players |
| Warlegend FR housing | https://www.warlegend.net/wildstar-presentation-du-housing/ |
| Disciplinary Action discovery maps | https://disciplinaryaction.wordpress.com/2016/05/22/wildstar-discovery-maps-by-zone/ |
| OwnedCore War of the Wilds | https://www.ownedcore.com/forums/mmo/wildstar/wildstar-guides/480220-guide-veteran-war-of-the-wilds.html |
| ingame.de circles/crafting (DE) | https://www.ingame.de/news/wildstar-guide-zirkeln-pvp-schlachtfeldern-crafting-runen-mehr-12795314.html |
| Carbine Mail.lua (COD) | https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Mail/Mail.lua |
| Carbine Leaderboards.lua | https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Leaderboards/Leaderboards.lua |
| Carbine ChallengeLog.lua | https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/Challenges/ChallengeLog.lua |
| Strain 1.0.9 full patch | http://wildstar.mmorpg-life.com/patch-notes/wildstar-patch-notes-1-0-9-strain/ |
| Steam 1.7.1 (PvE LB + realm bank) | https://store.steampowered.com/news/posts/?appids=376570&enddate=1494867315 |
| July 15 2014 patch (deserter) | http://wildstar.mmorpg-life.com/patch-notes/wildstar-patch-notes-july-15th/ |
| F2P / Signature limits | https://wildstar.fandom.com/wiki/Free-to-Play |
| Communities (Homecoming) | https://wildstar.fandom.com/wiki/Communities |
| Patch 09/06/2017 Homecoming | https://wildstar.fandom.com/wiki/Patch_09/06/2017 |
| Patch 12/06/2017 Primetime | https://wildstar.fandom.com/wiki/Patch_12/06/2017 |
| Cosmic Rewards | https://wildstar.fandom.com/wiki/Cosmic_Reward |
| Client 16042 archive | https://archive.org/details/wildstar_client |
| Steam Signing Off (sunset) | https://steamcommunity.com/app/376570/allnews/ |
| F2P launch PR | https://www.prnewswire.com/news-releases/carbine-studios-wildstar-free-to-play-launches-today-300150262.html |
| Carbine MarketplaceListings.lua | https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/MarketplaceListings/MarketplaceListings.lua |
| Carbine RealmBankViewer.lua | https://github.com/Zod-/Wildstar-Carbine-Addons/blob/master/Live/RealmBankViewer/RealmBankViewer.lua |
| Emulator RetailCertainRules | Source/NexusForever.Game/Retail/RetailCertainRules.cs |
| Emulator MarketplaceAccountLimits | Source/NexusForever.Game/Marketplace/MarketplaceAccountLimits.cs |

---

## Changelog

- **2026-05-22 (pass 7)** — S10 emulator crosswalk; S7 MarketplaceListings + RealmBankViewer; aligned AH 3/30 + tests + formula 1159; flagged deserter cross-queue / votekick / warplot queue gaps.
- **2026-05-22 (pass 6)** — **Retail terminal baseline** for build **16042 (1.7.8)**; F2P AH/CX slot limits; Communities final rules; sunset-only Sep 2018 table; shutdown timeline; S9 tier.
- **2026-05-22 (pass 5)** — Full winter beta pastebin (S8); Carbine `Stuck.lua`, `HousingRemodel.lua`, `MatchMaker.lua`; promoted requeue/votekick/cross-activity deserter, challenge medal win-chance, harvest % split, stuck recall branches, mail expiry UI, warplot surrender; guest-pass KB cited (fetch blocked).
- **2026-05-22 (pass 4)** — Carbine official UI Lua (Mail COD, Leaderboards PvE/PvP, Challenge share); Steam 1.7.1 PvE leaderboards + realm bank; Strain warplot queue 10/10; July 2014 deserter-through-death; promoted F-013 COD, F-032 PvE boards, F-033 shared challenge UI, F-011 warplot queue.
- **2026-05-22 (pass 3)** — Slash-command `/stuck`, Taugrim arena leaderboards, Notes from Nexus 180-day login detail, FR/DE housing, Protogames expeditions, vgolds mail settlement, Bio Break stuck usage, lore/discovery databases; promoted F-028 `/stuck` and F-032 PvP leaderboards.
- **2026-05-22 (deep pass)** — Forums, DE guides, winter beta pastebin, strain patch loot/marketplace, addon UI screenshot; added S4–S6 tiers, corroboration table, promoted loot/deserter/housing/marketplace mail claims; documented source conflicts.
- **2026-05-22** — Initial pass from online research; categorized Certain / Need confirmation / Not sure per `MISSING_FEATURE_MATRIX` row.
