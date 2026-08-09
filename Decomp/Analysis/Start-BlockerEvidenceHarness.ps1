#requires -Version 5.1
<#
.SYNOPSIS
Starts the local NexusForever stack for blocker evidence passes with Trace logging.

.DESCRIPTION
Thin wrapper around Restart-NexusForeverLocal.ps1 that applies the defaults
from Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md and prints log-tail commands for the
evidence bundle workflow.

.EXAMPLE
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 -ClientDirectory "I:\WildStar" -PromptForRootPassword
#>

[CmdletBinding()]
param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string] $ClientDirectory = 'I:\WildStar',
    [string] $BundleName = 'blocker-evidence',
    [string] $OutputRoot = '',
    [string[]] $PlayerIds = @(),
    [int[]] $WorldIds = @(),
    [int[]] $PublicEventIds = @(),
    [int[]] $ObjectiveIds = @(),
    [string[]] $NegativeCases = @(),
    [string] $Notes = '',
    [ValidateSet('Trace', 'Debug', 'Info', 'Warn', 'Error', 'Fatal', 'Off')]
    [string] $LogLevel = 'Trace',
    [switch] $Lws036ChecklistSmoke,
    [switch] $NewZoneAssetProof,
    [switch] $DustStalkerQ4516Smoke,
    [switch] $ArcterraPalaverSourceSmoke,
    [switch] $SkyplotHousingSmoke,
    [switch] $LiveEventSmoke,
    [switch] $QuestVirtualLootSmoke,
    [switch] $RidersReefSmoke,
    [switch] $FortuneRewardsSmoke,
    [switch] $PublicEventVoteScoreboardSmoke,
    [switch] $PublicEventObjectiveNotificationSmoke,
    [switch] $PvpAdventureSmoke,
    [switch] $ExpeditionSmoke,
    [switch] $DungeonSmoke,
    [switch] $RaidEventSmoke,
    [switch] $PromptForRootPassword,
    [switch] $CreateBundleOnly,
    [switch] $SkipServerLaunch,
    [switch] $SkipClientLaunch
)

$restartScript = Join-Path $RepoRoot 'Tools\Setup\Restart-NexusForeverLocal.ps1'
if (-not (Test-Path -LiteralPath $restartScript)) {
    throw "Restart script not found: $restartScript"
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$safeBundleName = ($BundleName -replace '[^A-Za-z0-9_.-]+', '-').Trim('-')
if ([string]::IsNullOrWhiteSpace($safeBundleName)) {
    $safeBundleName = 'blocker-evidence'
}

$selectedPresetCount = @($Lws036ChecklistSmoke, $NewZoneAssetProof, $DustStalkerQ4516Smoke, $ArcterraPalaverSourceSmoke, $SkyplotHousingSmoke, $LiveEventSmoke, $QuestVirtualLootSmoke, $RidersReefSmoke, $FortuneRewardsSmoke, $PublicEventVoteScoreboardSmoke, $PublicEventObjectiveNotificationSmoke, $PvpAdventureSmoke, $ExpeditionSmoke, $DungeonSmoke, $RaidEventSmoke).Where({ $_ }).Count
if ($selectedPresetCount -gt 1) {
    throw 'Choose only one evidence preset: -Lws036ChecklistSmoke, -NewZoneAssetProof, -DustStalkerQ4516Smoke, -ArcterraPalaverSourceSmoke, -SkyplotHousingSmoke, -LiveEventSmoke, -QuestVirtualLootSmoke, -RidersReefSmoke, -FortuneRewardsSmoke, -PublicEventVoteScoreboardSmoke, -PublicEventObjectiveNotificationSmoke, -PvpAdventureSmoke, -ExpeditionSmoke, -DungeonSmoke, or -RaidEventSmoke.'
}

if ($Lws036ChecklistSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-036-city-checklist-smoke'
    }

    if ($WorldIds.Count -eq 0) {
        $WorldIds = @(22, 51, 426, 870, 990, 1387, 2180, 3460)
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'inactive or missing quest objective does not progress',
            'wrong world or wrong prop/entity does not progress',
            'repeat interaction after completion does not duplicate progress'
        )
    }

    $lws036Notes = 'LWS-036 targeted quest/client smoke for Illium Ringo Hax, Thayd/Illium housing intro story panels, and small-world checklist/objective rows. Record exact character, quest id, objective id, entity/creature id, checklist index, world, coordinates, logs, screenshots, and negative cases before promoting any WIP/GUESSED row.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $lws036Notes
    } else {
        $Notes = "$Notes`n$lws036Notes"
    }
}

if ($NewZoneAssetProof) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'new-zone-asset-proof'
    }

    if ($WorldIds.Count -eq 0) {
        $WorldIds = @(22, 51, 1068)
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'zero-coordinate or area-zero source row is not promoted without visible placement proof',
            'destructive broad SQL replacement is not applied to runtime data',
            'wrong world or missing client asset remains rejected/blocked'
        )
    }

    $newZoneNotes = 'New-zone/sandbox asset-proof pass for LWS-040 through LWS-043. Capture client asset availability, runtime placement, coordinates, entity ids, screenshots/video, logs, and negative cases before promoting any Dreadmoor, Halon Ring, Murkmire, or sandbox/test-zone row.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $newZoneNotes
    } else {
        $Notes = "$Notes`n$newZoneNotes"
    }
}

if ($DustStalkerQ4516Smoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-051-dust-stalker-q4516'
    }

    if ($WorldIds.Count -eq 0) {
        $WorldIds = @(1138)
    }

    if ($ObjectiveIds.Count -eq 0) {
        $ObjectiveIds = @(7633, 6189, 7637, 7638, 7639)
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'activate bridge controls before killing Boss Xagg',
            'activate exit panel before bridge-control self-destruct state',
            'miss the escape timer after self-destruct starts',
            'repeat bridge-control or exit-panel interaction after quest completion'
        )
    }

    $dustStalkerNotes = 'LWS-051 Dust Stalker quest 4516 smoke for Boss Xagg, bridge controls, self-destruct timer, exit panel, escape timing, and cleanup/despawn behavior. Record exact quest state, objective ids, entity ids, coordinates, observed timer duration, logs, screenshots/video, and negative cases before implementing or unblocking runtime behavior.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $dustStalkerNotes
    } else {
        $Notes = "$Notes`n$dustStalkerNotes"
    }
}

if ($ArcterraPalaverSourceSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-052-arcterra-palaver-source-only'
    }

    if ($WorldIds.Count -eq 0) {
        $WorldIds = @(3335, 3519)
    }

    if ($ObjectiveIds.Count -eq 0) {
        $ObjectiveIds = @(21469, 21470, 21477, 21478, 21485, 21486, 21487, 21488)
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'activate Coldblood portal with missing or invalid target state',
            "interact with Palaver Ish'amel in an absent or wrong quest state",
            'interact with Arcterra Caretaker without the expected prerequisite',
            'repeat source-only row interaction after completion or rejection'
        )
    }

    $arcterraPalaverNotes = "LWS-052 source-only smoke for Arcterra Caretaker, Arcterra Coldblood portal, and Palaver Point Ish'amel. Record exact placement, stats, interaction result, portal target or failure behavior, quest state, logs, screenshots/video, and negative cases before removing WIP/GUESSED labels or adding runtime behavior."
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $arcterraPalaverNotes
    } else {
        $Notes = "$Notes`n$arcterraPalaverNotes"
    }
}

if ($SkyplotHousingSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-053-skyplot-housing'
    }

    if ($WorldIds.Count -eq 0) {
        $WorldIds = @(1229)
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'activate return pad without an active residence or community session',
            'open Skyplot vendor before housing or community unlock state',
            'request missing or invalid Skyplot vendor catalog row',
            'interact while Skyplot community lifecycle is inactive'
        )
    }

    $skyplotNotes = 'LWS-053 Skyplot housing smoke for return pad creature 35298 activePropId 4676188, vendors 68423 and 68424, vendor catalog completeness, and active residence/community lifecycle behavior. Record account/residence/community state, coordinates, vendor-list rows, return-pad result, logs, screenshots/video, and negative cases before promoting WIP/GUESSED rows or behavior.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $skyplotNotes
    } else {
        $Notes = "$Notes`n$skyplotNotes"
    }
}

if ($LiveEventSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-054-live-events'
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'event inactive state does not expose event-only spawns or vendors',
            'event ending during vendor interaction does not leave stale state',
            'post-cleanup interaction or vendor request is rejected',
            'duplicate, placeholder, or malformed skipped source row remains absent'
        )
    }

    $liveEventNotes = 'LWS-054 live-event smoke for Battle Chase, Dungeon Chase, Shades Eve, Space Chase, Starfall, Sim Chase 1, and zPrix. Record event calendar/state setup, spawn/vendor timing, lifecycle activation/deactivation timestamps, cleanup behavior, logs, screenshots/video, observed vendor rows, and negative cases before implementing runtime lifecycle behavior or removing WIP/GUESSED labels.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $liveEventNotes
    } else {
        $Notes = "$Notes`n$liveEventNotes"
    }
}

