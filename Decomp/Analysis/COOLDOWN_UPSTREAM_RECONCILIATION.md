# Cooldown effect73 reconciliation

2026-10-29; reviewed upstream #596 head `e100f4c29a1571a7181d06967ed8133612d99074` against its actual base `3588ca5f578387928e6593e3808bf15f6192b094`. Expedition ancestry is not a prerequisite for this fork implementation.

## Exact native evidence

WildStar64.exe build16042 SHA256 `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`. Existing fragments below are under `exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_<hash>/` relative to this directory.

- `140718120`, factory case `0x49`, allocates the effect instance with vtable `140b6e0a0` and calls `1405d9850`.
- `1405d9850` copies effect row offsets `0x40/0x44/0x48/0x4c` into a four-word record: scope, key, operation and float magnitude. A fresh recovered-project `DumpNearbyData.java` pass confirms vtable slot `+0x10` calls `1405d9930`, which resolves the target entity and attaches through `14046b7f0`.
- `1405d6880` computes lifetime from delay+duration (or max under a spell flag), with effect CancelOnly bit2 overriding timed expiry. `14046b7f0` retains records with lifetime and always applies them once to running nodes. `1405d97c0` removes the exact record pointer; removal does not restore modified timers.
- `14046a890` matches scope0 base spell,1 concrete spell,2 spell group,3 cooldown ID. Operation0 multiplies the original input and caps at that input;1 multiplies the accumulator;2 adds truncated milliseconds and floors;3 sets milliseconds. Insertion order matters. `14046b920` applies a newly attached record to running matched nodes only.
- `14046b1e0` starts a type1 direct node keyed by concrete spell ID and separate type2 shared nodes keyed by SpellCoolDown IDs. Shared table durations may differ from the direct spell duration. `14046afc0` starts type0 GCD nodes. `140095660` reads the wire row as 3-bit type,18-bit spell ID,32-bit key,32-bit time.
- `14046a810`, `14046b1e0` and class-tag helper `1404823c0` establish property136 cooldown scaling for tags1/2/7, with whole-millisecond truncation. The native path can produce zero when the multiplier is zero; this fork does not invent a nonpositive-to-one override or alter property defaults.
- `14046a3d0` folds held records only when creating a new nonzero-spell node. Therefore newly created direct/shared nodes are sent with the duration before the held-record fold; the server stores the folded duration. Existing-node re-arms and effect delivery do not fold/emit a second time.

Fresh evidence summaries are external: `remaining-goal/cooldown-native-vtable-summary.json`, `cooldown-native-callers-summary.json`, `cooldown-native-consumer-table-summary.json`. Exact logs are under `Decomp/Analysis/logs/runs/cooldown_native_*`. These used the existing recovered exact-hash Ghidra project with project locking and no label writes/cold import.

## Fork implementation

`SpellManager.CooldownModifiers.cs` owns concrete effect records and ordered arithmetic. Existing cooldown setters/getters remain public with their original signatures. Starting cooldowns now keeps direct and shared identities separate, uses nonzero shared row durations with the existing fallback for empty/missing rows, and keeps raw wire time separate from held-modified server time. Reset clears both direct and shared nodes while leaving global cooldown state alone.

`SpellHandler.HandleEffectModifySpellCooldown` now treats the original words as independent identity spaces and operations. The former magnitude/sign guessing and invented words4/5 arithmetic are removed. Existing interpretation field names and all raw diagnostic fields remain available; their scope/key meanings are documented explicitly. Invalid/unknown records fail closed, finite nonnegative timers are bounded, and idle cooldowns are never created by an effect re-arm.

Held records use their concrete effect ID plus source spell/casting metadata. Normal spell expiry and cancellation remove that ID; UnitEntity tracked-state removal includes cooldown modifiers, preserving dispel/force-remove count limits, unrelated records and existing removal messages/combat diagnostics. Recasts have separate concrete owners. Removing a modifier affects future cooldown starts only and does not undo a running timer.

## Verification and boundaries

Focused native cooldown/node/GCD, combat, AMP/passive and asset tests passed **187/187**, zero warnings/errors. Exact command/log/TRX are recorded in external `remaining-goal/COMBAT_NETWORK_PR_REVIEW.md` and `cooldown-tests/`.

Regression cases include independent raw 16-byte records; all four scopes and operations; low IDs; shared-node deduplication; mode0 original-input ordering; timer flooring/truncation; duplicate/recast ownership; ignored diagnostic tail words; direct/shared numeric-key collisions and wire decoding; distinct shared table duration; property136 tag/default/zero behavior; expiry, CancelOnly and cancellation; existing dispel/force-remove handlers and source-count limits.

Global cooldown packet fields retain the fork's existing API/shape. A candidate cached function `1403d9950` rejects zero-spell/zero-time rows, but the fresh reference pass found only unwind-data references and did not establish its network opcode linkage; that body is insufficient to change the wire contract. The adjacent apparent table addresses `140bcc654/664` are PE unwind metadata, not a callback table. Initial cooldown-list ordering versus retained client modifiers still warrants an exact receive-dispatch/runtime witness before claiming complete network parity. No whole expedition spell engine was imported.
