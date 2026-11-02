# Scientist scanbot name publication

2 November 2026 continuation. This connects the existing accepted Scientist
rename flow to the owned scanner's create model and visible name updates.
It does not qualify historical retail timing or native UI behavior. Computer Use
and native WildStar launch remain stopped.

## Evidence and scope

The [F-025 apply mapping](F025_STATIC_HANDLER_PROXY_MAPPING.md) binds build16042
to SHA256 `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`
with REA Evidence
`ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`.
Opcode08CC's table case1403edadd resolves payload+0 as the unit ID and passes
payload+8 wide text to14045b030, which copies name text and references
`UnitNameChanged`. The reader1400980f0 confirms the32-bit ID/wide-string layout.

Freshly inspected [upstream#514](https://github.com/NexusForever/NexusForever/pull/514),
open at head `c684d1fd572d876ea395b5aacdbfc76a86283e8c`, independently names08CC
`ServerUnitNameChange` with `UnitId` and `Name` and the same writer layout.
Its93 patch records were retained; null patches are not treated as absent files.
This corroborates the name-packet role, not every claim in that PR.

The current fork's public068C handler already requires a ScanBot type, an
unlocked profile and the existing text filter. `PetCustomisationManager.RenamePet`
already tracks that name for the existing Character save and sends the owner's012F customisation
update. Deployment selects the owned profile, but `ScannerUnitEntity` previously
always built `Name=string.Empty`; no accepted-name path updated the deployed
scanner. Two executed controls reproduce the empty create-model name before
admission and after a valid pending rename.

The selected012F native case1403f0e97 calls1403a8d60, which updates a profile
cache and emits `PetCustomizationUpdated`. The separate `Unit:GetName` binding
at140c5af30 ->140651bd0 resolves a unit and reads its name from entity+0x10,
the same storage updated by14045b030. This selected body does not call the
unit-name helper. It does not rule out Lua/event side effects; native presentation
has not been observed. Other viewers do not receive the owner's private profile
list. The scoped source producer therefore publishes the already accepted name
to the owned world entity, without adding new rename eligibility or policy.

## Integrated behavior

- Deployment applies the selected profile's stored name before map admission and
  create-model construction. A pending rename refreshes that model without
  emitting a zero-GUID update.
- An accepted rename of the tracked active profile updates the original owned
  scanner and broadcasts08CC to its visible owner and observers. Later viewers
  receive the same name in the scanner create model. Repeating the same name
  preserves the existing profile response and avoids a duplicate unit update.
- Profile mismatch, changed owner/identity, missing/replaced map entity and
  dismissal prevent a unit update. Unsummoning clears the tracked profile and
  actor; a later deployment reads the latest accepted name.
- `ServerUnitNameChange` supplies semantic fields over the existing serializer.
  The old raw08CC writer remains registered as a compatibility alias. Server
  registration is keyed by message type; no client reader/handler is added.

Persistence continues through the existing pet customisation/Character save
flow. There is no schema, financial settlement, text-filter, entitlement default
or grant change. Defaults15:1/17:1 remain; no44/seven baseline grants.

## Remaining unit-message requirements

This is a scoped08CC producer, not blanket#514 integration. The other five
F-025 aux producers remain unqualified. Upstream's093D writer uses a full32-bit
selector and two floats; the pinned reader instead consumes a5-bit selector and
two raw32-bit values, and its consumer stores a DWORD/float pair. That writer
must not replace the fork's existing native-shaped packet. Immunity selector
names, creature/UI flag bits, name/title table identity and tag-owner policy
still need their own evidence and integration review.

Native accepted/rejected UI behavior, historical retail packet cadence,
whole-service deployment and complete Scientist path/content qualification are
open. Source regressions cannot close those gates.

## Validation

Local records: `artifacts/continuation-20261102/f025-producer-corroboration/`.
Two actual name-loss failures are retained in `scanner-name-before-real.trx`.
The earlier missing-import/member and uninitialised-table fixture failures are
retained separately; they are not counted as behavior reproductions. The
corrected candidate passes14 new and256 affected tests. The full solution build
passes with zero warnings/errors in18.7seconds; all11,648 full tests pass with
zero failures/skips, including every prior11,634 label. The test command took
728.4seconds (the runner reports11m58s). Both Source review axes have0 findings.

Frozen6,204 Source/Tools inputs bind to
`6ae883831d3f06696b598eaa8aef51f2dd0015f8de9244ee892a43bb9d538ca2`;
test DLL SHA256 is
`49dfbc07be98d476bb624071335dfdf474c6bf9e4df9725e1964f111f87bfaea`.
All10 owner/test-copy DLL pairs match. Database source/schema are unchanged,
but rebuilding after the published commits changes assembly stamps/MVIDs and
DLL hashes. The generated informational version records base79568022. REA's
selected CharacterContext comparison matches175 method CIL/signatures and83
fields, reporting231 unchanged and27 unknown availability results; its literal/
type-identity and selected-type limits remain retained. This does not promote
historical MySQL executions to these rebuilt DLLs. No fresh MySQL/migration,
whole-service or native/retail qualification is claimed.
