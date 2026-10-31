# Character reward inventory reservation foundation

Latest30 October continuation: [inventory reward delivery](CHARACTER_REWARD_INVENTORY_DELIVERY.md)
passes7,364 tests with zero build warnings/errors,31 unchanged compatibility
probes and38 final MySQL inventory-owner cases. Whole-delivery reservation,
receipt/item enrollment, recovery, publication, expiration deferral and Player
save/economy preflight are implemented. Runtime-discovered Auth realm refresh
now preserves health for unchanged endpoints. Production Challenge repository,
eligibility/earning/claim routing and native UI remain open. F-033 stays partial.

Current isolated runtime: World48568/Auth48880 use frozen7,364 and pass200/200
audited cycles, including a70-second mixed run across the Auth definition refresh.
Seven other services retain their identities; six queues drain naturally. Defaults,
original data and private configurations are preserved. Computer Use remains stopped.

30 October 2026, Europe/Zurich. The low-level preparation guards are implemented and verified. **Those aggregate features were not implemented at this earlier preparation checkpoint; they are implemented by the linked7,364 continuation above.** Existing Challenge prizes and entitlement defaults remain unchanged.

## Implemented boundaries

`Bag.Reservations.cs` reserves an exact copied set of occupied and empty slots, with all-or-nothing acquisition under the bag mutation gate. No item ID, public item quantity or packet is produced during preparation. Empty reservations reduce the new `AvailableSlots` query while `SlotsRemaining` retains its physical-empty-slot meaning. Legacy `IBag` implementations inherit compatible defaults.

Ordinary allocation and the existing bound-item planner skip reserved merge targets and empty destinations. Bound creation retains charge/expiration compatibility, merges before new stacks, physical bag destinations, and binding before its first packet. Ordinary creation retains its own binding and satchel behavior. Bag Add/Remove/Move/Swap reject overlap before mutation; resize and snapshot restoration reject reserved state. Resident identity checks reject a different object claiming the same slot. A reserved destination capability can admit an already adopted item without invoking its scalar setters; it does not establish commit evidence itself.

`Item.RewardReservation.cs` provides preparation-only scalar ownership acquired through the owning Bag gate. This serializes preparation against ordinary mutations of that bag. Stack count, owner, location/index, charges, expiration, binding changes and deletion reject before assignment or dirty revision changes. Earlier ordinary dirty intent and PendingCreate are preserved; later durability and codec changes remain possible. Ordinary Item saves reject and permanently reject their Character context, including clean items which would otherwise be skipped. An unfinished or uncertain ordinary save must settle/reconcile before preparation. No receipt staging API can yet retain these capabilities through a database outcome.

`CharacterContext.SaveValidation.cs` permanently rejects a partially enrolled unit and validates registered invariants before each EF flush and EF transaction commit, synchronously and asynchronously. A rejection cannot be cleared by rollback, ChangeTracker.Clear, or catching the enrollment error. Healthy validation registrations span multiple explicit-transaction flushes and clear on terminal completion. The save interceptor runs after the public SavingChanges event, covering a late rejection before a single statement without an implicit transaction. Direct ADO.NET/raw SQL transactions bypass this EF contract.

Aggregate inventory moves reject prepared reservations before their global rollback snapshot can overlap them. Snapshot restoration checks before restoring item fields, and resize checks before relocating tail items. These are preflight guards; the future delivery owner still needs a whole-inventory logical mutation lease and Player/high-level economy integration.

`ItemManager.NextItemId` now serializes allocation and fails at exhaustion before wrapping to existing identifiers. Initialization uses checked arithmetic. The same two regressions executed against the frozen 7,182 binary **both fail behaviorally**: 32,000 concurrent calls yielded only 20,293 unique IDs in that bounded run, and exhaustion did not throw. Both pass against the current binary. This is a correctness observation, not a throughput benchmark.

## Verification

External evidence root: `I:\GIT\NexusForever-assessment-report\remaining-goal\inventory-reservation-foundation`.

- Full `Source\NexusForever.slnx` build: **zero warnings/errors**; `full-build.log`.
- Full current suite: **7,254/7,254**, zero skipped; `full.trx`, SHA256 `69CF835B94C52F5147480C0943D0735582148B0365203EC4A7AE9461B500265D`.
- **72 new cases**: Bag25, Item preparation23, Character validation14, allocator2, existing inventory creation suite +8 (27 total). Coverage includes 100 removal/acquisition races, 64 competing slot acquisitions, pre-mutation rejection, dirty-field preservation, fresh reconciliation, and rejection after an earlier SQL flush.
- **31 unchanged original compatibility probes** pass against the frozen new references; `original-compatibility/results/compatibility.trx`.
- **11 real MySQL save-validation cases** pass against the frozen new Character assembly in fresh retained schema `nexus_assessment_reward_validation_30a1`; `MySqlValidationProof/result.json`. Ten rejection cases leave zero committed rows; the healthy multi-flush case retains exactly three. The implicit-commit-specific case explicitly forces `AutoTransactionBehavior.Always`; this does not claim every single default-provider statement opens a transaction. The proof uses current-model EnsureCreated, **not migration or gameplay delivery**.
- `frozen-binaries.json` retains81 DLL hashes and `source-files.json` retains5,712 Source file hashes. `VERIFICATION.json` audits these inputs, test results and original checkout cleanliness.

The first expanded run retained three fixture failures caused by null codecs versus persisted empty defaults. Corrected fixtures retain full baseline comparison; no production validation was weakened. The separate allocator baseline retains its expected two failures and is not the current passing gate.

At this preparation checkpoint, World remained the previously verified **7,182** binary/PID31676; its26-cycle replay and9 populated migration/schema cases are historical scopes. This pass does not relabel them as 7,254 runtime evidence. Computer Use remains stopped; native Challenge/pet/Q5597 UI checks remain open. The user confirmed no backup of the June19 Q5597 bundle is available. Strict content coverage remains31 CSVs/167,484 rows, all not_retail_complete.

## Remaining implementation

Implement the Item quantity override without provisional live changes, preserving captured/later revisions. Enroll receipt, claimed state and all merge/new Item rows in one rejected-on-failure Character unit. Retain the same allocated IDs on verified rollback retry. Resolve false/savepoint/lost-ack outcomes through fresh receipt and full before/after row checks; preserve witnesses across cycles and reject newer consumption conflicts.

The aggregate owner must pair all Bag/Item capabilities atomically, snapshot static definitions/codecs, defer expiration elapsed time safely, coordinate normal/direct/cleanup saves before Auth work, and preflight trade/mail/auction/use before currency or removals. Keep capabilities through unresolved outcomes, adopt committed persistence state before releasing them, suppress stale-actor packets, and refresh collect objectives once. Only then expose a supported transactional inventory interface and wire the character reward repository, medal producer and claim route. The preparation-only capability must not be disposed as though it resolved a staged or ambiguous delivery. F-033 remains partial.
