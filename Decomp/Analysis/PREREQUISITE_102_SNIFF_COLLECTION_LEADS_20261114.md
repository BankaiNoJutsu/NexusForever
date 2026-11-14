# Type 102: upstream sniff-collection lead follow-through (2026-11-14)

Starting branch/head: `feature/spell-effects-remaining` / `4388354d`.
Question: do issues 370/372 lead to an attributable recording, collection
location, or extraction source that can establish untimed Fluff Start/Go/
remove/Finish ordering?

Before edits: **Observed** issues refer to flyover/vendor sniffs but have no
capture download in their bodies/comments. **Mapped** `.awps` reader format;
**Verified native** client-side admission/removal contracts remain in the
preceding notes. **Blocked** original-server policy: no qualifying trace has
been inspected. Reach remains 49 dependent slots / 42 untimed Fluff effect IDs,
not an affected-cast count.

Follow issue timelines and linked code/data history, then the public tools/data
associated with those leads. Preserve primary statements separately from actual
bytes. Candidate edits: this finding, source/lead inventory, ledger and handoff.
No application behavior edit without a separating witness. Verification:
snapshot identity, link/history and inventory checks, then `git diff --check`;
owning build/tests only if behavior changes. Read-only/static investigation;
no native client execution, login, private-channel access or messages to others.
No retail parity claim.

## Result and evidence boundary

**Mapped only / Blocked:** the public leads identify derived data and an
attributed parsed sniff fragment, but no original-server untimed-Fluff spell
sequence or downloadable recording. They do not settle admission or
Start/Go/remove/Finish order. No Source behavior, timer, packet layout, native
label or previously established native contract changes in this slice.

The [source inventory](coverage/prerequisite_102_retail_trace_sources_20261114.csv)
now has **32 scopes**: the preceding 23 plus nine follow-through scopes. Tiers
describe the reviewed material, not the original server's policy. Derived SQL,
community code and an author's retail-sniff attribution cannot promote that
policy to Verified.

## Issue timelines (Observed primary statements)

