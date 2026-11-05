# Native authentication, diagnostics and combat blocker audit - 5 November 2026

This pass covers the native/authentication and diagnostic/combat portions of the
[36-area inventory](ALL_BLOCKERS_INVENTORY_2026_11_05.md). It adds a selected STS
notification-guard map and checks the remaining evidence boundaries. No server
behavior or optional authentication route was enabled.

## STS: selected TokenKeyData notification guards narrowed

The previously mapped completion installs a derived filter but does not read
application reply fields. Fresh inspection now follows its upstream generic
dispatcher rather than assuming that an empty reply is acceptable.

The sender constructs `CTokenKeyDataTxnNotify` at `18000aa5e` with a flags DWORD
of exactly `0x10`. Its vtable is `180124a38`; slot `+8` reaches `180031570`,
slot `+0x18` reaches `18000c340` and the mapped filter completion, and slot
`+0x20` reaches `180003d50`. The latter copies the incoming status word into
the retained auth state and notifies its owner; it is a different rail from
filter installation.

At this selected flags value, the dispatcher reaches slot `+0x18` only on the
observed branch with all of the following:

- A non-null message object.
- A zero status word in its separate status argument.
- A nonzero result from message virtual slot `+0x38`.
- A zero result from message virtual slot `+0x30`.

The `CInetMsg` table at `18012b878` maps those latter slots to `180023e90`
(tests bit 1 of message `+0x5C`, returned as zero or minus one) and `180007f40`
(returns message DWORD `+0x60`). These are exact native field predicates;
this pass does not invent their full wire-header meanings or prove every
derived message type uses the base implementation. Other branches take the
status-notification rail, sometimes after constructing status4 and diagnostics
such as `message required`, `bad set of replies`, `invalid one/many reply`,
`has many replies`, or `pass/fail required`. Null-message behavior also depends
on the two status-argument words. A status callback is not evidence that the
crypto-filter completion ran.

The base message's XML accessor at `1800248a0` adds a separate parser boundary.
When it has neither a cached root nor a cached parse error, it obtains the body
through slot `+0x68`, requires a nonzero length and a final LF byte, replaces
that LF with NUL, and calls the XML parser. Zero length or missing LF returns
without caching a parse error. An actual XML-parser failure retains a parse-error
string and no root. This does not alone prove the selected TokenKeyData path
invokes that accessor, nor decide whether a header-only message is accepted.
The current [server packet wrapper](../../Source/NexusForever.StsServer/Network/Packet/ServerStsPacket.cs)
already appends LF to its body; that Source match is a narrow format cross-check,
not optional-route interoperability.

This narrows the generic validation gate to concrete flags, predicates and a
known parser boundary. Still required: wire-to-header/status field construction,
message cardinality and malformed-input handling across the complete parser,
trusted-key provisioning, server private-key/inverse/padding behavior, startup
order and an accepted optional-route transcript. The mapped RSA component
envelope and selected mode4 derivation/filter ownership are carried forward,
not reported as new discoveries.

### Public-source cross-check boundary

