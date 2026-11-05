# Runtime blocker evidence follow-through - 5 November 2026

This pass used the connected REA server, primary-source web research, selected
LLVM disassembly and an existing standalone Ghidra installation. Static
decompilation is available again through an owned project copy. The STS RSA
component envelope is now mapped, and historical Fortune rotation policy has
primary corroboration. None of the five consolidated production-emitter gates
is closed by this pass. No production Source or authentication behavior changed.

## Inputs and tool recovery

The matching artifacts were found in the read-only archive
`C:/GIT/_archive/cluster-3d/NexusForever/Decomp/Client64`.

| Input | SHA256 | Fresh REA identity evidence |
| --- | --- | --- |
| WildStar64.exe, build 16042 | `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` | `ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4` |
| StsConnLib64.MT.dll | `aa0ef5888d17eb65da28b1cbef178d1650a837303c10a6fafc9f3c646c2f3fd1` | `ev_dba29958b64299b3a64c41214e66af69697b23182238084a321fdaace185b187` |

REA 6.8.0 successfully inspected both PE import tables. Its native decompile
request returned `provider_unavailable`: Ghidra is unconfigured, Hopper is
unsupported on this host, and IDA/Kuna are unconfigured. No unchanged request
was retried. The returned provider diagnostic requires Ghidra 12.1.x; the
existing installation is 12.0.4. It was not misrepresented as an available
REA provider.

The existing JDK 21 and standalone Ghidra 12.0.4 were sufficient for a separate
supported inspection path. The archived WildStar project was copied into
`artifacts/continuation-20261105/blocker-unblock-research/ghidra-project`.
Postpasses opened that owned copy read-only with analysis disabled. A fresh
STS project in the same owned directory completed default analysis in 84
seconds, then supplied read-only follow-up decompilation. No client/DLL was
executed. No persistent REA, Java, PATH or application configuration changed.

REA Evidence was exported to `rea-bundle.json` in that artifact directory and
the REA session was closed. Standalone Ghidra output is separate inspector
evidence, not REA decompiler output. Generated projects, pseudocode and raw
logs remain outside tracked Source. Portable byte anchors are linked below.

## Indexed housing, crafting and tracked-unit dispatch

The existing entity dispatcher `1403ec6a0` bounds the unsigned
`opcode - 0x61` index and reads DWORD RVAs from `1403f1344`. Reading the exact
slots avoids the earlier literal-opcode search limitation.

| Opcode | Slot | Case | Observed next step |
| --- | --- | --- | --- |
| `0501` | `1403f25c4` | `1403efe75` | Calls `1404ba470`, the single-neighborhood apply path |
| `0506` | `1403f25d8` | `1403efe84` | Calls `1404ba4f0`, the neighborhood-list cache/event path |
| `0848` | `1403f32e0` | `1403f0bb9` | Calls the mapped tracked-unit disable path `1403f4200` |
| `0849` | `1403f32e4` | `1403f0ba7` | Calls the mapped tracked-unit update path `1403f4170` |
| `0853` | `1403f330c` | `1403ef2fa` | Routes the existing crafting-finish apply path |
| `0854` | `1403f3310` | `1403ef310` | Routes the existing current-craft apply path |
| `084B`, `0855` | `1403f32ec`, `1403f3314` | Both `1403f12d2` | This dispatcher's default reporting path ends with return value 1 |

The last row means **try the next handler on this path**. It does not prove
that these registered packets are globally unused, nor that their names,
fields or send timing can be inferred from adjacent crafting opcodes.
Other handler/delegate paths remain a separate question.

Fresh decompilation reconfirms that `0506` clears/replaces cached rows and
dispatches `HousingNeighborhoodRecieved`; `0854` applies the decoded crafting
state and dispatches `CraftingUpdateCurrent`; and tracked-unit update/disable
mutate the client cache before their events. These observations identify
client consumers, not a retail server push trigger.

The copied program's executable SHA256 and all **20 selected byte anchors**
were checked in Ghidra against the pinned PE. Six entity-aux table slots are
included as positive controls. See
[portable dispatch anchors](coverage/runtime-blocker-dispatch-20261105.json).
Raw disassembly, exact table destinations and verifier output are in the
owned artifact directory. Two neighborhood debug strings had zero recorded
references in the copied project; that bounded result is not proof that no
join/leave sender exists.

## STS: RSA component representation mapped

The fresh STS import reproduces the previously mapped token validation,
constructor and MD5 comparison. It additionally follows the constructor into
its component reader and integer storage, instead of stopping at CKeyRsa.

At `180037c30`, the constructor installs the table at `18012dc98` and invokes
`18003a700` twice, using the same advancing length/buffer pair. The destination
objects are `this+0x08` and `this+0x28`. Each invocation reads a native 32-bit
length, advances the buffer by four bytes, imports that many bytes, then
advances by the component length. On the pinned x64 little-endian artifact,
the observed component envelope is:

```text
uint32LE lengthA | integerA[lengthA] | uint32LE lengthB | integerB[lengthB]
```

The importer at `1800f48c0` copies successive DWORDs into successive limbs,
zero-pads a partial final DWORD, and trims zero words from the high end.
The exporter at `1800f4650` exposes the limb buffer with a length of
`usedWords * 4`. This maps a little-endian limb representation, rather than
an ASN.1 key parser at this constructor boundary. It supplies no captured
server key, accepted malformed-input policy or interoperability proof.

