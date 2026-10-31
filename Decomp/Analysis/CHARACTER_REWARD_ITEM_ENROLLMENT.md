# Character reward Item enrollment and outcome recovery

Later30 October continuation: the [inventory aggregate](CHARACTER_REWARD_INVENTORY_DELIVERY.md) now implements reservation, multi-item enrollment/recovery/publication and save/economy coordination, with7,364 tests,31 compatibility probes and38 final MySQL owner cases. The7,307 leaf proof below retains its original scope. Production earning/repository/claim routing and native UI remain open.

30 October 2026. The Item persistence bridge now stages reward merges and private new stacks without provisional live quantities. **At this earlier leaf checkpoint, the aggregate and production routing were unwired; the aggregate is implemented by the linked later continuation.** Existing direct prizes, satchel behavior and entitlement defaults remain unchanged.

## Implemented contract

[`Item.RewardReservation.cs`](../../Source/NexusForever.Game/Entity/Item.RewardReservation.cs) extends the [preparation guards](CHARACTER_REWARD_INVENTORY_RESERVATIONS.md). An existing bound stack retains its live quantity while an explicit Stage enrolls the final quantity through Item.Persistence. Earlier ordinary dirty count and other fields participate in that SQL snapshot. PendingCreate is supported. New private stacks use a copied canonical template and an already allocated GUID, stay outside public bags, and retain that GUID across verified rollback retries.

The leaf requires the aggregate's receipt/claimed-state validation callback; it does not decide eligibility or create a production receipt. Tests enroll the actual Character claim and claimed-slot models in the same transaction. Stage failure permanently rejects the supplied Character context, including failure after partial attach. Ordinary Item.Save can reuse only a fully enrolled override in that same healthy context; foreign or premature saves reject their whole context. Same-context reuse requires the same frozen award and guard. Retry retains quantity/GUID/database identity while a fresh guard binds the new unit.

EF validation distinguishes before-flush, before-implicit-commit and before-explicit-commit phases. It requires the Item's expected write state, frozen scalar values and permitted original values. Detaching, marking the unflushed Item unchanged, altering tracked identity/quantity/codecs, losing the claim or changing the claimed bit cannot leave a partial unit commit-ready. Explicit commit without an observed flush rejects. A suppressed zero-row save cannot acknowledge an unwritten override.

Commit does not publish the Item automatically. After an observed flushed terminal commit, PublishCommitted adopts the persisted baseline and quantity, acknowledges only captured revisions and retains later durability/rune/microchip intent. It does not mark a second dirty award or perform network/quest work. New-stack publication retains binding, normalized initial charges, codecs and the game-table owner; a first-wire regression verifies that a defined rune socket is not mislabeled as an added socket. Missing initial charge/default-socket normalization and lossy/noncanonical codecs reject before staging.

False completion, disposal, savepoint uncertainty or a lost acknowledgement retains ownership and the process/configured-database GUID lease. Dispose cannot release a staged or unresolved operation. Fresh resolution requires an untracked **serializable** transaction in the same configured database and the repository's exact receipt decision in that transaction. A committed receipt plus the exact after state permits publication; an absent receipt plus the exact before state permits retry/cancellation. Mere receipt existence cannot resurrect a consumed, moved or changed Item. Conflicts quarantine the operation. The repository must finish the original unit before resolution and retain durable receipt witnesses across cycles.

Authoritative-abandon releases the process lease only after terminal completion and permanently prevents writes from the abandoned live Item, permitting a newly loaded owner to proceed. These leases order in-process Item owners; they are not cross-process concurrency tokens or substitutes for the future repository's database locks and receipt constraints.

## Verification

Final evidence root: `I:\GIT\NexusForever-assessment-report\remaining-goal\item-reward-enrollment\final`.

- Full `Source\NexusForever.slnx` build: **0 warnings, 0 errors** (`verified-build.log`).
- Full suite: **7,307/7,307**, zero skipped. `full.trx` SHA256: `32809DF3D8232F5D58011A955A75351A6E4FA71B7AB89A61E7D98C440A3DF652`.
- **53 new cases** since7,254:48 Item enrollment cases and5 validation-phase/reentrancy cases. Existing ordinary Item durability45, preparation23 and all other cases remain green.
- **31 unchanged compatibility probes** pass against the frozen final references.
- **47 MySQL cases** pass in fresh retained `nexus_assessment_reward_item_30a2`:46 unchanged source cases plus one MySQL-specific weaker-isolation rejection. The SQLite-specific trigger and deliberate SQLite foreign-provider probes run in the main suite and are explicitly excluded from this provider replay. The case list includes pure input guards as well as actual commit, rollback, lost-ack, receipt, new-stack, consumption and lease cases;47 does not mean47 concurrent load scenarios.
- Provider case rows are snapshotted and hashed before deleting only the exact synthetic owner42 in the newly created proof schema. The proof uses current-model EnsureCreated, not migration or gameplay eligibility. Private configuration is unchanged. `MySqlProof/result.json` records loaded/frozen hashes, per-case snapshots and exclusions.
- `VERIFICATION.json` audits169 DLLs and5,713 Source files, the exact test/compatibility/provider results, six changed Source paths relative to7,254, and original checkout cleanliness. Entitlement source hashes remain unchanged.

The intermediate7,305/45-case checkpoint is retained separately under the parent evidence directory; it is not relabeled as this final result. An initial test used a nonexistent location enum, and another tried to mutate an EF-protected primary key during setup; both fixture errors were corrected and retained in earlier logs. The final tracked-original-values regression exercises a legal EF mutation rejected by the owner guard.

At this leaf checkpoint, the running World remained the earlier **7,182** binary/PID31676; Game, Character and World assembly hashes still match that frozen checkpoint. Its26 live cycles and earlier migration/item proofs retain their original scope. Computer Use remains stopped. No new native Challenge, pet or Q5597 UI claim follows from these database/wire tests. Strict content coverage remains31 CSVs/167,484 rows, all not_retail_complete.

## Integration plan at the earlier leaf checkpoint (aggregate now implemented)

Pair all Item/Bag capabilities atomically under a whole-inventory logical mutation lease. Freeze template/default sockets once, allocate IDs only after eligibility wins, compose the multi-item receipt payload, stage receipt/claimed state/all Items in one repository unit, and resolve the entire delivery consistently before publishing any part. Defer elapsed expiration time safely; coordinate normal/direct/cleanup Player saves before Auth work; preflight trade/mail/auction/use before spending or removal. Suppress stale-actor packets, admit new stacks into reserved slots and refresh collect objectives once after bookkeeping succeeds. Then wire the character track repository, attempt/medal producer and claim routing. F-033 remains partial.