if ($QuestVirtualLootSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-055-quest-virtual-loot'
    }

    if ($ObjectiveIds.Count -eq 0) {
        $ObjectiveIds = @(13011, 12605, 6700, 6313, 21278)
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'objective inactive does not grant virtual-item quest loot',
            'creature killed before objective activation does not grant later retroactive credit',
            'repeated eligible kills after objective completion do not duplicate progress',
            'loot window declined, abandoned, or unavailable does not incorrectly complete objective'
        )
    }

    $questVirtualLootNotes = 'LWS-055 quest virtual-loot smoke for representative LaughingWS rows. Record active quest/objective state, target entity id, kill/activation count, loot window or immediate grant behavior, ServerLoot traffic when available, virtual item feedback, objective progress before/after, logs, repeated eligible attempts, and negative cases before widening trigger cadence, probability, corpse/source selection, or objective integration behavior.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $questVirtualLootNotes
    } else {
        $Notes = "$Notes`n$questVirtualLootNotes"
    }
}

if ($RidersReefSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'F-023-riders-reef-smoke'
    }

    if ($WorldIds.Count -eq 0) {
        $WorldIds = @(3460, 51, 870, 990, 1387)
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'wrong-faction departure terminal does not route or complete the opposite faction handoff',
            'repeat terminal interaction after final quest completion does not duplicate rewards or quest state',
            'logout/re-entry after movement/projector progress restores the expected tutorial chain without root-skip regression',
            'hoverboard/projector recovery does not strand the player or duplicate ring/projector credit',
            'destination welcome quest is absent or mismatched and the handoff remains blocked instead of inventing quest state'
        )
    }

    $ridersReefNotes = 'F-023 Rider''s Reef Exile and Dominion smoke from character creation through final terminal handoff. Capture quest acceptance, movement/projector/hoverboard/mine/combat loops, reward UI/toasts, respawn/re-entry, CSI, terminal routing to Everstar Grove/Northern Wilds/Crimson Isle/Levian Bay, destination welcome quests, logs, screenshots/video, and negative cases before promoting remaining NPE behavior.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $ridersReefNotes
    } else {
        $Notes = "$Notes`n$ridersReefNotes"
    }
}

if ($FortuneRewardsSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-066-fortune-rewards'
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'open Fortune UI without enough FortuneCoin and confirm no card deal or payout',
            'flip a card before starting a session and capture reset/rejection behavior',
            'repeat a card flip for an already flipped card and capture reset/rejection behavior',
            'reload after a partial Fortune session and verify persisted card state before any new start'
        )
    }

    $fortuneRewardsNotes = 'LWS-066/F-031 Fortune rewards smoke for Madame Fay catalog probabilities, active rotation evidence, FortuneCoin cost, card dealing/flipping, payout, and account_fortune_session reload behavior. Capture ServerFortuneRewards item/probability payloads or storefront catalog data; do not replace emulator rarity-tier weights without per-item/rotation evidence.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $fortuneRewardsNotes
    } else {
        $Notes = "$Notes`n$fortuneRewardsNotes"
    }
}

if ($PublicEventVoteScoreboardSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-071-072-public-event-vote-scoreboard'
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'non-participant public-event vote is ignored or rejected without tally/result mutation',
            'duplicate vote after a valid response is ignored or rejected without extra tally/result mutation',
            'late vote after vote end is ignored or rejected without reopening the vote',
            'scoreboard request for an event the player is not participating in does not emit a stats snapshot',
            'scoreboard unsubscribe does not emit a stats snapshot'
        )
    }

    $publicEventVoteScoreboardNotes = 'LWS-071/LWS-072 public-event vote and scoreboard smoke. Capture vote initiate/detailed-initiate, client choice, tally/end ordering, timeout/default choice, team/vote id handling, scoreboard subscribe/unsubscribe cadence, stats rows, event end/reward thresholds, logs, screenshots/video, and negative cases before widening vote lifecycle, subscription cadence, or reward delivery behavior.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $publicEventVoteScoreboardNotes
    } else {
        $Notes = "$Notes`n$publicEventVoteScoreboardNotes"
    }
}

if ($PublicEventObjectiveNotificationSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-073-public-event-objective-notification'
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'objective start/update is not emitted to a player outside the event audience',
            'phase completion does not duplicate objective notification packets after repeat trigger or duplicate status change',
            'quest-share prompt or quest objective text is not emitted before the mapped public-event objective state allows it',
            'objective notification mode change after event completion is ignored or rejected without reopening the objective'
        )
    }

    $objectiveNotificationNotes = 'LWS-073 public-event objective notification smoke. Capture ServerPublicEventObjectiveStart, ServerPublicEventObjectiveUpdate, ServerPublicEventObjectiveStatusUpdate, ServerPublicEventObjectiveNotificationMode, related quest-share/text packets, target audience, packet order, mode values, objective/phase text ids, logs, screenshots/video, and negative cases before widening notification text, audience, ordering, or quest-share behavior.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $objectiveNotificationNotes
    } else {
        $Notes = "$Notes`n$objectiveNotificationNotes"
    }
}

if ($PvpAdventureSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-080-085-pvp-adventure'
    }

    if ($WorldIds.Count -eq 0) {
        $WorldIds = @(797, 1393, 1627, 2166, 3022, 3449)
    }

    if ($PublicEventIds.Count -eq 0) {
        $PublicEventIds = @(158, 170, 171, 213, 217, 366, 438, 466, 581, 582, 876, 877)
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'queue or enter with insufficient players and capture the rejection or waiting state',
            'leave or disconnect during match start and capture cleanup/deserter/state behavior',
            'request scoreboard or rewards before event completion and capture no premature delivery',
            'force objective interaction from the wrong team or wrong phase and capture rejection/no-op behavior',
            'finish or abandon an event before required objectives and capture result/reward behavior'
        )
    }

    $pvpAdventureNotes = 'LWS-080 through LWS-085 PvP/adventure smoke for Cryo-Plex, Daggerstone Pass, Halls of the Bloodsworn, Walatiki Temple, War of the Wilds, and Rage Logic. Capture queue/match lifecycle, start/end packets, scoring, objective flow, scoreboards, rewards, PvP stats, faction-specific starts, vehicle choice, logs, screenshots/video, and negative cases before widening scaffold behavior.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $pvpAdventureNotes
    } else {
        $Notes = "$Notes`n$pvpAdventureNotes"
    }
}

if ($ExpeditionSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-090-096-expeditions'
    }

    if ($WorldIds.Count -eq 0) {
        $WorldIds = @(1232, 1319, 2149, 2183, 2188, 3180, 3404)
    }

    if ($PublicEventIds.Count -eq 0) {
        $PublicEventIds = @(95, 108, 390, 446, 447, 680, 781)
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'use a branch zero-vector or placeholder trigger and confirm it remains disabled/rejected',
            'interact with a door, shuttle, or teleporter before the required phase and capture rejection/no-op behavior',
            'enter a trigger volume from the wrong phase or after completion and capture no duplicate progress',
            'skip or finish a completion-only cinematic placeholder and capture exact follow-up state before replacing it',
            'finish or abandon the expedition before required objectives and capture result/reward behavior'
        )
    }

    $expeditionNotes = 'LWS-090 through LWS-096 expedition smoke for Outpost M-13, Space Madness, Fragment Zero, Gauntlet, Infestation, Evil from the Ether, and Deep Space Exploration. Capture shuttle/door/trigger/cinematic/communicator/teleport/cleanup/routing/reward behavior, logs, screenshots/video, and negative cases before widening WIP scaffolds or replacing completion-only cinematic placeholders.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $expeditionNotes
    } else {
        $Notes = "$Notes`n$expeditionNotes"
    }
}

if ($DungeonSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-100-106-dungeons'
    }

    if ($WorldIds.Count -eq 0) {
        $WorldIds = @(382, 1263, 1271, 1336, 2980, 3173, 3522)
    }

    if ($PublicEventIds.Count -eq 0) {
        $PublicEventIds = @(145, 148, 161, 166, 594, 667, 907)
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'enter an objective trigger before its required phase and capture rejection/no duplicate progress',
            'interact with a door, platform, launcher, or teleporter before required objectives and capture rejection/no-op behavior',
            'force optional route objectives outside the selected route and capture no premature activation',
            'skip or finish a completion-only cinematic placeholder and capture exact follow-up state before replacing it',
            'finish or abandon the dungeon before required objectives and capture result/reward behavior'
        )
    }

    $dungeonNotes = 'LWS-100 through LWS-106 dungeon smoke for Coldblood Citadel, Protogames Academy, Ruins of Kel Voreth, Stormtalon''s Lair, Skullcano, Sanctuary of the Swordmaiden, and Ultimate Protogames dungeon. Capture route/objective selection, triggers, doors/platforms/launchers, communicators, cinematics, boss cadence/mechanics, cleanup, rewards, logs, screenshots/video, and negative cases before widening WIP scaffolds or replacing completion-only cinematic placeholders.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $dungeonNotes
    } else {
        $Notes = "$Notes`n$dungeonNotes"
    }
}

