# Runtime blocker web evidence - 5 November 2026

Status: historical retail policy corroborated; production packet timing and
exact Fortune weights remain evidence-gated.

This pass addresses F-004 housing, F-008 crafting, F-025 tracked/entity aux,
F-031 Fortune and optional STS token authentication. It does not change Source,
enable emitters, launch the client, or alter the recorded stop on Computer Use.
REA's shared native session is handled separately by the parent investigation.

The companion [native follow-through](BLOCKER_UNBLOCK_RESEARCH_2026_11_05.md#sts-rsa-component-representation-mapped)
subsequently maps the STS component envelope through fresh standalone Ghidra
analysis and selected LLVM instructions. The web-only negatives below do not
supersede that new mapping or close the remaining token/session gates.

## What this pass resolves

1. Fortune mount rotation was **catalog-specific**, not a universal fixed
   schedule. Publisher announcements distinguish a sequential Hellhound color
   rotation from a later collection in which all Astrosaurus colors were
   available together. This resolves a historical policy question, not the
   build-16042 active catalog or numeric per-item probabilities.
2. Publisher documentation independently corroborates temporary community plot
   occupancy ending at logout and reserved plots persisting while offline.
   It does not establish the `0501/0506` packet producer or their row fields.
3. The final-update announcement's store availability rules do not identify a
   Fortune reward rotation. Do not use that announcement to enable every
   Fortune reward or assign a probability.

## Primary source observations

### F-031: catalog-specific rotation

On 7 February 2017, the publisher described the Medieval collection's
Hellhound colors as red, then blue, then green, changing approximately every
few weeks; all colors would subsequently be offered together before the next
catalog refresh. The rare gold version could drop independently of those
color rotations. This provides a historical sequence, but no exact transition
timestamps, account-item IDs or numeric chance values.
[Publisher announcement via Steam API, gid 75835160708918916](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1486501811).

On 25 April 2017, the publisher explicitly distinguished the Island Paradise
collection: every Astrosaurus color would be available throughout that run.
Onyx could be obtained as a rare direct reward or through exchanging the other
three unclaimed mount items. This is independent first-party evidence that a
single inferred color-rotation algorithm would be wrong across collections.
[Publisher announcement via Steam API, gid 1858141774322426024](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1493140524).

The Homecoming announcement identifies the Arcterra collection as returning on
6 September 2017 for a limited period. A later Fortune announcement says those
old offerings would retire on 4 October when the seasonal collection replaced
them. These are bounded historical catalog transitions; the posts do not
enumerate a complete ID/probability array for a supported final build.
[Homecoming publisher announcement](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1504647195),
[seasonal publisher announcement](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1506646221).

The 26 September 2018 final-update announcement makes seasonal store stock and
rotating costumes/mounts available through the store. It does not specify the
Fortune catalog, its probabilities, or a mapping to `ServerFortuneRewards`.
Treating that store rule as Fortune catalog evidence is an unsupported
inference.
[Final-update publisher announcement](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1537979558).

**Remaining gate:** accepted retail `ServerFortuneRewards` payloads with an
identified date/catalog and exact item/probability arrays, or a trustworthy
server/catalog artifact. Preserve the emulator policy in
[FORTUNE_WEIGHT_AUDIT.md](FORTUNE_WEIGHT_AUDIT.md); this pass supplies no
replacement weights or final-build active rotation.

### F-004: housing community lifecycle

The publisher's 28 August 2017 Communities article describes a maximum of
20 member characters and five displayed personal plots. A displayed personal
home cannot simultaneously be visited as its separate personal plot. An
unreserved display placement lasts for the play session and is released at
logout; a reserved placement persists offline until an authorized revocation.
This corroborates the distinction between occupancy, membership and reservation
without deriving a neighborhood-list send trigger from it.
[Communities publisher announcement, gid 2094592080082834707](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1503943857).

**Remaining gate:** exact `0501/0506` trigger/order and backing fields still
need a native producer, accepted packet history or independently verified
client request/consumer chain. The article names no opcode. It does not
equate the legacy neighborhood cache with the implemented guild-backed
community operations. Existing community publication/recovery work is not
reopened by this evidence.

### F-008: crafting design corroboration

The crafting systems designer's own professional record describes two
frameworks: gear-oriented circuit crafting and coordinate crafting using
controlled randomness with vector/radius controls. This corroborates the
existence of distinct crafting models; it supplies no distribution, coordinate
formula, service-key meaning, discovery-unlock rule or enqueue timing. The
record covers his 2011-2014 employment, so it cannot establish final-build
behavior.
[Phillip Chan's 2019 professional record, page 2](https://islink.org/Phillip_Chan_Resume-Game_Designer_2019.pdf).

**Remaining gate:** native/table/capture corroboration for the actual final-build
discovery and complex-craft transitions, `084B/0855` aux emission, current-craft
cadence and `056C` item patch timing. Pre-release guides and mirrored beta
patch notes found by search were not promoted to final-build specifications.

### F-025: tracked units and remaining entity aux

The current [native handler/proxy mapping](F025_STATIC_HANDLER_PROXY_MAPPING.md)
already supersedes the old reader-only status. Its 5 November entries map
additional consumers and the `0939` name/title wire model. This research does
not regress that status. The existing scanner-name producer is distinct from
the other aux producer gates.

Exact-name web searches for `MapTrackedUnitUpdate`,
`HousingNeighborhoodRecieved`, `TrackingSlot`, and the crafting update event
surfaced API inventories or unrelated results, not an accepted build-16042
server send path or retail packet trace. API/event names corroborate client
surface existence only. No emitter eligibility, unit ID allocation, update
cadence or disable timing is established here.

### F-001: optional STS token authentication

Exact route/helper searches did not find a WildStar-native primary source
resolving the recorded CKeyRsa encoding/padding, server key/signature creation,
premaster processing or optional-route ordering.

One public Aion Classic reverse-engineering report discusses similarly named
STS routes. It is a different game's binary/protocol, and describes an
additional `EncryptionKey`/AES transport scheme. This was excluded as WildStar
implementation evidence. Shared route names do not establish matching key
formats, reply grammar or state transitions.
[Cross-game report inspected and excluded](https://forum.ragezone.com/threads/aion-classic-server-protocol-reverse-engineering-%E2%80%94-crypto-opcodes-packet-structures.1267025/).

**Remaining gate:** the supported WildStar client/library's native crypto
construction plus accepted optional-route evidence. Keep the optional handlers
absent until those contracts are established.

## Retrieval identity and bounded search

The Steam API request used app `376570`, feed
`steam_community_announcements`, `count=1000`, `maxlength=0`. It returned
**112 news items** on 5 November 2026. That is the observed API result, not a
claim that every historical publisher post is available there. The exact
one-item API links above were tested to select their stated GIDs.

The following SHA256 values bind the UTF-8 encoding of each returned
`contents` string, not an HTTP byte stream or a screenshot:

| GID | UTC publication time returned by API | UTF-8 bytes | Body SHA256 |
| --- | --- | ---: | --- |
| 75835160708918916 | 2017-02-07T21:10:10Z | 4075 | `16CEEA7D61A4CD5DC74E1661D544CFB18C67E62C839F2C641E9CBAB66E8299C4` |
| 1858141774322426024 | 2017-04-25T17:15:23Z | 3864 | `93ACC5FBAA844C356FA0F74C3313F4CCE84F268A6175C59480E6E2A95CEDA32A` |
| 2094592080082834707 | 2017-08-28T18:10:56Z | 6324 | `795401879C5449996DB4D5E66CC108D8FDE9D24DD8123CF4CB817ADFCC538B63` |
| 2095718613785654421 | 2017-09-05T21:33:14Z | 5143 | `858E38652FA0779C3BFC79C6FC4F9295D21BC4CC938B14B9212C7C10564FE69D` |
| 2175660043466330019 | 2017-09-29T00:50:20Z | 2964 | `1BC1C111B222B04D49622B5B97C31AD935DB44697DDAA0630387FA4364059D64` |
| 2450412598619259202 | 2018-09-26T16:32:37Z | 5869 | `28437CA3DFC748E34C589028211C82CB1D68FE4601B467B535AFD395B9936AA5` |

Steam's presentation page displays the seasonal article as 28 September in
some regional views; its API timestamp is 29 September UTC. The retirement
date asserted in the article is 4 October. These are separate dates.

Representative search boundaries:

- Publisher-domain and Steam searches for Madame Fay, odds, rotation,
  Homecoming, neighborhoods and coordinate crafting.
- Exact route/helper searches for `LoginTokenStart`, `TokenKeyData`,
  `StsConnLib`, `CKeyRsa`, `AuthnToken` and `PremasterSecret`.
- Exact UI/packet searches for `HousingNeighborhoodRecieved`,
  `MapTrackedUnitUpdate`, `CraftingUpdateCurrent` and `ServerFortuneRewards`.
- GitHub repository/API discovery for original UI code and packet tools;
  unofficial API inventories were not treated as primary server evidence.
- Read-only `rg --files --hidden --no-ignore -g '*.lua'` under the supplied
  archive `C:/GIT/_archive/cluster-3d/NexusForever` returned no Lua files.
  This says nothing about files packed inside a client archive elsewhere.

Direct individual Steam announcement URLs were unavailable to the web reader;
the Steam news API returned their bodies and metadata successfully. The
historical publisher links embedded in those bodies remain provenance clues,
not claims of current website availability.

## Follow-through

The useful new inputs are dated publisher policy, the Steam API recovery route,
and negative classification of several tempting substitutes. A recovered
matching client archive can supply shipped UI/table corroboration; server
timing or hidden retail weights may still require accepted traffic/server data.
Do not repeat this unchanged general web search as a substitute for those
missing inputs. No runtime blocker is marked implemented by this document.

Validation: the six exact one-item API selectors and contents digests were
checked; Markdown links and whitespace were checked. No code changed, so no
build or test suite was run for this research-only note.
