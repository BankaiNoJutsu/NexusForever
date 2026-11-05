# ICComm typed channels and asynchronous callbacks: authored-source corroboration

5 November 2026. This research narrows the ICComm signature, callback and scope
questions from the [gameplay/social audit](ALL_BLOCKERS_GAMEPLAY_SOCIAL_RESEARCH_2026_11_05.md#iccomm-and-chat-entitlement-identity-and-restoration-remain-distinct).
It supplies historical authored API text and real addon callers, not a
build16042 server contract, accepted traffic or a production implementation.
No Source, existing tracker, client or shared native session changed.

## Fresh result: the two JoinChannel signatures belong to different API eras

The Houston-manual conversion identifies itself as **API15**. Its ICCommLib
entry declares `JoinChannel(strChannel, eChannelType, guildContext)`, returning
an ICComm object; Guild channels require Guild userdata as context. This is
substantive corroboration beyond the older bare enum listing. The conversion
preserves original API documentation in a third-party repository; neither its
2018 repository head nor its README authenticates build16042 equality.
[Manual provenance, README:3-7](https://github.com/Hammster/wildstar-api-docs/blob/dd9a803184b6910eb352c9bee59bc28632a7f4b9/README.md#L3-L7),
[typed signature and return, ICCommLib.md:43-58](https://github.com/Hammster/wildstar-api-docs/blob/dd9a803184b6910eb352c9bee59bc28632a7f4b9/Classes/ICCommLib.md#L43-L58).

Independent addon authors use both forms at their recorded dates:

| Original caller | Observed call and boundary |
| --- | --- |
| GeminiEditor, 3 April2014 | Passes a channel name, callback name and addon context directly to JoinChannel. This corroborates the older signature, not the typed final-client form. [GeminiEditor.lua:120-122](https://github.com/wildstarnasa/GeminiEditor/blob/772a497134baa642db5b9e7f8cdd8d4cb8ad8cce/GeminiEditor.lua#L120-L122). |
| RaidOps BID, last path change 20 April2016 | Passes a name, `CodeEnumICCommChannelType.Guild` and `self.uGuild`, then installs a separate receive callback. Its pinned main manifest declares API14. [BID.lua:1318-1330](https://github.com/Drutol/RaidOps/blob/2af52a5adbbd1a681226866f264b6864cec45fa1/RaidOps/BID.lua#L1318-L1330), [toc.xml:2](https://github.com/Drutol/RaidOps/blob/2af52a5adbbd1a681226866f264b6864cec45fa1/RaidOps/toc.xml#L2). |
| RaidCore, last path change 24 January2017 | Joins a named Group channel and uses the returned object for readiness and callback registration. Its pinned main manifest also declares API14. [RaidCore_CombatInterface.lua:442-481](https://github.com/Immemor/RaidCore/blob/48173d4b7954dd98fbfa986e4ceecb5e82480f68/RaidCore_CombatInterface.lua#L442-L481), [toc.xml:2](https://github.com/Immemor/RaidCore/blob/48173d4b7954dd98fbfa986e4ceecb5e82480f68/toc.xml#L2). |

A preserved publisher announcement dated 5 May2015 explicitly describes an
ICComm API rewrite and gives the typed signature with Guild context. The
archive identifies the original author as Sunshine and attributes its text to
the official forums, but was itself populated in2026 and disclaims publisher
affiliation. It is preserved original-authored text, not an independently
authenticated publisher download. Taken with the manual and actual callers,
it explains why the old callback-argument signature must not be combined with
the typed channel API. It does not date every bundled help file.
[Archive provenance/date:1-8](https://github.com/heyiamsven/Wildstar-Patch-Notes-Archive/blob/346eed7edbe38fdfc72907fe98bd5cba7d8f12cd/2015-05-05%20-%20INVASION%20Nexus%20Patch%20Notes%20-%2005%20May%202015.md#L1-L8),
[API rewrite:1430-1434](https://github.com/heyiamsven/Wildstar-Patch-Notes-Archive/blob/346eed7edbe38fdfc72907fe98bd5cba7d8f12cd/2015-05-05%20-%20INVASION%20Nexus%20Patch%20Notes%20-%2005%20May%202015.md#L1430-L1434).

## A channel object is distinct from successful asynchronous admission

RaidCore checks `IsReady()` after JoinChannel. If not ready, it registers
`SetJoinResultFunction`; its `(channel, result)` handler treats the symbolic
Join result as success and logs other result names. Success installs separate
received-message and send-result callbacks. This is concrete original caller
evidence for a pending object and later result, not merely method names.
[Join and callback paths:442-481](https://github.com/Immemor/RaidCore/blob/48173d4b7954dd98fbfa986e4ceecb5e82480f68/RaidCore_CombatInterface.lua#L442-L481).

Its send-result handler receives a channel, result and message ID. Its received
handler instead treats the third argument as a sender name and resolves a
player from it. Preserve that distinction when tracing the native callback
builders; the old announcement names the received argument differently, so
that announcement alone is insufficient to settle its exact final-build type.
[Actual callback bodies:741-755](https://github.com/Immemor/RaidCore/blob/48173d4b7954dd98fbfa986e4ceecb5e82480f68/RaidCore_CombatInterface.lua#L741-L755),
[historical event declarations:1450-1458](https://github.com/heyiamsven/Wildstar-Patch-Notes-Archive/blob/346eed7edbe38fdfc72907fe98bd5cba7d8f12cd/2015-05-05%20-%20INVASION%20Nexus%20Patch%20Notes%20-%2005%20May%202015.md#L1450-L1458).

The addon retries joins with a timer increasing from5 to30 seconds and stops it
on success. That is **addon policy**, not a server reconnect, backoff or durable
membership contract. A separate preserved 19 May2015 publisher fix concerns
repeated joins before the server response, further motivating inspection of
pending/repeated admission. Neither source proves automatic reconnect or
exactly-once request handling.
[Retry loop:449-466](https://github.com/Immemor/RaidCore/blob/48173d4b7954dd98fbfa986e4ceecb5e82480f68/RaidCore_CombatInterface.lua#L449-L466),
[19 May fix:189-191](https://github.com/heyiamsven/Wildstar-Patch-Notes-Archive/blob/346eed7edbe38fdfc72907fe98bd5cba7d8f12cd/2015-05-19%20-%20Game%20Update%20-%2019%20May%202015.md#L189-L191).

## Scope, leave and capacity evidence: useful historical constraints

The preserved rewrite announcement distinguishes group-only cross-realm
messages, one guild association for a given guild-channel name even across a
circle/guild pair, and automatic removal when leaving that guild. Its join
result callback covers both join and leave outcomes. These are explicit
historical API rules to compare with same-build native owners; they do not
prove persistent channel IDs, login replay, group-change cadence or final-build
server fanout.
[Scope and leave:1436-1441](https://github.com/heyiamsven/Wildstar-Patch-Notes-Archive/blob/346eed7edbe38fdfc72907fe98bd5cba7d8f12cd/2015-05-05%20-%20INVASION%20Nexus%20Patch%20Notes%20-%2005%20May%202015.md#L1436-L1441).

API15 documents upload/download capacity getters **by channel type**, with
integer results expressed in bits per second. It describes one overshooting
message of up to50KB and a longer throttle recovery. The2015 announcement also
states25 messages per channel type per second. These are different budget
dimensions: none establishes a count of10 joined channels, actual final-build
capacity values or the complete throttle algorithm. Do not turn the historical
numbers into new Source constants without independent matching evidence.
[Capacity getters and units:64-96](https://github.com/Hammster/wildstar-api-docs/blob/dd9a803184b6910eb352c9bee59bc28632a7f4b9/Classes/ICCommLib.md#L64-L96),
[historical rate statement:1442-1449](https://github.com/heyiamsven/Wildstar-Patch-Notes-Archive/blob/346eed7edbe38fdfc72907fe98bd5cba7d8f12cd/2015-05-05%20-%20INVASION%20Nexus%20Patch%20Notes%20-%2005%20May%202015.md#L1442-L1449).

The API15 manual exposes MissingEntitlement in both join and message result
enums, but supplies no owning entitlement ID or applicability rule. Its ICComm
method inventory lists separate join, receive, send-result and throttled
setters; the old NASADocs `SetControlFunction` declaration is not evidence that
one generic callback has the same current contract.
[Result enums:19-37](https://github.com/Hammster/wildstar-api-docs/blob/dd9a803184b6910eb352c9bee59bc28632a7f4b9/Classes/ICCommLib.md#L19-L37),
[method inventory:48-72](https://github.com/Hammster/wildstar-api-docs/blob/dd9a803184b6910eb352c9bee59bc28632a7f4b9/Classes/ICComm.md#L48-L72),
[older declaration:40](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Help%20File%20Extracted/LuaAPI/Classes/ICCommLib.htm#L40).

## Retrieval identity and limits

Hashes below are SHA256 of raw response bytes at the immutable revisions linked
above, with no newline normalization. Mirror dates are not client build dates.

| File | Bytes | Raw SHA256 |
| --- | ---: | --- |
| Hammster `Classes/ICCommLib.md` |2673|`9d1bf05ba6ede59aa1a45186f14707b9dc32da2c3697c16850ffb12b16ab1c89`|
| Hammster `README.md` |924|`74608a09a6c94235f1000e97e9d864179b56e80a9c109ca85f4b27ef17369274`|
| Hammster `Classes/ICComm.md` |1292|`970a6a10d86d8c40ab435e32e585ff89b5a05e8fa58e86179f7cf5d363ce7e37`|
| May5 publisher-text mirror |92188|`6e698f3385e3bb176e0cd7b643d8cc1ffc7f4183c14252f857bcd6786febf637`|
| May19 publisher-text mirror |10356|`86889835968a8fe2cf2dd878f39a02da5184b4e46830c90b2230514d6d71e809`|
| RaidCore `RaidCore_CombatInterface.lua` |30983|`03e8eb5b4b6cb4c2ebc72f21762f1ad5690b171d5192771a4b6179a839f8fc72`|
| RaidOps `RaidOps/BID.lua` |133049|`c845cf1aec424ec3be2fecab35932cdfe7ff51b021135162a027a8918bbc86f4`|
| GeminiEditor `GeminiEditor.lua` |29674|`f66494d4c1360650e4df76f898f4dfea79c07a1c39f5e9b993cdca434e97e5c5`|

Exact-name GitHub searches covered the typed/old signatures, four callback
setters, MissingEntitlement, TooManyChannels, guild context and ICComm patch
notes. The sampled Carbine-addon mirror had no indexed ICCommLib caller;
third-party addon authors supply the concrete callers above. Modern emulator
enums, empty IDE stubs, unrelated games and substring matches were excluded as
retail policy evidence. No exact entitlement ID, numeric joined-channel cap or
automatic reconnect/persistence rule was recovered. Search indexing is bounded;
these negatives do not prove absence from every file or archive. A later repeat
of the addon-mirror query returned HTTP504 and is retained as a failed request,
not additional negative evidence.

Raw files, source revisions/hashes and GitHub per-path histories are retained
under `artifacts/continuation-20261105/iccomm-web/`. Local links, cited source
line ranges and raw hashes were checked. No application tests, target execution,
provider setup or runtime/retail qualification is claimed. Same-build native
mapping must own stronger field, quota, entitlement and lifecycle conclusions.