The first virtual slot points to `180037ec0`, which forwards to `180037d30`.
That transform imports the input as the same integer representation. It
passes component B to the divide/remainder and modular-power helpers and
component A to the modular-power exponent input. The modular-power routine
`1800f6030` scans component A in two-bit groups. These relationships support
the **derived** roles A=exponent and B=modulus; raw calls and component storage
are directly observed. The wrapper iterates over input chunks in base B,
accumulates transformed components and exports words. A standard padding or
trust policy must not be selected from the class name alone.

This narrows the former wholly-unknown key-format blocker. It does not
qualify server private-key construction, signature trust, padding/inverse
operation, premaster handling, TokenKeyData reply grammar, optional-route
startup order or post-token session encryption. No optional handler is enabled.
The validation boundary still compares 16 bytes of transformed signature
output with MD5 of the supplied public-key field, as previously mapped.

Eight selected byte anchors and the fresh project's library SHA256 were
checked against the REA-bound input. See
[portable RSA-component anchors](coverage/sts-rsa-components-20261105.json).
Decompilation and LLVM outputs are retained in the owned artifact directory;
the four follow-up passes did not execute the DLL or change the archived input.

## Web evidence and remaining implementation gates

Detailed sources, dates, hashes and search boundaries are in the
[runtime web note](RUNTIME_WEB_UNBLOCK_RESEARCH_2026_11_05.md) and
[Contract/communicator web note](CONTRACT_WEB_UNBLOCK_RESEARCH_2026_11_05.md).

| Roadmap area | New usable evidence | Remaining gate |
| --- | --- | --- |
| F-025 entity aux | Existing 2-5 November apply/field work supersedes the old reader-only summary; six exact table slots were rechecked | Other five producers still need eligibility, recipients and send-order proof; `08CC` Scientist naming is already implemented |
| F-025 tracked units | Exact indexed update/disable routes and cache-before-event order; subsequent lifecycle work proves the first word is the TrackingSlot row key | Retail row selection, neutral trailing bits and update/disable production lifecycle |
| F-004 neighborhoods | Exact indexed apply routes; publisher corroboration of temporary versus reserved community plots | `0501/0506` trigger, backing fields and retail order; community policy does not establish that legacy cache contract |
| F-008 crafting | Finish/current-craft consumers rechecked; `084B/0855` follow this dispatcher's default path | Other handler owner or accepted capture; discovery/service/complex-craft rules and item-patch cadence |
| F-031 Fortune | Publisher distinguishes sequential Medieval color rotations from Island Paradise's simultaneous colors | Exact item/probability arrays and build-16042 active catalog; no universal rotation algorithm is justified |
| Contract public `07CE` | Original reset discussion is directly readable; held active Contracts can span days | Final-first/unclaimed and excess-point handling, complete actual-table delivery and recovery |
| Communicator #609 | Authored schemas recovered, with exact-build limitations retained | Actual row corpus or independent runtime/sniff corroboration; a schema is not a row-value or zero-sentinel proof |
| Optional STS | Two length-prefixed integer components and their native transform boundary mapped | Accepted optional-route transcript and the server/session contracts listed above |
| Native gameplay/UI | No observation attempted | The recorded client/CUA stop remains in force; earlier visual world entry does not qualify current binaries |
| Docker/Aspire execution | A fresh read-only host check found no Docker CLI, `com.docker.service`, standard Docker Desktop executable or matching engine pipe | A usable container engine is a prerequisite for the actual Aspire container gate; no installation or daemon launch was attempted |

The first-party Fortune announcements establish different historical policies:
[Medieval collection](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1486501811)
and [Island Paradise](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1493140524).
The [publisher Communities article](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=376570&count=1&maxlength=0&feeds=steam_community_announcements&enddate=1503943857)
corroborates logout/reservation policy. None supplies packet-level server timing.

## Validation and continuation

- REA PE inspection succeeded for both pinned artifacts; native-provider failure
  is retained separately. The REA bundle was exported and the session closed.
- Standalone Ghidra recovered five existing client consumers, inspected the
  single-neighborhood path, completed a fresh STS autoanalysis and followed the
  component parser/transform. Selected byte checks bind both program databases
  to the inspected artifacts.
- Research notes and portable JSON were checked for local links, syntax and
  whitespace. No production code changed, so no application build or test suite
  is claimed for this pass. Concurrent marketplace work is outside its scope.
- No client launch, Computer Use, deployment or database mutation occurred.

The Docker host check is retained as `docker-availability.json` in the owned
artifact directory. It covers the command/service/standard executable and pipe
names inspected, rather than asserting absence of every possible container tool.

Continue static STS mapping from the now-known component/operation boundary.
For the hidden retail producer, weight and reset-edge gates, the next useful
input is accepted traffic/server data or matching table/packaged UI evidence,
not another unchanged general web search. Preserve the existing runtime gates
until that evidence supports a concrete implementation.

The subsequent [next-target pass](NEXT_REA_UNBLOCK_TARGETS_2026_11_05.md)
finds new Primal Matrix `06DF`, matching `0600` and spell `0810` consumer
associations. It reuses the recovered static workflow and distinguishes those
mapped next steps from the production gates retained in this report.
