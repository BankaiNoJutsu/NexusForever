# Spell Broadcast Roadmap

This document tracks the still-opaque spell broadcast family around opcodes `0x07F5` through `0x0818`.
The goal is not to guess packet meaning from field names. The goal is to preserve the current structural models,
record where each message fits in the spell lifecycle, and define the evidence required before any runtime send path is enabled.

## Current Lifecycle Surface

The server already uses the proven spell lifecycle packets:

| Opcode | File | Current role |
| --- | --- | --- |
| `0x07F4` | `ServerSpellGo` | Main spell-go broadcast with target and effect results. |
| `0x07F9` | `Server07F9` | Live cancel-cast follow-up sent from `Spell.CancelCast()`. |
| `0x07FC` | `ServerSpellCastResult` | Validation failure result to the caster. |
| `0x07FE` | `ServerSpellFinish` | Spell completion signal. |
| `0x07FF` | `ServerSpellStart` | Spell-start broadcast before delayed execution. |

Everything else in the `0x07F5..0x0818` range should currently be treated as a tracked but unimplemented follow-up surface.

## Opaque Follow-Up Cluster

| Opcode | File | Current hypothesis | Evidence needed before implementation |
| --- | --- | --- | --- |
| `0x07F5` | `Source/NexusForever.Network.World/Message/Model/Server07F5.cs` | Follow-up after `ServerSpellGo` or `ServerSpellStart`. | Client parse path plus one sniffed example. |
| `0x07F6` | `Source/NexusForever.Network.World/Message/Model/Server07F6.cs` | Follow-up stage in the same broadcast family. | Client parse path plus one sniffed example. |
| `0x07F7` | `Source/NexusForever.Network.World/Message/Model/Server07F7.cs` | Follow-up stage in the same broadcast family. | Client parse path plus one sniffed example. |
| `0x07F8` | `Source/NexusForever.Network.World/Message/Model/Server07F8.cs` | Nested per-effect or per-target follow-up. | Sniffed field counts, client parse loop, and one correlated cast fixture. |
| `0x07FA` | `Source/NexusForever.Network.World/Message/Model/Server07FA.cs` | Spell-family follow-up with unknown role. | Client parse path plus one sniffed example. |
| `0x07FB` | `Source/NexusForever.Network.World/Message/Model/Server07FB.cs` | Likely miss, immunity, or invalid-target report. | Sniffed miss or immune cast plus target-result correlation. |
| `0x07FD` | `Source/NexusForever.Network.World/Message/Model/Server07FD.cs` | Position or telegraph replay follow-up. | Sniffed telegraph case plus client position-parse evidence. |
| `0x0811` | `Source/NexusForever.Network.World/Message/Model/Server0811.cs` | Partial target list or subset replay of `0x07FF`. | Client parse path plus target-count correlation. |
| `0x0814` | `Source/NexusForever.Network.World/Message/Model/Server0814.cs` | Spell effect trigger or follow-up boolean gate. | Sniffed trigger case plus client parse evidence. |
| `0x0816` | `Source/NexusForever.Network.World/Message/Model/Server0816.cs` | Root or parent spell hierarchy follow-up. | Chained or proxy cast sniff plus client parse path. |
| `0x0817` | `Source/NexusForever.Network.World/Message/Model/Server0817.cs` | Opaque spell event byte tied to one `Spell4Id`. | Repeated sniff witnesses with stable byte semantics. |
| `0x0818` | `Source/NexusForever.Network.World/Message/Model/Server0818.cs` | Target-info follow-up seen near NPC telegraph buff cases. | Sniffed telegraph buff case plus client parse path. |

## Safe Near-Term Work

- Keep field widths and list counts accurate in the placeholder models.
- Improve comments and XML documentation when new structural evidence lands.
- Record every new hypothesis in terms of lifecycle position, not guessed retail names.
- Prefer test-only or diagnostic-only send experiments over production runtime behavior.
- Update this document when one opcode advances from Observed to Correlated, Mapped, or Verified.

## Verification Loop

1. Start from one narrow spell fixture and record the expected lifecycle using `ServerSpellStart`, `ServerSpellGo`, `ServerSpellCastResult`, and `ServerSpellFinish`.
2. Compare the server placeholder model with `selected_decompiled.c`, `selected_xrefs.csv`, and `selected_reasons_summary.csv` for the matching client binary export.
3. Add sniff evidence only when the fixture clearly shows one extra packet in the `0x07F5..0x0818` range.
4. Do not enable a runtime send path until at least two evidence sources agree on when the packet fires and what its list counts or flag widths mean.
5. When a packet reaches Verified, document the exact enabling condition here before changing runtime code.