if ($RaidEventSmoke) {
    if ($BundleName -eq 'blocker-evidence') {
        $safeBundleName = 'LWS-110-117-raid-event-instances'
    }

    if ($WorldIds.Count -eq 0) {
        $WorldIds = @(1333, 1462, 3032, 3040, 3044, 3045, 3094)
    }

    if ($PublicEventIds.Count -eq 0) {
        $PublicEventIds = @(157, 159, 595, 597, 605, 679, 705)
    }

    if ($NegativeCases.Count -eq 0) {
        $NegativeCases = @(
            'enter a trigger or target set before the required phase and capture rejection/no duplicate progress',
            'interact with a door, elevator, town gate, store room, or encounter object before required objectives and capture rejection/no-op behavior',
            'force weekly/random/wing/room selection outside the selected route and capture no premature activation',
            'skip or finish a completion-only cinematic placeholder and capture exact follow-up state before replacing it',
            'finish or abandon the raid or event instance before required objectives and capture result/reward behavior'
        )
    }

    $raidEventNotes = 'LWS-110 through LWS-117 raid/event-instance smoke for Initialization Core Y-83, Red Moon Terror, Genetic Archives, Datascape, Shade''s Eve, Protostar SuperMall in the Sky, and Journey into OMNICore-1. Capture target sets, route/wing/room selection, triggers, doors/elevators/town gates, communicators, cinematics, encounter mechanics, cleanup, rewards, logs, screenshots/video, and negative cases before widening WIP scaffolds or replacing completion-only cinematic placeholders.'
    if ([string]::IsNullOrWhiteSpace($Notes)) {
        $Notes = $raidEventNotes
    } else {
        $Notes = "$Notes`n$raidEventNotes"
    }
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $RepoRoot 'artifacts\blocker_evidence'
}

$bundleDirectory = Join-Path $OutputRoot "$timestamp-$safeBundleName"
$screenshotsDirectory = Join-Path $bundleDirectory 'screenshots'
$videoDirectory = Join-Path $bundleDirectory 'video'
$logsDirectory = Join-Path $bundleDirectory 'logs'
New-Item -ItemType Directory -Force -Path $bundleDirectory, $screenshotsDirectory, $videoDirectory, $logsDirectory | Out-Null

$relativePlanPath = 'Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md'
$requiredFiles = @(
    'manifest.json',
    'commands.md',
    'server-log-notes.md',
    'client-observations.md',
    'negative-cases.md'
)
if ($Lws036ChecklistSmoke) {
    $requiredFiles += 'lws-036-targets.md'
}
if ($NewZoneAssetProof) {
    $requiredFiles += 'new-zone-asset-proof-targets.md'
}
if ($DustStalkerQ4516Smoke) {
    $requiredFiles += 'lws-051-dust-stalker-targets.md'
}
if ($ArcterraPalaverSourceSmoke) {
    $requiredFiles += 'lws-052-arcterra-palaver-targets.md'
}
if ($SkyplotHousingSmoke) {
    $requiredFiles += 'lws-053-skyplot-housing-targets.md'
}
if ($LiveEventSmoke) {
    $requiredFiles += 'lws-054-live-event-targets.md'
}
if ($QuestVirtualLootSmoke) {
    $requiredFiles += 'lws-055-quest-virtual-loot-targets.md'
}
if ($RidersReefSmoke) {
    $requiredFiles += 'f-023-riders-reef-smoke-targets.md'
}
if ($FortuneRewardsSmoke) {
    $requiredFiles += 'lws-066-fortune-rewards-targets.md'
}
if ($PublicEventVoteScoreboardSmoke) {
    $requiredFiles += 'lws-071-072-public-event-vote-scoreboard-targets.md'
}
if ($PublicEventObjectiveNotificationSmoke) {
    $requiredFiles += 'lws-073-public-event-objective-notification-targets.md'
}
if ($PvpAdventureSmoke) {
    $requiredFiles += 'lws-080-085-pvp-adventure-targets.md'
}
if ($ExpeditionSmoke) {
    $requiredFiles += 'lws-090-096-expedition-targets.md'
}
if ($DungeonSmoke) {
    $requiredFiles += 'lws-100-106-dungeon-targets.md'
}
if ($RaidEventSmoke) {
    $requiredFiles += 'lws-110-117-raid-event-targets.md'
}
$requiredFiles += @(
    'screenshots/',
    'video/',
    'logs/'
)

$manifest = [ordered]@{
    createdAt       = (Get-Date).ToString('o')
    bundleName      = $safeBundleName
    repoRoot        = $RepoRoot
    clientDirectory = $ClientDirectory
    logLevel        = $LogLevel
    plan            = $relativePlanPath
    playerIds       = $PlayerIds
    worldIds        = $WorldIds
    publicEventIds  = $PublicEventIds
    objectiveIds    = $ObjectiveIds
    negativeCases   = $NegativeCases
    notes           = $Notes
    requiredFiles   = $requiredFiles
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $bundleDirectory 'manifest.json') -Encoding UTF8

$commandsTemplate = @"
# Command And Action Transcript

Bundle: $safeBundleName
Created: $timestamp

Record every command, UI action, and client-visible result in order.

| Time | Client/account | Command or action | Expected result | Observed result | Evidence file |
| --- | --- | --- | --- | --- | --- |
| | | `!help` | | | |

Baseline commands:

~~~text
!help
!help teleport
!character level 50
!currency character add Credits 10000000
!character save
!teleport name Thayd
!teleport name Illium
!teleport coordinates <x> <y> <z> [worldId]
~~~
"@
$commandsTemplate | Set-Content -Path (Join-Path $bundleDirectory 'commands.md') -Encoding UTF8

$serverLogTemplate = @"
# Server Log Notes

Copy relevant log excerpts into `logs/` and summarize the exact matching lines
here. Include player/request ids, world ids, public event ids, objective ids,
entity ids, spell ids, packet names, and rejection reasons where applicable.

| Time | Log file | Component | Matching line or summary | Related command/action |
| --- | --- | --- | --- | --- |
| | | | | |
"@
$serverLogTemplate | Set-Content -Path (Join-Path $bundleDirectory 'server-log-notes.md') -Encoding UTF8

$clientTemplate = @'
# Client Observations

Attach screenshots in `screenshots/` and video in `video/`. Reference file names
from the table below.

| Time | Client/account | World/coordinates | UI or world result | Screenshot/video |
| --- | --- | --- | --- | --- |
| | | | | |
'@
$clientTemplate | Set-Content -Path (Join-Path $bundleDirectory 'client-observations.md') -Encoding UTF8

$negativeCaseText = @"
# Negative Cases

Record at least one negative/rejection case for the blocker under test.

Configured negative cases:
$($NegativeCases | ForEach-Object { "- $_" } | Out-String)

| Time | Case | Command/action | Expected rejection | Observed rejection | Evidence file |
| --- | --- | --- | --- | --- | --- |
| | | | | | |
"@
$negativeCaseText | Set-Content -Path (Join-Path $bundleDirectory 'negative-cases.md') -Encoding UTF8

if ($Lws036ChecklistSmoke) {
    $lws036Targets = @'
# LWS-036 City And Checklist Smoke Targets

Use this worksheet with `Decomp/Analysis/LAUGHINGWS_CITY_CHECKLIST_SMOKE_GATE_REVIEW.md`.
Do not promote WIP/GUESSED rows until the matching target has positive proof and
at least one negative case. Copy final log excerpts into `logs/` and summarize
them in `server-log-notes.md`.

## Required Per Target

| Field | Value |
| --- | --- |
| Account/character | |
| Quest id and objective id | |
| World id and coordinates | |
| Entity id / creature id / activePropId | |
| questChecklistIdx, when applicable | |
| Server log evidence file | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Target Families

| Family | World(s) | Seed or source | Expected smoke proof |
| --- | ---: | --- | --- |
| Illium Ringo Hax `Return to Illium` finisher | 22 | `Tools/DataMapping/sql/laughingws_city_content_seed.sql`, creature `44961`, row `1100000015` | NPC visible/interactable at the seeded Illium coordinates; correct quest/objective progresses; inactive/wrong quest does not progress. |
| Illium housing intro props | 22 | creatures `65296`..`65299`, checklist indices `1`..`4` | Each prop shows the expected story panel/objective behavior and only progresses the matching housing intro objective. |
| Thayd housing intro props | 51 | creatures `54400`, `54401`, `54403`, `54404`, checklist indices `1`..`4` | Each prop shows the expected story panel/objective behavior and only progresses the matching housing intro objective. |
| Auroria official checklist updates | seed rows | `Tools/DataMapping/sql/laughingws_small_world_wip_seed.sql`, `67` coordinate-keyed checklist rows plus fallback rows on older imports | Representative positive and negative objective progress for each quest/objective group before converting any row family. |
| Crimson Isle official checklist updates | seed rows | `Tools/DataMapping/sql/laughingws_small_world_wip_seed.sql`, `24` coordinate-keyed checklist rows | Representative positive and negative objective progress for each quest/objective group before converting any row family. |
| Levian Bay Signal Flare / Drop Pod Landing Beacon | seed rows | `Tools/DataMapping/sql/laughingws_small_world_wip_seed.sql`, `14` coordinate-keyed checklist rows | Positive progress and wrong-objective rejection for each object family. |
| Everstar Grove Exo-Lab 71 Eldan Teleporter | seed rows | `Tools/DataMapping/sql/laughingws_small_world_wip_seed.sql`, `2` coordinate-keyed checklist rows | Positive progress and wrong-objective rejection for each teleporter row. |
| Wilderrun checklist updates | seed rows | `Tools/DataMapping/sql/laughingws_small_world_wip_seed.sql`, `33` coordinate-keyed checklist rows | Positive progress and wrong-objective rejection for each object family. |
| Northern Wilds objective/entity rows | 426 | `7` WIP placement rows plus `3` Q3673 signal flare rows | Entity visibility/interactability and quest/objective progress; wrong quest/objective rejection. |

## Useful Commands

~~~text
!character level 50
!currency character add Credits 10000000
!character save
!teleport name Illium
!teleport name Thayd
!teleport coordinates <x> <y> <z> [worldId]
~~~
'@

    $lws036Targets | Set-Content -Path (Join-Path $bundleDirectory 'lws-036-targets.md') -Encoding UTF8
}

