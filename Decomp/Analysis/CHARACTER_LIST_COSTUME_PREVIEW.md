# Character-list costume preview without a live Player

6 November 2026. Follow-through from the
[ICComm hosted qualification](ICCOMM_SOCKET_RECONNECT_QUALIFICATION.md):
CharacterListManager constructed CostumeManager with a null Player. The live
wardrobe ownership guard dereferenced that Player and the catch path omitted the
entire character from ServerCharacterList. The protocol fixture could still
select its owned SQL ID and reach world entry, so that milestone did not prove
that a native client could select a visible character row.

Pregame mapping now selects the active loaded CharacterCostumeModel and constructs
its presentation Costume directly. Inactive or missing selections keep the
ordinary inventory preview and default visibility mask. Existing appearance,
bones, level and gear mapping remain in the same path. Live CostumeManager owner
checks, persistence, save authority and publication are unchanged.

The three final regression cases cover inactive, active and missing selections
with no session Player. They require an actual row in the queued list packet,
correct visibility, appearance, bones and stat-derived level, unchanged loaded
selection and no logged mapping exception. Replaying these same final cases with
the frozen pre-fix World DLL produces three failures at CostumeManager line 61.
The fixed candidate passes all 93 scoped character-list, wardrobe manager,
wardrobe durability and ICComm cases. Incremental WorldServer builds have zero
warnings/errors; Game.Tests retains only existing ASPIRE010. No full solution or
full test suite ran.

Initial draft fixture failures are retained separately: inventory capacities were
uninitialized, and the active costume lacked its normal seven slot rows. Those
draft failures do not prove the production issue. The corrected baseline above
uses initialized capacities and all seven slots and observes the actual null-
Player stack. A namespace/type compile error was also repaired before the passing
build; no failed attempt was overwritten.

The [portable record](coverage/character-list-costume-preview-20261106.json)
binds exact changed source, baseline/final binaries, TRX and compiler checksums.
Spec and Standards primary-agent self-reviews found no remaining finding in this
bounded fix; no independent reviewer is claimed. No service, broker, database or
native client was started for this follow-through. The prior cleanup remains
intact, MySQL80 remains running, and Q3777 remains untouched in its other worktree.

Raw ignored evidence is at
`I:/GIT/NexusForever-iccomm-20261106/artifacts/character-list-costume-20261106/`;
transfer it separately. This is Source/packet-object regression qualification.
The changed World DLL has not inherited the earlier frozen candidate's hosted
qualification. Next, on another newly owned copied runtime, read the actual
ServerCharacterList wire rows and require the selected owned Character to be
present before sending CharacterSelect, then rerun bounded ICComm cycles.
Native character selection, addon callbacks, automatic reconnect, positive Group
and retail parity remain open. Computer Use and native launch remain stopped.
