# Q4526 sequential anomaly catch retry

6 November2026. An early catch in Spatial Anomaly no longer permanently blocks
the same anomaly's later valid credit. The old script cached the character
before the real sequential objective accepted progress. Returning after the
wreckage investigation then skipped the catch forever for that script instance.

Recovered Quest2 4526 orders6164 (ActivateEntity,Data0,count1,flags0) before6165
(ActivateEntity,Data0,count1,flags3:RequiresPrevious+Sequential). The script now
finds the original Player's active quest,current catch objective and incomplete
state before applying credit. The real objective owns duplicate protection;
there is no attempted-credit cache to retain a refused early catch.

The12535 filter,5m range and existing WIP movement/catch interpretation remain.
No native movement or catch timing policy was inferred from the table rows.

## Reproduction and verification

The baseline has three failures and three passing controls. Both early range
and explicit activation poison the later valid catch; the save/reload scenario
also stops at Accepted rather than Achieved before the fix.

All six new/all118 scoped cases pass. Three existing proxy-call positives now
assert real Quest progress and Achieved state. Controls cover valid entry points,
duplicate callbacks,abandonment and full real Player SQLite save failure/retry/
fresh Quest reload.

The same six case bodies also pass with actual recovered QuestInfo. The offline
adapter verifies385 payload identities/extents and loads21 typed tables. Its138
managed dependencies and native SQLite DLL match the scoped candidate. Both
final review axes have zero findings.

Owning Script/test-only builds are clean in7.35/29.81s; the adapter build is clean
in5.42s. The scoped runner reports17s. No full solution or full suite ran.

A separate exact-published archive candidate excludes concurrent loot/other
Source and shared build outputs. Published Game is rebuilt clean in10.45s.
The final comparison covers6,312 parent Source files,two owned tracked changes,
one new test and eight historical project/format variants. Absolute-path
clean-filtered Git hashes handle the archive's CRLF export. All138 DLLs are
bound; only Script.Main/Game.Tests change from this candidate's baseline,and
owning Game/Script match test copies.

Initial fixture namespace and missing executable-copy inputs are retained
setup failures. An initial relative-path hash audit read the ancestor root;
its failed result is superseded by the absolute-path comparison. These are
separate from the three executed gameplay failures.

The [portable record](coverage/q4526-sequential-catch-20261106.json) pins Source,
rows,baseline/final DLLs,TRX,actual inputs and reviews. Raw evidence remains under
artifacts/continuation-20261106/northern-wilds-rescue/.
The latest20 reviewed PR heads/bases/states remain unchanged;608 is open and609
closed/unmerged.

## Remaining scope

The quest is seeded Accepted and the wreckage objective is prepared through
the real Quest routine. Public NPC acceptance,full map/movement/catch gesture,
turnin/rewards,provider/socket/deployment and native/retail remain open. General
callback/publication failures are not qualified by this admission correction.

No schema,migration,setup,entitlement or provider/native-client/CUA workload
changed. Concurrent loot feedback work is preserved outside this slice. Hosted
World/Auth still needs its runtime assets; Primal allocation still lacks essence
debit/synchronization authority. The broader Goal remains active and incomplete.