if ($NewZoneAssetProof) {
    $newZoneTargets = @'
# New-Zone And Sandbox Asset-Proof Targets

Use this worksheet with `Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md`
and `Decomp/Analysis/LAUGHINGWS_SANDBOX_ZONE_ASSET_PROOF_CHECKLIST.md`.
Do not promote runtime seeds from broad replacement SQL unless a concrete row has
client asset proof, runtime placement proof, positive behavior, and a negative
safety case.

## Required Per Candidate Row

| Field | Value |
| --- | --- |
| Source SQL file and source row | |
| Proposed runtime world id | |
| Entity/stat/vendor/script row ids | |
| Creature/display/outfit/asset ids | |
| Exact coordinates and area | |
| Client asset proof file | |
| Runtime visibility/interactability proof | |
| Server log evidence file | |
| Negative/safety proof | |
| Outcome: implemented, rejected, or still blocked | |

## Target Families

| Family | Source | Current disposition | Required proof before promotion |
| --- | --- | --- | --- |
| Dreadmoor broad rows | `Map/Open World/New Zones/Dreadmoor.sql` | `blocked_laughingws_new_zone_broad_rows`; zero-coordinate placeholder entity rows | Client asset proof for a specific row, exact non-placeholder placement, current runtime script/interaction proof, and negative proof for nearby wrong rows. |
| Halon Ring broad residual rows | `Map/Open World/New Zones/Halon Ring.sql` | only curated city/transport crumbs are covered; broad residual rows remain blocked | Specific quest/transport/NPC proof beyond the already promoted city-content seed, exact coordinates, and no destructive broad replacement. |
| Murkmire broad rows | `Map/Open World/New Zones/Murkmire.sql` | `blocked_laughingws_new_zone_broad_rows`; TODO-heavy quest-checklist/stat placeholders | Current script/client proof for a specific row plus exact objective or interaction behavior. |
| Sandbox/test/unknown/not-in-client zones | `Test zones/*` | `sandbox_only_client_asset_check_required` / runtime rejected | Explicit sandbox investigation target, asset availability proof, isolated runtime seed plan, and proof that it does not affect build-16042 normal runtime. |

## Useful Commands

~~~text
!character level 50
!teleport coordinates <x> <y> <z> [worldId]
!character save
~~~
'@

    $newZoneTargets | Set-Content -Path (Join-Path $bundleDirectory 'new-zone-asset-proof-targets.md') -Encoding UTF8
}

if ($DustStalkerQ4516Smoke) {
    $dustStalkerTargets = @'
# LWS-051 Dust Stalker Quest 4516 Smoke Targets

Use this worksheet with `Decomp/Analysis/LAUGHINGWS_DUST_STALKER_Q4516_REVIEW.md`.
Do not implement self-destruct, exit-panel, escape timing, forced teleport, or
cleanup/despawn runtime behavior until this bundle proves the exact sequence.

## Required Evidence

| Field | Value |
| --- | --- |
| Account/character | |
| Quest state before entering | |
| World id, area id, and coordinates | |
| Objective id and objective text | |
| Entity id / creature id / activePropId | |
| Observed timer duration | |
| Server log evidence file | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Target Sequence

| Step | World / Area | Seed or source | Expected smoke proof |
| --- | ---: | --- | --- |
| Enter Boss Xagg's ship | `1138` / `950` | Quest `4516`, objective `7633` | Objective progresses only when entering the proved ship area; wrong area or missing quest does not progress. |
| Kill Boss Xagg | `1138` / `950` | Creature `16707`, entity `1100100001`, coords `-0.05071211, 6.279976, 243.2797`; DataMapping-backed stats health `8767`, level `19`, shield `2352`, interrupt armor `0` | Kill advances the matching objective and unlocks the next sequence state; premature bridge use remains rejected. |
| Activate bridge controls | `1138` / `950` | Creature `16733`, entity `1100100002`, type `32` SimpleCollidable, coords `-0.08464324, 8.691921, 258.9572` | Interaction starts the proved self-destruct state and records the exact timer/prompt/log behavior. |
| Escape through exit panel | `1138` / `950` | Source-only objective creature `23758`, entity `1100100003`, type `10`, coords `-7.8, 1.5, -15.4`; source comment `place for real` | Interaction before self-destruct is rejected; interaction during the proved window completes the escape objective and records teleport/cleanup/despawn behavior. |
| Timer expiry | `1138` / `950` | Objectives `7637`, `7638`, `7639` | Missing the timer produces the observed failure/reset/teleport behavior without duplicate completion. |

## Useful Commands

~~~text
!character level 50
!character save
!teleport coordinates -0.05071211 6.279976 243.2797 1138
!teleport coordinates -0.08464324 8.691921 258.9572 1138
!teleport coordinates -7.8 1.5 -15.4 1138
~~~
'@

    $dustStalkerTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-051-dust-stalker-targets.md') -Encoding UTF8
}

if ($ArcterraPalaverSourceSmoke) {
    $arcterraPalaverTargets = @'
# LWS-052 Arcterra And Palaver Source-Only Smoke Targets

Use this worksheet with `Decomp/Analysis/LAUGHINGWS_ARCTERRA_PALAVER_SOURCE_ONLY_REVIEW.md`.
Do not remove WIP/GUESSED labels or add portal, combat, quest, teleport, or
interaction behavior until this bundle proves the exact row behavior.

## Required Evidence

| Field | Value |
| --- | --- |
| Account/character | |
| Quest state and objective id, when applicable | |
| World id, area id, and coordinates | |
| Entity id / creature id / activePropId | |
| Observed stats or interaction result | |
| Portal target or failure behavior, when applicable | |
| Server log evidence file | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Source-Only Rows

| Target | World / Area | Seed or source | Expected smoke proof |
| --- | ---: | --- | --- |
| Arcterra The Caretaker | `3335` / `4767` | Source creature `70779`, entity `1100200007`, coords `-158.918, -895.3669, -311.746` | NPC visibility, exact placement, stats/interaction behavior, prerequisite quest/state if any, and wrong-state rejection. |
| Arcterra Coldblood Citadel portal | `3335` / `4776` | Source creature `75701`, entity `1100200008`, coords `-1161.66, -634.28, 761.08` | Portal interaction target, failure behavior when target/state is invalid, repeated activation behavior, and no broad teleport inference from source SQL alone. |
| Palaver Point Ish'amel the Bloodied | `3519` / `6016` | Source creature `75343`, entity `1100200009`, coords `758.9, 10.06005, -835.32` | NPC visibility, exact placement/stats, Palaver quest-state behavior, absent/wrong quest rejection, and repeat interaction behavior. |

## Useful Commands

~~~text
!character level 50
!character save
!teleport coordinates -158.918 -895.3669 -311.746 3335
!teleport coordinates -1161.66 -634.28 761.08 3335
!teleport coordinates 758.9 10.06005 -835.32 3519
~~~
'@

    $arcterraPalaverTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-052-arcterra-palaver-targets.md') -Encoding UTF8
}

if ($SkyplotHousingSmoke) {
    $skyplotTargets = @'
# LWS-053 Skyplot Housing Smoke Targets

Use this worksheet with `Decomp/Analysis/LAUGHINGWS_SKYPLOT_HOUSING_REVIEW.md`.
Do not promote the Skyplot WIP/GUESSED overlay or add return-pad, vendor,
catalogue, or active-community lifecycle behavior until this bundle proves the
exact runtime surface.

## Required Evidence

| Field | Value |
| --- | --- |
| Account/character | |
| Residence/community/session state | |
| World id, area id, and coordinates | |
| Entity id / creature id / activePropId | |
| Vendor list/catalog evidence file | |
| Return-pad activation result | |
| Server log evidence file | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Preserved Skyplot Rows

| Target | World / Area | Seed row | Expected smoke proof |
| --- | ---: | --- | --- |
| Return teleporter / pad | `1229` / `1136` | Creature `35298`, entity `2000000001`, `activePropId` `4676188`, coords `1567.3907, -707.63617, 1323.1534` | Visible return pad, proven activation result, correct residence/community state gating, and rejection without a valid session. |
| Skyplot vendor 68423 | `1229` / `1136` | Creature `68423`, entity `2000001001`, display `32799`, outfit `9902`, coords `1578.6104, -706.989, 1326.0824` | Vendor visibility, catalog open result, complete observed vendor rows against the generated seed, and rejection before unlock/state. |
| Skyplot vendor 68424 | `1229` / `1136` | Creature `68424`, entity `2000002001`, display `28454`, outfit `9269`, coords `1578.6283, -706.9887, 1326.0425` | Vendor visibility, catalog open result, complete observed vendor rows against the generated seed, and invalid catalog row rejection. |
| Active Skyplot lifecycle | `1229` / `1136` | `411` generated WIP/GUESSED rows in `Tools/DataMapping/sql/laughingws_housing_skyplot_wip_seed.sql` | Loaded residence/community state, entry/exit behavior, lifecycle activation, inactive-state rejection, screenshots/video, and server logs. |

## Useful Commands

~~~text
!character level 50
!character save
!teleport coordinates 1567.3907 -707.63617 1323.1534 1229
!teleport coordinates 1578.6104 -706.989 1326.0824 1229
!teleport coordinates 1578.6283 -706.9887 1326.0425 1229
~~~
'@

    $skyplotTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-053-skyplot-housing-targets.md') -Encoding UTF8
}

