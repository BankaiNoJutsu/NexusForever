# Item durability wire and repair publication

5 November 2026. Static inspection connects build16042 opcode `0x01A7` to
item durability. The repair producer now sends the committed item's GUID and
durability after its credit notice, through the original actor's publication
guard. Single-item and repair-all notices use the same settlement as payment.
Rollback sends no durability success. A lost commit acknowledgement resolves
the stored settlement without another debit. A first enqueue failure retries
only that notice, and session replacement suppresses remaining publication.

## Pinned static evidence

REA `open_binary` confirmed the archived `WildStar64.exe` with SHA-256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
LLVM disassembly and independent PE reads establish the following chain.
The deep REA provider remains unavailable. The binary was never launched.

| Stage | Address | Observation |
| --- | --- | --- |
| Registration | `140072caa` to `140072cd6` | Reader `14008ef80` is stored at stack+28, EDX receives `01A7`, and the registration vtable is called with object size16. This size includes decoded padding. |
| Reader | `14008efa8`, `14008efb8` | Reads GUID64 through `14006c120`, then32 bits through `14006c1c0` into decoded+8. The second primitive stores with MOVSS in both fast and fallback paths. Wire size is12 bytes. |
| Dispatcher | `1403ec6a0` | Validates session/channel, subtracts `0x61` from the incoming message ID and indexes DWORD RVAs at `1403f1344`. |
| Opcode slot | `1403f185c` | `1403f1344 + 4*(0x01A7-0x61)` contains RVA `3ef77a`. |
| Case | `1403ef77a`, call `1403ef780` | Forwards the entity and decoded payload to `1403b9690`. |
| Apply | `1403b9690` to `1403b9717` exclusive | Looks up the item by decoded GUID, preserves the old float at item+0x84, writes decoded+8 to that field and publishes the item event. A missing item returns without applying. |
| Event | `140afdd90` | ASCII `ItemDurabilityUpdate`, referenced directly by the apply helper. |

These independent registration, reader, dispatch and event anchors supersede
the old reputation-cluster inference from opcode adjacency. The old enum and
raw-bit model remain compatible aliases. `ServerItemDurability` supplies the
typed producer. Adjacent `01A8` selects a different case at `1403ef78c` and
writes item+0x88. This milestone does not add an emitter for that separate field.

The direct call was recovered with an overlapping raw E8/E9 target scan and
then confirmed at an LLVM instruction boundary. Exact absolute64 and RVA32
pointer searches alone found only the exception entry. They were insufficient
to conclude that this helper lacked a caller. Computed/virtual references and
other instruction forms are outside that scan's scope.

## Correction to historical exception-table annotations

The PE exception directory starts at `140dc7000` and has size `87f54`.
Historical notes and labels called several addresses inside this directory
"apply-table cells". Their12-byte entries are begin RVA, end RVA and unwind
RVA. They establish function ranges, not opcode dispatch. In particular

| Exception entry | Begin | End exclusive | Unwind |
| --- | --- | --- | --- |
| `140e00b64` | `1403b9690` | `1403b9717` | `140bbdf34` |
| `140e215ac` | `1405f2ae0` | `1405f2e86` | `140bef5dc` |
| `140e2163c` | `1405f3520` | `1405f354e` | `140bad354` |

The other seven cells in the historical partial map also decode as exception
entries. Their exact values are retained in the [portable evidence](coverage/item-durability-wire-20261105.json).
Do not recover an opcode walker from those entries or use them as independent
packet linkage. The public-event reader mappings and observed objective lookup
instructions retain their own evidence scope. This correction does not supply
new native event-delivery evidence.

## Validation and remaining gates

The pre-change repair regression reproduced five missing durability streams.
Its sixth failure was an incorrect fixture expectation for the existing
quarantine after session replacement during publication, which was corrected.
Local reproductions, REA identity, reference scanner and LLVM output are under
`artifacts/continuation-20261105/durability-wire/`.

Final owning/test-project builds passed with zero warnings or errors. The
changed producer build took38 seconds and the final test-only build23 seconds.
All24 repair/packet cases passed in9 seconds, followed by5 existing repair and
buyback handler controls. The tested Game, Network and Network.World DLLs match
the owning outputs. The mapping schema gate validates all5,016 rows across its
seven files. No full solution build or full test suite was run under the user's
scoped-only policy. The initial regression build took64 seconds and its nine
cases took9 seconds. The retained first result has five missing-notice failures
and the fixture expectation failure described above.

The final review checked registration compatibility, immutable post-commit
notices, rollback suppression, bounded retry and actor replacement. No shared
settlement or schema code changed.
The earlier [seven-case MySQL settlement qualification](DESTINATION_PROVIDER_QUALIFICATION.md#vendor-repair-settlement)
covers the previous repair candidate's payment and durability persistence.
The added notice producer changes no database schema or settlement algorithm.
Its new publication cases require their own qualification and do not inherit
configured-provider or deployed socket evidence from that earlier run.

Native display, deployed sockets, physical connection failure and restart
delivery remain open. Source and static client evidence do not establish retail
completeness. Computer Use and native-client launch remain stopped.
