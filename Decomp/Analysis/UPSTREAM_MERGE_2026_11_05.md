# game_rework integration, 5 November 2026

This merge brings upstream `NexusForever/NexusForever:game_rework` at
`d8e43d2c3cc5a05f03e56dfc388288a4126c7ed9` into
`legacy-di-storefront-hardening`, starting from
`8ab6ce34`. The merge base was `a53c2129`; upstream had 22 commits absent from
the branch's ancestry, changing 423 paths. The ordinary three-way merge
reported 110 conflicting paths, including differently renamed packet models.

Much of the incoming behavior was already reconciled into this branch in
October. Those implementations are kept together with their existing tests,
database layouts, native-evidence contracts and dependency-injection ownership.
The merge records upstream ancestry without replacing those implementations
with incompatible parallel systems. There is no rebase or history rewrite.

## Resolution decisions

| Upstream commits | Resolution |
| --- | --- |
| `e8465e89`, `a3e4a7b4`, `0b8aa1da`, merge `1f643f63` | Retain the already reconciled global loot manager, recipient eligibility, group distribution, map-bound loot, item-container/salvage paths and populated-fork loot schema. Do not add another loot manager, duplicate `LootTables` migration class, or rename live `minDrop`/`maxDrop` columns. See [loot reconciliation](LOOT_GROUP_UPSTREAM_RECONCILIATION.md). |
| `474b429e`, `4422db7c`, `99ff632e` | Retain immutable group snapshots, explicit primary associations, primary promotion and world-thread mutation/publication. Add the compatible Action synchronization API and function-task name, keeping the old task type and default interface implementations for existing consumers. Initialize tasks before publishing them to the concurrent queue. |
| `235ece6c`, merge `845651e6`, `56b81608` | Keep the already corrected archive widths, role widths, resurrection diagnostic bits, storefront float-bit preservation and other validated packet contracts. Do not replace named crafting fields or pet stance conversion with divergent upstream interpretations. See [protocol reconciliation](PROTOCOL_NETWORK_UPSTREAM_FIXES.md). |
| `17ef256c`, merge `d0817fdf` | Keep the existing `IReadable` random-party decoder and its registered matching flow. |
| `90b6e2e7` | The fork already closes sockets in `finally`, including sessions without an accepted socket. Keep the stronger existing lifecycle handling and tests. |
| `f9bb26ef` | Keep the public Blocking event API and its corrected deferred-event requeue. Removing it would break the fork's existing consumers; this choice is recorded in [protocol reconciliation](PROTOCOL_NETWORK_UPSTREAM_FIXES.md). |
| `3bdb8b23` | Keep the fork's housing/guild identities, model types, packet names, recoverable save graph and active migration chain. Import the previously absent `ClientHousingPlaceDecor` model and its operation enum without adding a gameplay handler. See [community lifecycle](COMMUNITY_RESIDENCE_UPSTREAM_FIXES.md) and [current creation settlement](COMMUNITY_TOKEN_CREATION.md). |
| `dade7f0d`, `b234b443` | Keep the existing account/spell registrations and APIs rather than introducing second models for the same numeric opcodes. Import the seven additional models listed below. Account currency naming, storefront settlement, cast-context fields and evidence-backed spell models remain coherent with their existing consumers. |
| `fa5d5e55` | Adopt stream-based TextTable loading through the same parser used for files. The constructor owns/closes the supplied stream, matching upstream. |
| `121f4c3e`, merge `2aaafba9` | Adopt the optional web-console character field. Resolve the named online character when the queued command executes, using the injected player manager. Preserve WebSocket role permissions, explicit invoker/target contexts and safe typed target lookup. |
| `ad8bae57` | Upgrade the Aspire SDK and all three centrally pinned Aspire hosting packages together from 13.4.6 to upstream's 13.6.0. Keep the fork's other current dependency versions and launch defaults. |
| `d8e43d2c` | Keep the fork's existing idempotent `DatacubeManager.AddDatacube`, which merges existing progress and resends it instead of throwing. Preserve the activation's achievement, archive and Scientist hooks. |

Clean identity-mapping helper renames and the player-count API are also
integrated. Unrelated gameplay, data promotion and local runtime configuration
are outside this merge.

## Additional packet models

The pre-merge comparison used numeric opcodes, because many upstream and fork
classes have different names. These are upstream models newly present in this
merge; serializer/decoder checks establish their stated upstream wire shapes,
not native-client or retail qualification. No new handler or runtime producer
is attached to them.

| Opcode | Model |
| --- | --- |
| `04E6` | `ClientHousingPlaceDecor` |
| `077D` | `ServerMissileActorRemove` |
| `0784` | `ServerSpellVisualEffectRemove` |
| `0785` | `ServerSpellVisualEffectRemoveFromUnit` |
| `079E` | `ClientPremiumLockboxKeyRequest` |
| `080E` | `ServerSpellDisableCooldowns` |
| `0832` | `ServerAnnounceCommandOnCooldown` |
| `096F` | `ServerOmnibitsWeeklyBonus` |

The comparison also found the pre-existing upstream recruitment typo `776E`;
the fork keeps its corrected `076E` registration. That file was outside the
22-commit delta and is not changed here.

## Verification

Validation uses the installed user-profile .NET SDK 10.0.401 explicitly. The
system `dotnet` executable has runtimes but cannot find an SDK. Builds run with
one MSBuild worker and shared compilation disabled after an unrestricted initial
build exhausted worker resources. No SDK installation or global setting change
was needed.

The unchanged branch and candidate use separate worktrees. The original
checkout's independent ActionSet/experience publication work is excluded from
both qualification trees and is preserved in that checkout.

New focused coverage exercises registration through the production message
registry, packet field alignment, the public WebSocket JSON receive/queue flow,
deferred character resolution and permissions, TextTable stream/file equivalence
with supplementary Unicode, and action/function synchronization compatibility.

Final build/test results are recorded here after qualification.

The unchanged full-suite baseline exposed test-harness defects separately from
the merge: the presence-upgrade fixture tried to insert today's Account model
into a historical schema, and the crafting-outbox/group-job fixtures threw EF's
global twenty-provider warning when run after other isolated-provider tests.
The upgrade fixture now seeds historical columns directly and checks the new
inventory-id default after migration. The two intentionally isolated-provider
fixtures log that diagnostic rather than throwing it. Production EF options,
migrations, persistence behavior and the test assertions are preserved.

This merge does not apply database migrations, launch services, run a graphical
client, or establish deployed-provider, socket gameplay or retail parity.
