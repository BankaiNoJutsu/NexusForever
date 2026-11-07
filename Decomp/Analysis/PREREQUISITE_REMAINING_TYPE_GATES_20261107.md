# Gate for every remaining unhandled prerequisite type (2026-11-07)

Evidence tier: classification generated from `Prerequisite.tbl` (slot counts), the registered handler classes and the client evaluator (`decomp_evaluator.txt` plus the evaluator vtable at `140b67740`: a slot whose function is `FUN_140001ba0` is the shared client stub). Source/SQLite only; no retail parity claim.

After types 5, 20, 42, 50, 53, 54, 59, 60, 74, 110, 111, 113, 114, 115, 130, 162, 231 and 280 were handled this stretch: **2902 slots over 63 types** have no handler (2281 slots on client stubs, 3 on types with no evaluator case, 618 on real client bodies).

Gate classes: **stub** = the client evaluator slot is the shared return-0 stub, so the meaning is server-side only and needs a server or retail witness; **real body** = the client body exists; the table says what is read and what is missing; **no case** = the client never evaluates it.

| Type | Enum name | Slots | Client body | Blocking input |
|---|---|---|---|---|
| 47 | QuestObjective47 | 1788 | stub | Client slot is the shared return-0 stub; Value is a QuestObjective id (Equal 847, NotEqual 941). Server-only concept with no client body: needs a server or retail witness for the meaning. |
| 107 | QuestObjective7 | 124 | vtable 14049fff0 | Body read (`FUN_14049fff0`): for a non-player unit, the path mission `ObjectId` (mission lookup) ProgressCount bit `1 << unit+0x35f8` (virtual `+0x40`); player/pet pairs only answer NotEqual true. Blocked on the creature participation slot at `unit+0x35f8` (no server model of which bit a creature owns). |
| 119 | PublicEventObjectiveObject | 122 | vtable 1404a0650 | Body read (`FUN_1404a0650`): the public event objective `ObjectId`: event active and the objective`s virtual `+0x90` result for the unit participation slot `+0x35f8` (same slot as 107). Blocked on that slot. |
| 154 | Holdout | 119 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 153 | HoldoutWave | 95 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 126 | PathHoldout | 72 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 49 | Schedule | 68 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 30 | FacilityWithinDistance | 54 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 149 | State | 46 | vtable 14049c840 | Body read (`FUN_14049c840`): `unit+0x1e4 != 0 || unit+0x2ac != 0` compared with Value (0/1); prior notes: `+0x2ac` is a cast/action update block flag, `+0x1e4` is overloaded; no server owner for either (46 slots). |
| 146 | ItemTradeSkillLevel | 34 | vtable 1404a0f90 | Body read (`FUN_1404a0f90`): ObjectId is a tradeskill id (1 to 16), the unit word at `+0x15c` or `+0x160` (chosen by bit 0 of `unit+0x14c`) is counted against a list of entries with stride `0x30` and threshold at `+0x34`, and the index reached is compared with Value. Not mapped to `TradeskillTier` (at most 6 tiers per tradeskill, while 11 retail slots compare with Value 10): the list is a different, unidentified table, and the 0-based index versus the server`s 1-based tier rank is unresolved. |
| 9 | EpisodeState | 34 | vtable 14049f700 | Episode class state (`FUN_14049f700`): per-quest states at episode node `+0x24` and the server has no episode model; see PREREQUISITE_TYPE_COVERAGE. |
| 116 | Stealth | 32 | inline | Client unit field `+0x54 > 1`; 354 writers of offset `+0x54` across the client, none tied to a stealth state. |
| 252 | Personal3V3ArenaRating252 | 27 | vtable 14049dc50 | Body read: true only for the local player; the value is `FUN_1405bffe0(PvP object DAT_140c65b98, its rating type (3v3), 0xffffffff)` compared with Value by the integer helper. The PvP object is filled from the player-create packet and later PvP packets; the server has no rating producer or storage, and the client default for a character that never played is unknown, so no value can be reported honestly. |
| 251 | Personal2V2ArenaRating251 | 21 | vtable 14049dbf0 | Body read: true only for the local player; the value is `FUN_1405bffe0(PvP object DAT_140c65b98, rating type 0 (2v2), 0xffffffff)` compared with Value by the integer helper. The PvP object is filled from the player-create packet and later PvP packets; the server has no rating producer or storage, and the client default for a character that never played is unknown, so no value can be reported honestly. |
| 253 | Personal5V5ArenaRating253 | 21 | vtable 14049dcb0 | Body read: true only for the local player; the value is `FUN_1405bffe0(PvP object DAT_140c65b98, its rating type (5v5), 0xffffffff)` compared with Value by the integer helper. The PvP object is filled from the player-create packet and later PvP packets; the server has no rating producer or storage, and the client default for a character that never played is unknown, so no value can be reported honestly. |
| 254 | Battlegrounds254 | 21 | vtable 14049dd10 | Body read: true only for the local player; the value is `FUN_1405bffe0(PvP object DAT_140c65b98, rating type 3 (battlegrounds), 0xffffffff)` compared with Value by the integer helper. The PvP object is filled from the player-create packet and later PvP packets; the server has no rating producer or storage, and the client default for a character that never played is unknown, so no value can be reported honestly. |
| 255 | Warplots255 | 21 | vtable 14049dd70 | Body read: true only for the local player; the value is `FUN_1405bffe0(PvP object DAT_140c65b98, rating type 4 (warplots), 0xffffffff)` compared with Value by the integer helper. The PvP object is filled from the player-create packet and later PvP packets; the server has no rating producer or storage, and the client default for a character that never played is unknown, so no value can be reported honestly. |
| 18 | ScheduledEvent | 17 | vtable 14049ef00 | Hash set at global `+0x72a0` holds the ids; its contents (active scheduled events?) are unidentified. |
| 199 | PublicEvent199 | 14 | vtable 1404a0b70 | Body read (`FUN_1404a0b70`): the public event `ObjectId` exists and its field `+0x1d0 == 1` (event state); Equal true/NotEqual false. The mapping of `+0x1d0` to the server`s event state is unverified (14 slots). |
| 193 | Unknown193 | 12 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 239 | ActionBar | 12 | vtable 14049ca10 | Bit 0 of four dwords at global `+0x1448` (index = Value 0 to 3, player only); what the four flags are is unidentified. |
| 127 | PathHoldoutPlayer | 11 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 200 | PublicEventObjective200 | 11 | vtable 1404a0bf0 | Body read (`FUN_1404a0bf0`): event active and the objective virtual `+0xa0` word AND `Value` (a status/phase mask); the mask meaning is unmapped (11 slots). |
| 142 | Unknown142 | 10 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 77 | QuestObjectiveOnTarget | 9 | inline | Inline QuestObjectiveOnTarget: reads quest objective state on the prerequisite target; unread. |
| 189 | Unknown189 | 9 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 147 | PlayersInWorld | 9 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 230 | WarplotRating | 8 | inline | Warplot rating: no server producer or storage. |
| 203 | Unknown203 | 7 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 216 | Shield216 | 6 | inline/helper FUN_1404a2090 | Inline shield compare using `FUN_1404a2090`; the unit shield value source is unread. |
| 226 | Entitlement | 6 | vtable 1404a1620 | Calls `FUN_1403d2140` (account item count owned by Item2 id, Equal count / NotEqual zero); the account item count source on the server (`IAccountInventoryManager`) is not mapped. |
| 276 | OutOfBounds | 5 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 112 | HazardProperty112 | 4 | vtable 1404a0490 | Hazard family (`FUN_1404a0490`); same gate as 111. |
| 259 | Unknown259 | 4 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 192 | Unknown192 | 3 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 131 | Level131 | 3 | inline/helper FUN_1404a2090 | Inline level compare (`FUN_1404a2090`) with a second unit property; unread. |
| 187 | WarplotPlugUpgrade | 3 | vtable 1404a1340 | Warplot plug upgrade (`FUN_1404a1340`): no warplot model on the server. |
| 21 | TradeSkillSchematicLevel | 2 | no evaluator case (falls to default) | No evaluator case: the client never evaluates it (falls to the default). |
| 222 | Unknown222 | 2 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 48 | Unknown48 | 2 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 150 | HubEconomyProgress | 2 | vtable 1404a01e0 | Body read: the settler hub manager (`DAT_140c65960`) record for hub `ObjectId` (163, 164): its economy avenue points (record `+0x60`) over the hub definition`s `MaxAvenueEconomy` (def `+0x4`) times 100, compared with Value as a float (actual first). The server has no avenue-point model: `IPathManager.GetSettlerHub*ProgressPercent` are unused loose proxies of other quantities (mission progress, bundle counts), so nothing honest can be reported without an avenue point source. |
| 151 | HubQualityOfLife | 2 | vtable 1404a0260 | Body read: the settler hub manager (`DAT_140c65960`) record for hub `ObjectId` (163, 164): its quality of life avenue points (record `+0x68`) over the hub definition`s `MaxAvenueQualityOfLife` (def `+0xc`) times 100, compared with Value as a float (actual first). The server has no avenue-point model: `IPathManager.GetSettlerHub*ProgressPercent` are unused loose proxies of other quantities (mission progress, bundle counts), so nothing honest can be reported without an avenue point source. |
| 152 | HubSecurity | 2 | vtable 1404a02e0 | Body read: the settler hub manager (`DAT_140c65960`) record for hub `ObjectId` (163, 164): its security avenue points (record `+0x64`) over the hub definition`s `MaxAvenueSecurity` (def `+0x8`) times 100, compared with Value as a float (actual first). The server has no avenue-point model: `IPathManager.GetSettlerHub*ProgressPercent` are unused loose proxies of other quantities (mission progress, bundle counts), so nothing honest can be reported without an avenue point source. |
| 225 | LevelGrantAmpPoints | 2 | vtable 14049f010 | Compares Value with a constant-table level (`FUN_140200220(0x3ae)`, default 6): the Constant table row is not exposed on the server. |
| 163 | PublicEventParticipantCount | 2 | stub | Client stub: public event participant count is server-side. |
| 237 | BonusPower | 2 | vtable 14049c990 | Compares Value with the player`s BonusPower word (`+0x6e64`); the server publishes it in packets but exposes no getter on the player. |
| 235 | BonusAbilityTierPoints | 2 | vtable 14049c910 | Compares Value with the player-create packet`s `UnknownA0` word (`+0x6de4`, bonus ability tier points); the server does not track that value (it sends 0). |
| 243 | Faction243 | 2 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 248 | NpcInventoryItemCount | 2 | vtable 1404a0cb0 | NPC inventory item count (`FUN_1404a0cb0`): no NPC inventory model. |
| 271 | Unknown271 | 2 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 285 | Unknown285 | 2 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 145 | ItemIsSelfCraftedWeapon | 1 | inline | Inline ItemIsSelfCraftedWeapon: item flag unread. |
| 122 | PublicEventObjective122 | 1 | vtable 1404a08f0 | Body read (`FUN_1404a08f0`): the public event `ObjectId` is active and the unit`s record map at `unit+0x3780` contains the event id (the player participates in it). Needs the server`s per-player participation record for events (1 slot). |
| 234 | WarplotUpgrade | 1 | vtable 1404a1370 | Warplot upgrade (`FUN_1404a1370`): no warplot model. |
| 224 | LevelGrantAbilityTierPoints | 1 | vtable 14049efc0 | Looks Value up in a table row (`FUN_14024dfc0`, field `+0xc` non-zero): table identity unread. |
| 228 | TradeSkill228 | 1 | inline | Inline tradeskill check: unread. |
| 244 | OwnsAccountItem | 1 | vtable 14049ca70 | Account item lookup `FUN_1401ed460` by a global id at `+0x1694`; unmapped. |
| 260 | Unknown260 | 1 | vtable 1404a1220 | Real body `FUN_1404a1220` unread. |
| 277 | QuestObjective47OnCasterAndTarget | 1 | stub | Same stub family as 47 (caster and target variant). |
| 278 | Unknown278 | 1 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 281 | Falling | 1 | inline | Inline Falling check: movement state unread. |
| 294 | Unknown294 | 1 | stub | Client stub: server-only concept, needs a server or retail witness for its meaning. |
| 295 | ? | 1 | no evaluator case (falls to default) | No evaluator case and no enum name. |

Update: 111 and 113 are implemented ([PREREQUISITE_HAZARD_20261107.md](PREREQUISITE_HAZARD_20261107.md)); 112 (4 slots) remains and shares the hazard manager gate.

Update: 114 is implemented ([PREREQUISITE_VITAL_COMPARE_20261107.md](PREREQUISITE_VITAL_COMPARE_20261107.md)).

Update: 115 is implemented ([PREREQUISITE_UNIT_PROPERTY_COMPARE_20261107.md](PREREQUISITE_UNIT_PROPERTY_COMPARE_20261107.md)).

Corrections (same day): types 64 and 55 were mapped wrongly before ([PREREQUISITE_RESOURCE_SLOT_20261107.md](PREREQUISITE_RESOURCE_SLOT_20261107.md), [PREREQUISITE_UNIT_PROPERTY_VALUE_20261107.md](PREREQUISITE_UNIT_PROPERTY_VALUE_20261107.md)); they are now native-body implementations. Method for any further inline type: dump the evaluator jump table (int32 entries at `1404a4848`, index type-1, base `140000000`) and read the target block.
