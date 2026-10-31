# Mini pet-bar dismiss (build16042)

The bounded producer exposes ActionBarShortcutSet499 for already-supported Engineer combat bots. The actual row has slot0 type11/object2 and eleven empty slots. It is an explicit server policy selecting a verified compatible dismiss surface; it is not proof that every retail Engineer bot used that row.

Client SHA256: `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`. Registration14006c290 and writer14007ec70 give opcode0x068A two full-width u32 fields. Sender1405c9b90 sends the shortcut set and slot index. Command constructor1405c7de0 stores the table object separately; wire slot0 therefore selects row499's command object2. Lua GetPetDismissCommand1406f8230 searches mini sets2..6 for the associated unit and type11/object2 and returns `set*12+slot`. GetPlayerPets1406f80e0 resolves the concrete cached entities. PetSpawned1403c08d0 and PetDespawned1403c09b0 update that cache without managing mini bars.

The extracted ActionBarShortcutSet.tbl has541 rows and SHA256 `29299f458d1f78c8ed695e61a98c901633b9e181494300991c7c93ebc240b232`. Actual row499, row299 and native jump-table/Lua references were independently corroborated in `I:/GIT/NexusForever-assessment-report/remaining-goal/mini-petbar-068a/`. FastTargetFrame and PotatoUI call GetPetDismissCommand for the concrete unit from their PetSpawned handling, so the mini binding precedes that event.

The server allocates stable first-free sets2..6 and binds the exact pet object, GUID, map and owner admission generation. It requires current map-registry identity, tracked summon ownership, the existing Engineer creature families and the exact row499 shape. Only slot0 is accepted. A successful click marks the binding closing, clears its bar, and calls the existing SummonFactory.Unsummon once. The closing index remains reserved until the exact entity callback. Despawn clears before PetDespawned; owner removal drops all bindings, and cancellation drops them without outbound packets. Ordinary primary299/all-bots0, command projection, summon caps and unrelated pet lifecycles retain their existing producers.

The packet carries no GUID, nonce or generation. After deliberate index reuse a valid delayed click denotes the current authoritative binding. Internal generation/reference checks reject stale server state but cannot recover an absent client intent nonce.

Dismiss followed by summon-effect cancellation can queue the same physical removal twice before a map tick. BaseMap executes a queued remove only while its registry and the entity's map identify the exact object. Real MapGrid regressions cover duplicate removal/callback/GUID recycling, stale maps, reused GUIDs and the actual summon-effect lifetime cleanup after dismiss. The existing movement queue fixture now registers its current entities so its remove assertions exercise this real registry contract.

The independent audit identified two reentrant continuation gaps. Clear now captures the admission at entry and skips an old index if a replacement has already rebound it; a callback advancing admission also stops the remaining old clears. Engineer despawn clears obsolete server selectors before its first outbound callback, so cancellation cannot strand a departing selector and a replacement registering during publication retains its new mapping. Later hide decisions use fresh active-bot counts. The original generation follows the family-command and action-set restoration tail, with checks before each packet and after building the restored set. Concrete SpellManager/actual selector rows and non-null action-set fixtures cover those boundaries. The physical remove guard deduplicates removal execution, not the pre-existing per-request OnEnqueueRemoveFromMap notification.

Restoration also captures the exact tracked Engineer object references at callback entry. If any newly introduced Engineer source appears during publication, including a different family, the old callback suppresses its remaining base-action-set restoration. This preserves a replacement's newly published projection without changing restoration policy for other-family bots already present at entry. Different-family regressions run the existing Bruiser registration/projection producer with summon42894, selector35501, action52419 and a real LAS shortcut27082 during both mini unbind and cache-clear callbacks. This is a bounded new-source guard, not a general multi-family LAS merge or a redesign of pending summons.

The78 dedicated cases in UpstreamMiniPetBarPacketTests, PetMiniBarManagerTests,
ClientPetMiniCommandHandlerTests, EngineerMiniPetBarLifecycleTests and
MapRemovalLifecycleTests pass in the full6,923-test suite with zero build
warnings/errors. Independent review found no remaining material defect in the
approved scope. Eight relevant pet/Challenge labels were applied and appear in
the fresh `pet-property-labeled-export/WildStar64.exe/functions.csv` export.
The frozen6,923 login/load replay passes23 fully audited cycles; native pet UI
remains unverified. Attack/stop mini layouts, mini spell slots, deprecated
layouts and unrelated pet kinds remain outside this slice.