if ($LiveEventSmoke) {
    $liveEventTargets = @'
# LWS-054 Live Event Smoke Targets

Use this worksheet with `Decomp/Analysis/LAUGHINGWS_LIVE_EVENT_SMOKE_REVIEW.md`.
Do not implement spawn timing, vendor timing, lifecycle activation, cleanup, or
event-specific runtime state until a completed bundle proves the concrete event
behavior.

## Required Per Event Family

| Field | Value |
| --- | --- |
| Event family and configured calendar/state | |
| Account/character | |
| World id and coordinates sampled | |
| Entity/vendor ids observed | |
| Activation timestamp and source | |
| Deactivation/cleanup timestamp | |
| Vendor list/catalog evidence file | |
| Server log evidence file | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Event Families

| Event family | Source | Preserved rows | Required smoke proof |
| --- | --- | ---: | --- |
| Battle Chase | `Events/Battle Chase.sql` | `55` | Spawn timing, vendor timing when present, lifecycle activation, cleanup, inactive-state rejection. |
| Dungeon Chase | `Events/Dungeon Chase.sql` | `77` | Spawn/vendor timing, lifecycle activation, cleanup, and continued hidden SMC placeholder absence unless storefront schema proof changes. |
| Shades Eve | `Events/Shades Eve.sql` | `139` | Entity/vendor timing, activation, cleanup, interaction proof, and malformed skipped vendor row absence. |
| Space Chase | `Events/Space Chase.sql` | `48` | Spawn/vendor timing, lifecycle activation, cleanup, duplicate-source coordinate absence. |
| Starfall week 1-5 | `Events/Starfall/Starfall week 1.sql` through `week 5.sql` | `46` | Weekly selection, spawn timing, lifecycle activation, wrong-week rejection, cleanup. |
| Sim Chase 1 | `Events/Sim Chase/Sim Chase 1.sql` | `61` | Spawn/vendor timing, lifecycle activation, cleanup, duplicate Sim Chase source absence. |
| zPrix | `Events/zPrix.sql` | `14` | WIP source placement, spawn timing, lifecycle activation, cleanup, inactive-state rejection. |

## Known Source Hazards To Check

| Hazard | Expected proof |
| --- | --- |
| Official duplicate rows | Skipped rows remain absent or official-owned, not duplicated by the WIP seed. |
| Placeholder coordinates | Placeholder rows are not visible/promoted without proof. |
| Duplicate event coordinates | Duplicate-source rows remain absent unless a specific row is independently proven. |
| Event remover scripts | Removers are not imported as runtime behavior from branch SQL alone. |
| Malformed Shade's Eve vendor item | The malformed source row remains absent and does not appear in vendor output. |

## Useful Commands

~~~text
!character level 50
!currency character add Credits 10000000
!character save
!teleport coordinates <x> <y> <z> [worldId]
~~~
'@

    $liveEventTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-054-live-event-targets.md') -Encoding UTF8
}

if ($QuestVirtualLootSmoke) {
    $questVirtualLootTargets = @'
# LWS-055 Quest Virtual Loot Smoke Targets

Use this worksheet with `Decomp/Analysis/LAUGHINGWS_QUEST_VIRTUAL_LOOT_REVIEW.md`.
Do not widen quest-loot trigger cadence, probability, corpse/source selection, or
objective-side logic until representative client/server evidence proves the
behavior.

## Required Per Representative Row

| Field | Value |
| --- | --- |
| Account/character | |
| Quest id and objective id | |
| Target entity id / creature id | |
| Loot group and virtual item id | |
| Quest/objective state before action | |
| Kill/activation attempt count | |
| Loot window or immediate grant result | |
| Objective progress before/after | |
| Server log or packet evidence file | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Representative Parsed Rows

| Loot group | Objective | Virtual item | Example comment | Required smoke proof |
| ---: | ---: | ---: | --- | --- |
| `1200000001` | `13011` | `265` | Cold Survival Kit | Active objective grant, inactive rejection, repeated eligible attempts for cadence/probability. |
| `1200000002` | `12605` | `190` | Torine Weapon Fragment | Active objective grant, source/corpse selection, repeated attempt behavior. |
| `1200000003` | `6700` | `340` | Cannon Activation Code | Virtual item feedback, objective progress before/after, completed-objective duplicate rejection. |
| `1200000004` | `6313` | `335` | Koryn Surne's Datachron | Loot-window or immediate grant behavior, abandonment/decline result, objective integration. |
| `1200000005` | `21278` | `1221` | Conduit Core | Probability/cadence evidence across repeated eligible attempts, inactive/completed objective rejection. |

## Current Safe Runtime Boundary

| Surface | Current behavior to verify before widening |
| --- | --- |
| `QuestObjectiveActive` condition `8` | Runtime checks `player.QuestManager.IsActiveObjectiveId(condition)`. |
| `LootItemType.VirtualItem` type `6` | Runtime calls `QuestManager.ObjectiveUpdate(QuestObjectiveType.VirtualCollect, StaticId, Amount)`. |
| DataMapping shifted ids | `loot_group` ids are shifted by `+1200000000`; every child reference must stay resolved. |

## Useful Commands

~~~text
!character level 50
!character save
!teleport coordinates <x> <y> <z> [worldId]
~~~
'@

    $questVirtualLootTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-055-quest-virtual-loot-targets.md') -Encoding UTF8
}

if ($RidersReefSmoke) {
    $ridersReefTargets = @'
# F-023 Rider's Reef Exile/Dominion Smoke Targets

Use this worksheet with `CURRENT_STATUS.md`,
`Decomp/Analysis/MISSING_FEATURE_MATRIX.md`, and
`Decomp/Analysis/coverage/RIDERS_REEF_EVIDENCE_MATRIX_2026-05-25.md`.
Do not promote remaining NPE/Rider's Reef behavior without client-visible smoke
for both faction flows and terminal handoff into the expected starter
destinations.

## Required Evidence

| Field | Value |
| --- | --- |
| Account/character/faction | |
| New character creation timestamp | |
| Start world and coordinates | |
| Quest ids accepted/completed | |
| Movement/projector/hoverboard objective evidence | |
| Mine warning/combat loop evidence | |
| Reward UI/toast evidence | |
| Respawn/re-entry/logout recovery evidence | |
| CSI / exact pad pose / visual-SFX notes | |
| Final terminal creature id and faction route | |
| Destination world and welcome quest evidence | |
| Server log or packet evidence file | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Target Sequence

| Step | Expected smoke proof |
| --- | --- |
| Create Exile and Dominion novice characters | Confirm build-16042 flow enters Rider's Reef (`worldId=3460`) and grants only the expected initial tutorial chain. |
| Opening movement and projector sequence | Capture the "three points -> platform -> projector" beat without mid-play `ServerQuestInit` refresh or duplicate root-skip behavior. |
| Hoverboard course | Verify ring lightning/booster visuals, trail persistence, remount recovery, finish snap, and no movement-stranding disable state. |
| Mine/combat/projector follow-up | Verify mine warning timing, combat objective progress, projector activation fallback, reward UI/toasts, and respawn behavior. |
| Logout/re-entry after partial progress | Confirm recovered quest chain and terminal state without duplicate rewards or skipped visible steps. |
| Final departure terminal | Verify Exile/Dominion terminal routing and final quest completion before visible surface receiver. |
| Starter-zone handoff | Verify Everstar Grove, Northern Wilds, Crimson Isle, and Levian Bay handoff/welcome quest behavior for the appropriate faction route. |

## Current Safe Runtime Boundary

| Surface | Current behavior to verify before widening |
| --- | --- |
| Initial quest chain | Fresh entry grants only the expected starting quests; movement recovery grants follow-up quests only after completed movement. |
| Hoverboard visuals | Table-backed ring/booster visual casts exist, but exact `84387` producer/use and SFX remain blocked. |
| Mine warning | Danger-zone `Spell4.CastTime` controls the warning window; exact native visual/SFX timing still needs client proof. |
| Final terminal | Current code has terminal routing and fallback handling; destination welcome quest rows still need client-visible proof. |

## Useful Commands

~~~text
!character level 6
!character save
!teleport coordinates <x> <y> <z> 3460
~~~
'@

    $ridersReefTargets | Set-Content -Path (Join-Path $bundleDirectory 'f-023-riders-reef-smoke-targets.md') -Encoding UTF8
}

