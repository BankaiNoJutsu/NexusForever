# Legacy neighborhood API/UI corroboration - 5 November 2026

Status: historical request/getter spellings recovered; no build16042 legacy
neighborhood request trigger established. The newer random-Community UI flow
is concrete and distinct. Event-name persistence is insufficient to connect
that flow to `0501/0506`.

## Source scope and version boundary

The technical web evidence below is original Carbine/NCsoft-authored Lua and
extracted Houston help preserved by third-party mirrors. Lua copyright headers
identify NCsoft; the mirrors themselves are not official publisher accounts.
Commit dates and version labels identify the sampled records, not a proof that
their contents match the pinned build16042 executable byte for byte.

| Label used here | Immutable revision | Mirror commit date and scope |
| --- | --- | --- |
| H2015 | [`wildstarnasa/NASADocs` `5a154a6f1ec384ef852f216ed07f5ae0ed174126`](https://github.com/wildstarnasa/NASADocs/commit/5a154a6f1ec384ef852f216ed07f5ae0ed174126) | 2015-06-19 21:43:41 UTC; commit adds extracted Houston help; addon build not pinned by that message |
| U12652 | [`NexusInstruments/CarbineAddons` `cf063b3e22b08f49ff51c12cec258a9f1cf86d68`](https://github.com/NexusInstruments/CarbineAddons/commit/cf063b3e22b08f49ff51c12cec258a9f1cf86d68) | 2016-02-26 12:17:17 UTC; labelled version `1.3.0.12652` |
| U15736 | [`Zod-/Wildstar-Carbine-Addons` `0827880c8a820b5cbb3f1dff3e3888c9ab9f0263`](https://github.com/Zod-/Wildstar-Carbine-Addons/commit/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263) | 2017-09-12 21:38:29 UTC; labelled `v1.7.3.15736 API 16`; only `Live/` source used for conclusions |

Reads and web searches occurred on 5 November 2026. Line numbers below are the
original file lines, rather than the web reader's normalized lines. The source
hash inventory at the end permits independent exact-file comparisons.

## Concrete old API names recovered

The H2015 Houston `HousingLib` help declares these exact Lua API names:

| Operation suggested by the declaration name | Names | Original file lines |
| --- | --- | --- |
| Request a neighborhood list | `RequestNeighborhoods` | 204 |
| Read neighborhood state | `GetMyNeighborhoodList`, `GetNeighborhood`, `GetNeighborhoodCount`, `GetNeighborhoodList` | 115,117-119 |
| Membership-related operations | `JoinNeighborhood`, `LeaveNeighborhood` | 180-181 |
| Guild neighborhood creation | `CreateGuildNeighborhood` | 91 |

These are bare declarations without parameter, return-row, event, opcode or
ordering contracts. The Lua spelling is `JoinNeighborhood` / `LeaveNeighborhood`;
the executable's diagnostic text `RequestJoinNeighborhood()` is not evidence
for an identically named exposed Lua API. A documented `RequestNeighborhoods`
name is a new search seed, not a demonstrated build16042 sender.
[H2015 HousingLib.htm:91,115-120,180-181,204](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Help%20File%20Extracted/LuaAPI/Classes/HousingLib.htm#L115-L120)

## Persistent event hook has no recovered implementation in RecallFrame

H2015 `RecallFrame.lua:70` registers `HousingNeighborhoodRecieved` to
`OnNeighborhoodsUpdated`. The complete 391-line file contains that callback
name only once and supplies no function definition for it. It contains none
of the eight old API names above, nor either neighborhood-selection event.
`OnChangeWorld` resets `bHaveNeighborhoods` at line347, but the sampled source
does not use that flag to request a list.
[H2015 RecallFrame.lua](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/RecallFrame/RecallFrame.lua#L69-L70)

The U12652 and U15736 `RecallFrame.lua` files are byte-identical: 13,905 bytes,
SHA256 `54f2f8a77575390d7531bc33283d2dce6f2f4e2a23f9501b0d1076b8c7489733`.
Both retain the event registration at line69 and the flag reset at line346;
both still contain only one callback-name reference and no definition or old
API calls. This is a concrete older/newer source comparison. It supports
treating the registration as an unresolved historical hook, not as an intact
request/response chain. It does not exclude another addon modifying the table
or a differently packaged UI snapshot.
[U12652 RecallFrame.lua](https://github.com/NexusInstruments/CarbineAddons/blob/cf063b3e22b08f49ff51c12cec258a9f1cf86d68/RecallFrame/RecallFrame.lua#L69),
[U15736 Live/RecallFrame.lua](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/RecallFrame/RecallFrame.lua#L69)

An additional mirror-maintained inventory says it lists events found in Carbine
Lua/XML. Its `Pre6600s` list includes `HousingNeighborhoodSelectOpen`,
`HousingNeighborhoodSelectClose`, `HousingNeighborhoodRecieved` and both guild
neighborhood broker events. The version-labelled `1.0.7.6683` sorted and
`1.0.8.6692` lists retain `Recieved`, but omit the selection and broker events.
These are **source-observer inventories**, not publisher API-removal notices
or complete native event registries. Their omissions do not prove an event was
removed from the client, especially because the names remain in build16042.
[Inventory description](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Events/README.md),
[Pre6600s:398-408](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Events/CRBAddonEvents-Pre6600s.txt#L398-L408),
[6683:412-422](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Events/CRBAddonEvents-1.0.7.6683-sorted.txt#L412-L422),
[6692:1174-1176](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Events/CRBAddonEvents-1.0.8.6692.txt#L1174-L1176)

## Newer authored Communities UI has its own explicit chain

U15736 `HousingRemodel.lua` registers `HousingRandomCommunityListReceived` at
line554. `FillRandomCommunityList` reads `GetRandomCommunityList`; when asked
to request an empty list, it calls `RequestRandomCommunityList`. Its result
handler rebuilds the Community tab. Displayed rows use `nId`,
`strResidenceName` and `strCommunityLeader`. Refresh requests the same list;
visiting calls either `RequestVisitCommunityByName` or
`RequestRandomCommunityVisit`. None of the eight old neighborhood API names
or selection events occurs in this complete 2,999-line file.
This establishes a specific newer UI chain, not a `0506` trigger or row-field
mapping.
[U15736 HousingRemodel.lua:554,723-727,752-813,870-875](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/Housing/HousingRemodel.lua#L752-L813)

The matching Community roster UI registers guild roster/result/member/invite
events. Its leave action calls `guildCurr:Leave()` at line648. Community
registration identifies `GuildLib.GuildType_Community`, and its confirmation
uses `GameLib.CodeEnumConfirmButtonType.PurchaseCommunity`; it does not invoke
the old `HousingLib.JoinNeighborhood` declaration. Neither of these complete
sampled files contains the old eight names or selection events. This is
separate membership and registration evidence; it does not establish that the
legacy neighborhood protocol was replaced on a particular date.
[U15736 CommunitiesMain.lua:53-60,643-650](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/Communities/CommunitiesMain.lua#L643-L650),
[U15736 CommunityRegistration.lua:65-73,193-221](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/0827880c8a820b5cbb3f1dff3e3888c9ab9f0263/Live/GuildRegistration/CommunityRegistration.lua#L193-L221)

## Pinned native literal and callback seeds

A separate read-only PE byte scan used
`C:\GIT\_archive\cluster-3d\NexusForever\Decomp\Client64\WildStar64.exe`,
SHA256 `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Its parent REA identity anchor is
`ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`.
The scan maps PE sections and reads literal/pointer bytes; it is not a new REA
or decompiler result and does not open the shared native project.

All eight historical API names above have **zero** occurrences as exact-name
NUL-terminated ASCII or UTF16LE sequences in this executable. This is a bounded
literal negative: computed strings, aliases or other native-only operations
are not excluded. It is stronger evidence against using old help declarations
as current bindings than the persistence of a diagnostic/event string is for
using them.

| Retained string seed | Example VA | Scope |
| --- | --- | --- |
| `HousingNeighborhoodSelectOpen` | `140afce20` | Multiple pool copies; no callback association asserted |
| `HousingNeighborhoodSelectClose` | `140afce40` | Multiple pool copies; no callback association asserted |
| `HousingNeighborhoodRecieved` | `140afcf40` | Multiple copies; known `0506` event is separately mapped |
| `RequestJoinNeighborhood() failed: ...` | `140b095e0`, `140b09640`, `140b09788` | Diagnostics, not exposed Lua registration names |
| `RequestLeaveNeighborhood() failed: ...` | `140b094d0`, `140b09530`, `140b09580`, `140b096a0`, `140b096e0` | Diagnostics, not accepted leave requests |
| `HousingRandomCommunityListReceived` | `140afcb70` | Distinct event spelling and identity from legacy `Recieved` |

The newer UI's four exact API names do have registration pairs. Each pair below
contains the name pointer followed by a code pointer in the pinned image:

| API | String VA | Registration-pair VA | Code pointer |
| --- | --- | --- | --- |
| `RequestRandomCommunityList` | `140b47538` | `140b74870` | `140736500` |
| `GetRandomCommunityList` | `140b47730` | `140b74920` | `140733960` |
| `RequestRandomCommunityVisit` | `140b47800` | `140b748a0` | `140736700` |
| `RequestVisitCommunityByName` | `140b477c8` | `140b748c0` | `140736890` |

These are useful positive controls and native investigation seeds. This note
does not decompile them or infer that their lists are the legacy neighborhood
cache. The existing [runtime blocker record](RUNTIME_BLOCKER_RE_RESULTS.md)
already distinguishes the random-Community request/reply from `0506`.

## Consequence for the blocker

The [native consumer follow-through](DEEPER_ROADMAP_CONTRACTS_2026_11_05.md#legacy-housing-entry-do-not-infer-membership-from-the-packet-name)
remains the exact-build authority: `0501` reaches string replacement at
`1404ba470`, while `0506` replaces cached rows at `1404ba4f0` and emits the
legacy received event. A zero-word lookup in the former does not establish a
membership transition. Neither handler supplies the missing client request.

Newly narrowed: search the historical **actual Lua spellings** rather than
promoting `RequestJoinNeighborhood()` diagnostic text into an API; treat the
RecallFrame hook as incomplete evidence; keep the concrete random-Community
chain separate. The combined source/literal observations justify keeping
legacy neighborhood emission disabled while searching for a different native
owner or accepted traffic.

Still unresolved: a build16042 exposed legacy request/getter or accepted C2S
trigger, `0501/0506` server publication conditions and ordering, row identities
and remaining fields, and any exact historical removal/replacement boundary.
No publisher removal notice or complete matching-build UI source was recovered.
General Communities occupancy policy supplies none of these missing edges.

Exact web/GitHub searches included the three legacy event spellings,
`RequestJoinNeighborhood`, `RequestNeighborhoods`, `GetNeighborhoodList` and
`HousingLib.LeaveNeighborhood`. Search results led to the primary content above
and derivative API stubs; derivative stubs were not treated as independent
authority. Some later GitHub code-search requests were rate-limited, so no
exhaustive public-code negative is claimed. No Source, archive or shared-session
mutation, client launch, commit or push occurred. Application tests do not
apply to this research-only note.

## Exact fetched-file hashes

| Label and original path | Bytes | SHA256 |
| --- | ---: | --- |
| H2015 `Help File Extracted/LuaAPI/Classes/HousingLib.htm` | 13619 | `81f726ff7959864dcd2a37b4e4e52e488fd06da841e80d22a49c3fd04710539f` |
| H2015 `Carbine Addons/RecallFrame/RecallFrame.lua` | 14274 | `a1720e5948df7a03a717108a48db9536cc03c006cfe0e1164e81054a6b32a6b0` |
| U12652 `RecallFrame/RecallFrame.lua` | 13905 | `54f2f8a77575390d7531bc33283d2dce6f2f4e2a23f9501b0d1076b8c7489733` |
| U15736 `Live/RecallFrame/RecallFrame.lua` | 13905 | `54f2f8a77575390d7531bc33283d2dce6f2f4e2a23f9501b0d1076b8c7489733` |
| U15736 `Live/Housing/HousingRemodel.lua` | 126567 | `8e154b8073a23994f22e0417f2dad5835031e6aa95dc4127c1c62f1a9d34a161` |
| U15736 `Live/Communities/CommunitiesMain.lua` | 38056 | `36cebb106b8792ed1ab2b0e01d4c35311d6b9564bbed01575b4d37d5cacd763b` |
| U15736 `Live/GuildRegistration/CommunityRegistration.lua` | 13327 | `4e8e41d870b10078f320c33e66076f69f27fcf0ad96d5988c5db8352f13b1b5b` |
| H2015 `Events/README.md` | 108 | `617b84a4b2362a493a4dce2885bf17505e1d66127c658badfe44e57805b9139b` |
| H2015 `Events/CRBAddonEvents-Pre6600s.txt` | 16605 | `a2cbdca3c89a6629c415e7010d95349d51daad8322f30c8cfa6b3828e359a179` |
| H2015 `Events/CRBAddonEvents-1.0.7.6683-sorted.txt` | 20296 | `8599826de27c445bc962c7bc94320cd0b3591a60a4b27bdced6300d31438e916` |
| H2015 `Events/CRBAddonEvents-1.0.8.6692.txt` | 31452 | `41544eb5bede620cd4f942be902b9aea50db4538ceaf5e757969a81938511f32` |
