# Confirmed support ticket storage

The real `ClientSupportTicketHandler` now waits for `AppendAsync` to confirm
storage before acknowledging success. The previous concrete file store returned
true as soon as it queued the writer; the regression received `Success=true`
while no writer had run and no submission file existed.

The writer serializes the original account/Character metadata and payload before
queuing background work. It appends one JSONL record, requests `Flush(true)` and
closes the stream before completing true. Serialization, queue admission and
write failures complete false. Under its exclusive writer, a partial append
restores the earlier file length. If that repair fails, the file path is blocked
from further acceptance within this process. Earlier records remain protected.
Legacy reports without a response retain their `TryAppend` queue-admission API
and existing record format. Production DI and storage location remain unchanged.

Ticket completion runs on the captured original event queue and checks the
original Player, account, IDs and session. A replacement suppresses the old
result without discarding the captured submission. A result enqueue rejected
before acceptance retries a fresh packet on that original queue, without
appending the ticket again. Invalid-language failure retains its existing path.

## Verification

All12 new concrete-writer/handler cases and57 scoped Support tests pass, with
zero failures/skips. Cases cover the queued-write boundary, frozen request data,
write/admission failures, partial append/failed repair, acknowledgement retry,
Player/account/queue replacement, failed/canceled storage and an unacknowledged
report control. Both final Source review axes report zero findings. The final
WorldServer build takes10.59s, test compile15.59s and scoped execution3s, with
zero build warnings/errors. These are observed validation times.

The [portable report](coverage/support-ticket-persistence-20261105.json) binds
the six changed Source files, normalized Source/Tools input identity, matching
owning/test DLLs, baseline/final results, reviews and incremental upstream check.
Local evidence is under
`artifacts/continuation-20261105/support-ticket-persistence/`.

No database/model/migration or wire field changed, and no runtime service,
MySQL or native client was started. The support-case database workflow,
readback protocol/backend and moderation tooling remain open. Damaged-path
quarantine is process-local. Physical crash/restart, cross-process replay,
deployed sockets and native/retail behavior remain unqualified; `Flush(true)`
alone is not power-loss proof. The overall Goal stays active. Defaults15:1/17:1,
no automatic44/seven baseline grants, and the client/CUA stop are preserved.