if ($FortuneRewardsSmoke) {
    $fortuneRewardsTargets = @'
# LWS-066 Fortune Rewards And Rotation Smoke Targets

Use this worksheet with `Decomp/Analysis/FORTUNE_WEIGHT_AUDIT.md` and
`Decomp/Analysis/LAUGHINGWS_PATH_FORTUNE_BLOCKER_REVIEW.md`. Do not replace the
current rarity-tier emulator weights unless this bundle proves per-item retail
probabilities or a storefront/catalog rotation source for the same item ids.

## Required Evidence

| Field | Value |
| --- | --- |
| Account/character | |
| FortuneCoin/currency state before action | |
| Active rotation or storefront context | |
| `ServerFortuneRewards` packet/log evidence file | |
| Reward item ids and probabilities observed | |
| Card ids/rarities dealt | |
| Payout item and account-inventory evidence | |
| Reload/session persistence evidence | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Target Sequence

| Step | Expected smoke proof |
| --- | --- |
| Open Fortune UI / storefront | Capture `ServerFortuneRewards` item ids and `fProbability` values, plus any storefront/server response that identifies the active rotation. |
| Start Fortune with enough `FortuneCoin` | Confirm the cost, `ServerFortuneCards` deal, card rarity/id payloads, and account currency debit. |
| Flip each card path in separate sessions | Capture `ServerFortuneCardUpdate`, payout item, inventory/account item result, and repeated-flip rejection behavior. |
| Reload during partial session | Confirm `account_fortune_session` restores or resets exactly as observed before claiming persistence parity. |
| Insufficient currency / invalid flip | Confirm no payout and capture reset/rejection packet/log behavior. |

## Current Safe Runtime Boundary

| Surface | Current behavior to verify before widening |
| --- | --- |
| `ServerFortuneRewards.RewardItemProbabilities` | Emulator sends table-audited rarity-tier probabilities, not retail per-item weights. |
| Branch gacha arrays | Rejected as superseded/non-evidence for retail rotations. |
| `account_fortune_session` | Current code persists account-scoped card state; exact reload parity still needs smoke proof. |

## Useful Commands

~~~text
!character level 50
!currency account add FortuneCoin 10
!character save
~~~
'@

    $fortuneRewardsTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-066-fortune-rewards-targets.md') -Encoding UTF8
}

if ($PublicEventVoteScoreboardSmoke) {
    $publicEventVoteScoreboardTargets = @'
# LWS-071/LWS-072 Public-Event Vote And Scoreboard Smoke Targets

Use this worksheet with
`Decomp/Analysis/LAUGHINGWS_SHARED_PUBLIC_EVENT_BLOCKER_REVIEW.md`. Do not widen
vote lifecycle, scoreboard subscription cadence, reward delivery, or result
ordering until this bundle proves the exact client-visible packet/UI behavior.

## Required Evidence

| Field | Value |
| --- | --- |
| Account/characters | |
| Public event id and world id | |
| Public event team id(s) | |
| Vote id, default choice, and duration | |
| Vote initiate/detailed-initiate packet/log evidence | |
| Client choice packet/log evidence | |
| Tally/end packet ordering evidence | |
| Timeout/default-choice evidence | |
| Scoreboard subscribe/unsubscribe packet/log evidence | |
| Stats row and reward-threshold evidence | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Target Sequence

| Step | Expected smoke proof |
| --- | --- |
| Start a public-event vote | Capture initiate/detailed-initiate fields, target audience, elapsed time, vote id, team id, and available choices. |
| Submit valid choices from all participants | Capture each `ClientPublicEventVote`, tally ordering, end/result ordering, and whether result packets differ by team or participant. |
| Let a vote time out | Capture default-choice behavior, exact timeout cadence, and whether tally packets are emitted before end. |
| Send invalid/duplicate/late/non-participant votes | Capture rejection or ignored behavior without extra tally/result mutation. |
| Subscribe to scoreboard | Capture `ClientPublicEventRequestScoreboard`, first `ServerPublicEventStatsUpdate`, stat masks/values, team rows, participant rows, and live update cadence if any. |
| Unsubscribe or request as non-participant | Capture absence or presence of further stats updates and any rejection/log behavior. |
| Finish the event | Capture `ServerPublicEventEnd`, reward tier/type/threshold rows, result reason, elapsed time, and any item/currency delivery. |

## Current Safe Runtime Boundary

| Surface | Current behavior to verify before widening |
| --- | --- |
| Public-event votes | Current code handles known local `PublicEventVote` state idempotently, but retail UI sequence, timeout cadence, result order, vote/team mismatch, and default-choice semantics remain blocked. |
| Scoreboard subscribe | Current handler emits one participant-scoped snapshot for subscribed members; live cadence and unsubscribe behavior remain blocked. |
| Event rewards | Current shared reward delivery is not widened; reward tier/type semantics need packet or smoke proof. |

## Useful Commands

~~~text
!character level 50
!character save
!teleport coordinates <x> <y> <z> [worldId]
~~~
'@

    $publicEventVoteScoreboardTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-071-072-public-event-vote-scoreboard-targets.md') -Encoding UTF8
}

if ($PublicEventObjectiveNotificationSmoke) {
    $publicEventObjectiveNotificationTargets = @'
# LWS-073 Public-Event Objective Notification Smoke Targets

Use this worksheet with
`Decomp/Analysis/LAUGHINGWS_SHARED_PUBLIC_EVENT_BLOCKER_REVIEW.md`. Do not widen
objective notification text, target audience, packet ordering, or quest-share
behavior until this bundle proves the exact client-visible packet/UI behavior.

## Required Evidence

| Field | Value |
| --- | --- |
| Account/characters | |
| Public event id and world id | |
| Objective id(s) and phase id(s) | |
| Objective start packet/log evidence (`0x06F9`) | |
| Objective update packet/log evidence (`0x0132`) | |
| Objective status update packet/log evidence (`0x0134`) | |
| Objective notification mode packet/log evidence (`0x0133`) | |
| Objective/phase text ids or UI text evidence | |
| Quest-share or quest-objective packet/log evidence, if present | |
| Target audience evidence (participant, team, nearby, non-participant) | |
| Packet ordering and duplicate-suppression evidence | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Target Sequence

| Step | Expected smoke proof |
| --- | --- |
| Join or start a public event | Capture objective start and full objective update order for each newly active objective. |
| Advance a phase or objective | Capture status update, full objective update, notification mode, UI text, and whether packets are participant-only or wider audience. |
| Complete an objective | Capture completion notification order, target audience, duplicate behavior, and any quest-share or quest-objective side effects. |
| Repeat the same trigger/status transition | Capture whether duplicate start/update/status/notification packets are suppressed or re-emitted. |
| Observe as a non-participant or wrong-team player | Capture absence or presence of objective notifications and text. |
| Finish or abandon the event | Capture whether later notification mode changes are ignored, rejected, or still emitted. |

## Current Safe Runtime Boundary

| Surface | Current behavior to verify before widening |
| --- | --- |
| Objective notification packets | Packet writers are pinned to decompile-mapped shapes for opcodes `0x06F9`, `0x0132`, `0x0133`, and `0x0134`; exact emission order and audience remain blocked. |
| Objective/phase text | Text ids and UI display rules are not inferred from branch SQL alone. |
| Quest-share behavior | Quest-share and quest-objective side effects remain blocked until packet/log proof maps when they are emitted. |

## Useful Commands

~~~text
!character level 50
!character save
!teleport coordinates <x> <y> <z> [worldId]
~~~
'@

    $publicEventObjectiveNotificationTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-073-public-event-objective-notification-targets.md') -Encoding UTF8
}

if ($PvpAdventureSmoke) {
    $pvpAdventureTargets = @'
# LWS-080 Through LWS-085 PvP And Adventure Smoke Targets

Use this worksheet with `Decomp/Analysis/LAUGHINGWS_PVP_ADVENTURE_SMOKE_REVIEW.md`.
The current PvP/adventure scripts are WIP-guessed scaffolds. Do not widen queue,
match, scoring, reward, stat, faction-start, vehicle, or objective behavior
until this bundle proves the concrete runtime surface.

## Required Evidence

| Field | Value |
| --- | --- |
| Account/characters and teams | |
| World id and instance/match id | |
| Public event ids observed | |
| Queue/match start packet/log evidence | |
| Scoreboard/stat packet/log evidence | |
| Objective/capture/round evidence | |
| End/result/reward evidence | |
| PvP stat/deserter/leave evidence | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Target Content

| LWS | Content | World | Public events | Required smoke proof |
| --- | --- | ---: | --- | --- |
| LWS-080 | Cryo-Plex arena | `3022` | `581`, `582` | Queue/match entry, team spawn, round/score flow, forcefield/resurrection timing, PvP stats, rewards, and leave/disconnect cleanup. |
| LWS-081 | Daggerstone Pass | `2166` | `438`, `466` | Queue/start/end, capture/round objectives, scoring, scoreboard, rewards, and wrong-team objective rejection. |
| LWS-082 | Halls of the Bloodsworn | `3449` | `876`, `877` | Queue/start/end, objective flow, scoring, scoreboard, rewards, and abandon/early-finish behavior. |
| LWS-083 | Walatiki Temple | `797` | `217`, `366` | Mask capture/return/scoring, round end, scoreboard, rewards, and wrong-team/wrong-phase interaction behavior. |
| LWS-084 | War of the Wilds | `1393` | `158`, `170`, `171` | Faction-specific start events, giant Moodie totem objectives, end-delay behavior, chat timing, rewards, and adventure smoke. |
| LWS-085 | Rage Logic | `1627` | `213` | Vehicle choice, objective routing after `ChooseAVehicle`, encounter flow, rewards, and wrong/late vehicle interaction behavior. |

## Current Safe Runtime Boundary

| Surface | Current behavior to verify before widening |
| --- | --- |
| PvP map bindings | Daggerstone Pass, Halls of the Bloodsworn, and Walatiki Temple have map-only event binding scaffolds. |
| Cryo-Plex | Current branch-derived arena phase/death/resurrection scaffold is WIP-guessed and test-covered, not full arena parity. |
| War of the Wilds | Current scaffold activates the base public event `158`; faction start events `170`/`171` are still proof-gated. |
| Rage Logic | Current scaffold stops at the branch `ChooseAVehicle` phase until vehicle/objective routing is proven. |

## Useful Commands

~~~text
!character level 50
!character save
!teleport coordinates <x> <y> <z> [worldId]
~~~
'@

    $pvpAdventureTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-080-085-pvp-adventure-targets.md') -Encoding UTF8
}

