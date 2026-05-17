## NexusForever
[![Discord](https://img.shields.io/discord/499473932131500034.svg?style=flat&logo=discord)](https://discord.gg/8wT3GEQ)

### Information
A server emulator for WildStar written in C# that supports build 16042.

### Getting Started
[Server Setup Guide](https://www.emulator.ws/installation/server-guide)

For local database setup and SQL dump import automation in this checkout, see
[Tools/Setup/README.md](Tools/Setup/README.md).

For Codex and contributor orientation, see [AGENTS.md](AGENTS.md). It captures
the repo layout, common commands, generated files, constraints, and done
criteria for future agent sessions.

### Requirements
 * Visual Studio 2026 (.NET 10 and C# 14 support required)
 * MySQL Server (or equivalent, eg: MariaDB)
 * Message Broker (RabbitMQ or Azure Service Bus)
 * WildStar 16042 client

### Developer Quick Reference

From the repository root:

```powershell
dotnet restore Source\NexusForever.sln
dotnet build Source\NexusForever.sln -v minimal --nologo
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
```

Run a focused project build for faster edit loops:

```powershell
dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo
```

No dedicated repo-specific lint, format, or separate typecheck command is
documented in this checkout. Use `dotnet build` as the compile/typecheck gate
and preserve the local style in edited files.

Local setup and launch scripts live under `Tools\Setup`:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -InstallDotNetEf
.\Tools\Setup\Start-NexusForeverLocal.ps1 -ClientDirectory "D:\Games\WildStar" -PromptForRootPassword
```

Useful workflow docs:

* [Local setup automation](Tools/Setup/README.md)
* [Client decompile workflow](Decomp/Analysis/README.md)
* [Decompile continuation guide](Decomp/Analysis/CONTINUATION_GUIDE.md)
* [WildStar/Jabbithole data mapping](Tools/DataMapping/README.md)
* [Safe DataMapping SQL imports](Tools/DataMapping/sql/README.md)
* [Login-to-world load test harness](Tools/NexusForever.LoadTest/README.md)

### Branches
NexusForever has multiple branches:
* **[Master](https://github.com/NexusForever/NexusForever/tree/master)**  
Latest stable release, develop is merged into master once enough content has accumulated in develop.  
Compiled binary releases are based on this branch.
* **[Game Rework](https://github.com/NexusForever/NexusForever/tree/game_rework)**  
Current active development branch, major refactors and updates to the project are underway in this branch.  
All PR's should be targeted to this branch.  
This branch will eventually be merged back into develop.  
* **[Develop](https://github.com/NexusForever/NexusForever/tree/develop)**  
~~Active development branch with the latest features but may be unstable.  
Any new pull requests must be targed towards this branch.~~

### Links
 * [Website](https://emulator.ws)
 * [Discord](https://discord.gg/8wT3GEQ)
 * [World Database](https://github.com/NexusForever/NexusForever.WorldDatabase)

## Build Status
### Windows
Automated builds that will run on Windows or Windows Server.

Master:  
![Master](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Master%20Windows)  
Game Rework:  
![Game Rework](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Develop%20Windows?branchName=game_rework)  
Development:  
![Development](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Develop%20Windows?branchName=develop)
### Linux
Automated builds that will run on various Linux distributions.  
See the [.NET runtime identifer documentation](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#linux-rids)  for more information on exact distributions.

Master:  
![Master](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Master%20Linux)  
Game Rework:  
![Game Rework](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Develop%20Linux?branchName=game_rework)  
Development:  
![Development](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Develop%20Linux?branchName=develop)