Exact WildStar optional-route/diagnostic searches supplied no accepted current
trace. A [2014 first-person stream question](https://osqa-ask.wireshark.org/questions/30104/the-tcp-stream-analyzing/)
contains an unidentified client's similarly named fields. Its displayed public
key prefix decodes to a 4-byte component, integer 65537 and a 128-byte next
component. This resembles the already mapped envelope but has no WildStar
build, original packet file, authenticated origin or accepted-session binding.
It is not a trust key, padding witness or interoperability vector for this task.
Raw-page fetching did not expose the expected field token; the small displayed
prefix was transcribed from the web renderer. Other posted token/ciphertext
fields were neither retained nor used. Aion/Blade-and-Soul emulator descriptions
were excluded from WildStar protocol proof.

## Diagnostic requests and Server0015 remain genuinely semantic gates

The current opcode table and log-only handler collection enumerate the twelve
numeric requests `00C8/00ED/011B/011D/012D/0550/062A/0634/063E/0701/07E3/0928`
plus four structural realm/addon surfaces. Their neutral fields and no-response
boundaries remain intentional. Shared empty/uint32/string writers, ordinal
adjacency, a lookup constant matching an opcode, or another packet's reader
cannot establish their owner. Existing exact analyzed-instruction scans and
negative guards remain usable; this audit does not present an unchanged search
as a newly resolved sender.

`Server0x0015` has a 5-bit-plus-uint32 reader, with no proven owner. The entity
dispatch table used for the recent high-range successes starts at `0061` and
does not supply a `0015` case. Matching `0628` or Fortune row-helper reuse does
not name this packet. Next useful artifacts are an exact lower-level dispatcher
or callback/indirect send witness, or an accepted packet with actual context.
[Current diagnostic guards](../../Source/NexusForever.WorldServer/Network/Message/Handler/Misc/ClientUnresolvedDiagnosticHandlers.cs),
[opcode identities](../../Source/NexusForever.Network/Message/GameMessageOpcode.cs).

## Spell, creature and content evidence boundaries

| Family | Current usable evidence | Remaining artifact needed |
| --- | --- | --- |
| Proc holders/events | Existing interpretation/diagnostics and holder-specific fixtures | A clean holder334 report and actual event/chance/cooldown/recursion ordering; empty holder1/294 reports do not prove missing code |
| Damage/heal/vitals | Effect151 validation, property/filter paths and Source implementations exist | Actual rows, caster/target ownership, stacking and Focus/damage/rounding outcomes; an assumed formula or a schema is insufficient |
| Buffs/CC/movement | `0810` wrapper/unit/absolute count and timer refresh; `0812` non-effect-ID key mapped | Initial identity availability, recipients/order, count limits, stack/DR/breakout/movement fixtures and actual production triggers |
| Summons/traps/vehicles/AI | Selected creation/ownership/wire and placement fixtures | Exact controller/service/seat/trigger graph, kits, social aggro/leash and lifecycle for each supported family |
| RavelSignal | Sender/effect surface exists | Receiver graph, mode/payload and script-state effects; no generic receiver behavior is inferred |
| LAS/AMP/attributes/Primal | Public Primal wire milestone and mapped local guards | Allocation catalog/rewards/validation/settlement, async server lifecycle and persistence/publication; see separate Source-probe candidate below |
| Quest/instance/path/achievement/map content | Existing reviewed bridges and scoped restoration fixtures | The exact generated next-slice producer/placement/dialog/action/reward evidence plus its required native smoke; one completed slice does not qualify all rows |

The [healing evidence](HEALING_PERSONAL_MODIFIER_EVIDENCE.md) explicitly separates
row validation from application and Focus rates. The
[Armor Pierce evidence](NATIVE_STATIC_ARMOR_PIERCE_EVIDENCE.md) retains pairwise
application/rating-threshold gates. [Spell auxiliary evidence](SPELL_AUX_CONSUMER_CORROBORATION_2026_11_05.md)
rejects arbitrary replay of equal counts because timers refresh. Current
`SapVital` Source uses selected flat/fraction heuristics; this is existing
emulator behavior, not newly verified retail mode semantics. Those boundaries
remain even where local fixtures pass.

The [gameplay/social audit](ALL_BLOCKERS_GAMEPLAY_SOCIAL_RESEARCH_2026_11_05.md)
adds an independent authored LAS immediate-result versus later-error flow.
Current handler mutation/publication offers a rejected-save/original-actor probe
that can proceed as Source work. No such probe was executed in this research
pass, so this is an actionable hypothesis, not a reproduced defect.

## Host and qualification gates

A fresh read-only discovery found `dotnet` on PATH, no `docker`, `podman` or
`mysql` command among those queried, no queried MySQL/MariaDB/Docker process,
and no default `docker_engine` pipe. These are bounded local observations, not
proof no external engine or differently named provider exists. No installation,
daemon, database, server, game client or Computer Use was started.

Provider/copied-upgrade, original-owner recovery, restart/outbox/broker, physical
socket, deployment/load and current native-client qualification remain separate
from REA field mapping. A DLL or addon cannot replace those executions. The
recorded client/CUA stop remains in force.

## Evidence identity and validation

STS DLL SHA256:
`aa0ef5888d17eb65da28b1cbef178d1650a837303c10a6fafc9f3c646c2f3fd1`;
fresh REA identity
`ev_dba29958b64299b3a64c41214e66af69697b23182238084a321fdaace185b187`.
Standalone Ghidra12.0.4/JDK21 opened the existing owned STS project read-only
with analysis disabled. REA's known unavailable native provider was not retried.
Candidate vtable inspection windows extend past some small tables; only the
explicit selected slots documented above are used as method evidence.

The [portable guard record](coverage/all-blockers-sts-guards-20261105.json)
binds the selected code, flags and table cells to the PE and its copied Ghidra
program. Raw logs, prefix cross-check and host results remain under
`artifacts/continuation-20261105/all-blockers/`. No C# tests or native protocol
session were run for this research-only pass.
All 13 selected code ranges and 12 additional anchors match the pinned DLL and
the read-only Ghidra program. The matches verify selected byte identity, not
complete grammar, runtime acceptance or optional-route qualification.
