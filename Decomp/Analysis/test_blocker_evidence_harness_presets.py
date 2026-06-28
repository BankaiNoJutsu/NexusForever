import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


class BlockerEvidenceHarnessPresetTests(unittest.TestCase):
    def test_client_log_tail_uses_install_root_when_client_directory_contains_client64(self) -> None:
        repo_root = _find_repo_root()
        script = repo_root / "Decomp" / "Analysis" / "Start-BlockerEvidenceHarness.ps1"
        output_root = Path(tempfile.mkdtemp(prefix="nf-blocker-harness-"))
        client_root = Path(tempfile.mkdtemp(prefix="nf-wildstar-root-"))
        try:
            (client_root / "Client64").mkdir()

            command = [
                "powershell",
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                str(script),
                "-RepoRoot",
                str(repo_root),
                "-BundleName",
                "client-log-root",
                "-ClientDirectory",
                str(client_root),
                "-CreateBundleOnly",
                "-OutputRoot",
                str(output_root),
            ]
            result = subprocess.run(command, cwd=repo_root, check=True, capture_output=True, text=True)
            bundles = [path for path in output_root.iterdir() if path.is_dir()]

            self.assertEqual(1, len(bundles))
            self.assertIn(str(client_root / "Logs" / "*.txt"), result.stdout)
            self.assertIn(str(client_root / "Errors" / "WildStar64*.log"), result.stdout)

            tail_script = (bundles[0] / "Tail-BlockerEvidenceLogs.ps1").read_text(encoding="utf-8-sig")
            collect_script = (bundles[0] / "Collect-BlockerEvidenceBundle.ps1").read_text(encoding="utf-8-sig")
            self.assertIn(str(client_root), tail_script)
            self.assertIn(str(client_root), collect_script)
        finally:
            shutil.rmtree(output_root, ignore_errors=True)
            shutil.rmtree(client_root, ignore_errors=True)

    def test_worlddb_special_blocker_presets_create_required_bundles(self) -> None:
        repo_root = _find_repo_root()
        script = repo_root / "Decomp" / "Analysis" / "Start-BlockerEvidenceHarness.ps1"
        presets = [
            (
                "Lws036ChecklistSmoke",
                "LWS-036-city-checklist-smoke",
                "lws-036-targets.md",
                [22, 51, 426, 870, 990, 1387, 2180, 3460],
                [],
                ["Ringo Hax", "housing intro"],
                [],
                3,
            ),
            (
                "NewZoneAssetProof",
                "new-zone-asset-proof",
                "new-zone-asset-proof-targets.md",
                [22, 51, 1068],
                [],
                ["Dreadmoor", "Sandbox/test"],
                [],
                3,
            ),
            (
                "DustStalkerQ4516Smoke",
                "LWS-051-dust-stalker-q4516",
                "lws-051-dust-stalker-targets.md",
                [1138],
                [7633, 6189, 7637, 7638, 7639],
                ["premature bridge", "self-destruct"],
            ),
            (
                "ArcterraPalaverSourceSmoke",
                "LWS-052-arcterra-palaver-source-only",
                "lws-052-arcterra-palaver-targets.md",
                [3335, 3519],
                [21469, 21470, 21477, 21478, 21485, 21486, 21487, 21488],
                ["Arcterra Coldblood Citadel portal", "Palaver"],
            ),
            (
                "SkyplotHousingSmoke",
                "LWS-053-skyplot-housing",
                "lws-053-skyplot-housing-targets.md",
                [1229],
                [],
                ["return pad", "catalog open result"],
            ),
            (
                "LiveEventSmoke",
                "LWS-054-live-events",
                "lws-054-live-event-targets.md",
                [],
                [],
                ["Battle Chase", "cleanup"],
            ),
            (
                "QuestVirtualLootSmoke",
                "LWS-055-quest-virtual-loot",
                "lws-055-quest-virtual-loot-targets.md",
                [],
                [13011, 12605, 6700, 6313, 21278],
                ["Quest Virtual Loot", "probability"],
            ),
            (
                "RidersReefSmoke",
                "F-023-riders-reef-smoke",
                "f-023-riders-reef-smoke-targets.md",
                [3460, 51, 870, 990, 1387],
                [],
                ["Rider's Reef", "terminal handoff"],
                [],
                5,
            ),
            (
                "FortuneRewardsSmoke",
                "LWS-066-fortune-rewards",
                "lws-066-fortune-rewards-targets.md",
                [],
                [],
                ["ServerFortuneRewards", "account_fortune_session"],
            ),
            (
                "PublicEventVoteScoreboardSmoke",
                "LWS-071-072-public-event-vote-scoreboard",
                "lws-071-072-public-event-vote-scoreboard-targets.md",
                [],
                [],
                ["Public-Event Vote", "scoreboard"],
            ),
            (
                "PublicEventObjectiveNotificationSmoke",
                "LWS-073-public-event-objective-notification",
                "lws-073-public-event-objective-notification-targets.md",
                [],
                [],
                ["Objective Notification", "notification mode"],
            ),
            (
                "PvpAdventureSmoke",
                "LWS-080-085-pvp-adventure",
                "lws-080-085-pvp-adventure-targets.md",
                [797, 1393, 1627, 2166, 3022, 3449],
                [],
                ["Cryo-Plex", "Rage Logic"],
                [158, 170, 171, 213, 217, 366, 438, 466, 581, 582, 876, 877],
            ),
            (
                "ExpeditionSmoke",
                "LWS-090-096-expeditions",
                "lws-090-096-expedition-targets.md",
                [1232, 1319, 2149, 2183, 2188, 3180, 3404],
                [],
                ["Outpost M-13", "Deep Space Exploration"],
                [95, 108, 390, 446, 447, 680, 781],
            ),
            (
                "DungeonSmoke",
                "LWS-100-106-dungeons",
                "lws-100-106-dungeon-targets.md",
                [382, 1263, 1271, 1336, 2980, 3173, 3522],
                [],
                ["Coldblood Citadel", "Ultimate Protogames"],
                [145, 148, 161, 166, 594, 667, 907],
            ),
            (
                "RaidEventSmoke",
                "LWS-110-117-raid-event-instances",
                "lws-110-117-raid-event-targets.md",
                [1333, 1462, 3032, 3040, 3044, 3045, 3094],
                [],
                ["Datascape", "Journey into OMNICore-1"],
                [157, 159, 595, 597, 605, 679, 705],
            ),
        ]

        for preset_data in presets:
            preset, bundle_name, target_file, world_ids, objective_ids, target_markers, *optional = preset_data
            public_event_ids = optional[0] if optional else []
            min_negative_cases = optional[1] if len(optional) > 1 else 4
            with self.subTest(preset=preset):
                bundle = _create_bundle(repo_root, script, preset)
                try:
                    manifest = json.loads((bundle / "manifest.json").read_text(encoding="utf-8-sig"))
                    target_text = (bundle / target_file).read_text(encoding="utf-8-sig")

                    self.assertEqual(bundle_name, manifest["bundleName"])
                    self.assertEqual(world_ids, manifest["worldIds"])
                    self.assertEqual(public_event_ids, manifest["publicEventIds"])
                    self.assertEqual(objective_ids, manifest["objectiveIds"])
                    self.assertIn(target_file, manifest["requiredFiles"])
                    self.assertGreaterEqual(len(manifest["negativeCases"]), min_negative_cases)
                    self.assertTrue((bundle / "negative-cases.md").exists())
                    self.assertTrue((bundle / "commands.md").exists())
                    for marker in target_markers:
                        self.assertIn(marker, target_text)
                finally:
                    shutil.rmtree(bundle.parent, ignore_errors=True)


def _create_bundle(repo_root: Path, script: Path, preset: str) -> Path:
    output_root = Path(tempfile.mkdtemp(prefix="nf-blocker-harness-"))
    command = [
        "powershell",
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        str(script),
        "-RepoRoot",
        str(repo_root),
        f"-{preset}",
        "-CreateBundleOnly",
        "-OutputRoot",
        str(output_root),
    ]
    subprocess.run(command, cwd=repo_root, check=True, capture_output=True, text=True)
    bundles = [path for path in output_root.iterdir() if path.is_dir()]
    if len(bundles) != 1:
        shutil.rmtree(output_root, ignore_errors=True)
        raise AssertionError(f"Expected one bundle for {preset}, found {len(bundles)}")
    return bundles[0]


def _find_repo_root() -> Path:
    directory = Path(__file__).resolve()
    for parent in [directory, *directory.parents]:
        if (parent / "Source" / "NexusForever.sln").exists():
            return parent

    raise RuntimeError("Unable to locate NexusForever repository root.")


if __name__ == "__main__":
    unittest.main()
