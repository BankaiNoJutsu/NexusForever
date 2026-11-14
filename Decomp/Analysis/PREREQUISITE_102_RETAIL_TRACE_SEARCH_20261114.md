# Type 102: original-server untimed Fluff trace search (2026-11-14)

Starting branch/head: `feature/spell-effects-remaining` / `9419040d`.
Question: can an available original-server capture establish Start, Go,
receiver removal and Finish ordering for an identified untimed Fluff row?

Before this pass: **Verified native** client admission, CastMethod-10 prune,
removal readers and wrapper teardown are recorded in
[Fluff admission](PREREQUISITE_102_FLUFF_ADMISSION_20261114.md). **Blocked**:
the original server's admission/retention/emission policy has no trace witness.
Reach: 49 type-102 slots / 42 untimed Fluff effect IDs (20 slots at CastMethod
10, 29 at other methods). These are dependency counts, not affected casts.

Search local indexes and newly located WildStar material, discover actual
capture formats from parser source, then inspect public capture repositories,
issues and archive links. A parser or sniffer is not itself a retail trace.
An acceptable witness needs attributable original-server provenance, build,
wrapper/effect/receiver IDs and packet order, with truncation and exclusions
explicit. Candidate edits: this evidence record, a focused source inventory,
ledger/tracker/handoff. No Source behavior change without that witness; no
native launch, Computer Use, login, or messages to third parties. Verification:
source/format checks, inventory consistency and `git diff --check`; an owning
build and focused tests only if behavior changes. No retail parity claim.

## Result and corrected search scope

**End state: mapped only / Blocked. No qualifying original-server trace was
located in the reviewed sources.** This is a bounded search result, not proof
that such captures never existed or cannot be obtained. No Fluff admission,
parent retention, timer, packet layout or emission behavior is changed.

Two gaps in the preceding search scope were corrected:

- **Mapped Source format:** the public WildstarPacketParser consumes `.awps`,
  an extension omitted from the preceding loose-file inventory. Its
  [pinned Program.cs](https://github.com/MaxtorCoder/WildstarPacketParser/blob/47dc67a9857485c5ea788935ed30981e50442719/WildstarPacketParser/Program.cs#L30)
  splits textual records on spaces/semicolons, using field 3 as direction,
  field 5 as a decimal opcode and field 7 as hex payload. Its README explicitly
  names `.awps`. This establishes a search/reader lead, not retail provenance.
  No sample capture is supplied in the reviewed default-branch tree or releases.
- **Observed local corpus:** the historical addon location is absent, but the
  876-archive addon corpus was rediscovered on another local volume, alongside
  a second client installation and archived client builds/tables. The earlier
  path failure did not establish that the corpus was unavailable everywhere.

The machine-readable [source inventory](coverage/prerequisite_102_retail_trace_sources_20261114.csv)
records **23 reviewed source scopes**, their identities, tiers, results and
remaining limits. Source/tool availability is not an additional retail witness.

## Local evidence review (Observed)

| Scope | Reviewed | Result / boundary |
| --- | --- | --- |
| Additional client/project/download roots | 1,365,016 file paths; filename scan completed without errors | No loose `.awps`, `.awp`, `.wsp`, `.wscap`, `.cap`, `.pcap`, `.pcapng`, `.pkt`, `.sniff` or `.wspp` candidate. Arbitrarily renamed contents were not exhaustively scanned. |
| Additional ZIPs | 911 archives / 27,977 member names | All listings succeeded; no known capture extension or sniff/dump name candidate. Includes 876 addon archives and 35 archived client-table ZIPs. |
| Archived client binaries | 122 7z archives / 1,298 member names | All listings succeeded; no known capture extension or sniff/dump name candidate. Binaries were not executed. |
| Broader current-drive project name scan | 41 capture-format paths | All belong to two unrelated projects; payloads excluded from this WildStar investigation. |
| Lean continuation index | 2,154 records | Three packet/capture-name matches resolve to Source test files. The lean index intentionally omits optional datasets. |

`WildStarLogger-2.0.zip` is a four-member addon source package, SHA256
`33d2fa599551d9c61a7a15bf3ee7fc130130931c0b9370c9ca07e1b214859399`.
Its Lua registers CombatLog callbacks; it is not a recorded play session or
a Start/Go/remove/Finish packet trace. **Rejected as the requested witness**,
not rejected as useful addon source for other questions.

A bounded `.txt`/`.log` keyword search of the known client logs and current
checkout artifacts (8 MiB per-file cap) found only this project's native
registration export for `awps`/`ServerSpellStart`/`ServerSpellGo`. That does not
exclude arbitrary renamed, binary or differently labelled data. Modern emulator
logs would not establish original-server ordering even if their opcodes matched.

## Public sources and the strongest remaining lead

**Observed primary statements:** upstream
[issue 370](https://github.com/NexusForever/NexusForever/issues/370)
(2021-08-25) refers to flyover sniffs, and
[issue 372](https://github.com/NexusForever/NexusForever/issues/372)
(2021-08-25) refers to a vendor sniff collection. A
[2021-11-28 comment](https://github.com/NexusForever/NexusForever/issues/372#issuecomment-981132547)
states that the contributor lacked sniff access. Reviewed bodies/comments/events
contain no capture attachment or download. These statements are leads to a
collection, not verification of its present accessibility, build, contents or
untimed-Fluff coverage. Do not describe that uninspected collection as absent
or as known to contain the needed sequence.

The public [NexusForever.PacketParser snapshot](https://github.com/NexusForever/NexusForever.PacketParser/tree/df7b14146b0b188fdec31f71dc2ae90a076a1d99)
contains only a README on the listed master branch. The MaxtorCoder parser and
the reviewed gokudo-graves fork contain code without a trace file. Other
inspected data leads resolve to an addon extractor (FSDataMiner), a client asset
archive explorer (Halon), and client RE notes, rather than network recordings.
**Rejected as trace witnesses:** parser implementations and their agreement
with Source cannot determine original-server scheduling.

The official Tools tree and Wiki tree/README/contribution guide/summary/FAQ
were also checked. The reviewed documentation exposes no capture artifact or
download link. That observation does not cover unreviewed history or external
collections; unrelated setup claims in those documents are not adopted here.

The current Nexus Reborn homepage was accessible by direct HTTP after the
browser-text fetch returned 403. Its reviewed links lead to a client download
and Discord, not a packet archive; the cached archive wording yielded no
downloadable capture. No private-channel access, login or third-party message
was performed. Public code/issue/repository and web searches produced no
applicable trace; search indexing/default-branch limits remain explicit.

## Internet Archive review (Observed catalogs; no trace promotion)

A `title:wildstar` catalog query returned 109 records, reviewed across two
pages. A separate title plus sniff/packet/capture query returned zero records.
Those search results do not establish global absence. Applicable collection
leads were inspected directly:

| Public collection | ZIP members | Additional content review |
| --- | ---: | --- |
| [Repository collection, 2010–2015](https://archive.org/details/Ephemeral_Software_Collection_Repositories_wildstar_20100911-20151107) | 6,135 | README identifies addon-oriented repositories collected through CurseForge; retained packed Git histories were not exhaustively decoded. |
| [WildStar 2014–2017](https://archive.org/details/Ephemeral_Software_Collection_Wildstar_2014-2017) | 20,054 | Resource/thread indexes and READMEs identify addon download mirrors. |
| [WildStar 2017–2018](https://archive.org/details/Ephemeral_Software_Collection_Wildstar_2017-2018) | 1,914 | Resource/thread indexes and READMEs identify addon download mirrors. |

HTTP range reads inspected the central directories of the approximately
94 MB, 3.76 GB and 884 MB ZIPs without fetching the complete archives. Known
capture-name/extension searches found no candidate. Because some downloaded
members have no extension, **37 selected README/resource/thread index members**
were also read. Keyword hits resolve to an addon author's handle/credits,
not packet records. Nested extensionless downloads were not all unpacked;
these collections are not certified capture-free. Catalog SHA1 values are
recorded as reported metadata; full ZIP digests were not recomputed.

Two small wiki history archives were downloaded and checked against their
published SHA1 values, then the named XML member was read:

- [Wildstar Info, 2015-07-05](https://archive.org/details/wiki-wikiwildstarinfocom):
  455 pages; no packet/sniff/awps/ServerSpell/SpellGo/SpellFinish keyword hit.
- [WildStar NASA Reference, 2018-05-05](https://archive.org/details/wiki-wikiwildstarnasacom):
  1,918 pages; 13 matching revisions on two discussion/user pages refer to a
  person called Packet, not a capture or protocol sequence.

These are **Rejected as the requested trace witnesses**. Their retail-era
dates do not convert addon/API/reference text into an original-server recording.

## Admission gate for a future candidate

A useful next artifact is the actual recording from the referenced sniff
collection, or another attributable original-server capture. Preserve the raw
record order and recording context; a parser's output, filename or filesystem
date alone does not establish origin. The build must be identified, or its
compatibility and effect-row/flags mapping must be independently established.

For the relevant spell cluster, retain Start (`0x07ff`), Go (`0x07f4`), the
applicable removal (`0x0811`, `0x0813` or `0x0819`) and Finish (`0x07fe`), with
wrapper IDs, receiving-unit IDs and the Fluff effect-row ID. Identify missing
segments, filtering, dynamic overrides and ID reuse before interpreting absence
of a packet. An identified untimed Fluff row outside the 42 directly referenced
IDs can still be a bounded witness if its row/build mapping is established.
A multi-target survivor is needed before generalizing receiver removal to a
whole-wrapper policy. One trace is not automatically proof for all Fluff rows.

**Blocked:** the reviewed sources do not supply that artifact. Parent-retention,
replacement-wrapper and conditional admission models remain unresolved. The next
step is access to an actual attributable capture, not another timer registry,
native-client run against the emulator, or inferred Source Finish change.
No retail parity claim.

## Verification and reproduction

Source inventory checks validate unique IDs, required fields, allowed tiers,
the local archive counts/error lists, three remote ZIP catalogs and selected
wiki-history download hashes. `git diff --check` passes. This slice changes
evidence/tracker files only; no C# build/tests or full-suite repeat is warranted.
The preceding non-green Source suite and all existing native evidence retain
their earlier limits. No new native labels or runtime qualification are claimed.

Ignored root: `artifacts/prerequisite-type102-retail-trace/`. Local records:
`additional-root-files.txt`, `additional-capture-leads.txt`,
`i-drive-capture-name-leads.txt`, `text-log-candidate-paths.txt`,
`zip-inventory.json`, `sevenzip-inventory.json`, `wildstarlogger-review.json`.
Public records: `*-metadata.json`, `*-members.json`, `*-zip-summary.json`,
`*-index-review.json`, `archive-indexes/`, `archive-index-keyword-review.json`,
`*-history-review.json`, `internet-archive-*.json`, and the reviewed homepage.
Local helper scripts reproduce ZIP/7z listing, range-read catalog review and
named XML extraction. Raw third-party archives, wiki payloads, file inventories
and source code stay ignored; only this finding and the bounded source inventory
are promoted. No retail parity claim.
