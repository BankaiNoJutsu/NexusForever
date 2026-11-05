# STS token-key validation and local cipher transition

5 November2026. Disposition: **Mapped static client behavior; server optional
authentication remains blocked.** No production Source or Tools changed, and no
client/DLL was executed. This pass uses the reverse-engineer-anything workflow.

## Input and inspection boundary

REA6.8.0 freshly inspected the pinned `StsConnLib64.MT.dll`, SHA256
`aa0ef5888d17eb65da28b1cbef178d1650a837303c10a6fafc9f3c646c2f3fd1`,
retaining identity/import Evidence
`ev_dba29958b64299b3a64c41214e66af69697b23182238084a321fdaace185b187`.
Its import inventory covers six DLLs/182 functions. The Evidence bundle was
exported and REA closed; no native-provider setup or unavailable decompile call
was repeated. Ghidra12.0.4 and JDK21 separately inspected the existing owned STS
project copy with analysis disabled and read-only mode. Its output is standalone
inspector evidence, not REA decompiler output. LLVM supplied independent
instruction observations from the pinned PE.

The [portable record](coverage/sts-token-key-transition-20261105.json) retains
20 exact byte/branch anchors and12 selected code-range hashes checked both
against the PE and the Ghidra program. Eleven ranges have observed exception
metadata; the small key-length getter has an explicitly bounded disassembly
window without exception metadata. These are static consistency checks, not
native executions or end-to-end authentication tests. Raw pseudocode, project
data, scripts and logs remain under
`artifacts/continuation-20261105/sts-trust-boundary/`.

## What the key-material gate authenticates

`StsConn_SendTokenKeyData` at`18000a730` reads `ServerRand`, `ServerPublicKey`
and `ServerSignature`. Its call at`18000a917` passes the staged caller key
material, public-key length/buffer, and signature length/buffer to`180012de0`.
The validator constructs its RSA object from the staged caller key, transforms
the supplied signature through virtual slot0, computes the existing MD5 wrapper
over the supplied public-key bytes, and compares16 bytes. Register preservation
and calls at`180012e1f`, `180012e60` and`180012e75` establish those arguments.

`ServerRand` is not a direct input to that digest comparison. Reading all three
fields must not be described as the signature directly authenticating all three.
The actual trusted key value/provisioning, signature padding, output-length and
malformed-input policies, server private-key behavior and interoperability are
not established here. Failure of the gate sets the selected auth state's error
to4; the premaster/request branch follows the successful gate.

The premaster helper at`180012f50` produces a48-byte buffer with literal prefix
`03 03` and asks`1800365b0` to fill the other46 bytes. This maps a buffer format
and call, not observed random output or a complete entropy-failure audit. It
does not alone establish a TLS protocol/version. The sender encrypts premaster
and token through its constructed server-key object and writes the existing
`PremasterSecret`, `AuthnToken`, optional `AuthProviderCode`, and `AppId` fields.

## TokenKeyData notification and filter construction

The observed `CLoginTokenStartTxnNotify` and `CTokenKeyDataTxnNotify` vtables
each have six entries. Their slot`+0x18` values are`18000c330` and`18000c340`.
The first forwards to the token-key sender. The second forwards the retained
auth state and reply object to`18000ac30`, obtaining a third object through
`18002f7d0`. The shared notification dispatcher at`180031570` still performs
its generic reply/status checks before the selected slot is reached.

The selected completion at`18000ac30` overwrites its incoming reply-object
register before use and makes no application reply-field reads. Instead it
passes staged premaster, server-random and client-random buffers to`1800131f0`
with mode4. The table at`180129b10` contains mode4 layout bytes`10 80 00`.
The derivation routine requests48 bytes from its first helper call, then288
bytes from its second call. It skips two16-byte prefixes and supplies two
128-byte key outputs. The getter at`180012da0` returns128 **bytes**, not bits.
The follow-up below maps the selected derivation internals and filter paths.

`18003a860` builds two key objects through`1800f73e0`. That factory reaches
`1800f7320`, whose RTTI names `Crypt::CKeyRc4` and whose256-entry initialization,
key-byte cycling and swap loop corroborate RC4 key scheduling. The factory's
source-path literal names `CptRc4.cpp`. The filter stores the second derived
key's object at`+0x10` and the first at`+0x18`; a virtual call on its first key
also occurs. The follow-up below resolves its selected behavior and maps which
key each local filter path uses.

After the expected sizes/pointers pass, the completion calls virtual slot`+0x50`
on the third object with the filter and sets auth error0. Invalid derived
outputs select error4; a null filter selects error`0x30`. The selected error
numbers are native observations, not newly assigned server result enums.

## Remaining implementation gate

The old "notification not decompiled" gap is narrowed to a mapped callback and
local filter-construction transition. No application-field reads in this selected
completion do **not** prove that an empty wire reply is accepted: generic XML,
status, message/cardinality and transaction validation remain upstream.

Required next evidence remains an accepted optional-route transcript plus the
trusted key/provisioning, server inverse/private-key and padding behavior,
independent corroboration of the mapped derivation/filter rules, exact reply
grammar and startup ordering. Do not enable `LoginTokenStart`/`TokenKeyData` from
this map.
The implemented password/SRP path is unchanged. No C# build or suite was run for
this static-only pass. Client/CUA remain stopped; defaults15:1/17:1 and no
automatic44/seven baseline Character grants are preserved.

