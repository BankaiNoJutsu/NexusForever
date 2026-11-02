# Mail attachment index evidence

1 November 2026 destination continuation. These are mapped static observations,
not Verified packet semantics or native runtime qualification. No server index,
attachment or wire behavior was changed from this investigation.

The read-only archived build16042 client remains SHA256
`231BB2BB3FC6C37F3E8A43A0BA965CC3645287BBC6CCAD83D073A495C396B3E5`,
bound by the prior REA PE record
`ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`.
The REA layout inspector returned `capability_unavailable`: its current artifact
profile supports Linux x64 only. That complete result is retained. No setup,
provider repair, client execution or process attach was attempted. Installed
LLVM22.1.6 disassembly and explicit PE section/exception-directory parsing supply
the separate observations below, following the earlier static cross-check's
chained-unwind method.

The candidate 16-byte string/function neighborhood contains 19 literal pairs.
Its complete table boundaries and registration root are not established. The
following selected pointers and called bodies have complete observed exception-
directory chained groups; this does not establish every possible semantic tail.

| Anchor | Observed fact |
| --- | --- |
| `140b70100` | Literal `TakeAttachment` points to `1406b4ad0` |
| `1406b4b37` | The Lua-facing wrapper subtracts one from its default/parsed index before calling `14062a8c0` |
| `14062a9df–14062aa00` | The called body uses opcode `0126`; its payload stores the mail identity, supplied zero-based index and mailbox unit identity before the send helper |
| `140b700b0` | Literal `GetMessageInfo` points to `1406b43c0`; that lookup calls `1406b33f0` to build its result |
| `1406b4290–1406b42b0` | The builder exports slot ordinal plus one under the UTF16LE key `nServerIndex` at `140b370a8` |
| `1406b3ed0–1406b4309` | The builder examines slots up to the stored count with an outer limit of ten, advances records by `60` hex bytes and skips a slot when its item lookup fails |
| `140b700f0` | Literal `TakeAllAttachments` points to `1406b4770`; its called body prepares opcode `0125` with a collection of mail identities |

The five instruction anchors, literal pointer pairs, UTF16 strings, original
code ranges and complete disassembly outputs pass a separate local hash/byte
verifier. Scripts and raw outputs are under
`artifacts/continuation-20261031/native-static`; `mail-verification.json` records
the bound identities and qualification limits. The initial verifier used a
24-byte hardcoded span for the terminated `nServerIndex` string and failed; its
span now derives from the actual encoded length. This was a verifier correction,
not a client or server change.

Current server `MailItem.GetAttachment` and the legacy `AttachmentDelete` path
use mutable list positions. Concrete-Inventory bulk selection now captures
exact link references and its replies retain the persisted index; this source
composition fix does not reinterpret wire indices. `ServerMailAvailable` still
emits a compact attachment list without an explicit stored index. These paths
expose a concrete consistency question after
a take or reload with a missing slot. The static producer/builder facts above
do not yet prove whether the receive/update path clears, compacts or replaces
slots. In particular, raw immediate-byte candidates containing `05A8` are not
automatically opcode registrations. The later bounded examination of the
140367f17 candidate now shows its05A8 argument becoming a list-link offset:
140370b57 retains it in rsi and140370b8c reads a next link at rbx+rsi+8. The
indirect callback140354f50 computes a difference of squared distances from two
integer coordinates. That context supports discarding this specific lead as
an opcode registration. It does not find the actual05A8 response/update handler.

This follow-up has one complete observed chained group and a separate128-byte
callback window with unknown complete body extent (no exception entry). The
window includes adjacent code; its observed ret is not a complete CFG claim.
The initial inspector failed requiring an exception entry and was corrected to
record that unknown explicitly. Eight byte anchors and exact output ranges
verify under native-static/receive-probe-verification.json. Other raw immediate
matches remain unclassified; no receive-handler claim or source correction is
made from them.

The later complete raw32-value inventory finds143 matches for the five current
mail opcode values across raw PE sections; only four nearby-value matches occur
in one registration-shaped routine at14006c290. This is a byte inventory, not
143 handlers. Its05A8 sequence at140072843–14007286f supplies callback14008e890
and object size16 to a virtual method. The callback transfers fields at offsets
0,8,12 using64,8,32-bit primitives, consistent with the current reconstructed
ServerMailTakeAttachment id/result/index shape. Four complete observed chained
groups and twelve byte anchors verify in native-static/mail-receive-verification.json.
This identifies a codec candidate, not MailSystem's response consumer; named
field semantics are inferred from the association and current source. It does
not establish slot clearing, compaction, replacement or persistent-index meaning.
No source/index change or Verified/native/retail promotion follows.

Recover the `05A8` receive/update path and independent table/Lua/sniff/packet
corroboration before promoting an index correction through the continuation
guide's Verified gate. Reproduce the bulk-selection and post-take/relog cases
through the existing caller APIs, preserve confirmed ordinary Item/mail recovery,
and keep economic atomicity separate from any newly inferred wire behavior.
Computer Use/native client launch remain stopped; source tests cannot establish
retail completeness.

Five explicitly mapped analyst aliases were added to `function_labels.csv` for
these selected bodies. No Ghidra project or export was modified or claimed
applied; the existing generated function inventory remains a dated snapshot.