Both [370](https://github.com/NexusForever/NexusForever/issues/370) and
[372](https://github.com/NexusForever/NexusForever/issues/372) were created on
2021-08-25, remain open in the reviewed API snapshots, and name Rawaho as
author/assignee. Issue 370 asks for Olyssia QuestChecklistIdx extraction from
flyover sniffs and says the other continents are done. Issue 372 asks for vendor
data from a sniff collection; its one comment proposes Jabbithole because the
commenter lacks sniff access.

The returned timelines contain respectively **two and five events** (label,
assignment, and for 372 its comment, milestone and title change). Neither
contains a linked commit/PR event or a recording URL. This identifies an issue
participant, not the collection's custodian, recording build, current location,
availability or Fluff coverage. The following repositories/PRs are topical leads;
do not turn that correlation into a literal backlink from either issue.

## WorldDatabase provenance and the checklist path

**Observed primary provenance statement:** the
[WorldDatabase README](https://github.com/NexusForever/NexusForever.WorldDatabase/blob/7397e7f8a810fe240922f6e7bf5a8b7e551f8b3b/README.md)
attributes its initial data to custom tooling consuming recordings made while
retail was live, followed by manual fixes. **Correlated provenance:** this fits
the issues' proposed extraction workflow; it is not independent verification of
the source recordings. The README supplies no extractor source, raw capture
location, recording build or spell packet order.

**Correlated historical outputs:**
[c2d3449e](https://github.com/NexusForever/NexusForever.WorldDatabase/commit/c2d3449ec25579bba6e7e97d8e4c83661e621019)
(2021-02-28, seven Alizar files) and
[6cd10d08](https://github.com/NexusForever/NexusForever.WorldDatabase/commit/6cd10d08b672c047ade943013c663a876965fff0)
(2021-03-01, five Isigrol files) add QuestChecklistIdx values. They predate issue
370 and fit its statement about other continents; no direct issue linkage was
found in the reviewed records.

**Mapped Source output shape:** all 19 SQL files were inspected at each of those
two commits and local snapshot `1e5ff9279c5dc78df9ea8fb6b3c9e35676bc175b`
(2021-03-02). They insert `entity`, `entity_stats` and `entity_spline` records;
entity IDs use generated `@GUID` offsets. The reviewed schemas do not preserve
cast-wrapper/effect-instance identity, timestamps or an ordered spell stream.
Names containing "sniff" identify creatures, not recording metadata. These
exports are **Rejected as a replacement for the requested lifecycle trace**;
their claimed retail-derived origin is not rejected.

The local checkout remains unchanged at that historical head. The separately
reviewed remote master is `7397e7f8a810fe240922f6e7bf5a8b7e551f8b3b`
(2025-12-19); it must not be confused with the local snapshot.

**Observed linked code:** [PR 283](https://github.com/NexusForever/NexusForever/pull/283)
asks for checklist re-dumps. Its
[2023-09-18 comment](https://github.com/NexusForever/NexusForever/pull/283#issuecomment-1723930075)
links [kirmmin's d9b4c0aa](https://github.com/kirmmin/NexusForever/commit/d9b4c0aa8b4f8ee111d799aa2f4d646d9304f1d1),
a QuestObjective calculation change. The reviewed discussion and linked change
do not publish an extractor or capture. Community calculation code is not a
server-ordering witness.

## Vendor/store outputs and the assignee's public tool

**Mapped Source output shape:** WorldDatabase
[PR 3](https://github.com/NexusForever/NexusForever.WorldDatabase/pull/3), head
`ea13eb80fdaeb6a3820692bc8452c4f532580036`, adds one 491-line housing SQL
file containing entity/stat/vendor/category/item inserts.
[PR 2](https://github.com/NexusForever/NexusForever.WorldDatabase/pull/2), head
`181cdfd22dac342fb7a5b6c247d5d48c9e67e26b`, has 19 path renames plus a
3,967-line store catalog. Its six tables hold categories, offer groups, group
categories, items, item data and prices. Both returned issue-comment and review-
comment lists are empty. No capture input or ordered spell stream appears in
these reviewed SQL additions. Their relation to the particular collection in
372 remains unverified. **Rejected as Fluff lifecycle witnesses.**

**Mapped Source tool input:** the issue assignee's public
[SpellWorks](https://github.com/Rawaho/NexusForever.SpellWorks/tree/032fbda89d885d86ee85c4f8deb6cf759f094343)
is a spell-data viewer. At that revision,
[ArchiveService](https://github.com/Rawaho/NexusForever.SpellWorks/blob/032fbda89d885d86ee85c4f8deb6cf759f094343/NexusForever.SpellWorks/Services/ArchiveService.cs)
opens installed client archives, and
[ResourceService](https://github.com/Rawaho/NexusForever.SpellWorks/blob/032fbda89d885d86ee85c4f8deb6cf759f094343/NexusForever.SpellWorks/Services/ResourceService.cs)
loads text/game tables and spell models. The README's `Capture.PNG` is its
screenshot. The returned tree and empty releases list provide no recording;
the reviewed loading path is not a packet extractor. **Rejected as the requested
trace**, with no claim about unpublished tools or historical external files.

## An attributed public sniff fragment, with limits

**Observed parsed excerpt:** the
[2018-12-09 PR 62 comment](https://github.com/NexusForever/NexusForever/pull/62#issuecomment-445571978)
contains a decoded player-entity fragment and says it came from a sniff shared
by `lucas-jones` on Discord. This is a concrete public excerpt and a stated
sharing route, rather than only a request to use sniffs. It contains entity
appearance/realm/class metadata, not Start/Go, an effect-row ID, removal or
Finish. The comment does not establish recording build/date, original-server
provenance independently, or a raw-file download. No player payload is copied
here. The fourteen returned review comments supply no capture attachment/link.

**Observed public fork inventory:** the cited participant's
[NexusForever fork](https://github.com/lucas-jones/NexusForever) has five listed
branches. All returned recursive trees are complete (`truncated=false`), with
no known capture-name/extension candidate; its releases list is empty.

| Branch | Reviewed commit |
| --- | --- |
| master | `c9338189b560a23f42c1a786856103abbafbb9a0` |
| AddPlayerPaths | `3614e43798696ba9ec91358ac3a39887652af575` |
| BasicChat | `64141a307c02b022d40e36696ca359f71d2981f3` |
| RealmEnums | `97c091dddd14e6c8b04f1dc43f4206ac499bbed1` |
| WhoList | `82a34b62cc7c003619de8f71667ef86b56ab0c7d` |

This bounds a public tree/name search; it does not exhaust Git history,
arbitrarily named bytes, private channels or external storage. A sharing
attribution is not proof of ownership, collection custody or present access.

**Observed later citations:** commit
[aef1cd10](https://github.com/NexusForever/NexusForever/commit/aef1cd1064e8541717b4e0ac1f213c6df4e28762)
cites retail sniffs in its threat-refactor message;
[c60f106e / PR 498](https://github.com/NexusForever/NexusForever/commit/c60f106ed8073f30ec75ff565ab6316430735ba2)
cites parsed sniffs for challenge messages. The reviewed messages and PR 498's
empty issue/review-comment lists add no recording location. These are provenance
claims, not additional inspected packet sequences.

## Decision, next gate and verification

**Blocked original-server policy:** obtain actual recording bytes or a complete
attributable extraction retaining build/context, concrete Spell4/effect row,
wrapper ID, receiving-unit IDs and ordered Start/Go/remove/Finish records for
untimed Fluff. Include a multi-target survivor before generalizing per-receiver
versus whole-wrapper retirement. The reported flyover/vendor collections and
the historical sharing route are provenance leads, not known available inputs.
No contact, login or private-channel access was performed.

**Rejected inference:** static entity/vendor/store re-dumps cannot reconstruct
the discarded spell ordering. A player-entity excerpt cannot choose between
retaining an untimed child until explicit removal and sending Finish at queue
exhaustion. Neither hypothesis is promoted. The existing native/shipped findings
remain at their recorded tier; Source Fluff remains unchanged.

**Observed verification:** API timeline/link identities, SQL shapes, branch/tree
limits and CSV uniqueness/required-field/tier counts were checked; all 32 scope
records have explicit limits. `git diff --check` passes. Ignored reproducibility
summaries: `artifacts/prerequisite-type102-sniff-leads/api-review.json`,
`link-review.json`, `worlddb-shape-review.json`, `worlddb-local-history.txt`.
Recheck command: `python artifacts/prerequisite-type102-sniff-leads/verify_slice.py`.
No C# behavior changes, so no build/test repeat. Prior non-green full-suite
result remains recorded in the handoff, not represented as passing here.
End state: **mapped only with the original-server witness blocked**.
No retail parity claim.