if ($ExpeditionSmoke) {
    $expeditionTargets = @'
# LWS-090 Through LWS-096 Expedition Smoke Targets

Use this worksheet with `Decomp/Analysis/LAUGHINGWS_EXPEDITION_SMOKE_REVIEW.md`.
The current expedition scripts are WIP-guessed scaffolds. Do not widen shuttle,
door, trigger, cinematic, communicator, teleport, cleanup, encounter, routing, or
reward behavior until this bundle proves the concrete runtime surface.

## Required Evidence

| Field | Value |
| --- | --- |
| Account/character | |
| World id and instance id | |
| Public event id | |
| Phase/objective state before action | |
| Door/shuttle/trigger/teleport evidence | |
| Cinematic/communicator evidence | |
| Combat/spawn/cleanup evidence | |
| End/result/reward evidence | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Target Content

| LWS | Content | World | Public event | Required smoke proof |
| --- | --- | ---: | ---: | --- |
| LWS-090 | Outpost M-13 | `1319` | `108` | Real shuttle world-location trigger or proof that branch id `5` stays rejected; shuttle return, cleanup, and reward behavior. |
| LWS-091 | Space Madness | `2149` | `390` | Branch door ids, direct local teleport, airlock/research trigger timing, real cinematic payload, combat/spawn behavior, and full route smoke. |
| LWS-092 | Fragment Zero | `3180` | `680` | Trigger timing, real cinematic payload, communicator timing, door behavior, entity cleanup, and completion behavior. |
| LWS-093 | Gauntlet | `2183` | `446` | Cinematics, announcer/communicator timing, arena doors, exact trigger timing, and full route smoke. |
| LWS-094 | Infestation | `1232` | `95` | Cargo-ship turnstile placement/range, door/open-vent choreography, real cinematic payload, medical-bay attack timing, parasite objective activation, and full route smoke. |
| LWS-095 | Evil from the Ether | `3404` | `781` | Exact trigger rows/coordinates, direct medbay transition, door/marker cleanup, crew-log targeting/order, drive-spark visuals, Katja choreography, boss cadence/mechanics, cinematics, rewards, and residual no-hook placement review. |
| LWS-096 | Deep Space Exploration | `2188` | `447` | Follow-up expedition routing, doors, real cinematic payload, encounter order, rewards, and full route smoke. |

## Current Safe Runtime Boundary

| Surface | Current behavior to verify before widening |
| --- | --- |
| Outpost M-13 shuttle | Branch world-location id `5` is zero-vector and remains rejected until real trigger proof exists. |
| Completion-only cinematics | Current placeholders avoid claiming actor/camera/text/timing parity; replace only with real payload proof. |
| WIP triggers and doors | Current trigger/door scripts are branch-derived scaffolds and require exact coordinate/range/state proof before broadening. |
| Expedition rewards | Shared reward delivery and medal/result behavior remain blocked pending route-end proof. |

## Useful Commands

~~~text
!character level 50
!character save
!teleport coordinates <x> <y> <z> [worldId]
~~~
'@

    $expeditionTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-090-096-expedition-targets.md') -Encoding UTF8
}

if ($DungeonSmoke) {
    $dungeonTargets = @'
# LWS-100 Through LWS-106 Dungeon Smoke Targets

Use this worksheet with `Decomp/Analysis/LAUGHINGWS_DUNGEON_SMOKE_REVIEW.md`.
The current dungeon scripts are WIP-guessed scaffolds. Do not widen route,
trigger, door, platform, launcher, communicator, cinematic, boss, cleanup, or
reward behavior until this bundle proves the concrete runtime surface.

## Required Evidence

| Field | Value |
| --- | --- |
| Account/character | |
| World id and instance id | |
| Public event id | |
| Route/optional-objective selection evidence | |
| Phase/objective state before action | |
| Trigger/door/platform/launcher evidence | |
| Cinematic/communicator evidence | |
| Boss/combat/cadence evidence | |
| Cleanup/end/result/reward evidence | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Target Content

| LWS | Content | World | Public event | Required smoke proof |
| --- | --- | ---: | ---: | --- |
| LWS-100 | Coldblood Citadel | `3522` | `907` | Optional route weights, objective trigger placement, door choreography, Hailstone ability cadence, gather-ring cleanup, and full route smoke. |
| LWS-101 | Protogames Academy | `3173` | `667` | Invulnotron, Gromka, Iruki Boldbeard, Seek-N-Slaughter, Icebox Mk. 2 mechanics, trigger timing, communicator timing, platform/launcher cleanup, and entity-removal choreography. |
| LWS-102 | Ruins of Kel Voreth | `1336` | `161` | Boss mechanics, trigger placement, door choreography, optional route weights/objective availability, faction-specific communicator targeting, real Blood Pit cinematic payload, and duplicate-owner Forgemaster/Drokk trigger split. |
| LWS-103 | Stormtalon's Lair | `382` | `145` | Boss combat, trigger placement/count, real Stormtalon reborn cinematic payload, optional route weights, objective availability, and boss spawn/version selection. |
| LWS-104 | Skullcano | `1263` | `148` | Boss mechanics, cave/chasm route weights, Chief Kaskalak choreography, trigger placement, door/platform choreography, communicator timing/faction pairing, optional objective availability, and full route smoke. |
| LWS-105 | Sanctuary of the Swordmaiden | `1271` | `166` | Miniboss mechanics, Temple vs Moldwood route weights, objective availability, trigger placement, door choreography, communicator timing, and full route smoke. |
| LWS-106 | Ultimate Protogames dungeon | `2980` | `594` | Room randomization, objective routing beyond `RandomEvent1`, boss mechanics, rewards, and full route smoke. |

## Current Safe Runtime Boundary

| Surface | Current behavior to verify before widening |
| --- | --- |
| Optional routes | Current route/optional-objective selection is WIP-guessed and test-covered, not retail parity. |
| Doors, platforms, launchers, and triggers | Current scripts only cover branch-derived scaffold hooks; exact coordinates, radii, timing, and cleanup remain proof-gated. |
| Completion-only cinematics | Current placeholders avoid claiming actor/camera/text/timing parity; replace only with real payload proof. |
| Dungeon boss mechanics and rewards | Objective-credit hooks and coarse completion scaffolds are not enough proof for combat cadence, challenge semantics, result screens, or reward delivery. |

## Useful Commands

~~~text
!character level 50
!character save
!teleport coordinates <x> <y> <z> [worldId]
~~~
'@

    $dungeonTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-100-106-dungeon-targets.md') -Encoding UTF8
}

