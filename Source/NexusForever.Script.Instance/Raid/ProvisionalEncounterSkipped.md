# Provisional encounter scripts not written (raids, World Story, event instances)

Bar used: a boss creature id verified by name, Spell4 rows bound to it by tag, text anchor or internal name, AND an ability the packet (INSTANCE_RAIDS_EVENTS_PACKET_20261109.md) reports from a clip. Written: Initialization Core Y-83 only (single-source, one phase, two casts). Everything below was not written; the exact missing input is stated.

Data caveat for the lead: the packet's spell ids come from the older SQL dump (`wildstar_client_mysql`) and do NOT match the binary-table CSV that the tag catalog uses (for example id 77715 is `[RMT] e4558 ... Lift Up` in the SQL dump, while the catalog lists that row as 77805; id 41088 is `Tugga Molten Rain` in the CSV but `Engineer - Flak Cannon` in the SQL dump). Spell ids in the new Y-83 file are catalog (CSV) ids. The packet's `w3009`, `w3045` tags do not exist in the CSV at all (only a few `[Dev]` rows), so those instances have no verifiable spell rows.

## Raid/RedMoonTerror
* Engineers (Wilbargh 65758, Orvulgh 65759; `[RMT] e4550`): creature ids verified by name and rows exist, but no readable clip reports any Engineers ability (packet: "Not covered by any readable clip"). Missing input: a clip or subtitle naming at least one Engineers cast.
* Star-Eater the Voracious (72758; `[RMT] e4556`): id verified, rows exist (named `Bosun Octog - ...` templates), but no readable clip (the 2018-03-20 stream is only a description jump point). Missing input: a readable clip naming at least one cast.
* Swabbie Ski'Li (68655; `[RMT] e4558`): id verified, single clip (lwjQxCnwpIM), but it reports only phase-level mechanics (three add waves, a Shredder phase, bomb stacking, circle telegraphs, kicks) and no cast name. Binding them to the Poke, Junk Trap, Tether Mine or Pull to Fan rows would be a guess, and the Shredder phase is a scripted phase, not a periodic boss cast. Missing input: a cast name for the boss from any clip, plus the phase triggers.
* Robomination (66085) is the only RMT boss with two-clip agreement but was not in this assignment.

## Raid/InitializationCoreY83
Written: Prime Evolutionary Operant 50472 only. Doubts: the casts are mapped to the rows by the name of yells heard in one stream (phonetic caption matching by the packet author), not by a cast bar; no table says which creature casts the `[IC] e2681` rows; 50472 carries the internal name `[OLD] Inspired Redbeak-on-a-Stick`; the Prime Phage Distributor 50423 is flagged Deprecated and was dropped.

## WorldStory/HallOfTheHundred
Harizog Coldblood (67444) and Varegor (67457) names verify. Missing input: the one clip with a boss fight (Keza16SraM4) never names the boss (the packet reads a garbled caption as Harizog but says it is not established), and the `w3009` Part 6 boss rows the packet lists are not present in the binary-table CSV (only `[DEV] Boss Leap` 84046 and a music row). Needs a second clip naming Harizog's abilities and verified spell rows.

## WorldStory/JourneyIntoOMNICore1
Cortex Controller (65186) name verifies. Missing input: the packet reports the Cortex fight only as scripted voice lines (shield and healing, lasers), identical across clips, with no cast names; the `w3045` rows (Assault Beam, Deadly Rays, Healing Protocol) are not present in the binary-table CSV. Needs verified Spell4 rows and a clip naming casts.

## EventInstance/ShadesEve
Jack Shade (62681) name verifies, and `Jack Shade (Group Boss)` rows exist in the CSV (Hat Trick, Terrorize, and so on), but the packet reports only dialogue from the clips, no ability, and says which template belongs to the Instance Boss creature is not stated. Missing input: any clip showing a Jack Shade cast, and the template-to-creature link.

## EventInstance/ProtostarsSuperMallInTheSky
No boss creature: the packet describes randomised non-combat store events plus a Main Event, with no creature ids resolved and the `[WF14]` tag being event-wide. Missing input: a named boss creature with packet-reported abilities.

## Raid/RedMoonTerror/FortyMan, world 3524, BAD (3042)
Out of scope per the packet (missing input: no footage, no rows).

## Initialization Core Y-83 (lead decision, 2026-11-09)
A provisional Y-83 script was written by an agent and removed by the lead before commit: the only creature row (50472, display name "Prime Evolutionary Operant") has the internal name `[OLD] Inspired Redbeak-on-a-Stick`, no table says which creature casts the `[IC] e2681` rows, and the two spell ids were mapped by name from boss yells in a single stream. Missing input: a creature-to-spell binding for e2681 and a second clip showing the casts.
