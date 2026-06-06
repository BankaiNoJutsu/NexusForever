# ArchiveProbe

ArchiveProbe is a small diagnostic extractor for WildStar patch archives. It is
useful when a local investigation needs to list or extract a focused path from
`ClientData.index` without adding one-off scratch projects under `artifacts/`.

## Usage

```powershell
dotnet run --project Tools\ArchiveProbe -- `
  "D:\Games\WildStar\Patch" `
  contains:Houston `
  .nexusforever-runtime\archive-probe\houston `
  --list-only
```

To extract matching files, omit `--list-only`.

```powershell
dotnet run --project Tools\ArchiveProbe -- `
  "D:\Games\WildStar\Patch" `
  ClientData/Houston/Houston.chm `
  .nexusforever-runtime\archive-probe\houston
```

The query accepts either an archive path or `contains:<text>` for a
case-insensitive path search.