if ($RaidEventSmoke) {
    $raidEventTargets = @'
# LWS-110 Through LWS-117 Raid And Event-Instance Smoke Targets

Use this worksheet with `Decomp/Analysis/LAUGHINGWS_RAID_EVENT_SMOKE_REVIEW.md`.
The current raid and event-instance scripts are WIP-guessed scaffolds. Do not
widen route, wing, room, trigger, door, elevator, town-gate, communicator,
cinematic, encounter, cleanup, or reward behavior until this bundle proves the
concrete runtime surface.

## Required Evidence

| Field | Value |
| --- | --- |
| Account/character or group | |
| World id and instance id | |
| Public event id | |
| Route/wing/room/weekly selection evidence | |
| Phase/objective state before action | |
| Trigger/target-set/door/elevator/gate evidence | |
| Cinematic/communicator evidence | |
| Encounter/boss/challenge evidence | |
| Cleanup/end/result/reward evidence | |
| Screenshot/video evidence file | |
| Negative case evidence | |
| Outcome: implemented, rejected, or still blocked | |

## Target Content

| LWS | Content | World | Public event | Required smoke proof |
| --- | --- | ---: | ---: | --- |
| LWS-110 | Initialization Core Y-83 | `3040` | `595` | Quarantine/corridor trigger placement, door entity choreography, communicator/cinematic timing, target-set proof, real open-door cinematic payload, and raid smoke. |
| LWS-111 | Red Moon Terror | `3032` | `705` | Ish'amel/engineering timing, Laveka choreography, awakening/challenge mechanics, door/elevator movement, and raid smoke. |
| LWS-112 | Genetic Archives | `1462` | `159` | Experiment X-89, Kuralak, Kuralak pillar, Ohmna, weekly/random encounter selection, boss choreography, communicator and cinematic timing, door/elevator movement, and raid smoke. |
| LWS-113 | Datascape | `1333` | `157` | Hydroflux and Mnemesis mechanics, communicator and cinematic timing, encounter choreography, door/trigger placement, exact wing-order proof, challenge mechanics, and raid smoke. |
| LWS-115 | Shade's Eve | `3044` | `597` | Gather-ring cleanup, town-gate opening, communicator/cinematic timing, real cinematic payload, exact vote follow-up, Etty/fountain seed interaction smoke, and event smoke. |
| LWS-116 | Protostar SuperMall in the Sky | `3094` | `679` | Store/room routing, encounter logic, real cinematic payload, rewards, and full route smoke. |
| LWS-117 | Journey into OMNICore-1 | `3045` | `605` | Real cinematic actor/camera/text/timing payload, event routing, rewards, and encounter behavior before replacing the immediate completion placeholder. |

## Current Safe Runtime Boundary

| Surface | Current behavior to verify before widening |
| --- | --- |
| Raid target sets and doors | Current scripts only cover branch-derived scaffolds; exact target membership, coordinates, timing, and cleanup remain proof-gated. |
| Random/weekly/wing/room routing | Current route selection is WIP-guessed or intentionally coarse; broad routing needs completed smoke or decompile-backed proof. |
| Completion-only cinematics | Current placeholders avoid claiming actor/camera/text/timing parity; replace only with real payload proof. |
| Encounter mechanics and rewards | Objective-credit hooks and coarse completion scaffolds are not enough proof for boss cadence, challenge semantics, result screens, or reward delivery. |

## Useful Commands

~~~text
!character level 50
!character save
!teleport coordinates <x> <y> <z> [worldId]
~~~
'@

    $raidEventTargets | Set-Content -Path (Join-Path $bundleDirectory 'lws-110-117-raid-event-targets.md') -Encoding UTF8
}

function Resolve-ClientLogRoot {
    param(
        [string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }

    $resolvedPath = $Path
    if (Test-Path -LiteralPath $Path) {
        $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    }

    if ((Split-Path -Leaf $resolvedPath) -ieq 'Client64') {
        return (Split-Path -Parent $resolvedPath)
    }

    if (Test-Path -LiteralPath (Join-Path $resolvedPath 'Client64')) {
        return $resolvedPath
    }

    return (Split-Path -Parent $resolvedPath)
}

$clientLogRoot = Resolve-ClientLogRoot -Path $ClientDirectory
$tailScript = @"
`$RepoRoot = '$RepoRoot'
`$ClientLogRoot = '$clientLogRoot'
`$RuntimeWorldLog = Get-ChildItem -Path (Join-Path `$RepoRoot '.nexusforever-runtime\logs') -Filter 'NexusForever.WorldServer*.stdout.log' -File -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1
if (`$RuntimeWorldLog) { Get-Content -Wait -Tail 200 -LiteralPath `$RuntimeWorldLog.FullName }
# Additional useful tails:
# Get-Content -Wait -Tail 200 (Join-Path `$RepoRoot 'Source\NexusForever.WorldServer\bin\Debug\net10.0\logs\NexusForever.WorldServer_*.log')
# Get-Content -Wait -Tail 200 (Join-Path `$RepoRoot '.nexusforever-runtime\logs\NexusForever.Server.GroupServer.stdout.log')
# Get-Content -Wait -Tail 200 (Join-Path `$RepoRoot '.nexusforever-runtime\logs\NexusForever.Server.Friendship.stdout.log')
# if (![string]::IsNullOrWhiteSpace(`$ClientLogRoot)) { Get-Content -Wait -Tail 200 (Join-Path `$ClientLogRoot 'Logs\*.txt') }
# if (![string]::IsNullOrWhiteSpace(`$ClientLogRoot)) { Get-Content -Wait -Tail 200 (Join-Path `$ClientLogRoot 'Errors\WildStar64*.log') }
"@
$tailScript | Set-Content -Path (Join-Path $bundleDirectory 'Tail-BlockerEvidenceLogs.ps1') -Encoding UTF8

$collectScript = @"
param(
    [string] `$RepoRoot = '$RepoRoot',
    [string] `$BundleDirectory = '$bundleDirectory'
)

`$ErrorActionPreference = 'Stop'
`$logsDirectory = Join-Path `$BundleDirectory 'logs'
New-Item -ItemType Directory -Force -Path `$logsDirectory | Out-Null
`$ClientLogRoot = '$clientLogRoot'
`$EvidenceStartedAtUtc = [DateTimeOffset]::Parse('$($manifest.createdAt)').UtcDateTime
`$EvidenceWindowStartUtc = `$EvidenceStartedAtUtc.AddMinutes(-1)
`$runtimeLogRoot = Join-Path `$RepoRoot '.nexusforever-runtime\logs'

`$runtimeLogPatterns = @(
    'NexusForever.WorldServer*.stdout.log',
    'NexusForever.Server.GroupServer*.stdout.log',
    'NexusForever.Server.Friendship*.stdout.log'
)

foreach (`$pattern in `$runtimeLogPatterns) {
    Get-ChildItem -Path `$runtimeLogRoot -Filter `$pattern -File -ErrorAction SilentlyContinue |
        Where-Object { `$_.CreationTimeUtc -ge `$EvidenceWindowStartUtc -or `$_.LastWriteTimeUtc -ge `$EvidenceWindowStartUtc } |
        ForEach-Object {
            Copy-Item -LiteralPath `$_.FullName -Destination (Join-Path `$logsDirectory `$_.Name) -Force
        }
}

`$worldServerLog = Get-ChildItem -Path (Join-Path `$RepoRoot 'Source\NexusForever.WorldServer\bin\Debug\net10.0\logs') -Filter 'NexusForever.WorldServer_*.log' -File -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1
if (`$worldServerLog) {
    Copy-Item -LiteralPath `$worldServerLog.FullName -Destination (Join-Path `$logsDirectory `$worldServerLog.Name) -Force
}

if (![string]::IsNullOrWhiteSpace(`$ClientLogRoot)) {
    foreach (`$pattern in @('Logs\*.txt', 'Errors\WildStar64*.log', 'Errors\WildStar64*.txt')) {
        Get-ChildItem -Path (Join-Path `$ClientLogRoot `$pattern) -ErrorAction SilentlyContinue |
            Where-Object { `$_.CreationTimeUtc -ge `$EvidenceWindowStartUtc -or `$_.LastWriteTimeUtc -ge `$EvidenceWindowStartUtc } |
            ForEach-Object {
                Copy-Item -LiteralPath `$_.FullName -Destination (Join-Path `$logsDirectory `$_.Name) -Force
            }
    }
}

Write-Host "Collected logs into `$logsDirectory"
"@
$collectScript | Set-Content -Path (Join-Path $bundleDirectory 'Collect-BlockerEvidenceBundle.ps1') -Encoding UTF8

Write-Host 'Starting blocker evidence harness (Trace server logging, client console and CLog enabled).' -ForegroundColor Cyan
Write-Host "Plan: Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md" -ForegroundColor DarkGray
Write-Host "Client logging: Decomp/Analysis/CLIENT_LOGGING.md" -ForegroundColor DarkGray
Write-Host "Evidence bundle: $bundleDirectory" -ForegroundColor Green

if ($CreateBundleOnly) {
    Write-Host 'CreateBundleOnly was supplied; skipping server/client launch.' -ForegroundColor Yellow
} else {
    & $restartScript `
        -RepoRoot $RepoRoot `
        -ClientDirectory $ClientDirectory `
        -EnableClientConsole `
        -EnableClientLogging `
        -LogLevel $LogLevel `
        -PromptForRootPassword:$PromptForRootPassword `
        -SkipServerLaunch:$SkipServerLaunch `
        -SkipClientLaunch:$SkipClientLaunch
}

Write-Host ''
Write-Host 'Evidence log tails (run in separate terminals):' -ForegroundColor Yellow
Write-Host "  Get-Content -Wait -Tail 200 ((Get-ChildItem -Path '$(Join-Path $RepoRoot '.nexusforever-runtime\logs')' -Filter 'NexusForever.WorldServer*.stdout.log' -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).FullName)"
Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $RepoRoot 'Source\NexusForever.WorldServer\bin\Debug\net10.0\logs\NexusForever.WorldServer_*.log')"
Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $RepoRoot '.nexusforever-runtime\logs\NexusForever.Server.GroupServer.stdout.log')"
Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $RepoRoot '.nexusforever-runtime\logs\NexusForever.Server.Friendship.stdout.log')"
if (![string]::IsNullOrWhiteSpace($clientLogRoot)) {
    Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $clientLogRoot 'Logs\*.txt')"
    Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $clientLogRoot 'Errors\WildStar64*.log')"
}
Write-Host ''
Write-Host 'Bundle helpers:' -ForegroundColor Yellow
Write-Host "  $(Join-Path $bundleDirectory 'Tail-BlockerEvidenceLogs.ps1')"
Write-Host "  $(Join-Path $bundleDirectory 'Collect-BlockerEvidenceBundle.ps1')"
