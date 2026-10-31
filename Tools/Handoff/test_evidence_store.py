import gzip, json, tempfile, unittest
from pathlib import Path
from evidence_store import digest, sanitize_json, sanitize_json_lines, sanitize_text, safe_target, restore, lean_member

class EvidenceStoreTests(unittest.TestCase):
    def test_lean_selection_retains_latest_proofs_and_focused_re_without_bulk_data(self):
        for name, category in [('assessment-report/remaining-goal/realm-bank-splits/AuthProof/Program.cs', 'assessment'), ('assessment-report/remaining-goal/vendor-labeled-export/function.c', 'assessment'), ('agent-skills/nexus-change-review/SKILL.md', 'agent-skills')]:
            self.assertTrue(lean_member({'Path': name, 'Category': category}))
        for name, category in [('reference-sql/game.sql', 'reference-sql'), ('decomp-exports/function.c', 'reverse-engineering'), ('assessment-report/remaining-goal/vendor-labeled-export/selected_call_edges.csv', 'assessment'), ('assessment-report/remaining-goal/realm-bank-splits/full/full.trx', 'assessment')]:
            self.assertFalse(lean_member({'Path': name, 'Category': category}))
    def test_redacts_uri_cli_and_inline_credentials_and_is_idempotent(self):
        source = b'''amqp://user:secret@localhost --password=secret Password="secret"; Password=secret; user@private.invalid'''
        clean, fields = sanitize_text(source)
        self.assertNotIn(b'secret', clean); self.assertNotIn(b'user@private.invalid', clean)
        self.assertTrue(fields); self.assertEqual(sanitize_text(clean)[0], clean)

    def test_handles_large_dotted_non_email_records(self):
        data = ('a.' * 40000 + ' no-email').encode()
        self.assertEqual(sanitize_text(data), (data, []))

    def test_json_lines_redaction_preserves_one_record_per_line(self):
        data = b'{"Password":"private","Passed":true}\n{"Cases":4}\n'
        clean, fields = sanitize_json_lines(data)
        records = [json.loads(line) for line in clean.splitlines()]
        self.assertEqual(len(records), 2); self.assertTrue(records[0]['Passed'])
        self.assertEqual(records[1]['Cases'], 4); self.assertNotIn(b'private', clean)
        self.assertEqual(sanitize_json_lines(clean)[0], clean)
    def test_redacts_nested_credentials_without_discarding_proof_counts(self):
        data = json.dumps({'Passed': True, 'Cases': 103, 'Account': {'PasswordHash': 'private', 'Salt': 'private', 'Email': 'person@real.invalid'}, 'ConnectionString': 'private'}).encode()
        cleaned, fields = sanitize_json(data); value = json.loads(cleaned)
        self.assertTrue(value['Passed']); self.assertEqual(value['Cases'], 103)
        self.assertNotIn(b'private', cleaned); self.assertNotIn(b'person@', cleaned); self.assertEqual(len(fields), 4)

    def test_refuses_traversal_absolute_and_drive_paths(self):
        with tempfile.TemporaryDirectory() as folder:
            for name in ['../outside', '/outside', 'C:/outside', 'a\\outside']:
                with self.assertRaises(ValueError): safe_target(Path(folder), name)

    def test_redacts_numeric_auth_material_but_preserves_file_hashes(self):
        data = json.dumps({'Salt': [1, 2, 3], 'KeyData': [4, 5], 'Hash': 'auth-proof', 'SourceSHA256': 'source-proof', 'PasswordPresent': True}).encode()
        cleaned, fields = sanitize_json(data); value = json.loads(cleaned)
        self.assertEqual(value['Salt'], '[REDACTED]')
        self.assertEqual(value['KeyData'], '[REDACTED]')
        self.assertEqual(value['Hash'], '[REDACTED]')
        self.assertEqual(value['SourceSHA256'], 'source-proof')
        self.assertIs(value['PasswordPresent'], True)

    def test_digest_checked_restore_is_idempotent_and_refuses_other_content(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); store = root / 'store'; output = root / 'output'; data = b'public evidence\r\n'; sha = digest(data)
            obj = store / 'objects' / sha[:2] / (sha + '.gz'); obj.parent.mkdir(parents=True); obj.write_bytes(gzip.compress(data, mtime=0))
            row = {'Path': 'assessment/proof.txt', 'PublishedSHA256': sha, 'PublishedBytes': len(data), 'Chunks': [{'SHA256': sha, 'Bytes': len(data)}]}
            restore(store, row, output); restore(store, row, output)
            self.assertEqual((output / row['Path']).read_bytes(), data)
            (output / row['Path']).write_bytes(b'other')
            with self.assertRaises(ValueError): restore(store, row, output)

    def test_restores_a_hashed_object_from_a_pack_offset(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); (root/'packs').mkdir(); data = b'focused native evidence'; sha = digest(data)
            compressed = gzip.compress(data, mtime=0)
            (root/'packs/objects-000.pack').write_bytes(b'prefix'+compressed+b'suffix')
            index = {sha: {'Pack': 'objects-000.pack', 'Offset': 6, 'Bytes': len(compressed)}}
            (root/'object-index.json.gz').write_bytes(gzip.compress(json.dumps(index).encode(), mtime=0))
            row = {'Path': 'native/trace.txt', 'PublishedSHA256': sha, 'PublishedBytes': len(data), 'Chunks': [{'SHA256': sha, 'Bytes': len(data)}]}
            restore(root, row, root/'out')
            self.assertEqual((root/'out/native/trace.txt').read_bytes(), data)

    def test_rejects_corrupt_object_before_publishing_a_member(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); data = b'proof'; sha = digest(data); obj = root / 'objects' / sha[:2] / (sha + '.gz'); obj.parent.mkdir(parents=True); obj.write_bytes(gzip.compress(b'wrong', mtime=0))
            row = {'Path': 'proof.txt', 'PublishedSHA256': sha, 'PublishedBytes': len(data), 'Chunks': [{'SHA256': sha, 'Bytes': len(data)}]}
            with self.assertRaises(ValueError): restore(root, row, root / 'out')
            self.assertFalse((root / 'out/proof.txt').exists())

if __name__ == '__main__': unittest.main()
