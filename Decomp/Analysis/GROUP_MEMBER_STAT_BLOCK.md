# Group member stat block (`0x60` bytes)

Client copier: `Group_CopyMemberStatBlockFromPayload` @ `140607490`
Used by: `Group_HandleMemberAdd_ReadPayload` @ `1406031d0` (opcode `0x0436` / `ServerGroupRosterUpdate`), member remove, ready-check paths sharing the same stat layout.

NexusForever models: `ServerGroupMemberStatUpdate`, `ServerGroupRosterUpdate`, `ServerGroupMemberDetailUpdate` (prefix only).

**2026-05-23:** WorldServer `GroupMemberMappingExtensions` now maps `InterruptArmor`/`InterruptArmorMax` from group-server `GroupCharacter.InterruptArmour*` and `GroupMemberId` from `GroupIndex`. Raid-frame addons use client unit stats; this fixes NF-emitted `0x0466` gaps for IA bars when group-server stats are authoritative.

## Parsed stat-block offsets (`param_2` in `140607490`)

| Offset | Wire (NF) | Client runtime (`param_1`) | Notes |
|--------|-----------|----------------------------|-------|
| `+0x18` | `Level` (7-bit) | `+0x78` | Verified |
| `+0x19` | `EffectiveLevel` (7-bit) | `+0x7c` | Verified |
| `+0x1c` | `StatBlockPrefix17` (17-bit) | `+0x98` | Copied as `u32`; gameplay meaning blocked |
| `+0x20` | `GroupMemberId` (packed) | `+0x9c` | `FUN_1401c9770` packed read |
| `+0x22` | five `GroupMemberStatSlot` rows | `+0x84` (`memcpy` 0x14) | ushort + byte (`0x30`) per row on wire |
| `+0x36`..`+0x48` | packed floats | `+0xa0`..`+0xc4` | Health..ManaMax region |
| `+0x4a`, `+0x4c` | packed floats | `+0xf4`, `+0xf8` | Healing absorb pair (after path in struct layout) |
| `+0x50` | `PhaseFlags1` | `+0xec` | Verified |
| `+0x54` | `PhaseFlags2` | `+0xf0` | Verified |
| `+0x58` | `Path` (3-bit) | `+0x70` | Verified |

Full `GroupCharacter` roster wire (name, faction, zone, `Unknown10+` ushort block) is **not** part of this `0x60` copier; NF emitters only populate the stat-block subset today.

**Callers in fragment cache (2026-05-23):** only `Group_HandleMemberRemove_ReadPayload` @ `140603380` invokes `Group_CopyMemberStatBlockFromPayload` besides the copier itself (`140607490`). `1406031d0` does not call it (promote path).

## Re-open / blocked

- `StatBlockPrefix17` semantics
- `GroupMemberStatSlot.Value` meaning (ushort per row)
- `GroupCharacter.Unknown10`–`Unknown22` roster tail (separate handler past stat block)
