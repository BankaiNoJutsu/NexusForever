import csv
import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


class DecompCoverageSnapshotTests(unittest.TestCase):
    def test_current_server_opcode_total_distinguishes_named_and_placeholder_models(self) -> None:
        repo_root = _find_repo_root()
        script = repo_root / "Decomp" / "Analysis" / "Get-DecompCoverageSnapshot.ps1"
        fixture_root = Path(tempfile.mkdtemp(prefix="nf-current-coverage-snapshot-"))
        try:
            output_dir = fixture_root / "exports"
            log_dir = fixture_root / "logs"
            coverage_dir = fixture_root / "coverage"
            command = [
                "powershell",
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                str(script),
                "-RepoRoot",
                str(repo_root),
                "-OutputDir",
                str(output_dir),
                "-LogDir",
                str(log_dir),
                "-CoverageDir",
                str(coverage_dir),
                "-OpcodeFile",
                str(repo_root / "Source" / "NexusForever.Network" / "Message" / "GameMessageOpcode.cs"),
                "-SourceDir",
                str(repo_root / "Source"),
            ]
            subprocess.run(command, cwd=repo_root, check=True, capture_output=True, text=True)

            summary = json.loads((log_dir / "LATEST_COVERAGE_SUMMARY.json").read_text(encoding="utf-8-sig"))
            opcode_summary = summary["opcodeSummary"]
            self.assertEqual(713, opcode_summary["serverTotal"])
            self.assertEqual(713, opcode_summary["serverModeled"])
            self.assertEqual(712, opcode_summary["serverNamedModeled"])
            self.assertEqual(1, opcode_summary["serverPlaceholderModeled"])
            self.assertEqual(0, opcode_summary["serverEnumOnly"])
            self.assertEqual(
                ["Server0x0015"],
                [
                    row["opcode"]
                    for row in summary["queues"]["placeholderModels"]
                    if row["direction"] == "Server"
                ],
            )
        finally:
            shutil.rmtree(fixture_root, ignore_errors=True)

    def test_numeric_opcode_names_with_0x_are_tracked_as_placeholder_models(self) -> None:
        repo_root = _find_repo_root()
        script = repo_root / "Decomp" / "Analysis" / "Get-DecompCoverageSnapshot.ps1"
        fixture_root = Path(tempfile.mkdtemp(prefix="nf-coverage-snapshot-"))
        try:
            source_dir = fixture_root / "Source"
            model_dir = source_dir / "NexusForever.Network.World" / "Message" / "Model"
            model_dir.mkdir(parents=True)

            opcode_file = fixture_root / "GameMessageOpcode.cs"
            opcode_file.write_text(
                """
namespace NexusForever.Network.Message
{
    public enum GameMessageOpcode
    {
        Server0x0015 = 0x0015, // unresolved server shape
        Client0x011B = 0x011B, // unresolved client shape
        ServerNamedOpcode = 0x0020, // named server shape
    }
}
""".lstrip(),
                encoding="utf-8",
            )

            (model_dir / "FixturePackets.cs").write_text(
                """
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model;

[Message(GameMessageOpcode.Server0x0015)]
public class Server0x0015 : IWritable
{
}

[Message(GameMessageOpcode.Client0x011B)]
public class Client0x011B : IReadable
{
}

[Message(GameMessageOpcode.ServerNamedOpcode)]
public class ServerNamedOpcode : IWritable
{
}
""".lstrip(),
                encoding="utf-8",
            )

            output_dir = fixture_root / "exports"
            log_dir = fixture_root / "logs"
            coverage_dir = fixture_root / "coverage"
            command = [
                "powershell",
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                str(script),
                "-RepoRoot",
                str(fixture_root),
                "-OutputDir",
                str(output_dir),
                "-LogDir",
                str(log_dir),
                "-CoverageDir",
                str(coverage_dir),
                "-OpcodeFile",
                str(opcode_file),
                "-SourceDir",
                str(source_dir),
            ]
            subprocess.run(command, cwd=repo_root, check=True, capture_output=True, text=True)

            summary = json.loads((log_dir / "LATEST_COVERAGE_SUMMARY.json").read_text(encoding="utf-8-sig"))
            self.assertEqual(2, summary["opcodeSummary"]["placeholderModeled"])
            self.assertEqual(1, summary["opcodeSummary"]["serverPlaceholderModeled"])
            self.assertEqual(1, summary["opcodeSummary"]["serverNamedModeled"])
            self.assertEqual(
                ["Server0x0015", "Client0x011B"],
                [row["opcode"] for row in summary["queues"]["placeholderModels"]],
            )

            with (coverage_dir / "opcode_coverage_inventory.csv").open(encoding="utf-8-sig", newline="") as handle:
                rows = {row["opcode"]: row for row in csv.DictReader(handle)}

            self.assertEqual("placeholder", rows["Server0x0015"]["decodeState"])
            self.assertEqual("True", rows["Server0x0015"]["placeholderModeled"])
            self.assertEqual("placeholder", rows["Client0x011B"]["decodeState"])
            self.assertEqual("True", rows["Client0x011B"]["placeholderModeled"])
            self.assertEqual("named", rows["ServerNamedOpcode"]["decodeState"])
            self.assertEqual("False", rows["ServerNamedOpcode"]["placeholderModeled"])
        finally:
            shutil.rmtree(fixture_root, ignore_errors=True)


def _find_repo_root() -> Path:
    directory = Path(__file__).resolve()
    for parent in [directory, *directory.parents]:
        if (
            (parent / "Source" / "NexusForever.slnx").exists()
            or (parent / "Source" / "NexusForever.sln").exists()
        ):
            return parent

    raise RuntimeError("Unable to locate NexusForever repository root.")


if __name__ == "__main__":
    unittest.main()
