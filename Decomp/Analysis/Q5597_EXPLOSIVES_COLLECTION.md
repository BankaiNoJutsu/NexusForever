# Q5597 accepted explosive collection

The two existing Chua Explosives activation adapters now consume a pickup only
when the original real Player's Q5597 objective actually advances. A refused
pending-delete attempt remains available for retry. An already satisfied
objective does not spend another explosive. Accepted credit is retained through
failed original-object removal; later activation retries removal without another
credit. Reentrant activation is fenced, and a newly loaded owner has independent
pickup state. Existing virtual event routing and creature24286 filters remain.

Recovered Quest5597 has one objective8256: Type32 VirtualCollect, Data364, Flags4,
Count6. Six distinct accepted explosives achieve the real quest. This closes the
collection producer boundary; the separately completed
[reward turn-in qualification](coverage/q5597-actual-catalog-20261106.json)
remains valid and is not counted again as new work.

## Verification

Against published parent fba67cb84c6d834e9d8213d350b9e604f0f91c04, the original
script fails7 of11 new cases: refused pending deletion(2), satisfied objective(2),
failed removal retry(2) and a newly loaded owner(1). Four controls pass. All11 new
and82 scoped cases now pass with no skips. Four legacy positive bodies now assert
real objective progress and removal instead of counting proxy update calls.
The scope covers both adapters, Crimson Isle interactions/chain, Quest persistence
and existing Q5597 fixed-item turn-in/reload controls.

The same11 case bodies pass with recovered QuestInfo and objective metadata after
verifying SHA256 and extents for all385 recovered payloads. Six credits/Achieved
survive a full real Player SQLite save refusal, retry and fresh Quest reload;
an unused explosive does not disappear when the reloaded quest is achieved.
No authoring database, provider engine, native client or Computer Use was launched.

Owning Script.Main build3.79s, test build16.83s and actual adapter build4.48s are
clean. The scoped run took18s. Both final Source review axes report0 actionable
findings and matching pins. The initial missing test namespace import is retained
as a compile setup failure, corrected before the executed baseline.

The reused isolated candidate starts from6313 published Source files with eight
recorded historical project/filter/BOM variants. Only the two owned tracked files
and one new test source differ for this milestone. All138 managed dependencies
are bound; only Script.Main and Game.Tests DLLs change. Game is unchanged from the
published baseline; the owning Script output matches the tested copy. The actual
adapter uses exact managed/native SQLite copies. Concurrent root Source is excluded.
Twenty upstream PR heads/bases/states remain unchanged, including608 open and609
closed/unmerged. No upstream mutation was made.

The [portable evidence record](coverage/q5597-explosives-collection-20261106.json)
pins Source, DLLs, baseline/scoped results, actual inputs and review outcomes.
Raw runs are retained under
`artifacts/continuation-20261106/q5597-explosives-collection/`.

## Remaining scope

Acceptance and spatial/session inputs are prepared; tests manually call script
adapters. This is not public NPC acceptance/turn-in, a parsed activation handler,
physical map/spawn/visibility/respawn or native/retail qualification. General Quest
callback partial-state repair and durable world pickup recovery remain separate,
as do configured providers, physical producer loss/restart, sockets and deployment.
No sharing, respawn, award or entitlement policy was invented. The broader Goal
remains active; continue independent actionable gameplay/progression while its
external evidence gates stay explicit.
