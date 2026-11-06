# Q3741 accepted supply-crate collection

6 November 2026. This is the first row of the generated content next-slice queue,
`quest-3741`, Scattered Supplies. The row remains `not_retail_complete` with
`retail_claim_allowed=false`; its native/client smoke gates remain open. No
generated queue or content-tracker artifact was edited.

Both existing creature12919 activation adapters now consume a crate only after
the original Player's Q3741 objective actually advances. A pending-delete refusal
retains the pickup for retry; a satisfied objective does not consume another
crate. Failed removal retains the original object and retries removal without
another credit. Reentrant activation is fenced, and a different loaded owner has
independent state. The existing VirtualCollect event, creature filters and
six-crate gameplay are unchanged.

The implementation reuses the established Q5597 accepted-credit pattern within
the owning script. Recovered Quest3741 has only objective4813: Type32
VirtualCollect, Data363, Flags4, Count6. Actual table data corroborates the
existing producer's identity; no native behavior was guessed or broadened.

Against unchanged parent `ce6881ff`, seven of eleven new real-Quest cases fail:
pending deletion(two), satisfied objective(two), failed removal(two) and a newly
loaded owner(one). Four controls pass. Three old positive test bodies previously
counted proxy ObjectiveUpdate calls; they now assert actual objective progress
and original-object removal through the real Player/Quest fixture. Their names
remain, but these are explicitly revised oracles rather than unchanged controls.

All11 new and103 selected cases pass with zero failures/skips. The scope includes
Q3741 reward/lifecycle/activation controls, nearby Northern Wilds interaction and
early-zone producers, plus the completed Q5597 collection controls. Normal owning
project dependency builds compile Script.Main and copy its DLL into the tests;
the owning and tested Script.Main hashes match. No full solution or suite ran.

All11 same case bodies also pass against recovered QuestInfo, objective and
Creature2 inputs. The explicit offline runner verifies all21 loaded table hashes
against the existing1,211-file asset inventory, then uses the owning GameTable
reader. This is the retained prepared cache, separately identified from the older
385-payload recovery representation. Six accepted pickups achieve the quest;
rejected Player SQL flush leaves stored progress unchanged, and retry plus a
fresh QuestManager reload retains six credits/Achieved. A further unused crate
is preserved after reload. Actor/map/session preparation is explicit; this is
not public quest acceptance or physical activation.

Primary-agent Spec review checks refused progress, duplicate/reentrant admission,
original removal identity, fresh owner and save/reload boundaries. Standards
review checks the local script boundary, existing Q5597 pattern, typed table
provenance and test assertions. Both have no remaining actionable finding; no
independent reviewer is claimed.

[Portable bindings](coverage/q3741-accepted-supply-collection-20261106.json)
retain source/DLL/inputs/TRX identities. Raw logs, SQLite fixtures and the small
reflection runner are ignored in `artifacts/q3741-collection/`. The reusable
`Q3741SupplyCrateCollectionTests.RunActualCatalogAsync(assetRoot, newOutputDirectory)`
entrypoint is tracked; client tables are not.

Public/NPC acceptance and turn-in, reward presentation, physical map/activation,
respawn, configured providers, sockets, native UI and retail qualification remain
open. No service, broker, native client or authoring database was started for
this slice. The earlier hosted character-list milestone retains its own frozen
Script DLL identity; it is not qualification of this later script change. The
broader Goal continues; native-client launch and Computer Use remain stopped.
