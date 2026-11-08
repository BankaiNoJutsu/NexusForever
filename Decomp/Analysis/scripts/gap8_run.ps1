# Usage: powershell -NoProfile -File gap8_run.ps1 -Script Gap8Decomp -Out path -ScriptArgs "addr1,addr2"  (one headless process, read-only, no analysis, no save)
param([string]$Script='Gap8Decomp',[string]$Out,[string]$ScriptArgs)
$ErrorActionPreference='Stop'
$sd='I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\scripts'
$a = $ScriptArgs -split ','
& 'I:\ghidra_12.1.4_PUBLIC\support\analyzeHeadless.bat' 'I:\tools\ghidra_proj' 'WildStar64' -process 'WildStar64.exe' -noanalysis -readOnly -scriptPath $sd -postScript "$Script.java" $Out @a
