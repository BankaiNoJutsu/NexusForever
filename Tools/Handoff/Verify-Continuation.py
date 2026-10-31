"""Verify the published source and historical checkpoint without old machine PIDs."""
from pathlib import Path
import argparse, hashlib, json, subprocess, xml.etree.ElementTree as ET

parser = argparse.ArgumentParser()
parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[2])
args = parser.parse_args()
repo = args.repo.resolve()
proof = repo / 'Decomp/Analysis/assessment-snapshot/checkpoint10277'
manifest = json.loads((proof / 'portable-source-manifest.json').read_text())
paths = [row['Path'] for row in manifest]
assert len(paths) == len(set(paths)) == 5888
assert all((repo / path).is_file() for path in paths), 'A checkpoint source file is missing.'
actual = subprocess.check_output(['git', 'hash-object', '--stdin-paths'], cwd=repo,
    input='\n'.join(paths) + '\n', text=True, stderr=subprocess.DEVNULL).splitlines()
assert actual == [row['GitBlobSHA1'] for row in manifest], 'Source differs from the verified checkpoint (after Git newline normalization).'
subprocess.run(['git', 'merge-base', '--is-ancestor', '71cde855988446c304341f906556542d6868c412', 'HEAD'], cwd=repo, check=True)
ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
for name, total, passed, failed in [('full/full.trx', 10277, 10277, 0),
    ('focused2/focused.trx', 207, 207, 0), ('baseline/baseline.trx', 6, 0, 6)]:
    counts = ET.parse(proof / name).find('t:ResultSummary/t:Counters', ns).attrib
    assert (int(counts['total']), int(counts['passed']), int(counts['failed'])) == (total, passed, failed), name
verification = json.loads((proof / 'final-review/VERIFICATION.json').read_text())
assert verification['Passed'] and verification['Checkpoint'] == 10277 and not verification['GoalComplete']
for name, key in [('full/full.trx', 'FullTrxSHA256'), ('focused2/focused.trx', 'FocusedTrxSHA256'), ('full-build.log', 'BuildLogSHA256')]:
    assert hashlib.sha256((proof / name).read_bytes()).hexdigest().upper() == verification[key], name
print('PASS:5888 committed source/tool contents,10277 full tests,207 focused checks and six executed historical negatives.')
print('Historical evidence only: this does not rerun MySQL proofs, verify a new runtime, migrate data, or establish retail completeness.')