This read-only pass applies no labels and exports no new applied-label
inventory. `function_labels.csv` and mapping CSVs retain their prior bindings:
no opcode/layout correction, Source implementation or live evidence bundle is
established. The internal filter offsets remain in this focused map; they are
not promoted as wire/server structure fields. The records carry these
observations until a suitable annotation/export pass is performed.

## Follow-up: selected mode4 expansion and filter paths

5 November2026. The [follow-up record](coverage/sts-key-derivation-20261105.json)
reuses the exact REA artifact identity above and the prior mode4/caller/factory
ranges. Ten newly selected routines contribute12 code ranges;24 data anchors
include both vtables, the selected prefix pointers/bytes, the constant-return
leaf and the SHA-256 round table. All36 new range/anchor hashes match the pinned
PE and the read-only Ghidra program. This remains **Mapped** static evidence.

### Expansion inputs and digest output

The expansion helper at`180012f90` concatenates a table-selected prefix, secret,
first seed and second seed, then calls`180039bd0`. That wrapper passes one buffer
to`180039a30`. Its eight initial words,64-byte compression at`180039450`, all64
round constants at`18012dcf0`, padding/length handling and big-endian32-byte
output at`180039880` identify SHA-256. The expansion then hashes secret followed
by those32 bytes through the previously mapped MD5 wrapper`18003a670`.
`18003a560` emits its four state words in little-endian byte order. Up to16 bytes
from that digest become the next output block.

For the selected48-byte and288-byte requests, let `||` mean byte concatenation
and slices use zero-based, end-exclusive offsets. The bounded reconstruction is:

```text
prefix(i) = byte('A' + i) repeated i times, for i = 1..18
block(i)  = MD5(secret || SHA256(prefix(i) || secret || seedA || seedB))
Expand(secret, seedA, seedB, length) = concatenated block(i), truncated to length

master   = Expand(premaster, ClientRand, ServerRand, 48)
keyBlock = Expand(master, ServerRand, ClientRand, 288)
firstKey = keyBlock[32:160]
secondKey = keyBlock[160:288]
```

The observed copy at`180013079` indexes the pointer table`180171b30` with
`(counter %26)+1`, and the copied length is that same index. Entry0 points at
`A`, entry1 at `BB`, entry2 at `CCC`, etc. Consequently the actual first copied
prefixes are **`B`, `CC`, `DDD`**, ending with18 `S` bytes in the selected288-byte
request. Substituting an assumed TLS PRF or the usual `A`, `BB`, `CCC` sequence
would change the mapped inputs. The first expansion uses indices1..3; the second
uses1..18. No general table bound, wrap policy, zero-length behavior or arbitrary
output-size support is asserted. Ghidra types the table start as a single
pointer, not a declared array.

The caller/deriver argument trace at`18000acd5`, `1800132bc` and`180013301`
establishes the seed order above. Two16-byte prefixes are skipped; they are not
assigned MAC or other protocol meaning by this pass. No native digest or session
test vector was executed. The formula is an explanation of the selected code,
not an independently verified server implementation.

### Key ownership and the selected state call

The factory`18003a860` receives the second derivation output as its first
argument and the first output as its second argument. Its first constructed
key object therefore contains **secondKey**. It calls that object's virtual
slot`+0x10` at`18003a896`, stores it at filter`+0x10`, and stores the other object
(**firstKey**) at filter`+0x18`.

The `Crypt::CKeyRc4` vtable`18014ec78` has five observed slots. Slot`+0x10`
points at`180100a00`, whose six bytes are `b8 01 00 00 00 c3`: return1, with no
memory write or further call. The recovered shared-case name is not a semantic
method name. **That selected invocation does not discard keystream or advance
RC4 state.** This does not claim that no other part of a full session can do so.

The filter vtable`18012ddf8` has nine observed slots:

| Local path | Key object | Observed contract |
| --- | --- | --- |
| Slot`+0x28`, `18003aa40` | filter`+0x18`, **firstKey** | Walks16-byte length/pointer descriptors, transforms each through key slot`+8`, gathers bytes, then forwards length/buffer through downstream slot`+0x38`. |
| Slot`+0x38`, `18003a8f0` | filter`+0x10`, **secondKey** | Transforms input through key slot0 into its retained buffer, calls downstream slot`+0x28` with the accumulated bytes and a consumed-count output, then retains the unconsumed tail. |

Key slot0 at`1800f74f0` forwards to slot`+8`'s implementation`1800f7420`.
That implementation advances persistent RC4 indices/permutation and XORs each
input byte. The vector-write and buffered-parser contracts support **inferred
client outbound=firstKey, inbound=secondKey**. This is local dataflow evidence;
socket traffic, a working server inverse and accepted transition timing remain
unobserved. The existing password/SRP route is not evaluated or changed here.

### Follow-up verification and limits

Raw scripts, decompilation, bounded LLVM output and hash checks are retained in
`artifacts/continuation-20261105/sts-key-derivation/`. The first prefix-inspection
helper attempted index26 and failed to read mapped memory before decompilation;
its failed log is retained. The corrected inspection completed; the portable
record deliberately limits the formula to the selected requests and indices.
That helper error is not target execution or a runtime failure observation.

Source/Tools remain unchanged, so no C# build or tests were run. The historical
handoff Source-divergence result is reused. No native client/DLL execution,
Computer Use, provider/configuration installation or production behavior change
occurred. Trust provisioning, RSA inverse/padding/malformed-input policy,
generic reply grammar, accepted optional-route traffic and startup ordering
remain the next authentication gates. Continue independent roadmap behavior
while those external inputs remain missing.
