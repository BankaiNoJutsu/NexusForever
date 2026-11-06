# Reward Rotation refresh authority

6 November 2026. Public07CC refresh requests now read one index and publish
schedule/existing grant readback. Unmapped trailing bytes cannot create grant
rows or grant upserts. This fixes a concrete prerequisite to supported delivery;
it does not enable rewards from an unqualified content/catalog association.
Base:fbca92687028be541d8127cddc7297a08c2922d9.
The [portable record](coverage/reward-rotation-refresh-20261106.json) binds Source,
native spans, actual table inputs, baseline/final tests, binaries and review.

## Native request contract

Fresh bytes match build16042 SHA256
231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5.
The existing exact-hash REA PE-import identity is reused. Standalone
Ghidra12.0.4/JDK21 opens the retained analyzed project read-only with analysis
disabled. Installed LLVM22.1.6 reads selected raw code. This is static evidence;
no client launch, UI observation or REA native-provider qualification occurs.

| Evidence | Observed boundary |
| --- | --- |
| Sender140636ba0 | Initializes one int32 index below7 and calls the07CC sender. |
| Registry14006c290, span140078439..14007846e | Registers opcode07CC, payload size4, size callback14007d000 and writer14007d010. |
| Size14007d000 | Adds32bits. |
| Writer14007d010 | Loads one payload DWORD, writes it, advances32bits; slow path also receives32. |

Every selected Ghidra span matches the original PE bytes. The two tiny writer
entries lack defined functions in that project; the retained Ghidra script
failure is not a successful decompilation even though the launcher returns0.
LLVM supplies the separate size/writer instruction evidence. The additional
Lua getter span is retained without deriving server selection or earning policy.

## Reproduced defect and correction

The former decoder accepted a trailing content ID and reward lane as an
invented optional claim. The refresh handler matched those values to a schedule
row, marked it granted and emitted07CB without delivering an award.
All three lane regressions persist a grant through real Auth/SQLite save while
inventory and currency rows remain empty. Another regression observes an
upsert for an existing grant; the decoder also consumes the five extra bytes.

The decoder now reads only the mapped index. Extra bytes remain neutral under
the existing frame-reader contract. The handler sends its provider's schedule
and existing entry-state snapshot without invoking claim-record operations.
Existing persisted grant readback remains, including a request with extra bytes.
No existing rows are deleted or reinterpreted. Explicit internal grant service
APIs remain available for a future qualified completion/delivery path.

The runtime evidence report now states that07CC refresh does not authorize
a grant. It removes the superseded claim that07CD lacks a Rotation apply owner;
07CD/07D3 are already mapped to reward tracks. The single reviewed provenance
row records Verified refresh wire shape, with delivery gates still explicit.

## Actual catalog boundary

Eight recovered tables are hash-checked and loaded through GameTableManager.
There are35 content rows,6 item rows,20 essence rows and16 modifier rows, plus
the existing World/WorldZone/PublicEvent/MatchType content-context joins.
All six item rows have raw type3/object18, counts1000..6000, minimum level50 and
difficulty masks1or2. This record does not assign those raw numbers a new
currency or delivery meaning.

These schemas contain no direct content-to-reward association. Current
content-ID modulo selection and auction-formula duration are emulator fallbacks.
A qualified server quote still needs content/difficulty association, completion
owner, schedule/reset/throttle policy and durable delivery/compensation/recovery.
Another invented client claim payload cannot supply those authorities.

## Validation

The qualified baseline has5 failures and3 controls. Final8 new/all56 selected
Rotation/provider/catalog/protocol/evidence cases pass with no failures/skips.
Two historical tests encoding the invented wire claim are superseded by these
parsed Auth/SQLite negative and readback cases; the internal claim-service
tests remain. Owning/test builds are clean in21.53/13.80seconds; the scoped
runner reports2seconds. The final catalog probe build takes2.59seconds.
No full solution or full suite ran.

Six of138 DLLs differ from the baseline. Network.World/Game/World owning
outputs match their tested copies; the final actual-table probe uses the tested
Game DLL. All six owned Source files match root/candidate. The isolated
candidate retains only the eight previously recorded project/filter or BOM/
newline differences outside the owned scope. Initial test-constant/catalog API
setup errors are retained separately. Local evidence is under
artifacts/continuation-20261106/reward-rotation-refresh/.

## Standards

Zero actionable Source-standard or heuristic findings. All six pinned
identities match. Schedule/readback and direct internal claim APIs remain.
The canonical note required by the provenance row is this file.
Review was inspection only.

## Spec

Zero remaining findings. The index-only decoder and mutation-free public
refresh match the mapped payload and preserve existing readback. Actual
catalog/native records retain the unresolved delivery boundaries.
Review was inspection only.

The latest20-PR refresh is unchanged, including#608 open and#609 closed/unmerged.
Configured providers, copied upgrades, physical restart, hosted World/Auth/
socket, native UI and retail qualification remain open. Client/CUA remain
stopped. Defaults15:1/17:1 remain; no44/seven baseline grants.
Continue independent actionable progression/gameplay; the overall Goal is active.
