# Reward-track and rotation wire ownership

2026-10-29. This routing correction supersedes the earlier #272 rejection based
on the labels GenericMapNodeChoose, reward-rotation content-context and auction
auxiliary. Fresh native owner evidence takes precedence over those labels.

## Exact client evidence

WildStar build 16042 executable SHA256:
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
REA open_binary returned the same digest. The MCP native provider lacked its
Ghidra environment; the existing pinned Ghidra 12.0.4/JDK 21 toolchain opened the
matching recovered project with `-readOnly -noanalysis`. The analysis logs report
discarding project changes. No live-client behavior is claimed from this pass.

| Opcode/API | Dispatch or registration chain | Meaning |
| --- | --- | --- |
| 0x07CF | ChooseRewardTrack table 140b72620 → 1406f3050 → 1404d1e40 | choose a reward track; not a generic-map request |
| 0x07CE | ClaimRewardPoint table 140c5e250 → 1406bb960 → 1404d1490 | claim reward-row ID plus choice |
| 0x07CD | slot 1403f30f4 → stub 1403f1115 → 1404d16d0 | add reward-track state |
| 0x07D3 | slot 1403f310c → stub 1403f1106 → 1404d1640 | clear/reload tracks; emits RewardTracksLoaded |
| 0x07D5 | slot 1403f3114 → stub 1403f112e → 1404d18e0 | progress/claimed/unlock/active; emits RewardTrackUpdated/Active |
| 0x07D0 | slot 1403f3100 → stub 1403f113d → 1404d1a40 | remove track; zero clears all |
| 0x07C7 | slot 1403f30dc → stub 1403f1289 → 140636ac0 | remove a separate rotation entry |
| 0x07C9 | slot 1403f30e4 → stub 1403f127a → 1406369c0 | update a rotation entry |
| 0x07CB | slot 1403f30ec → stub 1403f126b → 1406368d0 | upsert a rotation entry |

The high-range table at 1403f1344 uses DWORD RVAs indexed by `(opcode-0x61)*4`.
Fresh stubs print direct calls to the listed consumers. Rotation schedule/state
consumers at140636xxx are separate from reward-track global 140c65a38, its
RewardTrack static rows and its RewardTrackRewards row index.

Registrar 140071638 selects the shared one 14-bit writer 14007a610 for 0x07CF;
size stub 14007a600 adds 14 bits. Registrar 140071601 selects writer 14007ab80 for
0x07CE; size stub 14007ab70 adds 46 bits, corresponding to 14-bit row ID and 32-bit
choice. Those shared writers also have other packet users; their old labels do
not restrict their ownership to storefront or transport.

## Runtime correction

The message registry now registers ChooseTrack on 0x07CF. The former generic-map
request compatibility model has no message attribute; its handler cannot capture
that opcode. Existing GenericMapNodeChosen 0x0754 behavior is preserved.

Canonical reward-track add/reload/update/remove models own their server opcodes.
Former rotation content-context and auction auxiliary compatibility serializers
retain their public data APIs without wire registration. Rotation refresh retains
diagnostic context data but emits no 0x07CD/0x07D3 packets. Its valid schedule,
entry-state, claim-service and persisted account_reward_rotation_grant paths are
preserved. Upsert now uses 0x07CB and remove uses 0x07C7, matching the proven native
consumers. No stored rotation row was converted, reset or reinterpreted.

Track full rows and updates validate their 14-bit identity and 32-bit claimed mask.
Full rows retain reward-row order, limited by that 32-bit mask; reload validates
its row collection. Points, claimed bits, count, unlock state, reward-row IDs and
the final active bit follow the native reader order. The unlock field's broader
server policy is not guessed by this routing slice.

## Verification and remaining boundary

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo --filter 'FullyQualifiedName~Reward|FullyQualifiedName~GenericMap'
```

**397/397 pass**, zero warnings/errors, in the assessment report's
`remaining-goal/reward-routing-focused.log`. Tests exercise actual MessageManager
registration, the existing 0754 handler delegate, exact bit-packed reader layouts,
claimed bits 0/8/31, row boundaries, choice FFFFFFFF, malformed state, rotation
handler emission suppression and grant-service upsert opcode preservation.

The same report folder contains reproducible PE/TBL inventories and fresh native
logs: `reward-native-routing-fresh.log`, `reward-native-dispatch-fresh.log`,
`reward-native-registration-fresh.log`, `reward-native-register-windows.log`, and
`reward-07fd-label-readback.log`. The final readback confirms selected corrected
reward names and all five 07FD labels from the parallel spell slice. Applying the
entire historic label file also reports unrelated pre-existing missing/data labels
and an old duplicate; this is not a claim that all historical labels applied cleanly.

Selection is intentionally model-only until the next track-manager slice. This
change adds no progression/default track, migration, reward delivery, entitlement
grant or account/character integration. The next slice must establish persistence
scope, default activation/reset policy, and transactional claim delivery. Current
RewardPropertyManager premium modifiers and the conservative challenge reward
path remain separate. Source routing verification is not retail completeness.
