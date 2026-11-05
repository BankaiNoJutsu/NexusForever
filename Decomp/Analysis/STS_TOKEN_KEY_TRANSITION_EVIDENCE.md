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
Exact derivation internals are not fully validated by this pass.

`18003a860` builds two key objects through`1800f73e0`. That factory reaches
`1800f7320`, whose RTTI names `Crypt::CKeyRc4` and whose256-entry initialization,
key-byte cycling and swap loop corroborate RC4 key scheduling. The factory's
source-path literal names `CptRc4.cpp`. The filter stores the second derived
key's object at`+0x10` and the first at`+0x18`; a virtual call on its first key
also occurs. Cipher directions and that state operation remain unassigned.

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
complete derivation/direction/state-advance rules, exact reply grammar and
startup ordering. Do not enable `LoginTokenStart`/`TokenKeyData` from this map.
The implemented password/SRP path is unchanged. No C# build or suite was run for
this static-only pass. Client/CUA remain stopped; defaults15:1/17:1 and no
automatic44/seven baseline Character grants are preserved.

This read-only pass applies no labels and exports no new applied-label
inventory. `function_labels.csv` and mapping CSVs retain their prior bindings:
no opcode/layout correction, Source implementation or live evidence bundle is
established. The internal filter offsets remain in this focused map with their
direction/state semantics unassigned; they are not promoted as wire/server
structure fields. The record carries the new observations until a suitable
annotation/export pass is performed.
