# Live Client Probes

Reusable CDB and lightweight debugger helpers for local WildStar build 16042
evidence passes.

The scripts in this folder are tracked tooling. They write session logs and
generated CDB command files to `artifacts/live-prereq-debug` by default; those
outputs remain local evidence and are not intended for source control.

## Common Use

Run scripts from the repository root:

```powershell
.\Tools\LiveClientProbes\Run-GuildCdbProbe.ps1
```

If `WildStar64.exe` is not running, the scripts try to launch it with the local
client connector at `I:\WildStar\Client64\NexusForever.ClientConnector.exe`.
Override that when needed:

```powershell
.\Tools\LiveClientProbes\Run-GuildCdbProbe.ps1 `
  -ConnectorPath "D:\Games\WildStar\Client64\NexusForever.ClientConnector.exe"
```

While CDB is attached, trigger the requested in-game UI actions. When finished,
press `Ctrl+C` in the CDB console and enter `qd` to detach without killing the
client.

## Scripts

- `Run-GuildCdbProbe.ps1`: guild holomark, guild bank, recruitment, war-party
  token, and guild-prerequisite evidence. It watches mapped client send helpers
  `Network_SendOpcodePayloadHelper` and `Network_SendOpcodePayloadOrPackedHelper`.
- `Run-GroupRaidCdbProbe.ps1`: group finder, matching, ready-response, raid
  info, and group setting evidence. It watches mapped client send helpers
  `Network_SendOpcodePayloadHelper` and `Network_SendOpcodePayloadOrPackedHelper`.
- `Run-PrereqCdbProbe.ps1`: account-item and prerequisite CDB breakpoints.
- `Run-PrereqBreakpointProbe.ps1`: Python breakpoint harness for timed JSONL
  account-item/prerequisite captures.
- `Run-StorefrontCdbProbe.ps1`: storefront `RequestHistory` / opcode `0x082E`
  purchase-history request evidence.

For the Guild blocker row, create a guild first if needed:

```text
!guild register Guild Nexus
```

Then exercise holomark changes, guild bank tab open, item or money movement,
recruitment subscribe/details, and war-party boss-token UI where available.
